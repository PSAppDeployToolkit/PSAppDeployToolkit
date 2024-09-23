using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;

namespace PSADT.Registry
{
    /// <summary>
    /// Provides P/Invoke signatures for interacting with native Windows Registry APIs.
    /// </summary>
    internal static class NativeMethods
    {
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
    }
}
