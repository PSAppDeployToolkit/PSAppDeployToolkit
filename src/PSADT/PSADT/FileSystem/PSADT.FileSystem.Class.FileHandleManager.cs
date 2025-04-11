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
using Windows.Win32.System.WindowsProgramming;
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
            var handleEntryExSize = Marshal.SizeOf<Ntdll.SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX>();
            var handleInfoExSize = Marshal.SizeOf<Ntdll.SYSTEM_HANDLE_INFORMATION_EX>();

            // Query the total system handle information.
            var handleBufferSize = handleInfoExSize + handleEntryExSize;
            var handleBufferPtr = Marshal.AllocHGlobal(handleBufferSize);
            var status = Ntdll.NtQuerySystemInformation(SystemExtendedHandleInformation, handleBufferPtr, handleBufferSize, out int handleBufferReqLength);
            while (status == NTSTATUS.STATUS_INFO_LENGTH_MISMATCH)
            {
                Marshal.FreeHGlobal(handleBufferPtr);
                handleBufferSize = handleBufferReqLength;
                handleBufferPtr = Marshal.AllocHGlobal(handleBufferReqLength);
                status = Ntdll.NtQuerySystemInformation(SystemExtendedHandleInformation, handleBufferPtr, handleBufferSize, out handleBufferReqLength);
            }
            if (status != NTSTATUS.STATUS_SUCCESS)
            {
                Marshal.FreeHGlobal(handleBufferPtr);
                throw ExceptionUtilities.GetExceptionForLastWin32Error((WIN32_ERROR)PInvoke.RtlNtStatusToDosError(status));
            }

            // Read the number of handles from the buffer.
            var handleCount = Marshal.PtrToStructure<Ntdll.SYSTEM_HANDLE_INFORMATION_EX>(handleBufferPtr).NumberOfHandles.ToUInt64();
            var handleEntryPtr = handleBufferPtr + handleInfoExSize;

            // Process all found handles.
            var openHandles = new List<FileHandleInfo>();
            for (ulong i = 0; i < handleCount; i++)
            {
                // Read the handle information into a structure.
                var sysHandle = Marshal.PtrToStructure<Ntdll.SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX>(handleEntryPtr);
                handleEntryPtr += handleEntryExSize;

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

                // Determine what we're working with.
                string? objectType;
                string? objectName;
                try
                {
                    // Get the handle's type name to check if it's a file/directory handle.
                    objectType = GetObjectInformation(localHandle, OBJECT_INFORMATION_CLASS.ObjectTypeInformation);
                    if (string.IsNullOrWhiteSpace(objectType) || (objectType != "File" && objectType != "Directory"))
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

                    // Get the handle's name to check if it's a hard drive path.
                    objectName = GetObjectInformation(localHandle, OBJECT_INFORMATION_CLASS.ObjectNameInformation);
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

            // Free the allocated memory and return the results.
            Marshal.FreeHGlobal(handleBufferPtr);
            return openHandles.AsReadOnly();
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
        /// Retrieves the name of an object associated with a handle.
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="infoClass"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private static string? GetObjectInformation(HANDLE handle, OBJECT_INFORMATION_CLASS infoClass)
        {
            // Do an initial query to get the required buffer size.
            int bufferReqLength;
            try
            {
                Ntdll.NtQueryObject(handle, infoClass, IntPtr.Zero, 0, out bufferReqLength);
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
                    Ntdll.NtQueryObject(handle, infoClass, bufferPtr, bufferReqLength, out _);
                }
                catch (Win32Exception ex) when ((ex.NativeErrorCode == (int)WIN32_ERROR.ERROR_NOT_SUPPORTED) || (ex.NativeErrorCode == (int)WIN32_ERROR.ERROR_BAD_PATHNAME))
                {
                    return null;
                }
                catch (UnauthorizedAccessException ex) when (ex.HResult == HRESULT.E_ACCESSDENIED)
                {
                    return null;
                }
                switch (infoClass)
                {
                    case OBJECT_INFORMATION_CLASS.ObjectNameInformation:
                        return Marshal.PtrToStructure<OBJECT_NAME_INFORMATION>(bufferPtr).Name.Buffer.ToString()?.Trim('\0').Trim();
                    case OBJECT_INFORMATION_CLASS.ObjectTypeInformation:
                        return Marshal.PtrToStructure<PUBLIC_OBJECT_TYPE_INFORMATION>(bufferPtr).TypeName.Buffer.ToString()?.Trim('\0').Trim();
                    default:
                        throw new ArgumentOutOfRangeException(nameof(infoClass), $"Unsupported OBJECT_INFORMATION_CLASS: {infoClass}");
                }
            }
            finally
            {
                Marshal.FreeHGlobal(bufferPtr);
            }
        }

        /// <summary>
        /// The SystemExtendedHandleInformation class from the kernel.
        /// </summary>
        private const int SystemExtendedHandleInformation = 64;
    }
}
