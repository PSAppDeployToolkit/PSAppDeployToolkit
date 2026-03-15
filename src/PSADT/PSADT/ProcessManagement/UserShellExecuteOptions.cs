using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;

namespace PSADT.ProcessManagement
{
    /// <summary>
    /// Provides options for launching a managed process.
    /// </summary>
    [DataContract]
    public sealed record UserShellExecuteOptions
    {
        /// <summary>
        /// Initializes a new instance of the ProcessLaunchInfo class with the specified process launch parameters.
        /// </summary>
        /// <param name="filePath">The fully qualified path to the executable file to launch. Cannot be null. If not using shell execute and
        /// not starting with '%', the path must be rooted.</param>
        /// <param name="argumentList">An optional collection of command-line arguments to pass to the process. If null or empty, no arguments are
        /// provided.</param>
        /// <param name="workingDirectory">The working directory for the process. If null or whitespace, the process uses the current directory.</param>
        /// <param name="expandEnvironmentVariables">true to expand environment variables in the file path and arguments before launching the process; otherwise,
        /// false.</param>
        /// <param name="verb">The action to take when starting the process, such as 'runas' or 'open'. If null or whitespace, the default
        /// verb is used.</param>
        /// <param name="createNoWindow">true to start the process without creating a new window; otherwise, false.</param>
        /// <param name="waitForChildProcesses">true to wait for all child processes to exit before completing; otherwise, false.</param>
        /// <param name="killChildProcessesWithParent">true to terminate all child processes when the parent process exits; otherwise, false.</param>
        /// <param name="windowStyle">The window style to use when launching the process. If null, the default window style is used.</param>
        /// <param name="priorityClass">The priority class for the new process. If null, the default priority is used.</param>
        /// <exception cref="ArgumentNullException">Thrown if filePath is null.</exception>
        public UserShellExecuteOptions(string filePath, IEnumerable<string>? argumentList = null, string? workingDirectory = null, bool expandEnvironmentVariables = false, string? verb = null, bool createNoWindow = false, bool waitForChildProcesses = false, bool killChildProcessesWithParent = false, ProcessWindowStyle? windowStyle = null, ProcessPriorityClass? priorityClass = null)
        {
            // Validate all string parameters are properly set up.
            ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
            if (verb is not null)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(verb);
                Verb = verb;
            }

            // Initially set ArgumentList and FilePath, and test that the caller hasn't done something weird by quoting the path.
            ArgumentList = new ReadOnlyCollection<string>([.. argumentList ?? []]);
            FilePath = filePath.TrimStart('"').TrimEnd('"');

            // Create an arguments string out of our ArgumentList (ShellExecute needs this).
            Arguments = ArgumentList.Count > 1 ? CommandLineUtilities.ArgumentListToCommandLine(ArgumentList) : ArgumentList.Count > 0 ? ArgumentList[0] : null;

            // Set the WorkingDirectory if specified.
            if (workingDirectory is not null)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(workingDirectory);
                WorkingDirectory = new(workingDirectory);
            }

            // Handle remaining nullable parameters.
            if (windowStyle is not null)
            {
                WindowStyle = windowStyle.Value;
            }
            if (priorityClass is not null)
            {
                PriorityClass = priorityClass.Value;
            }

            // Set remaining parameters.
            ExpandEnvironmentVariables = expandEnvironmentVariables;
            CreateNoWindow = createNoWindow;
            WaitForChildProcesses = waitForChildProcesses;
            KillChildProcessesWithParent = killChildProcessesWithParent;
        }

        /// <summary>
        /// Gets the file path of the process to launch.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
        [DataMember]
        public readonly string FilePath;

        /// <summary>
        /// Gets the arguments to pass to the process.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
        [DataMember]
        public readonly string? Arguments;

        /// <summary>
        /// Gets the arguments to pass to the process.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
        [DataMember]
        public readonly IReadOnlyList<string> ArgumentList;

        /// <summary>
        /// Gets the working directory of the process.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
        [DataMember]
        public readonly DirectoryInfo? WorkingDirectory;

        /// <summary>
        /// Indicates whether environment variables in the input should be expanded.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
        [DataMember]
        public readonly bool ExpandEnvironmentVariables;

        /// <summary>
        /// Gets the verb to use when starting the process.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
        [DataMember]
        public readonly string? Verb;

        /// <summary>
        /// Gets a value indicating whether to create a new window for the process.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
        [DataMember]
        public readonly bool CreateNoWindow;

        /// <summary>
        /// Gets a value indicating whether the process should wait for child processes to exit before completing.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
        [DataMember]
        public readonly bool WaitForChildProcesses;

        /// <summary>
        /// Gets a value indicating whether any child processes spawned with the parent should terminate when the parent closes.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
        [DataMember]
        public readonly bool KillChildProcessesWithParent;

        /// <summary>
        /// Gets the window style of the process.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
        [DataMember]
        public readonly ProcessWindowStyle? WindowStyle;

        /// <summary>
        /// Gets the priority class of the process.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
        [DataMember]
        public readonly ProcessPriorityClass? PriorityClass;

        /// <summary>
        /// Generates the command-line string representation for the current configuration.
        /// </summary>
        /// <remarks>This method uses default options when generating the command-line. To customize the
        /// output, use the overload that accepts parameters.</remarks>
        /// <returns>A string containing the command-line arguments constructed from the current settings.</returns>
        public string MakeCommandLine()
        {
            return MakeCommandLine(false);
        }

        /// <summary>
        /// Creates a command-line string for the process, optionally appending a null terminator.
        /// </summary>
        /// <param name="nullTerminated">Specifies whether the resulting command-line string should be null-terminated. Set to <see langword="true"/>
        /// to append a null character at the end; otherwise, <see langword="false"/>.</param>
        /// <returns>A string containing the command-line representation of the process, including the file path and any
        /// arguments. If <paramref name="nullTerminated"/> is <see langword="true"/>, the string will end with a null
        /// character.</returns>
        internal string MakeCommandLine(bool nullTerminated)
        {
            return $"\"{FilePath}\"{(!string.IsNullOrWhiteSpace(Arguments) ? $" {Arguments}" : null)}{(nullTerminated ? '\0' : null)}";
        }

        /// <summary>
        /// Creates a new instance of the process launch information using the current configuration settings.
        /// </summary>
        /// <remarks>The returned object encapsulates all relevant launch parameters, including file path,
        /// arguments, working directory, window style, and process priority. This method is intended for internal use
        /// when preparing to start a process with the configured options.</remarks>
        /// <returns>A <see cref="ProcessLaunchInfo"/> object containing the parameters required to launch a process with the
        /// specified settings.</returns>
        internal ProcessLaunchInfo ToLaunchInfo()
        {
            return new(
                filePath: FilePath,
                argumentList: ArgumentList,
                workingDirectory: WorkingDirectory?.FullName,
                expandEnvironmentVariables: ExpandEnvironmentVariables,
                useShellExecute: true, verb: Verb,
                createNoWindow: CreateNoWindow,
                waitForChildProcesses: WaitForChildProcesses,
                killChildProcessesWithParent: KillChildProcessesWithParent,
                windowStyle: WindowStyle,
                priorityClass: PriorityClass);
        }
    }
}
