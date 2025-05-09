using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PSADT.UserInterface.Dialogs.Classic
{
    public partial class HelpDialog : Form
    {
        /// <summary>
        /// Static constructor to initialize the application settings.
        /// </summary>
        static HelpDialog()
        {
            // Only run in the actual app, not in Visual Studio's designer.
            if (LicenseManager.UsageMode == LicenseUsageMode.Runtime)
            {
                #warning "TODO: Move to DialogManager"
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
            }
        }

        public HelpDialog()
        {
            InitializeComponent();
        }
    }
}
