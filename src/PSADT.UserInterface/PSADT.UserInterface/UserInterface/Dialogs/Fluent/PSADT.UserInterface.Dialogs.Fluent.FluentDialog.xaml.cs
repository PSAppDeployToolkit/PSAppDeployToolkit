using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using PSADT.UserInterface.Services;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace PSADT.UserInterface.Dialogs.Fluent
{
    /// <summary>
    /// Unified dialog for PSAppDeployToolkit that consolidates all dialog types into one
    /// </summary>
    public partial class FluentDialog : FluentWindow, IDisposable, INotifyPropertyChanged
    {
        // Dialog Expiry and Cancellation
        private readonly CancellationTokenSource _dialogCancellationTokenSource;

        private TimeSpan? _dialogExpiryDuration;
        private readonly Timer? _dialogExpiryTimer;
        private DialogPosition? _dialogPosition;
        private bool _dialogAllowMove;
        private bool _dialogTopMost;

        // Countdown Handling and Timers
        private TimeSpan? _countdownDuration;
        private TimeSpan? _countdownNoMinimizeDuration; // Minutes before the end when minimize is disabled
        private Timer? _countdownTimer;
        private TimeSpan _countdownRemainingTime;

        // Deferrals Handling and Timers
        private DateTime? _deferralDeadline;
        private TimeSpan? _deferralDeadlineRemainingTime;
        private int? _deferralsRemaining;

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
        private bool _isProcessing = false;
        private bool _isDisposed = false;
        private bool _canClose = false;

        // Icon cache for improved performance
        private static readonly Dictionary<string, BitmapImage> _iconCache = new();

        // Store original and alternative texts
        private string? _originalMessage;

        private string? _alternativeMessage;
        private string? _buttonLeftOriginalText;
        private string? _buttonRightOriginalText;
        private string? _buttonRightAlternativeText;
        private string? _inputTextResult; // To store text from Input dialog

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

        /// <summary>
        /// Initializes a new instance of FluentDialog
        /// </summary>
        /// <param name="dialogType">Type of dialog to display</param>
        /// <param name="dialogExpiryDuration">Optional duration after which the dialog will automatically close</param>
        /// <param name="dialogAccentColor">Optional accent color for the dialog</param>
        /// <param name="dialogPosition">Position of the window on screen (default: BottomRight)</param>
        /// <param name="dialogTopMost">Whether the dialog should stay on top of other windows</param>
        /// <param name="dialogAllowMove">Whether to allow the user to move the window (default: false)</param>
        public FluentDialog(DialogType dialogType, TimeSpan? dialogExpiryDuration = null, String? dialogAccentColor = null, DialogPosition? dialogPosition = DialogPosition.BottomRight, bool? dialogTopMost = false, bool? dialogAllowMove = false)
        {
            DataContext = this;

            if (dialogAccentColor != null && dialogAccentColor != string.Empty)
                {
                    try
                    {
                        SystemThemeWatcher.Watch(this, WindowBackdropType.Acrylic, false);

                        // Apply the accent color to the application theme
                        var colorDialogAccentColor = StringToColor(dialogAccentColor);
                        ApplicationTheme appTheme = ApplicationThemeManager.GetAppTheme();
                        ApplicationAccentColorManager.Apply(colorDialogAccentColor, appTheme, true);


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

                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Failed to apply accent color: {ex.Message}");
                    }
                }
            else
            {
                SystemThemeWatcher.Watch(this, WindowBackdropType.Acrylic, true);
            }

            InitializeComponent();

            _dialogCancellationTokenSource = new CancellationTokenSource();

            _dialogType = dialogType;
            _dialogExpiryDuration = dialogExpiryDuration ?? TimeSpan.FromMinutes(55);
            _dialogPosition = dialogPosition ?? DialogPosition.BottomRight;
            _dialogAllowMove = dialogAllowMove ?? false;
            _dialogTopMost = dialogTopMost ?? false;




            // Configure window events
            Loaded += FluentDialog_Loaded;
            SizeChanged += FluentDialog_SizeChanged;
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
                    Loaded -= FluentDialog_Loaded;
                    SizeChanged -= FluentDialog_SizeChanged;
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

        ~FluentDialog()
        {
            Dispose(false);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
