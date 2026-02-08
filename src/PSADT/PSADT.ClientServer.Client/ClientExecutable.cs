using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Threading;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using PSADT.AccountManagement;
using PSADT.ClientServer.Payloads;
using PSADT.DeviceManagement;
using PSADT.LibraryInterfaces;
using PSADT.ProcessManagement;
using PSADT.Security;
using PSADT.Types;
using PSADT.UserInterface;
using PSADT.UserInterface.DialogOptions;
using PSADT.UserInterface.DialogState;
using PSADT.Utilities;
using PSADT.WindowManagement;
using PSAppDeployToolkit.Logging;
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
                using (ClientPipeEncryption ioEncryption = new())
                using (ClientPipeEncryption logEncryption = new())
                {
                    // Perform ECDH key exchange for encrypted communication.
                    try
                    {
                        ioEncryption.PerformKeyExchange(outputPipeClient, inputPipeClient);
                        logEncryption.PerformKeyExchange(outputPipeClient, inputPipeClient);
                    }
                    catch (Exception ex)
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
                                PipeCommand command = (PipeCommand)requestBytes[0]; int payloadOffset = 1;
                                try
                                {
                                    switch (command)
                                    {
                                        case PipeCommand.Open:
                                            {
                                                WriteSuccess(true);
                                                break;
                                            }

                                        case PipeCommand.Close:
                                            {
                                                WriteSuccess(true);
                                                return (int)ClientExitCode.Success;
                                            }

                                        case PipeCommand.InitCloseAppsDialog:
                                            {
                                                closeAppsDialogState = new(DeserializeBytes<InitCloseAppsDialogPayload>(requestBytes, payloadOffset).ProcessDefinitions, WriteLog);
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
                                                ShowModalDialogPayload payload = DeserializeBytes<ShowModalDialogPayload>(requestBytes, payloadOffset);
                                                WriteSuccess(InvokeModalDialog(payload.DialogType, payload.DialogStyle, payload.Options, closeAppsDialogState));
                                                break;
                                            }

                                        case PipeCommand.ShowProgressDialog:
                                            {
                                                ShowProgressDialogPayload payload = DeserializeBytes<ShowProgressDialogPayload>(requestBytes, payloadOffset);
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
                                                UpdateProgressDialogPayload payload = DeserializeBytes<UpdateProgressDialogPayload>(requestBytes, payloadOffset);
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
                                                DialogManager.ShowBalloonTip(DeserializeBytes<ShowBalloonTipPayload>(requestBytes, payloadOffset).Options);
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
                                                WriteSuccess(SendKeys(DeserializeBytes<SendKeysPayload>(requestBytes, payloadOffset).Options));
                                                break;
                                            }

                                        case PipeCommand.GetProcessWindowInfo:
                                            {
                                                WriteSuccess(WindowUtilities.GetProcessWindowInfo(DeserializeBytes<GetProcessWindowInfoPayload>(requestBytes, payloadOffset).Options));
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
                                                WriteSuccess(EnvironmentUtilities.GetEnvironmentVariable(DeserializeBytes<EnvironmentVariablePayload>(requestBytes, payloadOffset).Name, EnvironmentVariableTarget.User) ?? ServerInstance.SuccessSentinel);
                                                break;
                                            }

                                        case PipeCommand.SetEnvironmentVariable:
                                            {
                                                EnvironmentVariablePayload payload = DeserializeBytes<EnvironmentVariablePayload>(requestBytes, payloadOffset);
                                                EnvironmentUtilities.SetEnvironmentVariable(payload.Name, payload.Value, EnvironmentVariableTarget.User, payload.Expandable, payload.Append, payload.Remove);
                                                WriteSuccess(true);
                                                break;
                                            }

                                        case PipeCommand.RemoveEnvironmentVariable:
                                            {
                                                EnvironmentUtilities.RemoveEnvironmentVariable(DeserializeBytes<EnvironmentVariablePayload>(requestBytes, payloadOffset).Name, EnvironmentVariableTarget.User);
                                                WriteSuccess(true);
                                                break;
                                            }

                                        case PipeCommand.GroupPolicyUpdate:
                                            {
                                                WriteSuccess(GroupPolicyUpdate(DeserializeBytes<GroupPolicyUpdatePayload>(requestBytes, payloadOffset).Force));
                                                break;
                                            }

                                        default:
                                            {
                                                throw new ClientException($"The specified command [{command}] is not recognised.", ClientExitCode.InvalidArguments);
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
                    Console.WriteLine(SerializeToString(true));
                    return (int)ClientExitCode.Success;
                }
                else if (arg is "/GetProcessWindowInfo" or "/gpwi")
                {
                    Console.WriteLine(SerializeToString(WindowUtilities.GetProcessWindowInfo(DeserializeString<WindowInfoOptions>(GetOptionsFromArguments(ArgvToDictionary(argv))))));
                    return (int)ClientExitCode.Success;
                }
                else if (arg is "/GetUserNotificationState" or "/guns")
                {
                    Console.WriteLine(SerializeToString(ShellUtilities.GetUserNotificationState()));
                    return (int)ClientExitCode.Success;
                }
                else if (arg is "/GetForegroundWindowProcessId" or "/gfwpi")
                {
                    Console.WriteLine(SerializeToString(ShellUtilities.GetForegroundWindowProcessId()));
                    return (int)ClientExitCode.Success;
                }
                else if (arg is "/RefreshDesktopAndEnvironmentVariables" or "/rdaev")
                {
                    ShellUtilities.RefreshDesktopAndEnvironmentVariables();
                    Console.WriteLine(SerializeToString(true));
                    return (int)ClientExitCode.Success;
                }
                else if (arg is "/MinimizeAllWindows" or "/maw")
                {
                    ShellUtilities.MinimizeAllWindows();
                    Console.WriteLine(SerializeToString(true));
                    return (int)ClientExitCode.Success;
                }
                else if (arg is "/RestoreAllWindows" or "/raw")
                {
                    ShellUtilities.RestoreAllWindows();
                    Console.WriteLine(SerializeToString(true));
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
                    Console.WriteLine(SerializeToString(EnvironmentUtilities.GetEnvironmentVariable(variable, EnvironmentVariableTarget.User) ?? ServerInstance.SuccessSentinel));
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
                    Console.WriteLine(SerializeToString(true));
                    return (int)ClientExitCode.Success;
                }
                else if (arg is "/RemoveEnvironmentVariable" or "/rev")
                {
                    if (!ArgvToDictionary(argv).TryGetValue("Variable", out string? variable) || string.IsNullOrWhiteSpace(variable))
                    {
                        throw new ClientException("A required Variable was not specified on the command line.", ClientExitCode.InvalidArguments);
                    }
                    EnvironmentUtilities.RemoveEnvironmentVariable(variable, EnvironmentVariableTarget.User);
                    Console.WriteLine(SerializeToString(true));
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
                    Console.WriteLine(SerializeToString(true));
                    return (int)ClientExitCode.Success;
                }
                else if (arg is "/GetLastInputTime" or "/glit")
                {
                    Console.WriteLine(ShellUtilities.GetLastInputTime().Ticks);
                    return (int)ClientExitCode.Success;
                }
                else if (arg is "/TokenBroker" or "/tb")
                {
                    BrokerTokenForCaller(ArgvToDictionary(argv));
                    return (int)ClientExitCode.Success;
                }
                else if (arg is "/GroupPolicyUpdate" or "/gpu")
                {
                    if (ArgvToDictionary(argv) is not ReadOnlyDictionary<string, string> arguments || !arguments.TryGetValue("Force", out string? forceStr) || string.IsNullOrWhiteSpace(forceStr) || !bool.TryParse(forceStr, out bool force))
                    {
                        throw new ClientException("The 'Sync' argument is required and cannot be null or whitespace.", ClientExitCode.InvalidArguments);
                    }
                    ClientServerUtilities.SetClientServerOperationSuccess();
                    Console.WriteLine(SerializeToString(GroupPolicyUpdate(force)));
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
        /// <param name="closeAppsDialogState">An optional <see cref="BaseDialogState"/> object representing the state of a Close Apps dialog, if applicable.</param>
        /// <param name="argv">An optional array of command-line arguments, used for special handling in BlockExecution scenarios.</param>
        /// <returns>A JSON-serialized string representing the result of the dialog. The format and content of the result depend
        /// on the dialog type.</returns>
        /// <exception cref="ClientException">Thrown if any of the following conditions occur: <list type="bullet"> <item><description>The
        /// <c>DialogType</c> key is missing, empty, or invalid.</description></item> <item><description>The
        /// <c>DialogStyle</c> key is missing, empty, or invalid.</description></item> <item><description>The
        /// <c>DialogOptions</c> key is missing, empty, or invalid.</description></item> <item><description>The
        /// specified <c>DialogType</c> is not supported.</description></item> </list></exception>
        private static string ShowModalDialog(ReadOnlyDictionary<string, string> arguments, BaseDialogState? closeAppsDialogState = null, string[]? argv = null)
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
                if (handle?.Task.GetAwaiter().GetResult().ExitCode is int exitCode)
                {
                    Environment.Exit(exitCode);
                }
                return SerializeToString(BlockExecution.ButtonText);
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

            // Deserialize the options to the correct type based on DialogType and show the dialog.
            IDialogOptions options = dialogType switch
            {
                DialogType.CloseAppsDialog => DataSerialization.DeserializeFromString<CloseAppsDialogOptions>(GetOptionsFromArguments(arguments)),
                DialogType.CustomDialog => DataSerialization.DeserializeFromString<CustomDialogOptions>(GetOptionsFromArguments(arguments)),
                DialogType.DialogBox => DataSerialization.DeserializeFromString<DialogBoxOptions>(GetOptionsFromArguments(arguments)),
                DialogType.HelpConsole => DataSerialization.DeserializeFromString<HelpConsoleOptions>(GetOptionsFromArguments(arguments)),
                DialogType.InputDialog => DataSerialization.DeserializeFromString<InputDialogOptions>(GetOptionsFromArguments(arguments)),
                DialogType.RestartDialog => DataSerialization.DeserializeFromString<RestartDialogOptions>(GetOptionsFromArguments(arguments)),
                DialogType.ProgressDialog or _ => throw new ClientException($"The specified DialogType of [{dialogType}] is not supported for deserialization.", ClientExitCode.UnsupportedDialog)
            };
            return SerializeToString(InvokeModalDialog(dialogType, dialogStyle, options, closeAppsDialogState));
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
        private static object InvokeModalDialog(DialogType dialogType, DialogStyle dialogStyle, IDialogOptions options, BaseDialogState? closeAppsDialogState = null)
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
        private static void BrokerTokenForCaller(ReadOnlyDictionary<string, string> arguments)
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
            if (!arguments.TryGetValue("UseLinkedAdminToken", out string? useLinkedAdminTokenStr) || string.IsNullOrWhiteSpace(useLinkedAdminTokenStr) || !bool.TryParse(useLinkedAdminTokenStr, out bool useLinkedAdminToken))
            {
                throw new ClientException("The 'UseLinkedAdminTokenStr' argument is required and cannot be null or whitespace.", ClientExitCode.InvalidArguments);
            }
            if (!arguments.TryGetValue("UseHighestAvailableToken", out string? useHighestAvailableTokenStr) || string.IsNullOrWhiteSpace(useHighestAvailableTokenStr) || !bool.TryParse(useHighestAvailableTokenStr, out bool useHighestAvailableToken))
            {
                throw new ClientException("The 'UseHighestAvailableTokenStr' argument is required and cannot be null or whitespace.", ClientExitCode.InvalidArguments);
            }

            // Confirm the session Id is greater than 0; we never want to broker SYSTEM tokens.
            if (sessionId == 0)
            {
                throw new ClientException("Brokering of the Local System session token is not permitted.", ClientExitCode.InvalidArguments);
            }

            // Connect to the named pipe server.
            using NamedPipeClientStream pipe = new(".", pipeName, PipeDirection.InOut, PipeOptions.None);
            pipe.Connect();

            // Get the user's token from the WTS subsystem.
            _ = WtsApi32.WTSQueryUserToken(sessionId, out SafeFileHandle hUserToken);
            SafeFileHandle hPrimaryToken;
            using (hUserToken)
            {
                if (useLinkedAdminToken || useHighestAvailableToken)
                {
                    try
                    {
                        hPrimaryToken = TokenManager.GetLinkedPrimaryToken(hUserToken);
                    }
                    catch (Exception ex)
                    {
                        if (!useHighestAvailableToken)
                        {
                            throw new ClientException("Failed to get linked admin token.", ClientExitCode.LinkedAdminTokenFailure, ex);
                        }
                        hPrimaryToken = TokenManager.GetPrimaryToken(hUserToken);
                    }
                }
                else
                {
                    hPrimaryToken = TokenManager.GetPrimaryToken(hUserToken);
                }
            }

            // Duplicate the token to the specified process ID.
            SafeFileHandle hDupToken;
            using (SafeFileHandle hSourceProcess = Kernel32.OpenProcess(PROCESS_ACCESS_RIGHTS.PROCESS_DUP_HANDLE, false, processId))
            using (SafeProcessHandle hCurrentProcess = Kernel32.GetCurrentProcess())
            using (hPrimaryToken)
            {
                _ = Kernel32.DuplicateHandle(hCurrentProcess, hPrimaryToken, hSourceProcess, out hDupToken, 0, false, DUPLICATE_HANDLE_OPTIONS.DUPLICATE_SAME_ACCESS);
            }

            // Write the duplicated token to the pipe.
            using (hDupToken)
            {
                if (IntPtr.Size == 8)
                {
                    pipe.WriteByte(8); pipe.Write(BitConverter.GetBytes(hDupToken.DangerousGetHandle().ToInt64()), 0, 8);
                }
                else
                {
                    pipe.WriteByte(4); pipe.Write(BitConverter.GetBytes(hDupToken.DangerousGetHandle().ToInt32()), 0, 4);
                }
            }
            pipe.Flush(); pipe.WaitForPipeDrain();
        }

        /// <summary>
        /// Runs a Group Policy update on the local machine by invoking the gpupdate utility.
        /// </summary>
        /// <param name="force">A value indicating whether to force the update, reapplying all policy settings even if they have not
        /// changed. If <see langword="true"/>, all settings are reapplied.</param>
        /// <returns>A <see cref="ProcessResult"/> object that contains the results of the Group Policy update operation.</returns>
        internal static ProcessResult GroupPolicyUpdate(bool force)
        {
            // Build out argument list for gpupdate.exe.
            List<string> argumentList = ["/Target:User"];
            if (force)
            {
                argumentList.Add("/Force");
            }

            // Set up the process and return its result.
            ProcessLaunchInfo launchInfo = new(
                Path.Combine(Environment.SystemDirectory, "gpupdate.exe"),
                argumentList,
                standardInput: ["N"],
                createNoWindow: true
            );
            return ProcessManager.LaunchAsync(launchInfo)!.Task.GetAwaiter().GetResult();
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
                    if (arguments.TryGetValue("RemoveArgumentsDictionaryStorage", out string? removeStorage) && bool.Parse(removeStorage))
                    {
                        registryKey.DeleteValue(valueName);
                    }
                    return DeserializeString<ReadOnlyDictionary<string, string>>(argvDictContent);
                }
                else if (File.Exists(argvDictValue))
                {
                    // Provided value is a file path.
                    string argvDictContent = File.ReadAllText(argvDictValue);
                    if (arguments.TryGetValue("RemoveArgumentsDictionaryStorage", out string? removeStorage) && bool.Parse(removeStorage))
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
            catch (Exception ex)
            {
                throw new ClientException($"An error occurred while deserializing the provided input.", ClientExitCode.InvalidOptions, ex);
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
            catch (Exception ex)
            {
                throw new ClientException($"An error occurred while deserializing the provided input.", ClientExitCode.InvalidOptions, ex);
            }
        }

        /// <summary>
        /// Serializes the specified object into a UTF-8 encoded byte array.
        /// </summary>
        /// <typeparam name="T">The type of the object to serialize.</typeparam>
        /// <param name="result">The object to be serialized. Cannot be null.</param>
        /// <returns>A UTF-8 encoded byte array representation of the serialized object.</returns>
        /// <exception cref="ClientException">Thrown if an error occurs during serialization. The exception includes details about the failure.</exception>
        private static byte[] SerializeToBytes<T>(T result)
        {
            try
            {
                return DataSerialization.SerializeToBytes(result);
            }
            catch (Exception ex)
            {
                throw new ClientException($"An error occurred while serializing the provided result.", ClientExitCode.InvalidResult, ex);
            }
        }

        /// <summary>
        /// Serializes the specified object into a string representation.
        /// </summary>
        /// <typeparam name="T">The type of the object to serialize.</typeparam>
        /// <param name="result">The object to be serialized. Cannot be null.</param>
        /// <returns>A string representation of the serialized object.</returns>
        /// <exception cref="ClientException">Thrown if an error occurs during serialization. The exception includes details about the failure.</exception>
        private static string SerializeToString<T>(T result)
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
