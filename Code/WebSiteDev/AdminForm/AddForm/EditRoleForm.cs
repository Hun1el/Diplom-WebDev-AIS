using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace WebSiteDev.AdminForm.AddForm
{
    public partial class EditRoleForm : Form
    {
        private int selectedRoleID;
        private string roleName;

        public EditRoleForm(int selectedRoleID, string roleName)
        {
            InitializeComponent();

            this.selectedRoleID = selectedRoleID;
            this.roleName = roleName;
        }

        private void EditRoleForm_Load(object sender, EventArgs e)
        {
            LabelColor.ApplyRedStar(this);

            textBox1.Text = roleName;
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

            // Запрашиваем подтверждение
            var result = MessageBox.Show("Вы действительно хотите изменить роль?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                // Обновляем роль в БД
                if (DataUpdate.UpdateRole(selectedRoleID, textBox1.Text.Trim()))
                {
                    MessageBox.Show("Роль успешно изменена!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.Close();
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
