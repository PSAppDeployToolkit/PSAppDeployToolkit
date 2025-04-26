using System;
using System.Runtime.InteropServices;
using PSADT.Utilities;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.Accessibility;
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

        /// <summary>
        /// Retrieves the display monitor that is nearest to the specified point.
        /// </summary>
        /// <param name="pt"></param>
        /// <param name="dwFlags"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        internal static HMONITOR MonitorFromPoint(System.Drawing.Point pt, MONITOR_FROM_FLAGS dwFlags)
        {
            var res = PInvoke.MonitorFromPoint(pt, dwFlags);
            if (res.IsNull && (dwFlags & MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONULL) == MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONULL)
            {
                throw new InvalidOperationException("Failed to retrieve monitor from point.");
            }
            return res;
        }

        /// <summary>
        /// Retrieves the display monitor that is nearest to the specified point.
        /// </summary>
        /// <param name="pt"></param>
        /// <param name="dwFlags"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        internal static HMONITOR MonitorFromPoint(System.Windows.Point pt, MONITOR_FROM_FLAGS dwFlags)
        {
            return MonitorFromPoint(new System.Drawing.Point((int)pt.X, (int)pt.Y), dwFlags);
        }

        /// <summary>
        /// Retrieves the display monitor that is nearest to the specified rectangle.
        /// </summary>
        /// <param name="nIndex"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        internal static int GetSystemMetrics(SYSTEM_METRICS_INDEX nIndex)
        {
            var res = PInvoke.GetSystemMetrics(nIndex);
            if (res == 0)
            {
                throw new InvalidOperationException("Failed to retrieve system metrics.");
            }
            return res;
        }

        /// <summary>
        /// Retrieves the display monitor that is nearest to the specified rectangle.
        /// </summary>
        /// <param name="uiAction"></param>
        /// <param name="uiParam"></param>
        /// <param name="pvParam"></param>
        /// <param name="fWinIni"></param>
        /// <returns></returns>
        internal static unsafe BOOL SystemParametersInfo(SYSTEM_PARAMETERS_INFO_ACTION uiAction, uint uiParam, void* pvParam, SYSTEM_PARAMETERS_INFO_UPDATE_FLAGS fWinIni)
        {
            var res = PInvoke.SystemParametersInfo(uiAction, uiParam, pvParam, fWinIni);
            if (!res)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            return res;
        }

        /// <summary>
        /// Retrieves the display monitor that is nearest to the specified rectangle.
        /// </summary>
        /// <param name="uiAction"></param>
        /// <param name="pvParam"></param>
        /// <param name="fWinIni"></param>
        /// <returns></returns>
        internal static unsafe BOOL SystemParametersInfo(SYSTEM_PARAMETERS_INFO_ACTION uiAction, out HIGHCONTRASTW pvParam, SYSTEM_PARAMETERS_INFO_UPDATE_FLAGS fWinIni = 0)
        {
            var pvParamLocal = new HIGHCONTRASTW
            {
                cbSize = (uint)Marshal.SizeOf(typeof(HIGHCONTRASTW))
            };
            var res = SystemParametersInfo(uiAction, pvParamLocal.cbSize, &pvParamLocal, fWinIni);
            pvParam = pvParamLocal;
            return res;
        }

        /// <summary>
        /// Retrieves the display monitor that is nearest to the specified rectangle.
        /// </summary>
        /// <param name="uiAction"></param>
        /// <param name="pvParam"></param>
        /// <param name="fWinIni"></param>
        /// <returns></returns>
        internal static unsafe BOOL SystemParametersInfo(SYSTEM_PARAMETERS_INFO_ACTION uiAction, out RECT pvParam, SYSTEM_PARAMETERS_INFO_UPDATE_FLAGS fWinIni = 0)
        {
            RECT pvParamLocal = new();
            var res = SystemParametersInfo(uiAction, 0, &pvParamLocal, fWinIni);
            pvParam = pvParamLocal;
            return res;
        }

        /// <summary>
        /// Retrieves the display monitor that is nearest to the specified rectangle.
        /// </summary>
        /// <param name="hdc"></param>
        /// <param name="lprcClip"></param>
        /// <param name="lpfnEnum"></param>
        /// <param name="dwData"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        internal static BOOL EnumDisplayMonitors(HDC hdc, RECT? lprcClip, MONITORENUMPROC lpfnEnum, LPARAM dwData)
        {
            var res = PInvoke.EnumDisplayMonitors(hdc, lprcClip, lpfnEnum, dwData);
            if (!res)
            {
                throw new InvalidOperationException("Failed to enumerate display monitors.");
            }
            return res;
        }

        /// <summary>
        /// Retrieves the display monitor that is nearest to the specified rectangle.
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="bRevert"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        internal static DestroyMenuSafeHandle GetSystemMenu(HWND hWnd, BOOL bRevert)
        {
            var res = PInvoke.GetSystemMenu_SafeHandle(hWnd, bRevert);
            if (res.IsInvalid)
            {
                throw new InvalidOperationException("Failed to retrieve the menu handle.");
            }
            return res;
        }

        /// <summary>
        /// Enables or disables a menu item.
        /// </summary>
        /// <param name="hMenu"></param>
        /// <param name="uIDEnableItem"></param>
        /// <param name="uEnable"></param>
        /// <returns></returns>
        internal static BOOL EnableMenuItem(SafeHandle hMenu, uint uIDEnableItem, MENU_ITEM_FLAGS uEnable)
        {
            var res = PInvoke.EnableMenuItem(hMenu, uIDEnableItem, uEnable);
            if (res == -1)
            {
                throw new InvalidOperationException("Failed to change menu item.");
            }
            return res;
        }

        /// <summary>
        /// Sends a message to the specified window handle.
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="Msg"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <returns></returns>
        internal static LRESULT SendMessage(HWND hWnd, uint Msg, WPARAM wParam, LPARAM lParam)
        {
            PInvoke.SetLastError(0);
            var res = PInvoke.SendMessage(hWnd, Msg, wParam, lParam);
            var err = (WIN32_ERROR)Marshal.GetLastWin32Error();
            if (err != WIN32_ERROR.NO_ERROR)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error(err);
            }
            return res;
        }

        /// <summary>
        /// Releases the mouse capture from the window.
        /// </summary>
        /// <returns></returns>
        internal static BOOL ReleaseCapture()
        {
            var res = PInvoke.ReleaseCapture();
            if (!res)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            return res;
        }
    }
}
