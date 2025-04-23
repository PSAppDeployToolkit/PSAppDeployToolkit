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
    public partial class ClassicDialog : Form
    {
        static ClassicDialog()
        {
            // Only run in the actual app, not in Visual Studio's designer.
            if (LicenseManager.UsageMode == LicenseUsageMode.Runtime)
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
            }
        }

        public ClassicDialog()
        {
            InitializeComponent();
        }
    }
}
