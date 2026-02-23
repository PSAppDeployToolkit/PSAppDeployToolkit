using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Forms;
using PSADT.DeviceManagement;
using PSADT.UserInterface.DialogOptions;

namespace PSADT.UserInterface.Interfaces.Classic
{
    /// <summary>
    /// Restart dialog form.
    /// </summary>
    internal partial class RestartDialog : ClassicDialog, IModalDialog
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RestartDialog"/> class.
        /// </summary>
        internal RestartDialog() : this(null!)
        {
            if (LicenseManager.UsageMode == LicenseUsageMode.Runtime)
            {
                throw new InvalidOperationException("This constructor cannot be used in runtime mode.");
            }
        }

        /// <summary>
        /// Initializes a new instance of the RestartDialog class using the specified dialog options.
        /// </summary>
        /// <remarks>This constructor applies the provided options to set up the dialog's user interface,
        /// such as setting the title, custom messages, and initializing the countdown timer if specified. If certain
        /// options are not provided, the corresponding UI elements are removed from the dialog. This allows for
        /// flexible customization of the dialog's content and behavior based on the supplied options.</remarks>
        /// <param name="options">The options that configure the dialog's appearance and behavior, including title, custom messages, and
        /// countdown settings. Cannot be null.</param>
        internal RestartDialog(RestartDialogOptions options) : base(options, null!)
        {
            // Initialise the form and reset the control order.
            // The designer tries to add its controls ahead of the base's.
            InitializeComponent();
            SuspendLayout();
            flowLayoutPanelBase.SuspendLayout();
            flowLayoutPanelDialog.SuspendLayout();

            // Reset the dialog's title. It must be that of the string table in the options.
            Text = StripFormattingTags(options.Strings.Title);

            // Apply options to the form if we have any (i.e. not in the designer).
            if (options is not null)
            {
                // Set up the picturebox.
                SetPictureBox(pictureBanner, options);

                // Set the countdown timer.
                if (options.CountdownDuration is not null)
                {
                    countdownTimer = new(CountdownTimer_Tick, null, System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
                    countdownDuration = options.CountdownDuration.Value;
                    if (options.CountdownNoMinimizeDuration is not null)
                    {
                        minimizeDuration = options.CountdownNoMinimizeDuration.Value;
                    }
                }
                else
                {
                    flowLayoutPanelDialog.Controls.Remove(flowLayoutPanelCountdown);
                }

                // Set the custom message text if we have one.
                if (options.CustomMessageText is not null)
                {
                    labelCustomMessage.Text = StripFormattingTags(options.CustomMessageText);
                }
                else
                {
                    flowLayoutPanelDialog.Controls.Remove(labelCustomMessage);
                }

                // Set up remaining options.
                labelMessage.Text = StripFormattingTags(options.Strings.Message);
                labelRestartMessage.Text = StripFormattingTags($"{options.Strings.MessageTime} {options.Strings.MessageRestart}");
                labelTimeRemaining.Text = StripFormattingTags(options.Strings.TimeRemaining);
                buttonRestartNow.Text = StripFormattingTags(options.Strings.ButtonRestartNow);
                buttonMinimize.Text = StripFormattingTags(options.Strings.ButtonRestartLater);
            }

            // Resume the dialog now that we've applied any options.
            flowLayoutPanelDialog.ResumeLayout(false);
            flowLayoutPanelDialog.PerformLayout();
            flowLayoutPanelBase.ResumeLayout(false);
            flowLayoutPanelBase.PerformLayout();
            ResumeLayout();
            PerformLayout();
        }

        /// <summary>
        /// Handles the form's load event and initializes the countdown timer if available.
        /// </summary>
        /// <remarks>This method overrides the base form load event to provide additional logic for
        /// managing a countdown timer. If a countdown timer is present and not already running, it is started when the
        /// form loads.</remarks>
        /// <param name="sender">The source of the event, typically the form instance.</param>
        /// <param name="e">An object that contains the event data.</param>
        /// <exception cref="InvalidOperationException">Thrown if the countdown timer fails to start.</exception>
        private protected override void Form_Load(object? sender, EventArgs e)
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
                if (!countdownTimer.Change(0, 1000))
                {
                    throw new InvalidOperationException("Failed to start the countdown timer.");
                }
            }
        }

        /// <summary>
        /// Handles the form's closing event, allowing for cleanup operations and the option to cancel the closing
        /// process based on application logic.
        /// </summary>
        /// <remarks>If the form cannot be closed, the closing event is canceled. Performs necessary
        /// resource cleanup before delegating to the base implementation.</remarks>
        /// <param name="sender">The source of the event, typically the form instance that is being closed.</param>
        /// <param name="e">A FormClosingEventArgs that contains the event data, including the ability to cancel the closing operation.</param>
        private protected override void Form_FormClosing(object? sender, FormClosingEventArgs e)
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
        /// Handles the event when the left button is clicked, initiating an immediate system restart.
        /// </summary>
        /// <remarks>This method overrides the base implementation to perform a system restart before
        /// invoking the base handler. Use with caution, as it will immediately restart the computer without further
        /// confirmation.</remarks>
        /// <param name="sender">The source of the event, typically the button that was clicked.</param>
        /// <param name="e">An object that contains the event data.</param>
        private protected override void ButtonLeft_Click(object sender, EventArgs e)
        {
            // Restart the computer immediately.
            DeviceUtilities.RestartComputer();
            base.ButtonLeft_Click(sender, e);
        }

        /// <summary>
        /// Handles the right button click event by minimizing the window and restarting the persistence timer.
        /// </summary>
        /// <remarks>This override does not call the base implementation, ensuring that only the custom
        /// minimize and timer reset logic is executed when the right button is clicked.</remarks>
        /// <param name="sender">The source of the event, typically the right button that was clicked.</param>
        /// <param name="e">An object that contains the event data.</param>
        private protected override void ButtonRight_Click(object sender, EventArgs e)
        {
            // Minimise the window and restart the persistence timer.
            // Note that we deliberately do not call the base handler!
            WindowState = FormWindowState.Minimized;
            ResetPersistTimer();
        }

        /// <summary>
        /// Handles the timer tick event for the countdown, updating the countdown display and managing related UI state
        /// based on the remaining time.
        /// </summary>
        /// <remarks>If the countdown reaches zero, the method triggers the restart action. When the
        /// remaining time is less than or equal to the minimize duration, it disables the minimize button and restores
        /// the window to ensure user attention.</remarks>
        /// <param name="state">An optional state object associated with the timer event. This parameter is not used.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0058:Expression value is never used", Justification = "We can't suppress a mix of object/void returns.")]
        private void CountdownTimer_Tick(object? state)
        {
            TimeSpan remaining = countdownDuration!.Value - countdownStopwatch.Elapsed;
            if (remaining < TimeSpan.Zero)
            {
                remaining = TimeSpan.Zero;
            }
            _ = Invoke(() => labelCountdown.Text = FormatTime(remaining));
            if (remaining <= TimeSpan.Zero)
            {
                Invoke(buttonRestartNow.PerformClick);
            }
            else if ((minimizeDuration is not null) && (remaining <= minimizeDuration))
            {
                Invoke(() =>
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
