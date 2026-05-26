using Microsoft.Win32;
using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.IO;
using System.Windows.Forms;
using WebSiteDev.ManagerForm.AddEditForm;

namespace WebSiteDev.ManagerForm
{
    public partial class OrderControl : ScalableUserControl
    {
        private DataManipulation dataManipulation;
        private string userRole;
        private int selectedOrderID = -1;
        private int lastRevealedRowIndex = -1;
        private Timer timer1 = new Timer();

        public static int CurrentUserID { get; set; } = 0;
        public static string CurrentUserName { get; set; } = "";

        private DataSecurity dataSecurity = new DataSecurity();


        public OrderControl(string role, int userID = 0, string userName = "")
        {
            InitializeComponent();
            userRole = role;
            CurrentUserID = userID;
            CurrentUserName = userName;
            timer1.Interval = 20000;
            timer1.Tick += Timer1_Tick;
            GetDate();
        }

        /// <summary>
        /// При загрузке контрола
        /// </summary>
        private void OrderControl_Load(object sender, EventArgs e)
        {
            comboBox1.SelectedIndex = 0;
            comboBox6.SelectedIndex = 0;
            DateTime dateTimeNow = DateTime.Now;

            dataGridView1.ContextMenuStrip = contextMenuStrip1;
            ClearViewSelection();

            dataGridView1.Columns["OrderID"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            dataGridView1.Columns["ClientName"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            dataGridView1.Columns["UserName"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            dataGridView1.Columns["StatusName"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            dataGridView1.Columns["OrderCost"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
        }

        void GetDate()
        {
            using (MySqlConnection con = new MySqlConnection(Data.GetConnectionString()))
            {
                con.Open();

                // Запрос для получения полной информации о заказах
                MySqlCommand cmd = new MySqlCommand(@"SELECT o.OrderID,
                             CONCAT(c.Surname, ' ', c.FirstName, ' ', COALESCE(c.MiddleName, '')) AS ClientName,
                             CONCAT(u.Surname, ' ', u.FirstName, ' ', COALESCE(u.MiddleName, '')) AS UserName,
                                o.OrderDate, o.OrderCompDate,
                             GROUP_CONCAT(DISTINCT p.ProductName SEPARATOR ', ') AS ProductName,
                                s.StatusName, o.OrderCost
                             FROM `Order` o
                             LEFT JOIN Clients c ON o.ClientID = c.ClientID
                             LEFT JOIN Users u ON o.UserID = u.UserID
                             LEFT JOIN orderproduct op ON o.OrderID = op.OrderID
                             LEFT JOIN Product p ON op.ProductID = p.ProductID
                             LEFT JOIN Status s ON o.StatusID = s.StatusID
                             GROUP BY o.OrderID
                             ORDER BY o.OrderDate ASC", con);

                cmd.ExecuteNonQuery();

                MySqlDataAdapter da = new MySqlDataAdapter(cmd);
                DataTable dt = new DataTable();

                da.Fill(dt);

                // Сохраняем оригинальные имена для маскирования
                dataSecurity.LoadOriginalClientNames(dt, "ClientName");
                dataSecurity.LoadOriginalUserNames(dt, "UserName");
                lastRevealedRowIndex = -1;

                dataGridView1.DataSource = dt;
                dataGridView1.Columns["OrderID"].HeaderText = "№ заказа";
                dataGridView1.Columns["ClientName"].HeaderText = "Клиент";
                dataGridView1.Columns["UserName"].HeaderText = "Сотрудник";
                dataGridView1.Columns["OrderDate"].HeaderText = "Дата заказа";
                dataGridView1.Columns["OrderCompDate"].HeaderText = "Срок выполнения заказа";
                dataGridView1.Columns["ProductName"].Visible = false;
                dataGridView1.Columns["StatusName"].HeaderText = "Статус";
                dataGridView1.Columns["OrderCost"].HeaderText = "Итоговая цена";

                dataGridView1.Columns["OrderID"].SortMode = DataGridViewColumnSortMode.NotSortable;
                dataGridView1.Columns["ClientName"].SortMode = DataGridViewColumnSortMode.NotSortable;
                dataGridView1.Columns["UserName"].SortMode = DataGridViewColumnSortMode.NotSortable;
                dataGridView1.Columns["OrderDate"].SortMode = DataGridViewColumnSortMode.NotSortable;
                dataGridView1.Columns["OrderCompDate"].SortMode = DataGridViewColumnSortMode.NotSortable;
                dataGridView1.Columns["ProductName"].SortMode = DataGridViewColumnSortMode.NotSortable;
                dataGridView1.Columns["StatusName"].SortMode = DataGridViewColumnSortMode.NotSortable;
                dataGridView1.Columns["OrderCost"].SortMode = DataGridViewColumnSortMode.NotSortable;

                dataManipulation = new DataManipulation(dt);

                dataManipulation.FillComboBoxWithStatuses(comboBox6, "Статус не выбран");

                MySqlCommand count = new MySqlCommand("SELECT COUNT(*) FROM `Order`", con);
                int resultcount = Convert.ToInt32(count.ExecuteScalar());

                label1.Text = $"Количество записей: {resultcount}";

                // Окрашиваем строки в зависимости от статуса
                ColorizeRowsByStatus();
            }
        }

        /// <summary>
        /// Форматирует отображение ячеек
        /// </summary>
        private void DataGridView1_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0)
            {
                return;
            }

            int orderID = Convert.ToInt32(dataGridView1.Rows[e.RowIndex].Cells["OrderID"].Value);

            string status = "";
            object statusValue = dataGridView1.Rows[e.RowIndex].Cells["StatusName"].Value;

            if (statusValue != null)
            {
                status = statusValue.ToString();
            }

            // Окрашиваем строки в зависимости от статуса
            if (status == "Завершён")
            {
                e.CellStyle.BackColor = System.Drawing.Color.LightGreen;
            }
            else if (status == "Отменён")
            {
                e.CellStyle.BackColor = System.Drawing.Color.IndianRed;
            }

            // Если строка открыта показываем оригинальные данные
            if (e.RowIndex == lastRevealedRowIndex)
            {
                string columnName = dataGridView1.Columns[e.ColumnIndex].Name;

                if (columnName == "ClientName")
                {
                    string original = dataSecurity.GetOriginalClientName(orderID);

                    if (original != null)
                    {
                        e.Value = original;
                        e.FormattingApplied = true;
                    }
                }
                else if (columnName == "UserName")
                {
                    string original = dataSecurity.GetOriginalUserName(orderID);

                    if (original != null)
                    {
                        e.Value = original;
                        e.FormattingApplied = true;
                    }
                }

                return;
            }

            // Маскируем имена для защиты данных
            if (dataGridView1.Columns[e.ColumnIndex].Name == "ClientName")
            {
                string original = dataSecurity.GetOriginalClientName(orderID);
                if (e.Value != null && original != null)
                {
                    e.Value = DataSecurity.MaskClientName(original);
                    e.FormattingApplied = true;
                }
            }

            if (dataGridView1.Columns[e.ColumnIndex].Name == "UserName")
            {
                string original = dataSecurity.GetOriginalUserName(orderID);
                if (e.Value != null && original != null)
                {
                    e.Value = DataSecurity.MaskUserName(original);
                    e.FormattingApplied = true;
                }
            }
        }

        /// <summary>
        /// Обработка правого клика на таблице выделение строки для контекстного меню
        /// </summary>
        private void DataGridView1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                DataGridView.HitTestInfo hit = dataGridView1.HitTest(e.X, e.Y);

                if (hit.RowIndex >= 0)
                {
                    dataGridView1.ClearSelection();
                    dataGridView1.Rows[hit.RowIndex].Selected = true;
                }
            }
        }

        /// <summary>
        /// При вводе номера заказа фильтрует таблицу по данному номеру
        /// </summary>
        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            dataManipulation.ApplyAllOrder(comboBox1, comboBox6, textBox1);
            dataManipulation.UpdateRecordCountLabel(label1);
            InputRest.FirstLetter(textBox1);

            ClearViewSelection();
        }

        /// <summary>
        /// Кнопка редактировать
        /// </summary>
        private void button1_Click(object sender, EventArgs e)
        {
            if (selectedOrderID == -1)
            {
                MessageBox.Show("Выберите заказ для редактирования!", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int editingOrderID = selectedOrderID;

            EditOrderForm editOrderForm = new EditOrderForm(editingOrderID);
            editOrderForm.ShowDialog();

            GetDate();

            // Восстанавливаем выделение отредактированной строки
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                object cellValue = row.Cells["OrderID"].Value;

                if (cellValue != null)
                {
                    int orderID = Convert.ToInt32(cellValue);
                    if (orderID == editingOrderID)
                    {
                        row.Selected = true;
                        dataGridView1.FirstDisplayedScrollingRowIndex = row.Index;
                        selectedOrderID = editingOrderID;
                        button1.Enabled = true;
                        button5.Enabled = true;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Кнопка создать новый заказ переходит к форме выбора товаров
        /// </summary>
        private void button2_Click(object sender, EventArgs e)
        {
            ProductControl.CurrentOrder.Clear();

            ManagerMainForm managerForm = (ManagerMainForm)this.FindForm();
            managerForm.LoadControl(new ProductControl(userRole, CurrentUserID, CurrentUserName));
            managerForm.Text = "Оформление заказа";

            managerForm.SelectButtonPublic(managerForm.Button2);
        }

        /// <summary>
        /// При изменении фильтра по дате обновляет отображение таблицы
        /// </summary>
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            dataManipulation.ApplyAllOrder(comboBox1, comboBox6, textBox1);
            dataManipulation.UpdateRecordCountLabel(label1);

            ClearViewSelection();
        }

        /// <summary>
        /// При изменении фильтра по статусу обновляет отображение таблицы
        /// </summary>
        private void comboBox6_SelectedIndexChanged(object sender, EventArgs e)
        {
            dataManipulation.ApplyAllOrder(comboBox1, comboBox6, textBox1);
            dataManipulation.UpdateRecordCountLabel(label1);

            ClearViewSelection();
        }

        /// <summary>
        /// Кнопка сброс фильтров отображает все заказы
        /// </summary>
        private void button4_Click(object sender, EventArgs e)
        {
            dataManipulation.ResetFilters(comboBox1, comboBox6, textBox1);

            ClearViewSelection();
        }

        /// <summary>
        /// Ограничивает ввод только цифрами в поле поиска
        /// </summary>
        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            InputRest.OnlyNumbers(e);
        }

        /// <summary>
        /// Двойной клик на ячейку таблицы открывает форму со списком товаров в заказе
        /// </summary>
        private void dataGridView1_CellMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex >= 0 && dataGridView1.Rows[e.RowIndex].Cells["OrderID"].Value != null)
            {
                int orderID = Convert.ToInt32(dataGridView1.Rows[e.RowIndex].Cells["OrderID"].Value);
                OrderProductForm orderProductForm = new OrderProductForm(orderID);
                orderProductForm.ShowDialog();
            }
        }

        /// <summary>
        /// Пункт контекстного меню просмотр состава заказа
        /// </summary>
        private void просмотрСоставаЗаказаToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count > 0)
            {
                int orderID = Convert.ToInt32(dataGridView1.SelectedRows[0].Cells["OrderID"].Value);
                OrderProductForm orderProductForm = new OrderProductForm(orderID);
                orderProductForm.ShowDialog();
            }
            else
            {
                MessageBox.Show("Выберите заказ для просмотра!", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// При клике на строку таблицы загружает данные заказа
        /// </summary>
        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                selectedOrderID = Convert.ToInt32(dataGridView1.Rows[e.RowIndex].Cells["OrderID"].Value);
                button5.Enabled = true;
                button1.Enabled = true;
            }
        }

        /// <summary>
        /// Кнопка печать чека создаёт документ Word с информацией о заказе
        /// </summary>
        private void button5_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count == 0)
            {
                MessageBox.Show("Выберите заказ для создания чека!", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Проверяем установлен ли Microsoft Word на компьютере
            if (!IsWordInstalled())
            {
                MessageBox.Show("Microsoft Word не установлен на вашем компьютере!\n\nДля создания чека требуется установленное приложение Microsoft Office Word.\n\nПожалуйста, установите Microsoft Office и повторите попытку.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            int orderID = Convert.ToInt32(dataGridView1.SelectedRows[0].Cells["OrderID"].Value);

            // Получаем оригинальное имя клиента из защитного хранилища
            string clientName = dataSecurity.GetOriginalClientName(orderID);

            if (clientName == null)
            {
                clientName = dataGridView1.SelectedRows[0].Cells["ClientName"].Value.ToString();
            }

            string orderDate = Convert.ToDateTime(dataGridView1.SelectedRows[0].Cells["OrderDate"].Value).ToString("dd.MM.yyyy");
            string statusName = dataGridView1.SelectedRows[0].Cells["StatusName"].Value.ToString();
            string orderCost = dataGridView1.SelectedRows[0].Cells["OrderCost"].Value.ToString();

            // Формируем сообщение подтверждения
            string message = "Вы хотите создать чек для следующего заказа?\n\n";
            message = message + "Номер заказа: " + orderID + "\n";
            message = message + "Клиент: " + clientName + "\n";
            message = message + "Дата заказа: " + orderDate + "\n";
            message = message + "Сумма: " + orderCost + " руб.\n\n";
            message = message + "Продолжить?";

            var result = MessageBox.Show(message, "Подтверждение создания чека", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.No)
            {
                return;
            }

            Doc.CheckWord.CreateCheck(orderID);
        }

        /// <summary>
        /// Проверяет установлен ли Microsoft Word через COM объекты
        /// </summary>
        private bool IsWordInstalled()
        {
            string[] paths =
            {
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\WINWORD.EXE",
                @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\App Paths\WINWORD.EXE"
            };

            foreach (string path in paths)
            {
                RegistryKey key = Registry.LocalMachine.OpenSubKey(path);

                if (key == null)
                {
                    key = Registry.CurrentUser.OpenSubKey(path);
                }

                if (key != null)
                {
                    object value = key.GetValue("");

                    if (value != null)
                    {
                        string wordPath = value.ToString();
                        if (File.Exists(wordPath))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Двойной клик на ячейку показывает/скрывает оригинальные имена на 20 секунд
        /// </summary>
        private void dataGridView1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
            {
                return;
            }

            if (e.RowIndex == lastRevealedRowIndex)
            {
                lastRevealedRowIndex = -1;
                dataGridView1.InvalidateRow(e.RowIndex);
                timer1.Stop();
                return;
            }

            // Закрываем предыдущую открытую строку
            if (lastRevealedRowIndex >= 0)
            {
                int previousRow = lastRevealedRowIndex;
                lastRevealedRowIndex = -1;
                dataGridView1.InvalidateRow(previousRow);
            }

            // Открываем новую строку
            lastRevealedRowIndex = e.RowIndex;
            dataGridView1.InvalidateRow(e.RowIndex);

            timer1.Stop();
            timer1.Start();
        }

        /// <summary>
        /// Таймер срабатывает через 20 секунд и скрывает открытые данные
        /// </summary>
        private void Timer1_Tick(object sender, EventArgs e)
        {
            timer1.Stop();

            if (lastRevealedRowIndex >= 0)
            {
                int rowToHide = lastRevealedRowIndex;
                lastRevealedRowIndex = -1;
                dataGridView1.InvalidateRow(rowToHide);
            }
        }

        /// <summary>
        /// Окрашивает строки таблицы в зависимости от статуса заказа
        /// </summary>
        private void ColorizeRowsByStatus()
        {
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.Cells["StatusName"].Value != null)
                {
                    string status = row.Cells["StatusName"].Value.ToString();

                    if (status == "Завершён")
                    {
                        foreach (DataGridViewCell cell in row.Cells)
                        {
                            cell.Style.BackColor = System.Drawing.Color.LightGreen;
                        }
                    }
                    else if (status == "Отменён")
                    {
                        foreach (DataGridViewCell cell in row.Cells)
                        {
                            cell.Style.BackColor = System.Drawing.Color.IndianRed;
                        }
                    }
                }
            }
        }

        private void ClearViewSelection()
        {
            selectedOrderID = -1;
            dataGridView1.ClearSelection();
            button1.Enabled = false;
            button5.Enabled = false;
        }
    }
}