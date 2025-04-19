using System.Windows;
using System.Windows.Automation;
using System.Windows.Media;
using System.Windows.Threading;

namespace PSADT.UserInterface.Dialogs.Fluent
{
    /// <summary>
    /// A fluent implementation of PSAppDeployToolkit's CloseApps dialog.
    /// </summary>
    public sealed class CloseAppsDialog : FluentDialog, IDisposable
    {
        /// <summary>
        /// Instantiates a new CloseApps dialog.
        /// </summary>
        /// <param name="options">Mandatory options needed to construct the window.</param>
        public CloseAppsDialog(CloseAppsDialogOptions options) : base(options, options.CustomMessageText, options.CountdownDuration, null, "Continue")
        {
            // Store original and alternative texts
            _closeAppsMessageText = options.CloseAppsMessageText;
            _alternativeCloseAppsMessageText = options.AlternativeCloseAppsMessageText;
            _buttonLeftOriginalText = options.DeferButtonText;
            _buttonRightOriginalText = options.ContinueButtonText;
            _buttonRightAlternativeText = options.AlternativeContinueButtonText;
            _deferralsRemaining = options.DeferralsRemaining;
            _deferralDeadline = options.DeferralDeadline;

            // Set up UI
            FormatMessageWithHyperlinks(MessageTextBlock, _closeAppsMessageText);
            CloseAppsStackPanel.Visibility = Visibility.Visible;
            DeferStackPanel.Visibility = _deferralsRemaining.HasValue || _deferralDeadline.HasValue ? Visibility.Visible : Visibility.Collapsed;
            DeferralDeadlineHeadingTextBlock.Text = !_deferralDeadline.HasValue ? options.DeferralsRemainingText : options.DeferralDeadlineText;
            CountdownHeadingTextBlock.Text = options.AutomaticStartCountdownText;
            ButtonPanel.Visibility = Visibility.Visible;

            // Configure buttons
            SetButtonContentWithAccelerator(ButtonLeft, _buttonLeftOriginalText);
            ButtonLeft.Visibility = _deferralsRemaining.HasValue || _deferralDeadline.HasValue ? Visibility.Visible : Visibility.Collapsed;
            SetButtonContentWithAccelerator(ButtonRight, _buttonRightOriginalText);
            ButtonRight.Visibility = Visibility.Visible;

            // Set button automation properties
            AutomationProperties.SetName(ButtonLeft, _buttonLeftOriginalText);
            AutomationProperties.SetName(ButtonRight, _buttonRightOriginalText);
            UpdateDeferralValues();

            // Attach to window events specific to this dialog type
            if (options.AppsToClose != null && options.AppsToClose.Length > 0)
            {
                //_processEvaluationService = new ProcessEvaluationService();
                //_processEvaluationService.ProcessStarted += ProcessEvaluationService_ProcessStarted;
                //_processEvaluationService.ProcessExited += ProcessEvaluationService_ProcessExited;

                // Start monitoring processes
                //UpdateAppsToCloseList();
                //_processCancellationTokenSource = new CancellationTokenSource();
                //_ = StartProcessEvaluationLoopAsync(_appsToClose!, _processCancellationTokenSource.Token);
            }
            else
            {
                // No apps to close. CloseAppsStackPanel.Visibility = Visibility.Collapsed;
                FormatMessageWithHyperlinks(MessageTextBlock, _alternativeCloseAppsMessageText); // Use helper method
                SetButtonContentWithAccelerator(ButtonRight, _buttonRightAlternativeText);
                AutomationProperties.SetName(ButtonRight, "Install");
            }

            // Focus the continue button by default
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () =>
            {
                ButtonRight.Focus();
            });
        }

        /// <summary>
        /// Updates the deferral values displayed in the dialog.
        /// </summary>
        private void UpdateDeferralValues()
        {
            // First handle default case - if no deferral settings, just disable the button
            if (!_deferralsRemaining.HasValue && !_deferralDeadline.HasValue)
            {
                ButtonLeft.IsEnabled = false;
                return;
            }

            // Handle deferrals remaining counter
            if (_deferralsRemaining.HasValue)
            {
                UpdateDeferralsRemainingUI();
            }
            // Handle deferral deadline
            else if (_deferralDeadline.HasValue)
            {
                UpdateDeferralDeadlineUI();
            }
        }

        /// <summary>
        /// Updates the UI to reflect the number of deferrals remaining.
        /// </summary>
        private void UpdateDeferralsRemainingUI()
        {
            // Only enable the button if there are deferrals remaining
            ButtonLeft.IsEnabled = _deferralsRemaining > 0;

            // Update text value
            var displayText = $"{_deferralsRemaining} remain";
            DeferralDeadlineValueTextBlock.Text = displayText;

            // Update accessibility properties
            AutomationProperties.SetName(DeferralDeadlineValueTextBlock, displayText);

            // Update text color based on remaining deferrals
            if (_deferralsRemaining == 0)
            {
                DeferralDeadlineValueTextBlock.Foreground = (Brush)Resources["SystemFillColorCriticalBrush"];
            }
            else if (_deferralsRemaining <= 1)
            {
                DeferralDeadlineValueTextBlock.Foreground = (Brush)Resources["SystemFillColorCautionBrush"];
            }
            else
            {
                DeferralDeadlineValueTextBlock.Foreground = (Brush)Resources["TextFillColorPrimaryBrush"];
            }
        }

        /// <summary>
        /// Updates the UI to reflect the deferral deadline.
        /// </summary>
        private void UpdateDeferralDeadlineUI()
        {
            // Calculate time remaining until deadline
            TimeSpan timeRemaining = _deferralDeadline!.Value - DateTime.Now;
            bool isExpired = timeRemaining <= TimeSpan.Zero;

            // Set button state based on deadline
            ButtonLeft.IsEnabled = !isExpired;

            // Update text content
            string displayText;
            Brush textBrush;
            if (!isExpired)
            {
                displayText = _deferralDeadline.Value.ToString("r");
                if (timeRemaining < TimeSpan.FromDays(1))
                {
                    // Less than 1 day remaining - use caution color
                    textBrush = (Brush)Resources["SystemFillColorCautionBrush"];
                }
                else
                {
                    textBrush = (Brush)Resources["TextFillColorPrimaryBrush"];
                }
            }
            else
            {
                displayText = "Expired";
                textBrush = (Brush)Resources["SystemFillColorCriticalBrush"];
            }
            DeferralDeadlineValueTextBlock.Text = displayText;
            DeferralDeadlineValueTextBlock.Foreground = textBrush;

            // Update accessibility properties
            AutomationProperties.SetName(DeferralDeadlineValueTextBlock, displayText);
        }

        /// <summary>
        /// Handles the click event of the left button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void ButtonLeft_Click(object sender, RoutedEventArgs e)
        {
            // Set the result and call base method to handle window closure.
            if (_disposed)
            {
                return;
            }
            DialogResult = "Defer";
            base.ButtonLeft_Click(sender, e);
        }

        /// <summary>
        /// Handles the click event of the right button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void ButtonRight_Click(object sender, RoutedEventArgs e)
        {
            // Set the result and call base method to handle window closure.
            if (_disposed)
            {
                return;
            }
            DialogResult = "Continue";
            base.ButtonLeft_Click(sender, e);
        }

        /// <summary>
        /// The message to display when there's apps to close.
        /// </summary>
        private readonly string _closeAppsMessageText;

        /// <summary>
        /// The message to display when there's no apps to close.
        /// </summary>
        private readonly string _alternativeCloseAppsMessageText;

        /// <summary>
        /// The text for the left button.
        /// </summary>
        private readonly string _buttonLeftOriginalText;

        /// <summary>
        /// The text for the right button when there's apps to close.
        /// </summary>
        private readonly string _buttonRightOriginalText;

        /// <summary>
        /// The text for the right button when there's no apps to close.
        /// </summary>
        private readonly string _buttonRightAlternativeText;

        /// <summary>
        /// The deadline for deferral, if applicable.
        /// </summary>
        private readonly DateTime? _deferralDeadline;

        /// <summary>
        /// The number of deferrals remaining, if applicable.
        /// </summary>
        private readonly int? _deferralsRemaining;

        /// <summary>
        /// Whether this window has been disposed.
        /// </summary>
        private bool _disposed = false;

        /// <summary>
        /// Dispose managed and unmanaged resources
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;

            if (!disposing)
            {
                return;
            }
            base.Dispose(disposing);
        }
    }
}
