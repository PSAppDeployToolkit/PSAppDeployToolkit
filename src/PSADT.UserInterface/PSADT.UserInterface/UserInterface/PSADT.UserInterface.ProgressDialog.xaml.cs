using System.Windows.Media;
using System.Windows.Media.Imaging;
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
            string? progressMessage,
            string? progressMessageDetail)
            : base(null)
        {
            DataContext = this;

            // Set up Mica backdrop and watch for theme changes
            SystemThemeWatcher.Watch(this, Wpf.Ui.Controls.WindowBackdropType.Acrylic, true);

            InitializeComponent();

            AppTitleTextBlock.Text = appTitle ?? "Application";
            SubtitleTextBlock.Text = subtitle ?? "";
            Topmost = topMost ?? false;
            ProgressMessageTextBlock.Text = progressMessage ?? "Installation in progress. Please wait ...";
            ProgressMessageDetailTextBlock.Text = progressMessageDetail ?? "This message will close automatically when the installation is complete.";

            // Set App Icon Image
            appIconImage ??= "pack://application:,,,/PSADT.UserInterface;component/Resources/appIcon.png";
            if (!string.IsNullOrWhiteSpace(appIconImage))
            {
                AppIconImage.Source = new BitmapImage(new Uri(appIconImage, UriKind.Absolute));
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
