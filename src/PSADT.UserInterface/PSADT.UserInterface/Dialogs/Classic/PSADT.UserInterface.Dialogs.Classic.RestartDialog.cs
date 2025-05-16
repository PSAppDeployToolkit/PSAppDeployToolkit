using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Forms;
using PSADT.UserInterface.DialogOptions;

namespace PSADT.UserInterface.Dialogs.Classic
{
    /// <summary>
    /// Restart dialog form.
    /// </summary>
    public partial class RestartDialog : ClassicDialog
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RestartDialog"/> class.
        /// </summary>
        public RestartDialog() : this(default!)
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
        public RestartDialog(RestartDialogOptions options) : base(options)
        {
            // Initialise the form and reset the control order.
            // The designer tries to add its controls ahead of the base's.
            InitializeComponent();
            this.SuspendLayout();
            this.flowLayoutPanelBase.SuspendLayout();
            this.flowLayoutPanelDialog.SuspendLayout();
            this.Controls.Remove(this.flowLayoutPanelDialog);

            // Reset the dialog's title. It must be that of the string table in the options.
            this.Text = options.Strings.Title;

            // Apply options to the form if we have any (i.e. not in the designer).
            if (null != options)
            {
                // Set the countdown timer.
                if (null != options.CountdownDuration)
                {
                    this.countdownTimer = new System.Threading.Timer(CountdownTimer_Tick, null, System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
                    this.countdownDuration = options.CountdownDuration.Value;
                    if (null != options.CountdownNoMinimizeDuration)
                    {
                        this.minimizeDuration = options.CountdownNoMinimizeDuration.Value;
                    }
                }
                else
                {
                    this.flowLayoutPanelDialog.Controls.Remove(this.flowLayoutPanelCountdown);
                }

                // Set the custom message text if we have one.
                if (null != options.Strings.CustomMessage)
                {
                    this.labelCustomMessage.Text = options.Strings.CustomMessage;
                }
                else
                {
                    this.flowLayoutPanelDialog.Controls.Remove(this.labelCustomMessage);
                }

                // Set up remaining options.
                this.labelMessage.Text = options.Strings.Message;
                this.labelRestartMessage.Text = $"{options.Strings.MessageTime} {options.Strings.MessageRestart}";
                this.labelTimeRemaining.Text = options.Strings.TimeRemaining;
                this.buttonRestartNow.Text = options.Strings.ButtonRestartNow;
                this.buttonMinimize.Text = options.Strings.ButtonRestartLater;
            }

            // Resume the dialog now that we've applied any options.
            this.flowLayoutPanelDialog.ResumeLayout();
            this.flowLayoutPanelBase.Controls.Add(this.flowLayoutPanelDialog);
            this.flowLayoutPanelBase.ResumeLayout();
            this.Load += Form_Load;
            this.ResumeLayout();
        }

        /// <summary>
        /// Handles the form's load event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form_Load(object? sender, EventArgs e)
        {
            // Start the counterdown timer if we have one.
            if (null != countdownTimer)
            {
                countdownStopwatch.Start();
                countdownTimer.Change(0, 1000);
            }
        }

        /// <summary>
        /// Handles the click event of the left button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void buttonRestartNow_Click(object sender, EventArgs e)
        {
            // Restart the computer immediately.
            DialogTools.RestartComputer();
        }

        /// <summary>
        /// Handles the click event of the right button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void buttonMinimize_Click(object sender, EventArgs e)
        {
            // Minimise the window and restart the persistence timer.
            this.WindowState = FormWindowState.Minimized;
            this.PersistTimer?.Stop();
            this.PersistTimer?.Start();
        }

        /// <summary>
        /// Ticker for the countdown timer.
        /// </summary>
        /// <param name="state"></param>
        private void CountdownTimer_Tick(object? state)
        {
            var remaining = countdownDuration - countdownStopwatch.Elapsed;
            if (remaining < TimeSpan.Zero)
            {
                remaining = TimeSpan.Zero;
            }
            this.Invoke(() => labelCountdown.Text = FormatTime(remaining));
            if (remaining <= TimeSpan.Zero)
            {
                DialogTools.RestartComputer();
            }
            else if ((null != minimizeDuration) && (countdownStopwatch.Elapsed >= minimizeDuration))
            {
                this.Invoke(() => buttonMinimize.Enabled = false);
            }
        }

        /// <summary>
        /// Format the time span to a string.
        /// </summary>
        /// <param name="ts"></param>
        /// <returns></returns>
        private static string FormatTime(TimeSpan ts) => $"{ts.Days * 24 + ts.Hours}:{ts.Minutes:D2}:{ts.Seconds:D2}";

        /// <summary>
        /// A restart countdown timer to perform an automatic reboot.
        /// </summary>
        private readonly System.Threading.Timer? countdownTimer;

        /// <summary>
        /// The stopwatch to keep track of the elapsed time.
        /// </summary>
        private readonly Stopwatch countdownStopwatch = new();

        /// <summary>
        /// The time span until the automatic restart is required.
        /// </summary>
        private readonly TimeSpan countdownDuration;

        /// <summary>
        /// The time span until the minimize button is disabled.
        /// </summary>
        private readonly TimeSpan? minimizeDuration;
    }
}
