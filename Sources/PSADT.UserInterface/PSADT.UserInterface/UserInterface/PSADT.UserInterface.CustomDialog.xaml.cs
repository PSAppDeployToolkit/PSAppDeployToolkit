using System.Windows;
using System.Windows.Media.Imaging;
using Wpf.Ui.Appearance;

namespace PSADT.UserInterface
{
    public partial class CustomDialog : BaseDialog
    {
        public string? Result { get; private set; }


        public CustomDialog(
            string? appTitle,
            string? subtitle,
            bool? topMost,
            string? appIconImage,
            string? bannerImageLight,
            string? bannerImageDark,
            string customMessage,
            string? button1Text,
            string? button2Text,
            string? button3Text)
            : base()

        {
            DataContext = this;

            SystemThemeWatcher.Watch(this);

            InitializeComponent();

            Loaded += CustomWindow_Loaded;

            AppTitleTextBlock.Text = appTitle ?? "Application";
            SubtitleTextBlock.Text = subtitle ?? "";
            Topmost = topMost ?? false;
            CustomMessageTextBlock.Text = customMessage ?? "Your custom message goes here.";

            Button1.Content = button1Text ?? "Ok";
            Button2.Content = button2Text ?? "Cancel";
            Button3.Content = button3Text ?? "Continue";

            Button1.Visibility = string.IsNullOrWhiteSpace(button1Text) ? Visibility.Collapsed : Visibility.Visible;
            Button2.Visibility = string.IsNullOrWhiteSpace(button2Text) ? Visibility.Collapsed : Visibility.Visible;
            Button3.Visibility = string.IsNullOrWhiteSpace(button3Text) ? Visibility.Collapsed : Visibility.Visible;

            // Set Banner Image based on theme
            if (ApplicationThemeManager.IsMatchedDark())
            {
                if (!string.IsNullOrWhiteSpace(bannerImageDark))
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
                if (!string.IsNullOrWhiteSpace(bannerImageLight))
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
            if (!string.IsNullOrWhiteSpace(appIconImage))
            {
                AppIconImage.Source = new BitmapImage(new Uri(appIconImage, UriKind.Absolute));
            }
        }

        private void CustomWindow_Loaded(object sender, RoutedEventArgs e)
        {


        }

        private void NotifyIcon_LeftClick(object sender, RoutedEventArgs e)
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
        }

        private void ShowItem_Click(object sender, RoutedEventArgs e)
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
        }

        private void ExitItem_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            // Instead of closing, minimize to tray
            e.Cancel = true;
            Hide();
        }

        private void Button1_Click(object sender, RoutedEventArgs e)
        {
            Result = Button1.Content.ToString();
            CloseDialog();
        }

        private void Button2_Click(object sender, RoutedEventArgs e)
        {
            Result = Button2.Content.ToString();
            CloseDialog();
        }

        private void Button3_Click(object sender, RoutedEventArgs e)
        {
            Result = Button3.Content.ToString();
            CloseDialog();
        }

        private void CloseDialog()
        {

            Close();
            Dispose();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Dispose managed resources
            }

            base.Dispose(disposing);
        }
    }
}
