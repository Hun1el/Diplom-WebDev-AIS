using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Windows.Forms;
using WebSiteDev.AddForm;
using WebSiteDev.AdminForm.AddForm;

namespace WebSiteDev.AdminForm
{
    /// <summary>
    /// Пользовательский контрол для управления категориями товаров
    /// </summary>
    public partial class CategoryControl : ScalableUserControl
    {
        private DataManipulation dataManipulation;
        private int selectedCategoryID = -1;
        private int selectedRowIndex = -1;

        public CategoryControl()
        {
            InitializeComponent();
            GetDate();
        }

        /// <summary>
        /// Обработчик загрузки
        /// </summary>
        private void CategoryControl_Load(object sender, EventArgs e)
        {
            comboBox3.SelectedIndex = 0;
            ClearViewSelection();
        }

        /// <summary>
        /// Загружает все категории из БД и отображает их в таблице
        /// </summary>
        public void GetDate()
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(Data.GetConnectionString()))
                {
                    string SelectCmd = @"SELECT * FROM Category";
                    string CountCmd = @"SELECT COUNT(*) FROM Category";

                    con.Open();

                    // Получаем все категории из БД
                    MySqlCommand cmd1 = new MySqlCommand(SelectCmd, con);
                    cmd1.ExecuteNonQuery();

                    // Заполняем таблицу данными
                    MySqlDataAdapter da = new MySqlDataAdapter(cmd1);
                    DataTable dt = new DataTable();

                    da.Fill(dt);

                    dataGridView1.DataSource = dt;

                    dataGridView1.Columns["CategoryID"].Visible = false;
                    dataGridView1.Columns["CategoryName"].HeaderText = "Наименование категории";
                    dataGridView1.Columns["CategoryName"].SortMode = DataGridViewColumnSortMode.NotSortable;

                    dataManipulation = new DataManipulation(dt);

                    // Получаем количество категорий и выводим в метку
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

        /// <summary>
        /// Обработчик изменения текста в поле поиска
        /// </summary>
        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            dataManipulation.ApplyAllCategory(comboBox3, textBox1);
            dataManipulation.UpdateRecordCountLabel(label1);
            InputRest.FirstLetter(textBox1);

            ClearViewSelection();
        }

        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                selectedRowIndex = e.RowIndex; // Сохраняем индекс выбранной строки
                selectedCategoryID = Convert.ToInt32(dataGridView1.Rows[e.RowIndex].Cells["CategoryID"].Value); // Получаем ID роли из первого столбца
                button1.Enabled = true;
                button7.Enabled = true;
            }
        }

        /// <summary>
        /// Обработчик кнопки добавления новой категории
        /// Открывает диалоговую форму и перезагружает данные
        /// </summary>
        private void button2_Click(object sender, EventArgs e)
        {
            AddEditCategoryForm addEditCategoryForm = new AddEditCategoryForm(FormMode.Add);
            addEditCategoryForm.ShowDialog();

            GetDate();
        }

        /// <summary>
        /// Обработчик кнопки сброса фильтров
        /// </summary>
        private void button4_Click(object sender, EventArgs e)
        {
            dataManipulation.ResetFilters(textSearch: textBox1, comboSort: comboBox3);
            dataManipulation.ApplyAllCategory(comboBox3, textBox1);

            ClearViewSelection();
        }

        /// <summary>
        /// Обработчик изменения выбора в комбо-боксе сортировки
        /// </summary>
        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            dataManipulation.ApplyAllCategory(comboBox3, textBox1);
            dataManipulation.UpdateRecordCountLabel(label1);

            ClearViewSelection();
        }

        /// <summary>
        /// Ограничивает ввод в поле поиска только допустимыми символами
        /// </summary>
        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            InputRest.CategoryInput(e);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string categoryName = dataGridView1.Rows[selectedRowIndex].Cells["CategoryName"].Value.ToString();

            AddEditCategoryForm addEditCategoryForm = new AddEditCategoryForm(FormMode.Edit, selectedCategoryID, categoryName);
            addEditCategoryForm.ShowDialog();

            GetDate();

            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (Convert.ToInt32(row.Cells["CategoryID"].Value) == selectedCategoryID)
                {
                    row.Selected = true;
                    selectedRowIndex = row.Index;
                    break;
                }
            }
        }

        /// <summary>
        /// Обработчик кнопки удаления выбранной категории
        /// </summary>
        private void button7_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show("Вы действительно хотите удалить категорию?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                // Удаляем категорию из БД
                if (DataDelete.DeleteCategory(selectedCategoryID))
                {
                    MessageBox.Show("Категория успешно удалена!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    GetDate();
                    ClearViewSelection();
                }
            }
        }

        /// <summary>
        /// Очищает поля редактирования и сбрасывает ID выбранной категории
        /// </summary>
        private void ClearViewSelection()
        {
            selectedCategoryID = -1;
            selectedRowIndex = -1;
            dataGridView1.ClearSelection();
            button1.Enabled = false;
            button7.Enabled = false;
        }
    }
}