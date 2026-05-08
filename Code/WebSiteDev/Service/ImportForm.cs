using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Windows.Forms;

namespace WebSiteDev.Service
{
    public partial class ImportForm : ScalableForm
    {
        public ImportForm()
        {
            InitializeComponent();
        }

        private void ImportForm_Load(object sender, EventArgs e)
        {
            comboBox2.Items.AddRange(new string[] { ";", ",", ":", "|" });
            comboBox2.SelectedIndex = 0;
            LabelColor.ApplyRedStar(this);

            checkBox1.Checked = true;

            LoadTables();
        }

        private void DisableGridSorting()
        {
            for (int i = 0; i < dataGridView1.Columns.Count; i++)
            {
                dataGridView1.Columns[i].SortMode = DataGridViewColumnSortMode.NotSortable;
            }
        }

        private void LoadTables()
        {
            try
            {
                comboBox1.Items.Clear();
                comboBox1.Items.Add("- -");

                using (MySqlConnection con = new MySqlConnection(Data.GetConnectionString()))
                {
                    string ShowCmd = @"SHOW TABLES;";

                    con.Open();

                    MySqlCommand cmd = new MySqlCommand(ShowCmd, con);

                    using (MySqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            comboBox1.Items.Add(dr.GetValue(0).ToString());
                        }
                    }
                }

                comboBox1.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось загрузить таблицы\nОшибка: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadColumns(string TableName)
        {
            try
            {
                DataTable dt = new DataTable();

                using (MySqlConnection con = new MySqlConnection(Data.GetConnectionString()))
                {
                    string ColumnCmd = @"SHOW COLUMNS FROM `" + TableName + "`;";

                    con.Open();

                    MySqlCommand cmd = new MySqlCommand(ColumnCmd, con);

                    using (MySqlDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            dt.Columns.Add(rdr.GetValue(0).ToString());
                        }
                    }
                }

                dataGridView1.DataSource = dt;

                DisableGridSorting();
                UpdateGridLayout();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось загрузить колонки таблицы\nОшибка: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (comboBox1.SelectedItem == null || comboBox1.SelectedItem.ToString() == "- -")
                {
                    dataGridView1.DataSource = null;
                    dataGridView1.Columns.Clear();

                    return;
                }

                LoadColumns(comboBox1.SelectedItem.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось загрузить колонки\nОшибка: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Title = "Выберите файл для импорта";
                openFileDialog.Filter = "CSV-файлы (*.csv)|*.csv";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    textBox1.Text = openFileDialog.FileName;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка выбора файла\nОшибка: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (comboBox1.SelectedItem == null || comboBox1.SelectedItem.ToString() == "- -" || string.IsNullOrEmpty(textBox1.Text))
            {
                MessageBox.Show("Заполните поля, отмеченные *", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                string TableName = comboBox1.SelectedItem.ToString();
                string separator = comboBox2.SelectedItem.ToString();
                string FilePath = textBox1.Text;
                bool SkipHeader = checkBox1.Checked;

                int UploadedRows = Service.Import(TableName, separator, FilePath, SkipHeader);

                MessageBox.Show("Импорт успешно завершён!\nЗагружено строк: " + UploadedRows.ToString(), "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось импортировать данные\nОшибка: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show("Вы действительно хотите выйти?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }

        private void UpdateGridLayout()
        {
            if (dataGridView1.Columns.Count == 0)
            {
                return;
            }

            int TotalWidth = 0;
            int i;

            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

            for (i = 0; i < dataGridView1.Columns.Count; i++)
            {
                int PreferredWidth = dataGridView1.Columns[i].GetPreferredWidth(DataGridViewAutoSizeColumnMode.AllCells, true);

                dataGridView1.Columns[i].Width = PreferredWidth;
                TotalWidth += PreferredWidth;
            }

            if (TotalWidth < dataGridView1.ClientSize.Width)
            {
                dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            }
            else
            {
                dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            }
        }

        private void ImportForm_SizeChanged(object sender, EventArgs e)
        {
            UpdateGridLayout();
        }
    }
}