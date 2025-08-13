using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Security.AccessControl;
using PSADT.LibraryInterfaces;
using Windows.Win32.Storage.FileSystem;

namespace PSADT.FileSystem
{
    /// <summary>
    /// A class containing utility methods for file system operations.
    /// </summary>
    public static class FileSystemUtilities
    {
        /// <summary>
        /// Returns a lookup table for NT paths to drive letters.
        /// </summary>
        /// <returns></returns>
        internal static ReadOnlyDictionary<string, string> GetNtPathLookupTable()
        {
            var lookupTable = new Dictionary<string, string> { { @"\Device\Mup", @"\" } };
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
            return new(lookupTable);
        }

        /// <summary>
        /// Tests whether the specified file can be accessed with the desired access rights.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="desiredAccess"></param>
        /// <returns></returns>
        public static bool TestFileAccess(FileInfo path, FileSystemRights desiredAccess = FileSystemRights.ReadAndExecute)
        {
            var dwShareMode = FILE_SHARE_MODE.FILE_SHARE_NONE;
            if ((desiredAccess & FileSystemRights.Read) == FileSystemRights.Read)
            {
                dwShareMode |= FILE_SHARE_MODE.FILE_SHARE_READ;
            }
            if ((desiredAccess & FileSystemRights.Write) == FileSystemRights.Write)
            {
                dwShareMode |= FILE_SHARE_MODE.FILE_SHARE_WRITE;
            }
            if ((desiredAccess & FileSystemRights.Delete) == FileSystemRights.Delete)
            {
                dwShareMode |= FILE_SHARE_MODE.FILE_SHARE_DELETE;
            }
            try
            {
                using var hFile = Kernel32.CreateFile(path.FullName, desiredAccess, dwShareMode, null, FILE_CREATION_DISPOSITION.OPEN_EXISTING, FILE_FLAGS_AND_ATTRIBUTES.FILE_ATTRIBUTE_NORMAL);
                if (hFile.IsInvalid)
                {
                    return false;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
