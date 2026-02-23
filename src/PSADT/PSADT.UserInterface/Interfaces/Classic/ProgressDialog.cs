using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using PSADT.Interop;
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
        /// Initializes a new instance of the ProgressDialog class using the specified options.
        /// </summary>
        /// <remarks>This constructor applies the provided options to set up the dialog's controls,
        /// including the progress message, detail message, and progress percentage. The layout is configured to ensure
        /// proper display based on the options supplied.</remarks>
        /// <param name="options">The options that configure the appearance and behavior of the progress dialog. Cannot be null.</param>
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
        /// Updates the progress display with the specified message, detail, percentage, and alignment options.
        /// </summary>
        /// <remarks>This method is thread-safe and can be called from any thread. Only the provided
        /// arguments are updated; omitted arguments leave the corresponding display elements unchanged.</remarks>
        /// <param name="progressMessage">The main progress message to display. If null, no message is shown.</param>
        /// <param name="progressMessageDetail">Additional detail to display beneath the main progress message. If null, no additional detail is shown.</param>
        /// <param name="progressPercentage">The percentage of progress completed, as a value between 0 and 100. If null, the progress percentage is not
        /// updated.</param>
        /// <param name="messageAlignment">Specifies the alignment of the progress message. If null, the default alignment is used.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0058:Expression value is never used", Justification = "We can't suppress a mix of object/void returns.")]
        public void UpdateProgress(string? progressMessage = null, string? progressMessageDetail = null, double? progressPercentage = null, DialogMessageAlignment? messageAlignment = null)
        {
            Invoke(() => UpdateProgressImpl(progressMessage, progressMessageDetail, progressPercentage, messageAlignment));
        }

        /// <summary>
        /// Updates the progress dialog with the specified message, detail, progress percentage, and message alignment.
        /// </summary>
        /// <remarks>This method updates the user interface elements of the progress dialog. Only non-null
        /// and non-empty values are applied, allowing selective updates to the displayed information.</remarks>
        /// <param name="progressMessage">The main progress message to display. If null or empty, the existing message is not updated.</param>
        /// <param name="progressMessageDetail">The detailed progress message to display below the main message. If null or empty, the existing detail
        /// message is not updated.</param>
        /// <param name="progressPercentage">The percentage of progress completed, as a value between 0 and 100. If null, the progress bar displays in
        /// marquee style to indicate indeterminate progress.</param>
        /// <param name="messageAlignment">The alignment of the progress messages. If null or invalid, the messages are center-aligned by default.</param>
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
        /// Enables drag-and-drop movement for the specified control and all of its child controls by attaching mouse
        /// event handlers.
        /// </summary>
        /// <remarks>This method recursively attaches the mouse down event handler to all child controls,
        /// allowing for a consistent drag-and-drop experience across the control hierarchy.</remarks>
        /// <param name="parent">The parent control to which drag movement functionality will be applied. This control and all of its child
        /// controls will respond to mouse down events.</param>
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
        /// Handles the MouseDown event for a control to initiate a window drag operation when the left mouse button is
        /// pressed.
        /// </summary>
        /// <remarks>This method enables dragging of the window by simulating a non-client area mouse down
        /// event when the left mouse button is pressed on the control. This is commonly used to allow custom window
        /// chrome or controls to act as draggable areas.</remarks>
        /// <param name="sender">The source of the event, typically the control that received the mouse down action.</param>
        /// <param name="e">A MouseEventArgs object that contains the event data, including information about which mouse button was
        /// pressed.</param>
        private void AnyControl_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _ = NativeMethods.ReleaseCapture();
                _ = NativeMethods.SendMessage((HWND)Handle, WINDOW_MESSAGE.WM_NCLBUTTONDOWN, (nuint)WM_NCHITTEST.HTCAPTION, default);
            }
        }
    }
}
