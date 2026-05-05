using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace WebSiteDev
{
    public static class CaptchaGenerator
    {
        private static readonly Random random = new Random(); // Объявление рандома
        private const string Chars = "ABCDEFGHIJKLMNPQRSTUVWXYZabcdefghijklmnpqrstuvwxyz123456789"; // Символы которые учавствуют в капче
        private const string NoiseChars = "ABCDEFGHIJKLMNPQRSTUVWXYZabcdefghijklmnpqrstuvwxyz123456789@#$%&"; // Символы которые создают шум в капче
        private static readonly string[] FontNames = { "Arial", "Verdana", "Tahoma", "Georgia", "Segoe UI" }; // Допустимые шрифты в капче

        // Цвета
        private static readonly Color[] Colors =
        {
            Color.FromArgb(30, 80, 160), Color.FromArgb(160, 30, 30),
            Color.FromArgb(20, 120, 60), Color.FromArgb(120, 50, 150),
            Color.FromArgb(180, 100, 0), Color.FromArgb(0, 100, 140),
            Color.FromArgb(140, 0, 80), Color.FromArgb(0, 120, 100),
            Color.FromArgb(100, 80, 0), Color.FromArgb(60, 60, 160)
        };

        /// <summary>
        /// Генерирует текстовую капчу и отрисовывает её
        /// </summary>
        /// <param name="width">Ширина картинки</param>
        /// <param name="height">Высота картинки</param>
        /// <param name="length">Длина текста капчи</param>
        /// <param name="captchaText">Возвращаемый сгенерированный текст капчи</param>
        /// <returns>Графическое изображение капчи (Bitmap)</returns>
        public static Bitmap GenerateImage(int width, int height, int length, out string captchaText)
        {
            captchaText = GenerateText(length);

            Bitmap bmp = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;

                DrawBackground(g, width, height);
                DrawNoiseDots(g, width, height);
                DrawNoiseChars(g, width, height);
                DrawLines(g, width, height);
                DrawText(g, captchaText, width, height);
            }

            return bmp;
        }

        /// <summary>
        /// Создание текста
        /// </summary>
        private static string GenerateText(int length)
        {
            char[] result = new char[length];

            for (int i = 0; i < length; i++)
            {
                result[i] = Chars[random.Next(Chars.Length)];
            }

            return new string(result);
        }

        /// <summary>
        /// Задний фон капчи
        /// </summary>
        private static void DrawBackground(Graphics g, int width, int height)
        {
            using (LinearGradientBrush bgBrush = new LinearGradientBrush(
                new Rectangle(0, 0, width, height),
                Color.FromArgb(230, 240, 255),
                Color.FromArgb(200, 220, 245),
                LinearGradientMode.ForwardDiagonal))
            {
                g.FillRectangle(bgBrush, 0, 0, width, height);
            }
        }

        /// <summary>
        /// Шумные точки
        /// </summary>
        private static void DrawNoiseDots(Graphics g, int width, int height)
        {
            for (int i = 0; i < 150; i++)
            {
                int x = random.Next(width);
                int y = random.Next(height);
                int size = random.Next(1, 4);
                int alpha = random.Next(40, 120);

                using (SolidBrush dotBrush = new SolidBrush(Color.FromArgb(alpha,
                       random.Next(80, 180), random.Next(80, 180), random.Next(150, 220))))
                {
                    g.FillEllipse(dotBrush, x, y, size, size);
                }
            }
        }

        /// <summary>
        /// Шумные символы
        /// </summary>
        private static void DrawNoiseChars(Graphics g, int width, int height)
        {
            using (Font noiseFont = new Font("Arial", random.Next(7, 11), FontStyle.Regular))
            {
                for (int i = 0; i < 12; i++)
                {
                    string nc = NoiseChars[random.Next(NoiseChars.Length)].ToString();
                    int alpha = random.Next(40, 90);

                    using (SolidBrush noiseBrush = new SolidBrush(Color.FromArgb(alpha,
                        random.Next(80, 160), random.Next(80, 160), random.Next(120, 200))))
                    {
                        g.DrawString(nc, noiseFont, noiseBrush,
                            random.Next(0, width - 10), random.Next(0, height - 12));
                    }
                }
            }
        }

        /// <summary>
        /// Линии
        /// </summary>
        private static void DrawLines(Graphics g, int width, int height)
        {
            for (int l = 0; l < 2; l++)
            {
                int alpha = random.Next(60, 110);

                using (Pen linePen = new Pen(Color.FromArgb(alpha,
                    random.Next(80, 160), random.Next(80, 160), random.Next(140, 200)), 1))
                {
                    int y1 = random.Next(height / 4, height * 3 / 4);
                    int y2 = random.Next(height / 4, height * 3 / 4);
                    int ymid = random.Next(height / 4, height * 3 / 4);

                    g.DrawBezier(linePen,
                        0, y1,
                        width / 3, ymid - random.Next(-15, 15),
                        width * 2 / 3, ymid + random.Next(-15, 15),
                        width, y2);
                }
            }
        }

        /// <summary>
        /// Создание текста в капче с различными цветами
        /// </summary>
        private static void DrawText(Graphics g, string text, int width, int height)
        {
            int startX = 12;
            int cellWidth = (width - startX * 2) / text.Length;

            for (int i = 0; i < text.Length; i++)
            {
                string fontName = FontNames[random.Next(FontNames.Length)];
                float fontSize = random.Next(22, 30);

                FontStyle style;

                if (random.Next(2) == 0)
                {
                    style = FontStyle.Bold;
                }
                else
                {
                    style = FontStyle.Regular;
                }

                Color symbolColor = Colors[random.Next(Colors.Length)];

                using (Font font = new Font(fontName, fontSize, style))
                using (SolidBrush brush = new SolidBrush(symbolColor))
                {
                    var state = g.Save();
                    float cx = startX + i * cellWidth + cellWidth / 2f;
                    float cy = height / 2f + random.Next(-6, 7);

                    g.TranslateTransform(cx, cy);
                    g.RotateTransform(random.Next(-15, 16));

                    SizeF sz = g.MeasureString(text[i].ToString(), font);
                    g.DrawString(text[i].ToString(), font, brush, -sz.Width / 2f, -sz.Height / 2f);

                    g.Restore(state);
                }
            }
        }
    }
}