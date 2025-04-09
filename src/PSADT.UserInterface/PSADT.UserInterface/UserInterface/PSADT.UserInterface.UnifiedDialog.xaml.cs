using PSADT.UserInterface.Services;
using PSADT.UserInterface.Utilities;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions; // Added for Regex
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation; // Added for RequestNavigateEventArgs
using System.Windows.Threading;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace PSADT.UserInterface
{
    /// <summary>
    /// Dialog types supported by the UnifiedDialog
    /// </summary>
    public enum DialogType
    {
        CloseApps,
        Progress,
        Restart,
        Input,
        Custom
    }

    /// <summary>
    /// Defines the position of the dialog window on the screen
    /// </summary>
    public enum DialogPosition
    {
        /// <summary>
        /// Position in the bottom right corner of the screen (default)
        /// </summary>
        BottomRight,

        /// <summary>
        /// Position in the center of the screen
        /// </summary>
        Center,

        /// <summary>
        /// Position at the top center of the screen
        /// </summary>
        TopCenter
    }

    /// <summary>
    /// Unified dialog for PSAppDeployToolkit that consolidates all dialog types into one
    /// </summary>
    public partial class UnifiedDialog : FluentWindow, IDisposable, INotifyPropertyChanged
    {
        #region Private Fields

        // Dialog Expiry and Cancellation
        private readonly CancellationTokenSource _dialogCancellationTokenSource;

        private TimeSpan? _dialogExpiryDuration;
        private readonly Timer? _dialogExpiryTimer;
        private DialogPosition? _dialogPosition;
        private bool _dialogAllowMove;
        private bool _dialogTopMost;

        // Countdown Timer
        private TimeSpan? _countdownDuration;

        private TimeSpan? _countdownNoMinimizeDuration; // Minutes before the end when minimize is disabled
        private Timer? _countdownTimer;
        private TimeSpan _countdownRemainingTime;

        // Process Evaluation
        private CancellationTokenSource? _processCancellationTokenSource;

        private IProcessEvaluationService? _processEvaluationService;
        private List<AppProcessInfo>? _appsToClose;
        private List<AppProcessInfo> _previousProcessInfo = [];
        private readonly SemaphoreSlim _processEvaluationLock = new(1, 1); // For thread safety in process evaluation

        // Adaptive delay for process evaluation with optimized defaults
        private TimeSpan _processEvaluationDelay = TimeSpan.FromSeconds(1.5);

        private const int MAX_DELAY_SECONDS = 4;
        private const double MIN_DELAY_SECONDS = 0.75;

        // Cache for recently removed processes to prevent flickering
        private readonly Dictionary<string, DateTime> _recentlyRemovedProcesses = new();
        private const int PROCESS_CACHE_EXPIRY_MS = 500; // Time to keep removed processes in cache

        // Dialog
        private DialogType _dialogType;

        private string? _dialogResult;
        private double _progressBarValue = 0;
        private int? _deferralsRemaining;
        private TimeSpan? _deferralDeadline;
        private bool _isProcessing = false;
        private bool _isAnimating = false;
        private bool _isDisposed = false;
        private bool _canClose = false;

        // Constants for UI layout
        private const double ListViewItemHeight = 40; // Height of each ProcessGrid item

        private const double ListViewMaxItems = 3; // Maximum number of visible items before scrolling
        private const double ListViewPadding = 16; // Additional padding for the ListView (8px top + 8px bottom)
        private const double BaseWindowHeight = 198; // Base height for the window without content
        private const double MaxListViewHeight = 198; // Maximum height for the ListView before scrolling (matches MaxHeight in XAML)

        // Icon cache for improved performance
        private static readonly Dictionary<string, BitmapImage> _iconCache = new();

        // Store original and alternative texts
        private string? _originalMessage;

        private string? _alternativeMessage;
        private string? _buttonLeftOriginalText;
        private string? _buttonLeftAlternativeText;
        private string? _buttonMiddleOriginalText;
        private string? _buttonMiddleAlternativeText;
        private string? _buttonRightOriginalText;
        private string? _buttonRightAlternativeText;
        private string? _deferralsRemainingText;
        private string? _deferralDeadlineText;
        private string? _inputTextResult; // To store text from Input dialog

        // Removed animation-related fields

        #endregion Private Fields

        #region Public Properties

        /// <summary>
        /// The result of the dialog interaction
        /// </summary>
        public new string DialogResult
        {
            get => _dialogResult ?? "Cancel";
            private set
            {
                _dialogResult = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// The type of dialog being displayed
        /// </summary>
        public DialogType DialogType
        {
            get => _dialogType;
            private set
            {
                _dialogType = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Collection of apps that need to be closed
        /// </summary>
        public ObservableCollection<AppProcessInfo> AppsToCloseCollection { get; } = [];

        /// <summary>
        /// Current progress percentComplete
        /// </summary>
        public double ProgressBarValue
        {
            get => _progressBarValue;
            set
            {
                if (Math.Abs(_progressBarValue - value) > 0.01) // Only update if percentComplete has changed significantly
                {
                    _progressBarValue = value;
                    OnPropertyChanged();

                    // Update accessibility properties
                    Dispatcher.InvokeAsync(() =>
                    {
                        AutomationProperties.SetName(ProgressBar, $"Progress: {value:F0}%");
                    }, DispatcherPriority.Background);
                }
            }
        }

        /// <summary>
        /// Gets whether this dialog has been disposed
        /// </summary>
        public bool IsDisposed => _isDisposed;

        /// <summary>
        /// CancellationToken for the dialog operations
        /// </summary>
        public CancellationToken DialogCancellationToken => _dialogCancellationTokenSource.Token;

        /// <summary>
        /// Gets the text entered by the user in the Input dialog, if applicable.
        /// Returns null for other dialog types or if the dialog was cancelled before input could be captured.
        /// </summary>
        public string? InputTextResult => _inputTextResult;

        #endregion Public Properties

        #region Constructor and Initialization

        /// <summary>
        /// Initializes a new instance of UnifiedDialog
        /// </summary>
        /// <param name="dialogType">Type of dialog to display</param>
        /// <param name="dialogExpiryDuration">Optional duration after which the dialog will automatically close</param>
        /// <param name="dialogAccentColor">Optional accent color for the dialog</param>
        /// <param name="dialogPosition">Position of the window on screen (default: BottomRight)</param>
        /// <param name="dialogTopMost">Whether the dialog should stay on top of other windows</param>
        /// <param name="dialogAllowMove">Whether to allow the user to move the window (default: false)</param>
        public UnifiedDialog(DialogType dialogType, TimeSpan? dialogExpiryDuration = null, String? dialogAccentColor = null, DialogPosition? dialogPosition = DialogPosition.BottomRight, bool? dialogTopMost = false, bool? dialogAllowMove = false)
        {
            DataContext = this;

            SystemThemeWatcher.Watch(this, WindowBackdropType.Acrylic, false);

            InitializeComponent();

            _dialogCancellationTokenSource = new CancellationTokenSource();

            _dialogType = dialogType;
            _dialogExpiryDuration = dialogExpiryDuration ?? TimeSpan.FromMinutes(55);
            _dialogPosition = dialogPosition ?? DialogPosition.BottomRight;
            _dialogAllowMove = dialogAllowMove ?? false;
            _dialogTopMost = dialogTopMost ?? false;

            if (dialogAccentColor != null || dialogAccentColor != string.Empty)
            {
                try
                {
                    // Apply the accent color to the application theme
                    var colorDialogAccentColor = StringToColor(dialogAccentColor);
                    ApplicationTheme appTheme = ApplicationThemeManager.GetAppTheme();
                    ApplicationAccentColorManager.Apply(colorDialogAccentColor, appTheme, false);

                    // And set the dialog accent color on the sidebar
                    SolidColorBrush accentBrush = new(colorDialogAccentColor);
                    accentBrush.Freeze(); // Improve performance by freezing the brush
                    AccentSidebar.Fill = accentBrush;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to apply accent color: {ex.Message}");
                }
            }
            else
            {
                SystemThemeWatcher.Watch(this, Wpf.Ui.Controls.WindowBackdropType.Acrylic, true);
            }

            var converter = new ResourceReferenceExpressionConverter();
            var brushes = new Dictionary<string, SolidColorBrush>
            {
                ["SystemAccentColor"] = new SolidColorBrush((Color)Application.Current.Resources["SystemAccentColor"]),
                ["SystemAccentColorPrimary"] = new SolidColorBrush((Color)Application.Current.Resources["SystemAccentColorPrimary"]),
                ["SystemAccentColorSecondary"] = new SolidColorBrush((Color)Application.Current.Resources["SystemAccentColorSecondary"]),
                ["SystemAccentColorTertiary"] = new SolidColorBrush((Color)Application.Current.Resources["SystemAccentColorTertiary"])
            };
            ResourceDictionary themeDictionary = Application.Current.Resources.MergedDictionaries[0];
            foreach (DictionaryEntry entry in themeDictionary)
            {
                if (entry.Value is SolidColorBrush brush)
                {
                    var dynamicColor = brush.ReadLocalValue(SolidColorBrush.ColorProperty);
                    if (dynamicColor is not Color &&
                        converter.ConvertTo(dynamicColor, typeof(MarkupExtension)) is DynamicResourceExtension dynamicResource &&
                        brushes.ContainsKey((string)dynamicResource.ResourceKey))
                    {
                        themeDictionary[entry.Key] = brushes[(string)dynamicResource.ResourceKey];
                    }
                }
            }

            // Configure window events
            Loaded += UnifiedDialog_Loaded;
            SizeChanged += UnifiedDialog_SizeChanged;
            AppsToCloseCollection.CollectionChanged += AppsToCloseCollection_CollectionChanged;

            // Set up window and cancellation timer
            WindowStartupLocation = WindowStartupLocation.Manual;
            if (dialogExpiryDuration.HasValue)
            {
                _dialogExpiryTimer = new Timer(CloseDialog, null, dialogExpiryDuration.Value, Timeout.InfiniteTimeSpan);
            }

            this.Topmost = _dialogTopMost;

            // Animation debounce timer removed
        }

        public static Color StringToColor(string colorStr)
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(colorStr, "^#([0-9A-Fa-f]{6}|[0-9A-Fa-f]{8})$"))
            {
                throw new FormatException("Invalid hex color string.");
            }

            TypeConverter cc = TypeDescriptor.GetConverter(typeof(Color));
            var result = cc.ConvertFromString(colorStr) as Color?;
            if (result is null)
            {
                throw new InvalidOperationException("Failed to convert color string to Color.");
            }
            return result.Value;
        }

        /// <summary>
        /// Initializes the UI elements and behavior for the CloseApps dialog type.
        /// </summary>
        /// <param name="appTitle">Main title of the dialog window.</param>
        /// <param name="subtitle">Subtitle displayed below the main title.</param>
        /// <param name="appIconImage">Path to the icon image file.</param>
        /// <param name="appsToClose">List of applications the user needs to close.</param>
        /// <param name="countdownDuration">Optional duration for a countdown timer before automatic action.</param>
        /// <param name="deferralsRemaining">Optional number of deferrals allowed.</param>
        /// <param name="deferralDeadline">Optional deadline until which deferral is allowed.</param>
        /// <param name="closeAppsMessageText">Message displayed when apps need closing.</param>
        /// <param name="alternativeCloseAppsMessageText">Message displayed when no apps need closing.</param>
        /// <param name="deferralsRemainingText">Text displayed next to the deferral count.</param>
        /// <param name="deferralDeadlineText">Text displayed next to the deferral deadline.</param>
        /// <param name="automaticStartCountdownText">Heading text for the countdown timer.</param>
        /// <param name="deferButtonText">Text for the defer button.</param>
        /// <param name="continueButtonText">Text for the continue/close apps button.</param>
        /// <param name="alternativeContinueButtonText">Text for the continue button when no apps need closing.</param>
        /// <param name="processEvaluationService">Optional service for dynamic process evaluation.</param>
        public void InitializeCloseAppsDialog(
            string? appTitle,
            string? subtitle,
            string? appIconImage,
            List<AppProcessInfo>? appsToClose,
            TimeSpan? countdownDuration,
            int? deferralsRemaining,
            TimeSpan? deferralDeadline,
            string? closeAppsMessageText,
            string? alternativeCloseAppsMessageText,
            string? deferralsRemainingText,
            string? deferralDeadlineText,
            string? automaticStartCountdownText,
            string? deferButtonText,
            string? continueButtonText,
            string? alternativeContinueButtonText,
            IProcessEvaluationService? processEvaluationService = null
            )

        {
            // Set basic properties
            Title = appTitle ?? "Close Applications";
            AppTitleTextBlock.Text = appTitle;
            SubtitleTextBlock.Text = subtitle;

            // Set accessibility properties
            AutomationProperties.SetName(this, appTitle ?? "Close Applications Dialog");

            // Set up Close Apps properties
            _countdownDuration = countdownDuration;
            _appsToClose = appsToClose != null ? new List<AppProcessInfo>(appsToClose) : null; // Create a deep copy to avoid reference issues
            _processEvaluationService = processEvaluationService;
            _deferralsRemaining = deferralsRemaining;

            // Store original and alternative texts
            _originalMessage = closeAppsMessageText ?? "Please close the following applications:";
            _alternativeMessage = alternativeCloseAppsMessageText ?? "Please continue with the installation.";
            _buttonLeftOriginalText = deferButtonText ?? "Defer";
            _buttonMiddleOriginalText = null;
            _buttonRightOriginalText = continueButtonText ?? "Close Apps & Install";
            _buttonRightAlternativeText = alternativeContinueButtonText ?? "Install";

            // Set up UI
            FormatMessageWithHyperlinks(MessageTextBlock, closeAppsMessageText ?? _originalMessage); // Use helper method
            CloseAppsStackPanel.Visibility = Visibility.Visible;
            ProgressStackPanel.Visibility = Visibility.Collapsed;
            InputBoxStackPanel.Visibility = Visibility.Collapsed; // Ensure hidden by default
            DeferStackPanel.Visibility = deferralsRemaining.HasValue && deferralsRemaining > 0 || deferralDeadline.HasValue ? Visibility.Visible : Visibility.Collapsed;
            DeferralDeadlineHeadingTextBlock.Text = deferralDeadline == null ? deferralsRemainingText : deferralDeadlineText;

            CountdownStackPanel.Visibility = countdownDuration.HasValue ? Visibility.Visible : Visibility.Collapsed;
            CountdownHeadingTextBlock.Text = automaticStartCountdownText;

            // Configure buttons
            ButtonPanel.Visibility = Visibility.Visible;
            SetButtonContentWithAccelerator(ButtonLeft, _buttonLeftOriginalText);
            ButtonLeft.Visibility = deferralsRemaining.HasValue && deferralsRemaining > 0 | deferralDeadline.HasValue ? Visibility.Visible : Visibility.Collapsed;
            ButtonMiddle.Visibility = Visibility.Collapsed;
            SetButtonContentWithAccelerator(ButtonRight, _buttonRightOriginalText);
            ButtonRight.Visibility = Visibility.Visible;

            // Set button automation properties
            AutomationProperties.SetName(ButtonLeft, _buttonLeftOriginalText);
            AutomationProperties.SetName(ButtonRight, _buttonRightOriginalText);

            UpdateDeferralValues();
            UpdateButtonLayout();

            // Set app icon
            SetAppIcon(appIconImage);

            // Initialize countdown if specified
            if (countdownDuration.HasValue)
            {
                InitializeCountdown(countdownDuration.Value);
            }

            // Attach to window events specific to this dialog type
            if (_processEvaluationService != null && appsToClose != null && appsToClose.Count > 0)
            {
                _processEvaluationService.ProcessStarted += ProcessEvaluationService_ProcessStarted;
                _processEvaluationService.ProcessExited += ProcessEvaluationService_ProcessExited;

                // Start monitoring processes
                UpdateAppsToCloseList();
                _processCancellationTokenSource = new CancellationTokenSource();
                _ = StartProcessEvaluationLoopAsync(_appsToClose!, _processCancellationTokenSource.Token);
            }
            else
            {
                // No apps to close
                // CloseAppsStackPanel.Visibility = Visibility.Collapsed;
                FormatMessageWithHyperlinks(MessageTextBlock, _alternativeMessage); // Use helper method
                SetButtonContentWithAccelerator(ButtonRight, _buttonRightAlternativeText);
                AutomationProperties.SetName(ButtonRight, "Install");
            }

            // Focus the continue button by default
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () =>
            {
                ButtonRight.Focus();
            });
        }

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
        /// Initializes the UI elements and behavior for the Custom dialog type.
        /// </summary>
        /// <param name="appTitle">Main title of the dialog window.</param>
        /// <param name="subtitle">Subtitle displayed below the main title.</param>
        /// <param name="appIconImage">Path to the icon image file.</param>
        /// <param name="customMessage">The main message text to display.</param>
        /// <param name="ButtonLeftText">Text for the left button. If null or empty, the button is hidden.</param>
        /// <param name="ButtonMiddleText">Text for the middle button. If null or empty, the button is hidden.</param>
        /// <param name="ButtonRightText">Text for the right button. If null or empty, the button is hidden.</param>
        public void InitializeCustomDialog(
            string? appTitle,
            string? subtitle,
            string? appIconImage,
            string? customMessage,
            string? ButtonLeftText,
            string? ButtonMiddleText,
            string? ButtonRightText)
        {
            // Set basic properties
            Title = appTitle ?? "Message";
            AppTitleTextBlock.Text = appTitle;
            SubtitleTextBlock.Text = subtitle;

            // Set accessibility properties
            AutomationProperties.SetName(this, appTitle ?? "Message Dialog");

            // Set up UI
            FormatMessageWithHyperlinks(MessageTextBlock, customMessage ?? string.Empty); // Use helper method
            CloseAppsStackPanel.Visibility = Visibility.Collapsed;
            ProgressStackPanel.Visibility = Visibility.Collapsed;
            InputBoxStackPanel.Visibility = Visibility.Collapsed; // Ensure hidden by default
            DeferStackPanel.Visibility = Visibility.Collapsed;
            CountdownStackPanel.Visibility = Visibility.Collapsed;
            ButtonPanel.Visibility = Visibility.Visible;

            // Configure buttons based on provided texts
            SetButtonContentWithAccelerator(ButtonLeft, ButtonLeftText ?? "_OK");
            ButtonLeft.Visibility = string.IsNullOrWhiteSpace(ButtonLeftText) ? Visibility.Collapsed : Visibility.Visible;
            AutomationProperties.SetName(ButtonLeft, ButtonLeftText ?? "OK");

            SetButtonContentWithAccelerator(ButtonMiddle, ButtonMiddleText ?? "_Cancel");
            ButtonMiddle.Visibility = string.IsNullOrWhiteSpace(ButtonMiddleText) ? Visibility.Collapsed : Visibility.Visible;
            AutomationProperties.SetName(ButtonMiddle, ButtonMiddleText ?? "Cancel");

            SetButtonContentWithAccelerator(ButtonRight, ButtonRightText ?? "_Continue");
            ButtonRight.Visibility = string.IsNullOrWhiteSpace(ButtonRightText) ? Visibility.Collapsed : Visibility.Visible;
            AutomationProperties.SetName(ButtonRight, ButtonRightText ?? "Continue");

            UpdateButtonLayout();

            // Set app icon
            SetAppIcon(appIconImage);

            // Focus the default button
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () =>
            {
                if (ButtonRight.Visibility == Visibility.Visible)
                    ButtonRight.Focus();
                else if (ButtonLeft.Visibility == Visibility.Visible)
                    ButtonLeft.Focus();
                else if (ButtonMiddle.Visibility == Visibility.Visible)
                    ButtonMiddle.Focus();
            });
        }

        /// <summary>
        /// Initializes the UI elements and behavior for the Input dialog type.
        /// </summary>
        /// <param name="appTitle">Main title of the dialog window.</param>
        /// <param name="subtitle">Subtitle displayed below the main title.</param>
        /// <param name="appIconImage">Path to the icon image file.</param>
        /// <param name="customMessage">The message text displayed above the input box.</param>
        /// <param name="initialInputText">The initial text pre-filled in the input box.</param>
        /// <param name="ButtonLeftText">Text for the left button. If null or empty, the button is hidden.</param>
        /// <param name="ButtonMiddleText">Text for the middle button. If null or empty, the button is hidden.</param>
        /// <param name="ButtonRightText">Text for the right button. If null or empty, the button is hidden.</param>
        public void InitializeInputDialog(
            string? appTitle,
            string? subtitle,
            string? appIconImage,
            string? customMessage, // Renamed from inputBoxTextBlock for consistency
            string? initialInputText, // Renamed from inputBoxText
            string? ButtonLeftText,
            string? ButtonMiddleText,
            string? ButtonRightText)
        {
            // Set basic properties
            Title = appTitle ?? "Input Required";
            AppTitleTextBlock.Text = appTitle;
            SubtitleTextBlock.Text = subtitle;

            // Set accessibility properties
            AutomationProperties.SetName(this, appTitle ?? "Input Dialog");

            // Set up UI
            FormatMessageWithHyperlinks(MessageTextBlock, customMessage ?? "Please enter a value:"); // Use helper method
            InputBoxText.Text = initialInputText ?? string.Empty; // Set initial text
            CloseAppsStackPanel.Visibility = Visibility.Collapsed;
            ProgressStackPanel.Visibility = Visibility.Collapsed;
            InputBoxStackPanel.Visibility = Visibility.Visible; // Show the input controls
            DeferStackPanel.Visibility = Visibility.Collapsed;
            CountdownStackPanel.Visibility = Visibility.Collapsed;
            ButtonPanel.Visibility = Visibility.Visible;

            // Configure buttons based on provided texts
            SetButtonContentWithAccelerator(ButtonLeft, ButtonLeftText ?? "_Cancel"); // Default Left to Cancel
            ButtonLeft.Visibility = string.IsNullOrWhiteSpace(ButtonLeftText) ? Visibility.Collapsed : Visibility.Visible;
            AutomationProperties.SetName(ButtonLeft, ButtonLeftText ?? "Cancel");

            SetButtonContentWithAccelerator(ButtonMiddle, ButtonMiddleText); // No default for Middle
            ButtonMiddle.Visibility = string.IsNullOrWhiteSpace(ButtonMiddleText) ? Visibility.Collapsed : Visibility.Visible;
            if (!string.IsNullOrWhiteSpace(ButtonMiddleText)) AutomationProperties.SetName(ButtonMiddle, ButtonMiddleText);


            SetButtonContentWithAccelerator(ButtonRight, ButtonRightText ?? "_OK"); // Default Right to OK
            ButtonRight.Visibility = string.IsNullOrWhiteSpace(ButtonRightText) ? Visibility.Collapsed : Visibility.Visible;
            AutomationProperties.SetName(ButtonRight, ButtonRightText ?? "OK");

            UpdateButtonLayout();

            // Set app icon
            SetAppIcon(appIconImage);

            // Focus the input box initially
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () =>
            {
                InputBoxText.Focus();
                InputBoxText.SelectAll();
            });
        }

        ///// <summary>
        ///// Initialize the dialog as a Custom dialog - OLD COMMENTED CODE - REMOVE
        ///// </summary>
        //public void InitializeInputDialog(
        //    string? appTitle,
        //    string? subtitle,
        //    string? appIconImage,
        //    string? inputBoxTextBlock,
        //    string? inputBoxText,
        //    string? ButtonLeftText,
        //    string? ButtonMiddleText,
        //    string? ButtonRightText)
        //{
        //    // Set basic properties
        //    Title = appTitle ?? "Message";
        //    AppTitleTextBlock.Text = appTitle;
        //    SubtitleTextBlock.Text = subtitle;

        //    // Set accessibility properties
        //    AutomationProperties.SetName(this, appTitle ?? "Input Dialog");

        //    // Set up UI
        //    MessageTextBlock.Text = inputBoxTextBlock ?? string.Empty;
        //    InputBoxText.Text = inputBoxText ?? string.Empty;
        //    CloseAppsStackPanel.Visibility = Visibility.Collapsed;
        //    ProgressStackPanel.Visibility = Visibility.Collapsed;
        //    InputBoxStackPanel.Visibility = Visibility.Visible;
        //    DeferStackPanel.Visibility = Visibility.Collapsed;
        //    CountdownStackPanel.Visibility = Visibility.Collapsed;
        //    ButtonPanel.Visibility = Visibility.Visible;

        //    // Configure buttons based on provided texts
        //    SetButtonContentWithAccelerator(ButtonLeft, ButtonLeftText ?? "_OK");
        //    ButtonLeft.Visibility = string.IsNullOrWhiteSpace(ButtonLeftText) ? Visibility.Collapsed : Visibility.Visible;
        //    AutomationProperties.SetName(ButtonLeft, ButtonLeftText ?? "OK");

        //    SetButtonContentWithAccelerator(ButtonMiddle, ButtonMiddleText ?? "_Cancel");
        //    ButtonMiddle.Visibility = string.IsNullOrWhiteSpace(ButtonMiddleText) ? Visibility.Collapsed : Visibility.Visible;
        //    AutomationProperties.SetName(ButtonMiddle, ButtonMiddleText ?? "Cancel");

        //    SetButtonContentWithAccelerator(ButtonRight, ButtonRightText ?? "_Continue");
        //    ButtonRight.Visibility = string.IsNullOrWhiteSpace(ButtonRightText) ? Visibility.Collapsed : Visibility.Visible;
        //    AutomationProperties.SetName(ButtonRight, ButtonRightText ?? "Continue");

        //    UpdateButtonLayout();

        //    // Set app icon
        //    SetAppIcon(appIconImage);

        //    // Focus the default button
        //    Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () =>
        //    {
        //        if (ButtonRight.Visibility == Visibility.Visible)
        //            ButtonRight.Focus();
        //        else if (ButtonLeft.Visibility == Visibility.Visible)
        //            ButtonLeft.Focus();
        //        else if (ButtonMiddle.Visibility == Visibility.Visible)
        //            ButtonMiddle.Focus();
        //    });
        //}


        /// <summary>
        /// Initializes the UI elements and behavior for the Restart dialog type.
        /// </summary>
        /// <param name="appTitle">Main title of the dialog window.</param>
        /// <param name="subtitle">Subtitle displayed below the main title.</param>
        /// <param name="appIconImage">Path to the icon image file.</param>
        /// <param name="countdownDuration">Optional duration for a countdown timer before automatic restart.</param>
        /// <param name="countdownNoMinimizeDuration">Optional duration before the end of the countdown when the 'Dismiss' button is disabled.</param>
        /// <param name="restartMessageText">The main message text asking for restart confirmation.</param>
        /// <param name="countdownRestartMessageText">Message text displayed when the countdown is active.</param>
        /// <param name="countdownAutomaticRestartText">Heading text for the countdown timer.</param>
        /// <param name="dismissButtonText">Text for the dismiss/restart later button.</param>
        /// <param name="restartButtonText">Text for the restart now button.</param>
        public void InitializeRestartDialog(
            string? appTitle,
            string? subtitle,
            string? appIconImage,
            TimeSpan? countdownDuration,
            TimeSpan? countdownNoMinimizeDuration,
            string? restartMessageText,
            string? countdownRestartMessageText,
            string? countdownAutomaticRestartText,
            string? dismissButtonText,
            string? restartButtonText
            )
        {
            // Set basic properties
            Title = appTitle ?? "Restart Required";
            AppTitleTextBlock.Text = appTitle;
            SubtitleTextBlock.Text = subtitle;

            // Set accessibility properties
            AutomationProperties.SetName(this, appTitle ?? "Restart Required Dialog");

            // Restart Countdown
            _countdownDuration = countdownDuration;
            _countdownNoMinimizeDuration = countdownNoMinimizeDuration;

            // Set up UI
            FormatMessageWithHyperlinks(MessageTextBlock, countdownRestartMessageText ?? restartMessageText ?? "A system restart is required to complete the installation."); // Use helper method
            CountdownHeadingTextBlock.Text = countdownAutomaticRestartText;

            CloseAppsStackPanel.Visibility = Visibility.Collapsed;
            ProgressStackPanel.Visibility = Visibility.Collapsed;
            InputBoxStackPanel.Visibility = Visibility.Collapsed; // Ensure hidden by default
            DeferStackPanel.Visibility = Visibility.Collapsed;
            CountdownStackPanel.Visibility = Visibility.Visible;
            ButtonPanel.Visibility = Visibility.Visible;

            // Configure buttons
            SetButtonContentWithAccelerator(ButtonLeft, dismissButtonText ?? "Dismiss");
            ButtonLeft.Visibility = Visibility.Visible;
            AutomationProperties.SetName(ButtonLeft, dismissButtonText ?? "Dismiss");

            ButtonMiddle.Visibility = Visibility.Hidden;
            SetButtonContentWithAccelerator(ButtonRight, restartButtonText ?? "Restart Now");
            ButtonRight.Visibility = Visibility.Visible;
            AutomationProperties.SetName(ButtonRight, restartButtonText ?? "Restart Now");

            UpdateButtonLayout();

            // Set app icon
            SetAppIcon(appIconImage);

            // Initialize countdown if specified
            if (countdownDuration.HasValue)
            {
                InitializeCountdown(countdownDuration.Value);
            }

            // Focus the restart button by default
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () =>
            {
                ButtonRight.Focus();
            });
        }

        /// <summary>
        /// Sets the application icon displayed in the header and the window's taskbar icon.
        /// Uses a cache for performance.
        /// </summary>
        /// <param name="appIconImage">Path or URI to the icon image file. Defaults to embedded resource if null.</param>
        private void SetAppIcon(string? appIconImage)
        {
            try
            {
                appIconImage ??= "pack://application:,,,/PSADT.UserInterface;component/Resources/appIcon.png";
                if (!string.IsNullOrWhiteSpace(appIconImage))
                {
                    BitmapImage iconImage;

                    // Try to get from cache first
                    if (!_iconCache.TryGetValue(appIconImage, out iconImage!))
                    {
                        iconImage = new BitmapImage();

                        // Use BeginInit/EndInit pattern for better performance
                        iconImage.BeginInit();
                        iconImage.CacheOption = BitmapCacheOption.OnLoad;
                        iconImage.UriSource = new Uri(appIconImage, UriKind.Absolute);
                        iconImage.EndInit();

                        if (iconImage.CanFreeze)
                            iconImage.Freeze(); // Make it shareable across threads

                        _iconCache[appIconImage] = iconImage;
                    }

                    AppIconImage.Source = iconImage;
                    Icon = iconImage;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to set app icon: {ex.Message}");
            }
        }

        #endregion Constructor and Initialization

        #region Window Events and Layout Management

        private void UnifiedDialog_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateLayout();

            // Initialize countdown display if needed
            if ((_dialogType == DialogType.Restart || _dialogType == DialogType.CloseApps) && _countdownDuration.HasValue)
            {
                UpdateCountdownDisplay();
            }

            // Update row definitions based on current content
            UpdateRowDefinition();

            // Apply fixed sizing directly
            UpdateWindowHeight();

            // Position the window
            PositionWindow();
        }

        private void UnifiedDialog_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Only reposition window - no animations
            PositionWindow();

            // Add hook to prevent window movement
            WindowInteropHelper helper = new(this);
            HwndSource? source = HwndSource.FromHwnd(helper.Handle);
            if (source != null)
            {
                source.AddHook(new HwndSourceHook(WndProc));
            }
        }

        /// <summary>
        /// Prevent window movement by handling WM_SYSCOMMAND
        /// </summary>
        private const int WM_SYSCOMMAND = 0x0112;

        private const int SC_MOVE = 0xF010;

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_SYSCOMMAND)
            {
                int command = wParam.ToInt32() & 0xfff0;
                if (command == SC_MOVE && !_dialogAllowMove)
                {
                    handled = true;
                }
            }
            return IntPtr.Zero;
        }

        /// <summary>
        /// Positions the window on the screen based on the specified window position
        /// </summary>
        private void PositionWindow()
        {
            try
            {
                IntPtr windowHandle = new WindowInteropHelper(this).Handle;
                WPFScreen screen = WPFScreen.FromHandle(windowHandle);

                // Get the working area in DIPs
                Rect workingArea = screen.GetWorkingAreaInDips(this);

                // Ensure layout is updated to get ActualWidth and ActualHeight
                double windowWidth = ActualWidth;
                double windowHeight = ActualHeight;

                double left, top;
                const double margin = 0; // Margin to prevent overlap with screen edges

                // Calculate positions based on window position setting
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

                // Ensure the window is within the screen bounds
                left = Math.Max(workingArea.Left, Math.Min(left, workingArea.Right - windowWidth));
                top = Math.Max(workingArea.Top, Math.Min(top, workingArea.Bottom - windowHeight));

                // Align positions to whole pixels
                left = Math.Floor(left);
                top = Math.Floor(top);

                // Adjust for workArea offset
                left += 1;
                top += 1;

                // Set positions in DIPs
                Left = left;
                Top = top;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error positioning window: {ex.Message}");
                CenterWindowOnScreen(); // Fallback to center
            }
        }

        /// <summary>
        /// Centers the window on the screen (used as a fallback positioning method)
        /// </summary>
        private void CenterWindowOnScreen()
        {
            try
            {
                var windowHandle = new WindowInteropHelper(this).Handle;
                var screen = WPFScreen.FromHandle(windowHandle);

                // Get the bounds in DIPs
                var bounds = screen.GetBoundsInDips(this);

                // Calculate center positions
                double left = bounds.Left + ((bounds.Width - Width) / 2);
                double top = bounds.Top + ((bounds.Height - Height) / 2);

                // Set positions
                Left = left;
                Top = top;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error centering window: {ex.Message}");
                // Use default window positioning if all else fails
                WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }
        }

        /// <summary>
        /// Updates the Grid RowDefinition based on the current content
        /// </summary>
        private void UpdateRowDefinition()
        {
            // Always use Auto sizing for all dialog types
            CenterPanelRow.Height = new GridLength(1, GridUnitType.Auto);
        }

        /// <summary>
        /// Sets the window height directly based on content
        /// </summary>
        private void UpdateWindowHeight()
        {
            try
            {
                double contentHeight = 0;
                if (DialogType == DialogType.CloseApps && AppsToCloseCollection.Count > 0)
                {
                    // Calculate the height needed for the visible items (max 3)
                    int visibleItemCount = Math.Min(AppsToCloseCollection.Count, (int)ListViewMaxItems);
                    contentHeight = (ListViewItemHeight * visibleItemCount) + ListViewPadding;

                    // Ensure we don't exceed the maximum height for the ListView
                    contentHeight = Math.Min(contentHeight, MaxListViewHeight);
                }
                else
                {
                    contentHeight = ContentBorder.ActualHeight;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error setting window height: {ex.Message}");
            }
        }

        #endregion Window Events and Layout Management

        #region CloseApps Dialog Features

        private void AppsToCloseCollection_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (DialogType != DialogType.CloseApps)
                return;

            try
            {
                // Update row definitions
                UpdateRowDefinition();

                // Apply fixed sizing directly
                UpdateWindowHeight();

                // Update accessibility count
                AutomationProperties.SetName(CloseAppsListView, $"Applications to Close: {AppsToCloseCollection.Count} items");

                if (AppsToCloseCollection.Count == 0 && _alternativeMessage != null)
                {
                    // Update the message and button content with alternative texts
                    FormatMessageWithHyperlinks(MessageTextBlock, _alternativeMessage); // Use helper method
                    SetButtonContentWithAccelerator(ButtonRight, _buttonRightAlternativeText);
                    AutomationProperties.SetName(ButtonRight, _buttonRightAlternativeText ?? "Install");

                    // Hide the entire apps to close panel when there are no apps
                    // CloseAppsStackPanel.Visibility = Visibility.Collapsed;
                }
                else if (_originalMessage != null)
                {
                    // Revert to original texts
                    FormatMessageWithHyperlinks(MessageTextBlock, _originalMessage); // Use helper method
                    SetButtonContentWithAccelerator(ButtonRight, _buttonRightOriginalText);
                    AutomationProperties.SetName(ButtonRight, _buttonRightOriginalText ?? "Close Apps & Install");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in AppsToCloseCollection_CollectionChanged: {ex.Message}");
            }
        }

        private void UpdateAppsToCloseList()
        {
            try
            {
                if (_appsToClose == null || _appsToClose.Count == 0)
                {
                    // Only set to collapsed if initially there are no apps to close
                    CloseAppsStackPanel.Visibility = Visibility.Collapsed;
                    return;
                }
                else
                {
                    // Ensure the list is visible when we have apps to close
                    CloseAppsStackPanel.Visibility = Visibility.Visible;
                }

                // Rest of the method remains unchanged
                if (_processEvaluationService == null)
                {
                    // Populate the collection directly
                    foreach (AppProcessInfo app in _appsToClose)
                    {
                        if (AppsToCloseCollection.FirstOrDefault(a => a.ProcessName.Equals(app.ProcessName, StringComparison.OrdinalIgnoreCase)) == null)
                        {
                            AppsToCloseCollection.Add(app);
                        }
                    }
                    return;
                }

                // Evaluate running processes and populate the collection
                var updatedAppsToClose = _processEvaluationService.EvaluateRunningProcesses(_appsToClose);

                // Clear existing items
                AppsToCloseCollection.Clear();

                // Add updated apps
                foreach (var app in updatedAppsToClose)
                {
                    if (AppsToCloseCollection.FirstOrDefault(a => a.ProcessName.Equals(app.ProcessName, StringComparison.OrdinalIgnoreCase)) == null)
                    {
                        AppsToCloseCollection.Add(app);
                    }
                }

                _previousProcessInfo = new List<AppProcessInfo>(updatedAppsToClose);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in UpdateAppsToCloseList: {ex.Message}");
            }
        }

        private void UpdateDeferralValues()
        {
            try
            {
                if (_deferralsRemaining.HasValue)
                {
                    // Only enable the button if there are deferrals remaining
                    ButtonLeft.IsEnabled = _deferralsRemaining > 0;

                    // Format the remaining time as hh:mm:ss
                    CountdownValueTextBlock.Text = $"{_countdownRemainingTime.Hours}h {_countdownRemainingTime.Minutes}m {_countdownRemainingTime.Seconds}s";

                    // Update accessibility properties
                    AutomationProperties.SetName(DeferralDeadlineValueTextBlock, $"{_deferralsRemaining} remain");

                    // Set the value correctly
                    DeferralDeadlineValueTextBlock.Text = $"{_deferralsRemaining} remain";

                    // Update text color based on remaining deferrals
                    if (_deferralsRemaining <= 1)
                    {
                        // Less than 1 deferral remaining - use caution color
                        DeferralDeadlineValueTextBlock.Foreground = Application.Current.Resources["SystemFillColorCautionBrush"] as Brush;
                    }
                }
                else if (_deferralDeadline.HasValue)
                {
                    // Only enable the button if the deadline hasn't passed
                    ButtonLeft.IsEnabled = _deferralDeadline > TimeSpan.Zero;

                    // Update text color based on remaining time
                    if (_deferralDeadline > TimeSpan.FromDays(1))
                    {
                        // Less than 1 deferral remaining - use caution color
                        DeferralDeadlineValueTextBlock.Foreground = Application.Current.Resources["SystemFillColorCautionBrush"] as Brush;
                    }

                }
                else
                {
                    ButtonLeft.IsEnabled = false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in UpdateDeferralValues: {ex.Message}");
            }
        }

        private async Task StartProcessEvaluationLoopAsync(List<AppProcessInfo> initialApps, CancellationToken token)
        {
            var stopwatch = new Stopwatch();

            try
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        // Wait based on adaptive delay
                        await Task.Delay(_processEvaluationDelay, token);

                        // Skip if we're in the process of closing the dialog
                        if (_isProcessing || _isDisposed)
                            break;

                        stopwatch.Restart();

                        // Acquire lock for thread safety
                        await _processEvaluationLock.WaitAsync(token);

                        try
                        {
                            // Asynchronously evaluate running processes
                            List<AppProcessInfo> updatedApps = await _processEvaluationService!.EvaluateRunningProcessesAsync(initialApps, token).ConfigureAwait(false);

                            // Check if there's any change compared to the previous list
                            if (!AreProcessListsEqual(_previousProcessInfo, updatedApps))
                            {
                                // Update the collection on the UI thread
                                await Dispatcher.InvokeAsync(() =>
                                {
                                    if (_isDisposed) return;

                                    AppsToCloseCollection.Clear();
                                    foreach (var app in updatedApps)
                                    {
                                        if (AppsToCloseCollection.FirstOrDefault(a => a.ProcessName.Equals(app.ProcessName, StringComparison.OrdinalIgnoreCase)) == null)
                                        {
                                            AppsToCloseCollection.Add(app);
                                        }
                                    }
                                }, DispatcherPriority.Background);

                                // Update the previous process info for the next comparison
                                _previousProcessInfo = new List<AppProcessInfo>(updatedApps);
                            }

                            // If no more apps to close, exit the loop
                            if (updatedApps.Count == 0)
                            {
                                break;
                            }
                        }
                        finally
                        {
                            // Release the lock
                            _processEvaluationLock.Release();
                        }

                        stopwatch.Stop();

                        // Adjust delay based on evaluation time
                        if (stopwatch.ElapsedMilliseconds > 500)
                        {
                            // Evaluation is slow, increase delay
                            _processEvaluationDelay = TimeSpan.FromSeconds(
                                Math.Min(_processEvaluationDelay.TotalSeconds * 1.5, MAX_DELAY_SECONDS));
                        }
                        else if (stopwatch.ElapsedMilliseconds < 100)
                        {
                            // Evaluation is fast, decrease delay slightly
                            _processEvaluationDelay = TimeSpan.FromSeconds(
                                Math.Max(_processEvaluationDelay.TotalSeconds * 0.9, MIN_DELAY_SECONDS));
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // Rethrow to be caught by outer handler
                        throw;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error during process evaluation iteration: {ex.Message}");

                        // Continue the loop unless we're being canceled
                        if (token.IsCancellationRequested)
                            break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Task was canceled, no action needed
                Debug.WriteLine("Process evaluation loop was canceled");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Critical error in process evaluation loop: {ex.Message}");
                // Consider logging to a more permanent store
            }
        }

        private static bool AreProcessListsEqual(List<AppProcessInfo> list1, List<AppProcessInfo> list2)
        {
            if (list1.Count != list2.Count)
                return false;

            // Order the lists to ensure consistent comparison
            var sortedList1 = list1.OrderBy(app => app.ProcessName).ToList();
            var sortedList2 = list2.OrderBy(app => app.ProcessName).ToList();

            for (int i = 0; i < sortedList1.Count; i++)
            {
                if (!string.Equals(sortedList1[i].ProcessName, sortedList2[i].ProcessName, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }

        private void ProcessEvaluationService_ProcessStarted(object? sender, AppProcessInfo e)
        {
            if (e == null || DialogType != DialogType.CloseApps || _isDisposed)
                return;

            try
            {
                // Check if the process is already in the collection to avoid duplicates
                Dispatcher.Invoke(() =>
                {
                    if (_isDisposed) return;

                    if (!AppsToCloseCollection.Contains(e))
                    {
                        var existingApp = AppsToCloseCollection.FirstOrDefault(a =>
                            a.ProcessName.Equals(e.ProcessName, StringComparison.OrdinalIgnoreCase));

                        if (existingApp == null)
                        {
                            AppsToCloseCollection.Add(e);
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ProcessEvaluationService_ProcessStarted: {ex.Message}");
            }
        }

        private void ProcessEvaluationService_ProcessExited(object? sender, AppProcessInfo e)
        {
            if (e == null || DialogType != DialogType.CloseApps || _isDisposed)
                return;

            try
            {
                // Add to recently removed cache to prevent flickering
                lock (_recentlyRemovedProcesses)
                {
                    _recentlyRemovedProcesses[e.ProcessName] = DateTime.Now;
                }

                Dispatcher.Invoke(() =>
                {
                    if (_isDisposed) return;

                    var processToRemove = AppsToCloseCollection.FirstOrDefault(a =>
                        a.ProcessName.Equals(e.ProcessName, StringComparison.OrdinalIgnoreCase));

                    if (processToRemove != null)
                    {
                        // Animation logic removed - handled by UnifiedAdtApplication.RemoveAppToClose
                        AppsToCloseCollection.Remove(processToRemove);
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ProcessEvaluationService_ProcessExited: {ex.Message}");
            }
        }

        #region Progress Dialog Features

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

        #endregion Progress Dialog Features

        #region Countdown Features

        /// <summary>
        /// Initializes the countdown timer and display for dialogs that support it (CloseApps, Restart).
        /// </summary>
        /// <param name="duration">The total duration of the countdown.</param>
        private void InitializeCountdown(TimeSpan duration)
        {
            try
            {
                _countdownRemainingTime = duration;

                // Update the display initially
                UpdateCountdownDisplay();

                // Set up the timer to update every second
                _countdownTimer = new Timer(CountdownTimerCallback, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in InitializeCountdown: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates the countdown text display and adjusts text color based on remaining time.
        /// Handles disabling the dismiss button for Restart dialogs based on `countdownNoMinimizeDuration`.
        /// </summary>
        private void UpdateCountdownDisplay()
        {
            try
            {
                // Format the remaining time as hh:mm:ss
                CountdownValueTextBlock.Text = $"{_countdownRemainingTime.Hours}h {_countdownRemainingTime.Minutes}m {_countdownRemainingTime.Seconds}s";

                // Update accessibility properties
                AutomationProperties.SetName(CountdownValueTextBlock, $"Time remaining: {_countdownRemainingTime.Hours} hours, {_countdownRemainingTime.Minutes} minutes, {_countdownRemainingTime.Seconds} seconds");

                // Update text color based on remaining time
                if (_countdownRemainingTime.TotalSeconds <= 60)
                {
                    // Less than 60 seconds - use critical color
                    CountdownValueTextBlock.Foreground = Application.Current.Resources["SystemFillColorCriticalBrush"] as Brush;
                }
                else if (_countdownNoMinimizeDuration.HasValue && _countdownRemainingTime <= _countdownNoMinimizeDuration)
                {
                    // Less than no-minimize duration - use attention color
                    CountdownValueTextBlock.Foreground = Application.Current.Resources["SystemFillColorCautionBrush"] as Brush;
                }
                else
                {
                    // Normal time - use default text color
                    CountdownValueTextBlock.Foreground = Application.Current.Resources["TextFillColorPrimaryBrush"] as Brush;
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
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in UpdateCountdownDisplay: {ex.Message}");
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

            try
            {
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
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in CountdownTimerCallback: {ex.Message}");
            }
        }

        private void ButtonLeft_Click(object sender, RoutedEventArgs e)
        {
            if (_isDisposed)
                return;

            try
            {
                switch (DialogType)
                {
                    case DialogType.CloseApps:
                        if (_deferralsRemaining.HasValue && _deferralsRemaining > 0)
                        {
                            _deferralsRemaining--;
                            UpdateDeferralValues();
                        }
                        DialogResult = "Defer";
                        break;

                    case DialogType.Restart:
                        DialogResult = "Dismiss";
                        // Just minimize the window instead of closing
                        this.WindowState = WindowState.Minimized;
                        return; // Don't close the dialog

                    case DialogType.Input:
                        DialogResult = (ButtonLeft.Content as AccessText)?.Text.Replace("_", "") ?? "ButtonLeft"; // Store button text as result
                        _inputTextResult = InputBoxText.Text; // Capture input text
                        break;

                    case DialogType.Custom:
                    default:
                        DialogResult = (ButtonLeft.Content as AccessText)?.Text.Replace("_", "") ?? "ButtonLeft"; // Store button text as result
                        break;
                }

                // Only close if not minimizing (Restart dialog)
                CloseDialog(null);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ButtonLeft_Click: {ex.Message}");
            }
        }

        private void ButtonMiddle_Click(object sender, RoutedEventArgs e)
        {
            if (_isDisposed)
                return;

            try
            {
                DialogResult = (ButtonMiddle.Content as AccessText)?.Text.Replace("_", "") ?? "ButtonMiddle"; // Store button text as result
                if (DialogType == DialogType.Input)
                {
                    _inputTextResult = InputBoxText.Text; // Capture input text
                }
                CloseDialog(null);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ButtonMiddle_Click: {ex.Message}");
            }
        }

        private void ButtonRight_Click(object sender, RoutedEventArgs e)
        {
            if (_isDisposed)
                return;

            try
            {
                switch (DialogType)
                {
                    case DialogType.CloseApps:
                        DialogResult = "Continue";
                        break;

                    case DialogType.Restart:
                        DialogResult = "Restart";
                        break;

                    case DialogType.Input:
                        DialogResult = (ButtonRight.Content as AccessText)?.Text.Replace("_", "") ?? "ButtonRight"; // Store button text as result
                        _inputTextResult = InputBoxText.Text; // Capture input text
                        break;

                    case DialogType.Custom:
                    default:
                        DialogResult = (ButtonRight.Content as AccessText)?.Text.Replace("_", "") ?? "ButtonRight"; // Store button text as result
                        break;
                }

                CloseDialog(null);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ButtonRight_Click: {ex.Message}");
            }
        }

        /// <summary>
        /// Closes the dialog window and cancels associated operations. Can be called by timers or button clicks.
        /// </summary>
        /// <param name="state">State object, typically from a timer callback (not used).</param>
        public void CloseDialog(object? state)
        {
            // If we're already processing, just return
            if (_isProcessing || _isDisposed)
                return;

            _canClose = true;
            _isProcessing = true;

            try
            {
                Dispatcher.Invoke(() =>
                {
                    try
                    {
                        // Cancel all operations
                        _dialogCancellationTokenSource.Cancel();

                        // Close the dialog
                        Close();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error in CloseDialog inner: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in CloseDialog: {ex.Message}");
            }
        }

        #endregion Countdown Features

        #region Disposal and Cleanup

        /// <summary>
        /// Prevents the user from closing the app via the taskbar
        /// </summary>
        protected override void OnClosing(CancelEventArgs e)
        {
            if (_isDisposed)
                return;

            e.Cancel = !_canClose; // Prevent the window from closing unless explicitly allowed in code
                                   // This is to prevent the user from closing the dialog via taskbar
        }

        /// <summary>
        /// Clean up resources when the window is closed
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            Dispose();
        }

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
        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed)
                return;

            _isDisposed = true;

            if (disposing)
            {
                try
                {
                    // Animation-related cleanup removed

                    // Dispose timers
                    _dialogExpiryTimer?.Dispose();
                    _countdownTimer?.Dispose();

                    // Cancel token sources
                    _dialogCancellationTokenSource.Cancel();
                    _dialogCancellationTokenSource.Dispose();
                    _processCancellationTokenSource?.Cancel();
                    _processCancellationTokenSource?.Dispose();

                    // Detach event handlers
                    Loaded -= UnifiedDialog_Loaded;
                    SizeChanged -= UnifiedDialog_SizeChanged;
                    AppsToCloseCollection.CollectionChanged -= AppsToCloseCollection_CollectionChanged;

                    // Animation timer event handler removed

                    if (_processEvaluationService != null && DialogType == DialogType.CloseApps)
                    {
                        _processEvaluationService.ProcessStarted -= ProcessEvaluationService_ProcessStarted;
                        _processEvaluationService.ProcessExited -= ProcessEvaluationService_ProcessExited;
                    }

                    // Dispose the semaphore
                    _processEvaluationLock.Dispose();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error in Dispose: {ex.Message}");
                }
            }
        }

        ~UnifiedDialog()
        {
            Dispose(false);
        }

        #endregion Disposal and Cleanup

        #region INotifyPropertyChanged implementation

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void UpdateButtonLayout()
        {
            try
            {
                // Build a list of visible buttons in the order they appear.
                var visibleButtons = new List<UIElement>();
                if (ButtonLeft.Visibility == Visibility.Visible)
                    visibleButtons.Add(ButtonLeft);
                if (ButtonMiddle.Visibility == Visibility.Visible)
                    visibleButtons.Add(ButtonMiddle);
                if (ButtonRight.Visibility == Visibility.Visible)
                    visibleButtons.Add(ButtonRight);

                // Clear any existing column definitions.
                ActionButtons.ColumnDefinitions.Clear();

                // Special case: if there's only one visible button, limit its width to half of the grid
                if (visibleButtons.Count == 1)
                {
                    // Add two columns - one for the button (50% width) and one empty (50% width)
                    ActionButtons.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    ActionButtons.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                    // Place the single button in the second column
                    System.Windows.Controls.Grid.SetColumn(visibleButtons[0], 1);

                    // Set appropriate margin
                    Wpf.Ui.Controls.Button button = (Wpf.Ui.Controls.Button)visibleButtons[0];
                    button.Margin = new Thickness(0, 0, 4, 0);
                }
                else
                {
                    // Create equally sized columns for each visible button (original behavior)
                    for (int i = 0; i < visibleButtons.Count; i++)
                    {
                        ActionButtons.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                        System.Windows.Controls.Grid.SetColumn(visibleButtons[i], i);

                        // Set margin based on position
                        Wpf.Ui.Controls.Button button = (Wpf.Ui.Controls.Button)visibleButtons[i];
                        if (i == 0)
                            button.Margin = new Thickness(0, 0, 4, 0);
                        else if (i == visibleButtons.Count - 1)
                            button.Margin = new Thickness(4, 0, 0, 0);
                        else
                            button.Margin = new Thickness(4, 0, 4, 0);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in UpdateButtonLayout: {ex.Message}");
            }
        }

        #endregion INotifyPropertyChanged implementation

        // Helper method to properly set button content with accelerator keys
        private void SetButtonContentWithAccelerator(Wpf.Ui.Controls.Button button, string? text)
        {
            if (string.IsNullOrEmpty(text))
                return;

            // Create AccessText to properly handle the underscore as accelerator
            AccessText accessText = new AccessText
            {
                Text = text
            };

            // Set the AccessText as button content
            button.Content = accessText;
        }

        // Helper method to format message text with clickable hyperlinks, supporting both plain URLs and Markdown-style links [text](url)
        private void FormatMessageWithHyperlinks(Wpf.Ui.Controls.TextBlock textBlock, string message)
        {
            textBlock.Inlines.Clear();
            if (string.IsNullOrEmpty(message)) return;

            // Regex to find Markdown links `[text](url)` or plain URLs.
            // Group 1: Full Markdown link (optional)
            // Group 2: Link text from Markdown (optional)
            // Group 3: URL from Markdown (optional)
            // Group 4: Full plain URL (optional)
            var linkRegex = new Regex(
                @"(\[([^\]]+)\]\(([^)\s]+)\))" + // Markdown link: [text](url)
                @"|" +
                @"((?i)\b(?:(?:https?|ftp|mailto):(?://)?|www\.|ftp\.)[-A-Z0-9+&@#/%?=~_|$!:,.;]*[A-Z0-9+&@#/%=~_|$])", // Plain URL
                RegexOptions.Compiled);

            int lastPos = 0;
            foreach (Match match in linkRegex.Matches(message))
            {
                // Add text before the hyperlink
                if (match.Index > lastPos)
                {
                    textBlock.Inlines.Add(new Run(message.Substring(lastPos, match.Index - lastPos)));
                }

                string url;
                string displayText;

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
                if (!navigateUrl.Contains("://") && !navigateUrl.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
                {
                    if (navigateUrl.StartsWith("www.", StringComparison.OrdinalIgnoreCase) || navigateUrl.StartsWith("ftp.", StringComparison.OrdinalIgnoreCase))
                    {
                        navigateUrl = "http://" + navigateUrl; // Assume http for www/ftp starts if no scheme
                    }
                    // else - if it doesn't start with a known prefix and has no scheme, it might not be a valid URL to open
                }

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
            if (lastPos < message.Length)
            {
                textBlock.Inlines.Add(new Run(message.Substring(lastPos)));
            }
        }

        // Event handler for hyperlink clicks
        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                // Use ShellExecute to open the URL in the default browser/handler
                Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                // Log or handle the error (e.g., show a message box)
                Debug.WriteLine($"Could not open hyperlink: {e.Uri}. Error: {ex.Message}");
            }
            e.Handled = true; // Mark the event as handled
        }

        #endregion CloseApps Dialog Features
    }
}
