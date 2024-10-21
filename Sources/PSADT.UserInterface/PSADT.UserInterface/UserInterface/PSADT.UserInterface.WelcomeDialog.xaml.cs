using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using PSADT.UserInterface.Services;
using Wpf.Ui.Appearance;

namespace PSADT.UserInterface
{
    public partial class WelcomeDialog : BaseDialog
    {
        private readonly IProcessEvaluationService? _processEvaluationService;
        private int? _defersRemaining;
        private readonly string? _deferRemainText;
        private readonly string? _deferButtonText;

        private readonly bool _isAppsToClose;
        private readonly List<AppProcessInfo>? _appsToClose;
        private CancellationTokenSource? _cts;
        private List<AppProcessInfo> _previousProcessInfo = [];

        public string? Result { get; private set; }

        // ObservableCollection bound to the ListView
        public ObservableCollection<AppProcessInfo> AppsToCloseCollection { get; } = [];

        /// <summary>
        /// Constants for animation and sizing
        /// </summary>
        private const double ListViewItemHeight = 50; // Height of each ListView item
        private const double ListViewPadding = 0; // Padding/margin for ListView
        private const double BaseWindowHeight = 300; // Base height excluding ListView

        // Flag to prevent overlapping animations
        private bool _isAnimating = false;

        // Store original texts
        private readonly string _originalCloseAppMessageText;
        private readonly string _originalContinueButtonContent;
        private readonly string _altCloseAppMessage;
        private readonly string _altContinueButtonContent;

        public WelcomeDialog(
            string? appTitle,
            string? subtitle,
            bool? topMost,
            int? defersRemaining,
            List<AppProcessInfo>? appsToClose,
            string? appIconImage,
            string? bannerImageLight,
            string? bannerImageDark,
            string closeAppMessage,
            string altCloseAppMessage,
            string? deferRemainText,
            string? deferButtonText,
            string? continueButtonText,
            string? altContinueButtonText,
            IProcessEvaluationService? processEvaluationService = null)
            : base()

        {
            DataContext = this;

            SystemThemeWatcher.Watch(this);

            InitializeComponent();

            if (appsToClose?.Count > 0)
            {
                _isAppsToClose = true;
                _appsToClose = appsToClose;
            }

            Loaded += WelcomeWindow_Loaded;

            AppsToCloseCollection.CollectionChanged += AppsToCloseCollection_CollectionChanged;

            AppsToCloseListView.Loaded += AppsToCloseListView_Loaded;

            _processEvaluationService = processEvaluationService;
            _defersRemaining = defersRemaining;
            _deferRemainText = deferRemainText;
            _deferButtonText = deferButtonText;

            AppTitleTextBlock.Text = appTitle ?? "Application";
            SubtitleTextBlock.Text = subtitle ?? "";
            Topmost = topMost ?? false;
            DeferButton.Content = deferButtonText ?? "Defer";

            _originalCloseAppMessageText = closeAppMessage;
            _altCloseAppMessage = altCloseAppMessage;
            _originalContinueButtonContent = continueButtonText ?? "Continue";
            _altContinueButtonContent = altContinueButtonText ?? "Install";

            CloseAppMessageTextBlock.Text = closeAppMessage;
            ContinueButton.Content = continueButtonText ?? "Continue";

            // Set Banner Image based on theme
            if (ApplicationThemeManager.IsMatchedDark())
            {
                if (!string.IsNullOrEmpty(bannerImageDark))
                {
                    BannerImage.Source = new BitmapImage(new Uri(bannerImageDark, UriKind.Absolute));
                }
                else
                {
                    BannerImage.Source = new BitmapImage(new Uri("pack://application:,,,/PSADT.UserInterface;component/Resources/Banner.Fluent.Dark.png", UriKind.Absolute));
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(bannerImageLight))
                {
                    BannerImage.Source = new BitmapImage(new Uri(bannerImageLight, UriKind.Absolute));
                }
                else
                {
                    BannerImage.Source = new BitmapImage(new Uri("pack://application:,,,/PSADT.UserInterface;component/Resources/Banner.Fluent.Light.png", UriKind.Absolute));
                }
            }

            // Set App Icon Image
            appIconImage ??= "pack://application:,,,/PSADT.UserInterface;component/Resources/appIcon.png";
            if (!string.IsNullOrEmpty(appIconImage))
            {
                AppIconImage.Source = new BitmapImage(new Uri(appIconImage, UriKind.Absolute));
            }

            // Bind the ListView to the AppsToCloseCollection
            AppsToCloseListView.ItemsSource = AppsToCloseCollection;

            UpdateDeferButtonState();

            // Update the AppsToCloseList synchronously
            UpdateAppsToCloseList(appsToClose);
        }

        private void WelcomeWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (_isAppsToClose)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    UpdateAppsToCloseList(_appsToClose);

                    if (_processEvaluationService != null)
                    {
                        _processEvaluationService.ProcessStarted += ProcessEvaluationService_ProcessStarted;
                        _processEvaluationService.ProcessExited += ProcessEvaluationService_ProcessExited;
                    }
                });

                _cts = new CancellationTokenSource();
                _ = StartProcessEvaluationLoopAsync(_appsToClose!, _cts.Token);
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                UpdateRowDefinition();
                AnimateWindowHeight(); // Set initial height
            });

            if (AppsToCloseCollection.Count == 0)
            {
                CloseAppMessageTextBlock.Text = _altCloseAppMessage;
                ContinueButton.Content = _altContinueButtonContent;
            }
        }

        private void AppsToCloseListView_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateListViewHeight();
        }

        private void AppsToCloseCollection_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateListViewHeight();

            if (AppsToCloseCollection.Count == 0)
            {
                // Update the message and button content with alternative texts
                CloseAppMessageTextBlock.Text = _altCloseAppMessage;
                ContinueButton.Content = _altContinueButtonContent;
            }
            else
            {
                // Revert to original texts
                CloseAppMessageTextBlock.Text = _originalCloseAppMessageText;
                ContinueButton.Content = _originalContinueButtonContent;
            }
        }

        /// <summary>
        /// Calculates and animates the window's height based on the number of items in the ListView.
        /// </summary>
        private void UpdateListViewHeight()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                double desiredListViewHeight = 0;

                if (AppsToCloseCollection.Count <= 2)
                {
                    desiredListViewHeight = (ListViewItemHeight * AppsToCloseCollection.Count) + ListViewPadding;
                }
                else if (AppsToCloseCollection.Count >= 5)
                {
                    desiredListViewHeight = (ListViewItemHeight * 5) + ListViewPadding;
                }
                else
                {
                    desiredListViewHeight = (ListViewItemHeight * AppsToCloseCollection.Count) + ListViewPadding;
                }

                // Calculate desired window height
                double desiredWindowHeight = BaseWindowHeight + desiredListViewHeight;

                // Animate window height
                AnimateWindowHeight(desiredWindowHeight);
            });
        }

        /// <summary>
        /// Updates the Grid RowDefinition based on the number of items.
        /// </summary>
        private void UpdateRowDefinition()
        {
            if (AppsToCloseCollection.Count <= 5)
            {
                // Set Row Height to Auto when items are 5 or fewer
                ListViewRow.Height = new GridLength(1, GridUnitType.Auto);
            }
            else
            {
                // Set Row Height to * when items exceed 5
                ListViewRow.Height = new GridLength(1, GridUnitType.Star);
            }
        }

        private void UpdateAppsToCloseList(List<AppProcessInfo>? appsToClose)
        {
            if (appsToClose == null || appsToClose.Count == 0)
            {
                AppsToCloseListView.Visibility = Visibility.Collapsed;
                return;
            }

            if (_processEvaluationService == null)
            {
                // Populate the collection directly
                foreach (var app in appsToClose)
                {
                    AppsToCloseCollection.Add(app);
                }
                AppsToCloseListView.Visibility = AppsToCloseCollection.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
                return;
            }

            // Evaluate running processes and populate the collection
            var updatedAppsToClose = _processEvaluationService.EvaluateRunningProcesses(appsToClose);

            // Clear existing items
            AppsToCloseCollection.Clear();

            // Add updated apps
            foreach (var app in updatedAppsToClose)
            {
                AppsToCloseCollection.Add(app);
            }

            AppsToCloseListView.Visibility = AppsToCloseCollection.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

            _previousProcessInfo = new List<AppProcessInfo>(updatedAppsToClose);
        }

        private void UpdateDeferButtonState()
        {
            if (_defersRemaining.HasValue)
            {
                DeferButton.Content = $"{_deferButtonText} ({_defersRemaining} {_deferRemainText})";
                DeferButton.IsEnabled = _defersRemaining > 0;
            }
            else
            {
                DeferButton.Visibility = Visibility.Collapsed;
            }
        }

        private void DeferButton_Click(object sender, RoutedEventArgs e)
        {
            if (_defersRemaining.HasValue && _defersRemaining > 0)
            {
                _defersRemaining--;

                Application.Current.Dispatcher.Invoke(() => UpdateDeferButtonState());
            }
            Result = "Defer";
            Close();
        }

        private void ContinueButton_Click(object sender, RoutedEventArgs e)
        {
            Result = "Continue";
            Close();
        }

        private void AppsToCloseListView_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() => UpdateRowDefinition());
        }

        private async Task StartProcessEvaluationLoopAsync(List<AppProcessInfo> initialApps, CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    // Wait for a reasonable interval (e.g., 2 seconds)
                    await Task.Delay(TimeSpan.FromSeconds(2), token);

                    // Asynchronously evaluate running processes
                    List<AppProcessInfo> updatedApps = await _processEvaluationService!.EvaluateRunningProcessesAsync(initialApps, token).ConfigureAwait(false);

                    // Check if there's any change compared to the previous list
                    if (!AreProcessListsEqual(_previousProcessInfo, updatedApps))
                    {
                        // Update the collection on the UI thread
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            AppsToCloseCollection.Clear();
                            foreach (var app in updatedApps)
                            {
                                AppsToCloseCollection.Add(app);
                            }

                            // Update ListView visibility
                            AppsToCloseListView.Visibility = AppsToCloseCollection.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
                        });

                        // Update the previous process info for the next comparison
                        _previousProcessInfo = new List<AppProcessInfo>(updatedApps);
                    }

                    // If no more apps to close, exit the loop
                    if (updatedApps.Count == 0)
                    {
                        break;
                    }
                }
            }
            catch (TaskCanceledException)
            {
                // Task was canceled, no action needed
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in process evaluation loop: {ex.Message}");
            }
        }

        private bool AreProcessListsEqual(List<AppProcessInfo> list1, List<AppProcessInfo> list2)
        {
            if (list1.Count != list2.Count)
                return false;

            // Order the lists to ensure consistent comparison
            var sortedList1 = list1.OrderBy(app => app.ProcessName).ToList();
            var sortedList2 = list2.OrderBy(app => app.ProcessName).ToList();

            return sortedList1.SequenceEqual(sortedList2);
        }

        private void ProcessEvaluationService_ProcessStarted(object? sender, AppProcessInfo? e)
        {
            if (e == null)
                return;

            // Check if the process is already in the collection to avoid duplicates
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (!AppsToCloseCollection.Contains(e))
                {
                    AppsToCloseCollection.Add(e);
                    AppsToCloseListView.Visibility = Visibility.Visible;
                }
            });
        }

        private void ProcessEvaluationService_ProcessExited(object? sender, AppProcessInfo? e)
        {
            if (e == null)
                return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                if (AppsToCloseCollection.Contains(e))
                {
                    AppsToCloseCollection.Remove(e);
                    if (AppsToCloseCollection.Count == 0)
                    {
                        AppsToCloseListView.Visibility = Visibility.Collapsed;
                    }
                }
            });
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            Loaded -= WelcomeWindow_Loaded;

            if (_isAppsToClose)
            {
                _processEvaluationService!.ProcessStarted -= ProcessEvaluationService_ProcessStarted;
                _processEvaluationService.ProcessExited -= ProcessEvaluationService_ProcessExited;

                AppsToCloseCollection.CollectionChanged -= AppsToCloseCollection_CollectionChanged;
            }

            _cts?.Cancel();
            _cts?.Dispose();

            Dispose();
        }

        /// <summary>
        /// Animates the window's height to the desired height smoothly.
        /// Prevents overlapping animations by using a flag.
        /// </summary>
        /// <param name="desiredHeight">The target height for the window.</param>
        private void AnimateWindowHeight(double desiredHeight)
        {
            if (_isAnimating)
                return;

            _isAnimating = true;

            // Clamp the desired height within MinHeight and MaxHeight
            desiredHeight = Math.Max(this.MinHeight, Math.Min(this.MaxHeight, desiredHeight));

            double currentHeight = this.ActualHeight;

            // If the desired height is same as current, do nothing
            if (Math.Abs(desiredHeight - currentHeight) < 1)
            {
                _isAnimating = false;
                return;
            }

            // Create the animation
            var heightAnimation = new DoubleAnimation
            {
                From = currentHeight,
                To = desiredHeight,
                Duration = TimeSpan.FromMilliseconds(300), // Adjust duration as needed
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };

            // Handle animation completion
            heightAnimation.Completed += (s, e) => _isAnimating = false;

            // Begin the animation
            this.BeginAnimation(Window.HeightProperty, heightAnimation);
        }

        /// <summary>
        /// Calculates the desired window height based on the current number of items and animates the window.
        /// </summary>
        private void AnimateWindowHeight()
        {
            double desiredListViewHeight;

            if (AppsToCloseCollection.Count <= 2)
            {
                desiredListViewHeight = (ListViewItemHeight * AppsToCloseCollection.Count) + ListViewPadding;
            }
            else if (AppsToCloseCollection.Count >= 5)
            {
                desiredListViewHeight = (ListViewItemHeight * 5) + ListViewPadding;
            }
            else
            {
                desiredListViewHeight = (ListViewItemHeight * AppsToCloseCollection.Count) + ListViewPadding;
            }

            // Calculate desired window height
            double desiredWindowHeight = BaseWindowHeight + desiredListViewHeight;

            // Animate window height
            AnimateWindowHeight(desiredWindowHeight);
        }
    }
}
