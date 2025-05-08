using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Security.Principal;
using System.Threading;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Windows.Win32.UI.WindowsAndMessaging;

namespace PSADT.Execution
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
        /// <param name="username"></param>
        /// <param name="useLinkedAdminToken"></param>
        /// <param name="inheritEnvironmentVariables"></param>
        /// <param name="useShellExecute"></param>
        /// <param name="verb"></param>
        /// <param name="createNoWindow"></param>
        /// <param name="streamEncoding"></param>
        /// <param name="windowStyle"></param>
        /// <param name="priorityClass"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="noTerminateOnTimeout"></param>
        /// <exception cref="ArgumentException"></exception>
        public ProcessLaunchInfo(
            string filePath,
            string[]? argumentList = null,
            string? workingDirectory = null,
            NTAccount? username = null,
            bool useLinkedAdminToken = false,
            bool inheritEnvironmentVariables = false,
            bool useShellExecute = false,
            string? verb = null,
            bool createNoWindow = false,
            Encoding? streamEncoding = null,
            ProcessWindowStyle? windowStyle = null,
            ProcessPriorityClass? priorityClass = null,
            CancellationToken? cancellationToken = null,
            bool noTerminateOnTimeout = false)
        {
            // Ensure CreateNoWindow and the WindowStyle are compatible.
            if (null != windowStyle)
            {
                if (windowStyle != ProcessWindowStyle.Hidden)
                {
                    if (createNoWindow)
                    {
                        throw new ArgumentException("Cannot set WindowStyle to a value other than Hidden when CreateNoWindow is true.");
                    }
                }
                else
                {
                    CreateNoWindow = true;
                }
                WindowStyle = WindowStyleMap[windowStyle.Value];
            }
            else if (createNoWindow)
            {
                WindowStyle = WindowStyleMap[ProcessWindowStyle.Hidden];
                CreateNoWindow = true;
            }

            // Validate all nullable parameters.
            if (!string.IsNullOrWhiteSpace(workingDirectory))
            {
                WorkingDirectory = workingDirectory!.Trim();
            }
            else if (Path.GetDirectoryName(filePath) is string fileDir && !string.IsNullOrWhiteSpace(fileDir))
            {
                WorkingDirectory = fileDir;
            }
            if ((null != argumentList) && (string.Join(" ", argumentList.Select(x => x.Trim())).Trim() is string args) && !string.IsNullOrWhiteSpace(args))
            {
                Arguments = args;
            }
            if (!string.IsNullOrWhiteSpace(verb))
            {
                Verb = verb;
            }
            if (null != streamEncoding)
            {
                StreamEncoding = streamEncoding;
            }
            if (null != priorityClass)
            {
                PriorityClass = priorityClass.Value;
            }
            if (null != cancellationToken)
            {
                CancellationToken = cancellationToken.Value;
            }

            // Set remaining boolean parameters.
            FilePath = filePath;
            Username = username;
            UseLinkedAdminToken = useLinkedAdminToken;
            InheritEnvironmentVariables = inheritEnvironmentVariables;
            UseShellExecute = useShellExecute;
            NoTerminateOnTimeout = noTerminateOnTimeout;
            CommandLine = $"\"{FilePath}\"{((null != Arguments) ? $" {Arguments}" : null)}\0";
        }

        /// <summary>
        /// Initializes a new instance of the ManagedProcessOptions class.
        /// </summary>
        /// <param name="filePath"></param>
        public ProcessLaunchInfo(string filePath) : this(filePath, null!) { }

        /// <summary>
        /// Initializes a new instance of the ManagedProcessOptions class.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="argumentList"></param>
        public ProcessLaunchInfo(string filePath, string[] argumentList) : this(filePath, argumentList, null) { }

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
        /// Gets the file path of the process to launch.
        /// </summary>
        public readonly string FilePath;

        /// <summary>
        /// Gets the arguments to pass to the process.
        /// </summary>
        public readonly string? Arguments = null;

        /// <summary>
        /// Gets the command line to use when starting the process.
        /// </summary>
        public readonly string CommandLine;

        /// <summary>
        /// Gets the working directory of the process.
        /// </summary>
        public readonly string? WorkingDirectory = null;

        /// <summary>
        /// Gets the username to use when starting the process.
        /// </summary>
        public readonly NTAccount? Username;

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
        /// Gets a value indicating whether to create a new window for the process.
        /// </summary>
        public readonly bool CreateNoWindow;

        /// <summary>
        /// Gets the encoding type to use when parsing stdout/stderr text.
        /// </summary>
        public readonly Encoding StreamEncoding = Encoding.Default;

        /// <summary>
        /// Gets the window style of the process.
        /// </summary>
        public readonly ushort WindowStyle = WindowStyleMap[ProcessWindowStyle.Normal];

        /// <summary>
        /// Gets the priority class of the process.
        /// </summary>
        public readonly ProcessPriorityClass PriorityClass = ProcessPriorityClass.Normal;

        /// <summary>
        /// Gets the cancellation token to cancel the process.
        /// </summary>
        public readonly CancellationToken CancellationToken = default;

        /// <summary>
        /// Gets whether to not end the process upon CancellationToken expiring.
        /// </summary>
        public readonly bool NoTerminateOnTimeout;
    }
}
