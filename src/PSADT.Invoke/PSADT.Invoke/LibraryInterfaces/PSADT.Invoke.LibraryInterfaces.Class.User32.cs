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
        /// <exception cref="Win32Exception"></exception>
        internal static bool SetProcessDPIAware()
        {
            // Import the SetProcessDPIAware function from user32.dll.
            [DllImport("user32.dll", ExactSpelling = true, SetLastError = false)]
            [return: MarshalAs(UnmanagedType.Bool)]
            static extern bool SetProcessDPIAware();

            // Call the SetProcessDPIAware function to set the process as DPI aware.
            var res = SetProcessDPIAware();
            if (!res)
            {
                throw new Win32Exception("The call to SetProcessDPIAware() failed.");
            }
            return res;
        }
    }
}
