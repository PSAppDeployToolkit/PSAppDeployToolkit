using System;
using System.ComponentModel;
using System.Windows.Forms;
using Microsoft.Win32;

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
            if ("Server Core".Equals(Registry.GetValue(@"HKEY_LOCAL_MACHINE\Software\Microsoft\Windows NT\CurrentVersion", "InstallationType", null)))
            {
                throw new NotSupportedException("The dialog style [Classic] is not supported on Windows Server Core.");
            }
            if (LicenseManager.UsageMode == LicenseUsageMode.Runtime)
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
            }
        }
    }
}
