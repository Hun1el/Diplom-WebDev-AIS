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
        /// Создание ручной резевной копии базы данных
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

            return FileName;
        }

        /// <summary>
        /// Восстановление базы данных из sql файла
        /// </summary>
        public static bool RestoreBackup(string FilePath)
        {
            // Проверка наличия файла
            if (!File.Exists(FilePath))
            {
                MessageBox.Show("Файл для восстановления не найден!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            bool hasConnection = false;
            bool hasCreateTable = false;

            // Проверка подключения к серверу БД БЕЗ указания конкретной базы
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

            // Проверка наличия CREATE TABLE в SQL-файле
            string fileContent = File.ReadAllText(FilePath);
            hasCreateTable = fileContent.IndexOf("CREATE TABLE", StringComparison.OrdinalIgnoreCase) >= 0;

            // Формируем предупреждения
            var warnings = new List<string>();
            if (!hasConnection)
            {
                warnings.Add("Нет подключения к серверу баз данных - восстановление может не выполниться!");
            }
            if (!hasCreateTable)
            {
                warnings.Add("В файле не обнаружено инструкций CREATE TABLE — таблицы не будут созданы заново.");
            }

            if (warnings.Count > 0)
            {
                string message = string.Join("\n", warnings) + "\n\nВсё равно продолжить восстановление?";
                var result = MessageBox.Show(message, "Предупреждение", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                if (result != DialogResult.Yes)
                {
                    return false; // Отмена пользователем
                }
            }

            // Если подключение к серверу есть принудительно создаем нужную базу данных если её нет
            if (hasConnection)
            {
                try
                {
                    using (MySqlConnection con = new MySqlConnection(Data.GetConnectionStringNoDB()))
                    {
                        con.Open();
                        // Получаем имя БД из настроек проекта
                        string dbName = Properties.Settings.Default.DbName;
                        using (MySqlCommand cmd = new MySqlCommand($"CREATE DATABASE IF NOT EXISTS `{dbName}`;", con))
                        {
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
                catch 
                {

                }
            }

            // Восстановление
            using (MySqlConnection con = new MySqlConnection(Data.GetConnectionString()))
            {
                using (MySqlCommand cmd = con.CreateCommand())
                {
                    using (MySqlBackup mb = new MySqlBackup(cmd))
                    {
                        con.Open();
                        mb.ImportFromFile(FilePath);
                    }
                }
            }

            return true; // Восстановление прошло успешно
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
                    using (StreamWriter writer = new StreamWriter(FilePath, false, Encoding.GetEncoding("windows-1251")))
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

            using (MySqlConnection con = new MySqlConnection(Data.GetConnectionStringInFile()))
            {
                con.Open();

                MySqlBulkLoader loader = new MySqlBulkLoader(con);
                loader.Local = true;
                loader.TableName = TableName;
                loader.FileName = FilePath;
                loader.FieldTerminator = separator;
                loader.LineTerminator = "\n";
                loader.CharacterSet = "cp1251";

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
    }
}
