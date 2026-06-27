using System;
using System.Globalization;
using System.Windows;
using System.Windows.Automation;
using PSADT.DeviceManagement;
using PSADT.UserInterface.DialogOptions;
using Fluence.Wpf;

namespace PSADT.UserInterface.Interfaces.Fluent
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
        internal ProgressDialog(ProgressDialogOptions options) : base(options, null!)
        {
            UpdateProgressImpl(options.ProgressMessageText, options.ProgressDetailMessageText, options.ProgressPercentage);
            if (_dialogPosition is not DialogPosition.Oobe || (!DeviceUtilities.IsOOBEComplete() && !_dialogAllowMove))
            {
                IsMinimizeButtonVisible = Visibility.Visible;
            }
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
            if (progressMessage is not null)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(progressMessage);
            }
            if (progressMessageDetail is not null)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(progressMessageDetail);
            }
            UpdateProgressImpl(progressMessage, progressMessageDetail, progressPercentage);
        }

        /// <summary>
        /// Updates the progress display in the Progress dialog. Animates the progress bar value if `percentComplete` is provided.
        /// </summary>
        /// <param name="progressMessage">Optional new main progress message.</param>
        /// <param name="progressMessageDetail">Optional new detail message.</param>
        /// <param name="percentComplete">Optional progress percentage (0-100). If provided, the progress bar becomes determinate and animates.</param>
        private void UpdateProgressImpl(string? progressMessage = null, string? progressMessageDetail = null, double? percentComplete = null)
        {
            if (progressMessage is not null && !string.IsNullOrWhiteSpace(progressMessage))
            {
                FormatMessageWithHyperlinks(MessageTextBlock, progressMessage);
                AutomationProperties.SetName(MessageTextBlock, progressMessage);
            }

            if (progressMessageDetail is not null && !string.IsNullOrWhiteSpace(progressMessageDetail))
            {
                FormatMessageWithHyperlinks(ProgressMessageDetailTextBlock, progressMessageDetail);
                AutomationProperties.SetName(ProgressMessageDetailTextBlock, progressMessageDetail);
            }

            if (percentComplete is not null)
            {
                // Update the properties as well to maintain state
                ProgressBar.ProgressMode = ProgressBarMode.StepProgress;
                ProgressBar.Value = percentComplete.Value;

                // Update accessibility properties
                AutomationProperties.SetName(ProgressBar, $"Progress: {percentComplete.Value.ToString("F0", CultureInfo.InvariantCulture)}%");
            }
            else
            {
                // Update the properties as well to maintain state
                ProgressBar.ProgressMode = ProgressBarMode.Indeterminate;
            }
        }
    }
}
