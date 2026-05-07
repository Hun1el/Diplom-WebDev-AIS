using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            
        }

        private void button2_Click(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// Кнопка "Создать резеврную копию"
        /// </summary>
        private void button3_Click(object sender, EventArgs e)
        {
            try
            {
                string Path = Backup.MakeBackup();
                
                MessageBox.Show($"Резервная копия создана по пути: {Path}", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось создать резервную копию\nОшибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// Кнопка "Выйти из учетной записи"
        /// </summary>
        private void button5_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show("Вы действительно хотите выйти?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }
    }
}
