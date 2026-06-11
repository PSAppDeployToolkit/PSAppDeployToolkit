using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using PSADT.AccountManagement;
using PSADT.ClientServer.Payloads;
using PSADT.DeviceManagement;
using PSADT.Foundation;
using PSADT.Interop;
using PSADT.ProcessManagement;
using PSADT.Security;
using PSADT.UserInterface;
using PSADT.UserInterface.DialogOptions;
using PSADT.UserInterface.DialogState;
using PSADT.UserInterface.Interfaces;
using PSADT.Utilities;
using PSADT.WindowManagement;
using PSADT.WindowsRuntime.UI.Notifications;
using PSADT.WindowsRuntime.UI.Shell;
using PSAppDeployToolkit.Logging;
using Windows.UI.Notifications;
using Windows.Win32.Foundation;
using Windows.Win32.System.Threading;

namespace PSADT.ClientServer
{
    /// <summary>
    /// Provides the main entry point for the PSAppDeployToolkit User Interface Display Server application.
    /// </summary>
    /// <remarks>This application is designed to be used in conjunction with the PSAppDeployToolkit PowerShell
    /// module and should not be directly invoked by end-users. It processes command-line arguments to display various
    /// types of dialogs based on the provided options. If no arguments are supplied, the application displays an error
    /// message and exits with an appropriate exit code. The application also validates the provided arguments and
    /// ensures they conform to the expected format before executing the requested operation.</remarks>
    internal static class ClientExecutable
    {
        /// <summary>
        /// Initializes the application by setting the unhandled exception handler for the dialog manager.
        /// </summary>
        /// <remarks>This method is automatically called when the module is loaded, ensuring that any
        /// unhandled exceptions are managed appropriately.</remarks>
        [ModuleInitializer]
        internal static void Init()
        {
            AppDomain.CurrentDomain.SetData("PSADT.UserInterface.DialogManager.UnhandledExceptionHandler", static (Exception ex) => Console.Error.WriteLine(DataSerialization.SerializeToString(ex)));
        }

        /// <summary>
        /// Initializes the <see cref="ClientExecutable"/> class by eagerly loading all referenced assemblies located in
        /// the application directory.
        /// </summary>
        /// <remarks>Performs a breadth-first traversal of all assembly references, loading only those
        /// assemblies that exist in the same directory as the current assembly. Dynamic assemblies and Windows Runtime
        /// assemblies are skipped.</remarks>
        /// <exception cref="InvalidOperationException">The application directory cannot be determined from the assembly location.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1065:Do not raise exceptions in unexpected locations", Justification = "This is a guard exception that should never fire.")]
        static ClientExecutable()
        {
            string applicationDirectory = Path.GetDirectoryName(AssemblyInfo.Location) ?? string.Empty;
            if (string.IsNullOrWhiteSpace(applicationDirectory))
            {
                throw new InvalidOperationException("Failed to determine the application directory from the assembly location.");
            }
            HashSet<string> attemptedReferences = new(StringComparer.OrdinalIgnoreCase);
            HashSet<string> queuedAssemblies = new(StringComparer.OrdinalIgnoreCase);
            Queue<Assembly> queue = new();
            EnqueueIfNeeded(AssemblyInfo);
            while (queue.Count > 0)
            {
                foreach (AssemblyName referencedAssemblyName in queue.Dequeue().GetReferencedAssemblies())
                {
                    // Skip over any invalid or already seen assembly names.
                    if (referencedAssemblyName.ContentType == AssemblyContentType.WindowsRuntime)
                    {
                        continue;
                    }
                    if (referencedAssemblyName.Name is not string simpleName)
                    {
                        continue;
                    }
                    if (referencedAssemblyName.FullName is not string requestedFullName)
                    {
                        continue;
                    }
                    if (!attemptedReferences.Add(requestedFullName))
                    {
                        continue;
                    }

                    // Load the assembly and enqueue if it's adjacent to this binary.
                    if (File.Exists(Path.Join(applicationDirectory, simpleName + ".dll")) || File.Exists(Path.Join(applicationDirectory, simpleName + ".exe")))
                    {
                        EnqueueIfNeeded(Assembly.Load(referencedAssemblyName));
                    }
                }
            }

            // Local function to enqueue assemblies if they haven't been seen before.
            void EnqueueIfNeeded(Assembly assembly)
            {
                if (assembly.IsDynamic)
                {
                    return;
                }
                if (assembly.FullName is not string fullName)
                {
                    return;
                }
                if (queuedAssemblies.Add(fullName))
                {
                    queue.Enqueue(assembly);
                }
            }
        }

        /// <summary>
        /// The asynchronous main entry point for the application.
        /// </summary>
        /// <param name="argv">A string array containing the command-line arguments passed to the application.</param>
        [STAThread]
        private static async Task<int> Main(string[] argv)
        {
            // Detect what mode the executable has been asked to run in.
            try
            {
                // Determine the mode of operation based on the provided arguments.
                if (argv.Length == 0)
                {
                    string productVersion = AssemblyInfo.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? throw new ClientException("Failed to retrieve assembly version information.", ClientExitCode.Unknown);
                    string helpTitle = $"{AssemblyInfo.GetCustomAttribute<AssemblyTitleAttribute>()?.Title ?? throw new ClientException("Failed to retrieve assembly title information.", ClientExitCode.Unknown)} {new Version(productVersion[..productVersion.IndexOf('+', StringComparison.OrdinalIgnoreCase)])}";
                    string helpMessage = string.Join(Environment.NewLine,
                    [
                        helpTitle,
                        string.Empty,
                        AssemblyInfo.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright ?? throw new ClientException("Failed to retrieve assembly copyright information.", ClientExitCode.Unknown),
                        string.Empty,
                        "This application is designed to be used with the PSAppDeployToolkit PowerShell module and should not be directly invoked.",
                        string.Empty,
                        "If you're an end-user or employee of your organization, please report this message to your helpdesk for further assistance.",
                    ]);
                    _ = await DialogManager.ShowDialogBoxAsync(helpTitle, helpMessage, DialogBoxButtons.Ok, DialogBoxDefaultButton.First, DialogBoxIcon.Stop, TopMost: true, default).ConfigureAwait(false);
                    throw new ClientException("No arguments were provided to the display server.", ClientExitCode.NoArguments);
                }
                return argv.Any(static arg => arg.Equals("/ClientServer", StringComparison.Ordinal) || arg.Equals("/cs", StringComparison.Ordinal))
                    ? await EnterClientServerModeAsync(ArgvToDictionary(argv)).ConfigureAwait(false)
                    : await EnterStandaloneModeAsync(argv).ConfigureAwait(false);
            }
            catch (ClientException ex)
            {
                // We've caught our own error. Write it out, the error handler will get the exit code out of it.
                return InvokeMainErrorHandler(ex, $"Failed to perform the requested operation with error code [{ex.HResult.ToString("X8", CultureInfo.InvariantCulture)}].");
            }
            catch (Exception ex) when (ex.Message is not null)
            {
                // This block is here as a fail-safe and should never be reached.
                return InvokeMainErrorHandler(ex, $"An unexpected exception occurred with HRESULT [{ex.HResult.ToString("X8", CultureInfo.InvariantCulture)}].", ClientExitCode.Unknown);
            }
        }

        /// <summary>
        /// Enters client-server mode by establishing communication through input and output pipes.
        /// </summary>
        /// <remarks>This method initializes anonymous pipe clients for input and output communication
        /// using the provided pipe handles. If the required pipe handles are missing, invalid, or cannot be opened,
        /// the method writes an error message to the standard error stream and terminates the process with an
        /// appropriate exit code.</remarks>
        /// <param name="arguments">A read-only dictionary containing the pipe handles required for communication. The dictionary must include
        /// the keys <c>"InputPipe"</c> and <c>"OutputPipe"</c>, each mapped to a valid, non-empty pipe handle string.</param>
        /// <exception cref="ClientException">Thrown when a required pipe handle is missing, invalid, or cannot be opened.</exception>
        private static async Task<int> EnterClientServerModeAsync(ReadOnlyDictionary<string, string> arguments)
        {
            // Get the pipe handles from the arguments.
            if (!arguments.TryGetValue("OutputPipe", out string? outputPipeHandle) || string.IsNullOrWhiteSpace(outputPipeHandle))
            {
                throw new ClientException("The specified OutputPipe handle was null or invalid.", ClientExitCode.NoOutputPipe);
            }
            if (!arguments.TryGetValue("InputPipe", out string? inputPipeHandle) || string.IsNullOrWhiteSpace(inputPipeHandle))
            {
                throw new ClientException("The specified InputPipe handle was null or invalid.", ClientExitCode.NoInputPipe);
            }
            if (!arguments.TryGetValue("LogPipe", out string? logPipeHandle) || string.IsNullOrWhiteSpace(logPipeHandle))
            {
                throw new ClientException("The specified LogPipe handle was null or invalid.", ClientExitCode.NoLogPipe);
            }

            // Establish the pipe objects.
            AnonymousPipeClientStream outputPipeClient;
            AnonymousPipeClientStream inputPipeClient;
            AnonymousPipeClientStream logPipeClient;
            try
            {
                outputPipeClient = new(PipeDirection.Out, outputPipeHandle);
            }
            catch (Exception ex) when (ex.Message is not null)
            {
                throw new ClientException("Failed to open a pipe client for the specified OutputHandle.", ClientExitCode.InvalidOutputPipe, ex);
            }
            try
            {
                inputPipeClient = new(PipeDirection.In, inputPipeHandle);
            }
            catch (Exception ex) when (ex.Message is not null)
            {
                throw new ClientException("Failed to open a pipe client for the specified InputHandle.", ClientExitCode.InvalidInputPipe, ex);
            }
            try
            {
                logPipeClient = new(PipeDirection.Out, logPipeHandle);
            }
            catch (Exception ex) when (ex.Message is not null)
            {
                throw new ClientException("Failed to open a pipe client for the specified LogHandle.", ClientExitCode.InvalidLogPipe, ex);
            }

            // Start reading data from the pipes. We only return from here when the server's pipe closes on us.
            try
            {
                // Ensure everything is properly disposed of.
                using (outputPipeClient) using (inputPipeClient) using (logPipeClient)
                using (ClientPipeEncryption ioEncryption = new())
                using (ClientPipeEncryption logEncryption = new())
                {
                    // Perform ECDH key exchange for encrypted communication.
                    try
                    {
                        ioEncryption.PerformKeyExchange(outputPipeClient, inputPipeClient);
                        logEncryption.PerformKeyExchange(outputPipeClient, inputPipeClient);
                    }
                    catch (Exception ex) when (ex.Message is not null)
                    {
                        throw new ClientException("Failed to establish encrypted communication with the server process.", ClientExitCode.EncryptionError, ex);
                    }

                    // Set up writer helper methods.
                    void WriteSuccess<T>(T result)
                    {
                        byte[] data = SerializeToBytes(result);
                        byte[] response = new byte[data.Length + 1];
                        response[0] = (byte)ResponseMarker.Success;
                        data.CopyTo(response.AsSpan(1));
                        ioEncryption.WriteEncrypted(outputPipeClient, response);
                    }
                    void WriteError(Exception ex)
                    {
                        byte[] data = SerializeToBytes(ex);
                        byte[] response = new byte[data.Length + 1];
                        response[0] = (byte)ResponseMarker.Error;
                        data.CopyTo(response.AsSpan(1));
                        ioEncryption.WriteEncrypted(outputPipeClient, response);
                    }
                    void WriteLog(string message, LogSeverity severity, string source)
                    {
                        logEncryption.WriteEncrypted(logPipeClient, SerializeToBytes(new LogMessagePayload(message, severity, source)));
                    }

                    // Continuously loop until the end. When we receive null, the server has closed the pipe, so we should break and exit.
                    CloseAppsDialogState? closeAppsDialogState = null;
                    try
                    {
                        while (true)
                        {
                            try
                            {
                                // Read and decrypt the request: [1-byte command][serialized payload]
                                byte[] requestBytes = ioEncryption.ReadEncrypted(inputPipeClient);
                                if (requestBytes.Length == 0)
                                {
                                    throw new ClientException("Received empty request from server.", ClientExitCode.InvalidRequest);
                                }
                                PipeCommand command = (PipeCommand)requestBytes[0]; const int payloadOffset = 1;
                                try
                                {
                                    switch (command)
                                    {
                                        case PipeCommand.Open:
                                            WriteSuccess(result: true);
                                            break;

                                        case PipeCommand.Close:
                                            WriteSuccess(result: true);
                                            return (int)ClientExitCode.Success;

                                        case PipeCommand.InitCloseAppsDialog:
                                            // We have the suppression here as the analyser can't handle our setup with IAsyncDisposable.
                                            // It is correct though and under no circumstances is any memory leaked out of our setup.
                                            if (closeAppsDialogState is not null)
                                            {
                                                await closeAppsDialogState.DisposeAsync().ConfigureAwait(false);
                                            }
                                            #pragma warning disable format, CA2000 
                                            closeAppsDialogState = new(DeserializeBytes<InitCloseAppsDialogPayload>(requestBytes, payloadOffset).ProcessDefinitions, WriteLog);
                                            #pragma warning restore CA2000, format
                                            WriteSuccess(result: true);
                                            break;

                                        case PipeCommand.PromptToCloseApps:
                                            // If we're here without a RunningProcessService, the InitCloseAppsDialog command was not called properly.
                                            if (closeAppsDialogState?.RunningProcessService is null)
                                            {
                                                throw new ClientException("The PromptToCloseApps command can only be called when ProcessDefinitions were provided to the InitCloseAppsDialog command.", ClientExitCode.InvalidRequest);
                                            }

                                            // Get all the windows that haven't failed on us and start closing them.
                                            TimeSpan promptToSaveTimeout = DeserializeBytes<PromptToCloseAppsPayload>(requestBytes, payloadOffset).Timeout; List<nint> failures = []; Process[] runningProcesses;
                                            while ((runningProcesses = [.. closeAppsDialogState.RunningProcessService.RunningProcesses.Select(static rp => rp.Process)]).Length > 0 && WindowUtilities.GetProcessWindowInfo(runningProcesses).Where(w => w.WindowHandle == w.ParentProcessMainWindowHandle && !failures.Contains(w.WindowHandle)).ToArray() is { Length: > 0 } windows)
                                            {
                                                // Start gracefully closing each open window.
                                                foreach (WindowInfo window in windows)
                                                {
                                                    Process process = Process.GetProcessById(window.ParentProcessId);
                                                    closeAppsDialogState.LogAction($"Closing window with title [{window.WindowTitle}] for process [{process.ProcessName}], prompting to save if necessary.", LogSeverity.Info);
                                                    try
                                                    {
                                                        WindowTools.BringWindowToFront((HWND)window.WindowHandle);
                                                    }
                                                    catch (Exception ex) when (ex.Message is not null)
                                                    {
                                                        closeAppsDialogState.LogAction($"Failed to bring window [{window.WindowTitle}] for process [{process.ProcessName}] to the foreground for closing: {ex}", LogSeverity.Error);
                                                        failures.Add(window.WindowHandle);
                                                        continue;
                                                    }

                                                    // Attempt to close out the process's main window.
                                                    try
                                                    {
                                                        if (!process.CloseMainWindow())
                                                        {
                                                            throw new ClientException("The call to CloseMainWindow() returned false, indicating the main window may be disabled due to a modal dialog being shown.", ClientExitCode.PromptToSaveFailure);
                                                        }
                                                    }
                                                    catch (Exception ex) when (ex.Message is not null)
                                                    {
                                                        closeAppsDialogState.LogAction($"The call to CloseMainWindow() method on process [{process.ProcessName}] with window title [{window.WindowTitle}] failed: {ex}", LogSeverity.Error);
                                                        failures.Add(window.WindowHandle);
                                                        continue;
                                                    }

                                                    // Spin until the window is closed or we time out.
                                                    Stopwatch promptToCloseStopwatch = Stopwatch.StartNew();
                                                    while (true)
                                                    {
                                                        if (WindowUtilities.GetProcessWindowInfo(parentProcessIdFilter: [process.Id], windowHandleFilter: [window.WindowHandle]).Count == 0)
                                                        {
                                                            closeAppsDialogState.LogAction($"Window [{window.WindowTitle}] for process [{process.ProcessName}] was successfully closed.", LogSeverity.Info);
                                                            break;
                                                        }
                                                        if (promptToCloseStopwatch.Elapsed >= promptToSaveTimeout)
                                                        {
                                                            closeAppsDialogState.LogAction($"Timed out waiting for window [{window.WindowTitle}] for process [{process.ProcessName}] to close.", LogSeverity.Warning);
                                                            break;
                                                        }
                                                        await Task.Delay(2000).ConfigureAwait(false);
                                                    }
                                                }
                                            }

                                            // If we didn't have any failures and we've still got running processes, they're processes without windows, so just kill them before returning.
                                            if (failures.Count == 0 && runningProcesses.Length > 0)
                                            {
                                                closeAppsDialogState.LogAction("Stopping remaining processes without open windows...", LogSeverity.Info);
                                                foreach (Process process in runningProcesses)
                                                {
                                                    closeAppsDialogState.LogAction($"Stopping process {process.ProcessName}...", LogSeverity.Info);
                                                    if (!process.HasExited)
                                                    {
                                                        process.Kill(); await process.WaitForExitAsync().ConfigureAwait(false);
                                                    }
                                                }
                                            }
                                            WriteSuccess(result: true);
                                            break;

                                        case PipeCommand.ShowModalDialog:
                                            {
                                                ShowModalDialogPayload payload = DeserializeBytes<ShowModalDialogPayload>(requestBytes, payloadOffset);
                                                WriteSuccess(await InvokeModalDialogAsync(payload.DialogType, payload.DialogStyle, payload.Options, closeAppsDialogState).ConfigureAwait(false));
                                                break;
                                            }

                                        case PipeCommand.ShowProgressDialog:
                                            {
                                                ShowProgressDialogPayload payload = DeserializeBytes<ShowProgressDialogPayload>(requestBytes, payloadOffset);
                                                await DialogManager.ShowProgressDialogAsync(payload.DialogStyle, payload.Options).ConfigureAwait(false);
                                                WriteSuccess(DialogManager.ProgressDialogOpen());
                                                break;
                                            }

                                        case PipeCommand.ProgressDialogOpen:
                                            WriteSuccess(DialogManager.ProgressDialogOpen());
                                            break;

                                        case PipeCommand.UpdateProgressDialog:
                                            {
                                                UpdateProgressDialogPayload payload = DeserializeBytes<UpdateProgressDialogPayload>(requestBytes, payloadOffset);
                                                await DialogManager.UpdateProgressDialogAsync(payload.Message, payload.DetailMessage, payload.Percentage, payload.Alignment).ConfigureAwait(false);
                                                WriteSuccess(result: true);
                                                break;
                                            }

                                        case PipeCommand.CloseProgressDialog:
                                            await DialogManager.CloseProgressDialogAsync().ConfigureAwait(false);
                                            WriteSuccess(!DialogManager.ProgressDialogOpen());
                                            break;

                                        case PipeCommand.ShowNotifyIcon:
                                            await DialogManager.ShowNotifyIconAsync(DeserializeBytes<ShowNotifyIconPayload>(requestBytes, payloadOffset).Options).ConfigureAwait(false);
                                            WriteSuccess(result: true);
                                            break;

                                        case PipeCommand.NotifyIconOpen:
                                            WriteSuccess(DialogManager.NotifyIconOpen());
                                            break;

                                        case PipeCommand.UpdateNotifyIcon:
                                            await DialogManager.UpdateNotifyIconAsync(DeserializeBytes<UpdateNotifyIconPayload>(requestBytes, payloadOffset).MessageText).ConfigureAwait(false);
                                            WriteSuccess(result: true);
                                            break;

                                        case PipeCommand.ShowBalloonTip:
                                            await DialogManager.ShowBalloonTipAsync(DeserializeBytes<ShowBalloonTipPayload>(requestBytes, payloadOffset).Options).ConfigureAwait(false);
                                            WriteSuccess(result: true);
                                            break;

                                        case PipeCommand.CloseNotifyIcon:
                                            await DialogManager.CloseNotifyIconAsync().ConfigureAwait(false);
                                            WriteSuccess(!DialogManager.NotifyIconOpen());
                                            break;

                                        case PipeCommand.MinimizeAllWindows:
                                            DesktopUtilities.MinimizeAllWindows();
                                            WriteSuccess(result: true);
                                            break;


                                        case PipeCommand.RestoreAllWindows:
                                            DesktopUtilities.RestoreAllWindows();
                                            WriteSuccess(result: true);
                                            break;

                                        case PipeCommand.SendKeys:
                                            await DialogManager.SendKeysAsync(DeserializeBytes<SendKeysPayload>(requestBytes, payloadOffset).Options).ConfigureAwait(false);
                                            WriteSuccess(result: true);
                                            break;

                                        case PipeCommand.GetProcessWindowInfo:
                                            WriteSuccess(WindowUtilities.GetProcessWindowInfo(DeserializeBytes<GetProcessWindowInfoPayload>(requestBytes, payloadOffset).Options));
                                            break;

                                        case PipeCommand.RefreshDesktopAndEnvironmentVariables:
                                            DesktopUtilities.RefreshDesktopAndEnvironmentVariables();
                                            WriteSuccess(result: true);
                                            break;

                                        case PipeCommand.GetUserNotificationState:
                                            WriteSuccess(DesktopUtilities.GetUserNotificationState());
                                            break;

                                        case PipeCommand.GetForegroundWindowProcessId:
                                            WriteSuccess(DesktopUtilities.GetForegroundWindowProcessId());
                                            break;

                                        case PipeCommand.GetEnvironmentVariable:
                                            WriteSuccess(EnvironmentUtilities.GetEnvironmentVariable(DeserializeBytes<EnvironmentVariablePayload>(requestBytes, payloadOffset).Name, EnvironmentVariableTarget.User) ?? ServerInstance.SuccessSentinel);
                                            break;

                                        case PipeCommand.SetEnvironmentVariable:
                                            {
                                                EnvironmentVariablePayload payload = DeserializeBytes<EnvironmentVariablePayload>(requestBytes, payloadOffset);
                                                EnvironmentUtilities.SetEnvironmentVariable(payload.Name, payload.Value, EnvironmentVariableTarget.User, payload.Expandable, payload.Append, payload.Remove);
                                                WriteSuccess(result: true);
                                                break;
                                            }

                                        case PipeCommand.RemoveEnvironmentVariable:
                                            EnvironmentUtilities.RemoveEnvironmentVariable(DeserializeBytes<EnvironmentVariablePayload>(requestBytes, payloadOffset).Name, EnvironmentVariableTarget.User);
                                            WriteSuccess(result: true);
                                            break;

                                        case PipeCommand.GroupPolicyUpdate:
                                            {
                                                using ProcessResult result = await GroupPolicyUpdateAsync(DeserializeBytes<GroupPolicyUpdatePayload>(requestBytes, payloadOffset).Force).ConfigureAwait(false);
                                                WriteSuccess(result);
                                                break;
                                            }

                                        case PipeCommand.ShellExecuteProcess:
                                            {
                                                using ProcessResult result = await ShellExecuteProcessAsync(DeserializeBytes<ShellExecuteProcessPayload>(requestBytes, payloadOffset).Options).ConfigureAwait(false);
                                                WriteSuccess(result);
                                                break;
                                            }

                                        case PipeCommand.GetUserFocusModeState:
                                            WriteSuccess(GetUserFocusModeState());
                                            break;

                                        case PipeCommand.GetUserToastNotificationMode:
                                            WriteSuccess(GetUserToastNotificationMode());
                                            break;

                                        default:
                                            throw new ClientException($"The specified command [{command}] is not recognised.", ClientExitCode.InvalidArguments);
                                    }
                                }
                                catch (Exception ex) when (ex.Message is not null)
                                {
                                    // Something we weren't expecting occurred. Write the error response.
                                    WriteError(ex);
                                }
                            }
                            catch (EndOfStreamException)
                            {
                                break;
                            }
                        }
                    }
                    finally
                    {
                        if (closeAppsDialogState is not null)
                        {
                            await closeAppsDialogState.DisposeAsync().ConfigureAwait(false);
                            closeAppsDialogState = null;
                        }
                    }
                    return (int)ClientExitCode.Success;
                }
            }
            catch (Exception ex) when (ex.Message is not null)
            {
                throw new ClientException("Failed to read or write from the pipe.", ClientExitCode.PipeReadWriteError, ex);
            }
        }

        /// <summary>
        /// Parses and executes a standalone command-line operation based on the specified arguments.
        /// </summary>
        /// <remarks>This method is intended for use in standalone or command-line scenarios where a
        /// single operation is performed per invocation. Supported operations include showing dialogs, managing
        /// environment variables, interacting with windows, and system actions such as restarting the computer. The
        /// specific operation is determined by the presence of recognized command-line switches in the arguments
        /// array.</remarks>
        /// <param name="argv">An array of command-line arguments that specify the operation to perform and any required options.</param>
        /// <returns>An integer exit code indicating the result of the operation. Returns 0 for success or a nonzero value for
        /// error conditions.</returns>
        /// <exception cref="ClientException">Thrown if required arguments are missing, invalid, or if the specified arguments do not correspond to a
        /// supported operation.</exception>
        private static async Task<int> EnterStandaloneModeAsync(string[] argv)
        {
            // Parse the arguments and execute the requested operation.
            foreach (string arg in argv)
            {
                if (arg.Equals("/ShowModalDialog", StringComparison.Ordinal) || arg.Equals("/smd", StringComparison.Ordinal))
                {
                    Console.WriteLine(await ShowModalDialogAsync(ArgvToDictionary(argv), argv: argv).ConfigureAwait(false));
                    return (int)ClientExitCode.Success;
                }
                if (arg.Equals("/GetProcessWindowInfo", StringComparison.Ordinal) || arg.Equals("/gpwi", StringComparison.Ordinal))
                {
                    Console.WriteLine(SerializeToString(WindowUtilities.GetProcessWindowInfo(DeserializeString<WindowInfoOptions>(GetOptionsFromArguments(ArgvToDictionary(argv))))));
                    return (int)ClientExitCode.Success;
                }
                if (arg.Equals("/GetUserNotificationState", StringComparison.Ordinal) || arg.Equals("/guns", StringComparison.Ordinal))
                {
                    Console.WriteLine(SerializeToString(DesktopUtilities.GetUserNotificationState()));
                    return (int)ClientExitCode.Success;
                }
                if (arg.Equals("/GetForegroundWindowProcessId", StringComparison.Ordinal) || arg.Equals("/gfwpi", StringComparison.Ordinal))
                {
                    Console.WriteLine(SerializeToString(DesktopUtilities.GetForegroundWindowProcessId()));
                    return (int)ClientExitCode.Success;
                }
                if (arg.Equals("/RefreshDesktopAndEnvironmentVariables", StringComparison.Ordinal) || arg.Equals("/rdaev", StringComparison.Ordinal))
                {
                    DesktopUtilities.RefreshDesktopAndEnvironmentVariables();
                    Console.WriteLine(SerializeToString(result: true));
                    return (int)ClientExitCode.Success;
                }
                if (arg.Equals("/MinimizeAllWindows", StringComparison.Ordinal) || arg.Equals("/maw", StringComparison.Ordinal))
                {
                    DesktopUtilities.MinimizeAllWindows();
                    Console.WriteLine(SerializeToString(result: true));
                    return (int)ClientExitCode.Success;
                }
                if (arg.Equals("/RestoreAllWindows", StringComparison.Ordinal) || arg.Equals("/raw", StringComparison.Ordinal))
                {
                    DesktopUtilities.RestoreAllWindows();
                    Console.WriteLine(SerializeToString(result: true));
                    return (int)ClientExitCode.Success;
                }
                if (arg.Equals("/SendKeys", StringComparison.Ordinal) || arg.Equals("/sk", StringComparison.Ordinal))
                {
                    await DialogManager.SendKeysAsync(DeserializeString<SendKeysOptions>(GetOptionsFromArguments(ArgvToDictionary(argv)))).ConfigureAwait(false);
                    Console.WriteLine(SerializeToString(result: true));
                    return (int)ClientExitCode.Success;
                }
                if (arg.Equals("/GetEnvironmentVariable", StringComparison.Ordinal) || arg.Equals("/gev", StringComparison.Ordinal))
                {
                    if (ArgvToDictionary(argv) is not ReadOnlyDictionary<string, string> arguments || !arguments.TryGetValue("Variable", out string? variable) || string.IsNullOrWhiteSpace(variable))
                    {
                        throw new ClientException("A required Variable was not specified on the command line.", ClientExitCode.InvalidArguments);
                    }
                    Console.WriteLine(SerializeToString(EnvironmentUtilities.GetEnvironmentVariable(variable, EnvironmentVariableTarget.User) ?? ServerInstance.SuccessSentinel));
                    return (int)ClientExitCode.Success;
                }
                if (arg.Equals("/SetEnvironmentVariable", StringComparison.Ordinal) || arg.Equals("/sev", StringComparison.Ordinal))
                {
                    if (ArgvToDictionary(argv) is not ReadOnlyDictionary<string, string> arguments || !arguments.TryGetValue("Variable", out string? variable) || string.IsNullOrWhiteSpace(variable))
                    {
                        throw new ClientException("A required Variable was not specified on the command line.", ClientExitCode.InvalidArguments);
                    }
                    if (!arguments.TryGetValue("Value", out string? value) || string.IsNullOrWhiteSpace(value))
                    {
                        throw new ClientException("A required Value was not specified on the command line.", ClientExitCode.InvalidArguments);
                    }
                    if (!arguments.TryGetValue("Expandable", out string? expandableStr) || string.IsNullOrWhiteSpace(expandableStr) || !bool.TryParse(expandableStr, out bool expandable))
                    {
                        throw new ClientException("The 'Expandable' argument is required and cannot be null or whitespace.", ClientExitCode.InvalidArguments);
                    }
                    if (!arguments.TryGetValue("Append", out string? appendStr) || string.IsNullOrWhiteSpace(appendStr) || !bool.TryParse(appendStr, out bool append))
                    {
                        throw new ClientException("The 'Expandable' argument is required and cannot be null or whitespace.", ClientExitCode.InvalidArguments);
                    }
                    if (!arguments.TryGetValue("Remove", out string? removeStr) || string.IsNullOrWhiteSpace(removeStr) || !bool.TryParse(removeStr, out bool remove))
                    {
                        throw new ClientException("The 'Expandable' argument is required and cannot be null or whitespace.", ClientExitCode.InvalidArguments);
                    }
                    EnvironmentUtilities.SetEnvironmentVariable(variable, value, EnvironmentVariableTarget.User, expandable, append, remove);
                    Console.WriteLine(SerializeToString(result: true));
                    return (int)ClientExitCode.Success;
                }
                if (arg.Equals("/RemoveEnvironmentVariable", StringComparison.Ordinal) || arg.Equals("/rev", StringComparison.Ordinal))
                {
                    if (!ArgvToDictionary(argv).TryGetValue("Variable", out string? variable) || string.IsNullOrWhiteSpace(variable))
                    {
                        throw new ClientException("A required Variable was not specified on the command line.", ClientExitCode.InvalidArguments);
                    }
                    EnvironmentUtilities.RemoveEnvironmentVariable(variable, EnvironmentVariableTarget.User);
                    Console.WriteLine(SerializeToString(result: true));
                    return (int)ClientExitCode.Success;
                }
                if (arg.Equals("/SilentRestart", StringComparison.Ordinal) || arg.Equals("/sr", StringComparison.Ordinal))
                {
                    if (!ArgvToDictionary(argv).TryGetValue("Delay", out string? delayArg) || string.IsNullOrWhiteSpace(delayArg) || !int.TryParse(delayArg, NumberStyles.Integer, CultureInfo.InvariantCulture, out int delayValue))
                    {
                        throw new ClientException("A required Delay was not specified on the command line.", ClientExitCode.InvalidArguments);
                    }
                    ClientServerUtilities.SetOperationSuccessFlag();
                    await Task.Delay(delayValue * 1000).ConfigureAwait(false);
                    await DeviceUtilities.RestartComputer().ConfigureAwait(false);
                    Console.WriteLine(SerializeToString(result: true));
                    return (int)ClientExitCode.Success;
                }
                if (arg.Equals("/GetLastInputTime", StringComparison.Ordinal) || arg.Equals("/glit", StringComparison.Ordinal))
                {
                    Console.WriteLine(DesktopUtilities.GetLastInputTime().Ticks);
                    return (int)ClientExitCode.Success;
                }
                if (arg.Equals("/TokenBroker", StringComparison.Ordinal) || arg.Equals("/tb", StringComparison.Ordinal))
                {
                    await BrokerTokenForCaller(ArgvToDictionary(argv)).ConfigureAwait(false);
                    return (int)ClientExitCode.Success;
                }
                if (arg.Equals("/GroupPolicyUpdate", StringComparison.Ordinal) || arg.Equals("/gpu", StringComparison.Ordinal))
                {
                    if (ArgvToDictionary(argv) is not ReadOnlyDictionary<string, string> arguments || !arguments.TryGetValue("Force", out string? forceStr) || string.IsNullOrWhiteSpace(forceStr) || !bool.TryParse(forceStr, out bool force))
                    {
                        throw new ClientException("The 'Sync' argument is required and cannot be null or whitespace.", ClientExitCode.InvalidArguments);
                    }
                    ClientServerUtilities.SetOperationSuccessFlag();
                    using ProcessResult result = await GroupPolicyUpdateAsync(force).ConfigureAwait(false);
                    Console.WriteLine(SerializeToString(result));
                    return (int)ClientExitCode.Success;
                }
                if (arg.Equals("/ShellExecuteProcess", StringComparison.Ordinal) || arg.Equals("/sep", StringComparison.Ordinal))
                {
                    using ProcessResult result = await ShellExecuteProcessAsync(DeserializeString<UserShellExecuteOptions>(GetOptionsFromArguments(ArgvToDictionary(argv)))).ConfigureAwait(false);
                    Console.WriteLine(SerializeToString(result));
                    return (int)ClientExitCode.Success;
                }
                if (arg.Equals("/GetUserFocusModeState", StringComparison.Ordinal) || arg.Equals("/gufms", StringComparison.Ordinal))
                {
                    Console.WriteLine(SerializeToString(GetUserFocusModeState()));
                    return (int)ClientExitCode.Success;
                }
                if (arg.Equals("/GetUserToastNotificationMode", StringComparison.Ordinal) || arg.Equals("/gutnm", StringComparison.Ordinal))
                {
                    Console.WriteLine(SerializeToString(GetUserToastNotificationMode()));
                    return (int)ClientExitCode.Success;
                }
            }
            throw new ClientException("The specified arguments were unable to be resolved into a type of operation.", ClientExitCode.InvalidMode);
        }

        /// <summary>
        /// Displays a modal dialog of the specified type and style, using the provided arguments to configure its
        /// behavior.
        /// </summary>
        /// <remarks>The dialog type and style must be specified in the arguments. If the dialog is
        /// configured to block execution and is running as the SYSTEM account, the method may launch a process and exit
        /// with its exit code. The returned string can be deserialized to obtain the dialog result.</remarks>
        /// <param name="arguments">A read-only dictionary containing the arguments required to configure the dialog. Must include valid values
        /// for 'DialogType' and 'DialogStyle'.</param>
        /// <param name="closeAppsDialogState">An optional state object that can influence the dialog's behavior when handling application closure
        /// scenarios.</param>
        /// <param name="argv">An optional array of command-line arguments used to determine the executable to launch if the dialog is
        /// configured for execution blocking.</param>
        /// <returns>A serialized string representing the result of the modal dialog, such as the selected button text or other
        /// relevant outcome information.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the method is configured to block execution but fails to launch the specified process.</exception>"
        /// <exception cref="ClientException">Thrown if a required argument is missing or invalid, such as when 'DialogType' or 'DialogStyle' is not
        /// specified or is invalid, or if the dialog type is not supported.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Blocker Code Smell", "S1147:Exit methods should not be called", Justification = "This code can deliberately short circuit.")]
        private static async Task<string> ShowModalDialogAsync(ReadOnlyDictionary<string, string> arguments, BaseDialogState? closeAppsDialogState = null, string[]? argv = null)
        {
            // Return early if this is a BlockExecution dialog and we're running as SYSTEM.
            if (arguments.TryGetValue("BlockExecution", out string? blockExecutionArg) && bool.TryParse(blockExecutionArg, out bool blockExecution) && blockExecution && AccountUtilities.CallerIsLocalSystem && argv is not null)
            {
                // Exit with the underlying process's exit code if available, otherwise exit with the BlockExecution button text.
                string[] command = [.. argv.SkipWhile(static arg => !File.Exists(arg))]; string filePath = command[0]; IEnumerable<string>? argumentList = command.Length > 1 ? command.Skip(1) : null;
                using (ProcessResult result = await (ProcessManager.LaunchAsync(new(filePath, argumentList, Environment.CurrentDirectory, bypassIfeo: true)) ?? throw new InvalidOperationException("Failed to launch the process.")).ConfigureAwait(false))
                {
                    Environment.Exit(result.ExitCode);
                }
                return SerializeToString(BlockExecution.ButtonText);
            }

            // Confirm we have a DialogType and that it's valid.
            if (!arguments.TryGetValue("DialogType", out string? dialogTypeArg) || string.IsNullOrWhiteSpace(dialogTypeArg))
            {
                throw new ClientException("A required DialogType was not specified on the command line.", ClientExitCode.NoDialogType);
            }
            if (!Enum.TryParse(dialogTypeArg, ignoreCase: true, out DialogType dialogType))
            {
                throw new ClientException($"The specified DialogType of [{dialogTypeArg}] is invalid.", ClientExitCode.InvalidDialog);
            }

            // Confirm we've got a DialogStyle and that it's valid.
            if (!arguments.TryGetValue("DialogStyle", out string? dialogStyleArg) || string.IsNullOrWhiteSpace(dialogStyleArg))
            {
                throw new ClientException("A required DialogStyle was not specified on the command line.", ClientExitCode.NoDialogStyle);
            }
            if (!Enum.TryParse(dialogStyleArg, ignoreCase: true, out DialogStyle dialogStyle))
            {
                throw new ClientException($"The specified DialogStyle of [{dialogStyleArg}] is invalid.", ClientExitCode.NoDialogStyle);
            }

            // Deserialize the options to the correct type based on DialogType and show the dialog.
            IDialogOptions options = dialogType switch
            {
                DialogType.CloseAppsDialog => DataSerialization.DeserializeFromString<CloseAppsDialogOptions>(GetOptionsFromArguments(arguments)),
                DialogType.CustomDialog => DataSerialization.DeserializeFromString<CustomDialogOptions>(GetOptionsFromArguments(arguments)),
                DialogType.DialogBox => DataSerialization.DeserializeFromString<DialogBoxOptions>(GetOptionsFromArguments(arguments)),
                DialogType.HelpConsole => DataSerialization.DeserializeFromString<HelpConsoleOptions>(GetOptionsFromArguments(arguments)),
                DialogType.InputDialog => DataSerialization.DeserializeFromString<InputDialogOptions>(GetOptionsFromArguments(arguments)),
                DialogType.ListSelectionDialog => DataSerialization.DeserializeFromString<ListSelectionDialogOptions>(GetOptionsFromArguments(arguments)),
                DialogType.RestartDialog => DataSerialization.DeserializeFromString<RestartDialogOptions>(GetOptionsFromArguments(arguments)),
                DialogType.ProgressDialog or _ => throw new ClientException($"The specified DialogType of [{dialogType}] is not supported for deserialization.", ClientExitCode.UnsupportedDialog),
            };
            return SerializeToString(await InvokeModalDialogAsync(dialogType, dialogStyle, options, closeAppsDialogState).ConfigureAwait(false));
        }

        /// <summary>
        /// Displays a modal dialog of the specified type and style, using the provided options and optional state
        /// information.
        /// </summary>
        /// <remarks>The caller is responsible for providing the correct options and state objects
        /// matching the selected dialog type. Passing an incorrect type for the options or state parameters may result
        /// in a runtime exception. Not all dialog types require a style or state parameter; these are only used for
        /// dialog types that support them.</remarks>
        /// <param name="dialogType">The type of dialog to display. Must be a supported value of <see cref="DialogType"/>.</param>
        /// <param name="dialogStyle">The visual style or presentation mode to use for the dialog. This parameter is required for dialog types
        /// that support styling.</param>
        /// <param name="options">An options object containing configuration data specific to the selected dialog type. The object must be of
        /// the appropriate type for the dialog.</param>
        /// <param name="closeAppsDialogState">An optional state object required when displaying a CloseAppsDialog. Must be of type <see
        /// cref="CloseAppsDialogState"/> if <paramref name="dialogType"/> is <see cref="DialogType.CloseAppsDialog"/>;
        /// otherwise, this parameter is ignored.</param>
        /// <returns>An object representing the result of the dialog interaction. The type and meaning of the return value depend
        /// on the dialog type displayed.</returns>
        /// <exception cref="ClientException">Thrown if an unsupported dialog type is specified, or if <paramref name="dialogType"/> is <see
        /// cref="DialogType.CloseAppsDialog"/> and <paramref name="closeAppsDialogState"/> is not provided.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static async Task<object> InvokeModalDialogAsync(DialogType dialogType, DialogStyle dialogStyle, IDialogOptions options, BaseDialogState? closeAppsDialogState = null)
        {
            return dialogType switch
            {
                DialogType.CloseAppsDialog => options is CloseAppsDialogOptions closeAppsOptions ? await DialogManager.ShowCloseAppsDialogAsync(dialogStyle, closeAppsOptions, (CloseAppsDialogState?)closeAppsDialogState ?? throw new ClientException("A required CloseAppsDialogState was not provided for the CloseAppsDialog.", ClientExitCode.NoCloseAppsDialogState)).ConfigureAwait(false) : throw new ClientException($"The specified options type [{options.GetType().FullName}] is invalid for dialog type [{dialogType}].", ClientExitCode.InvalidOptions),
                DialogType.DialogBox => options is DialogBoxOptions dialogBoxOptions ? await DialogManager.ShowDialogBoxAsync(dialogBoxOptions).ConfigureAwait(false) : throw new ClientException($"The specified options type [{options.GetType().FullName}] is invalid for dialog type [{dialogType}].", ClientExitCode.InvalidOptions),
                DialogType.HelpConsole => options is HelpConsoleOptions helpConsoleOptions ? await DialogManager.ShowHelpConsoleAsync(helpConsoleOptions).ConfigureAwait(false) : throw new ClientException($"The specified options type [{options.GetType().FullName}] is invalid for dialog type [{dialogType}].", ClientExitCode.InvalidOptions),
                DialogType.InputDialog => options is InputDialogOptions inputDialogOptions ? await DialogManager.ShowInputDialogAsync(dialogStyle, inputDialogOptions).ConfigureAwait(false) : throw new ClientException($"The specified options type [{options.GetType().FullName}] is invalid for dialog type [{dialogType}].", ClientExitCode.InvalidOptions),
                DialogType.CustomDialog => options is CustomDialogOptions customDialogOptions ? await DialogManager.ShowCustomDialogAsync(dialogStyle, customDialogOptions).ConfigureAwait(false) : throw new ClientException($"The specified options type [{options.GetType().FullName}] is invalid for dialog type [{dialogType}].", ClientExitCode.InvalidOptions),
                DialogType.ListSelectionDialog => options is ListSelectionDialogOptions listSelectionDialogOptions ? await DialogManager.ShowListSelectionDialogAsync(dialogStyle, listSelectionDialogOptions).ConfigureAwait(false) : throw new ClientException($"The specified options type [{options.GetType().FullName}] is invalid for dialog type [{dialogType}].", ClientExitCode.InvalidOptions),
                DialogType.RestartDialog => options is RestartDialogOptions restartDialogOptions ? await DialogManager.ShowRestartDialogAsync(dialogStyle, restartDialogOptions).ConfigureAwait(false) : throw new ClientException($"The specified options type [{options.GetType().FullName}] is invalid for dialog type [{dialogType}].", ClientExitCode.InvalidOptions),
                DialogType.ProgressDialog or _ => throw new ClientException($"The specified DialogType of [{dialogType}] is not supported.", ClientExitCode.UnsupportedDialog),
            };
        }

        /// <summary>
        /// Brokers a security token for a caller process by duplicating a user token and transmitting it over a named
        /// pipe. This operation is restricted to processes running as the Local System account.
        /// </summary>
        /// <remarks>This method is intended for internal use in scenarios where a privileged process
        /// needs to broker a user token to another process securely. The operation requires elevated privileges and
        /// should only be invoked in trusted environments. The method communicates with the target process via a named
        /// pipe and expects all required arguments to be present and valid.</remarks>
        /// <param name="arguments">A read-only dictionary containing the required arguments for token brokering. Must include the following
        /// keys: 'PipeName' (the name of the pipe to connect to), 'ProcessId' (the ID of the target process),
        /// 'SessionId' (the session ID for the user token), 'UseLinkedAdminToken' (whether to use the linked
        /// administrator token), and 'UseHighestAvailableToken' (whether to use the highest available token). All
        /// values must be non-null and non-whitespace.</param>
        /// <exception cref="ClientException">Thrown if the caller is not running as the Local System account, or if any required argument is missing,
        /// invalid, or cannot be parsed.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "MA0099:Use Explicit enum value instead of 0", Justification = "There's no zero value for this enum.")]
        private static async Task BrokerTokenForCaller(ReadOnlyDictionary<string, string> arguments)
        {
            // Confirm we're running as the SYSTEM account before proceeding.
            if (!AccountUtilities.CallerIsLocalSystem)
            {
                throw new ClientException("Token brokering can only be performed when running as the Local System account.", ClientExitCode.InvalidCaller);
            }

            // Read our arguments and make sure they're all valid.
            if (!arguments.TryGetValue("PipeName", out string? pipeName) || string.IsNullOrWhiteSpace(pipeName))
            {
                throw new ClientException("The 'PipeName' argument is required and cannot be null or whitespace.", ClientExitCode.InvalidArguments);
            }
            if (!arguments.TryGetValue("ProcessId", out string? processIdStr) || string.IsNullOrWhiteSpace(processIdStr) || !uint.TryParse(processIdStr, out uint processId))
            {
                throw new ClientException("The 'ProcessId' argument is required and cannot be null or whitespace.", ClientExitCode.InvalidArguments);
            }
            if (!arguments.TryGetValue("SessionId", out string? sessionIdStr) || string.IsNullOrWhiteSpace(sessionIdStr) || !uint.TryParse(sessionIdStr, out uint sessionId))
            {
                throw new ClientException("The 'SessionId' argument is required and cannot be null or whitespace.", ClientExitCode.InvalidArguments);
            }
            if (!arguments.TryGetValue("UIAccess", out string? uiAccessStr) || string.IsNullOrWhiteSpace(uiAccessStr) || !bool.TryParse(uiAccessStr, out bool uiAccess))
            {
                throw new ClientException("The 'UIAccess' argument is required and cannot be null or whitespace.", ClientExitCode.InvalidArguments);
            }

            // Confirm we've got a ElevatedTokenType and that it's valid.
            if (!arguments.TryGetValue("ElevatedTokenType", out string? elevatedTokenTypeArg) || string.IsNullOrWhiteSpace(elevatedTokenTypeArg))
            {
                throw new ClientException("A required ElevatedTokenType was not specified on the command line.", ClientExitCode.InvalidArguments);
            }
            if (!Enum.TryParse(elevatedTokenTypeArg, ignoreCase: true, out ElevatedTokenType elevatedTokenType))
            {
                throw new ClientException($"The specified ElevatedTokenType of [{elevatedTokenType}] is invalid.", ClientExitCode.InvalidArguments);
            }

            // Confirm the session Id is greater than 0; we never want to broker SYSTEM tokens.
            if (sessionId == 0)
            {
                throw new ClientException("Brokering of the Local System session token is not permitted.", ClientExitCode.InvalidArguments);
            }

            // Connect to the named pipe server.
            using NamedPipeClientStream pipe = new(".", pipeName, PipeDirection.InOut, PipeOptions.None);
            await pipe.ConnectAsync().ConfigureAwait(false);

            // Duplicate the token to the specified process ID.
            SafeFileHandle hDupToken;
            using (SafeFileHandle hPrimaryToken = await TokenManager.GetUserPrimaryTokenAsync(sessionId, elevatedTokenType, uiAccess).ConfigureAwait(false))
            using (SafeFileHandle hSourceProcess = NativeMethods.OpenProcess(PROCESS_ACCESS_RIGHTS.PROCESS_DUP_HANDLE, bInheritHandle: false, processId))
            using (SafeProcessHandle hCurrentProcess = NativeMethods.GetCurrentProcess())
            {
                _ = NativeMethods.DuplicateHandle(hCurrentProcess, hPrimaryToken, hSourceProcess, out hDupToken, 0, bInheritHandle: false, DUPLICATE_HANDLE_OPTIONS.DUPLICATE_SAME_ACCESS);
            }

            // Write the duplicated token to the pipe.
            using (hDupToken)
            {
                if (IntPtr.Size == 8)
                {
                    pipe.WriteByte(8); await pipe.WriteAsync(BitConverter.GetBytes(hDupToken.DangerousGetHandle().ToInt64()), 0, 8).ConfigureAwait(false);
                }
                else
                {
                    pipe.WriteByte(4); await pipe.WriteAsync(BitConverter.GetBytes(hDupToken.DangerousGetHandle().ToInt32()), 0, 4).ConfigureAwait(false);
                }
            }
            await pipe.FlushAsync().ConfigureAwait(false); pipe.WaitForPipeDrain();
        }

        /// <summary>
        /// Runs a Group Policy update on the local machine by invoking the gpupdate utility.
        /// </summary>
        /// <param name="force">A value indicating whether to force the update, reapplying all policy settings even if they have not
        /// changed. If <see langword="true"/>, all settings are reapplied.</param>
        /// <returns>A <see cref="ProcessResult"/> object that contains the results of the Group Policy update operation.</returns>
        /// <exception cref="ClientException">Thrown if the Group Policy update process fails to launch.</exception>
        private static async Task<ProcessResult> GroupPolicyUpdateAsync(bool force)
        {
            // Build out argument list for gpupdate.exe.
            List<string> argumentList = ["/Target:User"];
            if (force)
            {
                argumentList.Add("/Force");
            }

            // Set up the process and return its result.
            return ProcessManager.LaunchAsync(new(Path.Join(Environment.SystemDirectory, "gpupdate.exe"), argumentList, standardInput: ["N"], createNoWindow: true)) is not ProcessHandle handle
                ? throw new ClientException("Failed to launch the Group Policy update process.", ClientExitCode.InvalidResult)
                : await handle.ConfigureAwait(false);
        }

        /// <summary>
        /// Executes a process using the specified shell execution options and returns the result asynchronously.
        /// </summary>
        /// <param name="options">The options that define how the process should be launched, including executable path, arguments, and user
        /// context.</param>
        /// <returns>A ProcessResult object containing the outcome of the executed process. If the process could not be started,
        /// returns a result indicating success with a default code.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "It's either this or a 'Dispose objects before losing scope' warning on the ternary.")]
        private static async Task<ProcessResult> ShellExecuteProcessAsync(UserShellExecuteOptions options)
        {
            if (ProcessManager.LaunchAsync(options.ToLaunchInfo()) is not ProcessHandle handle)
            {
                return new(ClientServerUtilities.ShellExecuteProcessSuccessCode);
            }
            return await handle.ConfigureAwait(false);
        }

        /// <summary>
        /// Retrieves the current focus mode state for the user.
        /// </summary>
        /// <remarks>Focus mode is a Windows feature that helps minimize distractions by suppressing
        /// notifications and other interruptions. The return value indicates whether focus mode is currently active for
        /// the user, or if the state could not be determined due to an error or unsupported environment.</remarks>
        /// <returns>1 if focus mode is active; 0 if focus mode is inactive; -1 if the focus mode state could not be determined.</returns>
        private static int GetUserFocusModeState()
        {
            return !ShellUtilities.TryGetFocusSessionActive(out bool? active) || !active.HasValue ? -1 : active.Value ? 1 : 0;
        }

        /// <summary>
        /// Retrieves the current toast notification mode for the user.
        /// </summary>
        /// <remarks>The returned value may be -1 if the notification mode is unavailable or cannot be
        /// retrieved. Callers should check for this value to handle such cases appropriately.</remarks>
        /// <returns>A value of the <see cref="ToastNotificationMode"/> enumeration that indicates the user's toast notification
        /// mode. Returns a value of -1 if the mode cannot be determined.</returns>
        private static int GetUserToastNotificationMode()
        {
            return !NotificationsUtilities.TryGetNotificationMode(out ToastNotificationMode mode) ? -1 : (int)mode;
        }

        /// <summary>
        /// Converts an array of command-line arguments into a read-only dictionary of key-value pairs.
        /// </summary>
        /// <remarks>Each key in the input must start with a hyphen ('-'), and its value must immediately
        /// follow as a separate argument. If a key is not followed by a valid value (e.g., null, empty, or another
        /// key-like argument), the method writes an error message to the standard error stream and terminates the
        /// application with an exit code indicating invalid arguments.</remarks>
        /// <param name="argv">An array of strings representing command-line arguments. Each key must be prefixed with a hyphen ('-') and
        /// followed by its corresponding value as a separate argument.</param>
        /// <returns>A <see cref="ReadOnlyDictionary{TKey, TValue}"/> containing the parsed key-value pairs from the input
        /// arguments.</returns>
        /// <exception cref="ClientException">Thrown if any argument key is not followed by a valid value, such as when the value is null, empty, whitespace, or resembles another key.</exception>"
        private static ReadOnlyDictionary<string, string> ArgvToDictionary(string[] argv)
        {
            // Loop through arguments and match argument names to their values.
            Dictionary<string, string> arguments = [];
            for (int i = 0; i < argv.Length; i++)
            {
                if (!argv[i].StartsWith("-"))
                {
                    continue;
                }
                string key = argv[i][1..].Trim();
                string? value = (i + 1 < argv.Length) ? argv[i + 1].Trim() : null;
                if (value is null || string.IsNullOrWhiteSpace(value) || value.StartsWith("-") || value.StartsWith("/"))
                {
                    throw new ClientException($"The argument [{argv[i]}] has an invalid value.", ClientExitCode.InvalidArguments);
                }
                arguments.Add(key, value);
            }

            // Check whether an ArgumentsDictionary was provided.
            if (arguments.TryGetValue("ArgumentsDictionary", out string? argvDictValue) || arguments.TryGetValue("ArgV", out argvDictValue))
            {
                // Assume it's a registry key if it starts with HKEY, otherwise assume it's a file path or literal string.
                if (argvDictValue.StartsWith("HKEY", StringComparison.Ordinal))
                {
                    // Provided value is a registry key path.
                    int lastBackslashIndex = argvDictValue.LastIndexOf('\\');
                    using RegistryKey registryKey = RegistryUtilities.GetRegistryKeyForPath(argvDictValue[..lastBackslashIndex]);
                    return registryKey.GetValue(argvDictValue[(lastBackslashIndex + 1)..], defaultValue: null) is not string argvDictContent
                        ? throw new ClientException($"The specified ArgumentsDictionary registry key [{argvDictValue}] does not exist or is invalid.", ClientExitCode.InvalidArguments)
                        : DeserializeString<ReadOnlyDictionary<string, string>>(argvDictContent);
                }
                return File.Exists(argvDictValue)
                    ? DeserializeString<ReadOnlyDictionary<string, string>>(File.ReadAllText(argvDictValue))
                    : DeserializeString<ReadOnlyDictionary<string, string>>(argvDictValue);
            }

            // This data should never change once read, so return read-only.
            return new(arguments);
        }

        /// <summary>
        /// Retrieves the value of the "Options" key from the provided arguments dictionary.
        /// </summary>
        /// <remarks>This method ensures that the "Options" key exists and its value is valid. If the key
        /// is missing or the value is invalid, a <see cref="ClientException"/> is thrown.</remarks>
        /// <param name="arguments">A read-only dictionary containing key-value pairs of command-line arguments. Must include a valid "Options"
        /// key.</param>
        /// <returns>The value associated with the "Options" key in the dictionary.</returns>
        /// <exception cref="ClientException">Thrown if the "Options" key is missing, null, or contains only whitespace.</exception>
        private static string GetOptionsFromArguments(ReadOnlyDictionary<string, string> arguments)
        {
            // Confirm we have options and they're not null/invalid.
            return !arguments.TryGetValue("Options", out string? options)
                ? throw new ClientException("The required options were not specified on the command line.", ClientExitCode.NoOptions)
                : string.IsNullOrWhiteSpace(options)
                ? throw new ClientException("The specified options are null or invalid.", ClientExitCode.InvalidOptions)
                : options;
        }

        /// <summary>
        /// Deserializes the specified byte span into an object of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of the object to deserialize.</typeparam>
        /// <param name="input">The UTF-8 encoded byte span representation of the object to deserialize. Cannot be empty.</param>
        /// <param name="offset">The zero-based byte offset in the input span where deserialization should begin.</param>
        /// <returns>An object of type <typeparamref name="T"/> deserialized from the input bytes.</returns>
        /// <exception cref="ClientException">Thrown if an error occurs during deserialization, such as invalid input format or type mismatch.</exception>
        private static T DeserializeBytes<T>(byte[] input, int offset)
        {
            try
            {
                return DataSerialization.DeserializeFromBytes<T>(input, offset);
            }
            catch (Exception ex) when (ex.Message is not null)
            {
                throw new ClientException("An error occurred while deserializing the provided input.", ClientExitCode.InvalidOptions, ex);
            }
        }

        /// <summary>
        /// Deserializes the specified string into an object of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of the object to deserialize.</typeparam>
        /// <param name="input">The string representation of the object to deserialize. Cannot be null or empty.</param>
        /// <returns>An object of type <typeparamref name="T"/> deserialized from the input string.</returns>
        /// <exception cref="ClientException">Thrown if an error occurs during deserialization, such as invalid input format or type mismatch.</exception>
        private static T DeserializeString<T>(string input)
        {
            try
            {
                return DataSerialization.DeserializeFromString<T>(input);
            }
            catch (Exception ex) when (ex.Message is not null)
            {
                throw new ClientException("An error occurred while deserializing the provided input.", ClientExitCode.InvalidOptions, ex);
            }
        }

        /// <summary>
        /// Serializes the specified object into a UTF-8 encoded byte array.
        /// </summary>
        /// <typeparam name="T">The type of the object to serialize.</typeparam>
        /// <param name="result">The object to be serialized. Cannot be null.</param>
        /// <returns>A UTF-8 encoded byte array representation of the serialized object.</returns>
        /// <exception cref="ClientException">Thrown if an error occurs during serialization. The exception includes details about the failure.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Critical Code Smell", "S2302:\"nameof\" should be used", Justification = "This is a false positive.")]
        private static byte[] SerializeToBytes<T>(T result)
        {
            try
            {
                return DataSerialization.SerializeToBytes(result);
            }
            catch (Exception ex) when (ex.Message is not null)
            {
                throw new ClientException("An error occurred while serializing the provided result.", ClientExitCode.InvalidResult, ex);
            }
        }

        /// <summary>
        /// Serializes the specified object into a string representation.
        /// </summary>
        /// <typeparam name="T">The type of the object to serialize.</typeparam>
        /// <param name="result">The object to be serialized. Cannot be null.</param>
        /// <returns>A string representation of the serialized object.</returns>
        /// <exception cref="ClientException">Thrown if an error occurs during serialization. The exception includes details about the failure.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Critical Code Smell", "S2302:\"nameof\" should be used", Justification = "This is a false positive.")]
        private static string SerializeToString<T>(T result)
        {
            try
            {
                return DataSerialization.SerializeToString(result);
            }
            catch (Exception ex) when (ex.Message is not null)
            {
                throw new ClientException("An error occurred while serializing the provided result.", ClientExitCode.InvalidResult, ex);
            }
        }

        /// <summary>
        /// Handles an unhandled exception by reporting the error and determining the process exit code.
        /// </summary>
        /// <remarks>If the current process is a launcher, the method terminates the process immediately
        /// using Environment.FailFast. Otherwise, it writes the serialized exception to the standard error
        /// stream.</remarks>
        /// <param name="exception">The exception that triggered the error handler. Cannot be null.</param>
        /// <param name="message">A descriptive message to include in the error report.</param>
        /// <param name="exitCode">An optional exit code to use when terminating the process. If null, the exception's HResult is used.</param>
        /// <returns>An integer representing the process exit code. Returns the specified exit code if provided; otherwise,
        /// returns the HResult of the exception.</returns>
        private static int InvokeMainErrorHandler(Exception exception, string message, ClientExitCode? exitCode = null)
        {
            if (ClientServerUtilities.CallerIsClientServerClientLauncher)
            {
                Environment.FailFast($"{message.TrimEnd('.')}.{Environment.NewLine}Exception Info: {exception}", exception);
            }
            try
            {
                Console.Error.WriteLine(DataSerialization.SerializeToString(exception));
            }
            catch (Exception ex) when (ex.Message is not null)
            {
                Environment.FailFast($"An unexpected exception occurred while serializing main exception [{ex}].{Environment.NewLine}Exception Info: {exception}", exception);
            }
            return (int?)exitCode ?? exception.HResult;
        }

        /// <summary>
        /// The <see cref="Assembly"/> containing the <see cref="ClientExecutable"/> type.
        /// </summary>
        private static readonly Assembly AssemblyInfo = typeof(ClientExecutable).Assembly;
    }
}
