using System.Windows;
using System.Windows.Media.Imaging;
using Wpf.Ui.Appearance;

namespace PSADT.UserInterface
{
    public partial class CustomDialog : BaseDialog
    {
        /// <summary>
        /// Result of the dialog
        /// </summary>
        public string? Result { get; private set; }

        /// <summary>
        /// Constructor for CustomDialog
        /// </summary>
        /// <param name="dialogExpiryDuration"></param>
        /// <param name="appTitle"></param>
        /// <param name="subtitle"></param>
        /// <param name="topMost"></param>
        /// <param name="appIconImage"></param>
        /// <param name="customMessage"></param>
        /// <param name="button1Text"></param>
        /// <param name="button2Text"></param>
        /// <param name="button3Text"></param>
        public CustomDialog(
            TimeSpan dialogExpiryDuration,
            string? appTitle,
            string? subtitle,
            bool? topMost,
            string? appIconImage,
            string customMessage,
            string? button1Text,
            string? button2Text,
            string? button3Text)
            : base(dialogExpiryDuration)

        {
            DataContext = this;

            // Set up Mica backdrop and watch for theme changes
            SystemThemeWatcher.Watch(this, Wpf.Ui.Controls.WindowBackdropType.Acrylic, true);

            InitializeComponent();

            Loaded += CustomWindow_Loaded;

            AppTitleTextBlock.Text = appTitle ?? "Application";
            this.Title = appTitle ?? "Application";
            SubtitleTextBlock.Text = subtitle ?? "";
            Topmost = topMost ?? false;

            // TODO - Implement. Ran out of time
            //CustomMessageHeadingTextBlock.Text = customMessageHeading ?? "Please select from the options below."
            CustomMessageTextBlock.Text = customMessage ?? "Your custom message goes here.";

            Button1.Content = button1Text ?? "Ok";
            Button2.Content = button2Text ?? "Cancel";
            Button3.Content = button3Text ?? "Continue";

            Button1.Visibility = string.IsNullOrWhiteSpace(button1Text) ? Visibility.Collapsed : Visibility.Visible;
            Button2.Visibility = string.IsNullOrWhiteSpace(button2Text) ? Visibility.Collapsed : Visibility.Visible;
            Button3.Visibility = string.IsNullOrWhiteSpace(button3Text) ? Visibility.Collapsed : Visibility.Visible;

           // Set App Icon Image
            appIconImage ??= "pack://application:,,,/PSADT.UserInterface;component/Resources/appIcon.png";
            if (!string.IsNullOrWhiteSpace(appIconImage))
            {
                AppIconImage.Source = new BitmapImage(new Uri(appIconImage, UriKind.Absolute));
                this.Icon = new BitmapImage(new Uri(appIconImage, UriKind.Absolute));
            }


        }

        private void CustomWindow_Loaded(object sender, RoutedEventArgs e)
        {
        }


        private void ExitItem_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
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
            _canClose = true;
            Close();
            Dispose();
        }

        /// <summary>
        /// Dispose of managed resources
        /// </summary>
        /// <param name="disposing"></param>
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
