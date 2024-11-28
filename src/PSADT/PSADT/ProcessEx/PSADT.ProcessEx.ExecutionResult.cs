using System.Collections.Generic;

namespace PSADT.ProcessEx
{
    /// <summary>
    /// Contains information about processes that were executed.
    /// </summary>
    public class ExecutionResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExecutionResult"/> class.
        /// </summary>
        public ExecutionResult()
        {
            ExitedProcessInfo = new List<ExecutionDetails>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExecutionResult"/> class with specified values.
        /// </summary>
        /// <param name="hasTimedOut">A value indicating whether the wait operation timed out.</param>
        /// <param name="exitedProcessInfo">The list of processes that exited, along with their exit details.</param>
        public ExecutionResult(bool hasTimedOut, List<ExecutionDetails> exitedProcessInfo)
        {
            HasTimedOut = hasTimedOut;
            ExitedProcessInfo = exitedProcessInfo ?? new List<ExecutionDetails>();
        }

        /// <summary>
        /// Gets a value indicating whether the wait operation timed out.
        /// </summary>
        public bool HasTimedOut { get; }

        /// <summary>
        /// Gets the list of processes that exited, along with their exit details.
        /// </summary>
        public List<ExecutionDetails> ExitedProcessInfo { get; }
    }
}
