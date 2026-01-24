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
using PSADT.Extensions;
using PSADT.LibraryInterfaces;
using PSADT.LibraryInterfaces.SafeHandles;
using PSADT.LibraryInterfaces.Utilities;
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
            // Internal helper to get the required buffer size for object types information.
            static int GetObjectTypesBufferSize(int queryBufferSize)
            {
                Span<byte> queryBuffer = stackalloc byte[queryBufferSize];
                _ = NtDll.NtQueryObject(null, LibraryInterfaces.OBJECT_INFORMATION_CLASS.ObjectTypesInformation, queryBuffer, out uint requiredLength);
                return (int)requiredLength;
            }

            // Build the StartRoutine template once during static initialization.
            using (FreeLibrarySafeHandle hKernel32Ptr = Kernel32.LoadLibrary("kernel32.dll"))
            using (FreeLibrarySafeHandle hNtdllPtr = Kernel32.LoadLibrary("ntdll.dll"))
            {
                // Build the start routine stub to call NtQueryObject and exit the thread once done.
                FARPROC ntQueryObject = Kernel32.GetProcAddress(hNtdllPtr, "NtQueryObject");
                FARPROC exitThread = Kernel32.GetProcAddress(hKernel32Ptr, "ExitThread");
                int handleOffset, bufferOffset, bufferLengthOffset; List<byte> startRoutine = [];
                Architecture processArchitecture = RuntimeInformation.ProcessArchitecture;
                if (processArchitecture == Architecture.X64)
                {
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
                    NtQueryObjectStartRoutineTemplate = new([.. startRoutine], handleOffset, bufferOffset, bufferLengthOffset);
                }
                else if (processArchitecture == Architecture.X86)
                {
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
                    NtQueryObjectStartRoutineTemplate = new([.. startRoutine], handleOffset, bufferOffset, bufferLengthOffset);
                }
                else if (processArchitecture == Architecture.Arm64)
                {
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
                    foreach (uint instr in code)
                    {
                        startRoutine.AddRange(BitConverter.GetBytes(instr));
                    }
                    NtQueryObjectStartRoutineTemplate = new([.. startRoutine], handleOffset, bufferOffset, bufferLengthOffset);
                }
                else
                {
                    HandleUnsupportedArchitecture();
                }
            }

            // Allocate an appropriately sized buffer and query the system for object types information.
            int objectTypesSize = NtDll.ObjectInfoClassSizes[LibraryInterfaces.OBJECT_INFORMATION_CLASS.ObjectTypesInformation];
            int objectTypeSize = NtDll.ObjectInfoClassSizes[LibraryInterfaces.OBJECT_INFORMATION_CLASS.ObjectTypeInformation];
            Span<byte> typesBufferPtr = stackalloc byte[GetObjectTypesBufferSize(objectTypesSize)];
            _ = NtDll.NtQueryObject(null, LibraryInterfaces.OBJECT_INFORMATION_CLASS.ObjectTypesInformation, typesBufferPtr, out _);

            // Read the number of types from the buffer and store the built-out dictionary.
            ref readonly OBJECT_TYPES_INFORMATION typesInfo = ref typesBufferPtr.AsReadOnlyStructure<OBJECT_TYPES_INFORMATION>();
            uint typesCount = typesInfo.NumberOfTypes; Dictionary<ushort, string> typeTable = new((int)typesCount);
            int ptrOffset = LibraryUtilities.AlignUp(objectTypesSize);
            for (uint i = 0; i < typesCount; i++)
            {
                // Marshal the data into our structure and add the necessary values to the dictionary.
                ref readonly OBJECT_TYPE_INFORMATION typeInfo = ref typesBufferPtr.Slice(ptrOffset).AsReadOnlyStructure<OBJECT_TYPE_INFORMATION>();
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
            // Internal helper to get the required buffer size for extended handle information.
            static int GetExtendedHandleBufferSize(int queryBufferSize)
            {
                Span<byte> queryBuffer = stackalloc byte[queryBufferSize];
                _ = NtDll.NtQuerySystemInformation(SYSTEM_INFORMATION_CLASS.SystemExtendedHandleInformation, queryBuffer, out uint requiredLength);
                return (int)requiredLength;
            }

            // Allocate an appropriately sized buffer and query the system for extended handle information. We increase
            // the size slightly to account for new handles being created between the size check and the actual query.
            int handleInfoExSize = Marshal.SizeOf<SYSTEM_HANDLE_INFORMATION_EX>();
            int handleEntryExSize = Marshal.SizeOf<SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX>();
            byte[] handleBuffer = new byte[GetExtendedHandleBufferSize(handleInfoExSize + handleEntryExSize) * 5 / 4];
            _ = NtDll.NtQuerySystemInformation(SYSTEM_INFORMATION_CLASS.SystemExtendedHandleInformation, handleBuffer, out _);

            // Use thread-local storage for both the object buffer and the reusable StartRoutine buffer.
            using ThreadLocal<(SafePinnedGCHandle ObjectBuffer, SafeVirtualAllocHandle StartRoutineBuffer)> threadBuffers = new
            (
                () => (SafePinnedGCHandle.Alloc(new byte[1024]), SafeVirtualAllocHandle.Alloc(NtQueryObjectStartRoutineTemplate.Bytes.Length, VIRTUAL_ALLOCATION_TYPE.MEM_COMMIT | VIRTUAL_ALLOCATION_TYPE.MEM_RESERVE, PAGE_PROTECTION_FLAGS.PAGE_EXECUTE_READWRITE).Write(NtQueryObjectStartRoutineTemplate.Bytes)),
                trackAllValues: true
            );

            // Loop through all handles and return list of open file handles.
            try
            {
                ref readonly SYSTEM_HANDLE_INFORMATION_EX handleInfo = ref handleBuffer.AsSpan().AsReadOnlyStructure<SYSTEM_HANDLE_INFORMATION_EX>();
                ReadOnlyDictionary<string, string> ntPathLookupTable = FileSystemUtilities.GetNtPathLookupTable();
                using SafeProcessHandle currentProcessHandle = Kernel32.GetCurrentProcess();
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
                        fileProcessHandle = Kernel32.OpenProcess(PROCESS_ACCESS_RIGHTS.PROCESS_DUP_HANDLE, false, sysHandle.UniqueProcessId.ToUInt32());
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
                            _ = Kernel32.DuplicateHandle(fileProcessHandle, fileOpenHandle, currentProcessHandle, out fileDupHandle, 0, false, DUPLICATE_HANDLE_OPTIONS.DUPLICATE_SAME_ACCESS);
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
                    catch (Win32Exception ex) when (ex.NativeErrorCode is ((int)WIN32_ERROR.ERROR_INVALID_HANDLE) or ((int)WIN32_ERROR.ERROR_INVALID_FUNCTION))
                    {
                        return;
                    }

                    // Get the handle's name to check if it's a hard drive path.
                    string? objectName;
                    using (fileDupHandle)
                    {
                        (SafePinnedGCHandle objectBuffer, SafeVirtualAllocHandle startRoutineBuffer) = threadBuffers.Value;
                        bool fileDupHandleAddRef = false; bool objectBufferAddRef = false;
                        try
                        {
                            // Start the thread to retrieve the object name and wait for the outcome.
                            fileDupHandle.DangerousAddRef(ref fileDupHandleAddRef); objectBuffer.DangerousAddRef(ref objectBufferAddRef);
                            PatchStartRoutineBuffer(startRoutineBuffer, fileDupHandle.DangerousGetHandle(), objectBuffer.DangerousGetHandle(), objectBuffer.Length);
                            _ = NtDll.NtCreateThreadEx(out SafeThreadHandle hThread, THREAD_ACCESS_RIGHTS.THREAD_ALL_ACCESS, currentProcessHandle, startRoutineBuffer);
                            NTSTATUS res;
                            using (hThread)
                            {
                                // Terminate the thread if it's taking longer than our timeout (NtQueryObject() has hung); otherwise just get the exit code.
                                if (Kernel32.WaitForSingleObject(hThread, 500) != WAIT_EVENT.WAIT_OBJECT_0)
                                {
                                    _ = NtDll.NtTerminateThread(hThread, NTSTATUS.STATUS_TIMEOUT);
                                }
                                _ = Kernel32.GetExitCodeThread(hThread, out uint exitCode); res = unchecked((NTSTATUS)exitCode);
                            }

                            // Handle the result of the NtQueryObject call; returning early on certain expected failure codes.
                            if (res == NTSTATUS.STATUS_TIMEOUT || res == NTSTATUS.STATUS_PENDING || res == NTSTATUS.STATUS_NOT_SUPPORTED || res == NTSTATUS.STATUS_OBJECT_PATH_INVALID || res == NTSTATUS.STATUS_ACCESS_DENIED || res == NTSTATUS.STATUS_PIPE_DISCONNECTED)
                            {
                                return;
                            }
                            if (res != NTSTATUS.STATUS_SUCCESS)
                            {
                                throw ExceptionUtilities.GetExceptionForLastWin32Error((WIN32_ERROR)PInvoke.RtlNtStatusToDosError(res));
                            }
                            ref readonly OBJECT_NAME_INFORMATION objectBufferData = ref objectBuffer.AsReadOnlyStructure<OBJECT_NAME_INFORMATION>();
                            objectName = objectBufferData.Name.Buffer.ToString()?.TrimRemoveNull();
                        }
                        finally
                        {
                            if (fileDupHandleAddRef)
                            {
                                fileDupHandle.DangerousRelease();
                            }
                            if (objectBufferAddRef)
                            {
                                objectBuffer.DangerousRelease();
                            }
                            objectBuffer.Clear();
                        }
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
                return new ReadOnlyCollection<FileHandleInfo>([.. openHandles]);
            }
            finally
            {
                foreach ((SafePinnedGCHandle objectBuffer, SafeVirtualAllocHandle startRoutineBuffer) in threadBuffers.Values)
                {
                    startRoutineBuffer.Dispose();
                    objectBuffer.Dispose();
                }
            }
        }

        /// <summary>
        /// Closes the specified handles.
        /// </summary>
        /// <param name="handleEntries"></param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "No idea, but the compiler just doesn't understand that this is OK.")]
        public static void CloseHandles(SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX[] handleEntries)
        {
            // Confirm the provided input isn't null.
            if (handleEntries is null)
            {
                throw new ArgumentNullException(nameof(handleEntries));
            }

            // Open each process handle, duplicate it with close source flag, then close the duplicated handle to close the original handle.
            using SafeProcessHandle currentProcessHandle = Kernel32.GetCurrentProcess();
            foreach (SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX handleEntry in handleEntries)
            {
                using SafeFileHandle fileProcessHandle = Kernel32.OpenProcess(PROCESS_ACCESS_RIGHTS.PROCESS_DUP_HANDLE, false, handleEntry.UniqueProcessId.ToUInt32());
                using SafeFileHandle fileOpenHandle = new((HANDLE)handleEntry.HandleValue, false);
                _ = Kernel32.DuplicateHandle(fileProcessHandle, fileOpenHandle, currentProcessHandle, out SafeFileHandle localHandle, 0, false, DUPLICATE_HANDLE_OPTIONS.DUPLICATE_CLOSE_SOURCE);
                localHandle.Dispose();
            }
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
            // Patch the handle and buffer pointers into the StartRoutine template.
            Architecture processArchitecture = RuntimeInformation.ProcessArchitecture;
            if (processArchitecture == Architecture.X64)
            {
                // Patch handle at offset (mov rcx, handle -> 2 bytes opcode + 8 bytes value)
                // Patch buffer at offset (mov r8, buffer -> 3 bytes opcode + 8 bytes value)
                // Patch buffer length at offset (mov r9, bufferSize -> 3 bytes opcode + 8 bytes value)
                _ = startRoutineBuffer.WriteInt64(fileHandle.ToInt64(), NtQueryObjectStartRoutineTemplate.HandleOffset);
                _ = startRoutineBuffer.WriteInt64(infoBuffer.ToInt64(), NtQueryObjectStartRoutineTemplate.BufferOffset);
                _ = startRoutineBuffer.WriteInt64(infoBufferLength, NtQueryObjectStartRoutineTemplate.BufferLengthOffset);
            }
            else if (processArchitecture == Architecture.X86)
            {
                // Patch buffer length (push bufferSize)
                // Patch buffer (push buffer)
                // Patch handle (push handle)
                _ = startRoutineBuffer.WriteInt32(infoBufferLength, NtQueryObjectStartRoutineTemplate.BufferLengthOffset);
                _ = startRoutineBuffer.WriteInt32(infoBuffer.ToInt32(), NtQueryObjectStartRoutineTemplate.BufferOffset);
                _ = startRoutineBuffer.WriteInt32(fileHandle.ToInt32(), NtQueryObjectStartRoutineTemplate.HandleOffset);
            }
            else if (processArchitecture == Architecture.Arm64)
            {
                // For ARM64, we need to regenerate the MOVZ/MOVK sequences for each 64-bit value. Each instruction is 4 bytes, patch in place.
                // Handle is at x0 (instructions 0-3), buffer is at x2 (instructions 8-11), length is at x3 (instructions 12-15).
                uint[] handleInstrs = [.. NativeUtilities.Load64(0, (ulong)fileHandle.ToInt64())];
                uint[] bufferInstrs = [.. NativeUtilities.Load64(2, (ulong)infoBuffer.ToInt64())];
                uint[] lengthInstrs = [.. NativeUtilities.Load64(3, (ulong)infoBufferLength)];
                for (int j = 0; j < 4; j++)
                {
                    _ = startRoutineBuffer.WriteInt32(unchecked((int)handleInstrs[j]), NtQueryObjectStartRoutineTemplate.HandleOffset + (j * 4));
                    _ = startRoutineBuffer.WriteInt32(unchecked((int)bufferInstrs[j]), NtQueryObjectStartRoutineTemplate.BufferOffset + (j * 4));
                    _ = startRoutineBuffer.WriteInt32(unchecked((int)lengthInstrs[j]), NtQueryObjectStartRoutineTemplate.BufferLengthOffset + (j * 4));
                }
            }
            else
            {
                HandleUnsupportedArchitecture();
            }
        }

        /// <summary>
        /// Throws a PlatformNotSupportedException to indicate that the current processor architecture is not supported.
        /// </summary>
        /// <exception cref="PlatformNotSupportedException">Thrown when the method is called on an unsupported processor architecture.</exception>
        private static void HandleUnsupportedArchitecture()
        {
            throw new PlatformNotSupportedException("Unsupported architecture: " + RuntimeInformation.ProcessArchitecture);
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
