using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;
using PSADT.UserInterface.LibraryInterfaces;
using Windows.Win32;
using Windows.Win32.Graphics.Gdi;

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
        /// <param name="hdc"></param>
        private WPFScreen(IntPtr monitor, IntPtr hdc)
        {
            if (!multiMonitorSupport || monitor == (IntPtr)PRIMARY_MONITOR)
            {
                Bounds = SystemInformation.VirtualScreen;
                Primary = true;
                DeviceName = "DISPLAY";
            }
            else
            {
                User32.GetMonitorInfo((HMONITOR)monitor, out MONITORINFOEXW info);

                Bounds = new Rect(
                    info.monitorInfo.rcMonitor.left,
                    info.monitorInfo.rcMonitor.top,
                    info.monitorInfo.rcMonitor.right - info.monitorInfo.rcMonitor.left,
                    info.monitorInfo.rcMonitor.bottom - info.monitorInfo.rcMonitor.top);
                Primary = (info.monitorInfo.dwFlags & PInvoke.MONITORINFOF_PRIMARY) != 0;
                DeviceName = info.szDevice.ToString().Replace("\0", string.Empty).Trim();
            }
            hMonitor = monitor;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WPFScreen"/> class with the specified monitor handle.
        /// </summary>
        /// <param name="monitor"></param>
        private WPFScreen(IntPtr monitor) : this(monitor, IntPtr.Zero)
        {
        }

        /// <summary>
        /// Static constructor to initialize the multi-monitor support flag.
        /// </summary>
        static WPFScreen()
        {
            multiMonitorSupport = NativeMethods.GetSystemMetrics(NativeMethods.SM_CMONITORS) != 0;
        }

        /// <summary>
        /// Retrieves a Screen for the display that contains the specified window handle.
        /// </summary>
        internal static WPFScreen FromHandle(IntPtr hwnd)
        {
            if (multiMonitorSupport)
            {
                var monitor = NativeMethods.MonitorFromWindow(new HandleRef(null, hwnd), NativeMethods.MONITOR_DEFAULTTONEAREST);
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
                var pt = new NativeMethods.POINTSTRUCT((int)point.X, (int)point.Y);
                var monitor = NativeMethods.MonitorFromPoint(pt, NativeMethods.MONITOR_DEFAULTTONEAREST);
                return new WPFScreen(monitor);
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
                    NativeMethods.MonitorEnumProc callback = (IntPtr monitor, IntPtr hdc, IntPtr lprcMonitor, IntPtr lParam) =>
                    {
                        screens.Add(new WPFScreen(monitor, hdc));
                        return true;
                    };
                    NativeMethods.EnumDisplayMonitors(NativeMethods.NullHandleRef, IntPtr.Zero, callback, IntPtr.Zero);
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
                    // High Contrast mode is enabled
                    // Determine if High Contrast theme is dark or light
                    var highContrast = new NativeMethods.HIGHCONTRAST
                    {
                        cbSize = Marshal.SizeOf(typeof(NativeMethods.HIGHCONTRAST))
                    };

                    bool success = NativeMethods.SystemParametersInfo(
                        NativeMethods.SPI_GETHIGHCONTRAST,
                        highContrast.cbSize,
                        ref highContrast,
                        0);

                    if (success && (highContrast.dwFlags & NativeMethods.HCF_HIGHCONTRASTON) != 0)
                    {
                        // Analyze lpszDefaultScheme to determine if the High Contrast theme is dark
                        var schemeName = Marshal.PtrToStringAuto(highContrast.lpszDefaultScheme);
                        if (!string.IsNullOrWhiteSpace(schemeName))
                        {
                            // List of known dark and light High Contrast themes
                            var darkSchemes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                            {
                                "High Contrast Black",
                                "High Contrast #1",
                            };

                            var lightSchemes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                            {
                                "High Contrast White",
                                "High Contrast #2",
                            };

                            if (darkSchemes.Contains(schemeName))
                            {
                                return true;
                            }
                            else if (lightSchemes.Contains(schemeName))
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

                int result = NativeMethods.DwmGetColorizationColor(out uint colorizationColor, out bool opaqueBlend);
                if (result == 0)
                {
                    // Extract ARGB components from the colorization color
                    byte a = (byte)((colorizationColor >> 24) & 0xFF);
                    byte r = (byte)((colorizationColor >> 16) & 0xFF);
                    byte g = (byte)((colorizationColor >> 8) & 0xFF);
                    byte b = (byte)(colorizationColor & 0xFF);

                    Color color = Color.FromArgb(a, r, g, b);

                    // Determine if the color is dark
                    return IsColorDark(color);
                }
                else
                {
                    // If all else fails, default to light theme
                    return false;
                }
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

    /// <summary>
    /// Provides information about the system.
    /// </summary>
    public static class SystemInformation
    {
        /// <summary>
        /// Gets the bounds of the virtual screen in device pixels.
        /// </summary>
        public static Rect VirtualScreen
        {
            get
            {
                int width = NativeMethods.GetSystemMetrics(NativeMethods.SM_CXVIRTUALSCREEN);
                int height = NativeMethods.GetSystemMetrics(NativeMethods.SM_CYVIRTUALSCREEN);
                int left = NativeMethods.GetSystemMetrics(NativeMethods.SM_XVIRTUALSCREEN);
                int top = NativeMethods.GetSystemMetrics(NativeMethods.SM_YVIRTUALSCREEN);
                return new Rect(left, top, width, height);
            }
        }

        /// <summary>
        /// Gets the working area of the primary display in device pixels.
        /// </summary>
        public static Rect WorkingArea
        {
            get
            {
                var rc = new NativeMethods.RECT();
                NativeMethods.SystemParametersInfo(NativeMethods.SPI_GETWORKAREA, 0, ref rc, 0);
                return new Rect(rc.left, rc.top, rc.right - rc.left, rc.bottom - rc.top);
            }
        }
    }
}
