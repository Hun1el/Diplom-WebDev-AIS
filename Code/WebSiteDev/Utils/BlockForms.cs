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
            inactivityTimeoutSeconds = Properties.Settings.Default.InactivityTime; // время бездействия из настроек
            lastActivityTime = DateTime.Now;
            isRunning = false; // система не запущена

            // инициализация таймера
            inactivityTimer = new Timer();
            inactivityTimer.Interval = 1000; // тикает каждую секунду
            inactivityTimer.Tick += InactivityTimer_Tick; // подписка на событие
        }

        /// <summary>
        /// Регистрирует форму для отслеживания активности
        /// </summary>
        public void RegisterForm(Form form)
        {
            monitoredForms.Add(form);
            SubscribeToActivityEvents(form);
        }

        /// <summary>
        /// Удаляет форму из отслеживания
        /// </summary>
        public void UnregisterForm(Form form)
        {
            monitoredForms.Remove(form);
        }

        /// <summary>
        /// Подписка на события активности для контрола и всех его элементов
        /// </summary>
        private void SubscribeToActivityEvents(Control parent)
        {
            parent.MouseMove += Activity_Detected;
            parent.MouseClick += Activity_Detected;
            parent.KeyDown += Activity_Detected;

            // Проход по всем событиям
            foreach (Control child in parent.Controls)
            {
                SubscribeToActivityEvents(child);
            }
        }

        /// <summary>
        /// Обработчик любого события активности пользователя
        /// Обновляет время последней активности
        /// </summary>
        private void Activity_Detected(object sender, EventArgs e)
        {
            lastActivityTime = DateTime.Now;
        }

        /// <summary>
        /// Обработчик тика таймера
        /// Проверяет прошло ли время бездействия
        /// </summary>
        private void InactivityTimer_Tick(object sender, EventArgs e)
        {
            TimeSpan inactivityDuration = DateTime.Now - lastActivityTime;

            // Если время бездействия превысило лимит
            if (inactivityDuration.TotalSeconds >= inactivityTimeoutSeconds)
            {
                OnInactivityDetected?.Invoke(this, EventArgs.Empty);
                Stop();
            }
        }

        /// <summary>
        /// Запускает мониторинг
        /// </summary>
        public void Start()
        {
            lastActivityTime = DateTime.Now;
            isRunning = true;
            inactivityTimer.Start();
        }

        /// <summary>
        /// Останавливает мониторинг
        /// </summary>
        public void Stop()
        {
            isRunning = false;
            inactivityTimer.Stop();
        }

        /// <summary>
        /// Перезапускает мониторинг (stop + start)
        /// </summary>
        public void Restart()
        {
            Stop();
            lastActivityTime = DateTime.Now;
            Start();
        }

        /// <summary>
        /// Блокирует все отслеживаемые формы и показывает форму авторизации
        /// </summary>
        public void LockAllForms()
        {
            foreach (Form form in monitoredForms.ToList())
            {
                if (form != null && !form.IsDisposed)
                {
                    // Закрываем все формы кроме AuthForm
                    if (form.GetType().Name != "AuthForm")
                    {
                        form.Close();
                    }
                }
            }

            // Показываем форму авторизации
            if (loginForm != null && !loginForm.IsDisposed)
            {
                loginForm.Show();
                loginForm.BringToFront();
            }
        }

        /// <summary>
        /// Обновляет время таймаута и сохраняет в настройках
        /// </summary>
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
