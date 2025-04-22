using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace PSADT.UserInterface.Dialogs.Fluent
{
    /// <summary>
    /// A fluent implementation of PSAppDeployToolkit's ProgressDialog dialog.
    /// </summary>
    internal sealed class ProgressDialog : FluentDialog
    {
        /// <summary>
        /// Instantiates a new ProgressDialog dialog.
        /// </summary>
        /// <param name="options">Mandatory options needed to construct the window.</param>
        internal ProgressDialog(ProgressDialogOptions options) : base(options)
        {
            // Set accessibility properties
            AutomationProperties.SetName(ProgressBar, "Operation Progress");

            // Set up UI
            FormatMessageWithHyperlinks(MessageTextBlock, options.ProgressMessageText);
            ProgressMessageDetailTextBlock.Text = options.ProgressDetailMessageText;
            ProgressStackPanel.Visibility = Visibility.Visible;

            // Initialize progress bar
            ProgressBar.IsIndeterminate = true;
            ProgressBar.Value = 0;
        }

        /// <summary>
        /// Updates the progress display in the Progress dialog. Animates the progress bar value if `percentComplete` is provided.
        /// </summary>
        /// <param name="progressMessage">Optional new main progress message.</param>
        /// <param name="progressMessageDetail">Optional new detail message.</param>
        /// <param name="percentComplete">Optional progress percentage (0-100). If provided, the progress bar becomes determinate and animates.</param>
        internal void UpdateProgress(string? progressMessage = null, string? progressMessageDetail = null, double? percentComplete = null)
        {
            Dispatcher.Invoke(() =>
            {
                if (!string.IsNullOrWhiteSpace(progressMessage))
                {
                    FormatMessageWithHyperlinks(MessageTextBlock, progressMessage);
                    AutomationProperties.SetName(MessageTextBlock, progressMessage);
                }

                if (!string.IsNullOrWhiteSpace(progressMessageDetail))
                {
                    ProgressMessageDetailTextBlock.Text = progressMessageDetail;
                    AutomationProperties.SetName(ProgressMessageDetailTextBlock, progressMessage);
                }

                if (percentComplete != null)
                {
                    // Create a smooth animation for the progress value
                    var animation = new DoubleAnimation
                    {
                        To = percentComplete.Value,
                        Duration = TimeSpan.FromMilliseconds(300),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };

                    // Begin the animation
                    ProgressBar.BeginAnimation(System.Windows.Controls.Primitives.RangeBase.ValueProperty, animation);

                    // Update the property as well to maintain state
                    ProgressBar.Value = percentComplete.Value;

                    // Update accessibility properties
                    AutomationProperties.SetName(ProgressBar, $"Progress: {percentComplete:F0}%");

                    // Turn off indeterminate mode if it was on
                    ProgressBar.IsIndeterminate = false;
                }
                else
                {
                    ProgressBar.IsIndeterminate = true;
                }
            });
        }
    }
}
