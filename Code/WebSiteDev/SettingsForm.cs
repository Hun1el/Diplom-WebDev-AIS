using MySql.Data.MySqlClient;
using System;
using System.Windows.Forms;

namespace WebSiteDev
{
    // Форма настроек подключения к базе данных и времени неактивности
    public partial class SettingsForm : Form
    {
        public SettingsForm()
        {
            InitializeComponent();
            LoadSettings();
        }

        // Загружает настройки подключения и время неактивности из application settings
        private void LoadSettings()
        {
            textBox1.Text = Properties.Settings.Default.DbHost;
            textBox2.Text = Properties.Settings.Default.DbUser;
            textBox3.Text = Properties.Settings.Default.DbPassword;
            textBox4.Text = Properties.Settings.Default.DbName;
            textBox5.Text = Properties.Settings.Default.InactivityTime.ToString();
        }

        /// <summary>
        /// Кнопка "К авторизации" закрывает форму без сохранения
        /// </summary>
        private void button3_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show("Вы действительно хотите выйти? Несохранённые изменения не будут применены.", "Выход из настроек", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }

        /// <summary>
        /// Кнопка "Сохранить изменения" сохраняет все параметры подключения и время неактивности
        /// </summary>
        private void button1_Click(object sender, EventArgs e)
        {
            // Проверяем что все обязательные поля заполнены
            if (string.IsNullOrWhiteSpace(textBox1.Text) || string.IsNullOrWhiteSpace(textBox2.Text) || string.IsNullOrWhiteSpace(textBox4.Text))
            {
                MessageBox.Show("Заполните все поля!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Время неактивности (если пусто - 30 сек, если > 1800 - 1800 сек)
            int InactivityTimeout = ValidateInactivityTimeout(textBox5.Text);

            // Сохраняем все настройки в application settings
            Properties.Settings.Default.DbHost = textBox1.Text;
            Properties.Settings.Default.DbUser = textBox2.Text;
            Properties.Settings.Default.DbPassword = textBox3.Text;
            Properties.Settings.Default.DbName = textBox4.Text;
            Properties.Settings.Default.InactivityTime = InactivityTimeout;
            Properties.Settings.Default.Save();

            // Обновляем время неактивности в работающей системе блокировки
            BlockForms blockForms = Program.GetBlockForms();

            if (blockForms != null)
            {
                blockForms.UpdateTimeout(InactivityTimeout);
            }

            MessageBox.Show($"Все настройки сохранены!\nВремя неактивности: {InactivityTimeout} сек", "Сохранение настроек", MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.Close();
        }

        /// <summary>
        /// Переключает видимость пароля при нажатии на иконку глаза
        /// </summary>
        private void pictureBox1_Click(object sender, EventArgs e)
        {
            // Если пароль скрыт - показываем его и меняем иконку
            if (textBox3.UseSystemPasswordChar)
            {
                textBox3.UseSystemPasswordChar = false;
                pictureBox1.BackgroundImage = Properties.Resources.EyeHide;
            }
            else
            {
                // Иначе скрываем пароль обратно
                textBox3.UseSystemPasswordChar = true;
                pictureBox1.BackgroundImage = Properties.Resources.EyeView;
            }
        }

        /// <summary>
        /// Кнопка "Тест соединения" проверяет подключение к БД с введёнными параметрами
        /// </summary>
        private void button2_Click(object sender, EventArgs e)
        {
            string host = textBox1.Text;
            string user = textBox2.Text;
            string password = textBox3.Text;
            string dbname = textBox4.Text;

            // Проверяем что обязательные поля заполнены
            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(dbname))
            {
                MessageBox.Show("Заполните обязательные поля!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Строка подключения
            string connStr = $"host={host};database={dbname};uid={user};pwd={password};";

            using (MySqlConnection con = new MySqlConnection(connStr))
            {
                try
                {
                    con.Open();

                    MessageBox.Show("Подключение успешно!", "Проверка подключения", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (MySqlException MySqlEx)
                {
                    HandleDatabaseError(MySqlEx);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Неожиданная ошибка:\n" + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        /// <summary>
        /// Обрабатывает ошибки БД
        /// </summary>
        private void HandleDatabaseError(MySqlException ex)
        {
            string ErrorMessage = "";

            // Определяем тип ошибки по коду и выводим подходящее сообщение
            switch (ex.Number)
            {
                case 0:
                    ErrorMessage = "Не удаётся подключиться к серверу базы данных.\n\nПроверьте:\nАдрес хоста (может быть localhost или ip)\nДоступность сервера";
                    break;

                case 1045:
                    ErrorMessage = "Ошибка доступа отклонена!\n\nПроверьте:\nИмя пользователя\nПароль";
                    break;

                case 1049:
                    ErrorMessage = "База данных не найдена!\n\nПроверьте имя базы данных.";
                    break;

                case 2003:
                    ErrorMessage = "Не удаётся подключиться к MySQL серверу.\n\nПроверьте:\nip адрес хоста\n• Работает ли сервер MySQL";
                    break;

                case 2006:
                    ErrorMessage = "MySQL сервер отключен или потеряна связь.\n\nПожалуйста, проверьте состояние сервера.";
                    break;

                default:
                    ErrorMessage = string.Format("Ошибка базы данных (код: {0}):\n{1}", ex.Number, ex.Message);
                    break;
            }

            MessageBox.Show(ErrorMessage, "Ошибка подключения", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        /// <summary>
        /// Если пусто устанавливает 30 сек по умолчанию
        /// Если < 1 или > 1800 показывает предупреждение и выставляет граничное значение
        /// </summary>
        private int ValidateInactivityTimeout(string input)
        {
            int Timeout = 30;

            if (string.IsNullOrWhiteSpace(input))
            {
                return Timeout;
            }

            // Пытаемся преобразовать строку в число
            if (int.TryParse(input, out int ParsedValue))
            {
                // Проверяем минимальное значение
                if (ParsedValue < 1)
                {
                    MessageBox.Show("Время неактивности должно быть минимум 30 секунд.\nУстановлено 30 секунд по умолчанию.", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    textBox5.Text = "30";
                    return 30;
                }

                // Проверяем максимальное значение (1800 сек = 30 минут)
                if (ParsedValue > 1800)
                {
                    MessageBox.Show("Время неактивности не может быть больше 1800 секунд (30 минут).\nУстановлено 1800 секунд.", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    textBox5.Text = "1800";
                    return 1800;
                }

                // Значение корректное
                Timeout = ParsedValue;
            }
            else
            {
                // Пользователь ввёл не число
                MessageBox.Show("Введите корректное число!\nУстановлено 30 секунд по умолчанию.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBox5.Text = "30";
                return 30;
            }

            return Timeout;
        }

        /// <summary>
        /// Загрузка формы
        /// </summary>
        private void SettingsForm_Load(object sender, EventArgs e)
        {
            Inactivity.OnFormLoad(this);
            LabelColor.ApplyRedStar(this);
        }

        /// <summary>
        /// Закрытие формы
        /// </summary>
        private void SettingsForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Inactivity.OnFormClosing(this);
        }

        /// <summary>
        /// Обработчик нажатия кнопок в поле ввода
        /// </summary>
        private void textBox5_KeyPress(object sender, KeyPressEventArgs e)
        {
            InputRest.OnlyNumbers(e);
        }
    }
}