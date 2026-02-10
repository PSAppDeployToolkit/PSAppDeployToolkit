using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
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
    [DataContract]
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
        /// <param name="useUnelevatedToken">true to attempt to launch the process with an unelevated token; otherwise, false.</param>
        /// <param name="standardInput">Optional string to write to the process's standard input stream. If null or empty, no data is written.
        /// The string is encoded using the specified <paramref name="streamEncoding"/> (or the default encoding if not specified).</param>
        /// <param name="handlesToInherit">An optional collection of handles to inherit by the new process. If null, no additional handles are
        /// inherited.</param>
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
            bool useUnelevatedToken = false,
            IEnumerable<string>? standardInput = null,
            IEnumerable<nint>? handlesToInherit = null,
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
            if (argumentList?.Any() == true)
            {
                ArgumentList = new ReadOnlyCollection<string>([.. argumentList]);
            }
            if (!string.IsNullOrWhiteSpace(workingDirectory))
            {
                WorkingDirectory = workingDirectory!.Trim();
            }
            if (standardInput?.Any() == true)
            {
                StandardInput = new ReadOnlyCollection<string>([.. standardInput]);
            }
            if (handlesToInherit?.Any() == true)
            {
                HandlesToInheritValues = new ReadOnlyCollection<long>([.. handlesToInherit.Select(static h => (long)h)]);
            }
            if (!string.IsNullOrWhiteSpace(verb))
            {
                Verb = verb;
            }
            if (streamEncoding is not null)
            {
                StreamEncodingWebName = streamEncoding.WebName;
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
        [DataMember]
        public string FilePath { get; private set; }

        /// <summary>
        /// Gets the arguments to pass to the process.
        /// </summary>
        [DataMember]
        public IReadOnlyList<string>? ArgumentList { get; private set; }

        /// <summary>
        /// Gets the working directory of the process.
        /// </summary>
        [DataMember]
        public string? WorkingDirectory { get; private set; }

        /// <summary>
        /// Gets the username to use when starting the process.
        /// </summary>
        [DataMember]
        public RunAsActiveUser? RunAsActiveUser { get; private set; }

        /// <summary>
        /// Gets a value indicating whether to use the linked admin token to start the process.
        /// </summary>
        [DataMember]
        public bool UseLinkedAdminToken { get; private set; }

        /// <summary>
        /// Gets a value indicating whether to use the highest available token to start the process.
        /// </summary>
        [DataMember]
        public bool UseHighestAvailableToken { get; private set; }

        /// <summary>
        /// Gets a value indicating whether to inherit the environment variables of the current process.
        /// </summary>
        [DataMember]
        public bool InheritEnvironmentVariables { get; private set; }

        /// <summary>
        /// Indicates whether environment variables in the input should be expanded.
        /// </summary>
        [DataMember]
        public bool ExpandEnvironmentVariables { get; private set; }

        /// <summary>
        /// Indicates whether user termination is denied.
        /// </summary>
        [DataMember]
        public bool DenyUserTermination { get; private set; }

        /// <summary>
        /// Indicates whether an unelevated token should be used for operations.
        /// </summary>
        [DataMember]
        public bool UseUnelevatedToken { get; private set; }

        /// <summary>
        /// Gets the lines to write to the process's standard input stream.
        /// </summary>
        /// <remarks>Each string in the collection is written as a separate line, encoded using <see cref="StreamEncoding"/>.</remarks>
        [DataMember]
        public IReadOnlyList<string>? StandardInput { get; private set; }

        /// <summary>
        /// Gets an optional collection of handles that the child process should inherit.
        /// When specified, a STARTUPINFOEX structure with PROC_THREAD_ATTRIBUTE_HANDLE_LIST is used.
        /// </summary>
        [IgnoreDataMember]
        public IReadOnlyList<nint>? HandlesToInherit => HandlesToInheritValues?.Select(static h => (nint)h).ToList().AsReadOnly();

        /// <summary>
        /// Gets a value indicating whether to use the shell to execute the process.
        /// </summary>
        [DataMember]
        public bool UseShellExecute { get; private set; }

        /// <summary>
        /// Gets the verb to use when starting the process.
        /// </summary>
        [DataMember]
        public string? Verb { get; private set; }

        /// <summary>
        /// Gets a value indicating whether to create a new window for the process.
        /// </summary>
        [DataMember]
        public bool CreateNoWindow { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the process should wait for child processes to exit before completing.
        /// </summary>
        [DataMember]
        public bool WaitForChildProcesses { get; private set; }

        /// <summary>
        /// Gets a value indicating whether any child processes spawned with the parent should terminate when the parent closes.
        /// </summary>
        [DataMember]
        public bool KillChildProcessesWithParent { get; private set; }

        /// <summary>
        /// Gets the encoding type to use when parsing stdout/stderr text.
        /// </summary>
        [IgnoreDataMember]
        public Encoding StreamEncoding => Encoding.GetEncoding(StreamEncodingWebName);

        /// <summary>
        /// Gets the window style of the process.
        /// </summary>
        [DataMember]
        public SHOW_WINDOW_CMD? WindowStyle { get; private set; }

        /// <summary>
        /// Gets the window style of the process.
        /// </summary>
        [DataMember]
        public System.Diagnostics.ProcessWindowStyle? ProcessWindowStyle { get; private set; }

        /// <summary>
        /// Gets the priority class of the process.
        /// </summary>
        [DataMember]
        public System.Diagnostics.ProcessPriorityClass? PriorityClass { get; private set; }

        /// <summary>
        /// Gets the cancellation token to cancel the process.
        /// </summary>
        [IgnoreDataMember]
        public CancellationToken? CancellationToken { get; private set; }

        /// <summary>
        /// Gets whether to not end the process upon CancellationToken expiring.
        /// </summary>
        [DataMember]
        public bool NoTerminateOnTimeout { get; private set; }

        /// <summary>
        /// Gets the subsystem required to run the image.
        /// </summary>
        [DataMember]
        public IMAGE_SUBSYSTEM ImageSubsystem { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the application is a command-line interface (CLI) application.
        /// </summary>
        public bool IsCliApplication => ImageSubsystem != IMAGE_SUBSYSTEM.IMAGE_SUBSYSTEM_WINDOWS_GUI;

        /// <summary>
        /// Gets an optional collection of handles that the child process should inherit.
        /// When specified, a STARTUPINFOEX structure with PROC_THREAD_ATTRIBUTE_HANDLE_LIST is used.
        /// </summary>
        [DataMember]
        private ReadOnlyCollection<long>? HandlesToInheritValues;

        /// <summary>
        /// Gets the encoding web name string for serialization.
        /// </summary>
        [DataMember]
        private readonly string StreamEncodingWebName = Encoding.Default.WebName;
    }
}
