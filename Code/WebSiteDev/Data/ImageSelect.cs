using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace WebSiteDev
{
    /// <summary>
    /// Класс для обработки выбранного изображения
    /// </summary>
    public class ImageSelect
    {
        public string TempFilePath; // Путь к временному файлу сжатого изображения
        public string FileExtension; // Расширение файла определяется автоматически по наличию альфа-канала
        public bool IsDuplicate; // true если выбрано изображение идентичное текущему
        public byte[] ImageBytes; // Сжатые байты изображения для загрузки в PictureBox без блокировки файла

        // Внутреннее поле для хранения расширения на время обработки
        private string selectedImageExtension;

        /// <summary>
        /// Обрабатывает выбранный файл
        /// Результат работы записывается в публичные поля класса
        /// </summary>
        public bool Process(string sourcePath, long maxSizeBytes, string oldFullPath)
        {
            // Сжимаем изображение до заданного размера
            byte[] compressedBytes = CompressImage(sourcePath, maxSizeBytes);

            // Если все равно больше лимита
            if (compressedBytes.Length > maxSizeBytes)
            {
                throw new Exception("Не удалось сжать изображение до заданного размера.");
            }

            // Сохраняем результат в поля класса
            ImageBytes = compressedBytes;
            FileExtension = selectedImageExtension;

            // Создаём временный файл с правильным расширением
            string tempFolder = Path.Combine(Path.GetTempPath(), "WebShop", "TempImages");
            Directory.CreateDirectory(tempFolder);

            string tempPath = Path.Combine(tempFolder, Guid.NewGuid().ToString() + FileExtension);
            File.WriteAllBytes(tempPath, compressedBytes);

            // Сравниваем новое изображение со старым чтобы избежать дублей
            if (!string.IsNullOrEmpty(oldFullPath))
            {
                if (File.Exists(oldFullPath))
                {
                    byte[] oldImageBytes = File.ReadAllBytes(oldFullPath);

                    // Сравниваем побайтово
                    if (oldImageBytes.Length == compressedBytes.Length)
                    {
                        bool isIdentical = true;

                        for (int i = 0; i < oldImageBytes.Length; i++)
                        {
                            if (oldImageBytes[i] != compressedBytes[i])
                            {
                                isIdentical = false;
                                break;
                            }
                        }

                        // Если содержимое идентично сообщаем об этом и удаляем временный файл
                        if (isIdentical)
                        {
                            IsDuplicate = true;
                            try
                            {
                                File.Delete(tempPath);
                            }
                            catch
                            {
                            }
                            return false;
                        }
                    }
                }
            }

            // Заполняем результат и помечаем что дубля нет
            TempFilePath = tempPath;
            IsDuplicate = false;
            return true;
        }

        /// <summary>
        /// Сжимает изображение до заданного размера
        /// Если изображение содержит альфа-канал сохраняет png
        /// </summary>
        private byte[] CompressImage(string sourcePath, long maxSizeBytes)
        {
            Image img = null;
            Bitmap resized = null;
            Graphics g = null;
            MemoryStream memoryStream = null;

            try
            {
                img = Image.FromFile(sourcePath);
                // Проверяем есть ли у изображения прозрачность
                bool hasAlpha = Image.IsAlphaPixelFormat(img.PixelFormat);

                if (hasAlpha)
                {
                    selectedImageExtension = ".png";
                }
                else
                {
                    selectedImageExtension = ".jpg";
                }

                // Максимальные размеры для картинок
                int maxWidth = 1920;
                int maxHeight = 1080;
                int newWidth = img.Width;
                int newHeight = img.Height;

                // Масштабируем если изображение слишком большое
                if (newWidth > maxWidth || newHeight > maxHeight)
                {
                    double ratioX = (double)maxWidth / (double)newWidth;
                    double ratioY = (double)maxHeight / (double)newHeight;
                    double ratio = ratioX;

                    if (ratioY < ratio)
                    {
                        ratio = ratioY;
                    }

                    newWidth = (int)((double)newWidth * ratio);
                    newHeight = (int)((double)newHeight * ratio);
                }

                resized = new Bitmap(newWidth, newHeight);
                g = Graphics.FromImage(resized);

                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                g.DrawImage(img, 0, 0, newWidth, newHeight);

                // Если есть прозрачность
                if (hasAlpha)
                {
                    memoryStream = new MemoryStream();
                    resized.Save(memoryStream, ImageFormat.Png);

                    // png сжимается без потерь
                    if (memoryStream.Length > maxSizeBytes)
                    {
                        byte[] result = ResizeBitmapToFit(resized, maxSizeBytes, true, null);
                        return result;
                    }

                    byte[] pngResult = memoryStream.ToArray();
                    return pngResult;
                }
                else
                {
                    // Для обычных фото используем jpeg
                    ImageCodecInfo jpegCodec = GetEncoder(ImageFormat.Jpeg);

                    if (jpegCodec == null)
                    {
                        throw new Exception("JPEG кодек не найден в системе");
                    }

                    memoryStream = new MemoryStream();
                    int quality = 95;

                    // Качество от 95 до 30 с шагом 5
                    while (quality >= 30)
                    {
                        memoryStream.SetLength(0);
                        EncoderParameters parameters = new EncoderParameters(1);
                        parameters.Param[0] = new EncoderParameter(Encoder.Quality, (long)quality);
                        resized.Save(memoryStream, jpegCodec, parameters);

                        if (memoryStream.Length <= maxSizeBytes)
                        {
                            byte[] result = memoryStream.ToArray();
                            return result;
                        }

                        quality = quality - 5;
                    }

                    // Если при 30 не влезли уменьшаем разрешение ещё сильнее
                    byte[] jpegResult = ResizeBitmapToFit(resized, maxSizeBytes, false, jpegCodec);
                    return jpegResult;
                }
            }
            finally // Очистка 
            {
                if (memoryStream != null)
                {
                    memoryStream.Dispose();
                }

                if (g != null)
                {
                    g.Dispose();
                }

                if (resized != null)
                {
                    resized.Dispose();
                }

                if (img != null)
                {
                    img.Dispose();
                }
            }
        }

        /// <summary>
        /// Агрессивное уменьшение разрешения если стандартное сжатие не помогло
        /// </summary>
        private byte[] ResizeBitmapToFit(Bitmap source, long maxSizeBytes, bool isPng, ImageCodecInfo jpegCodec)
        {
            double scale = 0.7; // Уменьшаем на 30% за итерацию
            int width = source.Width;
            int height = source.Height;

            while (width > 100 && height > 100)
            {
                width = (int)((double)width * scale);
                height = (int)((double)height * scale);

                Bitmap smaller = null;
                Graphics g = null;
                MemoryStream ms = null;

                try
                {
                    smaller = new Bitmap(width, height);
                    g = Graphics.FromImage(smaller);

                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                    g.DrawImage(source, 0, 0, width, height);

                    ms = new MemoryStream();

                    if (isPng)
                    {
                        smaller.Save(ms, ImageFormat.Png);
                    }
                    else
                    {
                        EncoderParameters parameters = new EncoderParameters(1);
                        parameters.Param[0] = new EncoderParameter(Encoder.Quality, 70L);
                        smaller.Save(ms, jpegCodec, parameters);
                    }

                    if (ms.Length <= maxSizeBytes)
                    {
                        byte[] result = ms.ToArray();
                        return result;
                    }
                }
                finally
                {
                    if (ms != null)
                    {
                        ms.Dispose();
                    }

                    if (g != null)
                    {
                        g.Dispose();
                    }

                    if (smaller != null)
                    {
                        smaller.Dispose();
                    }
                }
            }

            // Крайний случай возвращаем последний вариант
            MemoryStream memoryStreamF = new MemoryStream();

            try
            {
                if (isPng)
                {
                    source.Save(memoryStreamF, ImageFormat.Png);
                }
                else
                {
                    EncoderParameters finalParams = new EncoderParameters(1);
                    finalParams.Param[0] = new EncoderParameter(Encoder.Quality, 30L);
                    source.Save(memoryStreamF, jpegCodec, finalParams);
                }

                byte[] result = memoryStreamF.ToArray();
                return result;
            }
            finally
            {
                memoryStreamF.Dispose();
            }
        }

        /// <summary>
        /// Возвращает кодек для указанного формата
        /// </summary>
        private ImageCodecInfo GetEncoder(ImageFormat format)
        {
            foreach (ImageCodecInfo codec in ImageCodecInfo.GetImageEncoders())
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }
    }
}