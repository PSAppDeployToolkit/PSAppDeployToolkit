using System;
using System.Runtime.InteropServices.ComTypes;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using PSADT.Extensions;
using PSADT.Interop;
using PSADT.Interop.Extensions;
using Windows.Win32.System.Registry;

namespace PSADT.Utilities
{
    /// <summary>
    /// Registry utilities using CsWin32.
    /// </summary>
    public static class RegistryUtilities
    {
        /// <summary>
        /// Retrieves the last write time of the specified registry key.
        /// </summary>
        /// <remarks>This method opens the registry key specified by the fullKeyPath and queries its last
        /// write time. Ensure that the provided key path is valid and accessible.</remarks>
        /// <param name="fullKeyPath">The full path of the registry key whose last write time is to be retrieved. This parameter cannot be null or
        /// empty.</param>
        /// <returns>A DateTime representing the last write time of the specified registry key.</returns>
        public static DateTime GetRegistryKeyLastWriteTime(string fullKeyPath)
        {
            using SafeRegistryHandle hKey = OpenRegistryKey(fullKeyPath);
            _ = NativeMethods.RegQueryInfoKey(hKey, null, out _, out _, out _, out _, out _, out _, out _, out _, out FILETIME lastWriteTime);
            return lastWriteTime.ToDateTime();
        }

        /// <summary>
        /// Renames a subkey within the specified registry key path.
        /// </summary>
        /// <remarks>This method uses the Windows API to rename a registry subkey. Ensure that the caller
        /// has sufficient permissions to modify the registry and that the specified key path and subkey names are
        /// valid.</remarks>
        /// <param name="keyPath">The path of the registry key containing the subkey to rename. This must be a valid registry key path.</param>
        /// <param name="subKeyName">The name of the subkey to rename. If this is null, the key path is what will be renamed.</param>
        /// <param name="newKeyName">The new name for the subkey. This cannot be null or empty.</param>
        public static void RenameRegistryKey(string keyPath, string? subKeyName, string newKeyName)
        {
            using SafeRegistryHandle hKey = OpenRegistryKey(keyPath, REG_SAM_FLAGS.KEY_READ | REG_SAM_FLAGS.KEY_WRITE);
            _ = NativeMethods.RegRenameKey(hKey, subKeyName, newKeyName);
        }

        /// <summary>
        /// Retrieves a read-only registry key corresponding to the specified registry path.
        /// </summary>
        /// <remarks>The returned registry key is opened in read-only mode. The method supports both full
        /// and abbreviated hive names. The caller is responsible for disposing the returned <see cref="RegistryKey"/>
        /// when it is no longer needed.</remarks>
        /// <param name="keyPath">The full registry key path, including the hive name (e.g., "HKEY_LOCAL_MACHINE\Software\MyApp"). Abbreviated
        /// hive names such as "HKLM" are also supported. Cannot be null or empty.</param>
        /// <param name="writable">Indicates whether the returned <see cref="RegistryKey"/> should be opened with write access.</param>
        /// <returns>A read-only <see cref="RegistryKey"/> object representing the specified registry key.</returns>
        /// <exception cref="FormatException">Thrown if <paramref name="keyPath"/> is null, empty, not in a valid format, specifies an unrecognized hive,
        /// or if the specified registry key does not exist.</exception>
        internal static RegistryKey GetRegistryKeyForPath(string keyPath, bool writable)
        {
            keyPath = keyPath.Replace(@"Microsoft.PowerShell.Core\Registry::", null);
            int firstBackslashIndex = keyPath.IndexOf('\\');
            if (firstBackslashIndex == -1)
            {
                throw new FormatException("Invalid registry key format.");
            }
            string hiveName = keyPath.Substring(0, firstBackslashIndex);
            RegistryKey baseKey = hiveName switch
            {
                "HKEY_LOCAL_MACHINE" or "HKLM" => Registry.LocalMachine,
                "HKEY_CURRENT_USER" or "HKCU" => Registry.CurrentUser,
                "HKEY_CLASSES_ROOT" or "HKCR" => Registry.ClassesRoot,
                "HKEY_USERS" or "HKU" => Registry.Users,
                "HKEY_CURRENT_CONFIG" or "HKCC" => Registry.CurrentConfig,
                _ => throw new FormatException($"Invalid registry hive: {hiveName}"),
            };
            return baseKey.OpenSubKey(keyPath.Substring(firstBackslashIndex + 1), writable) ?? throw new InvalidOperationException("The specified registry key does not exist."); ;
        }

        /// <summary>
        /// Opens a registry key specified by its full path and returns a handle to the key.
        /// </summary>
        /// <remarks>The method validates the input path, determines the appropriate registry hive, and
        /// opens the specified subkey with read-only access. The caller is responsible for disposing of the returned
        /// <see cref="SafeRegistryHandle"/> to release the associated resources.</remarks>
        /// <param name="fullKeyPath">The full path of the registry key to open, including the hive name (e.g.,
        /// "HKEY_LOCAL_MACHINE\Software\Example").</param>
        /// <param name="openFlags">The access flags to use when opening the registry key. Defaults to <see cref="REG_SAM_FLAGS.KEY_READ"/>.</param>
        /// <returns>A <see cref="SafeRegistryHandle"/> representing the opened registry key.</returns>
        /// <exception cref="FormatException">Thrown if <paramref name="fullKeyPath"/> is null, empty, or not in a valid registry key format.</exception>
        private static SafeRegistryHandle OpenRegistryKey(string fullKeyPath, REG_SAM_FLAGS openFlags = REG_SAM_FLAGS.KEY_READ)
        {
            // Split hive and subkey so we know what root hive we're accessing.
            string[] parts = fullKeyPath.ThrowIfNullOrWhiteSpace().Replace(@"Microsoft.PowerShell.Core\Registry::", null).Split(['\\'], 2);
            if (parts.Length < 2)
            {
                throw new FormatException("Invalid registry key format.");
            }
            string hiveName = parts[0];
            string subKeyPath = parts[1];

            // Open the registry key and return it to the caller.
            using SafeRegistryHandle hKeyRoot = GetRegistryHiveHandle(hiveName);
            _ = NativeMethods.RegOpenKeyEx(hKeyRoot, subKeyPath, openFlags, out SafeRegistryHandle hKey);
            return hKey;
        }

        /// <summary>
        /// Retrieves a handle to the specified registry hive.
        /// </summary>
        /// <param name="hiveName">The name of the registry hive to retrieve. Supported values are <c>"HKEY_LOCAL_MACHINE"</c>,
        /// <c>"HKEY_CURRENT_USER"</c>, <c>"HKEY_CLASSES_ROOT"</c>, <c>"HKEY_USERS"</c>, and <c>"HKEY_CURRENT_CONFIG"</c>.</param>
        /// <returns>A <see cref="SafeRegistryHandle"/> representing the handle to the specified
        /// registry hive.</returns>
        /// <exception cref="FormatException">Thrown if <paramref name="hiveName"/> is not one of the supported registry hive names.</exception>
        private static SafeRegistryHandle GetRegistryHiveHandle(string hiveName)
        {
            return hiveName switch
            {
                "HKEY_LOCAL_MACHINE" => new(HKEY.HKEY_LOCAL_MACHINE, false),
                "HKEY_CURRENT_USER" => new(HKEY.HKEY_CURRENT_USER, false),
                "HKEY_CLASSES_ROOT" => new(HKEY.HKEY_CLASSES_ROOT, false),
                "HKEY_USERS" => new(HKEY.HKEY_USERS, false),
                "HKEY_CURRENT_CONFIG" => new(HKEY.HKEY_CURRENT_CONFIG, false),
                _ => throw new FormatException($"Invalid registry hive: {hiveName}"),
            };
        }
    }
}
