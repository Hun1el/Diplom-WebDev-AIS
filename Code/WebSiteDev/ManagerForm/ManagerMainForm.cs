using MySqlX.XDevAPI.Common;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace WebSiteDev.ManagerForm
{
    /// <summary>
    /// Главная форма менеджера
    /// </summary>
    public partial class ManagerMainForm : ScalableForm
    {
        private string fullName;
        private string roleName;
        private string fullNameForm;
        private int CurrentUserID;
        private Button currentSelectedButton = null;
        private UserControl currentControl = null;

        /// <summary>
        /// Свойство для доступа к кнопке услуг из других форм
        /// </summary>
        public Button Button2
        {
            get { return button2; }
        }

        public ManagerMainForm(string fullName, string roleName, int userID, string fullNameForm)
        {
            InitializeComponent();
            this.fullName = fullName;
            this.roleName = roleName;
            this.fullNameForm = fullNameForm;
            this.CurrentUserID = userID;
        }

        private void ManagerMainForm_Load(object sender, EventArgs e)
        {
            Inactivity.OnFormLoad(this);

            this.Text = $"Главное меню ({roleName}: {fullNameForm})";
            label2.Text = $"Сотрудник: {fullName}";
            label3.Text = $"Доступ: {roleName}";

            this.ActiveControl = null;
        }

        public void LoadControl(UserControl control)
        {
            pictureBox2.Visible = false;
            label1.Visible = false;
            label2.Visible = false;
            label3.Visible = false;
            label4.Visible = false;

            // Удаляем старый контрол если он есть
            if (currentControl != null)
            {
                panel2.Controls.Remove(currentControl);
                currentControl.Dispose();
                currentControl = null;
            }

            // Добавляем новый контрол
            control.Dock = DockStyle.Fill;
            panel2.Controls.Add(control);

            currentControl = control;
        }

        /// <summary>
        /// Метод для выбора кнопки из других форм
        /// </summary>
        public void SelectButtonPublic(Button button)
        {
            SelectButton(button);
        }

        /// <summary>
        /// Кнопка "Клиенты" загружает контрол для управления клиентами
        /// </summary>
        private void button1_Click(object sender, EventArgs e)
        {
            // Если уже открыт этот контрол ничего не делаем
            if (currentSelectedButton == button1)
            {
                return;
            }

            LoadControl(new ClientsControl());
            this.Text = $"Список клиентов ({roleName}: {fullNameForm})";
            SelectButton(button1);
        }

        /// <summary>
        /// Кнопка "Услуги" загружает контрол для просмотра и управления услугами
        /// </summary>
        private void button2_Click(object sender, EventArgs e)
        {
            // Если уже открыт этот контрол ничего не делаем
            if (currentSelectedButton == button2)
            {
                return;
            }

            LoadControl(new ProductControl(roleName, CurrentUserID, fullName));
            this.Text = $"Список услуг ({roleName}: {fullNameForm})";
            SelectButton(button2);
        }

        /// <summary>
        /// Кнопка "Заказы" загружает контрол для управления заказами
        /// </summary>
        private void button3_Click(object sender, EventArgs e)
        {
            // Если уже открыт этот контрол ничего не делаем
            if (currentSelectedButton == button3)
            {
                return;
            }

            LoadControl(new OrderControl(roleName, CurrentUserID, fullName));
            this.Text = $"Список заказов ({roleName}: {fullNameForm})";
            SelectButton(button3);
        }

        /// <summary>
        /// Кнопка "Смены пользователя" закрывает форму и возвращает на форму входа
        /// </summary>
        private void button5_Click(object sender, EventArgs e)
        {
            SelectButton(button5);

            // Запрашиваем подтверждение
            var result = MessageBox.Show("Вы действительно хотите сменить учетную запись?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                // Очищаем открытый контрол
                if (currentControl != null)
                {
                    FormControl.ClearPanelControls(panel2);
                    currentControl = null;
                }

                this.Close();
            }
            else
            {
                // Если отказали выбираем предыдущую кнопку
                SelectButton(currentSelectedButton);
                this.ActiveControl = null;
            }
        }

        /// <summary>
        /// Кнопка "Выход" закрывает приложение полностью
        /// </summary>
        private void button6_Click(object sender, EventArgs e)
        {
            SelectButton(button6);

            var result1 = MessageBox.Show("Вы действительно хотите выйти из приложения?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            var result2 = Service.Service.CanOpenForm();

            if (result1 == DialogResult.Yes)
            {
                if (result2.ErrorCode == null)
                {
                    Service.Service.MakeBackup();
                    Application.Exit();
                }
                else
                {
                    SelectButton(currentSelectedButton);
                    this.ActiveControl = null;
                }
            }
        }

        /// <summary>
        /// Выбирает кнопку и изменяет её оформление отмечает активную кнопку
        /// </summary>
        private void SelectButton(Button selectedButton)
        {
            Button[] buttons = { button1, button2, button3, button5, button6 };

            foreach (Button btn in buttons)
            {
                if (btn == selectedButton)
                {
                    // Окрашиваем выбранную кнопку в голубой цвет
                    btn.Font = new Font("Segoe UI Semibold", btn.Font.Size);
                    btn.BackColor = Color.FromArgb(45, 156, 219);

                    // Кнопка выхода красная
                    if (btn == button6)
                    {
                        btn.ForeColor = Color.White;
                        btn.BackColor = Color.Crimson;
                    }
                    else
                    {
                        // Остальные кнопки белый текст на голубом фоне
                        btn.ForeColor = Color.White;
                    }
                }
                else
                {
                    // Невыбранные кнопки стандартное оформление
                    btn.Font = new Font("Segoe UI", btn.Font.Size);
                    btn.BackColor = Color.White;

                    // Кнопка выхода красный текст
                    if (btn == button6)
                    {
                        btn.ForeColor = Color.Red;
                    }
                    else
                    {
                        btn.ForeColor = Color.Black;
                    }
                }
            }

            // Сохраняем текущую выбранную кнопку
            if (selectedButton != button5 && selectedButton != button6)
            {
                currentSelectedButton = selectedButton;
            }
        }

        private void ManagerMainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Inactivity.OnFormClosing(this);
        }
    }
}
