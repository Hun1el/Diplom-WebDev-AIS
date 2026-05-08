using MySql.Data.MySqlClient;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace WebSiteDev.Service
{
    public static class Service
    {
        /// <summary>
        /// Создание ручной резевной копии базы данных
        /// </summary>
        public static string MakeBackup()
        {
            string PathProject = string.Join("\\", Directory.GetCurrentDirectory().Split('\\').TakeWhile(item => item != "bin"));
            
            if (!Directory.Exists($@"{PathProject}\Resources\Backups\"))
            {
                Directory.CreateDirectory($@"{PathProject}\Resources\Backups\");
            }

            string FileName = $@"{PathProject}\Resources\Backups\backup_{DateTime.Now:yyyyMMdd_HHmmss_}{DateTime.Now.Millisecond}.sql";
            
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
        public static void RestoreBackup(string FilePath)
        {
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
                    using (StreamWriter writer = new StreamWriter(FilePath, false, new UTF8Encoding(false)))
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
    }
}
