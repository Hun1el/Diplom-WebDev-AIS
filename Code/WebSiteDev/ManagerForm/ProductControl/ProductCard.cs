using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace WebSiteDev
{
    /// <summary>
    /// Контрол-карточка товара с отображением информации и возможностью редактирования
    /// </summary>
    public partial class ProductCard : ScalableUserControl
    {
        public event EventHandler EditButtonClicked;
        public event EventHandler DeleteButtonClicked;
        public event EventHandler AddToCartClicked;
        public event EventHandler CancelEditClicked;
        public event EventHandler SaveButtonClicked;

        private string originalImagePath;

        /// <summary>
        /// Идентификатор товара
        /// </summary>
        public int ProductID { get; set; }

        /// <summary>
        /// Данные товара из таблицы БД
        /// </summary>
        public DataRowView RowData { get; set; }

        public ProductCard()
        {
            InitializeComponent();
        }

        public void InitializeCard(DataRowView row, string userRole)
        {
            RowData = row;

            // Загружаем изображение товара
            imageControl1.InitializeImage(row["ProductPhoto"].ToString());

            string productName = row["ProductName"].ToString();
            string productDesc = row["ProductDescription"].ToString();

            label1.Text = productName;
            label2.Text = productDesc;

            string categoryName = row["Category"].ToString();
            label3.Text = "Категория: " + categoryName;

            decimal price = Convert.ToDecimal(row["BasePrice"]);
            label4.Text = "Цена: " + price.ToString("0.00") + " руб.";

            if (userRole == "Менеджер")
            {
                button1.Visible = false;
                button2.Visible = false;
                button6.Visible = true;
            }
            else
            {
                button1.Text = "Редактировать";
                button2.Text = "Удалить";
                button1.Visible = true;
                button2.Visible = true;
                button6.Visible = false;
            }

            // Проверяем влезает ли описание в label
            UpdateExpandButtonVisibility();

            button5.ContextMenuStrip = null;
            LabelColor.ApplyRedStar(this);
            label8.Visible = false;
            label9.Visible = false;
            label10.Visible = false;
            label12.Visible = false;
            panel1.Visible = false;
        }

        /// <summary>
        /// Проверяет помещается ли текст с учётом текущего масштаба и размера
        /// </summary>
        private bool IsTextFitting()
        {
            if (string.IsNullOrEmpty(label2.Text))
            {
                return true;
            }

            Size textSize = TextRenderer.MeasureText(
                label2.Text,
                label2.Font,
                new Size(label2.Width, int.MaxValue),
                TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl
            );

            if (textSize.Height <= label2.Height)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Обновляет видимость кнопки "Полное описание"
        /// </summary>
        private void UpdateExpandButtonVisibility()
        {
            if (!label2.Visible)
            {
                button5.Visible = false;
                return;
            }

            if (IsTextFitting())
            {
                button5.Visible = false;
            }
            else
            {
                button5.Visible = true;
            }
        }

        /// <summary>
        /// Кнопка "Полное описание" открывает отдельное окно с полным описанием товара
        /// </summary>
        private void button5_Click(object sender, EventArgs e)
        {
            string productDesc = RowData["ProductDescription"].ToString();

            DescriptionProduct descriptionProduct = new DescriptionProduct();
            descriptionProduct.SetDescription(RowData["ProductName"].ToString(), productDesc);
            descriptionProduct.ShowDialog();
        }

        /// <summary>
        /// Кнопка "Добавить в корзину" вызывает событие для добавления товара в корзину
        /// </summary>
        private void button6_Click(object sender, EventArgs e)
        {
            if (AddToCartClicked != null)
            {
                AddToCartClicked(this, new MouseEventArgs(MouseButtons.Left, 1, 0, 0, 0));
            }
        }

        /// <summary>
        /// Кнопка "Редактировать" вызывает событие редактирования товара
        /// </summary>
        private void button1_Click(object sender, EventArgs e)
        {
            if (EditButtonClicked != null)
            {
                EditButtonClicked(this, e);
            }
        }

        /// <summary>
        /// Кнопка "Удалить" вызывает событие удаления товара
        /// </summary>
        private void button2_Click(object sender, EventArgs e)
        {
            if (DeleteButtonClicked != null)
            {
                DeleteButtonClicked(this, e);
            }
        }

        /// <summary>
        /// Кнопка "Сохранить" вызывает событие сохранения изменений товара
        /// </summary>
        private void button3_Click(object sender, EventArgs e)
        {
            if (SaveButtonClicked != null)
            {
                SaveButtonClicked(this, e);
            }
        }

        /// <summary>
        /// Кнопка "Отмена" отменяет редактирование и восстанавливает оригинальное изображение
        /// </summary>
        private void button4_Click(object sender, EventArgs e)
        {
            ImageControl img = GetImageControl();
            if (img != null)
            {
                img.CancelEdit();
                img.InitializeImage(originalImagePath);
            }

            if (CancelEditClicked != null)
            {
                CancelEditClicked(this, e);
            }

            HideEditMode();
        }

        /// <summary>
        /// Переходит в режим редактирования показывает поля ввода для изменения данных товара
        /// </summary>
        public void ShowEditMode(DataManipulation dataManipulation)
        {
            // Сохраняем оригинальный путь изображения для возможности отката
            originalImagePath = RowData["ProductPhoto"].ToString();

            // Загружаем текущие значения в поля редактирования
            textBox1.Text = label1.Text;
            textBox2.Text = label2.Text;

            // Разбиваем цену на рубли и копейки
            decimal price = Convert.ToDecimal(RowData["BasePrice"]);
            int rubles = (int)price;
            int kopecks = (int)Math.Round((price - rubles) * 100, MidpointRounding.AwayFromZero);

            textBox3.Text = rubles.ToString();
            numericUpDown1.Value = kopecks;

            // Получаем текущую категорию
            string categoryName = label3.Text.Replace("Категория: ", "");

            comboBox1.Items.Clear();

            // Заполняем список категорий из всех товаров
            DataTable fullTable = dataManipulation.table;

            for (int i = 0; i < fullTable.Rows.Count; i++)
            {
                string cat = fullTable.Rows[i]["Category"].ToString();

                bool categoryExists = false;

                for (int j = 0; j < comboBox1.Items.Count; j++)
                {
                    if (comboBox1.Items[j].ToString() == cat)
                    {
                        categoryExists = true;
                        break;
                    }
                }

                if (!categoryExists)
                {
                    comboBox1.Items.Add(cat);
                }
            }

            // Выбираем текущую категорию
            for (int i = 0; i < comboBox1.Items.Count; i++)
            {
                if (comboBox1.Items[i].ToString() == categoryName)
                {
                    comboBox1.SelectedIndex = i;
                    break;
                }
            }

            // Скрываем элементы режима просмотра
            label1.Visible = false;
            label2.Visible = false;
            label3.Visible = false;
            label4.Visible = false;
            button1.Visible = false;
            button2.Visible = false;
            button5.Visible = false;
            button6.Visible = false;

            // Показываем элементы режима редактирования
            textBox1.Visible = true;
            textBox2.Visible = true;
            comboBox1.Visible = true;
            textBox3.Visible = true;
            numericUpDown1.Visible = true;
            label5.Visible = true;
            label6.Visible = true;
            button3.Visible = true;
            button4.Visible = true;
            label8.Visible = true;
            label9.Visible = true;
            label10.Visible = true;
            label12.Visible = true;
            panel1.Visible = true;
        }

        public void HideEditMode()
        {
            // Скрываем элементы режима редактирования
            textBox1.Visible = false;
            textBox2.Visible = false;
            comboBox1.Visible = false;
            textBox3.Visible = false;
            numericUpDown1.Visible = false;
            label5.Visible = false;
            label6.Visible = false;
            button3.Visible = false;
            button4.Visible = false;
            label8.Visible = false;
            label9.Visible = false;
            label10.Visible = false;
            label12.Visible = false;
            panel1.Visible = false;

            // Показываем элементы режима просмотра
            label1.Visible = true;
            label2.Visible = true;
            label3.Visible = true;
            label4.Visible = true;
            button1.Visible = true;
            button2.Visible = true;

            // Проверяем влезает ли описание после выхода из редактирования
            UpdateExpandButtonVisibility();
        }

        /// <summary>
        /// Получает контрол изображения для управления фото товара
        /// </summary>
        public ImageControl GetImageControl()
        {
            return imageControl1;
        }

        public void UpdateAddToCartButtonState(bool isProductInCart, string userRole)
        {
            if (userRole != "Менеджер")
            {
                return;
            }

            button6.Enabled = true;
            if (isProductInCart)
            {
                button6.Enabled = false;
            }
        }

        private void ProductCard_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                if (AddToCartClicked != null)
                {
                    AddToCartClicked(this, e);
                }
            }
        }

        /// <summary>
        /// Перенаправляет событие клика с изображения на карточку
        /// </summary>
        private void PictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            ProductCard_MouseDown(this, e);
        }

        /// <summary>
        /// Ограничивает ввод в поле цены только цифрами и не позволяет начинать с нуля
        /// </summary>
        private void textBox3_KeyPress(object sender, KeyPressEventArgs e)
        {
            InputRest.OnlyNumbers(e);

            // Не позволяем вводить ноль если поле пусто или уже содержит ноль
            if ((textBox3.Text.Length == 0 || textBox3.Text == "0") && e.KeyChar == '0' && !char.IsControl(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        /// <summary>
        /// При вводе названия товара делает первую букву заглавной
        /// </summary>
        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            InputRest.FirstLetter(textBox1);
        }

        /// <summary>
        /// При вводе описания товара делает первую букву заглавной
        /// </summary>
        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            InputRest.FirstLetter(textBox2);
        }

        /// <summary>
        /// Ограничивает ввод копеек только цифрами и максимум 2 цифры (00-99)
        /// </summary>
        private void numericUpDown1_KeyPress(object sender, KeyPressEventArgs e)
        {
            NumericUpDown nud = sender as NumericUpDown;

            InputRest.OnlyNumbers(e);

            if (char.IsControl(e.KeyChar))
            {
                return;
            }

            string currentText = nud.Text;

            // Не позволяем вводить более 2 символов
            if (currentText.Length >= 2)
            {
                e.Handled = true;
                return;
            }

            string newText = currentText.Insert(currentText.Length, e.KeyChar.ToString());
            if (int.TryParse(newText, out int value))
            {
                // Не позволяем значения больше 99
                if (value > 99)
                {
                    e.Handled = true;
                }
            }
        }

        private void button5_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                ContextMenuStrip = null;
            }
        }

        /// <summary>
        /// При изменении размера контрола перепроверяем видимость кнопки
        /// </summary>
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateExpandButtonVisibility();
        }

        /// <summary>
        /// При масштабировании контрола перепроверяем видимость кнопки
        /// </summary>
        protected override void ScaleControl(SizeF factor, BoundsSpecified specified)
        {
            base.ScaleControl(factor, specified);
            UpdateExpandButtonVisibility();
        }
    }
}