using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;
using PSADT.AccountManagement;
using PSADT.Foundation;
using PSADT.Interop;
using PSADT.SafeHandles;
using PSADT.Security;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Security;
using Windows.Win32.Security.Authorization;
using Windows.Win32.System.JobObjects;
using Windows.Win32.System.Threading;

namespace PSADT.ProcessManagement
{
    /// <summary>
    /// Represents a handle to a process, encapsulating the process, its module information, launch details, command
    /// line, and associated asynchronous task.
    /// </summary>
    /// <remarks>This record provides a structured way to manage and interact with a process, offering access
    /// to its core components and the ability to handle its asynchronous operations.</remarks>
    public sealed class ProcessHandle
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessHandle"/> record with the specified process launch information, process handle and ID, command line, caller privileges, and optional standard stream handles and resume delegate.
        /// </summary>
        /// <param name="launchInfo">The launch configuration and metadata used to start the process.</param>
        /// <param name="hProcess">The handle to the running process.</param>
        /// <param name="dwProcessId">The process ID of the running process.</param>
        /// <param name="commandLine">The full command line used to launch the process.</param>
        /// <param name="callerPrivileges">The caller's privileges as per the PrivilegeManager class.</param>
        /// <param name="stdOutErrHandles">A tuple containing the handles responsible for asynchronously reading the standard output and standard error streams of the process, along with a read-only collection containing the combined output from both streams.</param>
        /// <param name="stdInHandle">An optional handle for writing to the standard input stream of the process, if input is being provided.</param>
        /// <param name="resumeProcessDelegate">A delegate that can be invoked to resume the process if it was started in a suspended state.</param>
        internal ProcessHandle(ProcessLaunchInfo launchInfo, SafeProcessHandle hProcess, uint dwProcessId, string commandLine, ReadOnlyCollection<SE_PRIVILEGE> callerPrivileges, (ProcessReadStream StdOut, ProcessReadStream StdErr, IReadOnlyCollection<string> InterleavedBuffer)? stdOutErrHandles, ProcessWriteStream? stdInHandle, Action resumeProcessDelegate)
        {
            // Confirm all inputs are valid and set up fields.
            ArgumentException.ThrowIfNullOrWhiteSpace(commandLine);
            ArgumentException.ThrowIfNullOrClosed(hProcess);
            Process = GetProcessByIdAndHandle(dwProcessId, hProcess);
            LaunchInfo = launchInfo; CommandLine = commandLine;

            // Post-process the handle to ensure it is in the correct state for the requested launch configuration.
            if (launchInfo.DenyUserTermination)
            {
                DenyProcessTermination(launchInfo, hProcess, callerPrivileges);
            }

            // Create a job object and IO completion port if required by the launch configuration, then release the process into it.
            (SafeFileHandle jobObject, SafeFileHandle ioCompletionPort)? job = null;
            try
            {
                if (launchInfo.RequiresJobObject)
                {
                    job = CreateProcessJob(launchInfo, hProcess);
                }
                resumeProcessDelegate?.Invoke();
            }
            catch (Exception ex)
            {
                using (job?.ioCompletionPort)
                using (job?.jobObject)
                {
                    ExceptionDispatchInfo.Capture(ex).Throw();
                    throw;
                }
            }
            Task = GetTaskAsync();

            // Internal worker to satisfy S4457 so that the error handling works properly.
            async System.Threading.Tasks.Task<ProcessResult> GetTaskAsync()
            {
                // Wait for the process to exit or for a cancellation request, and handle the exit code accordingly.
                CancellationToken cancellationToken = launchInfo.CancellationToken ?? CancellationToken.None;
                const uint timeoutExitCode = unchecked((uint)ProcessManager.TimeoutExitCode);
                int exitCode = ProcessManager.TimeoutExitCode; bool processFinished = false;

                // Set the client/server success flag if the client started a ShellExecuteEx process invocation.
                if (ClientServerUtilities.CallerIsClientServerExecutable && launchInfo.UseShellExecute)
                {
                    ClientServerUtilities.SetOperationSuccessFlag();
                }

                // If a job object and IO completion port were created, monitor the IO completion port for process exit or timeout events.
                if (job is (SafeFileHandle jobObject, SafeFileHandle ioCompletionPort))
                {
                    // Monitor the IO completion port for process exit or timeout events on a thread of its own, as the wait blocks for the life of the process.
                    using (ioCompletionPort)
                    using (jobObject)
                    {
                        await System.Threading.Tasks.Task.Factory.StartNew(() =>
                        {
                            using CancellationTokenRegistration? ctr = cancellationToken.CanBeCanceled ? cancellationToken.Register(() => NativeMethods.PostQueuedCompletionStatus(ioCompletionPort, timeoutExitCode, default)) : null;
                            while (true)
                            {
                                _ = NativeMethods.GetQueuedCompletionStatus(ioCompletionPort, out uint lpCompletionCode, out _, out nuint lpOverlapped, PInvoke.INFINITE);
                                if (lpCompletionCode == timeoutExitCode)
                                {
                                    if (launchInfo.NoTerminateOnTimeout)
                                    {
                                        // When KillChildProcessesWithParent is true, the job has JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE set.
                                        // Disposing the job would terminate the process we're supposed to let run, so we intentionally
                                        // leak the job handle in this specific scenario to honor the NoTerminateOnTimeout request.
                                        if (launchInfo.KillChildProcessesWithParent)
                                        {
                                            jobObject.SetHandleAsInvalid();
                                        }
                                        break;
                                    }
                                    _ = NativeMethods.TerminateJobObject(jobObject, timeoutExitCode);
                                }
                                else if ((lpCompletionCode == (uint)JOB_OBJECT_MSG.JOB_OBJECT_MSG_EXIT_PROCESS && (uint)lpOverlapped == Process.Id && !launchInfo.WaitForChildProcesses) || (lpCompletionCode == (uint)JOB_OBJECT_MSG.JOB_OBJECT_MSG_ACTIVE_PROCESS_ZERO))
                                {
                                    _ = NativeMethods.GetExitCodeProcess(Process.SafeHandle, out uint lpExitCode);
                                    exitCode = unchecked((int)lpExitCode);
                                    processFinished = true;
                                    break;
                                }
                            }
                        }, CancellationToken.None, System.Threading.Tasks.TaskCreationOptions.LongRunning, System.Threading.Tasks.TaskScheduler.Default).ConfigureAwait(false);
                    }
                }
                else
                {
                    try
                    {
                        await Process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
                        exitCode = Process.ExitCode;
                        processFinished = true;
                    }
                    catch (OperationCanceledException) when (cancellationToken.CanBeCanceled && cancellationToken.IsCancellationRequested)
                    {
                        if (!launchInfo.NoTerminateOnTimeout)
                        {
                            try
                            {
                                Process.Kill();
                            }
                            catch (InvalidOperationException)
                            {
                                // Already exited.
                                exitCode = Process.ExitCode;
                                processFinished = true;
                            }
                            if (!processFinished)
                            {
                                await Process.WaitForExitAsync(default).ConfigureAwait(false);
                                exitCode = ProcessManager.TimeoutExitCode;
                                processFinished = true;
                            }
                        }
                        else
                        {
                            exitCode = ProcessManager.TimeoutExitCode;
                        }
                    }
                }
                if (processFinished)
                {
                    await System.Threading.Tasks.Task.WhenAll(stdOutErrHandles?.StdOut.Task ?? System.Threading.Tasks.Task.CompletedTask, stdOutErrHandles?.StdErr.Task ?? System.Threading.Tasks.Task.CompletedTask, stdInHandle?.Task ?? System.Threading.Tasks.Task.CompletedTask).WaitAsync(cancellationToken.CanBeCanceled && !cancellationToken.IsCancellationRequested ? cancellationToken : CancellationToken.None).ConfigureAwait(false);
                }
                return new(Process, launchInfo, commandLine, exitCode, stdOutErrHandles?.StdOut.Buffer, stdOutErrHandles?.StdErr.Buffer, stdOutErrHandles?.InterleavedBuffer);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessHandle"/> record with the specified process launch information, process handle and ID, command line, and resume delegate.
        /// </summary>
        /// <param name="launchInfo">The launch configuration and metadata used to start the process.</param>
        /// <param name="hProcess">The handle to the running process.</param>
        /// <param name="dwProcessId">The process ID of the running process.</param>
        /// <param name="commandLine">The full command line used to launch the process.</param>
        /// <param name="resumeProcessDelegate">A delegate that can be invoked to resume the process if it was started in a suspended state.</param>
        internal ProcessHandle(ProcessLaunchInfo launchInfo, SafeProcessHandle hProcess, uint dwProcessId, string commandLine, Action resumeProcessDelegate) : this(launchInfo, hProcess, dwProcessId, commandLine, PrivilegeManager.GetPrivileges(), stdOutErrHandles: null, stdInHandle: null, resumeProcessDelegate)
        {
        }

        /// <summary>
        /// Represents the process associated with the current operation.
        /// </summary>
        /// <remarks>This field provides access to the underlying <see cref="System.Diagnostics.Process"/>
        /// instance. It is read-only and should be used to retrieve information about the process or to perform
        /// operations on it.</remarks>
        public Process Process { get; }

        /// <summary>
        /// Gets the information required to launch a process.
        /// </summary>
        public ProcessLaunchInfo LaunchInfo { get; }

        /// <summary>
        /// Gets the command line string associated with the current process.
        /// </summary>
        public string CommandLine { get; }

        /// <summary>
        /// Represents an asynchronous operation that returns a <see cref="ProcessResult"/>.
        /// </summary>
        /// <remarks>This field holds a <see cref="System.Threading.Tasks.Task{TResult}"/> that, when awaited, provides the
        /// result of a process. The task is read-only and should be awaited to retrieve the <see
        /// cref="ProcessResult"/>.</remarks>
        public System.Threading.Tasks.Task<ProcessResult> Task { get; }

        /// <summary>
        /// Gets the current status of the process completion task.
        /// </summary>
        public System.Threading.Tasks.TaskStatus Status => Task.Status;

        /// <summary>
        /// Gets a value indicating whether the process completion task has been canceled.
        /// </summary>
        public bool IsCanceled => Task.IsCanceled;

        /// <summary>
        /// Gets a value indicating whether the process completion task has completed.
        /// </summary>
        public bool IsCompleted => Task.IsCompleted;

        /// <summary>
        /// Gets a value indicating whether the process completion task has completed with an error.
        /// </summary>
        public bool IsFaulted => Task.IsFaulted;

        /// <summary>
        /// Gets an awaiter for the process completion task.
        /// </summary>
        /// <returns>An awaiter for the process result.</returns>
        public TaskAwaiter<ProcessResult> GetAwaiter()
        {
            return Task.GetAwaiter();
        }

        /// <summary>
        /// Configures an awaiter used to await this process handle.
        /// </summary>
        /// <param name="continueOnCapturedContext">
        /// <see langword="true"/> to attempt to marshal the continuation back to the original context captured;
        /// otherwise, <see langword="false"/>.
        /// </param>
        /// <returns>A configured task awaitable.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD003:Avoid awaiting foreign Tasks", Justification = "This task is started within our context.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Roslynator", "RCS1046:Asynchronous method name should end with 'Async'", Justification = "This isn't appropriate here.")]
        public ConfiguredTaskAwaitable<ProcessResult> ConfigureAwait(bool continueOnCapturedContext)
        {
            return Task.ConfigureAwait(continueOnCapturedContext);
        }

        /// <summary>
        /// Creates a Process object from an existing process ID and handle.
        /// </summary>
        /// <param name="dwProcessId">The ID of the existing process.</param>
        /// <param name="hProcess">The handle of the existing process.</param>
        /// <returns>A Process object representing the existing process.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the Process object cannot be created or the handle cannot be set.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S3011:Reflection should not be used to increase accessibility of classes, methods, or fields", Justification = "This is unfortuantely deliberate as the CLR does not provide a way to instantiate a Process object using an existing handle.")]
        private static Process GetProcessByIdAndHandle(uint dwProcessId, SafeProcessHandle hProcess)
        {
            // Use reflection to create a Process instance using its private constructor, then set the handle via its private `SetProcessHandle` method.
            Process process = (Process?)Activator.CreateInstance(typeof(Process), BindingFlags.Instance | BindingFlags.NonPublic, binder: null, [".", false, (int)dwProcessId, null], culture: null, activationAttributes: null) ?? throw new InvalidOperationException($"Failed to create process with ID {dwProcessId}.");
            try
            {
                _ = (typeof(Process).GetMethod("SetProcessHandle", BindingFlags.Instance | BindingFlags.NonPublic) ?? throw new InvalidOperationException("Failed to get SetProcessHandle method.")).Invoke(process, [hProcess]);
                return process;
            }
            catch (Exception ex)
            {
                using (process)
                {
                    ExceptionDispatchInfo.Capture(ex).Throw();
                    throw;
                }
            }
        }

        /// <summary>
        /// Modifies the access control list (ACL) of the specified process to deny termination and other dangerous operations.
        /// </summary>
        /// <param name="launchInfo">The launch configuration and metadata used to start the process.</param>
        /// <param name="hProcess">A safe handle to the process, used for resource management and native operations.</param>
        /// <param name="callerPrivileges">The caller's privileges as per the PrivilegeManager class.</param>
        private static void DenyProcessTermination(ProcessLaunchInfo launchInfo, SafeProcessHandle hProcess, ReadOnlyCollection<SE_PRIVILEGE> callerPrivileges)
        {
            // If the client/server process isn't ours, we'll want to change the owner to ourselves if we can.
            RunAsActiveUser runAsActiveUser = launchInfo.RunAsActiveUser ?? AccountUtilities.CallerRunAsActiveUser; bool changeOwner = false;
            if (runAsActiveUser.SID != AccountUtilities.CallerSid && callerPrivileges.Contains(SE_PRIVILEGE.SeSecurityPrivilege) && callerPrivileges.Contains(SE_PRIVILEGE.SeTakeOwnershipPrivilege))
            {
                PrivilegeManager.EnablePrivilegeIfDisabled(SE_PRIVILEGE.SeSecurityPrivilege);
                PrivilegeManager.EnablePrivilegeIfDisabled(SE_PRIVILEGE.SeTakeOwnershipPrivilege);
                changeOwner = true;
            }

            // Create a restricted access control list (ACL) for the client process so the user can't terminate it.
            using SafePinnedGCHandle pinnedUserSid = SafePinnedGCHandle.Alloc(runAsActiveUser.SID.GetBinaryForm());
            bool pinnedUserSidAddRef = false;
            try
            {
                // Generate an explicit access control entry (ACE) for the user SID.
                pinnedUserSid.DangerousAddRef(ref pinnedUserSidAddRef);
                TRUSTEE_W aceTrustee = new()
                {
                    TrusteeForm = TRUSTEE_FORM.TRUSTEE_IS_SID,
                    ptstrName = new(pinnedUserSid.DangerousGetHandle()),
                };

                // Create a DENY ACE for dangerous permissions that could be used for code injection or process manipulation.
                EXPLICIT_ACCESS_W denyAce = new()
                {
                    grfAccessPermissions = (uint)(
                        PROCESS_ACCESS_RIGHTS.PROCESS_TERMINATE |                    // Prevent termination
                        PROCESS_ACCESS_RIGHTS.PROCESS_VM_WRITE |                     // Prevent memory writes (code injection)
                        PROCESS_ACCESS_RIGHTS.PROCESS_VM_OPERATION |                 // Prevent memory operations
                        PROCESS_ACCESS_RIGHTS.PROCESS_CREATE_THREAD |                // Prevent remote thread creation
                        PROCESS_ACCESS_RIGHTS.PROCESS_DUP_HANDLE |                   // Prevent handle duplication attacks
                        PROCESS_ACCESS_RIGHTS.PROCESS_SET_INFORMATION |              // Prevent process info modification
                        PROCESS_ACCESS_RIGHTS.PROCESS_SUSPEND_RESUME),               // Prevent suspend/resume manipulation
                    grfAccessMode = ACCESS_MODE.DENY_ACCESS,
                    grfInheritance = ACE_FLAGS.NO_INHERITANCE,
                    Trustee = aceTrustee,
                };

                // Create a GRANT ACE for limited permissions (query and synchronize only).
                EXPLICIT_ACCESS_W grantAce = new()
                {
                    grfAccessPermissions = (uint)(
                        PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_LIMITED_INFORMATION |    // Allow querying limited info
                        PROCESS_ACCESS_RIGHTS.PROCESS_SYNCHRONIZE),                  // Allow synchronization
                    grfAccessMode = ACCESS_MODE.GRANT_ACCESS,
                    grfInheritance = ACE_FLAGS.NO_INHERITANCE,
                    Trustee = aceTrustee,
                };

                // Apply the ACL and potentially change the owner of the client process. DENY ACEs are processed before GRANT ACEs by Windows.
                _ = NativeMethods.SetEntriesInAcl([denyAce, grantAce], out LocalFreeSafeHandle pAcl);
                using (pAcl)
                {
                    if (changeOwner)
                    {
                        using SafePinnedGCHandle pinnedCallerSid = SafePinnedGCHandle.Alloc(AccountUtilities.CallerSid.GetBinaryForm());
                        _ = NativeMethods.SetSecurityInfo(hProcess, SE_OBJECT_TYPE.SE_KERNEL_OBJECT, OBJECT_SECURITY_INFORMATION.OWNER_SECURITY_INFORMATION | OBJECT_SECURITY_INFORMATION.DACL_SECURITY_INFORMATION, pinnedCallerSid, psidGroup: null, pAcl, pSacl: null);
                    }
                    else
                    {
                        _ = NativeMethods.SetSecurityInfo(hProcess, SE_OBJECT_TYPE.SE_KERNEL_OBJECT, OBJECT_SECURITY_INFORMATION.DACL_SECURITY_INFORMATION, psidOwner: null, psidGroup: null, pAcl, pSacl: null);
                    }
                }
            }
            catch
            {
                return;
                throw;
            }
            finally
            {
                if (pinnedUserSidAddRef)
                {
                    pinnedUserSid.DangerousRelease();
                }
            }
        }

        /// <summary>
        /// Creates a job object that reports to a new IO completion port, and assigns the process to it.
        /// </summary>
        /// <param name="launchInfo">The launch the job is for, which decides whether closing the job kills its processes.</param>
        /// <param name="hProcess">The process to assign to the job.</param>
        /// <returns>The job object and the IO completion port it reports to.</returns>
        private static (SafeFileHandle jobObject, SafeFileHandle ioCompletionPort) CreateProcessJob(ProcessLaunchInfo launchInfo, SafeProcessHandle hProcess)
        {
            SafeFileHandle? ioCompletionPort = null;
            SafeFileHandle? jobObject = null;
            try
            {
                jobObject = NativeMethods.CreateJobObject();
                ioCompletionPort = NativeMethods.CreateIoCompletionPort(0);
                JOBOBJECT_ASSOCIATE_COMPLETION_PORT completionPort = new()
                {
                    CompletionPort = (HANDLE)ioCompletionPort.DangerousGetHandle(),
                    CompletionKey = null,
                };
                _ = NativeMethods.SetInformationJobObject(jobObject, in completionPort);
                if (launchInfo.KillChildProcessesWithParent)
                {
                    JOBOBJECT_EXTENDED_LIMIT_INFORMATION extendedLimitInformation = new()
                    {
                        BasicLimitInformation = new()
                        {
                            LimitFlags = JOB_OBJECT_LIMIT.JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE,
                        },
                    };
                    _ = NativeMethods.SetInformationJobObject(jobObject, in extendedLimitInformation);
                }
                _ = NativeMethods.AssignProcessToJobObject(jobObject, hProcess);
                return (jobObject, ioCompletionPort);
            }
            catch (Exception ex)
            {
                using (ioCompletionPort)
                using (jobObject)
                {
                    ExceptionDispatchInfo.Capture(ex).Throw();
                    throw;
                }
            }
        }
    }
}
