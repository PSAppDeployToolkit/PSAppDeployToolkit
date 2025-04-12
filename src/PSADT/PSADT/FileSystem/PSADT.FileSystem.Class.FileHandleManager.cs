using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using PSADT.LibraryInterfaces;
using PSADT.Utilities;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Threading;
using Windows.Wdk.Foundation;

namespace PSADT.FileSystem
{
    /// <summary>
    /// Provides methods to manage file handles.
    /// </summary>
    public static class FileHandleManager
    {
        /// <summary>
        /// Retrieves a list of open handles, optionally filtered by path.
        /// </summary>
        /// <param name="directoryPath"></param>
        /// <param name="close"></param>
        /// <returns></returns>
        public static ReadOnlyCollection<FileHandleInfo> GetOpenHandles(string? directoryPath = null)
        {
            // Pre-calculate the sizes of the structures we need to read.
            var handleEntryExSize = Marshal.SizeOf<NtDll.SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX>();
            var handleInfoExSize = Marshal.SizeOf<NtDll.SYSTEM_HANDLE_INFORMATION_EX>();

            // Query the total system handle information.
            var handleBufferSize = handleInfoExSize + handleEntryExSize;
            var handleBufferPtr = Marshal.AllocHGlobal(handleBufferSize);
            var status = NtDll.NtQuerySystemInformation(SYSTEM_INFORMATION_CLASS.SystemExtendedHandleInformation, handleBufferPtr, handleBufferSize, out int handleBufferReqLength);
            while (status == NTSTATUS.STATUS_INFO_LENGTH_MISMATCH)
            {
                Marshal.FreeHGlobal(handleBufferPtr);
                handleBufferSize = handleBufferReqLength;
                handleBufferPtr = Marshal.AllocHGlobal(handleBufferReqLength);
                status = NtDll.NtQuerySystemInformation(SYSTEM_INFORMATION_CLASS.SystemExtendedHandleInformation, handleBufferPtr, handleBufferSize, out handleBufferReqLength);
            }
            if (status != NTSTATUS.STATUS_SUCCESS)
            {
                Marshal.FreeHGlobal(handleBufferPtr);
                throw ExceptionUtilities.GetExceptionForLastWin32Error((WIN32_ERROR)PInvoke.RtlNtStatusToDosError(status));
            }

            // Process all handles and return a read-only list of the ones matching our directory filter.
            try
            {
                var handleCount = Marshal.PtrToStructure<NtDll.SYSTEM_HANDLE_INFORMATION_EX>(handleBufferPtr).NumberOfHandles.ToUInt64();
                var handleEntry = handleBufferPtr + handleInfoExSize;
                var openHandles = new List<FileHandleInfo>();
                for (ulong i = 0; i < handleCount; i++)
                {
                    // Read the handle information into a structure.
                    var sysHandle = Marshal.PtrToStructure<NtDll.SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX>(handleEntry);
                    handleEntry += handleEntryExSize;

                    // Skip this handle if it's not a file or directory handle.
                    if (!ObjectTypeLookupTable.TryGetValue(sysHandle.ObjectTypeIndex, out string? objectType) || (objectType != "File" && objectType != "Directory"))
                    {
                        continue;
                    }

                    // Filter out handles that are known to cause NtQueryObject to hang.
                    if ((sysHandle.GrantedAccess == 0x00120189 && (sysHandle.HandleAttributes == 0 || sysHandle.HandleAttributes == 2)) ||
                        (sysHandle.GrantedAccess == 0x0012019F && (sysHandle.HandleAttributes == 0 || sysHandle.HandleAttributes == 2)) ||
                        (sysHandle.GrantedAccess == 0x001A019F && (sysHandle.HandleAttributes == 2)))
                    {
                        continue;
                    }

                    // Open the owning process with rights to duplicate handles.
                    HANDLE processHandle;
                    try
                    {
                        processHandle = Kernel32.OpenProcess(PROCESS_ACCESS_RIGHTS.PROCESS_DUP_HANDLE, false, sysHandle.UniqueProcessId.ToUInt32());
                    }
                    catch (UnauthorizedAccessException ex) when (ex.HResult == HRESULT.E_ACCESSDENIED)
                    {
                        continue;
                    }
                    catch (ArgumentException ex) when (ex.HResult == HRESULT.E_INVALIDARG)
                    {
                        continue;
                    }

                    // Duplicate the remote handle into our process.
                    HANDLE localHandle;
                    try
                    {
                        Kernel32.DuplicateHandle(processHandle, (HANDLE)sysHandle.HandleValue, PInvoke.GetCurrentProcess(), out localHandle, 0, true, DUPLICATE_HANDLE_OPTIONS.DUPLICATE_SAME_ACCESS);
                    }
                    catch (Win32Exception ex) when ((ex.NativeErrorCode == (int)WIN32_ERROR.ERROR_NOT_SUPPORTED) || (ex.NativeErrorCode == (int)WIN32_ERROR.ERROR_INVALID_HANDLE))
                    {
                        continue;
                    }
                    catch (UnauthorizedAccessException ex) when (ex.HResult == HRESULT.E_ACCESSDENIED)
                    {
                        continue;
                    }
                    finally
                    {
                        Kernel32.CloseHandle(ref processHandle);
                    }

                    // Get the handle's name to check if it's a hard drive path.
                    string? objectName;
                    try
                    {
                        objectName = GetObjectName(localHandle);
                        if (string.IsNullOrWhiteSpace(objectName) || !objectName!.StartsWith("\\Device\\HarddiskVolume"))
                        {
                            continue;
                        }
                    }
                    finally
                    {
                        Kernel32.CloseHandle(ref localHandle);
                    }

                    // Add the handle information to the list if it matches the specified directory path.
                    if (FileSystemUtilities.ConvertNtPathToDosPath(objectName) is string dosPath && (null == directoryPath || dosPath.StartsWith(directoryPath, StringComparison.OrdinalIgnoreCase)))
                    {
                        openHandles.Add(new FileHandleInfo(sysHandle, dosPath, objectName, objectType));
                    }
                }                
                return openHandles.AsReadOnly();
            }
            finally
            {
                Marshal.FreeHGlobal(handleBufferPtr);
            }
        }

        /// <summary>
        /// Retrieves a list of open handles for the system.
        /// </summary>
        /// <returns></returns>
        public static ReadOnlyCollection<FileHandleInfo> GetOpenHandles()
        {
            return GetOpenHandles(null);
        }

        /// <summary>
        /// Closes the specified handles.
        /// </summary>
        /// <param name="handleEntries"></param>
        public static void CloseHandles(NtDll.SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX[] handleEntries)
        {
            // Open each process handle, duplicate it with close source flag, then close the duplicated handle to close the original handle.
            foreach (var handleEntry in handleEntries)
            {
                var processHandle = Kernel32.OpenProcess(PROCESS_ACCESS_RIGHTS.PROCESS_DUP_HANDLE, false, handleEntry.UniqueProcessId.ToUInt32());
                try
                {
                    Kernel32.DuplicateHandle(processHandle, (HANDLE)handleEntry.HandleValue, PInvoke.GetCurrentProcess(), out var localHandle, 0, false, DUPLICATE_HANDLE_OPTIONS.DUPLICATE_CLOSE_SOURCE);
                    Kernel32.CloseHandle(ref localHandle);
                }
                finally
                {
                    Kernel32.CloseHandle(ref processHandle);
                }
            }
        }

        /// <summary>
        /// Retrieves the name of an object associated with a handle.
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private static string? GetObjectName(HANDLE handle)
        {
            // Do an initial query to get the required buffer size.
            int bufferReqLength;
            try
            {
                NtDll.NtQueryObject(handle, OBJECT_INFORMATION_CLASS.ObjectNameInformation, IntPtr.Zero, 0, out bufferReqLength);
            }
            catch (Win32Exception ex) when ((ex.NativeErrorCode == (int)WIN32_ERROR.ERROR_NOT_SUPPORTED) || (ex.NativeErrorCode == (int)WIN32_ERROR.ERROR_BAD_PATHNAME))
            {
                return null;
            }
            catch (UnauthorizedAccessException ex) when (ex.HResult == HRESULT.E_ACCESSDENIED)
            {
                return null;
            }

            // Query the object information and return the string result.
            IntPtr bufferPtr = Marshal.AllocHGlobal(bufferReqLength);
            try
            {
                try
                {
                    NtDll.NtQueryObject(handle, OBJECT_INFORMATION_CLASS.ObjectNameInformation, bufferPtr, bufferReqLength, out _);
                }
                catch (Win32Exception ex) when ((ex.NativeErrorCode == (int)WIN32_ERROR.ERROR_NOT_SUPPORTED) || (ex.NativeErrorCode == (int)WIN32_ERROR.ERROR_BAD_PATHNAME))
                {
                    return null;
                }
                catch (UnauthorizedAccessException ex) when (ex.HResult == HRESULT.E_ACCESSDENIED)
                {
                    return null;
                }
                return Marshal.PtrToStructure<OBJECT_NAME_INFORMATION>(bufferPtr).Name.Buffer.ToString()?.Trim('\0').Trim();
            }
            finally
            {
                Marshal.FreeHGlobal(bufferPtr);
            }
        }

        /// <summary>
        /// Retrieves a lookup table of object types.
        /// </summary>
        /// <returns></returns>
        private static ReadOnlyDictionary<ushort, string> GetObjectTypeLookupTable()
        {
            // Pre-calculate the sizes of the structures we need to read.
            var objectTypesSize = NtDll.ObjectInfoClassSizes[OBJECT_INFORMATION_CLASS.ObjectTypesInformation];
            var objectTypeSize = NtDll.ObjectInfoClassSizes[OBJECT_INFORMATION_CLASS.ObjectTypeInformation];

            // Query the system for all object type info.
            var typesBufferSize = objectTypesSize;
            var typesBufferPtr = Marshal.AllocHGlobal(typesBufferSize);
            var status = NtDll.NtQueryObject((HANDLE)IntPtr.Zero, OBJECT_INFORMATION_CLASS.ObjectTypesInformation, typesBufferPtr, typesBufferSize, out int typesBufferReqLength);
            while (status == NTSTATUS.STATUS_INFO_LENGTH_MISMATCH)
            {
                Marshal.FreeHGlobal(typesBufferPtr);
                typesBufferSize = typesBufferReqLength;
                typesBufferPtr = Marshal.AllocHGlobal(typesBufferReqLength);
                status = NtDll.NtQueryObject((HANDLE)IntPtr.Zero, OBJECT_INFORMATION_CLASS.ObjectTypesInformation, typesBufferPtr, typesBufferSize, out typesBufferReqLength);
            }

            // Read the number of types from the buffer and return a built-out dictionary.
            try
            {
                var typesCount = Marshal.PtrToStructure<NtDll.OBJECT_TYPES_INFORMATION>(typesBufferPtr).NumberOfTypes;
                var typeTable = new Dictionary<ushort, string>((int)typesCount);
                var ptrOffset = LibraryUtilities.AlignUp(objectTypesSize);
                for (uint i = 0; i < typesCount; i++)
                {
                    // Marshal the data into our structure and add the necessary values to the dictionary.
                    var typeInfo = Marshal.PtrToStructure<NtDll.OBJECT_TYPE_INFORMATION>(IntPtr.Add(typesBufferPtr, ptrOffset));
                    typeTable.Add(typeInfo.TypeIndex, typeInfo.TypeName.Buffer.ToString().Trim('\0').Trim());
                    ptrOffset += objectTypeSize + LibraryUtilities.AlignUp(typeInfo.TypeName.MaximumLength);
                }
                return new ReadOnlyDictionary<ushort, string>(typeTable);
            }
            finally
            {
                Marshal.FreeHGlobal(typesBufferPtr);
            }
        }

        /// <summary>
        /// The lookup table of object types.
        /// </summary>
        private static readonly ReadOnlyDictionary<ushort, string> ObjectTypeLookupTable = GetObjectTypeLookupTable();
    }
}
