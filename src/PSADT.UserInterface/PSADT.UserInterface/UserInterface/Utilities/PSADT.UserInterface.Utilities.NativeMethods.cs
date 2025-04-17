using System.Runtime.InteropServices;
using System.Text;
using System.Windows;

namespace PSADT.UserInterface.Utilities
{
    internal static partial class NativeMethods
    {
        public static readonly IntPtr RtGroupIcon = (IntPtr)14;
        public static readonly IntPtr RtIcon = (IntPtr)3;

        public const int SM_CMONITORS = 80;
        public const int SM_CXSCREEN = 0;
        public const int SM_CYSCREEN = 1;
        public const int SPI_GETWORKAREA = 48;

        public const int SM_CXVIRTUALSCREEN = 78;
        public const int SM_CYVIRTUALSCREEN = 79;
        public const int SM_XVIRTUALSCREEN = 76;
        public const int SM_YVIRTUALSCREEN = 77;

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

        [DllImport("user32.dll", ExactSpelling = true, SetLastError = false)]
        public static extern IntPtr MonitorFromPoint(POINTSTRUCT pt, int dwFlags);

        [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Auto, SetLastError = false)]
        public static extern int GetSystemMetrics(int nIndex);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
        public static extern bool SystemParametersInfo(int uiAction, int uiParam, ref RECT pvParam, int fWinIni);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
        public static extern bool SystemParametersInfo(
            int uiAction,
            int uiParam,
            ref HIGHCONTRAST pvParam,
            int fWinIni);

        [DllImport("user32.dll", ExactSpelling = true, SetLastError = false)]
        public static extern bool GetCursorPos([In, Out] POINT pt);

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

        [StructLayout(LayoutKind.Sequential)]
        public struct POINTSTRUCT(int x, int y)
        {
            public int x = x;
            public int y = y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public class POINT
        {
            public int x;
            public int y;

            public POINT()
            {
            }

            public POINT(int x, int y)
            {
                this.x = x;
                this.y = y;
            }

#if DEBUG
            public override string ToString()
            {
                return $"{{x={x}, y={y}}}";
            }
#endif
        }

        [StructLayout(LayoutKind.Sequential)]
        public class COMRECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;

            public COMRECT()
            {
            }

            public COMRECT(Rect r)
            {
                left = (int)r.X;
                top = (int)r.Y;
                right = (int)r.Right;
                bottom = (int)r.Bottom;
            }

            public COMRECT(int left, int top, int right, int bottom)
            {
                this.left = left;
                this.top = top;
                this.right = right;
                this.bottom = bottom;
            }

            public static COMRECT FromXYWH(int x, int y, int width, int height)
            {
                return new COMRECT(x, y, x + width, y + height);
            }

            public override string ToString()
            {
                return $"Left = {left}, Top = {top}, Right = {right}, Bottom = {bottom}";
            }
        }
    }
}
