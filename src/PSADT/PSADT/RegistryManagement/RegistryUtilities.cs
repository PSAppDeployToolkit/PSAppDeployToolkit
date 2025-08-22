using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Win32.SafeHandles;
using PSADT.Extensions;
using PSADT.LibraryInterfaces;
using Windows.Win32.System.Registry;

namespace PSADT.RegistryManagement
{
    /// <summary>
    /// Registry utilities using CsWin32.
    /// </summary>
    public static class RegistryUtilities
    {
        /// <summary>
        /// Returns a given registry key path's LastWriteTime as a DateTime object.
        /// </summary>
        /// <param name="fullKeyPath"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static DateTime GetRegistryKeyLastWriteTime(string fullKeyPath)
        {
            using var hKey = OpenRegistryKey(fullKeyPath);
            AdvApi32.RegQueryInfoKey(hKey, null, out _, out _, out _, out _, out _, out _, out _, out _, out var lastWriteTime);
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
            using var hKey = OpenRegistryKey(keyPath, REG_SAM_FLAGS.KEY_READ | REG_SAM_FLAGS.KEY_WRITE);
            AdvApi32.RegRenameKey(hKey, subKeyName, newKeyName);
        }

        /// <summary>
        /// Opens a registry key specified by its full path and returns a handle to the key.
        /// </summary>
        /// <remarks>The method validates the input path, determines the appropriate registry hive, and
        /// opens the specified subkey with read-only access. The caller is responsible for disposing of the returned
        /// <see cref="Microsoft.Win32.SafeHandles.SafeRegistryHandle"/>  to release the associated resources.</remarks>
        /// <param name="fullKeyPath">The full path of the registry key to open, including the hive name (e.g.,
        /// "HKEY_LOCAL_MACHINE\Software\Example").</param>
        /// <returns>A <see cref="Microsoft.Win32.SafeHandles.SafeRegistryHandle"/> representing the opened registry key.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="fullKeyPath"/> is null, empty, or not in a valid registry key format.</exception>
        private static SafeRegistryHandle OpenRegistryKey(string fullKeyPath, REG_SAM_FLAGS openFlags = REG_SAM_FLAGS.KEY_READ)
        {
            // Ensure the supplied input isn't null.
            if (string.IsNullOrWhiteSpace(fullKeyPath))
            {
                throw new ArgumentException("Registry path cannot be empty.", nameof(fullKeyPath));
            }

            // Split hive and subkey so we know what root hive we're accessing.
            var parts = fullKeyPath.Replace(@"Microsoft.PowerShell.Core\Registry::", null).Split(['\\'], 2);
            if (parts.Length < 2)
            {
                throw new ArgumentException("Invalid registry key format.", nameof(fullKeyPath));
            }
            string hiveName = parts[0];
            string subKeyPath = parts[1];

            // Validate and get the correct handle for the root hive.
            if (!HiveMap.TryGetValue(hiveName, out var hKeyRoot))
            {
                throw new ArgumentException($"Invalid registry hive: {hiveName}", nameof(fullKeyPath));
            }

            // Open the registry key and return it to the caller.
            AdvApi32.RegOpenKeyEx(hKeyRoot, subKeyPath, openFlags, out var hKey);
            return hKey;
        }

        /// <summary>
        /// Registry hive lookup table.
        /// </summary>
        private static readonly ReadOnlyDictionary<string, SafeRegistryHandle> HiveMap = new(new Dictionary<string, SafeRegistryHandle>()
        {
            { "HKEY_LOCAL_MACHINE", new(HKEY.HKEY_LOCAL_MACHINE, false) },
            { "HKEY_CURRENT_USER", new(HKEY.HKEY_CURRENT_USER, false) },
            { "HKEY_CLASSES_ROOT", new(HKEY.HKEY_LOCAL_MACHINE, false) },
            { "HKEY_USERS", new(HKEY.HKEY_USERS, false) },
            { "HKEY_CURRENT_CONFIG", new(HKEY.HKEY_CURRENT_CONFIG, false) }
        });
    }
}
