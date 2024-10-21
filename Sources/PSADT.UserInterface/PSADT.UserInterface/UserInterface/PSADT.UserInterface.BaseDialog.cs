using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using PSADT.UserInterface.Utilities;
using Wpf.Ui.Controls;

namespace PSADT.UserInterface
{
    public abstract class BaseDialog : FluentWindow, IDisposable
    {
        private bool _disposed = false;

        private string _bannerImageLight = "pack://application:,,,/PSADT.UserInterface;component/Resources/Banner.Fluent.Light.png";
        public string BannerImageLight
        {
            get => _bannerImageLight;
            set
            {
                if (_bannerImageLight != value)
                {
                    _bannerImageLight = value;
                }
            }
        }

        private string _bannerImageDark = "pack://application:,,,/PSADT.UserInterface;component/Resources/Banner.Fluent.Dark.png";
        public string BannerImageDark
        {
            get => _bannerImageDark;
            set
            {
                if (_bannerImageDark != value)
                {
                    _bannerImageDark = value;
                }
            }
        }

        private bool _isDarkTheme;
        public bool IsDarkTheme
        {
            get => _isDarkTheme;
            protected set
            {
                if (_isDarkTheme != value)
                {
                    _isDarkTheme = value;
                }
            }
        }

        private readonly UserPreferenceChangedEventHandler _userPreferenceChangedEventHandler;

        protected BaseDialog(UserPreferenceChangedEventHandler userPreferenceChangedEventHandler)
        {
            _userPreferenceChangedEventHandler = userPreferenceChangedEventHandler;
        }

        protected BaseDialog()
        {
            // Ensure WindowStartupLocation is Manual
            WindowStartupLocation = WindowStartupLocation.Manual;

            SystemParameters.StaticPropertyChanged += SystemParameters_StaticPropertyChanged;
            Loaded += BaseDialog_Loaded;

            // Subscribe to theme change events
            _userPreferenceChangedEventHandler = new UserPreferenceChangedEventHandler(SystemEvents_UserPreferenceChanged);
            SystemEvents.UserPreferenceChanged += _userPreferenceChangedEventHandler;

            SizeChanged += BaseDialog_SizeChanged;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            // Force layout update
            UpdateLayout();

            // Position the window
            PositionWindow();
        }

        private void BaseDialog_Loaded(object sender, RoutedEventArgs e)
        {
            // Apply the initial theme
            IsDarkTheme = WPFScreen.IsDarkTheme();
            UpdateBanner();
        }

        private void BaseDialog_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            PositionWindow();
        }

        private void SystemEvents_UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category == UserPreferenceCategory.Color ||
            e.Category == UserPreferenceCategory.General ||
            e.Category == UserPreferenceCategory.VisualStyle)
            {
                // Theme or color settings changed
                HandleThemeChange();
            }
        }

        private void SystemParameters_StaticPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SystemParameters.HighContrast))
            {
                // High Contrast mode changed
                HandleThemeChange();
            }
            else if (e.PropertyName == nameof(SystemParameters.WindowGlassBrush))
            {
                // Theme color changed
                HandleThemeChange();
            }
            else if (e.PropertyName == nameof(SystemParameters.WindowGlassColor))
            {
                // Theme color changed
                HandleThemeChange();
            }
            else if (e.PropertyName == "WindowTheme")
            {
                // Windows theme changed (available in newer versions of WPF)
                HandleThemeChange();
            }
        }

        private void HandleThemeChange()
        {
            // Marshal to the UI thread if necessary
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(HandleThemeChange);
                return;
            }

            bool oldTheme = IsDarkTheme;
            IsDarkTheme = WPFScreen.IsDarkTheme();

            if (oldTheme != IsDarkTheme)
            {
                UpdateBanner();
            }

            /*// Update application resources based on the theme
            if (IsDarkTheme)
            {
                ApplyDarkTheme();
            }
            else
            {
                ApplyLightTheme();
            }*/
        }

        protected virtual void PositionWindow()
        {
            IntPtr windowHandle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            WPFScreen screen = WPFScreen.FromHandle(windowHandle);

            // Get the working area in DIPs
            Rect workingArea = screen.GetWorkingAreaInDips(this);

            // Ensure layout is updated to get ActualWidth and ActualHeight
            double windowWidth = ActualWidth;
            double windowHeight = ActualHeight;

            // Calculate positions in DIPs
            double left = workingArea.Left + (workingArea.Width - windowWidth);
            double top = workingArea.Top + (workingArea.Height - windowHeight);

            // Apply a margin to prevent overlap
            const double margin = 8; // Adjust as needed
            left -= margin;
            top -= margin;

            // Ensure the window is within the screen bounds
            left = Math.Max(workingArea.Left, Math.Min(left, workingArea.Right - windowWidth));
            top = Math.Max(workingArea.Top, Math.Min(top, workingArea.Bottom - windowHeight));

            // Align positions to whole pixels
            left = Math.Floor(left);
            top = Math.Floor(top);

            // Debug output
            Debug.WriteLine($"Monitor: {screen.DeviceName}");
            Debug.WriteLine($"Working Area (DIPs): Left={workingArea.Left}, Top={workingArea.Top}, Right={workingArea.Right}, Bottom={workingArea.Bottom}");
            Debug.WriteLine($"Window Actual Width: {windowWidth}, Actual Height: {windowHeight}");
            Debug.WriteLine($"Calculated Left: {left}, Top: {top}");

            // Set positions in DIPs
            Left = left;
            Top = top;
        }

        protected virtual void CenterWindowOnScreen()
        {
            var windowHandle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            var screen = WPFScreen.FromHandle(windowHandle);

            // Get the bounds in DIPs
            var bounds = screen.GetBoundsInDips(this);

            // Calculate center positions
            double left = bounds.Left + ((bounds.Width - Width) / 2);
            double top = bounds.Top + ((bounds.Height - Height) / 2);

            // Set positions
            Left = left;
            Top = top;
        }

        protected virtual void UpdateBanner()

        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(UpdateBanner);
                return;
            }

            try
            {
                string bannerUri = IsDarkTheme ? BannerImageDark : BannerImageLight;

                if (FindName("BannerImage") is System.Windows.Controls.Image bannerImageControl)
                {
                    bannerImageControl.Source = new BitmapImage(new Uri(bannerUri, UriKind.Absolute));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating banner image: {ex.Message}");
            }
        }

        //private void ApplyDarkTheme()
        //{
        //    // Remove existing theme dictionaries
        //    RemoveThemeDictionaries();

        //    // Add dark theme resource dictionary
        //    Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary
        //    {
        //        Source = new Uri("pack://application:,,,/YourAssembly;component/Themes/DarkTheme.xaml")
        //    });
        //}

        //private void ApplyLightTheme()
        //{
        //    // Remove existing theme dictionaries
        //    RemoveThemeDictionaries();

        //    // Add light theme resource dictionary
        //    Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary
        //    {
        //        Source = new Uri("pack://application:,,,/YourAssembly;component/Themes/LightTheme.xaml")
        //    });
        //}

        //private void RemoveThemeDictionaries()
        //{
        //    // Remove existing theme dictionaries
        //    var dictionariesToRemove = Application.Current.Resources.MergedDictionaries
        //        .Where(d => d.Source != null && d.Source.OriginalString.Contains("Themes/"))
        //        .ToList();

        //    foreach (var dictionary in dictionariesToRemove)
        //    {
        //        Application.Current.Resources.MergedDictionaries.Remove(dictionary);
        //    }
        //}

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                SystemParameters.StaticPropertyChanged -= SystemParameters_StaticPropertyChanged;
                Loaded -= BaseDialog_Loaded;
                SystemEvents.UserPreferenceChanged -= _userPreferenceChangedEventHandler;
                SizeChanged -= BaseDialog_SizeChanged;
            }

            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~BaseDialog()
        {
            Dispose(false);
        }
    }
}
