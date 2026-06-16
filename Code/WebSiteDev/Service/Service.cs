using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace WebSiteDev.Service
{
    public static class Service
    {
        public static (string ErrorMessage, string ErrorCode) CanOpenForm()
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(Data.GetConnectionString()))
                {
                    con.Open();
                }

                return (null, null);
            }
            catch (MySqlException ex)
            {
                return ("Ошибка подключения: " + ex.Message, ex.Number.ToString());
            }
        }

        /// <summary>
        /// Создание ручной резервной копии базы данных
        /// </summary>
        public static string MakeBackup()
        {
            string backupFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "WebShop",
                "Backups"
            );

            if (!Directory.Exists(backupFolder))
            {
                Directory.CreateDirectory(backupFolder);
            }

            string FileName = Path.Combine(backupFolder, $"backup_{DateTime.Now:yyyyMMdd_HHmmss_}{DateTime.Now.Millisecond}.sql");

            using (MySqlConnection con = new MySqlConnection(Data.GetConnectionString()))
            {
                using (MySqlCommand cmd = con.CreateCommand())
                {
                    using (MySqlBackup backup = new MySqlBackup(cmd))
                    {
                        con.Open();
                        backup.ExportToFile(FileName);
                    }
                }
            }

            string originalContent = File.ReadAllText(FileName, Encoding.UTF8);
            string fixedContent = "SET NAMES utf8mb4;\n" + originalContent;
            File.WriteAllText(FileName, fixedContent, new UTF8Encoding(false));

            return FileName;
        }

        /// <summary>
        /// Восстановление базы данных из sql файла
        /// </summary>
        public static bool RestoreBackup(string FilePath)
        {
            if (!File.Exists(FilePath))
            {
                MessageBox.Show("Файл для восстановления не найден!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            bool hasConnection = false;

            using (MySqlConnection test = new MySqlConnection(Data.GetConnectionStringNoDB()))
            {
                try
                {
                    test.Open();
                    hasConnection = true;
                }
                catch (MySqlException)
                {
                }
            }

            if (!hasConnection)
            {
                MessageBox.Show("Нет подключения к серверу баз данных.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            string fileContent = File.ReadAllText(FilePath, Encoding.UTF8);

            bool hasCreateTable = fileContent.IndexOf("CREATE TABLE", StringComparison.OrdinalIgnoreCase) >= 0;
            
            if (!hasCreateTable)
            {
                var result = MessageBox.Show("В файле не обнаружено инструкций CREATE TABLE.\nВсё равно продолжить?", "Предупреждение", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                
                if (result != DialogResult.Yes)
                {
                    return false;
                }
            }

            if (!fileContent.TrimStart().StartsWith("SET NAMES", StringComparison.OrdinalIgnoreCase))
            {
                fileContent = "SET NAMES utf8mb4;\n" + fileContent;
            }

            string tempFilePath = Path.GetTempFileName() + ".sql";
            File.WriteAllText(tempFilePath, fileContent, new UTF8Encoding(false));

            try
            {
                // Подключаемся БЕЗ указания базы — дамп сам создаст db67
                using (MySqlConnection con = new MySqlConnection(Data.GetConnectionStringNoDB()))
                {
                    using (MySqlCommand cmd = con.CreateCommand())
                    {
                        using (MySqlBackup mb = new MySqlBackup(cmd))
                        {
                            con.Open();
                            mb.ImportFromFile(tempFilePath);
                        }
                    }
                }
            }
            finally
            {
                try
                {
                    File.Delete(tempFilePath);
                }
                catch
                {

                }
            }

            return true;
        }

        /// <summary>
        /// Экспорт данных из БД в csv
        /// </summary>
        public static void ExportToCSV(string TableName, string separator, string FilePath)
        {
            using (MySqlConnection con = new MySqlConnection(Data.GetConnectionString()))
            {
                string SelectCmd = @"SELECT * FROM `" + TableName + "`";

                con.Open();

                MySqlCommand cmd = new MySqlCommand(SelectCmd, con);

                using (MySqlDataReader rdr = cmd.ExecuteReader())
                {
                    using (StreamWriter writer = new StreamWriter(FilePath, false, Encoding.UTF8))
                    {
                        // Заголовки
                        for (int i = 0; i < rdr.FieldCount; i++)
                        {
                            writer.Write(rdr.GetName(i));

                            if (i < rdr.FieldCount - 1)
                            {
                                writer.Write(separator);
                            }
                        }

                        writer.Write("\n");

                        // Данные
                        while (rdr.Read())
                        {
                            for (int i = 0; i < rdr.FieldCount; i++)
                            {
                                string value = "";

                                if (rdr.IsDBNull(i))
                                {
                                    value = "";
                                }
                                else if (rdr.GetFieldType(i) == typeof(DateTime))
                                {
                                    value = DateTime.Parse(rdr.GetValue(i).ToString()).ToString("yyyy-MM-dd HH:mm");
                                }
                                else if (rdr.GetFieldType(i) == typeof(byte[]))
                                {
                                    value = "";
                                }
                                else
                                {
                                    value = rdr.GetValue(i).ToString().Replace("\r", "");
                                }

                                writer.Write(value);

                                if (i < rdr.FieldCount - 1)
                                {
                                    writer.Write(separator);
                                }
                            }

                            writer.Write("\n");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Импорт данных из csv в БД
        /// </summary>
        public static int Import(string TableName, string separator, string FilePath, bool SkipHeader)
        {
            CsvImporter.ValidateFile(FilePath, separator, TableName, SkipHeader);

            string tempFilePath = Path.GetTempFileName();
            string content = File.ReadAllText(FilePath, Encoding.UTF8);
            File.WriteAllText(tempFilePath, content, new UTF8Encoding(false));

            try
            {
                using (MySqlConnection con = new MySqlConnection(Data.GetConnectionStringInFile()))
                {
                    con.Open();

                    MySqlBulkLoader loader = new MySqlBulkLoader(con);
                    loader.Local = true;
                    loader.TableName = TableName;
                    loader.FileName = tempFilePath;
                    loader.FieldTerminator = separator;
                    loader.LineTerminator = "\n";
                    loader.CharacterSet = "utf8mb4";

                    if (SkipHeader)
                    {
                        loader.NumberOfLinesToSkip = 1;
                    }
                    else
                    {
                        loader.NumberOfLinesToSkip = 0;
                    }

                    return loader.Load();
                }
            }
            finally
            {
                try
                {
                    File.Delete(tempFilePath);
                }
                catch
                {

                }
            }
        }
    }
}