namespace PSADT.Interop
{
    /// <summary>
    /// Represents the various messages that can be sent by the system to a job object.
    /// </summary>
    /// <remarks>These messages are used to notify about specific events or conditions related to the
    /// processes associated with a job object. Each message corresponds to a particular event, such as a process
    /// exceeding a time limit or the job reaching its memory limit. These notifications can be used to monitor and
    /// manage the behavior of processes within a job.</remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1712:Do not prefix enum values with type name", Justification = "These values are precisely as they're defined in the Win32 API.")]
    internal enum JOB_OBJECT_MSG : uint
    {
        /// <summary>
        /// Indicates that a process associated with the job exited with an exit code that indicates an abnormal exit (see the list following this table).
        /// </summary>
        JOB_OBJECT_MSG_ABNORMAL_EXIT_PROCESS = Windows.Win32.PInvoke.JOB_OBJECT_MSG_ABNORMAL_EXIT_PROCESS,

        /// <summary>
        /// Indicates that the active process limit has been exceeded.
        /// </summary>
        JOB_OBJECT_MSG_ACTIVE_PROCESS_LIMIT = Windows.Win32.PInvoke.JOB_OBJECT_MSG_ACTIVE_PROCESS_LIMIT,

        /// <summary>
        /// Indicates that the active process count has been decremented to 0. For example, if the job currently has two active processes, the system sends this message after they both terminate.
        /// </summary>
        JOB_OBJECT_MSG_ACTIVE_PROCESS_ZERO = Windows.Win32.PInvoke.JOB_OBJECT_MSG_ACTIVE_PROCESS_ZERO,

        /// <summary>
        /// Indicates that the JOB_OBJECT_POST_AT_END_OF_JOB option is in effect and the end-of-job time limit has been reached. Upon posting this message, the time limit is canceled and the job's processes can continue to run.
        /// </summary>
        JOB_OBJECT_MSG_END_OF_JOB_TIME = Windows.Win32.PInvoke.JOB_OBJECT_MSG_END_OF_JOB_TIME,

        /// <summary>
        /// Indicates that a process has exceeded a per-process time limit. The system sends this message after the process termination has been requested.
        /// </summary>
        JOB_OBJECT_MSG_END_OF_PROCESS_TIME = Windows.Win32.PInvoke.JOB_OBJECT_MSG_END_OF_PROCESS_TIME,

        /// <summary>
        /// Indicates that a process associated with the job has exited.
        /// </summary>
        JOB_OBJECT_MSG_EXIT_PROCESS = Windows.Win32.PInvoke.JOB_OBJECT_MSG_EXIT_PROCESS,

        /// <summary>
        /// Indicates that a process associated with the job caused the job to exceed the job-wide memory limit (if one is in effect).
        /// </summary>
        JOB_OBJECT_MSG_JOB_MEMORY_LIMIT = Windows.Win32.PInvoke.JOB_OBJECT_MSG_JOB_MEMORY_LIMIT,

        /// <summary>
        /// Indicates that a process has been added to the job. Processes added to a job at the time a completion port is associated are also reported.
        /// </summary>
        JOB_OBJECT_MSG_NEW_PROCESS = Windows.Win32.PInvoke.JOB_OBJECT_MSG_NEW_PROCESS,

        /// <summary>
        /// Indicates that a process associated with a job that has registered for resource limit notifications has exceeded one or more limits. Use the QueryInformationJobObject function with JobObjectLimitViolationInformation to determine which limit was exceeded.
        /// </summary>
        JOB_OBJECT_MSG_NOTIFICATION_LIMIT = Windows.Win32.PInvoke.JOB_OBJECT_MSG_NOTIFICATION_LIMIT,

        /// <summary>
        /// Indicates that a process associated with the job has exceeded its memory limit (if one is in effect).
        /// </summary>
        JOB_OBJECT_MSG_PROCESS_MEMORY_LIMIT = Windows.Win32.PInvoke.JOB_OBJECT_MSG_PROCESS_MEMORY_LIMIT,
    }
}
