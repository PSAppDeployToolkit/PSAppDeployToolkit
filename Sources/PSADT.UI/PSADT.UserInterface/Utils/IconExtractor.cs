using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows.Media.Imaging;

namespace PSADT.Utils
{
    public static class IconExtractor
    {
        [SuppressMessage(
            "StyleCop.CSharp.NamingRules",
            "SA1310:Field names should not contain underscore",
            Justification = "Follow C++ enum naming instead.")]
        private static readonly uint LoadLibraryAsDatafile = 0x00000002;

        [SuppressMessage(
            "StyleCop.CSharp.NamingRules",
            "SA1310:Field names should not contain underscore",
            Justification = "Follow C++ enum naming instead.")]
        private static readonly IntPtr RtIcon = (IntPtr)3;

        [SuppressMessage(
            "StyleCop.CSharp.NamingRules",
            "SA1310:Field names should not contain underscore",
            Justification = "Follow C++ enum naming instead.")]
        private static readonly IntPtr RtGroupIcon = (IntPtr)14;

        [UnmanagedFunctionPointer(CallingConvention.Winapi, SetLastError = true, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        [SuppressMessage(
            "StyleCop.CSharp.NamingRules",
            "SA1310:Field names should not contain underscore",
            Justification = "Follow C++ enum naming instead.")]
        private delegate bool Enumresnameproc(IntPtr hModule, IntPtr lpszType, IntPtr lpszName, IntPtr lParam);

        public static BitmapFrame ExtractIcon(string filePath)
        {
            try
            {
                MemoryStream iconStream = ExtractIconsFromResource(filePath);
                if (iconStream == null)
                {
                    return null;
                }

                using (iconStream)
                {
                    iconStream.Position = 0;
                    BitmapFrame frame = BitmapDecoder.Create(
                        iconStream,
                        BitmapCreateOptions.IgnoreImageCache | BitmapCreateOptions.PreservePixelFormat,
                        BitmapCacheOption.OnLoad).Frames
                        .OrderByDescending(f => f.Width)
                        .FirstOrDefault();
                    return frame;
                }
            }
            catch
            {
                return null;
            }
        }

        private static MemoryStream ExtractIconsFromResource(string fileName)
        {
            if (fileName == null)
            {
                throw new ArgumentNullException("fileName");
            }

            IntPtr hModule = IntPtr.Zero;
            MemoryStream icon = null;
            try
            {
                hModule = LoadLibraryEx(fileName, IntPtr.Zero, LoadLibraryAsDatafile);
                if (hModule == IntPtr.Zero)
                {
                    throw new Win32Exception();
                }

                Enumresnameproc callback = (h, t, name, l) =>
                {
                    // Refer the following URL for the data structures used here:
                    // http://msdn.microsoft.com/en-us/library/ms997538.aspx

                    // RT_GROUP_ICON resource consists of a GRPICONDIR and GRPICONDIRENTRY's.
                    var dir = GetDataFromResource(hModule, RtGroupIcon, name);

                    // Calculate the size of an entire .icon file.
                    int count = BitConverter.ToUInt16(dir, 4);  // GRPICONDIR.idCount
                    int len = 6 + (16 * count);                 // sizeof(ICONDIR) + sizeof(ICONDIRENTRY) * count
                    for (int i = 0; i < count; ++i)
                    {
                        len += BitConverter.ToInt32(dir, 6 + (14 * i) + 8);   // GRPICONDIRENTRY.dwBytesInRes
                    }

                    icon = new MemoryStream(len);
                    BinaryWriter dst = new BinaryWriter(icon);
                    try
                    {
                        // Copy GRPICONDIR to ICONDIR.
                        dst.Write(dir, 0, 6);

                        int picOffset = 6 + (16 * count); // sizeof(ICONDIR) + sizeof(ICONDIRENTRY) * count

                        for (int i = 0; i < count; ++i)
                        {
                            // Load the picture.
                            ushort id = BitConverter.ToUInt16(dir, 6 + (14 * i) + 12);    // GRPICONDIRENTRY.nID
                            var pic = GetDataFromResource(hModule, RtIcon, (IntPtr)id);

                            // Copy GRPICONDIRENTRY to ICONDIRENTRY.
                            dst.Seek(6 + (16 * i), SeekOrigin.Begin);

                            dst.Write(dir, 6 + (14 * i), 8);  // First 8bytes are identical.
                            dst.Write(pic.Length);            // ICONDIRENTRY.dwBytesInRes
                            dst.Write(picOffset);             // ICONDIRENTRY.dwImageOffset

                            // Copy a picture.
                            dst.Seek(picOffset, SeekOrigin.Begin);
                            dst.Write(pic, 0, pic.Length);

                            picOffset += pic.Length;
                        }
                    }
                    catch
                    {
                        dst.Dispose();
                        throw;
                    }

                    return false;
                };
                EnumResourceNames(hModule, RtGroupIcon, callback, IntPtr.Zero);
            }
            finally
            {
                if (hModule != IntPtr.Zero)
                {
                    FreeLibrary(hModule);
                }
            }

            return icon ?? new MemoryStream();
        }

        private static byte[] GetDataFromResource(IntPtr hModule, IntPtr type, IntPtr name)
        {
            // Load the binary data from the specified resource.
            IntPtr hResInfo = FindResource(hModule, name, type);
            if (hResInfo == IntPtr.Zero)
            {
                throw new Win32Exception();
            }

            IntPtr hResData = LoadResource(hModule, hResInfo);
            if (hResData == IntPtr.Zero)
            {
                throw new Win32Exception();
            }

            IntPtr pResData = LockResource(hResData);
            if (pResData == IntPtr.Zero)
            {
                throw new Win32Exception();
            }

            uint size = SizeofResource(hModule, hResInfo);
            if (size == 0)
            {
                throw new Win32Exception();
            }

            byte[] buf = new byte[size];
            Marshal.Copy(pResData, buf, 0, buf.Length);

            return buf;
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hFile, uint dwFlags);

        [DllImport("kernel32.dll", SetLastError = true)]
        [SuppressUnmanagedCodeSecurity]
        private static extern bool FreeLibrary(IntPtr hModule);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private static extern bool EnumResourceNames(IntPtr hModule, IntPtr lpszType, Enumresnameproc lpEnumFunc, IntPtr lParam);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private static extern IntPtr FindResource(IntPtr hModule, IntPtr lpName, IntPtr lpType);

        [DllImport("kernel32.dll", SetLastError = true)]
        [SuppressUnmanagedCodeSecurity]
        private static extern IntPtr LoadResource(IntPtr hModule, IntPtr hResInfo);

        [DllImport("kernel32.dll", SetLastError = true)]
        [SuppressUnmanagedCodeSecurity]
        private static extern IntPtr LockResource(IntPtr hResData);

        [DllImport("kernel32.dll", SetLastError = true)]
        [SuppressUnmanagedCodeSecurity]
        private static extern uint SizeofResource(IntPtr hModule, IntPtr hResInfo);
    }
}