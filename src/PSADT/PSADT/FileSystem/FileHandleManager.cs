using System;
using System.Linq;
using System.Diagnostics;
using System.ComponentModel;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using PSADT.Extensions;
using PSADT.LibraryInterfaces;
using PSADT.SafeHandles;
using PSADT.Utilities;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Memory;
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
        /// Static constructor to set up the necessary function pointers.
        /// </summary>
        static FileHandleManager()
        {
            // Load the necessary libraries and get the function pointers.
            using (var hNtdllPtr = Kernel32.LoadLibrary("ntdll.dll"))
            {
                NtQueryObjectProcAddr = Kernel32.GetProcAddress(hNtdllPtr, "NtQueryObject");
            }
            using (var hKernel32Ptr = Kernel32.LoadLibrary("kernel32.dll"))
            {
                ExitThreadProcAddr = Kernel32.GetProcAddress(hKernel32Ptr, "ExitThread");
            }

            // Query the system for the required buffer size for object types information.
            var objectTypesSize = NtDll.ObjectInfoClassSizes[OBJECT_INFORMATION_CLASS.ObjectTypesInformation];
            var objectTypeSize = NtDll.ObjectInfoClassSizes[OBJECT_INFORMATION_CLASS.ObjectTypeInformation];
            using (var typesBufferPtr = SafeHGlobalHandle.Alloc(objectTypesSize))
            {
                // Reallocate the buffer until we get the required size.
                var status = NtDll.NtQueryObject(SafeBaseHandle.NullHandle, OBJECT_INFORMATION_CLASS.ObjectTypesInformation, typesBufferPtr, out int typesBufferReqLength);
                while (status == NTSTATUS.STATUS_INFO_LENGTH_MISMATCH)
                {
                    typesBufferPtr.ReAlloc(typesBufferReqLength);
                    status = NtDll.NtQueryObject(SafeBaseHandle.NullHandle, OBJECT_INFORMATION_CLASS.ObjectTypesInformation, typesBufferPtr, out typesBufferReqLength);
                }

                // Read the number of types from the buffer and store the built-out dictionary.
                var typesCount = typesBufferPtr.ToStructure<NtDll.OBJECT_TYPES_INFORMATION>().NumberOfTypes;
                var typeTable = new Dictionary<ushort, string>((int)typesCount);
                var ptrOffset = LibraryUtilities.AlignUp(objectTypesSize);
                for (uint i = 0; i < typesCount; i++)
                {
                    // Marshal the data into our structure and add the necessary values to the dictionary.
                    var typeInfo = typesBufferPtr.ToStructure<NtDll.OBJECT_TYPE_INFORMATION>(ptrOffset);
                    typeTable.Add(typeInfo.TypeIndex, typeInfo.TypeName.Buffer.ToString().TrimRemoveNull());
                    ptrOffset += objectTypeSize + LibraryUtilities.AlignUp(typeInfo.TypeName.MaximumLength);
                }
                ObjectTypeLookupTable = new(typeTable);
            }
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
            using (var handleBufferPtr = SafeHGlobalHandle.Alloc(handleInfoExSize + handleEntryExSize))
            {
                // Reallocate the buffer until we get the required size.
                var status = NtDll.NtQuerySystemInformation(SYSTEM_INFORMATION_CLASS.SystemExtendedHandleInformation, handleBufferPtr, out int handleBufferReqLength);
                while (status == NTSTATUS.STATUS_INFO_LENGTH_MISMATCH)
                {
                    handleBufferPtr.ReAlloc(handleBufferReqLength);
                    status = NtDll.NtQuerySystemInformation(SYSTEM_INFORMATION_CLASS.SystemExtendedHandleInformation, handleBufferPtr, out handleBufferReqLength);
                }

                // Set up required handles for GetObjectName().
                using (var currentProcessHandle = Kernel32.GetCurrentProcess())
                {
                    // Loop through all handles and return list of open file handles.
                    var ntPathLookupTable = FileSystemUtilities.GetNtPathLookupTable();
                    var handleCount = handleBufferPtr.ToStructure<NtDll.SYSTEM_HANDLE_INFORMATION_EX>().NumberOfHandles.ToUInt32();
                    var entryOffset = handleInfoExSize;
                    ConcurrentBag<FileHandleInfo> openHandles = [];
                    Parallel.For(0, (int)handleCount, i =>
                    {
                        // Read the handle information into a structure, skipping over if it's not a file or directory handle.
                        var sysHandle = handleBufferPtr.ToStructure<NtDll.SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX>(entryOffset + (handleEntryExSize * i));
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

                        // Get the handle's name to check if it's a hard drive path.
                        string? objectName;
                        using (fileDupHandle)
                        {
                            objectName = GetObjectName(currentProcessHandle, fileDupHandle);
                        }

                        // Skip to next iteration if the handle doesn't meet our criteria
                        if (string.IsNullOrWhiteSpace(objectName) || !objectName!.StartsWith(@"\Device\HarddiskVolume"))
                        {
                            return;
                        }

                        // Add the handle information to the list if it matches the specified directory path.
                        string objectNameKey = $@"\{string.Join(@"\", objectName.Split(['\\'], StringSplitOptions.RemoveEmptyEntries).Take(2))}";
                        if (ntPathLookupTable.TryGetValue(objectNameKey, out string? driveLetter) && objectName.Replace(objectNameKey, driveLetter) is string dosPath && (null == directoryPath || dosPath.StartsWith(directoryPath, StringComparison.OrdinalIgnoreCase)))
                        {
                            openHandles.Add(new FileHandleInfo(sysHandle, dosPath, objectName, objectType));
                        }
                    });
                    return openHandles.ToList().AsReadOnly();
                }
            }
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
            if (null == handleEntries)
            {
                throw new ArgumentNullException(nameof(handleEntries));
            }

            // Open each process handle, duplicate it with close source flag, then close the duplicated handle to close the original handle.
            using (var currentProcessHandle = Kernel32.GetCurrentProcess())
            {
                foreach (var handleEntry in handleEntries)
                {
                    using var fileProcessHandle = Kernel32.OpenProcess(PROCESS_ACCESS_RIGHTS.PROCESS_DUP_HANDLE, false, handleEntry.UniqueProcessId.ToUInt32());
                    using SafeFileHandle fileOpenHandle = new((HANDLE)handleEntry.HandleValue, false);
                    Kernel32.DuplicateHandle(fileProcessHandle, fileOpenHandle, currentProcessHandle, out var localHandle, 0, false, DUPLICATE_HANDLE_OPTIONS.DUPLICATE_CLOSE_SOURCE);
                    localHandle.Dispose();
                    localHandle = null;
                }
            }
        }

        /// <summary>
        /// Retrieves the name of an object associated with a handle.
        /// </summary>
        /// <param name="currentProcessHandle"></param>
        /// <param name="fileHandle"></param>
        /// <returns></returns>
        private static string? GetObjectName(SafeProcessHandle currentProcessHandle, SafeFileHandle fileHandle)
        {
            if (fileHandle is null || fileHandle.IsClosed || fileHandle.IsInvalid)
            {
                throw new ArgumentNullException(nameof(fileHandle));
            }

            using var objectBuffer = SafeHGlobalHandle.Alloc(1024);
            bool objectBufferAddRef = false;
            bool fileHandleAddRef = false;
            try
            {
                // Start the thread to retrieve the object name and wait for the outcome.
                fileHandle.DangerousAddRef(ref fileHandleAddRef); objectBuffer.DangerousAddRef(ref objectBufferAddRef);
                using var shellcode = GetObjectTypeShellcode(NtQueryObjectProcAddr, fileHandle.DangerousGetHandle(), OBJECT_INFORMATION_CLASS.ObjectNameInformation, objectBuffer.DangerousGetHandle(), objectBuffer.Length, ExitThreadProcAddr);
                NtDll.NtCreateThreadEx(out var hThread, THREAD_ACCESS_RIGHTS.THREAD_ALL_ACCESS, IntPtr.Zero, currentProcessHandle, shellcode, IntPtr.Zero, 0, 0, 0, 0, IntPtr.Zero);
                using (hThread)
                {
                    // Terminate the thread if it's taking longer than our timeout (NtQueryObject() has hung).
                    if (PInvoke.WaitForSingleObject(hThread, GetObjectNameThreadTimeout) == WAIT_EVENT.WAIT_TIMEOUT)
                    {
                        NtDll.NtTerminateThread(hThread, NTSTATUS.STATUS_TIMEOUT);
                    }

                    // Get the exit code of the thread and throw an exception if it failed.
                    Kernel32.GetExitCodeThread(hThread, out var exitCode);
                    try
                    {
                        if ((NTSTATUS)ValueTypeConverter<int>.Convert(exitCode) is NTSTATUS res && res != NTSTATUS.STATUS_SUCCESS)
                        {
                            throw ExceptionUtilities.GetExceptionForLastWin32Error((WIN32_ERROR)PInvoke.RtlNtStatusToDosError(res));
                        }
                    }
                    catch (Win32Exception ex) when ((ex.NativeErrorCode == (int)WIN32_ERROR.ERROR_NOT_SUPPORTED) || (ex.NativeErrorCode == (int)WIN32_ERROR.ERROR_BAD_PATHNAME) || (ex.NativeErrorCode == (int)WIN32_ERROR.ERROR_TIMEOUT) || (ex.NativeErrorCode == (int)WIN32_ERROR.ERROR_IO_PENDING))
                    {
                        return null;
                    }
                    catch (UnauthorizedAccessException ex) when (ex.HResult == HRESULT.E_ACCESSDENIED)
                    {
                        return null;
                    }
                    return objectBuffer.ToStructure<OBJECT_NAME_INFORMATION>().Name.Buffer.ToString()?.TrimRemoveNull();
                }
            }
            finally
            {
                if (fileHandleAddRef)
                {
                    fileHandle.DangerousRelease();
                }
                if (objectBufferAddRef)
                {
                    objectBuffer.DangerousRelease();
                }
            }
        }

        /// <summary>
        /// The context for the thread that retrieves the object name.
        /// </summary>
        /// <param name="exitThread"></param>
        /// <param name="ntQueryObject"></param>
        /// <param name="fileHandle"></param>
        /// <param name="infoClass"></param>
        /// <param name="infoBuffer"></param>
        /// <returns></returns>
        /// <exception cref="PlatformNotSupportedException"></exception>
        private static SafeVirtualAllocHandle GetObjectTypeShellcode(FARPROC ntQueryObject, IntPtr fileHandle, OBJECT_INFORMATION_CLASS infoClass, IntPtr infoBuffer, int infoBufferLength, FARPROC exitThread)
        {
            // Build the shellcode stub to call NtQueryObject.
            List<byte> shellcode = [];
            switch (RuntimeInformation.ProcessArchitecture)
            {
                case Architecture.X64:
                    // mov rcx, handle
                    shellcode.Add(0x48); shellcode.Add(0xB9);
                    shellcode.AddRange(BitConverter.GetBytes((ulong)fileHandle));

                    // mov rdx, infoClass
                    shellcode.Add(0x48); shellcode.Add(0xBA);
                    shellcode.AddRange(BitConverter.GetBytes((ulong)(uint)infoClass));

                    // mov r8, buffer
                    shellcode.Add(0x49); shellcode.Add(0xB8);
                    shellcode.AddRange(BitConverter.GetBytes((ulong)infoBuffer));

                    // mov r9, bufferSize
                    shellcode.Add(0x49); shellcode.Add(0xB9);
                    shellcode.AddRange(BitConverter.GetBytes((ulong)infoBufferLength));

                    // sub rsp, 0x28 — shadow space + ReturnLength
                    shellcode.Add(0x48); shellcode.Add(0x83); shellcode.Add(0xEC); shellcode.Add(0x28);

                    // mov qword [rsp + 0x20], 0  (null for PULONG ReturnLength)
                    shellcode.Add(0x48); shellcode.Add(0xC7); shellcode.Add(0x44); shellcode.Add(0x24); shellcode.Add(0x20);
                    shellcode.AddRange(new byte[4]); // 0

                    // mov rax, NtQueryObject
                    shellcode.Add(0x48); shellcode.Add(0xB8);
                    shellcode.AddRange(BitConverter.GetBytes((ulong)ntQueryObject.Value));

                    // call rax
                    shellcode.Add(0xFF); shellcode.Add(0xD0);

                    // mov ecx, eax (exit code)
                    shellcode.Add(0x89); shellcode.Add(0xC1);

                    // mov rax, ExitThread
                    shellcode.Add(0x48); shellcode.Add(0xB8);
                    shellcode.AddRange(BitConverter.GetBytes((ulong)exitThread.Value));

                    // call rax
                    shellcode.Add(0xFF); shellcode.Add(0xD0);
                    break;
                case Architecture.X86:
                    // push NULL (ReturnLength)
                    shellcode.Add(0x6A);
                    shellcode.Add(0x00);

                    // push bufferSize
                    shellcode.Add(0x68);
                    shellcode.AddRange(BitConverter.GetBytes(infoBufferLength));

                    // push buffer
                    shellcode.Add(0x68);
                    shellcode.AddRange(BitConverter.GetBytes(infoBuffer.ToInt32()));

                    // push infoClass
                    shellcode.Add(0x68);
                    shellcode.AddRange(BitConverter.GetBytes((int)infoClass));

                    // push handle
                    shellcode.Add(0x68);
                    shellcode.AddRange(BitConverter.GetBytes(fileHandle.ToInt32()));

                    // mov eax, NtQueryObject
                    shellcode.Add(0xB8);
                    shellcode.AddRange(BitConverter.GetBytes(ntQueryObject.Value.ToInt32()));

                    // call eax
                    shellcode.Add(0xFF); shellcode.Add(0xD0);

                    // push eax (NTSTATUS)
                    shellcode.Add(0x50);

                    // mov eax, ExitThread
                    shellcode.Add(0xB8);
                    shellcode.AddRange(BitConverter.GetBytes(exitThread.Value.ToInt32()));

                    // call eax
                    shellcode.Add(0xFF); shellcode.Add(0xD0);
                    break;
                case Architecture.Arm64:
                    // x0 = handle
                    List<uint> code = [];
                    code.AddRange(NativeUtilities.Load64(0, (ulong)fileHandle.ToInt64()));

                    // x1 = infoClass (zero-extended)
                    code.AddRange(NativeUtilities.Load64(1, (ulong)infoClass));

                    // x2 = buffer
                    code.AddRange(NativeUtilities.Load64(2, (ulong)infoBuffer.ToInt64()));

                    // x3 = bufferSize
                    code.AddRange(NativeUtilities.Load64(3, (ulong)infoBufferLength));

                    // x4 = NULL (for ReturnLength)
                    code.AddRange(NativeUtilities.Load64(4, 0));

                    // x16 = NtQueryObject
                    code.AddRange(NativeUtilities.Load64(16, (ulong)ntQueryObject.Value.ToInt64()));

                    // br x16
                    code.Add(NativeUtilities.EncodeBr(16));

                    // x16 = ExitThread. result is in x0 → already correct for ExitThread
                    code.AddRange(NativeUtilities.Load64(16, (ulong)exitThread.Value.ToInt64()));

                    // br x16
                    code.Add(NativeUtilities.EncodeBr(16));

                    // Convert instruction list to byte array
                    foreach (var instr in code)
                    {
                        shellcode.AddRange(BitConverter.GetBytes(instr));
                    }
                    break;
                default:
                    throw new PlatformNotSupportedException("Unsupported architecture: " + RuntimeInformation.ProcessArchitecture);
            }
            var mem = SafeVirtualAllocHandle.Alloc(shellcode.Count, VIRTUAL_ALLOCATION_TYPE.MEM_COMMIT | VIRTUAL_ALLOCATION_TYPE.MEM_RESERVE, PAGE_PROTECTION_FLAGS.PAGE_EXECUTE_READWRITE);
            mem.Write(shellcode.ToArray());
            return mem;
        }

        /// <summary>
        /// The lookup table of object types.
        /// </summary>
        private static readonly ReadOnlyDictionary<ushort, string> ObjectTypeLookupTable;

        /// <summary>
        /// Represents the function pointer for the NtQueryObject native API method.
        /// </summary>
        /// <remarks>This field holds the address of the NtQueryObject function, which is resolved at runtime. It is intended for internal use only and should not be accessed directly by external code.</remarks>
        private static readonly FARPROC NtQueryObjectProcAddr;

        /// <summary>
        /// Represents the address of the ExitThread procedure in the native library.
        /// </summary>
        /// <remarks>This field holds a function pointer to the ExitThread procedure, which is typically used in low-level interop scenarios. It is initialized to the appropriate address during runtime and should not be modified directly.</remarks>
        private static readonly FARPROC ExitThreadProcAddr;

        /// <summary>
        /// The duration to wait for a hung NtQueryObject thread to terminate.
        /// </summary>
        private const uint GetObjectNameThreadTimeout = 125;
    }
}
