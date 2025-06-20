using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace PSADT.Invoke.LibraryInterfaces
{
    /// <summary>
    /// Contains methods for interacting with the Windows kernel.
    /// </summary>
    internal static class Kernel32
    {
        /// <summary>
        /// Gets a handle to the allocated console window.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Win32Exception"></exception>
        internal static bool AllocConsole()
        {
            // Import the AllocConsole function from kernel32.dll.
            [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            static extern bool AllocConsole();

            // Call the AllocConsole function to allocate a console for the process.
            var res = AllocConsole();
            if (!res)
            {
                throw new Win32Exception();
            }
            return res;
        }

        /// <summary>
        /// Gets a handle to the allocated console window.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Win32Exception"></exception>
        internal static IntPtr GetConsoleWindow()
        {
            // Import the GetConsoleWindow function from kernel32.dll.
            [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = false)]
            static extern IntPtr GetConsoleWindow();

            // Call the GetConsoleWindow function to retrieve the handle to the console window.
            var res = GetConsoleWindow();
            if (res == IntPtr.Zero)
            {
                throw new Win32Exception("Failed to get a handle for the console window.");
            }
            return res;
        }

        /// <summary>
        /// Frees the console allocated to the process.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Win32Exception"></exception>
        internal static bool FreeConsole()
        {
            // Import the FreeConsole function from kernel32.dll.
            [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            static extern bool FreeConsole();

            // Call the FreeConsole function to free the console allocated to the process.
            var res = FreeConsole();
            if (!res)
            {
                throw new Win32Exception();
            }
            return res;
        }
    }
}
