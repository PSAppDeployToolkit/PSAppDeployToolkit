using System.Windows;
using System.Windows.Media;
using PSADT.UserInterface.LibraryInterfaces;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
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
                Bounds = new Rect(
                    User32.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_XVIRTUALSCREEN),
                    User32.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_YVIRTUALSCREEN),
                    User32.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CXVIRTUALSCREEN),
                    User32.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CYVIRTUALSCREEN));
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
                    User32.SystemParametersInfo(SYSTEM_PARAMETERS_INFO_ACTION.SPI_GETWORKAREA, out RECT rc);
                    return new Rect(rc.left, rc.top, rc.right - rc.left, rc.bottom - rc.top);
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
}
