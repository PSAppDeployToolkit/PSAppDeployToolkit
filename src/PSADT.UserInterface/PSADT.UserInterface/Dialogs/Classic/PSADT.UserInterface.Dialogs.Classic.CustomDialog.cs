using System;
using System.ComponentModel;

namespace PSADT.UserInterface.Dialogs.Classic
{
    /// <summary>
    /// Custom classic dialog form.
    /// </summary>
    public partial class CustomDialog : AbortableDialog
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CustomDialog"/> class.
        /// </summary>
        public CustomDialog() : this(default!)
        {
            if (LicenseManager.UsageMode == LicenseUsageMode.Runtime)
            {
                throw new InvalidOperationException("This constructor cannot be used in runtime mode.");
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomDialog"/> class with the specified options.
        /// </summary>
        /// <param name="options"></param>
        public CustomDialog(CustomDialogOptions options) : base(options)
        {
            InitializeComponent();
        }
    }
}
