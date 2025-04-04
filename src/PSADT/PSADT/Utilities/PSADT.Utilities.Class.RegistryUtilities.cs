using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using PSADT.LibraryInterfaces;
using Windows.Win32.Foundation;
using Windows.Win32.System.Registry;

namespace PSADT.Utilities
{
    /// <summary>
    /// Registry utilities using CsWin32.
    /// </summary>
    public static class RegistryUtilities
    {
        /// <summary>
        /// Registry hive lookup table.
        /// </summary>
        private static readonly ReadOnlyDictionary<string, HKEY> HiveMap = new ReadOnlyDictionary<string, HKEY>(new Dictionary<string, HKEY>(StringComparer.OrdinalIgnoreCase)
        {
            { "HKEY_LOCAL_MACHINE", HKEY.HKEY_LOCAL_MACHINE },
            { "HKEY_CURRENT_USER", HKEY.HKEY_CURRENT_USER },
            { "HKEY_CLASSES_ROOT", HKEY.HKEY_LOCAL_MACHINE },
            { "HKEY_USERS", HKEY.HKEY_USERS },
            { "HKEY_CURRENT_CONFIG", HKEY.HKEY_CURRENT_CONFIG }
        });

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
            var parts = fullKeyPath.Replace(@"Microsoft.PowerShell.Core\Registry::", null).Split(new[] { '\\' }, 2);
            if (parts.Length < 2)
            {
                throw new ArgumentException("Invalid registry key format.", nameof(fullKeyPath));
            }
            string hiveName = parts[0];
            string subKeyPath = parts[1];

            // Validate and get the correct handle for the root hive.
            if (!HiveMap.TryGetValue(hiveName, out HKEY hKeyRoot))
            {
                throw new ArgumentException($"Invalid registry hive: {hiveName}", nameof(fullKeyPath));
            }

            // Open the registry key using the root hive's handle.
            var result = AdvApi32.RegOpenKeyEx(hKeyRoot, subKeyPath, 0, REG_SAM_FLAGS.KEY_READ, out HKEY hKey);
            if (result != WIN32_ERROR.ERROR_SUCCESS || hKey.IsNull)
            {
                throw new Win32Exception((int)result, "Failed to open the registry key.");
            }

            // Get the modified time from the registry.
            try
            {
                result = AdvApi32.RegQueryInfoKey(hKey, null, out _, out _, out _, out _, out _, out _, out _, out _, out _, out var lastWriteTime);
                return DateTime.FromFileTime((long)lastWriteTime.dwHighDateTime << 32 | lastWriteTime.dwLowDateTime & 0xFFFFFFFFL);
            }
            finally
            {
                // Ensure the handle is properly disposed.
                AdvApi32.RegCloseKey(ref hKey);
            }
        }
    }
}
