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
            internal ProcessorArchitectureUnion uProcessorInfo;
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
        /// Contains information about the processor architecture.
        /// </summary>
        [StructLayout(LayoutKind.Explicit)]
        internal struct ProcessorArchitectureUnion
        {
            [FieldOffset(0)]
            internal ProcessorArchitecture wProcessorArchitecture;

            [FieldOffset(2)]
            internal ushort wReserved;
        }

        /// <summary>
        /// Retrieves information about the current system to an application running under WOW64.
        /// </summary>
        /// <param name="lpSystemInfo"></param>
        [DllImport("kernel32.dll", SetLastError = false, ExactSpelling = true)]
        internal static extern void GetNativeSystemInfo(out SYSTEM_INFO lpSystemInfo);

        /// <summary>
        /// Retrieves information about the current system to an application running under WOW64.
        /// </summary>
        /// <returns></returns>
        internal static SYSTEM_INFO GetNativeSystemInfo()
        {
            GetNativeSystemInfo(out var systemInfo);
            return systemInfo;
        }

        /// <summary>
        /// Allocates a console to the process.
        /// </summary>
        /// <returns></returns>
        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true, EntryPoint = "AllocConsole")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AllocConsoleNative();

        /// <summary>
        /// Gets a handle to the allocated console window.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Win32Exception"></exception>
        internal static bool AllocConsole()
        {
            var res = AllocConsoleNative();
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
        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = false, EntryPoint = "GetConsoleWindow")]
        private static extern IntPtr GetConsoleWindowNative();

        /// <summary>
        /// Gets a handle to the allocated console window.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Win32Exception"></exception>
        internal static IntPtr GetConsoleWindow()
        {
            var res = GetConsoleWindowNative();
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
        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true, EntryPoint = "FreeConsole")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FreeConsoleNative();

        /// <summary>
        /// Frees the console allocated to the process.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Win32Exception"></exception>
        internal static bool FreeConsole()
        {
            var res = FreeConsoleNative();
            if (!res)
            {
                throw new Win32Exception();
            }
            return res;
        }
    }
}
