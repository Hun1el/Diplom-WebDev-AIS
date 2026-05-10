using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace WebSiteDev
{
    class BlockForms
    {
        private Timer inactivityTimer; // таймер
        private int inactivityTimeoutSeconds; // время бездействия до блокировки
        private DateTime lastActivityTime; // время последней активности
        private List<Form> monitoredForms; // список форм
        private Form loginForm; // форма авторизации
        private bool isRunning; // флаг для проверок

        // Событие которое вызывается при обнаружении бездействия
        public event EventHandler OnInactivityDetected;

        /// <summary>
        /// Конструктор. Инициализирует систему с формой авторизации
        /// </summary>
        /// <param name="login">Форма авторизации</param>
        public BlockForms(Form login)
        {
            loginForm = login;
            monitoredForms = new List<Form>();
            inactivityTimeoutSeconds = Properties.Settings.Default.InactivityTime;
            lastActivityTime = DateTime.Now;
            isRunning = false;

            inactivityTimer = new Timer();
            inactivityTimer.Interval = 1000;
            inactivityTimer.Tick += InactivityTimer_Tick;
        }

        public void RegisterForm(Form form)
        {
            if (!monitoredForms.Contains(form))
            {
                monitoredForms.Add(form);
                SubscribeToActivityEvents(form);

                // Дополнительные события для отслеживания изменения формы (масштабирование)
                form.Resize += Activity_Detected;
                form.LocationChanged += Activity_Detected;
                form.Activated += Activity_Detected; // Когда окно становится активным
            }
        }

        public void UnregisterForm(Form form)
        {
            form.Resize -= Activity_Detected;
            form.LocationChanged -= Activity_Detected;
            form.Activated -= Activity_Detected;

            monitoredForms.Remove(form);
        }

        private void SubscribeToActivityEvents(Control parent)
        {
            // Убираем старые подписки чтобы не было дублей при динамическом изменении контролов
            parent.MouseMove -= Activity_Detected;
            parent.MouseClick -= Activity_Detected;
            parent.KeyDown -= Activity_Detected;

            parent.MouseMove += Activity_Detected;
            parent.MouseClick += Activity_Detected;
            parent.KeyDown += Activity_Detected;

            // Если это контейнер подписываемся на события добавления новых контролов
            parent.ControlAdded += (s, e) => SubscribeToActivityEvents(e.Control);

            foreach (Control child in parent.Controls)
            {
                SubscribeToActivityEvents(child);
            }
        }

        private void Activity_Detected(object sender, EventArgs e)
        {
            lastActivityTime = DateTime.Now;
        }

        private void InactivityTimer_Tick(object sender, EventArgs e)
        {
            TimeSpan inactivityDuration = DateTime.Now - lastActivityTime;

            if (inactivityDuration.TotalSeconds >= inactivityTimeoutSeconds)
            {
                // Проверяем, не открыто окно MessageBox и т.д
                OnInactivityDetected?.Invoke(this, EventArgs.Empty);
                Stop();
            }
        }

        public void Start()
        {
            lastActivityTime = DateTime.Now;
            isRunning = true;
            inactivityTimer.Start();
        }

        public void Stop()
        {
            isRunning = false;
            inactivityTimer.Stop();
        }

        public void Restart()
        {
            Stop();
            Start();
        }

        public void LockAllForms()
        {
            // Используем ToList для избежания ошибок при модификации коллекции во время цикла
            var formsToClose = Application.OpenForms.Cast<Form>().ToList();

            foreach (Form form in formsToClose)
            {
                if (form != loginForm && form.Name != "AuthForm")
                {
                    form.Close();
                }
            }

            if (loginForm != null && !loginForm.IsDisposed)
            {
                loginForm.Show();
                loginForm.BringToFront();
            }
        }

        public void UpdateTimeout(int newTimeoutSeconds)
        {
            inactivityTimeoutSeconds = newTimeoutSeconds;
            Properties.Settings.Default.InactivityTime = newTimeoutSeconds;
            Properties.Settings.Default.Save();
        }

        /// <summary>
        /// Возвращает текущее значение таймаута
        /// </summary>
        public int GetTimeout()
        {
            return inactivityTimeoutSeconds;
        }

        /// <summary>
        /// Проверяет запущен ли мониторинг
        /// </summary>
        public bool IsRunning()
        {
            return isRunning;
        }
    }
}
