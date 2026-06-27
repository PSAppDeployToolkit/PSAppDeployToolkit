using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
using PSADT.DeviceManagement;
using PSADT.Foundation;
using PSADT.UserInterface.DialogOptions;
using PSADT.Utilities;
using PSADT.WindowManagement;
using Windows.Win32.Foundation;
using Fluence.Wpf;
using Fluence.Wpf.Controls;

namespace PSADT.UserInterface.Interfaces.Fluent
{
    /// <summary>
    /// Unified dialog for PSAppDeployToolkit that consolidates all dialog types into one
    /// </summary>
    internal abstract partial class FluentDialog : FluenceWindow, IBaseDialog
    {
        /// <summary>
        /// Initializes a new instance of the FluentDialog class with the specified dialog options, result, and optional
        /// settings for custom messaging and countdown behavior.
        /// </summary>
        /// <remarks>This constructor is intended for use by derived dialog classes to provide flexible
        /// configuration of dialog appearance and behavior. Most dialog features are initialized to be hidden by
        /// default; derived classes should enable the features they require. The countdown timer and custom message are
        /// only shown if their respective parameters are provided.</remarks>
        /// <param name="options">The options that configure the dialog's appearance, language, accent color, positioning, accessibility, and
        /// icon settings.</param>
        /// <param name="dialogResult">The result value to be returned when the dialog is closed, typically indicating the user's selection or
        /// action.</param>
        /// <param name="customMessageText">An optional custom message to display in the dialog. May include hyperlinks and is shown if provided.</param>
        /// <param name="countdownDuration">An optional duration for a countdown timer. If specified, the dialog will display a countdown and may close
        /// automatically when the timer expires.</param>
        /// <param name="countdownWarningDuration">An optional duration for a warning before the countdown expires, indicating the impending closure of the
        /// dialog.</param>
        /// <param name="countdownStopwatch">An optional Stopwatch instance used to track the countdown duration. If not provided, a new Stopwatch is
        /// created.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0056:Do not call overridable members in constructor", Justification = "This is OK here.")]
        private protected FluentDialog(BaseDialogOptions options, IDialogResult dialogResult, string? customMessageText = null, TimeSpan? countdownDuration = null, TimeSpan? countdownWarningDuration = null, Stopwatch? countdownStopwatch = null)
        {
            // Confirm nullable input is valid before proceeding.
            if (customMessageText is not null)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(customMessageText);
            }

            // Initialize the window
            InitializeComponent();

            // Set up everything related to the dialog accent.
            Color? accentColorDark = options.FluentAccentColorDark is not null ? IntToColor(options.FluentAccentColorDark.Value) : null;
            Color? accentColor = options.FluentAccentColor is not null ? IntToColor(options.FluentAccentColor.Value) : null;
            _dialogAccentCache = new(new Dictionary<ApplicationTheme, Color?>
            {
                { ApplicationTheme.Light, accentColor },
                { ApplicationTheme.Dark, accentColorDark ?? accentColor },
                { ApplicationTheme.HighContrast, accentColorDark ?? accentColor },
                { ApplicationTheme.Auto, accentColor },
            });

            // Set the language and flow direction for the dialog.
            Language = System.Windows.Markup.XmlLanguage.GetLanguage(options.Language.IetfLanguageTag);
            FlowDirection = options.Language.TextInfo.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

            // Set basic properties
            Title = options.AppTitle;
            AppTitleTextBlock.Text = options.AppTitle;
            SubtitleTextBlock.Text = options.Subtitle;

            // Set remaining properties from the options
            if (options.DialogPosition is not null)
            {
                _dialogPosition = options.DialogPosition.Value;
            }
            else if (!DeviceUtilities.IsOOBEComplete())
            {
                _dialogPosition = DialogPosition.Oobe;
            }
            if (options.DialogAllowMove is not null)
            {
                _dialogAllowMove = options.DialogAllowMove.Value;
            }
            IsMoveable = _dialogAllowMove;
            if (options.DialogAllowMinimize is not null)
            {
                _dialogMinimizeVisible = options.DialogAllowMinimize.Value;
            }
            IsMinimizeButtonVisible = _dialogMinimizeVisible ? Visibility.Visible : Visibility.Collapsed;
            WindowStartupLocation = WindowStartupLocation.Manual;

            // Park the window far off every monitor before any layout or show. SizeToContent,
            // FluentDialog_SizeChanged, FD.Loaded, and PositionWindow calls will all no-op or
            // operate on this off-screen position until OnContentRendered clears _firstShowPending
            // and places the window on-screen in one step. The FluenceWindow cloak (belt-and-braces)
            // will have already hidden the window by the time it is moved on-screen.
            Left = OffscreenCoordinate;
            Top = OffscreenCoordinate;
            Topmost = options.DialogTopMost;

            // Set supplemental options also
            _customMessageText = customMessageText;
            _countdownDuration = countdownDuration;
            _countdownWarningDuration = countdownWarningDuration;
            _countdownStopwatch = countdownStopwatch ?? new();
            CountdownStackPanel.Visibility = _countdownDuration.HasValue ? Visibility.Visible : Visibility.Collapsed;

            // Pre-format the custom message if we have one
            if (_customMessageText is not null && !string.IsNullOrWhiteSpace(_customMessageText))
            {
                FormatMessageWithHyperlinks(CustomMessageTextBlock, _customMessageText);
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
            ListSelectionStackPanel.Visibility = Visibility.Collapsed;
            CountdownDeferPanelSeparator.Visibility = Visibility.Collapsed;
            DeferRemainingStackPanel.Visibility = Visibility.Collapsed;
            DeferDeadlineStackPanel.Visibility = Visibility.Collapsed;
            ButtonPanel.Visibility = Visibility.Collapsed;
            ButtonLeft.Visibility = Visibility.Collapsed;
            ButtonMiddle.Visibility = Visibility.Collapsed;
            ButtonRight.Visibility = Visibility.Collapsed;

            // Set up the app's tray icon if an override has been specified.
            if (options.AppTaskbarIconImage is not null)
            {
                Icon = _appTaskbarIcon = GetIcon(options.AppTaskbarIconImage);
            }

            // Set up everything related to the dialog icon.
            _dialogBitmapCache = new(new Dictionary<ApplicationTheme, BitmapSource>
            {
                { ApplicationTheme.Light, GetIcon(options.AppIconImage) },
                { ApplicationTheme.Dark, GetIcon(options.AppIconDarkImage ?? options.AppIconImage) },
                { ApplicationTheme.HighContrast, GetIcon(options.AppIconDarkImage ?? options.AppIconImage) },
                { ApplicationTheme.Auto, GetIcon(options.AppIconImage) },

            });

            // Initialize the theme and accent color for the dialog based on the provided options, defaulting to automatic theming and accent if not specified.
            ApplicationThemeManager.Changed += ThemeManager_ActualThemeChanged;
            ApplicationThemeManager.Apply(ApplicationTheme.Auto);

            // Set the expiry timer if specified.
            if (options.DialogExpiryDuration > TimeSpan.Zero)
            {
                _expiryTimer = new() { Interval = options.DialogExpiryDuration.Value };
                _expiryTimer.Tick += ExpiryTimer_Tick;
            }

            // PersistPrompt timer code.
            if (options.DialogPersistInterval > TimeSpan.Zero)
            {
                _persistTimer = new() { Interval = options.DialogPersistInterval.Value };
                _persistTimer.Tick += PersistTimer_Tick;
            }

            // Initialize countdown if specified
            if (_countdownDuration is not null)
            {
                _countdownTimer = new() { Interval = TimeSpan.FromSeconds(1) };
                _countdownTimer.Tick += (s, e) => CountdownTimer_Tick(state: null);
                CountdownStackPanel.Visibility = Visibility.Visible;
                CountdownDeferPanelSeparator.Visibility = Visibility.Visible;
            }

            // Configure window events
            SystemParameters.StaticPropertyChanged += SystemParameters_StaticPropertyChanged;
            SizeChanged += FluentDialog_SizeChanged;
            Loaded += FluentDialog_Loaded;

            // Set the initial dialog result to the provided value, which may be updated by button clicks or timers.
            DialogResult = dialogResult;
        }

        /// <summary>
        /// Redefined ShowDialog method to allow for custom behavior.
        /// </summary>
        public new void ShowDialog()
        {
            _ = base.ShowDialog();
        }

        /// <summary>
        /// Closes the dialog window and cancels associated operations. Can be called by timers or button clicks.
        /// </summary>
        public void CloseDialog()
        {
            _canClose = true;
            _persistTimer?.Stop();
            _expiryTimer?.Stop();
            Close();
        }

        /// <summary>
        /// Handles the click event for the left button and closes the dialog.
        /// </summary>
        /// <remarks>Override this method to customize the behavior when the left button is clicked. This
        /// method is commonly used in dialog interfaces to respond to user actions.</remarks>
        /// <param name="sender">The source of the event, typically the button that was clicked.</param>
        /// <param name="e">The event data associated with the click event.</param>
        private protected virtual void ButtonLeft_Click(object? sender, RoutedEventArgs e)
        {
            CloseDialog();
        }

        /// <summary>
        /// Handles the click event for the middle button and closes the dialog.
        /// </summary>
        /// <remarks>Override this method in a derived class to implement custom behavior when the middle
        /// button is clicked.</remarks>
        /// <param name="sender">The source of the event, typically the button that was clicked.</param>
        /// <param name="e">The event data associated with the click event.</param>
        private protected virtual void ButtonMiddle_Click(object? sender, RoutedEventArgs e)
        {
            CloseDialog();
        }

        /// <summary>
        /// Handles the Click event for the right button of the dialog, typically closing the dialog or performing a
        /// related action.
        /// </summary>
        /// <remarks>Override this method in a derived class to implement custom behavior when the right
        /// button is clicked.</remarks>
        /// <param name="sender">The source of the event, usually the right button that was clicked.</param>
        /// <param name="e">The event data associated with the button click.</param>
        private protected virtual void ButtonRight_Click(object? sender, RoutedEventArgs e)
        {
            CloseDialog();
        }

        /// <summary>
        /// Invoked when the window is about to close, allowing the close operation to be canceled based on the current
        /// state.
        /// </summary>
        /// <remarks>Override this method to implement custom logic that determines whether the window can
        /// be closed. If the window cannot be closed, set <see cref="CancelEventArgs.Cancel"/> to <see
        /// langword="true"/> to cancel the operation.</remarks>
        /// <param name="e">A <see cref="CancelEventArgs"/> that contains the event data for the closing event. Set <see
        /// cref="CancelEventArgs.Cancel"/> to <see langword="true"/> to prevent the window from closing.</param>
        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = !_canClose;
        }

        /// <summary>
        /// Handles the event that occurs when the window is closed, ensuring that resources are released and timers are
        /// stopped.
        /// </summary>
        /// <remarks>This method overrides the base implementation to perform additional cleanup, such as
        /// stopping active timers and disposing of resources, when the window is closed.</remarks>
        /// <param name="e">An object that contains the event data associated with the window closing event.</param>
        protected override void OnClosed(EventArgs e)
        {
            _persistTimer?.Stop();
            _expiryTimer?.Stop();
            base.OnClosed(e);
            Dispose();
        }

        /// <inheritdoc />
        protected override void OnSourceInitialized(EventArgs e)
        {
            // FluenceWindow.OnSourceInitialized applies the window shell (chrome, opaque/backdrop
            // background, frame) before the first paint.
            base.OnSourceInitialized(e);

            // Configure all content-dependent layout and position the window BEFORE the first paint
            // so the first composed frame is already settled - no visible jump from a default
            // position, and no pre-arrangement flash of the buttons / content rows. This runs after
            // the full object graph is constructed (base plus subclass constructors, which set each
            // dialog's button visibility and content) but before the window renders. Forcing one
            // layout pass here gives PositionWindow the final ActualWidth / ActualHeight; the wired
            // SizeChanged handler still repositions if the content size changes later (e.g. the
            // CloseApps list updating).
            AutomationProperties.SetName(this, Title);
            UpdateButtonLayout();
            UpdateRowDefinition();
            UpdateLayout();
            // PositionWindow is suppressed while _firstShowPending=true; the window stays
            // off-screen at OffscreenCoordinate. _startingLeft/_startingTop will be set to
            // the on-screen coords by the reveal-time PositionWindow call in OnContentRendered.
            PositionWindow();

            // Pre-populate the countdown display so the first rendered frame shows the real
            // remaining time (full duration) rather than the XAML-default "00:00:00". The
            // _countdownStopwatch has not started yet at this point (InitializeCountdown starts
            // it at Loaded), so Elapsed == 0 and remaining == full duration -- the correct value.
            // UpdateCountdownDisplay guards against null _countdownDuration internally. This
            // does NOT start the timer; it only sets the text block once for the first frame.
            // InitializeCountdown (called from Loaded) still owns timer start and the tick loop.
            UpdateCountdownDisplay();
        }

        /// <inheritdoc />
        protected override void OnContentRendered(EventArgs e)
        {
            // First-show reveal: clear the off-screen hold, position the window on-screen, then
            // let the base (FluenceWindow.OnContentRendered -> RevealAfterFirstPaint) uncloak or
            // un-alpha the window. The ordering is deliberate:
            //   1. Clear _firstShowPending so PositionWindow computes the real on-screen coords.
            //   2. Call PositionWindow -- moves the HWND to its final position while the
            //      FluenceWindow cloak still holds the window invisible (belt-and-braces).
            //   3. base.OnContentRendered -> RevealAfterFirstPaint -> uncloak.
            //   Result: the window appears fully-formed at its final position in one step.
            //
            // _startingLeft/_startingTop are set inside PositionWindow (Left = _startingLeft = left)
            // so RestoreWindow will correctly restore to the on-screen position, not OffscreenCoordinate.
            if (_firstShowPending)
            {
                _firstShowPending = false;
                PositionWindow();
            }
            base.OnContentRendered(e);
        }

        /// <summary>
        /// Handles the Loaded event for the dialog. Layout, button arrangement, and window
        /// positioning now run in <see cref="OnSourceInitialized"/> (before the first paint) so the
        /// first composed frame is already settled; only genuine post-show concerns remain here.
        /// </summary>
        /// <param name="sender">The source of the event, typically the dialog instance being loaded.</param>
        /// <param name="e">A RoutedEventArgs object that contains the event data.</param>
        private protected virtual void FluentDialog_Loaded(object? sender, RoutedEventArgs e)
        {
            // Post-show concerns only: start the countdown and persist/expiry timers, signal the
            // client-server success flag the caller may be awaiting, and bring the realised window
            // to the front.
            InitializeCountdown();
            _persistTimer?.Start();
            _expiryTimer?.Start();
            ClientServerUtilities.SetOperationSuccessFlag();
            try
            {
                WindowTools.BringWindowToFront((HWND)new WindowInteropHelper(this).Handle);
            }
            catch
            {
                // Best-effort: failing to raise the window must never abort dialog display.
                return;
                throw;
            }
        }

        /// <summary>
        /// Handles the SizeChanged event for the FluentDialog window, repositioning the window as needed.
        /// </summary>
        /// <remarks>This method repositions the window without animations.</remarks>
        /// <param name="sender">The source of the event, typically the FluentDialog instance whose size has changed.</param>
        /// <param name="e">An object that contains the event data, including information about the new size of the window.</param>
        private void FluentDialog_SizeChanged(object? sender, SizeChangedEventArgs e)
        {
            // Only reposition window - no animations
            PositionWindow();
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
            if (e.PropertyName.Equals(nameof(SystemParameters.PrimaryScreenWidth), StringComparison.Ordinal) || e.PropertyName.Equals(nameof(SystemParameters.PrimaryScreenHeight), StringComparison.Ordinal) || e.PropertyName.Equals(nameof(SystemParameters.WorkArea), StringComparison.Ordinal))
            {
                PositionWindow();
            }
        }

        /// <summary>
        /// Handles the timer tick event to restore the application window state.
        /// </summary>
        /// <remarks>This method is intended to be used as an event handler for a timer's Tick event,
        /// ensuring the application window is restored at regular intervals.</remarks>
        /// <param name="sender">The source of the event, typically the timer that triggered the event.</param>
        /// <param name="e">An object that contains the event data.</param>
        private void ExpiryTimer_Tick(object? sender, EventArgs e)
        {
            CloseDialog();
        }

        /// <summary>
        /// Handles the event that occurs when the application theme changes, updating the dialog icon accordingly.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data associated with the theme change.</param>
        private void ThemeManager_ActualThemeChanged(object? sender, ThemeChangedEventArgs e)
        {
            SetDialogAccent(e.Theme);
            SetDialogIcon(e.Theme);
        }

        /// <summary>
        /// Handles the timer tick event to restore the application window state.
        /// </summary>
        /// <remarks>This method is intended to be used as an event handler for a timer's Tick event,
        /// ensuring the application window is restored at regular intervals.</remarks>
        /// <param name="sender">The source of the event, typically the timer that triggered the event.</param>
        /// <param name="e">An object that contains the event data.</param>
        private void PersistTimer_Tick(object? sender, EventArgs e)
        {
            RestoreWindow();
        }

        /// <summary>
        /// Handles a navigation request from a hyperlink by opening the specified URI in the default web browser.
        /// </summary>
        /// <remarks>This method uses the system's default handler to open the URI, ensuring the link is
        /// launched in the user's preferred web browser. The event is marked as handled to prevent further processing
        /// by other handlers.</remarks>
        /// <param name="sender">The source of the event, typically the hyperlink that was clicked.</param>
        /// <param name="e">The event data containing information about the navigation request, including the target URI.</param>
        private static void Hyperlink_RequestNavigate(object? sender, RequestNavigateEventArgs e)
        {
            // Use ShellExecute to open the URL in the default browser/handler
            using Process? process = Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }

        /// <summary>
        /// Applies hyperlink and text formatting to the specified message and updates the provided TextBlock with the
        /// formatted content.
        /// </summary>
        /// <remarks>This method processes the message for formatting tags and applies the corresponding
        /// styles, including hyperlinks, to the TextBlock. Nested and combined formatting tags are supported. The
        /// method does not perform any action if the message is null or whitespace.</remarks>
        /// <param name="textBlock">The TextBlock control to which the formatted message will be applied. This control is cleared before new
        /// content is added.</param>
        /// <param name="message">The message string containing text and formatting tags to be processed. If the message is null or consists
        /// only of whitespace, no formatting is applied.</param>
        private protected static void FormatMessageWithHyperlinks(System.Windows.Controls.TextBlock textBlock, string message)
        {
            // Don't waste time on an empty string.
            textBlock.Inlines.Clear();
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            // Use stack-based approach to handle nested/combined formatting
            Stack<FormattingContext> formattingStack = new();
            int lastPos = 0;

            foreach (Match match in DialogText.FormattingRegex.Matches(message))
            {
                // Add text before the current match with current formatting
                if (match.Index > lastPos)
                {
                    string textContent = message[lastPos..match.Index];
                    AddFormattedText(textBlock, textContent, formattingStack);
                }

                // Process the matched tag
                ProcessFormattingTag(textBlock, match, formattingStack);
                lastPos = match.Index + match.Length;
            }

            // Add any remaining text after the last match
            if (lastPos < message.Length)
            {
                string remainingText = message[lastPos..];
                AddFormattedText(textBlock, remainingText, formattingStack);
            }
        }

        /// <summary>
        /// Processes a formatting tag match and updates the formatting stack.
        /// </summary>
        /// <param name="textBlock">The TextBlock to add content to.</param>
        /// <param name="match">The regex match to process.</param>
        /// <param name="formattingStack">The current formatting context stack.</param>
        private static void ProcessFormattingTag(System.Windows.Controls.TextBlock textBlock, Match match, Stack<FormattingContext> formattingStack)
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
                FormattingContext newContext = GetCurrentFormattingContext(formattingStack).Clone();
                newContext.IsAccent = true;
                formattingStack.Push(newContext);
            }
            else if (match.Groups["CloseAccent"].Success)
            {
                PopFormattingContext(formattingStack, ctx => ctx.IsAccent);
            }
            else if (match.Groups["OpenBold"].Success)
            {
                FormattingContext newContext = GetCurrentFormattingContext(formattingStack).Clone();
                newContext.IsBold = true;
                formattingStack.Push(newContext);
            }
            else if (match.Groups["CloseBold"].Success)
            {
                PopFormattingContext(formattingStack, ctx => ctx.IsBold);
            }
            else if (match.Groups["OpenItalic"].Success)
            {
                FormattingContext newContext = GetCurrentFormattingContext(formattingStack).Clone();
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
                _ = formattingStack.Pop();
            }
        }

        /// <summary>
        /// Adds formatted text to the TextBlock based on the current formatting context.
        /// </summary>
        /// <param name="textBlock">The TextBlock to add text to.</param>
        /// <param name="text">The text content to add.</param>
        /// <param name="formattingStack">The current formatting context stack.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0030:Do not use banned APIs", Justification = "This is specifically allowed here.")]
        private static void AddFormattedText(System.Windows.Controls.TextBlock textBlock, string text, Stack<FormattingContext> formattingStack)
        {
            // Check for null only, not whitespace - we need to preserve whitespace-only
            // content (including line breaks) between formatting tags.
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            FormattingContext context = GetCurrentFormattingContext(formattingStack);
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
                run.SetResourceReference(ForegroundProperty, "AccentTextFillColorPrimaryBrush");
            }

            textBlock.Inlines.Add(run);
        }


        /// <summary>
        /// Creates a hyperlink with the specified URL and display text.
        /// </summary>
        /// <param name="textBlock">The TextBlock to add the hyperlink to.</param>
        /// <param name="url">The URL to navigate to when clicked.</param>
        /// <param name="displayText">The text to display for the hyperlink.</param>
        private static void ProcessUrlLink(System.Windows.Controls.TextBlock textBlock, string url, string displayText)
        {
            // Ensure the URL has a scheme for Process.Start
            string navigateUrl = url;
            if (!navigateUrl.Contains("://", StringComparison.Ordinal) && !navigateUrl.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase) && (navigateUrl.StartsWith("www.", StringComparison.OrdinalIgnoreCase) || navigateUrl.StartsWith("ftp.", StringComparison.OrdinalIgnoreCase)))
            {
                navigateUrl = "http://" + navigateUrl;
            }

            // Add the URL as a proper hyperlink
            if (!AccountUtilities.CallerIsSystemInteractive && Uri.TryCreate(navigateUrl, UriKind.Absolute, out Uri? uri))
            {
                Hyperlink link = new(new Run(displayText))
                {
                    NavigateUri = uri,
                    ToolTip = $"Open link: {url}",
                };
                link.SetResourceReference(ForegroundProperty, "AccentTextFillColorPrimaryBrush");
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
        /// Applies the accent style defined by the current theme to the specified button.
        /// </summary>
        /// <remarks>This method sets a resource reference on the button's style property, enabling the
        /// button to automatically reflect changes in the theme's accent styling.</remarks>
        /// <param name="button">The button to which the accent style will be applied. This parameter must not be null.</param>
        private protected static void SetAccentButton(Fluence.Wpf.Controls.Button button)
        {
            button.Appearance = ControlAppearance.Accent;
        }

        /// <summary>
        /// Sets the specified button as the cancel button for the current dialog. The cancel button is activated when
        /// the user presses the Escape key.
        /// </summary>
        /// <param name="button">The button to designate as the cancel button. This button will be triggered when the Escape key is pressed.</param>
        private protected static void SetCancelButton(Fluence.Wpf.Controls.Button button)
        {
            button.IsCancel = true;
        }

        /// <summary>
        /// Sets the specified button as the default button for the user interface, enabling it to be activated when the
        /// user presses the Enter key.
        /// </summary>
        /// <param name="button">The button to designate as the default. Cannot be null.</param>
        private protected static void SetDefaultButton(Fluence.Wpf.Controls.Button button)
        {
            button.IsDefault = true;
        }

        /// <summary>
        /// Sets the content of the specified button to display the provided text, interpreting underscores as
        /// accelerator keys.
        /// </summary>
        /// <remarks>This method uses an AccessText control to enable support for keyboard accelerators,
        /// allowing users to activate the button using keyboard shortcuts defined by underscores in the text.</remarks>
        /// <param name="button">The button whose content is to be set. Cannot be null.</param>
        /// <param name="text">The text to display on the button. Underscores in the text are interpreted as accelerator keys. If null or
        /// consists only of white space, the button's content is not changed.</param>
        private protected static void SetButtonContentWithAccelerator(Fluence.Wpf.Controls.Button button, string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            // Create AccessText to properly handle the underscore as accelerator
            button.Content = new AccessText
            {
                Text = text,
            };
        }

        /// <summary>
        /// Updates the Grid RowDefinition based on the current content.
        /// </summary>
        private protected void UpdateRowDefinition()
        {
            CenterPanelRow.Height = new(1, GridUnitType.Auto);
        }

        /// <summary>
        /// Converts a 32-bit integer representation of a color into a <see cref="Color"/> object.
        /// </summary>
        /// <remarks>The integer is interpreted as an ARGB value, where the most significant byte represents the alpha channel, followed by the red, green, and blue channels in order.</remarks>
        /// <param name="color">A 32-bit integer where each byte represents a component of the color in ARGB order.</param>
        /// <returns>A <see cref="Color"/> object corresponding to the specified integer value.</returns>
        private static Color IntToColor(int color)
        {
            byte[] colorBytes = BitConverter.GetBytes(color);
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
            if (!_dialogIconCache.TryGetValue(dialogIconPath, out BitmapSource? bitmapSource))
            {
                using Stream stream = MiscUtilities.GetBase64StringBytes(dialogIconPath) is not byte[] bytes ? new FileStream(dialogIconPath, FileMode.Open, FileAccess.Read, FileShare.Read) : new MemoryStream(bytes, writable: false);
                if (!DrawingUtilities.IsStreamAnIcon(stream))
                {
                    BitmapImage bitmapImage = new();
                    bitmapImage.BeginInit();
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.StreamSource = stream;
                    bitmapImage.EndInit();
                    if (bitmapImage.CanFreeze)
                    {
                        bitmapImage.Freeze();
                    }
                    _dialogIconCache.Add(dialogIconPath, bitmapSource = bitmapImage);
                }
                else
                {
                    BitmapFrame iconFrame = new IconBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad).Frames.OrderByDescending(static f => f.PixelWidth * f.PixelHeight).First();
                    if (iconFrame.CanFreeze)
                    {
                        iconFrame.Freeze();
                    }
                    _dialogIconCache.Add(dialogIconPath, bitmapSource = iconFrame);
                }
            }
            return bitmapSource;
        }

        /// <summary>
        /// Sets the icon for the dialog using the specified bitmap source.
        /// </summary>
        /// <remarks>This method updates both the dialog's window icon and any associated UI element
        /// displaying the application icon.</remarks>
        /// <param name="theme">The current application theme, used to determine which cached icon to apply to the dialog.</param>
        private void SetDialogIcon(ApplicationTheme theme)
        {
            AppIconImage.Source = _dialogBitmapCache[theme];
            if (_appTaskbarIcon is null)
            {
                Icon = AppIconImage.Source;
            }
        }

        /// <summary>
        /// Applies the accent color to the dialog based on the current application theme. The accent color is retrieved
        /// from the dialog's accent color cache.
        /// </summary>
        /// <param name="theme">The current application theme for which to apply the accent color. This parameter is used to look up the appropriate accent color from the cache.</param>
        private void SetDialogAccent(ApplicationTheme theme)
        {
            if (_dialogAccentCache[theme] is Color accentColor)
            {
                ApplicationAccentColorManager.ApplyCustomAccent(accentColor);
            }
        }

        /// <summary>
        /// Positions the window on the screen based on the specified dialog position. Suppressed
        /// while <see cref="_firstShowPending"/> is <see langword="true"/> so the window stays at
        /// <see cref="OffscreenCoordinate"/> until the first-show reveal in
        /// <see cref="OnContentRendered"/>.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S3458:Empty \"case\" clauses that fall through to the \"default\" should be omitted", Justification = "The fallthrough is deliberate.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Roslynator", "RCS1069:Remove unnecessary case label", Justification = "The fallthrough is deliberate to silence other analyser warnings.")]
        private void PositionWindow()
        {
            if (_firstShowPending)
            {
                return;
            }

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

                case DialogPosition.Oobe:
                    // Center vertically on full screen (compensating for non-existent taskbar in OOBE)
                    // Calculate taskbar offset: difference between full screen and working area
                    double taskbarOffset = SystemParameters.PrimaryScreenHeight - workingArea.Height - workingArea.Top;
                    left = workingArea.Left + ((workingArea.Width - ActualWidth) / 2) - (ActualWidth * 0.6);
                    top = workingArea.Top + ((workingArea.Height - ActualHeight) / 2) + (taskbarOffset / 2);
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
            left -= _dialogPosition is DialogPosition.Default || dialogPosName.EndsWith("Right", StringComparison.Ordinal) ? 18 : dialogPosName.EndsWith("Left", StringComparison.Ordinal) ? -18 : 0;
            top -= _dialogPosition is DialogPosition.Default || dialogPosName.StartsWith("Bottom", StringComparison.Ordinal) ? 14 : dialogPosName.StartsWith("Top", StringComparison.Ordinal) ? -14 : 0;

            // Set positions in DIPs.
            Left = _startingLeft = left;
            Top = _startingTop = top;
        }

        /// <summary>
        /// Restores the window to its normal state and repositions it to its original location.
        /// </summary>
        private protected void RestoreWindow()
        {
            // Reset the window and restore its location.
            WindowState = WindowState.Normal;
            Left = _startingLeft;
            Top = _startingTop;
        }

        /// <summary>
        /// Updates the layout of the action buttons based on which buttons are visible.
        /// </summary>
        private void UpdateButtonLayout()
        {
            // Build a list of visible buttons in the order they appear.
            List<UIElement> visibleButtons = [];
            if (ButtonLeft.Visibility is Visibility.Visible)
            {
                visibleButtons.Add(ButtonLeft);
            }
            if (ButtonMiddle.Visibility is Visibility.Visible)
            {
                visibleButtons.Add(ButtonMiddle);
            }
            if (ButtonRight.Visibility is Visibility.Visible)
            {
                visibleButtons.Add(ButtonRight);
            }

            // Clear any existing column definitions.
            ActionButtons.ColumnDefinitions.Clear();

            // Return early if there's no buttons.
            if (visibleButtons.Count is 0)
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
                    ActionButtons.ColumnDefinitions.Add(new() { Width = new(1, GridUnitType.Star) });
                    Grid.SetColumn(visibleButtons[i], i);
                    Fluence.Wpf.Controls.Button button = (Fluence.Wpf.Controls.Button)visibleButtons[i];
                    button.Margin = i is 0 ? new(0, 0, 4, 0) : i == visibleButtons.Count - 1 ? new(4, 0, 0, 0) : new(4, 0, 4, 0);
                }
            }
            else
            {
                // Add two columns - one for the button (50% width) and one empty (50% width)
                ActionButtons.ColumnDefinitions.Add(new() { Width = new(1, GridUnitType.Star) });
                ActionButtons.ColumnDefinitions.Add(new() { Width = new(1, GridUnitType.Star) });

                // Place the single button in the second column
                Grid.SetColumn(visibleButtons[0], 1);

                // Set appropriate margin
                Fluence.Wpf.Controls.Button button = (Fluence.Wpf.Controls.Button)visibleButtons[0];
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
            if (_countdownTimer is null)
            {
                return;
            }
            if (!_countdownStopwatch.IsRunning)
            {
                _countdownStopwatch.Start();
            }
            _countdownTimer.Start();
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
            CountdownValueTextBlock.Text = $"{((_countdownRemainingTime.Days * 24) + _countdownRemainingTime.Hours).ToString(CultureInfo.InvariantCulture)}h {_countdownRemainingTime.Minutes.ToString(CultureInfo.InvariantCulture)}m {_countdownRemainingTime.Seconds.ToString(CultureInfo.InvariantCulture)}s";
            AutomationProperties.SetName(CountdownValueTextBlock, $"Time remaining: {((_countdownRemainingTime.Days * 24) + _countdownRemainingTime.Hours).ToString(CultureInfo.InvariantCulture)} hours, {_countdownRemainingTime.Minutes.ToString(CultureInfo.InvariantCulture)} minutes, {_countdownRemainingTime.Seconds.ToString(CultureInfo.InvariantCulture)} seconds");

            // Update text color based on remaining time using style application
            if (_countdownRemainingTime.TotalSeconds <= 60)
            {
                CountdownValueTextBlock.SetResourceReference(ForegroundProperty, "SystemFillColorCriticalBrush");
                CountdownValueTextBlock.FontWeight = FontWeights.ExtraBold;

            }
            else if (_countdownWarningDuration.HasValue && _countdownRemainingTime <= _countdownWarningDuration.Value)
            {
                CountdownValueTextBlock.SetResourceReference(ForegroundProperty, "SystemFillColorCautionBrush");
                CountdownValueTextBlock.FontWeight = FontWeights.ExtraBold;
            }
        }

        /// <summary>
        /// Callback executed by the countdown timer every second. Decrements remaining time, updates display, and handles auto-action on timeout.
        /// </summary>
        /// <param name="state">Timer state object (not used).</param>
        private protected virtual void CountdownTimer_Tick(object? state)
        {
            UpdateCountdownDisplay();
        }

        /// <summary>
        /// The result of the dialog interaction.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1061:Do not hide base class methods", Justification = "The redefinition of this field is by design.")]
        public new virtual IDialogResult DialogResult { get; private protected set; }

        /// <summary>
        /// An optional custom message to display.
        /// </summary>
        private protected readonly string? _customMessageText;

        /// <summary>
        /// Whether this window has been disposed.
        /// </summary>
        private protected bool Disposed { get; private set; }

        /// <summary>
        /// Whether this window is able to be closed.
        /// </summary>
        private bool _canClose;

        /// <summary>
        /// The specified position of the dialog.
        /// </summary>
        private protected readonly DialogPosition _dialogPosition = DialogPosition.BottomRight;

        /// <summary>
        /// Whether the dialog is allowed to be moved.
        /// </summary>
        private protected readonly bool _dialogAllowMove;

        /// <summary>
        /// Whether the dialog's minimize control is visible.
        /// </summary>
        private protected readonly bool _dialogMinimizeVisible;

        /// <summary>
        /// The countdown duration for the dialog.
        /// </summary>
        private readonly DispatcherTimer? _countdownTimer;

        /// <summary>
        /// An optional countdown to zero to commence a preferred action.
        /// </summary>
        private protected readonly TimeSpan? _countdownDuration;

        /// <summary>
        /// An optional countdown to zero for when the dialog can be no longer minimized.
        /// </summary>
        private protected readonly TimeSpan? _countdownWarningDuration;

        /// <summary>
        /// The end date/time for the countdown duration, as determined during form load.
        /// </summary>
        private protected readonly Stopwatch _countdownStopwatch;

        /// <summary>
        /// Represents the remaining time in a countdown.
        /// </summary>
        private protected TimeSpan _countdownRemainingTime;

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
        /// A coordinate guaranteed to be off every monitor regardless of DPI or multi-monitor
        /// arrangement. The window is parked here from construction until the first
        /// <see cref="OnContentRendered"/> reveal so that <see cref="SizeToContent"/> growth,
        /// <see cref="PositionWindow"/> calls from <see cref="FluentDialog_SizeChanged"/>, and
        /// the <c>Loaded</c> event all occur while the window is invisible and off-screen.
        /// </summary>
        private const double OffscreenCoordinate = -32000;

        /// <summary>
        /// Guards the off-screen first-show hold. <see langword="true"/> from construction until
        /// <see cref="OnContentRendered"/> fires and the window is placed on-screen for the first
        /// time. While <see langword="true"/>, <see cref="PositionWindow"/> is suppressed so the
        /// window stays at <see cref="OffscreenCoordinate"/> regardless of <see cref="SizeToContent"/>
        /// growth or event-driven repositions. Cleared exactly once by
        /// <see cref="OnContentRendered"/>.
        /// </summary>
        private bool _firstShowPending = true;

        /// <summary>
        /// The application tray icon bitmap source, if AppTaskbarIconImage was specified.
        /// </summary>
        private readonly BitmapSource? _appTaskbarIcon;

        /// <summary>
        /// A read-only dictionary that caches dialog icons for different application themes.
        /// </summary>
        /// <remarks>This dictionary maps <see cref="ApplicationTheme"/> values to their corresponding
        /// <see cref="BitmapSource"/> icons. It is intended to optimize access to preloaded icons for dialogs, ensuring
        /// consistent and efficient retrieval.</remarks>
        private readonly ReadOnlyDictionary<ApplicationTheme, BitmapSource> _dialogBitmapCache;

        /// <summary>
        /// Dialog icon cache for improved performance.
        /// </summary>
        private static readonly Dictionary<string, BitmapSource> _dialogIconCache = [];

        /// <summary>
        /// A read-only dictionary that caches accent colors for different application themes.
        /// </summary>
        private readonly ReadOnlyDictionary<ApplicationTheme, Color?> _dialogAccentCache;

        /// <summary>
        /// Dispose managed resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
        }

        /// <summary>
        /// Releases the resources used by the dialog and optionally disposes of managed resources.
        /// </summary>
        /// <remarks>This method is part of the standard dispose pattern. When disposing is true, event
        /// handlers are detached and timers are stopped to prevent memory leaks. Call this method when the dialog is no
        /// longer needed to ensure that all resources are properly released.</remarks>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        private protected virtual void Dispose(bool disposing)
        {
            if (Disposed)
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
                if (_expiryTimer is not null)
                {
                    _expiryTimer.Tick -= ExpiryTimer_Tick;
                    _expiryTimer.Stop();
                }
                if (_persistTimer is not null)
                {
                    _persistTimer.Tick -= PersistTimer_Tick;
                    _persistTimer.Stop();
                }

                // Clean up resources.
                ApplicationThemeManager.Changed -= ThemeManager_ActualThemeChanged;
                _countdownTimer?.Stop();
            }
            Disposed = true;
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
                return new()
                {
                    IsAccent = IsAccent,
                    IsBold = IsBold,
                    IsItalic = IsItalic,
                };
            }
        }

        private void CloseAppsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Selection changes in the close-apps list view require no additional action.
        }
    }
}
