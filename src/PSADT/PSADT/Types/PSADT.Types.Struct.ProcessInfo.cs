using System;

namespace PSADT.Types
{
    /// <summary>
    /// Represents detailed information about a running process.
    /// </summary>
    public readonly struct ProcessInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessInfo"/> struct.
        /// </summary>
        /// <param name="id">The unique identifier of the process.</param>
        /// <param name="handle">The handle to the process.</param>
        /// <param name="processName">The name of the process.</param>
        public ProcessInfo(int id, IntPtr handle, string processName)
        {
            Id = id;
            Handle = handle;
            ProcessName = processName ?? string.Empty;
        }

        /// <summary>
        /// Gets the unique identifier of the process.
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// Gets the handle to the process.
        /// </summary>
        public IntPtr Handle { get; }

        /// <summary>
        /// Gets the name of the process.
        /// </summary>
        public string ProcessName { get; }

        /// <summary>
        /// Returns a string representation of the current <see cref="ProcessInfo"/> object.
        /// </summary>
        /// <returns>A formatted string containing the process information.</returns>
        public override string ToString()
        {
            return $"Process ID: {Id}, Name: {ProcessName}, Handle: {Handle}";
        }
    }
}
