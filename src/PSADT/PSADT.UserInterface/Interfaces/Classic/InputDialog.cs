using System;
using System.ComponentModel;
using PSADT.UserInterface.DialogOptions;

namespace PSADT.UserInterface.Interfaces.Classic
{
    /// <summary>
    /// Abortable classic dialog form.
    /// </summary>
    internal partial class InputDialog : CustomDialog, IModalDialog
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InputDialog"/> class.
        /// </summary>
        internal InputDialog() : this(null!)
        {
            if (LicenseManager.UsageMode == LicenseUsageMode.Runtime)
            {
                throw new InvalidOperationException("This constructor cannot be used in runtime mode.");
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InputDialog"/> class with the specified options.
        /// </summary>
        /// <param name="options"></param>
        internal InputDialog(InputDialogOptions options) : base(options, null!)
        {
            if (LicenseManager.UsageMode == LicenseUsageMode.Runtime)
            {
                throw new NotImplementedException("The InputDialog is only implemented for the Fluent dialog type.");
            }
            InitializeComponent();
        }
    }
}
