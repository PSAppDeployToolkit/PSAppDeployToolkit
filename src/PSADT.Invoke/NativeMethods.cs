using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using Windows.Wdk.System.Threading;
using Windows.Win32.Foundation;
using Windows.Win32.System.Threading;
using Windows.Win32.UI.WindowsAndMessaging;

namespace PSADT.Invoke
{
    internal static class NativeMethods
    {
        /// <summary>
        /// Retrieves basic information about the specified process using the native NtQueryInformationProcess API.
        /// </summary>
        /// <remarks>This method wraps the native NtQueryInformationProcess function to obtain basic
        /// process information, such as the process ID and parent process ID. The method converts NTSTATUS error codes
        /// to Win32 error codes for exception handling.</remarks>
        /// <param name="ProcessHandle">A handle to the process for which information is to be retrieved. The handle must have appropriate access
        /// rights, such as PROCESS_QUERY_INFORMATION.</param>
        /// <param name="ProcessBasicInformation">When this method returns, contains a PROCESS_BASIC_INFORMATION structure with information about the
        /// specified process.</param>
        /// <returns>An NTSTATUS code indicating the result of the operation. A value of 0 indicates success.</returns>
        /// <exception cref="Win32Exception">Thrown if the underlying NtQueryInformationProcess call fails. The exception's error code corresponds to the
        /// converted NTSTATUS value.</exception>
        internal static int NtQueryInformationProcess(SafeHandle ProcessHandle, out PROCESS_BASIC_INFORMATION ProcessBasicInformation)
        {
            uint ReturnLengthLocal = 0; NTSTATUS res;
            bool ProcessHandleAddRef = false;
            try
            {
                ProcessHandle.DangerousAddRef(ref ProcessHandleAddRef);
                unsafe
                {
                    fixed (PROCESS_BASIC_INFORMATION* pProcessBasicInformation = &ProcessBasicInformation)
                    {
                        res = Windows.Wdk.PInvoke.NtQueryInformationProcess((HANDLE)ProcessHandle.DangerousGetHandle(), PROCESSINFOCLASS.ProcessBasicInformation, pProcessBasicInformation, (uint)Marshal.SizeOf<PROCESS_BASIC_INFORMATION>(), ref ReturnLengthLocal);
                    }
                }
            }
            finally
            {
                if (ProcessHandleAddRef)
                {
                    ProcessHandle.DangerousRelease();
                }
            }
            return res != NTSTATUS.STATUS_SUCCESS ? throw new Win32Exception((int)Windows.Win32.PInvoke.RtlNtStatusToDosError(res)) : (int)res;
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
        /// <exception cref="Win32Exception">Thrown if the underlying OpenProcess system call fails. The exception's error code corresponds to the Win32 error code returned by the system.</exception>
        internal static SafeFileHandle OpenProcess(PROCESS_ACCESS_RIGHTS dwDesiredAccess, in BOOL bInheritHandle, uint dwProcessId)
        {
            SafeFileHandle res = Windows.Win32.PInvoke.OpenProcess_SafeHandle(dwDesiredAccess, bInheritHandle, dwProcessId);
            return res.IsInvalid ? throw new Win32Exception() : res;
        }

        /// <summary>
        /// Allocates a new console for the calling process.
        /// </summary>
        /// <remarks>This method is typically used by applications that do not have a console by default,
        /// such as Windows Forms or WPF applications, to enable console input and output. If a console is already
        /// allocated for the process, this method will fail.</remarks>
        /// <returns>true if the console was successfully allocated; otherwise, false.</returns>
        /// <exception cref="Win32Exception">Thrown if the underlying system call fails. The exception's error code corresponds to the Win32 error code
        /// returned by the system.</exception>
        internal static BOOL AllocConsole()
        {
            BOOL res = Windows.Win32.PInvoke.AllocConsole();
            return !res ? throw new Win32Exception() : res;
        }

        /// <summary>
        /// Retrieves the window handle for the current console window.
        /// </summary>
        /// <remarks>This method is typically used to obtain the native window handle for interop
        /// scenarios or advanced console manipulation. The returned handle is valid only if the process has an
        /// associated console window.</remarks>
        /// <returns>An <see cref="nint"/> that represents the handle to the console window. If the process does not have a
        /// console window, an exception is thrown.</returns>
        /// <exception cref="Win32Exception">Thrown if the handle to the console window cannot be retrieved.</exception>
        internal static HWND GetConsoleWindow()
        {
            HWND res = Windows.Win32.PInvoke.GetConsoleWindow();
            return res.IsNull ? throw new Win32Exception(6) : res;
        }

        /// <summary>
        /// Detaches the calling process from its console, if one is attached.
        /// </summary>
        /// <remarks>After calling this method, the process will no longer have access to its console
        /// window. Subsequent attempts to write to or read from the console may fail until a new console is allocated
        /// or attached.</remarks>
        /// <returns>true if the process was successfully detached from its console; otherwise, false.</returns>
        /// <exception cref="Win32Exception">Thrown if the underlying system call fails. The exception's error code corresponds to the Win32 error code
        /// returned by the operating system.</exception>
        internal static BOOL FreeConsole()
        {
            BOOL res = Windows.Win32.PInvoke.FreeConsole();
            return !res ? throw new Win32Exception() : res;
        }

        /// <summary>
        /// Displays a message box with the specified text, caption, and style, and returns the user's response.
        /// </summary>
        /// <remarks>This method wraps the Windows API <c>MessageBox</c> function and provides additional
        /// error handling. Ensure that the calling thread has a message loop if the message box is displayed in a GUI
        /// application.</remarks>
        /// <param name="hWnd">A handle to the owner window of the message box. Pass <see langword="null"/> to display the message box
        /// without an owner.</param>
        /// <param name="lpText">The text to display in the message box. Cannot be <see langword="null"/> or empty.</param>
        /// <param name="lpCaption">The caption to display in the title bar of the message box. Cannot be <see langword="null"/> or empty.</param>
        /// <param name="uType">The style of the message box, specified as a combination of <see cref="MESSAGEBOX_STYLE"/> flags.</param>
        /// <returns>A <see cref="MESSAGEBOX_RESULT"/> value indicating the button pressed by the user in response to the message
        /// box.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="lpText"/> is <see langword="null"/> or empty, or if <paramref name="lpCaption"/>
        /// is <see langword="null"/> or empty.</exception>
        /// <exception cref="Win32Exception">Thrown if the message box fails to display due to a Windows API error.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "MA0099:Use Explicit enum value instead of 0", Justification = "There's no zero value defined.")]
        internal static MESSAGEBOX_RESULT MessageBox(HWND? hWnd, string lpText, string lpCaption, MESSAGEBOX_STYLE uType)
        {
            if (string.IsNullOrWhiteSpace(lpText))
            {
                throw new ArgumentNullException(nameof(lpText), "Message text cannot be null or empty.");
            }
            if (string.IsNullOrWhiteSpace(lpCaption))
            {
                throw new ArgumentNullException(nameof(lpCaption), "Caption cannot be null or empty.");
            }
            MESSAGEBOX_RESULT res = Windows.Win32.PInvoke.MessageBox(hWnd ?? HWND.Null, lpText, lpCaption, uType);
            return res == 0 ? throw new Win32Exception() : res;
        }
    }
}
