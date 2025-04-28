using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Threading;
using PSADT.UserInterface.DialogOptions;

namespace PSADT.UserInterface.Dialogs.Fluent
{
    /// <summary>
    /// A fluent implementation of PSAppDeployToolkit's RestartDialog dialog.
    /// </summary>
    internal sealed class RestartDialog : FluentDialog
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

            // Configure buttons
            SetButtonContentWithAccelerator(ButtonLeft, options.Strings.ButtonRestartLater);
            ButtonLeft.Visibility = Visibility.Visible;
            SetButtonContentWithAccelerator(ButtonRight, options.Strings.ButtonRestartNow);
            ButtonRight.Visibility = Visibility.Visible;

            // Set button automation properties
            AutomationProperties.SetName(ButtonLeft, options.Strings.ButtonRestartLater);
            AutomationProperties.SetName(ButtonRight, options.Strings.ButtonRestartNow);

            // Focus the restart button by default
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () =>
            {
                ButtonRight.Focus();
            });
        }

        /// <summary>
        /// Handles the click event of the left button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void ButtonLeft_Click(object sender, RoutedEventArgs e)
        {
            // Just minimise the window.
            WindowState = WindowState.Minimized;
        }

        /// <summary>
        /// Handles the click event of the right button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void ButtonRight_Click(object sender, RoutedEventArgs e)
        {
            // Immediately restart the computer.
            DialogTools.RestartComputer();
            base.ButtonLeft_Click(sender, e);
        }

        /// <summary>
        /// Handles the countdown timer tick event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void CountdownTimer_Tick(object? sender, EventArgs e)
        {
            // Call the base timer and test local expiration.
            base.CountdownTimer_Tick(sender, e);
            var dateTime = DateTime.Now;
            if ((_countdownEnd - dateTime) < TimeSpan.Zero)
            {
                DialogTools.RestartComputer();
            }
            else if (null != _countdownWarningEnd && (_countdownWarningEnd - dateTime) < TimeSpan.Zero)
            {
                ButtonLeft.IsEnabled = false;
            }
        }
    }
}
