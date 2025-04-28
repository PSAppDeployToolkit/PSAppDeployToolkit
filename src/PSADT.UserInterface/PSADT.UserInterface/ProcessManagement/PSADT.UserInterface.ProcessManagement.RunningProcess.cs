using System.Diagnostics;

namespace PSADT.UserInterface.ProcessManagement
{
    /// <summary>
    /// Represents a running process.
    /// </summary>
    public sealed class RunningProcess
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RunningProcess"/> class with specified properties.
        /// </summary>
        /// <param name="process"></param>
        /// <param name="description"></param>
        /// <param name="fileName"></param>
        /// <param name="arguments"></param>
        internal RunningProcess(Process process, string description, string fileName, string? arguments)
        {
            Process = process;
            Description = description;
            FileName = fileName;
            if (!string.IsNullOrWhiteSpace(arguments))
            {
                Arguments = arguments;
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
