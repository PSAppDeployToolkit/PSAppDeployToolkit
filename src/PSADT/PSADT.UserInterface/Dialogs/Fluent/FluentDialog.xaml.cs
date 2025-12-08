using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;
using PSADT.AccountManagement;
using PSADT.LibraryInterfaces;
using PSADT.UserInterface.DialogOptions;
using iNKORE.UI.WPF.Modern;
using iNKORE.UI.WPF.Modern.Controls;
using iNKORE.UI.WPF.Modern.Controls.Primitives;

namespace PSADT.UserInterface.Dialogs.Fluent
{
    /// <summary>
    /// Unified dialog for PSAppDeployToolkit that consolidates all dialog types into one
    /// </summary>
    internal abstract partial class FluentDialog : Window, IDialogBase, INotifyPropertyChanged
    {
        /// <summary>
        /// Static constructor to set up the theme and resources for the dialog.
        /// </summary>
        static FluentDialog()
        {
            Application.Current.Resources.MergedDictionaries.Add(new ThemeResources());
            Application.Current.Resources.MergedDictionaries.Add(new XamlControlsResources());
        }

        /// <summary>
        /// Initializes a new instance of FluentDialog
        /// </summary>
        /// <param name="options">Mandatory options needed to construct the window.</param>
        /// <param name="customMessageText"></param>
        /// <param name="countdownDuration"></param>
        /// <param name="countdownWarningDuration"></param>
        /// <param name="countdownStopwatch"></param>
        private protected FluentDialog(BaseOptions options, string? customMessageText = null, TimeSpan? countdownDuration = null, TimeSpan? countdownWarningDuration = null, Stopwatch? countdownStopwatch = null)
        {
            // Initialize the window
            InitializeComponent();

            // If the accent color is passed through, update via ThemeManager
            if (null != options.FluentAccentColor)
            {
                ThemeManager.Current.AccentColor = IntToColor(options.FluentAccentColor.Value);
            }

            // Set the language and flow direction for the dialog.
            Language = System.Windows.Markup.XmlLanguage.GetLanguage(options.Language.IetfLanguageTag);
            FlowDirection = options.Language.TextInfo.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

            // Set basic properties
            Title = options.AppTitle;
            AppTitleTextBlock.Text = options.AppTitle;
            SubtitleTextBlock.Text = options.Subtitle;

            // Set accessibility properties
            AutomationProperties.SetName(this, options.AppTitle);

            // Set remaining properties from the options
            if (null != options.DialogPosition)
            {
                _dialogPosition = options.DialogPosition.Value;
            }
            if (null != options.DialogAllowMove)
            {
                _dialogAllowMove = options.DialogAllowMove.Value;
            }
            if (_dialogAllowMove)
            {
                MouseLeftButtonDown += (sender, e) => DragMove();
            }
            WindowStartupLocation = WindowStartupLocation.Manual;
            Topmost = options.DialogTopMost;

            // Set supplemental options also
            _customMessageText = customMessageText;
            _countdownDuration = countdownDuration;
            _countdownWarningDuration = countdownWarningDuration;
            _countdownStopwatch = countdownStopwatch ?? new();
            CountdownStackPanel.Visibility = _countdownDuration.HasValue ? Visibility.Visible : Visibility.Collapsed;

            // Pre-format the custom message if we have one
            if (!string.IsNullOrWhiteSpace(_customMessageText))
            {
                FormatMessageWithHyperlinks(CustomMessageTextBlock, _customMessageText!);
                CustomMessageTextBlock.Visibility = Visibility.Visible;
            }
            else
            {
                CustomMessageTextBlock.Visibility = Visibility.Collapsed;
            }

            // Set everything to not visible by default, it's up to the derived class to enable what they need.
            CloseAppsStackPanel.Visibility = Visibility.Collapsed;
            ProgressStackPanel.Visibility = Visibility.Collapsed;
            InputBoxStackPanel.Visibility = Visibility.Collapsed;
            CountdownDeferPanelSeparator.Visibility = Visibility.Collapsed;
            DeferRemainingStackPanel.Visibility = Visibility.Collapsed;
            DeferDeadlineStackPanel.Visibility = Visibility.Collapsed;
            ButtonPanel.Visibility = Visibility.Collapsed;
            ButtonLeft.Visibility = Visibility.Collapsed;
            ButtonMiddle.Visibility = Visibility.Collapsed;
            ButtonRight.Visibility = Visibility.Collapsed;

            // Set up everything related to the dialog icon.
            _dialogBitmapCache = new(new Dictionary<ApplicationTheme, BitmapSource>
            {
                { ApplicationTheme.Light, GetIcon(options.AppIconImage) },
                { ApplicationTheme.Dark, GetIcon(options.AppIconDarkImage) }
            });
            ThemeManager.AddActualThemeChangedHandler(this, (_, _) => SetDialogIcon());
            SetDialogIcon();

            // Set the expiry timer if specified.
            if (null != options.DialogExpiryDuration && options.DialogExpiryDuration.Value != TimeSpan.Zero)
            {
                _expiryTimer = new DispatcherTimer { Interval = options.DialogExpiryDuration.Value };
                _expiryTimer.Tick += (sender, e) => CloseDialog();
            }

            // PersistPrompt timer code.
            if (null != options.DialogPersistInterval && options.DialogPersistInterval.Value != TimeSpan.Zero)
            {
                _persistTimer = new DispatcherTimer { Interval = options.DialogPersistInterval.Value };
                _persistTimer.Tick += PersistTimer_Tick;
            }

            // Initialize countdown if specified
            if (null != _countdownDuration)
            {
                _countdownTimer = new(CountdownTimer_Tick, null, Timeout.Infinite, Timeout.Infinite);
                CountdownStackPanel.Visibility = Visibility.Visible;    
                CountdownDeferPanelSeparator.Visibility = Visibility.Visible;
            }

            // Configure window events
            SystemParameters.StaticPropertyChanged += SystemParameters_StaticPropertyChanged;
            SizeChanged += FluentDialog_SizeChanged;
            Loaded += FluentDialog_Loaded;
        }

        /// <summary>
        /// Redefined ShowDialog method to allow for custom behavior.
        /// </summary>
        public new void ShowDialog() => base.ShowDialog();

        /// <summary>
        /// Closes the dialog window and cancels associated operations. Can be called by timers or button clicks.
        /// </summary>
        public void CloseDialog()
        {
            _canClose = true;
            _persistTimer?.Stop();
            _expiryTimer?.Stop();
            Dispatcher.Invoke(Close);
        }

        /// <summary>
        /// Raises the PropertyChanged event for the specified property.
        /// </summary>
        /// <param name="propertyName"></param>
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new(propertyName));

        /// <summary>
        /// Prevent window movement by handling WM_SYSCOMMAND
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="msg"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <param name="handled"></param>
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == (uint)WINDOW_MESSAGE.WM_SYSCOMMAND && (wParam.ToInt32() & 0xFFF0) == (uint)WM_SYSCOMMAND.SC_MOVE && !_dialogAllowMove)
            {
                handled = true;
            }
            return IntPtr.Zero;
        }

        /// <summary>
        /// Handles the click event of the left button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void ButtonLeft_Click(object sender, RoutedEventArgs e)
        {
            CloseDialog();
        }

        /// <summary>
        /// Handles the click event of the middle button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void ButtonMiddle_Click(object sender, RoutedEventArgs e)
        {
            CloseDialog();
        }

        /// <summary>
        /// Handles the click event of the right button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void ButtonRight_Click(object sender, RoutedEventArgs e)
        {
            CloseDialog();
        }

        /// <summary>
        /// Prevents the user from closing the app via the taskbar
        /// </summary>
        /// <param name="e"></param>
        protected override void OnClosing(CancelEventArgs e) => e.Cancel = !_canClose;

        /// <summary>
        /// Clean up resources when the window is closed
        /// </summary>
        /// <param name="e"></param>
        protected override void OnClosed(EventArgs e)
        {
            _persistTimer?.Stop();
            _expiryTimer?.Stop();
            base.OnClosed(e);
            Dispose();
        }

        /// <summary>
        /// Handles the loaded event of the window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void FluentDialog_Loaded(object sender, RoutedEventArgs e)
        {
            // Force software rendering.
            ((HwndSource)PresentationSource.FromVisual(this)).CompositionTarget.RenderMode = RenderMode.SoftwareOnly;

            // Update dialog layout
            UpdateButtonLayout();
            UpdateLayout();

            // Initialize countdown display if needed
            InitializeCountdown();

            // Update row definitions based on current content
            UpdateRowDefinition();

            // Position the window
            PositionWindow();

            // Record the starting point for the window.
            _startingLeft = Left;
            _startingTop = Top;

            // Start the timers if specified
            _persistTimer?.Start();
            _expiryTimer?.Start();
        }

        /// <summary>
        /// Handles the size changed event of the window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FluentDialog_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Only reposition window - no animations
            PositionWindow();

            // Add hook to prevent window movement
            if (_hwndSource == null)
            {
                WindowInteropHelper helper = new(this);
                _hwndSource = HwndSource.FromHwnd(helper.Handle);
                _hwndSource.AddHook(WndProc);
            }
        }

        /// <summary>
        /// Handles changes to system parameters that may affect window positioning, such as screen size or work area
        /// updates.
        /// </summary>
        /// <remarks>This handler responds to changes in system properties like screen width, height, or
        /// work area, ensuring the window remains correctly positioned when the display configuration
        /// changes.</remarks>
        /// <param name="sender">The source of the event, typically the SystemParameters class.</param>
        /// <param name="e">An object that contains information about the property that changed, including its name.</param>
        private void SystemParameters_StaticPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // Reposition the window if screen dimensions or work area change.
            if (e.PropertyName == nameof(SystemParameters.PrimaryScreenWidth) || e.PropertyName == nameof(SystemParameters.PrimaryScreenHeight) || e.PropertyName == nameof(SystemParameters.WorkArea))
            {
                PositionWindow();
            }
        }

        /// <summary>
        /// Handles the timer tick event for persisting the dialog.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PersistTimer_Tick(object? sender, EventArgs e) => RestoreWindow();

        /// <summary>
        /// Handles the request navigate event of the hyperlink.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            // Use ShellExecute to open the URL in the default browser/handler
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }

        /// <summary>
        /// Formats the message text with enhanced markdown support including hyperlinks, bold, italic, and accent colored.
        /// Supports nested and combined formatting: [url], [accent], [bold], [italic] tags that can be combined for cumulative effects.
        /// </summary>
        /// <param name="textBlock"></param>
        /// <param name="message"></param>
        protected void FormatMessageWithHyperlinks(TextBlock textBlock, string message)
        {
            // Don't waste time on an empty string.
            textBlock.Inlines.Clear();
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            // Use stack-based approach to handle nested/combined formatting
            Stack<FormattingContext> formattingStack = new();
            var lastPos = 0;

            foreach (Match match in DialogConstants.TextFormattingRegex.Matches(message))
            {
                // Add text before the current match with current formatting
                if (match.Index > lastPos)
                {
                    var textContent = message.Substring(lastPos, match.Index - lastPos);
                    AddFormattedText(textBlock, textContent, formattingStack);
                }

                // Process the matched tag
                ProcessFormattingTag(textBlock, match, formattingStack);
                lastPos = match.Index + match.Length;
            }

            // Add any remaining text after the last match
            if (lastPos < message.Length)
            {
                var remainingText = message.Substring(lastPos);
                AddFormattedText(textBlock, remainingText, formattingStack);
            }
        }

        /// <summary>
        /// Processes a formatting tag match and updates the formatting stack.
        /// </summary>
        /// <param name="textBlock">The TextBlock to add content to.</param>
        /// <param name="match">The regex match to process.</param>
        /// <param name="formattingStack">The current formatting context stack.</param>
        private void ProcessFormattingTag(TextBlock textBlock, Match match, Stack<FormattingContext> formattingStack)
        {
            if (match.Groups["UrlLinkSimple"].Success)
            {
                ProcessUrlLink(textBlock, match.Groups["UrlLinkSimpleContent"].Value, match.Groups["UrlLinkSimpleContent"].Value);
            }
            else if (match.Groups["UrlLinkDescriptive"].Success)
            {
                ProcessUrlLink(textBlock, match.Groups["UrlLinkUrl"].Value, match.Groups["UrlLinkDescription"].Value);
            }
            else if (match.Groups["OpenAccent"].Success)
            {
                var newContext = GetCurrentFormattingContext(formattingStack).Clone();
                newContext.IsAccent = true;
                formattingStack.Push(newContext);
            }
            else if (match.Groups["CloseAccent"].Success)
            {
                PopFormattingContext(formattingStack, ctx => ctx.IsAccent);
            }
            else if (match.Groups["OpenBold"].Success)
            {
                var newContext = GetCurrentFormattingContext(formattingStack).Clone();
                newContext.IsBold = true;
                formattingStack.Push(newContext);
            }
            else if (match.Groups["CloseBold"].Success)
            {
                PopFormattingContext(formattingStack, ctx => ctx.IsBold);
            }
            else if (match.Groups["OpenItalic"].Success)
            {
                var newContext = GetCurrentFormattingContext(formattingStack).Clone();
                newContext.IsItalic = true;
                formattingStack.Push(newContext);
            }
            else if (match.Groups["CloseItalic"].Success)
            {
                PopFormattingContext(formattingStack, ctx => ctx.IsItalic);
            }
        }

        /// <summary>
        /// Gets the current formatting context from the stack.
        /// </summary>
        /// <param name="formattingStack">The formatting context stack.</param>
        /// <returns>The current formatting context.</returns>
        private static FormattingContext GetCurrentFormattingContext(Stack<FormattingContext> formattingStack)
        {
            return formattingStack.Count > 0 ? formattingStack.Peek() : new();
        }

        /// <summary>
        /// Pops the most recent formatting context that matches the specified predicate.
        /// </summary>
        /// <param name="formattingStack">The formatting context stack.</param>
        /// <param name="predicate">The condition to match for popping.</param>
        private static void PopFormattingContext(Stack<FormattingContext> formattingStack, Func<FormattingContext, bool> predicate)
        {
            if (formattingStack.Count > 0 && predicate(formattingStack.Peek()))
            {
                formattingStack.Pop();
            }
        }

        /// <summary>
        /// Adds formatted text to the TextBlock based on the current formatting context.
        /// </summary>
        /// <param name="textBlock">The TextBlock to add text to.</param>
        /// <param name="text">The text content to add.</param>
        /// <param name="formattingStack">The current formatting context stack.</param>
        private void AddFormattedText(TextBlock textBlock, string text, Stack<FormattingContext> formattingStack)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            var context = GetCurrentFormattingContext(formattingStack);
            Run run = new(text);

            // Apply formatting based on context
            if (context.IsBold)
            {
                run.FontWeight = FontWeights.Bold;
            }
            if (context.IsItalic)
            {
                run.FontStyle = FontStyles.Italic;
            }
            if (context.IsAccent)
            {
                if (!context.IsBold) // Only set bold if not already bold
                {
                    run.FontWeight = FontWeights.Bold;
                }
                run.SetResourceReference(ForegroundProperty, ThemeKeys.AccentTextFillColorPrimaryBrushKey);
            }

            textBlock.Inlines.Add(run);
        }


        /// <summary>
        /// Creates a hyperlink with the specified URL and display text.
        /// </summary>
        /// <param name="textBlock">The TextBlock to add the hyperlink to.</param>
        /// <param name="url">The URL to navigate to when clicked.</param>
        /// <param name="displayText">The text to display for the hyperlink.</param>
        private void ProcessUrlLink(TextBlock textBlock, string url, string displayText)
        {
            // Ensure the URL has a scheme for Process.Start
            string navigateUrl = url;
            if (!navigateUrl.Contains("://") && !navigateUrl.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
            {
                if (navigateUrl.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ||
                    navigateUrl.StartsWith("ftp.", StringComparison.OrdinalIgnoreCase))
                {
                    navigateUrl = "http://" + navigateUrl;
                }
            }

            // Add the URL as a proper hyperlink
            if (!AccountUtilities.CallerIsSystemInteractive && Uri.TryCreate(navigateUrl, UriKind.Absolute, out var uri))
            {
                Hyperlink link = new(new Run(displayText))
                {
                    NavigateUri = uri,
                    ToolTip = $"Open link: {url}"
                };
                link.RequestNavigate += Hyperlink_RequestNavigate;
                textBlock.Inlines.Add(link);
            }
            else
            {
                // If it's not a valid URI, just add as plain text
                textBlock.Inlines.Add(displayText);
            }
        }

        /// <summary>
        /// Sets the button to be styled with an accent color.
        /// </summary>
        /// <param name="button"></param>
        protected void SetAccentButton(Button button)
        {
            button.SetResourceReference(StyleProperty, ThemeKeys.AccentButtonStyleKey);
        }

        /// <summary>
        /// Sets the button to be the form cancel button.
        /// </summary>
        /// <param name="button"></param>
        protected void SetCancelButton(Button button)
        {
            button.IsCancel = true;
        }

        /// <summary>
        /// Sets the button to be the form default button.
        /// </summary>
        /// <param name="button"></param>
        protected void SetDefaultButton(Button button)
        {
            button.IsDefault = true;
        }

        /// <summary>
        /// Sets the button content with an accelerator key (underscore) for accessibility.
        /// </summary>
        /// <param name="button"></param>
        /// <param name="text"></param>
        protected void SetButtonContentWithAccelerator(Button button, string text)
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
        /// Updates the Grid RowDefinition based on the current content.
        /// </summary>
        protected void UpdateRowDefinition() => CenterPanelRow.Height = new(1, GridUnitType.Auto);

        /// <summary>
        /// Converts a 32-bit integer representation of a color into a <see cref="Color"/> object.
        /// </summary>
        /// <remarks>The integer is interpreted as an ARGB value, where the most significant byte represents the alpha channel, followed by the red, green, and blue channels in order.</remarks>
        /// <param name="color">A 32-bit integer where each byte represents a component of the color in ARGB order.</param>
        /// <returns>A <see cref="Color"/> object corresponding to the specified integer value.</returns>
        private static Color IntToColor(int color)
        {
            var colorBytes = BitConverter.GetBytes(color);
            return Color.FromArgb(colorBytes[3], colorBytes[2], colorBytes[1], colorBytes[0]);
        }

        /// <summary>
        /// Retrieves a bitmap representation of the icon specified by the given file path.
        /// </summary>
        /// <remarks>The method caches the retrieved icons to improve performance on subsequent calls with
        /// the same file path. If the icon or image can be frozen, it is made shareable across threads.</remarks>
        /// <param name="dialogIconPath">The absolute file path to the icon. This can be a path to an .ico file or another image format.</param>
        /// <returns>A <see cref="BitmapSource"/> representing the icon. If the icon is an .ico file, the highest resolution
        /// frame is returned.</returns>
        private static BitmapSource GetIcon(string dialogIconPath)
        {
            // Try to get from cache first.
            if (!_dialogIconCache.TryGetValue(dialogIconPath, out var bitmapSource))
            {
                // Nothing cached. If we have an icon, get the highest resolution frame.
                if (Path.GetExtension(dialogIconPath).Equals(".ico", StringComparison.OrdinalIgnoreCase))
                {
                    // Use IconBitmapDecoder to get the icon frame.
                    var iconFrame = new IconBitmapDecoder(new Uri(dialogIconPath, UriKind.Absolute), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad).Frames.OrderByDescending(f => f.PixelWidth * f.PixelHeight).First();

                    // Make it shareable across threads
                    if (iconFrame.CanFreeze)
                    {
                        iconFrame.Freeze();
                    }
                    _dialogIconCache.Add(dialogIconPath, iconFrame);
                    bitmapSource = iconFrame;
                }
                else
                {
                    // Use BeginInit/EndInit pattern for better performance.
                    BitmapImage bitmapImage = new();
                    bitmapImage.BeginInit();
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.UriSource = new(dialogIconPath, UriKind.Absolute);
                    bitmapImage.EndInit();

                    // Make it shareable across threads
                    if (bitmapImage.CanFreeze)
                    {
                        bitmapImage.Freeze();
                    }
                    _dialogIconCache.Add(dialogIconPath, bitmapImage);
                    bitmapSource = bitmapImage;
                }
            }
            return bitmapSource;
        }

        /// <summary>
        /// Sets the icon for the dialog using the specified bitmap source.
        /// </summary>
        /// <remarks>This method updates both the dialog's window icon and any associated UI element
        /// displaying the application icon.</remarks>
        private void SetDialogIcon() => Icon = AppIconImage.Source = _dialogBitmapCache[ThemeManager.Current.ActualApplicationTheme];

        /// <summary>
        /// Positions the window on the screen based on the specified dialog position.
        /// </summary>
        private void PositionWindow()
        {
            // Get the working area in DIPs.
            Rect workingArea = SystemParameters.WorkArea;

            // Calculate positions based on window position setting.
            double left, top;
            switch (_dialogPosition)
            {
                case DialogPosition.TopLeft:
                    // Align to top left corner
                    left = workingArea.Left;
                    top = workingArea.Top;
                    break;

                case DialogPosition.Top:
                    // Center horizontally, align to top
                    left = workingArea.Left + ((workingArea.Width - ActualWidth) / 2);
                    top = workingArea.Top;
                    break;

                case DialogPosition.TopRight:
                    // Align to top right corner
                    left = workingArea.Left + (workingArea.Width - ActualWidth);
                    top = workingArea.Top;
                    break;

                case DialogPosition.TopCenter:
                    // Center horizontally, align to top but not to the top of the screen
                    left = workingArea.Left + ((workingArea.Width - ActualWidth) / 2);
                    top = workingArea.Top + ((workingArea.Height - ActualHeight) * (1.0 / 6.0));
                    break;

                case DialogPosition.Center:
                    // Center horizontally and vertically
                    left = workingArea.Left + ((workingArea.Width - ActualWidth) / 2);
                    top = workingArea.Top + ((workingArea.Height - ActualHeight) / 2);
                    break;

                case DialogPosition.BottomLeft:
                    // Align to bottom left corner
                    left = workingArea.Left;
                    top = workingArea.Top + (workingArea.Height - ActualHeight);
                    break;

                case DialogPosition.Bottom:
                    // Center horizontally, align to bottom
                    left = workingArea.Left + ((workingArea.Width - ActualWidth) / 2);
                    top = workingArea.Top + (workingArea.Height - ActualHeight);
                    break;

                case DialogPosition.BottomCenter:
                    // Center horizontally, align to bottom but not to the bottom of the screen
                    left = workingArea.Left + ((workingArea.Width - ActualWidth) / 2);
                    top = workingArea.Top + ((workingArea.Height - ActualHeight) * (5.0 / 6.0));
                    break;

                case DialogPosition.BottomRight:
                case DialogPosition.Default:
                default:
                    // Align to bottom right (original behavior)
                    left = workingArea.Left + (workingArea.Width - ActualWidth);
                    top = workingArea.Top + (workingArea.Height - ActualHeight);
                    break;
            }

            // Ensure the window is within the screen bounds.
            left = Math.Max(workingArea.Left, Math.Min(left, workingArea.Right - ActualWidth));
            top = Math.Max(workingArea.Top, Math.Min(top, workingArea.Bottom - ActualHeight));

            // Align positions to whole pixels.
            left = Math.Floor(left);
            top = Math.Floor(top);

            // Adjust for workArea offset.
            string dialogPosName = _dialogPosition.ToString();
            left -= _dialogPosition == DialogPosition.Default || dialogPosName.EndsWith("Right") ? 18 : dialogPosName.EndsWith("Left") ? -18 : 0;
            top -= _dialogPosition == DialogPosition.Default || dialogPosName.StartsWith("Bottom") ? 14 : dialogPosName.StartsWith("Top") ? -14 : 0;

            // Set positions in DIPs.
            Left = _startingLeft = left;
            Top = _startingTop = top;
        }

        /// <summary>
        /// Restores the window to its normal state and repositions it to its original location.
        /// </summary>
        protected void RestoreWindow()
        {
            // Reset the window and restore its location.
            WindowState = WindowState.Normal;
            Left = _startingLeft;
            Top = _startingTop;
        }

        /// <summary>
        /// Sets the minimize button availability based on specific conditions.
        /// </summary>
        /// <param name="availability">The desired button availability state.</param>
        protected void SetMinimizeButtonAvailability(TitleBarButtonAvailability availability)
        {
            TitleBar.SetMinimizeButtonAvailability(this, availability);
        }

        /// <summary>
        /// Updates the layout of the action buttons based on which buttons are visible.
        /// </summary>
        private void UpdateButtonLayout()
        {
            // Build a list of visible buttons in the order they appear.
            List<UIElement> visibleButtons = [];
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

            // Return early if there's no buttons.
            if (visibleButtons.Count == 0)
            {
                return;
            }

            // Special case: if there's only one visible button, limit its width to half of the grid
            if (visibleButtons.Count > 1)
            {
                // Create equally sized columns for each visible button (original behavior)
                for (int i = 0; i < visibleButtons.Count; i++)
                {
                    // Set margin based on position
                    ActionButtons.ColumnDefinitions.Add(new ColumnDefinition { Width = new(1, GridUnitType.Star) });
                    Grid.SetColumn(visibleButtons[i], i);
                    Button button = (Button)visibleButtons[i];
                    if (i == 0)
                    {
                        button.Margin = new(0, 0, 4, 0);
                    }
                    else if (i == visibleButtons.Count - 1)
                    {
                        button.Margin = new(4, 0, 0, 0);
                    }
                    else
                    {
                        button.Margin = new(4, 0, 4, 0);
                    }
                }
            }
            else
            {
                // Add two columns - one for the button (50% width) and one empty (50% width)
                ActionButtons.ColumnDefinitions.Add(new ColumnDefinition { Width = new(1, GridUnitType.Star) });
                ActionButtons.ColumnDefinitions.Add(new ColumnDefinition { Width = new(1, GridUnitType.Star) });

                // Place the single button in the second column
                Grid.SetColumn(visibleButtons[0], 1);

                // Set appropriate margin
                Button button = (Button)visibleButtons[0];
                button.Margin = new(0, 0, 0, 0);

                // Set this to be the default button with accent
                SetDefaultButton(button);
                SetAccentButton(button);
            }
        }

        /// <summary>
        /// Initializes the countdown timer and display for dialogs that support it (CloseApps, Restart).
        /// </summary>
        private void InitializeCountdown()
        {
            // Return early if there's no countdown to set.
            if (null == _countdownTimer)
            {
                return;
            }
            if (!_countdownStopwatch.IsRunning)
            {
                _countdownStopwatch.Start();
            }
            _countdownTimer.Change(0, 1000);
        }

        /// <summary>
        /// Updates the countdown text display and adjusts text color based on remaining time.
        /// Handles disabling the dismiss button for Restart dialogs based on `countdownNoMinimizeDuration`.
        /// </summary>
        private void UpdateCountdownDisplay()
        {
            // Get the current remaining time.
            if (_countdownDuration is null)
            {
                return;
            }
            _countdownRemainingTime = _countdownDuration.Value - _countdownStopwatch.Elapsed;
            if (_countdownRemainingTime < TimeSpan.Zero)
            {
                _countdownRemainingTime = TimeSpan.Zero;
            }

            // Format the remaining time as hh:mm:ss
            CountdownValueTextBlock.Text = $"{_countdownRemainingTime.Hours}h {_countdownRemainingTime.Minutes}m {_countdownRemainingTime.Seconds}s";
            AutomationProperties.SetName(CountdownValueTextBlock, $"Time remaining: {_countdownRemainingTime.Hours} hours, {_countdownRemainingTime.Minutes} minutes, {_countdownRemainingTime.Seconds} seconds");

            // Update text color based on remaining time using style application
            if (_countdownRemainingTime.TotalSeconds <= 60)
            {
                CountdownValueTextBlock.Style = (Style)FindResource("CriticalTextBlockStyle");
            }
            else if (_countdownWarningDuration.HasValue && _countdownRemainingTime <= _countdownWarningDuration.Value)
            {
                CountdownValueTextBlock.Style = (Style)FindResource("CautionTextBlockStyle");
            }
        }

        /// <summary>
        /// Callback executed by the countdown timer every second. Decrements remaining time, updates display, and handles auto-action on timeout.
        /// </summary>
        /// <param name="state">Timer state object (not used).</param>
        protected virtual void CountdownTimer_Tick(object? state) => Dispatcher.Invoke(UpdateCountdownDisplay);

        /// <summary>
        /// The result of the dialog interaction.
        /// </summary>
        public new virtual object DialogResult
        {
            get => _dialogResult;
            private protected set
            {
                _dialogResult = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// An optional custom message to display.
        /// </summary>
        protected readonly string? _customMessageText;

        /// <summary>
        /// The cancellation token source for the dialog.
        /// </summary>
        private object _dialogResult = "Timeout";

        /// <summary>
        /// Whether this window has been disposed.
        /// </summary>
        protected bool _disposed { get; private set; } = false;

        /// <summary>
        /// Whether this window is able to be closed.
        /// </summary>
        private bool _canClose = false;

        /// <summary>
        /// The specified position of the dialog.
        /// </summary>
        private readonly DialogPosition _dialogPosition = DialogPosition.BottomRight;

        /// <summary>
        /// Whether the dialog is allowed to be moved.
        /// </summary>
        private readonly bool _dialogAllowMove = false;

        /// <summary>
        /// The countdown duration for the dialog.
        /// </summary>
        private readonly Timer? _countdownTimer;

        /// <summary>
        /// An optional countdown to zero to commence a preferred action.
        /// </summary>
        protected readonly TimeSpan? _countdownDuration;

        /// <summary>
        /// An optional countdown to zero for when the dialog can be no longer minimized.
        /// </summary>
        protected readonly TimeSpan? _countdownWarningDuration;

        /// <summary>
        /// The end date/time for the countdown duration, as determined during form load.
        /// </summary>
        protected readonly Stopwatch _countdownStopwatch;

        /// <summary>
        /// Represents the remaining time in a countdown.
        /// </summary>
        protected TimeSpan _countdownRemainingTime;

        /// <summary>
        /// A timer used to close the dialog at a configured interval after no user response.
        /// </summary>
        private readonly DispatcherTimer? _expiryTimer;

        /// <summary>
        /// A timer used to restore the dialog's position on the screen at a configured interval.
        /// </summary>
        private readonly DispatcherTimer? _persistTimer;

        /// <summary>
        /// Represents the initial top position of an element or object.
        /// </summary>
        private double _startingTop;

        /// <summary>
        /// Represents the initial left position of the window.
        /// </summary>
        private double _startingLeft;

        /// <summary>
        /// Represents the underlying window handle source for a WPF application.
        /// </summary>
        /// <remarks>This field is used to manage the interoperation between WPF and Win32 by providing
        /// access to the window handle source. It is typically used in scenarios involving advanced window management
        /// or interoperation with native code.</remarks>
        private HwndSource? _hwndSource;

        /// <summary>
        /// A read-only dictionary that caches dialog icons for different application themes.
        /// </summary>
        /// <remarks>This dictionary maps <see cref="ApplicationTheme"/> values to their corresponding
        /// <see cref="BitmapSource"/> icons. It is intended to optimize access to preloaded icons for dialogs, ensuring
        /// consistent and efficient retrieval.</remarks>
        private readonly ReadOnlyDictionary<ApplicationTheme, BitmapSource> _dialogBitmapCache; 

        /// <summary>
        /// Dialog icon cache for improved performance
        /// </summary>
        private static readonly Dictionary<string, BitmapSource> _dialogIconCache = [];

        /// <summary>
        /// Event handler for when a window property has changed.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Dispose managed resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose managed and unmanaged resources
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }
            if (disposing)
            {
                // Remove event handlers.
                SystemParameters.StaticPropertyChanged -= SystemParameters_StaticPropertyChanged;
                SizeChanged -= FluentDialog_SizeChanged;
                Loaded -= FluentDialog_Loaded;

                // Remove timer event handlers if they exist.
                if (_expiryTimer != null)
                {
                    _expiryTimer.Tick -= (sender, e) => CloseDialog();
                    _expiryTimer.Stop();
                }
                if (_persistTimer != null)
                {
                    _persistTimer.Tick -= PersistTimer_Tick;
                    _persistTimer.Stop();
                }

                // Clean up resources.
                ThemeManager.RemoveActualThemeChangedHandler(this, (_, _) => SetDialogIcon());
                _hwndSource?.RemoveHook(WndProc);
                _countdownTimer?.Dispose();
            }
            _disposed = true;
        }

        /// <summary>
        /// Represents the formatting context for nested text formatting.
        /// </summary>
        private sealed class FormattingContext
        {
            public bool IsAccent { get; set; }
            public bool IsBold { get; set; }
            public bool IsItalic { get; set; }

            public FormattingContext Clone()
            {
                return new FormattingContext
                {
                    IsAccent = IsAccent,
                    IsBold = IsBold,
                    IsItalic = IsItalic
                };
            }
        }
    }
}
