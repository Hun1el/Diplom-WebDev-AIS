using MySql.Data.MySqlClient;
using System;
using System.Windows.Forms;

namespace WebSiteDev.AddForm
{
    public enum FormMode
    {
        Add = 0,
        Edit = 1
    }

    /// <summary>
    /// Форма для добавления новой категории товаров в базу данных
    /// </summary>
    public partial class AddEditCategoryForm : ScalableForm
    {
        protected override float MaxScale => 1.6f;
        protected override float MinScale => 0.9f;

        private FormMode mode;
        private int selectedCategoryID;
        private string categoryName;

        public AddEditCategoryForm(FormMode mode, int selectedCategoryID = -1, string categoryName = "")
        {
            InitializeComponent();

            this.mode = mode;
            this.selectedCategoryID = selectedCategoryID;
            this.categoryName = categoryName;
        }

        private void AddCategoryForm_Load(object sender, EventArgs e)
        {
            Inactivity.OnFormLoad(this);
            LabelColor.ApplyRedStar(this);

            if (mode == FormMode.Edit)
            {
                textBox1.Text = categoryName;
                textBox1.SelectionStart = textBox1.Text.Length;
                textBox1.SelectionLength = 0;
                button2.Text = "Изменить категорию";
                this.Text = "Изменение категории";
            }
            else
            {
                button2.Text = "Добавить";
                this.Text = "Добавление категории";
            }
        }

        /// <summary>
        /// Обработчик кнопки закрытия формы
        /// </summary>
        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Обработчик кнопки добавления новой категории
        /// </summary>
        private void button2_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(textBox1.Text))
            {
                MessageBox.Show("Необходимо заполнить поля отмеченные \"*\"", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (MySqlConnection con = new MySqlConnection(Data.GetConnectionString()))
                {
                    string СheckQuery = @"SELECT COUNT(*) FROM Category WHERE CategoryName = @name";
                    string InsertQuery = @"INSERT INTO Category (CategoryName) VALUES (@name)";

                    con.Open();

                    if (mode == FormMode.Add)
                    {
                        string name = textBox1.Text.Trim();

                        // Проверяем существует ли уже такая категория
                        using (MySqlCommand cmd1 = new MySqlCommand(СheckQuery, con))
                        {
                            cmd1.Parameters.AddWithValue("@name", name);
                            int count = Convert.ToInt32(cmd1.ExecuteScalar());

                            if (count > 0)
                            {
                                MessageBox.Show("Такая категория уже существует!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                return;
                            }
                        }

                        // Добавляем новую категорию
                        using (MySqlCommand cmd2 = new MySqlCommand(InsertQuery, con))
                        {
                            cmd2.Parameters.AddWithValue("@name", name);
                            cmd2.ExecuteNonQuery();
                        }

                        MessageBox.Show("Категория успешно добавлена!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        textBox1.Clear();
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(textBox1.Text))
                        {
                            MessageBox.Show("Необходимо заполнить поля отмеченные \"*\"", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }

                        var result = MessageBox.Show("Вы действительно хотите изменить категорию?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                        if (result == DialogResult.Yes)
                        {
                            // Обновляем категорию в БД
                            if (DataUpdate.UpdateCategory(selectedCategoryID, textBox1.Text.Trim()))
                            {
                                MessageBox.Show("Категория успешно изменена!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                this.Close();
                            }
                        }
                    }
                }
            }
            catch (Exception Ex)
            {
                MessageBox.Show("Ошибка: " + Ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Применяет форматирование первой буквы при вводе текста
        /// </summary>
        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            InputRest.FirstLetter(textBox1);
        }

        /// <summary>
        /// Ограничивает ввод только допустимыми символами для названия категории
        /// </summary>
        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            InputRest.CategoryInput(e);
        }

        private void AddCategoryForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Inactivity.OnFormClosing(this);
        }
    }
}