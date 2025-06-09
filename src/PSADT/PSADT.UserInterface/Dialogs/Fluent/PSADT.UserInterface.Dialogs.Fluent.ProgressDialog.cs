using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using PSADT.UserInterface.DialogOptions;

namespace PSADT.UserInterface.Dialogs.Fluent
{
    /// <summary>
    /// A fluent implementation of PSAppDeployToolkit's ProgressDialog dialog.
    /// </summary>
    internal sealed class ProgressDialog : FluentDialog, IProgressDialog
    {
        /// <summary>
        /// Instantiates a new ProgressDialog dialog.
        /// </summary>
        /// <param name="options">Mandatory options needed to construct the window.</param>
        internal ProgressDialog(ProgressDialogOptions options) : base(options)
        {
            UpdateProgressImpl(options.ProgressMessageText, options.ProgressDetailMessageText, options.ProgressPercentage, options.MessageAlignment);
            ProgressStackPanel.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Updates the progress display in the Progress dialog. Animates the progress bar value if `percentComplete` is provided.
        /// </summary>
        /// <param name="progressMessage">Optional new main progress message.</param>
        /// <param name="progressMessageDetail">Optional new detail message.</param>
        /// <param name="progressPercentage">Optional progress percentage (0-100). If provided, the progress bar becomes determinate and animates.</param>
        /// <param name="messageAlignment">Unused message alignment, just here to satisfy the public interface contract.</param>
        public void UpdateProgress(string? progressMessage = null, string? progressMessageDetail = null, double? progressPercentage = null, DialogMessageAlignment? messageAlignment = null)
        {
            Dispatcher.Invoke(() => UpdateProgressImpl(progressMessage, progressMessageDetail, progressPercentage, messageAlignment));
        }

        /// <summary>
        /// Updates the progress display in the Progress dialog. Animates the progress bar value if `percentComplete` is provided.
        /// </summary>
        /// <param name="progressMessage">Optional new main progress message.</param>
        /// <param name="progressMessageDetail">Optional new detail message.</param>
        /// <param name="percentComplete">Optional progress percentage (0-100). If provided, the progress bar becomes determinate and animates.</param>
        /// <param name="messageAlignment">Unused message alignment, just here to satisfy the public interface contract.</param>
        private void UpdateProgressImpl(string? progressMessage = null, string? progressMessageDetail = null, double? percentComplete = null, DialogMessageAlignment? messageAlignment = null)
        {
            if (!string.IsNullOrWhiteSpace(progressMessage))
            {
                FormatMessageWithHyperlinks(MessageTextBlock, progressMessage!);
                AutomationProperties.SetName(MessageTextBlock, progressMessage!);
            }

            if (!string.IsNullOrWhiteSpace(progressMessageDetail))
            {
                ProgressMessageDetailTextBlock.Text = progressMessageDetail;
                AutomationProperties.SetName(ProgressMessageDetailTextBlock, progressMessage);
            }

            if (!(ProgressBar.IsIndeterminate = percentComplete == null))
            {
                // Create a smooth animation for the progress value
                var animation = new DoubleAnimation
                {
                    To = percentComplete!.Value,
                    Duration = TimeSpan.FromMilliseconds(300),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };

                // Begin the animation
                ProgressBar.BeginAnimation(RangeBase.ValueProperty, animation);

                // Update the property as well to maintain state
                ProgressBar.Value = percentComplete.Value;

                // Update accessibility properties
                AutomationProperties.SetName(ProgressBar, $"Progress: {percentComplete:F0}%");
            }
        }
    }
}
