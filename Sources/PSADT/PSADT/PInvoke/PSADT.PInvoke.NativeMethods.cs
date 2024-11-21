using System;
using System.IO;
using System.Text;
using System.Security;
using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;
using System.Collections.Generic;

/// Some Native Method Declarations from:
/// https://github.com/dahall/Vanara

namespace PSADT.PInvoke
{
    /// <summary>
    /// Contains native method declarations for Win32 API calls.
    /// </summary>
    internal static partial class NativeMethods
    {
        #region PInvoke: user32.dll

        /// <summary>
        /// Retrieves the specified system metric or system configuration setting for the current operating system.
        /// System metrics can be used to retrieve dimensions of various display elements, the status of certain system features, and other system settings.
        /// </summary>
        /// <param name="nIndex">
        /// The system metric or configuration setting to be retrieved. This value must be one of the constants defined in the <see cref="SystemMetric"/> enumeration.
        /// </param>
        /// <returns>
        /// If the function succeeds, the return value is the requested system metric or configuration setting.
        /// If the function fails, the return value is 0. To get extended error information, call <see cref="Marshal.GetLastWin32Error"/>.
        /// </returns>
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern int GetSystemMetrics(SystemMetric nIndex);

        /// <summary>
        /// Enumerates all top-level windows on the screen.
        /// </summary>
        /// <param name="lpEnumFunc">The callback function to invoke for each window.</param>
        /// <param name="lParam">A pointer to application-defined data.</param>
        /// <returns>True if successful; otherwise, false.</returns>
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumWindows(EnumWindowsProcD lpEnumFunc, ref IntPtr lParam);

        /// <summary>
        /// Retrieves the length of the text in the specified window.
        /// </summary>
        /// <param name="hWnd">Handle to the window.</param>
        /// <returns>The length of the window text in characters.</returns>
        [DllImport("user32.dll", SetLastError = false, CharSet = CharSet.Unicode)]
        public static extern int GetWindowTextLength(IntPtr hWnd);

        /// <summary>
        /// Retrieves the text of the specified window.
        /// </summary>
        /// <param name="hWnd">Handle to the window.</param>
        /// <param name="lpString">The buffer that receives the text.</param>
        /// <param name="nMaxCount">The maximum number of characters to copy to the buffer.</param>
        /// <returns>The length of the copied text.</returns>
        [DllImport("user32.dll", SetLastError = false, CharSet = CharSet.Unicode)]
        public static extern int GetWindowText(IntPtr hWnd, [Out] char[] lpString, int nMaxCount);

        /// <summary>
        /// Checks whether the specified window is enabled.
        /// </summary>
        /// <param name="hWnd">Handle to the window.</param>
        /// <returns>True if the window is enabled; otherwise, false.</returns>
        [DllImport("user32.dll", SetLastError = false, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWindowEnabled(IntPtr hWnd);

        /// <summary>
        /// Determines whether the specified window is visible.
        /// </summary>
        /// <param name="hWnd">Handle to the window.</param>
        /// <returns>True if the window is visible; otherwise, false.</returns>
        [DllImport("user32.dll", SetLastError = false, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        /// <summary>
        /// Checks whether the specified window is minimized (iconic).
        /// </summary>
        /// <param name="hWnd">Handle to the window.</param>
        /// <returns>True if the window is minimized; otherwise, false.</returns>
        [DllImport("user32.dll", SetLastError = false, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsIconic(IntPtr hWnd);

        /// <summary>
        /// Shows or hides a window based on the specified flag.
        /// </summary>
        /// <param name="hWnd">Handle to the window.</param>
        /// <param name="flags">Specifies how the window is to be shown.</param>
        /// <returns>True if the window was previously visible; otherwise, false.</returns>
        [DllImport("user32.dll", SetLastError = false, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ShowWindow(IntPtr hWnd, ShowWindowEnum flags);

        /// <summary>
        /// Sets the specified window as the active window.
        /// </summary>
        /// <param name="hwnd">Handle to the window.</param>
        /// <returns>A handle to the previously active window, or IntPtr.Zero if none.</returns>
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr SetActiveWindow(IntPtr hwnd);

        /// <summary>
        /// Brings the specified window to the top of the Z-order.
        /// </summary>
        /// <param name="hWnd">Handle to the window.</param>
        /// <returns>True if successful; otherwise, false.</returns>
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool BringWindowToTop(IntPtr hWnd);

        /// <summary>
        /// Retrieves a handle to the foreground window (the window with which the user is currently working).
        /// </summary>
        /// <returns>A handle to the foreground window. The foreground window can be NULL in certain circumstances.</returns>
        [DllImport("user32.dll", SetLastError = false, CharSet = CharSet.Unicode)]
        public static extern IntPtr GetForegroundWindow();

        /// <summary>
        /// Brings the specified window to the foreground and activates it.
        /// </summary>
        /// <param name="hWnd">Handle to the window to be brought to the foreground.</param>
        /// <returns>True if successful; otherwise, false.</returns>
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        /// <summary>
        /// Sets the keyboard focus to the specified window.
        /// </summary>
        /// <param name="hWnd">Handle to the window that will receive the keyboard focus.</param>
        /// <returns>If the function succeeds, the return value is the handle to the window that previously had the keyboard focus. If the hWnd parameter is invalid or the window is not attached to the calling thread's message queue, the return value is NULL.</returns>
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr SetFocus(IntPtr hWnd);

        /// <summary>
        /// Retrieves the identifier of the thread that created the specified window and optionally returns the process ID.
        /// </summary>
        /// <param name="hWnd">Handle to the window.</param>
        /// <param name="lpdwProcessId">An output parameter that receives the process ID.</param>
        /// <returns>The thread identifier that created the window.</returns>
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

        /// <summary>
        /// Attaches or detaches the input processing mechanism of one thread to another thread.
        /// </summary>
        /// <param name="idAttach">The identifier of the thread to attach.</param>
        /// <param name="idAttachTo">The identifier of the thread to which to attach.</param>
        /// <param name="fAttach">True to attach, false to detach.</param>
        /// <returns>True if successful; otherwise, false.</returns>
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AttachThreadInput(int idAttach, int idAttachTo, [MarshalAs(UnmanagedType.Bool)] bool fAttach);

        /// <summary>
        /// Retrieves information about the specified window.
        /// </summary>
        /// <param name="hWnd">Handle to the window.</param>
        /// <param name="nIndex">Specifies the zero-based offset to the value to be retrieved.</param>
        /// <returns>The value at the specified offset.</returns>
        [DllImport("user32.dll", SetLastError = false, EntryPoint = "GetWindowLong", CharSet = CharSet.Unicode)]
        public static extern IntPtr GetWindowLong32(IntPtr hWnd, int nIndex);

        /// <summary>
        /// Retrieves information about the specified window in a 64-bit process.
        /// </summary>
        /// <param name="hWnd">Handle to the window.</param>
        /// <param name="nIndex">Specifies the zero-based offset to the value to be retrieved.</param>
        /// <returns>The value at the specified offset.</returns>
        [DllImport("user32.dll", SetLastError = false, EntryPoint = "GetWindowLongPtr", CharSet = CharSet.Unicode)]
        public static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

        /// <summary>
        /// Retrieves the handle to the system menu for the specified window.
        /// </summary>
        /// <param name="hWnd">Handle to the window.</param>
        /// <param name="bRevert">True to restore the default menu, false to retrieve the current menu.</param>
        /// <returns>A handle to the system menu.</returns>
        [DllImport("user32.dll", SetLastError = false, CharSet = CharSet.Unicode)]
        public static extern IntPtr GetSystemMenu(IntPtr hWnd, [MarshalAs(UnmanagedType.Bool)] bool bRevert);

        /// <summary>
        /// Enables, disables, or grays out a menu item in a system menu.
        /// </summary>
        /// <param name="hMenu">Handle to the menu.</param>
        /// <param name="uIDEnableItem">Specifies the menu item to enable or disable.</param>
        /// <param name="uEnable">Specifies whether to enable or disable the menu item.</param>
        /// <returns>True if successful; otherwise, false.</returns>
        [DllImport("user32.dll", SetLastError = false, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnableMenuItem(IntPtr hMenu, uint uIDEnableItem, uint uEnable);

        /// <summary>
        /// Destroys the specified menu and frees any memory that the menu occupies.
        /// </summary>
        /// <param name="hWnd">Handle to the menu to be destroyed.</param>
        /// <returns>True if successful; otherwise, false.</returns>
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DestroyMenu(IntPtr hWnd);

        /// <summary>
        /// Enumerates the child windows of a parent window.
        /// </summary>
        /// <param name="hWndParent">Handle to the parent window.</param>
        /// <param name="lpEnumFunc">The callback function to be called for each child window.</param>
        /// <param name="lParam">Pointer to application-defined data.</param>
        /// <returns>True if successful; otherwise, false.</returns>
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumChildWindows(IntPtr hWndParent, EnumChildProc lpEnumFunc, IntPtr lParam);

        /// <summary>
        /// Sends the specified message to a window or windows.
        /// </summary>
        /// <param name="hWnd">Handle to the window.</param>
        /// <param name="Msg">The message to be sent.</param>
        /// <param name="wParam">Additional message-specific information.</param>
        /// <param name="lParam">Additional message-specific information.</param>
        /// <returns>The result of the message processing; it depends on the message sent.</returns>
        [DllImport("user32.dll", SetLastError = false, CharSet = CharSet.Unicode)]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        /// <summary>
        /// Retrieves a handle to the device context (DC) for the specified window or for the entire screen.
        /// </summary>
        /// <param name="hWnd">Handle to the window or screen.</param>
        /// <returns>A handle to the DC.</returns>
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr GetDC(IntPtr hWnd);

        /// <summary>
        /// Releases a device context (DC), freeing the DC for use by other applications.
        /// </summary>
        /// <param name="hWnd">Handle to the window.</param>
        /// <param name="hDC">Handle to the DC to be released.</param>
        /// <returns>1 if successful; otherwise, 0.</returns>
        [DllImport("user32.dll", SetLastError = false, CharSet = CharSet.Unicode)]
        public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        /// <summary>
        /// Retrieves a handle to the display monitor that is nearest to the specified window.
        /// </summary>
        /// <param name="hwnd">Handle to the window.</param>
        /// <param name="dwFlags">Determines the function's return value if the window does not intersect any display monitor.</param>
        /// <returns>A handle to the display monitor.</returns>
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        /// <summary>
        /// Retrieves the DPI of the display for the given window.
        /// </summary>
        /// <param name="hwnd">Handle to the window.</param>
        /// <returns>The DPI for the display where the window is located.</returns>
        [DllImport("user32.dll", SetLastError = false, CharSet = CharSet.Unicode)]
        public static extern uint GetDpiForWindow(IntPtr hwnd);

        /// <summary>
        /// Sends a message to the specified window, allowing the message to return immediately without waiting for the recipient to process the message.
        /// </summary>
        /// <param name="hWnd">A handle to the window whose window procedure will receive the message. If this parameter is <see cref="IntPtr.Zero"/>, the message is sent to all top-level windows in the system.</param>
        /// <param name="Msg">The message to be sent.</param>
        /// <param name="wParam">Additional message-specific information.</param>
        /// <param name="lParam">Additional message-specific information.</param>
        /// <returns>
        /// If the function succeeds, the return value is <c>true</c>. If the function fails, the return value is <c>false</c>. To get extended error information, call <see cref="Marshal.GetLastWin32Error"/>.
        /// </returns>
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SendNotifyMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        /// <summary>
        /// Sends the specified message to a window or windows. If the window was created by the calling thread, SendMessageTimeout calls the window procedure for the window and does not return until the window procedure has processed the message.
        /// </summary>
        /// <param name="hWnd">A handle to the window whose window procedure will receive the message. If this parameter is <see cref="IntPtr.Zero"/>, the message is sent to all top-level windows in the system.</param>
        /// <param name="Msg">The message to be sent.</param>
        /// <param name="wParam">Additional message-specific information.</param>
        /// <param name="lParam">The message string to send, or <c>null</c> for no string.</param>
        /// <param name="fuFlags">The behavior of this function. This parameter can be one or more of the following values: <see cref="SMTO_NORMAL"/>, <see cref="SMTO_BLOCK"/>, <see cref="SMTO_ABORTIFHUNG"/>, or <see cref="SMTO_NOTIMEOUTIFNOTHUNG"/>.</param>
        /// <param name="uTimeout">The duration, in milliseconds, of the time-out period.</param>
        /// <param name="lpdwResult">Receives the result of the message processing.</param>
        /// <returns>The return value is the result of the message processing, which depends on the message sent.</returns>
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, IntPtr wParam, string? lParam, uint fuFlags, uint uTimeout, IntPtr lpdwResult);

        /// <summary>
        /// Notifies the system of an event that an application has performed.
        /// </summary>
        /// <param name="eventId">Describes the event that has occurred. This parameter can be one of the values from the <see cref="SHCNE"/> enumeration.</param>
        /// <param name="flags">Flags that indicate the meaning of the <paramref name="item1"/> and <paramref name="item2"/> parameters. This parameter can be one or more of the values from the <see cref="SHCNF"/> enumeration.</param>
        /// <param name="item1">A handle to the first item involved in the event.</param>
        /// <param name="item2">A handle to the second item involved in the event. This parameter is optional and may be <see cref="IntPtr.Zero"/>.</param>
        [DllImport("shell32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern void SHChangeNotify(int eventId, uint flags, IntPtr item1, IntPtr item2);

        /// <summary>
        /// Loads a string resource from the specified module's executable file and copies the string into a <see cref="StringBuilder"/>.
        /// </summary>
        /// <param name="hInstance">A handle to an instance of the module whose executable file contains the string resource.</param>
        /// <param name="uID">The identifier of the string to be loaded.</param>
        /// <param name="lpBuffer">The <see cref="StringBuilder"/> that receives the string.</param>
        /// <param name="nBufferMax">The maximum number of characters to be copied into the buffer.</param>
        /// <returns>
        /// If the function succeeds, the return value is the number of characters copied into the buffer, not including the terminating null character.
        /// If the function fails, the return value is 0. To get extended error information, call <see cref="Marshal.GetLastWin32Error"/>.
        /// </returns>
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [System.Security.SecurityCritical]
        public static extern int LoadString(SafeLibraryHandle hInstance, int uID, StringBuilder lpBuffer, int nBufferMax);

        /// <summary>
        /// Loads a string resource from the executable file associated with a specified module.
        /// </summary>
        /// <param name="hInstance">
        /// A handle to an instance of the module whose executable file contains the string resource. To get the handle to the application
        /// itself, call the GetModuleHandle function with NULL.
        /// </param>
        /// <param name="uID">The identifier of the string to be loaded.</param>
        /// <param name="loadedString">
        /// When this method returns, contains the string resource loaded from the module, if the operation is successful.
        /// If the operation fails, this will be set to <see langword="null"/>.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the string resource is successfully loaded; otherwise, <see langword="false"/>.
        /// To get extended error information, call <see cref="Marshal.GetLastWin32Error"/>.
        /// </returns>
        public static bool LoadString(SafeLibraryHandle hInstance, int uID, out string? loadedString)
        {
            const int bufferSize = 255;
            StringBuilder buffer = new StringBuilder(bufferSize);

            int result = LoadString(hInstance, uID, buffer, buffer.Capacity);
            if (result == 0)
            {
                loadedString = null;
                return false;
            }

            loadedString = buffer.ToString(0, result);
            return true;
        }

        /// <summary>
        /// <para>This function has no parameters.</para>
        /// </summary>
        /// <returns>
        /// <para>Type: <c>Type: <c>BOOL</c></c></para>
        /// <para>If the function succeeds, the return value is nonzero. Otherwise, the return value is zero.</para>
        /// </returns>
        /// <remarks>
        /// <para>For more information, see Setting the default DPI awareness for a process.</para>
        /// </remarks>
        [DllImport("user32.dll", SetLastError = false, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetProcessDPIAware();

        /// <summary>
        /// <para>
        /// It is recommended that you set the process-default DPI awareness via application manifest. See Setting the default DPI awareness
        /// for a process for more information. Setting the process-default DPI awareness via API call can lead to unexpected application behavior.
        /// </para>
        /// <para>
        /// Sets the current process to a specified dots per inch (dpi) awareness context. The DPI awareness contexts are from the
        /// DPI_AWARENESS_CONTEXT value.
        /// </para>
        /// </summary>
        /// <param name="value">A DPI_AWARENESS_CONTEXT handle to set.</param>
        /// <returns>
        /// <para>
        /// This function returns TRUE if the operation was successful, and FALSE otherwise. To get extended error information, call GetLastError.
        /// </para>
        /// <para>
        /// Possible errors are <c>ERROR_INVALID_PARAMETER</c> for an invalid input, and <c>ERROR_ACCESS_DENIED</c> if the default API
        /// awareness mode for the process has already been set (via a previous API call or within the application manifest).
        /// </para>
        /// </returns>
        /// <remarks>
        /// <para>
        /// This API is a more advanced version of the previously existing SetProcessDpiAwareness API, allowing for the process default to be
        /// set to the finer-grained DPI_AWARENESS_CONTEXT values. Most importantly, this allows you to programmatically set <c>Per Monitor
        /// v2</c> as the process default value, which is not possible with the previous API.
        /// </para>
        /// <para>
        /// This method sets the default DPI_AWARENESS_CONTEXT for all threads within an application. Individual threads can have their DPI
        /// awareness changed from the default with the SetThreadDpiAwarenessContext method.
        /// </para>
        /// <para>
        /// <c>Important</c> In general, it is recommended to not use <c>SetProcessDpiAwarenessContext</c> to set the DPI awareness for your
        /// application. If possible, you should declare the DPI awareness for your application in the application manifest. For more
        /// information, see Setting the default DPI awareness for a process.
        /// </para>
        /// <para>
        /// You must call this API before you call any APIs that depend on the DPI awareness (including before creating any UI in your
        /// process). Once API awareness is set for an app, any future calls to this API will fail. This is true regardless of whether you
        /// set the DPI awareness in the manifest or by using this API.
        /// </para>
        /// <para>If the DPI awareness level is not set, the default value is <c>DPI_AWARENESS_CONTEXT_UNAWARE</c>.</para>
        /// </remarks>
        [DllImport("user32.dll", SetLastError = true, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT value);

        /// <summary>Set the DPI awareness for the current thread to the provided value.</summary>
        /// <param name="dpiContext">The new DPI_AWARENESS_CONTEXT for the current thread. This context includes the DPI_AWARENESS value.</param>
        /// <returns>
        /// The old DPI_AWARENESS_CONTEXT for the thread. If the dpiContext is invalid, the thread will not be updated and the return value
        /// will be <c>NULL</c>. You can use this value to restore the old <c>DPI_AWARENESS_CONTEXT</c> after overriding it with a predefined value.
        /// </returns>
        /// <remarks>Use this API to change the DPI_AWARENESS_CONTEXT for the thread from the default value for the app.</remarks>
        [DllImport("user32.dll", SetLastError = false, ExactSpelling = true)]
        public static extern DPI_AWARENESS_CONTEXT SetThreadDpiAwarenessContext(DPI_AWARENESS_CONTEXT dpiContext);

        #endregion

        #region PInvoke: shlwapi.dll

        /// <summary>
        /// Searches for a file in a set of directories.
        /// </summary>
        /// <param name="pszFile">The name of the file for which to search. The function does not search for a file name specified by a relative path.</param>
        /// <param name="ppszOtherDirs">A null-terminated array of null-terminated strings, each specifying a directory to be searched. This value can be null.</param>
        /// <returns>If the function finds the file, the return value is a nonzero value. If the function does not find the file, the return value is zero.</returns>
        [DllImport("shlwapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool PathFindOnPath([In, Out] StringBuilder pszFile, [In, Optional] string[] ppszOtherDirs);

        #endregion

        #region PInvoke: kernel32.dll

        /// <summary>
        /// Allocates a new console for the calling process.
        /// </summary>
        /// <returns>If the function succeeds, the return value is nonzero. If the function fails, the return value is zero.</returns>
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AllocConsole();

        /// <summary>Attaches the calling process to the console of the specified process.</summary>
        /// <param name="dwProcessId">
        /// <para>The identifier of the process whose console is to be used. This parameter can be one of the following values.</para>
        /// <para>
        /// <list type="table">
        /// <listheader>
        /// <term>Value</term>
        /// <term>Meaning</term>
        /// </listheader>
        /// <item>
        /// <term>pid</term>
        /// <term>Use the console of the specified process.</term>
        /// </item>
        /// <item>
        /// <term>ATTACH_PARENT_PROCESS (DWORD)-1</term>
        /// <term>Use the console of the parent of the current process.</term>
        /// </item>
        /// </list>
        /// </para>
        /// </param>
        /// <returns>
        /// <para>If the function succeeds, the return value is nonzero.</para>
        /// <para>If the function fails, the return value is zero. To get extended error information, call <c>GetLastError</c>.</para>
        /// </returns>
        // BOOL WINAPI AttachConsole( _In_ DWORD dwProcessId );
        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AttachConsole(uint dwProcessId);

        /// <summary>
        /// Detaches the calling process from its console.
        /// </summary>
        /// <returns>If the function succeeds, the return value is nonzero. If the function fails, the return value is zero.</returns>
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool FreeConsole();

        /// <summary>
        /// Closes an open object handle.
        /// </summary>
        /// <param name="hObject">A valid handle to an open object.</param>
        /// <returns>If the function succeeds, the return value is nonzero. If the function fails, the return value is zero.</returns>
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr hObject);

        /// <summary>Creates an anonymous pipe, and returns handles to the read and write ends of the pipe.</summary>
        /// <param name="hReadPipe">A pointer to a variable that receives the read handle for the pipe.</param>
        /// <param name="hWritePipe">A pointer to a variable that receives the write handle for the pipe.</param>
        /// <param name="lpPipeAttributes">
        /// <para>
        /// A pointer to a <c>SECURITY_ATTRIBUTES</c> structure that determines whether the returned handle can be inherited by child
        /// processes. If lpPipeAttributes is <c>NULL</c>, the handle cannot be inherited.
        /// </para>
        /// <para>
        /// The <c>lpSecurityDescriptor</c> member of the structure specifies a security descriptor for the new pipe. If lpPipeAttributes is
        /// <c>NULL</c>, the pipe gets a default security descriptor. The ACLs in the default security descriptor for a pipe come from the
        /// primary or impersonation token of the creator.
        /// </para>
        /// </param>
        /// <param name="nSize">
        /// The size of the buffer for the pipe, in bytes. The size is only a suggestion; the system uses the value to calculate an
        /// appropriate buffering mechanism. If this parameter is zero, the system uses the default buffer size.
        /// </param>
        /// <returns>
        /// <para>If the function succeeds, the return value is nonzero.</para>
        /// <para>If the function fails, the return value is zero. To get extended error information, call <c>GetLastError</c>.</para>
        /// </returns>
        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CreatePipe(out SafeFileHandle hReadPipe, out SafeFileHandle hWritePipe, [In, Optional] SECURITY_ATTRIBUTES lpPipeAttributes, [In, Optional] uint nSize);

        /// <summary>
        /// Retrieves the Terminal Services session associated with a specified process.
        /// </summary>
        /// <param name="dwProcessId">The process identifier.</param>
        /// <param name="pSessionId">A pointer to a variable that receives the session identifier.</param>
        /// <returns>If the function succeeds, the return value is nonzero. If the function fails, the return value is zero.</returns>
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ProcessIdToSessionId(uint dwProcessId, out uint pSessionId);

        /// <summary>Retrieves the process identifier of the calling process.</summary>
        /// <returns>The return value is the process identifier of the calling process.</returns>
        [DllImport("kernel32.dll", SetLastError = false, CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern uint GetCurrentProcessId();

        /// <summary>
        /// Retrieves the identifier of the calling thread.
        /// </summary>
        /// <returns>The thread identifier of the calling thread.</returns>
        [DllImport("kernel32.dll", SetLastError = false, CharSet = CharSet.Unicode)]
        public static extern int GetCurrentThreadId();

        /// <summary>Retrieves a handle for the current process.</summary>
        /// <returns>The return value is a handle to the current process.</returns>
        // HANDLE WINAPI GetCurrentProcess(void); https://msdn.microsoft.com/en-us/library/windows/desktop/ms683179(v=vs.85).aspx
        [DllImport("kernel32.dll", SetLastError = false, ExactSpelling = true)]
        public static extern IntPtr GetCurrentProcess();

        /// <summary>Converts a file time to system time format. System time is based on Coordinated Universal Time (UTC).</summary>
    	/// <param name="lpFileTime">
    	/// A pointer to a FILETIME structure containing the file time to be converted to system (UTC) date and time format. This value must
    	/// be less than 0x8000000000000000. Otherwise, the function fails.
    	/// </param>
    	/// <param name="lpSystemTime">A pointer to a SYSTEMTIME structure to receive the converted file time.</param>
    	/// <returns>
    	/// If the function succeeds, the return value is nonzero. If the function fails, the return value is zero.To get extended error
    	/// information, call GetLastError.
    	/// </returns>
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true), SuppressUnmanagedCodeSecurity]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool FileTimeToSystemTime(in FILETIME lpFileTime, out SYSTEMTIME lpSystemTime);

        [DllImport("kernel32.dll", SetLastError = false, CharSet = CharSet.Unicode, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetProductInfo(uint dwOSMajorVersion, uint dwOSMinorVersion, uint dwSpMajorVersion, uint dwSpMinorVersion, out PRODUCT_SKU pdwReturnedProductType);

        /// <summary>
        /// Creates a handle for the specified file with read-only access.
        /// </summary>
        /// <param name="filePath">The file path of the file to open.</param>
        /// <returns>A <see cref="SafeFileHandle"/> for the opened file.</returns>
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern SafeFileHandle CreateFile(
            string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            IntPtr lpSecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        /// <summary>
        /// Creates an instance of a named pipe and returns a handle for subsequent pipe operations.
        /// </summary>
        /// <param name="lpName">The unique pipe name.</param>
        /// <param name="dwOpenMode">The open mode.</param>
        /// <param name="dwPipeMode">The pipe mode.</param>
        /// <param name="nMaxInstances">The maximum number of instances that can be created for this pipe.</param>
        /// <param name="nOutBufferSize">The number of bytes to reserve for the output buffer.</param>
        /// <param name="nInBufferSize">The number of bytes to reserve for the input buffer.</param>
        /// <param name="nDefaultTimeOut">The default time-out value, in milliseconds.</param>
        /// <param name="lpSecurityAttributes">A pointer to a SECURITY_ATTRIBUTES structure.</param>
        /// <returns>If the function succeeds, the return value is a handle to the server end of a named pipe instance.</returns>
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern SafePipeHandle CreateNamedPipe(
            string lpName,
            uint dwOpenMode,
            uint dwPipeMode,
            uint nMaxInstances,
            uint nOutBufferSize,
            uint nInBufferSize,
            uint nDefaultTimeOut,
            IntPtr lpSecurityAttributes);

        /// <summary>
        /// Enables a named pipe server process to wait for a client process to connect to an instance of a named pipe.
        /// </summary>
        /// <param name="hNamedPipe">A handle to the server end of a named pipe instance.</param>
        /// <param name="lpOverlapped">A pointer to an OVERLAPPED structure.</param>
        /// <returns>If the function succeeds, the return value is nonzero.</returns>
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ConnectNamedPipe(SafePipeHandle hNamedPipe, IntPtr lpOverlapped);

        /// <summary>
        /// <para>Retrieves the client process identifier for the specified named pipe.</para>
        /// </summary>
        /// <param name="Pipe">
        /// <para>A handle to an instance of a named pipe. This handle must be created by the CreateNamedPipe function.</para>
        /// </param>
        /// <param name="ClientProcessId">
        /// <para>The process identifier.</para>
        /// </param>
        /// <returns>
        /// <para>If the function succeeds, the return value is nonzero.</para>
        /// <para>If the function fails, the return value is zero. To get extended error information, call the GetLastError function.</para>
        /// </returns>
        /// <remarks>
        /// <para>
        /// <c>Windows 10, version 1709:</c> Pipes are only supported within an app-container; ie, from one UWP process to another UWP
        /// process that's part of the same app. Also, named pipes must use the syntax "\.\pipe\LOCAL" for the pipe name.
        /// </para>
        /// </remarks>
        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetNamedPipeClientProcessId(IntPtr Pipe, out uint ClientProcessId);

        /// <summary>
        /// Loads the specified module into the address space of the calling process. The module can be a dynamic-link library (DLL) or an executable file.
        /// </summary>
        /// <param name="lpLibFileName">The name of the module to be loaded. This can be either a full path or a filename without a path.</param>
        /// <param name="hFile">Reserved; must be <see cref="SafeLibraryHandle.Null"/> or <see cref="IntPtr.Zero"/>.</param>
        /// <param name="dwFlags">The action to be taken when loading the module. This parameter can include one or more of the <see cref="LoadLibraryExFlags"/>.</param>
        /// <returns>
        /// If the function succeeds, the return value is a handle to the loaded module. If the function fails, the return value is <see cref="SafeLibraryHandle.InvalidHandle"/>.
        /// To get extended error information, call <see cref="Marshal.GetLastWin32Error"/>.
        /// </returns>
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern SafeLibraryHandle LoadLibraryEx(
            [MarshalAs(UnmanagedType.LPWStr)] string lpLibFileName,
            SafeLibraryHandle hFile,
            [Optional] LoadLibraryExFlags dwFlags);

        /// <summary>
        /// Frees the loaded dynamic-link library (DLL) module and, if necessary, decrements its reference count.
        /// When the reference count reaches zero, the module is unloaded from the address space of the calling process.
        /// </summary>
        /// <param name="hModule">A handle to the loaded DLL module. The <see cref="LoadLibraryEx"/> function returns this handle.</param>
        /// <returns>
        /// If the function succeeds, the return value is <c>true</c>. If the function fails, the return value is <c>false</c>. To get extended error information, call <see cref="Marshal.GetLastWin32Error"/>.
        /// </returns>
        [DllImport("kernel32.dll", SetLastError = false)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool FreeLibrary(IntPtr hModule);

        /// <summary>
        /// Checks whether the system has completed the OOBE (Out of Box Experience) process.
        /// </summary>
        /// <param name="isOobeComplete">A reference to an integer where the result will be stored. The value will be set to 1 if OOBE is complete, 0 otherwise.</param>
        /// <returns><c>true</c> if the function call succeeds; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// If the function fails, use <see cref="Marshal.GetLastWin32Error"/> to retrieve extended error information. Ensure the application is running with the necessary permissions.
        /// </remarks>
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool OOBEComplete(out int isOobeComplete);

        /// <summary>
        /// Retrieves a string from the specified section in an initialization file.
        /// </summary>
        /// <param name="lpAppName">The name of the section containing the key name. If this parameter is null, all section names are returned.</param>
        /// <param name="lpKeyName">The name of the key whose associated string is to be retrieved. If this parameter is null, all key names in the section are returned.</param>
        /// <param name="lpDefault">A default string. If the key name cannot be found in the initialization file, the default value is returned.</param>
        /// <param name="lpReturnedString">A buffer that receives the retrieved string.</param>
        /// <param name="nSize">The size of the buffer pointed to by lpReturnedString, in characters.</param>
        /// <param name="lpFileName">The name of the initialization file.</param>
        /// <returns>The number of characters copied to the buffer, not including the terminating null character.</returns>
        /// <remarks>
        /// If the function succeeds, the return value is <c>true</c>. If the function fails, the return value is <c>false</c>. To get extended error information, call <see cref="Marshal.GetLastWin32Error"/>.
        /// </remarks>
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetPrivateProfileString(
            [Optional] string lpAppName,
            [Optional] string lpKeyName,
            [Optional] string lpDefault,
            StringBuilder lpReturnedString,
            uint nSize,
            string lpFileName);

        /// <summary>
        /// Retrieves a string from the specified section in an initialization file (INI file).
        /// </summary>
        /// <param name="lpAppName">
        /// The name of the section containing the key. If this parameter is <c>null</c>, all section names are retrieved.
        /// </param>
        /// <param name="lpKeyName">
        /// The name of the key whose associated string is to be retrieved. If this parameter is <c>null</c>, all keys in the section are retrieved.
        /// </param>
        /// <param name="lpDefault">
        /// A default string. If the key name cannot be found in the initialization file, <paramref name="lpDefault"/> is returned. If this parameter is <c>null</c>, an empty string is returned.
        /// </param>
        /// <param name="lpFileName">
        /// The name of the initialization file (INI file).
        /// </param>
        /// <param name="result">
        /// When this method returns, contains the string associated with the specified key, or the default string if the key is not found.
        /// </param>
        /// <param name="charLenHint">
        /// The hint for the buffer size. If not specified or if the value is less than 1, a default buffer size of 255 characters is used.
        /// </param>
        /// <returns>
        /// <c>true</c> if the string was successfully retrieved; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when both <paramref name="lpAppName"/> and <paramref name="lpKeyName"/> are non-<c>null</c>. Either one of these parameters must be <c>null</c>.
        /// </exception>
        /// <remarks>
        /// This method uses the native Windows function <c>GetPrivateProfileString</c> to retrieve a string from an INI file.
        /// If the retrieval is successful, the string is returned via the <paramref name="result"/> out parameter.
        /// </remarks>
        public static bool GetIniPrivateProfileString(
            [Optional] string? lpAppName,
            [Optional] string? lpKeyName,
            [Optional] string? lpDefault,
            string lpFileName,
            out string result,
            int charLenHint = -1)
        {
            if (lpAppName != null && lpKeyName != null)
            {
                throw new ArgumentException("Either lpAppName or lpKeyName must be <null>.");
            }

            int bufferSize = charLenHint > 0 ? charLenHint : 255;
            StringBuilder buffer = new StringBuilder(bufferSize);

            bool success = GetPrivateProfileString(lpAppName!, lpKeyName!, lpDefault!, buffer, (uint)buffer.Capacity, lpFileName);

            if (success && buffer.Length > 0)
            {
                result = buffer.ToString();
                return true;
            }

            result = string.Empty;
            return false;
        }

        /// <summary>
        /// Writes a string to the specified section of an initialization file.
        /// If the file does not exist, the function creates the file. 
        /// If <paramref name="lpKeyName"/> is <c>null</c>, the entire section, including all entries within the section, is deleted.
        /// If <paramref name="lpString"/> is <c>null</c>, the key specified by <paramref name="lpKeyName"/> is deleted.
        /// </summary>
        /// <param name="lpAppName">The name of the section to which the string will be written. This section name is typically enclosed in square brackets ('[]').</param>
        /// <param name="lpKeyName">The name of the key to be associated with a string. If this parameter is <c>null</c>, the entire section, including all entries within the section, is deleted.</param>
        /// <param name="lpString">The string to be written to the file. If this parameter is <c>null</c>, the key specified by <paramref name="lpKeyName"/> is deleted.</param>
        /// <param name="lpFileName">The name of the initialization file. If the file does not exist, the function creates the file.</param>
        /// <returns><c>true</c> if the function succeeds; otherwise, <c>false</c>. To get extended error information, call <see cref="Marshal.GetLastWin32Error"/>.</returns>
        /// <remarks>
        /// This function is typically used to modify `.ini` files. It creates or modifies the file if necessary. 
        /// Ensure that proper access permissions are granted to the file to avoid failure.
        /// This function is case-insensitive and ignores leading and trailing white spaces in the section and key names.
        /// </remarks>
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WritePrivateProfileString(
            string lpAppName,
            [Optional] string lpKeyName,
            [Optional] string lpString,
            string lpFileName);

        /// <summary>
        /// Writes a string to the specified section of an initialization (.ini) file.
        /// If the file does not exist, the function creates the file.
        /// If <paramref name="lpKeyName"/> is <c>null</c>, the entire section, including all entries within the section, is deleted.
        /// If <paramref name="lpString"/> is <c>null</c>, the key specified by <paramref name="lpKeyName"/> is deleted.
        /// </summary>
        /// <param name="lpAppName">The name of the section to which the string will be written. This section name is typically enclosed in square brackets ('[]').</param>
        /// <param name="lpKeyName">The name of the key to be associated with a string. If this parameter is <c>null</c>, the entire section is deleted.</param>
        /// <param name="lpString">The string to be written to the file. If this parameter is <c>null</c>, the key specified by <paramref name="lpKeyName"/> is deleted.</param>
        /// <param name="lpFileName">The name of the initialization (.ini) file. If the file does not exist, the function creates the file.</param>
        /// <returns>
        /// <c>true</c> if the string was successfully written to the file; otherwise, <c>false</c>. 
        /// To get extended error information when the method fails, call <see cref="Marshal.GetLastWin32Error"/>.
        /// </returns>
        /// <remarks>
        /// This method internally calls the Windows API <see cref="WritePrivateProfileString"/> to modify `.ini` files. 
        /// Make sure the file is not locked or in use by another process to avoid errors.
        /// </remarks>
        public static bool WriteIniPrivateProfileString(string lpAppName, [Optional] string? lpKeyName, [Optional] string? lpString, string lpFileName)
        {
            return WritePrivateProfileString(lpAppName, lpKeyName!, lpString!, lpFileName);
        }

        /// <summary>
        /// Retrieves information about the current system to an application running under WOW64. If the function is called from a 64-bit
        /// application, it is equivalent to the <c>GetSystemInfo</c> function.
        /// </summary>
        /// <param name="lpSystemInfo">A pointer to a <c>SYSTEM_INFO</c> structure that receives the information.</param>
        /// <returns>This function does not return a value.</returns>
        // void WINAPI GetNativeSystemInfo( _Out_ LPSYSTEM_INFO lpSystemInfo); https://msdn.microsoft.com/en-us/library/windows/desktop/ms724340(v=vs.85).aspx
        [DllImport("kernel32.dll", SetLastError = false, ExactSpelling = true)]
        public static extern void GetNativeSystemInfo(out SYSTEM_INFO lpSystemInfo);

        /// <summary>Retrieves the address of an exported function or variable from the specified dynamic-link library (DLL).</summary>
        /// <param name="hModule">
        /// <para>
        /// A handle to the DLL module that contains the function or variable. The <c>LoadLibrary</c>, <c>LoadLibraryEx</c>,
        /// <c>LoadPackagedLibrary</c>, or <c>GetModuleHandle</c> function returns this handle.
        /// </para>
        /// <para>
        /// The <c>GetProcAddress</c> function does not retrieve addresses from modules that were loaded using the
        /// <c>LOAD_LIBRARY_AS_DATAFILE</c> flag. For more information, see <c>LoadLibraryEx</c>.
        /// </para>
        /// </param>
        /// <param name="lpProcName">
        /// The function or variable name, or the function's ordinal value. If this parameter is an ordinal value, it must be in the
        /// low-order word; the high-order word must be zero.
        /// </param>
        /// <returns>
        /// <para>If the function succeeds, the return value is the address of the exported function or variable.</para>
        /// <para>If the function fails, the return value is NULL. To get extended error information, call <c>GetLastError</c>.</para>
        /// </returns>
        // FARPROC WINAPI GetProcAddress( _In_ HMODULE hModule, _In_ LPCSTR lpProcName); https://msdn.microsoft.com/en-us/library/windows/desktop/ms683212(v=vs.85).aspx
        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        public static extern IntPtr GetProcAddress(SafeLibraryHandle hModule, [MarshalAs(UnmanagedType.LPStr)] string lpProcName);

        /// <summary>
        /// Determines whether the specified process is running under WOW64; also returns additional machine process and architecture information.
        /// </summary>
        /// <param name="hProcess">
        /// A handle to the process. The handle must have the PROCESS_QUERY_INFORMATION or PROCESS_QUERY_LIMITED_INFORMATION access right.
        /// For more information, see Process Security and Access Rights.
        /// </param>
        /// <param name="pProcessMachine">
        /// On success, returns a pointer to an IMAGE_FILE_MACHINE_* value. The value will be IMAGE_FILE_MACHINE_UNKNOWN if the target
        /// process is not a WOW64 process; otherwise, it will identify the type of WoW process.
        /// </param>
        /// <param name="pNativeMachine">
        /// On success, returns a pointer to a possible IMAGE_FILE_MACHINE_* value identifying the native architecture of host system.
        /// </param>
        /// <returns>
        /// <para>If the function succeeds, the return value is a nonzero value.</para>
        /// <para>If the function fails, the return value is zero. To get extended error information, call <c>GetLastError</c>.</para>
        /// </returns>
        // BOOL WINAPI IsWow64Process( _In_ HANDLE hProcess, _Out_ USHORT *pProcessMachine, _Out_opt_ USHORT *pNativeMachine); https://msdn.microsoft.com/en-us/library/windows/desktop/mt804318(v=vs.85).aspx
        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWow64Process2([In] IntPtr hProcess, out IMAGE_FILE_MACHINE pProcessMachine, out IMAGE_FILE_MACHINE pNativeMachine);

        #endregion

        #region PInvoke: winsta.dll

        [DllImport("winsta.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WinStationQueryInformation(SafeWTSServer hServer, uint sessionId, int information, out WINSTATIONINFORMATIONW pBuffer, int bufferLength, out int returnedLength);

        #endregion

        #region PInvoke: wtsapi32.dll

        /// <summary>
        /// Retrieves a list of sessions on a Remote Desktop Session Host (RD Session Host) server.
        /// </summary>
        /// <param name="hServer">A handle to the RD Session Host server.
        /// You can use the <see cref="WTSOpenServer"/> function to retrieve a handle to a specific
        /// server, or <see cref="WTS_CURRENT_SERVER_HANDLE"/> to use the RD Session Host server that hosts your application.</param>
        /// <param name="Reserved">This parameter is reserved. It must be zero.</param>
        /// <param name="Version">The version of the enumeration request. This parameter must be 1.</param>
        /// <param name="ppSessionInfo">A pointer to <see cref="IEnumerable&lt;WTS_SESSION_INFO&gt;"/> structures that represent the retrieved
        /// sessions. Note, that returned object doesn't know overall count of sessions, and always return true for MoveNext, use it in pair
        /// with pCount parameter</param>
        /// <param name="pCount">A pointer to the number of WTS_SESSION_INFO structures returned in the ppSessionInfo parameter.</param>
        /// <returns>Returns zero if this function fails. If this function succeeds, a nonzero value is returned.</returns>
        [DllImport("wtsapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WTSEnumerateSessions(
            SafeWTSServer hServer,
            [Optional] uint Reserved,
            uint Version,
            out SafeWtsMemory ppSessionInfo,
            out uint pCount);

        /// <summary>
        /// Frees memory allocated by a Remote Desktop Services function.
        /// </summary>
        /// <param name="pMemory">A pointer to the memory to free.</param>
        [DllImport("wtsapi32.dll", SetLastError = false, CharSet = CharSet.Unicode)]
        public static extern void WTSFreeMemory(IntPtr pMemory);

        /// <summary>
        /// Closes an open handle to a Remote Desktop Session Host (RD Session Host) server.
        /// </summary>
        /// <param name="hServer">A handle to an RD Session Host server opened by a call to the <see cref="WTSOpenServer"/> or <see cref="WTSOpenServerEx"/> function.</param>
        [DllImport("wtsapi32.dll", SetLastError = false, CharSet = CharSet.Unicode)]
        public static extern void WTSCloseServer(IntPtr hServer);

        /// <summary>
        /// Obtains the access token of the logged-on user specified by the session ID.
        /// Returns a token with a SecurityIdentification level.
        /// </summary>
        /// <param name="sessionId">The session ID of the user to obtain the token for.</param>
        /// <param name="phToken">A pointer to a handle that receives the token.</param>
        /// <returns>If the function succeeds, the return value is nonzero. If the function fails, the return value is zero.</returns>
        [DllImport("wtsapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WTSQueryUserToken(uint sessionId, out SafeAccessToken phToken);

        /// <summary>
        /// Opens a handle to the specified Remote Desktop Session Host (RD Session Host) server.
        /// </summary>
        /// <param name="pServerName">A string that contains the NetBIOS name of the server.</param>
        /// <returns>If the function succeeds, the return value is a handle to the specified server.
        /// If the function fails, it returns an invalid handle. You can test the validity of the handle by using it in another function call.</returns>
        [DllImport("wtsapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr WTSOpenServer(string pServerName);

        /// <summary>
        /// Retrieves session information for the specified session on the specified Remote Desktop Session Host (RD Session Host) server.
        /// It can be used to query session information on local and remote RD Session Host servers.
        /// </summary>
        /// <param name="hServer">
        /// A handle to an RD Session Host server. Specify a handle opened by the WTSOpenServer function, or specify
        /// <c>WTS_CURRENT_SERVER_HANDLE</c> to indicate the RD Session Host server on which your application is running.
        /// </param>
        /// <param name="SessionId">
        /// <para>
        /// A Remote Desktop Services session identifier. To indicate the session in which the calling application is running (or the
        /// current session) specify <c>WTS_CURRENT_SESSION</c>. Only specify <c>WTS_CURRENT_SESSION</c> when obtaining session information
        /// on the local server. If <c>WTS_CURRENT_SESSION</c> is specified when querying session information on a remote server, the
        /// returned session information will be inconsistent. Do not use the returned data.
        /// </para>
        /// <para>
        /// You can use the GetWTSEnumerateSessions function to retrieve the identifiers of all sessions on a specified RD Session Host server.
        /// </para>
        /// <para>
        /// To query information for another user's session, you must have Query Information permission. For more information, see Remote
        /// Desktop Services Permissions. To modify permissions on a session, use the Remote Desktop Services Configuration administrative tool.
        /// </para>
        /// </param>
        /// <param name="WTSInfoClass">
        /// A value of the WTS_INFO_CLASS enumeration that indicates the type of session information to retrieve in a call to the
        /// <c>WTSQuerySessionInformation</c> function.
        /// </param>
        /// <param name="ppBuffer">
        /// A pointer to a variable that receives a pointer to the requested information. The format and contents of the data depend on the
        /// information class specified in the WTSInfoClass parameter. To free the returned buffer, call the WTSFreeMemory function.
        /// </param>
        /// <param name="pBytesReturned">A pointer to a variable that receives the size, in bytes, of the data returned in ppBuffer.</param>
        /// <returns>
        /// <para>If the function succeeds, the return value is a nonzero value.</para>
        /// <para>If the function fails, the return value is zero. To get extended error information, call GetLastError.</para>
        /// </returns>
        /// <remarks>
        /// <para>
        /// To retrieve the session ID for the current session when Remote Desktop Services is running, call
        /// <c>WTSQuerySessionInformation</c> and specify <c>WTS_CURRENT_SESSION</c> for the SessionId parameter and <c>WTSSessionId</c> for
        /// the WTSInfoClass parameter. The session ID will be returned in the ppBuffer parameter. If Remote Desktop Services is not
        /// running, calls to <c>WTSQuerySessionInformation</c> fail. In this situation, you can retrieve the current session ID by calling
        /// the ProcessIdToSessionId function.
        /// </para>
        /// <para>
        /// To determine whether your application is running on the physical console, you must specify <c>WTS_CURRENT_SESSION</c> for the
        /// SessionId parameter, and <c>WTSClientProtocolType</c> as the WTSInfoClass parameter. If ppBuffer is "0", the session is attached
        /// to the physical console.
        /// </para>
        /// </remarks>
        [DllImport("wtsapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WTSQuerySessionInformation(SafeWTSServer hServer, uint SessionId, WTS_INFO_CLASS WTSInfoClass,
            out SafeWtsMemory ppBuffer, out uint pBytesReturned);

        #endregion

        #region PInvoke: advapi32.dll

        /// <summary>
        /// Creates a new process and its primary thread. The new process runs in the security context of the specified token.
        /// </summary>
        /// <param name="hToken">A handle to the primary token that represents a user.</param>
        /// <param name="lpApplicationName">The name of the module to be executed.</param>
        /// <param name="lpCommandLine">The command line to be executed.</param>
        /// <param name="lpProcessAttributes">A pointer to a SECURITY_ATTRIBUTES structure that specifies a security descriptor for the new process object.</param>
        /// <param name="lpThreadAttributes">A pointer to a SECURITY_ATTRIBUTES structure that specifies a security descriptor for the new thread object.</param>
        /// <param name="bInheritHandles">If this parameter is TRUE, each inheritable handle in the calling process is inherited by the new process.</param>
        /// <param name="dwCreationFlags">The flags that control the priority class and the creation of the process.</param>
        /// <param name="lpEnvironment">A pointer to the environment block for the new process.</param>
        /// <param name="lpCurrentDirectory">The full path to the current directory for the process.</param>
        /// <param name="lpStartupInfo">A pointer to a STARTUPINFO structure.</param>
        /// <param name="lpProcessInformation">A pointer to a PROCESS_INFORMATION structure that receives identification information about the new process.</param>
        /// <returns>If the function succeeds, the return value is nonzero. If the function fails, the return value is zero.</returns>
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CreateProcessAsUser(
            SafeAccessToken hToken,
            [Optional] string lpApplicationName,
            [Optional] StringBuilder lpCommandLine,
            [Optional] SECURITY_ATTRIBUTES lpProcessAttributes,
            [Optional] SECURITY_ATTRIBUTES lpThreadAttributes,
            bool bInheritHandles,
            CREATE_PROCESS dwCreationFlags,
            [In, Optional] IntPtr lpEnvironment,
            [Optional] string lpCurrentDirectory,
            in STARTUPINFO lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation);

        /// <summary>
        /// Retrieves a specified type of information about an access token.
        /// </summary>
        /// <param name="TokenHandle">A handle to an access token from which information is retrieved.</param>
        /// <param name="TokenInformationClass">Specifies the type of information being retrieved.</param>
        /// <param name="TokenInformation">A pointer to a buffer the function fills with the requested information.</param>
        /// <param name="TokenInformationLength">Specifies the size, in bytes, of the buffer pointed to by the TokenInformation parameter.</param>
        /// <param name="ReturnLength">A pointer to a variable that receives the number of bytes needed for the buffer pointed to by the TokenInformation parameter.</param>
        /// <returns>If the function succeeds, the return value is true.</returns>
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetTokenInformation(
            SafeAccessToken TokenHandle,
            TOKEN_INFORMATION_CLASS TokenInformationClass,
            IntPtr TokenInformation,
            int TokenInformationLength,
            out int ReturnLength);

        /// <summary>
        /// Creates a new access token that duplicates an existing token.
        /// </summary>
        /// <param name="hExistingToken">A handle to an access token opened with TOKEN_DUPLICATE access.</param>
        /// <param name="dwDesiredAccess">
    	/// <para>
    	/// Specifies the requested access rights for the new token. The <c>DuplicateTokenEx</c> function compares the requested access
    	/// rights with the existing token's discretionary access control list (DACL) to determine which rights are granted or denied. To
    	/// request the same access rights as the existing token, specify zero. To request all access rights that are valid for the caller,
    	/// specify MAXIMUM_ALLOWED.
    	/// </para>
    	/// <para>For a list of access rights for access tokens, see Access Rights for Access-Token Objects.</para>
    	/// </param>
        /// <param name="lpTokenAttributes">
    	/// <para>
    	/// A pointer to a <c>SECURITY_ATTRIBUTES</c> structure that specifies a security descriptor for the new token and determines whether
    	/// child processes can inherit the token. If lpTokenAttributes is <c>NULL</c>, the token gets a default security descriptor and the
    	/// handle cannot be inherited. If the security descriptor contains a system access control list (SACL), the token gets
    	/// ACCESS_SYSTEM_SECURITY access right, even if it was not requested in dwDesiredAccess.
    	/// </para>
    	/// <para>
    	/// To set the owner in the security descriptor for the new token, the caller's process token must have the <c>SE_RESTORE_NAME</c>
    	/// privilege set.
    	/// </para>
    	/// </param>
        /// <param name="ImpersonationLevel">Specifies a SECURITY_IMPERSONATION_LEVEL enumerated type that supplies the impersonation level of the new token.</param>
        /// <param name="TokenType">Specifies a TOKEN_TYPE enumerated type that indicates whether the new token is a primary or impersonation token.</param>
        /// <param name="phNewToken">A pointer to a HANDLE variable that receives the new token.</param>
        /// <returns>If the function succeeds, the return value is true.</returns>
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DuplicateTokenEx(
            SafeAccessToken hExistingToken,
            TokenAccess dwDesiredAccess,
            [In, Optional] SECURITY_ATTRIBUTES lpTokenAttributes,
            SECURITY_IMPERSONATION_LEVEL ImpersonationLevel,
            TOKEN_TYPE TokenType,
            out SafeAccessToken phNewToken);

        /// <summary>
        /// Enables the server side of a named pipe to impersonate the client side.
        /// </summary>
        /// <param name="hNamedPipe">A handle to a named pipe.</param>
        /// <returns>If the function succeeds, the return value is nonzero.</returns>
        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool ImpersonateNamedPipeClient(SafePipeHandle hNamedPipe);

        /// <summary>
        /// Terminates the impersonation of a client application.
        /// </summary>
        /// <returns>If the function succeeds, the return value is nonzero.</returns>
        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool RevertToSelf();

        /// <summary>
        /// Retrieves the locally unique identifier (LUID) used on a specified system to locally represent the specified privilege name.
        /// </summary>
        /// <param name="lpSystemName">A pointer to a null-terminated string that specifies the name of the system on which the privilege name is retrieved.</param>
        /// <param name="lpName">A pointer to a null-terminated string that specifies the name of the privilege.</param>
        /// <param name="lpLuid">A pointer to a variable that receives the LUID by which the privilege is known on the system specified by the lpSystemName parameter.</param>
        /// <returns>If the function succeeds, the return value is nonzero.</returns>
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool LookupPrivilegeValue(
            string lpSystemName,
            string lpName,
            out LUID lpLuid);

        /// <summary>
        /// Enables or disables privileges in the specified access token.
        /// </summary>
        /// <param name="TokenHandle">A handle to the access token that contains the privileges to be modified.</param>
        /// <param name="DisableAllPrivileges">Specifies whether the function disables all of the token's privileges.</param>
        /// <param name="NewState">A pointer to a TOKEN_PRIVILEGES structure that specifies an array of privileges and their attributes.</param>
        /// <param name="BufferLength">Specifies the size, in bytes, of the buffer pointed to by the PreviousState parameter.</param>
        /// <param name="PreviousState">A pointer to a buffer that the function fills with a TOKEN_PRIVILEGES structure that contains the previous state of any privileges that the function modifies.</param>
        /// <param name="ReturnLength">A pointer to a variable that receives the required size, in bytes, of the buffer pointed to by the PreviousState parameter.</param>
        /// <returns>If the function succeeds, the return value is nonzero.</returns>
        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool AdjustTokenPrivileges(
            IntPtr TokenHandle,
            [MarshalAs(UnmanagedType.Bool)] bool DisableAllPrivileges,
            ref TOKEN_PRIVILEGES NewState,
            uint BufferLength,
            IntPtr PreviousState,
            IntPtr ReturnLength);

        /// <summary>
        /// Creates a new token that is a restricted version of an existing token.
        /// </summary>
        /// <param name="ExistingTokenHandle">A handle to an access token opened with TOKEN_DUPLICATE access.</param>
        /// <param name="Flags">A set of bit flags that specify options for the new token.</param>
        /// <param name="DisableSidCount">The number of entries in the SidsToDisable array.</param>
        /// <param name="SidsToDisable">An array of pointers to structures that identify the security identifiers (SIDs) to disable in the new token.</param>
        /// <param name="DeletePrivilegeCount">The number of entries in the PrivilegesToDelete array.</param>
        /// <param name="PrivilegesToDelete">An array of pointers to structures that identify the privileges to delete in the new token.</param>
        /// <param name="RestrictedSidCount">The number of entries in the SidsToRestrict array.</param>
        /// <param name="SidsToRestrict">An array of pointers to structures that identify the SIDs for which to apply access restrictions in the new token.</param>
        /// <param name="NewTokenHandle">A pointer to a variable that receives a handle to the new restricted token.</param>
        /// <returns>If the function succeeds, the return value is nonzero.</returns>
        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CreateRestrictedToken(
            IntPtr ExistingTokenHandle,
            uint Flags,
            uint DisableSidCount,
            IntPtr SidsToDisable,
            uint DeletePrivilegeCount,
            IntPtr PrivilegesToDelete,
            uint RestrictedSidCount,
            IntPtr SidsToRestrict,
            out SafeAccessToken NewTokenHandle);

        /// <summary>
        /// <para>Unloads the specified registry key and its subkeys from the registry.</para>
        /// <para>
        /// Applications that back up or restore system state including system files and registry hives should use the Volume Shadow Copy
        /// Service instead of the registry functions.
        /// </para>
        /// </summary>
        /// <param name="hKey">
        /// <para>
        /// A handle to the registry key to be unloaded. This parameter can be a handle returned by a call to RegConnectRegistry function or
        /// one of the following predefined handles:
        /// </para>
        /// <para><c>HKEY_LOCAL_MACHINE</c><c>HKEY_USERS</c></para>
        /// </param>
        /// <param name="lpSubKey">
        /// <para>
        /// The name of the subkey to be unloaded. The key referred to by the lpSubKey parameter must have been created by using the
        /// RegLoadKey function.
        /// </para>
        /// <para>Key names are not case sensitive.</para>
        /// <para>For more information, see Registry Element Size Limits.</para>
        /// </param>
        /// <returns>
        /// <para>If the function succeeds, the return value is true.</para>
        /// <para>
        /// If the function fails, the return value is a nonzero error code defined in Winerror.h. You can use the FormatMessage function
        /// with the FORMAT_MESSAGE_FROM_SYSTEM flag to get a generic description of the error.
        /// </para>
        /// </returns>
        /// <remarks>
        /// <para>
        /// This function removes a hive from the registry but does not modify the file containing the registry information. A hive is a
        /// discrete body of keys, subkeys, and values that is rooted at the top of the registry hierarchy.
        /// </para>
        /// <para>
        /// The calling process must have the SE_RESTORE_NAME and SE_BACKUP_NAME privileges on the computer in which the registry resides.
        /// For more information, see Running with Special Privileges.
        /// </para>
        /// </remarks>
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool RegUnLoadKey(HKEY hKey, [MarshalAs(UnmanagedType.LPWStr)] string lpSubKey);

        /// <summary>
        /// <para>
        /// Creates a subkey under <c>HKEY_USERS</c> or <c>HKEY_LOCAL_MACHINE</c> and loads the data from the specified registry hive into
        /// that subkey.
        /// </para>
        /// <para>
        /// Applications that back up or restore system state including system files and registry hives should use the Volume Shadow Copy
        /// Service instead of the registry functions.
        /// </para>
        /// </summary>
        /// <param name="hKey">
        /// <para>
        /// A handle to the key where the subkey will be created. This can be a handle returned by a call to RegConnectRegistry, or one of
        /// the following predefined handles:
        /// </para>
        /// <para>
        /// <c>HKEY_LOCAL_MACHINE</c><c>HKEY_USERS</c> This function always loads information at the top of the registry hierarchy. The
        /// <c>HKEY_CLASSES_ROOT</c> and <c>HKEY_CURRENT_USER</c> handle values cannot be specified for this parameter, because they
        /// represent subsets of the <c>HKEY_LOCAL_MACHINE</c> and <c>HKEY_USERS</c> handle values, respectively.
        /// </para>
        /// </param>
        /// <param name="lpSubKey">
        /// <para>
        /// The name of the key to be created under hKey. This subkey is where the registration information from the file will be loaded.
        /// </para>
        /// <para>Key names are not case sensitive.</para>
        /// <para>For more information, see Registry Element Size Limits.</para>
        /// </param>
        /// <param name="lpFile">
        /// <para>
        /// The name of the file containing the registry data. This file must be a local file that was created with the RegSaveKey function.
        /// If this file does not exist, a file is created with the specified name.
        /// </para>
        /// </param>
        /// <returns>
        /// <para>If the function succeeds, the return value is true.</para>
        /// <para>
        /// If the function fails, the return value is a nonzero error code defined in Winerror.h. You can use the FormatMessage function
        /// with the FORMAT_MESSAGE_FROM_SYSTEM flag to get a generic description of the error.
        /// </para>
        /// </returns>
        /// <remarks>
        /// <para>
        /// There are two registry hive file formats. Registry hives created on current operating systems typically cannot be loaded by
        /// earlier ones.
        /// </para>
        /// <para>If hKey is a handle returned by RegConnectRegistry, then the path specified in lpFile is relative to the remote computer.</para>
        /// <para>
        /// The calling process must have the SE_RESTORE_NAME and SE_BACKUP_NAME privileges on the computer in which the registry resides.
        /// For more information, see Running with Special Privileges. To load a hive without requiring these special privileges, use the
        /// RegLoadAppKey function.
        /// </para>
        /// </remarks>
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool RegLoadKey(HKEY hKey, [MarshalAs(UnmanagedType.LPWStr)] string lpSubKey, [MarshalAs(UnmanagedType.LPWStr)] string lpFile);

        /// <summary>
        /// <para>Establishes a connection to a predefined registry key on another computer.</para>
        /// </summary>
        /// <param name="lpMachineName">
        /// <para>The name of the remote computer. The string has the following form:</para>
        /// <para>\computername</para>
        /// <para>The caller must have access to the remote computer or the function fails.</para>
        /// <para>If this parameter is <c>NULL</c>, the local computer name is used.</para>
        /// </param>
        /// <param name="hKey">
        /// <para>A predefined registry handle. This parameter can be one of the following predefined keys on the remote computer.</para>
        /// <para><c>HKEY_LOCAL_MACHINE</c><c>HKEY_PERFORMANCE_DATA</c><c>HKEY_USERS</c></para>
        /// </param>
        /// <param name="phkResult">
        /// <para>A pointer to a variable that receives a key handle identifying the predefined handle on the remote computer.</para>
        /// </param>
        /// <returns>
        /// <para>If the function succeeds, the return value is true.</para>
        /// <para>
        /// If the function fails, the return value is a nonzero error code defined in Winerror.h. You can use the FormatMessage function
        /// with the FORMAT_MESSAGE_FROM_SYSTEM flag to get a generic description of the error.
        /// </para>
        /// </returns>
        /// <remarks>
        /// <para>
        /// <c>RegConnectRegistry</c> requires the Remote Registry service to be running on the remote computer. By default, this service is
        /// configured to be started manually. To configure the Remote Registry service to start automatically, run Services.msc and change
        /// the Startup Type of the service to Automatic.
        /// </para>
        /// <para><c>Windows Server 2003 and Windows XP/2000:</c> The Remote Registry service is configured to start automatically by default.</para>
        /// <para>When a handle returned by <c>RegConnectRegistry</c> is no longer needed, it should be closed by calling RegCloseKey.</para>
        /// <para>
        /// If the computer is joined to a workgroup and the "Force network logons using local accounts to authenticate as Guest" policy is
        /// enabled, the function fails. Note that this policy is enabled by default if the computer is joined to a workgroup.
        /// </para>
        /// <para>
        /// If the current user does not have proper access to the remote computer, the call to <c>RegConnectRegistry</c> fails. To connect
        /// to a remote registry, call LogonUser with LOGON32_LOGON_NEW_CREDENTIALS and ImpersonateLoggedOnUser before calling <c>RegConnectRegistry</c>.
        /// </para>
        /// <para>
        /// <c>Windows 2000:</c> One possible workaround is to establish a session to an administrative share such as IPC$ using a different
        /// set of credentials. To specify credentials other than those of the current user, use the WNetAddConnection2 function to connect
        /// to the share. When you have finished accessing the registry, cancel the connection.
        /// </para>
        /// <para>
        /// <c>Windows XP Home Edition:</c> You cannot use this function to connect to a remote computer running Windows XP Home Edition.
        /// This function does work with the name of the local computer even if it is running Windows XP Home Edition because this bypasses
        /// the authentication layer.
        /// </para>
        /// </remarks>
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool RegConnectRegistry([Optional, MarshalAs(UnmanagedType.LPWStr)] string lpMachineName, HKEY hKey, out SafeRegistryHandle phkResult);

        /// <summary>
        /// <para>Copies the specified registry key, along with its values and subkeys, to the specified destination key.</para>
        /// </summary>
        /// <param name="hKeySrc">
        /// <para>
        /// A handle to an open registry key. The key must have been opened with the KEY_READ access right. For more information, see
        /// Registry Key Security and Access Rights.
        /// </para>
        /// <para>This handle is returned by the RegCreateKeyEx or RegOpenKeyEx function, or it can be one of the predefined keys.</para>
        /// </param>
        /// <param name="lpSubKey">
        /// <para>
        /// The name of the key. This key must be a subkey of the key identified by the hKeySrc parameter. This parameter can also be <c>NULL</c>.
        /// </para>
        /// </param>
        /// <param name="hKeyDest">
        /// <para>A handle to the destination key. The calling process must have KEY_CREATE_SUB_KEY access to the key.</para>
        /// <para>This handle is returned by the RegCreateKeyEx or RegOpenKeyEx function, or it can be one of the predefined keys.</para>
        /// </param>
        /// <returns>
        /// <para>If the function succeeds, the return value is true.</para>
        /// <para>
        /// If the function fails, the return value is a nonzero error code defined in Winerror.h.
        /// </para>
        /// </returns>
        /// <remarks>
        /// <para>This function also copies the security descriptor for the key.</para>
        /// <para>
        /// To compile an application that uses this function, define _WIN32_WINNT as 0x0600 or later. For more information, see Using the
        /// Windows Headers.
        /// </para>
        /// </remarks>
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool RegCopyTree(HKEY hKeySrc, [Optional, MarshalAs(UnmanagedType.LPWStr)] string lpSubKey, HKEY hKeyDest);

        /// <summary>Closes a handle to the specified registry key.</summary>
        /// <param name="hKey">
        /// A handle to the open key to be closed. The handle must have been opened by the RegCreateKeyEx, RegCreateKeyTransacted,
        /// RegOpenKeyEx, RegOpenKeyTransacted, or RegConnectRegistry function.
        /// </param>
        /// <returns>
        /// If the function succeeds, the return value is ERROR_SUCCESS.
        /// <para>
        /// If the function fails, the return value is a nonzero error code defined in Winerror.h.
        /// </para>
        /// </returns>
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool RegCloseKey(HKEY hKey);

        #endregion

        #region PInvoke: userenv.dll

        /// <summary>
        /// Creates an environment block for the specified user.
        /// </summary>
        /// <param name="lpEnvironment">A pointer to a variable that receives a pointer to the new environment block.</param>
        /// <param name="hToken">A handle to the user's access token.</param>
        /// <param name="bInherit">A Boolean value that determines whether to inherit from the current process's environment.</param>
        /// <returns>If the function succeeds, the return value is true.</returns>
        [DllImport("userenv.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CreateEnvironmentBlock(out SafeEnvironmentBlock lpEnvironment, SafeHandle hToken, bool bInherit);

        /// <summary>
        /// Destroys an environment block created by the CreateEnvironmentBlock function.
        /// </summary>
        /// <param name="lpEnvironment">A pointer to the environment block to be destroyed.</param>
        /// <returns>If the function succeeds, the return value is true.</returns>
        [DllImport("userenv.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DestroyEnvironmentBlock(IntPtr lpEnvironment);

        #endregion

        #region PInvoke: ntdll.dll

        [DllImport("ntdll.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern NTSTATUS RtlGetVersion(out OSVERSIONINFOEX versionInfo);

        #endregion

        #region PInvoke: shell32.dll

        /// <summary>Retrieves information about an object in the file system, such as a file, folder, directory, or drive root.</summary>
        /// <param name="pszPath">
        /// A pointer to a null-terminated string of maximum length MAX_PATH that contains the path and file name. Both absolute and relative
        /// paths are valid.
        /// <para>
        /// If the uFlags parameter includes the SHGFI_PIDL flag, this parameter must be the address of an ITEMIDLIST (PIDL) structure that
        /// contains the list of item identifiers that uniquely identifies the file within the Shell's namespace. The PIDL must be a fully
        /// qualified PIDL. Relative PIDLs are not allowed.
        /// </para>
        /// <para>
        /// If the uFlags parameter includes the SHGFI_USEFILEATTRIBUTES flag, this parameter does not have to be a valid file name. The
        /// function will proceed as if the file exists with the specified name and with the file attributes passed in the dwFileAttributes
        /// parameter. This allows you to obtain information about a file type by passing just the extension for pszPath and passing
        /// FILE_ATTRIBUTE_NORMAL in dwFileAttributes.
        /// </para>
        /// <para>This string can use either short (the 8.3 form) or long file names.</para>
        /// </param>
        /// <param name="dwFileAttributes">
        /// A combination of one or more file attribute flags (FILE_ATTRIBUTE_ values as defined in Winnt.h). If uFlags does not include the
        /// SHGFI_USEFILEATTRIBUTES flag, this parameter is ignored.
        /// </param>
        /// <param name="psfi">Pointer to a SHFILEINFO structure to receive the file information.</param>
        /// <param name="cbFileInfo">The size, in bytes, of the SHFILEINFO structure pointed to by the psfi parameter.</param>
        /// <param name="uFlags">The flags that specify the file information to retrieve.</param>
        /// <returns>
        /// Returns a value whose meaning depends on the uFlags parameter.
        /// <para>If uFlags does not contain SHGFI_EXETYPE or SHGFI_SYSICONINDEX, the return value is nonzero if successful, or zero otherwise.</para>
        /// <para>
        /// If uFlags contains the SHGFI_EXETYPE flag, the return value specifies the type of the executable file. It will be one of the
        /// following values.
        /// </para>
        /// </returns>
        [DllImport("shell32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr SHGetFileInfo(string pszPath, FileAttributes dwFileAttributes, ref SHFILEINFO psfi, int cbFileInfo, SHGFI uFlags);

        /// <summary>
        /// Retrieves the current notification state of the user.
        /// </summary>
        /// <param name="pquns">An output parameter that receives the user notification state.</param>
        /// <returns>An integer representing the status of the query.</returns>
        [DllImport("shell32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern int SHQueryUserNotificationState(out QUERY_USER_NOTIFICATION_STATE pquns);

        /// <summary>
        /// Specifies a unique application-defined Application User Model ID (AppUserModelID) that identifies the current process to the
        /// taskbar. This identifier allows an application to group its associated processes and windows under a single taskbar button.
        /// </summary>
        /// <param name="AppID">Pointer to the AppUserModelID to assign to the current process.</param>
        [DllImport("shell32.dll", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern int SetCurrentProcessExplicitAppUserModelID([MarshalAs(UnmanagedType.LPWStr)] string AppID);

        #endregion

        #region PInvoke: wintrust.dll

        [DllImport("wintrust.dll", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern int WinVerifyTrust(IntPtr hwnd, [MarshalAs(UnmanagedType.LPStruct)] Guid pgActionID, ref WinTrustData pWVTData);

        [DllImport("wintrust.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool CryptCATAdminAcquireContext(ref SafeCatAdminHandle phCatAdmin, ref Guid pgSubsystem, uint dwFlags);

        [DllImport("wintrust.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool CryptCATAdminReleaseContext(IntPtr hCatAdmin, uint dwFlags);

        [DllImport("wintrust.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool CryptCATAdminCalcHashFromFileHandle(SafeFileHandle hFile, ref uint pcbHash, [Out] byte[] pbHash, uint dwFlags);

        [DllImport("wintrust.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr CryptCATAdminEnumCatalogFromHash(SafeCatAdminHandle hCatAdmin, [MarshalAs(UnmanagedType.LPArray)] byte[] pbHash, uint cbHash, uint dwFlags, IntPtr phPrevCatInfo);

        [DllImport("wintrust.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool CryptCATCatalogInfoFromContext(IntPtr hCatInfo, IntPtr psCatInfo, uint dwFlags);

        #endregion

        #region PInvoke: shcore.dll

        /// <summary>
        /// <para>
        /// It is recommended that you set the process-default DPI awareness via application manifest. See Setting the default DPI awareness
        /// for a process for more information. Setting the process-default DPI awareness via API call can lead to unexpected application behavior.
        /// </para>
        /// <para>
        /// Sets the process-default DPI awareness level. This is equivalent to calling SetProcessDpiAwarenessContext with the corresponding
        /// DPI_AWARENESS_CONTEXT value.
        /// </para>
        /// </summary>
        /// <param name="value">The DPI awareness value to set. Possible values are from the PROCESS_DPI_AWARENESSenumeration.</param>
        /// <returns>
        /// <para>This function returns one of the following values.</para>
        /// <list type="table">
        /// <listheader>
        /// <term>Return code</term>
        /// <term>Description</term>
        /// </listheader>
        /// <item>
        /// <term>S_OK</term>
        /// <term>The DPI awareness for the app was set successfully.</term>
        /// </item>
        /// <item>
        /// <term>E_INVALIDARG</term>
        /// <term>The value passed in is not valid.</term>
        /// </item>
        /// <item>
        /// <term>E_ACCESSDENIED</term>
        /// <term>The DPI awareness is already set, either by calling this API previously or through the application (.exe) manifest.</term>
        /// </item>
        /// </list>
        /// </returns>
        /// <remarks>
        /// <para>
        /// It is recommended that you set the process-default DPI awareness via application manifest. See Setting the default DPI awareness
        /// for a process for more information. Setting the process-default DPI awareness via API call can lead to unexpected application behavior.
        /// </para>
        /// <para>
        /// Previous versions of Windows only had one DPI awareness value for the entire application. For those applications, the
        /// recommendation was to set the DPI awareness value in the manifest as described in PROCESS_DPI_AWARENESS. Under that
        /// recommendation, you were not supposed to use <c>SetProcessDpiAwareness</c> to update the DPI awareness. In fact, future calls to
        /// this API would fail after the DPI awareness was set once. Now that DPI awareness is tied to a thread rather than an application,
        /// you can use this method to update the DPI awareness. However, consider using SetThreadDpiAwarenessContext instead.
        /// </para>
        /// <para><c>Important</c>
        /// <para></para>
        /// For older applications, it is strongly recommended to not use <c>SetProcessDpiAwareness</c> to set the DPI awareness for your
        /// application. Instead, you should declare the DPI awareness for your application in the application manifest. See
        /// PROCESS_DPI_AWARENESS for more information about the DPI awareness values and how to set them in the manifest.
        /// </para>
        /// <para>
        /// You must call this API before you call any APIs that depend on the dpi awareness. This is part of the reason why it is
        /// recommended to use the application manifest rather than the <c>SetProcessDpiAwareness</c> API. Once API awareness is set for an
        /// app, any future calls to this API will fail. This is true regardless of whether you set the DPI awareness in the manifest or by
        /// using this API.
        /// </para>
        /// <para>If the DPI awareness level is not set, the default value is <c>PROCESS_DPI_UNAWARE</c>.</para>
        /// </remarks>
        [DllImport("shcore.dll", SetLastError = false, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetProcessDpiAwareness(PROCESS_DPI_AWARENESS value);

        #endregion

        #region PInvoke: gdi32.dll

        /// <summary>
        /// Retrieves device-specific information for the specified device context.
        /// </summary>
        /// <param name="hDC">A handle to the device context.</param>
        /// <param name="nIndex">The index of the capability to retrieve. This can be one of the values from the <see cref="DeviceCap"/> enumeration.</param>
        /// <returns>The value of the requested device capability.</returns>
        [DllImport("gdi32.dll", SetLastError = false, CharSet = CharSet.Unicode)]
        public static extern int GetDeviceCaps(IntPtr hDC, int nIndex);

        #endregion

        #region PInvoke: oleaut32.dll

        /// <summary>
        /// Retrieves the current thread's error information and returns it as a pointer to an IErrorInfo interface.
        /// </summary>
        /// <param name="dwReserved">
        /// Reserved. This parameter must be zero.
        /// </param>
        /// <param name="errorInfoHandle">
        /// When this method returns, contains a handle to the error information object associated with the current thread.
        /// </param>
        /// <returns>
        /// Returns S_OK if successful; otherwise, an HRESULT error code.
        /// </returns>
        /// <remarks>
        /// This method is a platform invocation of the 'GetErrorInfo' function from the 'oleaut32.dll'.
        /// It retrieves the IErrorInfo interface for the current thread.
        /// </remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/api/oleauto/nf-oleauto-geterrorinfo">
        /// MSDN documentation for GetErrorInfo.
        /// </seealso>
        [DllImport("oleaut32.dll", SetLastError = false, ExactSpelling = true)]
        public static extern int GetErrorInfo([Optional] uint dwReserved, out SafeErrorInfoHandle errorInfoHandle);

        #endregion

        #region PInvoke: ole32.dll

        /// <summary>
        /// Initializes the COM library for use by the calling thread, sets the thread's concurrency model, and creates a new apartment for
        /// the thread if one is required.
        /// <para>
        /// You should call Windows::Foundation::Initialize to initialize the thread instead of CoInitializeEx if you want to use the
        /// Windows Runtime APIs or if you want to use both COM and Windows Runtime components. Windows::Foundation::Initialize is
        /// sufficient to use for COM components.
        /// </para>
        /// </summary>
        /// <param name="pvReserved">This parameter is reserved and must be NULL.</param>
        /// <param name="coInit">
        /// The concurrency model and initialization options for the thread. Values for this parameter are taken from the COINIT
        /// enumeration. Any combination of values from COINIT can be used, except that the COINIT_APARTMENTTHREADED and
        /// COINIT_MULTITHREADED flags cannot both be set. The default is COINIT_MULTITHREADED.
        /// </param>
        /// <returns>
        /// <list type="table">
        /// <listheader>
        /// <term>Return code</term>
        /// <term>Description</term>
        /// </listheader>
        /// <item>
        /// <term>S_OK</term>
        /// <defintion>The COM library was initialized successfully on this thread.</defintion>
        /// </item>
        /// <item>
        /// <term>S_FALSE</term>
        /// <defintion>The COM library is already initialized on this thread.</defintion>
        /// </item>
        /// <item>
        /// <term>RPC_E_CHANGED_MODE</term>
        /// <defintion>A previous call to CoInitializeEx specified the concurrency model for this thread as multithreaded apartment (MTA).
        /// This could also indicate that a change from neutral-threaded apartment to single-threaded apartment has occurred.</defintion>
        /// </item>
        /// </list>
        /// </returns>
        [DllImport("ole32.dll", ExactSpelling = true, CallingConvention = CallingConvention.StdCall, SetLastError = false)]
        public static extern int CoInitializeEx([Optional] IntPtr pvReserved, COINIT coInit = COINIT.COINIT_MULTITHREADED);

        /// <summary>
        /// Closes the COM library on the current thread, unloads all DLLs loaded by the thread, frees any other resources that the thread
        /// maintains, and forces all RPC connections on the thread to close.
        /// </summary>
        [DllImport("ole32.dll", ExactSpelling = true, CallingConvention = CallingConvention.StdCall, SetLastError = false)]
        public static extern void CoUninitialize();

        /// <summary>
        /// The PropVariantClear function frees all elements that can be freed in a given PROPVARIANT structure. For complex elements with
        /// known element pointers, the underlying elements are freed prior to freeing the containing element.
        /// </summary>
        /// <param name="pvar">
        /// A pointer to an initialized PROPVARIANT structure for which any deallocatable elements are to be freed. On return, all zeroes are
        /// written to the PROPVARIANT structure.
        /// </param>
        /// <returns>
        /// <list type="definition">
        /// <item>
        /// <term>S_OK</term>
        /// <definition>The VT types are recognized and all items that can be freed have been freed.</definition>
        /// </item>
        /// <item>
        /// <term>STG_E_INVALID_PARAMETER</term>
        /// <definition>The variant has an unknown VT type.</definition>
        /// </item>
        /// </list>
        /// </returns>
        [SecurityCritical, SuppressUnmanagedCodeSecurity]
        [DllImport("ole32.dll", ExactSpelling = true, CallingConvention = CallingConvention.StdCall, SetLastError = false)]
        public static extern int PropVariantClear([In, Out] PropVariant pvar);

        /// <summary>The PropVariantCopy function copies the contents of one PROPVARIANT structure to another.</summary>
        /// <param name="pDst">Pointer to an uninitialized PROPVARIANT structure that receives the copy.</param>
        /// <param name="pSrc">Pointer to the PROPVARIANT structure to be copied.</param>
        /// <returns>
        /// <list type="definition">
        /// <item>
        /// <term>S_OK</term>
        /// <definition>The VT types are recognized and all items that can be freed have been freed.</definition>
        /// </item>
        /// <item>
        /// <term>STG_E_INVALID_PARAMETER</term>
        /// <definition>The variant has an unknown VT type.</definition>
        /// </item>
        /// </list>
        /// </returns>
        [DllImport("ole32.dll", ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern int PropVariantCopy([In, Out] PropVariant pDst, [In] PropVariant pSrc);

        #endregion

        #region PInvoke: netapi32.dll

        /// <summary>The <c>NetUserGetLocalGroups</c> function retrieves a list of local groups to which a specified user belongs.</summary>
        /// <param name="servername">
        /// A pointer to a constant string that specifies the DNS or NetBIOS name of the remote server on which the function is to execute.
        /// If this parameter is <c>NULL</c>, the local computer is used.
        /// </param>
        /// <param name="username">
        /// A pointer to a constant string that specifies the name of the user for which to return local group membership information. If the
        /// string is of the form DomainName&lt;i&gt;UserName the user name is expected to be found on that domain. If the string is of the
        /// form UserName, the user name is expected to be found on the server specified by the servername parameter. For more information,
        /// see the Remarks section.
        /// </param>
        /// <param name="level">
        /// <para>The information level of the data. This parameter can be the following value.</para>
        /// <list type="table">
        /// <listheader>
        /// <term>Value</term>
        /// <term>Meaning</term>
        /// </listheader>
        /// <item>
        /// <term>0</term>
        /// <term>
        /// Return the names of the local groups to which the user belongs. The bufptr parameter points to an array of
        /// LOCALGROUP_USERS_INFO_0 structures.
        /// </term>
        /// </item>
        /// </list>
        /// </param>
        /// <param name="flags">
        /// A bitmask of flags that affect the operation. Currently, only the value defined is <c>LG_INCLUDE_INDIRECT</c>. If this bit is
        /// set, the function also returns the names of the local groups in which the user is indirectly a member (that is, the user has
        /// membership in a global group that is itself a member of one or more local groups).
        /// </param>
        /// <param name="bufptr">
        /// A pointer to the buffer that receives the data. The format of this data depends on the value of the level parameter. This buffer
        /// is allocated by the system and must be freed using the NetApiBufferFree function. Note that you must free the buffer even if the
        /// function fails with <c>ERROR_MORE_DATA</c>.
        /// </param>
        /// <param name="prefmaxlen">
        /// The preferred maximum length, in bytes, of the returned data. If <c>MAX_PREFERRED_LENGTH</c> is specified in this parameter, the
        /// function allocates the amount of memory required for the data. If another value is specified in this parameter, it can restrict
        /// the number of bytes that the function returns. If the buffer size is insufficient to hold all entries, the function returns
        /// <c>ERROR_MORE_DATA</c>. For more information, see Network Management Function Buffers and Network Management Function Buffer Lengths.
        /// </param>
        /// <param name="entriesread">A pointer to a value that receives the count of elements actually enumerated.</param>
        /// <param name="totalentries">A pointer to a value that receives the total number of entries that could have been enumerated.</param>
        /// <returns>
        /// <para>If the function succeeds, the return value is NERR_Success.</para>
        /// <para>If the function fails, the return value can be one of the following error codes.</para>
        /// <list type="table">
        /// <listheader>
        /// <term>Return code</term>
        /// <term>Description</term>
        /// </listheader>
        /// <item>
        /// <term>ERROR_ACCESS_DENIED</term>
        /// <term>
        /// The user does not have access rights to the requested information. This error is also returned if the servername parameter has a
        /// trailing blank.
        /// </term>
        /// </item>
        /// <item>
        /// <term>ERROR_INVALID_LEVEL</term>
        /// <term>The system call level is not correct. This error is returned if the level parameter was not specified as 0.</term>
        /// </item>
        /// <item>
        /// <term>ERROR_INVALID_PARAMETER</term>
        /// <term>A parameter is incorrect. This error is returned if the flags parameter contains a value other than LG_INCLUDE_INDIRECT.</term>
        /// </item>
        /// <item>
        /// <term>ERROR_MORE_DATA</term>
        /// <term>More entries are available. Specify a large enough buffer to receive all entries.</term>
        /// </item>
        /// <item>
        /// <term>ERROR_NOT_ENOUGH_MEMORY</term>
        /// <term>Insufficient memory was available to complete the operation.</term>
        /// </item>
        /// <item>
        /// <term>NERR_DCNotFound</term>
        /// <term>The domain controller could not be found.</term>
        /// </item>
        /// <item>
        /// <term>NERR_UserNotFound</term>
        /// <term>The user could not be found. This error is returned if the username could not be found.</term>
        /// </item>
        /// <item>
        /// <term>RPC_S_SERVER_UNAVAILABLE</term>
        /// <term>The RPC server is unavailable. This error is returned if the servername parameter could not be found.</term>
        /// </item>
        /// </list>
        /// </returns>
        /// <remarks>
        /// <para>
        /// If you are programming for Active Directory, you may be able to call certain Active Directory Service Interface (ADSI) methods to
        /// achieve the same functionality you can achieve by calling the network management user functions. For more information, see
        /// IADsUser and IADsComputer.
        /// </para>
        /// <para>
        /// If you call this function on a domain controller that is running Active Directory, access is allowed or denied based on the
        /// access control list (ACL) for the securable object. The default ACL permits all authenticated users and members of the
        /// "Pre-Windows 2000 compatible access" group to view the information. If you call this function on a member server or workstation,
        /// all authenticated users can view the information. For information about anonymous access and restricting anonymous access on
        /// these platforms, see Security Requirements for the Network Management Functions. For more information on ACLs, ACEs, and access
        /// tokens, see Access Control Model.
        /// </para>
        /// <para>
        /// The security descriptor of the Domain object is used to perform the access check for this function. The caller must have Read
        /// Property permission on the Domain object.
        /// </para>
        /// <para>To retrieve a list of global groups to which a specified user belongs, you can call the NetUserGetGroups function.</para>
        /// <para>
        /// User account names are limited to 20 characters and group names are limited to 256 characters. In addition, account names cannot
        /// be terminated by a period and they cannot include commas or any of the following printable characters: ", /, , [, ], :, |, &lt;,
        /// &gt;, +, =, ;, ?, *. Names also cannot include characters in the range 1-31, which are non-printable.
        /// </para>
        /// <para>Examples</para>
        /// <para>
        /// The following code sample demonstrates how to retrieve a list of the local groups to which a user belongs with a call to the
        /// <c>NetUserGetLocalGroups</c> function. The sample calls <c>NetUserGetLocalGroups</c>, specifying information level 0
        /// (LOCALGROUP_USERS_INFO_0). The sample loops through the entries and prints the name of each local group in which the user has
        /// membership. If all available entries are not enumerated, it also prints the number of entries actually enumerated and the total
        /// number of entries available. Finally, the code sample frees the memory allocated for the information buffer.
        /// </para>
        /// </remarks>
        // https://docs.microsoft.com/en-us/windows/desktop/api/lmaccess/nf-lmaccess-netusergetlocalgroups NET_API_STATUS NET_API_FUNCTION
        // NetUserGetLocalGroups( LPCWSTR servername, LPCWSTR username, DWORD level, DWORD flags, LPBYTE *bufptr, DWORD prefmaxlen, LPDWORD
        // entriesread, LPDWORD totalentries );
        [DllImport("netapi32.dll", SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]
        public static extern int NetUserGetLocalGroups(string servername, string username, uint level, uint flags, out IntPtr bufptr, uint prefmaxlen, out uint entriesread, out uint totalentries);

        /// <summary>
        /// The <c>NetLocalGroupGetMembers</c> function retrieves a list of the members of a particular local group in the security database,
        /// which is the security accounts manager (SAM) database or, in the case of domain controllers, the Active Directory. Local group
        /// members can be users or global groups.
        /// </summary>
        /// <param name="servername">
        /// Pointer to a constant string that specifies the DNS or NetBIOS name of the remote server on which the function is to execute. If
        /// this parameter is <c>NULL</c>, the local computer is used.
        /// </param>
        /// <param name="localgroupname">
        /// Pointer to a constant string that specifies the name of the local group whose members are to be listed. For more information, see
        /// the following Remarks section.
        /// </param>
        /// <param name="level">
        /// <para>Specifies the information level of the data. This parameter can be one of the following values.</para>
        /// <list type="table">
        /// <listheader>
        /// <term>Value</term>
        /// <term>Meaning</term>
        /// </listheader>
        /// <item>
        /// <term>0</term>
        /// <term>
        /// Return the security identifier (SID) associated with the local group member. The bufptr parameter points to an array of
        /// LOCALGROUP_MEMBERS_INFO_0 structures.
        /// </term>
        /// </item>
        /// <item>
        /// <term>1</term>
        /// <term>
        /// Return the SID and account information associated with the local group member. The bufptr parameter points to an array of
        /// LOCALGROUP_MEMBERS_INFO_1 structures.
        /// </term>
        /// </item>
        /// <item>
        /// <term>2</term>
        /// <term>
        /// Return the SID, account information, and the domain name associated with the local group member. The bufptr parameter points to
        /// an array of LOCALGROUP_MEMBERS_INFO_2 structures.
        /// </term>
        /// </item>
        /// <item>
        /// <term>3</term>
        /// <term>
        /// Return the account and domain names of the local group member. The bufptr parameter points to an array of
        /// LOCALGROUP_MEMBERS_INFO_3 structures.
        /// </term>
        /// </item>
        /// </list>
        /// </param>
        /// <param name="bufptr">
        /// Pointer to the address that receives the return information structure. The format of this data depends on the value of the level
        /// parameter. This buffer is allocated by the system and must be freed using the NetApiBufferFree function. Note that you must free
        /// the buffer even if the function fails with ERROR_MORE_DATA.
        /// </param>
        /// <param name="prefmaxlen">
        /// Specifies the preferred maximum length of returned data, in bytes. If you specify MAX_PREFERRED_LENGTH, the function allocates
        /// the amount of memory required for the data. If you specify another value in this parameter, it can restrict the number of bytes
        /// that the function returns. If the buffer size is insufficient to hold all entries, the function returns ERROR_MORE_DATA. For more
        /// information, see Network Management Function Buffers and Network Management Function Buffer Lengths.
        /// </param>
        /// <param name="entriesread">Pointer to a value that receives the count of elements actually enumerated.</param>
        /// <param name="totalentries">
        /// Pointer to a value that receives the total number of entries that could have been enumerated from the current resume position.
        /// </param>
        /// <param name="resumehandle">
        /// Pointer to a value that contains a resume handle which is used to continue an existing group member search. The handle should be
        /// zero on the first call and left unchanged for subsequent calls. If this parameter is <c>NULL</c>, then no resume handle is stored.
        /// </param>
        /// <returns>
        /// <para>If the function succeeds, the return value is NERR_Success.</para>
        /// <para>If the function fails, the return value can be one of the following error codes.</para>
        /// <list type="table">
        /// <listheader>
        /// <term>Return code</term>
        /// <term>Description</term>
        /// </listheader>
        /// <item>
        /// <term>ERROR_ACCESS_DENIED</term>
        /// <term>The user does not have access to the requested information.</term>
        /// </item>
        /// <item>
        /// <term>NERR_InvalidComputer</term>
        /// <term>The computer name is invalid.</term>
        /// </item>
        /// <item>
        /// <term>ERROR_MORE_DATA</term>
        /// <term>More entries are available. Specify a large enough buffer to receive all entries.</term>
        /// </item>
        /// <item>
        /// <term>ERROR_NO_SUCH_ALIAS</term>
        /// <term>The specified local group does not exist.</term>
        /// </item>
        /// </list>
        /// </returns>
        /// <remarks>
        /// <para>
        /// If you call this function on a domain controller that is running Active Directory, access is allowed or denied based on the
        /// access control list (ACL) for the securable object. The default ACL permits all authenticated users and members of the
        /// "Pre-Windows 2000 compatible access" group to view the information. If you call this function on a member server or workstation,
        /// all authenticated users can view the information. For information about anonymous access and restricting anonymous access on
        /// these platforms, see Security Requirements for the Network Management Functions. For more information on ACLs, ACEs, and access
        /// tokens, see Access Control Model.
        /// </para>
        /// <para>The security descriptor of the LocalGroup object is used to perform the access check for this function.</para>
        /// <para>
        /// User account names are limited to 20 characters and group names are limited to 256 characters. In addition, account names cannot
        /// be terminated by a period and they cannot include commas or any of the following printable characters: ", /, , [, ], :, |, &lt;,
        /// &gt;, +, =, ;, ?, *. Names also cannot include characters in the range 1-31, which are non-printable.
        /// </para>
        /// <para>
        /// If you are programming for Active Directory, you may be able to call certain Active Directory Service Interface (ADSI) methods to
        /// achieve the same functionality you can achieve by calling the network management local group functions. For more information, see IADsGroup.
        /// </para>
        /// <para>
        /// If this function returns <c>ERROR_MORE_DATA</c>, then it must be repeatedly called until <c>ERROR_SUCCESS</c> or
        /// <c>NERR_success</c> is returned. Failure to do so can result in an RPC connection leak.
        /// </para>
        /// </remarks>
        [DllImport("netapi32.dll", SetLastError = false, ExactSpelling = true, CharSet = CharSet.Unicode)]
        public static extern int NetLocalGroupGetMembers(string servername, string localgroupname, uint level, out IntPtr bufptr, uint prefmaxlen, out uint entriesread, out uint totalentries, ref IntPtr resumehandle);

        [DllImport("netapi32.dll")]
        public static extern int NetApiBufferFree(IntPtr Buffer);

        #endregion
    }
}
