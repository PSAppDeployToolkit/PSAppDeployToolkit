using System.Diagnostics;
using Microsoft.Management.Infrastructure;

namespace PSADT.Types
{
    public class RunningApplication
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
        public Process Process { get; }

        /// <summary>
        /// Gets the CIM instance associated with the running application.
        /// </summary>
        public CimInstance CimInstance { get; }

        /// <summary>
        /// Gets the description of the running application.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Gets the command line used to start the running application.
        /// </summary>
        public string CommandLine { get; }

        /// <summary>
        /// Gets the arguments passed to the running application.
        /// </summary>
        public string? Arguments { get; }
    }
}
