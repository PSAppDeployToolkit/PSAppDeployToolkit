using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PSADT.LibraryInterfaces;
using PSADT.Types;
using PSADT.Shared;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Security;
using Windows.Win32.System.JobObjects;
using Windows.Win32.System.Threading;
using Windows.Win32.UI.WindowsAndMessaging;

namespace PSADT.ProcessEx
{
    /// <summary>
    /// Provides methods for launching processes with more control over input/output.
    /// </summary>
    public static class ProcessExecutor
    {
        /// <summary>
        /// Launches a process with the specified start info and waits for it to complete.
        /// </summary>
        /// <param name="startInfo"></param>
        /// <returns></returns>
        /// <exception cref="TaskCanceledException"></exception>
        public static async Task<ProcessResult> LaunchAsync(ProcessOptions startInfo)
        {
            // Declare all variables C-style so we can close them in the finally block.
            HANDLE hStdOutWrite = default;
            HANDLE hStdErrWrite = default;
            HANDLE hStdOutRead = default;
            HANDLE hStdErrRead = default;
            HANDLE hStdInWrite = default;
            HANDLE hStdInRead = default;
            HANDLE hProcess = default;
            HANDLE iocp = default;
            HANDLE job = default;

            try
            {
                iocp = Kernel32.CreateIoCompletionPort(HANDLE.INVALID_HANDLE_VALUE, HANDLE.Null, UIntPtr.Zero, 1);
                job = Kernel32.CreateJobObject(out _, default);

                var assoc = new JOBOBJECT_ASSOCIATE_COMPLETION_PORT
                {
                    CompletionPort = iocp,
                    CompletionKey = null,
                };
                Kernel32.SetInformationJobObject(job, JOBOBJECTINFOCLASS.JobObjectAssociateCompletionPortInformation, ref assoc, (uint)Marshal.SizeOf<JOBOBJECT_ASSOCIATE_COMPLETION_PORT>());

                if (!startInfo.UseShellExecute)
                {
                    CreatePipe(out hStdOutRead, out hStdOutWrite, true);
                    CreatePipe(out hStdErrRead, out hStdErrWrite, true);
                    CreatePipe(out hStdInRead, out hStdInWrite, false);

                    var startupInfo = new STARTUPINFOW
                    {
                        cb = (uint)Marshal.SizeOf<STARTUPINFOW>(),
                        hStdOutput = hStdOutWrite,
                        hStdError = hStdErrWrite,
                        hStdInput = hStdInRead,
                    };

                    var creationFlags = PROCESS_CREATION_FLAGS.CREATE_UNICODE_ENVIRONMENT |
                        PROCESS_CREATION_FLAGS.CREATE_SUSPENDED |
                        PROCESS_CREATION_FLAGS.CREATE_NEW_PROCESS_GROUP;

                    if (!startInfo.NoNewWindow && ((SHOW_WINDOW_CMD)startInfo.WindowStyle != SHOW_WINDOW_CMD.SW_HIDE))
                    {
                        startupInfo.dwFlags = STARTUPINFOW_FLAGS.STARTF_USESHOWWINDOW;
                        startupInfo.wShowWindow = startInfo.WindowStyle;
                        if (GeneralUtilities.GetExecutableInfo(startInfo.FilePath).ExecutableType == ExecutableType.Console)
                        {
                            creationFlags |= PROCESS_CREATION_FLAGS.CREATE_NEW_CONSOLE;
                        }
                        else
                        {
                            creationFlags |= PROCESS_CREATION_FLAGS.DETACHED_PROCESS;
                        }
                    }
                    else
                    {
                        startupInfo.dwFlags = STARTUPINFOW_FLAGS.STARTF_USESTDHANDLES;
                        creationFlags |= PROCESS_CREATION_FLAGS.CREATE_NO_WINDOW;
                        creationFlags |= PROCESS_CREATION_FLAGS.DETACHED_PROCESS;
                    }

                    Kernel32.CreateProcess(startInfo.FilePath, startInfo.GetArgsForCreateProcess(), null, null, true, creationFlags, IntPtr.Zero, startInfo.WorkingDirectory, startupInfo, out var pi);
                    Kernel32.AssignProcessToJobObject(job, (hProcess = pi.hProcess));
                    Kernel32.ResumeThread(pi.hThread);
                    Kernel32.CloseHandle(ref pi.hThread);
                    Kernel32.CloseHandle(ref hStdOutWrite);
                    Kernel32.CloseHandle(ref hStdErrWrite);
                    Kernel32.CloseHandle(ref hStdInRead);
                }
                else
                {
                    var startupInfo = new Shell32.SHELLEXECUTEINFO
                    {
                        cbSize = Marshal.SizeOf<Shell32.SHELLEXECUTEINFO>(),
                        fMask = SEE_MASK_FLAGS.SEE_MASK_NOCLOSEPROCESS | SEE_MASK_FLAGS.SEE_MASK_FLAG_NO_UI,
                        lpFile = startInfo.FilePath,
                        lpParameters = startInfo.GetArgsForShellExecuteEx(),
                        lpDirectory = startInfo.WorkingDirectory,
                    };
                    if (startInfo.NoNewWindow || ((SHOW_WINDOW_CMD)startInfo.WindowStyle == SHOW_WINDOW_CMD.SW_HIDE))
                    {
                        startupInfo.fMask |= SEE_MASK_FLAGS.SEE_MASK_NO_CONSOLE;
                        startupInfo.nShow = (int)SHOW_WINDOW_CMD.SW_HIDE;
                    }
                    else
                    {
                        startupInfo.nShow = startInfo.WindowStyle;
                    }
                    Shell32.ShellExecuteEx(ref startupInfo);
                    if (startupInfo.hProcess != IntPtr.Zero)
                    {
                        Kernel32.AssignProcessToJobObject(job, (hProcess = (HANDLE)startupInfo.hProcess));
                    }
                }

                List<string> stdout = []; List<string> stderr = [];
                Task readOut = (hStdOutRead != default) ? Task.Run(() => ReadPipe(hStdOutRead, stdout, startInfo.CancellationToken)) : Task.CompletedTask;
                Task readErr = (hStdErrRead != default) ? Task.Run(() => ReadPipe(hStdErrRead, stderr, startInfo.CancellationToken)) : Task.CompletedTask;
                Task writeIn = (null != startInfo.StandardInput) ? Task.Run(() => WritePipe(hStdInWrite, startInfo.StandardInput!, startInfo.CancellationToken)) : Task.CompletedTask;
                Task waitForJob = (hProcess == default) ? Task.CompletedTask : Task.Run(() =>
                {
                    while (true)
                    {
                        Kernel32.GetQueuedCompletionStatus(iocp, out var lpCompletionCode, out _, out _, PInvoke.INFINITE);
                        if (lpCompletionCode == PInvoke.JOB_OBJECT_MSG_ACTIVE_PROCESS_ZERO)
                        {
                            break;
                        }
                        if (startInfo.CancellationToken.IsCancellationRequested)
                        {
                            throw new TaskCanceledException();
                        }
                    }
                });
                await Task.WhenAll(waitForJob, readOut, readErr, writeIn);

                uint exitCode = 0;
                if (hProcess != default)
                {
                    Kernel32.GetExitCodeProcess(hProcess, out exitCode);
                }
                return new ProcessResult(ValueTypeConverter<int>.Convert(exitCode), stdout.AsReadOnly(), stderr.AsReadOnly());
            }
            finally
            {
                Kernel32.CloseHandle(ref hStdOutWrite);
                Kernel32.CloseHandle(ref hStdErrWrite);
                Kernel32.CloseHandle(ref hStdOutRead);
                Kernel32.CloseHandle(ref hStdErrRead);
                Kernel32.CloseHandle(ref hStdInWrite);
                Kernel32.CloseHandle(ref hStdInRead);
                Kernel32.CloseHandle(ref hProcess);
                Kernel32.CloseHandle(ref iocp);
                Kernel32.CloseHandle(ref job);
            }
        }

        /// <summary>
        /// Launches a process with the specified start info and waits for it to complete.
        /// </summary>
        /// <param name="startInfo"></param>
        /// <returns></returns>
        public static ProcessResult Launch(ProcessOptions startInfo)
        {
            return LaunchAsync(startInfo).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Creates a pipe for reading or writing.
        /// </summary>
        /// <param name="readPipe"></param>
        /// <param name="writePipe"></param>
        /// <param name="readFromChild"></param>
        /// <exception cref="Win32Exception"></exception>
        private static void CreatePipe(out HANDLE readPipe, out HANDLE writePipe, bool readFromChild)
        {
            Kernel32.CreatePipe(out readPipe, out writePipe, new SECURITY_ATTRIBUTES { nLength = (uint)Marshal.SizeOf<SECURITY_ATTRIBUTES>(), bInheritHandle = true }, 0);
            Kernel32.SetHandleInformation(readFromChild ? readPipe : writePipe, (uint)HANDLE_FLAGS.HANDLE_FLAG_INHERIT, 0);
        }

        /// <summary>
        /// Reads from a pipe until the pipe is closed or the token is cancelled.
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="output"></param>
        /// <param name="token"></param>
        /// <exception cref="Win32Exception"></exception>
        private static void ReadPipe(HANDLE handle, List<string> output, CancellationToken token)
        {
            var buffer = new byte[4096];
            while (!token.IsCancellationRequested)
            {
                Kernel32.ReadFile(handle, buffer, out var bytesRead, out _);
                if (bytesRead == 0)
                {
                    break;
                }
                output.Add(Encoding.Default.GetString(buffer, 0, (int)bytesRead).TrimEnd());
            }
        }

        /// <summary>
        /// Writes to a pipe.
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="input"></param>
        /// <param name="token"></param>
        private static void WritePipe(HANDLE handle, string input, CancellationToken token)
        {
            Kernel32.WriteFile(handle, Encoding.Default.GetBytes(input), out _, out _);
        }
    }
}
