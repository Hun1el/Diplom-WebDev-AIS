using MySql.Data.MySqlClient;
using System;
using System.Windows.Forms;

namespace WebSiteDev.AddForm
{
    public partial class AddEditClientsForm : ScalableForm
    {
        public enum FormMode
        {
            Add = 0,
            Edit = 1
        }

        protected override float MaxScale => 1.6f;
        protected override float MinScale => 0.9f;

        private FormMode mode;
        private int editClientID = -1;
        private string editOldEmail = null;
        private string editOldPhone = null;

        public AddEditClientsForm()
        {
            InitializeComponent();
            mode = FormMode.Add;
            button2.Text = "Добавить";
            this.Text = "Добавление клиента";
        }

        public AddEditClientsForm(int clientID, string surname, string firstName, string middleName,
            string phone, string email)
        {
            InitializeComponent();
            mode = FormMode.Edit;
            editClientID = clientID;
            editOldPhone = phone;
            editOldEmail = email;

            textBox2.Text = surname;
            textBox3.Text = firstName;
            textBox4.Text = middleName;
            maskedTextBox1.Text = phone;

            if (email.Contains("@"))
            {
                string[] parts = email.Split('@');
                textBox5.Text = parts[0];
                string domainWithAt = "@" + parts[1];

                int domainIndex = comboBox1.FindString(domainWithAt);
                if (domainIndex >= 0)
                {
                    comboBox1.SelectedIndex = domainIndex;
                }
                else
                {
                    comboBox1.Items.Add(domainWithAt);
                    comboBox1.SelectedItem = domainWithAt;
                }
            }
            else
            {
                textBox5.Text = email;
                comboBox1.SelectedIndex = 0;
            }

            button2.Text = "Редактировать";
            this.Text = "Изменение клиента";
        }

        private void AddClientsForm_Load(object sender, EventArgs e)
        {
            Inactivity.OnFormLoad(this);

            if (mode == FormMode.Add)
            {
                comboBox1.SelectedIndex = 0;
            }

            LabelColor.ApplyRedStar(this);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            InputRest.FirstLetter(textBox2);
        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            InputRest.FirstLetter(textBox3);
        }

        private void textBox4_TextChanged(object sender, EventArgs e)
        {
            InputRest.FirstLetter(textBox4);
        }

        private void textBox2_KeyPress(object sender, KeyPressEventArgs e)
        {
            InputRest.OnlyRussianAndDash(e, textBox2);
        }

        private void textBox3_KeyPress(object sender, KeyPressEventArgs e)
        {
            InputRest.OnlyRussianAndDash(e, textBox3);
        }

        private void textBox4_KeyPress(object sender, KeyPressEventArgs e)
        {
            InputRest.OnlyRussian(e);
        }

        private void textBox5_KeyPress(object sender, KeyPressEventArgs e)
        {
            InputRest.EmailInput(e);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string surName = textBox2.Text.Trim();
            string firstName = textBox3.Text.Trim();
            string middleName = textBox4.Text.Trim();
            string emailName = textBox5.Text.Trim();
            string phoneNumber = maskedTextBox1.Text.Trim();

            if (string.IsNullOrEmpty(surName) || string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(phoneNumber) || string.IsNullOrEmpty(emailName) || comboBox1.SelectedIndex <= 0)
            {
                MessageBox.Show("Необходимо заполнить поля отмеченные \"*\"", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!maskedTextBox1.MaskFull)
            {
                MessageBox.Show("Введите корректный номер телефона!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string domain = comboBox1.SelectedItem.ToString();
            string fullEmail;

            if (emailName.Contains("@"))
            {
                fullEmail = emailName;
            }
            else
            {
                fullEmail = emailName + domain;
            }

            try
            {
                using (MySqlConnection con = new MySqlConnection(Data.GetConnectionString()))
                {
                    con.Open();

                    if (mode == FormMode.Add)
                    {
                        string checkEmailQuery = "SELECT COUNT(*) FROM Clients WHERE Email = @Email";
                        using (MySqlCommand cmd = new MySqlCommand(checkEmailQuery, con))
                        {
                            cmd.Parameters.AddWithValue("@Email", fullEmail);
                            int count = Convert.ToInt32(cmd.ExecuteScalar());

                            if (count > 0)
                            {
                                MessageBox.Show("Клиент с таким email уже существует!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                return;
                            }
                        }

                        string checkPhoneQuery = "SELECT COUNT(*) FROM Clients WHERE PhoneNumber = @PhoneNumber";
                        using (MySqlCommand cmd = new MySqlCommand(checkPhoneQuery, con))
                        {
                            cmd.Parameters.AddWithValue("@PhoneNumber", phoneNumber);
                            int count = Convert.ToInt32(cmd.ExecuteScalar());

                            if (count > 0)
                            {
                                MessageBox.Show("Клиент с таким номером телефона уже существует!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                return;
                            }
                        }

                        string insertQuery = @"INSERT INTO Clients (Surname, FirstName, MiddleName, Email, PhoneNumber) 
                                               VALUES (@Surname, @FirstName, @MiddleName, @Email, @PhoneNumber)";
                        using (MySqlCommand cmd = new MySqlCommand(insertQuery, con))
                        {
                            cmd.Parameters.AddWithValue("@Surname", surName);
                            cmd.Parameters.AddWithValue("@FirstName", firstName);
                            cmd.Parameters.AddWithValue("@MiddleName", middleName);
                            cmd.Parameters.AddWithValue("@Email", fullEmail);
                            cmd.Parameters.AddWithValue("@PhoneNumber", phoneNumber);
                            cmd.ExecuteNonQuery();
                        }

                        MessageBox.Show("Клиент успешно добавлен!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        textBox2.Clear();
                        textBox3.Clear();
                        textBox4.Clear();
                        textBox5.Clear();
                        maskedTextBox1.Clear();
                        comboBox1.SelectedIndex = 0;
                        textBox2.Focus();
                    }
                    else
                    {
                        if (!string.Equals(fullEmail, editOldEmail, StringComparison.OrdinalIgnoreCase))
                        {
                            string checkEmailQuery = "SELECT COUNT(*) FROM Clients WHERE Email = @Email AND ClientID != @ClientID";
                            
                            using (MySqlCommand cmd = new MySqlCommand(checkEmailQuery, con))
                            {
                                cmd.Parameters.AddWithValue("@Email", fullEmail);
                                cmd.Parameters.AddWithValue("@ClientID", editClientID);
                                int count = Convert.ToInt32(cmd.ExecuteScalar());

                                if (count > 0)
                                {
                                    MessageBox.Show("Клиент с таким email уже существует!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                    return;
                                }
                            }
                        }

                        if (!string.Equals(phoneNumber, editOldPhone, StringComparison.OrdinalIgnoreCase))
                        {
                            string checkPhoneQuery = "SELECT COUNT(*) FROM Clients WHERE PhoneNumber = @PhoneNumber AND ClientID != @ClientID";
                            using (MySqlCommand cmd = new MySqlCommand(checkPhoneQuery, con))
                            {
                                cmd.Parameters.AddWithValue("@PhoneNumber", phoneNumber);
                                cmd.Parameters.AddWithValue("@ClientID", editClientID);
                                int count = Convert.ToInt32(cmd.ExecuteScalar());

                                if (count > 0)
                                {
                                    MessageBox.Show("Клиент с таким номером телефона уже существует!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                    return;
                                }
                            }
                        }

                        var result = MessageBox.Show("Вы действительно хотите изменить клиента?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                        if (result == DialogResult.No)
                        {
                            return;
                        }

                        string updateQuery = @"UPDATE Clients SET Surname = @Surname, FirstName = @FirstName, MiddleName = @MiddleName, 
                                               Email = @Email, PhoneNumber = @PhoneNumber WHERE ClientID = @ClientID";

                        using (MySqlCommand cmd = new MySqlCommand(updateQuery, con))
                        {
                            cmd.Parameters.AddWithValue("@Surname", surName);
                            cmd.Parameters.AddWithValue("@FirstName", firstName);
                            cmd.Parameters.AddWithValue("@MiddleName", middleName);
                            cmd.Parameters.AddWithValue("@Email", fullEmail);
                            cmd.Parameters.AddWithValue("@PhoneNumber", phoneNumber);
                            cmd.Parameters.AddWithValue("@ClientID", editClientID);
                            cmd.ExecuteNonQuery();
                        }

                        MessageBox.Show("Клиент успешно изменён!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        this.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при сохранении клиента:\n" + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AddClientsForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Inactivity.OnFormClosing(this);
        }
    }
}