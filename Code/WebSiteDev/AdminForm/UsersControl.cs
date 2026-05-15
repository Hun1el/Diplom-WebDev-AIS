using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Text;
using System.Windows.Forms;
using WebSiteDev.AddForm;

namespace WebSiteDev.AdminForm
{
    /// <summary>
    /// Контрол для управления пользователями с маскированием чувствительных данных
    /// </summary>
    public partial class UsersControl : ScalableUserControl
    {
        private DataManipulation dataManipulation;
        private int selectedUserID = -1;
        private int currentUserID = 0;

        static readonly Random rand = new Random();

        private int lastRevealedRowIndex = -1;
        private DataSecurity dataSecurity = new DataSecurity();

        public UsersControl(int userID = 0)
        {
            InitializeComponent();
            currentUserID = userID;
            GetDate();
        }

        private void UsersControl_Load(object sender, EventArgs e)
        {
            dataGridView1.Columns["UserID"].Visible = false;
            comboBox3.SelectedIndex = 0;
            ClearViewSelection();

            // Таймер для скрытия данных через 20 секунд
            timer1.Interval = 20000;
            timer1.Stop();
        }

        /// <summary>
        /// Загружает всех пользователей из БД с информацией об их ролях
        /// </summary>
        void GetDate()
        {
            using (MySqlConnection con = new MySqlConnection(Data.GetConnectionString()))
            {
                string SelectCmd = @"SELECT u.UserID, u.Surname, u.FirstName, u.MiddleName, u.UserLogin,
                                                     u.UserPassword, r.RoleName AS RoleName, u.PhoneNumber, u.RoleID
                                              FROM Users u
                                              JOIN Role r ON u.RoleID = r.RoleID";
                string CountCmd = @"SELECT COUNT(*) FROM Users";

                con.Open();

                MySqlCommand cmd = new MySqlCommand(SelectCmd, con);
                cmd.ExecuteNonQuery();

                MySqlDataAdapter da = new MySqlDataAdapter(cmd);
                DataTable dt = new DataTable();

                da.Fill(dt);

                // Сохраняем оригинальные данные для маскирования
                dataSecurity.LoadOriginalData(dt);
                lastRevealedRowIndex = -1;

                dataGridView1.DataSource = dt;
                dataGridView1.Columns["UserID"].Visible = false;
                dataGridView1.Columns["RoleID"].Visible = false;
                dataGridView1.Columns["UserPassword"].Visible = false;
                dataGridView1.Columns["Surname"].HeaderText = "Фамилия";
                dataGridView1.Columns["FirstName"].HeaderText = "Имя";
                dataGridView1.Columns["MiddleName"].HeaderText = "Отчество";
                dataGridView1.Columns["UserLogin"].HeaderText = "Логин";
                dataGridView1.Columns["UserPassword"].HeaderText = "Пароль";
                dataGridView1.Columns["RoleName"].HeaderText = "Роль";
                dataGridView1.Columns["PhoneNumber"].HeaderText = "Телефон";

                dataGridView1.Columns["Surname"].SortMode = DataGridViewColumnSortMode.NotSortable;
                dataGridView1.Columns["FirstName"].SortMode = DataGridViewColumnSortMode.NotSortable;
                dataGridView1.Columns["MiddleName"].SortMode = DataGridViewColumnSortMode.NotSortable;
                dataGridView1.Columns["UserLogin"].SortMode = DataGridViewColumnSortMode.NotSortable;
                dataGridView1.Columns["RoleName"].SortMode = DataGridViewColumnSortMode.NotSortable;
                dataGridView1.Columns["PhoneNumber"].SortMode = DataGridViewColumnSortMode.NotSortable;

                dataManipulation = new DataManipulation(dt);

                dataManipulation.FillComboBoxWithRoles(comboBox1, "Роль не выбрана");

                // Показываем количество пользователей
                MySqlCommand count = new MySqlCommand(CountCmd, con);
                int resultcount = Convert.ToInt32(count.ExecuteScalar());

                label1.Text = $"Количество записей: {resultcount}";
            }
        }

        /// <summary>
        /// Форматирует отображение ячеек показывает оригинальные данные для открытой строки или маскирует
        /// </summary>
        private void DataGridView1_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0)
            {
                return;
            }

            int userID = Convert.ToInt32(dataGridView1.Rows[e.RowIndex].Cells["UserID"].Value);

            // Если строка открыта показываем оригинальные значения
            if (e.RowIndex == lastRevealedRowIndex)
            {
                if (dataGridView1.Columns[e.ColumnIndex].Name == "UserLogin")
                {
                    string original = dataSecurity.GetOriginalLogin(userID);

                    if (original != null)
                    {
                        e.Value = original;
                        e.FormattingApplied = true;
                    }
                }
                else if (dataGridView1.Columns[e.ColumnIndex].Name == "PhoneNumber")
                {
                    string original = dataSecurity.GetOriginalPhone(userID);

                    if (original != null)
                    {
                        e.Value = original;
                        e.FormattingApplied = true;
                    }
                }
                else if (dataGridView1.Columns[e.ColumnIndex].Name == "FirstName")
                {
                    string original = dataSecurity.GetOriginalFirstName(userID);

                    if (original != null)
                    {
                        e.Value = original;
                        e.FormattingApplied = true;
                    }
                }
                else if (dataGridView1.Columns[e.ColumnIndex].Name == "MiddleName")
                {
                    string original = dataSecurity.GetOriginalMiddleName(userID);

                    if (original != null)
                    {
                        e.Value = original;
                        e.FormattingApplied = true;
                    }
                }
                return;
            }

            // Маскируем чувствительные данные для других строк
            if (dataGridView1.Columns[e.ColumnIndex].Name == "UserLogin")
            {
                string original = dataSecurity.GetOriginalLogin(userID);

                if (e.Value != null && original != null)
                {
                    e.Value = DataSecurity.MaskLogin(original);
                    e.FormattingApplied = true;
                }
            }
            else if (dataGridView1.Columns[e.ColumnIndex].Name == "PhoneNumber")
            {
                string original = dataSecurity.GetOriginalPhone(userID);

                if (e.Value != null && original != null)
                {
                    e.Value = DataSecurity.MaskPhone(original);
                    e.FormattingApplied = true;
                }
            }
            else if (dataGridView1.Columns[e.ColumnIndex].Name == "FirstName")
            {
                string original = dataSecurity.GetOriginalFirstName(userID);

                if (e.Value != null && original != null)
                {
                    e.Value = DataSecurity.MaskName(original);
                    e.FormattingApplied = true;
                }
            }
            else if (dataGridView1.Columns[e.ColumnIndex].Name == "MiddleName")
            {
                string original = dataSecurity.GetOriginalMiddleName(userID);

                if (e.Value != null && original != null)
                {
                    e.Value = DataSecurity.MaskName(original);
                    e.FormattingApplied = true;
                }
            }
        }

        /// <summary>
        /// Двойной клик на ячейку показывает/скрывает данные на 20 секунд
        /// </summary>
        private void DataGridView1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
            {
                return;
            }

            // Если нажали на уже открытую строку закрываем её
            if (e.RowIndex == lastRevealedRowIndex)
            {
                lastRevealedRowIndex = -1;
                dataGridView1.InvalidateRow(e.RowIndex);
                timer1.Stop();
                return;
            }

            // Закрываем предыдущую открытую строку если она была
            if (lastRevealedRowIndex >= 0)
            {
                int previousRow = lastRevealedRowIndex;
                lastRevealedRowIndex = -1;
                dataGridView1.InvalidateRow(previousRow);
            }

            // Открываем новую строку
            lastRevealedRowIndex = e.RowIndex;
            dataGridView1.InvalidateRow(e.RowIndex);

            // Перезапускаем таймер
            timer1.Stop();
            timer1.Start();
        }

        /// <summary>
        /// Изменение текста в поле поиска применяет фильтры
        /// </summary>
        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            dataManipulation.ApplyAllUser(comboBox3, comboBox1, textBox1);
            dataManipulation.UpdateRecordCountLabel(label1);
            InputRest.FirstLetter(textBox1);

            ClearViewSelection();
        }

        /// <summary>
        /// Кнопка добавить нового пользователя
        /// </summary>
        private void button2_Click(object sender, EventArgs e)
        {
            AddEditUsersForm addUsersForm = new AddEditUsersForm(dataManipulation);
            addUsersForm.ShowDialog();
            GetDate();
            ClearViewSelection();
        }

        /// <summary>
        /// Кнопка сбросить фильтры
        /// </summary>
        private void button4_Click(object sender, EventArgs e)
        {
            dataManipulation.ResetFilters(comboSort: comboBox3, comboFilter: comboBox1, textSearch: textBox1);
            dataManipulation.ApplyAllUser(comboBox3, comboBox1, textBox1);

            ClearViewSelection();
        }

        /// <summary>
        /// Изменение выбора роли в фильтре применяет фильтр
        /// </summary>
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            dataManipulation.ApplyAllUser(comboBox3, comboBox1, textBox1);
            dataManipulation.UpdateRecordCountLabel(label1);

            ClearViewSelection();
        }

        /// <summary>
        /// Изменение сортировки применяет новый порядок
        /// </summary>
        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            dataManipulation.ApplyAllUser(comboBox3, comboBox1, textBox1);
            dataManipulation.UpdateRecordCountLabel(label1);

            ClearViewSelection();
        }

        // Ограничение ввода в поле поиска только русский и дефис
        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            InputRest.OnlyRussianAndDash(e, textBox1);
        }

        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                selectedUserID = Convert.ToInt32(dataGridView1.Rows[e.RowIndex].Cells["UserID"].Value);

                button6.Enabled = true;
                button7.Enabled = true;
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            if (selectedUserID == -1)
            {
                MessageBox.Show("Выберите пользователя!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Фамилия не маскируется — берём напрямую из таблицы
            string surname = dataGridView1.SelectedRows[0].Cells["Surname"].Value.ToString();

            string firstName = dataSecurity.GetOriginalFirstName(selectedUserID);

            if (firstName == null)
            {
                firstName = dataGridView1.SelectedRows[0].Cells["FirstName"].Value.ToString();
            }

            string middleName = dataSecurity.GetOriginalMiddleName(selectedUserID);

            if (middleName == null)
            {
                middleName = dataGridView1.SelectedRows[0].Cells["MiddleName"].Value.ToString();
            }

            string login = dataSecurity.GetOriginalLogin(selectedUserID);

            if (login == null)
            {
                login = dataGridView1.SelectedRows[0].Cells["UserLogin"].Value.ToString();
            }

            string phone = dataSecurity.GetOriginalPhone(selectedUserID);

            if (phone == null)
            {
                phone = dataGridView1.SelectedRows[0].Cells["PhoneNumber"].Value.ToString();
            }

            int roleID = Convert.ToInt32(dataGridView1.SelectedRows[0].Cells["RoleID"].Value);

            // Открываем форму редактирования
            AddEditUsersForm form = new AddEditUsersForm(dataManipulation, selectedUserID, surname, firstName, middleName, login, phone, roleID);

            if (form.ShowDialog() == DialogResult.OK)
            {
                GetDate();
                ClearViewSelection();
            }
        }

        /// <summary>
        /// Кнопка удалить выбранного пользователя
        /// </summary>
        private void button7_Click(object sender, EventArgs e)
        {
            if (selectedUserID == -1)
            {
                MessageBox.Show("Выберите пользователя для удаления!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Запрашиваем подтверждение
            var result = MessageBox.Show("Вы действительно хотите удалить пользователя?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.No)
            {
                return;
            }

            // Удаляем пользователя из БД
            if (DataDelete.DeleteUser(selectedUserID, currentUserID))
            {
                MessageBox.Show("Пользователь успешно удален!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                GetDate();
                ClearViewSelection();
            }
        }

        /// <summary>
        /// Таймер срабатывает через 20 секунд скрывает открытые чувствительные данные
        /// </summary>
        private void timer1_Tick(object sender, EventArgs e)
        {
            timer1.Stop();

            // Скрываем открытые данные
            if (lastRevealedRowIndex >= 0)
            {
                int rowToHide = lastRevealedRowIndex;
                lastRevealedRowIndex = -1;
                dataGridView1.InvalidateRow(rowToHide);
            }
        }

        private void ClearViewSelection()
        {
            selectedUserID = -1;
            lastRevealedRowIndex = -1;
            dataGridView1.ClearSelection();
            button6.Enabled = false;
            button7.Enabled = false;
        }
    }
}
