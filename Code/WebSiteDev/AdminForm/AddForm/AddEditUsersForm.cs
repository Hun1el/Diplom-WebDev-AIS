using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace WebSiteDev.AddForm
{
    /// <summary>
    /// Форма для добавления / редактирования пользователя
    /// </summary>
    public partial class AddEditUsersForm : ScalableForm
    {
        public enum FormMode
        {
            Add = 0,
            Edit = 1
        }

        protected override float MaxScale => 1.6f;
        protected override float MinScale => 0.9f;
        private FormMode mode;
        private int editUserID = -1;
        private string editOldLogin = null;
        private string editOldPhone = null;

        private DataManipulation dataManipulation;
        static readonly Random rand = new Random();

        /// <summary>
        /// Конструктор для добавления нового пользователя
        /// </summary>
        public AddEditUsersForm(DataManipulation dm)
        {
            InitializeComponent();

            dataManipulation = dm;
            dataManipulation.FillComboBoxWithRoles(comboBox1, "Выберите роль");

            mode = FormMode.Add;
            button2.Text = "Добавить";
            this.Text = "Добавление пользователя";
        }

        /// <summary>
        /// Конструктор для редактирования существующего пользователя
        /// </summary>
        public AddEditUsersForm(DataManipulation dm, int userID, string surname, string firstName, string middleName,
            string login, string phone, int roleID)
        {
            InitializeComponent();

            dataManipulation = dm;
            dataManipulation.FillComboBoxWithRoles(comboBox1, "Выберите роль");

            mode = FormMode.Edit;
            editUserID = userID;
            editOldLogin = login;
            editOldPhone = phone;

            // Заполняем поля данными пользователя
            textBox2.Text = surname;
            textBox3.Text = firstName;
            textBox4.Text = middleName;
            textBox5.Text = login;
            maskedTextBox1.Text = phone;

            // Выбираем роль в комбобоксе
            for (int i = 0; i < comboBox1.Items.Count; i++)
            {
                DataRowView row = comboBox1.Items[i] as DataRowView;

                if (row != null && Convert.ToInt32(row["RoleID"]) == roleID)
                {
                    comboBox1.SelectedIndex = i;
                    break;
                }
            }

            button2.Text = "Редактировать";
            this.Text = "Измение пользователя";
        }

        private void AddUsersForm_Load(object sender, EventArgs e)
        {
            LabelColor.ApplyRedStar(this);
            Inactivity.OnFormLoad(this);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Применяет форматирование первой буквы при вводе фамилии
        /// </summary>
        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            InputRest.FirstLetter(textBox2);
        }

        /// <summary>
        /// Применяет форматирование первой буквы при вводе имени
        /// </summary>
        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            InputRest.FirstLetter(textBox3);
        }

        /// <summary>
        /// Применяет форматирование первой буквы при вводе отчества
        /// </summary>
        private void textBox4_TextChanged(object sender, EventArgs e)
        {
            InputRest.FirstLetter(textBox4);
        }

        /// <summary>
        /// Ограничивает ввод только русскими буквами и дефисом в поле фамилии
        /// </summary>
        private void textBox2_KeyPress(object sender, KeyPressEventArgs e)
        {
            InputRest.OnlyRussianAndDash(e, textBox2);
        }

        /// <summary>
        /// Ограничивает ввод только русскими буквами и дефисом в поле имени
        /// </summary>
        private void textBox3_KeyPress(object sender, KeyPressEventArgs e)
        {
            InputRest.OnlyRussianAndDash(e, textBox3);
        }

        /// <summary>
        /// Ограничивает ввод только русскими буквами в поле отчества
        /// </summary>
        private void textBox4_KeyPress(object sender, KeyPressEventArgs e)
        {
            InputRest.OnlyRussian(e);
        }

        /// <summary>
        /// Ограничивает ввод в поле логина только допустимыми символами для логина
        /// </summary>
        private void textBox5_KeyPress(object sender, KeyPressEventArgs e)
        {
            InputRest.LoginInput(e);
        }

        /// <summary>
        /// Ограничивает ввод в поле пароля только английскими буквами, цифрами и спецсимволами
        /// </summary>
        private void textBox6_KeyPress(object sender, KeyPressEventArgs e)
        {
            InputRest.EnglishDigitsAndSpecial(e);
        }

        /// <summary>
        /// Обработчик кнопки добавления / сохранения пользователя
        /// </summary>
        private void button2_Click(object sender, EventArgs e)
        {
            // Получаем все введённые данные
            string SurName = textBox2.Text.Trim();
            string FirstName = textBox3.Text.Trim();
            string MiddleName = textBox4.Text.Trim();
            string UserLogin = textBox5.Text.Trim();
            string UserPassword = textBox6.Text.Trim();
            string PhoneNumber = maskedTextBox1.Text.Trim();

            string AdminLogin = Properties.Settings.Default.AdminLogin;

            // Проверяем что логин не совпадает с логином встроенного пользователя
            if (!string.IsNullOrWhiteSpace(AdminLogin) && string.Equals(UserLogin, AdminLogin, StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show($"Логин \"{AdminLogin}\" зарезервирован системой!\nВыберите другой логин.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrEmpty(SurName) || string.IsNullOrEmpty(FirstName) || string.IsNullOrEmpty(UserLogin) || comboBox1.SelectedIndex <= 0 ||
                string.IsNullOrEmpty(PhoneNumber))
            {
                MessageBox.Show("Необходимо заполнить поля отмеченные \"*\"", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (UserLogin.Length < 6)
            {
                MessageBox.Show("Логин должен содержать не менее 6 символов.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (UserPassword.Length < 6)
            {
                MessageBox.Show("Пароль должен содержать не менее 6 символов.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!maskedTextBox1.MaskFull)
            {
                MessageBox.Show("Введите корректный номер телефона!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string RoleId = comboBox1.SelectedValue.ToString();

            // Хешируем пароль (только если он был введён)
            string hashedPassword = null;
            if (!string.IsNullOrEmpty(UserPassword))
            {
                hashedPassword = GetSha256(UserPassword);
            }

            try
            {
                using (MySqlConnection con = new MySqlConnection(Data.GetConnectionString()))
                {
                    con.Open();

                    if (mode == FormMode.Add)
                    {
                        // Проверяем что пользователь с таким логином не существует
                        string СheckQuery = "SELECT COUNT(*) FROM `Users` WHERE UserLogin = @UserLogin";

                        using (MySqlCommand cmd1 = new MySqlCommand(СheckQuery, con))
                        {
                            cmd1.Parameters.AddWithValue("@UserLogin", UserLogin);

                            int count = Convert.ToInt32(cmd1.ExecuteScalar());

                            if (count > 0)
                            {
                                MessageBox.Show("Пользователь с таким логином уже существует!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                return;
                            }
                        }

                        // Проверяем что пользователь с таким номером телефона не существует
                        string СheckPhoneQuery = "SELECT COUNT(*) FROM `Users` WHERE PhoneNumber = @PhoneNumber";

                        using (MySqlCommand cmd2 = new MySqlCommand(СheckPhoneQuery, con))
                        {
                            cmd2.Parameters.AddWithValue("@PhoneNumber", PhoneNumber);

                            int phoneCount = Convert.ToInt32(cmd2.ExecuteScalar());

                            if (phoneCount > 0)
                            {
                                MessageBox.Show("Пользователь с таким номером телефона уже существует!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                return;
                            }
                        }

                        // Добавляем нового пользователя в БД
                        string insertQuery = @"INSERT INTO Users (Surname, FirstName, MiddleName, UserLogin, UserPassword, RoleID, PhoneNumber) 
                                               VALUES (@Surname, @FirstName, @MiddleName, @UserLogin, @UserPassword, @RoleID, @PhoneNumber)";

                        using (MySqlCommand cmd3 = new MySqlCommand(insertQuery, con))
                        {
                            cmd3.Parameters.AddWithValue("@Surname", SurName);
                            cmd3.Parameters.AddWithValue("@FirstName", FirstName);
                            cmd3.Parameters.AddWithValue("@MiddleName", MiddleName);
                            cmd3.Parameters.AddWithValue("@UserLogin", UserLogin);
                            cmd3.Parameters.AddWithValue("@UserPassword", hashedPassword);
                            cmd3.Parameters.AddWithValue("@RoleID", RoleId);
                            cmd3.Parameters.AddWithValue("@PhoneNumber", PhoneNumber);

                            cmd3.ExecuteNonQuery();
                        }

                        MessageBox.Show("Пользователь успешно добавлен!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        if (!string.Equals(UserLogin, editOldLogin, StringComparison.OrdinalIgnoreCase))
                        {
                            string СheckQuery = "SELECT COUNT(*) FROM `Users` WHERE UserLogin = @UserLogin AND UserID != @UserID";

                            using (MySqlCommand cmd1 = new MySqlCommand(СheckQuery, con))
                            {
                                cmd1.Parameters.AddWithValue("@UserLogin", UserLogin);
                                cmd1.Parameters.AddWithValue("@UserID", editUserID);

                                int count = Convert.ToInt32(cmd1.ExecuteScalar());

                                if (count > 0)
                                {
                                    MessageBox.Show("Пользователь с таким логином уже существует!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                    return;
                                }
                            }
                        }

                        // Проверяем что телефон не занят другим пользователем
                        if (!string.Equals(PhoneNumber, editOldPhone, StringComparison.OrdinalIgnoreCase))
                        {
                            string СheckPhoneQuery = "SELECT COUNT(*) FROM `Users` WHERE PhoneNumber = @PhoneNumber AND UserID != @UserID";

                            using (MySqlCommand cmd2 = new MySqlCommand(СheckPhoneQuery, con))
                            {
                                cmd2.Parameters.AddWithValue("@PhoneNumber", PhoneNumber);
                                cmd2.Parameters.AddWithValue("@UserID", editUserID);

                                int phoneCount = Convert.ToInt32(cmd2.ExecuteScalar());

                                if (phoneCount > 0)
                                {
                                    MessageBox.Show("Пользователь с таким номером телефона уже существует!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                    return;
                                }
                            }
                        }

                        var result = MessageBox.Show("Вы действительно хотите изменить пользователя?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                        if (result == DialogResult.No)
                        {
                            return;
                        }

                        // Запрос на обновление
                        string UpdateQuery;

                        if (!string.IsNullOrEmpty(hashedPassword))
                        {
                            UpdateQuery = @"UPDATE Users SET Surname = @Surname, FirstName = @FirstName, MiddleName = @MiddleName, 
                                            UserLogin = @UserLogin, UserPassword = @UserPassword, RoleID = @RoleID, PhoneNumber = @PhoneNumber 
                                            WHERE UserID = @UserID";
                        }
                        else
                        {
                            UpdateQuery = @"UPDATE Users SET Surname = @Surname, FirstName = @FirstName, MiddleName = @MiddleName, 
                                            UserLogin = @UserLogin, RoleID = @RoleID, PhoneNumber = @PhoneNumber 
                                            WHERE UserID = @UserID";
                        }

                        using (MySqlCommand cmd3 = new MySqlCommand(UpdateQuery, con))
                        {
                            cmd3.Parameters.AddWithValue("@Surname", SurName);
                            cmd3.Parameters.AddWithValue("@FirstName", FirstName);
                            cmd3.Parameters.AddWithValue("@MiddleName", MiddleName);
                            cmd3.Parameters.AddWithValue("@UserLogin", UserLogin);
                            cmd3.Parameters.AddWithValue("@RoleID", RoleId);
                            cmd3.Parameters.AddWithValue("@PhoneNumber", PhoneNumber);
                            cmd3.Parameters.AddWithValue("@UserID", editUserID);

                            if (!string.IsNullOrEmpty(hashedPassword))
                            {
                                cmd3.Parameters.AddWithValue("@UserPassword", hashedPassword);
                            }

                            cmd3.ExecuteNonQuery();
                        }

                        MessageBox.Show("Пользователь успешно изменён!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        this.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при сохранении пользователя:\n" + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Хеширует пароль алгоритмом SHA256
        /// </summary>
        private string GetSha256(string text)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(text);
                byte[] hash = sha.ComputeHash(bytes);
                StringBuilder sb = new StringBuilder();
                foreach (byte b in hash)
                {
                    sb.Append(b.ToString("x2"));
                }

                return sb.ToString();
            }
        }

        /// <summary>
        /// Переключает видимость пароля при нажатии на иконку глаза
        /// </summary>
        private void pictureBox2_Click(object sender, EventArgs e)
        {
            if (textBox6.UseSystemPasswordChar)
            {
                textBox6.UseSystemPasswordChar = false;
                pictureBox2.BackgroundImage = Properties.Resources.EyeHide;
            }
            else
            {
                textBox6.UseSystemPasswordChar = true;
                pictureBox2.BackgroundImage = Properties.Resources.EyeView;
            }
        }

        /// <summary>
        /// Перемешивает символы строки в случайном порядке
        /// </summary>
        static string Shuffle(string str)
        {
            var chars = str.ToCharArray();
            for (int i = chars.Length - 1; i > 0; i--)
            {
                int j = rand.Next(i);
                (chars[i], chars[j]) = (chars[j], chars[i]);
            }

            return new string(chars);
        }

        /// <summary>
        /// Обработчик кнопки генерации случайного пароля
        /// </summary>
        private void button5_Click(object sender, EventArgs e)
        {
            const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lower = "abcdefghijklmnopqrstuvwxyz";
            const string numbers = "0123456789";
            const string special = "!@#$%^&*()_+-=[]{}|;:,.<>?";

            string allChars = upper + lower + numbers + special;
            Random random = new Random();
            StringBuilder password = new StringBuilder();

            password.Append(upper[random.Next(upper.Length)]);
            password.Append(upper[random.Next(upper.Length)]);
            password.Append(numbers[random.Next(numbers.Length)]);
            password.Append(upper[random.Next(upper.Length)]);

            for (int i = 4; i < 12; i++)
            {
                password.Append(allChars[random.Next(allChars.Length)]);
            }

            string shufflepass = Shuffle(Convert.ToString(password));
            textBox6.Text = shufflepass;
        }

        private void AddUsersForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Inactivity.OnFormClosing(this);
        }
    }
}