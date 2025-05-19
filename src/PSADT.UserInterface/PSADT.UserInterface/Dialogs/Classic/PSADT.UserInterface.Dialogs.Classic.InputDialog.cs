using System;
using System.ComponentModel;
using PSADT.UserInterface.DialogOptions;

namespace PSADT.UserInterface.Dialogs.Classic
{
    /// <summary>
    /// Abortable classic dialog form.
    /// </summary>
    internal partial class InputDialog : ClassicDialog
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InputDialog"/> class.
        /// </summary>
        internal InputDialog() : this(default!)
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
        internal InputDialog(InputDialogOptions options) : base(options)
        {
            if (LicenseManager.UsageMode == LicenseUsageMode.Runtime)
            {
                throw new NotImplementedException("The InputDialog is only implemented for the Fluent dialog type.");
            }
            InitializeComponent();
        }
    }
}
