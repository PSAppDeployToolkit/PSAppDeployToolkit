using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using Microsoft.Win32.SafeHandles;
using PSADT.SafeHandles;
using PSADT.Utilities;
using Windows.Wdk.Foundation;
using Windows.Wdk.System.Threading;
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
            public FileSystemRights GrantedAccess;

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
            public OBJECT_ATTRIBUTES HandleAttributes;

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
            internal OBJECT_ATTRIBUTES InvalidAttributes;

            /// <summary>
            /// The type's generic mapping.
            /// </summary>
            internal GENERIC_MAPPING GenericMapping;

            /// <summary>
            /// The type's valid access mask.
            /// </summary>
            internal FileSystemRights ValidAccessMask;

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
        /// Enumeration of process information classes.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct SYSTEM_PROCESS_ID_INFORMATION
        {
            /// <summary>
            /// The number of processes in the system.
            /// </summary>
            internal IntPtr ProcessId;

            /// <summary>
            /// The number of threads in the system.
            /// </summary>
            internal UNICODE_STRING ImageName;
        }

        /// <summary>
        /// Gets the version info of the current operating system from the kernel.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Win32Exception"></exception>
        internal unsafe static NTSTATUS RtlGetVersion(out OSVERSIONINFOEXW lpVersionInformation)
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
        /// </summary>
        /// <param name="SystemInformationClass"></param>
        /// <param name="SystemInformation"></param>
        /// <param name="ReturnLength"></param>
        /// <returns></returns>
        internal static NTSTATUS NtQuerySystemInformation(SYSTEM_INFORMATION_CLASS SystemInformationClass, SafeMemoryHandle SystemInformation, out int ReturnLength)
        {
            if (SystemInformation is null || SystemInformation.IsClosed || SystemInformation.IsInvalid)
            {
                throw new ArgumentNullException(nameof(SystemInformation));
            }

            [DllImport("ntdll.dll", ExactSpelling = true)]
            static extern NTSTATUS NtQuerySystemInformation(SYSTEM_INFORMATION_CLASS SystemInformationClass, IntPtr SystemInformation, int SystemInformationLength, out int ReturnLength);
            bool SystemInformationAddRef = false;
            try
            {
                SystemInformation.DangerousAddRef(ref SystemInformationAddRef);
                var res = NtQuerySystemInformation(SystemInformationClass, SystemInformation.DangerousGetHandle(), SystemInformation.Length, out ReturnLength);
                if (res != NTSTATUS.STATUS_SUCCESS && res != NTSTATUS.STATUS_INFO_LENGTH_MISMATCH)
                {
                    throw ExceptionUtilities.GetExceptionForLastWin32Error((WIN32_ERROR)Windows.Win32.PInvoke.RtlNtStatusToDosError(res));
                }
                return res;
            }
            finally
            {
                if (SystemInformationAddRef)
                {
                    SystemInformation.DangerousRelease();
                }
            }
        }

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
            if (ObjectInformation is null || ObjectInformation.IsClosed || ObjectInformation.IsInvalid)
            {
                throw new ArgumentNullException(nameof(ObjectInformation));
            }
            if (Handle is null || Handle.IsClosed)
            {
                throw new ArgumentNullException(nameof(Handle));
            }

            [DllImport("ntdll.dll", ExactSpelling = true)]
            static extern NTSTATUS NtQueryObject(IntPtr ObjectHandle, OBJECT_INFORMATION_CLASS ObjectInformationClass, IntPtr ObjectInformation, int ObjectInformationLength, out int ReturnLength);
            bool ObjectInformationAddRef = false;
            bool HandleAddRef = false;
            try
            {
                ObjectInformation.DangerousAddRef(ref ObjectInformationAddRef);
                Handle.DangerousAddRef(ref HandleAddRef);
                var res = NtQueryObject(Handle.DangerousGetHandle(), ObjectInformationClass, ObjectInformation.DangerousGetHandle(), ObjectInformation.Length, out ReturnLength);
                if (res != NTSTATUS.STATUS_SUCCESS && ((null != Handle && !Handle.IsInvalid && !ObjectInformation.IsInvalid && 0 != ObjectInformation.Length) || ((null == Handle || Handle.IsInvalid) && ObjectInformation.Length != ObjectInfoClassSizes[ObjectInformationClass])))
                {
                    throw ExceptionUtilities.GetExceptionForLastWin32Error((WIN32_ERROR)Windows.Win32.PInvoke.RtlNtStatusToDosError(res));
                }
                return res;
            }
            finally
            {
                if (ObjectInformationAddRef)
                {
                    ObjectInformation.DangerousRelease();
                }
                if (HandleAddRef)
                {
                    Handle.DangerousRelease();
                }
            }
        }

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
        internal static NTSTATUS NtCreateThreadEx(out SafeThreadHandle threadHandle, THREAD_ACCESS_RIGHTS desiredAccess, IntPtr objectAttributes, SafeProcessHandle processHandle, SafeVirtualAllocHandle startAddress, IntPtr parameter, uint createFlags, uint zeroBits, uint stackSize, uint maximumStackSize, IntPtr attributeList)
        {
            if (startAddress is null || startAddress.IsClosed || startAddress.IsInvalid)
            {
                throw new ArgumentNullException(nameof(startAddress));
            }
            if (processHandle is null || processHandle.IsClosed)
            {
                throw new ArgumentNullException(nameof(processHandle));
            }

            [DllImport("ntdll.dll", ExactSpelling = true)]
            static extern NTSTATUS NtCreateThreadEx(out IntPtr threadHandle, THREAD_ACCESS_RIGHTS desiredAccess, IntPtr objectAttributes, IntPtr processHandle, IntPtr startAddress, IntPtr parameter, uint createFlags, uint zeroBits, uint stackSize, uint maximumStackSize, IntPtr attributeList);
            bool startAddressAddRef = false;
            bool processHandleAddRef = false;
            try
            {
                startAddress.DangerousAddRef(ref startAddressAddRef);
                processHandle.DangerousAddRef(ref processHandleAddRef);
                var res = NtCreateThreadEx(out var hThread, desiredAccess, objectAttributes, processHandle.DangerousGetHandle(), startAddress.DangerousGetHandle(), parameter, createFlags, zeroBits, stackSize, maximumStackSize, attributeList);
                if (res != NTSTATUS.STATUS_SUCCESS)
                {
                    throw ExceptionUtilities.GetExceptionForLastWin32Error((WIN32_ERROR)Windows.Win32.PInvoke.RtlNtStatusToDosError(res));
                }
                threadHandle = new SafeThreadHandle(hThread, true);
                return res;
            }
            finally
            {
                if (startAddressAddRef)
                {
                    startAddress.DangerousRelease();
                }
                if (processHandleAddRef)
                {
                    processHandle.DangerousRelease();
                }
            }
        }

        /// <summary>
        /// Terminates a thread.
        /// </summary>
        /// <param name="threadHandle"></param>
        /// <param name="exitStatus"></param>
        /// <returns></returns>
        internal static NTSTATUS NtTerminateThread(SafeThreadHandle threadHandle, NTSTATUS exitStatus)
        {
            if (threadHandle is null || threadHandle.IsClosed)
            {
                throw new ArgumentNullException(nameof(threadHandle));
            }

            [DllImport("ntdll.dll", ExactSpelling = true)]
            static extern NTSTATUS NtTerminateThread(IntPtr threadHandle, NTSTATUS exitStatus);
            bool threadHandleAddRef = false;
            try
            {
                threadHandle.DangerousAddRef(ref threadHandleAddRef);
                var res = NtTerminateThread(threadHandle.DangerousGetHandle(), exitStatus);
                if (res != NTSTATUS.STATUS_SUCCESS && res != exitStatus)
                {
                    throw ExceptionUtilities.GetExceptionForLastWin32Error((WIN32_ERROR)Windows.Win32.PInvoke.RtlNtStatusToDosError(res));
                }
                return res;
            }
            finally
            {
                if (threadHandleAddRef)
                {
                    threadHandle.DangerousRelease();
                }
            }
        }

        /// <summary>
        /// Queries information about a specified process.
        /// </summary>
        /// <param name="ProcessHandle"></param>
        /// <param name="ProcessInformationClass"></param>
        /// <param name="ProcessInformation"></param>
        /// <param name="ReturnLength"></param>
        /// <returns></returns>
        internal unsafe static NTSTATUS NtQueryInformationProcess(SafeHandle ProcessHandle, PROCESSINFOCLASS ProcessInformationClass, SafeMemoryHandle ProcessInformation, out uint ReturnLength)
        {
            if (ProcessHandle is null || ProcessHandle.IsClosed || ProcessHandle.IsInvalid)
            {
                throw new ArgumentNullException(nameof(ProcessHandle));
            }
            if (ProcessInformation is null || ProcessInformation.IsClosed)
            {
                throw new ArgumentNullException(nameof(ProcessInformation));
            }

            bool ProcessHandleAddRef = false;
            bool ProcessInformationAddRef = false;
            try
            {
                ProcessHandle.DangerousAddRef(ref ProcessHandleAddRef);
                ProcessInformation.DangerousAddRef(ref ProcessInformationAddRef);
                fixed (uint* ReturnLengthLocal = &ReturnLength)
                {
                    var res = Windows.Wdk.PInvoke.NtQueryInformationProcess((HANDLE)ProcessHandle.DangerousGetHandle(), ProcessInformationClass, ProcessInformation.DangerousGetHandle().ToPointer(), (uint)ProcessInformation.Length, ReturnLengthLocal);
                    if (res != NTSTATUS.STATUS_SUCCESS && (res != NTSTATUS.STATUS_INFO_LENGTH_MISMATCH || ProcessInformation.Length != 0))
                    {
                        throw ExceptionUtilities.GetExceptionForLastWin32Error((WIN32_ERROR)Windows.Win32.PInvoke.RtlNtStatusToDosError(res));
                    }
                    return res;
                }
            }
            finally
            {
                if (ProcessHandleAddRef)
                {
                    ProcessHandle.DangerousRelease();
                }
                if (ProcessInformationAddRef)
                {
                    ProcessInformation.DangerousRelease();
                }
            }
        }

        /// <summary>
        /// Retrieves basic information about a specified process.
        /// </summary>
        /// <remarks>This method wraps the native NtQueryInformationProcess function and provides a
        /// managed interface for querying process information.</remarks>
        /// <param name="processHandle">A handle to the process for which information is being queried. The handle must have the necessary access
        /// rights.</param>
        /// <param name="processInformation">When the method returns, contains a <see cref="PROCESS_BASIC_INFORMATION"/> structure with basic information
        /// about the process.</param>
        /// <returns>An <see cref="NTSTATUS"/> value indicating the result of the operation. Returns <see
        /// cref="NTSTATUS.STATUS_SUCCESS"/> if the operation succeeds.</returns>
        internal static unsafe NTSTATUS NtQueryInformationProcess(SafeHandle processHandle, out PROCESS_BASIC_INFORMATION processInformation)
        {
            bool processHandleAddRef = false;
            try
            {
                fixed (PROCESS_BASIC_INFORMATION* processInformationLocal = &processInformation)
                {
                    processHandle.DangerousAddRef(ref processHandleAddRef);  uint returnLength = 0;
                    var res = Windows.Wdk.PInvoke.NtQueryInformationProcess((HANDLE)processHandle.DangerousGetHandle(), PROCESSINFOCLASS.ProcessBasicInformation, processInformationLocal, (uint)Marshal.SizeOf<PROCESS_BASIC_INFORMATION>(), ref returnLength);
                    if (res != NTSTATUS.STATUS_SUCCESS)
                    {
                        throw ExceptionUtilities.GetExceptionForLastWin32Error((WIN32_ERROR)Windows.Win32.PInvoke.RtlNtStatusToDosError(res));
                    }
                    return res;
                }
            }
            finally
            {
                if (processHandleAddRef)
                {
                    processHandle.DangerousRelease();
                }
            }
        }

        /// <summary>
        /// Lookup table for object information class struct sizes.
        /// </summary>
        internal static ReadOnlyDictionary<OBJECT_INFORMATION_CLASS, int> ObjectInfoClassSizes = new(new Dictionary<OBJECT_INFORMATION_CLASS, int>
        {
            { OBJECT_INFORMATION_CLASS.ObjectNameInformation, Marshal.SizeOf<OBJECT_NAME_INFORMATION>() },
            { OBJECT_INFORMATION_CLASS.ObjectTypeInformation, Marshal.SizeOf<OBJECT_TYPE_INFORMATION>() },
            { OBJECT_INFORMATION_CLASS.ObjectTypesInformation, Marshal.SizeOf<OBJECT_TYPES_INFORMATION>() },
        });
    }
}
