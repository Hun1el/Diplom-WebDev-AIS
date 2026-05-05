using System;
using System.Windows.Forms;

namespace WebSiteDev
{
    public class Inactivity
    {

        /// <summary>
        /// При загрузке формы
        /// </summary>
        public static void OnFormLoad(Form form)
        {
            BlockForms blockForms = Program.GetBlockForms();

            if (blockForms != null)
            {
                blockForms.RegisterForm(form);
                blockForms.Start();
            }
        }

        /// <summary>
        /// При закрытии формы
        /// </summary>
        public static void OnFormClosing(Form form)
        {
            BlockForms blockForms = Program.GetBlockForms();

            if (blockForms != null)
            {
                blockForms.UnregisterForm(form);
            }
        }
    }
}
