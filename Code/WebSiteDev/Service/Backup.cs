using MySql.Data.MySqlClient;
using System;
using System.IO;
using System.Linq;

namespace WebSiteDev.Service
{
    public static class Backup
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
        public static void RestoreBackup(string filePath)
        {
            using (MySqlConnection con = new MySqlConnection(Data.GetConnectionString()))
            {
                using (MySqlCommand cmd = con.CreateCommand())
                {
                    using (MySqlBackup mb = new MySqlBackup(cmd))
                    {
                        con.Open();
                        mb.ImportFromFile(filePath);
                    }
                }
            }
        }
    }
}
