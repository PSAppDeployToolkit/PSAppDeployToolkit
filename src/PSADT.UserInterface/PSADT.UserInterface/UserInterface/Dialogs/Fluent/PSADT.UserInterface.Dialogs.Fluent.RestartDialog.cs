using System.ComponentModel;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Threading;
using Wpf.Ui.Controls;

namespace PSADT.UserInterface.Dialogs.Fluent
{
    /// <summary>
    /// Unified dialog for PSAppDeployToolkit that consolidates all dialog types into one.
    /// </summary>
    public partial class FluentDialog : FluentWindow, IDisposable, INotifyPropertyChanged
    {
        /// <summary>
        /// Initializes the UI elements and behavior for the Restart dialog type.
        /// </summary>
        /// <param name="appTitle">Main title of the dialog window.</param>
        /// <param name="subtitle">Subtitle displayed below the main title.</param>
        /// <param name="appIconImage">Path to the icon image file.</param>
        /// <param name="countdownDuration">Optional duration for a countdown timer before automatic restart.</param>
        /// <param name="countdownNoMinimizeDuration">Optional duration before the end of the countdown when the 'Dismiss' button is disabled.</param>
        /// <param name="restartMessageText">The main message text asking for restart confirmation.</param>
        /// <param name="countdownRestartMessageText">Message text displayed when the countdown is active.</param>
        /// <param name="countdownAutomaticRestartText">Heading text for the countdown timer.</param>
        /// <param name="dismissButtonText">Text for the dismiss/restart later button.</param>
        /// <param name="restartButtonText">Text for the restart now button.</param>
        public void InitializeRestartDialog(
            string? appTitle,
            string? subtitle,
            string? appIconImage,
            TimeSpan? countdownDuration,
            TimeSpan? countdownNoMinimizeDuration,
            string? restartMessageText,
            string? customMessageText,
            string? countdownRestartMessageText,
            string? countdownAutomaticRestartText,
            string? dismissButtonText,
            string? restartButtonText)
        {
            // Set basic properties
            Title = appTitle ?? "Restart Required";
            AppTitleTextBlock.Text = appTitle;
            SubtitleTextBlock.Text = subtitle;

            // Set accessibility properties
            AutomationProperties.SetName(this, appTitle ?? "Restart Required Dialog");

            // Restart Countdown
            _countdownDuration = countdownDuration;
            _countdownNoMinimizeDuration = countdownNoMinimizeDuration;

            // Set up UI
            FormatMessageWithHyperlinks(MessageTextBlock, countdownRestartMessageText ?? restartMessageText ?? "A system restart is required to complete the installation."); // Use helper method
            FormatMessageWithHyperlinks(CustomMessageTextBlock, customMessageText ?? string.Empty); // Use empty string if customMessageText is null
            CustomMessageTextBlock.Visibility = string.IsNullOrEmpty(customMessageText) ? Visibility.Collapsed : Visibility.Visible; // Show or hide the custom message based on its content

            CountdownHeadingTextBlock.Text = countdownAutomaticRestartText;
            CloseAppsStackPanel.Visibility = Visibility.Collapsed;
            ProgressStackPanel.Visibility = Visibility.Collapsed;
            InputBoxStackPanel.Visibility = Visibility.Collapsed; // Ensure hidden by default
            DeferStackPanel.Visibility = Visibility.Collapsed;
            CountdownStackPanel.Visibility = Visibility.Visible;
            ButtonPanel.Visibility = Visibility.Visible;

            // Configure buttons
            SetButtonContentWithAccelerator(ButtonLeft, dismissButtonText ?? "Dismiss");
            ButtonLeft.Visibility = Visibility.Visible;
            AutomationProperties.SetName(ButtonLeft, dismissButtonText ?? "Dismiss");

            ButtonMiddle.Visibility = Visibility.Hidden;
            SetButtonContentWithAccelerator(ButtonRight, restartButtonText ?? "Restart Now");
            ButtonRight.Visibility = Visibility.Visible;
            AutomationProperties.SetName(ButtonRight, restartButtonText ?? "Restart Now");

            UpdateButtonLayout();

            // Set app icon
            SetAppIcon(appIconImage);

            // Initialize countdown if specified
            if (countdownDuration.HasValue)
            {
                InitializeCountdown(countdownDuration.Value);
            }

            // Focus the restart button by default
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () =>
            {
                ButtonRight.Focus();
            });
        }
    }
}
