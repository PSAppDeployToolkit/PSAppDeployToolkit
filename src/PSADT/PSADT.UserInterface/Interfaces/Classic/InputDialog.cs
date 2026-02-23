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
        /// Initializes a new instance of the InputDialog class using the specified options.
        /// </summary>
        /// <remarks>This constructor sets up the dialog components and enforces licensing restrictions to
        /// ensure that InputDialog is only available in supported environments.</remarks>
        /// <param name="options">The options that configure the behavior and appearance of the input dialog.</param>
        /// <exception cref="NotImplementedException">Thrown if the dialog is instantiated in runtime license mode, as InputDialog is only implemented for the
        /// Fluent dialog type.</exception>
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
