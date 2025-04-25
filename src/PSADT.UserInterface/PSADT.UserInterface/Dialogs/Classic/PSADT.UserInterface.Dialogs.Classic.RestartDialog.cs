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

            // Apply options to the form if we have any (i.e. not in the designer).
            if (null != options)
            {
                // Set the countdown timer.
                if (null != options.CountdownDuration)
                {
                    this.countdownTimer = new Timer() { Interval = 1000 };
                    this.countdownTimer.Tick += CountdownTimer_Tick;
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
                if (null != options.CustomMessageText)
                {
                    this.labelCustomMessage.Text = options.CustomMessageText;
                }
                else
                {
                    this.flowLayoutPanelDialog.Controls.Remove(this.labelCustomMessage);
                }

                // Set up remaining options.
                this.labelMessage.Text = options.RestartMessageText;
                this.labelRestartMessage.Text = options.CountdownRestartMessageText;
                this.labelTimeRemaining.Text = options.CountdownAutomaticRestartText;
                this.buttonRestartNow.Text = options.RestartButtonText;
                this.buttonMinimize.Text = options.DismissButtonText;
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
                var dateTime = DateTime.Now;
                countdownEnd = dateTime.Add(countdownDuration);
                minimizeEnd = dateTime.Add(minimizeDuration);
                labelCountdown.Text = FormatTime(countdownDuration);
                countdownTimer.Start();
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
            RestartComputer();
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
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CountdownTimer_Tick(object? sender, EventArgs e)
        {
            var dateTime = DateTime.Now;
            var remaining = countdownEnd - dateTime;
            if (remaining <= TimeSpan.Zero)
            {
                // Reboot the system and hard-exit this process.
                RestartComputer();
            }
            else if ((minimizeEnd - dateTime) <= TimeSpan.Zero)
            {
                // No minimize for you!
                buttonMinimize.Enabled = false;
            }

            // Update the time remaining.
            labelCountdown.Text = FormatTime(remaining);
        }

        /// <summary>
        /// Reboots the computer and terminates this process.
        /// </summary>
        private static void RestartComputer()
        {
            // Reboot the system and hard-exit this process.
            using (var process = new Process())
            {
                process.StartInfo.FileName = "shutdown.exe";
                process.StartInfo.Arguments = "/r /f /t 0";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                process.WaitForExit();
            }
            Environment.Exit(0);
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
        private Timer? countdownTimer;

        /// <summary>
        /// The time span until the automatic restart is required.
        /// </summary>
        private TimeSpan countdownDuration;

        /// <summary>
        /// The end time for our timer. We do it like this to avoid clock drift.
        /// </summary>
        private DateTime countdownEnd;

        /// <summary>
        /// The time span until the minimize button is disabled.
        /// </summary>
        private TimeSpan minimizeDuration;

        /// <summary>
        /// The end time for being able to minimise the dialog.
        /// </summary>
        private DateTime minimizeEnd;
    }
}
