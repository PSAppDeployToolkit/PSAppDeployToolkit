using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using PSADT.Utilities;
using Windows.Win32.Foundation;
using Windows.Win32.System.SystemInformation;

namespace PSADT.LibraryInterfaces
{
    /// <summary>
    /// CsWin32 P/Invoke wrappers for the ntdll.dll library.
    /// </summary>
    public static class NtDll
    {
        /// <summary>
        /// System information class for querying system handles.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX
        {
            /// <summary>
            /// The kernel object's address.
            /// </summary>
            public IntPtr Object;

            /// <summary>
            /// The owning process's identifier.
            /// </summary>
            public UIntPtr UniqueProcessId;

            /// <summary>
            /// The handle's numerical identifier.
            /// </summary>
            public IntPtr HandleValue;

            /// <summary>
            /// The type of access granted to the handle.
            /// </summary>
            public uint GrantedAccess;

            /// <summary>
            /// The number of references to the object.
            /// </summary>
            public ushort CreatorBackTraceIndex;

            /// <summary>
            /// The type of the object.
            /// </summary>
            public ushort ObjectTypeIndex;

            /// <summary>
            /// The handle attributes.
            /// </summary>
            public uint HandleAttributes;

            /// <summary>
            /// Reserved for future use.
            /// </summary>
            public uint Reserved;
        }

        /// <summary>
        /// System information class for querying system handle information.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct SYSTEM_HANDLE_INFORMATION_EX
        {
            /// <summary>
            /// The number of handles in the system.
            /// </summary>
            internal UIntPtr NumberOfHandles;

            /// <summary>
            /// Reserved for future use.
            /// </summary>
            internal IntPtr Reserved;
        }

        /// <summary>
        /// Gets the version info of the current operating system from the kernel.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Win32Exception"></exception>
        internal static unsafe NTSTATUS RtlGetVersion(out OSVERSIONINFOEXW lpVersionInformation)
        {
            lpVersionInformation = new() { dwOSVersionInfoSize = (uint)Marshal.SizeOf<OSVERSIONINFOEXW>() };
            NTSTATUS status = Windows.Wdk.PInvoke.RtlGetVersion((OSVERSIONINFOW*)Unsafe.AsPointer(ref lpVersionInformation));
            if (status.Value < 0)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error((WIN32_ERROR)Windows.Win32.PInvoke.RtlNtStatusToDosError(status));
            }
            return status;
        }

        /// <summary>
        /// Queries system information from the kernel.
        /// This doesn't use CsWin32 because the SystemInformationClass enum in the library is incomplete.
        /// </summary>
        /// <param name="SystemInformationClass"></param>
        /// <param name="SystemInformation"></param>
        /// <param name="SystemInformationLength"></param>
        /// <param name="ReturnLength"></param>
        /// <returns></returns>
        [DllImport("ntdll.dll", ExactSpelling = true)]
        internal static extern NTSTATUS NtQuerySystemInformation(int SystemInformationClass, IntPtr SystemInformation, int SystemInformationLength, out int ReturnLength);

        /// <summary>
        /// Queries an object for information.
        /// This doesn't use CsWin32 because the OBJECT_INFORMATION_CLASS enum in the library is incomplete.
        /// </summary>
        /// <param name="ObjectHandle"></param>
        /// <param name="ObjectInformationClass"></param>
        /// <param name="ObjectInformation"></param>
        /// <param name="ObjectInformationLength"></param>
        /// <param name="ReturnLength"></param>
        /// <returns></returns>
        [DllImport("ntdll.dll", EntryPoint = "NtQueryObject")]
        private static extern NTSTATUS NtQueryObjectNative(HANDLE ObjectHandle, OBJECT_INFORMATION_CLASS ObjectInformationClass, IntPtr ObjectInformation, int ObjectInformationLength, out int ReturnLength);

        /// <summary>
        /// Queries an object for information.
        /// </summary>
        /// <param name="Handle"></param>
        /// <param name="ObjectInformationClass"></param>
        /// <param name="ObjectInformation"></param>
        /// <param name="ObjectInformationLength"></param>
        /// <param name="ReturnLength"></param>
        /// <returns></returns>
        internal static NTSTATUS NtQueryObject(HANDLE Handle, OBJECT_INFORMATION_CLASS ObjectInformationClass, IntPtr ObjectInformation, int ObjectInformationLength, out int ReturnLength)
        {
            var res = NtQueryObjectNative(Handle, ObjectInformationClass, ObjectInformation, ObjectInformationLength, out ReturnLength);
            if (res.Value < 0 && IntPtr.Zero != ObjectInformation && 0 != ObjectInformationLength)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error((WIN32_ERROR)Windows.Win32.PInvoke.RtlNtStatusToDosError(res));
            }
            return res;
        }
    }
}
