using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using static PSADT.UserInterface.Utilities.NativeMethods;

namespace PSADT.UserInterface.Utilities
{
    /// <summary>
    /// Utility class for process extensions
    /// </summary>
    public static class ProcessExtensions
    {
        private const int BufferSize = 1024;

        /// <summary>
        /// Gets the file name of the given process
        /// </summary>
        public static string? GetMainModuleFileName(this Process process)
        {
            if (process == null) throw new ArgumentNullException(nameof(process));

            var fileNameBuilder = new StringBuilder(BufferSize);
            uint bufferLength = (uint)fileNameBuilder.Capacity + 1;

            try
            {
                return NativeMethods.QueryFullProcessImageName(process.Handle, 0, fileNameBuilder, ref bufferLength) != 0 ?
                    fileNameBuilder.ToString() :
                    null;
            }
            catch (Win32Exception ex)
            {
                Debug.WriteLine($"Error getting main module file name for process {process.ProcessName}: {ex.Message}");
                return null;
            }
        }

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
                string? mainModuleFileName = process.GetMainModuleFileName();
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
                NativeMethods.DestroyIcon(shinfo.hIcon); // Cleanup unmanaged icon handle

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
