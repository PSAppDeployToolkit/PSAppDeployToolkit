using System;
using System.ComponentModel;

namespace PSADT.UserInterface.Dialogs.Classic
{
    /// <summary>
    /// Close applications dialog form.
    /// </summary>
    public partial class CloseAppsDialog : AbortableDialog
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CloseAppsDialog"/> class.
        /// </summary>
        public CloseAppsDialog() : this(default!)
        {
            if (LicenseManager.UsageMode == LicenseUsageMode.Runtime)
            {
                throw new InvalidOperationException("This constructor cannot be used in runtime mode.");
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CloseAppsDialog"/> class with the specified options.
        /// </summary>
        /// <param name="options"></param>
        public CloseAppsDialog(CloseAppsDialogOptions options) : base(options)
        {
            // Initialise the form and reset the control order.
            // The designer tries to add its controls ahead of the base's.
            InitializeComponent();
            this.SuspendLayout();
            this.flowLayoutPanelBase.SuspendLayout();
            this.Controls.Remove(this.flowLayoutPanelDialog);
            this.flowLayoutPanelBase.Controls.Add(this.flowLayoutPanelDialog);
            this.flowLayoutPanelBase.ResumeLayout();
            this.ResumeLayout();
        }
    }
}
