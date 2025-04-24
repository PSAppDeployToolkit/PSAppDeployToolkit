using System;
using System.ComponentModel;

namespace PSADT.UserInterface.Dialogs.Classic
{
    /// <summary>
    /// Abortable classic dialog form.
    /// </summary>
    public partial class AbortableDialog : ClassicDialog
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AbortableDialog"/> class.
        /// </summary>
        public AbortableDialog() : this(default!)
        {
            if (LicenseManager.UsageMode == LicenseUsageMode.Runtime)
            {
                throw new InvalidOperationException("This constructor cannot be used in runtime mode.");
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AbortableDialog"/> class with the specified options.
        /// </summary>
        /// <param name="options"></param>
        public AbortableDialog(DialogOptions options) : base(options)
        {
            InitializeComponent();
        }
    }
}
