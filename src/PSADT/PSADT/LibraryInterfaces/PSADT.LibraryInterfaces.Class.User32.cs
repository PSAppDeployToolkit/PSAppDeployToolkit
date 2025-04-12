using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using PSADT.Utilities;
using Windows.Win32;
using Windows.Win32.Foundation;
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
        /// Gets a handle to a menu for the given window handle.
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="bRevert"></param>
        /// <returns></returns>
        public static IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert)
        {
            return PInvoke.GetSystemMenu((HWND)hWnd, bRevert);
        }

        /// <summary>
        /// Enables a menu item for the given menu handle.
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
        /// </summary>
        /// <param name="hWnd"></param>
        /// <returns></returns>
        public static bool IsWindowVisible(IntPtr hWnd)
        {
            return PInvoke.IsWindowVisible((HWND)hWnd);
        }

        /// <summary>
        /// Tests whether a given window is enabled via its handle.
        /// </summary>
        /// <param name="hWnd"></param>
        /// <returns></returns>
        public static bool IsWindowEnabled(IntPtr hWnd)
        {
            return PInvoke.IsWindowEnabled((HWND)hWnd);
        }

        /// <summary>
        /// Gets a handle to the current foreground (active) window.
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
        internal static unsafe int LoadString(HINSTANCE hInstance, uint uID, Span<char> lpBuffer)
        {
            fixed (char* lpBufferPointer = lpBuffer)
            {
                var res = PInvoke.LoadString(hInstance, uID, lpBufferPointer, lpBuffer.Length);
                if (res == 0)
                {
                    throw ExceptionUtilities.GetExceptionForLastWin32Error();
                }
                return res;
            }
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
        /// <param name="nMaxCount"></param>
        /// <returns></returns>
        /// <exception cref="Win32Exception"></exception>
        internal static unsafe int GetWindowText(HWND hWnd, char[] lpString)
        {
            fixed (char* lpStringPointer = lpString)
            {
                var res = PInvoke.GetWindowText(hWnd, lpStringPointer, lpString.Length);
                if (res == 0)
                {
                    throw ExceptionUtilities.GetExceptionForLastWin32Error();
                }
                return res;
            }
        }

        /// <summary>
        /// Retrieves the text of the specified window.
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="lpdwProcessId"></param>
        /// <returns></returns>
        [DllImport("USER32.dll", ExactSpelling = true, EntryPoint = "GetWindowThreadProcessId")]
        internal static extern unsafe uint GetWindowThreadProcessIdNative(IntPtr hWnd, out uint lpdwProcessId);

        /// <summary>
        /// Retrieves the text of the specified window.
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="lpdwProcessId"></param>
        /// <returns></returns>
        /// <exception cref="Win32Exception"></exception>
        internal static uint GetWindowThreadProcessId(HWND hWnd, [Optional] out uint lpdwProcessId)
        {
            var res = GetWindowThreadProcessIdNative(hWnd, out lpdwProcessId);
            if (res == 0)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            return res;
        }

        /// <summary>
        /// Retrieves the text of the specified window.
        /// </summary>
        /// <param name="idAttach"></param>
        /// <param name="idAttachTo"></param>
        /// <param name="fAttach"></param>
        /// <returns></returns>
        [DllImport("user32.dll", SetLastError = true, EntryPoint = "AttachThreadInput")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AttachThreadInputNative(uint idAttach, uint idAttachTo, [MarshalAs(UnmanagedType.Bool)] bool fAttach);

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
            var res = AttachThreadInputNative(idAttach, idAttachTo, fAttach);
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
        internal static unsafe LRESULT SendMessageTimeout(HWND hWnd, uint Msg, WPARAM wParam, LPARAM lParam, SEND_MESSAGE_TIMEOUT_FLAGS fuFlags, uint uTimeout, out nuint lpdwResult)
        {
            fixed (nuint* lpdwResultPointer = &lpdwResult)
            {
                var res = PInvoke.SendMessageTimeout(hWnd, Msg, wParam, lParam, fuFlags, uTimeout, lpdwResultPointer);
                if (res == IntPtr.Zero)
                {
                    throw ExceptionUtilities.GetExceptionForLastWin32Error();
                }
                return res;
            }
        }
    }
}
