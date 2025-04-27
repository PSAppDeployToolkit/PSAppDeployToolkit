using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using PSADT.SafeHandles;
using PSADT.Utilities;
using Windows.Wdk.Foundation;
using Windows.Win32.Foundation;
using Windows.Win32.Security;
using Windows.Win32.System.SystemInformation;
using Windows.Win32.System.Threading;

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
            public UIntPtr HandleValue;

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
        internal struct SYSTEM_HANDLE_INFORMATION_EX
        {
            /// <summary>
            /// The number of handles in the system.
            /// </summary>
            internal UIntPtr NumberOfHandles;

            /// <summary>
            /// Reserved for future use.
            /// </summary>
            internal UIntPtr Reserved;
        }

        /// <summary>
        /// System information class for querying system handle information.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct OBJECT_TYPE_INFORMATION
        {
            /// <summary>
            /// The name of the type/
            /// </summary>
            internal UNICODE_STRING TypeName;

            /// <summary>
            /// The type's object count.
            /// </summary>
            internal uint TotalNumberOfObjects;

            /// <summary>
            /// The type's handle count.
            /// </summary>
            internal uint TotalNumberOfHandles;

            /// <summary>
            /// The type's paged pool usage.
            /// </summary>
            internal uint TotalPagedPoolUsage;

            /// <summary>
            /// The type's non-paged pool usage.
            /// </summary>
            internal uint TotalNonPagedPoolUsage;

            /// <summary>
            /// The type's name pool usage.
            /// </summary>
            internal uint TotalNamePoolUsage;

            /// <summary>
            /// The type's handle table usage.
            /// </summary>
            internal uint TotalHandleTableUsage;

            /// <summary>
            /// The type's high-water mark for object count.
            /// </summary>
            internal uint HighWaterNumberOfObjects;

            /// <summary>
            /// The type's high-water mark for handle count.
            /// </summary>
            internal uint HighWaterNumberOfHandles;

            /// <summary>
            /// The type's high-water mark for paged pool usage.
            /// </summary>
            internal uint HighWaterPagedPoolUsage;

            /// <summary>
            /// The type's high-water mark for non-paged pool usage.
            /// </summary>
            internal uint HighWaterNonPagedPoolUsage;

            /// <summary>
            /// The type's high-water mark for name pool usage.
            /// </summary>
            internal uint HighWaterNamePoolUsage;

            /// <summary>
            /// The type's high-water mark for handle table usage.
            /// </summary>
            internal uint HighWaterHandleTableUsage;

            /// <summary>
            /// The type's invalid attributes.
            /// </summary>
            internal uint InvalidAttributes;

            /// <summary>
            /// The type's generic mapping.
            /// </summary>
            internal GENERIC_MAPPING GenericMapping;

            /// <summary>
            /// The type's valid access mask.
            /// </summary>
            internal uint ValidAccessMask;

            /// <summary>
            /// The type's security required flag.
            /// </summary>
            [MarshalAs(UnmanagedType.U1)]
            internal bool SecurityRequired;

            /// <summary>
            /// The type's security descriptor present flag.
            /// </summary>
            [MarshalAs(UnmanagedType.U1)]
            internal bool MaintainHandleCount;

            /// <summary>
            /// The object type's index.
            /// </summary>
            internal byte TypeIndex;

            /// <summary>
            /// Reserved for future use.
            /// </summary>
            internal byte ReservedByte;

            /// <summary>
            /// The object type's pool type.
            /// </summary>
            internal uint PoolType;

            /// <summary>
            /// The default paged pool charge for the object type.
            /// </summary>
            internal uint DefaultPagedPoolCharge;

            /// <summary>
            /// The default non-paged pool charge for the object type.
            /// </summary>
            internal uint DefaultNonPagedPoolCharge;
        }

        /// <summary>
        /// System information class for querying system object types.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct OBJECT_TYPES_INFORMATION
        {
            /// <summary>
            /// The number of object types in the system.
            /// </summary>
            internal uint NumberOfTypes;
        }

        /// <summary>
        /// Gets the version info of the current operating system from the kernel.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Win32Exception"></exception>
        internal static unsafe NTSTATUS RtlGetVersion(out OSVERSIONINFOEXW lpVersionInformation)
        {
            lpVersionInformation = new() { dwOSVersionInfoSize = (uint)Marshal.SizeOf<OSVERSIONINFOEXW>() };
            NTSTATUS res = Windows.Wdk.PInvoke.RtlGetVersion((OSVERSIONINFOW*)Unsafe.AsPointer(ref lpVersionInformation));
            if (res != NTSTATUS.STATUS_SUCCESS)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error((WIN32_ERROR)Windows.Win32.PInvoke.RtlNtStatusToDosError(res));
            }
            return res;
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
        [DllImport("ntdll.dll", ExactSpelling = true, EntryPoint = "NtQuerySystemInformation")]
        internal static extern NTSTATUS NtQuerySystemInformationNative(SYSTEM_INFORMATION_CLASS SystemInformationClass, IntPtr SystemInformation, int SystemInformationLength, out int ReturnLength);

        /// <summary>
        /// Queries system information from the kernel.
        /// </summary>
        /// <param name="SystemInformationClass"></param>
        /// <param name="SystemInformation"></param>
        /// <param name="ReturnLength"></param>
        /// <returns></returns>
        internal static NTSTATUS NtQuerySystemInformation(SYSTEM_INFORMATION_CLASS SystemInformationClass, SafeHGlobalHandle SystemInformation, out int ReturnLength)
        {
            var res = NtQuerySystemInformationNative(SystemInformationClass, SystemInformation.DangerousGetHandle(), SystemInformation.Length, out ReturnLength);
            if (res != NTSTATUS.STATUS_SUCCESS && res != NTSTATUS.STATUS_INFO_LENGTH_MISMATCH)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error((WIN32_ERROR)Windows.Win32.PInvoke.RtlNtStatusToDosError(res));
            }
            return res;
        }

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
        [DllImport("ntdll.dll", ExactSpelling = true, EntryPoint = "NtQueryObject")]
        private static extern NTSTATUS NtQueryObjectNative(IntPtr ObjectHandle, OBJECT_INFORMATION_CLASS ObjectInformationClass, IntPtr ObjectInformation, int ObjectInformationLength, out int ReturnLength);

        /// <summary>
        /// Queries an object for information.
        /// </summary>
        /// <param name="Handle"></param>
        /// <param name="ObjectInformationClass"></param>
        /// <param name="ObjectInformation"></param>
        /// <param name="ReturnLength"></param>
        /// <returns></returns>
        internal static NTSTATUS NtQueryObject(SafeHandle Handle, OBJECT_INFORMATION_CLASS ObjectInformationClass, SafeHGlobalHandle ObjectInformation, out int ReturnLength)
        {
            var res = NtQueryObjectNative(Handle.DangerousGetHandle(), ObjectInformationClass, ObjectInformation.DangerousGetHandle(), ObjectInformation.Length, out ReturnLength);
            if (res != NTSTATUS.STATUS_SUCCESS && ((null != Handle && !Handle.IsInvalid && !ObjectInformation.IsInvalid && 0 != ObjectInformation.Length) || ((null == Handle || Handle.IsInvalid) && ObjectInformation.Length != ObjectInfoClassSizes[ObjectInformationClass])))
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error((WIN32_ERROR)Windows.Win32.PInvoke.RtlNtStatusToDosError(res));
            }
            return res;
        }

        /// <summary>
        /// Creates a thread in the specified process.
        /// This doesn't use CsWin32 because they don't publicise its availability.
        /// </summary>
        /// <param name="threadHandle"></param>
        /// <param name="desiredAccess"></param>
        /// <param name="objectAttributes"></param>
        /// <param name="processHandle"></param>
        /// <param name="startAddress"></param>
        /// <param name="parameter"></param>
        /// <param name="createFlags"></param>
        /// <param name="zeroBits"></param>
        /// <param name="stackSize"></param>
        /// <param name="maximumStackSize"></param>
        /// <param name="attributeList"></param>
        /// <returns></returns>
        [DllImport("ntdll.dll", ExactSpelling = true, EntryPoint = "NtCreateThreadEx")]
        private static extern NTSTATUS NtCreateThreadExNative(out IntPtr threadHandle, THREAD_ACCESS_RIGHTS desiredAccess, IntPtr objectAttributes, IntPtr processHandle, IntPtr startAddress, IntPtr parameter, uint createFlags, uint zeroBits, uint stackSize, uint maximumStackSize, IntPtr attributeList);

        /// <summary>
        /// Creates a thread in the specified process.
        /// </summary>
        /// <param name="threadHandle"></param>
        /// <param name="desiredAccess"></param>
        /// <param name="objectAttributes"></param>
        /// <param name="processHandle"></param>
        /// <param name="startAddress"></param>
        /// <param name="parameter"></param>
        /// <param name="createFlags"></param>
        /// <param name="zeroBits"></param>
        /// <param name="stackSize"></param>
        /// <param name="maximumStackSize"></param>
        /// <param name="attributeList"></param>
        /// <returns></returns>
        internal static NTSTATUS NtCreateThreadEx(out SafeThreadHandle threadHandle, THREAD_ACCESS_RIGHTS desiredAccess, IntPtr objectAttributes, IntPtr processHandle, SafeVirtualAllocHandle startAddress, IntPtr parameter, uint createFlags, uint zeroBits, uint stackSize, uint maximumStackSize, IntPtr attributeList)
        {
            var res = NtCreateThreadExNative(out var hThread, desiredAccess, objectAttributes, processHandle, startAddress.DangerousGetHandle(), parameter, createFlags, zeroBits, stackSize, maximumStackSize, attributeList);
            if (res != NTSTATUS.STATUS_SUCCESS)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error((WIN32_ERROR)Windows.Win32.PInvoke.RtlNtStatusToDosError(res));
            }
            threadHandle = new SafeThreadHandle(hThread, true);
            return res;
        }

        /// <summary>
        /// Terminates a thread.
        /// This doesn't use CsWin32 because they don't publicise its availability.
        /// </summary>
        /// <param name="threadHandle"></param>
        /// <param name="exitStatus"></param>
        /// <returns></returns>
        [DllImport("ntdll.dll", ExactSpelling = true, EntryPoint = "NtTerminateThread")]
        private static extern NTSTATUS NtTerminateThreadNative(IntPtr threadHandle, NTSTATUS exitStatus);

        /// <summary>
        /// Terminates a thread.
        /// </summary>
        /// <param name="threadHandle"></param>
        /// <param name="exitStatus"></param>
        /// <returns></returns>
        internal static NTSTATUS NtTerminateThread(SafeThreadHandle threadHandle, NTSTATUS exitStatus)
        {
            var res = NtTerminateThreadNative(threadHandle.DangerousGetHandle(), exitStatus);
            if (res != NTSTATUS.STATUS_SUCCESS && res != exitStatus)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error((WIN32_ERROR)Windows.Win32.PInvoke.RtlNtStatusToDosError(res));
            }
            return res;
        }

        /// <summary>
        /// Lookup table for object system class struct sizes.
        /// </summary>
        internal static ReadOnlyDictionary<SYSTEM_INFORMATION_CLASS, int> SystemInfoClassSizes = new ReadOnlyDictionary<SYSTEM_INFORMATION_CLASS, int>(new Dictionary<SYSTEM_INFORMATION_CLASS, int>
        {
            { SYSTEM_INFORMATION_CLASS.SystemExtendedHandleInformation, Marshal.SizeOf<SYSTEM_HANDLE_INFORMATION_EX>() + Marshal.SizeOf<SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX>() },
        });

        /// <summary>
        /// Lookup table for object information class struct sizes.
        /// </summary>
        internal static ReadOnlyDictionary<OBJECT_INFORMATION_CLASS, int> ObjectInfoClassSizes = new ReadOnlyDictionary<OBJECT_INFORMATION_CLASS, int>(new Dictionary<OBJECT_INFORMATION_CLASS, int>
        {
            { OBJECT_INFORMATION_CLASS.ObjectNameInformation, Marshal.SizeOf<OBJECT_NAME_INFORMATION>() },
            { OBJECT_INFORMATION_CLASS.ObjectTypeInformation, Marshal.SizeOf<OBJECT_TYPE_INFORMATION>() },
            { OBJECT_INFORMATION_CLASS.ObjectTypesInformation, Marshal.SizeOf<OBJECT_TYPES_INFORMATION>() },
        });
    }
}
