using System.Windows;
using PSADT.UserInterface.LibraryInterfaces;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace PSADT.UserInterface.Utilities
{
    /// <summary>
    /// Provides information about the system.
    /// </summary>
    internal static class SystemInformation
    {
        /// <summary>
        /// Gets the bounds of the virtual screen in device pixels.
        /// </summary>
        internal static Rect VirtualScreen
        {
            get
            {
                int width = User32.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CXVIRTUALSCREEN);
                int height = User32.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CYVIRTUALSCREEN);
                int left = User32.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_XVIRTUALSCREEN);
                int top = User32.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_YVIRTUALSCREEN);
                return new Rect(left, top, width, height);
            }
        }

        /// <summary>
        /// Gets the working area of the primary display in device pixels.
        /// </summary>
        internal static Rect WorkingArea
        {
            get
            {
                User32.SystemParametersInfo(SYSTEM_PARAMETERS_INFO_ACTION.SPI_GETWORKAREA, out RECT rc);
                return new Rect(rc.left, rc.top, rc.right - rc.left, rc.bottom - rc.top);
            }
        }
    }
}
