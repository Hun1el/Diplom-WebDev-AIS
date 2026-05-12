using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Windows.Forms;
using WebSiteDev.AdminForm.AddForm;

namespace WebSiteDev.AdminForm
{
    /// <summary>
    /// Пользовательский контрол для управления ролями пользователей
    /// </summary>
    public partial class RoleControl : ScalableUserControl
    {
        private DataManipulation dataManipulation;
        private int selectedRoleID = -1;
        private int selectedRowIndex = -1;

        public RoleControl()
        {
            InitializeComponent();
            GetDate();
        }

        /// <summary>
        /// Обработчик загрузки
        /// </summary>
        private void RoleControl_Load(object sender, EventArgs e)
        {
            ClearViewSelection();
        }

        /// <summary>
        /// Загружает все роли из БД и отображает их в таблице
        /// </summary>
        void GetDate()
        {
            using (MySqlConnection con = new MySqlConnection(Data.GetConnectionString()))
            {
                con.Open();

                // Получаем все роли из БД
                MySqlCommand cmd = new MySqlCommand("SELECT * FROM `Role`", con);
                cmd.ExecuteNonQuery();

                // Заполняем таблицу данными
                MySqlDataAdapter da = new MySqlDataAdapter(cmd);
                DataTable dt = new DataTable();

                da.Fill(dt);

                dataGridView1.DataSource = dt;

                dataGridView1.Columns["RoleID"].Visible = false;
                dataGridView1.Columns["RoleName"].HeaderText = "Наименование роли";
                dataGridView1.Columns["RoleName"].SortMode = DataGridViewColumnSortMode.NotSortable;

                dataManipulation = new DataManipulation(dt);

                // Получаем количество ролей и выводим в метку
                MySqlCommand count = new MySqlCommand("SELECT COUNT(*) FROM Role", con);
                int resultcount = Convert.ToInt32(count.ExecuteScalar());
                label1.Text = $"Количество записей: {resultcount}";
            }
        }

        /// <summary>
        /// Обработчик изменения текста в поле поиска
        /// </summary>
        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            dataManipulation.ApplySearchRole(textBox1);
            dataManipulation.UpdateRecordCountLabel(label1);

            InputRest.FirstLetter(textBox1);

            ClearViewSelection();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // Проверяем что роль выбрана
            if (selectedRoleID == -1)
            {
                MessageBox.Show("Выберите роль для редактирования!", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string roleName = dataGridView1.Rows[selectedRowIndex].Cells["RoleName"].Value.ToString();

            EditRoleForm editRoleForm = new EditRoleForm(selectedRoleID, roleName);
            editRoleForm.ShowDialog();
            
            GetDate();

            if (selectedRowIndex >= 0 && selectedRowIndex < dataGridView1.Rows.Count)
            {
                dataGridView1.Rows[selectedRowIndex].Selected = true;
            }
        }

        /// <summary>
        /// Обработчик кнопки сброса фильтров
        /// Возвращает таблицу в исходное состояние
        /// </summary>
        private void button4_Click(object sender, EventArgs e)
        {
            dataManipulation.ResetFilters(textSearch: textBox1);
            ClearViewSelection();
        }

        /// <summary>
        /// Ограничивает ввод в поле поиска только русскими буквами
        /// </summary>
        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            InputRest.OnlyRussian(e);
        }

        /// <summary>
        /// Обработчик клика по ячейке в таблице
        /// </summary>
        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                selectedRowIndex = e.RowIndex; // Сохраняем индекс выбранной строки
                selectedRoleID = Convert.ToInt32(dataGridView1.Rows[e.RowIndex].Cells["RoleID"].Value); // Получаем ID роли из первого столбца
            }
        }

        /// <summary>
        /// Обработчик кнопки удаления выбранной роли
        /// </summary>
        private void button7_Click(object sender, EventArgs e)
        {
            // Проверяем что роль выбрана
            if (selectedRoleID == -1)
            {
                MessageBox.Show("Выберите роль для удаления!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show("Вы действительно хотите удалить роль?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                // Удаляем роль из БД
                if (DataDelete.DeleteRole(selectedRoleID))
                {
                    MessageBox.Show("Роль успешно удалена!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    GetDate();
                    ClearViewSelection();
                }
            }
        }

        private void ClearViewSelection()
        {
            selectedRoleID = -1;
            selectedRowIndex = -1;
            dataGridView1.ClearSelection();
        }
    }
}