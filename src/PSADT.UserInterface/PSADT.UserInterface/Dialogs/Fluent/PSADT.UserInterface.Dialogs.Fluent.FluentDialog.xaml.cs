using System.Collections;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Windows.Win32;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace PSADT.UserInterface.Dialogs.Fluent
{
    /// <summary>
    /// Unified dialog for PSAppDeployToolkit that consolidates all dialog types into one
    /// </summary>
    internal abstract partial class FluentDialog : FluentWindow, IDisposable, INotifyPropertyChanged
    {
        /// <summary>
        /// Initializes a new instance of FluentDialog
        /// </summary>
        /// <param name="options">Mandatory options needed to construct the window.</param>
        private protected FluentDialog(DialogOptions options, string? customMessageText = null, TimeSpan? countdownDuration = null, TimeSpan? countdownNoMinimizeDuration = null, string? countdownDialogResult = null)
        {
            // Set up the context for data binding
            DataContext = this;

            // Process the given accent color from the options
            if (!string.IsNullOrWhiteSpace(options.DialogAccentColor))
            {
                // Don't update the window accent as we're setting it manually
                SystemThemeWatcher.Watch(this, WindowBackdropType.Acrylic, false);

                // Apply the accent color to the application theme
                ApplicationAccentColorManager.Apply(StringToColor(options.DialogAccentColor!), ApplicationThemeManager.GetAppTheme(), true);

                // Update the accent color in the theme dictionary
                // See https://github.com/lepoco/wpfui/issues/1188 for more info.
                var brushes = new Dictionary<string, SolidColorBrush>
                {
                    ["SystemAccentColor"] = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Application.Current.Resources["SystemAccentColor"]),
                    ["SystemAccentColorPrimary"] = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Application.Current.Resources["SystemAccentColorPrimary"]),
                    ["SystemAccentColorSecondary"] = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Application.Current.Resources["SystemAccentColorSecondary"]),
                    ["SystemAccentColorTertiary"] = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Application.Current.Resources["SystemAccentColorTertiary"])
                };
                ResourceDictionary themeDictionary = System.Windows.Application.Current.Resources.MergedDictionaries.First(static d => d.Source.AbsolutePath.StartsWith("/Wpf.Ui;component/Resources/Theme/"));
                var converter = new ResourceReferenceExpressionConverter();
                foreach (DictionaryEntry entry in themeDictionary)
                {
                    if (entry.Value is SolidColorBrush brush)
                    {
                        var dynamicColor = brush.ReadLocalValue(SolidColorBrush.ColorProperty);
                        if (dynamicColor is not System.Windows.Media.Color &&
                            converter.ConvertTo(dynamicColor, typeof(MarkupExtension)) is DynamicResourceExtension dynamicResource &&
                            brushes.ContainsKey((string)dynamicResource.ResourceKey))
                        {
                            themeDictionary[entry.Key] = brushes[(string)dynamicResource.ResourceKey];
                        }
                    }
                }
            }
            else
            {
                // Update the window accent based on the current theme
                SystemThemeWatcher.Watch(this, WindowBackdropType.Acrylic, true);
            }

            // Initialize the window
            InitializeComponent();

            // Set basic properties
            Title = options.AppTitle;
            AppTitleTextBlock.Text = options.AppTitle;
            SubtitleTextBlock.Text = options.Subtitle;

            // Set accessibility properties
            AutomationProperties.SetName(this, options.AppTitle);

            // Set remaining properties from the options
            _dialogPosition = options.DialogPosition;
            WindowStartupLocation = WindowStartupLocation.Manual;
            _dialogAllowMove = options.DialogAllowMove;
            Topmost = options.DialogTopMost;
            _dialogExpiryTimer = new System.Threading.Timer(CloseDialog, null, options.DialogExpiryDuration, Timeout.InfiniteTimeSpan);

            // Set supplemental options also
            _customMessageText = customMessageText;
            _countdownDuration = countdownDuration;
            _countdownNoMinimizeDuration = countdownNoMinimizeDuration;
            CountdownStackPanel.Visibility = _countdownDuration.HasValue ? Visibility.Visible : Visibility.Collapsed;

            // Pre-format the custom message if we have one
            FormatMessageWithHyperlinks(CustomMessageTextBlock, _customMessageText);
            CustomMessageTextBlock.Visibility = string.IsNullOrWhiteSpace(_customMessageText) ? Visibility.Collapsed : Visibility.Visible;

            // Set everything to not visible by default, it's up to the derived class to enable what they need.
            CloseAppsStackPanel.Visibility = Visibility.Collapsed;
            CloseAppsSeparator.Visibility = Visibility.Collapsed;
            ProgressStackPanel.Visibility = Visibility.Collapsed;
            InputBoxStackPanel.Visibility = Visibility.Collapsed;
            DeferStackPanel.Visibility = Visibility.Collapsed;
            ButtonPanel.Visibility = Visibility.Collapsed;
            ButtonLeft.Visibility = Visibility.Collapsed;
            ButtonMiddle.Visibility = Visibility.Collapsed;
            ButtonRight.Visibility = Visibility.Collapsed;

            // Set app icon
            SetAppIcon(options.AppIconImage);

            // Initialize countdown if specified
            if (countdownDuration.HasValue)
            {
                //InitializeCountdown(countdownDuration.Value);
            }

            // Configure window events
            Loaded += FluentDialog_Loaded;
            SizeChanged += FluentDialog_SizeChanged;
        }

        /// <summary>
        /// Closes the dialog window and cancels associated operations. Can be called by timers or button clicks.
        /// </summary>
        /// <param name="state">State object, typically from a timer callback (not used).</param>
        internal void CloseDialog(object? state)
        {
            // If we're already processing, just return.
            if (_disposed)
            {
                return;
            }
            _canClose = true;
            Dispatcher.Invoke(Close);
        }

        /// <summary>
        /// Raises the PropertyChanged event for the specified property.
        /// </summary>
        /// <param name="propertyName"></param>
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Prevent window movement by handling WM_SYSCOMMAND
        /// </summary>
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == PInvoke.WM_SYSCOMMAND)
            {
                int command = wParam.ToInt32() & 0xfff0;
                if (command == PInvoke.SC_MOVE && !_dialogAllowMove)
                {
                    handled = true;
                }
            }
            return IntPtr.Zero;
        }

        /// <summary>
        /// The result of the dialog interaction.
        /// </summary>
        internal new string DialogResult
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
        private string _dialogResult = "Timeout";

        /// <summary>
        /// Whether this window has been disposed.
        /// </summary>
        private bool _disposed = false;

        /// <summary>
        /// Whether this window is able to be closed.
        /// </summary>
        private bool _canClose = false;

        /// <summary>
        /// The specified position of the dialog.
        /// </summary>
        private readonly DialogPosition _dialogPosition;

        /// <summary>
        /// Whether the dialog is allowed to be moved.
        /// </summary>
        private readonly bool _dialogAllowMove;

        /// <summary>
        /// The countdown timer for the dialog to automatically close.
        /// </summary>
        private readonly System.Threading.Timer _dialogExpiryTimer;

        /// <summary>
        /// An optional countdown to zero to commence a preferred action.
        /// </summary>
        private readonly TimeSpan? _countdownDuration;

        /// <summary>
        /// An optional countdown to zero for when the dialog can be no longer minimised.
        /// </summary>
        private readonly TimeSpan? _countdownNoMinimizeDuration;

        /// <summary>
        /// Icon cache for improved performance
        /// </summary>
        private static readonly Dictionary<string, BitmapImage> _iconCache = [];

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
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;

            if (!disposing)
            {
                return;
            }

            // Dispose timers
            _dialogExpiryTimer.Dispose();

            // Detach event handlers
            Loaded -= FluentDialog_Loaded;
            SizeChanged -= FluentDialog_SizeChanged;
        }
    }
}
