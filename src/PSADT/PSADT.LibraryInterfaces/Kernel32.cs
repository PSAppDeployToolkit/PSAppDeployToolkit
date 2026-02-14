using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Threading;
using Microsoft.Win32.SafeHandles;
using PSADT.LibraryInterfaces.Extensions;
using PSADT.LibraryInterfaces.SafeHandles;
using PSADT.LibraryInterfaces.Utilities;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Security;
using Windows.Win32.Storage.FileSystem;
using Windows.Win32.System.Diagnostics.Debug;
using Windows.Win32.System.JobObjects;
using Windows.Win32.System.LibraryLoader;
using Windows.Win32.System.Power;
using Windows.Win32.System.SystemInformation;
using Windows.Win32.System.Threading;

namespace PSADT.LibraryInterfaces
{
    /// <summary>
    /// Provides managed wrappers for selected native Windows Kernel32.dll functions, enabling advanced process, thread,
    /// job object, file, and system operations from .NET code.
    /// </summary>
    /// <remarks>The methods in this class offer safe and convenient access to low-level Windows API
    /// functionality, including process and thread management, job object configuration, file and device operations,
    /// and system information retrieval. Most methods throw exceptions on failure, translating native error codes into
    /// .NET exceptions for easier error handling. Handles returned by these methods must be released by the caller to
    /// avoid resource leaks. This class is intended for advanced scenarios that require direct interaction with Windows
    /// system APIs.</remarks>
    internal static class Kernel32
    {
        /// <summary>
        /// Determines whether the Out-Of-Box Experience (OOBE) has been completed on the system.
        /// </summary>
        /// <param name="isOOBEComplete">When this method returns, contains a value that indicates whether OOBE is complete. Contains <see
        /// langword="true"/> if OOBE is complete; otherwise, <see langword="false"/>. This parameter is passed
        /// uninitialized.</param>
        /// <returns>A value that indicates whether the operation succeeded. Returns <see langword="true"/> if the call was
        /// successful; otherwise, <see langword="false"/>.</returns>
        internal static BOOL OOBEComplete(out BOOL isOOBEComplete)
        {
            BOOL res = PInvoke.OOBEComplete(out isOOBEComplete);
            return !res ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : res;
        }

        /// <summary>
        /// Loads the specified module into the address space of the calling process with the given loading options.
        /// </summary>
        /// <remarks>This method throws an exception if the module cannot be loaded. The returned handle
        /// must be released to avoid resource leaks.</remarks>
        /// <param name="lpLibFileName">The name or path of the module to load. This can be a library file name or a full path. Cannot be null or
        /// empty.</param>
        /// <param name="dwFlags">A combination of flags that control how the module is loaded. These flags determine aspects such as search
        /// path behavior and dependency resolution.</param>
        /// <returns>A safe handle representing the loaded module. The caller is responsible for releasing the handle when it is
        /// no longer needed.</returns>
        internal static FreeLibrarySafeHandle LoadLibraryEx(string lpLibFileName, LOAD_LIBRARY_FLAGS dwFlags)
        {
            FreeLibrarySafeHandle? res = PInvoke.LoadLibraryEx(lpLibFileName, dwFlags);
            return res is null || res.IsInvalid ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : res;
        }

        /// <summary>
        /// Retrieves the address of an exported function or variable from the specified dynamic-link library (DLL)
        /// module.
        /// </summary>
        /// <remarks>This method throws an exception if the specified function or variable cannot be
        /// found. The returned address can be used to invoke the function or access the variable. The caller is
        /// responsible for ensuring that the signature of the function or variable matches the expected type.</remarks>
        /// <param name="hModule">A handle to the DLL module that contains the function or variable. This handle must have been obtained by
        /// loading the module with a method such as LoadLibrary. Cannot be null.</param>
        /// <param name="lpProcName">The name of the function or variable to retrieve, or the ordinal value as a string. Cannot be null or empty.</param>
        /// <returns>A FARPROC representing the address of the specified function or variable.</returns>
        internal static FARPROC GetProcAddress(SafeHandle hModule, string lpProcName)
        {
            FARPROC res = PInvoke.GetProcAddress(hModule, lpProcName);
            return res.IsNull ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : res;
        }

        /// <summary>
        /// Retrieves the names of all sections in the specified initialization file.
        /// </summary>
        /// <remarks>If the buffer specified by lpReturnedString is too small to hold all section names,
        /// an exception is thrown. The section names are returned as a sequence of null-terminated strings, terminated
        /// by an additional null character.</remarks>
        /// <param name="lpReturnedString">A span of characters that receives the section names, separated by null characters. The buffer must be large
        /// enough to hold all section names and a final null terminator.</param>
        /// <param name="lpFileName">The full path to the initialization (.ini) file from which to retrieve section names. Cannot be null.</param>
        /// <returns>The number of characters copied to lpReturnedString, not including the final null character.</returns>
        internal static uint GetPrivateProfileSectionNames(Span<char> lpReturnedString, string lpFileName)
        {
            uint res = PInvoke.GetPrivateProfileSectionNames(lpReturnedString, lpFileName);
            return res == lpReturnedString.Length - 2
                ? throw ExceptionUtilities.GetException(WIN32_ERROR.ERROR_INSUFFICIENT_BUFFER)
                : res;
        }

        /// <summary>
        /// Retrieves all key name and value pairs for the specified section from the given initialization file.
        /// </summary>
        /// <remarks>If the buffer is too small to hold all the data, an exception is thrown. The returned
        /// data consists of key-value pairs separated by null characters, with a double null character marking the end
        /// of the data.</remarks>
        /// <param name="lpAppName">The name of the section in the initialization file whose key-value pairs are to be retrieved. Cannot be
        /// null.</param>
        /// <param name="lpReturnedString">A buffer that receives the key name and value pairs, formatted as a series of null-terminated strings. The
        /// buffer must be large enough to hold the data, including the final null terminator.</param>
        /// <param name="lpFileName">The name of the initialization file. Cannot be null.</param>
        /// <returns>The number of characters copied to the buffer, not including the terminating null character.</returns>
        internal static uint GetPrivateProfileSection(string lpAppName, Span<char> lpReturnedString, string lpFileName)
        {
            uint res = PInvoke.GetPrivateProfileSection(lpAppName, lpReturnedString, lpFileName);
            return res == lpReturnedString.Length - 2
                ? throw ExceptionUtilities.GetException(WIN32_ERROR.ERROR_INSUFFICIENT_BUFFER)
                : res;
        }

        /// <summary>
        /// Retrieves a string value from the specified section and key in an initialization (INI) file.
        /// </summary>
        /// <remarks>If the buffer is too small to hold the result, an exception is thrown. This method
        /// throws an exception if a Windows error occurs during the operation.</remarks>
        /// <param name="lpAppName">The name of the section containing the key. Cannot be null.</param>
        /// <param name="lpKeyName">The name of the key whose value is to be retrieved. If null, all key names in the specified section are
        /// returned.</param>
        /// <param name="lpDefault">The default string to return if the key is not found. If null, an empty string is used as the default.</param>
        /// <param name="lpReturnedString">A buffer that receives the retrieved string. The buffer must be large enough to hold the result, including
        /// the terminating null character.</param>
        /// <param name="lpFileName">The full path to the initialization file. Cannot be null.</param>
        /// <returns>The number of characters copied to the buffer, not including the terminating null character.</returns>
        internal static uint GetPrivateProfileString(string lpAppName, string? lpKeyName, string? lpDefault, Span<char> lpReturnedString, string lpFileName)
        {
            uint res = PInvoke.GetPrivateProfileString(lpAppName, lpKeyName, lpDefault, lpReturnedString, lpFileName);
            if (res == 0 && (ExceptionUtilities.GetLastWin32Error() is WIN32_ERROR lastWin32Error) && lastWin32Error != WIN32_ERROR.NO_ERROR)
            {
                throw ExceptionUtilities.GetException(lastWin32Error);
            }
            else if (res == lpReturnedString.Length - 1 || res == lpReturnedString.Length - 2)
            {
                throw ExceptionUtilities.GetException(WIN32_ERROR.ERROR_INSUFFICIENT_BUFFER);
            }
            return res;
        }

        /// <summary>
        /// Writes a section to the specified initialization file (INI file), replacing the existing section with the
        /// provided key-value pairs.
        /// </summary>
        /// <remarks>If <paramref name="lpString"/> is null, the specified section is removed from the
        /// file. The file must be accessible for writing. This method throws an exception if the underlying Windows API
        /// call fails.</remarks>
        /// <param name="lpAppName">The name of the section to be written to the initialization file. Cannot be null or empty.</param>
        /// <param name="lpString">A string containing the key-value pairs to write to the section, formatted as a sequence of null-terminated
        /// strings ending with two null characters. If null, the section is deleted.</param>
        /// <param name="lpFileName">The full path to the initialization file. Cannot be null or empty.</param>
        /// <returns>A value indicating whether the operation succeeded. Returns <see langword="true"/> if the section was
        /// written successfully; otherwise, <see langword="false"/>.</returns>
        internal static BOOL WritePrivateProfileSection(string lpAppName, string? lpString, string lpFileName)
        {
            BOOL res = PInvoke.WritePrivateProfileSection(lpAppName, lpString, lpFileName);
            return !res ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : res;
        }

        /// <summary>
        /// Writes a string value to the specified section and key in an initialization (INI) file.
        /// </summary>
        /// <remarks>This method throws an exception if the underlying Windows API call fails. The method
        /// is intended for use with legacy INI files and may not be suitable for new applications. The file specified
        /// by lpFileName must exist and be accessible for writing.</remarks>
        /// <param name="lpAppName">The name of the section to which the string will be written. This value cannot be null.</param>
        /// <param name="lpKeyName">The name of the key to be associated with the string. If this parameter is null, the entire section
        /// specified by lpAppName is deleted.</param>
        /// <param name="lpString">The string to write to the specified key. If this parameter is null, the key specified by lpKeyName is
        /// deleted.</param>
        /// <param name="lpFileName">The full path to the initialization file. This value cannot be null.</param>
        /// <returns>true if the operation succeeds; otherwise, false.</returns>
        internal static BOOL WritePrivateProfileString(string lpAppName, string? lpKeyName, string? lpString, string lpFileName)
        {
            BOOL res = PInvoke.WritePrivateProfileString(lpAppName, lpKeyName, lpString, lpFileName);
            return !res ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : res;
        }

        /// <summary>
        /// Creates a new I/O completion port or associates a file handle with an existing I/O completion port.
        /// </summary>
        /// <remarks>This method wraps the native CreateIoCompletionPort Windows API. If the operation
        /// fails, a Win32 exception is thrown. The returned handle should be closed when no longer needed to avoid
        /// resource leaks.</remarks>
        /// <param name="FileHandle">The file handle to associate with the I/O completion port. Can be a file, socket, or device handle. If null,
        /// a new completion port is created.</param>
        /// <param name="ExistingCompletionPort">An existing I/O completion port handle to associate with the file handle, or null to create a new completion
        /// port.</param>
        /// <param name="CompletionKey">A value to be returned through the completion port with each I/O completion packet for the specified file
        /// handle. Used to identify the source of the I/O operation.</param>
        /// <param name="NumberOfConcurrentThreads">The maximum number of threads that the operating system can allow to concurrently process I/O completion
        /// packets for the port. Must be greater than zero when creating a new port; ignored when associating with an
        /// existing port.</param>
        /// <returns>A SafeFileHandle representing the I/O completion port. The handle is valid and must be released by the
        /// caller.</returns>
        internal static SafeFileHandle CreateIoCompletionPort(SafeHandle FileHandle, SafeHandle? ExistingCompletionPort, nuint CompletionKey, uint NumberOfConcurrentThreads)
        {
            SafeFileHandle res = PInvoke.CreateIoCompletionPort(FileHandle, ExistingCompletionPort, CompletionKey, NumberOfConcurrentThreads);
            return res.IsInvalid ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : res;
        }

        /// <summary>
        /// Creates or associates an I/O completion port with a specified file handle, allowing asynchronous I/O
        /// operations to be managed and completed efficiently.
        /// </summary>
        /// <remarks>This method is intended for advanced scenarios involving asynchronous I/O on Windows
        /// platforms. The caller is responsible for managing the lifetime of the returned handle. Improper use may lead
        /// to resource leaks or undefined behavior.</remarks>
        /// <param name="FileHandle">The handle to a file, socket, or device to associate with the I/O completion port. If this parameter is set
        /// to a special value indicating no file association, a new completion port is created.</param>
        /// <param name="ExistingCompletionPort">An existing I/O completion port to associate with the file handle, or null to create a new completion port.</param>
        /// <param name="CompletionKey">A value to be returned through the completion port with each I/O completion packet for the specified file
        /// handle. This value can be used to identify the source of the I/O operation.</param>
        /// <param name="NumberOfConcurrentThreads">The maximum number of threads that the operating system can allow to concurrently process I/O completion
        /// packets for the port. Must be greater than zero.</param>
        /// <returns>A SafeFileHandle representing the I/O completion port. The handle can be used to post and retrieve I/O
        /// completion packets.</returns>
        internal static SafeFileHandle CreateIoCompletionPort(HANDLE FileHandle, SafeHandle? ExistingCompletionPort, nuint CompletionKey, uint NumberOfConcurrentThreads)
        {
            using SafeFileHandle safeFileHandle = new(FileHandle, false);
            return CreateIoCompletionPort(safeFileHandle, ExistingCompletionPort, CompletionKey, NumberOfConcurrentThreads);
        }

        /// <summary>
        /// Wrapper around CreateJobObject to manage error handling.
        /// </summary>
        /// <param name="lpJobAttributes"></param>
        /// <param name="lpName"></param>
        /// <returns></returns>
        /// <exception cref="Win32Exception"></exception>
        internal static SafeFileHandle CreateJobObject(SECURITY_ATTRIBUTES? lpJobAttributes, string? lpName)
        {
            SafeFileHandle res = PInvoke.CreateJobObject(lpJobAttributes, lpName);
            return res.IsInvalid ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : res;
        }

        /// <summary>
        /// Sets limits or configuration information for the specified job object.
        /// </summary>
        /// <remarks>This method throws an exception if the operation fails. The caller is responsible for
        /// ensuring that the buffer passed to lpJobObjectInformation is properly initialized and matches the expected
        /// structure for the specified information class.</remarks>
        /// <param name="hJob">A handle to the job object to be modified. This handle must have the JOB_OBJECT_SET_ATTRIBUTES access right.</param>
        /// <param name="JobObjectInformationClass">A value that specifies the type of information to set for the job object. This determines the structure
        /// expected in the information buffer.</param>
        /// <param name="lpJobObjectInformation">A pointer to a buffer that contains the information to be set. The structure and contents of this buffer
        /// depend on the value of the JobObjectInformationClass parameter.</param>
        /// <param name="cbJobObjectInformationLength">The size, in bytes, of the information buffer pointed to by lpJobObjectInformation.</param>
        /// <returns>true if the information was set successfully; otherwise, false.</returns>
        private static BOOL SetInformationJobObject(SafeHandle hJob, JOBOBJECTINFOCLASS JobObjectInformationClass, nint lpJobObjectInformation, uint cbJobObjectInformationLength)
        {
            bool hJobAddRef = false;
            BOOL res;
            try
            {
                hJob.DangerousAddRef(ref hJobAddRef);
                unsafe
                {
                    res = PInvoke.SetInformationJobObject((HANDLE)hJob.DangerousGetHandle(), JobObjectInformationClass, (void*)lpJobObjectInformation, cbJobObjectInformationLength);
                }
            }
            finally
            {
                if (hJobAddRef)
                {
                    hJob.DangerousRelease();
                }
            }
            return !res ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : res;
        }

        /// <summary>
        /// Associates a completion port with the specified job object.
        /// </summary>
        /// <remarks>This method is typically used to receive asynchronous notifications about job object
        /// events through an I/O completion port. The job object must not already be associated with a completion
        /// port.</remarks>
        /// <param name="hJob">A handle to the job object to associate with a completion port. This handle must have the
        /// JOB_OBJECT_SET_ATTRIBUTES access right.</param>
        /// <param name="lpJobObjectInformation">A structure that specifies the completion port and completion key to associate with the job object.</param>
        /// <returns>A nonzero value if the function succeeds; otherwise, zero. To get extended error information, call
        /// GetLastError.</returns>
        internal static BOOL SetInformationJobObject(SafeHandle hJob, in JOBOBJECT_ASSOCIATE_COMPLETION_PORT lpJobObjectInformation)
        {
            unsafe
            {
                fixed (JOBOBJECT_ASSOCIATE_COMPLETION_PORT* pInfo = &lpJobObjectInformation)
                {
                    return SetInformationJobObject(hJob, JOBOBJECTINFOCLASS.JobObjectAssociateCompletionPortInformation, (nint)pInfo, (uint)sizeof(JOBOBJECT_ASSOCIATE_COMPLETION_PORT));
                }
            }
        }

        /// <summary>
        /// Sets extended limit information for the specified job object.
        /// </summary>
        /// <remarks>Use this method to configure resource limits and other extended settings for a job
        /// object. The job object must have been created previously, and the caller must have appropriate permissions.
        /// For more information about job objects and their limits, see the Windows API documentation.</remarks>
        /// <param name="hJob">A handle to the job object to be updated. This handle must have the JOB_OBJECT_SET_ATTRIBUTES access right.</param>
        /// <param name="lpJobObjectInformation">A structure that contains the extended limit information to set for the job object.</param>
        /// <returns>A nonzero value if the function succeeds; otherwise, zero.</returns>
        internal static BOOL SetInformationJobObject(SafeHandle hJob, in JOBOBJECT_EXTENDED_LIMIT_INFORMATION lpJobObjectInformation)
        {
            unsafe
            {
                fixed (JOBOBJECT_EXTENDED_LIMIT_INFORMATION* pInfo = &lpJobObjectInformation)
                {
                    return SetInformationJobObject(hJob, JOBOBJECTINFOCLASS.JobObjectExtendedLimitInformation, (nint)pInfo, (uint)sizeof(JOBOBJECT_EXTENDED_LIMIT_INFORMATION));
                }
            }
        }

        /// <summary>
        /// Creates a new process and its primary thread using the specified application name, command line, security
        /// attributes, environment block, and startup information.
        /// </summary>
        /// <remarks>This method wraps the native CreateProcess Windows API and throws an exception if
        /// process creation fails. The caller is responsible for closing handles in the returned PROCESS_INFORMATION
        /// structure when they are no longer needed.</remarks>
        /// <param name="lpApplicationName">The name of the module to execute. If this parameter is null, the module name must be the first white
        /// space–delimited token in lpCommandLine.</param>
        /// <param name="lpCommandLine">A reference to a span containing the command line to execute. The string can include the application name
        /// and any arguments.</param>
        /// <param name="lpProcessAttributes">A SECURITY_ATTRIBUTES structure that determines whether the returned process handle can be inherited by
        /// child processes. Can be null to use default security.</param>
        /// <param name="lpThreadAttributes">A SECURITY_ATTRIBUTES structure that determines whether the returned thread handle can be inherited by child
        /// processes. Can be null to use default security.</param>
        /// <param name="bInheritHandles">true if each inheritable handle in the calling process is inherited by the new process; otherwise, false.</param>
        /// <param name="dwCreationFlags">A combination of PROCESS_CREATION_FLAGS values that control the priority class and creation of the process.</param>
        /// <param name="lpEnvironment">A SafeEnvironmentBlockHandle representing the environment block for the new process. Cannot be null or
        /// closed.</param>
        /// <param name="lpCurrentDirectory">The full path to the current directory for the new process. If null, the new process will have the same
        /// current drive and directory as the calling process.</param>
        /// <param name="lpStartupInfo">A reference to a STARTUPINFOW structure specifying the window station, desktop, standard handles, and
        /// appearance of the main window for the new process.</param>
        /// <param name="lpProcessInformation">When this method returns, contains a PROCESS_INFORMATION structure with information about the newly created
        /// process and its primary thread.</param>
        /// <returns>true if the process is created successfully; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown if lpEnvironment is null or has been closed.</exception>
        internal static BOOL CreateProcess(string? lpApplicationName, ref Span<char> lpCommandLine, in SECURITY_ATTRIBUTES? lpProcessAttributes, in SECURITY_ATTRIBUTES? lpThreadAttributes, in BOOL bInheritHandles, PROCESS_CREATION_FLAGS dwCreationFlags, SafeEnvironmentBlockHandle? lpEnvironment, string? lpCurrentDirectory, in STARTUPINFOW lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation)
        {
            bool lpEnvironmentAddRef = false;
            BOOL res;
            try
            {
                lpEnvironment?.DangerousAddRef(ref lpEnvironmentAddRef);
                unsafe
                {
                    res = PInvoke.CreateProcess(lpApplicationName, ref lpCommandLine, lpProcessAttributes, lpThreadAttributes, bInheritHandles, dwCreationFlags, lpEnvironment is not null ? (void*)lpEnvironment.DangerousGetHandle() : null, lpCurrentDirectory, in lpStartupInfo, out lpProcessInformation);
                }
            }
            finally
            {
                if (lpEnvironmentAddRef)
                {
                    lpEnvironment?.DangerousRelease();
                }
            }
            return !res ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : res;
        }

        /// <summary>
        /// Creates a new process and its primary thread using the specified application name, command line, security
        /// attributes, environment, and startup information.
        /// </summary>
        /// <remarks>This method is a low-level wrapper for the Windows CreateProcess API and requires
        /// careful management of memory and handles. The caller is responsible for ensuring that all parameters meet
        /// the requirements of the underlying Windows API. On failure, a Win32 exception is thrown with details from
        /// the last error code.</remarks>
        /// <param name="lpApplicationName">The name of the module to execute. If this parameter is null, the module name must be the first white
        /// space–delimited token in <paramref name="lpCommandLine"/>.</param>
        /// <param name="lpCommandLine">A span containing the command line to execute, including the application name and any arguments. The span
        /// must be null-terminated.</param>
        /// <param name="lpProcessAttributes">A reference to a <see cref="SECURITY_ATTRIBUTES"/> structure that determines whether the returned process
        /// handle can be inherited by child processes. If null, the handle cannot be inherited.</param>
        /// <param name="lpThreadAttributes">A reference to a <see cref="SECURITY_ATTRIBUTES"/> structure that determines whether the returned thread
        /// handle can be inherited by child processes. If null, the handle cannot be inherited.</param>
        /// <param name="bInheritHandles">Indicates whether each handle in the calling process is inherited by the new process. Specify <see
        /// langword="true"/> to inherit handles; otherwise, <see langword="false"/>.</param>
        /// <param name="dwCreationFlags">A set of flags that control the priority class and creation of the process. This parameter can be a
        /// combination of <see cref="PROCESS_CREATION_FLAGS"/> values.</param>
        /// <param name="lpEnvironment">A handle to an environment block for the new process. If null, the new process uses the environment of the
        /// calling process.</param>
        /// <param name="lpCurrentDirectory">The full path to the current directory for the new process. If null, the new process uses the current
        /// directory of the calling process.</param>
        /// <param name="lpStartupInfoEx">A reference to a <see cref="STARTUPINFOEXW"/> structure that specifies the window station, desktop, standard
        /// handles, and attributes for the new process.</param>
        /// <param name="lpProcessInformation">When this method returns, contains a <see cref="PROCESS_INFORMATION"/> structure with information about the
        /// newly created process and its primary thread.</param>
        /// <returns>A <see cref="BOOL"/> value that is <see langword="true"/> if the process is created successfully; otherwise,
        /// <see langword="false"/>.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="lpCommandLine"/> is not empty and does not contain a null terminator.</exception>
        internal static BOOL CreateProcess(string? lpApplicationName, ref Span<char> lpCommandLine, in SECURITY_ATTRIBUTES? lpProcessAttributes, in SECURITY_ATTRIBUTES? lpThreadAttributes, in BOOL bInheritHandles, PROCESS_CREATION_FLAGS dwCreationFlags, SafeEnvironmentBlockHandle? lpEnvironment, string? lpCurrentDirectory, in STARTUPINFOEXW lpStartupInfoEx, out PROCESS_INFORMATION lpProcessInformation)
        {
            if (lpCommandLine != Span<char>.Empty && lpCommandLine.LastIndexOf('\0') == -1)
            {
                throw new ArgumentException("Required null terminator missing.", nameof(lpCommandLine));
            }
            bool lpEnvironmentAddRef = false;
            try
            {
                BOOL res;
                unsafe
                {
                    fixed (char* lpApplicationNameLocal = lpApplicationName, plpCommandLine = lpCommandLine, lpCurrentDirectoryLocal = lpCurrentDirectory)
                    fixed (PROCESS_INFORMATION* lpProcessInformationLocal = &lpProcessInformation)
                    fixed (STARTUPINFOEXW* lpStartupInfoExLocal = &lpStartupInfoEx)
                    {
                        SECURITY_ATTRIBUTES lpProcessAttributesLocal = lpProcessAttributes ?? default;
                        SECURITY_ATTRIBUTES lpThreadAttributesLocal = lpThreadAttributes ?? default;
                        lpEnvironment?.DangerousAddRef(ref lpEnvironmentAddRef);
                        res = PInvoke.CreateProcess(lpApplicationNameLocal, plpCommandLine, lpProcessAttributes.HasValue ? &lpProcessAttributesLocal : null, lpThreadAttributes.HasValue ? &lpThreadAttributesLocal : null, bInheritHandles, dwCreationFlags, lpEnvironment is not null ? (void*)lpEnvironment.DangerousGetHandle() : null, lpCurrentDirectoryLocal, (STARTUPINFOW*)lpStartupInfoExLocal, lpProcessInformationLocal);
                        if (!res)
                        {
                            throw ExceptionUtilities.GetExceptionForLastWin32Error();
                        }
                        lpCommandLine = lpCommandLine.Slice(0, ((PWSTR)plpCommandLine).Length);
                    }
                }
                return res;
            }
            finally
            {
                if (lpEnvironmentAddRef)
                {
                    lpEnvironment?.DangerousRelease();
                }
            }
        }

        /// <summary>
        /// Associates a process with a job object, enabling the job object to manage and limit the process according to
        /// its configuration.
        /// </summary>
        /// <remarks>Once a process is assigned to a job object, it cannot be assigned to another job
        /// object. Attempting to assign a process that is already associated with a job object will fail.</remarks>
        /// <param name="hJob">A handle to the job object to which the process will be assigned. This handle must have
        /// JOB_OBJECT_ASSIGN_PROCESS access rights and must not be null.</param>
        /// <param name="hProcess">A handle to the process to assign to the job object. This handle must have PROCESS_SET_QUOTA and
        /// PROCESS_TERMINATE access rights and must not be null.</param>
        /// <returns>A value indicating whether the process was successfully assigned to the job object. Returns <see
        /// langword="true"/> if the operation succeeds; otherwise, <see langword="false"/>.</returns>
        internal static BOOL AssignProcessToJobObject(SafeHandle hJob, SafeHandle hProcess)
        {
            BOOL res = PInvoke.AssignProcessToJobObject(hJob, hProcess);
            return !res ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : res;
        }

        /// <summary>
        /// Resumes a thread that has been suspended, allowing it to continue execution.
        /// </summary>
        /// <param name="hThread">A handle to the thread to be resumed. The handle must have the THREAD_SUSPEND_RESUME access right and must
        /// not be closed or invalid.</param>
        /// <returns>The thread's previous suspend count. If the return value is zero, the thread was not previously suspended.</returns>
        internal static uint ResumeThread(SafeHandle hThread)
        {
            uint res = PInvoke.ResumeThread(hThread);
            return res == uint.MaxValue ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : res;
        }

        /// <summary>
        /// Retrieves the results of an I/O operation that has completed on the specified I/O completion port.
        /// </summary>
        /// <remarks>If the operation fails, an exception is thrown containing the last Win32 error. This
        /// method is typically used in advanced scenarios involving asynchronous I/O and completion ports.</remarks>
        /// <param name="CompletionPort">A handle to the I/O completion port from which to dequeue a completion packet. This handle must have been
        /// created by a call to the appropriate completion port creation function.</param>
        /// <param name="lpCompletionCode">When this method returns, contains the completion code associated with the completed I/O operation.</param>
        /// <param name="lpCompletionKey">When this method returns, contains the completion key that was specified when the file handle was associated
        /// with the completion port.</param>
        /// <param name="lpOverlapped">When this method returns, contains a pointer to the OVERLAPPED structure that was specified when the I/O
        /// operation was started.</param>
        /// <param name="dwMilliseconds">The number of milliseconds to wait for a completion packet. Specify INFINITE to wait indefinitely.</param>
        /// <returns>true if a completion packet was successfully dequeued; otherwise, false.</returns>
        internal static BOOL GetQueuedCompletionStatus(SafeHandle CompletionPort, out uint lpCompletionCode, out nuint lpCompletionKey, out nuint lpOverlapped, uint dwMilliseconds)
        {
            BOOL res;
            unsafe
            {
                res = PInvoke.GetQueuedCompletionStatus(CompletionPort, out lpCompletionCode, out lpCompletionKey, out NativeOverlapped* pOverlapped, dwMilliseconds);
                if (!res)
                {
                    throw ExceptionUtilities.GetExceptionForLastWin32Error();
                }
                lpOverlapped = (nuint)pOverlapped;
            }
            return res;
        }

        /// <summary>
        /// Retrieves the termination status code of the specified process.
        /// </summary>
        /// <param name="hProcess">A handle to the process whose exit code is to be retrieved. The handle must have the
        /// PROCESS_QUERY_INFORMATION or PROCESS_QUERY_LIMITED_INFORMATION access right.</param>
        /// <param name="lpExitCode">When this method returns, contains the exit code of the specified process if the function succeeds.</param>
        /// <returns>A value indicating whether the exit code was successfully retrieved. Returns <see langword="true"/> if
        /// successful; otherwise, <see langword="false"/>.</returns>
        internal static BOOL GetExitCodeProcess(SafeHandle hProcess, out uint lpExitCode)
        {
            BOOL res = PInvoke.GetExitCodeProcess(hProcess, out lpExitCode);
            return !res ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : res;
        }

        /// <summary>
        /// Terminates all processes associated with the specified job object and closes the job object handle.
        /// </summary>
        /// <remarks>After termination, the job object handle is closed and cannot be used in subsequent
        /// operations. This method throws an exception if the termination fails.</remarks>
        /// <param name="hJob">A handle to the job object to terminate. This handle must have the JOB_OBJECT_TERMINATE access right and
        /// must not be null.</param>
        /// <param name="uExitCode">The exit code to be used by all processes and threads in the job object.</param>
        /// <returns>true if the job object and all associated processes were terminated successfully; otherwise, false.</returns>
        internal static BOOL TerminateJobObject(SafeHandle hJob, uint uExitCode)
        {
            BOOL res = PInvoke.TerminateJobObject(hJob, uExitCode);
            return !res ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : res;
        }

        /// <summary>
        /// Retrieves the process identifier (PID) for the specified process handle.
        /// </summary>
        /// <param name="Process">A safe handle to the process whose identifier is to be retrieved. The handle must have the
        /// PROCESS_QUERY_INFORMATION or PROCESS_QUERY_LIMITED_INFORMATION access right.</param>
        /// <returns>The process identifier (PID) associated with the specified process handle.</returns>
        internal static uint GetProcessId(SafeHandle Process)
        {
            uint res = PInvoke.GetProcessId(Process);
            return res == 0 ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : res;
        }

        /// <summary>
        /// Duplicates an object handle from one process to another, allowing the target process to access the same
        /// object with specified access rights and options.
        /// </summary>
        /// <remarks>Both the source and target process handles must have the PROCESS_DUP_HANDLE access
        /// right. The caller is responsible for closing the duplicated handle when it is no longer needed to avoid
        /// resource leaks.</remarks>
        /// <param name="hSourceProcessHandle">A handle to the process with the handle to be duplicated. This handle must have the PROCESS_DUP_HANDLE
        /// access right.</param>
        /// <param name="hSourceHandle">The handle to be duplicated. This handle must be valid in the context of the source process.</param>
        /// <param name="hTargetProcessHandle">A handle to the process that will receive the duplicated handle. This handle must have the
        /// PROCESS_DUP_HANDLE access right.</param>
        /// <param name="lpTargetHandle">When this method returns, contains the duplicated handle, valid in the context of the target process.</param>
        /// <param name="dwDesiredAccess">The access rights for the duplicated handle. This parameter specifies the requested access to the object for
        /// the new handle.</param>
        /// <param name="bInheritHandle">A value that indicates whether the duplicated handle is inheritable by child processes. Specify <see
        /// langword="true"/> to make the handle inheritable; otherwise, <see langword="false"/>.</param>
        /// <param name="dwOptions">Options that control the duplication behavior. This parameter can be a combination of
        /// DUPLICATE_HANDLE_OPTIONS flags.</param>
        /// <returns>A value that is <see langword="true"/> if the handle was duplicated successfully; otherwise, <see
        /// langword="false"/>.</returns>
        internal static BOOL DuplicateHandle(SafeHandle hSourceProcessHandle, SafeHandle hSourceHandle, SafeHandle hTargetProcessHandle, out SafeFileHandle lpTargetHandle, PROCESS_ACCESS_RIGHTS dwDesiredAccess, in BOOL bInheritHandle, DUPLICATE_HANDLE_OPTIONS dwOptions)
        {
            BOOL res = PInvoke.DuplicateHandle(hSourceProcessHandle, hSourceHandle, hTargetProcessHandle, out lpTargetHandle, (uint)dwDesiredAccess, bInheritHandle, dwOptions);
            return !res ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : res;
        }

        /// <summary>
        /// Opens an existing local process object and returns a handle with the specified access rights.
        /// </summary>
        /// <remarks>If the process cannot be opened, an exception is thrown. The returned handle grants
        /// the access rights specified by <paramref name="dwDesiredAccess"/>. This method is intended for advanced
        /// scenarios that require direct process handle manipulation.</remarks>
        /// <param name="dwDesiredAccess">A combination of process access rights indicating the requested access to the process object. This
        /// determines the permitted operations on the returned handle.</param>
        /// <param name="bInheritHandle">A value that determines whether the returned handle can be inherited by child processes. Specify <see
        /// langword="true"/> to allow handle inheritance; otherwise, <see langword="false"/>.</param>
        /// <param name="dwProcessId">The identifier of the local process to open. This must be the process ID of an existing process.</param>
        /// <returns>A <see cref="SafeFileHandle"/> representing the opened process handle. The caller is responsible for
        /// releasing the handle when it is no longer needed.</returns>
        internal static SafeFileHandle OpenProcess(PROCESS_ACCESS_RIGHTS dwDesiredAccess, in BOOL bInheritHandle, uint dwProcessId)
        {
            SafeFileHandle? res = PInvoke.OpenProcess_SafeHandle(dwDesiredAccess, bInheritHandle, dwProcessId);
            return res is null || res.IsInvalid ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : res;
        }

        /// <summary>
        /// Retrieves information about MS-DOS device names and their associated target paths on the local system.
        /// </summary>
        /// <remarks>If lpDeviceName is null, lpTargetPath receives a list of all existing MS-DOS device
        /// names. If lpDeviceName is specified, lpTargetPath receives the target path(s) for that device. This method
        /// throws an exception if the underlying system call fails.</remarks>
        /// <param name="lpDeviceName">The device name to query. Specify a device name (such as "C:") to retrieve its mapping, or null to retrieve
        /// a list of all device names. If not null, the string must not be empty.</param>
        /// <param name="lpTargetPath">A buffer that receives the result of the query. The buffer should be large enough to hold the returned path
        /// or list of device names, including the terminating null character(s).</param>
        /// <returns>The number of characters stored in lpTargetPath, not including the terminating null character(s).</returns>
        internal static uint QueryDosDevice(string lpDeviceName, Span<char> lpTargetPath)
        {
            uint res = PInvoke.QueryDosDevice(lpDeviceName, lpTargetPath);
            return res == 0 ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : res;
        }

        /// <summary>
        /// Retrieves the termination status of the specified thread.
        /// </summary>
        /// <remarks>If the thread has not terminated, the exit code returned is STATUS_PENDING. This method
        /// throws an exception if the underlying system call fails.</remarks>
        /// <param name="hThread">A handle to the thread whose exit code is to be retrieved. The handle must have the THREAD_QUERY_INFORMATION
        /// or THREAD_QUERY_LIMITED_INFORMATION access right.</param>
        /// <param name="lpExitCode">When this method returns, contains the exit code of the specified thread if the function succeeds.</param>
        /// <returns>true if the exit code was successfully retrieved; otherwise, false.</returns>
        internal static BOOL GetExitCodeThread(SafeHandle hThread, out uint lpExitCode)
        {
            BOOL res = PInvoke.GetExitCodeThread(hThread, out lpExitCode);
            return !res ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : res;
        }

        /// <summary>
        /// Loads the specified dynamic-link library (DLL) into the address space of the calling process.
        /// </summary>
        /// <remarks>The caller is responsible for releasing the returned handle to avoid resource leaks.
        /// If the library cannot be loaded, an exception is thrown.</remarks>
        /// <param name="lpLibFileName">The name of the DLL file to load. This can be a full path or a file name that is searched for in the
        /// system's standard DLL search order. Cannot be null or empty.</param>
        /// <returns>A <see cref="FreeLibrarySafeHandle"/> representing the loaded library. The handle must be released when no
        /// longer needed.</returns>
        internal static FreeLibrarySafeHandle LoadLibrary(string lpLibFileName)
        {
            FreeLibrarySafeHandle? res = PInvoke.LoadLibrary(lpLibFileName);
            return res is null || res.IsInvalid ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : res;
        }

        /// <summary>
        /// Retrieves a safe handle representing the current process.
        /// </summary>
        /// <remarks>The returned handle is safe to use with Windows API functions that require a process
        /// handle. It is the caller's responsibility to ensure proper disposal of the handle to release system
        /// resources.</remarks>
        /// <returns>A <see cref="SafeHandle"/> that encapsulates a handle to the current process.</returns>
        internal static SafeProcessHandle GetCurrentProcess()
        {
            HANDLE res = PInvoke.GetCurrentProcess();
            return res != (nint)(-1) ? throw new InvalidOperationException("Failed to retrieve handle for current process.") : new(res, true);
        }

        /// <summary>
        /// Retrieves the session ID associated with a specified process ID.
        /// </summary>
        /// <param name="dwProcessId">The process ID for which to retrieve the session ID.</param>
        /// <param name="pSessionId">When this method returns, contains the session ID associated with the specified process ID.</param>
        /// <returns><see langword="true"/> if the session ID was successfully retrieved; otherwise, <see langword="false"/>.</returns>
        internal static BOOL ProcessIdToSessionId(uint dwProcessId, out uint pSessionId)
        {
            BOOL res = PInvoke.ProcessIdToSessionId(dwProcessId, out pSessionId);
            return !res ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : res;
        }

        /// <summary>
        /// Determines whether the system is currently in Terminal Services application installation mode.
        /// </summary>
        /// <remarks>Terminal Services application installation mode is used to install applications in a
        /// way that supports multiple users on a terminal server. This method can be used to check the current mode 
        /// before performing operations that depend on the installation mode.</remarks>
        /// <returns><see langword="true"/> if the system is in Terminal Services application installation mode; otherwise, <see
        /// langword="false"/>.</returns>
        [DllImport("kernel32.dll", SetLastError = false, ExactSpelling = true), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool TermsrvAppInstallMode();

        /// <summary>
        /// Retrieves system firmware table data for the specified firmware table provider and table ID.
        /// </summary>
        /// <remarks>This method retrieves firmware table data from the system using the specified
        /// provider and table ID. If the buffer provided in <paramref name="pFirmwareTableBuffer"/> is too small to
        /// hold the data, an <see cref="OverflowException"/> is thrown.</remarks>
        /// <param name="FirmwareTableProviderSignature">The signature of the firmware table provider. This identifies the type of firmware table to retrieve.</param>
        /// <param name="FirmwareTableID">The identifier of the specific firmware table to retrieve.</param>
        /// <param name="pFirmwareTableBuffer">A buffer to store the retrieved firmware table data. The buffer must be large enough to hold the data.</param>
        /// <returns>The size, in bytes, of the firmware table data retrieved.</returns>
        /// <exception cref="OverflowException">Thrown if the buffer provided in <paramref name="pFirmwareTableBuffer"/> is too small to hold the firmware
        /// table data.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Enforcing this rule just makes a mess.")]
        internal static uint GetSystemFirmwareTable(FIRMWARE_TABLE_PROVIDER FirmwareTableProviderSignature, FIRMWARE_TABLE_ID FirmwareTableID, Span<byte> pFirmwareTableBuffer)
        {
            uint res = PInvoke.GetSystemFirmwareTable(FirmwareTableProviderSignature, (uint)FirmwareTableID, pFirmwareTableBuffer);
            if (res == 0)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            if (pFirmwareTableBuffer.Length != 0 && res > pFirmwareTableBuffer.Length)
            {
                throw ExceptionUtilities.GetException(WIN32_ERROR.ERROR_INSUFFICIENT_BUFFER);
            }
            return res;
        }

        /// <summary>
        /// Retrieves the current system power status, including battery and AC power information.
        /// </summary>
        /// <remarks>This method wraps a call to the native Win32 API function
        /// <c>GetSystemPowerStatus</c>. It throws an exception if the underlying API call fails.</remarks>
        /// <param name="lpSystemPowerStatus">When the method returns, contains a <see cref="SYSTEM_POWER_STATUS"/> structure with details about the
        /// system's power status.</param>
        /// <returns><see langword="true"/> if the operation succeeds; otherwise, <see langword="false"/>.</returns>
        internal static BOOL GetSystemPowerStatus(out SYSTEM_POWER_STATUS lpSystemPowerStatus)
        {
            BOOL res = PInvoke.GetSystemPowerStatus(out lpSystemPowerStatus);
            return res == 0 ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : res;
        }

        /// <summary>
        /// Determines whether a specified process is running within a specified job.
        /// </summary>
        /// <param name="ProcessHandle">A handle to the process to be checked. This handle must have the PROCESS_QUERY_INFORMATION or
        /// PROCESS_QUERY_LIMITED_INFORMATION access right.</param>
        /// <param name="JobHandle">A handle to the job. If this parameter is <see langword="null"/>, the function checks if the process is
        /// running in any job.</param>
        /// <param name="Result">When this method returns, contains a <see langword="true"/> if the process is in the specified job;
        /// otherwise, <see langword="false"/>.</param>
        /// <returns><see langword="true"/> if the function succeeds; otherwise, <see langword="false"/>.</returns>
        internal static BOOL IsProcessInJob(SafeHandle ProcessHandle, SafeHandle? JobHandle, out BOOL Result)
        {
            BOOL res = PInvoke.IsProcessInJob(ProcessHandle, JobHandle, out Result);
            return !res ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : res;
        }

        /// <summary>
        /// Queries information about the specified job object.
        /// </summary>
        /// <remarks>This method is a wrapper around the native Windows API function
        /// <c>QueryInformationJobObject</c>. It is used to retrieve various types of information about a job object,
        /// such as accounting information, limits, and process information.</remarks>
        /// <param name="hJob">A handle to the job object. This handle must have the Query access right.</param>
        /// <param name="JobObjectInformationClass">The information class for the job object. This parameter specifies the type of information to be queried.</param>
        /// <param name="lpJobObjectInformation">A buffer that receives the information. The format of this data depends on the value of the <paramref
        /// name="JobObjectInformationClass"/> parameter.</param>
        /// <param name="lpReturnLength">When this method returns, contains the size of the data returned in the <paramref
        /// name="lpJobObjectInformation"/> buffer, in bytes.</param>
        /// <returns><see langword="true"/> if the function succeeds; otherwise, <see langword="false"/>.</returns>
        internal static BOOL QueryInformationJobObject(SafeHandle? hJob, JOBOBJECTINFOCLASS JobObjectInformationClass, Span<byte> lpJobObjectInformation, out uint lpReturnLength)
        {
            BOOL res = PInvoke.QueryInformationJobObject(hJob, JobObjectInformationClass, lpJobObjectInformation, out lpReturnLength);
            return !res ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : res;
        }

        /// <summary>
        /// Retrieves the Application User Model ID (AUMID) for a specified process.
        /// </summary>
        /// <remarks>This method wraps a PInvoke call to retrieve the AUMID, and throws an exception if
        /// the operation is unsuccessful.</remarks>
        /// <param name="hProcess">A handle to the process for which the AUMID is being retrieved. This handle must have the necessary access
        /// rights.</param>
        /// <param name="applicationUserModelIdLength">On input, specifies the size of the <paramref name="applicationUserModelId"/> buffer. On output, receives
        /// the length of the AUMID, including the null terminator.</param>
        /// <param name="applicationUserModelId">A buffer that receives the AUMID as a null-terminated string.</param>
        /// <returns>A <see cref="WIN32_ERROR"/> code indicating the result of the operation. Returns <see
        /// cref="WIN32_ERROR.NO_ERROR"/> if successful.</returns>
        internal static WIN32_ERROR GetApplicationUserModelId(SafeHandle hProcess, ref uint applicationUserModelIdLength, Span<char> applicationUserModelId)
        {
            return PInvoke.GetApplicationUserModelId(hProcess, ref applicationUserModelIdLength, applicationUserModelId).ThrowOnFailure();
        }

        /// <summary>
        /// Reads data from an area of memory in a specified process. The process is identified by a handle.
        /// </summary>
        /// <remarks>This method wraps the PInvoke call to ReadProcessMemory and throws an exception if
        /// the operation fails. Ensure that the buffer is large enough to hold the data being read to avoid an <see
        /// cref="OverflowException"/>.</remarks>
        /// <param name="hProcess">A handle to the process with memory that is being read. The handle must have PROCESS_VM_READ access.</param>
        /// <param name="lpBaseAddress">A pointer to the base address in the specified process from which to read.</param>
        /// <param name="lpBuffer">A pointer to a buffer that receives the contents from the address space of the specified process.</param>
        /// <param name="lpNumberOfBytesRead">A pointer to a variable that receives the number of bytes transferred into the specified buffer. This
        /// parameter can be null.</param>
        /// <returns>A <see cref="BOOL"/> indicating whether the operation succeeded.</returns>
        /// <exception cref="OverflowException">Thrown if the buffer was too small and the value was truncated.</exception>
        internal static BOOL ReadProcessMemory(SafeHandle hProcess, nint lpBaseAddress, Span<byte> lpBuffer, out nuint lpNumberOfBytesRead)
        {
            BOOL res;
            unsafe
            {
                res = PInvoke.ReadProcessMemory(hProcess, (void*)lpBaseAddress, lpBuffer, out lpNumberOfBytesRead);
            }
            return !res ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : res;
        }

        /// <summary>
        /// Retrieves a human-readable name for a specified language identifier.
        /// </summary>
        /// <remarks>This method wraps a call to a native function and throws an exception if the
        /// operation fails. Ensure that <paramref name="szLang"/> is sufficiently large to avoid truncation.</remarks>
        /// <param name="wLang">The language identifier for which the name is to be retrieved.</param>
        /// <param name="szLang">A span of characters that receives the language name. The buffer must be large enough to hold the name.</param>
        /// <returns>The number of characters written to <paramref name="szLang"/>, excluding the null terminator.</returns>
        /// <exception cref="OverflowException">Thrown if the buffer provided by <paramref name="szLang"/> is too small to hold the language name.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Enforcing this rule just makes a mess.")]
        internal static uint VerLanguageName(uint wLang, Span<char> szLang)
        {
            uint res = PInvoke.VerLanguageName(wLang, szLang);
            if (res == 0)
            {
                throw new InvalidOperationException("Failed to retrieve language name.");
            }
            if (res > szLang.Length)
            {
                throw ExceptionUtilities.GetException(WIN32_ERROR.ERROR_INSUFFICIENT_BUFFER);
            }
            return res;
        }

        /// <summary>
        /// Posts an I/O completion packet to the specified I/O completion port.
        /// </summary>
        /// <remarks>This method is typically used to queue custom completion packets to an I/O completion
        /// port, allowing threads waiting on the port to be notified. The method throws an exception if the underlying
        /// system call fails.</remarks>
        /// <param name="CompletionPort">A handle to the I/O completion port to which the completion packet will be posted. Must be a valid, open
        /// handle.</param>
        /// <param name="dwNumberOfBytesTransferred">The number of bytes associated with the I/O operation. This value is returned through the completion packet
        /// and can be used by the consumer to determine the amount of data transferred.</param>
        /// <param name="dwCompletionKey">A value to be associated with the completion packet. This value is returned when the completion packet is
        /// dequeued and can be used to identify the source or context of the completion.</param>
        /// <param name="lpOverlapped">A reference to a NativeOverlapped structure to be associated with the completion packet, or the default
        /// value if no overlapped structure is required.</param>
        /// <returns>true if the operation succeeds; otherwise, false.</returns>
        internal static BOOL PostQueuedCompletionStatus(SafeHandle CompletionPort, uint dwNumberOfBytesTransferred, nuint dwCompletionKey, in NativeOverlapped lpOverlapped = default)
        {
            BOOL res;
            unsafe
            {
                fixed (NativeOverlapped* pOverlapped = &lpOverlapped)
                {
                    res = PInvoke.PostQueuedCompletionStatus(CompletionPort, dwNumberOfBytesTransferred, dwCompletionKey, pOverlapped);
                }
            }
            return !res ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : res;
        }

        /// <summary>
        /// Opens an existing file or creates a new file, device, or named pipe, returning a handle with the specified
        /// access rights and sharing mode.
        /// </summary>
        /// <remarks>This method throws an exception if the file cannot be opened or created. The caller
        /// is responsible for closing the returned SafeFileHandle when it is no longer needed. The method is intended
        /// for advanced scenarios that require direct control over file creation and access flags, similar to the
        /// Windows CreateFile API.</remarks>
        /// <param name="lpFileName">The name or path of the file, device, or named pipe to be created or opened. This parameter cannot be null
        /// or empty.</param>
        /// <param name="dwDesiredAccess">The access rights requested for the returned handle, such as read, write, or execute permissions. Specify
        /// one or more values from the FileSystemRights enumeration.</param>
        /// <param name="dwShareMode">The sharing mode for the file or device, determining how the file can be shared with other processes.
        /// Specify one or more values from the FILE_SHARE_MODE enumeration.</param>
        /// <param name="lpSecurityAttributes">A pointer to a SECURITY_ATTRIBUTES structure that determines whether the returned handle can be inherited by
        /// child processes. Can be null to use default security settings.</param>
        /// <param name="dwCreationDisposition">Specifies the action to take on files that exist or do not exist. Use a value from the
        /// FILE_CREATION_DISPOSITION enumeration to control whether to create a new file, open an existing file, or
        /// overwrite an existing file.</param>
        /// <param name="dwFlagsAndAttributes">The file or device attributes and flags, such as file attributes, security flags, and other special options.
        /// Specify one or more values from the FileAttributes enumeration.</param>
        /// <param name="hTemplateFile">A handle to a template file with the desired attributes to apply to the file being created. Can be null if
        /// no template is needed.</param>
        /// <returns>A SafeFileHandle representing the opened or newly created file, device, or named pipe. The handle is valid
        /// and ready for use. If the operation fails, an exception is thrown.</returns>
        internal static SafeFileHandle CreateFile(string lpFileName, FileSystemRights dwDesiredAccess, FILE_SHARE_MODE dwShareMode, in SECURITY_ATTRIBUTES? lpSecurityAttributes, FILE_CREATION_DISPOSITION dwCreationDisposition, FileAttributes dwFlagsAndAttributes, SafeHandle? hTemplateFile = null)
        {
            SafeFileHandle res = PInvoke.CreateFile(lpFileName, (uint)dwDesiredAccess, dwShareMode, lpSecurityAttributes, dwCreationDisposition, (FILE_FLAGS_AND_ATTRIBUTES)dwFlagsAndAttributes, hTemplateFile);
            return res.IsInvalid ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : res;
        }

        /// <summary>
        /// Retrieves the product type of the operating system based on the specified version and service pack
        /// information.
        /// </summary>
        /// <param name="dwOSMajorVersion">The major version number of the operating system.</param>
        /// <param name="dwOSMinorVersion">The minor version number of the operating system.</param>
        /// <param name="dwSpMajorVersion">The major version number of the service pack installed on the operating system.</param>
        /// <param name="dwSpMinorVersion">The minor version number of the service pack installed on the operating system.</param>
        /// <param name="pdwReturnedProductType">When this method returns, contains the product type of the operating system. This parameter is passed
        /// uninitialized.</param>
        /// <returns><see langword="true"/> if the product type information was successfully retrieved; otherwise, <see
        /// langword="false"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the product type information could not be retrieved.</exception>
        internal static BOOL GetProductInfo(uint dwOSMajorVersion, uint dwOSMinorVersion, uint dwSpMajorVersion, uint dwSpMinorVersion, out OS_PRODUCT_TYPE pdwReturnedProductType)
        {
            BOOL res = PInvoke.GetProductInfo(dwOSMajorVersion, dwOSMinorVersion, dwSpMajorVersion, dwSpMinorVersion, out pdwReturnedProductType);
            return !res ? throw new InvalidOperationException("Failed to get product info.") : res;
        }

        /// <summary>
        /// Waits for the specified object to enter a signaled state or for the specified timeout interval to elapse.
        /// </summary>
        /// <param name="hHandle">A handle to the object to wait for. This handle must be valid and cannot be null.</param>
        /// <param name="dwMilliseconds">The time-out interval, in milliseconds. Specify <see langword="uint.MaxValue"/> to wait indefinitely.</param>
        /// <returns>A <see cref="WAIT_EVENT"/> value indicating the result of the wait operation. Possible values include <see
        /// cref="WAIT_EVENT.WAIT_OBJECT_0"/> for a signaled state, <see cref="WAIT_EVENT.WAIT_TIMEOUT"/> for a timeout,
        /// or <see cref="WAIT_EVENT.WAIT_ABANDONED"/> for an abandoned mutex.</returns>
        internal static WAIT_EVENT WaitForSingleObject(SafeHandle hHandle, uint dwMilliseconds)
        {
            WAIT_EVENT res = PInvoke.WaitForSingleObject(hHandle, dwMilliseconds);
            return res == WAIT_EVENT.WAIT_FAILED ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : res;
        }

        /// <summary>
        /// Defines, modifies, or deletes a symbolic link (DOS device name) in the system's device namespace.
        /// </summary>
        /// <remarks>This method wraps the Windows DefineDosDevice API and throws an exception if the
        /// operation fails. Use the appropriate flags to control the behavior of the symbolic link. Administrative
        /// privileges may be required to modify certain device mappings.</remarks>
        /// <param name="dwFlags">A combination of flags that specify the operation to perform and how the symbolic link is handled. These
        /// flags determine whether to create, modify, or remove the mapping, and may affect how the target path is
        /// interpreted.</param>
        /// <param name="lpDeviceName">The name of the DOS device (symbolic link) to define, modify, or delete. This value cannot be null or empty.</param>
        /// <param name="lpTargetPath">The target path for the symbolic link. This parameter is required when creating or modifying a mapping, and
        /// should be null when deleting a mapping.</param>
        /// <returns>A value indicating whether the operation succeeded. Returns <see langword="true"/> if the symbolic link was
        /// defined, modified, or deleted successfully; otherwise, <see langword="false"/>.</returns>
        internal static BOOL DefineDosDevice(DEFINE_DOS_DEVICE_FLAGS dwFlags, string lpDeviceName, string? lpTargetPath)
        {
            BOOL res = PInvoke.DefineDosDevice(dwFlags, lpDeviceName, lpTargetPath);
            return !res ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : res;
        }

        /// <summary>
        /// Retrieves the type of the specified file or device handle.
        /// </summary>
        /// <remarks>If the underlying system call fails, an exception is thrown. This method wraps the
        /// native GetFileType function and provides error handling for Win32 errors.</remarks>
        /// <param name="hFile">A safe handle to the file or device whose type is to be determined. The handle must be valid and open.</param>
        /// <returns>A value of the FILE_TYPE enumeration that indicates the type of the specified handle. Returns
        /// FILE_TYPE.FILE_TYPE_UNKNOWN if the type cannot be determined and no error occurred.</returns>
        internal static FILE_TYPE GetFileType(SafeHandle hFile)
        {
            FILE_TYPE res = PInvoke.GetFileType(hFile);
            return res == FILE_TYPE.FILE_TYPE_UNKNOWN && ExceptionUtilities.GetLastWin32Error() is WIN32_ERROR lastWin32Error && lastWin32Error != WIN32_ERROR.NO_ERROR
                ? throw ExceptionUtilities.GetException(lastWin32Error)
                : res;
        }

        /// <summary>
        /// Retrieves the file attributes for the specified file or directory path.
        /// </summary>
        /// <param name="lpFileName">The full path to the file or directory for which to retrieve attributes. This parameter cannot be null or an
        /// empty string.</param>
        /// <returns>A value of type FileAttributes that describes the attributes of the specified file or directory.</returns>
        internal static FileAttributes GetFileAttributes(string lpFileName)
        {
            uint res = PInvoke.GetFileAttributes(lpFileName);
            return res == PInvoke.INVALID_FILE_ATTRIBUTES ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : (FileAttributes)res;
        }

        /// <summary>
        /// Retrieves the full name of the executable image for the specified process.
        /// </summary>
        /// <remarks>This method wraps the Windows API QueryFullProcessImageName function. The caller is
        /// responsible for ensuring that the buffer provided by lpExeName is sufficiently large to hold the full path.
        /// If the buffer is too small, an exception is thrown.</remarks>
        /// <param name="hProcess">A handle to the process. The handle must have the PROCESS_QUERY_LIMITED_INFORMATION access right.</param>
        /// <param name="dwFlags">A value that specifies the format for the returned process name. This determines whether the name is in
        /// Win32 or native format.</param>
        /// <param name="lpExeName">A span of characters that receives the full path to the executable file. The buffer must be large enough to
        /// receive the path.</param>
        /// <param name="lpdwSize">When this method returns, contains the number of characters written to lpExeName, not including the null
        /// terminator.</param>
        /// <returns>A nonzero value if the function succeeds; otherwise, an exception is thrown.</returns>
        internal static BOOL QueryFullProcessImageName(SafeHandle hProcess, PROCESS_NAME_FORMAT dwFlags, Span<char> lpExeName, out uint lpdwSize)
        {
            lpdwSize = (uint)lpExeName.Length;
            BOOL res = PInvoke.QueryFullProcessImageName(hProcess, dwFlags, lpExeName, ref lpdwSize);
            return !res ? throw ExceptionUtilities.GetExceptionForLastWin32Error() : res;
        }

        /// <summary>
        /// Formats a message string based on a specified message identifier and language, using the provided formatting
        /// options and arguments.
        /// </summary>
        /// <remarks>This method is a managed wrapper for the Windows FormatMessage function. It is
        /// typically used to retrieve system error messages or custom messages defined in a resource module. The caller
        /// is responsible for providing a sufficiently sized buffer to receive the formatted message.</remarks>
        /// <param name="dwFlags">A set of formatting options that control the behavior of the message formatting. These options determine the
        /// source of the message definition and how the output is processed.</param>
        /// <param name="lpSource">An optional handle to a module that contains the message resource definition. If null, the system message
        /// table resource is used.</param>
        /// <param name="dwMessageId">The identifier for the message to be formatted. This value specifies which message definition to use.</param>
        /// <param name="lpBuffer">A span of characters that receives the formatted message string. The buffer must be large enough to hold the
        /// resulting message.</param>
        /// <param name="dwLanguageId">The language identifier that specifies the language of the message. This determines which localized message
        /// is retrieved.</param>
        /// <param name="Arguments">A pointer to an array of arguments to be inserted into the message. Can be null if the message does not
        /// require arguments.</param>
        /// <returns>The number of characters stored in the output buffer, excluding the terminating null character.</returns>
        /// <exception cref="Win32Exception">Thrown if the message formatting operation fails.</exception>
        internal static uint FormatMessage(FORMAT_MESSAGE_OPTIONS dwFlags, [Optional] FreeLibrarySafeHandle? lpSource, uint dwMessageId, Span<char> lpBuffer, uint dwLanguageId = 0, in nint Arguments = default)
        {
            uint res;
            unsafe
            {
                bool lpSourceAddRef = false;
                try
                {
                    lpSource?.DangerousAddRef(ref lpSourceAddRef);
                    res = PInvoke.FormatMessage(dwFlags, lpSource is not null ? (void*)lpSource.DangerousGetHandle() : null, dwMessageId, dwLanguageId, lpBuffer, (uint)lpBuffer.Length, (sbyte*)Arguments);

                }
                finally
                {
                    if (lpSourceAddRef)
                    {
                        lpSource!.DangerousRelease();
                    }
                }
            }
            return res == 0 ? throw new Win32Exception() : res;
        }
    }
}
