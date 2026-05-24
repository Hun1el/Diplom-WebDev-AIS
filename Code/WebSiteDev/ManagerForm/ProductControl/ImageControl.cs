using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace WebSiteDev
{
    /// <summary>
    /// Контрол для управления изображениями товара
    /// </summary>
    public partial class ImageControl : ScalableUserControl
    {
        private string selectedImagePath;
        private Image originalImage;
        // Расширение выбранного файла определяется автоматически по наличию альфа-канала
        private string selectedImageExtension;

        public string CurrentImagePath { get; set; }

        public ImageControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Получает путь к папке с изображениями в AppData
        /// </summary>
        private string GetImagesFolderPath()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, "WebShop", "Images");
        }

        /// <summary>
        /// Показывает или скрывает кнопку для изменения изображения
        /// </summary>
        public void ShowChangeButton(bool show)
        {
            button1.Visible = show;
        }

        public void InitializeImage(string currentImagePath)
        {
            CurrentImagePath = currentImagePath;
            LoadImage(currentImagePath);

            // Сохраняем оригинальное изображение для возможности отката изменений
            if (pictureBox1.Image != null)
            {
                originalImage = new Bitmap(pictureBox1.Image);
            }
            else
            {
                originalImage = null;
            }
        }

        /// <summary>
        /// Кнопка изменить открывает диалог выбора файла и загружает новое изображение
        /// </summary>
        private void button1_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp|All Files|*.*";
                openFileDialog.Title = "Выберите изображение";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string sourcePath = openFileDialog.FileName;

                    try
                    {
                        // Формируем путь к старому файлу для проверки на дубль
                        string oldFullPath = null;

                        if (!string.IsNullOrEmpty(CurrentImagePath))
                        {
                            string imagesFolder = GetImagesFolderPath();
                            oldFullPath = Path.Combine(imagesFolder, CurrentImagePath);
                        }

                        // Обрабатываем выбранный файл через отдельный класс
                        ImageSelect selector = new ImageSelect();
                        bool processed = selector.Process(sourcePath, 2L * 1024 * 1024, oldFullPath);

                        // Если дубль показываем сообщение и выходим
                        if (!processed)
                        {
                            if (selector.IsDuplicate)
                            {
                                MessageBox.Show("Вы выбрали изображение с идентичным содержимым. Изменений нет.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }

                            return;
                        }

                        // Сохраняем путь и расширение из результата обработки
                        selectedImagePath = selector.TempFilePath;
                        selectedImageExtension = selector.FileExtension;

                        // Загружаем через MemoryStream чтобы не блокировать файл
                        using (MemoryStream ms = new MemoryStream(selector.ImageBytes))
                        {
                            pictureBox1.Image = Image.FromStream(ms, true);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Ошибка: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        /// <summary>
        /// Сохраняет выбранное изображение в папку и обновляет путь в БД
        /// </summary>
        public void SaveImage(int productID)
        {
            // Если изображение не было выбрано выходим
            if (string.IsNullOrEmpty(selectedImagePath))
            {
                return;
            }

            // Очищаем PictureBox чтобы освободить файл для копирования
            pictureBox1.Image = null;

            string imagesFolder = GetImagesFolderPath();

            try
            {
                // Создаём папку для изображений если её нет
                if (!FolderPermissions.CreateFolderWithFullAccess(imagesFolder))
                {
                    MessageBox.Show("Ошибка: не удалось создать папку для изображений!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                byte[] newImageBytes = File.ReadAllBytes(selectedImagePath);

                // Формируем имя файла
                // GUID гарантирует уникальность
                string guidString = Guid.NewGuid().ToString("N");
                string baseName = "product_" + productID.ToString() + "_" + guidString;

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

                // На случай совпадения GUID добавляем номер к имени
                int n = 1;

                while (File.Exists(destPath))
                {
                    string suffix = "_" + n.ToString();
                    destPath = Path.Combine(imagesFolder, baseName + suffix + ext);
                    n++;
                }

                finalFileName = Path.GetFileName(destPath);

                // Копируем новый файл в папку изображений
                File.Copy(selectedImagePath, destPath, false);

                // Обновляем путь к фото в базе
                using (MySqlConnection con = new MySqlConnection(Data.GetConnectionString()))
                {
                    string UpdateQuery = "UPDATE Product SET ProductPhoto = @photo WHERE ProductID = @id";

                    con.Open();

                    using (MySqlCommand cmd = new MySqlCommand(UpdateQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@photo", finalFileName);
                        cmd.Parameters.AddWithValue("@id", productID);
                        cmd.ExecuteNonQuery();
                    }
                }

                // Загружаем сохранённое изображение и обновляем оригинал
                LoadImage(finalFileName);
                CurrentImagePath = finalFileName;
                selectedImagePath = null;
                selectedImageExtension = null;

                if (pictureBox1.Image != null)
                {
                    originalImage = new Bitmap(pictureBox1.Image);
                }
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show("Нет прав доступа к папке изображений!\n\nЗапустите программу от имени администратора.", "Ошибка доступа", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка сохранения: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Отменяет изменения и возвращает оригинальное изображение
        /// </summary>
        public void CancelEdit()
        {
            selectedImagePath = null;
            selectedImageExtension = null;

            // Восстанавливаем оригинальное изображение если оно было сохранено
            if (originalImage != null)
            {
                pictureBox1.Image = new Bitmap(originalImage);
            }
        }

        /// <summary>
        /// Загружает изображение товара из папки или показывает изображение по умолчанию
        /// </summary>
        public void LoadImage(string photoName)
        {
            if (string.IsNullOrEmpty(photoName))
            {
                return;
            }

            string imagesFolder = GetImagesFolderPath();
            string imagePath = Path.Combine(imagesFolder, photoName);

            if (File.Exists(imagePath))
            {
                try
                {
                    // Читаем файл в MemoryStream чтобы избежать блокировки файла
                    byte[] imageBytes = File.ReadAllBytes(imagePath);
                    using (MemoryStream ms = new MemoryStream(imageBytes))
                    {
                        pictureBox1.Image = Image.FromStream(ms, true);
                    }
                }
                catch
                {
                    // Если ошибка при загрузке показываем изображение по умолчанию
                    pictureBox1.Image = Properties.Resources.no_image;
                }
            }
            else
            {
                // Если файл не найден показываем изображение по умолчанию
                pictureBox1.Image = Properties.Resources.no_image;
            }
        }

        /// <summary>
        /// Перенаправляет событие клика с PictureBox на контрол
        /// </summary>
        private void PictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            this.OnMouseDown(e);
        }

        /// <summary>
        /// Обработчик события клика на контрол
        /// </summary>
        private void ImageControl_MouseDown(object sender, MouseEventArgs e)
        {

        }
    }
}