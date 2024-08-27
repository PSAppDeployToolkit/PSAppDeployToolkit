using System;

namespace PSADT.ProcessEx
{
    /// <summary>
    /// Contains details about an exited process.
    /// </summary>
    public class ExecutionDetails
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExecutionDetails"/> class with specified values.
        /// </summary>
        /// <param name="processName">The name of the process.</param>
        /// <param name="processId">The process id of the process.</param>
        /// <param name="exitCode">The exit code of the process.</param>
        /// <param name="sessionId">The session id where the process was executed.</param>
        /// <param name="username">The username of the session where the process was executed.</param>
        public ExecutionDetails(string processName, int processId, int exitCode, uint sessionId, string username)
        {
            ProcessName = processName;
            ExitCode = exitCode;
            SessionId = sessionId;
            Username = username ?? throw new ArgumentNullException(nameof(username), "Username cannot be null.");
        }

        /// <summary>
        /// Gets the name of the process.
        /// </summary>
        public string ProcessName { get; }

        /// <summary>
        /// Gets the process id of the process.
        /// </summary>
        public int ProcessId { get; }

        /// <summary>
        /// Gets the exit code of the process.
        /// </summary>
        public int ExitCode { get; }

        /// <summary>
        /// Gets the session id where the process was executed.
        /// </summary>
        public uint SessionId { get; }

        /// <summary>
        /// Gets the username of the session where the process was executed.
        /// </summary>
        public string Username { get; }
    }
}
