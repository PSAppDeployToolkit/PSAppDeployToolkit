using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace PSADT.ProcessManagement
{
    /// <summary>
    /// Represents a running process.
    /// </summary>
    public sealed record RunningProcess
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RunningProcess"/> class with specified properties.
        /// </summary>
        /// <param name="process"></param>
        /// <param name="description"></param>
        /// <param name="fileName"></param>
        /// <param name="arguments"></param>
        internal RunningProcess(Process process, string description, string fileName, IEnumerable<string>? arguments)
        {
            Process = process ?? throw new System.ArgumentNullException("Process cannot be null.", (Exception?)null);
            Description = !string.IsNullOrWhiteSpace(description) ? description : throw new ArgumentNullException("Description cannot be null or empty.", (Exception?)null);
            FileName = !string.IsNullOrWhiteSpace(fileName) ? fileName : throw new ArgumentNullException("FileName cannot be null or empty.", (Exception?)null);
            if (null != arguments)
            {
                Arguments = ProcessUtilities.ArgvToCommandLine(arguments);
            }
        }

        /// <summary>
        /// Gets the process associated with the running process.
        /// </summary>
        public readonly Process Process;

        /// <summary>
        /// Gets the description of the running process.
        /// </summary>
        public readonly string Description;

        /// <summary>
        /// Gets the file path of the running process.
        /// </summary>
        public readonly string FileName;

        /// <summary>
        /// Gets the arguments passed to the running process.
        /// </summary>
        public readonly string? Arguments;
    }
}
