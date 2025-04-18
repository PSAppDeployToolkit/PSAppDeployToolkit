using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using Wpf.Ui.Controls;

namespace PSADT.UserInterface.Dialogs.Fluent
{
    /// <summary>
    /// Unified dialog for PSAppDeployToolkit that consolidates all dialog types into one.
    /// </summary>
    public partial class FluentDialog : FluentWindow, IDisposable, INotifyPropertyChanged
    {
        /// <summary>
        /// Initializes the UI elements and behavior for the Progress dialog type.
        /// </summary>
        /// <param name="appTitle">Main title of the dialog window.</param>
        /// <param name="subtitle">Subtitle displayed below the main title.</param>
        /// <param name="appIconImage">Path to the icon image file.</param>
        /// <param name="progressMessage">The main progress message text.</param>
        /// <param name="progressDetailMessage">A secondary message providing more detail.</param>
        public void InitializeProgressDialog(
            string? appTitle,
            string? subtitle,
            string? appIconImage,
            string? progressMessage,
            string? progressDetailMessage)
        {
            // Set basic properties
            Title = appTitle ?? "Operation Progress";
            AppTitleTextBlock.Text = appTitle;
            SubtitleTextBlock.Text = subtitle;

            // Set accessibility properties
            AutomationProperties.SetName(this, appTitle ?? "Progress Dialog");
            AutomationProperties.SetName(ProgressBar, "Operation Progress");

            // Set up UI
            FormatMessageWithHyperlinks(MessageTextBlock, progressMessage ?? "Deployment operation in progress. Please wait..."); // Use helper method
            CustomMessageTextBlock.Visibility = Visibility.Collapsed;
            ProgressMessageDetailTextBlock.Text = progressDetailMessage ?? "Performing deployment operation...";
            CloseAppsStackPanel.Visibility = Visibility.Collapsed;
            CloseAppsSeparator.Visibility = Visibility.Collapsed; // Hide the separator when not needed
            ProgressStackPanel.Visibility = Visibility.Visible;
            InputBoxStackPanel.Visibility = Visibility.Collapsed; // Ensure hidden by default
            DeferStackPanel.Visibility = Visibility.Collapsed;
            CountdownStackPanel.Visibility = Visibility.Collapsed;
            ButtonPanel.Visibility = Visibility.Collapsed;
            UpdateButtonLayout();

            // Initialize progress bar
            ProgressBar.IsIndeterminate = true;
            ProgressBar.Value = 0;

            // Set app icon
            SetAppIcon(appIconImage);
        }

        /// <summary>
        /// Updates the progress display in the Progress dialog.
        /// Animates the progress bar value if `percentComplete` is provided.
        /// </summary>
        /// <param name="progressMessage">Optional new main progress message.</param>
        /// <param name="progressMessageDetail">Optional new detail message.</param>
        /// <param name="percentComplete">Optional progress percentage (0-100). If provided, the progress bar becomes determinate and animates.</param>
        public void UpdateProgress(string? progressMessage = null, string? progressMessageDetail = null, double? percentComplete = null)
        {
            if (DialogType != DialogType.Progress || _isDisposed)
                return;

            try
            {
                Dispatcher.Invoke(() =>
                {
                    if (_isDisposed) return;

                    if (progressMessage != null)
                    {
                        FormatMessageWithHyperlinks(MessageTextBlock, progressMessage); // Use helper method
                        AutomationProperties.SetName(MessageTextBlock, progressMessage);
                    }

                    if (progressMessageDetail != null)
                    {
                        ProgressMessageDetailTextBlock.Text = progressMessageDetail;
                        AutomationProperties.SetName(ProgressMessageDetailTextBlock, progressMessage);
                    }

                    if (percentComplete != null)
                    {
                        // Turn off indeterminate mode if it was on
                        ProgressBar.IsIndeterminate = false;

                        // Create a smooth animation for the progress value
                        var animation = new DoubleAnimation
                        {
                            To = (double)percentComplete,
                            Duration = TimeSpan.FromMilliseconds(300),
                            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                        };

                        // Begin the animation
                        ProgressBar.BeginAnimation(System.Windows.Controls.Primitives.RangeBase.ValueProperty, animation);

                        // Update the property as well to maintain state
                        ProgressBarValue = (double)percentComplete;

                        // Update accessibility properties
                        AutomationProperties.SetName(ProgressBar, $"Progress: {percentComplete:F0}%");
                    }
                    else
                    {
                        ProgressBar.IsIndeterminate = true;
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in UpdateProgress: {ex.Message}");
            }
        }
    }
}
