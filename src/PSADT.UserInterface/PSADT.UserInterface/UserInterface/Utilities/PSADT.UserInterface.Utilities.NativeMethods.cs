using System.Runtime.InteropServices;

namespace PSADT.UserInterface.Utilities
{
    internal static partial class NativeMethods
    {
        public delegate bool MonitorEnumProc(IntPtr monitor, IntPtr hdc, IntPtr lprcMonitor, IntPtr lParam);

        public static HandleRef NullHandleRef = new(null, IntPtr.Zero);

        [DllImport("user32.dll", ExactSpelling = true, SetLastError = false)]
        public static extern bool EnumDisplayMonitors(
            HandleRef hdc,
            IntPtr lprcClip,
            MonitorEnumProc lpfnEnum,
            IntPtr dwData);
    }
}
