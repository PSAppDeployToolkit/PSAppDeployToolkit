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
        /// Enables the current process to be DPI-aware.
        /// </summary>
        /// <returns>True if the operation was successful; otherwise, false.</returns>
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetProcessDPIAware();

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
        [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
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
        public static extern bool GetProductInfo(uint dwOSMajorVersion, uint dwOSMinorVersion, uint dwSpMajorVersion, uint dwSpMinorVersion, out PRODUCT_TYPE pdwReturnedProductType);

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
        internal static extern SafePipeHandle CreateNamedPipe(
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
        internal static extern bool ConnectNamedPipe(SafePipeHandle hNamedPipe, IntPtr lpOverlapped);

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
        /// <param name="hFile">Reserved; must be <see cref="SafeFileHandle.InvalidHandle"/> or <see cref="IntPtr.Zero"/>.</param>
        /// <param name="dwFlags">The action to be taken when loading the module. This parameter can include one or more of the <see cref="LoadLibraryExFlags"/>.</param>
        /// <returns>
        /// If the function succeeds, the return value is a handle to the loaded module. If the function fails, the return value is <see cref="SafeLibraryHandle.InvalidHandle"/>.
        /// To get extended error information, call <see cref="Marshal.GetLastWin32Error"/>.
        /// </returns>
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern SafeLibraryHandle LoadLibraryEx(
            [MarshalAs(UnmanagedType.LPTStr)] string lpLibFileName,
            SafeFileHandle hFile,
            LoadLibraryExFlags dwFlags);

        /// <summary>
        /// Frees the loaded dynamic-link library (DLL) module and, if necessary, decrements its reference count.
        /// When the reference count reaches zero, the module is unloaded from the address space of the calling process.
        /// </summary>
        /// <param name="hModule">A handle to the loaded DLL module. The <see cref="LoadLibraryEx"/> function returns this handle.</param>
        /// <returns>
        /// If the function succeeds, the return value is <c>true</c>. If the function fails, the return value is <c>false</c>. To get extended error information, call <see cref="Marshal.GetLastWin32Error"/>.
        /// </returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool FreeLibrary(IntPtr hModule);

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
        public static extern int SHQueryUserNotificationState(out UserNotificationState pquns);

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
        /// Retrieves the DPI for a given monitor.
        /// </summary>
        /// <param name="hMonitor">Handle to the monitor.</param>
        /// <param name="dpiType">The DPI type to retrieve.</param>
        /// <param name="dpiX">The horizontal DPI.</param>
        /// <param name="dpiY">The vertical DPI.</param>
        /// <returns>The status of the operation.</returns>
        [DllImport("shcore.dll", SetLastError = false, CharSet = CharSet.Unicode)]
        public static extern int GetDpiForMonitor(IntPtr hMonitor, MONITOR_DPI_TYPE dpiType, out uint dpiX, out uint dpiY);

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

        #region PInvoke: name.dll



        #endregion
    }
}
