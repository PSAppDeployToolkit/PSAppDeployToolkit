using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using PSADT.UserInterface.LibraryInterfaces;
using PSADT.UserInterface.Utilities;
using static PSADT.UserInterface.Utilities.NativeMethods;

namespace PSADT.UserInterface.Extensions
{
    /// <summary>
    /// Utility class for process extensions
    /// </summary>
    public static class ProcessExtensions
    {
        /// <summary>
        /// Get the icon of a process
        /// </summary>
        /// <param name="process"></param>
        /// <param name="largeIcon"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static Icon? GetIcon(this Process process, bool largeIcon = true)
        {
            if (process == null) throw new ArgumentNullException(nameof(process));

            try
            {
                string? mainModuleFileName = process.MainModule?.FileName;
                if (string.IsNullOrWhiteSpace(mainModuleFileName))
                {
                    return null;
                }

                SHFILEINFO shinfo = new();
                uint flags = SHGFI_ICON | (largeIcon ? SHGFI_LARGEICON : SHGFI_SMALLICON);
                IntPtr hImg = NativeMethods.SHGetFileInfo(mainModuleFileName!, 0, ref shinfo, (uint)Marshal.SizeOf(shinfo), flags);

                if (hImg == IntPtr.Zero)
                {
                    return null;
                }

                Icon icon = (Icon)Icon.FromHandle(shinfo.hIcon).Clone(); // Clone to prevent handle loss
                User32.DestroyIcon(shinfo.hIcon); // Cleanup unmanaged icon handle

                return icon;
            }
            catch (FileNotFoundException)
            {
                return null;
            }
            catch (UnauthorizedAccessException)
            {
                return null;
            }
            catch (IOException)
            {
                return null;
            }
            catch (Win32Exception)
            {
                return null;
            }
        }
    }
}
