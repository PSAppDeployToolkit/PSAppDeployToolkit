using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Windows.Win32.UI.WindowsAndMessaging;

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
        /// <param name="workingDirectory"></param>
        /// <param name="username"></param>
        /// <param name="useLinkedAdminToken"></param>
        /// <param name="useShellExecute"></param>
        /// <param name="verb"></param>
        /// <param name="windowStyle"></param>
        /// <param name="noNewWindow"></param>
        /// <param name="priorityClass"></param>
        /// <param name="cancellationToken"></param>
        public ProcessOptions(string filePath, string[]? argumentList = null, string? workingDirectory = null, string? username = null, bool useLinkedAdminToken = false, bool inheritEnvironmentVariables = false, bool useShellExecute = false, string? verb = null, ProcessWindowStyle windowStyle = ProcessWindowStyle.Normal, bool noNewWindow = false, ProcessPriorityClass priorityClass = ProcessPriorityClass.Normal, CancellationToken cancellationToken = default)
        {
            if (!string.IsNullOrWhiteSpace(workingDirectory))
            {
                workingDirectory = workingDirectory!.Trim();
            }
            else if (Path.GetDirectoryName(filePath) is string fileDir && !string.IsNullOrWhiteSpace(fileDir))
            {
                workingDirectory = fileDir;
            }

            if ((null != argumentList) && (string.Join(" ", argumentList.Select(x => x.Trim())).Trim() is string args) && !string.IsNullOrWhiteSpace(args))
            {
                Arguments = args;
            }

            if (!string.IsNullOrWhiteSpace(username))
            {
                Username = username;
            }

            if (!string.IsNullOrWhiteSpace(verb))
            {
                Verb = verb;
            }

            FilePath = filePath;
            UseLinkedAdminToken = useLinkedAdminToken;
            InheritEnvironmentVariables = inheritEnvironmentVariables;
            UseShellExecute = useShellExecute;
            WindowStyle = WindowStyleMap[windowStyle];
            NoNewWindow = noNewWindow;
            PriorityClass = priorityClass;
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
        /// Translator for ProcessWindowStyle to the corresponding value for CreateProcess.
        /// </summary>
        private static readonly ReadOnlyDictionary<ProcessWindowStyle, ushort> WindowStyleMap = new ReadOnlyDictionary<ProcessWindowStyle, ushort>(new Dictionary<ProcessWindowStyle, ushort>
        {
            { ProcessWindowStyle.Normal, (ushort)SHOW_WINDOW_CMD.SW_SHOWNORMAL },
            { ProcessWindowStyle.Hidden, (ushort)SHOW_WINDOW_CMD.SW_HIDE },
            { ProcessWindowStyle.Minimized, (ushort)SHOW_WINDOW_CMD.SW_SHOWMINIMIZED },
            { ProcessWindowStyle.Maximized, (ushort)SHOW_WINDOW_CMD.SW_SHOWMAXIMIZED }
        });

        /// <summary>
        /// Prepares a null-terminated character array of arguments for CreateProcess.
        /// </summary>
        /// <returns></returns>
        public string GetCreateProcessCommandLine()
        {
            return $"\"{FilePath}\"{((null != Arguments) ? $" {Arguments}" : null)}\0";
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
        /// Gets the working directory of the process.
        /// </summary>
        public readonly string? WorkingDirectory = null;

        /// <summary>
        /// Gets the username to use when starting the process.
        /// </summary>
        public readonly string? Username = null;

        /// <summary>
        /// Gets a value indicating whether to use the linked admin token to start the process.
        /// </summary>
        public readonly bool UseLinkedAdminToken;

        /// <summary>
        /// Gets a value indicating whether to inherit the environment variables of the current process.
        /// </summary>
        public readonly bool InheritEnvironmentVariables;

        /// <summary>
        /// Gets a value indicating whether to use the shell to execute the process.
        /// </summary>
        public readonly bool UseShellExecute;

        /// <summary>
        /// Gets the verb to use when starting the process.
        /// </summary>
        public readonly string? Verb = null;

        /// <summary>
        /// Gets the window style of the process.
        /// </summary>
        public readonly ushort WindowStyle;

        /// <summary>
        /// Gets a value indicating whether to create a new window for the process.
        /// </summary>
        public readonly bool NoNewWindow;

        /// <summary>
        /// Gets the priority class of the process.
        /// </summary>
        public readonly ProcessPriorityClass PriorityClass;

        /// <summary>
        /// Gets the cancellation token to cancel the process.
        /// </summary>
        public readonly CancellationToken CancellationToken;
    }
}
