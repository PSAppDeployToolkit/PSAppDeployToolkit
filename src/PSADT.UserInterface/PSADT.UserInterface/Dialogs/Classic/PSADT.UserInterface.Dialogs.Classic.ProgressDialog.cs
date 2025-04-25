using System;
using System.ComponentModel;
using PSADT.UserInterface.DialogOptions;

namespace PSADT.UserInterface.Dialogs.Classic
{
    /// <summary>
    /// Progress dialog form.
    /// </summary>
    public partial class ProgressDialog : ClassicDialog
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProgressDialog"/> class.
        /// </summary>
        public ProgressDialog() : this(default!)
        {
            if (LicenseManager.UsageMode == LicenseUsageMode.Runtime)
            {
                throw new InvalidOperationException("This constructor cannot be used in runtime mode.");
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProgressDialog"/> class with the specified options.
        /// </summary>
        /// <param name="options"></param>
        public ProgressDialog(ProgressDialogOptions options) : base(options)
        {
            // Initialise the form and reset the control order.
            // The designer tries to add its controls ahead of the base's.
            InitializeComponent();
            this.SuspendLayout();
            this.flowLayoutPanelBase.SuspendLayout();
            this.flowLayoutPanelDialog.SuspendLayout();
            this.Controls.Remove(this.flowLayoutPanelDialog);

            // Apply options to the form if we have any (i.e. not in the designer).
            if (null != options)
            {
                this.labelMessage.Text = options.ProgressMessageText;
                this.labelDetail.Text = options.ProgressDetailMessageText;
            }

            // Resume the dialog now that we've applied any options.
            this.flowLayoutPanelDialog.ResumeLayout();
            this.flowLayoutPanelBase.Controls.Add(this.flowLayoutPanelDialog);
            this.flowLayoutPanelBase.ResumeLayout();
            this.ResumeLayout();
        }

        /// <summary>
        /// Updates the progress dialog with the specified messages and percentage complete.
        /// </summary>
        /// <param name="progressMessage"></param>
        /// <param name="progressMessageDetail"></param>
        /// <param name="percentComplete"></param>
        internal void UpdateProgress(string? progressMessage = null, string? progressMessageDetail = null, double? percentComplete = null)
        {
            // Update the progress message.
            if (!string.IsNullOrWhiteSpace(progressMessage))
            {
                this.labelMessage.Text = progressMessage;
            }

            // Update the detail message.
            if (!string.IsNullOrWhiteSpace(progressMessageDetail))
            {
                this.labelDetail.Text = progressMessageDetail;
            }

            // Update the progress percentage.
            if (null != percentComplete)
            {
                throw new NotSupportedException("Progress percentage is not supported in the classic dialog.");
            }
        }
    }
}
