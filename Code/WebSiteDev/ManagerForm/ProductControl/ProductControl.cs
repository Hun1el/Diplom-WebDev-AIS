using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using WebSiteDev.AddForm;

namespace WebSiteDev.ManagerForm
{
    /// <summary>
    /// Контрол для просмотра и управления товарами/услугами с поддержкой корзины заказов
    /// </summary>
    public partial class ProductControl : ScalableUserControl
    {
        private DataManipulation dataManipulation;
        private string userRole;
        private ProductCard selectedCard;
        private int editingProductID = -1;

        // Пул для хранилищя уже созданных карточек
        // Создание карточки один раз и потом достаем её из словаря
        // Это посит быстродействие
        private Dictionary<int, ProductCard> CardPool = new Dictionary<int, ProductCard>();

        public static int CurrentUserID { get; set; } = 0;
        public static string CurrentUserName { get; set; } = "";

        /// <summary>
        /// Класс для хранения информации о товаре в корзине
        /// </summary>
        public class OrderItem
        {
            public int ProductID { get; set; }
            public string ProductName { get; set; }
            public string CategoryName { get; set; }
            public decimal BasePrice { get; set; }
            public int Quantity { get; set; }
            public string ProductPhoto { get; set; }
        }

        /// <summary>
        /// Статический класс для управления текущим заказом
        /// </summary>
        public static class CurrentOrder
        {
            public static BindingList<OrderItem> Items { get; set; } = new BindingList<OrderItem>();
            public static void Clear() { Items.Clear(); }
        }

        public ProductControl(string role, int userID = 0, string userName = "")
        {
            InitializeComponent();

            // Двойная буферизация
            // При двойной буферизации отрисовка происходит сначала в буфере
            // а уже из буфера на экран и это прям сильно повышает производительность системы без лишних мерцаний при какждом разе отрисовки
            PropertyInfo doubleBufferProperty = typeof(Control).GetProperty(
                "DoubleBuffered",
                BindingFlags.NonPublic | BindingFlags.Instance);

            if (doubleBufferProperty != null)
            {
                doubleBufferProperty.SetValue(flowPanel, true, null);
            }

            userRole = role;
            CurrentUserID = userID;
            CurrentUserName = userName;

            GetData();
            LoadAllCards(false);
        }

        private void ProductControl_Load(object sender, EventArgs e)
        {
            // Менеджеры не могут добавлять новые товары
            if (userRole == "Менеджер")
            {
                button2.Visible = false;
            }
            else
            {
                // Если это администратор и находится в режиме просмотра услуг скрываем кнопку просмотра заказа
                Form parentForm = this.FindForm();

                if (parentForm != null && parentForm.Text == "Список услуг")
                {
                    button1.Visible = false;
                }
            }

            comboBox3.SelectedIndex = 0;
            comboBox1.SelectedIndex = 0;

            CurrentOrder.Clear();
            RefreshProductCardStates();
            UpdateOrderButtonVisibility();
        }

        string ProductCmd = @"SELECT p.ProductID, p.ProductName, p.ProductDescription, p.ProductPhoto,
                              c.CategoryName AS Category, p.BasePrice, p.CategoryID FROM Product p JOIN Category c ON p.CategoryID = c.CategoryID";
        string ProductCount = @"SELECT COUNT(*) FROM Product";

        /// <summary>
        /// Загружает все товары из БД в DataTable
        /// </summary>
        private void GetData()
        {
            using (MySqlConnection con = new MySqlConnection(Data.GetConnectionString()))
            {
                con.Open();

                MySqlDataAdapter da = new MySqlDataAdapter(ProductCmd, con);

                DataTable dt = new DataTable();
                da.Fill(dt);

                dataManipulation = new DataManipulation(dt);
                dataManipulation.FillComboBoxWithCategories(comboBox1, "Все категории");

                MySqlCommand count = new MySqlCommand(ProductCount, con);
                label1.Text = "Количество записей: " + count.ExecuteScalar();
            }
        }

        /// <summary>
        /// Загружает все карточки товаров сразу
        /// Если ForceRecreate равен true то старый пул карточек полностью удаляется и создается заново
        /// Это нужно только когда данные реально изменились в базе
        /// </summary>
        private void LoadAllCards(bool ForceRecreate)
        {
            if (ForceRecreate)
            {
                // Удаляем старые карточки из памяти
                foreach (var kvp in CardPool)
                {
                    if (kvp.Value != null)
                    {
                        kvp.Value.Dispose();
                    }
                }
                CardPool.Clear();
            }

            // SuspendLayout говорит панели не перерисовывайся пока я добавляю контролы
            flowPanel.SuspendLayout();
            flowPanel.Controls.Clear();

            // Проходим по всем строкам из таблицы данных
            foreach (DataRowView row in dataManipulation.view)
            {
                // Берем ID товара из текущей строки
                int id = Convert.ToInt32(row["ProductID"]);

                // Пытаемся найти карточку в пуле по этому ID
                if (!CardPool.TryGetValue(id, out ProductCard card))
                {
                    // Если карточки нет в пуле создаем новую
                    card = CreateProductCard(row);
                    // И добавляем в пул чтобы в следующий раз не создавать заново
                    CardPool[id] = card;
                }

                // Добавляем карточку на панель для отображения
                flowPanel.Controls.Add(card);
            }

            UpdateAllCardWidths();

            flowPanel.ResumeLayout(true);
            flowPanel.PerformLayout();

            PreventHorizontalScroll();
            RefreshProductCardStates();
        }

        /// <summary>
        /// Перезагружает данные товаров из БД и пересоздаёт пул карточек
        /// Вызывать после сохранения удаления или добавления товара
        /// </summary>
        private void RefreshData()
        {
            // Чистим старый пул
            foreach (var kvp in CardPool)
            {
                if (kvp.Value != null)
                {
                    kvp.Value.Dispose();
                }
            }

            CardPool.Clear();
            flowPanel.Controls.Clear();

            using (MySqlConnection con = new MySqlConnection(Data.GetConnectionString()))
            {
                con.Open();

                MySqlDataAdapter da = new MySqlDataAdapter(ProductCmd, con);
                DataTable dt = new DataTable();
                da.Fill(dt);

                dataManipulation = new DataManipulation(dt);

                MySqlCommand count = new MySqlCommand(ProductCount, con);
                label1.Text = "Количество записей: " + count.ExecuteScalar();
            }

            // true  пересоздать пул заново потому что данные могли измениться
            LoadAllCards(true);
        }

        /// <summary>
        /// Создаёт карточку товара с событиями и контекстным меню
        /// </summary>
        private ProductCard CreateProductCard(DataRowView row)
        {
            ProductCard card = new ProductCard();
            card.ProductID = Convert.ToInt32(row["ProductID"]);
            card.RowData = row;
            card.Margin = new Padding(10);

            if (userRole == "Менеджер")
            {
                card.ContextMenuStrip = contextMenuStrip1;
            }

            card.InitializeCard(row, userRole);

            int productID = Convert.ToInt32(row["ProductID"]);
            bool isInCart = IsProductInCart(productID);
            card.UpdateAddToCartButtonState(isInCart, userRole);

            // Подписка на события карточки
            card.EditButtonClicked += Card_EditButtonClicked;
            card.DeleteButtonClicked += Card_DeleteButtonClicked;
            card.AddToCartClicked += Card_AddToCartClicked;
            card.CancelEditClicked += Card_CancelEditClicked;

            return card;
        }

        /// <summary>
        /// Проверяет находится ли товар уже в корзине
        /// </summary>
        private bool IsProductInCart(int productID)
        {
            foreach (var item in CurrentOrder.Items)
            {
                if (item.ProductID == productID)
                {
                    return true;
                }
            }

            return false;
        }

        private void Card_EditButtonClicked(object sender, EventArgs e)
        {
            StartEdit(sender as ProductCard);
        }

        private void Card_DeleteButtonClicked(object sender, EventArgs e)
        {
            DeleteProduct(sender as ProductCard);
        }

        /// <summary>
        /// Метод добавления в корзину для менеджера
        /// </summary>
        private void Card_AddToCartClicked(object sender, EventArgs e)
        {
            ProductCard card = sender as ProductCard;

            if (card == null)
            {
                return;
            }

            if (userRole != "Менеджер")
            {
                return;
            }

            // Событие движения и кликом мышки
            MouseEventArgs me = e as MouseEventArgs;
            selectedCard = card;

            if (me != null && me.Button == MouseButtons.Left)
            {
                AddToCartDirect(card);
                return;
            }

            if (me != null && me.Button == MouseButtons.Right)
            {
                contextMenuStrip1.Show(Control.MousePosition);
            }
        }

        private void AddToCartDirect(ProductCard card)
        {
            if (card == null)
            {
                return;
            }

            DataRowView row = card.RowData;
            int productID = Convert.ToInt32(row["ProductID"]);
            string productName = row["ProductName"].ToString();
            decimal basePrice = Convert.ToDecimal(row["BasePrice"]);

            foreach (OrderItem item in CurrentOrder.Items)
            {
                if (item.ProductID == productID)
                {
                    MessageBox.Show("Товар \"" + productName + "\" уже в корзине.", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            decimal currentTotal = 0;

            foreach (OrderItem item in CurrentOrder.Items)
            {
                currentTotal += item.BasePrice * item.Quantity;
            }

            decimal newTotal = currentTotal + basePrice;
            decimal newTotalWithSurcharge = Math.Round(newTotal * 1.15m, 2);
            decimal maxLimit = 9999999999.99m;

            // Проверка на превышения цены для защиты от переполнения
            if (newTotalWithSurcharge > maxLimit)
            {
                MessageBox.Show(
                    "Невозможно добавить товар!\n\n" +
                    "Сумма заказа с учётом надбавки 15% превысит допустимый лимит заказа (9 999 999 999.99 руб.).\n" +
                    "Текущая сумма: " + currentTotal.ToString("N2") + " руб.\n" +
                    "Будет после добавления: " + newTotal.ToString("N2") + " руб. (без надбавки)\n" +
                    "С учётом надбавки: " + newTotalWithSurcharge.ToString("N2") + " руб.",
                    "Превышение лимита",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );

                return;
            }

            OrderItem newItem = new OrderItem();
            newItem.ProductID = productID;
            newItem.ProductName = productName;
            newItem.BasePrice = basePrice;
            newItem.CategoryName = row["Category"].ToString();
            newItem.Quantity = 1;
            newItem.ProductPhoto = row["ProductPhoto"].ToString();

            CurrentOrder.Items.Add(newItem);

            card.UpdateAddToCartButtonState(true, "Менеджер");
            UpdateOrderButtonVisibility();
        }

        /// <summary>
        /// Метод отмены в режиме редактирования
        /// </summary>
        private void Card_CancelEditClicked(object sender, EventArgs e)
        {
            ProductCard card = sender as ProductCard;
            editingProductID = -1;
            card.button3.Click -= SaveProduct;

            ImageControl img = card.GetImageControl();
            if (img != null)
            {
                img.ShowChangeButton(false);
                img.CancelEdit();
                img.InitializeImage(card.RowData["ProductPhoto"].ToString());
            }

            card.HideEditMode();
        }

        /// <summary>
        /// Метод для перехода в режим редактирования
        /// </summary>
        private void StartEdit(ProductCard card)
        {
            if (card == null)
            {
                return;
            }

            DataRowView row = card.RowData;
            int productID = Convert.ToInt32(row["ProductID"]);

            if (editingProductID != -1 && editingProductID != productID)
            {
                MessageBox.Show("Уже редактируется другая услуга! Завершите редактирование.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            editingProductID = productID;

            ImageControl imageControl = card.GetImageControl();

            if (imageControl != null)
            {
                imageControl.InitializeImage(row["ProductPhoto"].ToString());
                imageControl.ShowChangeButton(true);
            }

            card.ShowEditMode(dataManipulation);
            card.button3.Tag = new object[] { productID, card.textBox1, card.textBox2, card.comboBox1, card.textBox3, card };
            card.button3.Click -= SaveProduct;
            card.button3.Click += SaveProduct;
        }

        /// <summary>
        /// Метод для кнопки сохранения изменений в режиме редактирования
        /// </summary>
        private void SaveProduct(object sender, EventArgs e)
        {
            object[] data = (sender as Button).Tag as object[];

            if (data == null)
            {
                return;
            }

            // Текстовые поля
            int productID = Convert.ToInt32(data[0]);
            TextBox textBox1 = data[1] as TextBox;
            TextBox textBox2 = data[2] as TextBox;
            ComboBox comboBox = data[3] as ComboBox;
            TextBox textBox3 = data[4] as TextBox;
            ProductCard card = data[5] as ProductCard;

            if (!ValidateProductData(textBox1, textBox2, textBox3, card.numericUpDown1, comboBox))
            {
                return;
            }

            var result = MessageBox.Show("Вы действительно хотите изменить услугу?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            // Если выбрали Нет при сохранении
            if (result != DialogResult.Yes)
            {
                ImageControl imgCancel = card.GetImageControl();

                if (imgCancel != null)
                {
                    imgCancel.ShowChangeButton(false);
                    imgCancel.CancelEdit();
                    imgCancel.InitializeImage(card.RowData["ProductPhoto"].ToString());
                }

                editingProductID = -1;
                card.button3.Click -= SaveProduct;
                card.HideEditMode();

                return;
            }

            string categoryName = comboBox.SelectedItem.ToString();
            int categoryID = GetCategoryID(categoryName);

            if (categoryID == 0)
            {
                MessageBox.Show("Категория не найдена!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            ImageControl imageControl = card.GetImageControl();

            if (imageControl != null)
            {
                imageControl.SaveImage(productID);
            }

            int rubles = 0;
            int kopecks = 0;

            int.TryParse(textBox3.Text, out rubles);
            kopecks = Convert.ToInt32(card.numericUpDown1.Value);

            decimal price = rubles + (kopecks / 100.0m);

            if (DataUpdate.UpdateProduct(productID, textBox1.Text.Trim(), textBox2.Text.Trim(), categoryID, price))
            {
                MessageBox.Show("Услуга успешно изменена!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                editingProductID = -1;
                card.button3.Click -= SaveProduct;

                ImageControl img = card.GetImageControl();

                if (img != null)
                {
                    img.ShowChangeButton(false);
                }

                card.HideEditMode();

                int savedFilterIndex = comboBox1.SelectedIndex;
                string savedSearchText = textBox1.Text;
                int savedSortIndex = comboBox3.SelectedIndex;

                RefreshData();

                comboBox1.SelectedIndex = savedFilterIndex;
                textBox1.Text = savedSearchText;
                comboBox3.SelectedIndex = savedSortIndex;

                ApplyFilters();
            }
        }

        /// <summary>
        /// Метод для удаления услуги
        /// </summary>
        private void DeleteProduct(ProductCard card)
        {
            if (card == null)
            {
                return;
            }

            DialogResult result = MessageBox.Show("Вы действительно хотите удалить услугу?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                int productID = Convert.ToInt32(card.RowData["ProductID"]);

                if (DataDelete.DeleteProduct(productID))
                {
                    MessageBox.Show("Услуга удалена!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    int savedFilterIndex = comboBox1.SelectedIndex;
                    string savedSearchText = textBox1.Text;
                    int savedSortIndex = comboBox3.SelectedIndex;

                    RefreshData();

                    comboBox1.SelectedIndex = savedFilterIndex;
                    textBox1.Text = savedSearchText;
                    comboBox3.SelectedIndex = savedSortIndex;

                    ApplyFilters();
                }
            }
        }

        /// <summary>
        /// Метод для проверки корректности введенных данных
        /// </summary>
        private bool ValidateProductData(TextBox name, TextBox description, TextBox rubles, NumericUpDown kopecks, ComboBox category)
        {
            if (string.IsNullOrWhiteSpace(name.Text) || string.IsNullOrWhiteSpace(description.Text) || category.SelectedIndex < 0 || string.IsNullOrWhiteSpace(rubles.Text))
            {
                MessageBox.Show("Необходимо заполнить поля отмеченные \"*\"", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (name.Text.Length < 3)
            {
                MessageBox.Show("Название услуги должно быть минимум 3 символа!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (description.Text.Length < 10)
            {
                MessageBox.Show("Описание должно быть минимум 10 символов!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (!int.TryParse(rubles.Text, out int rublesValue))
            {
                MessageBox.Show("Рубли должны быть числом!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Метод для получения идентификатора категории
        /// </summary>
        private int GetCategoryID(string categoryName)
        {
            if (dataManipulation == null || string.IsNullOrEmpty(categoryName))
            {
                return 0;
            }

            foreach (DataRow row in dataManipulation.table.Rows)
            {
                if (row["Category"].ToString() == categoryName)
                {
                    return Convert.ToInt32(row["CategoryID"]);
                }
            }

            return 0;
        }

        /// <summary>
        /// Контекстное меню
        /// </summary>
        private void contextMenuStrip1_Click(object sender, EventArgs e)
        {
            if (selectedCard == null)
            {
                return;
            }

            DataRowView row = selectedCard.RowData;
            int productID = Convert.ToInt32(row["ProductID"]);
            string productName = row["ProductName"].ToString();
            decimal basePrice = Convert.ToDecimal(row["BasePrice"]);

            foreach (OrderItem item in CurrentOrder.Items)
            {
                if (item.ProductID == productID)
                {
                    contextMenuStrip1.Close();
                    MessageBox.Show("Товар \"" + productName + "\" уже в корзине.", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            decimal currentTotal = 0;

            foreach (OrderItem item in CurrentOrder.Items)
            {
                currentTotal += item.BasePrice * item.Quantity;
            }

            decimal newTotal = currentTotal + basePrice;
            decimal newTotalWithSurcharge = Math.Round(newTotal * 1.15m, 2);
            decimal maxLimit = 9999999999.99m;

            if (newTotalWithSurcharge > maxLimit)
            {
                contextMenuStrip1.Close();
                MessageBox.Show(
                    "Невозможно добавить товар!\n\n" +
                    "Сумма заказа с учётом надбавки 15% превысит допустимый лимит заказа (9 999 999 999.99 руб.).\n" +
                    "Текущая сумма: " + currentTotal.ToString("N2") + " руб.\n" +
                    "Будет после добавления: " + newTotal.ToString("N2") + " руб. (без надбавки)\n" +
                    "С учётом надбавки: " + newTotalWithSurcharge.ToString("N2") + " руб.",
                    "Превышение лимита",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );

                return;
            }

            OrderItem newItem = new OrderItem();
            newItem.ProductID = productID;
            newItem.ProductName = productName;
            newItem.BasePrice = basePrice;
            newItem.CategoryName = row["Category"].ToString();
            newItem.Quantity = 1;
            newItem.ProductPhoto = row["ProductPhoto"].ToString();

            CurrentOrder.Items.Add(newItem);

            selectedCard.UpdateAddToCartButtonState(true, "Менеджер");
            UpdateOrderButtonVisibility();
            contextMenuStrip1.Close();
        }

        /// <summary>
        /// Метод для получения окончания кнопки корзины
        /// </summary>
        private string GetWordEnding(int count)
        {
            int mod = count % 100;

            if (mod >= 11 && mod <= 19)
            {
                return "услуг";
            }

            int last = count % 10;

            if (last == 1)
            {
                return "услуга";
            }

            if (last == 2 || last == 3 || last == 4)
            {
                return "услуги";
            }

            return "услуг";
        }

        /// <summary>
        /// Метод отвечающий за обновление видимости кнопки корзины
        /// </summary>
        public void UpdateOrderButtonVisibility()
        {
            if (button1 == null)
            {
                return;
            }

            button1.ForeColor = Color.White;
            button1.BackColor = Color.FromArgb(45, 156, 219);

            int totalQuantity = 0;

            foreach (OrderItem item in CurrentOrder.Items)
            {
                totalQuantity += item.Quantity;
            }

            if (totalQuantity > 0)
            {
                button1.Visible = true;
                button1.Text = "Просмотр заказа\n(" + totalQuantity + " " + GetWordEnding(totalQuantity) + ")";
                button1.Enabled = true;
            }
            else
            {
                button1.Visible = false;
                button1.Text = "Просмотр заказа";
                button1.Enabled = false;
            }
        }

        /// <summary>
        /// Метод для вызова фильтрации
        /// </summary>
        private void ApplyFilters()
        {
            ApplyFiltersInternal();
        }

        /// <summary>
        /// Берет уже созданные карточки из пула и показывает только те что подходят под фильтр
        /// Новые карточки НЕ создаются
        /// </summary>
        private void ApplyFiltersInternal()
        {
            flowPanel.SuspendLayout();

            // Применяем фильтры к DataView (сортировка, категория, поисковый текст)
            dataManipulation.ApplyAllProduct(comboBox3, comboBox1, textBox1);
            dataManipulation.UpdateRecordCountLabel(label1);

            // Убираем все карточки с панели без удаления из памяти
            flowPanel.Controls.Clear();

            // Проходим по отфильтрованным данным
            foreach (DataRowView row in dataManipulation.view)
            {
                int id = Convert.ToInt32(row["ProductID"]);

                // Ищем карточку в пуле
                if (!CardPool.TryGetValue(id, out ProductCard card))
                {
                    card = CreateProductCard(row);
                    CardPool[id] = card;
                }

                // Показываем карточку на панели
                flowPanel.Controls.Add(card);
            }

            UpdateAllCardWidths();

            flowPanel.ResumeLayout(true);
            PreventHorizontalScroll();

            RefreshProductCardStates();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            InputRest.FirstLetter(textBox1);
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            InputRest.AllowAll(e);
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            ApplyFilters();
        }

        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            ApplyFilters();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            BucketForm bucketForm = new BucketForm(dataManipulation, CurrentUserID, CurrentUserName);
            bucketForm.ShowDialog();

            RefreshProductCardStates();
            UpdateOrderButtonVisibility();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            AddProductForm addProductForm = new AddProductForm(dataManipulation);
            addProductForm.ShowDialog();
            RefreshData();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            dataManipulation.ResetFilters(comboSort: comboBox3, comboFilter: comboBox1, textSearch: textBox1);
            dataManipulation.ApplyAllProduct(comboBox3, comboBox1, textBox1);
            ApplyFilters();
        }

        public void RefreshProductCardStates()
        {
            foreach (Control control in flowPanel.Controls)
            {
                ProductCard card = control as ProductCard;

                if (card == null)
                {
                    continue;
                }

                int productID = Convert.ToInt32(card.RowData["ProductID"]);
                bool isInCart = false;

                for (int i = 0; i < CurrentOrder.Items.Count; i++)
                {
                    if (CurrentOrder.Items[i].ProductID == productID)
                    {
                        isInCart = true;
                        break;
                    }
                }

                card.UpdateAddToCartButtonState(isInCart, userRole);
            }
        }

        private void flowPanel_Resize(object sender, EventArgs e)
        {
            UpdateAllCardWidths();
            PreventHorizontalScroll();
        }

        /// <summary>
        /// Обновляет ширину всех карточек под текущую ширину flowPanel
        /// </summary>
        private void UpdateAllCardWidths()
        {
            if (flowPanel.IsDisposed || !flowPanel.IsHandleCreated)
                return;

            int AvailableWidth = flowPanel.ClientSize.Width;

            if (!flowPanel.VerticalScroll.Visible)
            {
                AvailableWidth = AvailableWidth - SystemInformation.VerticalScrollBarWidth;
            }

            foreach (Control control in flowPanel.Controls)
            {
                if (control.IsDisposed)
                {
                    continue;
                }

                if (control is ProductCard)
                {
                    int NewWidth = AvailableWidth - control.Margin.Horizontal - 1;

                    if (NewWidth > 0)
                    {
                        control.Width = NewWidth;
                    }
                }
            }
        }

        /// <summary>
        /// Принудительно отключает горизонтальную прокрутку во FlowPanel
        /// Этот метод его гарантированно убирает
        /// </summary>
        private void PreventHorizontalScroll()
        {
            if (flowPanel.IsDisposed || !flowPanel.IsHandleCreated)
            {
                return;
            }

            flowPanel.HorizontalScroll.Maximum = 0;
            flowPanel.HorizontalScroll.Visible = false;
            flowPanel.AutoScroll = false;
            flowPanel.AutoScroll = true;
        }
    }
}