using System;

namespace PSADT.ProcessUtilities
{
    /// <summary>
    /// Contains detailed information about a process that has locks on files or directories.
    /// </summary>
    public class ProcessInfo
    {
        /// <summary>
        /// Gets or sets the process identifier.
        /// </summary>
        public int ProcessId { get; set; }

        /// <summary>
        /// Gets or sets the name of the process.
        /// </summary>
        public string ProcessName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the main window title of the process.
        /// </summary>
        public string MainWindowTitle { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the full path to the process executable.
        /// </summary>
        public string Path { get; set; } = "Access Denied";

        /// <summary>
        /// Gets or sets the user name under which the process is running.
        /// </summary>
        public string UserName { get; set; } = "Unknown";

        /// <summary>
        /// Gets or sets the process start time.
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Gets or sets the path(s) that this process has locked.
        /// Multiple paths are separated by newlines.
        /// </summary>
        public string LockedPath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the working directory of the process.
        /// </summary>
        public string WorkingDirectory { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the command line used to start the process.
        /// </summary>
        public string CommandLine { get; set; } = string.Empty;

        /// <summary>
        /// Returns a string representation of the ProcessInfo object.
        /// </summary>
        public override string ToString()
        {
            return $"{ProcessName} (PID: {ProcessId}) - {Path}";
        }
    }
}