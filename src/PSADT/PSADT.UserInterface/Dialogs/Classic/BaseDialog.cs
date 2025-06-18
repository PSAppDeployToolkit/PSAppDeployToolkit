using System.ComponentModel;
using System.Windows.Forms;

namespace PSADT.UserInterface.Dialogs.Classic
{
    /// <summary>
    /// Represents the base class for custom dialog forms in a Windows Forms application.
    /// </summary>
    /// <remarks>This class ensures that visual styles and compatible text rendering are enabled for all derived dialog forms. It is intended to be used as a base class for creating consistent and properly initialized dialog windows.</remarks>
    internal class BaseDialog : Form
    {
        /// <summary>
        /// Static constructor to properly initialise WinForms dialogs.
        /// </summary>
        static BaseDialog()
        {
            if (LicenseManager.UsageMode == LicenseUsageMode.Runtime)
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
            }
        }
    }
}
