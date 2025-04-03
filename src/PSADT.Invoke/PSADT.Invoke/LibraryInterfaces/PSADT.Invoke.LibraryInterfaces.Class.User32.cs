using System.ComponentModel;
using System.Runtime.InteropServices;

namespace PSADT.Invoke.LibraryInterfaces
{
    /// <summary>
    /// Contains methods for interacting with the Windows user interface.
    /// </summary>
    internal static class User32
    {
        /// <summary>
        /// Sets the process as DPI aware.
        /// </summary>
        /// <returns></returns>
        [DllImport("user32.dll", ExactSpelling = true, SetLastError = false, EntryPoint = "SetProcessDPIAware")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetProcessDPIAwareNative();

        /// <summary>
        /// Sets the process as DPI aware.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Win32Exception"></exception>
        internal static bool SetProcessDPIAware()
        {
            var res = SetProcessDPIAwareNative();
            if (!res)
            {
                throw new Win32Exception("The call to SetProcessDPIAware() failed.");
            }
            return res;
        }
    }
}
