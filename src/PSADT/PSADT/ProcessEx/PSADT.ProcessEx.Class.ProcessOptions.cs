using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Threading;

namespace PSADT.ProcessEx
{
    /// <summary>
    /// Provides options for launching a managed process.
    /// </summary>
    public class ProcessOptions
    {
        /// <summary>
        /// Initializes a new instance of the ManagedProcessOptions class.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="argumentList"></param>
        /// <param name="windowStyle"></param>
        /// <param name="noNewWindow"></param>
        /// <param name="workingDirectory"></param>
        /// <param name="priorityClass"></param>
        /// <param name="useShellExecute"></param>
        /// <param name="standardInput"></param>
        /// <param name="cancellationToken"></param>
        public ProcessOptions(string filePath, string[]? argumentList = null, string? workingDirectory = null, ProcessPriorityClass priorityClass = ProcessPriorityClass.Normal, ProcessWindowStyle windowStyle = ProcessWindowStyle.Normal, bool noNewWindow = false, bool useShellExecute = false, string? standardInput = null, CancellationToken cancellationToken = default)
        {
            if ((null != workingDirectory) && !string.IsNullOrWhiteSpace(workingDirectory))
            {
                workingDirectory = workingDirectory.Trim();
            }
            else if (Path.GetDirectoryName(filePath) is string fileDir && !string.IsNullOrWhiteSpace(fileDir))
            {
                workingDirectory = fileDir;
            }

            if ((null != argumentList) && (string.Join(" ", argumentList.Select(x => x.Trim())).Trim() is string args) && !string.IsNullOrWhiteSpace(args))
            {
                Arguments = args;
            }
            if ((null != standardInput) && !string.IsNullOrWhiteSpace(standardInput))
            {
                StandardInput = standardInput.Trim();
            }

            FilePath = filePath;
            PriorityClass = priorityClass;
            WindowStyle = windowStyle;
            NoNewWindow = noNewWindow;
            UseShellExecute = useShellExecute;
            CancellationToken = cancellationToken;
        }

        /// <summary>
        /// Initializes a new instance of the ManagedProcessOptions class.
        /// </summary>
        /// <param name="filePath"></param>
        public ProcessOptions(string filePath) : this(filePath, null!) { }

        /// <summary>
        /// Initializes a new instance of the ManagedProcessOptions class.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="argumentList"></param>
        public ProcessOptions(string filePath, string[] argumentList) : this(filePath, argumentList, null) { }

        /// <summary>
        /// Prepares a null-terminated character array of arguments for CreateProcess.
        /// </summary>
        /// <returns></returns>
        public char[]? GetArgsForCreateProcess()
        {
            return (null != Arguments) ? (Arguments + "\0").ToCharArray() : null;
        }

        /// <summary>
        /// Gets the file path of the process to launch.
        /// </summary>
        public readonly string FilePath;

        /// <summary>
        /// Gets the arguments to pass to the process.
        /// </summary>
        public readonly string? Arguments = null;

        /// <summary>
        /// Gets the window style of the process.
        /// </summary>
        public readonly ProcessWindowStyle WindowStyle;

        /// <summary>
        /// Gets a value indicating whether to create a new window for the process.
        /// </summary>
        public readonly bool NoNewWindow;

        /// <summary>
        /// Gets the working directory of the process.
        /// </summary>
        public readonly string? WorkingDirectory = null;

        /// <summary>
        /// Gets the priority class of the process.
        /// </summary>
        public readonly ProcessPriorityClass PriorityClass;

        /// <summary>
        /// Gets a value indicating whether to use the shell to execute the process.
        /// </summary>
        public readonly bool UseShellExecute;

        /// <summary>
        /// Gets the standard input to pass to the process.
        /// </summary>
        public readonly string? StandardInput = null;

        /// <summary>
        /// Gets the cancellation token to cancel the process.
        /// </summary>
        public readonly CancellationToken CancellationToken;
    }
}
