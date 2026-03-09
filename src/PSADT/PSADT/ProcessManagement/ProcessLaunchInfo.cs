using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using Microsoft.Win32.SafeHandles;
using PSADT.AccountManagement;
using PSADT.FileSystem;
using PSADT.Foundation;
using PSADT.Interop;
using PSADT.Interop.Extensions;
using PSADT.Interop.SafeHandles;
using PSADT.Security;
using Windows.Win32;
using Windows.Win32.Foundation;

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
        /// <exception cref="DriveNotFoundException">Thrown if filePath is not a fully qualified path when required.</exception>
        public ProcessLaunchInfo(string filePath, IEnumerable<string>? argumentList = null, string? workingDirectory = null, RunAsActiveUser? runAsActiveUser = null, bool useLinkedAdminToken = false, bool useHighestAvailableToken = false, bool inheritEnvironmentVariables = false, bool expandEnvironmentVariables = false, bool denyUserTermination = false, bool useUnelevatedToken = false, IEnumerable<string>? standardInput = null, IEnumerable<nint>? handlesToInherit = null, bool useShellExecute = false, string? verb = null, bool createNoWindow = false, bool waitForChildProcesses = false, bool killChildProcessesWithParent = false, Encoding? streamEncoding = null, ProcessWindowStyle? windowStyle = null, ProcessPriorityClass? priorityClass = null, CancellationToken? cancellationToken = null, bool noTerminateOnTimeout = false)
        {
            // Validate all string parameters are properly set up.
            ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
            if (workingDirectory is not null)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(workingDirectory);
                WorkingDirectory = workingDirectory;
            }
            if (verb is not null)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(verb);
                Verb = verb;
            }

            // Initially set ArgumentList and FilePath, and test that the caller hasn't done something weird by quoting the path.
            ArgumentList = new ReadOnlyCollection<string>([.. argumentList?.Where(static s => !string.IsNullOrWhiteSpace(s)) ?? []]);
            FilePath = filePath.StartsWith("\"") && filePath.EndsWith("\"") ? filePath.TrimStart('"').TrimEnd('"') : filePath;

            // Set up all token-related variables. Allow useLinkedAdminToken to clobber useHighestAvailableToken.
            if (useHighestAvailableToken)
            {
                ElevatedTokenType = ElevatedTokenType.HighestAvailable;
            }
            if (useLinkedAdminToken)
            {
                ElevatedTokenType = ElevatedTokenType.HighestMandatory;
            }
            InheritEnvironmentVariables = inheritEnvironmentVariables;
            UseUnelevatedToken = useUnelevatedToken;
            RunAsActiveUser = runAsActiveUser;

            // Expand out environment variables for FilePath/ArgumentList as required.
            if (ExpandEnvironmentVariables = expandEnvironmentVariables)
            {
                if (RunAsActiveUser is not null && RunAsActiveUser != AccountUtilities.CallerRunAsActiveUser)
                {
                    using SafeFileHandle hPrimaryToken = TokenManager.GetUserPrimaryToken(RunAsActiveUser.SessionId);
                    _ = NativeMethods.CreateEnvironmentBlock(out SafeEnvironmentBlockHandle lpEnvironment, hPrimaryToken, InheritEnvironmentVariables);
                    using (lpEnvironment)
                    {
                        string ExpandEnvironmentVariables(string name)
                        {
                            unsafe
                            {
                                fixed (char* pInputArgument = name)
                                {
                                    UNICODE_STRING pInputString = default, pOutputString = default; PInvoke.RtlInitUnicodeString(&pInputString, pInputArgument);
                                    _ = NativeMethods.RtlExpandEnvironmentStrings_U(lpEnvironment, in pInputString, ref pOutputString, out uint requiredBytes);
                                    fixed (char* pOutputArgument = new char[requiredBytes / sizeof(char)])
                                    {
                                        pOutputString = new() { Buffer = pOutputArgument, Length = 0, MaximumLength = (ushort)requiredBytes };
                                        _ = NativeMethods.RtlExpandEnvironmentStrings_U(lpEnvironment, in pInputString, ref pOutputString, out _);
                                        return pOutputString.ToManagedString();
                                    }
                                }
                            }
                        }
                        if (WorkingDirectory is not null)
                        {
                            WorkingDirectory = ExpandEnvironmentVariables(WorkingDirectory);
                        }
                        ArgumentList = new ReadOnlyCollection<string>([.. ArgumentList.Select(ExpandEnvironmentVariables)]);
                        FilePath = ExpandEnvironmentVariables(FilePath);
                    }
                }
                else
                {
                    if (WorkingDirectory is not null)
                    {
                        WorkingDirectory = Environment.ExpandEnvironmentVariables(WorkingDirectory);
                    }
                    ArgumentList = new ReadOnlyCollection<string>([.. ArgumentList.Select(Environment.ExpandEnvironmentVariables)]);
                    FilePath = Environment.ExpandEnvironmentVariables(FilePath);
                }
            }

            // Validate the file path is rooted.
            if (!(UseShellExecute = useShellExecute) && !Path.IsPathRooted(FilePath))
            {
                throw new DriveNotFoundException("File path must be fully qualified.");
            }

            // Hard-coded adjustment specifically for the UIAccess-enabled client/server executable.
            if (RunAsActiveUser is null || RunAsActiveUser == AccountUtilities.CallerRunAsActiveUser)
            {
                if (FilePath == EnvironmentInfo.ClientServerClientDefaultPath)
                {
                    FilePath = EnvironmentInfo.ClientServerClientCompatiblePath;
                }
                if (FilePath == EnvironmentInfo.ClientServerClientLauncherDefaultPath)
                {
                    FilePath = EnvironmentInfo.ClientServerClientLauncherCompatiblePath;
                }
            }

            // Create an arguments string out of our ArgumentList (ShellExecute needs this).
            Arguments = ArgumentList.Count > 1 ? CommandLineUtilities.ArgumentListToCommandLine(ArgumentList) : ArgumentList.Count > 0 ? ArgumentList[0] : null;

            // Determine the type of file we're launching.
            try
            {
                ImageSubsystem = ExecutableInfo.Get(FilePath).Subsystem;
            }
            catch (Exception ex) when (ex.Message is not null)
            {
                string filePathExtension = Path.GetExtension(FilePath);
                ImageSubsystem = !string.IsNullOrWhiteSpace(filePathExtension)
                    && (filePathExtension.Equals(".com", StringComparison.OrdinalIgnoreCase)
                    || filePathExtension.Equals(".bat", StringComparison.OrdinalIgnoreCase)
                    || filePathExtension.Equals(".cmd", StringComparison.OrdinalIgnoreCase))
                    ? IMAGE_SUBSYSTEM.IMAGE_SUBSYSTEM_WINDOWS_CUI
                    : IMAGE_SUBSYSTEM.IMAGE_SUBSYSTEM_WINDOWS_GUI;
            }

            // Handle the CreateNoWindow parameter.
            if (createNoWindow)
            {
                WindowStyle = WindowStyleMap[System.Diagnostics.ProcessWindowStyle.Hidden];
                ProcessWindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                CreateNoWindow = true;
            }

            // Handle remaining nullable parameters.
            if (windowStyle is not null)
            {
                WindowStyle = WindowStyleMap[windowStyle.Value];
                ProcessWindowStyle = windowStyle.Value;
            }
            if (streamEncoding is not null)
            {
                StreamEncodingWebName = streamEncoding.WebName;
            }
            if (priorityClass is not null)
            {
                PriorityClass = priorityClass.Value;
            }
            if (cancellationToken is not null)
            {
                CancellationToken = cancellationToken.Value;
            }

            // Set remaining parameters.
            DenyUserTermination = denyUserTermination;
            StandardInput = new ReadOnlyCollection<string>([.. standardInput ?? []]);
            HandlesToInheritValues = new ReadOnlyCollection<long>([.. handlesToInherit?.Select(static h => (long)h) ?? []]);
            WaitForChildProcesses = waitForChildProcesses;
            KillChildProcessesWithParent = killChildProcessesWithParent;
            NoTerminateOnTimeout = noTerminateOnTimeout;

            // Confirm we're not using incompatible options.
            if (UseShellExecute && (RunAsActiveUser is not null || RunAsActiveUser != AccountUtilities.CallerRunAsActiveUser))
            {
                throw new InvalidOperationException("Cannot specify UseShellExecute while specifying a RunAsActiveUser.");
            }
        }

        /// <summary>
        /// Translator for ProcessWindowStyle to the corresponding value for CreateProcess.
        /// </summary>
        private static readonly ReadOnlyDictionary<ProcessWindowStyle, SHOW_WINDOW_CMD> WindowStyleMap = new(new Dictionary<ProcessWindowStyle, SHOW_WINDOW_CMD>()
        {
            { System.Diagnostics.ProcessWindowStyle.Normal, SHOW_WINDOW_CMD.SW_SHOWNORMAL },
            { System.Diagnostics.ProcessWindowStyle.Hidden, SHOW_WINDOW_CMD.SW_HIDE },
            { System.Diagnostics.ProcessWindowStyle.Minimized, SHOW_WINDOW_CMD.SW_SHOWMINIMIZED },
            { System.Diagnostics.ProcessWindowStyle.Maximized, SHOW_WINDOW_CMD.SW_SHOWMAXIMIZED },
        });

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
        public readonly string? WorkingDirectory;

        /// <summary>
        /// Gets the username to use when starting the process.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
        [DataMember]
        public readonly RunAsActiveUser? RunAsActiveUser;

        /// <summary>
        /// Gets a value indicating the token type to use when starting a process for another user.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
        [DataMember]
        public readonly ElevatedTokenType ElevatedTokenType;

        /// <summary>
        /// Gets a value indicating whether to inherit the environment variables of the current process.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
        [DataMember]
        public readonly bool InheritEnvironmentVariables;

        /// <summary>
        /// Indicates whether environment variables in the input should be expanded.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
        [DataMember]
        public readonly bool ExpandEnvironmentVariables;

        /// <summary>
        /// Indicates whether user termination is denied.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
        [DataMember]
        public readonly bool DenyUserTermination;

        /// <summary>
        /// Indicates whether an unelevated token should be used for operations.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
        [DataMember]
        public readonly bool UseUnelevatedToken;

        /// <summary>
        /// Gets the lines to write to the process's standard input stream.
        /// </summary>
        /// <remarks>Each string in the collection is written as a separate line, encoded using <see cref="StreamEncoding"/>.</remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
        [DataMember]
        public readonly IReadOnlyList<string> StandardInput;

        /// <summary>
        /// Gets an optional collection of handles that the child process should inherit.
        /// When specified, a STARTUPINFOEX structure with PROC_THREAD_ATTRIBUTE_HANDLE_LIST is used.
        /// </summary>
        [IgnoreDataMember]
        public IReadOnlyList<nint> HandlesToInherit => HandlesToInheritValues?.Select(static h => (nint)h).ToList().AsReadOnly() ?? new ReadOnlyCollection<nint>([]);

        /// <summary>
        /// Gets a value indicating whether to use the shell to execute the process.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
        [DataMember]
        public readonly bool UseShellExecute;

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
        /// Gets the encoding type to use when parsing stdout/stderr text.
        /// </summary>
        [IgnoreDataMember]
        public Encoding StreamEncoding => Encoding.GetEncoding(StreamEncodingWebName);

        /// <summary>
        /// Gets the window style of the process.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
        [DataMember]
        public readonly SHOW_WINDOW_CMD? WindowStyle;

        /// <summary>
        /// Gets the window style of the process.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
        [DataMember]
        public readonly ProcessWindowStyle? ProcessWindowStyle;

        /// <summary>
        /// Gets the priority class of the process.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
        [DataMember]
        public readonly ProcessPriorityClass? PriorityClass;

        /// <summary>
        /// Gets the cancellation token to cancel the process.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
        [IgnoreDataMember]
        public readonly CancellationToken? CancellationToken;

        /// <summary>
        /// Gets whether to not end the process upon CancellationToken expiring.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
        [DataMember]
        public readonly bool NoTerminateOnTimeout;

        /// <summary>
        /// Gets the subsystem required to run the image.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
        [DataMember]
        public readonly IMAGE_SUBSYSTEM ImageSubsystem;

        /// <summary>
        /// Gets a value indicating whether the application is a command-line interface (CLI) application.
        /// </summary>
        internal bool IsCliApplication()
        {
            return ImageSubsystem != IMAGE_SUBSYSTEM.IMAGE_SUBSYSTEM_WINDOWS_GUI;
        }

        /// <summary>
        /// Generates a command line string from the specified file path and argument list, formatted as a
        /// null-terminated character array.
        /// </summary>
        /// <remarks>If the argument list is empty, only the file path is included in the command line.
        /// The method handles cases where the argument list is empty or null, ensuring the result is always properly
        /// formatted for process invocation scenarios.</remarks>
        /// <returns>An array of characters representing the command line, including the file path and any additional arguments.
        /// The array ends with a null character.</returns>
        internal string MakeCommandLine()
        {
            return $"\"{FilePath}\"{(!string.IsNullOrWhiteSpace(Arguments) ? $" {Arguments}" : null)}\0";
        }

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
