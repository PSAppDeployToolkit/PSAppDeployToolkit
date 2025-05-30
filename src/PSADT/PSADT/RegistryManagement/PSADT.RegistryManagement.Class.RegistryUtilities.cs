using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Win32.SafeHandles;
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

            // Open the registry key and get the modified time it.
            AdvApi32.RegOpenKeyEx(hKeyRoot, subKeyPath, 0, REG_SAM_FLAGS.KEY_READ, out var hKey);
            using (hKey)
            {
                AdvApi32.RegQueryInfoKey(hKey, null, IntPtr.Zero, out _, out _, out _, out _, out _, out _, out _, out var lastWriteTime);
                return DateTime.FromFileTime((long)lastWriteTime.dwHighDateTime << 32 | lastWriteTime.dwLowDateTime & 0xFFFFFFFFL);
            }
        }

        /// <summary>
        /// Registry hive lookup table.
        /// </summary>
        private static readonly ReadOnlyDictionary<string, SafeRegistryHandle> HiveMap = new ReadOnlyDictionary<string, SafeRegistryHandle>(new Dictionary<string, SafeRegistryHandle>()
        {
            { "HKEY_LOCAL_MACHINE", new SafeRegistryHandle(HKEY.HKEY_LOCAL_MACHINE, false) },
            { "HKEY_CURRENT_USER", new SafeRegistryHandle(HKEY.HKEY_CURRENT_USER, false) },
            { "HKEY_CLASSES_ROOT", new SafeRegistryHandle(HKEY.HKEY_LOCAL_MACHINE, false) },
            { "HKEY_USERS", new SafeRegistryHandle(HKEY.HKEY_USERS, false) },
            { "HKEY_CURRENT_CONFIG", new SafeRegistryHandle(HKEY.HKEY_CURRENT_CONFIG, false) }
        });
    }
}
