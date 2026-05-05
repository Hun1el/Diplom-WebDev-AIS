using System;
using System.Drawing;
using System.Windows.Forms;

namespace WebSiteDev
{
    public static class LabelColor
    {
        /// <summary>
        /// Применяет красную звездочку ко всем Label внутри переданного контейнера
        /// </summary>
        public static void ApplyRedStar(Control parentContainer)
        {
            foreach (Control control in parentContainer.Controls)
            {
                // Если элемент является label
                if (control is Label)
                {
                    Label label = (Label)control;

                    // Если в тексте есть звездочка и мы ее еще не обрабатывали
                    if (label.Text.Contains("*") && label.Tag == null)
                    {
                        // Сохраняем оригинальный цвет в свойство Tag
                        label.Tag = label.ForeColor;

                        // Делаем стандартный текст прозрачным
                        label.ForeColor = Color.Transparent;

                        // Вешаем собственное событие отрисовки
                        label.Paint += Label_Paint;
                    }
                }

                // Если внутри элемента есть другие элементы ищем и в них
                if (control.Controls.Count > 0)
                {
                    ApplyRedStar(control);
                }
            }
        }

        // Собственный метод отрисовки текста в Label
        private static void Label_Paint(object sender, PaintEventArgs e)
        {
            Label label = (Label)sender;
            string text = label.Text;

            // Восстанавливаем оригинальный цвет текста из Tag
            Color OriginalColor = Color.Black;

            if (label.Tag is Color)
            {
                OriginalColor = (Color)label.Tag;
            }

            int StarIndex = text.IndexOf("*");

            if (StarIndex == -1)
            {
                return;
            }

            // Разбиваем текст на 3 части до звездочки, сама звездочка, и после звездочки
            string part1 = text.Substring(0, StarIndex);
            string part2 = "*";
            string part3 = text.Substring(StarIndex + 1);

            float CurrentX = 0; // Координата X для отрисовки
            float CurrentY = 0; // Координата Y для отрисовки

            using (SolidBrush mainBrush = new SolidBrush(OriginalColor))
            using (SolidBrush starBrush = new SolidBrush(Color.Red))
            using (StringFormat format = new StringFormat(StringFormat.GenericTypographic))
            {
                // Настройка формата чтобы убрать лишние отступы при измерении текста
                format.FormatFlags = format.FormatFlags | StringFormatFlags.MeasureTrailingSpaces;

                // Рисуем часть ДО звездочки
                if (part1.Length > 0)
                {
                    e.Graphics.DrawString(part1, label.Font, mainBrush, CurrentX, CurrentY, format);
                    SizeF size1 = e.Graphics.MeasureString(part1, label.Font, 10000, format);
                    CurrentX = CurrentX + size1.Width;
                }

                // Рисуем красную звездочку
                e.Graphics.DrawString(part2, label.Font, starBrush, CurrentX, CurrentY, format);
                SizeF size2 = e.Graphics.MeasureString(part2, label.Font, 10000, format);
                CurrentX = CurrentX + size2.Width;

                // Рисуем часть ПОСЛЕ звездочки
                if (part3.Length > 0)
                {
                    e.Graphics.DrawString(part3, label.Font, mainBrush, CurrentX, CurrentY, format);
                }
            }
        }
    }
}