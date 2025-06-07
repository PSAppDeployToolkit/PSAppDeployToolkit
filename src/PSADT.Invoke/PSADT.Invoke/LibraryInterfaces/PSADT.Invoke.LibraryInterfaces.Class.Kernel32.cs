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
        /// Contains information about the current system.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct SYSTEM_INFO
        {
            [StructLayout(LayoutKind.Explicit)]
            internal struct SYSTEM_INFO_PROCESSORINFO_UNION
            {
                [FieldOffset(0)]
                internal ProcessorArchitecture wProcessorArchitecture;

                [FieldOffset(2)]
                internal ushort wReserved;
            }

            internal SYSTEM_INFO_PROCESSORINFO_UNION uProcessorInfo;
            internal uint dwPageSize;
            internal IntPtr lpMinimumApplicationAddress;
            internal IntPtr lpMaximumApplicationAddress;
            internal IntPtr dwActiveProcessorMask;
            internal uint dwNumberOfProcessors;
            internal uint dwProcessorType;
            internal uint dwAllocationGranularity;
            internal ushort wProcessorLevel;
            internal ushort wProcessorRevision;
        }

        /// <summary>
        /// Retrieves information about the current system to an application running under WOW64.
        /// </summary>
        /// <returns></returns>
        internal static SYSTEM_INFO GetNativeSystemInfo()
        {
            // Import the GetNativeSystemInfo function from kernel32.dll.
            [DllImport("kernel32.dll", SetLastError = false, ExactSpelling = true)]
            static extern void GetNativeSystemInfo(out SYSTEM_INFO lpSystemInfo);

            // Call the GetNativeSystemInfo function to retrieve system information.
            GetNativeSystemInfo(out var systemInfo);
            return systemInfo;
        }

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
