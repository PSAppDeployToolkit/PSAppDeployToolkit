using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using PSADT.SafeHandles;
using PSADT.Utilities;
using Windows.Win32;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Accessibility;
using Windows.Win32.UI.HiDpi;
using Windows.Win32.UI.WindowsAndMessaging;

namespace PSADT.LibraryInterfaces
{
    /// <summary>
    /// CsWin32 P/Invoke wrappers for the user32.dll library.
    /// </summary>
    public static class User32
    {
        /// <summary>
        /// Enables a menu item for the given menu handle.
        /// This method uses IntPtr for compatibility within PowerShell.
        /// </summary>
        /// <param name="hMenu"></param>
        /// <param name="uIDEnableItem"></param>
        /// <param name="uEnable"></param>
        /// <returns></returns>
        public static int EnableMenuItem(IntPtr hMenu, uint uIDEnableItem, uint uEnable)
        {
            return PInvoke.EnableMenuItem((HMENU)hMenu, uIDEnableItem, (MENU_ITEM_FLAGS)uEnable);
        }

        /// <summary>
        /// Destroys a given menu handle.
        /// This method uses IntPtr for compatibility within PowerShell.
        /// </summary>
        /// <param name="hMenu"></param>
        /// <returns></returns>
        public static bool DestroyMenu(IntPtr hMenu)
        {
            var res = PInvoke.DestroyMenu((HMENU)hMenu);
            if (!res)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            return res;
        }

        /// <summary>
        /// Tests whether a given window is visible via its handle.
        /// This method uses IntPtr for compatibility within PowerShell.
        /// </summary>
        /// <param name="hWnd"></param>
        /// <returns></returns>
        public static bool IsWindowVisible(IntPtr hWnd)
        {
            return PInvoke.IsWindowVisible((HWND)hWnd);
        }

        /// <summary>
        /// Tests whether a given window is enabled via its handle.
        /// This method uses IntPtr for compatibility within PowerShell.
        /// </summary>
        /// <param name="hWnd"></param>
        /// <returns></returns>
        public static bool IsWindowEnabled(IntPtr hWnd)
        {
            return PInvoke.IsWindowEnabled((HWND)hWnd);
        }

        /// <summary>
        /// Gets a handle to the current foreground (active) window.
        /// This method uses IntPtr for compatibility within PowerShell.
        /// </summary>
        /// <returns></returns>
        public static IntPtr GetForegroundWindow()
        {
            var res = PInvoke.GetForegroundWindow();
            if (res == IntPtr.Zero)
            {
                throw new InvalidOperationException("Failed to get the foreground window.");
            }
            return res;
        }

        /// <summary>
        /// Sets the current process as DPI-aware (Windows Vista-onwards).
        /// </summary>
        /// <returns></returns>
        internal static BOOL SetProcessDPIAware()
        {
            var res = PInvoke.SetProcessDPIAware();
            if (!res)
            {
                throw new InvalidOperationException("Failed to set DPI awareness to Process DPI Aware.");
            }
            return res;
        }

        /// <summary>
        /// Sets the current process as DPI-aware (Windows 10-onwards).
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        /// <exception cref="Win32Exception"></exception>
        internal static BOOL SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT context)
        {
            var res = PInvoke.SetProcessDpiAwarenessContext(context);
            if (!res)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            return res;
        }

        /// <summary>
        /// Retrieves a string value from the provided library handle.
        /// </summary>
        /// <param name="hInstance"></param>
        /// <param name="uID"></param>
        /// <param name="lpBuffer"></param>
        /// <returns></returns>
        /// <exception cref="Win32Exception"></exception>
        internal static int LoadString(SafeHandle hInstance, uint uID, Span<char> lpBuffer)
        {
            var res = PInvoke.LoadString(hInstance, uID, lpBuffer, lpBuffer.Length);
            if (res == 0 && ((WIN32_ERROR)Marshal.GetLastWin32Error() is WIN32_ERROR lastWin32Error) && lastWin32Error != WIN32_ERROR.NO_ERROR)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error(lastWin32Error);
            }
            return res;
        }

        /// <summary>
        /// Wrapper around EnumWindows for error handling.
        /// </summary>
        /// <param name="lpEnumFunc"></param>
        /// <param name="lParam"></param>
        /// <returns></returns>
        /// <exception cref="Win32Exception"></exception>
        internal static BOOL EnumWindows(WNDENUMPROC lpEnumFunc, LPARAM lParam)
        {
            var res = PInvoke.EnumWindows(lpEnumFunc, lParam);
            if (!res)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            return res;
        }
        /// <summary>
        /// Retrieves the text of the specified window.
        /// </summary>
        /// <param name="hWnd"></param>
        /// <returns></returns>
        /// <exception cref="Win32Exception"></exception>
        internal static int GetWindowTextLength(HWND hWnd)
        {
            PInvoke.SetLastError(0);
            var res = PInvoke.GetWindowTextLength(hWnd);
            if (res == 0)
            {
                var error = (WIN32_ERROR)Marshal.GetLastWin32Error();
                if (error != WIN32_ERROR.NO_ERROR)
                {
                    throw ExceptionUtilities.GetExceptionForLastWin32Error(error);
                }
            }
            return res;
        }

        /// <summary>
        /// Retrieves the text of the specified window.
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="lpString"></param>
        /// <returns></returns>
        /// <exception cref="Win32Exception"></exception>
        internal static int GetWindowText(HWND hWnd, Span<char> lpString)
        {
            var res = PInvoke.GetWindowText(hWnd, lpString);
            if (res == 0)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            return res;
        }

        /// <summary>
        /// Retrieves the text of the specified window.
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="lpdwProcessId"></param>
        /// <returns></returns>
        /// <exception cref="Win32Exception"></exception>
        internal static unsafe uint GetWindowThreadProcessId(HWND hWnd, out uint lpdwProcessId)
        {
            fixed (uint* lpdwProcessIdPointer = &lpdwProcessId)
            {
                var res = PInvoke.GetWindowThreadProcessId(hWnd, lpdwProcessIdPointer);
                if (res == 0)
                {
                    throw ExceptionUtilities.GetExceptionForLastWin32Error();
                }
                return res;
            }
        }

        /// <summary>
        /// Attaches or detaches the input processing mechanism of one thread to another.
        /// </summary>
        /// <param name="idAttach"></param>
        /// <param name="idAttachTo"></param>
        /// <param name="fAttach"></param>
        /// <returns></returns>
        /// <exception cref="Win32Exception"></exception>
        internal static bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach)
        {
            var res = PInvoke.AttachThreadInput(idAttach, idAttachTo, fAttach);
            if (!res)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            return res;
        }

        /// <summary>
        /// Brings the specified window to the top of the Z order.
        /// </summary>
        /// <param name="hWnd"></param>
        /// <returns></returns>
        /// <exception cref="Win32Exception"></exception>
        internal static BOOL BringWindowToTop(HWND hWnd)
        {
            var res = PInvoke.BringWindowToTop(hWnd);
            if (!res)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            return res;
        }

        /// <summary>
        /// Sets the specified window as active.
        /// </summary>
        /// <param name="hWnd"></param>
        /// <returns></returns>
        /// <exception cref="Win32Exception"></exception>
        internal static HWND SetActiveWindow(HWND hWnd)
        {
            var res = PInvoke.SetActiveWindow(hWnd);
            if (res == IntPtr.Zero)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            return res;
        }

        /// <summary>
        /// Sets the keyboard focus to the specified window.
        /// </summary>
        /// <param name="hWnd"></param>
        /// <returns></returns>
        /// <exception cref="Win32Exception"></exception>
        internal static HWND SetFocus(HWND hWnd)
        {
            var res = PInvoke.SetFocus(hWnd);
            if (res == IntPtr.Zero)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            return res;
        }

        /// <summary>
        /// Sends the specified message to a window or windows.
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="Msg"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <param name="fuFlags"></param>
        /// <param name="uTimeout"></param>
        /// <param name="lpdwResult"></param>
        /// <returns></returns>
        /// <exception cref="Win32Exception"></exception>
        internal static unsafe LRESULT SendMessageTimeout(HWND hWnd, uint Msg, WPARAM wParam, SafeMemoryHandle lParam, SEND_MESSAGE_TIMEOUT_FLAGS fuFlags, uint uTimeout, out nuint lpdwResult)
        {
            if (lParam is not object || lParam.IsClosed)
            {
                throw new ArgumentNullException(nameof(lParam));
            }

            bool lParamAddRef = false;
            try
            {
                lParam.DangerousAddRef(ref lParamAddRef);
                fixed (nuint* lpdwResultPointer = &lpdwResult)
                {
                    var res = PInvoke.SendMessageTimeout(hWnd, Msg, wParam, lParam.DangerousGetHandle(), fuFlags, uTimeout, lpdwResultPointer);
                    if (res == IntPtr.Zero)
                    {
                        throw ExceptionUtilities.GetExceptionForLastWin32Error();
                    }
                    return res;
                }
            }
            finally
            {
                if (lParamAddRef)
                {
                    lParam.DangerousRelease();
                }
            }
        }

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

        /// <summary>
        /// Removes a menu item from the specified menu.
        /// </summary>
        /// <remarks>This method wraps the native <c>RemoveMenu</c> function and ensures that any failure is reported as a managed exception.</remarks>
        /// <param name="hMenu">A handle to the menu from which the item will be removed. This handle must be valid and cannot be null.</param>
        /// <param name="uPosition">The position of the menu item to be removed. The interpretation of this value depends on the <paramref name="uFlags"/> parameter.</param>
        /// <param name="uFlags">Specifies how the <paramref name="uPosition"/> parameter is interpreted. This can be a combination of <see cref="MENU_ITEM_FLAGS"/> values.</param>
        /// <returns><see langword="true"/> if the menu item was successfully removed; otherwise, <see langword="false"/>.</returns>
        internal static unsafe BOOL RemoveMenu(SafeHandle hMenu, uint uPosition, MENU_ITEM_FLAGS uFlags)
        {
            var res = PInvoke.RemoveMenu(hMenu, uPosition, uFlags);
            if (!res)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            return res;
        }
    }
}
