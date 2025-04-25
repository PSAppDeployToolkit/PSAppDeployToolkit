using System;
using System.ComponentModel;
using PSADT.UserInterface.DialogOptions;

namespace PSADT.UserInterface.Dialogs.Classic
{
    /// <summary>
    /// Abortable classic dialog form.
    /// </summary>
    public partial class InputDialog : ClassicDialog
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InputDialog"/> class.
        /// </summary>
        public InputDialog() : this(default!)
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
        public InputDialog(InputDialogOptions options) : base(options)
        {
            if (LicenseManager.UsageMode == LicenseUsageMode.Runtime)
            {
                throw new NotImplementedException("The InputDialog is only implemented for the Fluent dialog type.");
            }
            InitializeComponent();
        }
    }
}
