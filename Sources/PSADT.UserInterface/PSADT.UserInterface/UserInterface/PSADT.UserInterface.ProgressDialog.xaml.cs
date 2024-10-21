using System.Windows.Media;
using Wpf.Ui.Appearance;

namespace PSADT.UserInterface
{
    public partial class ProgressDialog : BaseDialog
    {
        public ProgressDialog(
            string? appTitle,
            string? subtitle,
            bool? topMost,
            string? appIconImage,
            string? bannerImageLight,
            string? bannerImageDark,
            string? progressMessage,
            string? progressMessageDetail)
            : base()
        {
            DataContext = this;

            SystemThemeWatcher.Watch(this);

            InitializeComponent();

            AppTitleTextBlock.Text = appTitle ?? "Application";
            SubtitleTextBlock.Text = subtitle ?? "";
            Topmost = topMost ?? false;
            ProgressMessageTextBlock.Text = progressMessage ?? "Installation in progress. Please wait ...";
            ProgressMessageDetailTextBlock.Text = progressMessageDetail ?? "This message will close automatically when the installation is complete.";

            if (bannerImageLight != null)
            {
                BannerImageLight = bannerImageLight;
            }

            if (bannerImageDark != null)
            {
                BannerImageDark = bannerImageDark;
            }

            appIconImage ??= "pack://application:,,,/PSADT.UserInterface;component/Resources/appIcon.png";
            if (appIconImage != null)
            {
                AppIconImage.Source = new ImageSourceConverter().ConvertFromString(appIconImage) as ImageSource;
            }

            ProgressBar.IsIndeterminate = true;
        }

        public void UpdateProgress(double value, string? message = null, string? detailMessage = null)
        {
            Dispatcher.Invoke(() =>
            {
                ProgressBar.IsIndeterminate = false;
                ProgressBar.Value = value;

                if (message != null)
                {
                    ProgressMessageTextBlock.Text = message;
                }

                if (detailMessage != null)
                {
                    ProgressMessageDetailTextBlock.Text = detailMessage;
                }
            });
        }

        public void SetIndeterminate(string? message = null, string? detailMessage = null)
        {
            Dispatcher.Invoke(() =>
            {
                ProgressBar.IsIndeterminate = true;

                if (message != null)
                {
                    ProgressMessageTextBlock.Text = message;
                }

                if (detailMessage != null)
                {
                    ProgressMessageDetailTextBlock.Text = detailMessage;
                }
            });
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            Dispose();
        }
    }
}
