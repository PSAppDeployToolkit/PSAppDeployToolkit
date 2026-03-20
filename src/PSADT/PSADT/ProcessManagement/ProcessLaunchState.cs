using System;
using System.Diagnostics;
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
        /// Initializes a new instance of the ProcessLaunchState class with the specified process launch information,
        /// process data, and related handles.
        /// </summary>
        /// <remarks>If the process is assigned to a job object, this constructor sets up job object
        /// tracking and completion port association as needed. Throws an exception if any required argument is null or
        /// invalid.</remarks>
        /// <param name="launchInfo">The launch information describing how the process was started. Cannot be null.</param>
        /// <param name="launchData">Optional additional data associated with the process launch, or null if not applicable.</param>
        /// <param name="process">The Process object representing the started process. Cannot be null.</param>
        /// <param name="processId">The unique identifier of the started process.</param>
        /// <param name="processHandle">A safe handle to the started process. Cannot be null.</param>
        /// <param name="commandLine">The full command line used to start the process. Cannot be null or whitespace.</param>
        internal ProcessLaunchState(ProcessLaunchInfo launchInfo, ProcessLaunchData? launchData, Process process, uint processId, SafeProcessHandle processHandle, string commandLine)
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
            LaunchData = launchData;
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
            catch
            {
                Dispose();
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
        /// Gets the result of the associated process execution.
        /// </summary>
        internal ProcessResult ProcessResult
        {
            get
            {
                // Return the backing field if we've already computed the result.
                if (field is not null)
                {
                    return field;
                }

                // Spin until complete or cancelled.
                uint timeoutExitCode = unchecked((uint)ProcessManager.TimeoutExitCode);
                int exitCode = ProcessManager.TimeoutExitCode;
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
                    using CancellationTokenRegistration? ctr = LaunchInfo.CancellationToken?.Register(() => NativeMethods.PostQueuedCompletionStatus(IoCompletionPort, timeoutExitCode, default));
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
                            LaunchData?.WaitForStdIoTaskCompletion();
                            _ = NativeMethods.GetExitCodeProcess(ProcessSafeHandle, out uint lpExitCode);
                            exitCode = unchecked((int)lpExitCode);
                            break;
                        }
                    }
                }
                else
                {
                    Process.WaitForExit();
                    LaunchData?.WaitForStdIoTaskCompletion();
                    exitCode = Process.ExitCode;
                }
                return field = LaunchData is not null
                    ? new(Process, LaunchInfo, CommandLine, exitCode, LaunchData.StdOut, LaunchData.StdErr, LaunchData.Interleaved)
                    : new(Process, LaunchInfo, CommandLine, exitCode, [], [], []);
            }
        }

        /// <summary>
        /// Gets the data used to launch the associated process.
        /// </summary>
        private readonly ProcessLaunchData? LaunchData;

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
        /// Indicates whether the object has been disposed.
        /// </summary>
        /// <remarks>This field is typically used to prevent multiple calls to the dispose logic and to
        /// detect object usage after disposal.</remarks>
        private bool Disposed;

        /// <summary>
        /// Indicates whether the job can be disposed.
        /// </summary>
        private bool CanDisposeJobObject = true;

        /// <summary>
        /// Indicates whether the process is currently assigned to a Windows job object.
        /// </summary>
        private bool ProcessAssignedToJobObject => LaunchInfo.WaitForChildProcesses || LaunchInfo.KillChildProcessesWithParent || LaunchInfo.CancellationToken.HasValue;

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
            if (!Disposed)
            {
                if (disposing)
                {
                    // Dispose of the process's associated resources.
                    if (LaunchData is not null)
                    {
                        ProcessSafeHandle.Dispose();
                        LaunchData.Dispose();
                    }

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
                Disposed = true;
            }
        }
    }
}
