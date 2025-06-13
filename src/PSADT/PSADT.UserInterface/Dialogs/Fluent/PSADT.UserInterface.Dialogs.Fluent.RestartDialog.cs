using iNKORE.UI.WPF.Modern;
using PSADT.UserInterface.DialogOptions;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Threading;

namespace PSADT.UserInterface.Dialogs.Fluent
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
            if (null != _countdownDuration)
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

            // Configure right button
            SetButtonContentWithAccelerator(ButtonRight, options.Strings.ButtonRestartLater);
            AutomationProperties.SetName(ButtonRight, options.Strings.ButtonRestartLater);
            ButtonRight.Visibility = Visibility.Visible;
            ButtonRight.SetResourceReference(StyleProperty, ThemeKeys.AccentButtonStyleKey);
        }

        /// <summary>
        /// Handles the click event of the left button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void ButtonLeft_Click(object sender, RoutedEventArgs e)
        {
            // Immediately restart the computer.
            DialogTools.RestartComputer();
            base.ButtonLeft_Click(sender, e);
        }

        /// <summary>
        /// Handles the click event of the right button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void ButtonRight_Click(object sender, RoutedEventArgs e)
        {
            // Just minimize the window.
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
                DialogTools.RestartComputer();
            }
            else if (null != _countdownWarningDuration && _countdownStopwatch.Elapsed >= _countdownWarningDuration)
            {
                Dispatcher.Invoke(() =>
                {
                    ButtonRight.IsEnabled = false;
                    RestoreWindow();
                });
            }
        }
    }
}
