using System;
using System.ComponentModel;
using PSADT.UserInterface.DialogOptions;

namespace PSADT.UserInterface.Interfaces.Classic
{
    /// <summary>
    /// Abortable classic dialog form.
    /// </summary>
    internal partial class ListSelectionDialog : ClassicDialog, IModalDialog
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ListSelectionDialog"/> class.
        /// </summary>
        internal ListSelectionDialog() : this(default!)
        {
            if (LicenseManager.UsageMode == LicenseUsageMode.Runtime)
            {
                throw new InvalidOperationException("This constructor cannot be used in runtime mode.");
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ListSelectionDialog"/> class with the specified options.
        /// </summary>
        /// <param name="options"></param>
        internal ListSelectionDialog(ListSelectionDialogOptions options) : base(options)
        {
            if (LicenseManager.UsageMode == LicenseUsageMode.Runtime)
            {
                throw new NotImplementedException("The ListSelectionDialog is only implemented for the Fluent dialog type.");
            }
            InitializeComponent();
        }
    }
}
