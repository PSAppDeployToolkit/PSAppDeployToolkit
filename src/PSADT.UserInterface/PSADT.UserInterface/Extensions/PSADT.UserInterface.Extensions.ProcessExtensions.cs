using System;
using System.Diagnostics;
using System.Drawing;
using PSADT.UserInterface.LibraryInterfaces;
using Windows.Win32.UI.Shell;

namespace PSADT.UserInterface.Extensions
{
    /// <summary>
    /// Utility class for process extensions
    /// </summary>
    internal static class ProcessExtensions
    {
        /// <summary>
        /// Get the icon of a process
        /// </summary>
        /// <param name="process"></param>
        /// <param name="largeIcon"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        internal static Icon? GetIcon(this Process process, bool largeIcon = true)
        {
            // Check if the process is null or if the main module's file name is not a string.
            if (process == null || !(process.MainModule?.FileName is string mainModuleFileName))
            {
                throw new ArgumentNullException(nameof(process));
            }

            // Get the icon handle using SHGetFileInfo, clone it, then return it.
            var shinfo = Shell32.SHGetFileInfo(mainModuleFileName, SHGFI_FLAGS.SHGFI_ICON | (largeIcon ? SHGFI_FLAGS.SHGFI_LARGEICON : SHGFI_FLAGS.SHGFI_SMALLICON));
            Icon icon = (Icon)Icon.FromHandle(shinfo.hIcon).Clone();
            User32.DestroyIcon(shinfo.hIcon);
            return icon;
        }
    }
}
