using MySql.Data.MySqlClient;
using System;
using System.IO;
using System.Linq;

namespace WebSiteDev.Service
{
    public static class Backup
    {
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
    }
}
