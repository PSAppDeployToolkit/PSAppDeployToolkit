using System.Windows;
using System.Windows.Automation;
using System.Windows.Threading;
using PSADT.DeviceManagement;
using PSADT.UserInterface.DialogOptions;
using iNKORE.UI.WPF.Modern.Controls.Primitives;

namespace PSADT.UserInterface.Interfaces.Fluent
{
    /// <summary>
    /// A fluent implementation of PSAppDeployToolkit's RestartDialog dialog.
    /// </summary>
    internal sealed class RestartDialog : FluentDialog, IModalDialog
    {
        /// <summary>
        /// Instantiates a new RestartDialog dialog.
        /// </summary>
        /// <param name="options">Mandatory options needed to construct the window.</param>
        internal RestartDialog(RestartDialogOptions options) : base(options, null!, options.CustomMessageText, options.CountdownDuration, options.CountdownNoMinimizeDuration)
        {
            // Reset the dialog's title. It must be that of the string table in the options.
            Title = options.Strings.Title;

            // Set up UI
            if (_countdownDuration is not null)
            {
                FormatMessageWithHyperlinks(MessageTextBlock, options.Strings.MessageRestart);
                CountdownHeadingTextBlock.Text = options.Strings.TimeRemaining;
            }
            else
            {
                FormatMessageWithHyperlinks(MessageTextBlock, options.Strings.Message);
            }
            ButtonPanel.Visibility = Visibility.Visible;

            // Configure left button
            SetButtonContentWithAccelerator(ButtonLeft, options.Strings.ButtonRestartNow);
            AutomationProperties.SetName(ButtonLeft, options.Strings.ButtonRestartNow);
            ButtonLeft.Visibility = Visibility.Visible;
            SetDefaultButton(ButtonLeft);
            SetAccentButton(ButtonLeft);

            // Configure right button
            SetButtonContentWithAccelerator(ButtonRight, options.Strings.ButtonRestartLater);
            AutomationProperties.SetName(ButtonRight, options.Strings.ButtonRestartLater);
            SetMinimizeButtonAvailability(TitleBarButtonAvailability.Enabled);
            ButtonRight.Visibility = Visibility.Visible;
            SetCancelButton(ButtonRight);
        }

        /// <summary>
        /// Handles the event when the left button is clicked, initiating an immediate system restart.
        /// </summary>
        /// <remarks>This method overrides the base implementation to provide custom behavior for the left
        /// button click event. Calling this method will immediately restart the computer, which may interrupt any
        /// unsaved work.</remarks>
        /// <param name="sender">The source of the event, typically the control that raised the event.</param>
        /// <param name="e">The event data associated with the click event.</param>
        private protected override void ButtonLeft_Click(object sender, RoutedEventArgs e)
        {
            // Immediately restart the computer.
            DeviceUtilities.RestartComputer();
            base.ButtonLeft_Click(sender, e);
        }

        /// <summary>
        /// Handles the right button click event by minimizing the window.
        /// </summary>
        /// <remarks>Overrides the default right button behavior to minimize the window instead of
        /// performing any other action.</remarks>
        /// <param name="sender">The source of the event, typically the button that was clicked.</param>
        /// <param name="e">The event data associated with the click event.</param>
        private protected override void ButtonRight_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        /// <summary>
        /// Handles the timer tick event for the countdown, performing actions when the countdown expires or when a
        /// warning threshold is reached.
        /// </summary>
        /// <remarks>If the countdown duration has elapsed, the computer is restarted. When the remaining
        /// time is less than or equal to the warning duration, the minimize button is disabled and the window is
        /// restored to alert the user. This method overrides the base timer tick behavior to provide custom countdown
        /// handling.</remarks>
        /// <param name="state">An optional state object that can be used to pass additional information to the timer event handler.</param>
        private protected override void CountdownTimer_Tick(object? state)
        {
            // Call the base timer and test local expiration.
            base.CountdownTimer_Tick(state);
            if (_countdownStopwatch.Elapsed >= _countdownDuration)
            {
                DeviceUtilities.RestartComputer();
            }
            else if (_countdownWarningDuration.HasValue && _countdownRemainingTime <= _countdownWarningDuration.Value)
            {
                Dispatcher.Invoke(() =>
                {
                    SetMinimizeButtonAvailability(TitleBarButtonAvailability.Disabled);
                    ButtonRight.IsEnabled = false;
                    RestoreWindow();
                });
            }
        }
    }
}
