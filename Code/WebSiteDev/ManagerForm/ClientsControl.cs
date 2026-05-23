using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Windows.Forms;
using WebSiteDev.AddForm;

namespace WebSiteDev.ManagerForm
{
    public partial class ClientsControl : ScalableUserControl
    {
        private DataManipulation dataManipulation;
        private int selectedClientID = -1;
        private int lastRevealedRowIndex = -1;
        private DataSecurity dataSecurity = new DataSecurity();

        public ClientsControl()
        {
            InitializeComponent();
            GetDate();
        }

        private void ClientsControl_Load(object sender, EventArgs e)
        {
            comboBox3.SelectedIndex = 0;
            ClearViewSelection();

            timer1.Interval = 20000;
            timer1.Stop();
        }

        void GetDate()
        {
            using (MySqlConnection con = new MySqlConnection(Data.GetConnectionString()))
            {
                string SelectCmd = @"SELECT * FROM `Clients`";
                string CountCmd = @"SELECT COUNT(*) FROM Clients";

                con.Open();

                MySqlCommand cmd = new MySqlCommand(SelectCmd, con);
                cmd.ExecuteNonQuery();

                MySqlDataAdapter da = new MySqlDataAdapter(cmd);
                DataTable dt = new DataTable();

                da.Fill(dt);

                dataSecurity.LoadOriginalData(dt, "", "PhoneNumber", "FirstName", "MiddleName");
                lastRevealedRowIndex = -1;

                dataGridView1.DataSource = dt;
                dataGridView1.Columns["ClientID"].Visible = false;
                dataGridView1.Columns["Surname"].HeaderText = "Фамилия";
                dataGridView1.Columns["FirstName"].HeaderText = "Имя";
                dataGridView1.Columns["MiddleName"].HeaderText = "Отчество";
                dataGridView1.Columns["PhoneNumber"].HeaderText = "Телефон";
                dataGridView1.Columns["Email"].HeaderText = "Эл. почта";

                dataGridView1.Columns["Surname"].SortMode = DataGridViewColumnSortMode.NotSortable;
                dataGridView1.Columns["FirstName"].SortMode = DataGridViewColumnSortMode.NotSortable;
                dataGridView1.Columns["MiddleName"].SortMode = DataGridViewColumnSortMode.NotSortable;
                dataGridView1.Columns["PhoneNumber"].SortMode = DataGridViewColumnSortMode.NotSortable;
                dataGridView1.Columns["Email"].SortMode = DataGridViewColumnSortMode.NotSortable;

                dataManipulation = new DataManipulation(dt);

                MySqlCommand count = new MySqlCommand(CountCmd, con);
                int resultcount = Convert.ToInt32(count.ExecuteScalar());

                label1.Text = $"Количество записей: {resultcount}";
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            dataManipulation.ApplyAllClient(comboBox3, textBox1);
            dataManipulation.UpdateRecordCountLabel(label1);
            InputRest.FirstLetter(textBox1);

            ClearViewSelection();
        }

        /// <summary>
        /// Кнопка добавить нового клиента
        /// </summary>
        private void button2_Click(object sender, EventArgs e)
        {
            AddEditClientsForm addClientsForm = new AddEditClientsForm();
            addClientsForm.ShowDialog();
            GetDate();
            ClearViewSelection();
        }

        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            dataManipulation.ApplyAllClient(comboBox3, textBox1);
            dataManipulation.UpdateRecordCountLabel(label1);

            ClearViewSelection();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            dataManipulation.ResetFilters(comboSort: comboBox3, textSearch: textBox1);
            dataManipulation.ApplyAllClient(comboBox3, textBox1);

            ClearViewSelection();
        }

        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
            {
                return;
            }

            selectedClientID = Convert.ToInt32(dataGridView1.Rows[e.RowIndex].Cells["ClientID"].Value);

            if (e.RowIndex == lastRevealedRowIndex)
            {
                timer1.Stop();
                timer1.Start();
            }

            button1.Enabled = true;
            button7.Enabled = true;
        }

        /// <summary>
        /// Кнопка редактировать клиента
        /// </summary>
        private void button1_Click(object sender, EventArgs e)
        {
            if (selectedClientID == -1)
            {
                MessageBox.Show("Выберите клиента!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Фамилия не маскируется берем напрямую из таблицы
            string surname = dataGridView1.SelectedRows[0].Cells["Surname"].Value.ToString();

            string firstName = dataSecurity.GetOriginalFirstName(selectedClientID);

            if (firstName == null)
            {
                firstName = dataGridView1.SelectedRows[0].Cells["FirstName"].Value.ToString();
            }

            string middleName = dataSecurity.GetOriginalMiddleName(selectedClientID);

            if (middleName == null)
            {
                middleName = dataGridView1.SelectedRows[0].Cells["MiddleName"].Value.ToString();
            }

            string phone = dataSecurity.GetOriginalPhone(selectedClientID);

            if (phone == null)
            {
                phone = dataGridView1.SelectedRows[0].Cells["PhoneNumber"].Value.ToString();
            }

            string email = dataGridView1.SelectedRows[0].Cells["Email"].Value.ToString();

            AddEditClientsForm form = new AddEditClientsForm(selectedClientID, surname, firstName, middleName, phone, email);
            form.ShowDialog();

            // Запоминаем ID выделенной записи
            int idToSelect = selectedClientID;
            GetDate();

            // Восстанавливаем выделение
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (Convert.ToInt32(row.Cells["ClientID"].Value) == idToSelect)
                {
                    dataGridView1.CurrentCell = row.Cells["Surname"];
                    row.Selected = true;
                    selectedClientID = idToSelect;
                    button1.Enabled = true;
                    button7.Enabled = true;
                    break;
                }
            }
        }

        /// <summary>
        /// Кнопка удалить
        /// </summary>
        private void button7_Click(object sender, EventArgs e)
        {
            if (selectedClientID == -1)
            {
                MessageBox.Show("Выберите клиента для удаления!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show("Вы действительно хотите удалить клиента?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.No)
            {
                return;
            }

            if (DataDelete.DeleteClient(selectedClientID))
            {
                MessageBox.Show("Клиент успешно удален!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                GetDate();
                ClearViewSelection();
            }
        }

        /// <summary>
        /// Форматирует отображение ячеек показывает оригинальные данные для открытой строки или маскирует
        /// </summary>
        private void dataGridView1_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0)
            {
                return;
            }

            int clientID = Convert.ToInt32(dataGridView1.Rows[e.RowIndex].Cells["ClientID"].Value);

            if (e.RowIndex == lastRevealedRowIndex)
            {
                if (dataGridView1.Columns[e.ColumnIndex].Name == "PhoneNumber")
                {
                    string original = dataSecurity.GetOriginalPhone(clientID);

                    if (original != null)
                    {
                        e.Value = original;
                        e.FormattingApplied = true;
                    }
                }
                else if (dataGridView1.Columns[e.ColumnIndex].Name == "FirstName")
                {
                    string original = dataSecurity.GetOriginalFirstName(clientID);

                    if (original != null)
                    {
                        e.Value = original;
                        e.FormattingApplied = true;
                    }
                }
                else if (dataGridView1.Columns[e.ColumnIndex].Name == "MiddleName")
                {
                    string original = dataSecurity.GetOriginalMiddleName(clientID);

                    if (original != null)
                    {
                        e.Value = original;
                        e.FormattingApplied = true;
                    }
                }
                return;
            }

            if (dataGridView1.Columns[e.ColumnIndex].Name == "PhoneNumber")
            {
                string original = dataSecurity.GetOriginalPhone(clientID);

                if (e.Value != null && original != null)
                {
                    e.Value = DataSecurity.MaskPhone(original);
                    e.FormattingApplied = true;
                }
            }
            else if (dataGridView1.Columns[e.ColumnIndex].Name == "FirstName")
            {
                string original = dataSecurity.GetOriginalFirstName(clientID);

                if (e.Value != null && original != null)
                {
                    e.Value = DataSecurity.MaskName(original);
                    e.FormattingApplied = true;
                }
            }
            else if (dataGridView1.Columns[e.ColumnIndex].Name == "MiddleName")
            {
                string original = dataSecurity.GetOriginalMiddleName(clientID);

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
        private void dataGridView1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
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
        /// Таймер срабатывает через 20 секунд скрывает открытые чувствительные данные
        /// </summary>
        private void timer1_Tick(object sender, EventArgs e)
        {
            timer1.Stop();

            if (lastRevealedRowIndex >= 0)
            {
                int rowToHide = lastRevealedRowIndex;
                lastRevealedRowIndex = -1;
                dataGridView1.InvalidateRow(rowToHide);
            }
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            InputRest.OnlyRussianAndDash(e, textBox1);
        }

        private void ClearViewSelection()
        {
            selectedClientID = -1;
            lastRevealedRowIndex = -1;
            dataGridView1.ClearSelection();
            button1.Enabled = false;
            button7.Enabled = false;
        }
    }
}