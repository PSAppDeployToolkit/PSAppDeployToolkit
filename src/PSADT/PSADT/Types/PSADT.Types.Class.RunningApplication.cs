using System.Diagnostics;
using Microsoft.Management.Infrastructure;

namespace PSADT.Types
{
    /// <summary>
    /// Represents a running application.
    /// </summary>
    public sealed class RunningApplication
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RunningApplication"/> class with specified properties.
        /// </summary>
        /// <param name="process"></param>
        /// <param name="cimInstance"></param>
        /// <param name="description"></param>
        /// <param name="commandLine"></param>
        /// <param name="arguments"></param>
        public RunningApplication(Process process, CimInstance cimInstance, string description, string commandLine, string arguments)
        {
            Process = process;
            CimInstance = cimInstance;
            Description = description;
            CommandLine = commandLine;
            if (!string.IsNullOrWhiteSpace(arguments))
            {
                Arguments = arguments;
            }
        }

        /// <summary>
        /// Gets the process associated with the running application.
        /// </summary>
        public readonly Process Process;

        /// <summary>
        /// Gets the CIM instance associated with the running application.
        /// </summary>
        public readonly CimInstance CimInstance;

        /// <summary>
        /// Gets the description of the running application.
        /// </summary>
        public readonly string Description;

        /// <summary>
        /// Gets the command line used to start the running application.
        /// </summary>
        public readonly string CommandLine;

        /// <summary>
        /// Gets the arguments passed to the running application.
        /// </summary>
        public readonly string? Arguments;
    }
}
