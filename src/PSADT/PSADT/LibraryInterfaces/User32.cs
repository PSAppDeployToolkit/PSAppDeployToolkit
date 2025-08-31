﻿using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using PSADT.SafeHandles;
using PSADT.Utilities;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.Input.KeyboardAndMouse;
using Windows.Win32.UI.WindowsAndMessaging;

namespace PSADT.LibraryInterfaces
{
    /// <summary>
    /// CsWin32 P/Invoke wrappers for the user32.dll library.
    /// </summary>
    internal static class User32
    {
        /// <summary>
        /// Enables a menu item for the given menu handle.
        /// </summary>
        /// <param name="hMenu"></param>
        /// <param name="uIDEnableItem"></param>
        /// <param name="uEnable"></param>
        /// <returns></returns>
        internal static BOOL EnableMenuItem(SafeHandle hMenu, WM_SYSCOMMAND uIDEnableItem, MENU_ITEM_FLAGS uEnable)
        {
            return PInvoke.EnableMenuItem(hMenu, (uint)uIDEnableItem, uEnable);
        }

        /// <summary>
        /// Tests whether a given window is visible via its handle.
        /// </summary>
        /// <param name="hWnd"></param>
        /// <returns></returns>
        internal static BOOL IsWindowVisible(HWND hWnd)
        {
            return PInvoke.IsWindowVisible(hWnd);
        }

        /// <summary>
        /// Tests whether a given window is enabled via its handle.
        /// </summary>
        /// <param name="hWnd"></param>
        /// <returns></returns>
        internal static BOOL IsWindowEnabled(HWND hWnd)
        {
            return PInvoke.IsWindowEnabled(hWnd);
        }

        /// <summary>
        /// Gets a handle to the current foreground (active) window.
        /// </summary>
        /// <returns></returns>
        internal static HWND GetForegroundWindow()
        {
            var res = PInvoke.GetForegroundWindow();
            if (res == HWND.Null)
            {
                throw new InvalidOperationException("Failed to get the foreground window.");
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
            if (res == 0 && ((WIN32_ERROR)Marshal.GetLastWin32Error() is WIN32_ERROR lastWin32Error) && lastWin32Error != WIN32_ERROR.NO_ERROR)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error(lastWin32Error);
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
        internal unsafe static uint GetWindowThreadProcessId(HWND hWnd, out uint lpdwProcessId)
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
        internal static BOOL AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach)
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
            if (res == HWND.Null)
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
            if (res == HWND.Null)
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
        internal unsafe static LRESULT SendMessageTimeout(HWND hWnd, WINDOW_MESSAGE Msg, WPARAM wParam, SafeMemoryHandle lParam, SEND_MESSAGE_TIMEOUT_FLAGS fuFlags, uint uTimeout, out nuint lpdwResult)
        {
            if (lParam is null || lParam.IsClosed)
            {
                throw new ArgumentNullException(nameof(lParam));
            }

            bool lParamAddRef = false;
            try
            {
                lParam.DangerousAddRef(ref lParamAddRef);
                fixed (nuint* lpdwResultPointer = &lpdwResult)
                {
                    var res = PInvoke.SendMessageTimeout(hWnd, (uint)Msg, wParam, lParam.DangerousGetHandle(), fuFlags, uTimeout, lpdwResultPointer);
                    if (res == default)
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
        /// Sends a message to the specified window handle.
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="Msg"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <returns></returns>
        internal static LRESULT SendMessage(HWND hWnd, WINDOW_MESSAGE Msg, WPARAM wParam, LPARAM lParam)
        {
            PInvoke.SetLastError(0); var res = PInvoke.SendMessage(hWnd, (uint)Msg, wParam, lParam);
            if ((WIN32_ERROR)Marshal.GetLastWin32Error() is WIN32_ERROR lastWin32Error && lastWin32Error != WIN32_ERROR.NO_ERROR)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error(lastWin32Error);
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
        internal static BOOL RemoveMenu(SafeHandle hMenu, WM_SYSCOMMAND uPosition, MENU_ITEM_FLAGS uFlags)
        {
            var res = PInvoke.RemoveMenu(hMenu, (uint)uPosition, uFlags);
            if (!res)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            return res;
        }

        /// <summary>
        /// Retrieves a handle to the top-level window that matches the specified class name and window name.
        /// </summary>
        /// <remarks>This method wraps the native <c>FindWindow</c> function and throws an exception if the window is not found. Use this method to locate a top-level window by its class name, window name, or both.</remarks>
        /// <param name="lpClassName">The class name of the window to find. This can be a null-terminated string or <see langword="null"/> to ignore the class name.</param>
        /// <param name="lpWindowName">The window name (title) of the window to find. This can be a null-terminated string or <see langword="null"/> to ignore the window name.</param>
        /// <returns>A handle to the window that matches the specified criteria.</returns>
        internal static HWND FindWindow(string lpClassName, string? lpWindowName)
        {
            var res = PInvoke.FindWindow(lpClassName, lpWindowName);
            if (res.IsNull)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            return res;
        }

        /// <summary>
        /// Retrieves a handle to the monitor that contains the specified point.
        /// </summary>
        /// <param name="pt">The <see cref="Point"/> structure specifying the coordinates of the point to check.</param>
        /// <param name="dwFlags">A <see cref="MONITOR_FROM_FLAGS"/> value that determines the behavior if the point is not contained within any monitor.</param>
        /// <returns>A handle to the monitor that contains the specified point.</returns>
        /// <exception cref="InvalidOperationException">Thrown if no monitor is found for the specified point.</exception>
        internal static HMONITOR MonitorFromPoint(Point pt, MONITOR_FROM_FLAGS dwFlags)
        {
            var monitor = PInvoke.MonitorFromPoint(pt, dwFlags);
            if (monitor.IsNull)
            {
                throw new InvalidOperationException("Failed to retrieve monitor from point.");
            }
            return monitor;
        }

        /// <summary>
        /// Retrieves the DPI (dots per inch) value for the specified window.
        /// </summary>
        /// <param name="hwnd">The handle of the window for which to retrieve the DPI value. Cannot be null.</param>
        /// <returns>The DPI value for the specified window. This value represents the scaling factor applied to the window.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="hwnd"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the DPI value could not be retrieved for the specified window handle.</exception>
        internal static uint GetDpiForWindow(HWND hwnd)
        {
            if (hwnd.IsNull)
            {
                throw new ArgumentNullException(nameof(hwnd), "Window handle cannot be null.");
            }
            var res = PInvoke.GetDpiForWindow(hwnd);
            if (res == 0)
            {
                throw new InvalidOperationException("Failed to get DPI scale for window handle.");
            }
            return res;
        }

        /// <summary>
        /// Displays a message box with a specified timeout, allowing the caller to specify text, caption, style, language, and timeout duration.
        /// </summary>
        /// <remarks>This method wraps a native Windows API call to display a message box with a timeout. If the timeout elapses before the user responds, the message box will close automatically.</remarks>
        /// <param name="hWnd">A handle to the owner window of the message box. Pass <see cref="IntPtr.Zero"/> if the message box has no owner.</param>
        /// <param name="lpText">The text to be displayed in the message box.</param>
        /// <param name="lpCaption">The caption to be displayed in the title bar of the message box.</param>
        /// <param name="uType">A combination of flags that specify the contents and behavior of the message box. See <see cref="MESSAGEBOX_STYLE"/> for valid options.</param>
        /// <param name="wLanguageId">The language identifier for the text in the message box. Use 0 for the system default language.</param>
        /// <param name="dwTimeout">The timeout duration after which the message box will automatically close if no user action is taken.</param>
        /// <returns>A <see cref="MESSAGEBOX_RESULT"/> value indicating the user's response to the message box.</returns>
        internal static MESSAGEBOX_RESULT MessageBoxTimeout(IntPtr hWnd, string lpText, string lpCaption, MESSAGEBOX_STYLE uType, ushort wLanguageId, TimeSpan dwTimeout)
        {
            if (string.IsNullOrWhiteSpace(lpText))
            {
                throw new ArgumentNullException(nameof(lpText), "Message text cannot be null or empty.");
            }
            if (string.IsNullOrWhiteSpace(lpCaption))
            {
                throw new ArgumentNullException(nameof(lpCaption), "Message caption cannot be null or empty.");
            }
            if (dwTimeout < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(dwTimeout), "Timeout duration cannot be negative.");
            }

            [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            static extern MESSAGEBOX_RESULT MessageBoxTimeoutW(IntPtr hWnd, string lpText, string lpCaption, MESSAGEBOX_STYLE uType, ushort wLanguageId, uint dwMilliseconds);
            var res = MessageBoxTimeoutW(hWnd, lpText, lpCaption, uType, wLanguageId, (uint)dwTimeout.TotalMilliseconds);
            if (res == 0)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            return res;
        }

        /// <summary>
        /// Sets the specified window as the foreground window.
        /// </summary>
        /// <param name="hWnd">A handle to the window to be set as the foreground window.</param>
        /// <returns><see langword="true"/> if the operation succeeds; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the operation fails to set the specified window as the foreground window.</exception>
        internal static BOOL SetForegroundWindow(HWND hWnd)
        {
            var res = PInvoke.SetForegroundWindow(hWnd);
            if (!res)
            {
                throw new InvalidOperationException($"Failed to set the window as foreground.");
            }
            return res;
        }

        /// <summary>
        /// Retrieves the handle to the shell's desktop window.
        /// </summary>
        /// <returns>A <see cref="HWND"/> representing the handle to the shell's desktop window.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the shell window handle cannot be retrieved.</exception>
        internal static HWND GetShellWindow()
        {
            var res = PInvoke.GetShellWindow();
            if (res.IsNull)
            {
                throw new InvalidOperationException("Failed to retrieve the shell window handle.");
            }
            return res;
        }

        /// <summary>
        /// Retrieves the time of the last input event (e.g., keyboard or mouse activity) for the system.
        /// </summary>
        /// <remarks>This method wraps the native <c>GetLastInputInfo</c> function and ensures that the
        /// <paramref name="plii"/> structure is properly initialized before the call. The caller can use the <see
        /// cref="LASTINPUTINFO.dwTime"/> value to calculate the duration of user inactivity by comparing it with the
        /// current system tick count.</remarks>
        /// <param name="plii">When this method returns, contains a <see cref="LASTINPUTINFO"/> structure that holds the time of the last
        /// input event. The <see cref="LASTINPUTINFO.dwTime"/> field represents the tick count at the time of the last
        /// input, relative to system startup.</param>
        /// <returns><see langword="true"/> if the operation succeeds; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the method fails to retrieve the last input information.</exception>
        internal static BOOL GetLastInputInfo(out LASTINPUTINFO plii)
        {
            var pliiLocal = new LASTINPUTINFO
            {
                cbSize = (uint)Marshal.SizeOf<LASTINPUTINFO>()
            };
            var res = PInvoke.GetLastInputInfo(ref pliiLocal);
            if (!res)
            {
                throw new InvalidOperationException("Failed to retrieve the last input info.");
            }
            plii = pliiLocal;
            return res;
        }

        /// <summary>
        /// A window command to minimise all windows.
        /// </summary>
        internal const nuint MIN_ALL = 419;

        /// <summary>
        /// A window command to restore all minimised windows.
        /// </summary>
        internal const nuint MIN_ALL_UNDO = 416;
    }
}
