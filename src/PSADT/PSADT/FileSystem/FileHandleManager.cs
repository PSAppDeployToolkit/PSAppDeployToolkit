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
using PSADT.SafeHandles;
using PSADT.Utilities;
using Windows.Wdk.Foundation;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Storage.FileSystem;
using Windows.Win32.System.Memory;
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1810:Initialize reference type static fields inline", Justification = "The static constructor is required here.")]
        static FileHandleManager()
        {
            // Build the StartRoutine template once during static initialization.
            using var hNtdllPtr = Kernel32.LoadLibrary("ntdll.dll"); using var hKernel32Ptr = Kernel32.LoadLibrary("kernel32.dll");
            NtQueryObjectStartRoutineTemplate = BuildNtQueryObjectStartRoutineTemplate(Kernel32.GetProcAddress(hNtdllPtr, "NtQueryObject"), Kernel32.GetProcAddress(hKernel32Ptr, "ExitThread"));

            // Query the system for the required buffer size for object types information.
            var objectTypesSize = NtDll.ObjectInfoClassSizes[LibraryInterfaces.OBJECT_INFORMATION_CLASS.ObjectTypesInformation];
            var objectTypeSize = NtDll.ObjectInfoClassSizes[LibraryInterfaces.OBJECT_INFORMATION_CLASS.ObjectTypeInformation];
            var typesBuffer = new byte[objectTypesSize]; Span<byte> typesBufferPtr = typesBuffer;

            // Reallocate the buffer until we get the required size.
            var status = NtDll.NtQueryObject(null, LibraryInterfaces.OBJECT_INFORMATION_CLASS.ObjectTypesInformation, typesBufferPtr, out uint typesBufferReqLength);
            while (status == NTSTATUS.STATUS_INFO_LENGTH_MISMATCH)
            {
                typesBuffer = new byte[typesBufferReqLength]; typesBufferPtr = typesBuffer;
                status = NtDll.NtQueryObject(null, LibraryInterfaces.OBJECT_INFORMATION_CLASS.ObjectTypesInformation, typesBufferPtr, out typesBufferReqLength);
            }

            // Read the number of types from the buffer and store the built-out dictionary.
            ref var typesInfo = ref Unsafe.As<byte, OBJECT_TYPES_INFORMATION>(ref MemoryMarshal.GetReference(typesBufferPtr));
            var typesCount = typesInfo.NumberOfTypes; var typeTable = new Dictionary<ushort, string>((int)typesCount);
            var ptrOffset = LibraryUtilities.AlignUp(objectTypesSize);
            for (uint i = 0; i < typesCount; i++)
            {
                // Marshal the data into our structure and add the necessary values to the dictionary.
                ref var typeInfo = ref Unsafe.As<byte, OBJECT_TYPE_INFORMATION>(ref MemoryMarshal.GetReference(typesBufferPtr.Slice(ptrOffset)));
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
            var handleEntryExSize = Marshal.SizeOf<SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX>();
            var handleInfoExSize = Marshal.SizeOf<SYSTEM_HANDLE_INFORMATION_EX>();
            var handleBuffer = new byte[handleInfoExSize + handleEntryExSize];
            Span<byte> handleBufferPtr = handleBuffer;

            // Reallocate the buffer until we get the required size.
            var status = NtDll.NtQuerySystemInformation(SYSTEM_INFORMATION_CLASS.SystemExtendedHandleInformation, handleBufferPtr, out uint handleBufferReqLength);
            while (status == NTSTATUS.STATUS_INFO_LENGTH_MISMATCH)
            {
                handleBuffer = new byte[(int)handleBufferReqLength]; handleBufferPtr = handleBuffer;
                status = NtDll.NtQuerySystemInformation(SYSTEM_INFORMATION_CLASS.SystemExtendedHandleInformation, handleBufferPtr, out handleBufferReqLength);
            }

            // Use thread-local storage for both the object buffer and the reusable StartRoutine buffer.
            using var threadBuffers = new ThreadLocal<(SafePinnedGCHandle ObjectBuffer, SafeVirtualAllocHandle StartRoutineBuffer)>
            (
                () => (AllocateObjectBuffer(), AllocateStartRoutineBuffer()),
                trackAllValues: true
            );

            // Loop through all handles and return list of open file handles.
            try
            {
                using var currentProcessHandle = Kernel32.GetCurrentProcess(); var ntPathLookupTable = FileSystemUtilities.GetNtPathLookupTable();
                ref var handleInfo = ref Unsafe.As<byte, SYSTEM_HANDLE_INFORMATION_EX>(ref MemoryMarshal.GetReference(handleBufferPtr));
                var openHandles = new ConcurrentBag<FileHandleInfo>();
                Parallel.For(0, (int)handleInfo.NumberOfHandles, i =>
                {
                    // Read the handle information into a structure, skipping over if it's not a file or directory handle.
                    ref var sysHandle = ref Unsafe.As<byte, SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX>(ref MemoryMarshal.GetReference(handleBuffer.AsSpan(handleInfoExSize + (handleEntryExSize * i))));
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
                        // Skip to the next iteration if the handle is invalid.
                        if (fileOpenHandle.IsInvalid)
                        {
                            return;
                        }

                        // Duplicate the handle into our process.
                        try
                        {
                            Kernel32.DuplicateHandle(fileProcessHandle, fileOpenHandle, currentProcessHandle, out fileDupHandle, 0, false, DUPLICATE_HANDLE_OPTIONS.DUPLICATE_SAME_ACCESS);
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

                    // Skip to the next iteration if the duplicated handle is invalid.
                    if (fileDupHandle.IsInvalid)
                    {
                        return;
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
                        objectName = GetObjectName(currentProcessHandle, fileDupHandle, threadBuffers.Value.ObjectBuffer, threadBuffers.Value.StartRoutineBuffer);
                    }

                    // Skip to next iteration if the handle doesn't meet our criteria.
                    if (!(objectName?.StartsWith(@"\Device\HarddiskVolume", StringComparison.Ordinal) == true))
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
            finally
            {
                foreach (var buffer in threadBuffers.Values)
                {
                    buffer.StartRoutineBuffer.Dispose();
                    buffer.ObjectBuffer.Dispose();
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
        public static void CloseHandles(SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX[] handleEntries)
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
        /// <param name="currentProcessHandle"></param>
        /// <param name="fileHandle"></param>
        /// <param name="objectBuffer"></param>
        /// <param name="startRoutineBuffer"></param>
        /// <returns></returns>
        private static string? GetObjectName(SafeProcessHandle currentProcessHandle, SafeFileHandle fileHandle, SafePinnedGCHandle objectBuffer, SafeVirtualAllocHandle startRoutineBuffer)
        {
            if (fileHandle is null || fileHandle.IsClosed || fileHandle.IsInvalid)
            {
                throw new ArgumentNullException(nameof(fileHandle));
            }
            bool objectBufferAddRef = false;
            bool fileHandleAddRef = false;
            try
            {
                // Start the thread to retrieve the object name and wait for the outcome.
                fileHandle.DangerousAddRef(ref fileHandleAddRef); objectBuffer.DangerousAddRef(ref objectBufferAddRef);
                PatchStartRoutineBuffer(startRoutineBuffer, fileHandle.DangerousGetHandle(), objectBuffer.DangerousGetHandle(), objectBuffer.Length);
                NtDll.NtCreateThreadEx(out var hThread, THREAD_ACCESS_RIGHTS.THREAD_ALL_ACCESS, currentProcessHandle, startRoutineBuffer);
                using (hThread)
                {
                    // Terminate the thread if it's taking longer than our timeout (NtQueryObject() has hung).
                    if (Kernel32.WaitForSingleObject(hThread, TimeSpan.FromSeconds(1)) != WAIT_EVENT.WAIT_OBJECT_0)
                    {
                        NtDll.NtTerminateThread(hThread, NTSTATUS.STATUS_TIMEOUT);
                    }

                    // Get the exit code of the thread, returning null under certain conditions or throwing an exception if it failed.
                    Kernel32.GetExitCodeThread(hThread, out var exitCode); var res = unchecked((NTSTATUS)exitCode);
                    if (res == NTSTATUS.STATUS_TIMEOUT || res == NTSTATUS.STATUS_PENDING || res == NTSTATUS.STATUS_NOT_SUPPORTED || res == NTSTATUS.STATUS_OBJECT_PATH_INVALID || res == NTSTATUS.STATUS_ACCESS_DENIED || res == NTSTATUS.STATUS_PIPE_DISCONNECTED)
                    {
                        return null;
                    }
                    if (res != NTSTATUS.STATUS_SUCCESS)
                    {
                        throw ExceptionUtilities.GetExceptionForLastWin32Error((WIN32_ERROR)PInvoke.RtlNtStatusToDosError(res));
                    }
                    ref var objectBufferData = ref Unsafe.As<byte, OBJECT_NAME_INFORMATION>(ref MemoryMarshal.GetReference(objectBuffer.AsReadOnlySpan<byte>()));
                    return objectBufferData.Name.Buffer.ToString()?.TrimRemoveNull();
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
                objectBuffer.Clear();
            }
        }

        /// <summary>
        /// Allocates a pinned buffer of 1,024 bytes and returns a handle that can be used to access the buffer safely.
        /// </summary>
        /// <remarks>The returned buffer is pinned in memory, preventing the garbage collector from
        /// relocating it. This is useful for scenarios that require fixed memory addresses, such as interoperability
        /// with unmanaged code.</remarks>
        /// <returns>A <see cref="SafePinnedGCHandle"/> representing a pinned buffer of 1,024 bytes. The handle must be disposed
        /// when no longer needed to release resources.</returns>
        private static SafePinnedGCHandle AllocateObjectBuffer()
        {
            return SafePinnedGCHandle.Alloc(new byte[1024], 1024);
        }

        /// <summary>
        /// Allocates a buffer in virtual memory containing the start routine template and returns a handle to the
        /// allocated memory.
        /// </summary>
        /// <remarks>The returned buffer is allocated with execute, read, and write permissions. The
        /// caller is responsible for disposing the handle when it is no longer needed to release the allocated
        /// memory.</remarks>
        /// <returns>A <see cref="SafeVirtualAllocHandle"/> representing the allocated virtual memory buffer containing the start
        /// routine template.</returns>
        private static SafeVirtualAllocHandle AllocateStartRoutineBuffer()
        {
            var mem = SafeVirtualAllocHandle.Alloc(NtQueryObjectStartRoutineTemplate.Bytes.Length, VIRTUAL_ALLOCATION_TYPE.MEM_COMMIT | VIRTUAL_ALLOCATION_TYPE.MEM_RESERVE, PAGE_PROTECTION_FLAGS.PAGE_EXECUTE_READWRITE);
            mem.Write(NtQueryObjectStartRoutineTemplate.Bytes);
            return mem;
        }

        /// <summary>
        /// Builds the StartRoutine template once during static initialization.
        /// Returns the bytes and the offsets where variable values need to be patched.
        /// </summary>
        private static (byte[] Bytes, int HandleOffset, int BufferOffset, int BufferLengthOffset) BuildNtQueryObjectStartRoutineTemplate(FARPROC ntQueryObject, FARPROC exitThread)
        {
            // Build the start routine stub to call NtQueryObject and exit the thread once done.
            int handleOffset, bufferOffset, bufferLengthOffset; List<byte> startRoutine = [];
            switch (RuntimeInformation.ProcessArchitecture)
            {
                case Architecture.X64:
                    // mov rcx, handle (placeholder)
                    startRoutine.Add(0x48); startRoutine.Add(0xB9);
                    handleOffset = startRoutine.Count; startRoutine.AddRange(new byte[8]); // placeholder for handle

                    // mov rdx, infoClass (ObjectNameInformation = 1)
                    startRoutine.Add(0x48); startRoutine.Add(0xBA);
                    startRoutine.AddRange(BitConverter.GetBytes((ulong)(uint)LibraryInterfaces.OBJECT_INFORMATION_CLASS.ObjectNameInformation));

                    // mov r8, buffer (placeholder)
                    startRoutine.Add(0x49); startRoutine.Add(0xB8);
                    bufferOffset = startRoutine.Count; startRoutine.AddRange(new byte[8]); // placeholder for buffer

                    // mov r9, bufferSize (placeholder)
                    startRoutine.Add(0x49); startRoutine.Add(0xB9);
                    bufferLengthOffset = startRoutine.Count; startRoutine.AddRange(new byte[8]); // placeholder for buffer length

                    // sub rsp, 0x28 — shadow space + ReturnLength
                    startRoutine.Add(0x48); startRoutine.Add(0x83); startRoutine.Add(0xEC); startRoutine.Add(0x28);

                    // mov qword [rsp + 0x20], 0  (null for PULONG ReturnLength)
                    startRoutine.Add(0x48); startRoutine.Add(0xC7); startRoutine.Add(0x44); startRoutine.Add(0x24); startRoutine.Add(0x20);
                    startRoutine.AddRange(new byte[4]); // 0

                    // mov rax, NtQueryObject
                    startRoutine.Add(0x48); startRoutine.Add(0xB8);
                    startRoutine.AddRange(BitConverter.GetBytes((ulong)ntQueryObject.Value));

                    // call rax
                    startRoutine.Add(0xFF); startRoutine.Add(0xD0);

                    // mov ecx, eax (exit code)
                    startRoutine.Add(0x89); startRoutine.Add(0xC1);

                    // mov rax, ExitThread
                    startRoutine.Add(0x48); startRoutine.Add(0xB8);
                    startRoutine.AddRange(BitConverter.GetBytes((ulong)exitThread.Value));

                    // call rax
                    startRoutine.Add(0xFF); startRoutine.Add(0xD0);
                    break;
                case Architecture.X86:
                    // push NULL (ReturnLength)
                    startRoutine.Add(0x6A);
                    startRoutine.Add(0x00);

                    // push bufferSize (placeholder)
                    startRoutine.Add(0x68);
                    bufferLengthOffset = startRoutine.Count; startRoutine.AddRange(new byte[4]); // placeholder

                    // push buffer (placeholder)
                    startRoutine.Add(0x68);
                    bufferOffset = startRoutine.Count; startRoutine.AddRange(new byte[4]); // placeholder

                    // push infoClass (ObjectNameInformation = 1)
                    startRoutine.Add(0x68);
                    startRoutine.AddRange(BitConverter.GetBytes((int)LibraryInterfaces.OBJECT_INFORMATION_CLASS.ObjectNameInformation));

                    // push handle (placeholder)
                    startRoutine.Add(0x68);
                    handleOffset = startRoutine.Count; startRoutine.AddRange(new byte[4]); // placeholder

                    // mov eax, NtQueryObject
                    startRoutine.Add(0xB8);
                    startRoutine.AddRange(BitConverter.GetBytes(ntQueryObject.Value.ToInt32()));

                    // call eax
                    startRoutine.Add(0xFF); startRoutine.Add(0xD0);

                    // push eax (NTSTATUS)
                    startRoutine.Add(0x50);

                    // mov eax, ExitThread
                    startRoutine.Add(0xB8);
                    startRoutine.AddRange(BitConverter.GetBytes(exitThread.Value.ToInt32()));

                    // call eax
                    startRoutine.Add(0xFF); startRoutine.Add(0xD0);
                    break;
                case Architecture.Arm64:
                    // x0 = handle (placeholder - 4 instructions)
                    List<uint> code = [];
                    handleOffset = code.Count * 4; code.AddRange(NativeUtilities.Load64(0, 0)); // placeholder

                    // x1 = infoClass (ObjectNameInformation = 1)
                    code.AddRange(NativeUtilities.Load64(1, (ulong)LibraryInterfaces.OBJECT_INFORMATION_CLASS.ObjectNameInformation));

                    // x2 = buffer (placeholder - 4 instructions)
                    bufferOffset = code.Count * 4; code.AddRange(NativeUtilities.Load64(2, 0)); // placeholder

                    // x3 = bufferSize (placeholder - 4 instructions)
                    bufferLengthOffset = code.Count * 4; code.AddRange(NativeUtilities.Load64(3, 0)); // placeholder

                    // x4 = NULL (for ReturnLength)
                    code.AddRange(NativeUtilities.Load64(4, 0));

                    // x16 = NtQueryObject
                    code.AddRange(NativeUtilities.Load64(16, (ulong)ntQueryObject.Value.ToInt64()));

                    // blr x16 (branch with link to x16)
                    code.Add(NativeUtilities.EncodeBlr(16));

                    // x16 = ExitThread (x0 already contains result, move to x0 for ExitThread (no-op, already there))
                    code.AddRange(NativeUtilities.Load64(16, (ulong)exitThread.Value.ToInt64()));

                    // br x16
                    code.Add(NativeUtilities.EncodeBr(16));

                    // Convert instruction list to byte array
                    foreach (var instr in code)
                    {
                        startRoutine.AddRange(BitConverter.GetBytes(instr));
                    }
                    break;
                default:
                    throw new PlatformNotSupportedException("Unsupported architecture: " + RuntimeInformation.ProcessArchitecture);
            }
            return new([.. startRoutine], handleOffset, bufferOffset, bufferLengthOffset);
        }

        /// <summary>
        /// Patches the StartRoutine buffer with the variable values (handle and buffer pointers).
        /// </summary>
        /// <param name="startRoutineBuffer">The pre-allocated startRoutine buffer.</param>
        /// <param name="fileHandle">The file handle to query.</param>
        /// <param name="infoBuffer">The buffer to receive the object name.</param>
        /// <param name="infoBufferLength">The length of the info buffer.</param>
        private static void PatchStartRoutineBuffer(SafeVirtualAllocHandle startRoutineBuffer, IntPtr fileHandle, IntPtr infoBuffer, int infoBufferLength)
        {
            switch (RuntimeInformation.ProcessArchitecture)
            {
                case Architecture.X64:
                    // Patch handle at offset (mov rcx, handle -> 2 bytes opcode + 8 bytes value)
                    // Patch buffer at offset (mov r8, buffer -> 3 bytes opcode + 8 bytes value)
                    // Patch buffer length at offset (mov r9, bufferSize -> 3 bytes opcode + 8 bytes value)
                    startRoutineBuffer.WriteInt64(fileHandle.ToInt64(), NtQueryObjectStartRoutineTemplate.HandleOffset);
                    startRoutineBuffer.WriteInt64(infoBuffer.ToInt64(), NtQueryObjectStartRoutineTemplate.BufferOffset);
                    startRoutineBuffer.WriteInt64(infoBufferLength, NtQueryObjectStartRoutineTemplate.BufferLengthOffset);
                    break;
                case Architecture.X86:
                    // Patch buffer length (push bufferSize)
                    // Patch buffer (push buffer)
                    // Patch handle (push handle)
                    startRoutineBuffer.WriteInt32(infoBufferLength, NtQueryObjectStartRoutineTemplate.BufferLengthOffset);
                    startRoutineBuffer.WriteInt32(infoBuffer.ToInt32(), NtQueryObjectStartRoutineTemplate.BufferOffset);
                    startRoutineBuffer.WriteInt32(fileHandle.ToInt32(), NtQueryObjectStartRoutineTemplate.HandleOffset);
                    break;
                case Architecture.Arm64:
                    // For ARM64, we need to regenerate the MOVZ/MOVK sequences for each 64-bit value. Each instruction is 4 bytes, patch in place.
                    // Handle is at x0 (instructions 0-3), buffer is at x2 (instructions 8-11), length is at x3 (instructions 12-15).
                    var handleInstrs = NativeUtilities.Load64(0, (ulong)fileHandle.ToInt64()).ToArray();
                    var bufferInstrs = NativeUtilities.Load64(2, (ulong)infoBuffer.ToInt64()).ToArray();
                    var lengthInstrs = NativeUtilities.Load64(3, (ulong)infoBufferLength).ToArray();
                    for (int j = 0; j < 4; j++)
                    {
                        startRoutineBuffer.WriteInt32(unchecked((int)handleInstrs[j]), NtQueryObjectStartRoutineTemplate.HandleOffset + (j * 4));
                        startRoutineBuffer.WriteInt32(unchecked((int)bufferInstrs[j]), NtQueryObjectStartRoutineTemplate.BufferOffset + (j * 4));
                        startRoutineBuffer.WriteInt32(unchecked((int)lengthInstrs[j]), NtQueryObjectStartRoutineTemplate.BufferLengthOffset + (j * 4));
                    }
                    break;
                default:
                    throw new PlatformNotSupportedException("Unsupported architecture: " + RuntimeInformation.ProcessArchitecture);
            }
        }

        /// <summary>
        /// The lookup table of object types.
        /// </summary>
        private static readonly ReadOnlyDictionary<ushort, string> ObjectTypeLookupTable;

        /// <summary>
        /// The pre-built StartRoutine template.
        /// </summary>
        private static readonly (byte[] Bytes, int HandleOffset, int BufferOffset, int BufferLengthOffset) NtQueryObjectStartRoutineTemplate;
    }
}
