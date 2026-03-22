using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using PSADT.Interop;
using PSADT.Interop.Extensions;
using PSADT.Interop.SafeHandles;
using PSADT.Interop.Utilities;
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
        /// Static constructor to initialize object type lookup state.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1810:Initialize reference type static fields inline", Justification = "The static constructor is required here.")]
        static FileHandleManager()
        {
            // Internal helper to get the required buffer size for object types information.
            static int GetObjectTypesBufferSize(int queryBufferSize)
            {
                Span<byte> queryBuffer = stackalloc byte[queryBufferSize];
                _ = NativeMethods.NtQueryObject(null, Interop.OBJECT_INFORMATION_CLASS.ObjectTypesInformation, queryBuffer, out uint requiredLength, retrievingLength: true);
                return (int)requiredLength;
            }

            // Allocate an appropriately sized buffer and query the system for object types information.
            int objectTypesSize = NativeMethods.ObjectInfoClassSizes[Interop.OBJECT_INFORMATION_CLASS.ObjectTypesInformation];
            int objectTypeSize = NativeMethods.ObjectInfoClassSizes[Interop.OBJECT_INFORMATION_CLASS.ObjectTypeInformation];
            Span<byte> typesBufferPtr = stackalloc byte[GetObjectTypesBufferSize(objectTypesSize)];
            _ = NativeMethods.NtQueryObject(null, Interop.OBJECT_INFORMATION_CLASS.ObjectTypesInformation, typesBufferPtr, out _);

            // Read the number of types from the buffer and store the built-out dictionary.
            ref readonly OBJECT_TYPES_INFORMATION typesInfo = ref typesBufferPtr.AsReadOnlyStructure<OBJECT_TYPES_INFORMATION>();
            uint typesCount = typesInfo.NumberOfTypes; Dictionary<ushort, string> typeTable = new((int)typesCount);
            int ptrOffset = (int)NativeMethods.ALIGN_UP_POINTER<nint>(objectTypesSize);
            for (uint i = 0; i < typesCount; i++)
            {
                // Marshal the data into our structure and add the necessary values to the dictionary.
                ref readonly OBJECT_TYPE_INFORMATION typeInfo = ref typesBufferPtr.Slice(ptrOffset).AsReadOnlyStructure<OBJECT_TYPE_INFORMATION>();
                typeTable.Add(typeInfo.TypeIndex, typeInfo.TypeName.ToManagedString());
                ptrOffset += objectTypeSize + (int)NativeMethods.ALIGN_UP_POINTER<nint>(typeInfo.TypeName.MaximumLength);
            }
            ObjectTypeLookupTable = new(typeTable);
        }

        /// <summary>
        /// Retrieves a collection of open file handles on the system, optionally filtered by a specified directory
        /// path.
        /// </summary>
        /// <remarks>This method queries the system for extended handle information and may require
        /// elevated permissions to access certain handles. It is designed to efficiently gather information about file
        /// handles, potentially using parallel processing to improve performance.</remarks>
        /// <param name="path">The optional path filter for the open file handles. If null, all open file handles are
        /// returned.</param>
        /// <returns>A read-only list of FileHandleInfo objects representing the open file handles that match the specified
        /// path.</returns>
        public static IReadOnlyList<FileHandleInfo> GetOpenHandles(string? path = null)
        {
            // Internal helper to get the required buffer size for extended handle information.
            static int GetExtendedHandleBufferSize(int queryBufferSize)
            {
                Span<byte> queryBuffer = stackalloc byte[queryBufferSize];
                _ = NativeMethods.NtQuerySystemInformation(SYSTEM_INFORMATION_CLASS.SystemExtendedHandleInformation, queryBuffer, out uint requiredLength, retrievingLength: true);
                return (int)requiredLength;
            }

            // Allocate an appropriately sized buffer and query the system for extended handle information. We increase
            // the size slightly to account for new handles being created between the size check and the actual query.
            if (path is not null)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(path);
            }
            int handleInfoExSize = Marshal.SizeOf<SYSTEM_HANDLE_INFORMATION_EX>();
            int handleEntryExSize = Marshal.SizeOf<SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX>();
            byte[] handleBuffer = new byte[GetExtendedHandleBufferSize(handleInfoExSize + handleEntryExSize) * 5 / 4];
            _ = NativeMethods.NtQuerySystemInformation(SYSTEM_INFORMATION_CLASS.SystemExtendedHandleInformation, handleBuffer, out _);

            // Loop through all handles and return list of open file handles.
            using ThreadLocal<SafePinnedGCHandle> threadBuffers = new(() => SafePinnedGCHandle.Alloc(new byte[1024]), trackAllValues: true);
            try
            {
                ref readonly SYSTEM_HANDLE_INFORMATION_EX handleInfo = ref handleBuffer.AsSpan().AsReadOnlyStructure<SYSTEM_HANDLE_INFORMATION_EX>();
                ReadOnlyDictionary<string, string> ntPathLookupTable = FileSystemUtilities.MakeNtPathLookupTable();
                using SafeProcessHandle currentProcessHandle = NativeMethods.GetCurrentProcess();
                ConcurrentBag<FileHandleInfo> openHandles = [];
                _ = Parallel.For(0, (int)handleInfo.NumberOfHandles, i =>
                {
                    // Read the handle information into a structure, skipping over if it's not a file or directory handle.
                    ref readonly SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX sysHandle = ref handleBuffer.AsSpan(handleInfoExSize + (handleEntryExSize * i)).AsReadOnlyStructure<SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX>();
                    if (!ObjectTypeLookupTable.TryGetValue(sysHandle.ObjectTypeIndex, out string? objectType) || (objectType != "File" && objectType != "Directory"))
                    {
                        return;
                    }

                    // Open the owning process with rights to duplicate handles.
                    SafeFileHandle fileProcessHandle;
                    try
                    {
                        fileProcessHandle = NativeMethods.OpenProcess(PROCESS_ACCESS_RIGHTS.PROCESS_DUP_HANDLE, false, (uint)sysHandle.UniqueProcessId);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        return;
                    }
                    catch (ArgumentException)
                    {
                        return;
                    }

                    // Duplicate the remote handle into our process.
                    SafeFileHandle fileDupHandle;
                    using (SafeFileHandle fileOpenHandle = new((HANDLE)sysHandle.HandleValue, false))
                    using (fileProcessHandle)
                    {
                        // Skip to the next iteration if the handle is invalid.
                        if (fileOpenHandle.IsInvalid)
                        {
                            return;
                        }

                        // Duplicate the handle into our process.
                        try
                        {
                            _ = NativeMethods.DuplicateHandle(fileProcessHandle, fileOpenHandle, currentProcessHandle, out fileDupHandle, 0, false, DUPLICATE_HANDLE_OPTIONS.DUPLICATE_SAME_ACCESS);
                        }
                        catch (Win32Exception ex) when (ex.NativeErrorCode is ((int)WIN32_ERROR.ERROR_NOT_SUPPORTED) or ((int)WIN32_ERROR.ERROR_INVALID_HANDLE))
                        {
                            return;
                        }
                        catch (UnauthorizedAccessException)
                        {
                            return;
                        }
                    }

                    // If the duplicated handle isn't a disk handle, skip to the next iteration.
                    try
                    {
                        if (NativeMethods.GetFileType(fileDupHandle) != FILE_TYPE.FILE_TYPE_DISK)
                        {
                            return;
                        }
                    }
                    catch (Win32Exception ex) when (ex.NativeErrorCode is ((int)WIN32_ERROR.ERROR_INVALID_HANDLE) or ((int)WIN32_ERROR.ERROR_INVALID_FUNCTION))
                    {
                        return;
                    }

                    // Get the handle's name to check if it's a hard drive path.
                    string objectName;
                    using (fileDupHandle)
                    {
                        SafePinnedGCHandle objectBuffer = threadBuffers.Value ?? throw new InvalidOperationException("Thread-local object buffer was not initialized.");
                        try
                        {
                            // Handle the result of the NtQueryObject call; returning early on certain expected failure codes.
                            NTSTATUS res = NativeMethods.NtQueryObjectWithTimeout(fileDupHandle, Interop.OBJECT_INFORMATION_CLASS.ObjectNameInformation, objectBuffer, 500, out _);
                            if (res == NTSTATUS.STATUS_TIMEOUT || res == NTSTATUS.STATUS_PENDING || res == NTSTATUS.STATUS_NOT_SUPPORTED || res == NTSTATUS.STATUS_OBJECT_PATH_INVALID || res == NTSTATUS.STATUS_ACCESS_DENIED || res == NTSTATUS.STATUS_PIPE_DISCONNECTED)
                            {
                                return;
                            }
                            if (res != NTSTATUS.STATUS_SUCCESS)
                            {
                                throw ExceptionUtilities.GetException(res);
                            }
                            ref readonly OBJECT_NAME_INFORMATION objectBufferData = ref objectBuffer.AsReadOnlyStructure<OBJECT_NAME_INFORMATION>();
                            if (objectBufferData.Name.Length == 0)
                            {
                                return;
                            }
                            objectName = objectBufferData.Name.ToManagedString();
                        }
                        finally
                        {
                            objectBuffer.Clear();
                        }
                    }

                    // Skip to next iteration if the handle doesn't meet our criteria.
                    if (!objectName.StartsWith(@"\Device\HarddiskVolume", StringComparison.Ordinal))
                    {
                        return;
                    }

                    // Add the handle information to the list if it matches the specified directory path.
                    string objectNameKey = $@"\{string.Join(@"\", objectName.Split(['\\'], StringSplitOptions.RemoveEmptyEntries).Take(2))}";
                    if (ntPathLookupTable.TryGetValue(objectNameKey, out string? driveLetter) && objectName.Replace(objectNameKey, driveLetter) is string dosPath && (path is null || dosPath.StartsWith(path, StringComparison.OrdinalIgnoreCase)))
                    {
                        openHandles.Add(new(sysHandle, dosPath, objectName, objectType));
                    }
                });
                return new ReadOnlyCollection<FileHandleInfo>([.. openHandles]);
            }
            finally
            {
                foreach (SafePinnedGCHandle objectBuffer in threadBuffers.Values)
                {
                    objectBuffer.Dispose();
                }
            }
        }

        /// <summary>
        /// Closes the specified process handles by duplicating them and then closing the duplicates.
        /// </summary>
        /// <remarks>This method ensures that all specified handles are properly closed, preventing
        /// resource leaks. It operates by duplicating each handle and closing the duplicate, which safely releases the
        /// original handle.</remarks>
        /// <param name="handleEntries">An array of SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX structures representing the handles to be closed. Each entry
        /// must be valid and correspond to an open handle.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="handleEntries"/> is null.</exception>
        public static void CloseHandles(SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX[] handleEntries)
        {
            // Open each process handle, duplicate it with close source flag, then close the duplicated handle to close the original handle.
            ArgumentNullException.ThrowIfNull(handleEntries);
            using SafeProcessHandle currentProcessHandle = NativeMethods.GetCurrentProcess();
            foreach (SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX handleEntry in handleEntries)
            {
                using SafeFileHandle fileProcessHandle = NativeMethods.OpenProcess(PROCESS_ACCESS_RIGHTS.PROCESS_DUP_HANDLE, false, (uint)handleEntry.UniqueProcessId);
                using SafeFileHandle fileOpenHandle = new((HANDLE)handleEntry.HandleValue, false);
                _ = NativeMethods.DuplicateHandle(fileProcessHandle, fileOpenHandle, currentProcessHandle, out SafeFileHandle localHandle, 0, false, DUPLICATE_HANDLE_OPTIONS.DUPLICATE_CLOSE_SOURCE);
                localHandle.Dispose();
            }
        }

        /// <summary>
        /// The lookup table of object types.
        /// </summary>
        private static readonly ReadOnlyDictionary<ushort, string> ObjectTypeLookupTable;
    }
}
