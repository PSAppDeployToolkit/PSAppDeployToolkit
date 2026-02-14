using System;
using System.Drawing;
using System.Runtime.InteropServices;
using PSADT.LibraryInterfaces.Utilities;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.Input.KeyboardAndMouse;
using Windows.Win32.UI.WindowsAndMessaging;

namespace PSADT.LibraryInterfaces
{
    /// <summary>
    /// Provides managed wrappers for selected native User32.dll Windows API functions, enabling window management,
    /// message handling, menu operations, and related system interactions in a .NET environment.
    /// </summary>
    /// <remarks>This static class exposes a subset of User32 functionality with .NET-friendly signatures,
    /// exception handling, and type safety. All methods are intended for advanced scenarios involving direct
    /// interaction with the Windows desktop environment, window handles, and system resources. Callers are responsible
    /// for ensuring correct usage of window and menu handles, and for understanding the security and threading
    /// implications of native Windows API calls. Most methods throw managed exceptions on failure, rather than
    /// returning error codes, to provide a more idiomatic .NET experience.</remarks>
    internal static class User32
    {
        /// <summary>
        /// Enables, disables, or grays a menu item in the specified menu.
        /// </summary>
        /// <param name="hMenu">A handle to the menu containing the item to be modified. This handle must be valid and refer to an existing
        /// menu.</param>
        /// <param name="uIDEnableItem">The identifier or position of the menu item to be modified. This value specifies which item in the menu will
        /// be enabled, disabled, or grayed.</param>
        /// <param name="uEnable">A combination of flags that determine the action to take on the menu item, such as enabling, disabling, or
        /// graying it. Must be a valid combination of MENU_ITEM_FLAGS values.</param>
        /// <returns>A value indicating the previous state of the menu item. Returns a nonzero value if successful; otherwise,
        /// returns zero.</returns>
        internal static BOOL EnableMenuItem(SafeHandle hMenu, WM_SYSCOMMAND uIDEnableItem, MENU_ITEM_FLAGS uEnable)
        {
            return PInvoke.EnableMenuItem(hMenu, (uint)uIDEnableItem, uEnable);
        }

        /// <summary>
        /// Determines whether the specified window is visible.
        /// </summary>
        /// <remarks>A window is considered visible if it has the WS_VISIBLE style bit set. However, the
        /// window may be obscured by other windows or outside the visible area of the screen.</remarks>
        /// <param name="hWnd">A handle to the window to be tested for visibility.</param>
        /// <returns>A nonzero value if the window is visible; otherwise, zero.</returns>
        internal static BOOL IsWindowVisible(HWND hWnd)
        {
            return PInvoke.IsWindowVisible(hWnd);
        }

        /// <summary>
        /// Determines whether the specified window is enabled to receive input.
        /// </summary>
        /// <param name="hWnd">A handle to the window to be tested.</param>
        /// <returns>A nonzero value if the window is enabled; otherwise, zero.</returns>
        internal static BOOL IsWindowEnabled(HWND hWnd)
        {
            return PInvoke.IsWindowEnabled(hWnd);
        }

        /// <summary>
        /// Retrieves a handle to the window that is currently in the foreground.
        /// </summary>
        /// <returns>A handle to the foreground window. The handle uniquely identifies the window currently receiving user input.</returns>
        /// <exception cref="InvalidOperationException">Thrown if no foreground window is found.</exception>
        internal static HWND GetForegroundWindow()
        {
            HWND res = PInvoke.GetForegroundWindow();
            return res == HWND.Null ? throw new InvalidOperationException("Failed to get the foreground window.") : res;
        }

        /// <summary>
        /// Loads a string resource identified by the specified resource identifier into the provided character buffer.
        /// </summary>
        /// <param name="hInstance">A handle to the module containing the string resource to be loaded. This handle must be valid and refer to a
        /// loaded module.</param>
        /// <param name="uID">The identifier of the string resource to be loaded.</param>
        /// <param name="lpBuffer">A span of characters that receives the loaded string. The buffer must be large enough to hold the string,
        /// including the terminating null character.</param>
        /// <returns>The number of characters copied into the buffer, not including the terminating null character. Returns 0 if
        /// the string resource is not found.</returns>
        internal static int LoadString(SafeHandle hInstance, uint uID, Span<char> lpBuffer)
        {
            int res = PInvoke.LoadString(hInstance, uID, lpBuffer, lpBuffer.Length);
            return res == 0 && ((WIN32_ERROR)Marshal.GetLastWin32Error() is WIN32_ERROR lastWin32Error) && lastWin32Error != WIN32_ERROR.NO_ERROR
                ? throw ExceptionUtilities.GetException(lastWin32Error)
                : res;
        }

        /// <summary>
        /// Enumerates all top-level windows on the screen by passing the handle of each window to a specified callback
        /// function.
        /// </summary>
        /// <remarks>If the callback function returns <see langword="false"/>, the enumeration stops. If
        /// the underlying Windows API call fails, an exception is thrown with the last Win32 error code.</remarks>
        /// <param name="lpEnumFunc">A callback function that is called for each top-level window. The function receives the handle to the window
        /// and the application-defined value specified by <paramref name="lParam"/>.</param>
        /// <param name="lParam">An application-defined value to be passed to the callback function. This value can be used to pass
        /// information to the callback.</param>
        /// <returns>A nonzero value if the enumeration succeeds; otherwise, the method throws an exception.</returns>
        internal static BOOL EnumWindows(WNDENUMPROC lpEnumFunc, LPARAM lParam)
        {
            BOOL res = PInvoke.EnumWindows(lpEnumFunc, lParam);
            return !res ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : res;
        }

        /// <summary>
        /// Enumerates all top-level windows on the screen by passing the handle of each window to a specified callback
        /// function.
        /// </summary>
        /// <param name="lpEnumFunc">A callback function that is called for each top-level window. The function receives the handle to the window
        /// and the application-defined value specified by <paramref name="lParam"/>.</param>
        /// <param name="lParam">An optional string value to be passed to the callback function. Can be <see langword="null"/>.</param>
        /// <returns>A nonzero value if the function succeeds; otherwise, zero. If the callback function returns <see
        /// langword="false"/>, the enumeration stops and the return value is zero.</returns>
        internal static BOOL EnumWindows(WNDENUMPROC lpEnumFunc, string? lParam)
        {
            unsafe
            {
                fixed (char* lParamPtr = lParam)
                {
                    return EnumWindows(lpEnumFunc, (nint)lParamPtr);
                }
            }
        }

        /// <summary>
        /// Retrieves the length, in characters, of the text associated with the specified window's title bar.
        /// </summary>
        /// <remarks>If the method returns 0, call Marshal.GetLastWin32Error to determine whether an error
        /// occurred. If an error is detected, an exception is thrown. This method corresponds to the Win32
        /// GetWindowTextLength API.</remarks>
        /// <param name="hWnd">A handle to the window whose title text length is to be retrieved.</param>
        /// <returns>The length, in characters, of the window's title text, not including the terminating null character. Returns
        /// 0 if the window has no title or if an error occurs.</returns>
        internal static int GetWindowTextLength(HWND hWnd)
        {
            PInvoke.SetLastError(0); int res = PInvoke.GetWindowTextLength(hWnd);
            return res == 0 && ((WIN32_ERROR)Marshal.GetLastWin32Error() is WIN32_ERROR lastWin32Error) && lastWin32Error != WIN32_ERROR.NO_ERROR
                ? throw ExceptionUtilities.GetException(lastWin32Error)
                : res;
        }

        /// <summary>
        /// Retrieves the text of the specified window and copies it into the provided character buffer.
        /// </summary>
        /// <remarks>If the window has no text, the return value is zero and an exception is thrown. This
        /// method throws an exception if the underlying Windows API call fails.</remarks>
        /// <param name="hWnd">A handle to the window whose text is to be retrieved. The window must belong to the calling process.</param>
        /// <param name="lpString">A span of characters that receives the window text. The buffer must be large enough to hold the text,
        /// including the terminating null character.</param>
        /// <returns>The number of characters copied to the buffer, not including the terminating null character.</returns>
        internal static int GetWindowText(HWND hWnd, Span<char> lpString)
        {
            int res = PInvoke.GetWindowText(hWnd, lpString);
            return res == 0 ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : res;
        }

        /// <summary>
        /// Retrieves the identifier of the thread that created the specified window and, optionally, the identifier of
        /// the process that created the window.
        /// </summary>
        /// <remarks>If the window handle is invalid, an exception is thrown. This method wraps the native
        /// GetWindowThreadProcessId function and throws an exception on failure instead of returning zero.</remarks>
        /// <param name="hWnd">A handle to the window whose thread and process identifiers are to be retrieved.</param>
        /// <param name="lpdwProcessId">When this method returns, contains the identifier of the process that created the window specified by hWnd.</param>
        /// <returns>The identifier of the thread that created the specified window.</returns>
        internal static uint GetWindowThreadProcessId(HWND hWnd, out uint lpdwProcessId)
        {
            uint res;
            unsafe
            {
                fixed (uint* p = &lpdwProcessId)
                {
                    [DllImport("USER32.dll", ExactSpelling = true, SetLastError = true), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
                    static extern uint GetWindowThreadProcessId(HWND hWnd, uint* lpdwProcessId);
                    res = GetWindowThreadProcessId(hWnd, p);
                }
            }
            return res == 0 ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : res;
        }

        /// <summary>
        /// Attaches or detaches the input processing mechanism of one thread to that of another thread.
        /// </summary>
        /// <remarks>When two threads are attached, their input processing mechanisms are shared, allowing
        /// one thread to send input to windows created by the other. Both threads must belong to the same desktop. Use
        /// this method with caution, as improper use can lead to unexpected input behavior or security risks.</remarks>
        /// <param name="idAttach">The identifier of the thread to be attached or detached. This thread's input processing will be affected by
        /// the operation.</param>
        /// <param name="idAttachTo">The identifier of the thread to which the input processing mechanism is to be attached or from which it is
        /// to be detached.</param>
        /// <param name="fAttach">A value that determines the operation. Specify <see langword="true"/> to attach the input processing
        /// mechanisms; <see langword="false"/> to detach them.</param>
        /// <returns>A value indicating whether the operation succeeded. Returns <see langword="true"/> if the input processing
        /// mechanisms were successfully attached or detached; otherwise, <see langword="false"/>.</returns>
        internal static BOOL AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach)
        {
            [DllImport("USER32.dll", ExactSpelling = true, SetLastError = true), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
            static extern BOOL AttachThreadInput(uint idAttach, uint idAttachTo, BOOL fAttach);
            BOOL res = AttachThreadInput(idAttach, idAttachTo, fAttach);
            return !res ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : res;
        }

        /// <summary>
        /// Brings the specified window to the top of the Z order, activating it if necessary.
        /// </summary>
        /// <remarks>If the window is a top-level window, it is activated and moved to the top of the
        /// stack. If the window is minimized or not visible, it may not be brought to the foreground. This method
        /// throws an exception if the underlying Windows API call fails.</remarks>
        /// <param name="hWnd">A handle to the window to bring to the top of the Z order. The window must be a valid window handle.</param>
        /// <returns>A value indicating whether the operation succeeded. Returns <see langword="true"/> if the window was brought
        /// to the top; otherwise, <see langword="false"/>.</returns>
        internal static BOOL BringWindowToTop(HWND hWnd)
        {
            BOOL res = PInvoke.BringWindowToTop(hWnd);
            return !res ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : res;
        }

        /// <summary>
        /// Activates the specified window and returns a handle to the previously active window.
        /// </summary>
        /// <param name="hWnd">A handle to the window to be activated. Must be a valid window handle.</param>
        /// <returns>A handle to the window that was previously active.</returns>
        internal static HWND SetActiveWindow(HWND hWnd)
        {
            HWND res = PInvoke.SetActiveWindow(hWnd);
            return res == HWND.Null ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : res;
        }

        /// <summary>
        /// Sets the keyboard focus to the specified window.
        /// </summary>
        /// <remarks>If the specified window cannot receive focus, an exception is thrown. The caller must
        /// ensure that the window handle is valid and that the window is able to receive input focus.</remarks>
        /// <param name="hWnd">The handle to the window that will receive keyboard input. Must be a valid window handle.</param>
        /// <returns>A handle to the window that previously had the keyboard focus.</returns>
        internal static HWND SetFocus(HWND hWnd)
        {
            HWND res = PInvoke.SetFocus(hWnd);
            return res == HWND.Null ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : res;
        }

        /// <summary>
        /// Sends a message to the specified window and returns immediately without waiting for the window to process
        /// the message.
        /// </summary>
        /// <remarks>This method wraps the native SendNotifyMessage function and throws an exception if
        /// the underlying call fails. Unlike SendMessage, this method returns immediately and does not wait for the
        /// message to be processed by the target window.</remarks>
        /// <param name="hWnd">A handle to the window whose window procedure will receive the message.</param>
        /// <param name="Msg">The message to be sent to the window.</param>
        /// <param name="wParam">Additional message-specific information. The contents depend on the value of the Msg parameter.</param>
        /// <param name="lParam">Additional message-specific information. The contents depend on the value of the Msg parameter.</param>
        /// <returns>A value indicating the result of the message send operation. If the operation fails, an exception is thrown.</returns>
        internal static BOOL SendNotifyMessage(HWND hWnd, WINDOW_MESSAGE Msg, WPARAM wParam, LPARAM lParam)
        {
            BOOL res = PInvoke.SendNotifyMessage(hWnd, (uint)Msg, wParam, lParam);
            return !res ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : res;
        }

        /// <summary>
        /// Sends a message to the specified window and returns immediately, without waiting for the window to process
        /// the message.
        /// </summary>
        /// <remarks>This method does not wait for the recipient window to process the message. Use this
        /// method when the sender does not require a result from the message processing. The caller is responsible for
        /// ensuring that the message and parameters are appropriate for the target window.</remarks>
        /// <param name="hWnd">A handle to the window whose window procedure will receive the message.</param>
        /// <param name="Msg">The message to be sent.</param>
        /// <param name="wParam">The message-specific first parameter, passed as a string. Can be null.</param>
        /// <param name="lParam">The message-specific second parameter, passed as a string. Can be null.</param>
        /// <returns>A value of type LRESULT that indicates the result of the message processing. The meaning of the return value
        /// depends on the message sent.</returns>
        internal static BOOL SendNotifyMessage(HWND hWnd, WINDOW_MESSAGE Msg, string? wParam = null, string? lParam = null)
        {
            unsafe
            {
                fixed (char* wParamPtr = wParam)
                fixed (char* lParamPtr = lParam)
                {
                    return SendNotifyMessage(hWnd, Msg, (nuint)wParamPtr, (nint)lParamPtr);
                }
            }
        }

        /// <summary>
        /// Retrieves a handle to the system menu for the specified window, or resets it to the default system menu.
        /// </summary>
        /// <param name="hWnd">The handle to the window whose system menu is to be retrieved or reset.</param>
        /// <param name="bRevert">A value that determines the operation to perform. Specify <see langword="false"/> to retrieve the current
        /// system menu, or <see langword="true"/> to reset the system menu to its default state.</param>
        /// <returns>A safe handle to the system menu associated with the specified window.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the system menu handle cannot be retrieved.</exception>
        internal static DestroyMenuSafeHandle GetSystemMenu(HWND hWnd, BOOL bRevert)
        {
            DestroyMenuSafeHandle res = PInvoke.GetSystemMenu_SafeHandle(hWnd, bRevert);
            return res.IsInvalid ? throw new InvalidOperationException("Failed to retrieve the menu handle.") : res;
        }

        /// <summary>
        /// Sends the specified message to a window or windows and returns the result, throwing an exception if the
        /// underlying Windows API call fails.
        /// </summary>
        /// <remarks>If the underlying Windows API call fails, this method throws an exception
        /// corresponding to the last Win32 error. This method resets the last error code before invoking the API
        /// call.</remarks>
        /// <param name="hWnd">A handle to the window whose window procedure will receive the message.</param>
        /// <param name="Msg">The message to be sent. This value determines the action to be performed by the window procedure.</param>
        /// <param name="wParam">Additional message-specific information. The exact meaning depends on the value of the Msg parameter.</param>
        /// <param name="lParam">Additional message-specific information. The exact meaning depends on the value of the Msg parameter.</param>
        /// <returns>The result of the message processing, as returned by the window procedure.</returns>
        internal static LRESULT SendMessage(HWND hWnd, WINDOW_MESSAGE Msg, WPARAM wParam, LPARAM lParam)
        {
            PInvoke.SetLastError(0); LRESULT res = PInvoke.SendMessage(hWnd, (uint)Msg, wParam, lParam);
            return (WIN32_ERROR)Marshal.GetLastWin32Error() is WIN32_ERROR lastWin32Error && lastWin32Error != WIN32_ERROR.NO_ERROR
                ? throw ExceptionUtilities.GetException(lastWin32Error)
                : res;
        }

        /// <summary>
        /// Sends the specified window message to the given window handle, using string parameters for wParam and
        /// lParam.
        /// </summary>
        /// <remarks>The string parameters are pinned and passed as pointers to the underlying native
        /// SendMessage call. The caller is responsible for ensuring that the message and parameter values are
        /// appropriate for the target window and message type.</remarks>
        /// <param name="hWnd">The handle to the window that will receive the message.</param>
        /// <param name="Msg">The window message to send.</param>
        /// <param name="wParam">The string value to be passed as the wParam parameter of the message. Can be null.</param>
        /// <param name="lParam">The string value to be passed as the lParam parameter of the message. Can be null.</param>
        /// <returns>A value of type LRESULT that contains the result of processing the message by the target window.</returns>
        internal static LRESULT SendMessage(HWND hWnd, WINDOW_MESSAGE Msg, string? wParam, string? lParam)
        {
            unsafe
            {
                fixed (char* wParamPtr = wParam)
                fixed (char* lParamPtr = lParam)
                {
                    return SendMessage(hWnd, Msg, (nuint)wParamPtr, (nint)lParamPtr);
                }
            }
        }

        /// <summary>
        /// Releases the mouse capture from a window in the current thread, allowing mouse input to be sent to other
        /// windows.
        /// </summary>
        /// <remarks>This method wraps the native ReleaseCapture function. If the operation fails, a Win32
        /// exception is thrown. Typically used in scenarios where mouse capture was previously set and needs to be
        /// released to restore normal mouse input behavior.</remarks>
        /// <returns>A value indicating whether the mouse capture was successfully released.</returns>
        internal static BOOL ReleaseCapture()
        {
            BOOL res = PInvoke.ReleaseCapture();
            return !res ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : res;
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
            BOOL res = PInvoke.RemoveMenu(hMenu, (uint)uPosition, uFlags);
            return !res ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : res;
        }

        /// <summary>
        /// Retrieves a handle to the top-level window that matches the specified class name and window name.
        /// </summary>
        /// <remarks>This method wraps the native <c>FindWindow</c> function and throws an exception if the window is not found. Use this method to locate a top-level window by its class name, window name, or both.</remarks>
        /// <param name="lpClassName">The class name of the window to find. This can be a null-terminated string or <see langword="null"/> to ignore the class name.</param>
        /// <param name="lpWindowName">The window name (title) of the window to find. This can be a null-terminated string or <see langword="null"/> to ignore the window name.</param>
        /// <returns>A handle to the window that matches the specified criteria.</returns>
        internal static HWND FindWindow(string? lpClassName, string? lpWindowName)
        {
            HWND res = PInvoke.FindWindow(lpClassName, lpWindowName);
            return res.IsNull ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : res;
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
            HMONITOR monitor = PInvoke.MonitorFromPoint(pt, dwFlags);
            return monitor.IsNull ? throw new InvalidOperationException("Failed to retrieve monitor from point.") : monitor;
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
            uint res = PInvoke.GetDpiForWindow(hwnd);
            return res == 0 ? throw new InvalidOperationException("Failed to get DPI scale for window handle.") : res;
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
        internal static MESSAGEBOX_RESULT MessageBoxTimeout(nint hWnd, string lpText, string lpCaption, MESSAGEBOX_STYLE uType, ushort wLanguageId, uint dwTimeout)
        {
            if (string.IsNullOrWhiteSpace(lpText))
            {
                throw new ArgumentNullException(nameof(lpText), "Message text cannot be null or empty.");
            }
            if (string.IsNullOrWhiteSpace(lpCaption))
            {
                throw new ArgumentNullException(nameof(lpCaption), "Message caption cannot be null or empty.");
            }
            [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
            static extern MESSAGEBOX_RESULT MessageBoxTimeoutW(nint hWnd, string lpText, string lpCaption, MESSAGEBOX_STYLE uType, ushort wLanguageId, uint dwMilliseconds);
            MESSAGEBOX_RESULT res = MessageBoxTimeoutW(hWnd, lpText, lpCaption, uType, wLanguageId, dwTimeout);
            return res == 0 ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : res;
        }

        /// <summary>
        /// Sets the specified window as the foreground window.
        /// </summary>
        /// <param name="hWnd">A handle to the window to be set as the foreground window.</param>
        /// <param name="throwOnError">If <see langword="true"/>, an exception is thrown if the operation fails; otherwise, the method returns <see langword="false"/> on failure.</param>
        /// <returns><see langword="true"/> if the operation succeeds; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the operation fails to set the specified window as the foreground window.</exception>
        internal static BOOL SetForegroundWindow(HWND hWnd, bool throwOnError = true)
        {
            BOOL res = PInvoke.SetForegroundWindow(hWnd);
            return !res && throwOnError ? throw new InvalidOperationException($"Failed to set the window as foreground.") : res;
        }

        /// <summary>
        /// Retrieves the handle to the shell's desktop window.
        /// </summary>
        /// <returns>A <see cref="HWND"/> representing the handle to the shell's desktop window.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the shell window handle cannot be retrieved.</exception>
        internal static HWND GetShellWindow()
        {
            HWND res = PInvoke.GetShellWindow();
            return res.IsNull ? throw new InvalidOperationException("Failed to retrieve the shell window handle.") : res;
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
            plii = new LASTINPUTINFO
            {
                cbSize = (uint)Marshal.SizeOf<LASTINPUTINFO>()
            };
            BOOL res = PInvoke.GetLastInputInfo(ref plii);
            return !res ? throw new InvalidOperationException("Failed to retrieve the last input info.") : res;
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
