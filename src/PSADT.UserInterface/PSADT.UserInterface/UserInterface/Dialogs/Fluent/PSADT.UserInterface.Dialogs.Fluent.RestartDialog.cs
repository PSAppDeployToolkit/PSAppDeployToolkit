using System.Windows;
using System.Windows.Automation;
using System.Windows.Threading;

namespace PSADT.UserInterface.Dialogs.Fluent
{
    /// <summary>
    /// A fluent implementation of PSAppDeployToolkit's RestartDialog dialog.
    /// </summary>
    public sealed class RestartDialog : FluentDialog
    {
        /// <summary>
        /// Instantiates a new RestartDialog dialog.
        /// </summary>
        /// <param name="options">Mandatory options needed to construct the window.</param>
        public RestartDialog(RestartDialogOptions options) : base(options, options.CustomMessageText, options.CountdownDuration, options.CountdownNoMinimizeDuration)
        {
            // Set up UI
            if (null != options.CountdownDuration)
            {
                FormatMessageWithHyperlinks(MessageTextBlock, options.CountdownRestartMessageText);
                CountdownHeadingTextBlock.Text = options.CountdownAutomaticRestartText;
                CountdownStackPanel.Visibility = Visibility.Visible;
            }
            else
            {
                FormatMessageWithHyperlinks(MessageTextBlock, options.RestartMessageText);
            }
            ButtonPanel.Visibility = Visibility.Visible;

            // Configure buttons
            SetButtonContentWithAccelerator(ButtonLeft, options.DismissButtonText);
            ButtonLeft.Visibility = Visibility.Visible;
            SetButtonContentWithAccelerator(ButtonRight, options.RestartButtonText);
            ButtonRight.Visibility = Visibility.Visible;

            // Set button automation properties
            AutomationProperties.SetName(ButtonLeft, options.DismissButtonText);
            AutomationProperties.SetName(ButtonRight, options.RestartButtonText);

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
            // Set the result but don't call the base method, we just want to return here.
            WindowState = WindowState.Minimized;
            DialogResult = "Dismiss";
        }

        /// <summary>
        /// Handles the click event of the right button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void ButtonRight_Click(object sender, RoutedEventArgs e)
        {
            // Set the result and call base method to handle window closure.
            DialogResult = "Restart";
            base.ButtonLeft_Click(sender, e);
        }
    }
}
