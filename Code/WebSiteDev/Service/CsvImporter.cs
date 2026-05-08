using MySql.Data.MySqlClient;
using System;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace WebSiteDev.Service
{
    public static class CsvImporter
    {
        /// <summary>
        /// Проверяет файл перед импортом:
        /// - файл существует
        /// - разделитель правильный (число колонок совпадает с таблицей)
        /// </summary>
        public static void ValidateFile(string FilePath, string separator, string TableName, bool SkipHeader)
        {
            if (!File.Exists(FilePath))
            {
                throw new Exception("Файл не найден:\n" + FilePath);
            }

            int TableColumnCount = GetTableColumnCount(TableName);
            int FileColumnCount = GetFileColumnCount(FilePath, separator, SkipHeader);

            if (FileColumnCount != TableColumnCount)
            {
                throw new Exception(
                    "Неверный разделитель или структура файла.\n" +
                    "Колонок в таблице: " + TableColumnCount + "\n" +
                    "Колонок в файле: " + FileColumnCount + "\n\n" +
                    "Проверьте выбранный разделитель."
                );
            }
        }

        /// <summary>
        /// Получает количество колонок в таблице БД
        /// </summary>
        private static int GetTableColumnCount(string TableName)
        {
            int count = 0;

            using (MySqlConnection con = new MySqlConnection(Data.GetConnectionString()))
            {
                string ShowCmd = @"SHOW COLUMNS FROM `" + TableName + "`;";

                con.Open();

                MySqlCommand cmd = new MySqlCommand(ShowCmd, con);

                using (MySqlDataReader rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        /// <summary>
        /// Считает количество колонок в первой строке данных файла
        /// </summary>
        private static int GetFileColumnCount(string FilePath, string separator, bool SkipHeader)
        {
            using (StreamReader reader = new StreamReader(FilePath, Encoding.GetEncoding("windows-1251")))
            {
                string line = null;

                // Пропускаем заголовок если нужно
                if (SkipHeader && !reader.EndOfStream)
                {
                    reader.ReadLine();
                }

                // Читаем первую строку данных
                while (!reader.EndOfStream)
                {
                    line = reader.ReadLine();

                    if (line != null && line.Trim() != "")
                    {
                        break;
                    }
                }

                if (line == null || line.Trim() == "")
                {
                    throw new Exception("Файл пустой или не содержит данных для импорта!");
                }

                return line.Split(new string[] { separator }, StringSplitOptions.None).Length;
            }
        }
    }
}