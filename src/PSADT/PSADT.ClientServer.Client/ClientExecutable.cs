using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Threading;
using Microsoft.Win32;
using PSADT.AccountManagement;
using PSADT.ClientServer.Payloads;
using PSADT.DeviceManagement;
using PSADT.LibraryInterfaces;
using PSADT.ProcessManagement;
using PSADT.RegistryManagement;
using PSADT.Types;
using PSADT.UserInterface;
using PSADT.UserInterface.DialogOptions;
using PSADT.UserInterface.Dialogs;
using PSADT.UserInterface.DialogState;
using PSADT.Utilities;
using PSADT.WindowManagement;
using PSAppDeployToolkit.Logging;
using Windows.Win32.Foundation;

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
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static int Main(string[] argv)
        {
            // Detect what mode the executable has been asked to run in.
            try
            {
                // Determine the mode of operation based on the provided arguments.
                if (!(argv?.Length > 0))
                {
                    FileVersionInfo fileInfo = FileVersionInfo.GetVersionInfo(typeof(ClientExecutable).Assembly.Location);
                    Version helpVersion = new(fileInfo.ProductVersion!.Split('+')[0]);
                    string helpTitle = $"{fileInfo.FileDescription!} {helpVersion}";
                    string helpMessage = string.Join(Environment.NewLine,
                    [
                        helpTitle,
                        "",
                        fileInfo.LegalCopyright,
                        "",
                        "This application is designed to be used with the PSAppDeployToolkit PowerShell module and should not be directly invoked.",
                        "",
                        "If you're an end-user or employee of your organization, please report this message to your helpdesk for further assistance.",
                    ]);
                    _ = DialogManager.ShowDialogBox(helpTitle, helpMessage, DialogBoxButtons.Ok, DialogBoxDefaultButton.First, DialogBoxIcon.Stop, true, default);
                    throw new ClientException("No arguments were provided to the display server.", ClientExitCode.NoArguments);
                }
                return argv.Any(static arg => arg is "/ClientServer" or "/cs") ? EnterClientServerMode(ArgvToDictionary(argv)) : EnterStandaloneMode(argv);
            }
            catch (ClientException ex)
            {
                // We've caught our own error. Write it out, the error handler will get the exit code out of it.
                return InvokeMainErrorHandler(ex, $"Failed to perform the requested operation with error code [{ex.HResult}].");
            }
            catch (Exception ex) when (ex.Message is not null)
            {
                // This block is here as a fail-safe and should never be reached.
                return InvokeMainErrorHandler(ex, $"An unexpected exception occurred with HRESULT [{ex.HResult}].", ClientExitCode.Unknown);
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
        private static int EnterClientServerMode(ReadOnlyDictionary<string, string> arguments)
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
            catch (Exception ex)
            {
                throw new ClientException($"Failed to open a pipe client for the specified OutputHandle.", ClientExitCode.InvalidOutputPipe, ex);
            }
            try
            {
                inputPipeClient = new(PipeDirection.In, inputPipeHandle);
            }
            catch (Exception ex)
            {
                throw new ClientException($"Failed to open a pipe client for the specified InputHandle.", ClientExitCode.InvalidInputPipe, ex);
            }
            try
            {
                logPipeClient = new(PipeDirection.Out, logPipeHandle);
            }
            catch (Exception ex)
            {
                throw new ClientException($"Failed to open a pipe client for the specified LogHandle.", ClientExitCode.InvalidLogPipe, ex);
            }

            // Start reading data from the pipes. We only return from here when the server's pipe closes on us.
            try
            {
                // Ensure everything is properly disposed of.
                using (outputPipeClient) using (inputPipeClient) using (logPipeClient)
                using (BinaryWriter outputWriter = new(outputPipeClient, ServerInstance.DefaultEncoding))
                using (BinaryReader inputReader = new(inputPipeClient, ServerInstance.DefaultEncoding))
                using (BinaryWriter logWriter = new(logPipeClient, ServerInstance.DefaultEncoding))
                using (PipeEncryption ioEncryption = new())
                using (PipeEncryption logEncryption = new())
                {
                    // Perform ECDH key exchange for encrypted communication.
                    try
                    {
                        ioEncryption.PerformClientKeyExchange(outputWriter, inputReader);
                        logEncryption.PerformClientKeyExchange(outputWriter, inputReader);
                    }
                    catch (Exception ex)
                    {
                        throw new ClientException("Failed to establish encrypted communication with the server process.", ClientExitCode.EncryptionError, ex);
                    }

                    // Set up writer helper methods.
                    void WriteSuccess<T>(T result)
                    {
                        ioEncryption.WriteEncrypted(outputWriter, SerializeObject(PipeResponse.Ok(result)));
                    }
                    void WriteError(Exception ex)
                    {
                        ioEncryption.WriteEncrypted(outputWriter, SerializeObject(PipeResponse.Fail(ex)));
                    }
                    void WriteLog(string message, LogSeverity severity, string source)
                    {
                        logEncryption.WriteEncrypted(logWriter, SerializeObject(new LogMessagePayload(message, severity, source)));
                    }

                    // Continuously loop until the end. When we receive null, the server has closed the pipe, so we should break and exit.
                    CloseAppsDialogState? closeAppsDialogState = null;
                    try
                    {
                        while (true)
                        {
                            try
                            {
                                // Read, decrypt, deserialize, then process the request. We never let an exception here kill the pipe.
                                PipeRequest request = DeserializeString<PipeRequest>(ioEncryption.ReadEncrypted(inputReader));
                                try
                                {
                                    switch (request.Command)
                                    {
                                        case PipeCommand.Open:
                                            {
                                                WriteSuccess(true);
                                                break;
                                            }

                                        case PipeCommand.Close:
                                            {
                                                WriteSuccess(true);
                                                Environment.Exit(0);
                                                return (int)ClientExitCode.Success;
                                            }

                                        case PipeCommand.InitCloseAppsDialog:
                                            {
                                                closeAppsDialogState = new(((InitCloseAppsDialogPayload?)request.Payload)?.ProcessDefinitions, WriteLog);
                                                WriteSuccess(true);
                                                break;
                                            }

                                        case PipeCommand.PromptToCloseApps:
                                            {
                                                // If we're here without a RunningProcessService, the InitCloseAppsDialog command was not called properly.
                                                if (closeAppsDialogState?.RunningProcessService is null)
                                                {
                                                    throw new ClientException("The PromptToCloseApps command can only be called when ProcessDefinitions were provided to the InitCloseAppsDialog command.", ClientExitCode.InvalidRequest);
                                                }

                                                // Get all the windows that haven't failed on us and start closing them.
                                                TimeSpan promptToSaveTimeout = ((PromptToCloseAppsPayload)request.Payload!).Timeout; List<nint> failures = []; Process[] runningProcesses;
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
                                                            if (WindowUtilities.GetProcessWindowInfo([process.Id], [window.WindowHandle]).Count == 0)
                                                            {
                                                                closeAppsDialogState.LogAction($"Window [{window.WindowTitle}] for process [{process.ProcessName}] was successfully closed.", LogSeverity.Info);
                                                                break;
                                                            }
                                                            if (promptToCloseStopwatch.Elapsed >= promptToSaveTimeout)
                                                            {
                                                                closeAppsDialogState.LogAction($"Timed out waiting for window [{window.WindowTitle}] for process [{process.ProcessName}] to close.", LogSeverity.Warning);
                                                                break;
                                                            }
                                                            Thread.Sleep(2000);
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
                                                            process.Kill();
                                                            process.WaitForExit();
                                                        }
                                                    }
                                                }
                                                WriteSuccess(true);
                                                break;
                                            }

                                        case PipeCommand.ShowModalDialog:
                                            {
                                                ShowModalDialogPayload payload = (ShowModalDialogPayload)request.Payload!;
                                                WriteSuccess(InvokeModalDialog(payload.DialogType, payload.DialogStyle, payload.Options, closeAppsDialogState));
                                                break;
                                            }

                                        case PipeCommand.ShowProgressDialog:
                                            {
                                                ShowProgressDialogPayload payload = (ShowProgressDialogPayload)request.Payload!;
                                                DialogManager.ShowProgressDialog(payload.DialogStyle, payload.Options);
                                                WriteSuccess(DialogManager.ProgressDialogOpen());
                                                break;
                                            }

                                        case PipeCommand.ProgressDialogOpen:
                                            {
                                                WriteSuccess(DialogManager.ProgressDialogOpen());
                                                break;
                                            }

                                        case PipeCommand.UpdateProgressDialog:
                                            {
                                                UpdateProgressDialogPayload payload = (UpdateProgressDialogPayload)request.Payload!;
                                                DialogManager.UpdateProgressDialog(payload.Message, payload.DetailMessage, payload.Percentage, payload.Alignment);
                                                WriteSuccess(true);
                                                break;
                                            }

                                        case PipeCommand.CloseProgressDialog:
                                            {
                                                DialogManager.CloseProgressDialog();
                                                WriteSuccess(!DialogManager.ProgressDialogOpen());
                                                break;
                                            }

                                        case PipeCommand.ShowBalloonTip:
                                            {
                                                DialogManager.ShowBalloonTip(((ShowBalloonTipPayload)request.Payload!).Options);
                                                WriteSuccess(true);
                                                break;
                                            }

                                        case PipeCommand.MinimizeAllWindows:
                                            {
                                                ShellUtilities.MinimizeAllWindows();
                                                WriteSuccess(true);
                                                break;
                                            }

                                        case PipeCommand.RestoreAllWindows:
                                            {
                                                ShellUtilities.RestoreAllWindows();
                                                WriteSuccess(true);
                                                break;
                                            }

                                        case PipeCommand.SendKeys:
                                            {
                                                WriteSuccess(SendKeys(((SendKeysPayload)request.Payload!).Options));
                                                break;
                                            }

                                        case PipeCommand.GetProcessWindowInfo:
                                            {
                                                WriteSuccess(WindowUtilities.GetProcessWindowInfo(((GetProcessWindowInfoPayload)request.Payload!).Options));
                                                break;
                                            }

                                        case PipeCommand.RefreshDesktopAndEnvironmentVariables:
                                            {
                                                ShellUtilities.RefreshDesktopAndEnvironmentVariables();
                                                WriteSuccess(true);
                                                break;
                                            }

                                        case PipeCommand.GetUserNotificationState:
                                            {
                                                WriteSuccess(ShellUtilities.GetUserNotificationState());
                                                break;
                                            }

                                        case PipeCommand.GetForegroundWindowProcessId:
                                            {
                                                WriteSuccess(ShellUtilities.GetForegroundWindowProcessId());
                                                break;
                                            }

                                        case PipeCommand.GetEnvironmentVariable:
                                            {
                                                WriteSuccess(Environment.GetEnvironmentVariable(((EnvironmentVariablePayload)request.Payload!).Name, EnvironmentVariableTarget.User) ?? ServerInstance.SuccessSentinel);
                                                break;
                                            }

                                        case PipeCommand.SetEnvironmentVariable:
                                            {
                                                EnvironmentVariablePayload payload = (EnvironmentVariablePayload)request.Payload!;
                                                Environment.SetEnvironmentVariable(payload.Name, payload.Value, EnvironmentVariableTarget.User);
                                                WriteSuccess(true);
                                                break;
                                            }

                                        case PipeCommand.RemoveEnvironmentVariable:
                                            {
                                                Environment.SetEnvironmentVariable(((EnvironmentVariablePayload)request.Payload!).Name, null, EnvironmentVariableTarget.User);
                                                WriteSuccess(true);
                                                break;
                                            }

                                        default:
                                            {
                                                throw new ClientException($"The specified command [{request.Command}] is not recognised.", ClientExitCode.InvalidArguments);
                                            }
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
                        closeAppsDialogState?.Dispose();
                    }
                    return (int)ClientExitCode.Success;
                }
            }
            catch (Exception ex)
            {
                throw new ClientException($"Failed to read or write from the pipe.", ClientExitCode.PipeReadWriteError, ex);
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
        /// <exception cref="InvalidOperationException">Thrown if an operation cannot be completed due to the current state, such as attempting to send keys to a
        /// disabled window.</exception>
        /// <exception cref="ClientException">Thrown if required arguments are missing, invalid, or if the specified arguments do not correspond to a
        /// supported operation.</exception>
        private static int EnterStandaloneMode(string[] argv)
        {
            // Parse the arguments and execute the requested operation.
            foreach (string arg in argv)
            {
                if (arg is "/ShowModalDialog" or "/smd")
                {
                    Console.WriteLine(ShowModalDialog(ArgvToDictionary(argv), null, argv));
                    return (int)ClientExitCode.Success;
                }
                else if (arg is "/ShowBalloonTip" or "/sbt")
                {
                    DialogManager.ShowBalloonTip(DeserializeString<BalloonTipOptions>(GetOptionsFromArguments(ArgvToDictionary(argv))));
                    Console.WriteLine(SerializeObject(true));
                    return (int)ClientExitCode.Success;
                }
                else if (arg is "/GetProcessWindowInfo" or "/gpwi")
                {
                    Console.WriteLine(SerializeObject(WindowUtilities.GetProcessWindowInfo(DeserializeString<WindowInfoOptions>(GetOptionsFromArguments(ArgvToDictionary(argv))))));
                    return (int)ClientExitCode.Success;
                }
                else if (arg is "/GetUserNotificationState" or "/guns")
                {
                    Console.WriteLine(SerializeObject(ShellUtilities.GetUserNotificationState()));
                    return (int)ClientExitCode.Success;
                }
                else if (arg is "/GetForegroundWindowProcessId" or "/gfwpi")
                {
                    Console.WriteLine(SerializeObject(ShellUtilities.GetForegroundWindowProcessId()));
                    return (int)ClientExitCode.Success;
                }
                else if (arg is "/RefreshDesktopAndEnvironmentVariables" or "/rdaev")
                {
                    ShellUtilities.RefreshDesktopAndEnvironmentVariables();
                    Console.WriteLine(SerializeObject(true));
                    return (int)ClientExitCode.Success;
                }
                else if (arg is "/MinimizeAllWindows" or "/maw")
                {
                    ShellUtilities.MinimizeAllWindows();
                    Console.WriteLine(SerializeObject(true));
                    return (int)ClientExitCode.Success;
                }
                else if (arg is "/RestoreAllWindows" or "/raw")
                {
                    ShellUtilities.RestoreAllWindows();
                    Console.WriteLine(SerializeObject(true));
                    return (int)ClientExitCode.Success;
                }
                else if (arg is "/SendKeys" or "/sk")
                {
                    Console.WriteLine(SendKeys(DeserializeString<SendKeysOptions>(GetOptionsFromArguments(ArgvToDictionary(argv)))));
                    return (int)ClientExitCode.Success;
                }
                else if (arg is "/GetEnvironmentVariable" or "/gev")
                {
                    if (ArgvToDictionary(argv) is not ReadOnlyDictionary<string, string> arguments || !arguments.TryGetValue("Variable", out string? variable) || string.IsNullOrWhiteSpace(variable))
                    {
                        throw new ClientException("A required Variable was not specified on the command line.", ClientExitCode.InvalidArguments);
                    }
                    Console.WriteLine(SerializeObject(Environment.GetEnvironmentVariable(variable, EnvironmentVariableTarget.User) ?? ServerInstance.SuccessSentinel));
                    return (int)ClientExitCode.Success;
                }
                else if (arg is "/SetEnvironmentVariable" or "/sev")
                {
                    if (ArgvToDictionary(argv) is not ReadOnlyDictionary<string, string> arguments || !arguments.TryGetValue("Variable", out string? variable) || string.IsNullOrWhiteSpace(variable))
                    {
                        throw new ClientException("A required Variable was not specified on the command line.", ClientExitCode.InvalidArguments);
                    }
                    if (!arguments.TryGetValue("Value", out string? value) || string.IsNullOrWhiteSpace(value))
                    {
                        throw new ClientException("A required Value was not specified on the command line.", ClientExitCode.InvalidArguments);
                    }
                    Environment.SetEnvironmentVariable(variable, value, EnvironmentVariableTarget.User);
                    Console.WriteLine(SerializeObject(true));
                    return (int)ClientExitCode.Success;
                }
                else if (arg is "/RemoveEnvironmentVariable" or "/rev")
                {
                    if (!ArgvToDictionary(argv).TryGetValue("Variable", out string? variable) || string.IsNullOrWhiteSpace(variable))
                    {
                        throw new ClientException("A required Variable was not specified on the command line.", ClientExitCode.InvalidArguments);
                    }
                    Environment.SetEnvironmentVariable(variable, null, EnvironmentVariableTarget.User);
                    Console.WriteLine(SerializeObject(true));
                    return (int)ClientExitCode.Success;
                }
                else if (arg is "/SilentRestart" or "/sr")
                {
                    if (!ArgvToDictionary(argv).TryGetValue("Delay", out string? delayArg) || string.IsNullOrWhiteSpace(delayArg) || !int.TryParse(delayArg, out int delayValue))
                    {
                        throw new ClientException("A required Delay was not specified on the command line.", ClientExitCode.InvalidArguments);
                    }
                    Thread.Sleep(delayValue * 1000);
                    DeviceUtilities.RestartComputer();
                    Console.WriteLine(SerializeObject(true));
                    return (int)ClientExitCode.Success;
                }
                else if (arg is "/GetLastInputTime" or "/glit")
                {
                    Console.WriteLine(ShellUtilities.GetLastInputTime().Ticks);
                    return (int)ClientExitCode.Success;
                }
            }
            throw new ClientException("The specified arguments were unable to be resolved into a type of operation.", ClientExitCode.InvalidMode);
        }

        /// <summary>
        /// Displays a modal dialog based on the specified arguments and returns the serialized result.
        /// </summary>
        /// <remarks>This method validates the provided arguments, determines the appropriate dialog type
        /// and style, and displays the dialog using the specified options. The result of the dialog is serialized and
        /// returned to the caller for further processing.</remarks>
        /// <param name="arguments">A read-only dictionary containing the parameters required to configure and display the dialog. The
        /// following keys are expected: <list type="bullet"> <item> <description><c>DialogType</c>: Specifies the type
        /// of dialog to display. Must be a valid <see cref="DialogType"/> value.</description> </item> <item>
        /// <description><c>DialogStyle</c>: Specifies the style of the dialog. Must be a valid <see
        /// cref="DialogStyle"/> value.</description> </item> <item> <description><c>DialogOptions</c>: A
        /// JSON-serialized string containing the options specific to the dialog type.</description> </item> </list></param>
        /// <param name="closeAppsDialogState">An optional <see cref="BaseState"/> object representing the state of a Close Apps dialog, if applicable.</param>
        /// <param name="argv">An optional array of command-line arguments, used for special handling in BlockExecution scenarios.</param>
        /// <returns>A JSON-serialized string representing the result of the dialog. The format and content of the result depend
        /// on the dialog type.</returns>
        /// <exception cref="ClientException">Thrown if any of the following conditions occur: <list type="bullet"> <item><description>The
        /// <c>DialogType</c> key is missing, empty, or invalid.</description></item> <item><description>The
        /// <c>DialogStyle</c> key is missing, empty, or invalid.</description></item> <item><description>The
        /// <c>DialogOptions</c> key is missing, empty, or invalid.</description></item> <item><description>The
        /// specified <c>DialogType</c> is not supported.</description></item> </list></exception>
        private static string ShowModalDialog(ReadOnlyDictionary<string, string> arguments, BaseState? closeAppsDialogState = null, string[]? argv = null)
        {
            // Return early if this is a BlockExecution dialog and we're running as SYSTEM.
            if (arguments.TryGetValue("BlockExecution", out string? blockExecutionArg) && bool.TryParse(blockExecutionArg, out bool blockExecution) && blockExecution && AccountUtilities.CallerIsLocalSystem && argv is not null)
            {
                // Set up the required variables.
                string[] command = [.. argv.SkipWhile(static arg => !File.Exists(arg))]; string filePath = command[0];
                string ifeoPath = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options";
                string fileName = Path.GetFileName(filePath); string ifeoName = Path.GetFileNameWithoutExtension(filePath) + ".ifeo";

                // Rename the IFEO subkey, start the process asynchronously, and then rename it back.
                RegistryUtilities.RenameRegistryKey(ifeoPath, fileName, ifeoName);
                ProcessHandle? handle;
                try
                {
                    handle = ProcessManager.LaunchAsync(new(filePath, command.Length > 1 ? command.Skip(1) : null, Environment.CurrentDirectory));
                }
                finally
                {
                    RegistryUtilities.RenameRegistryKey(ifeoPath, ifeoName, fileName);
                }

                // Exit with the underlying process's exit code if available, otherwise exit with the BlockExecution button text.
                if (handle?.Task.GetAwaiter().GetResult() is ProcessResult result)
                {
                    Environment.Exit(result.ExitCode);
                }
                return SerializeObject(DialogManager.BlockExecutionButtonText);
            }

            // Confirm we have a DialogType and that it's valid.
            if (!arguments.TryGetValue("DialogType", out string? dialogTypeArg) || string.IsNullOrWhiteSpace(dialogTypeArg))
            {
                throw new ClientException("A required DialogType was not specified on the command line.", ClientExitCode.NoDialogType);
            }
            if (!Enum.TryParse(dialogTypeArg, true, out DialogType dialogType))
            {
                throw new ClientException($"The specified DialogType of [{dialogTypeArg}] is invalid.", ClientExitCode.InvalidDialog);
            }

            // Confirm we've got a DialogStyle and that it's valid.
            if (!arguments.TryGetValue("DialogStyle", out string? dialogStyleArg) || string.IsNullOrWhiteSpace(dialogStyleArg))
            {
                throw new ClientException("A required DialogStyle was not specified on the command line.", ClientExitCode.NoDialogStyle);
            }
            if (!Enum.TryParse(dialogStyleArg, true, out DialogStyle dialogStyle))
            {
                throw new ClientException($"The specified DialogStyle of [{dialogStyleArg}] is invalid.", ClientExitCode.NoDialogStyle);
            }

            // Show the dialog and return the serialised result for the caller to handle.
            return SerializeObject(InvokeModalDialog(dialogType, dialogStyle, DeserializeString<object>(GetOptionsFromArguments(arguments)), closeAppsDialogState));
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
        /// the appropriate type for the dialog (for example, <see cref="CloseAppsDialogOptions"/> for <see
        /// cref="DialogType.CloseAppsDialog"/>).</param>
        /// <param name="closeAppsDialogState">An optional state object required when displaying a CloseAppsDialog. Must be of type <see
        /// cref="CloseAppsDialogState"/> if <paramref name="dialogType"/> is <see cref="DialogType.CloseAppsDialog"/>;
        /// otherwise, this parameter is ignored.</param>
        /// <returns>An object representing the result of the dialog interaction. The type and meaning of the return value depend
        /// on the dialog type displayed.</returns>
        /// <exception cref="ClientException">Thrown if an unsupported dialog type is specified, or if <paramref name="dialogType"/> is <see
        /// cref="DialogType.CloseAppsDialog"/> and <paramref name="closeAppsDialogState"/> is not provided.</exception>
        private static object InvokeModalDialog(DialogType dialogType, DialogStyle dialogStyle, object options, BaseState? closeAppsDialogState = null)
        {
            return dialogType switch
            {
                DialogType.CloseAppsDialog => DialogManager.ShowCloseAppsDialog(dialogStyle, (CloseAppsDialogOptions)options, (CloseAppsDialogState?)closeAppsDialogState ?? throw new ClientException("A required CloseAppsDialogState was not provided for the CloseAppsDialog.", ClientExitCode.NoCloseAppsDialogState)),
                DialogType.DialogBox => DialogManager.ShowDialogBox((DialogBoxOptions)options),
                DialogType.HelpConsole => DialogManager.ShowHelpConsole((HelpConsoleOptions)options),
                DialogType.InputDialog => DialogManager.ShowInputDialog(dialogStyle, (InputDialogOptions)options),
                DialogType.CustomDialog => DialogManager.ShowCustomDialog(dialogStyle, (CustomDialogOptions)options),
                DialogType.RestartDialog => DialogManager.ShowRestartDialog(dialogStyle, (RestartDialogOptions)options),
                DialogType.ProgressDialog or _ => throw new ClientException($"The specified DialogType of [{dialogType}] is not supported.", ClientExitCode.UnsupportedDialog)
            };
        }

        /// <summary>
        /// Sends a sequence of keystrokes to the specified window using the provided options.
        /// </summary>
        /// <remarks>This method brings the target window to the foreground before sending the keystrokes.
        /// The keystrokes are sent synchronously and may not be processed if the window is not ready to receive
        /// input.</remarks>
        /// <param name="options">An object that specifies the target window handle and the keys to send. The window must be enabled to
        /// receive input.</param>
        /// <exception cref="ClientException">Thrown if the target window is disabled, such as when a modal dialog is shown.</exception>
        private static bool SendKeys(SendKeysOptions options)
        {
            HWND hwnd = (HWND)options.WindowHandle;
            WindowTools.BringWindowToFront(hwnd);
            if (!User32.IsWindowEnabled(hwnd))
            {
                throw new ClientException("Unable to send keys to window because it may be disabled due to a modal dialog being shown.", ClientExitCode.SendKeysWindowNotEnabled);
            }
            System.Windows.Forms.SendKeys.SendWait(options.Keys);
            return true;
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
                string key = argv[i].Substring(1).Trim();
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
                if (argvDictValue.StartsWith("HKEY", StringComparison.Ordinal))
                {
                    // Provided value is a registry key path.
                    int lastBackslashIndex = argvDictValue.LastIndexOf('\\');
                    string valueName = argvDictValue.Substring(lastBackslashIndex + 1);
                    using RegistryKey registryKey = RegistryUtilities.GetRegistryKeyForPath(argvDictValue.Substring(0, lastBackslashIndex), true);
                    if (registryKey.GetValue(valueName, null) is not string argvDictContent)
                    {
                        throw new ClientException($"The specified ArgumentsDictionary registry key [{argvDictValue}] does not exist or is invalid.", ClientExitCode.InvalidArguments);
                    }
                    if (arguments.TryGetValue("RemoveArgumentsDictionaryStorage", out string? removeStorage) && int.Parse(removeStorage, CultureInfo.InvariantCulture) > 0)
                    {
                        registryKey.DeleteValue(valueName);
                    }
                    return DeserializeString<ReadOnlyDictionary<string, string>>(argvDictContent);
                }
                else if (File.Exists(argvDictValue))
                {
                    // Provided value is a file path.
                    string argvDictContent = File.ReadAllText(argvDictValue);
                    if (arguments.TryGetValue("RemoveArgumentsDictionaryStorage", out string? removeStorage) && int.Parse(removeStorage, CultureInfo.InvariantCulture) > 0)
                    {
                        File.Delete(argvDictValue);
                    }
                    return DeserializeString<ReadOnlyDictionary<string, string>>(argvDictContent);
                }
                else
                {
                    // Assume anything else is a literal Base64-encoded string.
                    return DeserializeString<ReadOnlyDictionary<string, string>>(argvDictValue);
                }
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Enforcing this rule just makes a mess.")]
        private static string GetOptionsFromArguments(ReadOnlyDictionary<string, string> arguments)
        {
            // Confirm we have options and they're not null/invalid.
            if (!arguments.TryGetValue("Options", out string? options))
            {
                throw new ClientException("The required options were not specified on the command line.", ClientExitCode.NoOptions);
            }
            if (string.IsNullOrWhiteSpace(options))
            {
                throw new ClientException($"The specified options are null or invalid.", ClientExitCode.InvalidOptions);
            }
            return options;
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
            catch (Exception ex)
            {
                throw new ClientException($"An error occurred while deserializing the provided input.", ClientExitCode.InvalidOptions, ex);
            }
        }

        /// <summary>
        /// Serializes the specified object into a string representation.
        /// </summary>
        /// <typeparam name="T">The type of the object to serialize.</typeparam>
        /// <param name="result">The object to be serialized. Cannot be null.</param>
        /// <returns>A string representation of the serialized object.</returns>
        /// <exception cref="ClientException">Thrown if an error occurs during serialization. The exception includes details about the failure.</exception>
        private static string SerializeObject<T>(T result)
        {
            try
            {
                return DataSerialization.SerializeToString(result);
            }
            catch (Exception ex)
            {
                throw new ClientException($"An error occurred while serializing the provided result.", ClientExitCode.InvalidResult, ex);
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
            if (ProcessUtilities.GetParentProcess().ProcessName.Equals(Path.GetFileNameWithoutExtension(typeof(ClientExecutable).Assembly.Location) + ".Launcher", StringComparison.OrdinalIgnoreCase))
            {
                Environment.FailFast($"{message.TrimEnd('.')}.\nException Info: {exception}", exception);
            }
            try
            {
                Console.Error.WriteLine(DataSerialization.SerializeToString(exception));
            }
            catch (Exception ex) when (ex.Message is not null)
            {
                Environment.FailFast($"An unexpected exception occurred while serializing main exception [{ex}].\nException Info: {exception}", exception);
            }
            return (int?)exitCode ?? exception.HResult;
        }
    }
}
