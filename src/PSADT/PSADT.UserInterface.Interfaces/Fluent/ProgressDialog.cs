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

            // The custom message and detail line are excluded from the UI Automation tree.
            _screenReaderSuppressedElements.Add(CustomMessageTextBlock);
            _screenReaderSuppressedElements.Add(ProgressMessageDetailTextBlock);

            // The message and percentage are spoken through the shared live-region announcer (see
            // AnnouncePendingMessageOnOpen and UpdateProgressImpl), like the countdown. The progress bar takes
            // initial focus so the dialog gains UI Automation focus and Narrator processes those announcements
            // (it has no buttons, so with nothing focused Narrator stays on the launching window and says
            // nothing); AccessibleProgressBar's empty control type keeps that focus near-silent.
            ContentRendered += AnnouncePendingMessageOnOpen;
            ProgressBar.Focusable = true;
            ProgressBar.SetResourceReference(FocusVisualStyleProperty, "DefaultControlFocusVisualStyle");
            System.Windows.Input.KeyboardNavigation.SetIsTabStop(ProgressBar, isTabStop: true);
        }

        /// <summary>
        /// Announces the initial progress message once the window is shown, so Narrator (which speaks only for
        /// a visible window) does not miss it.
        /// </summary>
        /// <param name="sender">The dialog.</param>
        /// <param name="e">The event data.</param>
        private void AnnouncePendingMessageOnOpen(object? sender, EventArgs e)
        {
            ContentRendered -= AnnouncePendingMessageOnOpen;
            if (_pendingOpenMessage is not null)
            {
                Announce(_pendingOpenMessage, AutomationLiveSetting.Polite);
                _pendingOpenMessage = null;
            }
        }

        /// <inheritdoc />
        private protected override FrameworkElement? GetInitialFocusElement()
        {
            return ProgressBar;
        }

        /// <summary>
        /// Updates the progress display, animating the bar when <paramref name="progressPercentage"/> is given.
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
        /// Updates the progress display, animating the bar when <paramref name="percentComplete"/> is given.
        /// </summary>
        /// <param name="progressMessage">Optional new main progress message.</param>
        /// <param name="progressMessageDetail">Optional new detail message.</param>
        /// <param name="percentComplete">Optional progress percentage (0-100). If provided, the progress bar becomes determinate and animates.</param>
        private void UpdateProgressImpl(string? progressMessage = null, string? progressMessageDetail = null, double? percentComplete = null)
        {
            if (progressMessage is not null && !string.IsNullOrWhiteSpace(progressMessage))
            {
                FormatMessageWithHyperlinks(MessageTextBlock, progressMessage);
                string plainMessage = GetPlainText(MessageTextBlock);

                // Announce once per genuine change, deduped on the plain text. Before the window is shown the
                // announcement is deferred to ContentRendered so the initial message is not missed.
                if (!string.Equals(plainMessage, _lastAnnouncedMessage, StringComparison.Ordinal))
                {
                    _lastAnnouncedMessage = plainMessage;
                    if (IsLoaded)
                    {
                        _pendingOpenMessage = null;
                        Announce(plainMessage, AutomationLiveSetting.Polite);
                    }
                    else
                    {
                        _pendingOpenMessage = plainMessage;
                    }
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

                // Announce the percentage through the live-region announcer, deduped on the whole-number
                // percent and only once the window is shown, so it is not lost or stacked on the on-open read.
                int announcedPercent = (int)Math.Round(percentComplete.Value, MidpointRounding.ToEven);
                if (announcedPercent != _lastAnnouncedPercent)
                {
                    _lastAnnouncedPercent = announcedPercent;
                    if (IsLoaded)
                    {
                        Announce(announcedPercent.ToString(System.Globalization.CultureInfo.CurrentCulture) + "%", AutomationLiveSetting.Polite);
                    }
                }
            }
            else
            {
                // Update the properties as well to maintain state
                ProgressBar.ProgressMode = ProgressBarMode.Indeterminate;
            }
        }

        /// <summary>Plain text of the last announced message; suppresses re-announcing unchanged text.</summary>
        private string? _lastAnnouncedMessage;

        /// <summary>The initial message awaiting announcement once the window is shown, or null if none is pending.</summary>
        private string? _pendingOpenMessage;

        /// <summary>The last whole-number percent announced; suppresses re-announcing an unchanged percentage.</summary>
        private int? _lastAnnouncedPercent;
    }
}
