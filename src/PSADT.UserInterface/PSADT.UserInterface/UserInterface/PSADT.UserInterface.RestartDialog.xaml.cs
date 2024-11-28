
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Wpf.Ui.Appearance;

namespace PSADT.UserInterface
{
    public partial class RestartDialog : BaseDialog
    {
        public string? Result { get; private set; }

        private DispatcherTimer _timer;
        private TimeSpan _remainingTime;

        public RestartDialog(
            string? appTitle,
            string? subtitle,
            bool? topMost,
            string? appIconImage,
            string? timeRemainingText,
            double restartCountdownMins,
            string? restartMessageText,
            string? dismissButtonText,
            string? restartButtonText)
            : base(null)

        {
            DataContext = this;

            // Set up Mica backdrop and watch for theme changes
            SystemThemeWatcher.Watch(this, Wpf.Ui.Controls.WindowBackdropType.Acrylic, true);

            InitializeComponent();

            Loaded += RestartDialog_Loaded;

            AppTitleTextBlock.Text = appTitle ?? "Application";
            SubtitleTextBlock.Text = subtitle ?? "";
            Topmost = topMost ?? false;

            // Initialize the countdown timer
            _remainingTime = TimeSpan.FromMinutes(restartCountdownMins);
            UpdateCountdownDisplay();

            TimeRemainingTextBlock.Text = timeRemainingText;
            RestartMessageTextBlock.Text = restartMessageText;
            DismissButton.Content = dismissButtonText ?? "Dismiss";
            RestartButton.Content = restartButtonText ?? "Restart";

            // Set App Icon Image
            appIconImage ??= "pack://application:,,,/PSADT.UserInterface;component/Resources/appIcon.png";
            if (!string.IsNullOrWhiteSpace(appIconImage))
            {
                AppIconImage.Source = new BitmapImage(new Uri(appIconImage, UriKind.Absolute));
            }

            // Initialize the DispatcherTimer
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += Timer_Tick;
        }

        private void RestartDialog_Loaded(object sender, RoutedEventArgs e)
        {
            _timer.Start();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (_remainingTime.TotalSeconds > 0)
            {
                _remainingTime = _remainingTime.Subtract(TimeSpan.FromSeconds(1));
                UpdateCountdownDisplay();
            }
            else
            {
                _timer.Stop();
                TriggerRestart();
            }
        }

        private void UpdateCountdownDisplay()
        {
            // Ensure that the display is in hh:mm:ss format
            RestartCountdownMinsTextBlock.Text = _remainingTime.ToString(@"hh\:mm\:ss");
        }

        private void TriggerRestart()
        {
            // Simulate the RestartButton being clicked
            RestartButton_Click(this, new RoutedEventArgs());
        }

        private void DismissButton_Click(object sender, RoutedEventArgs e)
        {
            _timer.Stop();
            this.Close();
        }

        private void RestartButton_Click(object sender, RoutedEventArgs e)
        {
            _timer.Stop();
            Result = "Restart";
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            Loaded -= RestartDialog_Loaded;

            if (_timer.IsEnabled)
            {
                _timer.Stop();
            }

            _timer.Tick -= Timer_Tick;
            _timer = null!;

            Dispose();
        }
    }
}
