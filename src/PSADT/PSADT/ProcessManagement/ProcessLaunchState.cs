using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
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
    /// Represents the state and resources associated with a launched process, including process handles, job object
    /// tracking, and command-line information. Provides mechanisms for managing process lifetime and cleanup of related
    /// resources.
    /// </summary>
    /// <remarks>This class encapsulates all relevant information and handles for a process started by the
    /// application, including optional job object and I/O completion port management for advanced process control
    /// scenarios. It implements IDisposable to ensure that unmanaged resources are released appropriately. Instances of
    /// this class are intended for internal use to coordinate process execution, monitoring, and cleanup.</remarks>
    internal sealed class ProcessLaunchState : IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the ProcessLaunchState class with detailed process and stream information,
        /// including asynchronous tasks and output buffers.
        /// </summary>
        /// <param name="launchInfo">The launch configuration and metadata used to start the process.</param>
        /// <param name="process">The Process object representing the running process.</param>
        /// <param name="processId">The unique identifier of the started process.</param>
        /// <param name="processHandle">A safe handle to the process, used for resource management and native operations.</param>
        /// <param name="commandLine">The full command line used to launch the process.</param>
        /// <param name="stdOutHandle">The handle responsible for asynchronously reading the standard output stream of the process.</param>
        /// <param name="stdErrHandle">The handle responsible for asynchronously reading the standard error stream of the process.</param>
        /// <param name="interleavedBuffer">A read-only collection containing the combined output from both standard output and standard error streams.</param>
        /// <param name="stdInHandle">An optional handle for writing to the standard input stream of the process, if input is being provided.</param>
        internal ProcessLaunchState(ProcessLaunchInfo launchInfo, Process process, uint processId, SafeProcessHandle processHandle, string commandLine, ProcessReadStream stdOutHandle, ProcessReadStream stdErrHandle, IReadOnlyCollection<string> interleavedBuffer, ProcessWriteStream? stdInHandle = null) : this(launchInfo, process, processId, processHandle, commandLine)
        {
            StdOut = stdOutHandle;
            StdErr = stdErrHandle;
            StdIn = stdInHandle;
            InterleavedBuffer = interleavedBuffer;
        }

        /// <summary>
        /// Initializes a new instance of the ProcessLaunchState class using the specified launch information, process,
        /// and command line.
        /// </summary>
        /// <param name="launchInfo">The launch information that describes how the process was started.</param>
        /// <param name="process">The Process object representing the running process.</param>
        /// <param name="commandLine">The full command line used to start the process.</param>
        internal ProcessLaunchState(ProcessLaunchInfo launchInfo, Process process, string commandLine) : this(launchInfo, process, (uint)process.Id, process.SafeHandle, commandLine)
        {
            CanDisposeProcessHandle = false;
        }

        /// <summary>
        /// Initializes a new instance of the ProcessLaunchState class with the specified process launch information,
        /// process data, and related handles.
        /// </summary>
        /// <remarks>If the process is assigned to a job object, this constructor sets up job object
        /// tracking and completion port association as needed. Throws an exception if any required argument is null or
        /// invalid.</remarks>
        /// <param name="launchInfo">The launch information describing how the process was started. Cannot be null.</param>
        /// <param name="process">The Process object representing the started process. Cannot be null.</param>
        /// <param name="processId">The unique identifier of the started process.</param>
        /// <param name="processHandle">A safe handle to the started process. Cannot be null.</param>
        /// <param name="commandLine">The full command line used to start the process. Cannot be null or whitespace.</param>
        internal ProcessLaunchState(ProcessLaunchInfo launchInfo, Process process, uint processId, SafeProcessHandle processHandle, string commandLine)
        {
            // Confirm all inputs are valid.
            ArgumentException.ThrowIfNullOrWhiteSpace(commandLine);
            ArgumentNullException.ThrowIfNull(processHandle);
            ArgumentNullException.ThrowIfNull(launchInfo);
            ArgumentNullException.ThrowIfNull(process);

            // Set up initial properties.
            ProcessSafeHandle = processHandle;
            CommandLine = commandLine;
            LaunchInfo = launchInfo;
            ProcessId = processId;
            Process = process;

            // Ensure we dispose of any resources we create if an exception is thrown during initialization.
            try
            {
                // Set up the necessary state for tracking child processes if requested.
                if (ProcessAssignedToJobObject)
                {
                    IoCompletionPort = NativeMethods.CreateIoCompletionPort(0);
                    JobObject = NativeMethods.CreateJobObject(null, default);
                    _ = NativeMethods.SetInformationJobObject(JobObject, new JOBOBJECT_ASSOCIATE_COMPLETION_PORT
                    {
                        CompletionPort = (HANDLE)IoCompletionPort.DangerousGetHandle(),
                        CompletionKey = null,
                    });
                    if (LaunchInfo.KillChildProcessesWithParent)
                    {
                        _ = NativeMethods.SetInformationJobObject(JobObject, new JOBOBJECT_EXTENDED_LIMIT_INFORMATION
                        {
                            BasicLimitInformation = new()
                            {
                                LimitFlags = JOB_OBJECT_LIMIT.JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE
                            }
                        });
                    }
                    _ = NativeMethods.AssignProcessToJobObject(JobObject, ProcessSafeHandle);
                }

                // Modify the process handle ACLs to deny user closure if requested.
                if (LaunchInfo.DenyUserTermination)
                {
                    // If the client/server process isn't ours, we'll want to change the owner to ourselves if we can.
                    RunAsActiveUser runAsActiveUser = LaunchInfo.RunAsActiveUser ?? AccountUtilities.CallerRunAsActiveUser; bool changeOwner = false;
                    if (runAsActiveUser.SID != AccountUtilities.CallerSid && PrivilegeManager.HasPrivilege(SE_PRIVILEGE.SeSecurityPrivilege) && PrivilegeManager.HasPrivilege(SE_PRIVILEGE.SeTakeOwnershipPrivilege))
                    {
                        PrivilegeManager.EnablePrivilegeIfDisabled(SE_PRIVILEGE.SeSecurityPrivilege);
                        PrivilegeManager.EnablePrivilegeIfDisabled(SE_PRIVILEGE.SeTakeOwnershipPrivilege);
                        changeOwner = true;
                    }

                    // Create a restricted access control list (ACL) for the client process so the user can't terminate it.
                    byte[] userSid = new byte[runAsActiveUser.SID.BinaryLength]; runAsActiveUser.SID.GetBinaryForm(userSid, 0);
                    using SafePinnedGCHandle pinnedUserSid = SafePinnedGCHandle.Alloc(userSid);
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
                                byte[] callerSid = new byte[AccountUtilities.CallerSid.BinaryLength]; AccountUtilities.CallerSid.GetBinaryForm(callerSid, 0);
                                using SafePinnedGCHandle pinnedCallerSid = SafePinnedGCHandle.Alloc(callerSid);
                                _ = NativeMethods.SetSecurityInfo(ProcessSafeHandle, SE_OBJECT_TYPE.SE_KERNEL_OBJECT, OBJECT_SECURITY_INFORMATION.OWNER_SECURITY_INFORMATION | OBJECT_SECURITY_INFORMATION.DACL_SECURITY_INFORMATION, pinnedCallerSid, null, pAcl, null);
                            }
                            else
                            {
                                _ = NativeMethods.SetSecurityInfo(ProcessSafeHandle, SE_OBJECT_TYPE.SE_KERNEL_OBJECT, OBJECT_SECURITY_INFORMATION.DACL_SECURITY_INFORMATION, null, null, pAcl, null);
                            }
                        }
                    }
                    finally
                    {
                        if (pinnedUserSidAddRef)
                        {
                            pinnedUserSid.DangerousRelease();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Dispose();
                ExceptionDispatchInfo.Capture(ex).Throw();
                throw;
            }
        }

        /// <summary>
        /// Represents the underlying process associated with this instance.
        /// </summary>
        internal readonly Process Process;

        /// <summary>
        /// Represents the identifier of the process associated with this instance.
        /// </summary>
        internal readonly uint ProcessId;

        /// <summary>
        /// Contains information about the process launch configuration.
        /// </summary>
        internal readonly ProcessLaunchInfo LaunchInfo;

        /// <summary>
        /// Gets the full command-line string associated with the current context.
        /// </summary>
        internal readonly string CommandLine;

        /// <summary>
        /// Asynchronously gets the result of the associated process execution.
        /// </summary>
        /// <returns>A task that resolves to the process execution result.</returns>
        internal Task<ProcessResult> GetProcessResultAsync()
        {
            // Internal implementation of the process result retrieval logic.
            async Task<ProcessResult> GetProcessResultAsyncImpl()
            {
                CancellationToken cancellationToken = LaunchInfo.CancellationToken ?? CancellationToken.None;
                uint timeoutExitCode = unchecked((uint)ProcessManager.TimeoutExitCode);
                int exitCode = ProcessManager.TimeoutExitCode;
                bool processFinished = false;
                if (ProcessAssignedToJobObject)
                {
                    if (IoCompletionPort is null)
                    {
                        throw new InvalidProgramException("The IO completion port is not initialized.");
                    }
                    if (JobObject is null)
                    {
                        throw new InvalidProgramException("The job object is not initialized.");
                    }
                    await Task.Run(() =>
                    {
                        using CancellationTokenRegistration? ctr = cancellationToken.CanBeCanceled ? cancellationToken.Register(() => NativeMethods.PostQueuedCompletionStatus(IoCompletionPort, timeoutExitCode, default)) : null;
                        while (true)
                        {
                            _ = NativeMethods.GetQueuedCompletionStatus(IoCompletionPort, out uint lpCompletionCode, out _, out nuint lpOverlapped, PInvoke.INFINITE);
                            if (lpCompletionCode == timeoutExitCode)
                            {
                                if (LaunchInfo.NoTerminateOnTimeout)
                                {
                                    // When KillChildProcessesWithParent is true, the job has JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE set.
                                    // Disposing the job would terminate the process we're supposed to let run, so we intentionally
                                    // leak the job handle in this specific scenario to honor the NoTerminateOnTimeout request.
                                    if (LaunchInfo.KillChildProcessesWithParent)
                                    {
                                        CanDisposeJobObject = false;
                                    }
                                    exitCode = ProcessManager.TimeoutExitCode;
                                    break;
                                }
                                _ = NativeMethods.TerminateJobObject(JobObject, timeoutExitCode);
                            }
                            else if ((lpCompletionCode == (uint)JOB_OBJECT_MSG.JOB_OBJECT_MSG_EXIT_PROCESS && !LaunchInfo.WaitForChildProcesses && (uint)lpOverlapped == ProcessId) || (lpCompletionCode == (uint)JOB_OBJECT_MSG.JOB_OBJECT_MSG_ACTIVE_PROCESS_ZERO))
                            {
                                _ = NativeMethods.GetExitCodeProcess(ProcessSafeHandle, out uint lpExitCode);
                                exitCode = unchecked((int)lpExitCode);
                                processFinished = true;
                                break;
                            }
                        }
                    }).ConfigureAwait(false);
                }
                else
                {
                    try
                    {
                        await Process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
                        processFinished = true; exitCode = Process.ExitCode;
                    }
                    catch (OperationCanceledException) when (cancellationToken.CanBeCanceled && cancellationToken.IsCancellationRequested)
                    {
                        if (!LaunchInfo.NoTerminateOnTimeout)
                        {
                            try
                            {
                                Process.Kill();
                            }
                            catch (InvalidOperationException)
                            {
                                // Already exited.
                            }
                            await Process.WaitForExitAsync().ConfigureAwait(false);
                            processFinished = true;
                        }
                        exitCode = ProcessManager.TimeoutExitCode;
                    }
                }
                if (processFinished)
                {
                    await WaitForStdIoTaskCompletionAsync(cancellationToken.CanBeCanceled && !cancellationToken.IsCancellationRequested ? cancellationToken : CancellationToken.None).ConfigureAwait(false);
                }
                return new(Process, LaunchInfo, CommandLine, exitCode, StdOut?.Buffer, StdErr?.Buffer, InterleavedBuffer);
            }

            // Ensure the object hasn't been disposed and return the cached task if it exists.
            lock (ProcessResultSyncRoot)
            {
                ObjectDisposedException.ThrowIf(Disposed != 0, this);
                return ProcessResultTask ??= GetProcessResultAsyncImpl();
            }
        }

        /// <summary>
        /// Asynchronously waits for the completion of all standard input, output, and error I/O tasks.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token used to cancel waiting for standard I/O completion.</param>
        /// <returns>A task that completes when all standard I/O tasks have finished or waiting is canceled.</returns>
        private async Task WaitForStdIoTaskCompletionAsync(CancellationToken cancellationToken)
        {
            await Task.WhenAll(StdOut?.Task ?? Task.CompletedTask, StdErr?.Task ?? Task.CompletedTask, StdIn?.Task ?? Task.CompletedTask).WaitAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Represents the safe handle for the associated process.
        /// </summary>
        private readonly SafeProcessHandle ProcessSafeHandle;

        /// <summary>
        /// Represents the handle to the associated I/O completion port, or null if no port is assigned.
        /// </summary>
        /// <remarks>This field is typically used to manage asynchronous I/O operations by associating a
        /// file handle with an I/O completion port. Access to this field should be performed with care to ensure proper
        /// resource management and thread safety.</remarks>
        private readonly SafeFileHandle? IoCompletionPort;

        /// <summary>
        /// Represents the handle to the associated Windows job object.
        /// </summary>
        /// <remarks>This handle is used to manage and control the lifetime and resource limits of
        /// processes grouped within the job object. The value may be null if no job object is associated.</remarks>
        private readonly SafeFileHandle? JobObject;

        /// <summary>
        /// Gets the handle used to read the standard output stream of the associated process asynchronously.
        /// </summary>
        /// <remarks>Use this handle to access the standard output produced by the process. Reading from
        /// this handle retrieves data written to the process's standard output stream.</remarks>
        private readonly ProcessReadStream? StdOut;

        /// <summary>
        /// Gets the handle used to read the standard error stream of the associated process.
        /// </summary>
        private readonly ProcessReadStream? StdErr;

        /// <summary>
        /// Represents the writable standard input stream for the associated process, if available.
        /// </summary>
        /// <remarks>This handle is only initialized if a standard input task is provided; otherwise, it
        /// is null. Use this handle to write input data to the process's standard input stream.</remarks>
        private readonly ProcessWriteStream? StdIn;

        /// <summary>
        /// Gets the collection of interleaved buffer strings associated with this instance.
        /// </summary>
        private readonly IReadOnlyCollection<string>? InterleavedBuffer;

        /// <summary>
        /// Indicates whether the object has been disposed (0 = not disposed, 1 = disposed).
        /// </summary>
        /// <remarks>This field is used with <see cref="Interlocked.Exchange(ref int, int)"/> to ensure
        /// thread-safe disposal and prevent multiple calls to the dispose logic.</remarks>
        private int Disposed;

        /// <summary>
        /// Indicates whether the process is currently assigned to a Windows job object.
        /// </summary>
        private bool ProcessAssignedToJobObject => LaunchInfo.WaitForChildProcesses || LaunchInfo.KillChildProcessesWithParent;

        /// <summary>
        /// Represents the cached task for obtaining process results.
        /// </summary>
        private Task<ProcessResult>? ProcessResultTask;

        /// <summary>
        /// Indicates whether the job can be disposed.
        /// </summary>
        private bool CanDisposeJobObject = true;

        /// <summary>
        /// Indicates whether the process handle can be disposed.
        /// </summary>
        private readonly bool CanDisposeProcessHandle = true;

        /// <summary>
        /// Synchronizes task initialization for process result retrieval.
        /// </summary>
        private readonly Lock ProcessResultSyncRoot = new();

        /// <summary>
        /// Releases all resources used by the current instance of the class.
        /// </summary>
        /// <remarks>Call this method when you are finished using the object to free unmanaged resources
        /// and perform other cleanup operations. After calling this method, the object should not be used
        /// further.</remarks>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the object and optionally releases the managed resources.
        /// </summary>
        /// <remarks>This method is typically called by the public Dispose method and the finalizer. When
        /// disposing is true, this method can dispose managed objects. When disposing is false, only unmanaged
        /// resources should be released.</remarks>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        private void Dispose(bool disposing)
        {
            // Return early if already disposed.
            if (Interlocked.Exchange(ref Disposed, 1) != 0 || !disposing)
            {
                return;
            }

            // Dispose of the process's associated resources.
            if (CanDisposeProcessHandle)
            {
                ProcessSafeHandle.Dispose();
            }
            StdOut?.Dispose();
            StdErr?.Dispose();
            StdIn?.Dispose();

            // Dispose of the I/O completion port and job object if they were created.
            if (JobObject is not null)
            {
                // Prevent the finalizer from closing the job handle, which would kill the processes
                // due to JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE. This intentionally leaks the handle.
                if (!CanDisposeJobObject)
                {
                    JobObject.SetHandleAsInvalid();
                }
                else
                {
                    JobObject.Dispose();
                }
            }
            IoCompletionPort?.Dispose();
        }
    }
}
