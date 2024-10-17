using System.Collections.Specialized;
using System.Windows;
using System.Windows.Media.Imaging;
using Wpf.Ui.Appearance;

namespace PSADT.UserInterface
{
    public partial class RestartDialog : BaseDialog
    {
        public string? Result { get; private set; }

        public RestartDialog(
            string? appTitle,
            string? subtitle,
            bool? topMost,
            string? appIconImage,
            string? bannerImageLight,
            string? bannerImageDark,
            double restartCountdownMins,
            string? restartMessage,
            string? dismissButtonText,
            string? restartButtonText)
            : base()

        {
            DataContext = this;

            SystemThemeWatcher.Watch(this);

            InitializeComponent();

            Loaded += RestartDialog_Loaded;

            AppTitleTextBlock.Text = appTitle ?? "Application";
            SubtitleTextBlock.Text = subtitle ?? "";
            Topmost = topMost ?? false;

            // Convert the minutes to a TimeSpan
            TimeSpan countdownTime = TimeSpan.FromMinutes(restartCountdownMins);
            // Format the TimeSpan as hh:mm:ss
            string formattedTime = countdownTime.ToString(@"hh\:mm\:ss");
            // Set the RestartCountdownMinsTextBlock.Text to the formatted time
            RestartCountdownMinsTextBlock.Text = formattedTime;

            RestartMessageTextBlock.Text = restartMessage;
            DismissButton.Content = dismissButtonText ?? "Dismiss";
            RestartButton.Content = restartButtonText ?? "Restart";

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
        }

        private void RestartDialog_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void AppsToCloseListView_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void AppsToCloseCollection_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
        }

        private void DismissButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void RestartButton_Click(object sender, RoutedEventArgs e)
        {
            Result = "Restart";
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            Loaded -= RestartDialog_Loaded;

            Dispose();
        }
    }
}
