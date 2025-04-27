using PSADT.UserInterface.LibraryInterfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSADT.UserInterface.Utilities
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
            var lookupTable = new Dictionary<string, string>();
            var targetPath = new Span<char>(new char[260]);
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
