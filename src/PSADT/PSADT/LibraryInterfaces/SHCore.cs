using System.Drawing;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.HiDpi;

namespace PSADT.LibraryInterfaces
{
    /// <summary>
    /// Provides methods for retrieving DPI (dots per inch) values for monitors.
    /// </summary>
    /// <remarks>This class contains static methods to retrieve DPI values for specific monitors or the default monitor. The DPI values can be used to adjust application scaling or rendering based on monitor resolution.</remarks>
    internal static class SHCore
    {
        /// <summary>
        /// Retrieves the DPI (dots per inch) values for a specified monitor.
        /// </summary>
        /// <param name="hmonitor">A handle to the monitor for which the DPI values are retrieved.</param>
        /// <param name="dpiType">The type of DPI value to retrieve, such as effective, angular, or raw DPI.</param>
        /// <param name="dpiX">When the method returns, contains the DPI value along the horizontal axis.</param>
        /// <param name="dpiY">When the method returns, contains the DPI value along the vertical axis.</param>
        /// <returns>A <see cref="HRESULT"/> indicating the success or failure of the operation. A successful result indicates that the DPI values were retrieved successfully.</returns>
        internal static HRESULT GetDpiForMonitor(HMONITOR hmonitor, MONITOR_DPI_TYPE dpiType, out uint dpiX, out uint dpiY)
        {
            return PInvoke.GetDpiForMonitor(hmonitor, dpiType, out dpiX, out dpiY).ThrowOnFailure();
        }

        /// <summary>
        /// Retrieves the DPI (dots per inch) values for the default monitor.
        /// </summary>
        /// <remarks>This method uses the primary monitor as the default monitor and retrieves its DPI values based on the specified <paramref name="dpiType"/>. The caller is responsible for handling the returned <see cref="HRESULT"/> and any potential errors.</remarks>
        /// <param name="dpiType">The type of DPI value to retrieve. This determines whether the effective, angular, or raw DPI is returned.</param>
        /// <param name="dpiX">When the method returns, contains the horizontal DPI of the default monitor.</param>
        /// <param name="dpiY">When the method returns, contains the vertical DPI of the default monitor.</param>
        /// <returns>A <see cref="HRESULT"/> indicating the success or failure of the operation. A successful result indicates that the DPI values were retrieved successfully.</returns>
        internal static HRESULT GetDpiForDefaultMonitor(MONITOR_DPI_TYPE dpiType, out uint dpiX, out uint dpiY)
        {
            return PInvoke.GetDpiForMonitor(User32.MonitorFromPoint(new Point(0, 0), MONITOR_FROM_FLAGS.MONITOR_DEFAULTTOPRIMARY), dpiType, out dpiX, out dpiY).ThrowOnFailure();
        }
    }
}
