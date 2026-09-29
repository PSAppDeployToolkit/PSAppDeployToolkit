using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.Pipes;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using PSADT.AccountManagement;
using PSADT.Foundation;
using PSADT.Interop;
using PSADT.Interop.SafeHandles;
using PSADT.SafeHandles;
using PSADT.Security;
using Windows.Win32.Foundation;
using Windows.Win32.System.JobObjects;
using Windows.Win32.System.Threading;
using Windows.Win32.UI.Shell;

namespace PSADT.ProcessManagement
{
    /// <summary>
    /// Provides methods for launching processes with more control over input/output.
    /// </summary>
    public static class ProcessManager
    {
        /// <summary>
        /// Special exit code used to signal when we're terminating a process due to timeout.
        /// The value is `'PSAppDeployToolkit'.GetHashCode()` under Windows PowerShell 5.1.
        /// </summary>
        public const int TimeoutExitCode = -443_991_205;

        /// <summary>
        /// Launches a new process asynchronously using the specified launch configuration and returns a handle for
        /// process management and monitoring.
        /// </summary>
        /// <remarks>This method supports advanced process launching scenarios, such as running as a
        /// different user, redirecting standard input/output streams, setting process priority, and managing child
        /// processes. It also allows for process cancellation and custom access control. The returned ProcessHandle
        /// enables monitoring process completion and retrieving output streams asynchronously.</remarks>
        /// <param name="launchInfo">An object that specifies the parameters for launching the process, including command line arguments, window
        /// style, user context, input/output redirection, process priority, and other process control options. Cannot
        /// be null.</param>
        /// <returns>A ProcessHandle object that provides access to the launched process and its associated asynchronous task, or
        /// null if the process could not be started.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="launchInfo"/> is null.</exception>
        public static ProcessHandle? LaunchAsync(ProcessLaunchInfo launchInfo)
        {
            // Only use ShellExecuteEx for non-console apps if we're not capturing stdio.
            ArgumentNullException.ThrowIfNull(launchInfo);
            return launchInfo.UseShellExecute && (!launchInfo.IsCliApplication() || !launchInfo.CreateNoWindow)
                ? LaunchWithShellExecuteExAsync(launchInfo)
                : LaunchWithCreateProcessAsync(launchInfo);
        }

        /// <summary>
        /// Launches a new process asynchronously using the Windows CreateProcess API, applying the specified launch
        /// options and user context.
        /// </summary>
        /// <remarks>This method supports advanced process launch scenarios, such as running under a
        /// different user token, customizing standard input/output/error streams, and controlling window and console
        /// behavior. The process is initially created in a suspended state and then resumed after setup. Callers are
        /// responsible for disposing of the returned ProcessHandle to release system resources.</remarks>
        /// <param name="launchInfo">An object containing the configuration and parameters for the process to be launched, including file path,
        /// arguments, user context, environment variables, and standard stream handling.</param>
        /// <returns>A handle to the launched process, encapsulated in a ProcessHandle object, which provides access to process
        /// state and standard streams as configured.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the process cannot be started.</exception>
        /// <exception cref="NotSupportedException">Thrown if the specified user context is not supported for process creation.</exception>
        private static ProcessHandle LaunchWithCreateProcessAsync(ProcessLaunchInfo launchInfo)
        {
            // Launch the process using the CreateProcess API and return a handle to the caller.
            ReadOnlyCollection<SE_PRIVILEGE> callerPrivileges = PrivilegeManager.GetPrivileges();
            (SafeProcessHandle hProcess, SafeThreadHandle hThread, uint dwProcessId, string commandLine) = CreateProcessApi.CreateProcess(launchInfo, callerPrivileges, out (ProcessReadStream StdOutHandle, ProcessReadStream StdErrHandle, IReadOnlyCollection<string> InterleavedBuffer)? stdOutErrHandles, out ProcessWriteStream? stdInHandle);
            bool resumed = false;
            try
            {
                if (launchInfo.BypassIfeo)
                {
                    _ = NativeMethods.DebugActiveProcessStop(dwProcessId);
                }
                return new(launchInfo, hProcess, dwProcessId, commandLine, callerPrivileges, stdOutErrHandles, stdInHandle, () =>
                {
                    using (hThread)
                    {
                        _ = NativeMethods.ResumeThread(hThread);
                        resumed = true;
                    }
                });
            }
            catch (Exception ex)
            {
                using (stdOutErrHandles?.StdOutHandle)
                using (stdOutErrHandles?.StdErrHandle)
                using (stdInHandle)
                using (hProcess)
                using (hThread)
                {
                    if (!resumed)
                    {
                        TerminateFailedLaunch(hProcess);
                    }
                    ExceptionDispatchInfo.Capture(ex).Throw();
                    throw;
                }
            }
        }

        /// <summary>
        /// Starts a new process using ShellExecuteEx with the specified launch parameters and returns a handle to the
        /// created process, or null if the operation is a pure shell action.
        /// </summary>
        /// <remarks>Where a job object or access control is wanted and the shell creates the process itself, it is held
        /// suspended until they are in place, so nothing it starts can escape them. The caller is responsible for disposing
        /// of the returned ProcessHandle when it is no longer needed.</remarks>
        /// <param name="launchInfo">An object containing the parameters required to launch the process, including file path, arguments, working
        /// directory, window style, and other process options.</param>
        /// <returns>A handle to the started process if the process was successfully created; otherwise, null if the operation
        /// was a pure shell action and no process was started.</returns>
        /// <exception cref="NotSupportedException">Thrown if the RunAsActiveUser property of launchInfo is set, as running as a different user is not supported
        /// with ShellExecuteEx.</exception>
        private static ProcessHandle? LaunchWithShellExecuteExAsync(ProcessLaunchInfo launchInfo)
        {
            // Throw if RunAsActiveUser is populated as it's not supported.
            if (!(launchInfo.RunAsActiveUser is null || launchInfo.RunAsActiveUser == AccountUtilities.CallerRunAsActiveUser))
            {
                throw new NotSupportedException("Running as a different user is not supported with ShellExecuteEx.");
            }

            // Launch through the shell. A pure shell action, such as a document handed to a running application, yields no process.
            (SafeProcessHandle? hProcess, bool suspended) = ShellExecuteExApi.ShellExecuteEx(launchInfo);
            if (hProcess is null)
            {
                ClientServerUtilities.SetOperationSuccessFlag();
                return null;
            }

            // Finalise the process creation, ending the process if it can't be handed back.
            try
            {
                return new(launchInfo, hProcess, NativeMethods.GetProcessId(hProcess), launchInfo.MakeCommandLine(), () =>
                {
                    if (suspended)
                    {
                        _ = NativeMethods.NtResumeProcess(hProcess);
                        suspended = false;
                    }
                });
            }
            catch (Exception ex)
            {
                using (hProcess)
                {
                    if (suspended)
                    {
                        TerminateFailedLaunch(hProcess);
                    }
                    ExceptionDispatchInfo.Capture(ex).Throw();
                    throw;
                }
            }
        }

        /// <summary>
        /// Ends the process of a launch that failed after creating it, so the failure never leaves it running or suspended.
        /// </summary>
        /// <param name="hProcess">The process to end.</param>
        private static void TerminateFailedLaunch(SafeProcessHandle hProcess)
        {
            try
            {
                _ = NativeMethods.TerminateProcess(hProcess, uint.MaxValue);
            }
            catch
            {
                return;
                throw;
            }
        }

        /// <summary>
        /// Contains helper methods for creating processes using the Windows CreateProcess API.
        /// </summary>
        private static class CreateProcessApi
        {
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "This is a false positive, we're directly consuming the ValueTask.")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD002:Avoid problematic synchronous waits", Justification = "We cannot refactor this method to be async at this stage.")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0045:Do not use blocking calls, even when the calling method must become async", Justification = "VSTHRD002")]
            internal static (SafeProcessHandle, SafeThreadHandle, uint, string) CreateProcess(ProcessLaunchInfo launchInfo, ReadOnlyCollection<SE_PRIVILEGE> callerPrivileges, out (ProcessReadStream StdOut, ProcessReadStream StdErr, IReadOnlyCollection<string> InterleavedBuffer)? stdOutErrHandles, out ProcessWriteStream? stdInHandle)
            {
                // Perform initial setup and get started with the process creation.
                stdOutErrHandles = null; stdInHandle = null;
                try
                {
                    // Set up the startup information for the process.
                    PROCESS_INFORMATION pi; STARTUPINFOW startupInfo = new()
                    {
                        cb = (uint)Unsafe.SizeOf<STARTUPINFOW>(),
                    };
                    PROCESS_CREATION_FLAGS creationFlags = ((PROCESS_CREATION_FLAGS?)launchInfo.PriorityClass ?? 0) |
                        (launchInfo.BypassIfeo ? PROCESS_CREATION_FLAGS.DEBUG_ONLY_THIS_PROCESS : 0) |
                        PROCESS_CREATION_FLAGS.CREATE_UNICODE_ENVIRONMENT |
                        PROCESS_CREATION_FLAGS.CREATE_NEW_PROCESS_GROUP |
                        PROCESS_CREATION_FLAGS.CREATE_SEPARATE_WOW_VDM |
                        PROCESS_CREATION_FLAGS.CREATE_SUSPENDED;

                    // Set up the window style if the caller's provided a value.
                    if (launchInfo.WindowStyle?.ToShowWindowCmd() is SHOW_WINDOW_CMD wShowWindow)
                    {
                        startupInfo.wShowWindow = (ushort)wShowWindow;
                        startupInfo.dwFlags |= STARTUPINFOW_FLAGS.STARTF_USESHOWWINDOW;
                    }

                    // We must create a console window for console apps when the window is shown.
                    if (launchInfo.IsCliApplication())
                    {
                        if (launchInfo.CreateNoWindow)
                        {
                            // If STARTF_USESHOWWINDOW is set, a console app showing UI elements
                            // won't appear. Because we have CREATE_NO_WINDOW, the console window
                            // (aka. the window we actually want hidden) will be hidden as expected.
                            startupInfo.dwFlags |= STARTUPINFOW_FLAGS.STARTF_USESTDHANDLES;
                            startupInfo.dwFlags &= ~STARTUPINFOW_FLAGS.STARTF_USESHOWWINDOW;
                            creationFlags |= PROCESS_CREATION_FLAGS.CREATE_NO_WINDOW;
                        }
                        else
                        {
                            creationFlags |= PROCESS_CREATION_FLAGS.CREATE_NEW_CONSOLE;
                        }
                    }

                    // Set up required stdio stuff if we're configured to capture these streams.
                    List<nint> handlesToInherit = [.. launchInfo.HandlesToInherit]; bool hasExternalHandles = handlesToInherit.Count > 0;
                    if (startupInfo.dwFlags.HasFlag(STARTUPINFOW_FLAGS.STARTF_USESTDHANDLES))
                    {
                        ConcurrentQueue<string> interleavedData = [];
                        if (launchInfo.StandardInput.Count > 0)
                        {
                            (startupInfo.hStdInput, stdInHandle) = CreateWritePipe(launchInfo.StandardInput, launchInfo.StreamEncoding);
                            handlesToInherit.Add(startupInfo.hStdInput);
                        }
                        (startupInfo.hStdOutput, ProcessReadStream stdOutHandle) = CreateReadPipe(interleavedData, launchInfo.StreamEncoding);
                        (startupInfo.hStdError, ProcessReadStream stdErrHandle) = CreateReadPipe(interleavedData, launchInfo.StreamEncoding);
                        handlesToInherit.Add(startupInfo.hStdOutput); handlesToInherit.Add(startupInfo.hStdError);
                        stdOutErrHandles = (stdOutHandle, stdErrHandle, interleavedData);
                    }

                    // Attempt to launch the process with the specified user's token if the necessary information was provided, otherwise just directly create the process.
                    Span<char> commandSpan = launchInfo.MakeCommandLine(nullTerminated: true).ToCharArray();
                    if (launchInfo.RunAsActiveUser?.Equals(AccountUtilities.CallerRunAsActiveUser) is false)
                    {
                        // Start the process with the user's token. Without creating an environment block, the process will take on the environment of the SYSTEM account.
                        if (!TokenManager.CanGetUserPrimaryToken(launchInfo.RunAsActiveUser.SessionId))
                        {
                            throw new NotSupportedException("Cannot retrieve the necessary user token as no acquisition route is available in this execution context.");
                        }
                        using SafeFileHandle hPrimaryToken = TokenManager.GetUserPrimaryTokenAsync(launchInfo.RunAsActiveUser, launchInfo.ElevatedTokenType ?? ElevatedTokenType.None, launchInfo.UIAccess).ConfigureAwait(false).GetAwaiter().GetResult();
                        _ = NativeMethods.CreateEnvironmentBlock(out SafeEnvironmentBlockHandle lpEnvironment, hPrimaryToken, launchInfo.InheritEnvironmentVariables);
                        using (lpEnvironment)
                        {
                            unsafe
                            {
                                fixed (char* pDesktop = @"winsta0\default")
                                {
                                    startupInfo.lpDesktop = new(pDesktop);
                                    _ = CreateProcessUsingToken(hPrimaryToken, callerPrivileges, launchInfo.FilePath, ref commandSpan, handlesToInherit, hasExternalHandles, creationFlags, lpEnvironment, launchInfo.WorkingDirectory?.FullName, launchInfo.RunAsInvoker, in startupInfo, out pi);
                                }
                            }
                        }
                    }
                    else if (AccountUtilities.CallerIsAdmin && (launchInfo.ElevatedTokenType is ElevatedTokenType.None || (launchInfo.UIAccess && AccountUtilities.CallerIsLoggedOnUser && TokenManager.CanGetUserPrimaryToken(AccountUtilities.CallerSessionId) && ((!hasExternalHandles && CanUseCreateProcessWithToken(isCallerToken: true, callerPrivileges, commandSpan) is CreateProcessUsingTokenStatus.OK) || CanUseCreateProcessAsUser(isCallerToken: true, callerPrivileges) is CreateProcessUsingTokenStatus.OK))))
                    {
                        // We're running elevated but have been asked to de-elevate.
                        if (!AccountUtilities.CallerIsLoggedOnUser)
                        {
                            throw new InvalidOperationException("Cannot create process using unelevated token when running in a different user's session.");
                        }
                        if (!TokenManager.CanGetUserPrimaryToken(AccountUtilities.CallerSessionId))
                        {
                            throw new NotSupportedException("Cannot retrieve the necessary user token as no acquisition route is available in this execution context.");
                        }
                        using SafeFileHandle hPrimaryToken = TokenManager.GetUserPrimaryTokenAsync(AccountUtilities.CallerRunAsActiveUser, launchInfo.ElevatedTokenType ?? ElevatedTokenType.HighestMandatory, launchInfo.UIAccess).ConfigureAwait(false).GetAwaiter().GetResult();
                        _ = CreateProcessUsingToken(hPrimaryToken, callerPrivileges, launchInfo.FilePath, ref commandSpan, handlesToInherit, hasExternalHandles, creationFlags, lpEnvironment: null, launchInfo.WorkingDirectory?.FullName, launchInfo.RunAsInvoker, in startupInfo, out pi);
                    }
                    else
                    {
                        // No username was specified and we weren't asked to de-elevate, so we're just creating the process as this current user as-is.
                        if (launchInfo.RunAsInvoker || handlesToInherit.Count > 0)
                        {
                            (STARTUPINFOEXW startupInfoEx, SafeProcThreadAttributeListHandle hAttributeList) = CreateStartupInfoEx(in startupInfo, handlesToInherit, forceBreakaway: false, launchInfo.RunAsInvoker || launchInfo.UIAccess, out SafePinnedGCHandle? pinnedHandles);
                            using (hAttributeList)
                            using (pinnedHandles)
                            {
                                _ = NativeMethods.CreateProcess(launchInfo.FilePath, ref commandSpan, lpProcessAttributes: null, lpThreadAttributes: null, bInheritHandles: true, creationFlags | PROCESS_CREATION_FLAGS.EXTENDED_STARTUPINFO_PRESENT, lpEnvironment: null, launchInfo.WorkingDirectory?.FullName, in startupInfoEx, out pi);
                            }
                        }
                        else
                        {
                            _ = NativeMethods.CreateProcess(launchInfo.FilePath, ref commandSpan, lpProcessAttributes: null, lpThreadAttributes: null, bInheritHandles: false, creationFlags, lpEnvironment: null, launchInfo.WorkingDirectory?.FullName, in startupInfo, out pi);
                        }
                    }
                    return (new(pi.hProcess, ownsHandle: true), new(pi.hThread, ownsHandle: true), pi.dwProcessId, commandSpan.ToString());
                }
                catch (Exception ex)
                {
                    using (stdOutErrHandles?.StdOut)
                    using (stdOutErrHandles?.StdErr)
                    using (stdInHandle)
                    {
                        ExceptionDispatchInfo.Capture(ex).Throw();
                        throw;
                    }
                }
                finally
                {
                    stdOutErrHandles?.StdOut.DisposeLocalCopyOfClientHandle();
                    stdOutErrHandles?.StdErr.DisposeLocalCopyOfClientHandle();
                    stdInHandle?.DisposeLocalCopyOfClientHandle();
                }
            }

            /// <summary>
            /// Creates a read pipe server stream and a corresponding task that consumes output from the child process.
            /// </summary>
            /// <param name="interleaved">The shared interleaved output buffer.</param>
            /// <param name="encoding">The text encoding for the stream.</param>
            /// <returns>The server stream, client handle, and read task.</returns>
            private static (HANDLE, ProcessReadStream) CreateReadPipe(ConcurrentQueue<string> interleaved, Encoding encoding)
            {
                AnonymousPipeServerStream stream = new(PipeDirection.In, HandleInheritability.Inheritable);
                List<string> output = [];
                async Task ReadToEndAsync()
                {
                    using (stream)
                    {
                        using StreamReader reader = new(new EndOfStreamLatchingStream(stream), encoding);
                        while ((await reader.ReadLineAsync(default).ConfigureAwait(false))?.TrimEnd() is string line)
                        {
                            interleaved.Enqueue(line);
                            output.Add(line);
                        }
                    }
                }
                try
                {
                    return ((HANDLE)stream.ClientSafePipeHandle.DangerousGetHandle(), new(stream, ReadToEndAsync(), output));
                }
                catch (Exception ex)
                {
                    using (stream)
                    {
                        ExceptionDispatchInfo.Capture(ex).Throw();
                        throw;
                    }
                }
            }

            /// <summary>
            /// Creates a write pipe server stream and a corresponding task that writes stdin data to the child process.
            /// </summary>
            /// <param name="input">The input lines to write.</param>
            /// <param name="encoding">The text encoding for the stream.</param>
            /// <returns>The server stream, client handle, and write task.</returns>
            private static (HANDLE, ProcessWriteStream) CreateWritePipe(IReadOnlyList<string> input, Encoding encoding)
            {
                AnonymousPipeServerStream stream = new(PipeDirection.Out, HandleInheritability.Inheritable);
                async Task WriteToEndAsync()
                {
                    using (stream)
                    {
                        try
                        {
                            using StreamWriter writer = new(stream, encoding.GetPreamble().Length is 0 ? encoding : encoding switch
                            {
                                UTF8Encoding => new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                                UnicodeEncoding => new UnicodeEncoding(bigEndian: encoding.CodePage is 1201, byteOrderMark: false),
                                UTF32Encoding => new UTF32Encoding(bigEndian: encoding.CodePage is 12001, byteOrderMark: false),
                                _ => encoding,
                            });
                            foreach (string line in input)
                            {
                                await writer.WriteLineAsync(line).ConfigureAwait(false);
                            }
                        }
                        catch (IOException)
                        {
                            // The child process didn't read all input before exiting.
                            return;
                        }
                    }
                }
                try
                {
                    return ((HANDLE)stream.ClientSafePipeHandle.DangerousGetHandle(), new(stream, WriteToEndAsync()));
                }
                catch (Exception ex)
                {
                    using (stream)
                    {
                        ExceptionDispatchInfo.Capture(ex).Throw();
                        throw;
                    }
                }
            }

            /// <summary>
            /// Creates a STARTUPINFOEX structure with the specified process thread attributes.
            /// </summary>
            /// <remarks>This method allocates and initializes a STARTUPINFOEX structure with the specified
            /// attributes. The attribute list can include handle inheritance lists and extended process creation flags.
            /// The caller is responsible for disposing of the returned attribute list handle.</remarks>
            /// <param name="startupInfo">The base STARTUPINFOW structure to extend.</param>
            /// <param name="handlesToInherit">An array of handles that the child process should inherit.</param>
            /// <param name="forceBreakaway">If true, adds the EXTENDED_PROCESS_CREATION_FLAG_FORCE_BREAKAWAY attribute.</param>
            /// <param name="runAsInvoker">If true, adds the EXTENDED_PROCESS_CREATION_FLAG_FORCELUA attribute.</param>
            /// <param name="pinnedHandles">When this method returns, contains the pinned GC handle for the handles array, or null if no handles were specified.</param>
            /// <returns>A tuple containing the STARTUPINFOEXW structure and the SafeProcThreadAttributeListHandle.</returns>
            /// <exception cref="InvalidOperationException">Thrown if no attributes are specified or if an error occurs during attribute list creation or updating.</exception>
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "MA0099:Use Explicit enum value instead of 0", Justification = "There is no zero value for the enums in question.")]
            private static (STARTUPINFOEXW, SafeProcThreadAttributeListHandle) CreateStartupInfoEx(in STARTUPINFOW startupInfo, List<nint> handlesToInherit, bool forceBreakaway, bool runAsInvoker, out SafePinnedGCHandle? pinnedHandles)
            {
                // Calculate the number of attributes needed.
                bool hasHandleInheritance = handlesToInherit.Count > 0;
                uint attributeCount = 0;
                if (hasHandleInheritance)
                {
                    attributeCount++;
                }
                if (forceBreakaway || runAsInvoker)
                {
                    attributeCount++;
                }

                // Validate that at least one attribute is specified.
                if (attributeCount == 0)
                {
                    throw new InvalidOperationException("At least one attribute must be specified.");
                }

                // Allocate the attribute list.
                SafeProcThreadAttributeListHandle hAttributeList = SafeProcThreadAttributeListHandle.Alloc(attributeCount);
                try
                {
                    // Add handle list attribute if handles are specified.
                    pinnedHandles = hasHandleInheritance ? SafePinnedGCHandle.Alloc([.. handlesToInherit]) : null;
                    try
                    {
                        // Add the handle list attribute if handles to inherit were specified.
                        if (pinnedHandles is not null)
                        {
                            // The handle list needs to be passed as a pointer to an array of handles.
                            nint handlesPtr = pinnedHandles.DangerousGetHandle();
                            int handleListSize = handlesToInherit.Count * IntPtr.Size;
                            unsafe
                            {
                                _ = hAttributeList.Update(PROC_THREAD_ATTRIBUTE.PROC_THREAD_ATTRIBUTE_HANDLE_LIST, new((void*)handlesPtr, handleListSize));
                            }
                        }

                        // Add extended flags attribute if force breakaway is requested.
                        EXTENDED_PROCESS_CREATION_FLAG extendedFlags = runAsInvoker ? EXTENDED_PROCESS_CREATION_FLAG.EXTENDED_PROCESS_CREATION_FLAG_FORCELUA : 0;
                        if (forceBreakaway)
                        {
                            PrivilegeManager.EnablePrivilegeIfDisabled(SE_PRIVILEGE.SeTcbPrivilege);
                            extendedFlags |= EXTENDED_PROCESS_CREATION_FLAG.EXTENDED_PROCESS_CREATION_FLAG_FORCE_BREAKAWAY;
                        }
                        if (extendedFlags != 0)
                        {
                            unsafe
                            {
                                _ = hAttributeList.Update(PROC_THREAD_ATTRIBUTE.PROC_THREAD_ATTRIBUTE_EXTENDED_FLAGS, MemoryMarshal.AsBytes(new ReadOnlySpan<int>(&extendedFlags, 1)));
                            }
                        }

                        // Create the STARTUPINFOEXW structure.
                        STARTUPINFOEXW startupInfoEx = new() { StartupInfo = startupInfo };
                        startupInfoEx.StartupInfo.cb = (uint)Unsafe.SizeOf<STARTUPINFOEXW>();
                        startupInfoEx.lpAttributeList = (LPPROC_THREAD_ATTRIBUTE_LIST)hAttributeList.DangerousGetHandle();
                        return (startupInfoEx, hAttributeList);
                    }
                    catch (Exception ex)
                    {
                        using (pinnedHandles)
                        {
                            ExceptionDispatchInfo.Capture(ex).Throw();
                            throw;
                        }
                    }
                }
                catch (Exception ex)
                {
                    using (hAttributeList)
                    {
                        ExceptionDispatchInfo.Capture(ex).Throw();
                        throw;
                    }
                }
            }

            /// <summary>
            /// Determines whether the current process can use the CreateProcessAsUser function.
            /// </summary>
            /// <param name="isCallerToken">true if the token being evaluated represents the current caller; false if it represents a different user or security context.</param>
            /// <param name="callerPrivilges">The privileges of the caller.</param>
            /// <remarks>This method checks if the current process has the necessary privileges and conditions
            /// to use the CreateProcessAsUser function. It verifies the presence of specific privileges and evaluates
            /// whether the process is part of a job object that allows breakaway.</remarks>
            /// <returns><see langword="true"/> if the process can use CreateProcessAsUser; otherwise, <see langword="false"/>.</returns>
            private static CreateProcessUsingTokenStatus CanUseCreateProcessAsUser(bool isCallerToken, ReadOnlyCollection<SE_PRIVILEGE> callerPrivilges)
            {
                // Test whether the caller has the required privileges to use CreateProcessAsUser.
                if (!callerPrivilges.Contains(SE_PRIVILEGE.SeIncreaseQuotaPrivilege))
                {
                    return CreateProcessUsingTokenStatus.SeIncreaseQuotaPrivilege;
                }
                if (!callerPrivilges.Contains(SE_PRIVILEGE.SeAssignPrimaryTokenPrivilege))
                {
                    return CreateProcessUsingTokenStatus.SeAssignPrimaryTokenPrivilege;
                }

                // Perform common job object checks.
                return CanCreateProcessUsingToken(isCallerToken, callerPrivilges);
            }

            /// <summary>
            /// Determines whether the current process has the necessary privileges to use the CreateProcessWithToken
            /// function.
            /// </summary>
            /// <param name="isCallerToken">true if the token being evaluated represents the current caller; false if it represents a different user or security context.</param>
            /// <param name="callerPrivilges">The privileges of the caller.</param>
            /// <param name="commandLine">The command line to be executed.</param>
            /// <returns><see langword="true"/> if the current process has the SeImpersonatePrivilege; otherwise, <see
            /// langword="false"/>.</returns>
            private static CreateProcessUsingTokenStatus CanUseCreateProcessWithToken(bool isCallerToken, ReadOnlyCollection<SE_PRIVILEGE> callerPrivilges, ReadOnlySpan<char> commandLine)
            {
                // If the command line exceeds 1024 characters, we can't use CreateProcessWithToken at all.
                if (commandLine.Length > 1024)
                {
                    return CreateProcessUsingTokenStatus.CommandLineTooLong;
                }

                // Test whether the caller has the required privileges to use CreateProcessWithToken.
                if (!callerPrivilges.Contains(SE_PRIVILEGE.SeImpersonatePrivilege))
                {
                    return CreateProcessUsingTokenStatus.SeImpersonatePrivilege;
                }

                // If the service is disabled, we cannot use CreateProcessWithToken. This
                // property will fail if the service is not found, so catch that as well.
                using ServiceController serviceController = new("seclogon");
                try
                {
                    if (serviceController.StartType is ServiceStartMode.Disabled)
                    {
                        return CreateProcessUsingTokenStatus.SecLogonServiceDisabled;
                    }
                }
                catch
                {
                    return CreateProcessUsingTokenStatus.SecLogonServiceNotFound;
                    throw;
                }

                // Perform common job object checks.
                return CanCreateProcessUsingToken(isCallerToken, callerPrivilges);
            }

            /// <summary>
            /// Determines whether the current process can create a new process using a specified security token, based on
            /// job object and privilege constraints.
            /// </summary>
            /// <remarks>This method checks whether the process is running within a job object and whether the
            /// necessary job object limits and privileges are present to allow process creation using a different token. If
            /// the process is restricted by job object settings or lacks the required privileges, the returned status will
            /// indicate the specific limitation.</remarks>
            /// <param name="isCallerToken">true to indicate that the token represents the current caller; false if the token represents a different
            /// user or security context.</param>
            /// <param name="callerPrivilges">A read-only collection of the privileges held by the caller, used to determine if specific
            /// privileges are present that may allow process creation even when job object restrictions are in place.</param>
            /// <returns>A CreateProcessUsingTokenStatus value indicating whether process creation is permitted, or the reason it is
            /// not allowed.</returns>
            private static CreateProcessUsingTokenStatus CanCreateProcessUsingToken(bool isCallerToken, ReadOnlyCollection<SE_PRIVILEGE> callerPrivilges)
            {
                // Test whether the token's SID is the same as the caller's SID.
                // If it is, the following job object checks are not necessary.
                if (isCallerToken)
                {
                    return CreateProcessUsingTokenStatus.OK;
                }

                // Test whether the process is part of an existing job object.
                using (SafeProcessHandle hProcess = NativeMethods.GetCurrentProcess())
                {
                    _ = NativeMethods.IsProcessInJob(hProcess, out BOOL inJob);
                    if (!inJob)
                    {
                        return CreateProcessUsingTokenStatus.OK;
                    }
                }

                // Since we're part of a job object, we need to check if the job has the JOB_OBJECT_LIMIT_BREAKAWAY_OK flag set.
                Span<byte> lpJobObjectInformation = stackalloc byte[Unsafe.SizeOf<JOBOBJECT_EXTENDED_LIMIT_INFORMATION>()];
                _ = NativeMethods.QueryInformationJobObject(JOBOBJECTINFOCLASS.JobObjectExtendedLimitInformation, lpJobObjectInformation, out _);
                ref readonly JOBOBJECT_EXTENDED_LIMIT_INFORMATION jobObjectInfo = ref lpJobObjectInformation.AsReadOnlyStructure<JOBOBJECT_EXTENDED_LIMIT_INFORMATION>();
                if (!(jobObjectInfo.BasicLimitInformation.LimitFlags.HasFlag(JOB_OBJECT_LIMIT.JOB_OBJECT_LIMIT_SILENT_BREAKAWAY_OK) || jobObjectInfo.BasicLimitInformation.LimitFlags.HasFlag(JOB_OBJECT_LIMIT.JOB_OBJECT_LIMIT_BREAKAWAY_OK)))
                {
                    return !callerPrivilges.Contains(SE_PRIVILEGE.SeTcbPrivilege) ? CreateProcessUsingTokenStatus.SeTcbPrivilege : CreateProcessUsingTokenStatus.JobBreakawayNotPermitted;
                }

                // If we're here, everything we need to be able to use CreateProcessAsUser() is available.
                return CreateProcessUsingTokenStatus.OK;
            }

            /// <summary>
            /// Creates a new process using the specified primary token and command line.
            /// </summary>
            /// <remarks>This method attempts to create a process using <c language="csharp">CreateProcessAsUser</c> if
            /// possible, falling back to <c language="csharp">CreateProcessWithToken</c> if necessary. It requires specific privileges to be
            /// enabled, such as <c language="csharp">SeIncreaseQuotaPrivilege</c> and <c language="csharp">SeAssignPrimaryTokenPrivilege</c>.</remarks>
            /// <param name="hPrimaryToken">The primary token representing the user context under which the process will be created.</param>
            /// <param name="callerPrivilges">A read-only collection of the privileges held by the caller, used to determine if specific
            /// privileges are present that may allow process creation even when job object restrictions are in place.</param>
            /// <param name="filePath">The fully qualified path to the executable file for the new process.</param>
            /// <param name="commandLine">The command line to be executed by the new process.</param>
            /// <param name="handlesToInherit">An array of specific handles that the child process should inherit. When specified,
            /// a STARTUPINFOEX with PROC_THREAD_ATTRIBUTE_HANDLE_LIST is used to limit inheritance to these handles only.</param>
            /// <param name="hasExternalHandles">Indicates whether there are any external handles to inherit, which would require
            /// the use of CreateProcessAsUser even if the caller token is the same as the current process token.</param>
            /// <param name="creationFlags">Flags that control the priority class and the creation of the process.</param>
            /// <param name="lpEnvironment">A handle to the environment block for the new process. Can be <see langword="null"/> to use the environment
            /// of the calling process.</param>
            /// <param name="workingDirectory">The full path to the current directory for the process. Can be <see langword="null"/> to use the current
            /// directory of the calling process.</param>
            /// <param name="runAsInvoker">Indicates that the process must be created with the EXTENDED_PROCESS_CREATION_FLAG_FORCELUA flag.</param>
            /// <param name="startupInfo">A reference to a <see cref="STARTUPINFOW"/> structure that specifies the window station, desktop, standard
            /// handles, and appearance of the main window for the new process.</param>
            /// <param name="pi">When this method returns, contains a <see cref="PROCESS_INFORMATION"/> structure with information about the
            /// newly created process and its primary thread.</param>
            /// <exception cref="InvalidOperationException">Thrown if the process cannot be started.</exception>
            private static BOOL CreateProcessUsingToken(SafeFileHandle hPrimaryToken, ReadOnlyCollection<SE_PRIVILEGE> callerPrivilges, string filePath, ref Span<char> commandLine, List<nint> handlesToInherit, bool hasExternalHandles, PROCESS_CREATION_FLAGS creationFlags, SafeEnvironmentBlockHandle? lpEnvironment, string? workingDirectory, bool runAsInvoker, in STARTUPINFOW startupInfo, out PROCESS_INFORMATION pi)
            {
                // If the parent process is associated with an existing job object, using the CREATE_BREAKAWAY_FROM_JOB flag can help
                // with E_ACCESSDENIED errors from CreateProcessWithToken() as processes in a job all need to be in the same session.
                // The use of this flag has effect if the parent is part of a job and that job has JOB_OBJECT_LIMIT_BREAKAWAY_OK set.
                bool isCallerToken = TokenUtilities.GetTokenSid(hPrimaryToken) == AccountUtilities.CallerSid;
                if (!isCallerToken)
                {
                    creationFlags |= PROCESS_CREATION_FLAGS.CREATE_BREAKAWAY_FROM_JOB;
                }

                // Attempt to use CreateProcessAsUser() first as it's gold standard, otherwise fall back to CreateProcessWithToken().
                // When the caller provides handles to inherit, we need to use CreateProcessAsUser() since it has bInheritHandles.
                CreateProcessUsingTokenStatus createProcessAsUserAbility = CanUseCreateProcessAsUser(isCallerToken, callerPrivilges);
                bool forceBreakaway = createProcessAsUserAbility is CreateProcessUsingTokenStatus.JobBreakawayNotPermitted;
                if (createProcessAsUserAbility is CreateProcessUsingTokenStatus.OK || forceBreakaway || runAsInvoker)
                {
                    // Use STARTUPINFOEX when we need to specify handle inheritance or force breakaway.
                    if (forceBreakaway || runAsInvoker || handlesToInherit.Count > 0)
                    {
                        // Create the extended startup info with the necessary attributes.
                        (STARTUPINFOEXW startupInfoEx, SafeProcThreadAttributeListHandle hAttributeList) = CreateStartupInfoEx(in startupInfo, handlesToInherit, forceBreakaway, runAsInvoker, out SafePinnedGCHandle? pinnedHandles);
                        using (hAttributeList)
                        using (pinnedHandles)
                        {
                            return NativeMethods.CreateProcessAsUser(hPrimaryToken, filePath, ref commandLine, lpProcessAttributes: null, lpThreadAttributes: null, bInheritHandles: true, creationFlags | PROCESS_CREATION_FLAGS.EXTENDED_STARTUPINFO_PRESENT, lpEnvironment, workingDirectory, in startupInfoEx, out pi);
                        }
                    }
                    return NativeMethods.CreateProcessAsUser(hPrimaryToken, filePath, ref commandLine, lpProcessAttributes: null, lpThreadAttributes: null, bInheritHandles: false, creationFlags, lpEnvironment, workingDirectory, in startupInfo, out pi);
                }
                if (hasExternalHandles)
                {
                    throw new InvalidOperationException($"Unable to create a new process using CreateProcessAsUser(): {createProcessAsUserAbility.GetDescription()}");
                }

                // Using CreateProcessAsUser() is not possible, so fall back to CreateProcessWithToken().
                CreateProcessUsingTokenStatus createProcessWithTokenAbility = CanUseCreateProcessWithToken(isCallerToken, callerPrivilges, commandLine);
                if (createProcessWithTokenAbility is not CreateProcessUsingTokenStatus.OK)
                {
                    throw new InvalidOperationException($"Unable to create a new process using CreateProcessWithToken(): {createProcessWithTokenAbility.GetDescription()}");
                }
                PrivilegeManager.EnablePrivilegeIfDisabled(SE_PRIVILEGE.SeImpersonatePrivilege);
                return NativeMethods.CreateProcessWithToken(hPrimaryToken, CREATE_PROCESS_LOGON_FLAGS.LOGON_WITH_PROFILE, filePath, ref commandLine, creationFlags, lpEnvironment, workingDirectory, in startupInfo, out pi);
            }

            /// <summary>
            /// Represents the status codes returned by the process creation operation using a token.
            /// </summary>
            /// <remarks>This enumeration provides specific status codes that indicate the result of
            /// attempting to create a process using a token. Each value corresponds to a particular condition or
            /// requirement related to privilege or service availability necessary for the operation.</remarks>
            private enum CreateProcessUsingTokenStatus
            {
                // This deliberately doesn't have a description as we should never need it/be asking for it.
                OK = 0,

                [Description("The calling process does not have the necessary SeIncreaseQuotaPrivilege privilege.")]
                SeIncreaseQuotaPrivilege = 1,

                [Description("The calling process does not have the necessary SeAssignPrimaryTokenPrivilege privilege.")]
                SeAssignPrimaryTokenPrivilege = 2,

                [Description("The calling process is part of a job that does not allow breakaway.")]
                JobBreakawayNotPermitted = 3,

                [Description("The calling process does not have the necessary SeTcbPrivilege privilege.")]
                SeTcbPrivilege = 4,

                [Description("The process command line exceeds the API limitation of 1024 characters.")]
                CommandLineTooLong = 5,

                [Description("The calling process does not have the necessary SeImpersonatePrivilege privilege.")]
                SeImpersonatePrivilege = 6,

                [Description("The system's Secondary Log-on service (seclogon) could not be found.")]
                SecLogonServiceNotFound = 7,

                [Description("The system's Secondary Log-on service (seclogon) is disabled.")]
                SecLogonServiceDisabled = 8,
            }
        }

        /// <summary>
        /// Contains helper methods for creating processes using the Windows ShellExecuteEx API.
        /// </summary>
        private static class ShellExecuteExApi
        {
            /// <summary>
            /// Calls ShellExecuteEx on a single-threaded apartment, which the shell requires, hopping onto a new one when the
            /// caller is not already on one.
            /// </summary>
            /// <param name="launchInfo">The launch to perform.</param>
            /// <returns>The process handle the shell returned, or null for a pure shell action, and whether the process was held suspended.</returns>
            internal static (SafeProcessHandle? hProcess, bool suspended) ShellExecuteEx(ProcessLaunchInfo launchInfo)
            {
                if (Thread.CurrentThread.GetApartmentState() is ApartmentState.STA)
                {
                    return ShellExecuteExImpl(launchInfo);
                }
                (SafeProcessHandle? hProcess, bool suspended) result = default;
                ExceptionDispatchInfo? failure = null;
                Thread thread = new(() =>
                {
                    try
                    {
                        result = ShellExecuteExImpl(launchInfo);
                    }
                    catch (Exception ex)
                    {
                        failure = ExceptionDispatchInfo.Capture(ex);
                        return;
                        throw;
                    }
                });
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                thread.Join();
                failure?.Throw();
                return result;
            }

            /// <summary>
            /// Launches through the shell with the flags Process.Start uses, and a site that gives the new process the
            /// creation flags the CreateProcess path would, holding it suspended wherever the shell creates it itself.
            /// </summary>
            /// <param name="launchInfo">The launch to perform.</param>
            /// <returns>The process handle the shell returned, or null for a pure shell action, and whether the process was held suspended.</returns>
            /// <exception cref="NotSupportedException">Thrown if image file execution options are to be bypassed for a launch the shell performs through DDE, which cannot be held for the debugger to be detached.</exception>
            /// <exception cref="InvalidOperationException">Thrown if the shell created its process without the flags it was asked to add, or held it suspended but returned no handle to resume it with.</exception>
            private static (SafeProcessHandle? hProcess, bool suspended) ShellExecuteExImpl(ProcessLaunchInfo launchInfo)
            {
                // The shell waits inside the call for a DDE conversation, which a process that is suspended or debugged can never answer.
                bool dde = HasDdeCommand(launchInfo);
                if (dde && launchInfo.BypassIfeo)
                {
                    throw new NotSupportedException("Cannot bypass image file execution options for a launch the shell performs through DDE.");
                }

                // Set up the specific process creation flags and create the underlying process. The process is only held when
                // something must be applied before it runs, as a launch the shell hands to AppInfo can be ended but never resumed.
                PROCESS_CREATION_FLAGS creationFlags = ((PROCESS_CREATION_FLAGS?)launchInfo.PriorityClass ?? 0) |
                    (!dde && (launchInfo.RequiresJobObject || launchInfo.DenyUserTermination) ? PROCESS_CREATION_FLAGS.CREATE_SUSPENDED : 0) |
                    (launchInfo.BypassIfeo ? PROCESS_CREATION_FLAGS.DEBUG_ONLY_THIS_PROCESS : 0) |
                    PROCESS_CREATION_FLAGS.CREATE_SEPARATE_WOW_VDM;
                CreatingProcessSite site = new(creationFlags);
                nint siteUnknown = Marshal.GetIUnknownForObject(site);
                SHELLEXECUTEINFOW execInfo = new()
                {
                    fMask = SEE_MASK_FLAGS.SEE_MASK_NOCLOSEPROCESS | SEE_MASK_FLAGS.SEE_MASK_FLAG_NO_UI | SEE_MASK_FLAGS.SEE_MASK_FLAG_DDEWAIT | SEE_MASK_FLAGS.SEE_MASK_FLAG_HINST_IS_SITE,
                    nShow = !launchInfo.CreateNoWindow ? launchInfo.WindowStyle?.ToShowWindowCmd() ?? SHOW_WINDOW_CMD.SW_NORMAL : SHOW_WINDOW_CMD.SW_HIDE,
                    hInstApp = (HINSTANCE)siteUnknown,
                };
                try
                {
                    unsafe
                    {
                        fixed (char* pVerb = launchInfo.Verb)
                        {
                            fixed (char* pFile = launchInfo.FilePath)
                            {
                                fixed (char* pParameters = !string.IsNullOrWhiteSpace(launchInfo.Arguments) ? launchInfo.Arguments : null)
                                {
                                    fixed (char* pDirectory = launchInfo.WorkingDirectory?.FullName)
                                    {
                                        execInfo.lpVerb = pVerb;
                                        execInfo.lpFile = pFile;
                                        execInfo.lpParameters = pParameters;
                                        execInfo.lpDirectory = pDirectory;
                                        _ = NativeMethods.ShellExecuteEx(ref execInfo);
                                    }
                                }
                            }
                        }
                    }
                }
                finally
                {
                    _ = Marshal.Release(siteUnknown);
                }

                // Verify the state of the handle we received back and ensure it's valid.
                SafeProcessHandle? hProcess = !execInfo.hProcess.IsNull ? new(execInfo.hProcess, ownsHandle: true) : null;
                if (!site.Invoked)
                {
                    return (hProcess, false);
                }
                if (!site.Applied)
                {
                    using (hProcess)
                    {
                        if (hProcess is not null)
                        {
                            TerminateFailedLaunch(hProcess);
                        }
                        throw new InvalidOperationException("The shell created the process without the flags it was asked to add.", site.Failure);
                    }
                }
                if (site.Suspended && hProcess is null)
                {
                    throw new InvalidOperationException("The shell created a suspended process but returned no handle to it.");
                }

                // The debug flag has bypassed IFEO by now, and the process can't run while it remains a debuggee of this thread.
                if (launchInfo.BypassIfeo && hProcess is not null)
                {
                    try
                    {
                        _ = NativeMethods.DebugActiveProcessStop(NativeMethods.GetProcessId(hProcess));
                    }
                    catch (Exception ex)
                    {
                        using (hProcess)
                        {
                            TerminateFailedLaunch(hProcess);
                            ExceptionDispatchInfo.Capture(ex).Throw();
                            throw;
                        }
                    }
                }
                return (hProcess, site.Suspended);
            }

            /// <summary>
            /// Determines whether the shell would perform the launch through a DDE conversation, which it waits on inside
            /// ShellExecuteEx and so cannot be held suspended.
            /// </summary>
            /// <param name="launchInfo">The launch to inspect.</param>
            /// <returns><see langword="true"/> if a DDE command is registered for the launch; otherwise, <see langword="false"/>.</returns>
            private static bool HasDdeCommand(ProcessLaunchInfo launchInfo)
            {
                // The shell resolves a URL by its scheme and anything else by its extension, under the verb it will run.
                (ASSOCF flags, string association) = !Uri.TryCreate(launchInfo.FilePath, UriKind.Absolute, out Uri? uri) || uri.IsFile ? (ASSOCF.ASSOCF_NONE, Path.GetExtension(launchInfo.FilePath)) : (ASSOCF.ASSOCF_IS_PROTOCOL, uri.Scheme);
                return !string.IsNullOrWhiteSpace(association) && NativeMethods.AssocQueryString(flags, ASSOCSTR.ASSOCSTR_DDECOMMAND, association, launchInfo.Verb ?? "open", default, out _) == HRESULT.S_FALSE;
            }
        }
    }
}
