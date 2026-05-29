using Microsoft.Win32;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace WebSiteDev.ManagerForm
{
    public partial class DirectorOrderControl : ScalableUserControl
    {
        private DataManipulation dataManipulation;
        private DataSecurity dataSecurity = new DataSecurity();
        private int lastRevealedRowIndex = -1;
        private Timer timer1 = new Timer();

        private Pagination pagination;
        private const int ItemsPerPage = 15; // Сколько показывать на одной странице

        public DirectorOrderControl()
        {
            InitializeComponent();
            timer1.Interval = 20000;
            timer1.Tick += Timer1_Tick;
            GetDate();

            pagination = new Pagination(dataManipulation.view.Count, ItemsPerPage);
            pagination.PageChanged += Pagination_PageChanged;

            LoadCurrentPage();      // Загрузка текущей страницы
            UpdatePaginationUI();   // Обновление UI пагинации
        }

        private void DirectorOrderControl_Load(object sender, EventArgs e)
        {
            comboBox3.SelectedIndex = 0;
            dataGridView1.ContextMenuStrip = contextMenuStrip1;
            SetDatePickerRange();
            dataManipulation.FillComboBoxWithStatuses(comboBox1, "Выберите статус");
            dataGridView1.ClearSelection();
        }

        /// <summary>
        /// Устанавливает минимальную и максимальную дату в календарях на основе первого и последнего заказа
        /// </summary>
        private void SetDatePickerRange()
        {
            using (MySqlConnection con = new MySqlConnection(Data.GetConnectionString()))
            {
                string DataCmd = @"SELECT MIN(OrderDate) AS FirstDate, MAX(OrderDate) AS LastDate FROM `Order`";

                con.Open();

                MySqlCommand cmd = new MySqlCommand(DataCmd, con);

                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        DateTime firstDate = DateTime.Now;
                        DateTime lastDate = DateTime.Now;

                        if (reader["FirstDate"] != DBNull.Value)
                        {
                            firstDate = Convert.ToDateTime(reader["FirstDate"]);
                        }

                        if (reader["LastDate"] != DBNull.Value)
                        {
                            lastDate = Convert.ToDateTime(reader["LastDate"]);
                        }

                        dateTimePicker1.MinDate = firstDate;
                        dateTimePicker1.MaxDate = lastDate;
                        dateTimePicker1.Value = firstDate;
                        dateTimePicker1.CustomFormat = "dd.MM.yyyy";

                        dateTimePicker2.MinDate = firstDate;
                        dateTimePicker2.MaxDate = lastDate;
                        dateTimePicker2.Value = lastDate;
                        dateTimePicker2.CustomFormat = "dd.MM.yyyy";
                    }
                }
            }
        }

        /// <summary>
        /// Загружает заказы за выбранный период из БД и отображает их в таблице
        /// </summary>
        private void GetDate()
        {
            using (MySqlConnection con = new MySqlConnection(Data.GetConnectionString()))
            {
                string dateFromStr = dateTimePicker1.Value.Date.ToString("yyyy-MM-dd");
                string dateToStr = dateTimePicker2.Value.Date.ToString("yyyy-MM-dd");

                string OrderCmd = $@"
                    SELECT 
                        o.OrderID,
                        CONCAT(c.Surname, ' ', c.FirstName, ' ', COALESCE(c.MiddleName, '')) AS ClientName,
                        CONCAT(u.Surname, ' ', u.FirstName, ' ', COALESCE(u.MiddleName, '')) AS UserName,
                        o.OrderDate,
                        o.OrderCompDate,
                        GROUP_CONCAT(DISTINCT p.ProductName SEPARATOR ', ') AS ProductName,
                        s.StatusName,
                        o.OrderCost
                    FROM `Order` o
                    LEFT JOIN Clients c ON o.ClientID = c.ClientID
                    LEFT JOIN Users u ON o.UserID = u.UserID
                    LEFT JOIN orderproduct op ON o.OrderID = op.OrderID
                    LEFT JOIN Product p ON op.ProductID = p.ProductID
                    LEFT JOIN Status s ON o.StatusID = s.StatusID
                    WHERE DATE(o.OrderDate) BETWEEN '{dateFromStr}' AND '{dateToStr}'
                    GROUP BY o.OrderID 
                    ORDER BY o.OrderDate ASC";

                con.Open();

                MySqlCommand cmd = new MySqlCommand(OrderCmd, con);

                MySqlDataAdapter da = new MySqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                dataSecurity.LoadOriginalClientNames(dt, "ClientName");
                dataSecurity.LoadOriginalUserNames(dt, "UserName");
                lastRevealedRowIndex = -1;

                dataManipulation = new DataManipulation(dt);

                dataManipulation.ApplyAllDirector(comboBox3, comboBox1, textBox1);
                dataManipulation.UpdateRecordCountLabel(label16);

                if (pagination == null)
                {
                    pagination = new Pagination(dataManipulation.view.Count, ItemsPerPage);
                    pagination.PageChanged += Pagination_PageChanged;
                }
                else
                {
                    pagination.TotalItems = dataManipulation.view.Count;
                    pagination.GoToPage(1);
                }

                LoadCurrentPage();
                UpdatePaginationUI();
            }
        }

        /// <summary>
        /// Настраивает колонки DataGridView
        /// </summary>
        private void SetupDataGridViewColumns()
        {
            if (dataGridView1.Columns["OrderID"] != null)
            {
                dataGridView1.Columns["OrderID"].HeaderText = "№ заказа";
                dataGridView1.Columns["OrderID"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                dataGridView1.Columns["OrderID"].SortMode = DataGridViewColumnSortMode.NotSortable;
            }
            if (dataGridView1.Columns["ClientName"] != null)
            {
                dataGridView1.Columns["ClientName"].HeaderText = "Клиент";
                dataGridView1.Columns["ClientName"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                dataGridView1.Columns["ClientName"].SortMode = DataGridViewColumnSortMode.NotSortable;
            }
            if (dataGridView1.Columns["UserName"] != null)
            {
                dataGridView1.Columns["UserName"].HeaderText = "Сотрудник";
                dataGridView1.Columns["UserName"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                dataGridView1.Columns["UserName"].SortMode = DataGridViewColumnSortMode.NotSortable;
            }
            if (dataGridView1.Columns["OrderDate"] != null)
            {
                dataGridView1.Columns["OrderDate"].HeaderText = "Дата заказа";
                dataGridView1.Columns["OrderDate"].SortMode = DataGridViewColumnSortMode.NotSortable;
            }
            if (dataGridView1.Columns["OrderCompDate"] != null)
            {
                dataGridView1.Columns["OrderCompDate"].HeaderText = "Срок выполнения заказа";
                dataGridView1.Columns["OrderCompDate"].SortMode = DataGridViewColumnSortMode.NotSortable;
            }
            if (dataGridView1.Columns["ProductName"] != null)
            {
                dataGridView1.Columns["ProductName"].Visible = false;
                dataGridView1.Columns["ProductName"].SortMode = DataGridViewColumnSortMode.NotSortable;
            }
            if (dataGridView1.Columns["StatusName"] != null)
            {
                dataGridView1.Columns["StatusName"].HeaderText = "Статус";
                dataGridView1.Columns["StatusName"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                dataGridView1.Columns["StatusName"].SortMode = DataGridViewColumnSortMode.NotSortable;
            }
            if (dataGridView1.Columns["OrderCost"] != null)
            {
                dataGridView1.Columns["OrderCost"].HeaderText = "Итоговая цена";
                dataGridView1.Columns["OrderCost"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                dataGridView1.Columns["OrderCost"].SortMode = DataGridViewColumnSortMode.NotSortable;
            }

            dataGridView1.ContextMenuStrip = contextMenuStrip1;
        }

        /// <summary>
        /// Загружает только строки текущей страницы из отфильтрованного view
        /// </summary>
        private void LoadCurrentPage()
        {
            if (pagination == null || dataManipulation == null || dataManipulation.view == null)
            {
                return;
            }

            dataGridView1.SuspendLayout();

            if (dataManipulation.view.Count == 0)
            {
                DataTable emptyTable = dataManipulation.table != null ? dataManipulation.table.Clone() : new DataTable();
                dataGridView1.DataSource = emptyTable;
                SetupDataGridViewColumns();

                dataGridView1.ResumeLayout();
                dataGridView1.ClearSelection();
                return;
            }

            int start = pagination.GetStartIndex();
            int count = pagination.GetTakeCount();

            if (start < 0)
            {
                start = 0;
            }

            DataTable pageTable = dataManipulation.table.Clone();

            for (int i = 0; i < count; i++)
            {
                int viewIndex = start + i;

                if (viewIndex >= dataManipulation.view.Count)
                {
                    break;
                }

                DataRowView rowView = dataManipulation.view[viewIndex];
                DataRow newRow = pageTable.NewRow();
                newRow.ItemArray = rowView.Row.ItemArray;
                pageTable.Rows.Add(newRow);
            }

            dataGridView1.DataSource = pageTable;
            SetupDataGridViewColumns();

            dataGridView1.ResumeLayout();
            dataGridView1.ClearSelection();
        }

        /// <summary>
        /// Обновляет текст и доступность кнопок пагинации
        /// </summary>
        private void UpdatePaginationUI()
        {
            if (pagination == null)
            {
                return;
            }

            textBox5.Text = pagination.CurrentPage.ToString();
            textBox5.SelectionStart = textBox5.Text.Length;
            textBox5.SelectionLength = 0;

            label1.Text = pagination.TotalPages.ToString();

            if (!pagination.HasPrevious && button6.Focused)
            {
                dataGridView1.Focus();
            }
            else if (!pagination.HasNext && button3.Focused)
            {
                dataGridView1.Focus();
            }

            button6.Enabled = pagination.HasPrevious;
            button3.Enabled = pagination.HasNext;
        }

        /// <summary>
        /// Событие срабатывает при смене страницы в пагинации
        /// </summary>
        private void Pagination_PageChanged(object sender, EventArgs e)
        {
            LoadCurrentPage();
            UpdatePaginationUI();
        }

        /// <summary>
        /// Кнопка для перехода на прошлую страницу
        /// </summary>
        private void button6_Click(object sender, EventArgs e)
        {
            pagination.PreviousPage();
        }

        /// <summary>
        /// Кнопка для перехода на следующую страницу
        /// </summary>
        private void button3_Click(object sender, EventArgs e)
        {
            pagination.NextPage();
        }

        /// <summary>
        /// Метод события обработки нажатия Enter для перехода на конкретную страницу
        /// </summary>
        private void textBox5_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (int.TryParse(textBox5.Text, out int page))
                {
                    pagination.GoToPage(page);
                }
                else
                {
                    textBox5.Text = pagination.CurrentPage.ToString();
                }

                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void textBox5_KeyPress(object sender, KeyPressEventArgs e)
        {
            InputRest.OnlyNumbers(e);
        }

        /// <summary>
        /// Применяет фильтры с обновлением пагинации
        /// </summary>
        private void ApplyFiltersInternal(bool resetToFirstPage = true)
        {
            if (dataManipulation == null)
            {
                return;
            }

            dataManipulation.ApplyAllDirector(comboBox3, comboBox1, textBox1);
            dataManipulation.UpdateRecordCountLabel(label16);

            if (pagination != null)
            {
                pagination.TotalItems = dataManipulation.view.Count;

                if (resetToFirstPage)
                {
                    pagination.GoToPage(1);
                }
                else if (pagination.CurrentPage > pagination.TotalPages)
                {
                    pagination.GoToPage(Math.Max(1, pagination.TotalPages));
                }
            }

            LoadCurrentPage();
            UpdatePaginationUI();
            dataGridView1.ClearSelection();
            dataGridView1.Refresh();
        }

        /// <summary>
        /// При вводе номера заказа фильтрует таблицу
        /// </summary>
        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            InputRest.FirstLetter(textBox1);
            ApplyFiltersInternal(resetToFirstPage: true);
        }

        /// <summary>
        /// При изменении сортировки применяет фильтры
        /// </summary>
        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            ApplyFiltersInternal(resetToFirstPage: true);
        }

        /// <summary>
        /// При изменении фильтра по статусу применяет фильтры
        /// </summary>
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            ApplyFiltersInternal(resetToFirstPage: true);
        }

        /// <summary>
        /// Кнопка "Очистить" очищает все фильтры и загружает заново
        /// </summary>
        private void button4_Click(object sender, EventArgs e)
        {
            dataManipulation.ResetFilters(comboBox3, comboBox1, textBox1);
            SetDatePickerRange();
            GetDate();

            dataGridView1.ClearSelection();
            dataGridView1.Refresh();
        }

        /// <summary>
        /// Ограничивает ввод в поле поиска только цифрами
        /// </summary>
        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            InputRest.OnlyNumbers(e);
        }

        /// <summary>
        /// Кнопка "Создать отчёт" экспортирует данные в Excel файл
        /// </summary>
        private void button2_Click(object sender, EventArgs e)
        {
            if (dataManipulation == null || dataManipulation.view == null || dataManipulation.view.Count == 0)
            {
                MessageBox.Show("Нет данных для формирования отчёта!", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!IsExcelInstalled())
            {
                MessageBox.Show(
                    "Microsoft Excel не установлен на вашем компьютере!\n\n" +
                    "Для создания отчёта требуется установленное приложение Microsoft Office Excel.\n\n" +
                    "Пожалуйста, установите Microsoft Office и повторите попытку.",
                    "Ошибка",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                return;
            }

            string message = "Вы хотите создать отчёт со следующими параметрами:\n\n";
            message = message + "Период: с " + dateTimePicker1.Value.ToString("dd.MM.yyyy") + " по " + dateTimePicker2.Value.ToString("dd.MM.yyyy") + "\n";

            if (string.IsNullOrWhiteSpace(textBox1.Text) == false)
            {
                message = message + "Поиск по номеру заказа: " + textBox1.Text + "\n";
            }

            string selectedStatus = "";
            if (comboBox1.SelectedIndex > 0)
            {
                object selected = comboBox1.SelectedItem;
                if (selected != null)
                {
                    if (selected is DataRowView row)
                    {
                        selectedStatus = row["StatusName"].ToString();
                        message = message + "Фильтр по статусу: " + selectedStatus + "\n";
                    }
                    else
                    {
                        selectedStatus = selected.ToString();
                        message = message + "Фильтр по статусу: " + selectedStatus + "\n";
                    }
                }
            }

            string selectedSort = "";
            if (comboBox3.SelectedIndex > 0)
            {
                selectedSort = comboBox3.SelectedItem.ToString();
                message = message + "Сортировка: " + selectedSort + "\n";
            }

            if (string.IsNullOrWhiteSpace(textBox1.Text) && comboBox1.SelectedIndex <= 0 && comboBox3.SelectedIndex <= 0)
            {
                message = message + "\nВсе заказы без поиска, фильтров и сортировки\n";
            }

            message = message + "\nВсего записей: " + dataManipulation.view.Count + "\n\nПродолжить?";

            var result = MessageBox.Show(message, "Подтверждение создания отчёта", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.No)
            {
                return;
            }

            // Собираем стоимости всех отфильтрованных заказов
            List <decimal> orderCosts = new List<decimal> ();

            foreach (DataRowView row in dataManipulation.view)
            {
                if (row["OrderCost"] != null && row["OrderCost"] != DBNull.Value)
                {
                    if (decimal.TryParse(row["OrderCost"].ToString(), out decimal cost))
                    {
                        orderCosts.Add(cost);
                    }
                }
            }

            // Создаём временный DataGridView со всеми отфильтрованными данными
            DataGridView dataGridView = new DataGridView();
            dataGridView.Visible = false;
            dataGridView.AutoGenerateColumns = true;
            dataGridView.AllowUserToAddRows = false;
            this.Controls.Add(dataGridView);

            DataTable fullTable = dataManipulation.table.Clone();

            foreach (DataRowView rv in dataManipulation.view)
            {
                DataRow row = fullTable.NewRow();
                row.ItemArray = rv.Row.ItemArray;
                fullTable.Rows.Add(row);
            }
            dataGridView.DataSource = fullTable;

            foreach (DataGridViewColumn col in dataGridView.Columns)
            {
                if (dataGridView1.Columns[col.Name] != null)
                {
                    col.HeaderText = dataGridView1.Columns[col.Name].HeaderText;
                }
            }

            ExcelReport.ExportToExcel(
                dataGridView,
                orderCosts,
                dateTimePicker1.Value,
                dateTimePicker2.Value,
                textBox1.Text,
                selectedStatus,
                selectedSort
            );

            this.Controls.Remove(dataGridView);
            dataGridView.Dispose();
        }

        /// <summary>
        /// Проверяет установлен ли Microsoft Excel на компьютере
        /// </summary>
        private bool IsExcelInstalled()
        {
            string[] paths =
            {
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\EXCEL.EXE",
                @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\App Paths\EXCEL.EXE"
            };

            foreach (string path in paths)
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(path))
                {
                    if (key != null)
                    {
                        object value = key.GetValue("");

                        if (value != null)
                        {
                            string ExcelPath = value.ToString();

                            if (File.Exists(ExcelPath))
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Двойной клик на строку таблицы открывает форму со составом заказа
        /// </summary>
        private void dataGridView1_CellMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex >= 0 && dataGridView1.Rows[e.RowIndex].Cells["OrderID"].Value != null)
            {
                int orderID = Convert.ToInt32(dataGridView1.Rows[e.RowIndex].Cells["OrderID"].Value);
                OrderProductForm form = new OrderProductForm(orderID);
                form.ShowDialog();
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
                OrderProductForm form = new OrderProductForm(orderID);
                form.ShowDialog();
            }
            else
            {
                MessageBox.Show("Выберите заказ для просмотра!", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// Правый клик на таблице выделяет строку для контекстного меню
        /// </summary>
        private void dataGridView1_MouseDown(object sender, MouseEventArgs e)
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
        /// При изменении начальной даты обновляет минимальную дату конечной и перезагружает данные
        /// </summary>
        private void dateTimePicker1_ValueChanged(object sender, EventArgs e)
        {
            dateTimePicker2.MinDate = dateTimePicker1.Value;
            GetDate();

            dataGridView1.ClearSelection();
        }

        private void dateTimePicker2_ValueChanged(object sender, EventArgs e)
        {
            GetDate();

            dataGridView1.ClearSelection();
        }

        /// <summary>
        /// Форматирует отображение ячеек окрашивает по статусу маскирует/показывает имена
        /// </summary>
        private void dataGridView1_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0)
            {
                return;
            }

            int orderID = Convert.ToInt32(dataGridView1.Rows[e.RowIndex].Cells["OrderID"].Value);
            string status = dataGridView1.Rows[e.RowIndex].Cells["StatusName"].Value?.ToString();

            // Окрашиваем строки в зависимости от статуса
            if (status == "Завершён")
            {
                e.CellStyle.BackColor = Color.LightGreen;
            }
            else if (status == "Отменён")
            {
                e.CellStyle.BackColor = Color.IndianRed;
            }
            else if (status == "Новый")
            {
                e.CellStyle.BackColor = Color.FromArgb(144, 202, 249);
            }
            else if (status == "В работе")
            {
                e.CellStyle.BackColor = Color.Gold;
            }

            // Если строка открыта показываем оригинальные данные
            if (e.RowIndex == lastRevealedRowIndex)
            {
                if (dataGridView1.Columns[e.ColumnIndex].Name == "ClientName")
                {
                    string original = dataSecurity.GetOriginalClientName(orderID);

                    if (original != null)
                    {
                        e.Value = original;
                        e.FormattingApplied = true;
                    }
                }
                else if (dataGridView1.Columns[e.ColumnIndex].Name == "UserName")
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
    }
}