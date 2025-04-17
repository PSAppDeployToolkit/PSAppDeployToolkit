using System.Runtime.InteropServices;
using PSADT.Utilities;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.WindowsAndMessaging;

namespace PSADT.UserInterface.LibraryInterfaces
{
    /// <summary>
    /// User32 interface P/Invoke wrappers.
    /// </summary>
    internal static class User32
    {
        /// <summary>
        /// Destroys an icon.
        /// </summary>
        /// <param name="hIcon"></param>
        /// <returns></returns>
        internal static BOOL DestroyIcon(HICON hIcon)
        {
            var res = PInvoke.DestroyIcon(hIcon);
            if (!res)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            return res;
        }

        /// <summary>
        /// Destroys an icon.
        /// </summary>
        /// <param name="hIcon"></param>
        /// <returns></returns>
        internal static BOOL DestroyIcon(IntPtr hIcon)
        {
            return DestroyIcon((HICON)hIcon);
        }

        /// <summary>
        /// Retrieves information about a display monitor.
        /// </summary>
        /// <param name="hMonitor"></param>
        /// <param name="lpmi"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        internal static unsafe BOOL GetMonitorInfo(HMONITOR hMonitor, out MONITORINFO lpmi)
        {
            var lpmiLocal = new MONITORINFO
            {
                cbSize = (uint)Marshal.SizeOf(typeof(MONITORINFO))
            };
            var res = PInvoke.GetMonitorInfo(hMonitor, &lpmiLocal);
            if (!res)
            {
                throw new InvalidOperationException("Failed to retrieve monitor information.");
            }
            lpmi = lpmiLocal;
            return res;
        }

        /// <summary>
        /// Retrieves information about a display monitor.
        /// </summary>
        /// <param name="hMonitor"></param>
        /// <param name="lpmi"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        internal static unsafe BOOL GetMonitorInfo(HMONITOR hMonitor, out MONITORINFOEXW lpmi)
        {
            var lpmiLocal = new MONITORINFOEXW
            {
                monitorInfo = new MONITORINFO
                {
                    cbSize = (uint)Marshal.SizeOf(typeof(MONITORINFOEXW))
                }
            };
            var res = PInvoke.GetMonitorInfo(hMonitor, (MONITORINFO*)&lpmiLocal);
            if (!res)
            {
                throw new InvalidOperationException("Failed to retrieve monitor information.");
            }
            lpmi = lpmiLocal;
            return res;
        }

        /// <summary>
        /// Retrieves the display monitor that is nearest to the specified point.
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="dwFlags"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        internal static HMONITOR MonitorFromWindow(HWND hwnd, MONITOR_FROM_FLAGS dwFlags)
        {
            var res = PInvoke.MonitorFromWindow(hwnd, dwFlags);
            if (res.IsNull && (dwFlags & MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONULL) == MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONULL)
            {
                throw new InvalidOperationException("Failed to retrieve monitor from window.");
            }
            return res;
        }
    }
}
