using System;

namespace PSADT.ProcessManagement
{
    /// <summary>
    /// Represents a process that needs to be closed.
    /// </summary>
    public sealed record ProcessToClose
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessToClose"/> class.
        /// </summary>
        /// <param name="runningProcess"></param>
        internal ProcessToClose(RunningProcess runningProcess)
        {
            Name = !string.IsNullOrWhiteSpace(runningProcess.Process.ProcessName) ? runningProcess.Process.ProcessName : throw new ArgumentNullException("RunningProcess Name cannot be null or empty.", (Exception?)null);
            Path = !string.IsNullOrWhiteSpace(runningProcess.FileName) ? runningProcess.FileName : throw new ArgumentNullException("RunningProcess Path cannot be null or empty.", (Exception?)null);
            Description = !string.IsNullOrWhiteSpace(runningProcess.Description) ? runningProcess.Description : throw new ArgumentNullException("RunningProcess Description cannot be null or empty.", (Exception?)null);
        }

        /// <summary>
        /// Gets the name of the process.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the path of the process.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Gets the description of the process.
        /// </summary>
        public string Description { get; }
    }
}
