using MySql.Data.MySqlClient;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace WebSiteDev.AddForm
{
    public partial class AddProductForm : ScalableForm
    {
        protected override float MaxScale => 1.6f;
        protected override float MinScale => 0.9f;

        private DataManipulation dataManipulation;
        private string SelectedFileName = null;
        public string CurrentImagePath { get; set; }

        public AddProductForm(DataManipulation dm)
        {
            InitializeComponent();

            dataManipulation = dm;
            dataManipulation.FillComboBoxWithCategories(comboBox1, "Выберите категорию");
        }

        private void AddProductForm_Load(object sender, EventArgs e)
        {
            LabelColor.ApplyRedStar(this);
            Inactivity.OnFormLoad(this);
        }

        /// <summary>
        /// Обработчик кнопки закрытия формы
        /// </summary>
        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Применяет форматирование первой буквы при вводе названия услуги
        /// </summary>
        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            InputRest.FirstLetter(textBox1);
        }

        /// <summary>
        /// Применяет форматирование первой буквы при вводе описания
        /// </summary>
        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            InputRest.FirstLetter(textBox2);
        }

        /// <summary>
        /// Разрешает вводить все символы в поле названия
        /// </summary>
        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            InputRest.AllowAll(e);
        }

        /// <summary>
        /// Разрешает вводить русские и английские буквы и цифры в описание
        /// </summary>
        private void textBox2_KeyPress(object sender, KeyPressEventArgs e)
        {
            InputRest.RussianEnglishAndDigits(e);
        }

        /// <summary>
        /// Разрешает вводить только цифры в поле рублей
        /// </summary>
        private void textBox3_KeyPress(object sender, KeyPressEventArgs e)
        {
            InputRest.OnlyNumbers(e);

            // Запрещает ввести 0 в начало если поле пусто
            if ((textBox3.Text.Length == 0 || textBox3.Text == "0") && e.KeyChar == '0' && !char.IsControl(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        /// <summary>
        /// Возвращает путь к папке для сохранения изображений в AppData
        /// </summary>
        private string GetImagesFolderPath()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            return Path.Combine(appData, "WebShop", "Images");
        }

        /// <summary>
        /// Обработчик кнопки выбора изображения
        /// </summary>
        private void button3_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Изображения (*.jpg; *.jpeg; *.png)|*.jpg;*.jpeg;*.png";
                openFileDialog.Title = "Выберите изображение услуги";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string sourcePath = openFileDialog.FileName;

                    // Получаем информацию о файле
                    FileInfo fileInfo = new FileInfo(sourcePath);

                    // Проверяем что размер не больше 2 МБ
                    if (fileInfo.Length > 2 * 1024 * 1024)
                    {
                        MessageBox.Show("Изображение не должно превышать 2 МБ!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    // Читаем новое изображение в память
                    byte[] newImageBytes = File.ReadAllBytes(sourcePath);

                    // Если уже выбрано изображение проверяем что это не одно и то же
                    if (!string.IsNullOrEmpty(SelectedFileName))
                    {
                        if (File.Exists(SelectedFileName))
                        {
                            try
                            {
                                byte[] oldImageBytes = File.ReadAllBytes(SelectedFileName);

                                // Сравниваем размеры файлов
                                if (oldImageBytes.Length == newImageBytes.Length)
                                {
                                    // Сравниваем содержимое побайтово
                                    bool isIdentical = true;

                                    for (int i = 0; i < oldImageBytes.Length; i++)
                                    {
                                        if (oldImageBytes[i] != newImageBytes[i])
                                        {
                                            isIdentical = false;
                                            break;
                                        }
                                    }

                                    // Если изображения идентичны прерываем выбор
                                    if (isIdentical)
                                    {
                                        MessageBox.Show("Данное изображение уже выбрано!", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                        return;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show("Ошибка при чтении старого изображения:\n" + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return;
                            }
                        }
                    }

                    // Сохраняем путь к файлу
                    SelectedFileName = sourcePath;

                    try
                    {
                        // Освобождаем старое изображение если оно есть
                        if (pictureBox1.Image != null)
                        {
                            pictureBox1.Image.Dispose();
                        }

                        // Отображаем новое изображение в превью
                        pictureBox1.BackgroundImage = null;
                        pictureBox1.Image = Image.FromFile(sourcePath);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Ошибка при загрузке изображения:\n" + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        SelectedFileName = null;
                    }
                }
            }
        }

        /// <summary>
        /// Обработчик кнопки добавления услуги
        /// </summary>
        private void button2_Click(object sender, EventArgs e)
        {
            // Получаем все введённые данные
            string ProductName = textBox1.Text.Trim();
            string ProductDesc = textBox2.Text.Trim();
            string RublesText = textBox3.Text.Trim();

            // Получаем ID категории из SelectedValue
            // Если категория не выбрана останется 0
            int CategoryId = 0;

            if (comboBox1.SelectedValue != null && comboBox1.SelectedValue != DBNull.Value)
            {
                int.TryParse(comboBox1.SelectedValue.ToString(), out CategoryId);
            }

            // Проверка обязательных полей
            if (string.IsNullOrEmpty(ProductName) || string.IsNullOrEmpty(ProductDesc) || CategoryId <= 0 || string.IsNullOrEmpty(RublesText))
            {
                MessageBox.Show("Необходимо заполнить поля отмеченные \"*\"", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (ProductName.Length < 3)
            {
                MessageBox.Show("Название услуги должно быть минимум 3 символа!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (ProductDesc.Length < 10)
            {
                MessageBox.Show("Описание должно быть минимум 10 символов!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!int.TryParse(RublesText, out int rubles))
            {
                MessageBox.Show("Рубли должны быть числом!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (rubles < 0)
            {
                MessageBox.Show("Рубли не могут быть отрицательными!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Получаем копейки
            int kopecks = Convert.ToInt32(numericUpDown1.Value);

            // Проверяем что цена не нулевая
            if (rubles == 0 && kopecks == 0)
            {
                MessageBox.Show("Цена должна быть больше нуля!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Формируем полную цену
            decimal price = rubles + (kopecks / 100.0m);

            string ProductPhoto = "";

            // Если изображение выбрано копируем его в папку AppData
            if (!string.IsNullOrEmpty(SelectedFileName))
            {
                string fileName = Path.GetFileNameWithoutExtension(SelectedFileName);
                string extension = Path.GetExtension(SelectedFileName);

                // Получаем путь к папке для сохранения изображений
                string imagesFolder = GetImagesFolderPath();

                // Создаём папку если её нет
                if (!FolderPermissions.CreateFolderWithFullAccess(imagesFolder))
                {
                    return;
                }

                try
                {
                    byte[] newImageBytes = File.ReadAllBytes(SelectedFileName);
                    string destPath = Path.Combine(imagesFolder, fileName + extension);

                    // Если файл с таким именем уже существует
                    if (File.Exists(destPath))
                    {
                        byte[] existingImageBytes = File.ReadAllBytes(destPath);
                        bool isIdentical = false;

                        // Сравниваем с существующим файлом
                        if (existingImageBytes.Length == newImageBytes.Length)
                        {
                            isIdentical = true;

                            for (int i = 0; i < newImageBytes.Length; i++)
                            {
                                if (newImageBytes[i] != existingImageBytes[i])
                                {
                                    isIdentical = false;
                                    break;
                                }
                            }
                        }

                        // Если файлы идентичны используем существующий
                        if (isIdentical)
                        {
                            ProductPhoto = Path.GetFileName(destPath);
                        }
                        else
                        {
                            // Если разные добавляем номер к имени файла в ()
                            int n = 1;

                            while (File.Exists(destPath))
                            {
                                destPath = Path.Combine(imagesFolder, fileName + " (" + n.ToString() + ")" + extension);
                                n++;
                            }
                            File.Copy(SelectedFileName, destPath, false);
                            ProductPhoto = Path.GetFileName(destPath);
                        }
                    }
                    else
                    {
                        // Копируем новый файл
                        File.Copy(SelectedFileName, destPath, false);
                        ProductPhoto = Path.GetFileName(destPath);
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    MessageBox.Show("Нет прав доступа к папке изображений!\n\nЗапустите программу от имени администратора.", "Ошибка доступа", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при копировании изображения:\n" + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            // Добавляем услугу в БД
            try
            {
                using (MySqlConnection con = new MySqlConnection(Data.GetConnectionString()))
                {
                    con.Open();

                    // Проверяем что услуга с таким названием не существует
                    string СheckQuery = "SELECT COUNT(*) FROM Product WHERE ProductName = @ProductName";

                    using (MySqlCommand cmd1 = new MySqlCommand(СheckQuery, con))
                    {
                        cmd1.Parameters.AddWithValue("@ProductName", ProductName);

                        int count = Convert.ToInt32(cmd1.ExecuteScalar());

                        if (count > 0)
                        {
                            MessageBox.Show("Услуга с таким названием уже существует!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                    }

                    // Добавляем новую услугу в таблицу
                    string InsertQuery = @"INSERT INTO Product (ProductName, ProductDescription, ProductPhoto, 
                                                                CategoryID, BasePrice) 
                                           VALUES (@ProductName, @ProductDesc, @ProductPhoto, @CategoryId, 
                                                   @BasePrice)";

                    using (MySqlCommand cmd2 = new MySqlCommand(InsertQuery, con))
                    {
                        cmd2.Parameters.AddWithValue("@ProductName", ProductName);
                        cmd2.Parameters.AddWithValue("@ProductDesc", ProductDesc);
                        cmd2.Parameters.AddWithValue("@ProductPhoto", ProductPhoto);
                        cmd2.Parameters.AddWithValue("@CategoryId", CategoryId);
                        cmd2.Parameters.AddWithValue("@BasePrice", price);

                        cmd2.ExecuteNonQuery();
                    }

                    MessageBox.Show("Услуга успешно добавлена!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // Очищаем все поля для добавления следующей услуги
                    textBox1.Clear();
                    textBox2.Clear();
                    textBox3.Clear();
                    numericUpDown1.Value = 0;
                    comboBox1.SelectedIndex = 0;

                    // Очищаем превью изображения
                    if (pictureBox1.Image != null)
                    {
                        pictureBox1.Image.Dispose();
                    }

                    pictureBox1.BackgroundImage = Properties.Resources.no_image;
                    pictureBox1.Image = null;

                    SelectedFileName = null;
                    CurrentImagePath = null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при добавлении услуги:\n" + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Обработчик кнопки сброса выбранного изображения
        /// </summary>
        private void button4_Click(object sender, EventArgs e)
        {
            // Запрашиваем подтверждение
            var result = MessageBox.Show("Вы действительно хотите сбросить выбранное изображение?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                // Освобождаем ресурсы старого изображения
                if (pictureBox1.Image != null)
                {
                    pictureBox1.Image.Dispose();
                }

                // Отображаем изображение по умолчанию
                pictureBox1.BackgroundImage = Properties.Resources.no_image;
                pictureBox1.Image = null;

                SelectedFileName = null;
            }
        }

        /// <summary>
        /// Ограничивает ввод в поле копеек только цифрами от 0 до 99
        /// </summary>
        private void numericUpDown1_KeyPress(object sender, KeyPressEventArgs e)
        {
            InputRest.OnlyNumbers(e);

            NumericUpDown numericUpDown = sender as NumericUpDown;

            // Пропускаем служебные клавиши
            if (char.IsControl(e.KeyChar))
            {
                return;
            }

            string currentText = numericUpDown.Text;

            // Не допускаем более 2 символов
            if (currentText.Length >= 2)
            {
                e.Handled = true;
                return;
            }

            // Проверяем что результат не превысит 99
            string newText = currentText.Insert(currentText.Length, e.KeyChar.ToString());

            if (int.TryParse(newText, out int value))
            {
                if (value > 99)
                {
                    e.Handled = true;
                }
            }
        }

        private void AddProductForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Inactivity.OnFormClosing(this);
        }
    }
}