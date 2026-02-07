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
        internal RestartDialog(RestartDialogOptions options) : base(options, options.CustomMessageText, options.CountdownDuration, options.CountdownNoMinimizeDuration)
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
        /// Handles the click event of the left button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void ButtonLeft_Click(object sender, RoutedEventArgs e)
        {
            // Immediately restart the computer.
            DeviceUtilities.RestartComputer();
            base.ButtonLeft_Click(sender, e);
        }

        /// <summary>
        /// Handles the click event of the right button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void ButtonRight_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        /// <summary>
        /// Handles the countdown timer tick event.
        /// </summary>
        /// <param name="state"></param>
        protected override void CountdownTimer_Tick(object? state)
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
