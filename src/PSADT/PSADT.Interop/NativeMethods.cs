using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Threading;
using Microsoft.Win32.SafeHandles;
using PSADT.Interop.Extensions;
using PSADT.Interop.SafeHandles;
using PSADT.Interop.Utilities;
using Windows.Wdk.Foundation;
using Windows.Wdk.System.Threading;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.Security;
using Windows.Win32.Security.Authentication.Identity;
using Windows.Win32.Security.Authorization;
using Windows.Win32.Storage.FileSystem;
using Windows.Win32.System.ApplicationInstallationAndServicing;
using Windows.Win32.System.Diagnostics.Debug;
using Windows.Win32.System.JobObjects;
using Windows.Win32.System.LibraryLoader;
using Windows.Win32.System.Power;
using Windows.Win32.System.ProcessStatus;
using Windows.Win32.System.Registry;
using Windows.Win32.System.RemoteDesktop;
using Windows.Win32.System.Services;
using Windows.Win32.System.SystemInformation;
using Windows.Win32.System.Threading;
using Windows.Win32.System.Variant;
using Windows.Win32.UI.Controls;
using Windows.Win32.UI.HiDpi;
using Windows.Win32.UI.Input.KeyboardAndMouse;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;

namespace PSADT.Interop
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
    internal static class NativeMethods
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
            return PInvoke.RegOpenKeyEx(hKey, lpSubKey, (uint)ulOptions, samDesired, out phkResult).ThrowOnFailure();
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
        internal static WIN32_ERROR RegOpenKeyEx(SafeHandle hKey, string? lpSubKey, REG_SAM_FLAGS samDesired, out SafeRegistryHandle phkResult)
        {
            return RegOpenKeyEx(hKey, lpSubKey, 0, samDesired, out phkResult).ThrowOnFailure();
        }

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
            return PInvoke.RegQueryInfoKey(hKey, lpClass, ref lpcchClass, out lpcSubKeys, out lpcbMaxSubKeyLen, out lpcbMaxClassLen, out lpcValues, out lpcbMaxValueNameLen, out lpcbMaxValueLen, out lpcbSecurityDescriptor, out lpftLastWriteTime).ThrowOnFailure();
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
            BOOL res = PInvoke.DuplicateTokenEx(hExistingToken, dwDesiredAccess, lpTokenAttributes, ImpersonationLevel, TokenType, out phNewToken);
            return !res ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : res;
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
            BOOL res = PInvoke.OpenProcessToken(ProcessHandle, DesiredAccess, out TokenHandle);
            return !res ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : res;
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
            BOOL res = PInvoke.LookupPrivilegeValue(lpSystemName, lpName.ToString(), out lpLuid);
            return !res ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : res;
        }

        /// <summary>
        /// Retrieves the locally unique identifier (LUID) that represents the specified privilege on the local system.
        /// </summary>
        /// <param name="lpName">The name of the privilege to retrieve the LUID for. This must be a valid privilege name recognized by the
        /// system.</param>
        /// <param name="lpLuid">When this method returns, contains the LUID that corresponds to the specified privilege name.</param>
        /// <returns>A value that indicates whether the operation succeeded. Returns <see langword="true"/> if the privilege name
        /// was found and the LUID was retrieved successfully; otherwise, <see langword="false"/>.</returns>
        internal static BOOL LookupPrivilegeValue(SE_PRIVILEGE lpName, out LUID lpLuid)
        {
            return LookupPrivilegeValue(null, lpName, out lpLuid);
        }

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
            BOOL res = PInvoke.GetTokenInformation(TokenHandle, TokenInformationClass, TokenInformation, out ReturnLength);
            return !res && 0 != TokenInformation.Length ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : res;
        }

        /// <summary>
        /// Sets information for the specified access token using the provided information class and data.
        /// </summary>
        /// <remarks>If the operation fails, an exception is thrown based on the last Win32 error. Ensure
        /// that the token handle is valid and has the required access rights before calling this method.</remarks>
        /// <param name="TokenHandle">A handle to the access token to modify. The handle must have the TOKEN_SET_INFORMATION access right.</param>
        /// <param name="TokenInformationClass">The type of information to set for the token. Specify a value from the TOKEN_INFORMATION_CLASS enumeration.</param>
        /// <param name="TokenInformation">A read-only span of bytes containing the information to set. The structure and content depend on the
        /// specified TokenInformationClass.</param>
        /// <returns>A value indicating whether the operation succeeded. If the operation fails, an exception is thrown.</returns>
        internal static BOOL SetTokenInformation(SafeHandle TokenHandle, TOKEN_INFORMATION_CLASS TokenInformationClass, ReadOnlySpan<byte> TokenInformation)
        {
            BOOL res = PInvoke.SetTokenInformation(TokenHandle, TokenInformationClass, TokenInformation);
            return !res ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : res;
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
            return !res ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : res;
        }

        /// <summary>
        /// Adjusts the privileges of the specified access token according to the provided new state.
        /// </summary>
        /// <param name="TokenHandle">A handle to the access token whose privileges are to be modified. The handle must have
        /// TOKEN_ADJUST_PRIVILEGES access rights.</param>
        /// <param name="NewState">A structure that specifies the privileges to enable or disable for the access token.</param>
        /// <returns>A value that indicates whether the function succeeds. Returns <see langword="true"/> if the operation is
        /// successful; otherwise, <see langword="false"/>.</returns>
        internal static BOOL AdjustTokenPrivileges(SafeHandle TokenHandle, in TOKEN_PRIVILEGES NewState)
        {
            return AdjustTokenPrivileges(TokenHandle, false, in NewState, null, out _);
        }

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
            BOOL res = PInvoke.LookupPrivilegeName(lpSystemName, in lpLuid, lpName, ref cchName);
            return !res ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : res;
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
            BOOL res;
            try
            {
                lpEnvironment?.DangerousAddRef(ref lpEnvironmentAddRef);
                unsafe
                {
                    res = PInvoke.CreateProcessWithToken(hToken, dwLogonFlags, lpApplicationName, ref lpCommandLine, dwCreationFlags, lpEnvironment is not null ? (void*)lpEnvironment.DangerousGetHandle() : null, lpCurrentDirectory, in lpStartupInfo, out lpProcessInformation);
                }
            }
            finally
            {
                if (lpEnvironmentAddRef)
                {
                    lpEnvironment?.DangerousRelease();
                }
            }
            return !res ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : res;
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
            BOOL res;
            try
            {
                lpEnvironment?.DangerousAddRef(ref lpEnvironmentAddRef);
                unsafe
                {
                    res = PInvoke.CreateProcessAsUser(hToken, lpApplicationName, ref lpCommandLine, lpProcessAttributes, lpThreadAttributes, bInheritHandles, dwCreationFlags, lpEnvironment is not null ? (void*)lpEnvironment.DangerousGetHandle() : null, lpCurrentDirectory, in lpStartupInfo, out lpProcessInformation);
                }
            }
            finally
            {
                if (lpEnvironmentAddRef)
                {
                    lpEnvironment?.DangerousRelease();
                }
            }
            return !res ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : res;
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
                throw new ArgumentException("Required null terminator missing.", nameof(lpCommandLine));
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
            CloseServiceHandleSafeHandle handle = PInvoke.OpenSCManager(lpMachineName, lpDatabaseName, (uint)dwDesiredAccess);
            return handle.IsInvalid ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : handle;
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
        internal static CloseServiceHandleSafeHandle OpenSCManager(string? lpDatabaseName, SC_MANAGER_ACCESS dwDesiredAccess)
        {
            return OpenSCManager(null, lpDatabaseName, dwDesiredAccess);
        }

        /// <summary>
        /// Opens a handle to the Service Control Manager on the local computer with the specified access rights.
        /// </summary>
        /// <param name="dwDesiredAccess">A bitmask of access rights to request for the Service Control Manager. This value determines the operations
        /// that can be performed with the returned handle.</param>
        /// <returns>A safe handle to the Service Control Manager. The caller is responsible for closing the handle when it is no
        /// longer needed.</returns>
        internal static CloseServiceHandleSafeHandle OpenSCManager(SC_MANAGER_ACCESS dwDesiredAccess)
        {
            return OpenSCManager(null, null, dwDesiredAccess);
        }

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
            CloseServiceHandleSafeHandle handle = PInvoke.OpenService(hSCManager, lpServiceName, (uint)dwDesiredAccess);
            return handle.IsInvalid ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : handle;
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
            BOOL res = PInvoke.QueryServiceStatusEx(hService, InfoLevel, lpBuffer, out pcbBytesNeeded);
            return !res && (ExceptionUtilities.GetLastWin32Error() is WIN32_ERROR lastWin32Error) && (lastWin32Error != WIN32_ERROR.ERROR_INSUFFICIENT_BUFFER || lpBuffer.Length != 0)
                ? throw ExceptionUtilities.GetException(lastWin32Error)
                : res;
        }

        /// <summary>
        /// Initializes an Access Control List (ACL) in the provided buffer.
        /// </summary>
        /// <remarks>The caller is responsible for providing a buffer of sufficient size for the ACL header
        /// and any Access Control Entries (ACEs) that will be added. The buffer can be stack-allocated via
        /// <c>stackalloc</c> or a pinned <c>byte[]</c> array.</remarks>
        /// <param name="pAcl">A span of bytes representing the buffer to initialize as an ACL. The buffer must be
        /// large enough to contain the ACL header and any ACEs that will be added.</param>
        /// <param name="dwAclRevision">The revision level of the ACL. Use a value from the <see cref="ACE_REVISION"/> enumeration.</param>
        /// <returns><see langword="true"/> if the ACL was successfully initialized; otherwise, <see langword="false"/>.</returns>
        internal static BOOL InitializeAcl(Span<byte> pAcl, ACE_REVISION dwAclRevision)
        {
            BOOL res;
            unsafe
            {
                fixed (byte* pAclPtr = pAcl)
                {
                    res = PInvoke.InitializeAcl((ACL*)pAclPtr, (uint)pAcl.Length, dwAclRevision);
                }
            }
            return !res ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : res;
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
                if (OldAcl?.IsClosed == false)
                {
                    OldAcl.DangerousAddRef(ref OldAclAddRef);
                }
                WIN32_ERROR res;
                unsafe
                {
                    ACL* NewAclLocal = null;
                    fixed (EXPLICIT_ACCESS_W* pListOfExplicitEntriesLocal = pListOfExplicitEntries)
                    {
                        res = PInvoke.SetEntriesInAcl((uint)pListOfExplicitEntries.Length, pListOfExplicitEntriesLocal, OldAcl is not null ? (ACL*)OldAcl.DangerousGetHandle() : (ACL*)null, &NewAclLocal).ThrowOnFailure();
                    }
                    NewAcl = new((nint)NewAclLocal, true);
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
        internal static WIN32_ERROR SetEntriesInAcl(ReadOnlySpan<EXPLICIT_ACCESS_W> pListOfExplicitEntries, out LocalFreeSafeHandle NewAcl)
        {
            return SetEntriesInAcl(pListOfExplicitEntries, null, out NewAcl).ThrowOnFailure();
        }

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
                unsafe
                {
                    return PInvoke.SetSecurityInfo((HANDLE)handle.DangerousGetHandle(), ObjectType, SecurityInfo, psidOwner is not null ? new PSID(psidOwner.DangerousGetHandle()) : (PSID)null, psidGroup is not null ? new PSID(psidGroup.DangerousGetHandle()) : (PSID)null, pDacl is not null ? (ACL*)pDacl.DangerousGetHandle() : (ACL*)null, pSacl is not null ? (ACL*)pSacl.DangerousGetHandle() : (ACL*)null).ThrowOnFailure();
                }
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Maintainability", "CA1508:Avoid dead conditional code", Justification = "The null checks in this method are required.")]
        internal static WIN32_ERROR GetNamedSecurityInfo(string pObjectName, SE_OBJECT_TYPE ObjectType, OBJECT_SECURITY_INFORMATION SecurityInfo, out SafeNoReleaseHandle? ppsidOwner, out SafeNoReleaseHandle? ppsidGroup, out LocalFreeSafeHandle? ppDacl, out LocalFreeSafeHandle? ppSacl, out LocalFreeSafeHandle ppSecurityDescriptor)
        {
            WIN32_ERROR res;
            unsafe
            {
                PSID psidOwner = default, pSidGroup = default; ACL* pDacl = null, pSacl = null; PSECURITY_DESCRIPTOR pSecurityDescriptor = default;
                fixed (char* pObjectNameLocal = pObjectName)
                {
                    res = PInvoke.GetNamedSecurityInfo(pObjectNameLocal, ObjectType, SecurityInfo, &psidOwner, &pSidGroup, &pDacl, &pSacl, &pSecurityDescriptor).ThrowOnFailure();
                }
                if (pSecurityDescriptor == default)
                {
                    throw ExceptionUtilities.GetException(WIN32_ERROR.ERROR_INVALID_HANDLE);
                }
                ppsidOwner = psidOwner != default ? new((nint)psidOwner.Value) : null;
                ppsidGroup = pSidGroup != default ? new((nint)pSidGroup.Value) : null;
                ppDacl = pDacl is not null ? new((nint)pDacl, false) : null;
                ppSacl = pSacl is not null ? new((nint)pSacl, false) : null;
                ppSecurityDescriptor = new((nint)pSecurityDescriptor, true);
            }
            return res;
        }

        /// <summary>
        /// Sets the security information of a specified object, such as a file, directory, or registry key.
        /// </summary>
        /// <remarks>This method modifies the security descriptor of the specified object. The caller must
        /// ensure that the provided handles are valid and properly initialized. If any of the optional parameters are
        /// provided, their corresponding security descriptor components will be updated; otherwise, those components
        /// will remain unchanged. The caller is responsible for managing the lifetime of the provided handles and
        /// ensuring they are released appropriately.</remarks>
        /// <param name="pObjectName">The name of the object whose security information is being set. This cannot be <see langword="null"/>.</param>
        /// <param name="ObjectType">The type of the object, such as a file, directory, or registry key. This determines how the object name is
        /// interpreted.</param>
        /// <param name="SecurityInfo">A combination of flags that specify the components of the security descriptor to set. For example, owner,
        /// group, DACL, or SACL.</param>
        /// <param name="psidOwner">An optional handle to the security identifier (SID) for the object's owner. Pass <see langword="null"/> to
        /// leave the owner unchanged.</param>
        /// <param name="psidGroup">An optional handle to the SID for the object's primary group. Pass <see langword="null"/> to leave the group
        /// unchanged.</param>
        /// <param name="pDacl">An optional handle to the discretionary access control list (DACL) for the object. Pass <see
        /// langword="null"/> to leave the DACL unchanged.</param>
        /// <param name="pSacl">An optional handle to the system access control list (SACL) for the object. Pass <see langword="null"/> to
        /// leave the SACL unchanged.</param>
        /// <returns>A <see cref="WIN32_ERROR"/> value indicating the result of the operation. Returns <see
        /// cref="WIN32_ERROR.ERROR_SUCCESS"/> if the operation succeeds.</returns>
        internal static WIN32_ERROR SetNamedSecurityInfo(string pObjectName, SE_OBJECT_TYPE ObjectType, OBJECT_SECURITY_INFORMATION SecurityInfo, SafeNoReleaseHandle? psidOwner, SafeNoReleaseHandle? psidGroup, [Optional] LocalFreeSafeHandle? pDacl, [Optional] LocalFreeSafeHandle? pSacl)
        {
            bool psidOwnerAddRef = false;
            bool psidGroupAddRef = false;
            bool pDaclAddRef = false;
            bool pSaclAddRef = false;
            try
            {
                psidOwner?.DangerousAddRef(ref psidOwnerAddRef);
                psidGroup?.DangerousAddRef(ref psidGroupAddRef);
                pDacl?.DangerousAddRef(ref pDaclAddRef);
                pSacl?.DangerousAddRef(ref pSaclAddRef);
                unsafe
                {
                    fixed (char* pObjectNameLocal = pObjectName)
                    {
                        return PInvoke.SetNamedSecurityInfo(pObjectNameLocal, ObjectType, SecurityInfo, psidOwner is not null ? (PSID)psidOwner.DangerousGetHandle() : (PSID)null, psidGroup is not null ? (PSID)psidGroup.DangerousGetHandle() : (PSID)null, pDacl is not null ? (ACL*)pDacl.DangerousGetHandle() : (ACL*)null, pSacl is not null ? (ACL*)pSacl.DangerousGetHandle() : (ACL*)null).ThrowOnFailure();
                    }
                }
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
            }
        }

        /// <summary>
        /// Sets the security information of a specified object using stack-allocated or array-backed ACL buffers.
        /// </summary>
        /// <remarks>This overload allows passing stack-allocated or array-backed ACL buffers directly.
        /// The method handles pinning internally. Pass an empty span to leave the corresponding ACL unchanged.</remarks>
        /// <param name="pObjectName">The name of the object whose security information is being set.</param>
        /// <param name="ObjectType">The type of the object, such as a file, directory, or registry key.</param>
        /// <param name="SecurityInfo">A combination of flags that specify the components of the security descriptor to set.</param>
        /// <param name="psidOwner">An optional pointer to the owner SID. Pass <see langword="default"/> to leave unchanged.</param>
        /// <param name="psidGroup">An optional pointer to the group SID. Pass <see langword="default"/> to leave unchanged.</param>
        /// <param name="pDacl">A span representing the DACL buffer. Pass an empty span to leave unchanged.</param>
        /// <param name="pSacl">A span representing the SACL buffer. Pass an empty span to leave unchanged.</param>
        /// <returns>A <see cref="WIN32_ERROR"/> value indicating the result of the operation.</returns>
        internal static WIN32_ERROR SetNamedSecurityInfo(string pObjectName, SE_OBJECT_TYPE ObjectType, OBJECT_SECURITY_INFORMATION SecurityInfo, PSID psidOwner, PSID psidGroup, Span<byte> pDacl, Span<byte> pSacl)
        {
            unsafe
            {
                fixed (char* pObjectNameLocal = pObjectName)
                fixed (byte* pDaclPtr = pDacl)
                fixed (byte* pSaclPtr = pSacl)
                {
                    return PInvoke.SetNamedSecurityInfo(pObjectNameLocal, ObjectType, SecurityInfo, psidOwner, psidGroup, (ACL*)pDaclPtr, (ACL*)pSaclPtr).ThrowOnFailure();
                }
            }
        }

        /// <summary>
        /// Resets the security information for a specified object and its subobjects in the object tree.
        /// </summary>
        /// <remarks>This method modifies the security settings of the specified object and its subobjects
        /// based on the provided parameters.  It is the caller's responsibility to ensure that the provided handles are
        /// valid and properly disposed of after use.</remarks>
        /// <param name="pObjectName">The name of the object for which to reset security information. This must be a valid path or object name.</param>
        /// <param name="ObjectType">The type of the object, such as a file, registry key, or service. This determines how the object is treated
        /// during the operation.</param>
        /// <param name="SecurityInfo">Specifies the security information to reset, such as owner, group, DACL, or SACL.</param>
        /// <param name="pOwner">An optional handle to the new owner SID. If null, the owner is not changed.</param>
        /// <param name="pGroup">An optional handle to the new group SID. If null, the group is not changed.</param>
        /// <param name="pDacl">An optional handle to the new discretionary access control list (DACL). If null, the DACL is not changed.</param>
        /// <param name="pSacl">An optional handle to the new system access control list (SACL). If null, the SACL is not changed.</param>
        /// <param name="KeepExplicit">A value indicating whether explicit access control entries (ACEs) in the DACL or SACL should be preserved. 
        /// Specify <see langword="true"/> to keep explicit ACEs; otherwise, <see langword="false"/>.</param>
        /// <param name="fnProgress">A callback function that is invoked to report progress during the operation. This can be used to monitor or
        /// cancel the operation.</param>
        /// <param name="ProgressInvokeSetting">Specifies how the progress function is invoked, such as on every object or only on errors.</param>
        /// <param name="Args">An optional pointer to additional arguments passed to the progress callback function.</param>
        /// <returns>A <see cref="WIN32_ERROR"/> value indicating the result of the operation.  Returns <see
        /// cref="WIN32_ERROR.ERROR_SUCCESS"/> if the operation completes successfully.</returns>
        internal static WIN32_ERROR TreeResetNamedSecurityInfo(string pObjectName, SE_OBJECT_TYPE ObjectType, OBJECT_SECURITY_INFORMATION SecurityInfo, SafeNoReleaseHandle? pOwner, SafeNoReleaseHandle? pGroup, [Optional] LocalFreeSafeHandle? pDacl, [Optional] LocalFreeSafeHandle? pSacl, BOOL KeepExplicit, FN_PROGRESS? fnProgress, PROG_INVOKE_SETTING ProgressInvokeSetting, [Optional] nint? Args)
        {
            bool pOwnerAddRef = false;
            bool pGroupAddRef = false;
            bool pDaclAddRef = false;
            bool pSaclAddRef = false;
            try
            {
                pOwner?.DangerousAddRef(ref pOwnerAddRef);
                pGroup?.DangerousAddRef(ref pGroupAddRef);
                pDacl?.DangerousAddRef(ref pDaclAddRef);
                pSacl?.DangerousAddRef(ref pSaclAddRef);
                unsafe
                {
                    fixed (char* pObjectNameLocal = pObjectName)
                    {
                        return PInvoke.TreeResetNamedSecurityInfo(pObjectNameLocal, ObjectType, SecurityInfo, pOwner is not null ? (PSID)pOwner.DangerousGetHandle() : (PSID)null, pGroup is not null ? (PSID)pGroup.DangerousGetHandle() : (PSID)null, pDacl is not null ? (ACL*)pDacl.DangerousGetHandle() : (ACL*)null, pSacl is not null ? (ACL*)pSacl.DangerousGetHandle() : (ACL*)null, KeepExplicit, fnProgress, ProgressInvokeSetting, Args is not null ? (void*)Args.Value : null).ThrowOnFailure();
                    }
                }
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
                if (pGroupAddRef)
                {
                    pGroup?.DangerousRelease();
                }
                if (pOwnerAddRef)
                {
                    pOwner?.DangerousRelease();
                }
            }
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Enforcing this rule just makes a mess.")]
        internal static BOOL AuthzInitializeResourceManager(AUTHZ_RESOURCE_MANAGER_FLAGS Flags, PFN_AUTHZ_DYNAMIC_ACCESS_CHECK? pfnDynamicAccessCheck, PFN_AUTHZ_COMPUTE_DYNAMIC_GROUPS? pfnComputeDynamicGroups, PFN_AUTHZ_FREE_DYNAMIC_GROUPS? pfnFreeDynamicGroups, string szResourceManagerName, out AuthzFreeResourceManagerSafeHandle phAuthzResourceManager)
        {
            BOOL res = PInvoke.AuthzInitializeResourceManager((uint)Flags, pfnDynamicAccessCheck, pfnComputeDynamicGroups, pfnFreeDynamicGroups, szResourceManagerName, out phAuthzResourceManager);
            if (!res)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            if (phAuthzResourceManager.IsInvalid)
            {
                throw ExceptionUtilities.GetException(WIN32_ERROR.ERROR_INVALID_HANDLE);
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Enforcing this rule just makes a mess.")]
        internal static BOOL AuthzInitializeContextFromSid(AUTHZ_CONTEXT_FLAGS Flags, SafeHandle UserSid, SafeHandle hAuthzResourceManager, long? pExpirationTime, in LUID Identifier, nint DynamicGroupArgs, out AuthzFreeContextSafeHandle phAuthzClientContext)
        {
            bool UserSidAddRef = false;
            BOOL res;
            try
            {
                UserSid.DangerousAddRef(ref UserSidAddRef);
                unsafe
                {
                    res = PInvoke.AuthzInitializeContextFromSid((uint)Flags, new(UserSid.DangerousGetHandle()), hAuthzResourceManager, pExpirationTime, Identifier, (void*)DynamicGroupArgs, out phAuthzClientContext);
                }
            }
            finally
            {
                if (UserSidAddRef)
                {
                    UserSid.DangerousRelease();
                }
            }
            if (!res)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            if (phAuthzClientContext.IsInvalid)
            {
                throw ExceptionUtilities.GetException(WIN32_ERROR.ERROR_INVALID_HANDLE);
            }
            return res;
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Enforcing this rule just makes a mess.")]
        internal static BOOL AuthzInitializeContextFromToken(AUTHZ_CONTEXT_FLAGS Flags, SafeHandle TokenHandle, SafeHandle hAuthzResourceManager, long? pExpirationTime, in LUID Identifier, nint DynamicGroupArgs, out AuthzFreeContextSafeHandle phAuthzClientContext)
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
                throw ExceptionUtilities.GetException(WIN32_ERROR.ERROR_INVALID_HANDLE);
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Enforcing this rule just makes a mess.")]
        internal static BOOL AuthzAccessCheck(AUTHZ_ACCESS_CHECK_FLAGS Flags, SafeHandle hAuthzClientContext, in AUTHZ_ACCESS_REQUEST pRequest, SafeHandle? hAuditEvent, LocalFreeSafeHandle pSecurityDescriptor, ReadOnlySpan<PSECURITY_DESCRIPTOR> OptionalSecurityDescriptorArray, ref AUTHZ_ACCESS_REPLY pReply, out AuthzFreeHandleSafeHandle phAccessCheckResults)
        {
            bool pSecurityDescriptorAddRef = false;
            BOOL res;
            try
            {
                pSecurityDescriptor.DangerousAddRef(ref pSecurityDescriptorAddRef);
                unsafe
                {
                    res = PInvoke.AuthzAccessCheck(Flags, hAuthzClientContext, in pRequest, hAuditEvent, (PSECURITY_DESCRIPTOR)pSecurityDescriptor.DangerousGetHandle(), OptionalSecurityDescriptorArray, ref pReply, out phAccessCheckResults);
                }
            }
            finally
            {
                if (pSecurityDescriptorAddRef)
                {
                    pSecurityDescriptor.DangerousRelease();
                }
            }
            if (!res)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            if (phAccessCheckResults.IsInvalid)
            {
                throw ExceptionUtilities.GetException(WIN32_ERROR.ERROR_INVALID_HANDLE);
            }
            return res;
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
            return PInvoke.RegRenameKey(hKey, lpSubKeyName, lpNewKeyName).ThrowOnFailure();
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
            return PInvoke.LsaOpenPolicy(SystemName, in ObjectAttributes, (uint)DesiredAccess, out PolicyHandle).ThrowOnFailure();
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
                res = PInvoke.LsaQueryInformationPolicy(PolicyHandle, InformationClass, out void* BufferLocal).ThrowOnFailure();
                Buffer = new((nint)BufferLocal, true);
            }
            return res;
        }

        /// <summary>
        /// Displays a task dialog, a modal dialog box that provides a flexible and customizable user interface for presenting information and receiving user input.
        /// </summary>
        /// <remarks>This method wraps the native TaskDialog API, providing a managed interface for displaying a task dialog. The dialog is modal and blocks the calling thread until the user closes it.</remarks>
        /// <param name="hwndOwner">A handle to the owner window for the task dialog. This can be <see langword="null"/> if the dialog has no owner.</param>
        /// <param name="hInstance">A handle to the module instance that contains the dialog box template. This can be <see langword="null"/> if not applicable.</param>
        /// <param name="pszWindowTitle">The title of the task dialog window. This can be <see langword="null"/> to use the default title.</param>
        /// <param name="pszMainInstruction">The main instruction text displayed prominently in the task dialog. This can be <see langword="null"/> if no main instruction is needed.</param>
        /// <param name="pszContent">The additional content text displayed in the task dialog. This can be <see langword="null"/> if no additional content is needed.</param>
        /// <param name="dwCommonButtons">A combination of <see cref="TASKDIALOG_COMMON_BUTTON_FLAGS"/> values that specify the common buttons to display in the task dialog.</param>
        /// <param name="pszIcon">The resource identifier or name of the icon to display in the task dialog. This can be <see langword="null"/> if no icon is needed.</param>
        /// <returns>A <see cref="MESSAGEBOX_RESULT"/> value indicating the result of the task dialog operation.</returns>
        internal static MESSAGEBOX_RESULT TaskDialog(HWND hwndOwner, HINSTANCE hInstance, string? pszWindowTitle, string? pszMainInstruction, string? pszContent, TASKDIALOG_COMMON_BUTTON_FLAGS dwCommonButtons, TASKDIALOG_ICON pszIcon)
        {
            int pnButtonLocal = 0;
            HRESULT res;
            unsafe
            {
                fixed (char* pszWindowTitleLocal = pszWindowTitle, pszMainInstructionLocal = pszMainInstruction, pszContentLocal = pszContent)
                {
                    res = PInvoke.TaskDialog(hwndOwner, hInstance, pszWindowTitleLocal, pszMainInstructionLocal, pszContentLocal, dwCommonButtons, pszIcon.ToPCWSTR(), &pnButtonLocal);
                }
            }
            return res != HRESULT.S_OK ? throw ExceptionUtilities.GetException(res) : (MESSAGEBOX_RESULT)pnButtonLocal;
        }

        /// <summary>
        /// Determines whether the Out-Of-Box Experience (OOBE) has been completed on the system.
        /// </summary>
        /// <param name="isOOBEComplete">When this method returns, contains a value that indicates whether OOBE is complete. Contains <see
        /// langword="true"/> if OOBE is complete; otherwise, <see langword="false"/>. This parameter is passed
        /// uninitialized.</param>
        /// <returns>A value that indicates whether the operation succeeded. Returns <see langword="true"/> if the call was
        /// successful; otherwise, <see langword="false"/>.</returns>
        internal static BOOL OOBEComplete(out BOOL isOOBEComplete)
        {
            BOOL res = PInvoke.OOBEComplete(out isOOBEComplete);
            return !res ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : res;
        }

        /// <summary>
        /// Loads the specified module into the address space of the calling process with the given loading options.
        /// </summary>
        /// <remarks>This method throws an exception if the module cannot be loaded. The returned handle
        /// must be released to avoid resource leaks.</remarks>
        /// <param name="lpLibFileName">The name or path of the module to load. This can be a library file name or a full path. Cannot be null or
        /// empty.</param>
        /// <param name="dwFlags">A combination of flags that control how the module is loaded. These flags determine aspects such as search
        /// path behavior and dependency resolution.</param>
        /// <returns>A safe handle representing the loaded module. The caller is responsible for releasing the handle when it is
        /// no longer needed.</returns>
        internal static FreeLibrarySafeHandle LoadLibraryEx(string lpLibFileName, LOAD_LIBRARY_FLAGS dwFlags)
        {
            FreeLibrarySafeHandle res = PInvoke.LoadLibraryEx(lpLibFileName, dwFlags);
            return res.IsInvalid ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : res;
        }

        /// <summary>
        /// Retrieves the address of an exported function or variable from the specified dynamic-link library (DLL)
        /// module.
        /// </summary>
        /// <remarks>This method throws an exception if the specified function or variable cannot be
        /// found. The returned address can be used to invoke the function or access the variable. The caller is
        /// responsible for ensuring that the signature of the function or variable matches the expected type.</remarks>
        /// <param name="hModule">A handle to the DLL module that contains the function or variable. This handle must have been obtained by
        /// loading the module with a method such as LoadLibrary. Cannot be null.</param>
        /// <param name="lpProcName">The name of the function or variable to retrieve, or the ordinal value as a string. Cannot be null or empty.</param>
        /// <returns>A FARPROC representing the address of the specified function or variable.</returns>
        internal static FARPROC GetProcAddress(SafeHandle hModule, string lpProcName)
        {
            FARPROC res = PInvoke.GetProcAddress(hModule, lpProcName);
            return res.IsNull ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : res;
        }

        /// <summary>
        /// Retrieves the names of all sections in the specified initialization file.
        /// </summary>
        /// <remarks>If the buffer specified by lpReturnedString is too small to hold all section names,
        /// an exception is thrown. The section names are returned as a sequence of null-terminated strings, terminated
        /// by an additional null character.</remarks>
        /// <param name="lpReturnedString">A span of characters that receives the section names, separated by null characters. The buffer must be large
        /// enough to hold all section names and a final null terminator.</param>
        /// <param name="lpFileName">The full path to the initialization (.ini) file from which to retrieve section names. Cannot be null.</param>
        /// <returns>The number of characters copied to lpReturnedString, not including the final null character.</returns>
        internal static uint GetPrivateProfileSectionNames(Span<char> lpReturnedString, string lpFileName)
        {
            uint res = PInvoke.GetPrivateProfileSectionNames(lpReturnedString, lpFileName);
            return res == lpReturnedString.Length - 2
                ? throw ExceptionUtilities.GetException(WIN32_ERROR.ERROR_INSUFFICIENT_BUFFER)
                : res;
        }

        /// <summary>
        /// Retrieves all key name and value pairs for the specified section from the given initialization file.
        /// </summary>
        /// <remarks>If the buffer is too small to hold all the data, an exception is thrown. The returned
        /// data consists of key-value pairs separated by null characters, with a double null character marking the end
        /// of the data.</remarks>
        /// <param name="lpAppName">The name of the section in the initialization file whose key-value pairs are to be retrieved. Cannot be
        /// null.</param>
        /// <param name="lpReturnedString">A buffer that receives the key name and value pairs, formatted as a series of null-terminated strings. The
        /// buffer must be large enough to hold the data, including the final null terminator.</param>
        /// <param name="lpFileName">The name of the initialization file. Cannot be null.</param>
        /// <returns>The number of characters copied to the buffer, not including the terminating null character.</returns>
        internal static uint GetPrivateProfileSection(string lpAppName, Span<char> lpReturnedString, string lpFileName)
        {
            uint res = PInvoke.GetPrivateProfileSection(lpAppName, lpReturnedString, lpFileName);
            return res == lpReturnedString.Length - 2
                ? throw ExceptionUtilities.GetException(WIN32_ERROR.ERROR_INSUFFICIENT_BUFFER)
                : res;
        }

        /// <summary>
        /// Retrieves a string value from the specified section and key in an initialization (INI) file.
        /// </summary>
        /// <remarks>If the buffer is too small to hold the result, an exception is thrown. This method
        /// throws an exception if a Windows error occurs during the operation.</remarks>
        /// <param name="lpAppName">The name of the section containing the key. Cannot be null.</param>
        /// <param name="lpKeyName">The name of the key whose value is to be retrieved. If null, all key names in the specified section are
        /// returned.</param>
        /// <param name="lpDefault">The default string to return if the key is not found. If null, an empty string is used as the default.</param>
        /// <param name="lpReturnedString">A buffer that receives the retrieved string. The buffer must be large enough to hold the result, including
        /// the terminating null character.</param>
        /// <param name="lpFileName">The full path to the initialization file. Cannot be null.</param>
        /// <returns>The number of characters copied to the buffer, not including the terminating null character.</returns>
        internal static uint GetPrivateProfileString(string lpAppName, string? lpKeyName, string? lpDefault, Span<char> lpReturnedString, string lpFileName)
        {
            uint res = PInvoke.GetPrivateProfileString(lpAppName, lpKeyName, lpDefault, lpReturnedString, lpFileName);
            if (res == 0 && (ExceptionUtilities.GetLastWin32Error() is WIN32_ERROR lastWin32Error) && lastWin32Error != WIN32_ERROR.NO_ERROR)
            {
                throw ExceptionUtilities.GetException(lastWin32Error);
            }
            else if (res == lpReturnedString.Length - 1 || res == lpReturnedString.Length - 2)
            {
                throw ExceptionUtilities.GetException(WIN32_ERROR.ERROR_INSUFFICIENT_BUFFER);
            }
            return res;
        }

        /// <summary>
        /// Writes a section to the specified initialization file (INI file), replacing the existing section with the
        /// provided key-value pairs.
        /// </summary>
        /// <remarks>If <paramref name="lpString"/> is null, the specified section is removed from the
        /// file. The file must be accessible for writing. This method throws an exception if the underlying Windows API
        /// call fails.</remarks>
        /// <param name="lpAppName">The name of the section to be written to the initialization file. Cannot be null or empty.</param>
        /// <param name="lpString">A string containing the key-value pairs to write to the section, formatted as a sequence of null-terminated
        /// strings ending with two null characters. If null, the section is deleted.</param>
        /// <param name="lpFileName">The full path to the initialization file. Cannot be null or empty.</param>
        /// <returns>A value indicating whether the operation succeeded. Returns <see langword="true"/> if the section was
        /// written successfully; otherwise, <see langword="false"/>.</returns>
        internal static BOOL WritePrivateProfileSection(string lpAppName, string? lpString, string lpFileName)
        {
            BOOL res = PInvoke.WritePrivateProfileSection(lpAppName, lpString, lpFileName);
            return !res ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : res;
        }

        /// <summary>
        /// Writes a string value to the specified section and key in an initialization (INI) file.
        /// </summary>
        /// <remarks>This method throws an exception if the underlying Windows API call fails. The method
        /// is intended for use with legacy INI files and may not be suitable for new applications. The file specified
        /// by lpFileName must exist and be accessible for writing.</remarks>
        /// <param name="lpAppName">The name of the section to which the string will be written. This value cannot be null.</param>
        /// <param name="lpKeyName">The name of the key to be associated with the string. If this parameter is null, the entire section
        /// specified by lpAppName is deleted.</param>
        /// <param name="lpString">The string to write to the specified key. If this parameter is null, the key specified by lpKeyName is
        /// deleted.</param>
        /// <param name="lpFileName">The full path to the initialization file. This value cannot be null.</param>
        /// <returns>true if the operation succeeds; otherwise, false.</returns>
        internal static BOOL WritePrivateProfileString(string lpAppName, string? lpKeyName, string? lpString, string lpFileName)
        {
            BOOL res = PInvoke.WritePrivateProfileString(lpAppName, lpKeyName, lpString, lpFileName);
            return !res ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : res;
        }

        /// <summary>
        /// Creates a new I/O completion port or associates a file handle with an existing I/O completion port.
        /// </summary>
        /// <remarks>This method wraps the native CreateIoCompletionPort Windows API. If the operation
        /// fails, a Win32 exception is thrown. The returned handle should be closed when no longer needed to avoid
        /// resource leaks.</remarks>
        /// <param name="FileHandle">The file handle to associate with the I/O completion port. Can be a file, socket, or device handle. If null,
        /// a new completion port is created.</param>
        /// <param name="ExistingCompletionPort">An existing I/O completion port handle to associate with the file handle, or null to create a new completion
        /// port.</param>
        /// <param name="CompletionKey">A value to be returned through the completion port with each I/O completion packet for the specified file
        /// handle. Used to identify the source of the I/O operation.</param>
        /// <param name="NumberOfConcurrentThreads">The maximum number of threads that the operating system can allow to concurrently process I/O completion
        /// packets for the port. Must be greater than zero when creating a new port; ignored when associating with an
        /// existing port.</param>
        /// <returns>A SafeFileHandle representing the I/O completion port. The handle is valid and must be released by the
        /// caller.</returns>
        internal static SafeFileHandle CreateIoCompletionPort(SafeHandle FileHandle, SafeHandle? ExistingCompletionPort, nuint CompletionKey, uint NumberOfConcurrentThreads)
        {
            SafeFileHandle res = PInvoke.CreateIoCompletionPort(FileHandle, ExistingCompletionPort, CompletionKey, NumberOfConcurrentThreads);
            return res.IsInvalid ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : res;
        }

        /// <summary>
        /// Creates or associates an I/O completion port with a specified file handle, allowing asynchronous I/O
        /// operations to be managed and completed efficiently.
        /// </summary>
        /// <remarks>This method is intended for advanced scenarios involving asynchronous I/O on Windows
        /// platforms. The caller is responsible for managing the lifetime of the returned handle. Improper use may lead
        /// to resource leaks or undefined behavior.</remarks>
        /// <param name="FileHandle">The handle to a file, socket, or device to associate with the I/O completion port. If this parameter is set
        /// to a special value indicating no file association, a new completion port is created.</param>
        /// <param name="ExistingCompletionPort">An existing I/O completion port to associate with the file handle, or null to create a new completion port.</param>
        /// <param name="CompletionKey">A value to be returned through the completion port with each I/O completion packet for the specified file
        /// handle. This value can be used to identify the source of the I/O operation.</param>
        /// <param name="NumberOfConcurrentThreads">The maximum number of threads that the operating system can allow to concurrently process I/O completion
        /// packets for the port. Must be greater than zero.</param>
        /// <returns>A SafeFileHandle representing the I/O completion port. The handle can be used to post and retrieve I/O
        /// completion packets.</returns>
        internal static SafeFileHandle CreateIoCompletionPort(HANDLE FileHandle, SafeHandle? ExistingCompletionPort, nuint CompletionKey, uint NumberOfConcurrentThreads)
        {
            using SafeFileHandle safeFileHandle = new(FileHandle, false);
            return CreateIoCompletionPort(safeFileHandle, ExistingCompletionPort, CompletionKey, NumberOfConcurrentThreads);
        }

        /// <summary>
        /// Creates a new Windows job object that can be used to manage and control a group of processes as a single
        /// unit.
        /// </summary>
        /// <remarks>The caller must have appropriate permissions to create job objects. If the job object
        /// cannot be created due to a system error, an exception is thrown containing the relevant error
        /// information.</remarks>
        /// <param name="lpJobAttributes">An optional SECURITY_ATTRIBUTES structure that specifies the security descriptor for the job object and
        /// determines whether child processes can inherit the returned handle. Specify null to use default security
        /// settings.</param>
        /// <param name="lpName">An optional name for the job object. Specify null to create an unnamed job object.</param>
        /// <returns>A SafeFileHandle representing the newly created job object. If the creation fails, an exception is thrown.</returns>
        internal static SafeFileHandle CreateJobObject(SECURITY_ATTRIBUTES? lpJobAttributes, string? lpName)
        {
            SafeFileHandle res = PInvoke.CreateJobObject(lpJobAttributes, lpName);
            return res.IsInvalid ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : res;
        }

        /// <summary>
        /// Sets limits or configuration information for the specified job object.
        /// </summary>
        /// <remarks>This method throws an exception if the operation fails. The caller is responsible for
        /// ensuring that the buffer passed to lpJobObjectInformation is properly initialized and matches the expected
        /// structure for the specified information class.</remarks>
        /// <param name="hJob">A handle to the job object to be modified. This handle must have the JOB_OBJECT_SET_ATTRIBUTES access right.</param>
        /// <param name="JobObjectInformationClass">A value that specifies the type of information to set for the job object. This determines the structure
        /// expected in the information buffer.</param>
        /// <param name="lpJobObjectInformation">A pointer to a buffer that contains the information to be set. The structure and contents of this buffer
        /// depend on the value of the JobObjectInformationClass parameter.</param>
        /// <param name="cbJobObjectInformationLength">The size, in bytes, of the information buffer pointed to by lpJobObjectInformation.</param>
        /// <returns>true if the information was set successfully; otherwise, false.</returns>
        private static BOOL SetInformationJobObject(SafeHandle hJob, JOBOBJECTINFOCLASS JobObjectInformationClass, nint lpJobObjectInformation, uint cbJobObjectInformationLength)
        {
            bool hJobAddRef = false;
            BOOL res;
            try
            {
                hJob.DangerousAddRef(ref hJobAddRef);
                unsafe
                {
                    res = PInvoke.SetInformationJobObject((HANDLE)hJob.DangerousGetHandle(), JobObjectInformationClass, (void*)lpJobObjectInformation, cbJobObjectInformationLength);
                }
            }
            finally
            {
                if (hJobAddRef)
                {
                    hJob.DangerousRelease();
                }
            }
            return !res ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : res;
        }

        /// <summary>
        /// Associates a completion port with the specified job object.
        /// </summary>
        /// <remarks>This method is typically used to receive asynchronous notifications about job object
        /// events through an I/O completion port. The job object must not already be associated with a completion
        /// port.</remarks>
        /// <param name="hJob">A handle to the job object to associate with a completion port. This handle must have the
        /// JOB_OBJECT_SET_ATTRIBUTES access right.</param>
        /// <param name="lpJobObjectInformation">A structure that specifies the completion port and completion key to associate with the job object.</param>
        /// <returns>A nonzero value if the function succeeds; otherwise, zero. To get extended error information, call
        /// GetLastError.</returns>
        internal static BOOL SetInformationJobObject(SafeHandle hJob, in JOBOBJECT_ASSOCIATE_COMPLETION_PORT lpJobObjectInformation)
        {
            unsafe
            {
                fixed (JOBOBJECT_ASSOCIATE_COMPLETION_PORT* pInfo = &lpJobObjectInformation)
                {
                    return SetInformationJobObject(hJob, JOBOBJECTINFOCLASS.JobObjectAssociateCompletionPortInformation, (nint)pInfo, (uint)sizeof(JOBOBJECT_ASSOCIATE_COMPLETION_PORT));
                }
            }
        }

        /// <summary>
        /// Sets extended limit information for the specified job object.
        /// </summary>
        /// <remarks>Use this method to configure resource limits and other extended settings for a job
        /// object. The job object must have been created previously, and the caller must have appropriate permissions.
        /// For more information about job objects and their limits, see the Windows API documentation.</remarks>
        /// <param name="hJob">A handle to the job object to be updated. This handle must have the JOB_OBJECT_SET_ATTRIBUTES access right.</param>
        /// <param name="lpJobObjectInformation">A structure that contains the extended limit information to set for the job object.</param>
        /// <returns>A nonzero value if the function succeeds; otherwise, zero.</returns>
        internal static BOOL SetInformationJobObject(SafeHandle hJob, in JOBOBJECT_EXTENDED_LIMIT_INFORMATION lpJobObjectInformation)
        {
            unsafe
            {
                fixed (JOBOBJECT_EXTENDED_LIMIT_INFORMATION* pInfo = &lpJobObjectInformation)
                {
                    return SetInformationJobObject(hJob, JOBOBJECTINFOCLASS.JobObjectExtendedLimitInformation, (nint)pInfo, (uint)sizeof(JOBOBJECT_EXTENDED_LIMIT_INFORMATION));
                }
            }
        }

        /// <summary>
        /// Creates a new process and its primary thread using the specified application name, command line, security
        /// attributes, environment block, and startup information.
        /// </summary>
        /// <remarks>This method wraps the native CreateProcess Windows API and throws an exception if
        /// process creation fails. The caller is responsible for closing handles in the returned PROCESS_INFORMATION
        /// structure when they are no longer needed.</remarks>
        /// <param name="lpApplicationName">The name of the module to execute. If this parameter is null, the module name must be the first white
        /// space–delimited token in lpCommandLine.</param>
        /// <param name="lpCommandLine">A reference to a span containing the command line to execute. The string can include the application name
        /// and any arguments.</param>
        /// <param name="lpProcessAttributes">A SECURITY_ATTRIBUTES structure that determines whether the returned process handle can be inherited by
        /// child processes. Can be null to use default security.</param>
        /// <param name="lpThreadAttributes">A SECURITY_ATTRIBUTES structure that determines whether the returned thread handle can be inherited by child
        /// processes. Can be null to use default security.</param>
        /// <param name="bInheritHandles">true if each inheritable handle in the calling process is inherited by the new process; otherwise, false.</param>
        /// <param name="dwCreationFlags">A combination of PROCESS_CREATION_FLAGS values that control the priority class and creation of the process.</param>
        /// <param name="lpEnvironment">A SafeEnvironmentBlockHandle representing the environment block for the new process. Cannot be null or
        /// closed.</param>
        /// <param name="lpCurrentDirectory">The full path to the current directory for the new process. If null, the new process will have the same
        /// current drive and directory as the calling process.</param>
        /// <param name="lpStartupInfo">A reference to a STARTUPINFOW structure specifying the window station, desktop, standard handles, and
        /// appearance of the main window for the new process.</param>
        /// <param name="lpProcessInformation">When this method returns, contains a PROCESS_INFORMATION structure with information about the newly created
        /// process and its primary thread.</param>
        /// <returns>true if the process is created successfully; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown if lpEnvironment is null or has been closed.</exception>
        internal static BOOL CreateProcess(string? lpApplicationName, ref Span<char> lpCommandLine, in SECURITY_ATTRIBUTES? lpProcessAttributes, in SECURITY_ATTRIBUTES? lpThreadAttributes, in BOOL bInheritHandles, PROCESS_CREATION_FLAGS dwCreationFlags, SafeEnvironmentBlockHandle? lpEnvironment, string? lpCurrentDirectory, in STARTUPINFOW lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation)
        {
            bool lpEnvironmentAddRef = false;
            BOOL res;
            try
            {
                lpEnvironment?.DangerousAddRef(ref lpEnvironmentAddRef);
                unsafe
                {
                    res = PInvoke.CreateProcess(lpApplicationName, ref lpCommandLine, lpProcessAttributes, lpThreadAttributes, bInheritHandles, dwCreationFlags, lpEnvironment is not null ? (void*)lpEnvironment.DangerousGetHandle() : null, lpCurrentDirectory, in lpStartupInfo, out lpProcessInformation);
                }
            }
            finally
            {
                if (lpEnvironmentAddRef)
                {
                    lpEnvironment?.DangerousRelease();
                }
            }
            return !res ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : res;
        }

        /// <summary>
        /// Creates a new process and its primary thread using the specified application name, command line, security
        /// attributes, environment, and startup information.
        /// </summary>
        /// <remarks>This method is a low-level wrapper for the Windows CreateProcess API and requires
        /// careful management of memory and handles. The caller is responsible for ensuring that all parameters meet
        /// the requirements of the underlying Windows API. On failure, a Win32 exception is thrown with details from
        /// the last error code.</remarks>
        /// <param name="lpApplicationName">The name of the module to execute. If this parameter is null, the module name must be the first white
        /// space–delimited token in <paramref name="lpCommandLine"/>.</param>
        /// <param name="lpCommandLine">A span containing the command line to execute, including the application name and any arguments. The span
        /// must be null-terminated.</param>
        /// <param name="lpProcessAttributes">A reference to a <see cref="SECURITY_ATTRIBUTES"/> structure that determines whether the returned process
        /// handle can be inherited by child processes. If null, the handle cannot be inherited.</param>
        /// <param name="lpThreadAttributes">A reference to a <see cref="SECURITY_ATTRIBUTES"/> structure that determines whether the returned thread
        /// handle can be inherited by child processes. If null, the handle cannot be inherited.</param>
        /// <param name="bInheritHandles">Indicates whether each handle in the calling process is inherited by the new process. Specify <see
        /// langword="true"/> to inherit handles; otherwise, <see langword="false"/>.</param>
        /// <param name="dwCreationFlags">A set of flags that control the priority class and creation of the process. This parameter can be a
        /// combination of <see cref="PROCESS_CREATION_FLAGS"/> values.</param>
        /// <param name="lpEnvironment">A handle to an environment block for the new process. If null, the new process uses the environment of the
        /// calling process.</param>
        /// <param name="lpCurrentDirectory">The full path to the current directory for the new process. If null, the new process uses the current
        /// directory of the calling process.</param>
        /// <param name="lpStartupInfoEx">A reference to a <see cref="STARTUPINFOEXW"/> structure that specifies the window station, desktop, standard
        /// handles, and attributes for the new process.</param>
        /// <param name="lpProcessInformation">When this method returns, contains a <see cref="PROCESS_INFORMATION"/> structure with information about the
        /// newly created process and its primary thread.</param>
        /// <returns>A <see cref="BOOL"/> value that is <see langword="true"/> if the process is created successfully; otherwise,
        /// <see langword="false"/>.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="lpCommandLine"/> is not empty and does not contain a null terminator.</exception>
        internal static BOOL CreateProcess(string? lpApplicationName, ref Span<char> lpCommandLine, in SECURITY_ATTRIBUTES? lpProcessAttributes, in SECURITY_ATTRIBUTES? lpThreadAttributes, in BOOL bInheritHandles, PROCESS_CREATION_FLAGS dwCreationFlags, SafeEnvironmentBlockHandle? lpEnvironment, string? lpCurrentDirectory, in STARTUPINFOEXW lpStartupInfoEx, out PROCESS_INFORMATION lpProcessInformation)
        {
            if (lpCommandLine != Span<char>.Empty && lpCommandLine.LastIndexOf('\0') == -1)
            {
                throw new ArgumentException("Required null terminator missing.", nameof(lpCommandLine));
            }
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
                        lpEnvironment?.DangerousAddRef(ref lpEnvironmentAddRef);
                        res = PInvoke.CreateProcess(lpApplicationNameLocal, plpCommandLine, lpProcessAttributes.HasValue ? &lpProcessAttributesLocal : null, lpThreadAttributes.HasValue ? &lpThreadAttributesLocal : null, bInheritHandles, dwCreationFlags, lpEnvironment is not null ? (void*)lpEnvironment.DangerousGetHandle() : null, lpCurrentDirectoryLocal, (STARTUPINFOW*)lpStartupInfoExLocal, lpProcessInformationLocal);
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
            }
        }

        /// <summary>
        /// Associates a process with a job object, enabling the job object to manage and limit the process according to
        /// its configuration.
        /// </summary>
        /// <remarks>Once a process is assigned to a job object, it cannot be assigned to another job
        /// object. Attempting to assign a process that is already associated with a job object will fail.</remarks>
        /// <param name="hJob">A handle to the job object to which the process will be assigned. This handle must have
        /// JOB_OBJECT_ASSIGN_PROCESS access rights and must not be null.</param>
        /// <param name="hProcess">A handle to the process to assign to the job object. This handle must have PROCESS_SET_QUOTA and
        /// PROCESS_TERMINATE access rights and must not be null.</param>
        /// <returns>A value indicating whether the process was successfully assigned to the job object. Returns <see
        /// langword="true"/> if the operation succeeds; otherwise, <see langword="false"/>.</returns>
        internal static BOOL AssignProcessToJobObject(SafeHandle hJob, SafeHandle hProcess)
        {
            BOOL res = PInvoke.AssignProcessToJobObject(hJob, hProcess);
            return !res ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : res;
        }

        /// <summary>
        /// Resumes a thread that has been suspended, allowing it to continue execution.
        /// </summary>
        /// <param name="hThread">A handle to the thread to be resumed. The handle must have the THREAD_SUSPEND_RESUME access right and must
        /// not be closed or invalid.</param>
        /// <returns>The thread's previous suspend count. If the return value is zero, the thread was not previously suspended.</returns>
        internal static uint ResumeThread(SafeHandle hThread)
        {
            uint res = PInvoke.ResumeThread(hThread);
            return res == uint.MaxValue ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : res;
        }

        /// <summary>
        /// Retrieves the results of an I/O operation that has completed on the specified I/O completion port.
        /// </summary>
        /// <remarks>If the operation fails, an exception is thrown containing the last Win32 error. This
        /// method is typically used in advanced scenarios involving asynchronous I/O and completion ports.</remarks>
        /// <param name="CompletionPort">A handle to the I/O completion port from which to dequeue a completion packet. This handle must have been
        /// created by a call to the appropriate completion port creation function.</param>
        /// <param name="lpCompletionCode">When this method returns, contains the completion code associated with the completed I/O operation.</param>
        /// <param name="lpCompletionKey">When this method returns, contains the completion key that was specified when the file handle was associated
        /// with the completion port.</param>
        /// <param name="lpOverlapped">When this method returns, contains a pointer to the OVERLAPPED structure that was specified when the I/O
        /// operation was started.</param>
        /// <param name="dwMilliseconds">The number of milliseconds to wait for a completion packet. Specify INFINITE to wait indefinitely.</param>
        /// <returns>true if a completion packet was successfully dequeued; otherwise, false.</returns>
        internal static BOOL GetQueuedCompletionStatus(SafeHandle CompletionPort, out uint lpCompletionCode, out nuint lpCompletionKey, out nuint lpOverlapped, uint dwMilliseconds)
        {
            BOOL res;
            unsafe
            {
                res = PInvoke.GetQueuedCompletionStatus(CompletionPort, out lpCompletionCode, out lpCompletionKey, out NativeOverlapped* pOverlapped, dwMilliseconds);
                if (!res)
                {
                    throw ExceptionUtilities.GetExceptionForLastWin32Error();
                }
                lpOverlapped = (nuint)pOverlapped;
            }
            return res;
        }

        /// <summary>
        /// Retrieves the termination status code of the specified process.
        /// </summary>
        /// <param name="hProcess">A handle to the process whose exit code is to be retrieved. The handle must have the
        /// PROCESS_QUERY_INFORMATION or PROCESS_QUERY_LIMITED_INFORMATION access right.</param>
        /// <param name="lpExitCode">When this method returns, contains the exit code of the specified process if the function succeeds.</param>
        /// <returns>A value indicating whether the exit code was successfully retrieved. Returns <see langword="true"/> if
        /// successful; otherwise, <see langword="false"/>.</returns>
        internal static BOOL GetExitCodeProcess(SafeHandle hProcess, out uint lpExitCode)
        {
            BOOL res = PInvoke.GetExitCodeProcess(hProcess, out lpExitCode);
            return !res ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : res;
        }

        /// <summary>
        /// Terminates all processes associated with the specified job object and closes the job object handle.
        /// </summary>
        /// <remarks>After termination, the job object handle is closed and cannot be used in subsequent
        /// operations. This method throws an exception if the termination fails.</remarks>
        /// <param name="hJob">A handle to the job object to terminate. This handle must have the JOB_OBJECT_TERMINATE access right and
        /// must not be null.</param>
        /// <param name="uExitCode">The exit code to be used by all processes and threads in the job object.</param>
        /// <returns>true if the job object and all associated processes were terminated successfully; otherwise, false.</returns>
        internal static BOOL TerminateJobObject(SafeHandle hJob, uint uExitCode)
        {
            BOOL res = PInvoke.TerminateJobObject(hJob, uExitCode);
            return !res ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : res;
        }

        /// <summary>
        /// Retrieves the process identifier (PID) for the specified process handle.
        /// </summary>
        /// <param name="Process">A safe handle to the process whose identifier is to be retrieved. The handle must have the
        /// PROCESS_QUERY_INFORMATION or PROCESS_QUERY_LIMITED_INFORMATION access right.</param>
        /// <returns>The process identifier (PID) associated with the specified process handle.</returns>
        internal static uint GetProcessId(SafeHandle Process)
        {
            uint res = PInvoke.GetProcessId(Process);
            return res == 0 ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : res;
        }

        /// <summary>
        /// Duplicates an object handle from one process to another, allowing the target process to access the same
        /// object with specified access rights and options.
        /// </summary>
        /// <remarks>Both the source and target process handles must have the PROCESS_DUP_HANDLE access
        /// right. The caller is responsible for closing the duplicated handle when it is no longer needed to avoid
        /// resource leaks.</remarks>
        /// <param name="hSourceProcessHandle">A handle to the process with the handle to be duplicated. This handle must have the PROCESS_DUP_HANDLE
        /// access right.</param>
        /// <param name="hSourceHandle">The handle to be duplicated. This handle must be valid in the context of the source process.</param>
        /// <param name="hTargetProcessHandle">A handle to the process that will receive the duplicated handle. This handle must have the
        /// PROCESS_DUP_HANDLE access right.</param>
        /// <param name="lpTargetHandle">When this method returns, contains the duplicated handle, valid in the context of the target process.</param>
        /// <param name="dwDesiredAccess">The access rights for the duplicated handle. This parameter specifies the requested access to the object for
        /// the new handle.</param>
        /// <param name="bInheritHandle">A value that indicates whether the duplicated handle is inheritable by child processes. Specify <see
        /// langword="true"/> to make the handle inheritable; otherwise, <see langword="false"/>.</param>
        /// <param name="dwOptions">Options that control the duplication behavior. This parameter can be a combination of
        /// DUPLICATE_HANDLE_OPTIONS flags.</param>
        /// <returns>A value that is <see langword="true"/> if the handle was duplicated successfully; otherwise, <see
        /// langword="false"/>.</returns>
        internal static BOOL DuplicateHandle(SafeHandle hSourceProcessHandle, SafeHandle hSourceHandle, SafeHandle hTargetProcessHandle, out SafeFileHandle lpTargetHandle, PROCESS_ACCESS_RIGHTS dwDesiredAccess, in BOOL bInheritHandle, DUPLICATE_HANDLE_OPTIONS dwOptions)
        {
            BOOL res = PInvoke.DuplicateHandle(hSourceProcessHandle, hSourceHandle, hTargetProcessHandle, out lpTargetHandle, (uint)dwDesiredAccess, bInheritHandle, dwOptions);
            return !res ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : res;
        }

        /// <summary>
        /// Opens an existing local process object and returns a handle with the specified access rights.
        /// </summary>
        /// <remarks>If the process cannot be opened, an exception is thrown. The returned handle grants
        /// the access rights specified by <paramref name="dwDesiredAccess"/>. This method is intended for advanced
        /// scenarios that require direct process handle manipulation.</remarks>
        /// <param name="dwDesiredAccess">A combination of process access rights indicating the requested access to the process object. This
        /// determines the permitted operations on the returned handle.</param>
        /// <param name="bInheritHandle">A value that determines whether the returned handle can be inherited by child processes. Specify <see
        /// langword="true"/> to allow handle inheritance; otherwise, <see langword="false"/>.</param>
        /// <param name="dwProcessId">The identifier of the local process to open. This must be the process ID of an existing process.</param>
        /// <returns>A <see cref="SafeFileHandle"/> representing the opened process handle. The caller is responsible for
        /// releasing the handle when it is no longer needed.</returns>
        internal static SafeFileHandle OpenProcess(PROCESS_ACCESS_RIGHTS dwDesiredAccess, in BOOL bInheritHandle, uint dwProcessId)
        {
            SafeFileHandle res = PInvoke.OpenProcess_SafeHandle(dwDesiredAccess, bInheritHandle, dwProcessId);
            return res.IsInvalid ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : res;
        }

        /// <summary>
        /// Retrieves information about MS-DOS device names and their associated target paths on the local system.
        /// </summary>
        /// <remarks>If lpDeviceName is null, lpTargetPath receives a list of all existing MS-DOS device
        /// names. If lpDeviceName is specified, lpTargetPath receives the target path(s) for that device. This method
        /// throws an exception if the underlying system call fails.</remarks>
        /// <param name="lpDeviceName">The device name to query. Specify a device name (such as "C:") to retrieve its mapping, or null to retrieve
        /// a list of all device names. If not null, the string must not be empty.</param>
        /// <param name="lpTargetPath">A buffer that receives the result of the query. The buffer should be large enough to hold the returned path
        /// or list of device names, including the terminating null character(s).</param>
        /// <returns>The number of characters stored in lpTargetPath, not including the terminating null character(s).</returns>
        internal static uint QueryDosDevice(string lpDeviceName, Span<char> lpTargetPath)
        {
            uint res = PInvoke.QueryDosDevice(lpDeviceName, lpTargetPath);
            return res == 0 ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : res;
        }

        /// <summary>
        /// Retrieves the termination status of the specified thread.
        /// </summary>
        /// <remarks>If the thread has not terminated, the exit code returned is STATUS_PENDING. This method
        /// throws an exception if the underlying system call fails.</remarks>
        /// <param name="hThread">A handle to the thread whose exit code is to be retrieved. The handle must have the THREAD_QUERY_INFORMATION
        /// or THREAD_QUERY_LIMITED_INFORMATION access right.</param>
        /// <param name="lpExitCode">When this method returns, contains the exit code of the specified thread if the function succeeds.</param>
        /// <returns>true if the exit code was successfully retrieved; otherwise, false.</returns>
        internal static BOOL GetExitCodeThread(SafeHandle hThread, out uint lpExitCode)
        {
            BOOL res = PInvoke.GetExitCodeThread(hThread, out lpExitCode);
            return !res ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : res;
        }

        /// <summary>
        /// Retrieves a safe handle representing the current process.
        /// </summary>
        /// <remarks>The returned handle is safe to use with Windows API functions that require a process
        /// handle. It is the caller's responsibility to ensure proper disposal of the handle to release system
        /// resources.</remarks>
        /// <returns>A <see cref="SafeHandle"/> that encapsulates a handle to the current process.</returns>
        internal static SafeProcessHandle GetCurrentProcess()
        {
            HANDLE res = PInvoke.GetCurrentProcess();
            return res != (nint)(-1) ? throw ExceptionUtilities.GetException(WIN32_ERROR.ERROR_INVALID_HANDLE) : new(res, true);
        }

        /// <summary>
        /// Retrieves the session ID associated with a specified process ID.
        /// </summary>
        /// <param name="dwProcessId">The process ID for which to retrieve the session ID.</param>
        /// <param name="pSessionId">When this method returns, contains the session ID associated with the specified process ID.</param>
        /// <returns><see langword="true"/> if the session ID was successfully retrieved; otherwise, <see langword="false"/>.</returns>
        internal static BOOL ProcessIdToSessionId(uint dwProcessId, out uint pSessionId)
        {
            BOOL res = PInvoke.ProcessIdToSessionId(dwProcessId, out pSessionId);
            return !res ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : res;
        }

        /// <summary>
        /// Determines whether the system is currently in Terminal Services application installation mode.
        /// </summary>
        /// <remarks>Terminal Services application installation mode is used to install applications in a
        /// way that supports multiple users on a terminal server. This method can be used to check the current mode 
        /// before performing operations that depend on the installation mode.</remarks>
        /// <returns><see langword="true"/> if the system is in Terminal Services application installation mode; otherwise, <see
        /// langword="false"/>.</returns>
        [DllImport("kernel32.dll", SetLastError = false, ExactSpelling = true), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool TermsrvAppInstallMode();

        /// <summary>
        /// Retrieves system firmware table data for the specified firmware table provider and table ID.
        /// </summary>
        /// <remarks>This method retrieves firmware table data from the system using the specified
        /// provider and table ID. If the buffer provided in <paramref name="pFirmwareTableBuffer"/> is too small to
        /// hold the data, an <see cref="OverflowException"/> is thrown.</remarks>
        /// <param name="FirmwareTableProviderSignature">The signature of the firmware table provider. This identifies the type of firmware table to retrieve.</param>
        /// <param name="FirmwareTableID">The identifier of the specific firmware table to retrieve.</param>
        /// <param name="pFirmwareTableBuffer">A buffer to store the retrieved firmware table data. The buffer must be large enough to hold the data.</param>
        /// <returns>The size, in bytes, of the firmware table data retrieved.</returns>
        /// <exception cref="OverflowException">Thrown if the buffer provided in <paramref name="pFirmwareTableBuffer"/> is too small to hold the firmware
        /// table data.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Enforcing this rule just makes a mess.")]
        internal static uint GetSystemFirmwareTable(FIRMWARE_TABLE_PROVIDER FirmwareTableProviderSignature, FIRMWARE_TABLE_ID FirmwareTableID, Span<byte> pFirmwareTableBuffer)
        {
            uint res = PInvoke.GetSystemFirmwareTable(FirmwareTableProviderSignature, (uint)FirmwareTableID, pFirmwareTableBuffer);
            if (res == 0)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            if (pFirmwareTableBuffer.Length != 0 && res > pFirmwareTableBuffer.Length)
            {
                throw ExceptionUtilities.GetException(WIN32_ERROR.ERROR_INSUFFICIENT_BUFFER);
            }
            return res;
        }

        /// <summary>
        /// Retrieves the current system power status, including battery and AC power information.
        /// </summary>
        /// <remarks>This method wraps a call to the native Win32 API function
        /// <c>GetSystemPowerStatus</c>. It throws an exception if the underlying API call fails.</remarks>
        /// <param name="lpSystemPowerStatus">When the method returns, contains a <see cref="SYSTEM_POWER_STATUS"/> structure with details about the
        /// system's power status.</param>
        /// <returns><see langword="true"/> if the operation succeeds; otherwise, <see langword="false"/>.</returns>
        internal static BOOL GetSystemPowerStatus(out SYSTEM_POWER_STATUS lpSystemPowerStatus)
        {
            BOOL res = PInvoke.GetSystemPowerStatus(out lpSystemPowerStatus);
            return res == 0 ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : res;
        }

        /// <summary>
        /// Determines whether a specified process is running within a specified job.
        /// </summary>
        /// <param name="ProcessHandle">A handle to the process to be checked. This handle must have the PROCESS_QUERY_INFORMATION or
        /// PROCESS_QUERY_LIMITED_INFORMATION access right.</param>
        /// <param name="JobHandle">A handle to the job. If this parameter is <see langword="null"/>, the function checks if the process is
        /// running in any job.</param>
        /// <param name="Result">When this method returns, contains a <see langword="true"/> if the process is in the specified job;
        /// otherwise, <see langword="false"/>.</param>
        /// <returns><see langword="true"/> if the function succeeds; otherwise, <see langword="false"/>.</returns>
        internal static BOOL IsProcessInJob(SafeHandle ProcessHandle, SafeHandle? JobHandle, out BOOL Result)
        {
            BOOL res = PInvoke.IsProcessInJob(ProcessHandle, JobHandle, out Result);
            return !res ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : res;
        }

        /// <summary>
        /// Queries information about the specified job object.
        /// </summary>
        /// <remarks>This method is a wrapper around the native Windows API function
        /// <c>QueryInformationJobObject</c>. It is used to retrieve various types of information about a job object,
        /// such as accounting information, limits, and process information.</remarks>
        /// <param name="hJob">A handle to the job object. This handle must have the Query access right.</param>
        /// <param name="JobObjectInformationClass">The information class for the job object. This parameter specifies the type of information to be queried.</param>
        /// <param name="lpJobObjectInformation">A buffer that receives the information. The format of this data depends on the value of the <paramref
        /// name="JobObjectInformationClass"/> parameter.</param>
        /// <param name="lpReturnLength">When this method returns, contains the size of the data returned in the <paramref
        /// name="lpJobObjectInformation"/> buffer, in bytes.</param>
        /// <returns><see langword="true"/> if the function succeeds; otherwise, <see langword="false"/>.</returns>
        internal static BOOL QueryInformationJobObject(SafeHandle? hJob, JOBOBJECTINFOCLASS JobObjectInformationClass, Span<byte> lpJobObjectInformation, out uint lpReturnLength)
        {
            BOOL res = PInvoke.QueryInformationJobObject(hJob, JobObjectInformationClass, lpJobObjectInformation, out lpReturnLength);
            return !res ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : res;
        }

        /// <summary>
        /// Retrieves the Application User Model ID (AUMID) for a specified process.
        /// </summary>
        /// <remarks>This method wraps a PInvoke call to retrieve the AUMID, and throws an exception if
        /// the operation is unsuccessful.</remarks>
        /// <param name="hProcess">A handle to the process for which the AUMID is being retrieved. This handle must have the necessary access
        /// rights.</param>
        /// <param name="applicationUserModelIdLength">On output, receives the length of the AUMID, including the null terminator.</param>
        /// <param name="applicationUserModelId">A buffer that receives the AUMID as a null-terminated string.</param>
        /// <returns>A <see cref="WIN32_ERROR"/> code indicating the result of the operation. Returns <see
        /// cref="WIN32_ERROR.NO_ERROR"/> if successful.</returns>
        internal static WIN32_ERROR GetApplicationUserModelId(SafeHandle hProcess, out uint applicationUserModelIdLength, Span<char> applicationUserModelId)
        {
            applicationUserModelIdLength = (uint)applicationUserModelId.Length;
            return PInvoke.GetApplicationUserModelId(hProcess, ref applicationUserModelIdLength, applicationUserModelId).ThrowOnFailure();
        }

        /// <summary>
        /// Reads data from an area of memory in a specified process. The process is identified by a handle.
        /// </summary>
        /// <remarks>This method wraps the PInvoke call to ReadProcessMemory and throws an exception if
        /// the operation fails. Ensure that the buffer is large enough to hold the data being read to avoid an <see
        /// cref="OverflowException"/>.</remarks>
        /// <param name="hProcess">A handle to the process with memory that is being read. The handle must have PROCESS_VM_READ access.</param>
        /// <param name="lpBaseAddress">A pointer to the base address in the specified process from which to read.</param>
        /// <param name="lpBuffer">A pointer to a buffer that receives the contents from the address space of the specified process.</param>
        /// <param name="lpNumberOfBytesRead">A pointer to a variable that receives the number of bytes transferred into the specified buffer. This
        /// parameter can be null.</param>
        /// <returns>A <see cref="BOOL"/> indicating whether the operation succeeded.</returns>
        /// <exception cref="OverflowException">Thrown if the buffer was too small and the value was truncated.</exception>
        internal static BOOL ReadProcessMemory(SafeHandle hProcess, nint lpBaseAddress, Span<byte> lpBuffer, out nuint lpNumberOfBytesRead)
        {
            BOOL res;
            unsafe
            {
                res = PInvoke.ReadProcessMemory(hProcess, (void*)lpBaseAddress, lpBuffer, out lpNumberOfBytesRead);
            }
            return !res ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : res;
        }

        /// <summary>
        /// Retrieves a human-readable name for a specified language identifier.
        /// </summary>
        /// <remarks>This method wraps a call to a native function and throws an exception if the
        /// operation fails. Ensure that <paramref name="szLang"/> is sufficiently large to avoid truncation.</remarks>
        /// <param name="wLang">The language identifier for which the name is to be retrieved.</param>
        /// <param name="szLang">A span of characters that receives the language name. The buffer must be large enough to hold the name.</param>
        /// <returns>The number of characters written to <paramref name="szLang"/>, excluding the null terminator.</returns>
        /// <exception cref="OverflowException">Thrown if the buffer provided by <paramref name="szLang"/> is too small to hold the language name.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Enforcing this rule just makes a mess.")]
        internal static uint VerLanguageName(uint wLang, Span<char> szLang)
        {
            uint res = PInvoke.VerLanguageName(wLang, szLang);
            if (res == 0)
            {
                throw ExceptionUtilities.GetException(WIN32_ERROR.ERROR_GEN_FAILURE, "Failed to retrieve language name.");
            }
            if (res > szLang.Length)
            {
                throw ExceptionUtilities.GetException(WIN32_ERROR.ERROR_INSUFFICIENT_BUFFER);
            }
            return res;
        }

        /// <summary>
        /// Posts an I/O completion packet to the specified I/O completion port.
        /// </summary>
        /// <remarks>This method is typically used to queue custom completion packets to an I/O completion
        /// port, allowing threads waiting on the port to be notified. The method throws an exception if the underlying
        /// system call fails.</remarks>
        /// <param name="CompletionPort">A handle to the I/O completion port to which the completion packet will be posted. Must be a valid, open
        /// handle.</param>
        /// <param name="dwNumberOfBytesTransferred">The number of bytes associated with the I/O operation. This value is returned through the completion packet
        /// and can be used by the consumer to determine the amount of data transferred.</param>
        /// <param name="dwCompletionKey">A value to be associated with the completion packet. This value is returned when the completion packet is
        /// dequeued and can be used to identify the source or context of the completion.</param>
        /// <param name="lpOverlapped">A reference to a NativeOverlapped structure to be associated with the completion packet, or the default
        /// value if no overlapped structure is required.</param>
        /// <returns>true if the operation succeeds; otherwise, false.</returns>
        internal static BOOL PostQueuedCompletionStatus(SafeHandle CompletionPort, uint dwNumberOfBytesTransferred, nuint dwCompletionKey, in NativeOverlapped lpOverlapped = default)
        {
            BOOL res;
            unsafe
            {
                fixed (NativeOverlapped* pOverlapped = &lpOverlapped)
                {
                    res = PInvoke.PostQueuedCompletionStatus(CompletionPort, dwNumberOfBytesTransferred, dwCompletionKey, pOverlapped);
                }
            }
            return !res ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : res;
        }

        /// <summary>
        /// Opens an existing file or creates a new file, device, or named pipe, returning a handle with the specified
        /// access rights and sharing mode.
        /// </summary>
        /// <remarks>This method throws an exception if the file cannot be opened or created. The caller
        /// is responsible for closing the returned SafeFileHandle when it is no longer needed. The method is intended
        /// for advanced scenarios that require direct control over file creation and access flags, similar to the
        /// Windows CreateFile API.</remarks>
        /// <param name="lpFileName">The name or path of the file, device, or named pipe to be created or opened. This parameter cannot be null
        /// or empty.</param>
        /// <param name="dwDesiredAccess">The access rights requested for the returned handle, such as read, write, or execute permissions. Specify
        /// one or more values from the FileSystemRights enumeration.</param>
        /// <param name="dwShareMode">The sharing mode for the file or device, determining how the file can be shared with other processes.
        /// Specify one or more values from the FILE_SHARE_MODE enumeration.</param>
        /// <param name="lpSecurityAttributes">A pointer to a SECURITY_ATTRIBUTES structure that determines whether the returned handle can be inherited by
        /// child processes. Can be null to use default security settings.</param>
        /// <param name="dwCreationDisposition">Specifies the action to take on files that exist or do not exist. Use a value from the
        /// FILE_CREATION_DISPOSITION enumeration to control whether to create a new file, open an existing file, or
        /// overwrite an existing file.</param>
        /// <param name="dwFlagsAndAttributes">The file or device attributes and flags, such as file attributes, security flags, and other special options.
        /// Specify one or more values from the FileAttributes enumeration.</param>
        /// <param name="hTemplateFile">A handle to a template file with the desired attributes to apply to the file being created. Can be null if
        /// no template is needed.</param>
        /// <returns>A SafeFileHandle representing the opened or newly created file, device, or named pipe. The handle is valid
        /// and ready for use. If the operation fails, an exception is thrown.</returns>
        internal static SafeFileHandle CreateFile(string lpFileName, FileSystemRights dwDesiredAccess, FILE_SHARE_MODE dwShareMode, in SECURITY_ATTRIBUTES? lpSecurityAttributes, FILE_CREATION_DISPOSITION dwCreationDisposition, FileAttributes dwFlagsAndAttributes, SafeHandle? hTemplateFile = null)
        {
            SafeFileHandle res = PInvoke.CreateFile(lpFileName, (uint)dwDesiredAccess, dwShareMode, lpSecurityAttributes, dwCreationDisposition, (FILE_FLAGS_AND_ATTRIBUTES)dwFlagsAndAttributes, hTemplateFile);
            return res.IsInvalid ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : res;
        }

        /// <summary>
        /// Retrieves the product type of the operating system based on the specified version and service pack
        /// information.
        /// </summary>
        /// <param name="dwOSMajorVersion">The major version number of the operating system.</param>
        /// <param name="dwOSMinorVersion">The minor version number of the operating system.</param>
        /// <param name="dwSpMajorVersion">The major version number of the service pack installed on the operating system.</param>
        /// <param name="dwSpMinorVersion">The minor version number of the service pack installed on the operating system.</param>
        /// <param name="pdwReturnedProductType">When this method returns, contains the product type of the operating system. This parameter is passed
        /// uninitialized.</param>
        /// <returns><see langword="true"/> if the product type information was successfully retrieved; otherwise, <see
        /// langword="false"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the product type information could not be retrieved.</exception>
        internal static BOOL GetProductInfo(uint dwOSMajorVersion, uint dwOSMinorVersion, uint dwSpMajorVersion, uint dwSpMinorVersion, out OS_PRODUCT_TYPE pdwReturnedProductType)
        {
            BOOL res = PInvoke.GetProductInfo(dwOSMajorVersion, dwOSMinorVersion, dwSpMajorVersion, dwSpMinorVersion, out pdwReturnedProductType);
            return !res ? throw ExceptionUtilities.GetException(WIN32_ERROR.ERROR_GEN_FAILURE, "Failed to get product info.") : res;
        }

        /// <summary>
        /// Waits for the specified object to enter a signaled state or for the specified timeout interval to elapse.
        /// </summary>
        /// <param name="hHandle">A handle to the object to wait for. This handle must be valid and cannot be null.</param>
        /// <param name="dwMilliseconds">The time-out interval, in milliseconds. Specify <see langword="uint.MaxValue"/> to wait indefinitely.</param>
        /// <returns>A <see cref="WAIT_EVENT"/> value indicating the result of the wait operation. Possible values include <see
        /// cref="WAIT_EVENT.WAIT_OBJECT_0"/> for a signaled state, <see cref="WAIT_EVENT.WAIT_TIMEOUT"/> for a timeout,
        /// or <see cref="WAIT_EVENT.WAIT_ABANDONED"/> for an abandoned mutex.</returns>
        internal static WAIT_EVENT WaitForSingleObject(SafeHandle hHandle, uint dwMilliseconds)
        {
            WAIT_EVENT res = PInvoke.WaitForSingleObject(hHandle, dwMilliseconds);
            return res == WAIT_EVENT.WAIT_FAILED ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : res;
        }

        /// <summary>
        /// Defines, modifies, or deletes a symbolic link (DOS device name) in the system's device namespace.
        /// </summary>
        /// <remarks>This method wraps the Windows DefineDosDevice API and throws an exception if the
        /// operation fails. Use the appropriate flags to control the behavior of the symbolic link. Administrative
        /// privileges may be required to modify certain device mappings.</remarks>
        /// <param name="dwFlags">A combination of flags that specify the operation to perform and how the symbolic link is handled. These
        /// flags determine whether to create, modify, or remove the mapping, and may affect how the target path is
        /// interpreted.</param>
        /// <param name="lpDeviceName">The name of the DOS device (symbolic link) to define, modify, or delete. This value cannot be null or empty.</param>
        /// <param name="lpTargetPath">The target path for the symbolic link. This parameter is required when creating or modifying a mapping, and
        /// should be null when deleting a mapping.</param>
        /// <returns>A value indicating whether the operation succeeded. Returns <see langword="true"/> if the symbolic link was
        /// defined, modified, or deleted successfully; otherwise, <see langword="false"/>.</returns>
        internal static BOOL DefineDosDevice(DEFINE_DOS_DEVICE_FLAGS dwFlags, string lpDeviceName, string? lpTargetPath)
        {
            BOOL res = PInvoke.DefineDosDevice(dwFlags, lpDeviceName, lpTargetPath);
            return !res ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : res;
        }

        /// <summary>
        /// Retrieves the type of the specified file or device handle.
        /// </summary>
        /// <remarks>If the underlying system call fails, an exception is thrown. This method wraps the
        /// native GetFileType function and provides error handling for Win32 errors.</remarks>
        /// <param name="hFile">A safe handle to the file or device whose type is to be determined. The handle must be valid and open.</param>
        /// <returns>A value of the FILE_TYPE enumeration that indicates the type of the specified handle. Returns
        /// FILE_TYPE.FILE_TYPE_UNKNOWN if the type cannot be determined and no error occurred.</returns>
        internal static FILE_TYPE GetFileType(SafeHandle hFile)
        {
            FILE_TYPE res = PInvoke.GetFileType(hFile);
            return res == FILE_TYPE.FILE_TYPE_UNKNOWN && ExceptionUtilities.GetLastWin32Error() is WIN32_ERROR lastWin32Error && lastWin32Error != WIN32_ERROR.NO_ERROR
                ? throw ExceptionUtilities.GetException(lastWin32Error)
                : res;
        }

        /// <summary>
        /// Retrieves the file attributes for the specified file or directory path.
        /// </summary>
        /// <param name="lpFileName">The full path to the file or directory for which to retrieve attributes. This parameter cannot be null or an
        /// empty string.</param>
        /// <returns>A value of type FileAttributes that describes the attributes of the specified file or directory.</returns>
        internal static FileAttributes GetFileAttributes(string lpFileName)
        {
            uint res = PInvoke.GetFileAttributes(lpFileName);
            return res == PInvoke.INVALID_FILE_ATTRIBUTES ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : (FileAttributes)res;
        }

        /// <summary>
        /// Retrieves the full name of the executable image for the specified process.
        /// </summary>
        /// <remarks>This method wraps the Windows API QueryFullProcessImageName function. The caller is
        /// responsible for ensuring that the buffer provided by lpExeName is sufficiently large to hold the full path.
        /// If the buffer is too small, an exception is thrown.</remarks>
        /// <param name="hProcess">A handle to the process. The handle must have the PROCESS_QUERY_LIMITED_INFORMATION access right.</param>
        /// <param name="dwFlags">A value that specifies the format for the returned process name. This determines whether the name is in
        /// Win32 or native format.</param>
        /// <param name="lpExeName">A span of characters that receives the full path to the executable file. The buffer must be large enough to
        /// receive the path.</param>
        /// <param name="lpdwSize">When this method returns, contains the number of characters written to lpExeName, not including the null
        /// terminator.</param>
        /// <returns>A nonzero value if the function succeeds; otherwise, an exception is thrown.</returns>
        internal static BOOL QueryFullProcessImageName(SafeHandle hProcess, PROCESS_NAME_FORMAT dwFlags, Span<char> lpExeName, out uint lpdwSize)
        {
            lpdwSize = (uint)lpExeName.Length;
            BOOL res = PInvoke.QueryFullProcessImageName(hProcess, dwFlags, lpExeName, ref lpdwSize);
            return !res ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : res;
        }

        /// <summary>
        /// Formats a message string based on a specified message identifier and language, using the provided formatting
        /// options and arguments.
        /// </summary>
        /// <remarks>This method is a managed wrapper for the Windows FormatMessage function. It is
        /// typically used to retrieve system error messages or custom messages defined in a resource module. The caller
        /// is responsible for providing a sufficiently sized buffer to receive the formatted message.</remarks>
        /// <param name="dwFlags">A set of formatting options that control the behavior of the message formatting. These options determine the
        /// source of the message definition and how the output is processed.</param>
        /// <param name="lpSource">An optional handle to a module that contains the message resource definition. If null, the system message
        /// table resource is used.</param>
        /// <param name="dwMessageId">The identifier for the message to be formatted. This value specifies which message definition to use.</param>
        /// <param name="lpBuffer">A span of characters that receives the formatted message string. The buffer must be large enough to hold the
        /// resulting message.</param>
        /// <param name="dwLanguageId">The language identifier that specifies the language of the message. This determines which localized message
        /// is retrieved.</param>
        /// <param name="Arguments">A pointer to an array of arguments to be inserted into the message. Can be null if the message does not
        /// require arguments.</param>
        /// <returns>The number of characters stored in the output buffer, excluding the terminating null character.</returns>
        /// <exception cref="Win32Exception">Thrown if the message formatting operation fails.</exception>
        internal static uint FormatMessage(FORMAT_MESSAGE_OPTIONS dwFlags, [Optional] FreeLibrarySafeHandle? lpSource, uint dwMessageId, Span<char> lpBuffer, uint dwLanguageId = 0, in nint Arguments = default)
        {
            uint res;
            unsafe
            {
                bool lpSourceAddRef = false;
                try
                {
                    lpSource?.DangerousAddRef(ref lpSourceAddRef);
                    res = PInvoke.FormatMessage(dwFlags, lpSource is not null ? (void*)lpSource.DangerousGetHandle() : null, dwMessageId, dwLanguageId, lpBuffer, (uint)lpBuffer.Length, (sbyte*)Arguments);

                }
                finally
                {
                    if (lpSourceAddRef)
                    {
                        lpSource!.DangerousRelease();
                    }
                }
            }
            return res == 0 ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : res;
        }

        /// <summary>
        /// Opens a Windows Installer database and returns a handle to the opened database.
        /// </summary>
        /// <remarks>The returned handle must be released by calling the appropriate close method to avoid
        /// resource leaks. If the operation fails, an exception is thrown and the out parameter is not set.</remarks>
        /// <param name="szDatabasePath">The path to the Windows Installer database file to open. Cannot be null or empty.</param>
        /// <param name="szPersist">The persistence mode to use when opening the database, specifying whether the database is opened as
        /// read-only or read/write.</param>
        /// <param name="phDatabase">When this method returns, contains a handle to the opened database. This handle must be closed by the caller
        /// when no longer needed.</param>
        /// <returns>A WIN32_ERROR value indicating the result of the operation. Returns WIN32_ERROR.ERROR_SUCCESS if the
        /// database was opened successfully.</returns>
        internal static WIN32_ERROR MsiOpenDatabase(string szDatabasePath, MSI_PERSISTENCE_MODE szPersist, out MsiCloseHandleSafeHandle phDatabase)
        {
            WIN32_ERROR res;
            unsafe
            {
                MSIHANDLE phDatabaseLocal = default;
                fixed (char* pszDatabasePath = szDatabasePath)
                {
                    res = ((WIN32_ERROR)PInvoke.MsiOpenDatabase(pszDatabasePath, szPersist.ToPCWSTR(), &phDatabaseLocal)).ThrowOnFailure();
                }
                phDatabase = new MsiCloseHandleSafeHandle(phDatabaseLocal, true);
            }
            return res;
        }

        /// <summary>
        /// Retrieves summary information from an installer database or summary information stream and returns a handle
        /// to access the summary information.
        /// </summary>
        /// <remarks>The caller is responsible for closing the returned summary information handle when it
        /// is no longer needed. If the operation fails, an exception is thrown and phSummaryInfo is not set.</remarks>
        /// <param name="hDatabase">A handle to the open installer database. If null, the function opens the database specified by
        /// szDatabasePath.</param>
        /// <param name="szDatabasePath">The path to the installer database file. Used only if hDatabase is null. Can be null if hDatabase is
        /// provided.</param>
        /// <param name="uiUpdateCount">The number of summary information properties to be updated. Set to 0 if no properties will be updated.</param>
        /// <param name="phSummaryInfo">When this method returns, contains a handle to the summary information stream. This parameter is passed
        /// uninitialized.</param>
        /// <returns>A WIN32_ERROR value indicating the result of the operation. Returns ERROR_SUCCESS if the operation succeeds.</returns>
        internal static WIN32_ERROR MsiGetSummaryInformation(SafeHandle? hDatabase, string? szDatabasePath, uint uiUpdateCount, out MsiCloseHandleSafeHandle phSummaryInfo)
        {
            MSIHANDLE phSummaryInfoLocal = default;
            WIN32_ERROR res;
            if (hDatabase is null)
            {
                using SafeFileHandle nullHandle = new(default, true);
                res = ((WIN32_ERROR)PInvoke.MsiGetSummaryInformation(nullHandle, szDatabasePath, uiUpdateCount, ref phSummaryInfoLocal)).ThrowOnFailure();
            }
            else
            {
                res = ((WIN32_ERROR)PInvoke.MsiGetSummaryInformation(hDatabase, szDatabasePath, uiUpdateCount, ref phSummaryInfoLocal)).ThrowOnFailure();
            }
            phSummaryInfo = new MsiCloseHandleSafeHandle(phSummaryInfoLocal, true);
            return res;
        }

        /// <summary>
        /// Retrieves summary information from an open Windows Installer database and returns a handle to the summary
        /// information stream.
        /// </summary>
        /// <param name="hDatabase">A handle to the open Windows Installer database. Can be null to indicate that the summary information is to
        /// be retrieved from a file specified elsewhere.</param>
        /// <param name="uiUpdateCount">The maximum number of updated properties that can be written to the summary information stream. Set to 0 to
        /// open the stream for read-only access.</param>
        /// <param name="phSummaryInfo">When this method returns, contains a handle to the summary information stream. This handle must be closed
        /// with the appropriate method when no longer needed.</param>
        /// <returns>A value of the WIN32_ERROR enumeration that indicates the result of the operation. Returns
        /// WIN32_ERROR.SUCCESS if the operation succeeds; otherwise, returns an error code.</returns>
        internal static WIN32_ERROR MsiGetSummaryInformation(SafeHandle hDatabase, uint uiUpdateCount, out MsiCloseHandleSafeHandle phSummaryInfo)
        {
            return MsiGetSummaryInformation(hDatabase, null, uiUpdateCount, out phSummaryInfo).ThrowOnFailure();
        }

        /// <summary>
        /// Retrieves summary information from a specified Windows Installer database file.
        /// </summary>
        /// <param name="szDatabasePath">The path to the Windows Installer database file. Can be null to indicate a null database handle.</param>
        /// <param name="uiUpdateCount">The number of summary information properties to be updated. Set to 0 to open the summary information in
        /// read-only mode.</param>
        /// <param name="phSummaryInfo">When this method returns, contains a handle to the summary information stream. This parameter is passed
        /// uninitialized.</param>
        /// <returns>A WIN32_ERROR value indicating the result of the operation. Returns WIN32_ERROR.SUCCESS if the summary
        /// information was retrieved successfully; otherwise, returns an error code.</returns>
        internal static WIN32_ERROR MsiGetSummaryInformation(string szDatabasePath, uint uiUpdateCount, out MsiCloseHandleSafeHandle phSummaryInfo)
        {
            return MsiGetSummaryInformation(null, szDatabasePath, uiUpdateCount, out phSummaryInfo).ThrowOnFailure();
        }

        /// <summary>
        /// Retrieves a property value from the specified Windows Installer summary information stream.
        /// </summary>
        /// <remarks>The caller should examine the data type returned in puiDataType to determine which of
        /// the output parameters contains the valid property value. Only one of piValue, pftValue, or szValueBuf will
        /// be populated based on the property type. If szValueBuf is too small to hold the string value, the required
        /// buffer size is returned in pcchValueBuf.</remarks>
        /// <param name="hSummaryInfo">A handle to the summary information stream from which to retrieve the property. The handle must be valid and
        /// open for reading.</param>
        /// <param name="uiProperty">The identifier of the summary property to retrieve.</param>
        /// <param name="puiDataType">When this method returns, contains the data type of the retrieved property value.</param>
        /// <param name="piValue">When this method returns, contains the integer value of the property, if applicable. Otherwise, set to zero.</param>
        /// <param name="pftValue">When this method returns, contains the FILETIME value of the property, if applicable.</param>
        /// <param name="szValueBuf">A buffer that receives the string value of the property, if applicable. If the property is not a string,
        /// this buffer is not modified.</param>
        /// <param name="pcchValueBuf">When this method returns, contains the number of characters written to szValueBuf, or the required buffer
        /// size if szValueBuf is too small.</param>
        /// <returns>A WIN32_ERROR value indicating the result of the operation. Returns ERROR_SUCCESS if the property was
        /// retrieved successfully; otherwise, returns an error code.</returns>
        internal static WIN32_ERROR MsiSummaryInfoGetProperty(SafeHandle hSummaryInfo, MSI_PROPERTY_ID uiProperty, out VARENUM puiDataType, out int piValue, out System.Runtime.InteropServices.ComTypes.FILETIME pftValue, Span<char> szValueBuf, out uint pcchValueBuf)
        {
            uint pcchValueBufLocal = (uint)szValueBuf.Length;
            WIN32_ERROR res = ((WIN32_ERROR)PInvoke.MsiSummaryInfoGetProperty(hSummaryInfo, (uint)uiProperty, out uint puiDataTypeLocal, out piValue, out pftValue, szValueBuf, ref pcchValueBufLocal)).ThrowOnFailure();
            puiDataType = (VARENUM)puiDataTypeLocal;
            pcchValueBuf = pcchValueBufLocal;
            return res;
        }

        /// <summary>
        /// Extracts the XML data embedded in a Windows Installer patch file and copies it into the provided character
        /// buffer.
        /// </summary>
        /// <remarks>If the buffer specified by <paramref name="szXMLData"/> is too small to hold the XML
        /// data, the method throws an exception. Ensure that the buffer is sized appropriately before calling this
        /// method.</remarks>
        /// <param name="szPatchPath">The full path to the patch file from which to extract XML data. Cannot be null.</param>
        /// <param name="szXMLData">A buffer that receives the extracted XML data as characters. The buffer must be large enough to hold the XML
        /// data; otherwise, the operation will fail.</param>
        /// <param name="pcchXMLData">When this method returns, contains the number of characters copied to <paramref name="szXMLData"/>.</param>
        /// <returns>A <see cref="WIN32_ERROR"/> value indicating the result of the extraction operation. Returns <see
        /// cref="WIN32_ERROR.ERROR_SUCCESS"/> if the XML data was successfully extracted.</returns>
        internal static WIN32_ERROR MsiExtractPatchXMLData(string szPatchPath, Span<char> szXMLData, out uint pcchXMLData)
        {
            uint pcchXMLDataLocal = (uint)szXMLData.Length;
            WIN32_ERROR res = ((WIN32_ERROR)PInvoke.MsiExtractPatchXMLData(szPatchPath, szXMLData, ref pcchXMLDataLocal)).ThrowOnFailure();
            pcchXMLData = pcchXMLDataLocal;
            return res;
        }

        /// <summary>
        /// Queries the installation state of a product identified by its GUID.
        /// </summary>
        /// <remarks>This method uses the Windows Installer API to determine the installation status of
        /// the product. Ensure that the provided GUID corresponds to a valid product code in the standard GUID
        /// format.</remarks>
        /// <param name="szProduct">The GUID that uniquely identifies the product to query.</param>
        /// <returns>An INSTALLSTATE value that indicates the current installation state of the specified product.</returns>
        internal static Windows.Win32.System.ApplicationInstallationAndServicing.INSTALLSTATE MsiQueryProductState(Guid szProduct)
        {
            return PInvoke.MsiQueryProductState(szProduct.ToString("B"));
        }

        /// <summary>
        /// Retrieves version information about the currently running Windows operating system.
        /// </summary>
        /// <remarks>This method throws an exception if the underlying native call fails. The output
        /// parameter is always initialized before the native call is made.</remarks>
        /// <param name="lpVersionInformation">When this method returns, contains an OSVERSIONINFOEXW structure that receives the operating system version
        /// information. The structure's dwOSVersionInfoSize field is initialized automatically.</param>
        /// <returns>A value of type NTSTATUS indicating the result of the operation. Returns STATUS_SUCCESS if the version
        /// information was retrieved successfully.</returns>
        internal static NTSTATUS RtlGetVersion(out OSVERSIONINFOEXW lpVersionInformation)
        {
            lpVersionInformation = new() { dwOSVersionInfoSize = (uint)Marshal.SizeOf<OSVERSIONINFOEXW>() };
            unsafe
            {
                fixed (OSVERSIONINFOEXW* lpVersionInformationLocal = &lpVersionInformation)
                {
                    return Windows.Wdk.PInvoke.RtlGetVersion((OSVERSIONINFOW*)lpVersionInformationLocal).ThrowOnFailure();
                }
            }
        }

        /// <summary>
        /// Retrieves system information for the specified information class by calling the native
        /// NtQuerySystemInformation function.
        /// </summary>
        /// <remarks>If the buffer specified by SystemInformation is too small to hold the requested data,
        /// the method returns STATUS_INFO_LENGTH_MISMATCH and sets ReturnLength to the required buffer size. The caller
        /// can then allocate a larger buffer and retry the operation. This method throws an exception for NTSTATUS
        /// values other than STATUS_SUCCESS and STATUS_INFO_LENGTH_MISMATCH.</remarks>
        /// <param name="SystemInformationClass">The type of system information to be queried. This value determines the structure and content of the data
        /// returned in the SystemInformation buffer.</param>
        /// <param name="SystemInformation">A buffer that receives the requested system information. The buffer must be large enough to hold the data
        /// for the specified information class and cannot be empty.</param>
        /// <param name="ReturnLength">When this method returns, contains the number of bytes written to the SystemInformation buffer or the number
        /// of bytes required if the buffer is too small.</param>
        /// <param name="retrievingLength">When true, indicates the caller is performing an initial query to determine required buffer size.
        /// STATUS_INFO_LENGTH_MISMATCH will be allowed without throwing. Default is false.</param>
        /// <returns>An NTSTATUS code indicating the result of the operation. Returns STATUS_SUCCESS if successful, or
        /// STATUS_INFO_LENGTH_MISMATCH if the buffer is too small.</returns>
        /// <exception cref="ArgumentNullException">Thrown if SystemInformation is empty.</exception>
        internal static NTSTATUS NtQuerySystemInformation(SYSTEM_INFORMATION_CLASS SystemInformationClass, Span<byte> SystemInformation, out uint ReturnLength, bool retrievingLength = false)
        {
            if (SystemInformation.IsEmpty)
            {
                throw new ArgumentNullException(nameof(SystemInformation));
            }
            ReturnLength = 0;
            NTSTATUS res;
            unsafe
            {
                fixed (byte* SystemInformationLocal = SystemInformation)
                {
                    res = Windows.Wdk.PInvoke.NtQuerySystemInformation((Windows.Wdk.System.SystemInformation.SYSTEM_INFORMATION_CLASS)SystemInformationClass, SystemInformationLocal, (uint)SystemInformation.Length, ref ReturnLength);
                }
            }
            return res != NTSTATUS.STATUS_SUCCESS && (res != NTSTATUS.STATUS_INFO_LENGTH_MISMATCH || (!retrievingLength && (!SystemInfoClassSizes.TryGetValue(SystemInformationClass, out int systemInfoQueryLength) || SystemInformation.Length != systemInfoQueryLength) && 0 != SystemInformation.Length))
                ? throw ExceptionUtilities.GetException(res)
                : res;
        }

        /// <summary>
        /// Queries information about the specified object handle by invoking the native NtQueryObject function.
        /// </summary>
        /// <remarks>This method is a managed wrapper for the native NtQueryObject function in ntdll.dll.
        /// The caller is responsible for providing a buffer of sufficient size in ObjectInformation. If the buffer is
        /// too small, the required size is returned in ReturnLength.</remarks>
        /// <param name="Handle">A SafeHandle representing the object to query. The handle must be valid and not closed.</param>
        /// <param name="ObjectInformationClass">The type of information to retrieve about the object, specified as an OBJECT_INFORMATION_CLASS value.</param>
        /// <param name="ObjectInformation">A span of bytes that receives the requested information. Must not be empty.</param>
        /// <param name="ReturnLength">When this method returns, contains the number of bytes written to ObjectInformation or required to store the
        /// information, depending on the operation.</param>
        /// <param name="retrievingLength">When true, indicates the caller is performing an initial query to determine required buffer size.
        /// STATUS_INFO_LENGTH_MISMATCH will be allowed without throwing. Default is false.</param>
        /// <returns>An NTSTATUS value indicating the result of the operation. STATUS_SUCCESS indicates success; otherwise, an
        /// error code is returned.</returns>
        /// <exception cref="ArgumentNullException">Thrown if Handle is null or closed, or if ObjectInformation is empty.</exception>
        internal static NTSTATUS NtQueryObject(SafeHandle? Handle, OBJECT_INFORMATION_CLASS ObjectInformationClass, Span<byte> ObjectInformation, out uint ReturnLength, bool retrievingLength = false)
        {
            if (ObjectInformation.IsEmpty)
            {
                throw new ArgumentNullException(nameof(ObjectInformation));
            }
            bool HandleAddRef = false;
            NTSTATUS res;
            try
            {
                Handle?.DangerousAddRef(ref HandleAddRef);
                res = Windows.Wdk.PInvoke.NtQueryObject(Handle is not null ? (HANDLE)Handle.DangerousGetHandle() : HANDLE.Null, (Windows.Wdk.Foundation.OBJECT_INFORMATION_CLASS)ObjectInformationClass, ObjectInformation, out ReturnLength);
            }
            finally
            {
                if (HandleAddRef)
                {
                    Handle?.DangerousRelease();
                }
            }
            return res != NTSTATUS.STATUS_SUCCESS && (res != NTSTATUS.STATUS_INFO_LENGTH_MISMATCH || (!retrievingLength && (!ObjectInfoClassSizes.TryGetValue(ObjectInformationClass, out int objectInfoQueryLength) || ObjectInformation.Length != objectInfoQueryLength) && 0 != ObjectInformation.Length))
                ? throw ExceptionUtilities.GetException(res)
                : res;
        }

        /// <summary>
        /// Creates a new thread in the specified process using the native NtCreateThreadEx system call.
        /// </summary>
        /// <remarks>This method is intended for advanced scenarios that require direct interaction with
        /// the Windows native thread creation API. The caller is responsible for ensuring that all parameters,
        /// especially handles and memory addresses, are valid and appropriate for the target process. Improper use may
        /// result in process instability or security risks.</remarks>
        /// <param name="ThreadHandle">When this method returns, contains a SafeThreadHandle representing the newly created thread. This parameter
        /// is passed uninitialized.</param>
        /// <param name="DesiredAccess">The access rights requested for the new thread. Specify a combination of THREAD_ACCESS_RIGHTS flags that
        /// determine the permitted operations on the thread.</param>
        /// <param name="ProcessHandle">A SafeProcessHandle representing the process in which to create the thread. The handle must have appropriate
        /// access rights for thread creation and must not be null or closed.</param>
        /// <param name="StartRoutine">A SafeVirtualAllocHandle specifying the starting address of the thread routine in the target process. This
        /// handle must not be null, closed, or invalid.</param>
        /// <param name="Argument">A pointer to a variable to be passed as a parameter to the thread routine, or default if no parameter is
        /// required.</param>
        /// <param name="CreateFlags">Flags that control the creation of the thread. This value can be zero or a combination of thread creation
        /// flags as defined by the native API.</param>
        /// <param name="ZeroBits">The number of high-order address bits that must be zero in the stack's base address. Typically set to zero.</param>
        /// <param name="StackSize">The initial size, in bytes, of the stack for the new thread. If zero, the default stack size for the
        /// executable is used.</param>
        /// <param name="MaximumStackSize">The maximum size, in bytes, of the stack for the new thread. If zero, the default maximum is used.</param>
        /// <returns>An NTSTATUS code indicating the result of the operation. STATUS_SUCCESS indicates success; otherwise, the
        /// code specifies the error.</returns>
        /// <exception cref="ArgumentNullException">Thrown if ProcessHandle is null or closed, or if StartRoutine is null, closed, or invalid.</exception>
        internal static NTSTATUS NtCreateThreadEx(out SafeThreadHandle ThreadHandle, THREAD_ACCESS_RIGHTS DesiredAccess, SafeProcessHandle ProcessHandle, SafeVirtualAllocHandle StartRoutine, nint? Argument = null, THREAD_CREATE_FLAGS CreateFlags = 0, uint ZeroBits = 0, uint StackSize = 0, uint MaximumStackSize = 0)
        {
            if (StartRoutine is null || StartRoutine.IsClosed || StartRoutine.IsInvalid)
            {
                throw new ArgumentNullException(nameof(StartRoutine));
            }
            if (ProcessHandle is null || ProcessHandle.IsClosed)
            {
                throw new ArgumentNullException(nameof(ProcessHandle));
            }
            [DllImport("ntdll.dll", ExactSpelling = true), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
            static extern NTSTATUS NtCreateThreadEx(out nint ThreadHandle, THREAD_ACCESS_RIGHTS DesiredAccess, nint ObjectAttributes, nint ProcessHandle, nint StartRoutine, nint Argument, THREAD_CREATE_FLAGS CreateFlags, uint ZeroBits, uint StackSize, uint MaximumStackSize, nint AttributeList);
            bool StartRoutineAddRef = false;
            bool ProcessHandleAddRef = false;
            try
            {
                StartRoutine.DangerousAddRef(ref StartRoutineAddRef);
                ProcessHandle.DangerousAddRef(ref ProcessHandleAddRef);
                NTSTATUS res = NtCreateThreadEx(out nint hThread, DesiredAccess, default, ProcessHandle.DangerousGetHandle(), StartRoutine.DangerousGetHandle(), Argument ?? default, CreateFlags, ZeroBits, StackSize, MaximumStackSize, default).ThrowOnFailure();
                ThreadHandle = new(hThread, true);
                return res;
            }
            finally
            {
                if (StartRoutineAddRef)
                {
                    StartRoutine.DangerousRelease();
                }
                if (ProcessHandleAddRef)
                {
                    ProcessHandle.DangerousRelease();
                }
            }
        }

        /// <summary>
        /// Terminates the specified thread and sets its exit status code.
        /// </summary>
        /// <remarks>This method wraps the native NtTerminateThread function from ntdll.dll. Terminating a
        /// thread can lead to resource leaks or inconsistent program state if not used carefully. Use this method only
        /// when it is necessary to forcibly terminate a thread.</remarks>
        /// <param name="ThreadHandle">A handle to the thread to be terminated. The handle must be valid and not closed.</param>
        /// <param name="ExitStatus">The exit status code to assign to the thread being terminated.</param>
        /// <returns>An NTSTATUS value indicating the result of the operation. Returns STATUS_SUCCESS if the thread was
        /// terminated successfully; otherwise, returns an error code.</returns>
        /// <exception cref="ArgumentNullException">Thrown if ThreadHandle is null or has already been closed.</exception>
        internal static NTSTATUS NtTerminateThread(SafeThreadHandle ThreadHandle, in NTSTATUS ExitStatus)
        {
            if (ThreadHandle is null || ThreadHandle.IsClosed)
            {
                throw new ArgumentNullException(nameof(ThreadHandle));
            }
            [DllImport("ntdll.dll", ExactSpelling = true), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
            static extern NTSTATUS NtTerminateThread(nint ThreadHandle, NTSTATUS ExitStatus);
            bool ThreadHandleAddRef = false;
            NTSTATUS res;
            try
            {
                ThreadHandle.DangerousAddRef(ref ThreadHandleAddRef);
                res = NtTerminateThread(ThreadHandle.DangerousGetHandle(), ExitStatus);
            }
            finally
            {
                if (ThreadHandleAddRef)
                {
                    ThreadHandle.DangerousRelease();
                }
            }
            return res != NTSTATUS.STATUS_SUCCESS && res != ExitStatus
                ? throw ExceptionUtilities.GetException(res)
                : res;
        }

        /// <summary>
        /// Retrieves information about the specified process by querying the native Windows NT API.
        /// </summary>
        /// <remarks>This method is a low-level interop call to the Windows NT kernel and is intended for
        /// advanced scenarios. The caller is responsible for ensuring that the ProcessInformation buffer is
        /// appropriately sized for the requested information class. Incorrect usage may result in partial or invalid
        /// data. This method may throw exceptions for certain NTSTATUS error codes.</remarks>
        /// <param name="ProcessHandle">A handle to the process to be queried. The handle must have appropriate access rights for the requested
        /// information.</param>
        /// <param name="ProcessInformationClass">The type of process information to retrieve. Specifies the class of information to be queried.</param>
        /// <param name="ProcessInformation">A span of bytes that receives the requested process information. The format and required size depend on the
        /// value of the ProcessInformationClass parameter. Must not be empty.</param>
        /// <param name="ReturnLength">When this method returns, contains the number of bytes written to ProcessInformation or, if the buffer was
        /// too small, the number of bytes required.</param>
        /// <returns>An NTSTATUS code that indicates the result of the operation. STATUS_SUCCESS indicates success;
        /// STATUS_INFO_LENGTH_MISMATCH indicates that the buffer was too small.</returns>
        /// <exception cref="ArgumentNullException">Thrown if ProcessHandle is null, closed, or invalid, or if ProcessInformation is empty.</exception>
        internal static NTSTATUS NtQueryInformationProcess(SafeHandle ProcessHandle, PROCESSINFOCLASS ProcessInformationClass, Span<byte> ProcessInformation, out uint ReturnLength)
        {
            if (ProcessHandle is null || ProcessHandle.IsClosed)
            {
                throw new ArgumentNullException(nameof(ProcessHandle));
            }
            bool ProcessHandleAddRef = false;
            NTSTATUS res;
            try
            {
                ProcessHandle.DangerousAddRef(ref ProcessHandleAddRef);
                unsafe
                {
                    fixed (byte* ProcessInformationLocal = ProcessInformation)
                    fixed (uint* ReturnLengthLocal = &ReturnLength)
                    {
                        res = Windows.Wdk.PInvoke.NtQueryInformationProcess((HANDLE)ProcessHandle.DangerousGetHandle(), ProcessInformationClass, ProcessInformationLocal, (uint)ProcessInformation.Length, ReturnLengthLocal);
                    }
                }
            }
            finally
            {
                if (ProcessHandleAddRef)
                {
                    ProcessHandle.DangerousRelease();
                }
            }
            return res != NTSTATUS.STATUS_SUCCESS && (res != NTSTATUS.STATUS_INFO_LENGTH_MISMATCH || ProcessInformation.Length != 0)
                ? throw ExceptionUtilities.GetException(res)
                : res;
        }

        /// <summary>
        /// Enumerates the modules in the specified process.
        /// </summary>
        /// <param name="hProcess">A handle to the process whose modules are to be enumerated. This handle must have the
        /// PROCESS_QUERY_INFORMATION and PROCESS_VM_READ access rights.</param>
        /// <param name="lphModule">When this method returns, contains a handle to the module. This parameter is passed uninitialized.</param>
        /// <param name="lpcbNeeded">When this method returns, contains the number of bytes required to store all module handles in the lphModule
        /// buffer.</param>
        /// <returns><see langword="true"/> if the function succeeds; otherwise, <see langword="false"/>.</returns>
        internal static BOOL EnumProcessModules(SafeHandle hProcess, Span<byte> lphModule, out uint lpcbNeeded)
        {
            bool hProcessAddRef = false;
            BOOL res;
            try
            {
                hProcess.DangerousAddRef(ref hProcessAddRef);
                res = PInvoke.EnumProcessModules(hProcess, lphModule, out lpcbNeeded);
            }
            finally
            {
                if (hProcessAddRef)
                {
                    hProcess.DangerousRelease();
                }
            }
            return !res ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : res;
        }

        /// <summary>
        /// Retrieves information about a specified module in the context of a given process.
        /// </summary>
        /// <param name="hProcess">A handle to the process that contains the module. This handle must have the PROCESS_QUERY_INFORMATION and
        /// PROCESS_VM_READ access rights.</param>
        /// <param name="hModule">A handle to the module whose information is to be retrieved.</param>
        /// <param name="lpmodinfo">When this method returns, contains a <see cref="MODULEINFO"/> structure that receives the module
        /// information.</param>
        /// <returns><see langword="true"/> if the function succeeds; otherwise, <see langword="false"/>.</returns>
        internal static BOOL GetModuleInformation(SafeHandle hProcess, in HMODULE hModule, out MODULEINFO lpmodinfo)
        {
            bool hProcessAddRef = false;
            BOOL res;
            try
            {
                hProcess.DangerousAddRef(ref hProcessAddRef);
                unsafe
                {
                    fixed (MODULEINFO* pModuleInfo = &lpmodinfo)
                    {
                        res = PInvoke.GetModuleInformation((HANDLE)hProcess.DangerousGetHandle(), hModule, pModuleInfo, (uint)Marshal.SizeOf<MODULEINFO>());
                    }
                }
            }
            finally
            {
                if (hProcessAddRef)
                {
                    hProcess.DangerousRelease();
                }
            }
            return !res ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : res;
        }

        /// <summary>
        /// Retrieves the full path of the executable image for the specified process.
        /// </summary>
        /// <remarks>This method is typically used to obtain the image file name of a process for
        /// diagnostic or monitoring purposes. The returned path is in device form (for example,
        /// '\\Device\\HarddiskVolume1\\Windows\\System32\\notepad.exe').</remarks>
        /// <param name="hProcess">A handle to the process whose executable image path is to be retrieved. The handle must have the
        /// PROCESS_QUERY_LIMITED_INFORMATION or PROCESS_QUERY_INFORMATION access right.</param>
        /// <param name="lpImageFileName">A span of characters that receives the full path to the executable image file. The buffer must be large
        /// enough to receive the path, including the null terminator.</param>
        /// <returns>The number of characters written to <paramref name="lpImageFileName"/>, not including the null terminator.
        /// Returns 0 if the function fails.</returns>
        internal static uint GetProcessImageFileName(SafeHandle hProcess, Span<char> lpImageFileName)
        {
            uint res = PInvoke.GetProcessImageFileName(hProcess, lpImageFileName);
            return res == 0 ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : res;
        }

        /// <summary>
        /// Retrieves the DPI (dots per inch) values for a specified monitor.
        /// </summary>
        /// <param name="hmonitor">A handle to the monitor for which the DPI values are retrieved.</param>
        /// <param name="dpiType">The type of DPI value to retrieve, such as effective, angular, or raw DPI.</param>
        /// <param name="dpiX">When the method returns, contains the DPI value along the horizontal axis.</param>
        /// <param name="dpiY">When the method returns, contains the DPI value along the vertical axis.</param>
        /// <returns>A <see cref="HRESULT"/> indicating the success or failure of the operation. A successful result indicates that the DPI values were retrieved successfully.</returns>
        internal static HRESULT GetDpiForMonitor(HMONITOR hmonitor, MONITOR_DPI_TYPE dpiType, out uint dpiX, out uint dpiY)
        {
            HRESULT res = PInvoke.GetDpiForMonitor(hmonitor, dpiType, out dpiX, out dpiY);
            return res != HRESULT.S_OK ? throw ExceptionUtilities.GetException(res) : res;
        }

        /// <summary>
        /// Retrieves the DPI (dots per inch) values for the default monitor.
        /// </summary>
        /// <remarks>This method uses the primary monitor as the default monitor and retrieves its DPI values based on the specified <paramref name="dpiType"/>. The caller is responsible for handling the returned <see cref="HRESULT"/> and any potential errors.</remarks>
        /// <param name="dpiType">The type of DPI value to retrieve. This determines whether the effective, angular, or raw DPI is returned.</param>
        /// <param name="dpiX">When the method returns, contains the horizontal DPI of the default monitor.</param>
        /// <param name="dpiY">When the method returns, contains the vertical DPI of the default monitor.</param>
        /// <returns>A <see cref="HRESULT"/> indicating the success or failure of the operation. A successful result indicates that the DPI values were retrieved successfully.</returns>
        internal static HRESULT GetDpiForDefaultMonitor(MONITOR_DPI_TYPE dpiType, out uint dpiX, out uint dpiY)
        {
            HRESULT res = GetDpiForMonitor(MonitorFromPoint(new(0, 0), MONITOR_FROM_FLAGS.MONITOR_DEFAULTTOPRIMARY), dpiType, out dpiX, out dpiY);
            return res != HRESULT.S_OK ? throw ExceptionUtilities.GetException(res) : res;
        }

        /// <summary>
        /// Sets the explicit Application User Model ID (AppUserModelID) for the current process.
        /// </summary>
        /// <remarks>Setting an explicit AppUserModelID allows the application's windows to be grouped and
        /// managed together in the Windows taskbar and Start menu. This method should be called before creating any
        /// windows to ensure consistent behavior.</remarks>
        /// <param name="AppID">The AppUserModelID to assign to the current process. This value is used by Windows to group windows and
        /// taskbar buttons. Cannot be null.</param>
        /// <returns>An HRESULT indicating the success or failure of the operation. A value of 0 indicates success; any other
        /// value indicates an error.</returns>
        internal static HRESULT SetCurrentProcessExplicitAppUserModelID(string AppID)
        {
            HRESULT res = PInvoke.SetCurrentProcessExplicitAppUserModelID(AppID);
            return res != HRESULT.S_OK ? throw ExceptionUtilities.GetException(res) : res;
        }

        /// <summary>
        /// Retrieves the current user notification state, indicating the user's availability for receiving
        /// notifications.
        /// </summary>
        /// <remarks>This method wraps the native SHQueryUserNotificationState function and throws an
        /// exception if the operation fails. The notification state can be used to determine whether it is appropriate
        /// to display notifications to the user.</remarks>
        /// <param name="pquns">When this method returns, contains a value from the QUERY_USER_NOTIFICATION_STATE enumeration that specifies
        /// the user's current notification state.</param>
        /// <returns>An HRESULT value indicating the success or failure of the operation.</returns>
        internal static HRESULT SHQueryUserNotificationState(out Windows.Win32.UI.Shell.QUERY_USER_NOTIFICATION_STATE pquns)
        {
            HRESULT res = PInvoke.SHQueryUserNotificationState(out pquns);
            return res != HRESULT.S_OK ? throw ExceptionUtilities.GetException(res) : res;
        }

        /// <summary>
        /// Notifies the system of a change to the shell namespace, such as the creation, deletion, or modification of
        /// files or folders.
        /// </summary>
        /// <remarks>This method is typically used to inform the Windows shell of changes made to the file
        /// system or shell items by applications, ensuring that the shell and other components remain in sync with the
        /// current state. The caller is responsible for ensuring that the parameters are valid and that the
        /// notification is appropriate for the event.</remarks>
        /// <param name="wEventId">A value that specifies the type of event that has occurred. This determines the kind of notification to
        /// send.</param>
        /// <param name="uFlags">Flags that indicate the meaning of the dwItem1 and dwItem2 parameters and how the notification is to be
        /// handled.</param>
        /// <param name="dwItem1">A pointer to an item or structure relevant to the event, as defined by the event type and flags. The
        /// interpretation depends on the values of wEventId and uFlags. This parameter is optional and may be
        /// default if not required.</param>
        /// <param name="dwItem2">A pointer to a second item or structure relevant to the event, as defined by the event type and flags. The
        /// interpretation depends on the values of wEventId and uFlags. This parameter is optional and may be
        /// default if not required.</param>
        internal static void SHChangeNotify([MarshalAs(UnmanagedType.I4)] SHCNE_ID wEventId, SHCNF_FLAGS uFlags, [Optional] nint dwItem1, [Optional] nint dwItem2)
        {
            unsafe
            {
                PInvoke.SHChangeNotify(wEventId, uFlags, (void*)dwItem1, (void*)dwItem2);
            }
        }

        /// <summary>
        /// Retrieves information about a file or folder, such as its icon, display name, and type, using the Windows
        /// Shell API.
        /// </summary>
        /// <remarks>This method is a managed wrapper for the native SHGetFileInfo function in
        /// shell32.dll. The caller is responsible for managing any resources associated with the returned handle, such
        /// as destroying icon handles if applicable. The method may return different types of handles depending on the
        /// flags specified in uFlags.</remarks>
        /// <param name="pszPath">The path to the file or folder for which to retrieve information. Can be null or an empty string if used
        /// with certain flags that do not require a path.</param>
        /// <param name="psfi">When this method returns, contains a structure that receives the file information retrieved by the function.</param>
        /// <param name="uFlags">A combination of flags that specify which file information to retrieve. These flags determine the attributes
        /// and details returned.</param>
        /// <param name="dwFileAttributes">File attribute flags to use when retrieving information. Used only if the uFlags parameter includes a flag
        /// indicating that file attributes are provided. The default is 0.</param>
        /// <returns>A handle to the system image list or icon, depending on the flags specified. The handle is valid only while
        /// the image list exists. Returns a non-zero value on success.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the file information could not be retrieved.</exception>
        internal static nint SHGetFileInfo(string pszPath, out SHFILEINFO psfi, SHGFI_FLAGS uFlags, FileAttributes dwFileAttributes = 0)
        {
            [DllImport("shell32.dll", CharSet = CharSet.Auto), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
            static extern nint SHGetFileInfoW(string pszPath, FileAttributes dwFileAttributes, ref SHFILEINFO psfi, uint cbFileInfo, SHGFI_FLAGS uFlags);
            psfi = new(); nint res = SHGetFileInfoW(pszPath, dwFileAttributes, ref psfi, (uint)Marshal.SizeOf(psfi), uFlags);
            return res == default ? throw ExceptionUtilities.GetException(WIN32_ERROR.ERROR_GEN_FAILURE, "Failed to retrieve file information.") : res;
        }

        /// <summary>
        /// Retrieves information about a specified stock icon, such as its handle, path, or index, based on the
        /// provided flags.
        /// </summary>
        /// <remarks>The caller should check the returned HRESULT to determine whether the operation
        /// succeeded. The contents of the returned SHSTOCKICONINFO structure depend on the flags specified in the
        /// uFlags parameter.</remarks>
        /// <param name="siid">The identifier of the stock icon to retrieve information for.</param>
        /// <param name="uFlags">A combination of flags that specify which information about the stock icon to retrieve.</param>
        /// <param name="psii">When this method returns, contains a structure that receives the requested stock icon information.</param>
        /// <returns>An HRESULT value indicating the success or failure of the operation.</returns>
        internal static HRESULT SHGetStockIconInfo(SHSTOCKICONID siid, SHGSI_FLAGS uFlags, out SHSTOCKICONINFO psii)
        {
            [DllImport("shell32.dll", CharSet = CharSet.Unicode), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
            static extern HRESULT SHGetStockIconInfo(SHSTOCKICONID siid, SHGSI_FLAGS uFlags, ref SHSTOCKICONINFO psii);
            psii = new() { cbSize = (uint)Marshal.SizeOf<SHSTOCKICONINFO>() };
            HRESULT res = SHGetStockIconInfo(siid, uFlags, ref psii);
            return res != HRESULT.S_OK ? throw ExceptionUtilities.GetException(res) : res;
        }

        /// <summary>
        /// Retrieves a handle to the system image list of the specified size.
        /// </summary>
        /// <remarks>This method is typically used to obtain a system image list for use with
        /// shell-related controls, such as list views or tree views. The caller is responsible for releasing the
        /// returned image list interface when it is no longer needed.</remarks>
        /// <param name="iImageList">The size of the image list to retrieve. This value specifies which system image list is returned.</param>
        /// <param name="ppvObj">When this method returns, contains the interface pointer to the retrieved image list. This parameter is
        /// passed uninitialized.</param>
        /// <returns>An HRESULT value indicating the success or failure of the operation.</returns>
        internal static HRESULT SHGetImageList(SHIL_SIZE iImageList, out IImageList ppvObj)
        {
            Guid riid = typeof(IImageList).GUID;
            HRESULT res = PInvoke.SHGetImageList((int)iImageList, in riid, out object ppvObjLocal);
            if (res != HRESULT.S_OK)
            {
                throw ExceptionUtilities.GetException(res);
            }
            ppvObj = (IImageList)ppvObjLocal;
            return res;
        }

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
            return res == HWND.Null ? throw ExceptionUtilities.GetException(WIN32_ERROR.ERROR_INVALID_HANDLE) : res;
        }

        /// <summary>
        /// Loads a string resource from the specified module instance using the provided resource identifier.
        /// </summary>
        /// <remarks>If the function fails and a Windows error code is set, an exception is thrown to
        /// indicate the specific error. The caller must ensure that the provided module handle remains valid for the
        /// duration of the call.</remarks>
        /// <param name="hInstance">A handle to the module that contains the string resource. This handle must be valid and is typically
        /// obtained from a previous call to a function such as LoadLibrary.</param>
        /// <param name="uID">The identifier of the string resource to load. This value must correspond to a valid string resource in the
        /// specified module.</param>
        /// <param name="lpBuffer">When this method returns, contains a pointer to the buffer that receives the string resource. The caller is
        /// responsible for managing the memory of this buffer.</param>
        /// <returns>The length of the loaded string, in characters. Returns zero if the function fails; in that case, an
        /// exception is thrown if a Windows error code is set.</returns>
        internal static int LoadString(SafeHandle hInstance, uint uID, out nint lpBuffer)
        {
            [DllImport("USER32.dll", ExactSpelling = true, EntryPoint = "LoadStringW", SetLastError = true), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
            static extern int LoadString(HINSTANCE hInstance, uint uID, out nint lpBuffer, int cchBufferMax);
            bool hInstanceAddRef = false;
            int res;
            try
            {
                hInstance.DangerousAddRef(ref hInstanceAddRef);
                res = LoadString((HINSTANCE)hInstance.DangerousGetHandle(), uID, out lpBuffer, 0);
            }
            finally
            {
                if (hInstanceAddRef)
                {
                    hInstance.DangerousRelease();
                }
            }
            return res == 0 && (ExceptionUtilities.GetLastWin32Error() is WIN32_ERROR lastWin32Error) && lastWin32Error != WIN32_ERROR.NO_ERROR
                ? throw ExceptionUtilities.GetException(lastWin32Error)
                : res;
        }

        /// <summary>
        /// Loads a string resource from the specified module instance using the given resource identifier.
        /// </summary>
        /// <remarks>This method retrieves a string resource from a native module and converts it to a
        /// managed string. Ensure that the resource identifier is valid and that the module handle is properly
        /// initialized before calling this method.</remarks>
        /// <param name="hInstance">A handle to the module instance that contains the string resource to be loaded.</param>
        /// <param name="uID">The identifier of the string resource to load.</param>
        /// <param name="lpBuffer">When this method returns, contains the loaded string if successful; otherwise, null.</param>
        /// <returns>The number of characters copied to the buffer, or zero if the string resource could not be loaded.</returns>
        internal static int LoadString(SafeHandle hInstance, uint uID, out string? lpBuffer)
        {
            int res = LoadString(hInstance, uID, out nint lpBufferPtr);
            lpBuffer = res > 0 ? Marshal.PtrToStringUni(lpBufferPtr, res) : null;
            return res;
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
            return res == 0 && (ExceptionUtilities.GetLastWin32Error() is WIN32_ERROR lastWin32Error) && lastWin32Error != WIN32_ERROR.NO_ERROR
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
            return res.IsInvalid ? throw ExceptionUtilities.GetException(WIN32_ERROR.ERROR_INVALID_HANDLE) : res;
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
            return ExceptionUtilities.GetLastWin32Error() is WIN32_ERROR lastWin32Error && lastWin32Error != WIN32_ERROR.NO_ERROR
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
            return monitor.IsNull ? throw ExceptionUtilities.GetException(WIN32_ERROR.ERROR_INVALID_HANDLE) : monitor;
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
            return res == 0 ? throw ExceptionUtilities.GetException(WIN32_ERROR.ERROR_GEN_FAILURE, "Failed to get DPI scale for window handle.") : res;
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
        /// <param name="noThrowOnFailure">If set to <see langword="true"/>, the method will not throw an exception on failure.</param>
        /// <returns><see langword="true"/> if the operation succeeds; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the operation fails to set the specified window as the foreground window.</exception>
        internal static BOOL SetForegroundWindow(HWND hWnd, bool noThrowOnFailure = false)
        {
            BOOL res = PInvoke.SetForegroundWindow(hWnd);
            return !res && !noThrowOnFailure ? throw ExceptionUtilities.GetException(WIN32_ERROR.ERROR_GEN_FAILURE, "Failed to set the window as foreground.") : res;
        }

        /// <summary>
        /// Retrieves the handle to the shell's desktop window.
        /// </summary>
        /// <returns>A <see cref="HWND"/> representing the handle to the shell's desktop window.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the shell window handle cannot be retrieved.</exception>
        internal static HWND GetShellWindow()
        {
            HWND res = PInvoke.GetShellWindow();
            return res.IsNull ? throw ExceptionUtilities.GetException(WIN32_ERROR.ERROR_INVALID_HANDLE) : res;
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
            plii = new() { cbSize = (uint)Marshal.SizeOf<LASTINPUTINFO>() }; BOOL res = PInvoke.GetLastInputInfo(ref plii);
            return !res ? throw ExceptionUtilities.GetException(WIN32_ERROR.ERROR_GEN_FAILURE, "Failed to retrieve the last input info.") : res;
        }

        /// <summary>
        /// Creates a new environment block for the specified user token, allowing environment variables to be inherited
        /// by child processes.
        /// </summary>
        /// <remarks>Throws an exception if the environment block cannot be created. The exception
        /// provides details based on the last Win32 error code.</remarks>
        /// <param name="lpEnvironment">When this method returns, contains a handle to the newly created environment block. The caller is
        /// responsible for releasing this handle when it is no longer needed.</param>
        /// <param name="hToken">A handle to the access token for the user whose environment block is to be created. The token must have
        /// appropriate permissions.</param>
        /// <param name="bInherit">A value that specifies whether the environment block should be inheritable by child processes. Specify <see
        /// langword="true"/> to allow inheritance; otherwise, <see langword="false"/>.</param>
        /// <returns>Returns <see langword="true"/> if the environment block is successfully created; otherwise, <see
        /// langword="false"/>.</returns>
        internal static BOOL CreateEnvironmentBlock(out SafeEnvironmentBlockHandle lpEnvironment, SafeFileHandle hToken, BOOL bInherit)
        {
            BOOL res;
            unsafe
            {
                res = PInvoke.CreateEnvironmentBlock(out void* lpEnvironmentPtr, hToken, bInherit);
                if (!res)
                {
                    throw ExceptionUtilities.GetExceptionForLastWin32Error();
                }
                lpEnvironment = new((nint)lpEnvironmentPtr, true);
            }
            return res;
        }

        /// <summary>
        /// Queries version information from the specified version-information resource.
        /// </summary>
        /// <param name="pBlock">A handle to the memory block containing the version-information resource. This handle must be valid and not
        /// null.</param>
        /// <param name="lpSubBlock">The version-information value to be retrieved. This string must specify a valid sub-block within the
        /// version-information resource.</param>
        /// <param name="lplpBuffer">When this method returns, contains a pointer to the requested version-information value. This parameter is
        /// passed uninitialized.</param>
        /// <param name="puLen">When this method returns, contains the length, in bytes, of the data pointed to by <paramref
        /// name="lplpBuffer"/>. This parameter is passed uninitialized.</param>
        /// <returns><see langword="true"/> if the specified version-information value is successfully retrieved; otherwise, <see
        /// langword="false"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the version-information value cannot be queried.</exception>
        internal static BOOL VerQueryValue(ReadOnlySpan<byte> pBlock, string lpSubBlock, out nint lplpBuffer, out uint puLen)
        {
            BOOL res;
            unsafe
            {
                fixed (byte* pBlockPtr = pBlock)
                {
                    res = PInvoke.VerQueryValue(pBlockPtr, lpSubBlock, out void* lplpBufferLocal, out puLen);
                    if (!res)
                    {
                        throw ExceptionUtilities.GetException(WIN32_ERROR.ERROR_GEN_FAILURE, $"Failed to query [{lpSubBlock}] version value.");
                    }
                    lplpBuffer = (nint)lplpBufferLocal;
                }
            }
            return res;
        }

        /// <summary>
        /// Verifies the trustworthiness of a specified object using the provided action identifier and verification
        /// data.
        /// </summary>
        /// <remarks>If the trust verification fails or if the provided parameters are invalid, an
        /// exception is thrown. Ensure that the action identifier and verification data are valid and appropriate for
        /// the intended verification operation.</remarks>
        /// <param name="hwnd">A handle to the parent window to be used for any user interface that may be displayed during the trust
        /// verification process.</param>
        /// <param name="pgActionID">A reference to a GUID that specifies the action to be performed during the trust verification.</param>
        /// <param name="pWVTData">A pointer to a structure containing additional data required for the trust verification. The structure and
        /// its contents depend on the action specified by the action identifier.</param>
        /// <returns>An integer value indicating the result of the trust verification. A value of 0 indicates that the
        /// verification succeeded.</returns>
        internal static unsafe HRESULT WinVerifyTrust(HWND hwnd, ref Guid pgActionID, void* pWVTData)
        {
            HRESULT res = (HRESULT)PInvoke.WinVerifyTrust(hwnd, ref pgActionID, pWVTData);
            return res != HRESULT.S_OK ? throw ExceptionUtilities.GetException(res) : res;
        }

        /// <summary>
        /// Enumerates processes running on a specified server and retrieves detailed process information.
        /// </summary>
        /// <remarks>This method wraps the Windows API function <c>WTSEnumerateProcessesEx</c> and
        /// provides a managed interface for enumerating processes. The level of detail returned is determined by the
        /// <paramref name="pLevel"/> parameter: <list type="bullet"> <item><description>Level 0 returns basic process
        /// information (<c>WTS_PROCESS_INFOW</c>).</description></item> <item><description>Level 1 returns extended
        /// process information (<c>WTS_PROCESS_INFO_EXW</c>).</description></item> </list> The <paramref
        /// name="pProcessInfo"/> parameter must be disposed to avoid memory leaks.</remarks>
        /// <param name="hServer">A handle to the server on which the processes are to be enumerated. Use <see langword="null"/> to specify
        /// the local server.</param>
        /// <param name="pLevel">The level of detail for the process information. Use 0 for basic information or 1 for extended information.</param>
        /// <param name="SessionId">The session ID for which processes are to be enumerated. Use 0 to enumerate processes for all sessions.</param>
        /// <param name="pProcessInfo">When the method returns, contains a <see cref="SafeWtsExHandle"/> object that holds the enumerated process
        /// information. The caller is responsible for disposing of this handle to release the allocated resources.</param>
        /// <returns><see langword="true"/> if the operation succeeds; otherwise, <see langword="false"/>.</returns>
        internal static BOOL WTSEnumerateProcessesEx(HANDLE hServer, uint pLevel, uint SessionId, out SafeWtsExHandle pProcessInfo)
        {
            if (pLevel > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(pLevel), "pLevel must be 0 or 1.");
            }
            BOOL res;
            unsafe
            {
                PWSTR ppProcessInfo; uint pCount;
                res = PInvoke.WTSEnumerateProcessesEx(hServer, &pLevel, SessionId, &ppProcessInfo, &pCount);
                if (!res)
                {
                    throw ExceptionUtilities.GetExceptionForLastWin32Error();
                }
                pProcessInfo = pLevel > 0
                    ? new((nint)ppProcessInfo.Value, WTS_TYPE_CLASS.WTSTypeProcessInfoLevel1, (int)pCount * sizeof(WTS_PROCESS_INFO_EXW), true)
                    : new((nint)ppProcessInfo.Value, WTS_TYPE_CLASS.WTSTypeProcessInfoLevel0, (int)pCount * sizeof(WTS_PROCESS_INFOW), true);
            }
            return res;
        }

        /// <summary>
        /// Enumerates the sessions on the specified Remote Desktop Session Host (RD Session Host) server.
        /// </summary>
        /// <remarks>If the method returns <see langword="false"/>, an exception is thrown containing the
        /// relevant Win32 error information. The returned SafeWtsHandle must be disposed to free the associated
        /// unmanaged resources.</remarks>
        /// <param name="hServer">A handle to an RD Session Host server. This handle must be opened with appropriate access rights.</param>
        /// <param name="pSessionInfo">When this method returns, contains a SafeWtsHandle that encapsulates the session information buffer. The
        /// caller is responsible for releasing the handle when it is no longer needed.</param>
        /// <returns>A value that is <see langword="true"/> if the session enumeration succeeds; otherwise, <see
        /// langword="false"/>.</returns>
        internal static BOOL WTSEnumerateSessions(HANDLE hServer, out SafeWtsHandle pSessionInfo)
        {
            BOOL res;
            unsafe
            {
                res = PInvoke.WTSEnumerateSessions(hServer, 0, 1, out WTS_SESSION_INFOW* ppSessionInfo, out uint pCount);
                if (!res)
                {
                    throw ExceptionUtilities.GetExceptionForLastWin32Error();
                }
                pSessionInfo = new((nint)ppSessionInfo, (int)pCount * sizeof(WTS_SESSION_INFOW), true);
            }
            return res;
        }

        /// <summary>
        /// Retrieves information about a specified session on a Remote Desktop Services server.
        /// </summary>
        /// <remarks>If the operation fails, an exception is thrown containing the relevant Win32 error
        /// information. The format and content of the returned buffer depend on the value of the WTSInfoClass
        /// parameter.</remarks>
        /// <param name="hServer">A handle to the server from which to retrieve session information. This handle must be obtained by calling
        /// the WTSOpenServer function.</param>
        /// <param name="SessionId">The identifier of the session for which information is being requested. This value is typically obtained
        /// from a previous call to WTSEnumerateSessions.</param>
        /// <param name="WTSInfoClass">A value that specifies the type of session information to retrieve. The value must be a member of the
        /// WTS_INFO_CLASS enumeration.</param>
        /// <param name="pBuffer">When this method returns, contains a SafeWtsHandle that encapsulates a buffer with the requested session
        /// information. The caller is responsible for disposing of this handle when it is no longer needed.</param>
        /// <returns>A value that indicates whether the operation succeeded. Returns <see langword="true"/> if the information
        /// was retrieved successfully; otherwise, <see langword="false"/>.</returns>
        internal static BOOL WTSQuerySessionInformation(HANDLE hServer, uint SessionId, WTS_INFO_CLASS WTSInfoClass, out SafeWtsHandle pBuffer)
        {
            BOOL res;
            res = PInvoke.WTSQuerySessionInformation(hServer, SessionId, WTSInfoClass, out PWSTR ppBuffer, out uint bytesReturned);
            if (!res)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            pBuffer = new(ppBuffer.ToIntPtr(), (int)bytesReturned, true);
            return res;
        }

        /// <summary>
        /// Retrieves the primary access token of the user associated with the specified Remote Desktop Services
        /// session.
        /// </summary>
        /// <remarks>This method throws an exception if the underlying native call fails. The returned
        /// token can be used to impersonate the user or to launch processes in the user's context.</remarks>
        /// <param name="SessionId">The identifier of the Remote Desktop Services session for which to retrieve the user token.</param>
        /// <param name="phToken">When this method returns, contains a handle to the primary token of the user associated with the specified
        /// session. The caller is responsible for releasing the handle.</param>
        /// <returns>A value that indicates whether the operation succeeded. Returns <see langword="true"/> if the token was
        /// retrieved successfully; otherwise, <see langword="false"/>.</returns>
        internal static BOOL WTSQueryUserToken(uint SessionId, out SafeFileHandle phToken)
        {
            BOOL res;
            unsafe
            {
                HANDLE phTokenLocal;
                res = PInvoke.WTSQueryUserToken(SessionId, &phTokenLocal);
                if (!res)
                {
                    throw ExceptionUtilities.GetExceptionForLastWin32Error();
                }
                phToken = new(phTokenLocal, true);
            }
            return res;
        }

        /// <summary>
        /// Applies a Windows Installer transform to the specified database using the provided transform file and error
        /// condition flags.
        /// </summary>
        /// <remarks>This method throws an exception if the operation fails, allowing callers to handle
        /// errors using standard exception handling mechanisms.</remarks>
        /// <param name="hDatabase">A handle to the database to which the transform will be applied. This handle must be valid and open for
        /// modification.</param>
        /// <param name="szTransformFile">The path to the transform file that contains the changes to be applied to the database. This value cannot be
        /// null or empty.</param>
        /// <param name="iErrorConditions">A combination of MSITRANSFORM_ERROR flags that specify which error conditions should be considered when
        /// applying the transform. The default is MSITRANSFORM_ERROR_NONE.</param>
        /// <returns>A WIN32_ERROR code that indicates the result of the operation. A value of ERROR_SUCCESS indicates success;
        /// otherwise, the error code represents the failure reason.</returns>
        internal static WIN32_ERROR MsiDatabaseApplyTransform(SafeHandle hDatabase, string szTransformFile, MSITRANSFORM_ERROR iErrorConditions = MSITRANSFORM_ERROR.MSITRANSFORM_ERROR_NONE)
        {
            return ((WIN32_ERROR)PInvoke.MsiDatabaseApplyTransform(hDatabase, szTransformFile, iErrorConditions)).ThrowOnFailure();
        }

        /// <summary>
        /// Lookup table for system information class struct sizes.
        /// </summary>
        internal static ReadOnlyDictionary<SYSTEM_INFORMATION_CLASS, int> SystemInfoClassSizes = new(new Dictionary<SYSTEM_INFORMATION_CLASS, int>()
        {
            { SYSTEM_INFORMATION_CLASS.SystemExtendedHandleInformation, Marshal.SizeOf<SYSTEM_HANDLE_INFORMATION_EX>() + Marshal.SizeOf<SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX>() },
            { SYSTEM_INFORMATION_CLASS.SystemProcessIdInformation, Marshal.SizeOf<SYSTEM_PROCESS_ID_INFORMATION>() },
        });

        /// <summary>
        /// Lookup table for object information class struct sizes.
        /// </summary>
        internal static ReadOnlyDictionary<OBJECT_INFORMATION_CLASS, int> ObjectInfoClassSizes = new(new Dictionary<OBJECT_INFORMATION_CLASS, int>()
        {
            { OBJECT_INFORMATION_CLASS.ObjectNameInformation, Marshal.SizeOf<OBJECT_NAME_INFORMATION>() },
            { OBJECT_INFORMATION_CLASS.ObjectTypeInformation, Marshal.SizeOf<OBJECT_TYPE_INFORMATION>() },
            { OBJECT_INFORMATION_CLASS.ObjectTypesInformation, Marshal.SizeOf<OBJECT_TYPES_INFORMATION>() }
        });

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
