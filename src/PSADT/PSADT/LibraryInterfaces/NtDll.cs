using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using PSADT.Utilities;
using Windows.Wdk.Foundation;
using Windows.Wdk.System.Threading;
using Windows.Win32.Foundation;
using Windows.Win32.Security;
using Windows.Win32.System.SystemInformation;

namespace PSADT.LibraryInterfaces
{
    /// <summary>
    /// Provides managed wrappers and supporting types for selected native Windows NT system calls and structures from
    /// ntdll.dll.
    /// </summary>
    /// <remarks>The NtDll class exposes low-level interop methods for advanced scenarios that require direct
    /// access to Windows NT kernel APIs, such as querying system information, manipulating handles, and creating or
    /// terminating threads. These methods are intended for use by experienced developers who need fine-grained control
    /// over system resources. Improper use of these APIs can lead to resource leaks, security vulnerabilities, or
    /// system instability. All methods in this class require careful management of handles and memory buffers, and may
    /// throw exceptions for certain error conditions.</remarks>
    public static class NtDll
    {
        /// <summary>
        /// System information class for querying system handles.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public readonly struct SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX
        {
            /// <summary>
            /// The kernel object's address.
            /// </summary>
            public readonly IntPtr Object;

            /// <summary>
            /// The owning process's identifier.
            /// </summary>
            public readonly UIntPtr UniqueProcessId;

            /// <summary>
            /// The handle's numerical identifier.
            /// </summary>
            public readonly UIntPtr HandleValue;

            /// <summary>
            /// The type of access granted to the handle.
            /// </summary>
            public readonly FileSystemRights GrantedAccess;

            /// <summary>
            /// The number of references to the object.
            /// </summary>
            public readonly ushort CreatorBackTraceIndex;

            /// <summary>
            /// The type of the object.
            /// </summary>
            public readonly ushort ObjectTypeIndex;

            /// <summary>
            /// The handle attributes.
            /// </summary>
            public readonly OBJECT_ATTRIBUTES HandleAttributes;

            /// <summary>
            /// Reserved for future use.
            /// </summary>
            public readonly uint Reserved;
        }

        /// <summary>
        /// System information class for querying system handle information.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal readonly struct SYSTEM_HANDLE_INFORMATION_EX
        {
            /// <summary>
            /// The number of handles in the system.
            /// </summary>
            internal readonly UIntPtr NumberOfHandles;

            /// <summary>
            /// Reserved for future use.
            /// </summary>
            internal readonly UIntPtr Reserved;
        }

        /// <summary>
        /// System information class for querying system handle information.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal readonly struct OBJECT_TYPE_INFORMATION
        {
            /// <summary>
            /// The name of the type/
            /// </summary>
            internal readonly UNICODE_STRING TypeName;

            /// <summary>
            /// The type's object count.
            /// </summary>
            internal readonly uint TotalNumberOfObjects;

            /// <summary>
            /// The type's handle count.
            /// </summary>
            internal readonly uint TotalNumberOfHandles;

            /// <summary>
            /// The type's paged pool usage.
            /// </summary>
            internal readonly uint TotalPagedPoolUsage;

            /// <summary>
            /// The type's non-paged pool usage.
            /// </summary>
            internal readonly uint TotalNonPagedPoolUsage;

            /// <summary>
            /// The type's name pool usage.
            /// </summary>
            internal readonly uint TotalNamePoolUsage;

            /// <summary>
            /// The type's handle table usage.
            /// </summary>
            internal readonly uint TotalHandleTableUsage;

            /// <summary>
            /// The type's high-water mark for object count.
            /// </summary>
            internal readonly uint HighWaterNumberOfObjects;

            /// <summary>
            /// The type's high-water mark for handle count.
            /// </summary>
            internal readonly uint HighWaterNumberOfHandles;

            /// <summary>
            /// The type's high-water mark for paged pool usage.
            /// </summary>
            internal readonly uint HighWaterPagedPoolUsage;

            /// <summary>
            /// The type's high-water mark for non-paged pool usage.
            /// </summary>
            internal readonly uint HighWaterNonPagedPoolUsage;

            /// <summary>
            /// The type's high-water mark for name pool usage.
            /// </summary>
            internal readonly uint HighWaterNamePoolUsage;

            /// <summary>
            /// The type's high-water mark for handle table usage.
            /// </summary>
            internal readonly uint HighWaterHandleTableUsage;

            /// <summary>
            /// The type's invalid attributes.
            /// </summary>
            internal readonly OBJECT_ATTRIBUTES InvalidAttributes;

            /// <summary>
            /// The type's generic mapping.
            /// </summary>
            internal readonly GENERIC_MAPPING GenericMapping;

            /// <summary>
            /// The type's valid access mask.
            /// </summary>
            internal readonly FileSystemRights ValidAccessMask;

            /// <summary>
            /// The type's security required flag.
            /// </summary>
            [MarshalAs(UnmanagedType.U1)]
            internal readonly bool SecurityRequired;

            /// <summary>
            /// The type's security descriptor present flag.
            /// </summary>
            [MarshalAs(UnmanagedType.U1)]
            internal readonly bool MaintainHandleCount;

            /// <summary>
            /// The object type's index.
            /// </summary>
            internal readonly byte TypeIndex;

            /// <summary>
            /// Reserved for future use.
            /// </summary>
            internal readonly sbyte ReservedByte;

            /// <summary>
            /// The object type's pool type.
            /// </summary>
            internal readonly uint PoolType;

            /// <summary>
            /// The default paged pool charge for the object type.
            /// </summary>
            internal readonly uint DefaultPagedPoolCharge;

            /// <summary>
            /// The default non-paged pool charge for the object type.
            /// </summary>
            internal readonly uint DefaultNonPagedPoolCharge;
        }

        /// <summary>
        /// System information class for querying system object types.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal readonly struct OBJECT_TYPES_INFORMATION
        {
            /// <summary>
            /// The number of object types in the system.
            /// </summary>
            internal readonly uint NumberOfTypes;
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
        /// Retrieves version information about the currently running Windows operating system.
        /// </summary>
        /// <remarks>This method throws an exception if the underlying native call fails. The output
        /// parameter is always initialized before the native call is made.</remarks>
        /// <param name="lpVersionInformation">When this method returns, contains an OSVERSIONINFOEXW structure that receives the operating system version
        /// information. The structure's dwOSVersionInfoSize field is initialized automatically.</param>
        /// <returns>A value of type NTSTATUS indicating the result of the operation. Returns STATUS_SUCCESS if the version
        /// information was retrieved successfully.</returns>
        internal static NTSTATUS RtlGetVersion(out OSVERSIONINFOEXW lpVersionInformation)
        {
            lpVersionInformation = new() { dwOSVersionInfoSize = (uint)Marshal.SizeOf<OSVERSIONINFOEXW>() };
            NTSTATUS res;
            unsafe
            {
                fixed (OSVERSIONINFOEXW* lpVersionInformationLocal = &lpVersionInformation)
                {
                    res = Windows.Wdk.PInvoke.RtlGetVersion((OSVERSIONINFOW*)lpVersionInformationLocal);
                }
            }
            if (res != NTSTATUS.STATUS_SUCCESS)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error((WIN32_ERROR)Windows.Win32.PInvoke.RtlNtStatusToDosError(res));
            }
            return res;
        }

        /// <summary>
        /// Retrieves system information for the specified information class by calling the native
        /// NtQuerySystemInformation function.
        /// </summary>
        /// <remarks>If the buffer specified by SystemInformation is too small to hold the requested data,
        /// the method returns STATUS_INFO_LENGTH_MISMATCH and sets ReturnLength to the required buffer size. The caller
        /// can then allocate a larger buffer and retry the operation. This method throws an exception for NTSTATUS
        /// values other than STATUS_SUCCESS and STATUS_INFO_LENGTH_MISMATCH.</remarks>
        /// <param name="SystemInformationClass">The type of system information to be queried. This value determines the structure and content of the data
        /// returned in the SystemInformation buffer.</param>
        /// <param name="SystemInformation">A buffer that receives the requested system information. The buffer must be large enough to hold the data
        /// for the specified information class and cannot be empty.</param>
        /// <param name="ReturnLength">When this method returns, contains the number of bytes written to the SystemInformation buffer or the number
        /// of bytes required if the buffer is too small.</param>
        /// <returns>An NTSTATUS code indicating the result of the operation. Returns STATUS_SUCCESS if successful, or
        /// STATUS_INFO_LENGTH_MISMATCH if the buffer is too small.</returns>
        /// <exception cref="ArgumentNullException">Thrown if SystemInformation is empty.</exception>
        internal static NTSTATUS NtQuerySystemInformation(SYSTEM_INFORMATION_CLASS SystemInformationClass, Span<byte> SystemInformation, out uint ReturnLength)
        {
            if (SystemInformation.IsEmpty)
            {
                throw new ArgumentNullException(nameof(SystemInformation));
            }
            ReturnLength = 0;
            NTSTATUS res;
            unsafe
            {
                fixed (byte* SystemInformationLocal = SystemInformation)
                {
                    res = Windows.Wdk.PInvoke.NtQuerySystemInformation((Windows.Wdk.System.SystemInformation.SYSTEM_INFORMATION_CLASS)SystemInformationClass, SystemInformationLocal, (uint)SystemInformation.Length, ref ReturnLength);
                }
            }
            if (res != NTSTATUS.STATUS_SUCCESS && res != NTSTATUS.STATUS_INFO_LENGTH_MISMATCH)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error((WIN32_ERROR)Windows.Win32.PInvoke.RtlNtStatusToDosError(res));
            }
            return res;
        }

        /// <summary>
        /// Queries information about the specified object handle by invoking the native NtQueryObject function.
        /// </summary>
        /// <remarks>This method is a managed wrapper for the native NtQueryObject function in ntdll.dll.
        /// The caller is responsible for providing a buffer of sufficient size in ObjectInformation. If the buffer is
        /// too small, the required size is returned in ReturnLength.</remarks>
        /// <param name="Handle">A SafeHandle representing the object to query. The handle must be valid and not closed.</param>
        /// <param name="ObjectInformationClass">The type of information to retrieve about the object, specified as an OBJECT_INFORMATION_CLASS value.</param>
        /// <param name="ObjectInformation">A span of bytes that receives the requested information. Must not be empty.</param>
        /// <param name="ReturnLength">When this method returns, contains the number of bytes written to ObjectInformation or required to store the
        /// information, depending on the operation.</param>
        /// <returns>An NTSTATUS value indicating the result of the operation. STATUS_SUCCESS indicates success; otherwise, an
        /// error code is returned.</returns>
        /// <exception cref="ArgumentNullException">Thrown if Handle is null or closed, or if ObjectInformation is empty.</exception>
        internal static NTSTATUS NtQueryObject(SafeHandle? Handle, OBJECT_INFORMATION_CLASS ObjectInformationClass, Span<byte> ObjectInformation, out uint ReturnLength)
        {
            if (ObjectInformation.IsEmpty)
            {
                throw new ArgumentNullException(nameof(ObjectInformation));
            }
            bool HandleAddRef = false;
            try
            {
                Handle?.DangerousAddRef(ref HandleAddRef);
                NTSTATUS res = Windows.Wdk.PInvoke.NtQueryObject(Handle is not null ? (HANDLE)Handle.DangerousGetHandle() : HANDLE.Null, (Windows.Wdk.Foundation.OBJECT_INFORMATION_CLASS)ObjectInformationClass, ObjectInformation, out ReturnLength);
                if (res != NTSTATUS.STATUS_SUCCESS && ((Handle is not null && !Handle.IsInvalid && 0 != ObjectInformation.Length) || ((Handle is null || Handle.IsInvalid) && ObjectInformation.Length != ObjectInfoClassSizes[ObjectInformationClass])))
                {
                    throw ExceptionUtilities.GetExceptionForLastWin32Error((WIN32_ERROR)Windows.Win32.PInvoke.RtlNtStatusToDosError(res));
                }
                return res;
            }
            finally
            {
                if (HandleAddRef)
                {
                    Handle?.DangerousRelease();
                }
            }
        }

        /// <summary>
        /// Retrieves information about the specified process by querying the native Windows NT API.
        /// </summary>
        /// <remarks>This method is a low-level interop call to the Windows NT kernel and is intended for
        /// advanced scenarios. The caller is responsible for ensuring that the ProcessInformation buffer is
        /// appropriately sized for the requested information class. Incorrect usage may result in partial or invalid
        /// data. This method may throw exceptions for certain NTSTATUS error codes.</remarks>
        /// <param name="ProcessHandle">A handle to the process to be queried. The handle must have appropriate access rights for the requested
        /// information.</param>
        /// <param name="ProcessInformationClass">The type of process information to retrieve. Specifies the class of information to be queried.</param>
        /// <param name="ProcessInformation">A span of bytes that receives the requested process information. The format and required size depend on the
        /// value of the ProcessInformationClass parameter. Must not be empty.</param>
        /// <param name="ReturnLength">When this method returns, contains the number of bytes written to ProcessInformation or, if the buffer was
        /// too small, the number of bytes required.</param>
        /// <returns>An NTSTATUS code that indicates the result of the operation. STATUS_SUCCESS indicates success;
        /// STATUS_INFO_LENGTH_MISMATCH indicates that the buffer was too small.</returns>
        /// <exception cref="ArgumentNullException">Thrown if ProcessHandle is null, closed, or invalid, or if ProcessInformation is empty.</exception>
        internal static NTSTATUS NtQueryInformationProcess(SafeHandle ProcessHandle, PROCESSINFOCLASS ProcessInformationClass, Span<byte> ProcessInformation, out uint ReturnLength)
        {
            if (ProcessHandle is null || ProcessHandle.IsClosed)
            {
                throw new ArgumentNullException(nameof(ProcessHandle));
            }
            bool ProcessHandleAddRef = false;
            try
            {
                ProcessHandle.DangerousAddRef(ref ProcessHandleAddRef);
                NTSTATUS res;
                unsafe
                {
                    fixed (byte* ProcessInformationLocal = ProcessInformation)
                    fixed (uint* ReturnLengthLocal = &ReturnLength)
                    {
                        res = Windows.Wdk.PInvoke.NtQueryInformationProcess((HANDLE)ProcessHandle.DangerousGetHandle(), ProcessInformationClass, ProcessInformationLocal, (uint)ProcessInformation.Length, ReturnLengthLocal);
                    }
                }
                if (res != NTSTATUS.STATUS_SUCCESS && (res != NTSTATUS.STATUS_INFO_LENGTH_MISMATCH || ProcessInformation.Length != 0))
                {
                    throw ExceptionUtilities.GetExceptionForLastWin32Error((WIN32_ERROR)Windows.Win32.PInvoke.RtlNtStatusToDosError(res));
                }
                return res;
            }
            finally
            {
                if (ProcessHandleAddRef)
                {
                    ProcessHandle.DangerousRelease();
                }
            }
        }

        /// <summary>
        /// Lookup table for object information class struct sizes.
        /// </summary>
        internal static ReadOnlyDictionary<OBJECT_INFORMATION_CLASS, int> ObjectInfoClassSizes = new(new Dictionary<OBJECT_INFORMATION_CLASS, int>()
        {
            { OBJECT_INFORMATION_CLASS.ObjectNameInformation, Marshal.SizeOf<OBJECT_NAME_INFORMATION>() },
            { OBJECT_INFORMATION_CLASS.ObjectTypeInformation, Marshal.SizeOf<OBJECT_TYPE_INFORMATION>() },
            { OBJECT_INFORMATION_CLASS.ObjectTypesInformation, Marshal.SizeOf < OBJECT_TYPES_INFORMATION >() }
        });
    }
}
