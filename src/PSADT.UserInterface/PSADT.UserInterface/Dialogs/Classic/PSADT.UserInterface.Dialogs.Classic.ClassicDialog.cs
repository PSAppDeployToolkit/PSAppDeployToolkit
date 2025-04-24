using System.ComponentModel;
using System.Windows.Forms;

namespace PSADT.UserInterface.Dialogs.Classic
{
    /// <summary>
    /// Base class for classic dialog forms.
    /// </summary>
    public partial class ClassicDialog : Form
    {
        /// <summary>
        /// Static constructor to initialize the application settings.
        /// </summary>
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
