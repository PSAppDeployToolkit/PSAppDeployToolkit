using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using PSADT.UserInterface.Utilities;
using Wpf.Ui.Controls;

namespace PSADT.UserInterface.Dialogs.Fluent
{
    /// <summary>
    /// Unified dialog for PSAppDeployToolkit that consolidates all dialog types into one.
    /// </summary>
    public partial class FluentDialog : FluentWindow, IDisposable, INotifyPropertyChanged
    {
        /// <summary>
        /// Updates the Grid RowDefinition based on the current content
        /// </summary>
        protected void UpdateRowDefinition()
        {
            // Always use Auto sizing for all dialog types
            CenterPanelRow.Height = new GridLength(1, GridUnitType.Auto);
        }

        /// <summary>
        /// Sets the button content with an accelerator key (underscore) for accessibility.
        /// </summary>
        /// <param name="button"></param>
        /// <param name="text"></param>
        protected void SetButtonContentWithAccelerator(Wpf.Ui.Controls.Button button, string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            // Create AccessText to properly handle the underscore as accelerator
            button.Content = new AccessText
            {
                Text = text
            };
        }

        /// <summary>
        /// Formats the message text with clickable hyperlinks, supporting both plain URLs and Markdown-style links [text](url).
        /// </summary>
        /// <param name="textBlock"></param>
        /// <param name="message"></param>
        protected void FormatMessageWithHyperlinks(Wpf.Ui.Controls.TextBlock textBlock, string? message)
        {
            // Ensure the textblock is cleared and reset.
            textBlock.Inlines.Clear();

            // Don't waste time on an empty string.
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            // Regex to find Markdown links `[text](url)` or plain URLs.
            // Group 1: Full Markdown link (optional)
            // Group 2: Link text from Markdown (optional)
            // Group 3: URL from Markdown (optional)
            // Group 4: Full plain URL (optional)
            var linkRegex = new Regex(
                @"(\[([^\]]+)\]\(([^)\s]+)\))" + @"|" + // Markdown link: [text](url)
                @"((?i)\b(?:(?:https?|ftp|mailto):(?://)?|www\.|ftp\.)[-A-Z0-9+&@#/%?=~_|$!:,.;]*[A-Z0-9+&@#/%=~_|$])", // Plain URL
                RegexOptions.Compiled);

            // Process each found match and convert into a URI object
            int lastPos = 0;
            foreach (Match match in linkRegex.Matches(message))
            {
                // Add text before the hyperlink
                if (match.Index > lastPos)
                {
                    textBlock.Inlines.Add(new Run(message!.Substring(lastPos, match.Index - lastPos)));
                }

                string displayText, url;
                if (match.Groups[1].Success) // Markdown link matched
                {
                    displayText = match.Groups[2].Value;
                    url = match.Groups[3].Value;
                }
                else // Plain URL matched
                {
                    url = match.Groups[4].Value;
                    displayText = url; // Display the URL itself as text
                }

                // Ensure the URL has a scheme for Process.Start
                string navigateUrl = url;
                if (!navigateUrl.Contains("://") && !navigateUrl.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase) &&
                    navigateUrl.StartsWith("www.", StringComparison.OrdinalIgnoreCase) || navigateUrl.StartsWith("ftp.", StringComparison.OrdinalIgnoreCase))
                {
                    navigateUrl = "http://" + navigateUrl; // Assume http for www/ftp starts if no scheme
                }

                // Add the URL as a proper hyperlink
                try
                {
                    Uri uri = new Uri(navigateUrl); // Validate and create Uri
                    Hyperlink link = new Hyperlink(new Run(displayText))
                    {
                        NavigateUri = uri,
                        ToolTip = $"Open link: {url}" // Use original URL in tooltip
                    };
                    link.RequestNavigate += Hyperlink_RequestNavigate;
                    textBlock.Inlines.Add(link);
                }
                catch (UriFormatException)
                {
                    // If it's not a valid URI, just add the original matched text (could be Markdown or plain URL)
                    textBlock.Inlines.Add(new Run(match.Value));
                }
                catch (ArgumentNullException)
                {
                    // Handle potential null argument
                    textBlock.Inlines.Add(new Run(match.Value));
                }
                lastPos = match.Index + match.Length;
            }

            // Add any remaining text after the last hyperlink
            if (lastPos < message!.Length)
            {
                textBlock.Inlines.Add(new Run(message.Substring(lastPos)));
            }
        }

        /// <summary>
        /// Converts a hex color string to a Color object.
        /// </summary>
        /// <param name="colorStr"></param>
        /// <returns></returns>
        /// <exception cref="FormatException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        private static Color StringToColor(string colorStr)
        {
            if (!Regex.IsMatch(colorStr, "^#([0-9A-Fa-f]{6}|[0-9A-Fa-f]{8})$"))
            {
                throw new FormatException("Invalid hex color string.");
            }
            if (!(TypeDescriptor.GetConverter(typeof(Color)).ConvertFromString(colorStr) is Color result))
            {
                throw new InvalidOperationException("Failed to convert color string to Color.");
            }
            return result;
        }

        /// <summary>
        /// Sets the application icon displayed in the header and the window's taskbar icon.
        /// Uses a cache for performance.
        /// </summary>
        /// <param name="appIconImage">Path or URI to the icon image file. Defaults to embedded resource if null.</param>
        private void SetAppIcon(string? appIconImage)
        {
            // Use the built-in icon if one isn't specified
            if (string.IsNullOrWhiteSpace(appIconImage))
            {
                appIconImage = "pack://application:,,,/PSADT.UserInterface;component/Resources/appIcon.png";
            }

            // Try to get from cache first
            if (!_iconCache.TryGetValue(appIconImage!, out var iconImage))
            {
                // Use BeginInit/EndInit pattern for better performance.
                iconImage = new BitmapImage();
                iconImage.BeginInit();
                iconImage.CacheOption = BitmapCacheOption.OnLoad;
                iconImage.UriSource = new Uri(appIconImage, UriKind.Absolute);
                iconImage.EndInit();

                // Make it shareable across threads
                if (iconImage.CanFreeze)
                {
                    iconImage.Freeze();
                }
                _iconCache[appIconImage!] = iconImage;
            }
            AppIconImage.Source = iconImage;
            Icon = iconImage;
        }

        /// <summary>
        /// Positions the window on the screen based on the specified window position
        /// </summary>
        private void PositionWindow()
        {
            // Get the working area in DIPs.
            Rect workingArea = WPFScreen.FromHandle(new WindowInteropHelper(this).Handle).GetWorkingAreaInDips(this);

            // Ensure layout is updated to get ActualWidth and ActualHeight.
            double windowWidth = ActualWidth;
            double windowHeight = ActualHeight;

            // Margin to prevent overlap with screen edges.
            const double margin = 0;

            // Calculate positions based on window position setting.
            double left, top;
            switch (_dialogPosition)
            {
                case DialogPosition.Center:
                    // Center horizontally and vertically
                    left = workingArea.Left + ((workingArea.Width - windowWidth) / 2);
                    top = workingArea.Top + ((workingArea.Height - windowHeight) / 2);
                    break;

                case DialogPosition.TopCenter:
                    // Center horizontally, align to top
                    left = workingArea.Left + ((workingArea.Width - windowWidth) / 2);
                    top = workingArea.Top + margin;
                    break;

                default:
                    // Align to bottom right (original behavior)
                    left = workingArea.Left + (workingArea.Width - windowWidth);
                    top = workingArea.Top + (workingArea.Height - windowHeight);
                    left -= margin;
                    top -= margin;
                    break;
            }

            // Ensure the window is within the screen bounds.
            left = Math.Max(workingArea.Left, Math.Min(left, workingArea.Right - windowWidth));
            top = Math.Max(workingArea.Top, Math.Min(top, workingArea.Bottom - windowHeight));

            // Align positions to whole pixels.
            left = Math.Floor(left);
            top = Math.Floor(top);

            // Adjust for workArea offset.
            left += 1;
            top += 1;

            // Set positions in DIPs.
            Left = left;
            Top = top;
        }

        /// <summary>
        /// Updates the layout of the action buttons based on their visibility.
        /// </summary>
        private void UpdateButtonLayout()
        {
            // Build a list of visible buttons in the order they appear.
            var visibleButtons = new List<UIElement>();
            if (ButtonLeft.Visibility == Visibility.Visible)
            {
                visibleButtons.Add(ButtonLeft);
            }
            if (ButtonMiddle.Visibility == Visibility.Visible)
            {
                visibleButtons.Add(ButtonMiddle);
            }
            if (ButtonRight.Visibility == Visibility.Visible)
            {
                visibleButtons.Add(ButtonRight);
            }

            // Clear any existing column definitions.
            ActionButtons.ColumnDefinitions.Clear();

            // Special case: if there's only one visible button, limit its width to half of the grid
            if (visibleButtons.Count > 1)
            {
                // Create equally sized columns for each visible button (original behavior)
                for (int i = 0; i < visibleButtons.Count; i++)
                {
                    // Set margin based on position
                    ActionButtons.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    Grid.SetColumn(visibleButtons[i], i);
                    Wpf.Ui.Controls.Button button = (Wpf.Ui.Controls.Button)visibleButtons[i];
                    if (i == 0)
                    {
                        button.Margin = new Thickness(0, 0, 4, 0);
                    }
                    else if (i == visibleButtons.Count - 1)
                    {
                        button.Margin = new Thickness(4, 0, 0, 0);
                    }
                    else
                    {
                        button.Margin = new Thickness(4, 0, 4, 0);
                    }
                }
            }
            else
            {
                // Add two columns - one for the button (50% width) and one empty (50% width)
                ActionButtons.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                ActionButtons.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                // Place the single button in the second column
                Grid.SetColumn(visibleButtons[0], 1);

                // Set appropriate margin
                Wpf.Ui.Controls.Button button = (Wpf.Ui.Controls.Button)visibleButtons[0];
                button.Margin = new Thickness(0, 0, 0, 0);
            }
        }

        /// <summary>
        /// Initializes the countdown timer and display for dialogs that support it (CloseApps, Restart).
        /// </summary>
        /// <param name="duration">The total duration of the countdown.</param>
        private void InitializeCountdown(TimeSpan duration)
        {
            _countdownRemainingTime = duration;
            _deferralDeadlineRemainingTime = duration;

            // Update the display initially
            UpdateCountdownDisplay();

            // Set up the timer to update every second
            _countdownTimer = new Timer(CountdownTimerCallback, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
        }

        /// <summary>
        /// Updates the countdown text display and adjusts text color based on remaining time.
        /// Handles disabling the dismiss button for Restart dialogs based on `countdownNoMinimizeDuration`.
        /// </summary>
        private void UpdateCountdownDisplay()
        {
            // Format the remaining time as hh:mm:ss
            CountdownValueTextBlock.Text = $"{_countdownRemainingTime.Hours}h {_countdownRemainingTime.Minutes}m {_countdownRemainingTime.Seconds}s";

            // Update accessibility properties
            AutomationProperties.SetName(CountdownValueTextBlock, $"Time remaining: {_countdownRemainingTime.Hours} hours, {_countdownRemainingTime.Minutes} minutes, {_countdownRemainingTime.Seconds} seconds");

            // Update text color based on remaining time
            if (_countdownRemainingTime.TotalSeconds <= 60)
            {
                // Less than 60 seconds - use critical color
                CountdownValueTextBlock.Foreground = (Brush)Resources["SystemFillColorCriticalBrush"];
            }
            else if (_countdownNoMinimizeDuration.HasValue && _countdownRemainingTime <= _countdownNoMinimizeDuration)
            {
                // Less than no-minimize duration - use attention color
                CountdownValueTextBlock.Foreground = (Brush)Resources["SystemFillColorCautionBrush"];
            }
            else
            {
                // Normal time - use default text color
                CountdownValueTextBlock.Foreground = (Brush)Resources["TextFillColorPrimaryBrush"];
            }

            // Handle countdown no minimize option for Restart dialog
            if (DialogType == DialogType.Restart && _countdownNoMinimizeDuration.HasValue)
            {
                bool canDismiss = _countdownRemainingTime > _countdownNoMinimizeDuration.Value;
                ButtonLeft.IsEnabled = canDismiss;

                // Update the button for accessibility
                if (canDismiss)
                {
                    AutomationProperties.SetHelpText(ButtonLeft, "Minimize the restart dialog");
                }
                else
                {
                    AutomationProperties.SetHelpText(ButtonLeft, "Button disabled, restart imminent");
                }
            }
        }

        /// <summary>
        /// Callback executed by the countdown timer every second. Decrements remaining time, updates display, and handles auto-action on timeout.
        /// </summary>
        /// <param name="state">Timer state object (not used).</param>
        private void CountdownTimerCallback(object? state)
        {
            if (_isDisposed)
                return;

            if (_countdownRemainingTime.TotalSeconds <= 0)
            {
                // Stop the timer
                _countdownTimer?.Change(Timeout.Infinite, Timeout.Infinite);

                // Trigger appropriate action based on dialog type
                if (DialogType == DialogType.Restart)
                {
                    Dispatcher.Invoke(() =>
                    {
                        if (_isDisposed) return;

                        // Auto-click the "Restart Now" button
                        DialogResult = "Restart";
                        CloseDialog(null);
                    });
                }
                else if (DialogType == DialogType.CloseApps)
                {
                    Dispatcher.Invoke(() =>
                    {
                        if (_isDisposed) return;

                        // Auto-click the "Continue" button
                        DialogResult = "Continue";
                        CloseDialog(null);
                    });
                }

                return;
            }

            // Decrement the remaining time
            _countdownRemainingTime = _countdownRemainingTime.Subtract(TimeSpan.FromSeconds(1));

            // Update the display on the UI thread
            try
            {
                Dispatcher.Invoke(UpdateCountdownDisplay);
            }
            catch (Exception ex) when (ex is TaskCanceledException || ex is OperationCanceledException)
            {
                // Application is shutting down, just ignore
            }
        }
    }
}
