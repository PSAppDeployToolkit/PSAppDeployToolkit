using System;
using System.Diagnostics;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using PSADT.Execution;
using PSADT.LibraryInterfaces;
using PSADT.Types;
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
            // Start the thread to retrieve the object name and wait for the outcome.
            var hNtdll = Kernel32.LoadLibrary("ntdll.dll");
            var hKrn32 = Kernel32.LoadLibrary("kernel32.dll");
            var buffer = Marshal.AllocHGlobal(GetObjectNameBufferSize);
            try
            {
                var shellcode = GetObjectTypeShellcode(Kernel32.GetProcAddress(hKrn32, "ExitThread"), Kernel32.GetProcAddress(hNtdll, "NtQueryObject"), OBJECT_INFORMATION_CLASS.ObjectNameInformation, handle, buffer, GetObjectNameBufferSize);
                try
                {
                    NTSTATUS status = NtDll.NtCreateThreadEx(out var hThread, THREAD_ACCESS_RIGHTS.THREAD_ALL_ACCESS, IntPtr.Zero, PInvoke.GetCurrentProcess(), shellcode, IntPtr.Zero, 0, 0, 0, 0, IntPtr.Zero);
                    try
                    {
                        // Terminate the thread if it's taking longer than 250 ms (NtQueryObject() has hung).
                        if (PInvoke.WaitForSingleObject(hThread, (uint)GetObjectNameThreadTimeout.Milliseconds) == WAIT_EVENT.WAIT_TIMEOUT)
                        {
                            NtDll.NtTerminateThread(hThread, NTSTATUS.STATUS_TIMEOUT);
                        }

                        // Get the exit code of the thread and throw an exception if it failed.
                        Kernel32.GetExitCodeThread(hThread, out var exitCode);
                        try
                        {
                            if ((NTSTATUS)ValueTypeConverter<int>.Convert(exitCode) is NTSTATUS res && res != NTSTATUS.STATUS_SUCCESS)
                            {
                                throw ExceptionUtilities.GetExceptionForLastWin32Error((WIN32_ERROR)Windows.Win32.PInvoke.RtlNtStatusToDosError(res));
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
                        return Marshal.PtrToStructure<OBJECT_NAME_INFORMATION>(buffer).Name.Buffer.ToString()?.Trim('\0').Trim();
                    }
                    finally
                    {
                        Kernel32.CloseHandle(ref hThread);
                    }
                }
                finally
                {
                    Kernel32.VirtualFree(shellcode, 0, VIRTUAL_FREE_TYPE.MEM_RELEASE);
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
                Kernel32.FreeLibrary(ref hKrn32);
                Kernel32.FreeLibrary(ref hNtdll);
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
        /// The context for the thread that retrieves the object name.
        /// </summary>
        /// <param name="ntQueryObject"></param>
        /// <param name="infoClass"></param>
        /// <param name="handle"></param>
        /// <param name="buffer"></param>
        /// <param name="bufferSize"></param>
        /// <returns></returns>
        /// <exception cref="PlatformNotSupportedException"></exception>
        private static IntPtr GetObjectTypeShellcode(IntPtr exitThread, IntPtr ntQueryObject, OBJECT_INFORMATION_CLASS infoClass, IntPtr handle, IntPtr buffer, int bufferSize)
        {
            // Build the shellcode stub to call NtQueryObject.
            var shellcode = new List<byte>();
            switch (ProcessArchitecture)
            {
                case SystemArchitecture.AMD64:
                    // mov rcx, handle
                    shellcode.Add(0x48); shellcode.Add(0xB9);
                    shellcode.AddRange(BitConverter.GetBytes((ulong)handle));

                    // mov rdx, infoClass
                    shellcode.Add(0x48); shellcode.Add(0xBA);
                    shellcode.AddRange(BitConverter.GetBytes((ulong)(uint)infoClass));

                    // mov r8, buffer
                    shellcode.Add(0x49); shellcode.Add(0xB8);
                    shellcode.AddRange(BitConverter.GetBytes((ulong)buffer));

                    // mov r9, bufferSize
                    shellcode.Add(0x49); shellcode.Add(0xB9);
                    shellcode.AddRange(BitConverter.GetBytes((ulong)bufferSize));

                    // sub rsp, 0x28 — shadow space + ReturnLength
                    shellcode.Add(0x48); shellcode.Add(0x83); shellcode.Add(0xEC); shellcode.Add(0x28);

                    // mov qword [rsp + 0x20], 0  (null for PULONG ReturnLength)
                    shellcode.Add(0x48); shellcode.Add(0xC7); shellcode.Add(0x44); shellcode.Add(0x24); shellcode.Add(0x20);
                    shellcode.AddRange(new byte[4]); // 0

                    // mov rax, NtQueryObject
                    shellcode.Add(0x48); shellcode.Add(0xB8);
                    shellcode.AddRange(BitConverter.GetBytes((ulong)ntQueryObject));

                    // call rax
                    shellcode.Add(0xFF); shellcode.Add(0xD0);

                    // mov ecx, eax (exit code)
                    shellcode.Add(0x89); shellcode.Add(0xC1);

                    // mov rax, ExitThread
                    shellcode.Add(0x48); shellcode.Add(0xB8);
                    shellcode.AddRange(BitConverter.GetBytes((ulong)exitThread));

                    // call rax
                    shellcode.Add(0xFF); shellcode.Add(0xD0);
                    break;
                case SystemArchitecture.i386:
                    // push NULL (ReturnLength)
                    shellcode.Add(0x6A);
                    shellcode.Add(0x00);

                    // push bufferSize
                    shellcode.Add(0x68);
                    shellcode.AddRange(BitConverter.GetBytes(bufferSize));

                    // push buffer
                    shellcode.Add(0x68);
                    shellcode.AddRange(BitConverter.GetBytes(buffer.ToInt32()));

                    // push infoClass
                    shellcode.Add(0x68);
                    shellcode.AddRange(BitConverter.GetBytes((int)infoClass));

                    // push handle
                    shellcode.Add(0x68);
                    shellcode.AddRange(BitConverter.GetBytes(handle.ToInt32()));

                    // mov eax, NtQueryObject
                    shellcode.Add(0xB8);
                    shellcode.AddRange(BitConverter.GetBytes(ntQueryObject.ToInt32()));

                    // call eax
                    shellcode.Add(0xFF); shellcode.Add(0xD0);

                    // push eax (NTSTATUS)
                    shellcode.Add(0x50);

                    // mov eax, ExitThread
                    shellcode.Add(0xB8);
                    shellcode.AddRange(BitConverter.GetBytes(exitThread.ToInt32()));

                    // call eax
                    shellcode.Add(0xFF); shellcode.Add(0xD0);
                    break;
                case SystemArchitecture.ARM64:
                    // x0 = handle
                    var code = new List<uint>();
                    code.AddRange(NativeUtilities.Load64(0, (ulong)handle.ToInt64()));

                    // x1 = infoClass (zero-extended)
                    code.AddRange(NativeUtilities.Load64(1, (ulong)(uint)infoClass));

                    // x2 = buffer
                    code.AddRange(NativeUtilities.Load64(2, (ulong)buffer.ToInt64()));

                    // x3 = bufferSize
                    code.AddRange(NativeUtilities.Load64(3, (ulong)bufferSize));

                    // x4 = NULL (for ReturnLength)
                    code.AddRange(NativeUtilities.Load64(4, 0));

                    // x16 = NtQueryObject
                    code.AddRange(NativeUtilities.Load64(16, (ulong)ntQueryObject.ToInt64()));

                    // br x16
                    code.Add(NativeUtilities.EncodeBr(16));

                    // x16 = ExitThread. result is in x0 → already correct for ExitThread
                    code.AddRange(NativeUtilities.Load64(16, (ulong)exitThread.ToInt64()));

                    // br x16
                    code.Add(NativeUtilities.EncodeBr(16));

                    // Convert instruction list to byte array
                    foreach (var instr in code)
                    {
                        shellcode.AddRange(BitConverter.GetBytes(instr));
                    }
                    break;
                default:
                    throw new PlatformNotSupportedException("Unsupported architecture: " + ProcessArchitecture);
            }
            return NativeUtilities.AllocateExecutableMemory(shellcode.ToArray());
        }

        /// <summary>
        /// The lookup table of object types.
        /// </summary>
        private static readonly ReadOnlyDictionary<ushort, string> ObjectTypeLookupTable = GetObjectTypeLookupTable();

        /// <summary>
        /// The architecture of the current process.
        /// </summary>
        private static readonly SystemArchitecture ProcessArchitecture = ExecutableUtilities.GetExecutableInfo(Process.GetCurrentProcess().MainModule!.FileName).Architecture;

        /// <summary>
        /// The duration to wait for a hung NtQueryObject thread to terminate.
        /// </summary>
        private static readonly TimeSpan GetObjectNameThreadTimeout = TimeSpan.FromMilliseconds(125);

        /// <summary>
        /// The size of the buffer used to retrieve object names.
        /// </summary>
        private const int GetObjectNameBufferSize = 1024;
    }
}
