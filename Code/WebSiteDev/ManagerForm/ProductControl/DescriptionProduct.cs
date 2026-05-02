using System;
using System.Windows.Forms;

namespace WebSiteDev
{
    /// <summary>
    /// Форма для просмотра полного описания товара в отдельном окне
    /// </summary>
    public partial class DescriptionProduct : Form
    {
        public DescriptionProduct()
        {
            InitializeComponent();
        }

        public void SetDescription(string productName, string description)
        {
            // Устанавливаем заголовок окна с названием товара
            this.Text = "Описание: " + productName;

            // Выводим полное описание в многострочное текстовое поле
            textBox1.Text = description;

            // Убираем выделение текста и устанавливаем курсор в начало
            textBox1.Select(0, 0);
        }

        /// <summary>
        /// Кнопка закрыть
        /// </summary>
        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void DescriptionProduct_Load(object sender, EventArgs e)
        {
            Inactivity.OnFormLoad(this);
        }

        private void DescriptionProduct_FormClosing(object sender, FormClosingEventArgs e)
        {
            Inactivity.OnFormClosing(this);
        }
    }
}