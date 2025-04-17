using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using Windows.Win32;

namespace PSADT.UserInterface.Utilities
{
    internal static partial class NativeMethods
    {
        public static readonly IntPtr RtGroupIcon = (IntPtr)14;
        public static readonly IntPtr RtIcon = (IntPtr)3;

        public const int SPI_GETWORKAREA = 48;
        public const int SPI_GETHIGHCONTRAST = 0x0042;
        public const int HCF_HIGHCONTRASTON = 0x00000001;

        public delegate bool MonitorEnumProc(IntPtr monitor, IntPtr hdc, IntPtr lprcMonitor, IntPtr lParam);

        public static HandleRef NullHandleRef = new(null, IntPtr.Zero);

        [DllImport("user32.dll", ExactSpelling = true, SetLastError = false)]
        public static extern bool EnumDisplayMonitors(
            HandleRef hdc,
            IntPtr lprcClip,
            MonitorEnumProc lpfnEnum,
            IntPtr dwData);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
        public static extern bool SystemParametersInfo(int uiAction, int uiParam, ref RECT pvParam, int fWinIni);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
        public static extern bool SystemParametersInfo(
            int uiAction,
            int uiParam,
            ref HIGHCONTRAST pvParam,
            int fWinIni);

        [DllImport("dwmapi.dll", EntryPoint = "DwmGetColorizationColor", PreserveSig = true)]
        public static extern int DwmGetColorizationColor(out uint pcrColorization, [MarshalAs(UnmanagedType.Bool)] out bool pfOpaqueBlend);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct HIGHCONTRAST
        {
            public int cbSize;
            public int dwFlags;
            [MarshalAs(UnmanagedType.LPWStr)]
            public IntPtr lpszDefaultScheme;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;

            public RECT(int left, int top, int right, int bottom)
            {
                this.left = left;
                this.top = top;
                this.right = right;
                this.bottom = bottom;
            }

            public RECT(Rect r)
            {
                left = (int)r.Left;
                top = (int)r.Top;
                right = (int)r.Right;
                bottom = (int)r.Bottom;
            }

            public static RECT FromXYWH(int x, int y, int width, int height)
            {
                return new RECT(x, y, x + width, y + height);
            }

            public readonly System.Windows.Size Size => new(right - left, bottom - top);
        }
    }
}
