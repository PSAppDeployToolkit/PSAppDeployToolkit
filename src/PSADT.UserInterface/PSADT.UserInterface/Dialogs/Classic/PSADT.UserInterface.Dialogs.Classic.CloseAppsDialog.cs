using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using PSADT.ProcessManagement;
using PSADT.UserInterface.DialogOptions;

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
        internal CloseAppsDialog() : this(default!)
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
        internal CloseAppsDialog(CloseAppsDialogOptions options) : base(options)
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
                // Set up main options.
                this.labelWelcomeMessage.Text = options.Strings.Classic.WelcomeMessage;
                this.labelAppName.Text = options.AppTitle.Replace("&", "&&");
                this.labelCloseProcessesMessage.Text = options.Strings.Classic.CloseAppsMessage;
                this.labelDeferralExpiryMessage.Text = options.Strings.Classic.ExpiryMessage;
                this.labelDeferWarningMessage.Text = options.Strings.Classic.ExpiryWarning;
                this.toolTipButtonContinue.RemoveAll();
                forcedCountdown = options.ForcedCountdown;
                countdownDuration = options.CountdownDuration;
                countdownClose = options.Strings.Classic.CountdownClose;
                countdownDefer = options.Strings.Classic.CountdownDefer;
                buttonContinueToolTipText = options.Strings.Classic.ButtonContinueTooltip;


                // Set the custom message text if we have one.
                if (null != options.CustomMessageText)
                {
                    this.labelCustomMessage.Text = options.CustomMessageText;
                }
                else
                {
                    this.flowLayoutPanelDialog.Controls.Remove(this.labelCustomMessage);
                }

                // Set up the process service.
                this.listBoxCloseProcesses.Items.Clear();
                if (null != options.RunningProcessService)
                {
                    // Get the current running apps and amend the form accordingly.
                    var runningApps = (runningProcessService = options.RunningProcessService).ProcessesToClose.Select(static p => p.Description).ToArray();
                    if (runningApps.Length > 0)
                    {
                        this.toolTipButtonContinue.SetToolTip(this.buttonContinue, buttonContinueToolTipText);
                        this.listBoxCloseProcesses.Items.AddRange(runningApps);
                    }
                    else
                    {
                        this.flowLayoutPanelCloseApps.Visible = false;
                        this.flowLayoutPanelCloseApps.Enabled = false;
                        this.buttonCloseProcesses.Enabled = false;
                    }
                }
                else
                {
                    this.flowLayoutPanelDialog.Controls.Remove(flowLayoutPanelCloseApps);
                    this.buttonCloseProcesses.Enabled = false;
                }

                // Set up our deferrals display.
                if (!((null == options.DeferralsRemaining) && (null == options.DeferralDeadline)))
                {
                    if (null != options.DeferralsRemaining)
                    {
                        this.labelDeferDeadline.Text = $"{options.Strings.Classic.DeferralsRemaining} {options.DeferralsRemaining}";
                    }
                    else if (null != options.DeferralDeadline)
                    {
                        this.labelDeferDeadline.Text = $"{options.Strings.Classic.DeferralDeadline} {options.DeferralDeadline}";
                    }
                }
                else
                {
                    this.flowLayoutPanelDialog.Controls.Remove(this.flowLayoutPanelDeferral);
                }

                // Set the countdown timer.
                if (null != countdownDuration)
                {
                    countdownTimer = new System.Threading.Timer(CountdownTimer_Tick, null, System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
                    countdownStopwatch = options.CountdownStopwatch;
                    if (this.listBoxCloseProcesses.Items.Count > 0)
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
            }

            // Resume the dialog now that we've applied any options.
            this.flowLayoutPanelDialog.ResumeLayout();
            this.flowLayoutPanelBase.Controls.Add(this.flowLayoutPanelDialog);
            this.flowLayoutPanelBase.ResumeLayout();
            this.ResumeLayout();
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
            SetResult("Close");
            base.ButtonLeft_Click(sender, e);
        }

        /// <summary>
        /// Handles the click event of the middle button (Defer).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void ButtonMiddle_Click(object sender, EventArgs e)
        {
            SetResult("Defer");
            base.ButtonMiddle_Click(sender, e);
        }

        /// <summary>
        /// Handles the click event of the right button (Continue).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void ButtonRight_Click(object sender, EventArgs e)
        {
            SetResult("Continue");
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
                    if (forcedCountdown && (null == runningProcessService || listBoxCloseProcesses.Items.Count == 0))
                    {
                        buttonContinue.PerformClick();
                    }
                    else if (forcedCountdown && this.flowLayoutPanelDialog.Controls.Contains(this.flowLayoutPanelDeferral))
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
                this.listBoxCloseProcesses.Items.Clear();
                if (e.ProcessesToClose.Count > 0)
                {
                    var runningApps = e.ProcessesToClose.Select(static p => p.Description).ToArray();
                    this.toolTipButtonContinue.SetToolTip(this.buttonContinue, buttonContinueToolTipText);
                    this.listBoxCloseProcesses.Items.AddRange(runningApps);
                    this.labelCountdownMessage.Text = countdownClose;
                    this.flowLayoutPanelCloseApps.Visible = true;
                    this.flowLayoutPanelCloseApps.Enabled = true;
                    this.buttonCloseProcesses.Enabled = true;
                }
                else
                {
                    this.toolTipButtonContinue.RemoveAll();
                    this.labelCountdownMessage.Text = countdownDefer;
                    this.flowLayoutPanelCloseApps.Visible = false;
                    this.flowLayoutPanelCloseApps.Enabled = false;
                    this.buttonCloseProcesses.Enabled = false;
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
        /// A restart countdown timer to perform an automatic reboot.
        /// </summary>
        private readonly System.Threading.Timer? countdownTimer;

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
    }
}
