using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using PSADT.ClientServer.Payloads;
using PSADT.Foundation;
using PSADT.Interop;
using PSADT.ProcessManagement;
using PSADT.UserInterface;
using PSADT.UserInterface.DialogOptions;
using PSADT.UserInterface.DialogResults;
using PSADT.WindowManagement;
using PSAppDeployToolkit.Foundation;

namespace PSADT.ClientServer
{
    /// <summary>
    /// Provides functionality for inter-process communication between a server and a client using anonymous pipes.
    /// </summary>
    /// <remarks>The <see cref="ServerInstance"/> class facilitates communication between a server and a client
    /// process through anonymous pipes. It manages the lifecycle of the client process, handles input and output
    /// streams, and provides methods to send commands and retrieve responses. This class implements <see
    /// cref="IAsyncDisposable"/> to ensure proper cleanup of resources. <para> Typical usage involves creating an instance
    /// of <see cref="ServerInstance"/>, calling <see cref="OpenAsync"/> to start the client-server communication, and
    /// using a number of predefined methods to send commands to the client. Once the communication is
    /// complete, the <see cref="DisposeAsync()"/> method should be called to release resources. </para></remarks>
    public sealed class ServerInstance : IAsyncDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServerInstance"/> class, setting up inter-process communication
        /// infrastructure using anonymous pipes.
        /// </summary>
        /// <param name="runAsActiveUser">Specifies whether the server instance should run as the active user.</param>
        /// <remarks>This constructor creates the instance with the specified user session information.
        /// All communication infrastructure (pipes, encryption, cancellation tokens) is initialized inline.
        /// Call <see cref="OpenAsync"/> to start the client process and begin communication.</remarks>
        public ServerInstance(RunAsActiveUser runAsActiveUser)
        {
            ArgumentNullException.ThrowIfNull(runAsActiveUser);
            RunAsActiveUser = runAsActiveUser;
        }

        /// <summary>
        /// Opens the client-server communication by starting the client process and initializing the connection.
        /// </summary>
        /// <remarks>This method launches the client process, performs key exchange for encrypted communication,
        /// and ensures that the client process is ready to receive commands before returning.</remarks>
        /// <exception cref="InvalidOperationException">Thrown if the server instance already has an associated client process.</exception>
        /// <exception cref="InvalidProgramException">Thrown if the opened client process returns an invalid response.</exception>
        /// <exception cref="ServerException">Thrown if the client process fails to respond to the initial command.</exception>
        public async ValueTask OpenAsync()
        {
            // Internal task method to handle log messages from the client process asynchronously.
            async Task ReadLogAsync()
            {
                // Read the log stream until cancellation is requested or the end of the stream is reached.
                ObjectDisposedException.ThrowIf(_disposed, this);
                while (!_logWriterTaskCts.IsCancellationRequested)
                {
                    try
                    {
                        // Read and decrypt the log message, then process it if a deployment session is active.
                        // We must read it before if there's a deployment session active to clear the queue.
                        if (await _logEncryption.ReadEncryptedAsync(_logServer).ConfigureAwait(false) is { Length: > 0 } decrypted && ModuleDatabase.IsDeploymentSessionActive())
                        {
                            // Deserialize the log message DTO.
                            LogMessagePayload logMessage = DataSerialization.DeserializeFromBytes<LogMessagePayload>(decrypted);
                            ModuleDatabase.GetDeploymentSession().WriteLogEntry(logMessage.Message.Trim(), logMessage.Severity, logMessage.Source);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // The log writer task was cancelled, exit the loop.
                        break;
                    }
                    catch (EndOfStreamException)
                    {
                        // The log writer task reached the end of the stream, exit the loop.
                        break;
                    }
                    catch (Exception ex)
                    {
                        // Some kind of read issue occurred that was unexpected.
                        throw new ServerException("An error occurred while reading from the log stream.", ex);
                    }
                }
            }

            // Don't re-open if there's already a client process associated with this instance.
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_clientProcess is not null)
            {
                throw new InvalidOperationException("The server instance already has an associated client process.");
            }

            // Start the client process using the pipe handles.
            try
            {
                nint outputServerClientSafePipeHandle = _outputServer.ClientSafePipeHandle.DangerousGetHandle();
                nint inputServerClientSafePipeHandle = _inputServer.ClientSafePipeHandle.DangerousGetHandle();
                nint logServerClientSafePipeHandle = _logServer.ClientSafePipeHandle.DangerousGetHandle();
                _clientProcess = ClientServerUtilities.StartClientOperationAsync(
                    ["/ClientServer", "-InputPipe", $"{((long)outputServerClientSafePipeHandle).ToString(CultureInfo.InvariantCulture)}", "-OutputPipe", $"{((long)inputServerClientSafePipeHandle).ToString(CultureInfo.InvariantCulture)}", "-LogPipe", $"{((long)logServerClientSafePipeHandle).ToString(CultureInfo.InvariantCulture)}"],
                    RunAsActiveUser,
                    [outputServerClientSafePipeHandle, inputServerClientSafePipeHandle, logServerClientSafePipeHandle],
                    _clientProcessCts.Token
                );
            }
            finally
            {
                _outputServer.DisposeLocalCopyOfClientHandle();
                _inputServer.DisposeLocalCopyOfClientHandle();
                _logServer.DisposeLocalCopyOfClientHandle();
            }

            // Perform key exchange for encrypted communication.
            await _ioEncryption.PerformKeyExchangeAsync(_outputServer, _inputServer).ConfigureAwait(false);
            await _logEncryption.PerformKeyExchangeAsync(_outputServer, _inputServer).ConfigureAwait(false);

            // Confirm the client starts and is ready to receive commands.
            bool opened = false;
            try
            {
                if (!(opened = await InvokeAsync<bool>(PipeCommand.Open).ConfigureAwait(false)))
                {
                    throw new InvalidProgramException("The opened client process returned an invalid response.");
                }
            }
            catch (Exception ex)
            {
                throw new ServerException("The opened client process is not properly responding to commands.", ex, _clientProcess);
            }
            finally
            {
                if (!opened)
                {
                    await using (this.ConfigureAwait(false))
                    {
                        _clientProcess = null;
                    }
                }
            }

            // Ensure this instance is disposed on process exit.
            AppDomain.CurrentDomain.ProcessExit += ProcessExit_Handler;

            // Set up the log writer task to run in the background.
            _logWriterTask = ReadLogAsync();
        }

        /// <summary>
        /// Initializes a dialog to close specified applications.
        /// </summary>
        /// <remarks>This method invokes the dialog initialization process, optionally including
        /// serialized information about the processes to be closed. Ensure that the <paramref name="closeProcesses"/>
        /// parameter is correctly populated if specific processes need to be targeted.</remarks>
        /// <param name="closeProcesses">An array of <see cref="ProcessDefinition"/> objects representing the processes to be closed. If <paramref
        /// name="closeProcesses"/> is <see langword="null"/>, no specific processes will be targeted.</param>
        /// <returns><see langword="true"/> if the dialog was successfully initialized; otherwise, <see langword="false"/>.</returns>
        public ValueTask<bool> InitCloseAppsDialogAsync(ReadOnlyCollection<ProcessDefinition>? closeProcesses)
        {
            return InvokeAsync<InitCloseAppsDialogPayload, bool>(PipeCommand.InitCloseAppsDialog, new(closeProcesses));
        }

        /// <summary>
        /// Prompts the user to close any running applications that may interfere with the installation process.
        /// </summary>
        /// <param name="promptToCloseTimeout">The timeout duration for the prompt to close applications.</param>
        /// <remarks>This method invokes a prompt to the user and returns their response. Ensure that the
        /// environment allows user interaction before calling this method.</remarks>
        /// <returns><see langword="true"/> if the user agrees to close the applications; otherwise, <see langword="false"/>.</returns>
        public ValueTask<bool> PromptToCloseAppsAsync(TimeSpan promptToCloseTimeout)
        {
            return InvokeAsync<PromptToCloseAppsPayload, bool>(PipeCommand.PromptToCloseApps, new(promptToCloseTimeout));
        }

        /// <summary>
        /// Displays a modal dialog prompting the user to close applications.
        /// </summary>
        /// <remarks>Use this method to prompt the user to close specific applications before proceeding
        /// with an operation. The dialog's behavior and appearance can be customized using the <paramref
        /// name="dialogStyle"/> and <paramref name="options"/> parameters.</remarks>
        /// <param name="dialogStyle">The style of the dialog, which determines its appearance and behavior.</param>
        /// <param name="options">The options for configuring the dialog, such as the list of applications to close and additional settings.</param>
        /// <returns>A <see cref="CloseAppsDialogResult"/> indicating the user's response to the dialog. The result may include
        /// information about whether the user chose to close the applications or canceled the operation.</returns>
        public ValueTask<CloseAppsDialogResult> ShowCloseAppsDialogAsync(DialogStyle dialogStyle, CloseAppsDialogOptions options)
        {
            return ShowModalDialogAsync<CloseAppsDialogResult>(DialogType.CloseAppsDialog, dialogStyle, options);
        }

        /// <summary>
        /// Displays a custom dialog with the specified style and options, and returns the result as a string.
        /// </summary>
        /// <remarks>Use this method to display a modal custom dialog to the user. The dialog's behavior
        /// and appearance are determined by the provided <paramref name="dialogStyle"/> and <paramref
        /// name="options"/>.</remarks>
        /// <param name="dialogStyle">The style of the dialog, which determines its appearance and behavior.</param>
        /// <param name="options">The options to configure the dialog, such as title, message, and input settings.</param>
        /// <returns>A string representing the result of the dialog interaction. The value depends on the dialog's configuration and user input.</returns>
        public ValueTask<CustomDialogResult> ShowCustomDialogAsync(DialogStyle dialogStyle, CustomDialogOptions options)
        {
            return ShowModalDialogAsync<CustomDialogResult>(DialogType.CustomDialog, dialogStyle, options);
        }

        /// <summary>
        /// Displays a list selection dialog to the user and returns the result of the interaction.
        /// </summary>
        /// <remarks>Use this method to present a modal list selection dialog to the user. The dialog's behavior
        /// and appearance can be customized using the <paramref name="dialogStyle"/> and <paramref name="options"/>
        /// parameters.</remarks>
        /// <param name="dialogStyle">The style of the dialog, which determines its appearance and behavior.</param>
        /// <param name="options">The options to configure the list selection dialog, such as the message, buttons, and list items.</param>
        /// <returns>A <see cref="ListSelectionDialogResult"/> object containing the button clicked and the selected list item.</returns>
        public ValueTask<ListSelectionDialogResult> ShowListSelectionDialogAsync(DialogStyle dialogStyle, ListSelectionDialogOptions options)
        {
            return ShowModalDialogAsync<ListSelectionDialogResult>(DialogType.ListSelectionDialog, dialogStyle, options);
        }

        /// <summary>
        /// Displays an input dialog to the user and returns the result of the interaction.
        /// </summary>
        /// <remarks>Use this method to present a modal input dialog to the user. The dialog's behavior
        /// and appearance can be customized using the <paramref name="dialogStyle"/> and <paramref name="options"/>
        /// parameters.</remarks>
        /// <param name="dialogStyle">The style of the dialog, which determines its appearance and behavior.</param>
        /// <param name="options">The options to configure the input dialog, such as the prompt text, default value, and validation rules.</param>
        /// <returns>An <see cref="InputDialogResult"/> object containing the user's input and the dialog's outcome.</returns>
        public ValueTask<InputDialogResult> ShowInputDialogAsync(DialogStyle dialogStyle, InputDialogOptions options)
        {
            return ShowModalDialogAsync<InputDialogResult>(DialogType.InputDialog, dialogStyle, options);
        }

        /// <summary>
        /// Displays a restart dialog to the user and returns the user's input as a string.
        /// </summary>
        /// <remarks>This method displays a modal dialog of type <see cref="DialogType.RestartDialog"/> and
        /// blocks execution until the user provides input or dismisses the dialog. The returned value depends on the
        /// specific implementation of the dialog and the user's interaction.</remarks>
        /// <param name="dialogStyle">The style of the dialog, which determines its appearance and behavior.</param>
        /// <param name="options">The options to configure the restart dialog, such as title, message, and default values.</param>
        /// <returns>A string representing the user's input from the dialog. The value may vary depending on the dialog
        /// configuration and user interaction.</returns>
        public ValueTask<string> ShowRestartDialogAsync(DialogStyle dialogStyle, RestartDialogOptions options)
        {
            return ShowModalDialogAsync<string>(DialogType.RestartDialog, dialogStyle, options);
        }

        /// <summary>
        /// Displays a modal dialog box with the specified options, and returns the result of the dialog interaction.
        /// </summary>
        /// <remarks>Use this method to display a modal dialog box that requires user input or
        /// confirmation. The dialog box will block the calling thread until the user closes it, and the result will
        /// indicate the user's action (e.g., OK, Cancel).</remarks>
        /// <param name="options">The options to configure the dialog box, such as title, message, and input fields.</param>
        /// <returns>A <see cref="DialogBoxResult"/> that represents the result of the user's interaction with the dialog box.</returns>
        public ValueTask<DialogBoxResult> ShowDialogBoxAsync(DialogBoxOptions options)
        {
            return ShowModalDialogAsync<DialogBoxResult>(DialogType.DialogBox, DialogStyle.Classic, options);
        }

        /// <summary>
        /// Displays a progress dialog with the specified style and options.
        /// </summary>
        /// <remarks>Use this method to display a progress dialog to inform users about ongoing
        /// operations. The dialog's appearance and behavior are determined by the provided <paramref
        /// name="dialogStyle"/> and <paramref name="options"/>.</remarks>
        /// <param name="dialogStyle">The style of the dialog to display. This determines the appearance and behavior of the progress dialog.</param>
        /// <param name="options">The configuration options for the progress dialog, such as title, message, and progress behavior. Cannot be
        /// null.</param>
        /// <returns><see langword="true"/> if the progress dialog was successfully displayed; otherwise, <see
        /// langword="false"/>.</returns>
        public ValueTask<bool> ShowProgressDialogAsync(DialogStyle dialogStyle, ProgressDialogOptions options)
        {
            return InvokeAsync<ShowProgressDialogPayload, bool>(PipeCommand.ShowProgressDialog, new(dialogStyle, options));
        }

        /// <summary>
        /// Determines whether the progress dialog is currently open.
        /// </summary>
        /// <remarks>This method checks the state of the progress dialog and returns a boolean value
        /// indicating whether it is currently displayed to the user.</remarks>
        /// <returns><see langword="true"/> if the progress dialog is open; otherwise, <see langword="false"/>.</returns>
        public ValueTask<bool> ProgressDialogOpenAsync()
        {
            return InvokeAsync<bool>(PipeCommand.ProgressDialogOpen);
        }

        /// <summary>
        /// Updates the progress dialog with the specified message, detail message, progress percentage, and message
        /// alignment.
        /// </summary>
        /// <remarks>This method allows updating various aspects of a progress dialog, including the main
        /// message, detailed message, progress percentage, and message alignment. Any parameter can be omitted by
        /// passing <see langword="null"/> to retain the current state of that aspect.</remarks>
        /// <param name="progressMessage">The main progress message to display. If <see langword="null"/> or whitespace, no message will be displayed.</param>
        /// <param name="progressDetailMessage">The detailed progress message to display. If <see langword="null"/> or whitespace, no detail message will be
        /// displayed.</param>
        /// <param name="progressPercentage">The progress percentage to display, as a value between 0 and 100. If <see langword="null"/>, no percentage
        /// will be displayed.</param>
        /// <param name="messageAlignment">The alignment of the progress messages within the dialog. If <see langword="null"/>, the default alignment
        /// will be used.</param>
        /// <returns><see langword="true"/> if the progress dialog was successfully updated; otherwise, <see langword="false"/>.</returns>
        public ValueTask<bool> UpdateProgressDialogAsync(string? progressMessage = null, string? progressDetailMessage = null, double? progressPercentage = null, DialogMessageAlignment? messageAlignment = null)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (progressMessage is not null)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(progressMessage);
            }
            if (progressDetailMessage is not null)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(progressDetailMessage);
            }
            return InvokeAsync<UpdateProgressDialogPayload, bool>(PipeCommand.UpdateProgressDialog, new(
                progressMessage,
                progressDetailMessage,
                progressPercentage,
                messageAlignment));
        }

        /// <summary>
        /// Closes the progress dialog if it is currently open.
        /// </summary>
        /// <remarks>This method attempts to close the progress dialog and returns a value indicating
        /// whether the operation was successful. Ensure that the progress dialog is open before calling this method to
        /// avoid unnecessary calls.</remarks>
        /// <returns><see langword="true"/> if the progress dialog was successfully closed; otherwise, <see langword="false"/>.</returns>
        public ValueTask<bool> CloseProgressDialogAsync()
        {
            return InvokeAsync<bool>(PipeCommand.CloseProgressDialog);
        }

        /// <summary>
        /// Displays a notification icon in the system tray with the specified options.
        /// </summary>
        /// <param name="options">The configuration options for the notification icon.</param>
        /// <returns><see langword="true"/> if the notification icon was successfully displayed; otherwise, <see
        /// langword="false"/>.</returns>
        public ValueTask<bool> ShowNotifyIconAsync(NotifyIconOptions options)
        {
            return InvokeAsync<ShowNotifyIconPayload, bool>(PipeCommand.ShowNotifyIcon, new(options));
        }

        /// <summary>
        /// Determines whether the notification icon is currently open.
        /// </summary>
        /// <remarks>This method checks the state of the notification icon and returns a boolean value
        /// indicating whether it is currently displayed to the user.</remarks>
        /// <returns><see langword="true"/> if the notification icon is open; otherwise, <see langword="false"/>.</returns>
        public ValueTask<bool> NotifyIconOpenAsync()
        {
            return InvokeAsync<bool>(PipeCommand.NotifyIconOpen);
        }

        /// <summary>
        /// Updates the notify icon with the specified message text.
        /// </summary>
        /// <param name="messageText">The message text to display.</param>
        /// <returns><see langword="true"/> if the notify icon was updated successfully; otherwise, <see langword="false"/>.</returns>
        public ValueTask<bool> UpdateNotifyIconAsync(string messageText)
        {
            return InvokeAsync<UpdateNotifyIconPayload, bool>(PipeCommand.UpdateNotifyIcon, new(messageText));
        }

        /// <summary>
        /// Displays a balloon tip notification in the system tray with the specified title, text, and icon.
        /// </summary>
        /// <remarks>This method sends a request to display a balloon tip notification in the system tray.
        /// Ensure that the provided icon paths or identifiers are valid and accessible. The method may return <see
        /// langword="false"/> if the operation fails, such as when the system tray is not available or the parameters
        /// are invalid.</remarks>
        /// <param name="options">The configuration options for the balloon tip, including its title, text, icon, and duration. This parameter
        /// cannot be null.</param>
        /// <returns><see langword="true"/> if the balloon tip was successfully displayed; otherwise, <see langword="false"/>.</returns>
        public ValueTask<bool> ShowBalloonTipAsync(BalloonTipOptions options)
        {
            return InvokeAsync<ShowBalloonTipPayload, bool>(PipeCommand.ShowBalloonTip, new(options));
        }

        /// <summary>
        /// Closes the notification icon.
        /// </summary>
        /// <returns><see langword="true"/> if the notification icon was successfully closed; otherwise, <see langword="false"/>.</returns>
        public ValueTask<bool> CloseNotifyIconAsync()
        {
            return InvokeAsync<bool>(PipeCommand.CloseNotifyIcon);
        }

        /// <summary>
        /// Minimizes all open windows on the desktop.
        /// </summary>
        /// <remarks>This method attempts to minimize all currently open windows. The return value
        /// indicates whether the operation was successful. Note that the success of this operation may depend on
        /// system permissions or the current state of the desktop environment.</remarks>
        /// <returns><see langword="true"/> if the operation succeeds; otherwise, <see langword="false"/>.</returns>
        public ValueTask<bool> MinimizeAllWindowsAsync()
        {
            return InvokeAsync<bool>(PipeCommand.MinimizeAllWindows);
        }

        /// <summary>
        /// Restores all minimized or hidden windows to their original state.
        /// </summary>
        /// <remarks>This method attempts to restore all windows that were previously minimized or hidden.
        /// The return value indicates whether the operation was successful for all windows.</remarks>
        /// <returns><see langword="true"/> if all windows were successfully restored; otherwise, <see langword="false"/>.</returns>
        public ValueTask<bool> RestoreAllWindowsAsync()
        {
            return InvokeAsync<bool>(PipeCommand.RestoreAllWindows);
        }

        /// <summary>
        /// Sends a sequence of keystrokes to the specified window.
        /// </summary>
        /// <remarks>Ensure that the specified window handle is valid and the target window is capable of
        /// receiving keystrokes. The format of the <paramref name="options"/> parameter may depend on the underlying
        /// implementation.</remarks>
        /// <param name="options">The configuration options that specify the keys to send and their associated behavior. This parameter cannot
        /// be null.</param>
        /// <returns><see langword="true"/> if the keystrokes were successfully sent; otherwise, <see langword="false"/>.</returns>
        public ValueTask<bool> SendKeysAsync(SendKeysOptions options)
        {
            return InvokeAsync<SendKeysPayload, bool>(PipeCommand.SendKeys, new(options));
        }

        /// <summary>
        /// Retrieves information about windows associated with processes, optionally filtered by window titles, window
        /// handles, or parent process names.
        /// </summary>
        /// <remarks>This method allows filtering windows based on their titles, handles, or parent
        /// process names. Filters can be combined to narrow down the results. If no windows match the specified
        /// filters, the returned list will be empty.</remarks>
        /// <param name="options">An object specifying the criteria for retrieving window information, such as filtering or sorting
        /// preferences.</param>
        /// <returns>A read-only list of <see cref="WindowInfo"/> objects containing details about the windows that match the
        /// specified filters. If no filters are provided, all windows are included in the result.</returns>
        public ValueTask<ReadOnlyCollection<WindowInfo>> GetProcessWindowInfoAsync(WindowInfoOptions options)
        {
            return InvokeAsync<GetProcessWindowInfoPayload, ReadOnlyCollection<WindowInfo>>(PipeCommand.GetProcessWindowInfo, new(options));
        }

        /// <summary>
        /// Refreshes the desktop and environment variables.
        /// </summary>
        /// <remarks>This method invokes the "RefreshDesktopAndEnvironmentVariables" operation to update
        /// the desktop and environment variables. The return value indicates whether the operation was
        /// successful.</remarks>
        /// <returns><see langword="true"/> if the operation succeeds; otherwise, <see langword="false"/>.</returns>
        public ValueTask<bool> RefreshDesktopAndEnvironmentVariablesAsync()
        {
            return InvokeAsync<bool>(PipeCommand.RefreshDesktopAndEnvironmentVariables);
        }

        /// <summary>
        /// Retrieves the current user notification state.
        /// </summary>
        /// <remarks>This method deserializes the user notification state from an input source. Ensure
        /// that the input source contains valid serialized data for <see
        /// cref="QUERY_USER_NOTIFICATION_STATE"/>.</remarks>
        /// <returns>An instance of <see cref="QUERY_USER_NOTIFICATION_STATE"/> representing the user's notification state.</returns>
        public ValueTask<QUERY_USER_NOTIFICATION_STATE> GetUserNotificationStateAsync()
        {
            return InvokeAsync<QUERY_USER_NOTIFICATION_STATE>(PipeCommand.GetUserNotificationState);
        }

        /// <summary>
        /// Retrieves the process ID of the application that currently owns the foreground window.
        /// </summary>
        /// <remarks>This method sends a command to query the foreground window's process ID and parses
        /// the result. The returned process ID can be used to identify the application currently in focus.</remarks>
        /// <returns>The process ID of the application that owns the foreground window.</returns>
        public ValueTask<uint> GetForegroundWindowProcessIdAsync()
        {
            return InvokeAsync<uint>(PipeCommand.GetForegroundWindowProcessId);
        }

        /// <summary>
        /// Retrieves the value of the specified environment variable from the remote environment.
        /// </summary>
        /// <param name="variable">The name of the environment variable to retrieve. Cannot be null or empty.</param>
        /// <returns>The value of the specified environment variable, or null if the variable is not found.</returns>
        public ValueTask<string> GetEnvironmentVariableAsync(string variable)
        {
            return InvokeAsync<EnvironmentVariablePayload, string>(PipeCommand.GetEnvironmentVariable, new(variable));
        }

        /// <summary>
        /// Sets, appends to, or removes an environment variable with the specified name and value.
        /// </summary>
        /// <remarks>If <paramref name="remove"/> is <see langword="true"/>, the environment variable is
        /// removed regardless of the values of <paramref name="value"/>, <paramref name="expandable"/>, or <paramref
        /// name="append"/>. If <paramref name="append"/> is <see langword="true"/>, the specified value is appended to
        /// the existing value, if any. Use <paramref name="expandable"/> to indicate that the value contains references
        /// to other environment variables (such as %PATH%).</remarks>
        /// <param name="variable">The name of the environment variable to set, append to, or remove. Cannot be null or empty.</param>
        /// <param name="value">The value to assign to the environment variable. If <paramref name="remove"/> is <see langword="true"/>,
        /// this parameter is ignored.</param>
        /// <param name="expandable">true to mark the variable as expandable (e.g., allows references to other environment variables within its
        /// value); otherwise, false.</param>
        /// <param name="append">true to append the specified value to the existing value of the environment variable; otherwise, false to
        /// overwrite the value.</param>
        /// <param name="remove">true to remove the environment variable; otherwise, false to set or append the value.</param>
        /// <returns>true if the operation succeeds; otherwise, false.</returns>
        public ValueTask<bool> SetEnvironmentVariableAsync(string variable, string value, bool expandable, bool append, bool remove)
        {
            return InvokeAsync<EnvironmentVariablePayload, bool>(PipeCommand.SetEnvironmentVariable, new(variable, value, expandable, append, remove));
        }

        /// <summary>
        /// Removes the specified environment variable from the current environment.
        /// </summary>
        /// <param name="variable">The name of the environment variable to remove. Cannot be null or empty.</param>
        /// <returns>true if the environment variable was successfully removed; otherwise, false.</returns>
        public ValueTask<bool> RemoveEnvironmentVariableAsync(string variable)
        {
            return InvokeAsync<EnvironmentVariablePayload, bool>(PipeCommand.RemoveEnvironmentVariable, new(variable));
        }

        /// <summary>
        /// Triggers a Group Policy update on the target system, optionally forcing the update and specifying whether to
        /// run synchronously or asynchronously.
        /// </summary>
        /// <param name="force">true to reapply all policy settings, even those that have not changed; false to update only changed
        /// settings.</param>
        /// <returns>A ProcessResult object containing the outcome of the Group Policy update operation.</returns>
        public ValueTask<ProcessResult> GroupPolicyUpdateAsync(bool force)
        {
            return InvokeAsync<GroupPolicyUpdatePayload, ProcessResult>(PipeCommand.GroupPolicyUpdate, new(force));
        }

        /// <summary>
        /// Executes a process using the specified shell execution options in the user context.
        /// </summary>
        /// <param name="shellExecuteOptions">The options that define how the process should be executed, including file name, arguments, working
        /// directory, and window display settings.</param>
        /// <returns>A ProcessResult object containing information about the executed process, or null if the process could not
        /// be started.</returns>
        public ValueTask<ProcessResult?> ShellExecuteProcessAsync(UserShellExecuteOptions shellExecuteOptions)
        {
            return InvokeAsync<ShellExecuteProcessPayload, ProcessResult?>(PipeCommand.ShellExecuteProcess, new(shellExecuteOptions));
        }

        /// <summary>
        /// Gets the current focus mode state for the user.
        /// </summary>
        /// <remarks>Focus mode may affect how notifications or interruptions are handled for the user.
        /// Refer to the application's documentation for the mapping of integer values to specific focus mode
        /// states.</remarks>
        /// <returns>An integer value representing the user's focus mode state. The meaning of the value depends on the
        /// application's focus mode enumeration.</returns>
        public ValueTask<int> GetUserFocusModeStateAsync()
        {
            return InvokeAsync<int>(PipeCommand.GetUserFocusModeState);
        }

        /// <summary>
        /// Gets the current toast notification mode for the user session.
        /// </summary>
        /// <returns>A value of the <see cref="int"/> enumeration that indicates the user's toast notification
        /// mode.</returns>
        public ValueTask<int> GetUserToastNotificationModeAsync()
        {
            return InvokeAsync<int>(PipeCommand.GetUserToastNotificationMode);
        }

        /// <summary>
        /// Retrieves the exception, if any, that occurred during the execution of the log writer task.
        /// </summary>
        /// <returns>An <see cref="AggregateException"/> containing the exceptions thrown by the log writer task, or <see
        /// langword="null"/> if no exception occurred or the task has not been initialized.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1024:Use properties where appropriate", Justification = "I like methods.")]
        public AggregateException? GetLogWriterException()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _logWriterTask?.Exception;
        }

        /// <summary>
        /// Releases all resources used by this instance, including shutting down the client process
        /// and cleaning up all communication infrastructure.
        /// </summary>
        /// <remarks>This method gracefully closes the client process if it is running, waits for
        /// the log writer task to complete, and disposes all pipes, encryption objects, and cancellation
        /// token sources. Once disposed, the instance should not be used further.</remarks>
        /// <exception cref="ServerException">Thrown if the client process fails during shutdown.</exception>
        /// <exception cref="ClientException">Thrown if the client process exits with a known error code during shutdown.</exception>
        /// <exception cref="InvalidProgramException">Thrown if the client process exits with an unknown error code during shutdown.</exception>
        public async ValueTask DisposeAsync()
        {
            // Check we're not already done.
            if (_disposed)
            {
                return;
            }

            // Dispose all infrastructure.
            using (_clientProcessCts)
            using (_logWriterTaskCts)
            using (_logEncryption)
            using (_ioEncryption)
            using (_outputServer)
            using (_inputServer)
            using (_logServer)
            {
                // Use try/finally to ensure the log writer task is cancelled and awaited even if the client process fails to close.
                try
                {
                    // Shut down the client process if one was started.
                    if (_clientProcess is not null)
                    {
                        // Attempt a graceful close if the process is still running.
                        bool wasRunning = IsRunning;
                        if (wasRunning)
                        {
                            try
                            {
                                if (!await InvokeAsync<bool>(PipeCommand.Close).ConfigureAwait(false))
                                {
                                    throw new InvalidProgramException("The client process failed to close gracefully.");
                                }
                            }
                            catch (Exception ex) when (ex.Message is not null)
                            {
                                // Failed to gracefully close the process, so cancel it.
                                if (!_clientProcessCts.IsCancellationRequested)
                                {
                                    await _clientProcessCts.CancelAsync().ConfigureAwait(false);
                                }
                            }
                        }

                        // We either closed or cancelled the process. Wait for that to occur.
                        ProcessResult clientResult;
                        try
                        {
                            clientResult = await _clientProcess.ConfigureAwait(false);
                        }
                        catch (OperationCanceledException)
                        {
                            // Expected when the process faulted before disposal.
                            return;
                        }
                        finally
                        {
                            _clientProcess = null;
                        }

                        // If the client process failed, throw a ServerException with the inner exception.
                        using (clientResult)
                        {
                            if (DataSerialization.DeserializeExceptionFromStdErr(clientResult) is Exception clientException)
                            {
                                throw new ServerException("The client process failed during shutdown.", clientException);
                            }
                            if (wasRunning && clientResult.ExitCode is not 0 and not ProcessManager.TimeoutExitCode)
                            {
                                if (Enum.IsDefined(typeof(ClientExitCode), clientResult.ExitCode))
                                {
                                    throw new ClientException($"The client process exited with code [{clientResult.ExitCode.ToString(CultureInfo.InvariantCulture)}] during shutdown.", (ClientExitCode)clientResult.ExitCode);
                                }
                                throw new InvalidProgramException($"The client process exited with an unknown code [{clientResult.ExitCode.ToString(CultureInfo.InvariantCulture)}] during shutdown.");
                            }
                        }
                    }
                }
                finally
                {
                    // Cancel the log writer and wait for it to finish.
                    await _logWriterTaskCts.CancelAsync().ConfigureAwait(false);
                    if (_logWriterTask is not null)
                    {
                        await _logWriterTask.ConfigureAwait(false);
                        _logWriterTask = null;
                    }

                    // Unregister the process exit handler and mark as disposed.
                    AppDomain.CurrentDomain.ProcessExit -= ProcessExit_Handler;
                    _disposed = true;
                }
            }
        }

        /// <summary>
        /// Invokes a modal dialog command and sets the appropriate log source.
        /// </summary>
        /// <typeparam name="TResult">The expected return type from the dialog.</typeparam>
        /// <param name="dialogType">The type of the dialog to display.</param>
        /// <param name="dialogStyle">The style of the dialog to display.</param>
        /// <param name="options">The options to configure the dialog.</param>
        /// <returns>The result from the dialog.</returns>
        private ValueTask<TResult> ShowModalDialogAsync<TResult>(DialogType dialogType, DialogStyle dialogStyle, IDialogOptions options)
        {
            return InvokeAsync<ShowModalDialogPayload, TResult>(PipeCommand.ShowModalDialog, new(dialogType, dialogStyle, options));
        }

        /// <summary>
        /// Executes the specified command without a payload and returns the result from the client.
        /// </summary>
        /// <typeparam name="TResult">The expected return type from the client.</typeparam>
        /// <param name="command">The command to execute.</param>
        /// <returns>The result from the client, deserialized to type <typeparamref name="TResult"/>.</returns>
        /// <exception cref="ServerException">Thrown when the client returns an error or no data.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Critical Code Smell", "S2302:\"nameof\" should be used", Justification = "This is a false positive.")]
        private async ValueTask<TResult> InvokeAsync<TResult>(PipeCommand command)
        {
            // Send the request: [1-byte command]
            ObjectDisposedException.ThrowIf(_disposed, this);
            try
            {
                await _ioEncryption.WriteEncryptedAsync(_outputServer, [(byte)command]).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new ServerException("An error occurred while writing to the output stream.", ex, _clientProcess!);
            }
            return await ReadResponseAsync<TResult>().ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the specified command with a payload and returns the result from the client.
        /// </summary>
        /// <remarks>This method sends the command and payload to the client, reads the response,
        /// and returns the strongly-typed result. The request format is: [1-byte command][serialized payload].
        /// The response format uses a single byte discriminator:
        /// <see cref="ResponseMarker.Success"/> (followed by serialized result) or
        /// <see cref="ResponseMarker.Error"/> (followed by serialized exception).</remarks>
        /// <typeparam name="TPayload">The payload type, which must implement <see cref="IClientServerPayload"/>.</typeparam>
        /// <typeparam name="TResult">The expected return type from the client.</typeparam>
        /// <param name="command">The command to execute.</param>
        /// <param name="payload">The payload data for the command.</param>
        /// <returns>The result from the client, deserialized to type <typeparamref name="TResult"/>.</returns>
        /// <exception cref="InvalidDataException">Thrown when there is an I/O error communicating with the client.</exception>
        /// <exception cref="ServerException">Thrown when the client returns an error or no data.</exception>
        private async ValueTask<TResult> InvokeAsync<TPayload, TResult>(PipeCommand command, TPayload payload) where TPayload : IClientServerPayload
        {
            // Build and send the request: [1-byte command][serialized payload]
            ObjectDisposedException.ThrowIf(_disposed, this);
            byte[] payloadBytes = DataSerialization.SerializeToBytes(payload);
            byte[] request = new byte[payloadBytes.Length + 1];
            request[0] = (byte)command;
            payloadBytes.CopyTo(request.AsSpan(1));
            try
            {
                await _ioEncryption.WriteEncryptedAsync(_outputServer, request).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new ServerException("An error occurred while writing to the output stream.", ex, _clientProcess!);
            }
            return await ReadResponseAsync<TResult>().ConfigureAwait(false);
        }

        /// <summary>
        /// Reads and deserializes the response from the client.
        /// </summary>
        /// <typeparam name="T">The expected return type from the client.</typeparam>
        /// <returns>The result from the client, deserialized to type <typeparamref name="T"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the client process returns an invalid or empty response.</exception>
        /// <exception cref="ServerException">Thrown when the client returns an error or no data.</exception>
        private async ValueTask<T> ReadResponseAsync<T>()
        {
            // Read and decrypt the client's response.
            ObjectDisposedException.ThrowIf(_disposed, this);
            byte[] response;
            try
            {
                if ((response = await _ioEncryption.ReadEncryptedAsync(_inputServer).ConfigureAwait(false)).Length < 2)
                {
                    throw new InvalidOperationException("The client process returned an invalid or empty response.");
                }
            }
            catch (Exception ex)
            {
                throw new ServerException("An error occurred while reading from the input stream.", ex, _clientProcess!);
            }

            // Deserialize based on the success marker.
            return response[0] != (byte)ResponseMarker.Success
                ? throw new ServerException("The client process returned an exception.", DataSerialization.DeserializeFromBytes<Exception>(response, 1))
                : DataSerialization.DeserializeFromBytes<T>(response, 1);
        }

        /// <summary>
        /// Handles the application's process exit event to perform necessary cleanup operations before the process
        /// terminates.
        /// </summary>
        /// <remarks>This handler is intended to be registered with the application's process exit event
        /// to ensure that resources are properly released when the process is shutting down. It should not be called
        /// directly.</remarks>
        /// <param name="sender">The source of the event, typically the current application domain.</param>
        /// <param name="e">An object that contains the event data.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD100:Avoid async void methods", Justification = "This is necessary here.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0155:Do not use async void methods", Justification = "This is necessary here.")]
        private async void ProcessExit_Handler(object? sender, EventArgs e)
        {
            if (!_disposed)
            {
                await using (this.ConfigureAwait(false))
                {
                    return;
                }
            }
        }

        /// <summary>
        /// Represents the session information for the current user.
        /// </summary>
        /// <remarks>This field stores details about the user's session, such as authentication or
        /// user-specific data. It is intended for internal use and should not be exposed directly to external
        /// consumers.</remarks>
        public RunAsActiveUser RunAsActiveUser { get; }

        /// <summary>
        /// Gets a value indicating whether the process is currently running.
        /// </summary>
        public bool IsRunning => (_clientProcess?.Process.HasExited) is false;

        /// <summary>
        /// Represents the sentinel character used to indicate a successful operation or status.
        /// </summary>
        /// <remarks>The value of this constant is the Unicode character U+001F (Unit Separator). It can
        /// be used as a marker in protocols or data streams to signify success. Ensure that the receiving system
        /// interprets this character as intended, as it is a non-printable control character.</remarks>
        public const string SuccessSentinel = "\x1F";

        /// <summary>
        /// Represents an asynchronous operation that retrieves the result of a client process.
        /// </summary>
        /// <remarks>The task encapsulates the execution of a client process and provides access to its
        /// result, which may be null if the process does not produce a result or if <see cref="OpenAsync"/> has not
        /// been called yet.</remarks>
        private ProcessHandle? _clientProcess;

        /// <summary>
        /// Represents the task responsible for writing log entries asynchronously.
        /// </summary>
        /// <remarks>This field holds a reference to the current logging task, if one is active. It may
        /// be null if <see cref="OpenAsync"/> has not been called yet.</remarks>
        private Task? _logWriterTask;

        /// <summary>
        /// Indicates whether the object has been disposed.
        /// </summary>
        /// <remarks>This field is used internally to track the disposal state of the object. It should
        /// not be accessed directly outside of the class.</remarks>
        private bool _disposed;

        /// <summary>
        /// Represents the server side of an anonymous pipe used for interprocess communication.
        /// </summary>
        /// <remarks>This pipe server is initialized with an output direction and allows the handle to be
        /// inherited by child processes. It is typically used to send data from the current process to another
        /// process.</remarks>
        private readonly AnonymousPipeServerStream _outputServer = new(PipeDirection.Out, HandleInheritability.Inheritable);

        /// <summary>
        /// Represents a server-side anonymous pipe stream for reading data.
        /// </summary>
        /// <remarks>This pipe stream is initialized with an input direction and inheritable handle
        /// settings, allowing it to be used for inter-process communication where the handle can be passed to a child
        /// process.</remarks>
        private readonly AnonymousPipeServerStream _inputServer = new(PipeDirection.In, HandleInheritability.Inheritable);

        /// <summary>
        /// Represents the server side of an anonymous pipe used for inter-process communication.
        /// </summary>
        /// <remarks>This field is used to manage the server stream for logging purposes. It provides a
        /// communication channel between processes, allowing data to be sent from the server to a connected
        /// client.</remarks>
        private readonly AnonymousPipeServerStream _logServer = new(PipeDirection.In, HandleInheritability.Inheritable);

        /// <summary>
        /// Provides ECDH-based encryption for the main command/response pipe communication.
        /// </summary>
        /// <remarks>This encryption instance is used to encrypt commands sent to the client and decrypt
        /// responses received from the client, ensuring secure communication across different security contexts.</remarks>
        private readonly ServerPipeEncryption _ioEncryption = new();

        /// <summary>
        /// Provides ECDH-based encryption for the log pipe communication.
        /// </summary>
        /// <remarks>This separate encryption instance is used for the log channel to allow independent
        /// encrypted communication for logging purposes.</remarks>
        private readonly ServerPipeEncryption _logEncryption = new();

        /// <summary>
        /// Represents the <see cref="CancellationTokenSource"/> used to manage cancellation of the client process.
        /// </summary>
        /// <remarks>This field is initialized in the constructor and is intended for internal use to signal
        /// cancellation of the client process. It is not exposed publicly.</remarks>
        private readonly CancellationTokenSource _clientProcessCts = new();

        /// <summary>
        /// Provides a mechanism to cancel the ongoing log writer task.
        /// </summary>
        /// <remarks>This field is initialized in the constructor and is used internally to signal
        /// cancellation for the log writer task.</remarks>
        private readonly CancellationTokenSource _logWriterTaskCts = new();
    }
}
