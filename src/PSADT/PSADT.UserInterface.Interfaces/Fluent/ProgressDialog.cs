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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0191:Do not use the null-forgiving operator", Justification = "This is necessary here.")]
        internal ProgressDialog(ProgressDialogOptions options) : base(options, null!)
        {
            // The percent announcer must be in the visual tree before the first UpdateProgressImpl call
            // so an initial determinate percentage can be announced.
            AutomationProperties.SetLiveSetting(_percentAnnouncer, AutomationLiveSetting.Polite);
            _ = ProgressStackPanel.Children.Add(_percentAnnouncer);
            UpdateProgressImpl(options.ProgressMessageText, options.ProgressDetailMessageText, options.ProgressPercentage);
            if (_dialogPosition is not DialogPosition.Oobe || (!DeviceUtilities.IsOOBEComplete() && !_dialogAllowMove))
            {
                IsMinimizeButtonVisible = Visibility.Visible;
            }
            ProgressStackPanel.Visibility = Visibility.Visible;

            // A progress dialog reads only the app title, the message, and the progress value: the
            // custom message and the detail line are excluded from the UI Automation tree, and progress
            // updates are announced in short form (see UpdateProgressImpl).
            _screenReaderSuppressedElements.Add(CustomMessageTextBlock);
            _screenReaderSuppressedElements.Add(ProgressMessageDetailTextBlock);

            // Initial keyboard focus goes to the app title, whose accessible name never changes: focus
            // must land INSIDE the window for screen readers to honor its live-region events (the percent
            // announcer), but the previously focused message element was re-announced by the reader every
            // time its text changed, which for a caller updating the message each second drowned out the
            // terse percent announcements.
            AppTitleTextBlock.Focusable = true;
            System.Windows.Input.KeyboardNavigation.SetIsTabStop(AppTitleTextBlock, isTabStop: false);
        }

        /// <inheritdoc />
        private protected override FrameworkElement? GetInitialFocusElement()
        {
            return AppTitleTextBlock;
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

                // Rendered plain text, not the raw string: formatting tags such as [accent] must never be
                // read aloud. Message changes are deliberately not announced: a caller updating the message
                // every second restarts speech each time, so the listener hears the message prefix over and
                // over and the percent never gets airtime. The terse percent below is the only per-update
                // announcement; the message itself is read when the dialog opens.
                AutomationProperties.SetName(MessageTextBlock, GetPlainText(MessageTextBlock));
            }

            if (progressMessageDetail is not null && !string.IsNullOrWhiteSpace(progressMessageDetail))
            {
                // Visual update only: the detail line is excluded from the UI Automation tree and its
                // changes are deliberately not announced (progress announcements are the terse percent).
                FormatMessageWithHyperlinks(ProgressMessageDetailTextBlock, progressMessageDetail);
            }

            if (percentComplete is not null)
            {
                // Update the properties as well to maintain state
                ProgressBar.ProgressMode = ProgressBarMode.StepProgress;
                ProgressBar.Value = percentComplete.Value;

                // Announce only the first update after 0%, 25%, 50% and 75% (e.g. "10%", "30%", "50%",
                // "80%" for a caller stepping by tens), through a dedicated invisible live region whose
                // content changes exactly at those points. The bar itself is deliberately NOT a live
                // region and carries no authored name: its native RangeValue pattern supplies the current
                // value when a screen reader navigates to it, and a live bar gets announced by the reader
                // on every name/value update regardless of our own events.
                int roundedPercent = (int)Math.Round(percentComplete.Value, MidpointRounding.AwayFromZero);
                int bucket = GetProgressAnnouncementBucket(roundedPercent);
                if (bucket > _lastAnnouncedProgressBucket)
                {
                    _lastAnnouncedProgressBucket = bucket;
                    string spokenPercent = $"{roundedPercent.ToString(CultureInfo.InvariantCulture)}%";
                    _percentAnnouncer.Text = spokenPercent;
                    AutomationProperties.SetName(_percentAnnouncer, spokenPercent);
                    AnnounceLiveRegionChanged(_percentAnnouncer);
                }
            }
            else
            {
                // Update the properties as well to maintain state
                ProgressBar.ProgressMode = ProgressBarMode.Indeterminate;
            }
        }

        /// <summary>
        /// Maps a whole-number percentage to its announcement bucket: -1 for 0% (nothing to announce),
        /// then one bucket per quarter (above 0%, at/after 25%, 50% and 75%). An announcement fires only
        /// when the bucket increases, i.e. at the first update after each quarter boundary. Pure function
        /// for unit testing.
        /// </summary>
        /// <param name="percent">The whole-number progress percentage.</param>
        /// <returns>The announcement bucket for the percentage.</returns>
        internal static int GetProgressAnnouncementBucket(int percent)
        {
            return percent >= 75 ? 3 : percent >= 50 ? 2 : percent >= 25 ? 1 : percent > 0 ? 0 : -1;
        }

        /// <summary>
        /// The highest progress announcement bucket spoken so far (see <see cref="GetProgressAnnouncementBucket"/>).
        /// </summary>
        private int _lastAnnouncedProgressBucket = -1;

        /// <summary>
        /// The visually-inert polite live region that speaks the quarter-bucket progress announcements.
        /// Kept separate from the progress bar so the only live element in the dialog is one whose
        /// content changes exactly at the announcement points.
        /// </summary>
        private readonly System.Windows.Controls.TextBlock _percentAnnouncer = new()
        {
            Width = 1,
            Height = 1,
            Opacity = 0,
            Focusable = false,
            IsHitTestVisible = false,
        };
    }
}
