using System.Windows.Media.Imaging;
using Wpf.Ui.Appearance;

namespace PSADT.UserInterface
{
    public partial class CustomDialog : FluentWindow
    {
        private static readonly string CommonDialogAppFramework = "PSAppDeployToolkit";

        // Text that is common to all dialogs
        private static readonly string CommonDialogAppTitle = "Application Title";

        private static readonly string? CommonDialogLogoImage;
        private static readonly string? CustomDialogButtonLeft = "Custom1";
        private static readonly string? CustomDialogButtonMiddle = "Custom2";
        private static readonly string? CustomDialogButtonRight = "Custom3";

        // Text that is specific to this dialog
        private readonly string CustomDialogMessage = "Do you want to take a particular action?";

        public CustomDialog(string? appTitle, string? logoIconImage, String? customMessage, string? button1,
            string? button2, string? button3)
        {
            SystemThemeWatcher.Watch(this, WindowBackdropType.Acrylic);

            InitializeComponent();

            if (string.IsNullOrEmpty(logoIconImage))

            {
                /*Uri.TryCreate(CommonDialogLogoImage, UriKind.RelativeOrAbsolute, out var logoUri);
                var frame = BitmapDecoder.Create(
                        logoUri,
                        BitmapCreateOptions.IgnoreImageCache | BitmapCreateOptions.PreservePixelFormat,
                        BitmapCacheOption.OnLoad).Frames
                    .OrderByDescending(f => f.Width)
                    .FirstOrDefault() ?? throw new InvalidOperationException();

                LogoImage.Source = frame;*/
            }
            else
            {
                Uri.TryCreate(logoIconImage, UriKind.RelativeOrAbsolute, out var logoUri);
                var frame = BitmapDecoder.Create(
                        logoUri,
                        BitmapCreateOptions.IgnoreImageCache | BitmapCreateOptions.PreservePixelFormat,
                        BitmapCacheOption.OnLoad).Frames
                    .OrderByDescending(f => f.Width)
                    .FirstOrDefault() ?? throw new InvalidOperationException();

                LogoImage.Source = frame;
            }

            // TODO - get the app status from PowerShell. Send through a splatted Object containing everything we need maybe?
            AppFramework.Text = CommonDialogAppFramework + " - App " + CommonDialogAppStatus.Install;

            // App Title
            if (!string.IsNullOrEmpty(appTitle))
                AppTitle.Text = appTitle;
            else
                AppTitle.Text = CommonDialogAppTitle;

            // TODO - Complete
            // Dialog Message
            CustomMessage.Text = customMessage;

            LeftButton.Content = button1;
            MiddleButton.Content = button2;
            RightButton.Content = button3;

            SizeChanged += CustomDialog_SizeChanged;
        }

        public string Result { get; private set; }

        private void CustomDialog_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var desktopWorkingArea = SystemParameters.WorkArea;
            Left = desktopWorkingArea.Right - ActualWidth;
            Top = desktopWorkingArea.Bottom - ActualHeight;
        }

        private void LeftButton_Click(object sender, RoutedEventArgs e)
        {
            Result = "LeftButton";
            Close();
        }

        private void MiddleButton_Click(object sender, RoutedEventArgs e)
        {
            Result = "MiddleButton";
            Close();
        }

        private void RightButton_Click(object sender, RoutedEventArgs e)
        {
            Result = "RightButton";
            Close();
        }

        private enum CommonDialogAppStatus
        {
            Install,
            Uninstall,
            Repair,
            Upgrade,
            Detection
        }
    }
}