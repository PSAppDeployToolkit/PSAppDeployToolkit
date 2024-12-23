using System;
using System.IO;
using System.Runtime.InteropServices;
using PSADT.PInvokes;

namespace PSADT.PE
{
    public static class ExecutableType
    {
        [DllImport("shell32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr SHGetFileInfo(string pszPath, FileAttributes dwFileAttributes, ref SHFILEINFO psfi, int cbFileInfo, SHGFI uFlags);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public int dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
            public static int Size => Marshal.SizeOf(typeof(SHFILEINFO));
        }

        public static bool IsGuiApplication(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("The specified file does not exist.", filePath);
            }

            SHFILEINFO info = new SHFILEINFO();
            IntPtr result = SHGetFileInfo(filePath, 0, ref info, SHFILEINFO.Size, SHGFI.SHGFI_EXETYPE);

            // The EXETYPE is returned directly in the return value
            // For GUI applications, the high order word is non-zero (usually 'PE')
            // For Console applications, only the low order word is non-zero (usually 'MZ')
            return (result.ToInt64() & 0xFFFF0000) != 0;
        }
    }
}
