using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Forms;
using PSADT.DeviceManagement;
using PSADT.UserInterface.DialogOptions;

namespace PSADT.UserInterface.Dialogs.Classic
{
    /// <summary>
    /// Restart dialog form.
    /// </summary>
    internal partial class RestartDialog : ClassicDialog, IModalDialog
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RestartDialog"/> class.
        /// </summary>
        internal RestartDialog() : this(default!)
        {
            if (LicenseManager.UsageMode == LicenseUsageMode.Runtime)
            {
                throw new InvalidOperationException("This constructor cannot be used in runtime mode.");
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RestartDialog"/> class with the specified options.
        /// </summary>
        /// <param name="options"></param>
        internal RestartDialog(RestartDialogOptions options) : base(options)
        {
            // Initialise the form and reset the control order.
            // The designer tries to add its controls ahead of the base's.
            InitializeComponent();
            this.SuspendLayout();
            this.flowLayoutPanelBase.SuspendLayout();
            this.flowLayoutPanelDialog.SuspendLayout();

            // Reset the dialog's title. It must be that of the string table in the options.
            this.Text = StripFormattingTags(options.Strings.Title);

            // Apply options to the form if we have any (i.e. not in the designer).
            if (options is not null)
            {
                // Set up the picturebox.
                SetPictureBox(this.pictureBanner, options);

                // Set the countdown timer.
                if (options.CountdownDuration is not null)
                {
                    this.countdownTimer = new(CountdownTimer_Tick, null, System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
                    this.countdownDuration = options.CountdownDuration.Value;
                    if (options.CountdownNoMinimizeDuration is not null)
                    {
                        this.minimizeDuration = options.CountdownNoMinimizeDuration.Value;
                    }
                }
                else
                {
                    this.flowLayoutPanelDialog.Controls.Remove(this.flowLayoutPanelCountdown);
                }

                // Set the custom message text if we have one.
                if (options.CustomMessageText is not null)
                {
                    this.labelCustomMessage.Text = StripFormattingTags(options.CustomMessageText);
                }
                else
                {
                    this.flowLayoutPanelDialog.Controls.Remove(this.labelCustomMessage);
                }

                // Set up remaining options.
                this.labelMessage.Text = StripFormattingTags(options.Strings.Message);
                this.labelRestartMessage.Text = StripFormattingTags($"{options.Strings.MessageTime} {options.Strings.MessageRestart}");
                this.labelTimeRemaining.Text = StripFormattingTags(options.Strings.TimeRemaining);
                this.buttonRestartNow.Text = StripFormattingTags(options.Strings.ButtonRestartNow);
                this.buttonMinimize.Text = StripFormattingTags(options.Strings.ButtonRestartLater);
            }

            // Resume the dialog now that we've applied any options.
            this.flowLayoutPanelDialog.ResumeLayout(false);
            this.flowLayoutPanelDialog.PerformLayout();
            this.flowLayoutPanelBase.ResumeLayout(false);
            this.flowLayoutPanelBase.PerformLayout();
            this.ResumeLayout();
            this.PerformLayout();
        }

        /// <summary>
        /// Handles the form's load event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void Form_Load(object? sender, EventArgs e)
        {
            // Perform the base event.
            base.Form_Load(sender, e);

            // Start the counterdown timer if we have one.
            if (countdownTimer is not null)
            {
                if (!countdownStopwatch.IsRunning)
                {
                    countdownStopwatch.Start();
                }
                countdownTimer.Change(0, 1000);
            }
        }

        /// <summary>
        /// Handles the form's closing event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void Form_FormClosing(object? sender, FormClosingEventArgs e)
        {
            // Cancel the event if we can't close (i.e. user has closed from the taskbar)
            if (!CanClose())
            {
                e.Cancel = true;
                return;
            }

            // We're actually closing. Perform certain disposals here
            // since we can't mess with the designer's Dispose override.
            countdownTimer?.Dispose();

            // Call through to the base method to ensure it's processed also.
            base.Form_FormClosing(sender, e);
        }

        /// <summary>
        /// Handles the click event of the left button (Restart Now).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void ButtonLeft_Click(object sender, EventArgs e)
        {
            // Restart the computer immediately.
            DeviceUtilities.RestartComputer();
            base.ButtonLeft_Click(sender, e);
        }

        /// <summary>
        /// Handles the click event of the right button (Minimise).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void ButtonRight_Click(object sender, EventArgs e)
        {
            // Minimise the window and restart the persistence timer.
            // Note that we deliberately do not call the base handler!
            this.WindowState = FormWindowState.Minimized;
            ResetPersistTimer();
        }

        /// <summary>
        /// Ticker for the countdown timer.
        /// </summary>
        /// <param name="state"></param>
        private void CountdownTimer_Tick(object? state)
        {
            var remaining = countdownDuration!.Value - countdownStopwatch.Elapsed;
            if (remaining < TimeSpan.Zero)
            {
                remaining = TimeSpan.Zero;
            }
            this.Invoke(() => labelCountdown.Text = FormatTime(remaining));
            if (remaining <= TimeSpan.Zero)
            {
                this.Invoke(() => buttonRestartNow.PerformClick());
            }
            else if ((minimizeDuration is not null) && (remaining <= minimizeDuration))
            {
                this.Invoke(() =>
                {
                    buttonMinimize.Enabled = false;
                    RestoreWindow();
                });
            }
        }

        /// <summary>
        /// A restart countdown timer to perform an automatic reboot.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "We can't override the designer's Dispose() implementation.")]
        private readonly System.Threading.Timer? countdownTimer;

        /// <summary>
        /// The stopwatch to keep track of the elapsed time.
        /// </summary>
        private readonly Stopwatch countdownStopwatch = new();

        /// <summary>
        /// The time span until the automatic restart is required.
        /// </summary>
        private readonly TimeSpan? countdownDuration;

        /// <summary>
        /// The time span until the minimize button is disabled.
        /// </summary>
        private readonly TimeSpan? minimizeDuration;
    }
}
