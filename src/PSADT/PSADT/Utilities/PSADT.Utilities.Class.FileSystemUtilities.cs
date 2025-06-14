using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using PSADT.LibraryInterfaces;

namespace PSADT.Utilities
{
    /// <summary>
    /// A class containing utility methods for file system operations.
    /// </summary>
    internal static class FileSystemUtilities
    {
        /// <summary>
        /// Returns a lookup table for NT paths to drive letters.
        /// </summary>
        /// <returns></returns>
        internal static ReadOnlyDictionary<string, string> GetNtPathLookupTable()
        {
            Dictionary<string, string> lookupTable = [];
            Span<char> targetPath = stackalloc char[260];
            for (char drive = 'A'; drive <= 'Z'; drive++)
            {
                var driveLetter = drive + ":";
                try
                {
                    Kernel32.QueryDosDevice(driveLetter, targetPath);
                }
                catch
                {
                    continue;
                }
                foreach (var path in targetPath.ToString().Trim('\0').Trim().Split('\0'))
                {
                    var ntPath = path.Trim();
                    if (ntPath.Length > 0)
                    {
                        lookupTable.Add(ntPath, driveLetter);
                    }
                }
                targetPath.Clear();
            }
            return new ReadOnlyDictionary<string, string>(lookupTable);
        }
    }
}
