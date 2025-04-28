using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using PSADT.UserInterface.DialogOptions;
using PSADT.UserInterface.LibraryInterfaces;
using PSADT.UserInterface.ProcessManagement;
using PSADT.UserInterface.Types;
using PSADT.UserInterface.Utilities;

namespace PSADT.UserInterface.Dialogs.Fluent
{
    /// <summary>
    /// A fluent implementation of PSAppDeployToolkit's CloseApps dialog.
    /// </summary>
    internal sealed class CloseAppsDialog : FluentDialog, IDisposable
    {
        /// <summary>
        /// The required data for displaying an app to close on the CloseAppsDialog.
        /// </summary>
        public sealed class AppToClose
        {
            /// <summary>
            /// Constructor for the ProcessToClose class.
            /// </summary>
            /// <param name="processToClose"></param>
            public AppToClose(ProcessToClose processToClose)
            {
                Name = Path.GetFileName(processToClose.Path);
                Description = processToClose.Description;
                Icon = GetAppIcon(processToClose.Path);
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
        internal CloseAppsDialog(CloseAppsDialogOptions options) : base(options, options.CustomMessageText, options.CountdownDuration, null, "Continue")
        {
            // Set up the context for data binding
            DataContext = this;

            // Store original and alternative texts
            _closeAppsNoProcessesMessageText = options.Strings.Fluent.DialogMessageNoProcesses;
            _closeAppsMessageText = options.Strings.Fluent.DialogMessage;
            _buttonRightNoProcessesText = options.Strings.Fluent.ButtonRightTextNoProcesses;
            _buttonRightText = options.Strings.Fluent.ButtonRightText;
            _deferralsRemaining = options.DeferralsRemaining;
            _deferralDeadline = options.DeferralDeadline;

            // Set up UI
            FormatMessageWithHyperlinks(MessageTextBlock, _closeAppsNoProcessesMessageText);
            DeferStackPanel.Visibility = _deferralsRemaining.HasValue || _deferralDeadline.HasValue ? Visibility.Visible : Visibility.Collapsed;
            DeferralDeadlineHeadingTextBlock.Text = !_deferralDeadline.HasValue ? options.Strings.Fluent.DeferralsRemaining : options.Strings.Fluent.DeferralDeadline;
            CountdownHeadingTextBlock.Text = options.Strings.Fluent.AutomaticStartCountdown;
            ButtonPanel.Visibility = Visibility.Visible;

            // Configure buttons
            SetButtonContentWithAccelerator(ButtonLeft, options.Strings.Fluent.ButtonLeftText);
            ButtonLeft.Visibility = _deferralsRemaining.HasValue || _deferralDeadline.HasValue ? Visibility.Visible : Visibility.Collapsed;
            AutomationProperties.SetName(ButtonLeft, options.Strings.Fluent.ButtonLeftText);
            SetButtonContentWithAccelerator(ButtonRight, _buttonRightNoProcessesText);
            ButtonRight.Visibility = Visibility.Visible;
            AutomationProperties.SetName(ButtonRight, _buttonRightNoProcessesText);

            // Set up/process optional values.
            if (null != options.RunningProcessService)
            {
                _runningProcessService = options.RunningProcessService;
            }
            UpdateDeferralValues();

            // Focus the continue button by default
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () =>
            {
                ButtonRight.Focus();
            });
        }

        /// <summary>
        /// Updates the deferral values displayed in the dialog.
        /// </summary>
        private void UpdateDeferralValues()
        {
            // First handle default case - if no deferral settings, just disable the button
            if (!_deferralsRemaining.HasValue && !_deferralDeadline.HasValue)
            {
                ButtonLeft.IsEnabled = false;
                return;
            }

            // Handle deferral values
            if (_deferralsRemaining.HasValue)
            {
                // Only enable the button if there are deferrals remaining
                ButtonLeft.IsEnabled = _deferralsRemaining > 0;

                // Update text value
                var displayText = $"{_deferralsRemaining} remain";
                DeferralDeadlineValueTextBlock.Text = displayText;

                // Update accessibility properties
                AutomationProperties.SetName(DeferralDeadlineValueTextBlock, displayText);

                // Update text color based on remaining deferrals
                if (_deferralsRemaining == 0)
                {
                    DeferralDeadlineValueTextBlock.Foreground = (Brush)Application.Current.Resources["SystemFillColorCriticalBrush"];
                }
                else if (_deferralsRemaining <= 1)
                {
                    DeferralDeadlineValueTextBlock.Foreground = (Brush)Application.Current.Resources["SystemFillColorCautionBrush"];
                }
                else
                {
                    DeferralDeadlineValueTextBlock.Foreground = (Brush)Application.Current.Resources["TextFillColorPrimaryBrush"];
                }
            }
            else if (_deferralDeadline.HasValue)
            {
                // Set button state based on deadline
                TimeSpan timeRemaining = _deferralDeadline!.Value - DateTime.Now;
                ButtonLeft.IsEnabled = timeRemaining > TimeSpan.Zero;

                // Update text content
                string displayText; Brush textBrush;
                if (ButtonLeft.IsEnabled)
                {
                    displayText = _deferralDeadline.Value.ToString("r");
                    if (timeRemaining < TimeSpan.FromDays(1))
                    {
                        // Less than 1 day remaining - use caution color
                        textBrush = (Brush)Application.Current.Resources["SystemFillColorCautionBrush"];
                    }
                    else
                    {
                        textBrush = (Brush)Application.Current.Resources["TextFillColorPrimaryBrush"];
                    }
                }
                else
                {
                    displayText = "Expired";
                    textBrush = (Brush)Application.Current.Resources["SystemFillColorCriticalBrush"];
                }
                DeferralDeadlineValueTextBlock.Text = displayText;
                DeferralDeadlineValueTextBlock.Foreground = textBrush;
                AutomationProperties.SetName(DeferralDeadlineValueTextBlock, displayText);
            }
        }

        /// <summary>
        /// Handles the event when the list of processes to close changes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RunningProcessService_ProcessesToCloseChanged(object? sender, ProcessesToCloseChangedEventArgs e)
        {
            Dispatcher.Invoke(() => AppsToCloseCollection.ResetItems(e.ProcessesToClose.Select(p => new AppToClose(p))));
        }

        /// <summary>
        /// Handles the event when the collection of apps to close changes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AppsToCloseCollection_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // Update the UI based on the changes in the collection.
            AutomationProperties.SetName(CloseAppsListView, $"Applications to Close: {AppsToCloseCollection.Count} items");
            UpdateRowDefinition();
            if (AppsToCloseCollection.Count == 0)
            {
                FormatMessageWithHyperlinks(MessageTextBlock, _closeAppsNoProcessesMessageText);
                SetButtonContentWithAccelerator(ButtonRight, _buttonRightNoProcessesText);
                AutomationProperties.SetName(ButtonRight, _buttonRightNoProcessesText);
                CloseAppsStackPanel.Visibility = Visibility.Collapsed;
                CloseAppsSeparator.Visibility = Visibility.Collapsed;
            }
            else
            {
                FormatMessageWithHyperlinks(MessageTextBlock, _closeAppsMessageText);
                SetButtonContentWithAccelerator(ButtonRight, _buttonRightText);
                AutomationProperties.SetName(ButtonRight, _buttonRightText);
                CloseAppsStackPanel.Visibility = Visibility.Visible;
                CloseAppsSeparator.Visibility = Visibility.Visible;
            }
        }

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
                AppsToCloseCollection.CollectionChanged += AppsToCloseCollection_CollectionChanged;
                AppsToCloseCollection.ResetItems(_runningProcessService.ProcessesToClose.Select(p => new AppToClose(p)));
                _runningProcessService.Start();
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
            if (_disposed)
            {
                return;
            }
            DialogResult = "Defer";
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
            if (_disposed)
            {
                return;
            }
            DialogResult = "Continue";
            base.ButtonLeft_Click(sender, e);
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
                // Get the icon as a System.Drawing.Bitmap.
                using (var drawingBitmap = DrawingUtilities.ExtractBitmapFromExecutable(appFilePath))
                {
                    // Create a BitmapSource from the System.Drawing.Bitmap and cache it before returning it.
                    IntPtr hBitmap = drawingBitmap.GetHbitmap();
                    try
                    {
                        bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                        bitmapSource.Freeze();
                        _appIconCache[appFilePath] = bitmapSource;
                    }
                    finally
                    {
                        Gdi32.DeleteObject(hBitmap);
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
        private readonly string _buttonRightNoProcessesText;

        /// <summary>
        /// The text for the right button when there's apps to close.
        /// </summary>
        private readonly string _buttonRightText;

        /// <summary>
        /// The service object for processing running applications.
        /// </summary>
        private readonly RunningProcessService? _runningProcessService;

        /// <summary>
        /// A collection of running apps on the device that require closing.
        /// </summary>
        public ResettableObservableCollection<AppToClose> AppsToCloseCollection { get; } = [];

        /// <summary>
        /// The deadline for deferral, if applicable.
        /// </summary>
        private readonly DateTime? _deferralDeadline;

        /// <summary>
        /// The number of deferrals remaining, if applicable.
        /// </summary>
        private readonly int? _deferralsRemaining;

        /// <summary>
        /// Whether this window has been disposed.
        /// </summary>
        private bool _disposed = false;

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
            _disposed = true;

            if (!disposing)
            {
                return;
            }
            base.Dispose(disposing);
        }
    }
}
