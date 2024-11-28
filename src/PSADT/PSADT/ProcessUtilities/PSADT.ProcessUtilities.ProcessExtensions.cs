using System;
using System.Diagnostics;

namespace PSADT.ProcessUtilities
{
    /// <summary>
    /// Provides extension methods for the Process class.
    /// </summary>
    public static class ProcessExtensions
    {
        /// <summary>
        /// Gets the working directory of a process.
        /// </summary>
        /// <param name="process">The process to query.</param>
        /// <returns>The working directory path, or an empty string if it cannot be retrieved.</returns>
        /// <exception cref="ArgumentNullException">Thrown when process is null.</exception>
        public static string GetWorkingDirectory(this Process process)
        {
            if (process == null)
                throw new ArgumentNullException(nameof(process));

            return ProcessParameters.GetWorkingDirectory((uint)process.Id);
        }

        /// <summary>
        /// Gets the command line of a process.
        /// </summary>
        /// <param name="process">The process to query.</param>
        /// <returns>The command line string, or an empty string if it cannot be retrieved.</returns>
        /// <exception cref="ArgumentNullException">Thrown when process is null.</exception>
        public static string GetCommandLine(this Process process)
        {
            if (process == null)
                throw new ArgumentNullException(nameof(process));

            return ProcessParameters.GetCommandLine((uint)process.Id);
        }
    }
}