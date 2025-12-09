using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using PSADT.ProcessManagement;
using PSADT.SafeHandles;
using PSADT.UserInterface.DialogOptions;
using PSADT.UserInterface.DialogResults;
using PSADT.UserInterface.DialogState;
using PSADT.UserInterface.Types;
using PSADT.UserInterface.Utilities;
using iNKORE.UI.WPF.Modern;
using iNKORE.UI.WPF.Modern.Controls.Primitives;

namespace PSADT.UserInterface.Dialogs.Fluent
{
    /// <summary>
    /// A fluent implementation of PSAppDeployToolkit's CloseApps dialog.
    /// </summary>
    internal sealed class CloseAppsDialog : FluentDialog, IModalDialog
    {
        /// <summary>
        /// The required data for displaying an app to close on the CloseAppsDialog.
        /// This class is deliberately public as it's required by WPF to be so.
        /// </summary>
        public sealed class AppToClose
        {
            /// <summary>
            /// Constructor for the ProcessToClose class.
            /// </summary>
            /// <param name="processToClose"></param>
            public AppToClose(ProcessToClose processToClose)
            {
                Name = Path.GetFileName(processToClose.Path).ToLowerInvariant();
                Description = processToClose.Description;
                Icon = GetAppIcon(processToClose.Path) ?? throw new ArgumentNullException("Could not retrieve an icon for the given application.", (Exception?)null);
                if (string.IsNullOrWhiteSpace(Name))
                {
                    throw new ArgumentNullException("Process name cannot be null or empty.", (Exception?)null);
                }
                if (string.IsNullOrWhiteSpace(Description))
                {
                    throw new ArgumentNullException("Process description cannot be null or empty.", (Exception?)null);
                }
            }

            /// <summary>
            /// The name of the process to close.
            /// </summary>
            public string Name { get; }

            /// <summary>
            /// The description of the process to close.
            /// </summary>
            public string Description { get; }

            /// <summary>
            /// The icon of the process to close.
            /// </summary>
            public BitmapSource Icon { get; }
        }

        /// <summary>
        /// Instantiates a new CloseApps dialog.
        /// </summary>
        /// <param name="options">Mandatory options needed to construct the window.</param>
        /// <param name="state">Optional state values for the dialog.</param>
        internal CloseAppsDialog(CloseAppsDialogOptions options, CloseAppsDialogState state) : base(options, options.CustomMessageText, options.CountdownDuration, null, state.CountdownStopwatch)
        {
            // Set up the context for data binding
            DataContext = this;

            // Store original and alternative texts
            _continueOnProcessClosure = options.ContinueOnProcessClosure;
            _closeAppsNoProcessesMessageText = options.Strings.Fluent.DialogMessageNoProcesses;
            _closeAppsMessageText = options.Strings.Fluent.DialogMessage;
            _buttonLeftText = options.Strings.Fluent.ButtonLeftText;
            _buttonLeftNoProcessesText = options.Strings.Fluent.ButtonLeftTextNoProcesses;
            _deferralsRemaining = options.DeferralsRemaining;
            _deferralDeadline = options.DeferralDeadline;
            _forcedCountdown = options.ForcedCountdown;
            _hideCloseButton = options.HideCloseButton;

            // Set up UI
            FormatMessageWithHyperlinks(MessageTextBlock, _closeAppsNoProcessesMessageText);
            DeferRemainingStackPanel.Visibility = _deferralsRemaining.HasValue && !options.UnlimitedDeferrals ? Visibility.Visible : Visibility.Collapsed;
            DeferRemainingHeadingTextBlock.Text = options.Strings.Fluent.DeferralsRemaining;
            DeferDeadlineStackPanel.Visibility = _deferralDeadline.HasValue ? Visibility.Visible : Visibility.Collapsed;
            DeferDeadlineHeadingTextBlock.Text = options.Strings.Fluent.DeferralDeadline;
            CountdownHeadingTextBlock.Text = options.Strings.Fluent.AutomaticStartCountdown;
            CountdownDeferPanelSeparator.Visibility = (_deferralsRemaining.HasValue || _deferralDeadline.HasValue) ? Visibility.Visible : Visibility.Collapsed;
            ButtonPanel.Visibility = Visibility.Visible;

            // Configure buttons
            SetButtonContentWithAccelerator(ButtonRight, options.Strings.Fluent.ButtonRightText);
            AutomationProperties.SetName(ButtonRight, options.Strings.Fluent.ButtonRightText);
            ButtonRight.Visibility = _deferralsRemaining.HasValue || _deferralDeadline.HasValue ? Visibility.Visible : Visibility.Collapsed;
            ButtonLeft.Visibility = Visibility.Visible;
            SetDefaultButton(ButtonLeft);
            SetAccentButton(ButtonLeft);

            // Allow the dialog to be minimised if specified.
            if (options.DialogAllowMinimize)
            {
                SetMinimizeButtonAvailability(TitleBarButtonAvailability.Enabled);
            }

            // Set up/process optional values.
            if (state.RunningProcessService is not null)
            {
                _runningProcessService = state.RunningProcessService;
                AppsToCloseCollection.ResetItems(_runningProcessService.ProcessesToClose.Select(static p => new AppToClose(p)), true);
                AppsToCloseCollection.CollectionChanged += AppsToCloseCollection_CollectionChanged;
            }
            UpdateRunningProcesses();
            if (state.LogWriter is not null)
            {
                _logWriter = state.LogWriter;
            }
            UpdateDeferralValues();

            // Set the dialog result to a default value.
            DialogResult = CloseAppsDialogResult.Timeout;
        }

        /// <summary>
        /// Determines whether deferrals are currently available.
        /// </summary>
        /// <returns><see langword="true"/> if there are remaining deferrals or a deferral deadline is set; otherwise, <see langword="false"/>.</returns>
        private bool DeferralsAvailable() => _deferralsRemaining.HasValue || _deferralDeadline.HasValue;

        /// <summary>
        /// Updates the deferral values displayed in the dialog.
        /// </summary>
        private void UpdateDeferralValues()
        {
            // First handle default case - if no deferral settings, just disable the button
            if (!DeferralsAvailable())
            {
                ButtonRight.IsEnabled = false;
                return;
            }

            // Handle deferral values
            if (_deferralsRemaining.HasValue)
            {
                // Only enable the button if there are deferrals remaining
                ButtonRight.IsEnabled = _deferralsRemaining > 0;

                // Update text value
                DeferRemainingValueTextBlock.Text = _deferralsRemaining.Value.ToString(CultureInfo.CurrentCulture);

                // Update accessibility properties
                AutomationProperties.SetName(DeferRemainingValueTextBlock, _deferralsRemaining.Value.ToString(CultureInfo.CurrentCulture));

                // Update text color based on remaining deferrals
                if (_deferralsRemaining == 0)
                {
                    DeferRemainingValueTextBlock.SetResourceReference(ForegroundProperty, ThemeKeys.SystemFillColorCriticalBrushKey);
                    DeferRemainingValueTextBlock.FontWeight = FontWeights.ExtraBold;
                }
                else if (_deferralsRemaining <= 1)
                {
                    DeferRemainingValueTextBlock.SetResourceReference(ForegroundProperty, ThemeKeys.SystemFillColorCautionBrushKey);
                    DeferRemainingValueTextBlock.FontWeight = FontWeights.ExtraBold;
                }
            }
            if (_deferralDeadline.HasValue)
            {
                // Set button state based on deadline
                TimeSpan timeRemaining = _deferralDeadline!.Value - DateTime.Now;
                ButtonRight.IsEnabled = timeRemaining > TimeSpan.Zero;

                // Update text content
                DateTimeFormatInfo dateTimeFormatInfo = new();
                DateTimeOffset deferralDeadlineOffset = new((DateTime)_deferralDeadline!);
                string displayText = deferralDeadlineOffset.ToLocalTime().ToString("f", CultureInfo.CurrentCulture);
                if (ButtonRight.IsEnabled)
                {
                    if (timeRemaining < TimeSpan.FromDays(1))
                    {
                        // Less than 1 day remaining - use caution color
                        DeferDeadlineValueTextBlock.SetResourceReference(ForegroundProperty, ThemeKeys.SystemFillColorCautionBrushKey);
                        DeferDeadlineValueTextBlock.FontWeight = FontWeights.ExtraBold;
                    }
                }
                else
                {
                    DeferDeadlineValueTextBlock.SetResourceReference(ForegroundProperty, ThemeKeys.SystemFillColorCriticalBrushKey);
                    DeferDeadlineValueTextBlock.FontWeight = FontWeights.ExtraBold;
                }
                DeferDeadlineValueTextBlock.Text = displayText;
                AutomationProperties.SetName(DeferDeadlineValueTextBlock, displayText);
            }
        }

        /// <summary>
        /// Handles the event when the list of processes to close changes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RunningProcessService_ProcessesToCloseChanged(object? sender, ProcessesToCloseChangedEventArgs e) => Dispatcher.Invoke(() => AppsToCloseCollection.ResetItems(e.ProcessesToClose.Select(p => new AppToClose(p))));

        /// <summary>
        /// Handles the event when the collection of apps to close changes.
        /// </summary>
        private void UpdateRunningProcesses()
        {
            // Update the UI based on the changes in the collection.
            AutomationProperties.SetName(CloseAppsListView, $"Applications to Close: {AppsToCloseCollection.Count} items");
            UpdateRowDefinition();
            if (AppsToCloseCollection.Count > 0)
            {
                if (null != _logWriter)
                {
                    _logWriter.Write($"The running processes have changed. Updating the apps to close: ['{string.Join("', '", AppsToCloseCollection.Select(static a => a.Description))}']...");
                    _logWriter.Flush();
                }
                FormatMessageWithHyperlinks(MessageTextBlock, _closeAppsMessageText);
                CloseAppsStackPanel.Visibility = Visibility.Visible;
                if (!_hideCloseButton)
                {
                    SetButtonContentWithAccelerator(ButtonLeft, _buttonLeftText);
                    AutomationProperties.SetName(ButtonLeft, _buttonLeftText);
                    ButtonLeft.IsEnabled = true;
                }
                else
                {
                    SetButtonContentWithAccelerator(ButtonLeft, _buttonLeftNoProcessesText);
                    AutomationProperties.SetName(ButtonLeft, _buttonLeftNoProcessesText);
                    ButtonLeft.IsEnabled = false;
                }
            }
            else
            {
                if (null != _logWriter)
                {
                    _logWriter.Write("Previously detected running processes are no longer running.");
                    _logWriter.Flush();
                }
                FormatMessageWithHyperlinks(MessageTextBlock, _closeAppsNoProcessesMessageText);
                SetButtonContentWithAccelerator(ButtonLeft, _buttonLeftNoProcessesText);
                AutomationProperties.SetName(ButtonLeft, _buttonLeftNoProcessesText);
                CloseAppsStackPanel.Visibility = Visibility.Collapsed;
                ButtonLeft.IsEnabled = true;
                if (_continueOnProcessClosure)
                {
                    ButtonLeft.RaiseEvent(new(Button.ClickEvent));
                }
            }
        }

        /// <summary>
        /// Handles the event when the collection of apps to close changes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AppsToCloseCollection_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => UpdateRunningProcesses();

        /// <summary>
        /// Handles the loading event of the dialog.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void FluentDialog_Loaded(object sender, RoutedEventArgs e)
        {
            // Call the base method to ensure proper loading.
            base.FluentDialog_Loaded(sender, e);

            // Initialize the running process service and set up event handlers.
            if (null != _runningProcessService)
            {
                _runningProcessService.ProcessesToCloseChanged += RunningProcessService_ProcessesToCloseChanged;
            }
        }

        /// <summary>
        /// Handles the click event of the left button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void ButtonLeft_Click(object sender, RoutedEventArgs e)
        {
            // Set the result and call base method to handle window closure.
            if (AutomationProperties.GetName(ButtonLeft) == _buttonLeftText)
            {
                DialogResult = CloseAppsDialogResult.Close;
            }
            else
            {
                DialogResult = CloseAppsDialogResult.Continue;
            }
            base.ButtonLeft_Click(sender, e);
        }

        /// <summary>
        /// Handles the click event of the right button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void ButtonRight_Click(object sender, RoutedEventArgs e)
        {
            // Set the result and call base method to handle window closure.
            DialogResult = CloseAppsDialogResult.Defer;
            base.ButtonRight_Click(sender, e);
        }

        /// <summary>
        /// Handles the click event of the close button.
        /// </summary>
        /// <param name="state"></param>
        protected override void CountdownTimer_Tick(object? state)
        {
            // Call the base timer and test local expiration.
            base.CountdownTimer_Tick(state);
            if (_countdownStopwatch.Elapsed >= _countdownDuration)
            {
                Dispatcher.Invoke(() =>
                {
                    if (_forcedCountdown && (null == _runningProcessService || AutomationProperties.GetName(ButtonLeft) == _buttonLeftNoProcessesText))
                    {
                        DialogResult = CloseAppsDialogResult.Continue;
                    }
                    else if (_forcedCountdown && DeferralsAvailable())
                    {
                        DialogResult = CloseAppsDialogResult.Defer;
                    }
                    else
                    {
                        if (AutomationProperties.GetName(ButtonLeft) == _buttonLeftText)
                        {
                            DialogResult = CloseAppsDialogResult.Close;
                        }
                        else
                        {
                            DialogResult = CloseAppsDialogResult.Continue;
                        }
                    }
                    CloseDialog();
                });
            }
        }

        /// <summary>
        /// Gets the icon for a given process.
        /// </summary>
        /// <param name="appFilePath"></param>
        /// <returns></returns>
        private static BitmapSource GetAppIcon(string appFilePath)
        {
            // Try to get from cache first
            if (!_appIconCache.TryGetValue(appFilePath, out var bitmapSource))
            {
                // Get the icon as a bitmap from the executable, then turn it into a BitmapSource.
                Bitmap drawingBitmap;
                try
                {
                    drawingBitmap = DrawingUtilities.ExtractBitmapFromExecutable(appFilePath);
                }
                catch
                {
                    drawingBitmap = SystemIcons.Get(DialogSystemIcon.Application);
                }
                using (drawingBitmap)
                using (SafeGdiObjectHandle hBitmap = new(drawingBitmap.GetHbitmap(), true))
                {
                    bool hBitmapAddRef = false;
                    try
                    {
                        hBitmap.DangerousAddRef(ref hBitmapAddRef);
                        (bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(hBitmap.DangerousGetHandle(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions())).Freeze();
                        _appIconCache.Add(appFilePath, bitmapSource);
                    }
                    finally
                    {
                        if (hBitmapAddRef)
                        {
                            hBitmap.DangerousRelease();
                        }
                    }
                }
            }
            return bitmapSource;
        }

        /// <summary>
        /// The message to display when there's no apps to close.
        /// </summary>
        private readonly string _closeAppsNoProcessesMessageText;

        /// <summary>
        /// The message to display when there's apps to close.
        /// </summary>
        private readonly string _closeAppsMessageText;

        /// <summary>
        /// The text for the right button when there's no apps to close.
        /// </summary>
        private readonly string _buttonLeftNoProcessesText;

        /// <summary>
        /// The text for the left button when there's apps to close.
        /// </summary>
        private readonly string _buttonLeftText;

        /// <summary>
        /// The service object for processing running applications.
        /// </summary>
        private readonly RunningProcessService? _runningProcessService;

        /// <summary>
        /// A collection of running apps on the device that require closing.
        /// This property is deliberately public as it's required by WPF to be so.
        /// </summary>
        public ResettableObservableCollection<AppToClose> AppsToCloseCollection { get; } = [];

        /// <summary>
        /// The deadline for deferral, if applicable.
        /// </summary>
        private readonly DateTime? _deferralDeadline;

        /// <summary>
        /// The number of deferrals remaining, if applicable.
        /// </summary>
        private readonly uint? _deferralsRemaining;

        /// <summary>
        /// Indicates whether the continue button should be implied when all processes have closed.
        /// </summary>
        private readonly bool _continueOnProcessClosure;

        /// <summary>
        /// Indicates whether the countdown is forced.
        /// </summary>
        private readonly bool _forcedCountdown;

        /// <summary>
        /// Indicates whether the close button should be hidden.
        /// </summary>
        /// <remarks>This field determines if the close button is visible or not. It is intended for
        /// internal use and should not be modified directly.</remarks>
        private readonly bool _hideCloseButton;

        /// <summary>
        /// Represents the underlying <see cref="StreamWriter"/> used for logging operations.
        /// </summary>
        /// <remarks>This field is used internally to write log messages to a stream. It may be null if
        /// logging is disabled or the stream has not been initialized.</remarks>
        private readonly BinaryWriter? _logWriter;

        /// <summary>
        /// App/process icon cache for improved performance
        /// </summary>
        private static readonly Dictionary<string, BitmapSource> _appIconCache = [];

        /// <summary>
        /// Dispose managed and unmanaged resources
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }
            if (disposing)
            {
                if (null != _runningProcessService)
                {
                    _runningProcessService.ProcessesToCloseChanged -= RunningProcessService_ProcessesToCloseChanged;
                }
                AppsToCloseCollection.CollectionChanged -= AppsToCloseCollection_CollectionChanged;
            }
            base.Dispose(disposing);
        }
    }
}
