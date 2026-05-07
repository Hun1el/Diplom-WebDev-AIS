using MySql.Data.MySqlClient;
using System;
using System.Drawing;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using WebSiteDev.ManagerForm;

namespace WebSiteDev
{
    public partial class AuthForm : ScalableForm
    {
        private BlockForms blockForms;
        private string captchaText;
        private bool captchaRequired = false;
        private int failedAttempts = 0;
        private Timer lockoutTimer;
        private int lockoutSeconds = 0;

        public AuthForm()
        {
            InitializeComponent();
            FolderPermissions.InitializeImagesFolder();

            lockoutTimer = new Timer();
            lockoutTimer.Interval = 1000;
            lockoutTimer.Tick += LockoutTimer_Tick;
        }

        private void AuthForm_Load(object sender, EventArgs e)
        {
            label6.AutoSize = false;
            label6.Size = new Size(340, 25);
            label6.Location = new Point(325, 560);
            label6.TextAlign = ContentAlignment.TopCenter;

            AddRightCenterRule(pictureBox2, textBox2, 8);       // глазик справа от поля пароля
            AddTopRightFormRule(pictureBox3, 18, 18);           // шестеренка в правом верхнем углу
            AddBottomCenterRule(pictureBox5, pictureBox4, 6);   // обновление капчи под картинкой

            blockForms = Program.GetBlockForms();
            blockForms.RegisterForm(this);
            blockForms.Start();

            HideCaptcha();
            LabelColor.ApplyRedStar(this);
        }

        private void ShowCaptcha()
        {
            SetCaptchaVisibility(true);

            ChangeControlOriginalLocation(button1, new Point(348, 455));
            ChangeControlOriginalLocation(button2, new Point(348, 510));

            UpdateCaptcha();
        }

        private void HideCaptcha()
        {
            SetCaptchaVisibility(false);

            ChangeControlOriginalLocation(button1, new Point(348, 235));
            ChangeControlOriginalLocation(button2, new Point(348, 510));

            textBox3.Clear();
        }

        private void UpdateCaptcha()
        {
            if (pictureBox4.Width <= 0 || pictureBox4.Height <= 0)
            {
                return;
            }

            if (pictureBox4.Image != null)
            {
                pictureBox4.Image.Dispose();
            }

            pictureBox4.Image = CaptchaGenerator.GenerateImage(pictureBox4.Width, pictureBox4.Height, 6, out captchaText);
        }

        private void SetCaptchaVisibility(bool isVisible)
        {
            pictureBox4.Visible = isVisible;
            textBox3.Visible = isVisible;
            pictureBox5.Visible = isVisible;
            label3.Visible = isVisible;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (lockoutTimer.Enabled)
            {
                MessageBox.Show("Вход заблокирован. Попробуйте через " + lockoutSeconds + " сек.", "Блокировка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string login = textBox1.Text.Trim();
            string password = textBox2.Text;

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Необходимо заполнить поля отмеченные \"*\"", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (captchaRequired)
            {
                if (string.IsNullOrEmpty(textBox3.Text))
                {
                    MessageBox.Show("Пожалуйста, введите код с картинки (CAPTCHA).", "Требуется CAPTCHA", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    textBox3.Focus();
                    return;
                }

                if (textBox3.Text != captchaText)
                {
                    MessageBox.Show("Неверно введена CAPTCHA!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    failedAttempts++;

                    if (failedAttempts >= 2)
                    {
                        MessageBox.Show("Вход заблокирован на 10 секунд!", "Блокировка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        
                        StartLockout();
                    }
                    else
                    {
                        UpdateCaptcha();
                    }

                    textBox3.Clear();
                    
                    return;
                }
            }

            string adminLogin = Properties.Settings.Default.AdminLogin;
            string adminPassword = Properties.Settings.Default.AdminPassword;

            if (login == adminLogin && password == adminPassword)
            {
                SuccessfulLogin(new ServiceForm());
                return;
            }

            AuthenticateUser(login, password);
        }

        private void AuthenticateUser(string login, string password)
        {
            string hashedPassword = GetSha256(password);

            using (MySqlConnection con = new MySqlConnection(Data.GetConnectionString()))
            {
                try
                {
                    con.Open();

                    string query = @"SELECT u.UserID, u.FirstName, u.Surname, u.MiddleName, r.RoleName 
                                     FROM Users u JOIN Role r ON u.RoleID = r.RoleID 
                                     WHERE u.UserLogin = @login AND u.UserPassword = @password LIMIT 1;";

                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@login", login);
                        cmd.Parameters.AddWithValue("@password", hashedPassword);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int userID = Convert.ToInt32(reader["UserID"]);
                                string fullName = reader["Surname"].ToString() + " " + reader["FirstName"].ToString() + " " + reader["MiddleName"].ToString();
                                string role = reader["RoleName"].ToString();

                                Form userForm = GetFormByRole(role, fullName, userID);

                                if (userForm != null)
                                {
                                    SuccessfulLogin(userForm);
                                }
                            }
                            else
                            {
                                HandleFailedLoginAttempt();
                            }
                        }
                    }
                }
                catch (MySqlException ex)
                {
                    HandleDatabaseError(ex);
                    ClearInputs();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка:\n" + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    ClearInputs();
                }
            }
        }

        private Form GetFormByRole(string role, string fullName, int userID)
        {
            if (role == "Администратор")
            {
                return new MainForm(fullName, role, userID);
            }
            else if (role == "Менеджер")
            {
                return new ManagerMainForm(fullName, role, userID);
            }
            else if (role == "Директор")
            {
                return new DirectorMainForm(fullName, role);
            }

            return null;
        }

        private void SuccessfulLogin(Form targetForm)
        {
            failedAttempts = 0;
            captchaRequired = false;
            HideCaptcha();
            ClearInputs();

            blockForms.Stop();
            blockForms.RegisterForm(targetForm);

            this.Hide();
            targetForm.ShowDialog();
            this.Show();

            blockForms.UnregisterForm(targetForm);
            blockForms.Restart();
        }

        private void HandleFailedLoginAttempt()
        {
            failedAttempts++;

            if (failedAttempts == 1)
            {
                MessageBox.Show("Неверный логин или пароль!\nДля дальнейших попыток требуется ввод CAPTCHA.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                captchaRequired = true;
                
                ShowCaptcha();
            }
            else if (failedAttempts >= 2)
            {
                MessageBox.Show("Неверный логин или пароль!\nВход заблокирован на 10 секунд!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                StartLockout();
            }

            ClearInputs();
            textBox3.Clear();
        }

        private void ClearInputs()
        {
            textBox1.Clear();
            textBox2.Clear();
        }

        private void pictureBox5_Click(object sender, EventArgs e)
        {
            UpdateCaptcha();
            
            textBox3.Clear();
            textBox3.Focus();
        }

        private void StartLockout()
        {
            lockoutSeconds = 10;
            ToggleControls(false);
            lockoutTimer.Start();
        }

        private void LockoutTimer_Tick(object sender, EventArgs e)
        {
            lockoutSeconds--;
            button1.Text = "Вход (" + lockoutSeconds + " сек)";

            if (lockoutSeconds <= 0)
            {
                lockoutTimer.Stop();
                ToggleControls(true);
                button1.Text = "Авторизоваться";
                UpdateCaptcha();
                textBox3.Clear();
                textBox3.Focus();
            }
        }

        private void ToggleControls(bool isEnabled)
        {
            button1.Enabled = isEnabled;
            button2.Enabled = isEnabled;
            textBox1.Enabled = isEnabled;
            textBox2.Enabled = isEnabled;
            textBox3.Enabled = isEnabled;
            pictureBox5.Enabled = isEnabled;
            pictureBox2.Enabled = isEnabled;
            pictureBox3.Enabled = isEnabled;
        }

        private void HandleDatabaseError(MySqlException ex)
        {
            string ErrorMessage = "";
            
            if (ex.Number == 0)
            {
                ErrorMessage = "Не удаётся подключиться к серверу базы данных.";
            }
            else if (ex.Number == 1045)
            {
                ErrorMessage = "Ошибка доступа отклонена!";
            }
            else if (ex.Number == 1049)
            {
                ErrorMessage = "База данных не найдена!";
            }
            else if (ex.Number == 2003)
            {
                ErrorMessage = "Не удаётся подключиться к MySQL серверу.";
            }
            else if (ex.Number == 2006)
            {
                ErrorMessage = "MySQL сервер отключен.";
            }
            else
            {
                ErrorMessage = "Ошибка БД (код: " + ex.Number + "): " + ex.Message;
            }

            MessageBox.Show(ErrorMessage, "Ошибка подключения", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show("Вы действительно хотите выйти?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            
            if (result == DialogResult.Yes)
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            if (textBox2.UseSystemPasswordChar)
            {
                textBox2.UseSystemPasswordChar = false;
                pictureBox2.BackgroundImage = Properties.Resources.EyeHide;
            }
            else
            {
                textBox2.UseSystemPasswordChar = true;
                pictureBox2.BackgroundImage = Properties.Resources.EyeView;
            }
        }

        private void pictureBox3_Click(object sender, EventArgs e)
        {
            SettingsForm settingsForm = new SettingsForm();
            this.Visible = false;
            settingsForm.ShowDialog();
            this.Visible = true;
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            InputRest.EnglishDigitsAndSpecial(e); 
        }

        private void textBox2_KeyPress(object sender, KeyPressEventArgs e)
        {
            InputRest.EnglishDigitsAndSpecial(e);
        }

        private void textBox3_KeyPress(object sender, KeyPressEventArgs e)
        {
            string AllowedChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            
            if (AllowedChars.IndexOf(e.KeyChar) == -1 && !char.IsControl(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            textBox3.SelectionStart = textBox3.Text.Length;
        }

        private string GetSha256(string text)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(text));
                StringBuilder sb = new StringBuilder();

                foreach (byte b in hash)
                {
                    sb.Append(b.ToString("x2"));
                }

                return sb.ToString();
            }
        }

        private void AuthForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (blockForms != null)
            {
                blockForms.UnregisterForm(this);
                blockForms.Stop();
            }

            if (lockoutTimer != null)
            {
                lockoutTimer.Stop();
            }
        }
    }
}