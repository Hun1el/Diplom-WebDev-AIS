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
        private string selectedImageExtension; // Расширение выбранного файла определяется автоматически по наличию альфа-канала
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

                    try
                    {
                        // Формируем путь к старому файлу для проверки на дубль
                        string oldFullPath = null;

                        if (!string.IsNullOrEmpty(SelectedFileName))
                        {
                            oldFullPath = SelectedFileName;
                        }

                        // Обрабатываем выбранный файл через отдельный класс
                        ImageSelect selector = new ImageSelect();
                        bool processed = selector.Process(sourcePath, 2L * 1024 * 1024, oldFullPath);

                        // Если дубль показываем сообщение и выходим
                        if (!processed)
                        {
                            if (selector.IsDuplicate)
                            {
                                MessageBox.Show("Данное изображение уже выбрано!", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                            return;
                        }

                        // Сохраняем путь к временному сжатому файлу и расширение
                        SelectedFileName = selector.TempFilePath;
                        selectedImageExtension = selector.FileExtension;

                        // Освобождаем старое изображение если оно есть
                        if (pictureBox1.Image != null)
                        {
                            pictureBox1.Image.Dispose();
                        }

                        // Отображаем новое изображение в превью через MemoryStream чтобы не блокировать файл
                        pictureBox1.BackgroundImage = null;

                        using (MemoryStream memoryStream = new MemoryStream(selector.ImageBytes))
                        {
                            pictureBox1.Image = Image.FromStream(memoryStream, true);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Ошибка при обработке изображения:\n" + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        SelectedFileName = null;
                        selectedImageExtension = null;
                    }
                }
            }
        }

        /// <summary>
        /// Обработчик кнопки добавления услуги
        /// Сначала добавляет запись затем копирует сжатое изображение
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

            // Добавляем услугу в БД
            try
            {
                using (MySqlConnection con = new MySqlConnection(Data.GetConnectionString()))
                {
                    string CheckQuery = "SELECT COUNT(*) FROM Product WHERE ProductName = @ProductName";

                    con.Open();

                    using (MySqlCommand cmd1 = new MySqlCommand(CheckQuery, con))
                    {
                        cmd1.Parameters.AddWithValue("@ProductName", ProductName);

                        int count = Convert.ToInt32(cmd1.ExecuteScalar());

                        if (count > 0)
                        {
                            MessageBox.Show("Услуга с таким названием уже существует!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                    }

                    // Добавляем новую услугу в таблицу без фото
                    string InsertQuery = @"INSERT INTO Product (ProductName, ProductDescription, ProductPhoto, 
                                                                CategoryID, BasePrice) 
                                           VALUES (@ProductName, @ProductDesc, @ProductPhoto, @CategoryId, 
                                                   @BasePrice)";

                    long newProductId = 0;

                    using (MySqlCommand cmd2 = new MySqlCommand(InsertQuery, con))
                    {
                        cmd2.Parameters.AddWithValue("@ProductName", ProductName);
                        cmd2.Parameters.AddWithValue("@ProductDesc", ProductDesc);
                        cmd2.Parameters.AddWithValue("@ProductPhoto", "");
                        cmd2.Parameters.AddWithValue("@CategoryId", CategoryId);
                        cmd2.Parameters.AddWithValue("@BasePrice", price);

                        cmd2.ExecuteNonQuery();
                        newProductId = cmd2.LastInsertedId;
                    }

                    // Если изображение выбрано копируем его в папку и обновляем путь в БД
                    if (!string.IsNullOrEmpty(SelectedFileName) && newProductId > 0)
                    {
                        string imagesFolder = GetImagesFolderPath();

                        // Создаём папку для изображений если её нет
                        if (!FolderPermissions.CreateFolderWithFullAccess(imagesFolder))
                        {
                            MessageBox.Show("Ошибка: не удалось создать папку для изображений!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }

                        try
                        {
                            // Формируем имя файла
                            // GUID гарантирует уникальность
                            string guidString = Guid.NewGuid().ToString("N");
                            string baseName = "product_" + newProductId.ToString() + "_" + guidString;

                            string ext;

                            if (selectedImageExtension == null)
                            {
                                ext = ".jpg";
                            }
                            else
                            {
                                ext = selectedImageExtension;
                            }

                            string destPath = Path.Combine(imagesFolder, baseName + ext);
                            string finalFileName = baseName + ext;

                            // На случай совпадения GUID
                            int n = 1;

                            while (File.Exists(destPath))
                            {
                                string suffix = "_" + n.ToString();
                                destPath = Path.Combine(imagesFolder, baseName + suffix + ext);
                                n++;
                            }

                            finalFileName = Path.GetFileName(destPath);

                            // Копируем сжатый временный файл в папку изображений
                            File.Copy(SelectedFileName, destPath, false);

                            // Обновляем путь к фото в базе
                            string UpdateQuery = "UPDATE Product SET ProductPhoto = @photo WHERE ProductID = @id";

                            using (MySqlCommand cmd3 = new MySqlCommand(UpdateQuery, con))
                            {
                                cmd3.Parameters.AddWithValue("@photo", finalFileName);
                                cmd3.Parameters.AddWithValue("@id", newProductId);
                                cmd3.ExecuteNonQuery();
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
                    selectedImageExtension = null;
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
                selectedImageExtension = null;
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