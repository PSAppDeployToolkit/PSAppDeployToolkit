using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using PSADT.SafeHandles;
using PSADT.Utilities;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Security;
using Windows.Win32.Security.Authentication.Identity;
using Windows.Win32.Security.Authorization;
using Windows.Win32.System.Registry;
using Windows.Win32.System.Services;
using Windows.Win32.System.Threading;

namespace PSADT.LibraryInterfaces
{
    /// <summary>
    /// Provides managed wrappers for selected Windows advanced API (AdvApi32.dll) functions related to security,
    /// registry, process, and service control operations.
    /// </summary>
    /// <remarks>This static class exposes a set of internal methods that facilitate interaction with
    /// low-level Windows security and system management APIs, such as registry access, token manipulation, process
    /// creation, service control, and authorization. Each method enforces error handling by throwing exceptions for
    /// failure scenarios, and callers are responsible for managing the lifetime of any returned handles or resources as
    /// documented per method. The class is intended for internal use within the assembly and is not designed for direct
    /// use by external consumers.</remarks>
    internal static class AdvApi32
    {
        /// <summary>
        /// Opens the specified registry key with desired access rights and returns a handle to the opened key.
        /// </summary>
        /// <remarks>If the operation does not succeed, an exception is thrown corresponding to the
        /// specific Win32 error code. The caller should ensure that the provided handles and parameters are valid and
        /// that the necessary permissions are granted.</remarks>
        /// <param name="hKey">A handle to an open registry key that serves as the root for the subkey to be opened. This handle must be
        /// valid and have appropriate access rights.</param>
        /// <param name="lpSubKey">The name of the registry subkey to be opened. This value can be null or an empty string to open the key
        /// identified by hKey itself.</param>
        /// <param name="ulOptions">Options that control how the key is opened. This parameter is typically set to zero unless specific open or
        /// create options are required.</param>
        /// <param name="samDesired">A mask that specifies the desired access rights to the key. This determines the operations that can be
        /// performed on the opened key.</param>
        /// <param name="phkResult">When this method returns, contains a SafeRegistryHandle representing the opened registry key. If the
        /// operation fails, this value is set to an invalid handle.</param>
        /// <returns>A WIN32_ERROR value indicating the result of the operation. Returns WIN32_ERROR.ERROR_SUCCESS if the key is
        /// opened successfully; otherwise, an error code is returned.</returns>
        internal static WIN32_ERROR RegOpenKeyEx(SafeHandle hKey, string? lpSubKey, REG_OPEN_CREATE_OPTIONS ulOptions, REG_SAM_FLAGS samDesired, out SafeRegistryHandle phkResult)
        {
            var res = PInvoke.RegOpenKeyEx(hKey, lpSubKey, (uint)ulOptions, samDesired, out phkResult);
            if (res != WIN32_ERROR.ERROR_SUCCESS)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error(res);
            }
            return res;
        }

        /// <summary>
        /// Opens the specified registry key with the desired access rights.
        /// </summary>
        /// <param name="hKey">A handle to an open registry key that serves as the root for the subkey to be opened. This handle must be
        /// valid and have appropriate access rights.</param>
        /// <param name="lpSubKey">The name of the registry subkey to be opened. This value is relative to the key specified by <paramref
        /// name="hKey"/>. If <see langword="null"/>, the function opens the key identified by <paramref name="hKey"/>
        /// itself.</param>
        /// <param name="samDesired">A bitmask specifying the requested access rights to the key. This determines the operations that can be
        /// performed on the opened key.</param>
        /// <param name="phkResult">When this method returns, contains a handle to the opened registry key if the operation succeeds; otherwise,
        /// contains an invalid handle.</param>
        /// <returns>A <see cref="WIN32_ERROR"/> value indicating the result of the operation. Returns <see
        /// cref="WIN32_ERROR.ERROR_SUCCESS"/> if the key is opened successfully; otherwise, returns a nonzero error
        /// code.</returns>
        internal static WIN32_ERROR RegOpenKeyEx(SafeHandle hKey, string? lpSubKey, REG_SAM_FLAGS samDesired, out SafeRegistryHandle phkResult) => RegOpenKeyEx(hKey, lpSubKey, 0, samDesired, out phkResult);

        /// <summary>
        /// Retrieves information about the specified registry key, including the number of subkeys, value entries, and
        /// other metadata.
        /// </summary>
        /// <remarks>This method throws an exception if the underlying Windows API call fails. All output
        /// parameters are set only if the operation succeeds.</remarks>
        /// <param name="hKey">A handle to an open registry key. The handle must have been opened with the appropriate access rights for
        /// querying information.</param>
        /// <param name="lpClass">A buffer that receives the user-defined class name of the key. This buffer can be empty if the class name is
        /// not required.</param>
        /// <param name="lpcchClass">On input, specifies the size of the lpClass buffer, in characters. On output, receives the number of
        /// characters stored in lpClass, not including the terminating null character.</param>
        /// <param name="lpcSubKeys">When this method returns, contains the number of subkeys that are contained by the specified key.</param>
        /// <param name="lpcbMaxSubKeyLen">When this method returns, contains the length, in characters, of the longest subkey name.</param>
        /// <param name="lpcbMaxClassLen">When this method returns, contains the length, in characters, of the longest class string among the subkeys.</param>
        /// <param name="lpcValues">When this method returns, contains the number of value entries for the key.</param>
        /// <param name="lpcbMaxValueNameLen">When this method returns, contains the length, in characters, of the longest value name.</param>
        /// <param name="lpcbMaxValueLen">When this method returns, contains the length, in bytes, of the longest data component among the key's
        /// values.</param>
        /// <param name="lpcbSecurityDescriptor">When this method returns, contains the size, in bytes, of the key's security descriptor.</param>
        /// <param name="lpftLastWriteTime">When this method returns, contains the last write time of the key as a FILETIME structure.</param>
        /// <returns>A WIN32_ERROR value indicating the result of the operation. Returns ERROR_SUCCESS if the call succeeds;
        /// otherwise, an error code is returned.</returns>
        internal static WIN32_ERROR RegQueryInfoKey(SafeHandle hKey, Span<char> lpClass, out uint lpcchClass, out uint lpcSubKeys, out uint lpcbMaxSubKeyLen, out uint lpcbMaxClassLen, out uint lpcValues, out uint lpcbMaxValueNameLen, out uint lpcbMaxValueLen, out uint lpcbSecurityDescriptor, out System.Runtime.InteropServices.ComTypes.FILETIME lpftLastWriteTime)
        {
            lpcchClass = (uint)lpClass.Length;
            var res = PInvoke.RegQueryInfoKey(hKey, lpClass, ref lpcchClass, out lpcSubKeys, out lpcbMaxSubKeyLen, out lpcbMaxClassLen, out lpcValues, out lpcbMaxValueNameLen, out lpcbMaxValueLen, out lpcbSecurityDescriptor, out lpftLastWriteTime);
            if (res != WIN32_ERROR.ERROR_SUCCESS)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error(res);
            }
            return res;
        }

        /// <summary>
        /// Creates a new access token that duplicates an existing token, with specified access rights, attributes,
        /// impersonation level, and token type.
        /// </summary>
        /// <remarks>This method throws an exception if the token duplication fails. The caller is
        /// responsible for closing the returned token handle when it is no longer needed.</remarks>
        /// <param name="hExistingToken">A handle to the existing access token to duplicate. This handle must have appropriate access rights for
        /// duplication.</param>
        /// <param name="dwDesiredAccess">A set of access rights to assign to the new token. Specify the desired access mask for the duplicated token.</param>
        /// <param name="lpTokenAttributes">A pointer to a SECURITY_ATTRIBUTES structure that determines whether the returned handle can be inherited by
        /// child processes. Can be null to use default attributes.</param>
        /// <param name="ImpersonationLevel">Specifies the security impersonation level for the new token. Determines how the new token can be used for
        /// impersonation.</param>
        /// <param name="TokenType">Specifies whether the new token is a primary token or an impersonation token.</param>
        /// <param name="phNewToken">When this method returns, contains a handle to the newly created access token.</param>
        /// <returns>true if the token was duplicated successfully; otherwise, false.</returns>
        internal static BOOL DuplicateTokenEx(SafeHandle hExistingToken, TOKEN_ACCESS_MASK dwDesiredAccess, in SECURITY_ATTRIBUTES? lpTokenAttributes, SECURITY_IMPERSONATION_LEVEL ImpersonationLevel, TOKEN_TYPE TokenType, out SafeFileHandle phNewToken)
        {
            var res = PInvoke.DuplicateTokenEx(hExistingToken, dwDesiredAccess, lpTokenAttributes, ImpersonationLevel, TokenType, out phNewToken);
            if (!res)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            return res;
        }

        /// <summary>
        /// Opens the access token associated with a process handle.
        /// </summary>
        /// <remarks>If the operation fails, an exception is thrown containing the relevant Win32 error
        /// information. The caller is responsible for closing the returned token handle when it is no longer
        /// needed.</remarks>
        /// <param name="ProcessHandle">A handle to the process whose access token is to be opened. The handle must have appropriate access rights
        /// for the requested token operations.</param>
        /// <param name="DesiredAccess">A set of bit flags that specify the requested access rights for the access token. This parameter determines
        /// the operations that can be performed on the token.</param>
        /// <param name="TokenHandle">When this method returns, contains a handle to the newly opened access token if the operation succeeds.</param>
        /// <returns>A value indicating whether the operation succeeded. Returns <see langword="true"/> if the access token was
        /// opened successfully; otherwise, <see langword="false"/>.</returns>
        internal static BOOL OpenProcessToken(SafeHandle ProcessHandle, TOKEN_ACCESS_MASK DesiredAccess, out SafeFileHandle TokenHandle)
        {
            var res = PInvoke.OpenProcessToken(ProcessHandle, DesiredAccess, out TokenHandle);
            if (!res)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            return res;
        }

        /// <summary>
        /// Retrieves the locally unique identifier (LUID) used on a specified system to locally represent the specified
        /// privilege name.
        /// </summary>
        /// <remarks>If the lookup fails, an exception is thrown containing the last Win32 error. This
        /// method wraps the native LookupPrivilegeValue function and enforces error handling via exceptions.</remarks>
        /// <param name="lpSystemName">The name of the system on which the privilege name is to be looked up. If this parameter is null, the local
        /// system is used.</param>
        /// <param name="lpName">The name of the privilege to look up. This must be a valid privilege constant.</param>
        /// <param name="lpLuid">When this method returns, contains the LUID that represents the specified privilege on the specified system.</param>
        /// <returns>true if the privilege name was successfully found and the LUID was retrieved; otherwise, false.</returns>
        internal static BOOL LookupPrivilegeValue(string? lpSystemName, SE_PRIVILEGE lpName, out LUID lpLuid)
        {
            var res = PInvoke.LookupPrivilegeValue(lpSystemName, lpName.ToString(), out lpLuid);
            if (!res)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            return res;
        }

        /// <summary>
        /// Retrieves the locally unique identifier (LUID) that represents the specified privilege on the local system.
        /// </summary>
        /// <param name="lpName">The name of the privilege to retrieve the LUID for. This must be a valid privilege name recognized by the
        /// system.</param>
        /// <param name="lpLuid">When this method returns, contains the LUID that corresponds to the specified privilege name.</param>
        /// <returns>A value that indicates whether the operation succeeded. Returns <see langword="true"/> if the privilege name
        /// was found and the LUID was retrieved successfully; otherwise, <see langword="false"/>.</returns>
        internal static BOOL LookupPrivilegeValue(SE_PRIVILEGE lpName, out LUID lpLuid) => LookupPrivilegeValue(null, lpName, out lpLuid);

        /// <summary>
        /// Retrieves information about the specified access token.
        /// </summary>
        /// <remarks>If the buffer specified by TokenInformation is too small, the method throws an
        /// exception. The required buffer size is returned in ReturnLength.</remarks>
        /// <param name="TokenHandle">A handle to the access token from which information is retrieved. This handle must have appropriate access
        /// rights for the requested information class.</param>
        /// <param name="TokenInformationClass">A value that specifies the type of information to retrieve about the token.</param>
        /// <param name="TokenInformation">A span of bytes that receives the requested token information. The required size depends on the information
        /// class specified.</param>
        /// <param name="ReturnLength">When this method returns, contains the number of bytes returned in the TokenInformation buffer or, if the
        /// buffer is too small, the number of bytes required.</param>
        /// <returns>A value that is <see langword="true"/> if the function succeeds; otherwise, <see langword="false"/>.</returns>
        internal static BOOL GetTokenInformation(SafeHandle TokenHandle, TOKEN_INFORMATION_CLASS TokenInformationClass, Span<byte> TokenInformation, out uint ReturnLength)
        {
            var res = PInvoke.GetTokenInformation(TokenHandle, TokenInformationClass, TokenInformation, out ReturnLength);
            if (!res && 0 != TokenInformation.Length)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            return res;
        }

        /// <summary>
        /// Adjusts the privileges of the specified access token.
        /// </summary>
        /// <remarks>This method wraps the native AdjustTokenPrivileges API and throws an exception if the
        /// operation fails. The caller is responsible for ensuring that the token handle has the necessary access
        /// rights.</remarks>
        /// <param name="TokenHandle">A handle to the access token whose privileges are to be modified. The handle must have
        /// TOKEN_ADJUST_PRIVILEGES access.</param>
        /// <param name="DisableAllPrivileges">A value that determines whether all privileges are to be disabled. Specify <see langword="true"/> to disable
        /// all privileges; otherwise, <see langword="false"/> to modify privileges as specified by <paramref
        /// name="NewState"/>.</param>
        /// <param name="NewState">A structure that specifies the privileges and their attributes to be adjusted. The contents are used only if
        /// <paramref name="DisableAllPrivileges"/> is <see langword="false"/>.</param>
        /// <param name="PreviousState">A buffer that receives the previous state of the token's privileges. This buffer can be empty if the
        /// previous state is not required.</param>
        /// <param name="ReturnLength">When this method returns, contains the number of bytes required to store the previous state of the token's
        /// privileges.</param>
        /// <returns>A value indicating whether the operation succeeded. If the function fails, an exception is thrown.</returns>
        internal static BOOL AdjustTokenPrivileges(SafeHandle TokenHandle, in BOOL DisableAllPrivileges, in TOKEN_PRIVILEGES NewState, Span<byte> PreviousState, out uint ReturnLength)
        {
            BOOL res;
            unsafe
            {
                fixed (TOKEN_PRIVILEGES* newStatePtr = &NewState)
                {
                    res = PInvoke.AdjustTokenPrivileges(TokenHandle, DisableAllPrivileges, newStatePtr, PreviousState, out ReturnLength);
                }
            }
            if (!res)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            return res;
        }

        /// <summary>
        /// Adjusts the privileges of the specified access token according to the provided new state.
        /// </summary>
        /// <param name="TokenHandle">A handle to the access token whose privileges are to be modified. The handle must have
        /// TOKEN_ADJUST_PRIVILEGES access rights.</param>
        /// <param name="NewState">A structure that specifies the privileges to enable or disable for the access token.</param>
        /// <returns>A value that indicates whether the function succeeds. Returns <see langword="true"/> if the operation is
        /// successful; otherwise, <see langword="false"/>.</returns>
        internal static BOOL AdjustTokenPrivileges(SafeHandle TokenHandle, in TOKEN_PRIVILEGES NewState) => AdjustTokenPrivileges(TokenHandle, false, in NewState, null, out _);

        /// <summary>
        /// Retrieves the name of a privilege specified by a locally unique identifier (LUID) on the specified system.
        /// </summary>
        /// <remarks>If the buffer specified by lpName is too small, an exception is thrown. The privilege
        /// name is returned as a null-terminated Unicode string.</remarks>
        /// <param name="lpSystemName">The name of the target system. If this parameter is null, the local system is used.</param>
        /// <param name="lpLuid">A reference to the LUID that uniquely identifies the privilege to look up.</param>
        /// <param name="lpName">A span of characters that receives the name of the privilege. The buffer must be large enough to receive the
        /// privilege name, including the terminating null character.</param>
        /// <param name="cchName">On input, specifies the size of the lpName buffer, in characters. On output, receives the number of
        /// characters written to lpName, including the terminating null character.</param>
        /// <returns>true if the privilege name was successfully retrieved; otherwise, false.</returns>
        internal static BOOL LookupPrivilegeName(string? lpSystemName, in LUID lpLuid, Span<char> lpName, out uint cchName)
        {
            cchName = (uint)lpName.Length;
            var res = PInvoke.LookupPrivilegeName(lpSystemName, in lpLuid, lpName, ref cchName);
            if (!res)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            return res;
        }

        /// <summary>
        /// Creates a new process using the specified token, application name, command line, and other parameters.
        /// </summary>
        /// <remarks>This method wraps the native Windows API function <c>CreateProcessWithTokenW</c>,
        /// providing a managed interface for creating processes with a specified access token. The caller is
        /// responsible for ensuring that the provided token and environment block are valid.</remarks>
        /// <param name="hToken">A handle to the access token that will be used to create the process. This handle must be valid and have the
        /// necessary privileges.</param>
        /// <param name="dwLogonFlags">Flags that control the logon behavior of the process. These flags determine how the process is created and
        /// logged on.</param>
        /// <param name="lpApplicationName">The name of the application to execute. Can be null if the application name is included in <paramref
        /// name="lpCommandLine"/>.</param>
        /// <param name="lpCommandLine">The command line to be executed, including the application name if <paramref name="lpApplicationName"/> is
        /// null.</param>
        /// <param name="dwCreationFlags">Flags that control the creation of the process, such as whether it is created in a suspended state.</param>
        /// <param name="lpEnvironment">A handle to the environment block for the new process. This handle must not be null, closed, or invalid.</param>
        /// <param name="lpCurrentDirectory">The full path to the current directory for the new process. Can be null to use the current directory of the
        /// calling process.</param>
        /// <param name="lpStartupInfo">A structure containing startup information for the new process, such as window settings and standard
        /// handles.</param>
        /// <param name="lpProcessInformation">When the method returns, contains information about the newly created process and its primary thread.</param>
        /// <returns><see langword="true"/> if the process is successfully created; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="lpEnvironment"/> is null, closed, or invalid.</exception>
        internal static BOOL CreateProcessWithToken(SafeHandle hToken, CREATE_PROCESS_LOGON_FLAGS dwLogonFlags, string? lpApplicationName, ref Span<char> lpCommandLine, PROCESS_CREATION_FLAGS dwCreationFlags, SafeEnvironmentBlockHandle? lpEnvironment, string? lpCurrentDirectory, in STARTUPINFOW lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation)
        {
            bool lpEnvironmentAddRef = false;
            try
            {
                lpEnvironment?.DangerousAddRef(ref lpEnvironmentAddRef);
                BOOL res;
                unsafe
                {
                    res = PInvoke.CreateProcessWithToken(hToken, dwLogonFlags, lpApplicationName, ref lpCommandLine, dwCreationFlags, lpEnvironment is not null ? (void*)lpEnvironment.DangerousGetHandle() : null, lpCurrentDirectory, in lpStartupInfo, out lpProcessInformation);
                }
                if (!res)
                {
                    throw ExceptionUtilities.GetExceptionForLastWin32Error();
                }
                return res;
            }
            finally
            {
                if (lpEnvironmentAddRef)
                {
                    lpEnvironment?.DangerousRelease();
                }
            }
        }

        /// <summary>
        /// Creates a new process and its primary thread in the security context of the specified user token.
        /// </summary>
        /// <param name="hToken">A handle to the primary token that represents the user for whom the new process is created. The token must
        /// have appropriate access rights for process creation.</param>
        /// <param name="lpApplicationName">The name of the module to be executed. This can be null, in which case the module name must be the first
        /// white space–delimited token in lpCommandLine.</param>
        /// <param name="lpCommandLine">A reference to a span containing the command line to be executed. The string can include the application
        /// name and any arguments.</param>
        /// <param name="lpProcessAttributes">A pointer to a SECURITY_ATTRIBUTES structure that determines whether the returned process handle can be
        /// inherited by child processes. Can be null.</param>
        /// <param name="lpThreadAttributes">A pointer to a SECURITY_ATTRIBUTES structure that determines whether the returned thread handle can be
        /// inherited by child processes. Can be null.</param>
        /// <param name="bInheritHandles">A value that indicates whether each inheritable handle in the calling process is inherited by the new
        /// process. Specify <see langword="true"/> to inherit handles; otherwise, <see langword="false"/>.</param>
        /// <param name="dwCreationFlags">Flags that control the priority class and the creation of the process. This parameter can be a combination
        /// of PROCESS_CREATION_FLAGS values.</param>
        /// <param name="lpEnvironment">A handle to the environment block for the new process. Must not be null or closed.</param>
        /// <param name="lpCurrentDirectory">The full path to the current directory for the new process. If this parameter is null, the new process will
        /// have the same current drive and directory as the calling process.</param>
        /// <param name="lpStartupInfo">A reference to a STARTUPINFOW structure that specifies the window station, desktop, standard handles, and
        /// appearance of the main window for the new process.</param>
        /// <param name="lpProcessInformation">When this method returns, contains a PROCESS_INFORMATION structure with information about the newly created
        /// process and its primary thread.</param>
        /// <returns>A value indicating whether the process was created successfully. Returns <see langword="true"/> if the
        /// process was created; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="lpEnvironment"/> is null or closed.</exception>
        internal static BOOL CreateProcessAsUser(SafeHandle hToken, string? lpApplicationName, ref Span<char> lpCommandLine, in SECURITY_ATTRIBUTES? lpProcessAttributes, in SECURITY_ATTRIBUTES? lpThreadAttributes, in BOOL bInheritHandles, PROCESS_CREATION_FLAGS dwCreationFlags, SafeEnvironmentBlockHandle? lpEnvironment, string? lpCurrentDirectory, in STARTUPINFOW lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation)
        {
            bool lpEnvironmentAddRef = false;
            try
            {
                lpEnvironment?.DangerousAddRef(ref lpEnvironmentAddRef);
                BOOL res;
                unsafe
                {
                    res = PInvoke.CreateProcessAsUser(hToken, lpApplicationName, ref lpCommandLine, lpProcessAttributes, lpThreadAttributes, bInheritHandles, dwCreationFlags, lpEnvironment is not null ? (void*)lpEnvironment.DangerousGetHandle() : null, lpCurrentDirectory, in lpStartupInfo, out lpProcessInformation);
                }
                if (!res)
                {
                    throw ExceptionUtilities.GetExceptionForLastWin32Error();
                }
                return res;
            }
            finally
            {
                if (lpEnvironmentAddRef)
                {
                    lpEnvironment?.DangerousRelease();
                }
            }
        }

        /// <summary>
        /// Creates a new process and its primary thread in the security context of the specified user token, using
        /// extended startup information and environment block.
        /// </summary>
        /// <remarks>This method wraps the native CreateProcessAsUser Windows API and requires appropriate
        /// privileges to create a process in another user's context. The caller is responsible for ensuring that all
        /// handles and structures provided are valid and remain valid for the duration of the call. On failure, a Win32
        /// exception is thrown with details from the last error code.</remarks>
        /// <param name="hToken">A handle to the primary token that represents the user for whom the new process is to be created. The handle
        /// must have appropriate access rights for process creation.</param>
        /// <param name="lpApplicationName">The name of the module to be executed. This can be null, in which case the module name is the first white
        /// space–delimited token in the command line.</param>
        /// <param name="lpCommandLine">A reference to a span of characters containing the command line to be executed. The span must be
        /// null-terminated; otherwise, an exception is thrown.</param>
        /// <param name="lpProcessAttributes">Optional security attributes for the new process. If null, default security attributes are used.</param>
        /// <param name="lpThreadAttributes">Optional security attributes for the primary thread of the new process. If null, default security attributes
        /// are used.</param>
        /// <param name="bInheritHandles">A value that determines whether each inheritable handle in the calling process is inherited by the new
        /// process. Specify <see langword="true"/> to inherit handles; otherwise, <see langword="false"/>.</param>
        /// <param name="dwCreationFlags">Flags that control the priority class and creation of the process. This parameter can be a combination of
        /// PROCESS_CREATION_FLAGS values.</param>
        /// <param name="lpEnvironment">A handle to an environment block for the new process. The environment block must be properly formatted for
        /// the target process.</param>
        /// <param name="lpCurrentDirectory">The full path to the current directory for the new process. If null, the new process will have the same
        /// current drive and directory as the calling process.</param>
        /// <param name="lpStartupInfoEx">A reference to a STARTUPINFOEXW structure that specifies the window station, desktop, standard handles, and
        /// extended attributes for the new process.</param>
        /// <param name="lpProcessInformation">When this method returns, contains information about the newly created process and its primary thread.</param>
        /// <returns>true if the process is created successfully; otherwise, false.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="lpCommandLine"/> is not null-terminated.</exception>
        internal static BOOL CreateProcessAsUser(SafeHandle hToken, string? lpApplicationName, ref Span<char> lpCommandLine, in SECURITY_ATTRIBUTES? lpProcessAttributes, in SECURITY_ATTRIBUTES? lpThreadAttributes, in BOOL bInheritHandles, PROCESS_CREATION_FLAGS dwCreationFlags, SafeEnvironmentBlockHandle? lpEnvironment, string? lpCurrentDirectory, in STARTUPINFOEXW lpStartupInfoEx, out PROCESS_INFORMATION lpProcessInformation)
        {
            if (lpCommandLine != Span<char>.Empty && lpCommandLine.LastIndexOf('\0') == -1)
            {
                throw new ArgumentException("Required null terminator missing.", "lpCommandLine");
            }
            bool hTokenAddRef = false;
            bool lpEnvironmentAddRef = false;
            try
            {
                BOOL res;
                unsafe
                {
                    fixed (char* lpApplicationNameLocal = lpApplicationName, plpCommandLine = lpCommandLine, lpCurrentDirectoryLocal = lpCurrentDirectory)
                    fixed (PROCESS_INFORMATION* lpProcessInformationLocal = &lpProcessInformation)
                    fixed (STARTUPINFOEXW* lpStartupInfoExLocal = &lpStartupInfoEx)
                    {
                        SECURITY_ATTRIBUTES lpProcessAttributesLocal = lpProcessAttributes ?? default;
                        SECURITY_ATTRIBUTES lpThreadAttributesLocal = lpThreadAttributes ?? default;
                        hToken.DangerousAddRef(ref hTokenAddRef);
                        lpEnvironment?.DangerousAddRef(ref lpEnvironmentAddRef);
                        res = PInvoke.CreateProcessAsUser((HANDLE)hToken.DangerousGetHandle(), lpApplicationNameLocal, plpCommandLine, lpProcessAttributes.HasValue ? &lpProcessAttributesLocal : null, lpThreadAttributes.HasValue ? &lpThreadAttributesLocal : null, bInheritHandles, dwCreationFlags, lpEnvironment is not null ? (void*)lpEnvironment.DangerousGetHandle() : null, lpCurrentDirectoryLocal, (STARTUPINFOW*)lpStartupInfoExLocal, lpProcessInformationLocal);
                        if (!res)
                        {
                            throw ExceptionUtilities.GetExceptionForLastWin32Error();
                        }
                        lpCommandLine = lpCommandLine.Slice(0, ((PWSTR)plpCommandLine).Length);
                    }
                }
                return res;
            }
            finally
            {
                if (lpEnvironmentAddRef)
                {
                    lpEnvironment?.DangerousRelease();
                }
                if (hTokenAddRef)
                {
                    hToken.DangerousRelease();
                }
            }
        }

        /// <summary>
        /// Opens a handle to the specified service control manager database.
        /// </summary>
        /// <param name="lpMachineName">The name of the target computer. If <see langword="null"/>, the local computer is used.</param>
        /// <param name="lpDatabaseName">The name of the service control manager database. If <see langword="null"/>, the default database is used.</param>
        /// <param name="dwDesiredAccess">The access rights to the service control manager. This parameter must be a combination of <see
        /// cref="SC_MANAGER_ACCESS"/> values.</param>
        /// <returns>A <see cref="CloseServiceHandleSafeHandle"/> that represents the handle to the service control manager
        /// database.</returns>
        internal static CloseServiceHandleSafeHandle OpenSCManager(string? lpMachineName, string? lpDatabaseName, SC_MANAGER_ACCESS dwDesiredAccess)
        {
            var handle = PInvoke.OpenSCManager(lpMachineName, lpDatabaseName, (uint)dwDesiredAccess);
            if (handle.IsInvalid)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            return handle;
        }

        /// <summary>
        /// Opens a handle to the service control manager database on the specified computer with the desired access
        /// rights.
        /// </summary>
        /// <param name="lpDatabaseName">The name of the service control manager database. This parameter is typically null to specify the default
        /// database, "ServicesActive".</param>
        /// <param name="dwDesiredAccess">A bitmask of access rights required for the returned handle. This determines the operations that can be
        /// performed on the service control manager.</param>
        /// <returns>A safe handle to the service control manager database. The caller is responsible for closing the handle when
        /// it is no longer needed.</returns>
        internal static CloseServiceHandleSafeHandle OpenSCManager(string? lpDatabaseName, SC_MANAGER_ACCESS dwDesiredAccess) => OpenSCManager(null, lpDatabaseName, dwDesiredAccess);

        /// <summary>
        /// Opens a handle to the Service Control Manager on the local computer with the specified access rights.
        /// </summary>
        /// <param name="dwDesiredAccess">A bitmask of access rights to request for the Service Control Manager. This value determines the operations
        /// that can be performed with the returned handle.</param>
        /// <returns>A safe handle to the Service Control Manager. The caller is responsible for closing the handle when it is no
        /// longer needed.</returns>
        internal static CloseServiceHandleSafeHandle OpenSCManager(SC_MANAGER_ACCESS dwDesiredAccess) => OpenSCManager(null, null, dwDesiredAccess);

        /// <summary>
        /// Opens an existing service in the specified service control manager database.
        /// </summary>
        /// <param name="hSCManager">A handle to the service control manager database. This handle is obtained from a previous call to the
        /// OpenSCManager function.</param>
        /// <param name="lpServiceName">The name of the service to be opened. This name is case-sensitive and must match the service name exactly.</param>
        /// <param name="dwDesiredAccess">The access rights to the service. This parameter specifies the access level required for the service.</param>
        /// <returns>A <see cref="CloseServiceHandleSafeHandle"/> that represents the handle to the opened service. The handle
        /// must be closed using the appropriate method when it is no longer needed.</returns>
        internal static CloseServiceHandleSafeHandle OpenService(SafeHandle hSCManager, string lpServiceName, SERVICE_ACCESS_RIGHTS dwDesiredAccess)
        {
            var handle = PInvoke.OpenService(hSCManager, lpServiceName, (uint)dwDesiredAccess);
            if (handle.IsInvalid)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            return handle;
        }

        /// <summary>
        /// Retrieves the current status of the specified service based on the provided information level.
        /// </summary>
        /// <param name="hService">A handle to the service. This handle is obtained from a previous call to the OpenService or CreateService
        /// function.</param>
        /// <param name="InfoLevel">The information level of the service status to be queried. This parameter specifies the type of information
        /// to retrieve.</param>
        /// <param name="lpBuffer">A buffer that receives the status information. The format of this data depends on the value of the <paramref
        /// name="InfoLevel"/> parameter.</param>
        /// <param name="pcbBytesNeeded">When the method returns, contains the number of bytes needed to store all the status information, if the
        /// buffer is too small.</param>
        /// <returns><see langword="true"/> if the function succeeds; otherwise, <see langword="false"/>.</returns>
        internal static BOOL QueryServiceStatusEx(SafeHandle hService, SC_STATUS_TYPE InfoLevel, Span<byte> lpBuffer, out uint pcbBytesNeeded)
        {
            var res = PInvoke.QueryServiceStatusEx(hService, InfoLevel, lpBuffer, out pcbBytesNeeded);
            if (!res && ((WIN32_ERROR)Marshal.GetLastWin32Error() is WIN32_ERROR lastWin32Error) && (lastWin32Error != WIN32_ERROR.ERROR_INSUFFICIENT_BUFFER || lpBuffer.Length != 0))
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            return res;
        }

        /// <summary>
        /// Modifies an access control list (ACL) by adding or updating the specified access control entries (ACEs).
        /// </summary>
        /// <remarks>This method uses the Windows API function <c>SetEntriesInAcl</c> to modify the ACL.
        /// The caller must ensure that the <paramref name="OldAcl"/> handle, if provided, is valid and not closed. The
        /// <paramref name="NewAcl"/> handle must be released by the caller to avoid memory leaks.</remarks>
        /// <param name="pListOfExplicitEntries">A read-only span of <see cref="EXPLICIT_ACCESS_W"/> structures that define the access control entries to be
        /// added or updated in the ACL.</param>
        /// <param name="OldAcl">An optional handle to the existing ACL to be modified. If <see langword="null"/>, a new ACL is created.</param>
        /// <param name="NewAcl">When this method returns, contains a handle to the newly created or modified ACL. The caller is responsible
        /// for releasing this handle.</param>
        /// <returns>A <see cref="WIN32_ERROR"/> value indicating the result of the operation. Returns <see
        /// cref="WIN32_ERROR.ERROR_SUCCESS"/> if the operation is successful.</returns>
		internal static WIN32_ERROR SetEntriesInAcl(ReadOnlySpan<EXPLICIT_ACCESS_W> pListOfExplicitEntries, LocalFreeSafeHandle? OldAcl, out LocalFreeSafeHandle NewAcl)
        {
            bool OldAclAddRef = false;
            try
            {
                if (OldAcl is not null && !OldAcl.IsClosed)
                {
                    OldAcl.DangerousAddRef(ref OldAclAddRef);
                }
                WIN32_ERROR res;
                unsafe
                {
                    ACL* NewAclLocal = null;
                    fixed (EXPLICIT_ACCESS_W* pListOfExplicitEntriesLocal = pListOfExplicitEntries)
                    {
                        res = PInvoke.SetEntriesInAcl((uint)pListOfExplicitEntries.Length, pListOfExplicitEntriesLocal, OldAcl is not null ? (ACL*)OldAcl.DangerousGetHandle() : (ACL*)null, &NewAclLocal);
                    }
                    if (res != WIN32_ERROR.ERROR_SUCCESS)
                    {
                        throw ExceptionUtilities.GetExceptionForLastWin32Error(res);
                    }
                    NewAcl = new((IntPtr)NewAclLocal, true);
                }
                return res;
            }
            finally
            {
                if (OldAclAddRef)
                {
                    OldAcl?.DangerousRelease();
                }
            }
        }

        /// <summary>
        /// Creates a new access control list (ACL) by applying the specified list of explicit access entries to a
        /// default or empty ACL.
        /// </summary>
        /// <param name="pListOfExplicitEntries">A read-only span containing the explicit access entries to apply to the new ACL. Each entry defines access
        /// permissions for a trustee.</param>
        /// <param name="NewAcl">When this method returns, contains a handle to the newly created ACL. The caller is responsible for
        /// releasing this handle when it is no longer needed.</param>
        /// <returns>A WIN32_ERROR value indicating the result of the operation. Returns WIN32_ERROR.ERROR_SUCCESS if the ACL was
        /// created successfully; otherwise, returns an error code.</returns>
        internal static WIN32_ERROR SetEntriesInAcl(ReadOnlySpan<EXPLICIT_ACCESS_W> pListOfExplicitEntries, out LocalFreeSafeHandle NewAcl) => SetEntriesInAcl(pListOfExplicitEntries, null, out NewAcl);

        /// <summary>
        /// Sets the security information for a specified object, such as a file, registry key, or other securable
        /// object.
        /// </summary>
        /// <remarks>This method wraps the native <c>SetSecurityInfo</c> function and ensures proper
        /// reference management for the provided handles. Callers are responsible for ensuring that the handles passed
        /// to this method are valid and not closed.</remarks>
        /// <param name="handle">A <see cref="SafeHandle"/> representing the handle to the object whose security information is being set.
        /// The handle must not be null or closed.</param>
        /// <param name="ObjectType">The type of object for which security information is being set. This is specified as a value of the <see
        /// cref="SE_OBJECT_TYPE"/> enumeration.</param>
        /// <param name="SecurityInfo">A bitmask of <see cref="OBJECT_SECURITY_INFORMATION"/> values that specify the type of security information
        /// to set (e.g., owner, group, DACL, or SACL).</param>
        /// <param name="psidOwner">An optional <see cref="SafeHandle"/> representing the new owner SID to set. Pass <c>null</c> to leave
        /// the owner unchanged.</param>
        /// <param name="psidGroup">An optional <see cref="SafeHandle"/> representing the new group SID to set. Pass <c>null</c> to leave
        /// the group unchanged.</param>
        /// <param name="pDacl">An optional <see cref="LocalFreeSafeHandle"/> representing the new discretionary access control list (DACL)
        /// to set. Pass <c>null</c> to leave the DACL unchanged.</param>
        /// <param name="pSacl">An optional <see cref="LocalFreeSafeHandle"/> representing the new system access control list (SACL) to set.
        /// Pass <c>null</c> to leave the SACL unchanged.</param>
        /// <returns>A <see cref="WIN32_ERROR"/> value indicating the result of the operation. Returns <see
        /// cref="WIN32_ERROR.ERROR_SUCCESS"/> if the operation succeeds.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="handle"/> is null or closed.</exception>
        internal static WIN32_ERROR SetSecurityInfo(SafeHandle handle, SE_OBJECT_TYPE ObjectType, OBJECT_SECURITY_INFORMATION SecurityInfo, SafeHandle? psidOwner, SafeHandle? psidGroup, LocalFreeSafeHandle? pDacl, LocalFreeSafeHandle? pSacl)
        {
            if (handle is null || handle.IsClosed)
            {
                throw new ArgumentNullException(nameof(handle));
            }
            bool handleAddRef = false;
            bool psidOwnerAddRef = false;
            bool psidGroupAddRef = false;
            bool pDaclAddRef = false;
            bool pSaclAddRef = false;
            try
            {
                handle.DangerousAddRef(ref handleAddRef);
                psidOwner?.DangerousAddRef(ref psidOwnerAddRef);
                psidGroup?.DangerousAddRef(ref psidGroupAddRef);
                pDacl?.DangerousAddRef(ref pDaclAddRef);
                pSacl?.DangerousAddRef(ref pSaclAddRef);
                WIN32_ERROR res;
                unsafe
                {
                    res = PInvoke.SetSecurityInfo((HANDLE)handle.DangerousGetHandle(), ObjectType, SecurityInfo, psidOwner is not null ? new PSID(psidOwner.DangerousGetHandle()) : (PSID)null, psidGroup is not null ? new PSID(psidGroup.DangerousGetHandle()) : (PSID)null, pDacl is not null ? (ACL*)pDacl.DangerousGetHandle() : (ACL*)null, pSacl is not null ? (ACL*)pSacl.DangerousGetHandle() : (ACL*)null);
                }
                if (res != WIN32_ERROR.ERROR_SUCCESS)
                {
                    throw ExceptionUtilities.GetExceptionForLastWin32Error(res);
                }
                return res;
            }
            finally
            {
                if (pSaclAddRef)
                {
                    pSacl?.DangerousRelease();
                }
                if (pDaclAddRef)
                {
                    pDacl?.DangerousRelease();
                }
                if (psidGroupAddRef)
                {
                    psidGroup?.DangerousRelease();
                }
                if (psidOwnerAddRef)
                {
                    psidOwner?.DangerousRelease();
                }
                if (handleAddRef)
                {
                    handle.DangerousRelease();
                }
            }
        }

        /// <summary>
        /// Retrieves security information for a specified object, such as its owner, group, DACL, or SACL.
        /// </summary>
        /// <remarks>This method wraps the native <c>GetNamedSecurityInfo</c> function and provides a
        /// managed interface for retrieving security information. The caller is responsible for freeing any handles or
        /// memory returned by this method to avoid resource leaks.</remarks>
        /// <param name="pObjectName">The name of the object for which to retrieve security information. This can be a file, registry key, or
        /// other securable object.</param>
        /// <param name="ObjectType">The type of the object specified by <paramref name="pObjectName"/>. This determines how the object name is
        /// interpreted.</param>
        /// <param name="SecurityInfo">A combination of flags that specify the type of security information to retrieve, such as owner, group,
        /// DACL, or SACL.</param>
        /// <param name="ppsidOwner">When the method returns, contains a handle to the security identifier (SID) of the object's owner. This
        /// handle must be freed by the caller using the appropriate method.</param>
        /// <param name="ppsidGroup">When the method returns, contains a handle to the security identifier (SID) of the object's primary group.
        /// This handle must be freed by the caller using the appropriate method.</param>
        /// <param name="ppDacl">A pointer to a pointer that, when the method returns, contains the discretionary access control list (DACL)
        /// of the object. This value is null if the DACL is not requested or does not exist.</param>
        /// <param name="ppSacl">A pointer to a pointer that, when the method returns, contains the system access control list (SACL) of the
        /// object. This value is null if the SACL is not requested or does not exist.</param>
        /// <param name="ppSecurityDescriptor">When the method returns, contains a pointer to the security descriptor of the object. The caller is
        /// responsible for freeing this memory.</param>
        /// <returns>A <see cref="WIN32_ERROR"/> value indicating the result of the operation. Returns <see
        /// cref="WIN32_ERROR.ERROR_SUCCESS"/> if the operation succeeds.</returns>
        internal static WIN32_ERROR GetNamedSecurityInfo(string pObjectName, SE_OBJECT_TYPE ObjectType, OBJECT_SECURITY_INFORMATION SecurityInfo, out SafeNoReleaseHandle? ppsidOwner, out SafeNoReleaseHandle? ppsidGroup, out LocalFreeSafeHandle? ppDacl, out LocalFreeSafeHandle? ppSacl, out LocalFreeSafeHandle ppSecurityDescriptor)
        {
            WIN32_ERROR res;
            unsafe
            {
                PSID psidOwner = default, pSidGroup = default; ACL* pDacl = null, pSacl = null; PSECURITY_DESCRIPTOR pSecurityDescriptor = default;
                fixed (char* pObjectNameLocal = pObjectName)
                {
                    res = PInvoke.GetNamedSecurityInfo(pObjectNameLocal, ObjectType, SecurityInfo, &psidOwner, &pSidGroup, &pDacl, &pSacl, &pSecurityDescriptor);
                }
                if (res != WIN32_ERROR.ERROR_SUCCESS)
                {
                    throw ExceptionUtilities.GetExceptionForLastWin32Error(res);
                }
                if (pSecurityDescriptor == default)
                {
                    throw new InvalidOperationException("Failed to retrieve security descriptor.");
                }
                ppsidOwner = psidOwner != default ? new((IntPtr)psidOwner.Value) : null;
                ppsidGroup = pSidGroup != default ? new((IntPtr)pSidGroup.Value) : null;
                ppDacl = pDacl is not null ? new((IntPtr)pDacl, false) : null;
                ppSacl = pSacl is not null ? new((IntPtr)pSacl, false) : null;
                ppSecurityDescriptor = new((IntPtr)pSecurityDescriptor, true);
            }
            return res;
        }

        /// <summary>
        /// Initializes a new Authz resource manager for managing access control and authorization operations.
        /// </summary>
        /// <param name="Flags">A set of flags that specify the behavior of the resource manager. This parameter can include values such as
        /// <see langword="0"/> for default behavior.</param>
        /// <param name="pfnDynamicAccessCheck">A callback function for performing dynamic access checks. This parameter can be <see langword="null"/> if no
        /// dynamic access checks are required.</param>
        /// <param name="pfnComputeDynamicGroups">A callback function for computing dynamic groups. This parameter can be <see langword="null"/> if no dynamic
        /// group computation is required.</param>
        /// <param name="pfnFreeDynamicGroups">A callback function for freeing resources allocated for dynamic groups. This parameter can be <see
        /// langword="null"/> if no dynamic group resources need to be freed.</param>
        /// <param name="szResourceManagerName">The name of the resource manager. This parameter cannot be <see langword="null"/> and must be a valid
        /// string.</param>
        /// <param name="phAuthzResourceManager">When this method returns, contains a handle to the initialized Authz resource manager. This handle must be
        /// released using the appropriate cleanup method.</param>
        /// <returns><see langword="true"/> if the resource manager was successfully initialized; otherwise, <see
        /// langword="false"/>.</returns>
        /// <exception cref="Win32Exception">Thrown if the initialization fails due to a system error.</exception>
        internal static BOOL AuthzInitializeResourceManager(AUTHZ_RESOURCE_MANAGER_FLAGS Flags, PFN_AUTHZ_DYNAMIC_ACCESS_CHECK? pfnDynamicAccessCheck, PFN_AUTHZ_COMPUTE_DYNAMIC_GROUPS? pfnComputeDynamicGroups, PFN_AUTHZ_FREE_DYNAMIC_GROUPS? pfnFreeDynamicGroups, string szResourceManagerName, out AuthzFreeResourceManagerSafeHandle phAuthzResourceManager)
        {
            var res = PInvoke.AuthzInitializeResourceManager((uint)Flags, pfnDynamicAccessCheck, pfnComputeDynamicGroups, pfnFreeDynamicGroups, szResourceManagerName, out phAuthzResourceManager);
            if (!res)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            if (phAuthzResourceManager.IsInvalid)
            {
                throw new InvalidOperationException("Failed to initialize Authz Resource Manager.");
            }
            return res;
        }

        /// <summary>
        /// Initializes an authorization context from a security identifier (SID).
        /// </summary>
        /// <remarks>This method wraps the native <c>AuthzInitializeContextFromSid</c> function and
        /// ensures proper error handling. It throws exceptions for common failure scenarios, such as invalid handles or
        /// system errors.</remarks>
        /// <param name="Flags">A combination of <see cref="AUTHZ_CONTEXT_FLAGS"/> values that specify the behavior of the authorization
        /// context.</param>
        /// <param name="UserSid">A <see cref="SafeHandle"/> representing the security identifier (SID) of the user for whom the context is
        /// being initialized. This parameter cannot be null.</param>
        /// <param name="hAuthzResourceManager">A <see cref="SafeHandle"/> representing the handle to the resource manager associated with the authorization
        /// context. This parameter cannot be null.</param>
        /// <param name="pExpirationTime">An optional expiration time for the context, specified as a nullable <see cref="long"/>. If null, the
        /// context does not expire.</param>
        /// <param name="Identifier">A <see cref="LUID"/> that uniquely identifies the authorization context.</param>
        /// <param name="DynamicGroupArgs">A pointer to application-defined data used to compute dynamic groups. This parameter can be null if no
        /// dynamic groups are required.</param>
        /// <param name="phAuthzClientContext">When this method returns, contains an <see cref="AuthzFreeContextSafeHandle"/> representing the initialized
        /// authorization context. This parameter is passed uninitialized.</param>
        /// <returns><see langword="true"/> if the authorization context is successfully initialized; otherwise, <see
        /// langword="false"/>.</returns>
        /// <exception cref="Win32Exception">Thrown if the initialization fails due to a Win32 error, or if the resulting authorization context is
        /// invalid.</exception>
        internal static BOOL AuthzInitializeContextFromSid(AUTHZ_CONTEXT_FLAGS Flags, SafeHandle UserSid, SafeHandle hAuthzResourceManager, long? pExpirationTime, in LUID Identifier, IntPtr DynamicGroupArgs, out AuthzFreeContextSafeHandle phAuthzClientContext)
        {
            bool UserSidAddRef = false;
            try
            {
                UserSid.DangerousAddRef(ref UserSidAddRef);
                BOOL res;
                unsafe
                {
                    res = PInvoke.AuthzInitializeContextFromSid((uint)Flags, new(UserSid.DangerousGetHandle()), hAuthzResourceManager, pExpirationTime, Identifier, (void*)DynamicGroupArgs, out phAuthzClientContext);
                }
                if (!res)
                {
                    throw ExceptionUtilities.GetExceptionForLastWin32Error();
                }
                if (phAuthzClientContext.IsInvalid)
                {
                    throw new InvalidOperationException("Failed to initialize Authz Client Context from SID.");
                }
                return res;
            }
            finally
            {
                if (UserSidAddRef)
                {
                    UserSid.DangerousRelease();
                }
            }
        }

        /// <summary>
        /// Initializes an authorization context from a specified token.
        /// </summary>
        /// <param name="Flags">A combination of <see cref="AUTHZ_CONTEXT_FLAGS"/> values that specify the behavior of the authorization
        /// context.</param>
        /// <param name="TokenHandle">A handle to the token from which the authorization context is initialized. This handle must be valid and
        /// cannot be null.</param>
        /// <param name="hAuthzResourceManager">A handle to the resource manager associated with the authorization context. This handle must be valid and
        /// cannot be null.</param>
        /// <param name="pExpirationTime">An optional expiration time for the authorization context, specified as a <see cref="long"/> value. If
        /// null, the context does not have an expiration time.</param>
        /// <param name="Identifier">A <see cref="LUID"/> that uniquely identifies the authorization context.</param>
        /// <param name="DynamicGroupArgs">A pointer to dynamic group arguments used during the initialization of the authorization context. This
        /// value can be <see cref="IntPtr.Zero"/> if no dynamic group arguments are provided.</param>
        /// <param name="phAuthzClientContext">When this method returns, contains an <see cref="AuthzFreeContextSafeHandle"/> representing the initialized
        /// authorization context. This handle must be released by the caller when no longer needed.</param>
        /// <returns><see langword="true"/> if the authorization context is successfully initialized; otherwise, <see
        /// langword="false"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the authorization context is initialized but the resulting handle is invalid.</exception>
        internal static BOOL AuthzInitializeContextFromToken(AUTHZ_CONTEXT_FLAGS Flags, SafeHandle TokenHandle, SafeHandle hAuthzResourceManager, long? pExpirationTime, in LUID Identifier, IntPtr DynamicGroupArgs, out AuthzFreeContextSafeHandle phAuthzClientContext)
        {
            BOOL res;
            unsafe
            {
                res = PInvoke.AuthzInitializeContextFromToken((uint)Flags, TokenHandle, hAuthzResourceManager, pExpirationTime, Identifier, (void*)DynamicGroupArgs, out phAuthzClientContext);
            }
            if (!res)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            if (phAuthzClientContext.IsInvalid)
            {
                throw new InvalidOperationException("Failed to initialize Authz Client Context from Token.");
            }
            return res;
        }

        /// <summary>
        /// Performs an access check using the specified client context, access request, and security descriptors.
        /// </summary>
        /// <remarks>This method wraps the native <c>AuthzAccessCheck</c> function and performs additional
        /// error handling to ensure proper resource management. The caller must ensure that all input handles and
        /// structures are valid and properly initialized before calling this method.</remarks>
        /// <param name="Flags">Flags that specify the behavior of the access check. This parameter can include one or more values from the
        /// <see cref="AUTHZ_ACCESS_CHECK_FLAGS"/> enumeration.</param>
        /// <param name="hAuthzClientContext">A handle to the client context used for the access check. This handle must be valid and initialized.</param>
        /// <param name="pRequest">The access request structure that specifies the desired access rights and other parameters for the access
        /// check.</param>
        /// <param name="hAuditEvent">An optional handle to an audit event. If provided, the access check may generate audit events based on the
        /// result.</param>
        /// <param name="pSecurityDescriptor">A handle to the primary security descriptor against which the access check is performed. This handle must be
        /// valid and properly initialized.</param>
        /// <param name="OptionalSecurityDescriptorArray">An optional array of additional security descriptors to be considered during the access check. This
        /// parameter can be empty if no additional descriptors are required.</param>
        /// <param name="pReply">A reference to an <see cref="AUTHZ_ACCESS_REPLY"/> structure that receives the results of the access check,
        /// including granted access rights and any error information.</param>
        /// <param name="phAccessCheckResults">When the method returns, contains a handle to the access check results. The caller is responsible for
        /// releasing this handle when it is no longer needed.</param>
        /// <returns><see langword="true"/> if the access check is successful and the results are valid; otherwise, <see
        /// langword="false"/>.</returns>
        /// <exception cref="Win32Exception">Thrown if the access check fails or if the results handle is invalid.</exception>
        internal static BOOL AuthzAccessCheck(AUTHZ_ACCESS_CHECK_FLAGS Flags, SafeHandle hAuthzClientContext, in AUTHZ_ACCESS_REQUEST pRequest, SafeHandle? hAuditEvent, LocalFreeSafeHandle pSecurityDescriptor, ReadOnlySpan<PSECURITY_DESCRIPTOR> OptionalSecurityDescriptorArray, ref AUTHZ_ACCESS_REPLY pReply, out AuthzFreeHandleSafeHandle phAccessCheckResults)
        {
            bool pSecurityDescriptorAddRef = false;
            try
            {
                pSecurityDescriptor.DangerousAddRef(ref pSecurityDescriptorAddRef);
                BOOL res;
                unsafe
                {
                    res = PInvoke.AuthzAccessCheck(Flags, hAuthzClientContext, in pRequest, hAuditEvent, (PSECURITY_DESCRIPTOR)pSecurityDescriptor.DangerousGetHandle(), OptionalSecurityDescriptorArray, ref pReply, out phAccessCheckResults);
                }
                if (!res)
                {
                    throw ExceptionUtilities.GetExceptionForLastWin32Error();
                }
                if (phAccessCheckResults.IsInvalid)
                {
                    throw new InvalidOperationException("Failed to perform Authz Access Check.");
                }
                return res;
            }
            finally
            {
                if (pSecurityDescriptorAddRef)
                {
                    pSecurityDescriptor.DangerousRelease();
                }
            }
        }

        /// <summary>
        /// Renames a subkey of the specified registry key.
        /// </summary>
        /// <param name="hKey">A handle to an open registry key. This handle must have the appropriate access rights for the operation.</param>
        /// <param name="lpSubKeyName">The name of the subkey to be renamed. This cannot be <see langword="null"/> or an empty string.</param>
        /// <param name="lpNewKeyName">The new name for the subkey. This cannot be <see langword="null"/> or an empty string.</param>
        /// <returns>A <see cref="WIN32_ERROR"/> value indicating the result of the operation. Returns <see
        /// cref="WIN32_ERROR.ERROR_SUCCESS"/> if the operation succeeds.</returns>
        internal static WIN32_ERROR RegRenameKey(SafeHandle hKey, string? lpSubKeyName, string lpNewKeyName)
        {
            var res = PInvoke.RegRenameKey(hKey, lpSubKeyName, lpNewKeyName);
            if (res != WIN32_ERROR.ERROR_SUCCESS)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error(res);
            }
            return res;
        }

        /// <summary>
        /// Opens a handle to the Local Security Authority (LSA) Policy object on a specified system.
        /// </summary>
        /// <remarks>This method wraps the native LsaOpenPolicy function and provides error handling by
        /// throwing a <see cref="Win32Exception"/> if the operation fails. Ensure that the caller has the necessary 
        /// privileges to access the specified policy object.</remarks>
        /// <param name="SystemName">An optional <see cref="LSA_UNICODE_STRING"/> that specifies the name of the system whose LSA Policy object
        /// is to be opened. If <see langword="null"/>, the local system's LSA Policy object is opened.</param>
        /// <param name="ObjectAttributes">A reference to an <see cref="LSA_OBJECT_ATTRIBUTES"/> structure that specifies attributes for the policy
        /// object. This parameter is typically initialized to default values.</param>
        /// <param name="DesiredAccess">A bitmask specifying the access rights requested for the policy object. Use constants defined in the
        /// LSA_POLICY_ACCESS enumeration to specify the desired access.</param>
        /// <param name="PolicyHandle">When this method returns, contains a <see cref="LsaCloseSafeHandle"/> that represents the opened policy
        /// object. The caller is responsible for closing this handle using the appropriate method.</param>
        /// <returns>An <see cref="NTSTATUS"/> value indicating the result of the operation. Returns <see
        /// cref="NTSTATUS.STATUS_SUCCESS"/> if the operation is successful.</returns>
        /// <exception cref="Win32Exception">Thrown if the operation fails. The exception's error code corresponds to the Windows error code derived
        /// from the returned <see cref="NTSTATUS"/> value.</exception>
        internal static NTSTATUS LsaOpenPolicy(LSA_UNICODE_STRING? SystemName, in LSA_OBJECT_ATTRIBUTES ObjectAttributes, LSA_POLICY_ACCESS DesiredAccess, out LsaCloseSafeHandle PolicyHandle)
        {
            var res = PInvoke.LsaOpenPolicy(SystemName, in ObjectAttributes, (uint)DesiredAccess, out PolicyHandle);
            if (res != NTSTATUS.STATUS_SUCCESS)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error((WIN32_ERROR)PInvoke.LsaNtStatusToWinError(res));
            }
            return res;
        }

        /// <summary>
        /// Queries information from a policy object based on the specified information class.
        /// </summary>
        /// <remarks>This method wraps the native LSA (Local Security Authority) function
        /// <c>LsaQueryInformationPolicy</c>. The caller must ensure that the <paramref name="PolicyHandle"/> is valid
        /// and has the necessary access rights. The returned buffer must be freed using the appropriate mechanism to
        /// avoid memory leaks.</remarks>
        /// <param name="PolicyHandle">A handle to the policy object from which information is to be queried. This handle must have the appropriate
        /// access rights for the requested information.</param>
        /// <param name="InformationClass">The class of information to query. This determines the type of policy information returned.</param>
        /// <param name="Buffer">When this method returns, contains a handle to the buffer that holds the queried policy information. The
        /// caller is responsible for freeing this buffer.</param>
        /// <returns>An <see cref="NTSTATUS"/> value indicating the result of the operation. A value of <see
        /// cref="NTSTATUS.STATUS_SUCCESS"/> indicates success.</returns>
        /// <exception cref="Win32Exception">Thrown if the operation fails, wrapping the corresponding Windows error code.</exception>
        internal static NTSTATUS LsaQueryInformationPolicy(SafeHandle PolicyHandle, POLICY_INFORMATION_CLASS InformationClass, out SafeLsaFreeMemoryHandle Buffer)
        {
            NTSTATUS res;
            unsafe
            {
                res = PInvoke.LsaQueryInformationPolicy(PolicyHandle, InformationClass, out var BufferLocal);
                if (res != NTSTATUS.STATUS_SUCCESS)
                {
                    throw ExceptionUtilities.GetExceptionForLastWin32Error((WIN32_ERROR)PInvoke.LsaNtStatusToWinError(res));
                }
                Buffer = new((IntPtr)BufferLocal, true);
            }
            return res;
        }
    }
}
