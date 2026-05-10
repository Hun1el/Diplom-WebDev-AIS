using MySql.Data.MySqlClient;
using System;
using System.Windows.Forms;
using WebSiteDev.Service;

namespace WebSiteDev
{
    public partial class ServiceForm : ScalableForm
    {
        public ServiceForm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Кнопка "Восстановить базу данных"
        /// </summary>
        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Title = "Выберите файл восстановления базы данных";
                openFileDialog.Filter = "SQL-файл (*.sql)|*.sql";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string FilePath = openFileDialog.FileName;

                    var result = MessageBox.Show("Вы уверены, что хотите восстановить базу данных из файла:\n" + FilePath + "\n\nДействие не может быть отменено.", "Внимание", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);

                    if (result == DialogResult.Yes)
                    {
                        Service.Service.RestoreBackup(FilePath);

                        MessageBox.Show("База данных успешно восстановлена!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось восстановить базу данных!\nОшибка: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Кнопка "Импорт данных"
        /// </summary>
        private void button2_Click(object sender, EventArgs e)
        {
            var result = Service.Service.CanOpenForm();

            if (result.ErrorCode == null)
            {
                ImportForm importForm = new ImportForm();
                this.Hide();
                importForm.ShowDialog();
                this.Show();
            }
            else
            {
                MessageBox.Show("Не удалось открыть форму импорта!\nКод ошибки: " + result.ErrorCode + "\n" + result.ErrorMessage, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Кнопка "Создать резеврную копию"
        /// </summary>
        private void button3_Click(object sender, EventArgs e)
        {
            var result = Service.Service.CanOpenForm();

            if (result.ErrorCode == null)
            {
                try
                {
                    string Path = Service.Service.MakeBackup();

                    MessageBox.Show($"Резервная копия создана по пути: {Path}", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Не удалось создать резервную копию\nОшибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("Не удалось создать резервную копию!\nКод ошибки: " + result.ErrorCode + "\n" + result.ErrorMessage, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Кнопка "Экспорт данных"
        /// </summary>
        private void button4_Click(object sender, EventArgs e)
        {
            var result = Service.Service.CanOpenForm();

            if (result.ErrorCode == null)
            {
                ExportForm exportForm = new ExportForm();
                this.Hide();
                exportForm.ShowDialog();
                this.Show();
            }
            else
            {
                MessageBox.Show("Не удалось открыть форму импорта!\nКод ошибки: " + result.ErrorCode + "\n" + result.ErrorMessage, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Кнопка "Выйти из учетной записи"
        /// </summary>
        private void button5_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show("Вы действительно хотите выйти из учетной записи?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                this.Close();
            }
        }
    }
}