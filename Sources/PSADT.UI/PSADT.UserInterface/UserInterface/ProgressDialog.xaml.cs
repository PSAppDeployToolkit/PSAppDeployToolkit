using System.Windows.Media;
using Wpf.Ui.Appearance;

namespace PSADT.UserInterface
{
    public partial class ProgressDialog : FluentWindow
    {
        public ImageSource AppIcon { get; set; }
        public ImageSource Banner { get; set; }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }

        public ProgressDialog(
            string? appTitle,
            string? subtitle,
            bool? topMost,
            string? appIconImage,
            string? bannerImageLight,
            string? bannerImageDark,
            string? progressMessage,
            string? progressMessageDetail)
        {

            InitializeComponent();
            SystemThemeWatcher.Watch(this, WindowBackdropType.Acrylic);
            DataContext = this;

            //// AppIcon Image
            //if (!string.IsNullOrEmpty(appIconImage) && File.Exists(appIconImage))
            //{
            //    AppIcon = new BitmapImage(new Uri(appIconImage, UriKind.Absolute));

            //}
            //else
            //{
            //    AppIcon = new BitmapImage(new Uri("pack://application:,,,/Resources/appIcon.png", UriKind.Absolute));
            //}

            //// Banner Image
            //if (!string.IsNullOrEmpty(bannerImageLight) && File.Exists(bannerImageLight))
            //{
            //    Banner = new BitmapImage(new Uri(bannerImageLight, UriKind.Absolute));

            //}
            //else
            //{
            //    Banner = new BitmapImage(new Uri("pack://application:,,,/Resources/Fluent.Banner.Light.png"));
            //}


            // TODO - Determine light dark and set Banner Image accordingly
            //if (!string.IsNullOrEmpty(appIconImage))
            //{
            //    Uri.TryCreate(appIconImage, UriKind.RelativeOrAbsolute, out var logoUri);
            //    var frame = BitmapDecoder.Create(
            //            logoUri,
            //            BitmapCreateOptions.IgnoreImageCache | BitmapCreateOptions.PreservePixelFormat,
            //            BitmapCacheOption.OnLoad).Frames
            //        .OrderByDescending(f => f.Width)
            //        .FirstOrDefault() ?? throw new InvalidOperationException();

            //    AppIconImage.Source = frame;
            //}

            // App Title
            if (!string.IsNullOrEmpty(appTitle))
            {
                AppTitle.Text = appTitle;
            }

            // Subtitle
            if (!string.IsNullOrEmpty(subtitle))
            {
                Subtitle.Text = subtitle;
            }

            // Progress Message 
            if (!string.IsNullOrEmpty(progressMessage))
            {
                ProgressMessage.Text = progressMessage;
            }

            // Progress Message Detail
            if (!string.IsNullOrEmpty(progressMessageDetail))
            {
                ProgressMessageDetail.Text = progressMessageDetail;
            }

            SizeChanged += ProgressDialog_SizeChanged;
        }


        public string? SetProgressMessage
        {
            get { return ProgressMessage.Text; }
            set { ProgressMessage.Text = value; }
        }

        public string? SetProgressMessageDetail
        {
            get { return ProgressMessageDetail.Text; }
            set { ProgressMessageDetail.Text = value; }
        }


        private void ProgressDialog_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var desktopWorkingArea = SystemParameters.WorkArea;
            Left = desktopWorkingArea.Right - ActualWidth;
            Top = desktopWorkingArea.Bottom - ActualHeight;
        }
    }
}