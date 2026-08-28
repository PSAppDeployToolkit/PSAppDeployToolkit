using System;
using System.IO;

namespace PSADT.ProcessManagement
{
    /// <summary>
    /// Represents a process that needs to be closed.
    /// </summary>
    public sealed record class ProcessToClose
    {
        /// <summary>
        /// Initializes a new instance of the ProcessToClose class using the specified running process information.
        /// </summary>
        /// <param name="runningProcessInfo">The running process information used to initialize the process name, path, and description. Cannot be null,
        /// and all properties must contain non-empty values.</param>
        /// <exception cref="ArgumentNullException">Thrown if the process name, path, or description in <paramref name="runningProcessInfo"/> is null or empty.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S3236:Caller information arguments should not be provided explicitly", Justification = "This is intentional as we're testing a parameter member.")]
        internal ProcessToClose(RunningProcessInfo runningProcessInfo)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(runningProcessInfo.Process.ProcessName, nameof(runningProcessInfo));
            ArgumentException.ThrowIfNullOrWhiteSpace(runningProcessInfo.Description, nameof(runningProcessInfo));
            ArgumentNullException.ThrowIfNull(runningProcessInfo.FileName, nameof(runningProcessInfo));
            Name = runningProcessInfo.Process.ProcessName;
            PathValue = runningProcessInfo.FileName.FullName;
            Description = runningProcessInfo.Description;
        }

        /// <summary>
        /// Gets the name of the process.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the path of the process.
        /// </summary>
        /// <remarks>Recorded as a path and rebuilt on each read. A <see cref="FileInfo"/> does not override
        /// equality, so holding one directly would make this record compare by reference and two descriptions of
        /// the same process would never match.</remarks>
        public FileInfo Path => new(PathValue);

        /// <summary>
        /// Gets the description of the process.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// The path recorded for <see cref="Path"/>.
        /// </summary>
        private readonly string PathValue;
    }
}
