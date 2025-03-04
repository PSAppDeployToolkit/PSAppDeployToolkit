using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Win32.SafeHandles;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Registry;

namespace PSADT.Registry
{
    /// <summary>
    /// Registry utilities using CsWin32.
    /// </summary>
    public static class Utilities
    {
        /// <summary>
        /// Registry hive to SafeRegistryHandle lookup table.
        /// </summary>
        private static readonly ReadOnlyDictionary<string, SafeRegistryHandle> HiveMap = new ReadOnlyDictionary<string, SafeRegistryHandle>(new Dictionary<string, SafeRegistryHandle>(StringComparer.OrdinalIgnoreCase)
        {
            { "HKEY_LOCAL_MACHINE", new SafeRegistryHandle(new IntPtr(unchecked((int)0x80000002)), false) },
            { "HKEY_CURRENT_USER", new SafeRegistryHandle(new IntPtr(unchecked((int)0x80000001)), false) },
            { "HKEY_CLASSES_ROOT", new SafeRegistryHandle(new IntPtr(unchecked((int)0x80000000)), false) },
            { "HKEY_USERS", new SafeRegistryHandle(new IntPtr(unchecked((int)0x80000003)), false) },
            { "HKEY_CURRENT_CONFIG", new SafeRegistryHandle(new IntPtr(unchecked((int)0x80000005)), false) }
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

            // Validate and get the correct SafeRegistryHandle for the root hive.
            if (!HiveMap.TryGetValue(hiveName, out SafeRegistryHandle? hKeyRoot) || (null == hKeyRoot))
            {
                throw new ArgumentException($"Invalid registry hive: {hiveName}", nameof(fullKeyPath));
            }

            // Open the registry key using SafeRegistryHandle for hKeyRoot.
            var result = PInvoke.RegOpenKeyEx(hKeyRoot, subKeyPath, 0, REG_SAM_FLAGS.KEY_READ, out SafeRegistryHandle hKey);
            if (result != WIN32_ERROR.ERROR_SUCCESS || hKey.IsInvalid)
            {
                throw new Win32Exception((int)result, "Failed to open the registry key.");
            }

            // Get the modified time from the registry. This must be unsafe as CsWin32 only allows FILETIME to be a pointer...
            try
            {
                System.Runtime.InteropServices.ComTypes.FILETIME lastWriteTime;
                unsafe
                {
                    result = PInvoke.RegQueryInfoKey(new HKEY(hKey.DangerousGetHandle()), null, null, null, null, null, null, null, null, null, null, &lastWriteTime);
                }
                if (result != WIN32_ERROR.ERROR_SUCCESS)
                {
                    throw new Win32Exception((int)result, "Failed to query the registry key.");
                }

                // Return the FILETIME as DateTime.
                return DateTime.FromFileTime(((long)lastWriteTime.dwHighDateTime << 32) | (lastWriteTime.dwLowDateTime & 0xFFFFFFFFL));
            }
            finally
            {
                // Ensure the SafeHandle is properly disposed.
                hKey.Dispose();
            }
        }
    }
}
