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

        /// <summary>
        /// Initializes a new instance of the <see cref="ClassicDialog"/> class.
        /// </summary>
        public ClassicDialog() : this(default!)
        {
            InitializeComponent();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClassicDialog"/> class with the specified options.
        /// </summary>
        /// <param name="options"></param>
        public ClassicDialog(DialogOptions options) : base()
        {
            InitializeComponent();
        }
    }
}
