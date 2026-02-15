using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using PSADT.ProcessManagement;
using PSADT.UserInterface.DialogOptions;
using PSADT.UserInterface.DialogResults;
using PSADT.UserInterface.DialogState;
using PSAppDeployToolkit.Logging;

namespace PSADT.UserInterface.Interfaces.Classic
{
    /// <summary>
    /// Close applications dialog form.
    /// </summary>
    internal partial class CloseAppsDialog : ClassicDialog, IModalDialog
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CloseAppsDialog"/> class.
        /// </summary>
        internal CloseAppsDialog() : this(null!, null!)
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
        /// <param name="state"></param>
        internal CloseAppsDialog(CloseAppsDialogOptions options, CloseAppsDialogState state) : base(options, CloseAppsDialogResult.Timeout)
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
                // Set up main options.
                labelWelcomeMessage.Text = StripFormattingTags(options.Strings.Classic.WelcomeMessage);
                labelAppName.Text = StripFormattingTags(Regex.Replace(options.AppTitle, @"(?<!&)&(?!&)", "&&"));
                labelCloseProcessesMessage.Text = StripFormattingTags(options.Strings.Classic.CloseAppsMessage);
                labelDeferralExpiryMessage.Text = StripFormattingTags(options.Strings.Classic.ExpiryMessage);
                labelDeferWarningMessage.Text = StripFormattingTags(options.Strings.Classic.ExpiryWarning);
                buttonCloseProcesses.Text = StripFormattingTags(options.Strings.Classic.ButtonClose);
                buttonDefer.Text = StripFormattingTags(options.Strings.Classic.ButtonDefer);
                buttonContinue.Text = StripFormattingTags(options.Strings.Classic.ButtonContinue);
                toolTipButtonContinue.RemoveAll();
                hideCloseButton = options.HideCloseButton;
                forcedCountdown = options.ForcedCountdown;
                countdownDuration = options.CountdownDuration;
                countdownClose = StripFormattingTags(options.Strings.Classic.CountdownClose);
                countdownDefer = StripFormattingTags(options.Strings.Classic.CountdownDefer);
                buttonContinueToolTipText = StripFormattingTags(options.Strings.Classic.ButtonContinueTooltip);
                continueOnProcessClosure = options.ContinueOnProcessClosure;

                // Set up the picturebox.
                SetPictureBox(pictureBanner, options);

                // Set the custom message text if we have one.
                if (options.CustomMessageText is not null)
                {
                    labelCustomMessage.Text = StripFormattingTags(options.CustomMessageText);
                }
                else
                {
                    flowLayoutPanelDialog.Controls.Remove(labelCustomMessage);
                }

                // Allow the dialog to be minimised if specified.
                if (options.DialogAllowMinimize)
                {
                    FormBorderStyle = FormBorderStyle.FixedSingle;
                    ControlBox = true;
                    MinimizeBox = true;
                    MaximizeBox = false;
                    ShowInTaskbar = true;
                }

                // Set up the process service.
                richTextBoxCloseProcesses.Lines = null;
                if (state.RunningProcessService is not null)
                {
                    // Get the current running apps and amend the form accordingly.
                    string[] runningApps = [.. (runningProcessService = state.RunningProcessService).ProcessesToClose.Select(static p => $"{(char)0x200A}{p.Description}")];
                    if (runningApps.Length > 0)
                    {
                        toolTipButtonContinue.SetToolTip(buttonContinue, buttonContinueToolTipText);
                        richTextBoxCloseProcesses.Lines = runningApps;
                        if (hideCloseButton)
                        {
                            buttonCloseProcesses.Enabled = false;
                            buttonContinue.Enabled = false;
                        }
                    }
                    else
                    {
                        flowLayoutPanelCloseApps.Visible = false;
                        buttonCloseProcesses.Enabled = false;
                        buttonCloseProcesses.Visible = false;
                    }
                }
                else
                {
                    flowLayoutPanelDialog.Controls.Remove(flowLayoutPanelCloseApps);
                    buttonCloseProcesses.Enabled = false;
                    buttonCloseProcesses.Visible = false;
                }

                // Set up our deferrals display.
                if (!((options.DeferralsRemaining is null) && (options.DeferralDeadline is null)))
                {
                    labelDeferDeadline.Text = null;
                    if (options.DeferralsRemaining is not null && !options.UnlimitedDeferrals)
                    {
                        labelDeferDeadline.Text = StripFormattingTags($"{options.Strings.Classic.DeferralsRemaining} {options.DeferralsRemaining}".Trim());
                        if (options.DeferralsRemaining <= 0)
                        {
                            buttonDefer.Enabled = false;
                        }
                    }
                    if (options.DeferralDeadline is not null)
                    {
                        labelDeferDeadline.Text = StripFormattingTags($"{labelDeferDeadline.Text}{Environment.NewLine}{options.Strings.Classic.DeferralDeadline} {options.DeferralDeadline.Value.ToString(DateTimeFormatInfo.CurrentInfo.RFC1123Pattern, CultureInfo.CurrentCulture) + options.DeferralDeadline.Value.ToString("zzz", CultureInfo.CurrentCulture)}".Trim());
                        if (options.DeferralDeadline <= DateTime.Now)
                        {
                            buttonDefer.Enabled = false;
                        }
                    }
                    if (string.IsNullOrWhiteSpace(labelDeferDeadline.Text))
                    {
                        flowLayoutPanelDialog.Controls.Remove(flowLayoutPanelDeferral);
                    }
                }
                else
                {
                    flowLayoutPanelDialog.Controls.Remove(flowLayoutPanelDeferral);
                    buttonDefer.Enabled = false;
                }

                // Set the countdown timer.
                if (countdownDuration is not null)
                {
                    countdownTimer = new(CountdownTimer_Tick, null, System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
                    countdownStopwatch = state.CountdownStopwatch;
                    labelCountdownMessage.Text = richTextBoxCloseProcesses.Lines?.Length > 0 ? forcedCountdown ? countdownDefer : countdownClose : countdownDefer;
                }
                else
                {
                    flowLayoutPanelDialog.Controls.Remove(flowLayoutPanelCountdown);
                }

                // Set up the log action.
                logAction = state.LogAction;
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
        /// Handles the form's load event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void Form_Load(object? sender, EventArgs e)
        {
            // Perform the base event.
            base.Form_Load(sender, e);

            // Initialize the running process service and set up event handlers.
            runningProcessService?.ProcessesToCloseChanged += RunningProcessService_ProcessesToCloseChanged;

            // Start the counterdown timer if we have one.
            if (countdownTimer is not null)
            {
                if (!countdownStopwatch!.IsRunning)
                {
                    countdownStopwatch.Start();
                }
                _ = countdownTimer.Change(0, 1000);
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
            countdownTimer = null;

            // Unhook the event handlers.
            runningProcessService?.ProcessesToCloseChanged -= RunningProcessService_ProcessesToCloseChanged;

            // Call through to the base method to ensure it's processed also.
            base.Form_FormClosing(sender, e);
        }

        /// <summary>
        /// Handles the click event of the left button (Close Processes).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void ButtonLeft_Click(object sender, EventArgs e)
        {
            DialogResult = CloseAppsDialogResult.Close;
            base.ButtonLeft_Click(sender, e);
        }

        /// <summary>
        /// Handles the click event of the middle button (Defer).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void ButtonMiddle_Click(object sender, EventArgs e)
        {
            DialogResult = CloseAppsDialogResult.Defer;
            base.ButtonMiddle_Click(sender, e);
        }

        /// <summary>
        /// Handles the click event of the right button (Continue).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void ButtonRight_Click(object sender, EventArgs e)
        {
            DialogResult = CloseAppsDialogResult.Continue;
            base.ButtonRight_Click(sender, e);
        }

        /// <summary>
        /// Ticker for the countdown timer.
        /// </summary>
        /// <param name="state"></param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0058:Expression value is never used", Justification = "We can't suppress a mix of object/void returns.")]
        private void CountdownTimer_Tick(object? state)
        {
            TimeSpan remaining = countdownDuration!.Value - countdownStopwatch!.Elapsed;
            if (remaining < TimeSpan.Zero)
            {
                remaining = TimeSpan.Zero;
            }
            _ = Invoke(() => labelCountdown.Text = FormatTime(remaining));
            if (remaining <= TimeSpan.Zero)
            {
                Invoke(() =>
                {
                    if (forcedCountdown && (runningProcessService is null || (richTextBoxCloseProcesses.Lines.Length == 0 && !hideCloseButton)))
                    {
                        buttonContinue.PerformClick();
                    }
                    else if (forcedCountdown && buttonDefer.Enabled)
                    {
                        buttonDefer.PerformClick();
                    }
                    else if (buttonCloseProcesses.CanFocus)
                    {
                        buttonCloseProcesses.PerformClick();
                    }
                    else
                    {
                        buttonContinue.PerformClick();
                    }
                });
            }
        }

        /// <summary>
        /// Handles the event when the list of processes to close changes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0058:Expression value is never used", Justification = "We can't suppress a mix of object/void returns.")]
        private void RunningProcessService_ProcessesToCloseChanged(object? sender, ProcessesToCloseChangedEventArgs e)
        {
            Invoke(() =>
            {
                richTextBoxCloseProcesses.Lines = null;
                if (e.ProcessesToClose.Count > 0)
                {
                    string[] runningApps = [.. e.ProcessesToClose.Select(static p => $"{(char)0x200A}{p.Description}")];
                    logAction?.Invoke($"The running processes have changed. Updating the apps to close: ['{string.Join("', '", runningApps)}']...", LogSeverity.Info);
                    toolTipButtonContinue.SetToolTip(buttonContinue, buttonContinueToolTipText);
                    richTextBoxCloseProcesses.Lines = runningApps;
                    labelCountdownMessage.Text = countdownClose;
                    flowLayoutPanelCloseApps.Visible = true;
                    buttonCloseProcesses.Enabled = true;
                    buttonCloseProcesses.Visible = true;
                    if (hideCloseButton)
                    {
                        buttonCloseProcesses.Enabled = false;
                        buttonContinue.Enabled = false;
                    }
                }
                else
                {
                    logAction?.Invoke("Previously detected running processes are no longer running.", LogSeverity.Info);
                    toolTipButtonContinue.RemoveAll();
                    labelCountdownMessage.Text = countdownDefer;
                    flowLayoutPanelCloseApps.Visible = false;
                    buttonCloseProcesses.Enabled = false;
                    buttonCloseProcesses.Visible = false;
                    buttonContinue.Enabled = true;
                    if (continueOnProcessClosure)
                    {
                        buttonContinue.PerformClick();
                    }
                }
            });
        }

        /// <summary>
        /// The service object used to update running processes within this form.
        /// </summary>
        private readonly RunningProcessService? runningProcessService;

        /// <summary>
        /// Text used on the continue button's tooltip.
        /// </summary>
        private readonly string? buttonContinueToolTipText;

        /// <summary>
        /// Indicates whether the continue button should be implied when all processes have closed.
        /// </summary>
        private readonly bool continueOnProcessClosure;

        /// <summary>
        /// A restart countdown timer to perform an automatic reboot.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "We can't override the designer's Dispose() implementation.")]
        private System.Threading.Timer? countdownTimer;

        /// <summary>
        /// The stopwatch to keep track of the elapsed time.
        /// </summary>
        private readonly Stopwatch? countdownStopwatch;

        /// <summary>
        /// The time span until the automatic restart is required.
        /// </summary>
        private readonly TimeSpan? countdownDuration;

        /// <summary>
        /// The string to display on the countdown when there's no apps to close.
        /// </summary>
        private readonly string? countdownDefer;

        /// <summary>
        /// The string to display on the countdown when there's apps to close.
        /// </summary>
        private readonly string? countdownClose;

        /// <summary>
        /// Whether the countdown is considered "forced" or not.
        /// </summary>
        private readonly bool forcedCountdown;

        /// <summary>
        /// Indicates whether the close button should be hidden.
        /// </summary>
        private readonly bool hideCloseButton;

        /// <summary>
        /// Represents the delegate used for logging operations with severity.
        /// </summary>
        /// <remarks>This delegate is invoked to write log messages with optional severity.</remarks>
        private readonly Action<string, LogSeverity>? logAction;
    }
}
