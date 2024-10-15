using System.Drawing;
using System.IO;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PSADT.UserInterface.Utilities;
using Wpf.Ui.Appearance;

namespace PSADT.UserInterface;

public partial class WelcomeDialog : FluentWindow
{
    public ImageSource AppIcon { get; set; }
    public ImageSource Banner { get; set; }


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
        string? buttonLeftText,
        string? buttonRightText)
    {
        InitializeComponent();
        SystemThemeWatcher.Watch(this, WindowBackdropType.Acrylic);
        DataContext = this;

        //BitmapFrame frame;
        //if (!string.IsNullOrEmpty(appIconImage))
        //{
        //    Uri.TryCreate(appIconImage, UriKind.RelativeOrAbsolute, out var logoUri);
        //    frame = LoadBitmapFrame(logoUri);
        //}
        //else
        //{
        //    Stream? iconStream = Application
        //       .GetResourceStream(
        //           new Uri("/Resources/AppIcon.ico"))?.Stream;
        //    if (iconStream != null)
        //    {
        //        frame = LoadBitmapFrame(iconStream);
        //    }
        //    else
        //    {
        //        throw new InvalidOperationException("Resource stream for app icon is null.");
        //    }
        //    frame = LoadBitmapFrame(iconStream);
        // AppIcon Image

        //AppIconImage.Source = frame;
        //NotifyIcon.Icon = frame;

        // AppIcon Image
        if (!string.IsNullOrEmpty(appIconImage) && File.Exists(appIconImage))
        {
            AppIcon = new BitmapImage(new Uri(appIconImage, UriKind.Absolute));

        }
        else
        {
            AppIcon = new BitmapImage(new Uri("pack://application:,,,/Resources/appIcon.png", UriKind.Absolute));
        }

        // Banner Image
        if (!string.IsNullOrEmpty(bannerImageLight) && File.Exists(bannerImageLight))
        {
            Banner = new BitmapImage(new Uri(bannerImageLight, UriKind.Absolute));

        }
        else
        {
            Banner = new BitmapImage(new Uri("pack://application:,,,/Resources/Fluent.Banner.Light.png"));
        }

        if (!string.IsNullOrEmpty(appTitle)) AppTitle.Text = appTitle;
        if (!string.IsNullOrEmpty(appTitle)) NotifyIcon.TooltipText = appTitle ?? "PSAppDeployToolkit";
        if (!string.IsNullOrEmpty(subtitle)) Subtitle.Text = subtitle;
        if (!string.IsNullOrEmpty(closeAppMessage)) CloseAppsMessage.Text = closeAppMessage;
        if (!string.IsNullOrEmpty(buttonLeftText))
        {
            LeftButton.Content = defersRemaining.HasValue ? $"{buttonLeftText} ({defersRemaining} remain)" : buttonLeftText;
        }

        if (!string.IsNullOrEmpty(buttonRightText)) RightButton.Content = buttonRightText;
        CloseAppsListView.ItemsSource = EvaluateRunningProcesses(appsToClose);
        SizeChanged += WelcomeDialog_SizeChanged;
    }

    public string? Result { get; private set; }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // Window Loaded Event
    }

    private void WelcomeDialog_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        var desktopWorkingArea = SystemParameters.WorkArea;
        Left = desktopWorkingArea.Right - ActualWidth + 1;
        Top = desktopWorkingArea.Bottom - ActualHeight + 1;
    }

    private void LeftButton_Click(object sender, RoutedEventArgs e)
    {
        Result = "Defer";
        Close();
    }

    private void RightButton_Click(object sender, RoutedEventArgs e)
    {
        Result = "Install";
        Close();
    }

    private List<AppProcessInfo>? EvaluateRunningProcesses(List<AppProcessInfo>? closeAppsList)
    {
        List<AppProcessInfo>? matchedAppsList = new List<AppProcessInfo>();
        HashSet<string>? uniqueProcessNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (Process? process in Process.GetProcesses())
        {
            AppProcessInfo? item = closeAppsList.FirstOrDefault(items => items.ProcessName == process.ProcessName);

            if (item != null && uniqueProcessNames.Add(process.ProcessName))
            {
                string? processFullFileName = process.GetMainModuleFileName();
                FileVersionInfo? processFileVersionInfo = FileVersionInfo.GetVersionInfo(processFullFileName);
                string? processFileName = Path.GetFileName(processFullFileName);
                string? processDescription = processFileVersionInfo.FileDescription;
                string? processProductName = processFileVersionInfo.ProductName;
                string? processPublisherName = processFileVersionInfo.CompanyName;

                Icon? ico = System.Drawing.Icon.ExtractAssociatedIcon(processFullFileName);

                matchedAppsList.Add(new AppProcessInfo
                {
                    ProcessName = processFileName,
                    ProcessDescription = processDescription,
                    ProductName = processProductName,
                    PublisherName = processPublisherName,
                    Icon = Imaging.CreateBitmapSourceFromHIcon(ico.Handle, Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions())
                });
            }
        }

        return matchedAppsList;
    }

    private BitmapFrame LoadBitmapFrame(Uri uri)
    {
        return BitmapDecoder.Create(uri, BitmapCreateOptions.IgnoreImageCache | BitmapCreateOptions.PreservePixelFormat,
                BitmapCacheOption.OnLoad)
            .Frames.OrderByDescending(f => f.Width)
            .FirstOrDefault() ?? throw new InvalidOperationException();
    }

    private BitmapFrame LoadBitmapFrame(Stream stream)
    {
        return BitmapDecoder.Create(stream,
                BitmapCreateOptions.IgnoreImageCache | BitmapCreateOptions.PreservePixelFormat,
                BitmapCacheOption.OnLoad)
            .Frames.OrderByDescending(f => f.Width)
            .FirstOrDefault() ?? throw new InvalidOperationException();
    }
}