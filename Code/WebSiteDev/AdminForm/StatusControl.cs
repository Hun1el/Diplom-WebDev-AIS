using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Windows.Forms;
using WebSiteDev.AdminForm.AddForm;

namespace WebSiteDev.AdminForm
{
    /// <summary>
    /// Пользовательский контрол для управления статусами заказов
    /// </summary>
    public partial class StatusControl : ScalableUserControl
    {
        private DataManipulation dataManipulation;
        private int selectedStatusID = -1;
        private int selectedRowIndex = -1;

        public StatusControl()
        {
            InitializeComponent();
            GetDate();
        }

        /// <summary>
        /// Обработчик загрузки
        /// </summary>
        private void StatusControl_Load(object sender, EventArgs e)
        {
            ClearViewSelection();
        }

        /// <summary>
        /// Загружает все статусы из БД и отображает их в таблице
        /// </summary>
        void GetDate()
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(Data.GetConnectionString()))
                {
                    string SelectCmd = @"SELECT * FROM `Status`";
                    string CountCmd = @"SELECT COUNT(*) FROM Status";

                    con.Open();

                    // Получаем все статусы из БД
                    MySqlCommand cmd1 = new MySqlCommand(SelectCmd, con);
                    cmd1.ExecuteNonQuery();

                    // Заполняем таблицу данными
                    MySqlDataAdapter da = new MySqlDataAdapter(cmd1);
                    DataTable dt = new DataTable();

                    da.Fill(dt);

                    dataGridView1.DataSource = dt;

                    dataGridView1.Columns["StatusID"].Visible = false;
                    dataGridView1.Columns["StatusName"].HeaderText = "Наименование статуса";
                    dataGridView1.Columns["StatusName"].SortMode = DataGridViewColumnSortMode.NotSortable;

                    dataManipulation = new DataManipulation(dt);

                    // Получаем количество статусов и выводим в метку
                    MySqlCommand cmd2 = new MySqlCommand(CountCmd, con);
                    int resultcount = Convert.ToInt32(cmd2.ExecuteScalar());

                    label1.Text = $"Количество записей: {resultcount}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            dataManipulation.ApplySearchStatus(textBox1);
            dataManipulation.UpdateRecordCountLabel(label1);

            InputRest.FirstLetter(textBox1);

            ClearViewSelection();
        }

        /// <summary>
        /// Обработчик кнопки сброса фильтров
        /// </summary>
        private void button4_Click(object sender, EventArgs e)
        {
            // Сбрасываем все фильтры
            dataManipulation.ResetFilters(textSearch: textBox1);

            ClearViewSelection();
        }

        /// <summary>
        /// Обработчик клика по ячейке в таблице
        /// </summary>
        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                selectedRowIndex = e.RowIndex; // Сохраняем индекс выбранной строки
                selectedStatusID = Convert.ToInt32(dataGridView1.Rows[e.RowIndex].Cells["StatusID"].Value); // Получаем ID статуса из первого столбца
                button1.Enabled = true;
                button7.Enabled = true;
            }
        }

        /// <summary>
        /// Ограничивает ввод в поле поиска только русскими буквами
        /// </summary>
        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            InputRest.OnlyRussian(e);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string statusName = dataGridView1.Rows[selectedRowIndex].Cells["StatusName"].Value.ToString();

            EditStatusForm editStatusForm = new EditStatusForm(selectedStatusID, statusName);
            editStatusForm.ShowDialog();

            GetDate();

            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (Convert.ToInt32(row.Cells["StatusID"].Value) == selectedStatusID)
                {
                    row.Selected = true;
                    selectedRowIndex = row.Index;
                    break;
                }
            }
        }

        /// <summary>
        /// Обработчик кнопки удаления выбранного статуса
        /// </summary>
        private void button7_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show("Вы действительно хотите удалить статус?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                // Удаляем статус из БД
                if (DataDelete.DeleteStatus(selectedStatusID))
                {
                    MessageBox.Show("Статус успешно удален!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    GetDate();
                    ClearViewSelection();
                }
            }
        }

        /// <summary>
        /// Очищает поля редактирования и сбрасывает ID выбранного статуса
        /// </summary>
        private void ClearViewSelection()
        {
            selectedStatusID = -1;
            selectedRowIndex = -1;
            dataGridView1.ClearSelection();
            button1.Enabled = false;
            button7.Enabled = false;
        }
    }
}