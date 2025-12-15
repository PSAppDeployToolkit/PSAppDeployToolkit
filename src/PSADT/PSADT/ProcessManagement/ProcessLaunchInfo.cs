using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using PSADT.LibraryInterfaces;
using PSADT.Core;

namespace PSADT.ProcessManagement
{
    /// <summary>
    /// Provides options for launching a managed process.
    /// </summary>
    public sealed record ProcessLaunchInfo
    {
        /// <summary>
        /// Initializes a new instance of the ManagedProcessOptions class.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="argumentList"></param>
        /// <param name="workingDirectory"></param>
        /// <param name="runAsActiveUser"></param>
        /// <param name="useLinkedAdminToken"></param>
        /// <param name="useHighestAvailableToken"></param>
        /// <param name="inheritEnvironmentVariables"></param>
        /// <param name="expandEnvironmentVariables"></param>
        /// <param name="denyUserTermination"></param>
        /// <param name="inheritHandles"></param>
        /// <param name="useUnelevatedToken"></param>
        /// <param name="useShellExecute"></param>
        /// <param name="verb"></param>
        /// <param name="createNoWindow"></param>
        /// <param name="waitForChildProcesses"></param>
        /// <param name="killChildProcessesWithParent"></param>
        /// <param name="streamEncoding"></param>
        /// <param name="windowStyle"></param>
        /// <param name="priorityClass"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="noTerminateOnTimeout"></param>
        /// <exception cref="ArgumentException"></exception>
        public ProcessLaunchInfo(
            string filePath,
            IEnumerable<string>? argumentList = null,
            string? workingDirectory = null,
            RunAsActiveUser? runAsActiveUser = null,
            bool useLinkedAdminToken = false,
            bool useHighestAvailableToken = false,
            bool inheritEnvironmentVariables = false,
            bool expandEnvironmentVariables = false,
            bool denyUserTermination = false,
            bool inheritHandles = false,
            bool useUnelevatedToken = false,
            bool useShellExecute = false,
            string? verb = null,
            bool createNoWindow = false,
            bool waitForChildProcesses = false,
            bool killChildProcessesWithParent = false,
            Encoding? streamEncoding = null,
            System.Diagnostics.ProcessWindowStyle? windowStyle = null,
            System.Diagnostics.ProcessPriorityClass? priorityClass = null,
            CancellationToken? cancellationToken = null,
            bool noTerminateOnTimeout = false)
        {
            // Handle file paths that may be wrapped in quotes.
            if (filePath is null)
            {
                throw new ArgumentNullException(nameof(filePath), "File path cannot be null.");
            }
            FilePath = filePath.StartsWith("\"", StringComparison.OrdinalIgnoreCase) && filePath.EndsWith("\"", StringComparison.OrdinalIgnoreCase) ? filePath.TrimStart('"').TrimEnd('"') : filePath;

            // Validate the file path is rooted.
            if (!Path.IsPathRooted(FilePath) && !useShellExecute && !FilePath.StartsWith("%", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("File path must be fully qualified.", nameof(filePath));
            }

            // Validate all nullable parameters.
            if (!string.IsNullOrWhiteSpace(workingDirectory))
            {
                WorkingDirectory = workingDirectory!.Trim();
            }
            if (argumentList?.Any() == true)
            {
                ArgumentList = new ReadOnlyCollection<string>([.. argumentList]);
            }
            if (!string.IsNullOrWhiteSpace(verb))
            {
                Verb = verb;
            }
            if (streamEncoding is not null)
            {
                StreamEncoding = streamEncoding;
            }
            if (windowStyle is not null)
            {
                WindowStyle = WindowStyleMap[windowStyle.Value];
                ProcessWindowStyle = windowStyle.Value;
            }
            if (priorityClass is not null)
            {
                PriorityClass = priorityClass.Value;
            }
            if (cancellationToken is not null)
            {
                CancellationToken = cancellationToken.Value;
            }

            // Handle the CreateNoWindow parameter.
            if (createNoWindow)
            {
                WindowStyle = WindowStyleMap[System.Diagnostics.ProcessWindowStyle.Hidden];
                ProcessWindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                CreateNoWindow = true;
            }

            // Set remaining parameters.
            RunAsActiveUser = runAsActiveUser;
            UseLinkedAdminToken = useLinkedAdminToken;
            UseHighestAvailableToken = useHighestAvailableToken;
            InheritEnvironmentVariables = inheritEnvironmentVariables;
            ExpandEnvironmentVariables = expandEnvironmentVariables;
            DenyUserTermination = denyUserTermination;
            InheritHandles = inheritHandles;
            UseUnelevatedToken = useUnelevatedToken;
            UseShellExecute = useShellExecute;
            WaitForChildProcesses = waitForChildProcesses;
            KillChildProcessesWithParent = killChildProcessesWithParent;
            NoTerminateOnTimeout = noTerminateOnTimeout;
        }

        /// <summary>
        /// Translator for ProcessWindowStyle to the corresponding value for CreateProcess.
        /// </summary>
        private static readonly ReadOnlyDictionary<System.Diagnostics.ProcessWindowStyle, SHOW_WINDOW_CMD> WindowStyleMap = new(new Dictionary<System.Diagnostics.ProcessWindowStyle, SHOW_WINDOW_CMD>()
        {
            { System.Diagnostics.ProcessWindowStyle.Normal, SHOW_WINDOW_CMD.SW_SHOWNORMAL },
            { System.Diagnostics.ProcessWindowStyle.Hidden, SHOW_WINDOW_CMD.SW_HIDE },
            { System.Diagnostics.ProcessWindowStyle.Minimized, SHOW_WINDOW_CMD.SW_SHOWMINIMIZED },
            { System.Diagnostics.ProcessWindowStyle.Maximized, SHOW_WINDOW_CMD.SW_SHOWMAXIMIZED },
        });

        /// <summary>
        /// Gets the file path of the process to launch.
        /// </summary>
        public string FilePath { get; }

        /// <summary>
        /// Gets the arguments to pass to the process.
        /// </summary>
        public IReadOnlyList<string>? ArgumentList { get; }

        /// <summary>
        /// Gets the working directory of the process.
        /// </summary>
        public string? WorkingDirectory { get; }

        /// <summary>
        /// Gets the username to use when starting the process.
        /// </summary>
        public RunAsActiveUser? RunAsActiveUser { get; }

        /// <summary>
        /// Gets a value indicating whether to use the linked admin token to start the process.
        /// </summary>
        public bool UseLinkedAdminToken { get; }

        /// <summary>
        /// Gets a value indicating whether to use the highest available token to start the process.
        /// </summary>
        public bool UseHighestAvailableToken { get; }

        /// <summary>
        /// Gets a value indicating whether to inherit the environment variables of the current process.
        /// </summary>
        public bool InheritEnvironmentVariables { get; }

        /// <summary>
        /// Indicates whether environment variables in the input should be expanded.
        /// </summary>
        public bool ExpandEnvironmentVariables { get; }

        /// <summary>
        /// Indicates whether user termination is denied.
        /// </summary>
        public bool DenyUserTermination { get; }

        /// <summary>
        /// Gets a value indicating whether anonymous handles are being used.
        /// </summary>
        public bool InheritHandles { get; }

        /// <summary>
        /// Indicates whether an unelevated token should be used for operations.
        /// </summary>
        public bool UseUnelevatedToken { get; }

        /// <summary>
        /// Gets a value indicating whether to use the shell to execute the process.
        /// </summary>
        public bool UseShellExecute { get; }

        /// <summary>
        /// Gets the verb to use when starting the process.
        /// </summary>
        public string? Verb { get; }

        /// <summary>
        /// Gets a value indicating whether to create a new window for the process.
        /// </summary>
        public bool CreateNoWindow { get; }

        /// <summary>
        /// Gets a value indicating whether the process should wait for child processes to exit before completing.
        /// </summary>
        public bool WaitForChildProcesses { get; }

        /// <summary>
        /// Gets a value indicating whether any child processes spawned with the parent should terminate when the parent closes.
        /// </summary>
        public bool KillChildProcessesWithParent { get; }

        /// <summary>
        /// Gets the encoding type to use when parsing stdout/stderr text.
        /// </summary>
        public Encoding StreamEncoding { get; } = Encoding.Default;

        /// <summary>
        /// Gets the window style of the process.
        /// </summary>
        public SHOW_WINDOW_CMD? WindowStyle { get; }

        /// <summary>
        /// Gets the window style of the process.
        /// </summary>
        public System.Diagnostics.ProcessWindowStyle? ProcessWindowStyle { get; }

        /// <summary>
        /// Gets the priority class of the process.
        /// </summary>
        public System.Diagnostics.ProcessPriorityClass? PriorityClass { get; }

        /// <summary>
        /// Gets the cancellation token to cancel the process.
        /// </summary>
        public CancellationToken? CancellationToken { get; }

        /// <summary>
        /// Gets whether to not end the process upon CancellationToken expiring.
        /// </summary>
        public bool NoTerminateOnTimeout { get; }
    }
}
