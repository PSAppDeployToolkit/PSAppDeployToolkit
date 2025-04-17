using System.Windows;

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
        internal static Rect WorkingArea
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
