using System;
using PSADT.Interop.Extensions;

namespace PSADT.ProcessManagement
{
    /// <summary>
    /// Represents a process that needs to be closed.
    /// </summary>
    public sealed record ProcessToClose
    {
        /// <summary>
        /// Initializes a new instance of the ProcessToClose class using the specified running process information.
        /// </summary>
        /// <param name="runningProcessInfo">The running process information used to initialize the process name, path, and description. Cannot be null,
        /// and all properties must contain non-empty values.</param>
        /// <exception cref="ArgumentNullException">Thrown if the process name, path, or description in <paramref name="runningProcessInfo"/> is null or empty.</exception>
        internal ProcessToClose(RunningProcessInfo runningProcessInfo)
        {
            Name = runningProcessInfo.Process.ProcessName.ThrowIfNullOrWhiteSpace();
            Path = runningProcessInfo.FileName.ThrowIfNullOrWhiteSpace();
            Description = runningProcessInfo.Description.ThrowIfNullOrWhiteSpace();
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
