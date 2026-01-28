using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using PSADT.FileSystem;
using PSADT.Foundation;
using PSADT.LibraryInterfaces;

namespace PSADT.ProcessManagement
{
    /// <summary>
    /// Provides options for launching a managed process.
    /// </summary>
    public sealed record ProcessLaunchInfo
    {
        /// <summary>
        /// Initializes a new instance of the ProcessLaunchInfo class with the specified process launch parameters.
        /// </summary>
        /// <param name="filePath">The fully qualified path to the executable file to launch. Cannot be null. If not using shell execute and
        /// not starting with '%', the path must be rooted.</param>
        /// <param name="argumentList">An optional collection of command-line arguments to pass to the process. If null or empty, no arguments are
        /// provided.</param>
        /// <param name="workingDirectory">The working directory for the process. If null or whitespace, the process uses the current directory.</param>
        /// <param name="runAsActiveUser">Specifies the user context under which to run the process. If null, the default user context is used.</param>
        /// <param name="useLinkedAdminToken">true to attempt to launch the process with a linked administrator token; otherwise, false.</param>
        /// <param name="useHighestAvailableToken">true to request the highest available privilege token for the process; otherwise, false.</param>
        /// <param name="inheritEnvironmentVariables">true to inherit the current process's environment variables; otherwise, false.</param>
        /// <param name="expandEnvironmentVariables">true to expand environment variables in the file path and arguments before launching the process; otherwise,
        /// false.</param>
        /// <param name="denyUserTermination">true to prevent the user from terminating the process; otherwise, false.</param>
        /// <param name="handlesToInherit">An optional collection of handles to inherit by the new process. If null, no additional handles are
        /// inherited.</param>
        /// <param name="useUnelevatedToken">true to attempt to launch the process with an unelevated token; otherwise, false.</param>
        /// <param name="useShellExecute">true to use the operating system shell to start the process; otherwise, false.</param>
        /// <param name="verb">The action to take when starting the process, such as 'runas' or 'open'. If null or whitespace, the default
        /// verb is used.</param>
        /// <param name="createNoWindow">true to start the process without creating a new window; otherwise, false.</param>
        /// <param name="waitForChildProcesses">true to wait for all child processes to exit before completing; otherwise, false.</param>
        /// <param name="killChildProcessesWithParent">true to terminate all child processes when the parent process exits; otherwise, false.</param>
        /// <param name="streamEncoding">The text encoding to use for standard input, output, and error streams. If null, the default encoding is
        /// used.</param>
        /// <param name="windowStyle">The window style to use when launching the process. If null, the default window style is used.</param>
        /// <param name="priorityClass">The priority class for the new process. If null, the default priority is used.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the process launch operation. If null, cancellation is not
        /// supported.</param>
        /// <param name="noTerminateOnTimeout">true to prevent the process from being terminated when a timeout occurs; otherwise, false.</param>
        /// <exception cref="ArgumentNullException">Thrown if filePath is null.</exception>
        /// <exception cref="ArgumentException">Thrown if filePath is not a fully qualified path when required.</exception>
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
            IEnumerable<IntPtr>? handlesToInherit = null,
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
            FilePath = filePath.StartsWith("\"") && filePath.EndsWith("\"") ? filePath.TrimStart('"').TrimEnd('"') : filePath;

            // Validate the file path is rooted.
            if (!Path.IsPathRooted(FilePath) && !useShellExecute && !FilePath.StartsWith("%"))
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
            if (handlesToInherit?.Any() == true)
            {
                HandlesToInherit = new ReadOnlyCollection<IntPtr>([.. handlesToInherit]);
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

            // Determine the type of file we're launching.
            try
            {
                // Try to get it directly from the PE header.
                ImageSubsystem = ExecutableInfo.Get(FilePath).Subsystem;
            }
            catch (Exception ex) when (ex.Message is not null)
            {
                // Assume we've got a GUI-based app unless its extension indicates otherwise.
                string filePathExtension = Path.GetExtension(FilePath);
                ImageSubsystem = filePathExtension.Equals(".com", StringComparison.OrdinalIgnoreCase) || filePathExtension.Equals(".bat", StringComparison.OrdinalIgnoreCase) || filePathExtension.Equals(".cmd", StringComparison.OrdinalIgnoreCase)
                    ? IMAGE_SUBSYSTEM.IMAGE_SUBSYSTEM_WINDOWS_CUI
                    : IMAGE_SUBSYSTEM.IMAGE_SUBSYSTEM_WINDOWS_GUI;
            }

            // Set remaining parameters.
            RunAsActiveUser = runAsActiveUser;
            UseLinkedAdminToken = useLinkedAdminToken;
            UseHighestAvailableToken = useHighestAvailableToken;
            InheritEnvironmentVariables = inheritEnvironmentVariables;
            ExpandEnvironmentVariables = expandEnvironmentVariables;
            DenyUserTermination = denyUserTermination;
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
        /// Gets an optional collection of handles that the child process should inherit.
        /// When specified, a STARTUPINFOEX structure with PROC_THREAD_ATTRIBUTE_HANDLE_LIST is used.
        /// </summary>
        public IReadOnlyList<IntPtr>? HandlesToInherit { get; }

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

        /// <summary>
        /// Gets the subsystem required to run the image.
        /// </summary>
        public IMAGE_SUBSYSTEM ImageSubsystem { get; }

        /// <summary>
        /// Gets a value indicating whether the application is a command-line interface (CLI) application.
        /// </summary>
        public bool IsCliApplication => ImageSubsystem != IMAGE_SUBSYSTEM.IMAGE_SUBSYSTEM_WINDOWS_GUI;
    }
}
