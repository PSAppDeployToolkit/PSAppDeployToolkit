using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using PSADT.LibraryInterfaces;
using PSADT.UserInterface.DialogOptions;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace PSADT.UserInterface.Dialogs.Classic
{
    /// <summary>
    /// Progress dialog form.
    /// </summary>
    internal partial class ProgressDialog : ClassicDialog, IProgressDialog
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProgressDialog"/> class.
        /// </summary>
        internal ProgressDialog() : this(default!)
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
        internal ProgressDialog(ProgressDialogOptions options) : base(options)
        {
            // Initialise the form and reset the control order.
            // The designer tries to add its controls ahead of the base's.
            InitializeComponent();
            this.SuspendLayout();
            this.flowLayoutPanelBase.SuspendLayout();
            this.flowLayoutPanelDialog.SuspendLayout();

            // Apply options to the form if we have any (i.e. not in the designer).
            if (null != options)
            {
                // Set up the picturebox.
                SetPictureBox(this.pictureBanner, options);

                // Set up the rest of the dialog controls.
                UpdateProgressImpl(options.ProgressMessageText, options.ProgressDetailMessageText, options.ProgressPercentage, options.MessageAlignment);
            }

            // Resume the dialog now that we've applied any options.
            this.flowLayoutPanelDialog.ResumeLayout(false);
            this.flowLayoutPanelDialog.PerformLayout();
            this.flowLayoutPanelBase.ResumeLayout(false);
            this.flowLayoutPanelBase.PerformLayout();
            this.ResumeLayout();
            this.PerformLayout();
            EnableDragMove(this);
        }

        /// <summary>
        /// Updates the progress dialog with the specified messages and percentage complete.
        /// </summary>
        /// <param name="progressMessage"></param>
        /// <param name="progressMessageDetail"></param>
        /// <param name="progressPercentage"></param>
        /// <param name="messageAlignment"></param>
        public void UpdateProgress(string? progressMessage = null, string? progressMessageDetail = null, double? progressPercentage = null, DialogMessageAlignment? messageAlignment = null) => this.Invoke(() => UpdateProgressImpl(progressMessage, progressMessageDetail, progressPercentage, messageAlignment));

        /// <summary>
        /// Updates the progress dialog with the specified messages and percentage complete.
        /// </summary>
        /// <param name="progressMessage"></param>
        /// <param name="progressMessageDetail"></param>
        /// <param name="progressPercentage"></param>
        /// <param name="messageAlignment"></param>
        private void UpdateProgressImpl(string? progressMessage = null, string? progressMessageDetail = null, double? progressPercentage = null, DialogMessageAlignment? messageAlignment = null)
        {
            // Update the progress message.
            if (!string.IsNullOrWhiteSpace(progressMessage))
            {
                this.labelMessage.Text = StripFormattingTags(progressMessage!);
            }

            // Update the detail message.
            if (!string.IsNullOrWhiteSpace(progressMessageDetail))
            {
                this.labelDetail.Text = StripFormattingTags(progressMessageDetail!);
            }

            // Update the message alignment.
            if ((null != messageAlignment) && Enum.TryParse<ContentAlignment>($"Top{messageAlignment}", out var alignment))
            {
                this.labelMessage.TextAlign = alignment;
                this.labelDetail.TextAlign = alignment;
            }
            else
            {
                this.labelMessage.TextAlign = ContentAlignment.TopCenter;
                this.labelDetail.TextAlign = ContentAlignment.TopCenter;
            }

            // Update the progress percentage.
            if (null != progressPercentage)
            {
                this.progressBar.Style = ProgressBarStyle.Blocks;
                this.progressBar.Value = (int)progressPercentage.Value;
            }
            else
            {
                this.progressBar.Style = ProgressBarStyle.Marquee;
            }
        }

        /// <summary>
        /// Enables the drag move functionality for the form and its child controls.
        /// </summary>
        /// <param name="parent"></param>
        private void EnableDragMove(Control parent)
        {
            // Attach to this control, then recurse into its children.
            parent.MouseDown += AnyControl_MouseDown;
            foreach (Control child in parent.Controls)
            {
                EnableDragMove(child);
            }
        }

        /// <summary>
        /// Handles the mouse down event for the form and allows it to be moved.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AnyControl_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                User32.ReleaseCapture();
                User32.SendMessage((HWND)this.Handle, PInvoke.WM_NCLBUTTONDOWN, PInvoke.HTCAPTION, IntPtr.Zero);
            }
        }
    }
}
