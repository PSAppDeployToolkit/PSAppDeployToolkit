using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using PSADT.Extensions;
using PSADT.LibraryInterfaces;
using PSADT.Utilities;
using Windows.Wdk.Foundation;
using Windows.Win32.Foundation;
using Windows.Win32.Storage.FileSystem;
using Windows.Win32.System.Threading;

namespace PSADT.FileSystem
{
    /// <summary>
    /// Provides methods to manage file handles.
    /// </summary>
    public static class FileHandleManager
    {
        /// <summary>
        /// Static constructor to set up the necessary function pointers.
        /// </summary>
        static FileHandleManager()
        {
            // Query the system for the required buffer size for object types information.
            var objectTypesSize = NtDll.ObjectInfoClassSizes[OBJECT_INFORMATION_CLASS.ObjectTypesInformation];
            var objectTypeSize = NtDll.ObjectInfoClassSizes[OBJECT_INFORMATION_CLASS.ObjectTypeInformation];
            var typesBuffer = new byte[objectTypesSize];
            Span<byte> typesBufferPtr = typesBuffer;

            // Reallocate the buffer until we get the required size.
            var status = NtDll.NtQueryObject(null, OBJECT_INFORMATION_CLASS.ObjectTypesInformation, typesBufferPtr, out int typesBufferReqLength);
            while (status == NTSTATUS.STATUS_INFO_LENGTH_MISMATCH)
            {
                typesBuffer = new byte[typesBufferReqLength]; typesBufferPtr = typesBuffer;
                status = NtDll.NtQueryObject(null, OBJECT_INFORMATION_CLASS.ObjectTypesInformation, typesBufferPtr, out typesBufferReqLength);
            }

            // Read the number of types from the buffer and store the built-out dictionary.
            ref var typesInfo = ref Unsafe.As<byte, NtDll.OBJECT_TYPES_INFORMATION>(ref MemoryMarshal.GetReference(typesBufferPtr));
            var typesCount = typesInfo.NumberOfTypes;
            var typeTable = new Dictionary<ushort, string>((int)typesCount);
            var ptrOffset = LibraryUtilities.AlignUp(objectTypesSize);
            for (uint i = 0; i < typesCount; i++)
            {
                // Marshal the data into our structure and add the necessary values to the dictionary.
                ref var typeInfo = ref Unsafe.As<byte, NtDll.OBJECT_TYPE_INFORMATION>(ref MemoryMarshal.GetReference(typesBufferPtr.Slice(ptrOffset)));
                typeTable.Add(typeInfo.TypeIndex, typeInfo.TypeName.Buffer.ToString().TrimRemoveNull());
                ptrOffset += objectTypeSize + LibraryUtilities.AlignUp(typeInfo.TypeName.MaximumLength);
            }
            ObjectTypeLookupTable = new(typeTable);
        }

        /// <summary>
        /// Retrieves a list of open handles, optionally filtered by path.
        /// </summary>
        /// <param name="directoryPath"></param>
        /// <returns></returns>
        public static IReadOnlyList<FileHandleInfo> GetOpenHandles(string? directoryPath = null)
        {
            // Query the system for the required buffer size for handle information.
            var handleEntryExSize = Marshal.SizeOf<NtDll.SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX>();
            var handleInfoExSize = Marshal.SizeOf<NtDll.SYSTEM_HANDLE_INFORMATION_EX>();
            var handleBuffer = new byte[handleInfoExSize + handleEntryExSize];
            Span<byte> handleBufferPtr = handleBuffer;

            // Reallocate the buffer until we get the required size.
            var status = NtDll.NtQuerySystemInformation(SYSTEM_INFORMATION_CLASS.SystemExtendedHandleInformation, handleBufferPtr, out uint handleBufferReqLength);
            while (status == NTSTATUS.STATUS_INFO_LENGTH_MISMATCH)
            {
                handleBuffer = new byte[(int)handleBufferReqLength]; handleBufferPtr = handleBuffer;
                status = NtDll.NtQuerySystemInformation(SYSTEM_INFORMATION_CLASS.SystemExtendedHandleInformation, handleBufferPtr, out handleBufferReqLength);
            }

            // Loop through all handles and return list of open file handles.
            using var objectBuffers = new ThreadLocal<byte[]>(() => new byte[1024], trackAllValues: true);
            using var currentProcessHandle = Kernel32.GetCurrentProcess(); var ntPathLookupTable = FileSystemUtilities.GetNtPathLookupTable();
            ref var handleInfo = ref Unsafe.As<byte, NtDll.SYSTEM_HANDLE_INFORMATION_EX>(ref MemoryMarshal.GetReference(handleBufferPtr));
            var handleCount = handleInfo.NumberOfHandles.ToUInt32();
            var entryOffset = handleInfoExSize;
            ConcurrentBag<FileHandleInfo> openHandles = [];
            Parallel.For(0, (int)handleCount, i =>
            {
                // Read the handle information into a structure, skipping over if it's not a file or directory handle.
                ref var sysHandle = ref Unsafe.As<byte, NtDll.SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX>(ref MemoryMarshal.GetReference(handleBuffer.AsSpan(entryOffset + (handleEntryExSize * i))));
                if (!ObjectTypeLookupTable.TryGetValue(sysHandle.ObjectTypeIndex, out string? objectType) || (objectType != "File" && objectType != "Directory"))
                {
                    return;
                }

                // Open the owning process with rights to duplicate handles.
                SafeFileHandle fileProcessHandle;
                try
                {
                    fileProcessHandle = Kernel32.OpenProcess(PROCESS_ACCESS_RIGHTS.PROCESS_DUP_HANDLE, false, sysHandle.UniqueProcessId.ToUInt32());
                }
                catch (UnauthorizedAccessException ex) when (ex.HResult == HRESULT.E_ACCESSDENIED)
                {
                    return;
                }
                catch (ArgumentException ex) when (ex.HResult == HRESULT.E_INVALIDARG)
                {
                    return;
                }

                // Duplicate the remote handle into our process.
                SafeFileHandle fileDupHandle;
                using (SafeFileHandle fileOpenHandle = new((HANDLE)sysHandle.HandleValue, false))
                using (fileProcessHandle)
                {
                    try
                    {
                        Kernel32.DuplicateHandle(fileProcessHandle, fileOpenHandle, currentProcessHandle, out fileDupHandle, 0, true, DUPLICATE_HANDLE_OPTIONS.DUPLICATE_SAME_ACCESS);
                    }
                    catch (Win32Exception ex) when ((ex.NativeErrorCode == (int)WIN32_ERROR.ERROR_NOT_SUPPORTED) || (ex.NativeErrorCode == (int)WIN32_ERROR.ERROR_INVALID_HANDLE))
                    {
                        return;
                    }
                    catch (UnauthorizedAccessException ex) when (ex.HResult == HRESULT.E_ACCESSDENIED)
                    {
                        return;
                    }
                }

                // If the duplicated handle isn't a disk handle, skip to the next iteration.
                try
                {
                    if (Kernel32.GetFileType(fileDupHandle) != FILE_TYPE.FILE_TYPE_DISK)
                    {
                        return;
                    }
                }
                catch (Win32Exception ex) when (ex.NativeErrorCode == (int)WIN32_ERROR.ERROR_INVALID_HANDLE || ex.NativeErrorCode == (int)WIN32_ERROR.ERROR_INVALID_FUNCTION)
                {
                    return;
                }

                // Get the handle's name to check if it's a hard drive path.
                string? objectName;
                using (fileDupHandle)
                {
                    objectName = GetObjectName(fileDupHandle, objectBuffers.Value);
                }

                // Skip to next iteration if the handle doesn't meet our criteria.
                if (string.IsNullOrWhiteSpace(objectName) || !objectName!.StartsWith(@"\Device\HarddiskVolume"))
                {
                    return;
                }

                // Add the handle information to the list if it matches the specified directory path.
                string objectNameKey = $@"\{string.Join(@"\", objectName.Split(['\\'], StringSplitOptions.RemoveEmptyEntries).Take(2))}";
                if (ntPathLookupTable.TryGetValue(objectNameKey, out string? driveLetter) && objectName.Replace(objectNameKey, driveLetter) is string dosPath && (directoryPath is null || dosPath.StartsWith(directoryPath, StringComparison.OrdinalIgnoreCase)))
                {
                    openHandles.Add(new(sysHandle, dosPath, objectName, objectType));
                }
            });
            return new ReadOnlyCollection<FileHandleInfo>(openHandles.ToArray());
        }

        /// <summary>
        /// Retrieves a list of open handles for the system.
        /// </summary>
        /// <returns></returns>
        public static IReadOnlyList<FileHandleInfo> GetOpenHandles() => GetOpenHandles(null);

        /// <summary>
        /// Closes the specified handles.
        /// </summary>
        /// <param name="handleEntries"></param>
        public static void CloseHandles(NtDll.SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX[] handleEntries)
        {
            // Confirm the provided input isn't null.
            if (handleEntries is null)
            {
                throw new ArgumentNullException(nameof(handleEntries));
            }

            // Open each process handle, duplicate it with close source flag, then close the duplicated handle to close the original handle.
            using var currentProcessHandle = Kernel32.GetCurrentProcess();
            foreach (var handleEntry in handleEntries)
            {
                using var fileProcessHandle = Kernel32.OpenProcess(PROCESS_ACCESS_RIGHTS.PROCESS_DUP_HANDLE, false, handleEntry.UniqueProcessId.ToUInt32());
                using SafeFileHandle fileOpenHandle = new((HANDLE)handleEntry.HandleValue, false);
                Kernel32.DuplicateHandle(fileProcessHandle, fileOpenHandle, currentProcessHandle, out var localHandle, 0, false, DUPLICATE_HANDLE_OPTIONS.DUPLICATE_CLOSE_SOURCE);
                localHandle.Dispose();
                localHandle = null;
            }
        }

        /// <summary>
        /// Retrieves the name of an object associated with a handle.
        /// </summary>
        /// <param name="fileHandle"></param>
        /// <param name="objectBuffer"></param>
        /// <returns></returns>
        private static string? GetObjectName(SafeFileHandle fileHandle, Span<byte> objectBuffer)
        {
            // Make sure the provided handle is valid.
            if (fileHandle is null || fileHandle.IsClosed || fileHandle.IsInvalid)
            {
                throw new ArgumentNullException(nameof(fileHandle));
            }

            // Query the object for its name and return the result.
            try
            {
                NtDll.NtQueryObject(fileHandle, OBJECT_INFORMATION_CLASS.ObjectNameInformation, objectBuffer, out _);
                ref var objectBufferData = ref Unsafe.As<byte, OBJECT_NAME_INFORMATION>(ref MemoryMarshal.GetReference(objectBuffer));
                return objectBufferData.Name.Buffer.ToString()?.TrimRemoveNull();
            }
            catch (Win32Exception ex) when ((ex.NativeErrorCode == (int)WIN32_ERROR.ERROR_NOT_SUPPORTED) || (ex.NativeErrorCode == (int)WIN32_ERROR.ERROR_BAD_PATHNAME) || (ex.NativeErrorCode == (int)WIN32_ERROR.ERROR_TIMEOUT) || (ex.NativeErrorCode == (int)WIN32_ERROR.ERROR_IO_PENDING) || (ex.NativeErrorCode == (int)WIN32_ERROR.ERROR_PIPE_NOT_CONNECTED))
            {
                return null;
            }
            catch (UnauthorizedAccessException ex) when (ex.HResult == HRESULT.E_ACCESSDENIED)
            {
                return null;
            }
            finally
            {
                objectBuffer.Clear();
            }
        }

        /// <summary>
        /// The lookup table of object types.
        /// </summary>
        private static readonly ReadOnlyDictionary<ushort, string> ObjectTypeLookupTable;
    }
}
