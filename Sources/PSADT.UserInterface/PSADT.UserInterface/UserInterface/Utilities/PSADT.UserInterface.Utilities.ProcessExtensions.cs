using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using static PSADT.UserInterface.Utilities.NativeMethods;

namespace PSADT.UserInterface.Utilities
{
    public static class ProcessExtensions
    {
        private const int BufferSize = 1024;

        public static string? GetMainModuleFileName(this Process process)
        {
            if (process == null) throw new ArgumentNullException(nameof(process));

            var fileNameBuilder = new StringBuilder(BufferSize);
            uint bufferLength = (uint)fileNameBuilder.Capacity + 1;
            return NativeMethods.QueryFullProcessImageName(process.Handle, 0, fileNameBuilder, ref bufferLength) != 0 ?
                fileNameBuilder.ToString() :
                null;
        }

        public static Icon? GetIcon(this Process process, bool largeIcon = true)
        {
            if (process == null) throw new ArgumentNullException(nameof(process));

            try
            {
                string? mainModuleFileName = process.GetMainModuleFileName();
                if (string.IsNullOrEmpty(mainModuleFileName))
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