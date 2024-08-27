using System;
using System.IO;
using System.Text;
using System.Security;
using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;

/// Native Method Declarations from:
/// https://github.com/dahall/Vanara

namespace PSADT.PInvoke
{
    /// <summary>
    /// Contains native method declarations for Win32 API calls.
    /// </summary>
    internal static partial class NativeMethods
    {
        #region Fields: advapi32.dll

        /// <summary>
        /// Error code indicating that the specified logon session does not exist.
        /// </summary>
        public const int ERROR_NO_SUCH_LOGON_SESSION = 1312;

        /// <summary>
        /// Error code indicating that the specified item was not found.
        /// </summary>
        public const int ERROR_NOT_FOUND = 1168;

        #endregion

        #region Fields: shlwapi.dll

        public const int MAX_PATH = 260;

        #endregion

        #region Fields: kernel32.dll

        public const uint ATTACH_PARENT_PROCESS = 0xFFFFFFFF;

        #endregion

        #region PInvoke: user32.dll

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern int GetSystemMetrics(SystemMetric nIndex);

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
        public static extern bool PathFindOnPath([In, Out] StringBuilder pszFile, [In, Optional] string[]? ppszOtherDirs);

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
        public static extern bool CreatePipe(out SafeFileHandle hReadPipe, out SafeFileHandle hWritePipe, [In, Optional] SECURITY_ATTRIBUTES? lpPipeAttributes, [In, Optional] uint nSize);

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
        [DllImport("shell32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr SHGetFileInfo(string pszPath, FileAttributes dwFileAttributes, ref SHFILEINFO psfi, int cbFileInfo, SHGFI uFlags);

        #endregion
    }
}
