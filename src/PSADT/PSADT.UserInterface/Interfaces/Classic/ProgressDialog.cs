using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using PSADT.LibraryInterfaces;
using PSADT.UserInterface.DialogOptions;
using Windows.Win32.Foundation;

namespace PSADT.UserInterface.Interfaces.Classic
{
    /// <summary>
    /// Progress dialog form.
    /// </summary>
    internal partial class ProgressDialog : ClassicDialog, IProgressDialog
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProgressDialog"/> class.
        /// </summary>
        internal ProgressDialog() : this(null!)
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
        internal ProgressDialog(ProgressDialogOptions options) : base(options, null!)
        {
            // Initialise the form and reset the control order.
            // The designer tries to add its controls ahead of the base's.
            InitializeComponent();
            SuspendLayout();
            flowLayoutPanelBase.SuspendLayout();
            flowLayoutPanelDialog.SuspendLayout();

            // Apply options to the form if we have any (i.e. not in the designer).
            if (options is not null)
            {
                // Set up the picturebox.
                SetPictureBox(pictureBanner, options);

                // Set up the rest of the dialog controls.
                UpdateProgressImpl(options.ProgressMessageText, options.ProgressDetailMessageText, options.ProgressPercentage, options.MessageAlignment);
            }

            // Resume the dialog now that we've applied any options.
            flowLayoutPanelDialog.ResumeLayout(false);
            flowLayoutPanelDialog.PerformLayout();
            flowLayoutPanelBase.ResumeLayout(false);
            flowLayoutPanelBase.PerformLayout();
            ResumeLayout();
            PerformLayout();
            EnableDragMove(this);
        }

        /// <summary>
        /// Updates the progress dialog with the specified messages and percentage complete.
        /// </summary>
        /// <param name="progressMessage"></param>
        /// <param name="progressMessageDetail"></param>
        /// <param name="progressPercentage"></param>
        /// <param name="messageAlignment"></param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0058:Expression value is never used", Justification = "We can't suppress a mix of object/void returns.")]
        public void UpdateProgress(string? progressMessage = null, string? progressMessageDetail = null, double? progressPercentage = null, DialogMessageAlignment? messageAlignment = null)
        {
            Invoke(() => UpdateProgressImpl(progressMessage, progressMessageDetail, progressPercentage, messageAlignment));
        }

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
                labelMessage.Text = StripFormattingTags(progressMessage!);
            }

            // Update the detail message.
            if (!string.IsNullOrWhiteSpace(progressMessageDetail))
            {
                labelDetail.Text = StripFormattingTags(progressMessageDetail!);
            }

            // Update the message alignment.
            if ((messageAlignment is not null) && Enum.TryParse($"Top{messageAlignment}", out ContentAlignment alignment))
            {
                labelMessage.TextAlign = alignment;
                labelDetail.TextAlign = alignment;
            }
            else
            {
                labelMessage.TextAlign = ContentAlignment.TopCenter;
                labelDetail.TextAlign = ContentAlignment.TopCenter;
            }

            // Update the progress percentage.
            if (progressPercentage is not null)
            {
                progressBar.Style = ProgressBarStyle.Blocks;
                progressBar.Value = (int)progressPercentage.Value;
            }
            else
            {
                progressBar.Style = ProgressBarStyle.Marquee;
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
                _ = User32.ReleaseCapture();
                _ = User32.SendMessage((HWND)Handle, WINDOW_MESSAGE.WM_NCLBUTTONDOWN, (nuint)WM_NCHITTEST.HTCAPTION, default);
            }
        }
    }
}
