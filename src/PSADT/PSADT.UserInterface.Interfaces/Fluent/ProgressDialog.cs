using System;
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0191:Do not use the null-forgiving operator", Justification = "This is necessary here.")]
        internal ProgressDialog(ProgressDialogOptions options) : base(options, null!)
        {
            UpdateProgressImpl(options.ProgressMessageText, options.ProgressDetailMessageText, options.ProgressPercentage);
            if (_dialogPosition is not DialogPosition.Oobe || (!DeviceUtilities.IsOOBEComplete() && !_dialogAllowMove))
            {
                IsMinimizeButtonVisible = Visibility.Visible;
            }
            ProgressStackPanel.Visibility = Visibility.Visible;

            // A screen reader reads only the app title and progress message; the custom message and detail
            // line are excluded from the UI Automation tree, and the percentage is never spoken.
            _screenReaderSuppressedElements.Add(CustomMessageTextBlock);
            _screenReaderSuppressedElements.Add(ProgressMessageDetailTextBlock);

            // Focus the message so it is read on open (after the window's name, the app title). Focusable
            // but not a tab stop.
            MessageTextBlock.Focusable = true;
            System.Windows.Input.KeyboardNavigation.SetIsTabStop(MessageTextBlock, isTabStop: false);
        }

        /// <inheritdoc />
        private protected override FrameworkElement? GetInitialFocusElement()
        {
            return MessageTextBlock;
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

                // Set the name once so it is read on open; later text changes are intentionally not re-read.
                if (!_accessibleMessageNameSet)
                {
                    AutomationProperties.SetName(MessageTextBlock, GetPlainText(MessageTextBlock));
                    _accessibleMessageNameSet = true;
                }
            }

            if (progressMessageDetail is not null && !string.IsNullOrWhiteSpace(progressMessageDetail))
            {
                // Visual update only: the detail line is excluded from the UI Automation tree and its
                // changes are deliberately never announced.
                FormatMessageWithHyperlinks(ProgressMessageDetailTextBlock, progressMessageDetail);
            }

            if (percentComplete is not null)
            {
                // Update the properties as well to maintain state
                ProgressBar.ProgressMode = ProgressBarMode.StepProgress;
                ProgressBar.Value = percentComplete.Value;
            }
            else
            {
                // Update the properties as well to maintain state
                ProgressBar.ProgressMode = ProgressBarMode.Indeterminate;
            }
        }

        /// <summary>
        /// Set once the progress message's accessible name has been assigned, so it is read during the
        /// natural on-open read and not re-read on later message updates.
        /// </summary>
        private bool _accessibleMessageNameSet;
    }
}
