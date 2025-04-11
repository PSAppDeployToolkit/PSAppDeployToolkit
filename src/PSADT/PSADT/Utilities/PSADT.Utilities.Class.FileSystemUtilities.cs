using System;
using PSADT.LibraryInterfaces;

namespace PSADT.Utilities
{
    internal static class FileSystemUtilities
    {
        /// <summary>
        /// Converts an NT path to a DOS path by checking against all drive letters (A-Z).
        /// </summary>
        /// <param name="ntPath"></param>
        /// <returns></returns>
        public static string ConvertNtPathToDosPath(string ntPath)
        {
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

                var devicePath = targetPath.ToString().Trim('\0').Trim();
                if (ntPath.StartsWith(devicePath))
                {
                    return ntPath.Replace(devicePath, driveLetter);
                }
                targetPath.Clear();
            }
            throw new InvalidOperationException($"Failed to convert NT path [{ntPath}] to DOS path.");
        }
    }
}
