using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;
using PSADT.UserInterface.LibraryInterfaces;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.Accessibility;
using Windows.Win32.UI.WindowsAndMessaging;

namespace PSADT.UserInterface.Utilities
{
    /// <summary>
    /// Represents a display device or multiple display devices on a single system.
    /// </summary>
    internal sealed class WPFScreen
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WPFScreen"/> class.
        /// </summary>
        /// <param name="monitor"></param>
        private WPFScreen(IntPtr monitor)
        {
            if (multiMonitorSupport && monitor != (IntPtr)PRIMARY_MONITOR)
            {
                User32.GetMonitorInfo((HMONITOR)monitor, out MONITORINFOEXW info);
                DeviceName = info.szDevice.ToString().Replace("\0", string.Empty).Trim();
                Primary = (info.monitorInfo.dwFlags & PInvoke.MONITORINFOF_PRIMARY) != 0;
                Bounds = new Rect(
                    info.monitorInfo.rcMonitor.left,
                    info.monitorInfo.rcMonitor.top,
                    info.monitorInfo.rcMonitor.right - info.monitorInfo.rcMonitor.left,
                    info.monitorInfo.rcMonitor.bottom - info.monitorInfo.rcMonitor.top);
            }
            else
            {
                DeviceName = "DISPLAY";
                Primary = true;
                Bounds = SystemInformation.VirtualScreen;
            }
            hMonitor = monitor;
        }

        /// <summary>
        /// Static constructor to initialize the multi-monitor support flag.
        /// </summary>
        static WPFScreen()
        {
            try
            {
                multiMonitorSupport = User32.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CMONITORS) != 0;
            }
            catch
            {
                multiMonitorSupport = false;
            }
        }

        /// <summary>
        /// Retrieves a Screen for the display that contains the specified window handle.
        /// </summary>
        internal static WPFScreen FromHandle(IntPtr hwnd)
        {
            if (multiMonitorSupport)
            {
                var monitor = User32.MonitorFromWindow((HWND)hwnd, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST);
                return new WPFScreen(monitor);
            }
            return new WPFScreen((IntPtr)PRIMARY_MONITOR);
        }

        /// <summary>
        /// Retrieves a Screen for the display that contains the specified point.
        /// </summary>
        internal static WPFScreen FromPoint(Point point)
        {
            if (multiMonitorSupport)
            {
                return new WPFScreen(User32.MonitorFromPoint(point, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST));
            }
            return new WPFScreen((IntPtr)PRIMARY_MONITOR);
        }

        /// <summary>
        /// Gets an array of all displays on the system.
        /// </summary>
        internal static IEnumerable<WPFScreen> AllScreens
        {
            get
            {
                if (multiMonitorSupport)
                {
                    var screens = new List<WPFScreen>();
                    MONITORENUMPROC callback;
                    unsafe
                    {
                        callback = (HMONITOR monitor, HDC hdc, RECT* lprcMonitor, LPARAM lParam) =>
                        {
                            screens.Add(new WPFScreen(monitor));
                            return true;
                        };
                    }
                    User32.EnumDisplayMonitors(HDC.Null, null, callback, IntPtr.Zero);
                    return screens;
                }
                return [new WPFScreen((IntPtr)PRIMARY_MONITOR)];
            }
        }

        /// <summary>
        /// Gets the primary display.
        /// </summary>
        internal static WPFScreen PrimaryScreen => AllScreens.FirstOrDefault(screen => screen.Primary) ?? new WPFScreen((IntPtr)PRIMARY_MONITOR);

        /// <summary>
        /// Gets the bounds of the display in device pixels.
        /// </summary>
        internal readonly Rect Bounds;

        /// <summary>
        /// Gets the device name associated with a display.
        /// </summary>
        internal readonly string DeviceName;

        /// <summary>
        /// Gets a value indicating whether a particular display is the primary device.
        /// </summary>
        internal readonly bool Primary;

        /// <summary>
        /// Gets the working area of the display in device pixels.
        /// </summary>
        internal Rect WorkingArea
        {
            get
            {
                if (!multiMonitorSupport || hMonitor == (IntPtr)PRIMARY_MONITOR)
                {
                    return SystemInformation.WorkingArea;
                }
                User32.GetMonitorInfo((HMONITOR)hMonitor, out MONITORINFO info);
                return new Rect(
                    info.rcWork.left,
                    info.rcWork.top,
                    info.rcWork.right - info.rcWork.left,
                    info.rcWork.bottom - info.rcWork.top);
            }
        }

        /// <summary>
        /// Converts the screen's working area from device pixels to device-independent pixels (DIPs).
        /// </summary>
        /// <param name="visual">A visual element used to get the DPI information.</param>
        /// <returns>The working area in DIPs.</returns>
        internal Rect GetWorkingAreaInDips(Visual visual)
        {
            var source = PresentationSource.FromVisual(visual);
            if (source?.CompositionTarget != null)
            {
                var transform = source.CompositionTarget.TransformFromDevice;
                var workingAreaDevice = new Rect(
                    WorkingArea.Left,
                    WorkingArea.Top,
                    WorkingArea.Width,
                    WorkingArea.Height);
                var workingAreaInDips = Rect.Transform(workingAreaDevice, transform);
                return workingAreaInDips;
            }
            else
            {
                // If unable to get the DPI transform, return the working area as is
                return WorkingArea;
            }
        }

        /// <summary>
        /// Converts the screen's bounds from device pixels to device-independent pixels (DIPs).
        /// </summary>
        /// <param name="visual">A visual element used to get the DPI information.</param>
        /// <returns>The bounds in DIPs.</returns>
        internal Rect GetBoundsInDips(Visual visual)
        {
            var source = PresentationSource.FromVisual(visual);
            if (source?.CompositionTarget != null)
            {
                var transform = source.CompositionTarget.TransformFromDevice;
                var boundsDevice = new Rect(
                    Bounds.Left,
                    Bounds.Top,
                    Bounds.Width,
                    Bounds.Height);
                var boundsInDips = Rect.Transform(boundsDevice, transform);
                return boundsInDips;
            }
            else
            {
                // If unable to get the DPI transform, return the bounds as is
                return Bounds;
            }
        }

        /// <summary>
        /// Detects if the system is using a dark theme, using both registry and DwmGetColorizationColor, with High Contrast mode support.
        /// </summary>
        internal static bool IsDarkTheme()
        {
            try
            {
                // Check High Contrast mode
                if (SystemParameters.HighContrast)
                {
                    // High Contrast mode is enabled. Determine if High Contrast theme is dark or light
                    User32.SystemParametersInfo(SYSTEM_PARAMETERS_INFO_ACTION.SPI_GETHIGHCONTRAST, out HIGHCONTRASTW highContrast);
                    if ((highContrast.dwFlags & HIGHCONTRASTW_FLAGS.HCF_HIGHCONTRASTON) != 0)
                    {
                        // Analyze lpszDefaultScheme to determine if the High Contrast theme is dark
                        var schemeName = highContrast.lpszDefaultScheme.ToString();
                        if (!string.IsNullOrWhiteSpace(schemeName))
                        {
                            // List of known dark and light High Contrast themes
                            if (DarkHighContrastSchemes.Contains(schemeName))
                            {
                                return true;
                            }
                            else if (LightHighContrastSchemes.Contains(schemeName))
                            {
                                return false;
                            }
                        }

                        // Default to false (light theme) if scheme is unrecognized
                        return false;
                    }
                }

                // First, check the AppsUseLightTheme registry key
                const string registryKey = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
                const string valueName = "AppsUseLightTheme";
                var registryValueObject = Registry.GetValue(registryKey, valueName, null);
                if (registryValueObject != null)
                {
                    int registryValue = (int)registryValueObject;
                    bool isDarkTheme = registryValue == 0; // 0 means dark theme
                    return isDarkTheme;
                }

                // If registry key is not available, fall back to DwmGetColorizationColor
                DwmApi.DwmGetColorizationColor(out uint colorizationColor, out BOOL opaqueBlend);
                byte a = (byte)((colorizationColor >> 24) & 0xFF);
                byte r = (byte)((colorizationColor >> 16) & 0xFF);
                byte g = (byte)((colorizationColor >> 8) & 0xFF);
                byte b = (byte)(colorizationColor & 0xFF);

                // Determine if the color is dark
                return IsColorDark(Color.FromArgb(a, r, g, b));
            }
            catch
            {
                // In case of any exception, default to light theme
                return false;
            }
        }

        /// <summary>
        /// Helper method to determine if a color is considered dark.
        /// </summary>
        private static bool IsColorDark(Color color)
        {
            // Calculate luminance
            double luminance = ((0.299 * color.R) + (0.587 * color.G) + (0.114 * color.B)) / 255;
            return luminance < 0.5;
        }

        /// <summary>
        /// Determines whether the specified object is equal to this Screen.
        /// </summary>
        public override bool Equals(object? obj)
        {
            if (obj is WPFScreen other)
            {
                return hMonitor == other.hMonitor;
            }
            return false;
        }

        /// <summary>
        /// Returns a hash code for this Screen.
        /// </summary>
        public override int GetHashCode()
        {
            return hMonitor.GetHashCode();
        }

        /// <summary>
        /// List of known dark High Contrast themes.
        /// </summary>
        private static readonly HashSet<string> DarkHighContrastSchemes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "High Contrast Black",
            "High Contrast #1",
        };

        /// <summary>
        /// List of known light High Contrast themes.
        /// </summary>
        private static readonly HashSet<string> LightHighContrastSchemes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "High Contrast White",
            "High Contrast #2",
        };

        /// <summary>
        /// The handle to the monitor.
        /// </summary>
        private readonly IntPtr hMonitor;

        /// <summary>
        /// The flag to indicate multi-monitor support.
        /// </summary>
        private static readonly bool multiMonitorSupport;

        /// <summary>
        /// The primary monitor constant.
        /// </summary>
        private const int PRIMARY_MONITOR = unchecked((int)0xBAADF00D);
    }
}
