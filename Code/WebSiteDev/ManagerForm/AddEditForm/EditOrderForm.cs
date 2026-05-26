using MySql.Data.MySqlClient;
using System;
using System.Windows.Forms;

namespace WebSiteDev.ManagerForm.AddEditForm
{
    /// <summary>
    /// Форма редактирования заказа (срок выполнения и статус)
    /// </summary>
    public partial class EditOrderForm : ScalableForm
    {
        protected override float MaxScale => 1.6f;
        protected override float MinScale => 0.9f;

        private int selectedOrderID;
        private string currentStatus = "";

        public EditOrderForm(int orderID)
        {
            InitializeComponent();
            this.selectedOrderID = orderID;
        }

        private void EditOrderForm_Load(object sender, EventArgs e)
        {
            Inactivity.OnFormLoad(this);
            LabelColor.ApplyRedStar(this);
            LoadOrderData();
        }

        /// <summary>
        /// Загружает срок и статус заказа
        /// </summary>
        private void LoadOrderData()
        {
            using (MySqlConnection con = new MySqlConnection(Data.GetConnectionString()))
            {
                con.Open();
                string query = @"SELECT OrderCompDate, StatusName 
                                 FROM `Order` o
                                 LEFT JOIN Status s ON o.StatusID = s.StatusID
                                 WHERE o.OrderID = @OrderID";

                MySqlCommand cmd = new MySqlCommand(query, con);
                cmd.Parameters.AddWithValue("@OrderID", selectedOrderID);
                MySqlDataReader rdr = cmd.ExecuteReader();

                if (rdr.Read())
                {
                    DateTime compDate = Convert.ToDateTime(rdr["OrderCompDate"]);
                    currentStatus = rdr["StatusName"].ToString();

                    dateTimePicker1.Value = compDate;
                    dateTimePicker1.MinDate = DateTime.Now.Date;

                    // Доступные статусы зависят от текущего
                    string[] statuses = GetAvailableStatuses(currentStatus);
                    FillStatuses(statuses);

                    // Завершённые и отменённые нельзя редактировать
                    if (currentStatus == "Завершён" || currentStatus == "Отменён")
                    {
                        comboBox5.Enabled = false;
                        dateTimePicker1.Enabled = false;
                        button2.Enabled = false;
                    }
                }

                rdr.Close();
            }
        }

        /// <summary>
        /// Возвращает доступные статусы для перехода из текущего
        /// </summary>
        private string[] GetAvailableStatuses(string status)
        {
            if (status == "Новый")
            {
                return new string[] { "Новый", "В работе", "Отменён" };
            }
            else if (status == "В работе")
            {
                return new string[] { "В работе", "Завершён", "Отменён" };
            }
            else
            {
                return new string[] { status };
            }
        }

        /// <summary>
        /// Заполняет comboBox5 статусами
        /// </summary>
        private void FillStatuses(string[] statuses)
        {
            comboBox5.Items.Clear();

            foreach (string s in statuses)
            {
                comboBox5.Items.Add(s);
            }
            if (comboBox5.Items.Count > 0)
            {
                comboBox5.SelectedIndex = 0;
            }
        }

        /// <summary>
        /// Кнопка "Назад" закрывает форму
        /// </summary>
        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Кнопка "Изменить заказ" сохраняет срок и статус
        /// </summary>
        private void button2_Click(object sender, EventArgs e)
        {
            if (comboBox5.SelectedItem == null || dateTimePicker1.Value == null)
            {
                MessageBox.Show("Необходимо заполнить поля отмеченные \"*\"", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string newStatus = comboBox5.SelectedItem.ToString();
            DateTime newDate = dateTimePicker1.Value;

            if (!IsValidTransition(currentStatus, newStatus))
            {
                MessageBox.Show("Недопустимый переход статуса!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (newDate < DateTime.Now.Date && currentStatus != "Завершён" && currentStatus != "Отменён")
            {
                MessageBox.Show("Срок выполнения не может быть в прошлом!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show("Вы действительно хотите изменить заказ?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
            {
                return;
            }

            try
            {
                using (MySqlConnection con = new MySqlConnection(Data.GetConnectionString()))
                {
                    con.Open();

                    // Получаем StatusID по имени
                    string getStatusID = "SELECT StatusID FROM Status WHERE StatusName = @Name";

                    MySqlCommand cmdStatus = new MySqlCommand(getStatusID, con);
                    cmdStatus.Parameters.AddWithValue("@Name", newStatus);
                    object resultQuery = cmdStatus.ExecuteScalar();

                    if (resultQuery == null)
                    {
                        MessageBox.Show("Статус не найден!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    int statusID = Convert.ToInt32(resultQuery);

                    string updateQuery = @"UPDATE `Order` SET 
                        OrderCompDate = @OrderCompDate, 
                        StatusID = @StatusID 
                        WHERE OrderID = @OrderID";

                    MySqlCommand cmd = new MySqlCommand(updateQuery, con);

                    cmd.Parameters.AddWithValue("@OrderCompDate", newDate);
                    cmd.Parameters.AddWithValue("@StatusID", statusID);
                    cmd.Parameters.AddWithValue("@OrderID", selectedOrderID);
                    cmd.ExecuteNonQuery();

                    MessageBox.Show("Заказ успешно изменён!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Проверяет допустимость перехода между статусами
        /// </summary>
        private bool IsValidTransition(string from, string to)
        {
            if (from == to)
            {
                return true;
            }

            if (from == "Новый" && (to == "В работе" || to == "Отменён"))
            {
                return true;
            }

            if (from == "В работе" && (to == "Завершён" || to == "Отменён"))
            {
                return true;
            }

            return false;
        }

        private void EditOrderForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Inactivity.OnFormClosing(this);
        }
    }
}