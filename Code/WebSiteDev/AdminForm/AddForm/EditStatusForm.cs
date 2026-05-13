using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WebSiteDev.AdminForm.AddForm
{
    public partial class EditStatusForm : ScalableForm
    {
        protected override float MaxScale => 1.6f;
        protected override float MinScale => 0.9f;

        private int selectedStatusID;
        private string statusName;

        public EditStatusForm(int selectedStatusID, string statusName)
        {
            InitializeComponent();
            this.selectedStatusID = selectedStatusID;
            this.statusName = statusName;
        }

        private void EditStatusForm_Load(object sender, EventArgs e)
        {
            LabelColor.ApplyRedStar(this);

            textBox1.Text = statusName;
            textBox1.SelectionStart = textBox1.Text.Length;
            textBox1.SelectionLength = 0;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(textBox1.Text))
            {
                MessageBox.Show("Необходимо заполнить поля отмеченные \"*\"", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show("Вы действительно хотите изменить статус?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                // Обновляем статус в БД
                if (DataUpdate.UpdateStatus(selectedStatusID, textBox1.Text.Trim()))
                {
                    MessageBox.Show("Статус успешно изменен!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.Close();
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            InputRest.FirstLetter(textBox1);
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            InputRest.OnlyRussian(e);
        }
    }
}
