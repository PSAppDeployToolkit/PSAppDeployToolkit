using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using PSADT.ProcessManagement;
using PSADT.UserInterface.DialogOptions;
using PSADT.UserInterface.DialogResults;
using PSADT.UserInterface.DialogState;

namespace PSADT.UserInterface.Dialogs.Classic
{
    /// <summary>
    /// Close applications dialog form.
    /// </summary>
    internal partial class CloseAppsDialog : ClassicDialog, IModalDialog
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CloseAppsDialog"/> class.
        /// </summary>
        internal CloseAppsDialog() : this(default!, default!)
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
        internal CloseAppsDialog(CloseAppsDialogOptions options, CloseAppsDialogState state) : base(options)
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
                // Set up main options.
                this.labelWelcomeMessage.Text = StripFormattingTags(options.Strings.Classic.WelcomeMessage);
                this.labelAppName.Text = StripFormattingTags(Regex.Replace(options.AppTitle, @"(?<!&)&(?!&)", "&&"));
                this.labelCloseProcessesMessage.Text = StripFormattingTags(options.Strings.Classic.CloseAppsMessage);
                this.labelDeferralExpiryMessage.Text = StripFormattingTags(options.Strings.Classic.ExpiryMessage);
                this.labelDeferWarningMessage.Text = StripFormattingTags(options.Strings.Classic.ExpiryWarning);
                this.buttonCloseProcesses.Text = StripFormattingTags(options.Strings.Classic.ButtonClose);
                this.buttonDefer.Text = StripFormattingTags(options.Strings.Classic.ButtonDefer);
                this.buttonContinue.Text = StripFormattingTags(options.Strings.Classic.ButtonContinue);
                this.toolTipButtonContinue.RemoveAll();
                hideCloseButton = options.HideCloseButton;
                forcedCountdown = options.ForcedCountdown;
                countdownDuration = options.CountdownDuration;
                countdownClose = StripFormattingTags(options.Strings.Classic.CountdownClose);
                countdownDefer = StripFormattingTags(options.Strings.Classic.CountdownDefer);
                buttonContinueToolTipText = StripFormattingTags(options.Strings.Classic.ButtonContinueTooltip);
                continueOnProcessClosure = options.ContinueOnProcessClosure;

                // Set up the picturebox.
                SetPictureBox(this.pictureBanner, options);

                // Set the custom message text if we have one.
                if (null != options.CustomMessageText)
                {
                    this.labelCustomMessage.Text = StripFormattingTags(options.CustomMessageText);
                }
                else
                {
                    this.flowLayoutPanelDialog.Controls.Remove(this.labelCustomMessage);
                }

                // Allow the dialog to be minimised if specified.
                if (options.DialogAllowMinimize)
                {
                    this.FormBorderStyle = FormBorderStyle.FixedSingle;
                    this.ControlBox = true;
                    this.MinimizeBox = true;
                    this.MaximizeBox = false;
                    this.ShowInTaskbar = true;
                }

                // Set up the process service.
                this.richTextBoxCloseProcesses.Lines = null;
                if (null != state.RunningProcessService)
                {
                    // Get the current running apps and amend the form accordingly.
                    var runningApps = (runningProcessService = state.RunningProcessService).ProcessesToClose.Select(static p => $"{(char)0x200A}{p.Description}").ToArray();
                    if (runningApps.Length > 0)
                    {
                        this.toolTipButtonContinue.SetToolTip(this.buttonContinue, buttonContinueToolTipText);
                        this.richTextBoxCloseProcesses.Lines = runningApps;
                        if (hideCloseButton)
                        {
                            this.buttonCloseProcesses.Enabled = false;
                            this.buttonContinue.Enabled = false;
                        }
                    }
                    else
                    {
                        this.flowLayoutPanelCloseApps.Visible = false;
                        this.buttonCloseProcesses.Enabled = false;
                        this.buttonCloseProcesses.Visible = false;
                    }
                }
                else
                {
                    this.flowLayoutPanelDialog.Controls.Remove(flowLayoutPanelCloseApps);
                    this.buttonCloseProcesses.Enabled = false;
                    this.buttonCloseProcesses.Visible = false;
                }

                // Set up our deferrals display.
                if (!((null == options.DeferralsRemaining) && (null == options.DeferralDeadline)))
                {
                    this.labelDeferDeadline.Text = null;
                    if (null != options.DeferralsRemaining && !options.UnlimitedDeferrals)
                    {
                        this.labelDeferDeadline.Text = StripFormattingTags($"{options.Strings.Classic.DeferralsRemaining} {options.DeferralsRemaining}".Trim());
                        if (options.DeferralsRemaining <= 0)
                        {
                            this.buttonDefer.Enabled = false;
                        }
                    }
                    if (null != options.DeferralDeadline)
                    {
                        this.labelDeferDeadline.Text = StripFormattingTags($"{this.labelDeferDeadline.Text}\n{options.Strings.Classic.DeferralDeadline} {options.DeferralDeadline.Value.ToString(DateTimeFormatInfo.CurrentInfo.RFC1123Pattern) + options.DeferralDeadline.Value.ToString("zzz")}".Trim());
                        if (options.DeferralDeadline <= DateTime.Now)
                        {
                            this.buttonDefer.Enabled = false;
                        }
                    }
                    if (string.IsNullOrWhiteSpace(this.labelDeferDeadline.Text))
                    {
                        this.flowLayoutPanelDialog.Controls.Remove(this.flowLayoutPanelDeferral);
                    }
                }
                else
                {
                    this.flowLayoutPanelDialog.Controls.Remove(this.flowLayoutPanelDeferral);
                    this.buttonDefer.Enabled = false;
                }

                // Set the countdown timer.
                if (null != countdownDuration)
                {
                    countdownTimer = new(CountdownTimer_Tick, null, System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
                    countdownStopwatch = state.CountdownStopwatch;
                    if (this.richTextBoxCloseProcesses.Lines?.Length > 0)
                    {
                        if (forcedCountdown)
                        {
                            this.labelCountdownMessage.Text = countdownDefer;
                        }
                        else
                        {
                            this.labelCountdownMessage.Text = countdownClose;
                        }
                    }
                    else
                    {
                        this.labelCountdownMessage.Text = countdownDefer;
                    }
                }
                else
                {
                    this.flowLayoutPanelDialog.Controls.Remove(this.flowLayoutPanelCountdown);
                }

                // Set up the log writer if we have one.
                if (null != state.LogWriter)
                {
                    logWriter = state.LogWriter;
                }
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

            // Initialize the running process service and set up event handlers.
            if (null != runningProcessService)
            {
                runningProcessService.ProcessesToCloseChanged += RunningProcessService_ProcessesToCloseChanged;
            }

            // Start the counterdown timer if we have one.
            if (null != countdownTimer)
            {
                if (!countdownStopwatch!.IsRunning)
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
            countdownTimer = null;

            // Unhook the event handlers.
            if (null != runningProcessService)
            {
                runningProcessService.ProcessesToCloseChanged -= RunningProcessService_ProcessesToCloseChanged;
            }

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
        private void CountdownTimer_Tick(object? state)
        {
            var remaining = countdownDuration!.Value - countdownStopwatch!.Elapsed;
            if (remaining < TimeSpan.Zero)
            {
                remaining = TimeSpan.Zero;
            }
            this.Invoke(() => labelCountdown.Text = FormatTime(remaining));
            if (remaining <= TimeSpan.Zero)
            {
                this.Invoke(() =>
                {
                    if (forcedCountdown && (null == runningProcessService || richTextBoxCloseProcesses.Lines.Length == 0))
                    {
                        buttonContinue.PerformClick();
                    }
                    else if (forcedCountdown && this.flowLayoutPanelDialog.Controls.Contains(this.flowLayoutPanelDeferral) && this.buttonDefer.Enabled)
                    {
                        buttonDefer.PerformClick();
                    }
                    else
                    {
                        if (buttonCloseProcesses.CanFocus)
                        {
                            buttonCloseProcesses.PerformClick();
                        }
                        else
                        {
                            buttonContinue.PerformClick();
                        }
                    }
                });
            }
        }

        /// <summary>
        /// Handles the event when the list of processes to close changes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RunningProcessService_ProcessesToCloseChanged(object? sender, ProcessesToCloseChangedEventArgs e)
        {
            this.Invoke(() =>
            {
                this.richTextBoxCloseProcesses.Lines = null;
                if (e.ProcessesToClose.Count > 0)
                {
                    var runningApps = e.ProcessesToClose.Select(static p => $"{(char)0x200A}{p.Description}").ToArray();
                    if (null != logWriter)
                    {
                        logWriter.Write($"The running processes have changed. Updating the apps to close: ['{string.Join("', '", runningApps)}']...");
                        logWriter.Flush();
                    }
                    this.toolTipButtonContinue.SetToolTip(this.buttonContinue, buttonContinueToolTipText);
                    this.richTextBoxCloseProcesses.Lines = runningApps;
                    this.labelCountdownMessage.Text = countdownClose;
                    this.flowLayoutPanelCloseApps.Visible = true;
                    this.buttonCloseProcesses.Enabled = true;
                    this.buttonCloseProcesses.Visible = true;
                    if (hideCloseButton)
                    {
                        this.buttonCloseProcesses.Enabled = false;
                        this.buttonContinue.Enabled = false;
                    }
                }
                else
                {
                    if (null != logWriter)
                    {
                        logWriter.Write("Previously detected running processes are no longer running.");
                        logWriter.Flush();
                    }
                    this.toolTipButtonContinue.RemoveAll();
                    this.labelCountdownMessage.Text = countdownDefer;
                    this.flowLayoutPanelCloseApps.Visible = false;
                    this.buttonCloseProcesses.Enabled = false;
                    this.buttonCloseProcesses.Visible = false;
                    this.buttonContinue.Enabled = true;
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
        /// Represents the underlying writer used for logging operations.
        /// </summary>
        /// <remarks>This field holds a reference to a <see cref="StreamWriter"/> instance, which is used
        /// to write log entries. If <c>null</c>, logging operations may be disabled or unavailable.</remarks>
        private readonly BinaryWriter? logWriter;
    }
}
