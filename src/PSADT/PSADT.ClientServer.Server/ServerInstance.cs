using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using PSADT.ClientServer.Payloads;
using PSADT.Foundation;
using PSADT.LibraryInterfaces;
using PSADT.ProcessManagement;
using PSADT.Types;
using PSADT.UserInterface;
using PSADT.UserInterface.DialogOptions;
using PSADT.UserInterface.DialogResults;
using PSADT.Utilities;
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
    /// cref="IDisposable"/> to ensure proper cleanup of resources. <para> Typical usage involves creating an instance
    /// of <see cref="ServerInstance"/>, calling <see cref="Open"/> to initialize the client-server communication, and
    /// using a number of predefined methods to send commands to the client. Once the communication is
    /// complete, the <see cref="Dispose()"/> method should be called to release resources. </para></remarks>
    public sealed record ServerInstance : IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServerInstance"/> class, setting up inter-process communication
        /// using anonymous pipes.
        /// </summary>
        /// <remarks>This constructor creates anonymous pipe streams for input and output communication.
        /// The input stream is configured for reading, while the output stream is configured for writing. The output
        /// stream is set to automatically flush data to ensure timely communication.</remarks>
        public ServerInstance(RunAsActiveUser runAsActiveUser)
        {
            RunAsActiveUser = runAsActiveUser ?? throw new ArgumentNullException("User cannot be null.", (Exception?)null);
        }

        /// <summary>
        /// Opens the client-server communication by starting the client process and initializing the connection.
        /// </summary>
        /// <remarks>This method launches the client process and establishes communication through
        /// inter-process pipes. It ensures that the client process is ready to receive commands before
        /// returning.</remarks>
        /// <exception cref="ApplicationException">Thrown if the client process fails to respond to the initial command, indicating that it is not properly
        /// initialized.</exception>
        public void Open()
        {
            // Don't allow opening if the object's been disposed.
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(ServerInstance), "Cannot open a connection on a disposed ServerInstance.");
            }

            // Don't re-open if there's already a client process associated with this instance.
            if (_clientProcessCts is not null || _clientProcess is not null)
            {
                throw new InvalidOperationException("The server instance already has an associated client process.");
            }

            // Start the server to listen for incoming connections and process data.
            _outputServer = new(PipeDirection.Out, HandleInheritability.Inheritable);
            _inputServer = new(PipeDirection.In, HandleInheritability.Inheritable);
            _logServer = new(PipeDirection.In, HandleInheritability.Inheritable);
            bool outputServerClientSafePipeHandleAddRef = false;
            bool inputServerClientSafePipeHandleAddRef = false;
            bool logServerClientSafePipeHandleAddRef = false;
            try
            {
                _outputServer.ClientSafePipeHandle.DangerousAddRef(ref outputServerClientSafePipeHandleAddRef);
                _inputServer.ClientSafePipeHandle.DangerousAddRef(ref inputServerClientSafePipeHandleAddRef);
                _logServer.ClientSafePipeHandle.DangerousAddRef(ref logServerClientSafePipeHandleAddRef);
                string outputServerClientSafePipeHandle = _outputServer.GetClientHandleAsString();
                string inputServerClientSafePipeHandle = _inputServer.GetClientHandleAsString();
                string logServerClientSafePipeHandle = _logServer.GetClientHandleAsString();
                _clientProcess = ProcessManager.LaunchAsync(new(
                    _assemblyLocation,
                    ["/ClientServer", "-InputPipe", outputServerClientSafePipeHandle, "-OutputPipe", inputServerClientSafePipeHandle, "-LogPipe", logServerClientSafePipeHandle],
                    Environment.SystemDirectory,
                    RunAsActiveUser,
                    UseLinkedAdminToken,
                    UseHighestAvailableToken,
                    denyUserTermination: true,
                    handlesToInherit: [NumericalUtilities.ParseIntPtr(outputServerClientSafePipeHandle), NumericalUtilities.ParseIntPtr(inputServerClientSafePipeHandle), NumericalUtilities.ParseIntPtr(logServerClientSafePipeHandle)],
                    createNoWindow: true,
                    waitForChildProcesses: true,
                    killChildProcessesWithParent: true,
                    windowStyle: ProcessWindowStyle.Hidden,
                    cancellationToken: (_clientProcessCts = new()).Token
                ));
            }
            finally
            {
                if (outputServerClientSafePipeHandleAddRef)
                {
                    _outputServer.ClientSafePipeHandle.DangerousRelease();
                }
                _outputServer.DisposeLocalCopyOfClientHandle();
                if (inputServerClientSafePipeHandleAddRef)
                {
                    _inputServer.ClientSafePipeHandle.DangerousRelease();
                }
                _inputServer.DisposeLocalCopyOfClientHandle();
                if (logServerClientSafePipeHandleAddRef)
                {
                    _logServer.ClientSafePipeHandle.DangerousRelease();
                }
                _logServer.DisposeLocalCopyOfClientHandle();
            }

            // Confirm the client starts and is ready to receive commands.
            bool? opened = null;
            try
            {
                (_ioEncryption = new()).PerformKeyExchange(_outputServer, _inputServer);
                (_logEncryption = new()).PerformKeyExchange(_outputServer, _inputServer);
                if (!(opened = Invoke<bool>(PipeCommand.Open)).Value)
                {
                    throw new InvalidOperationException("The opened client process returned an invalid response.");
                }
            }
            catch (Exception ex)
            {
                throw new ServerException("The opened client process is not properly responding to commands.", ex, _clientProcess!);
            }
            finally
            {
                if (opened is null || !opened.Value)
                {
                    Close(true);
                }
            }

            // Ensure this instance is closed/disposed on process exit.
            AppDomain.CurrentDomain.ProcessExit += ProcessExit_Handler;

            // Set up the log writer task to run in the background.
            _logWriterTask = Task.Run(ReadLog, (_logWriterTaskCts = new()).Token);
        }

        /// <summary>
        /// Closes the server instance and its associated client process.
        /// </summary>
        /// <remarks>This method ensures that the server instance and its client process are properly closed, releasing
        /// all associated resources. If the server instance is not open or has already been closed, an <see
        /// cref="InvalidOperationException"/> is thrown. The <paramref name="force"/> parameter can be used to forcibly
        /// terminate the client process if it does not respond to the close command.</remarks>
        /// <param name="force">A value indicating whether to forcibly close the client process. If <see langword="true"/>, the client process is
        /// terminated regardless of its response to the close command. If <see langword="false"/>, the method attempts to close
        /// the client process gracefully.</param>
        /// <exception cref="InvalidOperationException">Thrown if the server instance is not open or has already been closed.</exception>
        /// <exception cref="ApplicationException">Thrown if the client process does not properly respond to the close command and <paramref name="force"/> is <see
        /// langword="false"/>.</exception>
        internal void Close(bool force = false)
        {
            // Don't allow closing if the object's been disposed.
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(ServerInstance), "Cannot close a connection on a disposed ServerInstance.");
            }

            // Confirm that the server instance is open and has not been closed already.
            if (_clientProcessCts is null || _clientProcess is null)
            {
                throw new InvalidOperationException("The server instance is not open or has already been closed.");
            }

            // Shut down the client if it's running.
            int exitCode = -1;
            try
            {
                if (IsRunning)
                {
                    if (!force)
                    {
                        if (!Invoke<bool>(PipeCommand.Close))
                        {
                            throw new InvalidOperationException("The opened client process did not properly respond to the close command.");
                        }
                        if ((exitCode = _clientProcess.Task.GetAwaiter().GetResult().ExitCode) != 0)
                        {
                            throw new ServerException($"The client process exited with a non-zero exit code: {exitCode}.", _clientProcess);
                        }
                    }
                    else
                    {
                        _clientProcessCts.Cancel();
                    }
                }
                else if ((exitCode = _clientProcess.Task.GetAwaiter().GetResult().ExitCode) != 0)
                {
                    throw new ServerException($"The client process exited with a non-zero exit code: {exitCode}.", _clientProcess);
                }
            }
            finally
            {
                // Close the log writer and wait for it to finish.
                if (_logWriterTaskCts is not null && _logWriterTask is not null)
                {
                    _logWriterTaskCts.Cancel();
                    try
                    {
                        _logWriterTask.GetAwaiter().GetResult();
                    }
                    catch (TaskCanceledException)
                    {
                        // The log writer task was canceled, which is expected when closing the server instance.
                    }
                    finally
                    {
                        _logWriterTask.Dispose();
                        _logWriterTask = null;
                        _logWriterTaskCts.Dispose();
                        _logWriterTaskCts = null;
                    }
                }

                // Clean up the client process resources.
                try
                {
                    // We only need to cancel if we didn't get an exit code already.
                    if (exitCode == -1)
                    {
                        if (!_clientProcessCts.IsCancellationRequested)
                        {
                            _clientProcessCts.Cancel();
                        }
                        try
                        {
                            exitCode = _clientProcess.Task.GetAwaiter().GetResult().ExitCode;
                        }
                        catch (TaskCanceledException)
                        {
                            // The client process task was canceled, which is expected when closing the server instance.
                        }
                    }
                }
                finally
                {
                    // Wait for the client process to exit and dispose of its resources.
                    while (!_clientProcess.Task.IsCompleted)
                    {
                        Thread.Sleep(1);
                    }
                    _clientProcess.Task.Dispose();
                    _clientProcess.Process.Dispose();
                    _clientProcess = null;
                    _clientProcessCts.Dispose();
                    _clientProcessCts = null;

                    // Dispose encryption objects.
                    _logEncryption?.Dispose();
                    _ioEncryption?.Dispose();

                    // Dispose pipe servers.
                    _logServer?.Dispose();
                    _inputServer?.Dispose();
                    _outputServer?.Dispose();

                    // Unregister the process exit handler.
                    AppDomain.CurrentDomain.ProcessExit -= ProcessExit_Handler;
                }
            }
        }

        /// <summary>
        /// Closes the current connection and releases associated resources.
        /// </summary>
        /// <remarks>This method closes the connection and optionally performs additional cleanup
        /// operations depending on the internal implementation. Once closed, the connection cannot be reused and must
        /// be reopened if needed.</remarks>
        public void Close()
        {
            Close(false);
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
        public bool InitCloseAppsDialog(ReadOnlyCollection<ProcessDefinition>? closeProcesses)
        {
            return Invoke<InitCloseAppsDialogPayload, bool>(PipeCommand.InitCloseAppsDialog, new(closeProcesses));
        }

        /// <summary>
        /// Prompts the user to close any running applications that may interfere with the installation process.
        /// </summary>
        /// <remarks>This method invokes a prompt to the user and returns their response. Ensure that the
        /// environment allows user interaction before calling this method.</remarks>
        /// <returns><see langword="true"/> if the user agrees to close the applications; otherwise, <see langword="false"/>.</returns>
        public bool PromptToCloseApps(TimeSpan promptToCloseTimeout)
        {
            return Invoke<PromptToCloseAppsPayload, bool>(PipeCommand.PromptToCloseApps, new(promptToCloseTimeout));
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
        public CloseAppsDialogResult ShowCloseAppsDialog(DialogStyle dialogStyle, CloseAppsDialogOptions options)
        {
            return ShowModalDialog<CloseAppsDialogResult>(DialogType.CloseAppsDialog, dialogStyle, options);
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
        public string ShowCustomDialog(DialogStyle dialogStyle, CustomDialogOptions options)
        {
            return ShowModalDialog<string>(DialogType.CustomDialog, dialogStyle, options);
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
        public ListSelectionDialogResult ShowListSelectionDialog(DialogStyle dialogStyle, ListSelectionDialogOptions options)
        {
            return (ListSelectionDialogResult)ShowModalDialog(DialogType.ListSelectionDialog, dialogStyle, options)!;
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
        public InputDialogResult ShowInputDialog(DialogStyle dialogStyle, InputDialogOptions options)
        {
            return ShowModalDialog<InputDialogResult>(DialogType.InputDialog, dialogStyle, options);
        }

        /// <summary>
        /// Displays a restart dialog to the user and returns the user's input as a string.
        /// </summary>
        /// <remarks>This method displays a modal dialog of type <see cref="DialogType.InputDialog"/> and
        /// blocks execution until the user provides input or dismisses the dialog. The returned value depends on the
        /// specific implementation of the dialog and the user's interaction.</remarks>
        /// <param name="dialogStyle">The style of the dialog, which determines its appearance and behavior.</param>
        /// <param name="options">The options to configure the restart dialog, such as title, message, and default values.</param>
        /// <returns>A string representing the user's input from the dialog. The value may vary depending on the dialog
        /// configuration and user interaction.</returns>
        public string ShowRestartDialog(DialogStyle dialogStyle, RestartDialogOptions options)
        {
            return ShowModalDialog<string>(DialogType.RestartDialog, dialogStyle, options);
        }

        /// <summary>
        /// Displays a modal dialog box with the specified options, and returns the result of the dialog interaction.
        /// </summary>
        /// <remarks>Use this method to display a modal dialog box that requires user input or
        /// confirmation. The dialog box will block the calling thread until the user closes it, and the result will
        /// indicate the user's action (e.g., OK, Cancel).</remarks>
        /// <param name="options">The options to configure the dialog box, such as title, message, and input fields.</param>
        /// <returns>A <see cref="DialogBoxResult"/> that represents the result of the user's interaction with the dialog box.</returns>
        public DialogBoxResult ShowDialogBox(DialogBoxOptions options)
        {
            return ShowModalDialog<DialogBoxResult>(DialogType.DialogBox, 0, options);
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
        public bool ShowProgressDialog(DialogStyle dialogStyle, ProgressDialogOptions options)
        {
            return Invoke<ShowProgressDialogPayload, bool>(PipeCommand.ShowProgressDialog, new(dialogStyle, options));
        }

        /// <summary>
        /// Determines whether the progress dialog is currently open.
        /// </summary>
        /// <remarks>This method checks the state of the progress dialog and returns a boolean value
        /// indicating whether it is currently displayed to the user.</remarks>
        /// <returns><see langword="true"/> if the progress dialog is open; otherwise, <see langword="false"/>.</returns>
        public bool ProgressDialogOpen()
        {
            return Invoke<bool>(PipeCommand.ProgressDialogOpen);
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
        public bool UpdateProgressDialog(string? progressMessage = null, string? progressDetailMessage = null, double? progressPercentage = null, DialogMessageAlignment? messageAlignment = null)
        {
            return Invoke<UpdateProgressDialogPayload, bool>(PipeCommand.UpdateProgressDialog, new(
                !string.IsNullOrWhiteSpace(progressMessage) ? progressMessage : null,
                !string.IsNullOrWhiteSpace(progressDetailMessage) ? progressDetailMessage : null,
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
        public bool CloseProgressDialog()
        {
            return Invoke<bool>(PipeCommand.CloseProgressDialog);
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
        public bool ShowBalloonTip(BalloonTipOptions options)
        {
            return Invoke<ShowBalloonTipPayload, bool>(PipeCommand.ShowBalloonTip, new(options));
        }

        /// <summary>
        /// Minimizes all open windows on the desktop.
        /// </summary>
        /// <remarks>This method attempts to minimize all currently open windows. The return value
        /// indicates whether the operation was successful. Note that the success of this operation may depend on
        /// system permissions or the current state of the desktop environment.</remarks>
        /// <returns><see langword="true"/> if the operation succeeds; otherwise, <see langword="false"/>.</returns>
        public bool MinimizeAllWindows()
        {
            return Invoke<bool>(PipeCommand.MinimizeAllWindows);
        }

        /// <summary>
        /// Restores all minimized or hidden windows to their original state.
        /// </summary>
        /// <remarks>This method attempts to restore all windows that were previously minimized or hidden.
        /// The return value indicates whether the operation was successful for all windows.</remarks>
        /// <returns><see langword="true"/> if all windows were successfully restored; otherwise, <see langword="false"/>.</returns>
        public bool RestoreAllWindows()
        {
            return Invoke<bool>(PipeCommand.RestoreAllWindows);
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
        public bool SendKeys(SendKeysOptions options)
        {
            return Invoke<SendKeysPayload, bool>(PipeCommand.SendKeys, new(options));
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
        public IReadOnlyList<WindowInfo> GetProcessWindowInfo(WindowInfoOptions options)
        {
            return Invoke<GetProcessWindowInfoPayload, ReadOnlyCollection<WindowInfo>>(PipeCommand.GetProcessWindowInfo, new(options));
        }

        /// <summary>
        /// Refreshes the desktop and environment variables.
        /// </summary>
        /// <remarks>This method invokes the "RefreshDesktopAndEnvironmentVariables" operation to update
        /// the desktop and environment variables. The return value indicates whether the operation was
        /// successful.</remarks>
        /// <returns><see langword="true"/> if the operation succeeds; otherwise, <see langword="false"/>.</returns>
        public bool RefreshDesktopAndEnvironmentVariables()
        {
            return Invoke<bool>(PipeCommand.RefreshDesktopAndEnvironmentVariables);
        }

        /// <summary>
        /// Retrieves the current user notification state.
        /// </summary>
        /// <remarks>This method deserializes the user notification state from an input source. Ensure
        /// that the input source contains valid serialized data for <see
        /// cref="QUERY_USER_NOTIFICATION_STATE"/>.</remarks>
        /// <returns>An instance of <see cref="QUERY_USER_NOTIFICATION_STATE"/> representing the user's notification state.</returns>
        public QUERY_USER_NOTIFICATION_STATE GetUserNotificationState()
        {
            return Invoke<QUERY_USER_NOTIFICATION_STATE>(PipeCommand.GetUserNotificationState);
        }

        /// <summary>
        /// Retrieves the process ID of the application that currently owns the foreground window.
        /// </summary>
        /// <remarks>This method sends a command to query the foreground window's process ID and parses
        /// the result. The returned process ID can be used to identify the application currently in focus.</remarks>
        /// <returns>The process ID of the application that owns the foreground window.</returns>
        public uint GetForegroundWindowProcessId()
        {
            return Invoke<uint>(PipeCommand.GetForegroundWindowProcessId);
        }

        /// <summary>
        /// Retrieves the value of the specified environment variable from the remote environment.
        /// </summary>
        /// <param name="variable">The name of the environment variable to retrieve. Cannot be null or empty.</param>
        /// <returns>The value of the specified environment variable, or null if the variable is not found.</returns>
        public string GetEnvironmentVariable(string variable)
        {
            return Invoke<EnvironmentVariablePayload, string>(PipeCommand.GetEnvironmentVariable, new(variable));
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
        public bool SetEnvironmentVariable(string variable, string value, bool expandable, bool append, bool remove)
        {
            return Invoke<EnvironmentVariablePayload, bool>(PipeCommand.SetEnvironmentVariable, new(variable, value, expandable, append, remove));
        }

        /// <summary>
        /// Removes the specified environment variable from the current environment.
        /// </summary>
        /// <param name="variable">The name of the environment variable to remove. Cannot be null or empty.</param>
        /// <returns>true if the environment variable was successfully removed; otherwise, false.</returns>
        public bool RemoveEnvironmentVariable(string variable)
        {
            return Invoke<EnvironmentVariablePayload, bool>(PipeCommand.RemoveEnvironmentVariable, new(variable));
        }

        /// <summary>
        /// Triggers a Group Policy update on the target system, optionally forcing the update and specifying whether to
        /// run synchronously or asynchronously.
        /// </summary>
        /// <param name="force">true to reapply all policy settings, even those that have not changed; false to update only changed
        /// settings.</param>
        /// <returns>A ProcessResult object containing the outcome of the Group Policy update operation.</returns>
        public ProcessResult GroupPolicyUpdate(bool force)
        {
            return Invoke<GroupPolicyUpdatePayload, ProcessResult>(PipeCommand.GroupPolicyUpdate, new(force));
        }

        /// <summary>
        /// Retrieves the exception, if any, that occurred during the execution of the log writer task.
        /// </summary>
        /// <returns>An <see cref="AggregateException"/> containing the exceptions thrown by the log writer task, or <see
        /// langword="null"/> if no exception occurred or the task has not been initialized.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1024:Use properties where appropriate", Justification = "I like methods.")]
        public AggregateException? GetLogWriterException()
        {
            return _logWriterTask?.Exception;
        }

        /// <summary>
        /// Releases the resources used by the current instance of the class.
        /// </summary>
        /// <remarks>This method should be called when the instance is no longer needed to free up
        /// resources. It suppresses the finalization of the object to optimize garbage collection.</remarks>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Releases the resources used by the current instance of the class.
        /// </summary>
        /// <remarks>This method should be called to release both managed and unmanaged resources. If the
        /// <paramref name="disposing"/> parameter is <see langword="true"/>, the method releases managed resources in
        /// addition to unmanaged resources. Once disposed, the instance should not be used further.</remarks>
        /// <param name="disposing"><see langword="true"/> to release both managed and unmanaged resources; <see langword="false"/> to release
        /// only unmanaged resources.</param>
        private void Dispose(bool disposing)
        {
            // Check we're not already done.
            if (_disposed)
            {
                return;
            }

            // Close the client process if it is running.
            if (disposing && _clientProcess is not null)
            {
                Close();
            }
            _disposed = true;
        }

        /// <summary>
        /// Invokes a modal dialog command and sets the appropriate log source.
        /// </summary>
        /// <typeparam name="TResult">The expected return type from the dialog.</typeparam>
        /// <param name="dialogType">The type of the dialog to display.</param>
        /// <param name="dialogStyle">The style of the dialog to display.</param>
        /// <param name="options">The options to configure the dialog.</param>
        /// <returns>The result from the dialog.</returns>
        private TResult ShowModalDialog<TResult>(DialogType dialogType, DialogStyle dialogStyle, IDialogOptions options)
        {
            return Invoke<ShowModalDialogPayload, TResult>(PipeCommand.ShowModalDialog, new(dialogType, dialogStyle, options));
        }

        /// <summary>
        /// Executes the specified command without a payload and returns the result from the client.
        /// </summary>
        /// <typeparam name="TResult">The expected return type from the client.</typeparam>
        /// <param name="command">The command to execute.</param>
        /// <returns>The result from the client, deserialized to type <typeparamref name="TResult"/>.</returns>
        /// <exception cref="InvalidDataException">Thrown when there is an I/O error communicating with the client.</exception>
        private TResult Invoke<TResult>(PipeCommand command)
        {
            // Don't invoke anything if the object is disposed.
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(ServerInstance), "Cannot invoke a command on a disposed ServerInstance.");
            }

            // Ensure this object is opened before proceeding.
            if (_ioEncryption is null || _outputServer is null)
            {
                throw new InvalidOperationException("The server instance is not open.");
            }

            // Send the request: [1-byte command]
            try
            {
                _ioEncryption.WriteEncrypted(_outputServer, [(byte)command]);
            }
            catch (Exception ex)
            {
                throw new ServerException("An error occurred while writing to the output stream.", ex, _clientProcess!);
            }
            return ReadResponse<TResult>();
        }

        /// <summary>
        /// Executes the specified command with a payload and returns the result from the client.
        /// </summary>
        /// <remarks>This method sends the command and payload to the client, reads the response,
        /// and returns the strongly-typed result. The request format is: [1-byte command][serialized payload].
        /// The response format uses a single byte discriminator:
        /// <see cref="ResponseMarker.Success"/> (followed by serialized result) or 
        /// <see cref="ResponseMarker.Error"/> (followed by serialized exception).</remarks>
        /// <typeparam name="TPayload">The payload type, which must implement <see cref="IPayload"/>.</typeparam>
        /// <typeparam name="TResult">The expected return type from the client.</typeparam>
        /// <param name="command">The command to execute.</param>
        /// <param name="payload">The payload data for the command.</param>
        /// <returns>The result from the client, deserialized to type <typeparamref name="TResult"/>.</returns>
        /// <exception cref="InvalidDataException">Thrown when there is an I/O error communicating with the client.</exception>
        /// <exception cref="ServerException">Thrown when the client returns an error or no data.</exception>
        private TResult Invoke<TPayload, TResult>(PipeCommand command, TPayload payload) where TPayload : IPayload
        {
            // Don't invoke anything if the object is disposed.
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(ServerInstance), "Cannot invoke a command on a disposed ServerInstance.");
            }

            // Ensure this object is opened before proceeding.
            if (_ioEncryption is null || _outputServer is null)
            {
                throw new InvalidOperationException("The server instance is not open.");
            }

            // Build and send the request: [1-byte command][serialized payload]
            byte[] payloadBytes = DataSerialization.SerializeToBytes(payload);
            byte[] request = new byte[payloadBytes.Length + 1];
            request[0] = (byte)command;
            payloadBytes.CopyTo(request.AsSpan(1));
            try
            {
                _ioEncryption.WriteEncrypted(_outputServer, request);
            }
            catch (Exception ex)
            {
                throw new ServerException("An error occurred while writing to the output stream.", ex, _clientProcess!);
            }
            return ReadResponse<TResult>();
        }

        /// <summary>
        /// Reads and deserializes the response from the client.
        /// </summary>
        /// <typeparam name="T">The expected return type from the client.</typeparam>
        /// <returns>The result from the client, deserialized to type <typeparamref name="T"/>.</returns>
        /// <exception cref="InvalidDataException">Thrown when there is an I/O error communicating with the client.</exception>
        /// <exception cref="ServerException">Thrown when the client returns an error or no data.</exception>
        private T ReadResponse<T>()
        {
            // Don't read anything if the object is disposed.
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(ServerInstance), "Cannot read a response from a disposed ServerInstance.");
            }

            // Ensure this object is opened before proceeding.
            if (_ioEncryption is null || _inputServer is null)
            {
                throw new InvalidOperationException("The server instance is not open.");
            }

            // Read and decrypt the client's response.
            byte[] response;
            try
            {
                if ((response = _ioEncryption.ReadEncrypted(_inputServer)).Length < 2)
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
        /// Reads and processes log entries from the underlying log stream.
        /// </summary>
        /// <remarks>This method reads each line from the log stream until the end of the stream is
        /// reached. Non-empty and non-whitespace lines are processed as needed.</remarks>
        private void ReadLog()
        {
            // Don't read anything if the object is disposed.
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(ServerInstance), "Cannot read logs from a disposed ServerInstance.");
            }

            // Ensure the log encryption and server are initialized before proceeding.
            if (_logEncryption is null || _logServer is null)
            {
                throw new InvalidOperationException("The log reader is not initialized.");
            }

            // Read the log stream until cancellation is requested or the end of the stream is reached.
            while (!_logWriterTaskCts!.IsCancellationRequested)
            {
                try
                {
                    // Read and decrypt the log message, then process it if a deployment session is active.
                    // We must read it before if there's a deployment session active to clear the queue.
                    if (_logEncryption.ReadEncrypted(_logServer) is { Length: > 0 } decrypted && ModuleDatabase.IsDeploymentSessionActive())
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

        /// <summary>
        /// Handles the application's process exit event to perform necessary cleanup operations before the process
        /// terminates.
        /// </summary>
        /// <remarks>This handler is intended to be registered with the application's process exit event
        /// to ensure that resources are properly released when the process is shutting down. It should not be called
        /// directly.</remarks>
        /// <param name="sender">The source of the event, typically the current application domain.</param>
        /// <param name="e">An object that contains the event data.</param>
        private void ProcessExit_Handler(object? sender, EventArgs e)
        {
            if (!_disposed)
            {
                Close(true);
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
        public bool IsRunning => _clientProcess?.Process.HasExited == false;

        /// <summary>
        /// Represents the sentinel character used to indicate a successful operation or status.
        /// </summary>
        /// <remarks>The value of this constant is the Unicode character U+001F (Unit Separator). It can
        /// be used as a marker in protocols or data streams to signify success. Ensure that the receiving system
        /// interprets this character as intended, as it is a non-printable control character.</remarks>
        public const string SuccessSentinel = "\x1F";

        /// <summary>
        /// Indicates whether a linked administrator token should be used.
        /// </summary>
        /// <remarks>This constant is set to <see langword="false"/>, meaning that linked administrator
        /// tokens are not utilized by default. This value is internal and cannot be modified.</remarks>
        internal const bool UseLinkedAdminToken = false;

        /// <summary>
        /// Indicates whether the highest available token should be used.
        /// </summary>
        /// <remarks>This constant is set to <see langword="true"/> and is intended for internal use to
        /// specify that the highest available token should be utilized in relevant operations.</remarks>
        internal const bool UseHighestAvailableToken = true;

        /// <summary>
        /// Represents the server side of an anonymous pipe used for interprocess communication.
        /// </summary>
        /// <remarks>This pipe server is initialized with an output direction and allows the handle to be
        /// inherited by child processes. It is typically used to send data from the current process to another
        /// process.</remarks>
        private AnonymousPipeServerStream? _outputServer;

        /// <summary>
        /// Represents a server-side anonymous pipe stream for reading data.
        /// </summary>
        /// <remarks>This pipe stream is initialized with an input direction and inheritable handle
        /// settings, allowing it to be used for inter-process communication where the handle can be passed to a child
        /// process.</remarks>
        private AnonymousPipeServerStream? _inputServer;

        /// <summary>
        /// Represents the server side of an anonymous pipe used for inter-process communication.
        /// </summary>
        /// <remarks>This field is used to manage the server stream for logging purposes. It provides a
        /// communication channel between processes, allowing data to be sent from the server to a connected
        /// client.</remarks>
        private AnonymousPipeServerStream? _logServer;

        /// <summary>
        /// Provides ECDH-based encryption for the main command/response pipe communication.
        /// </summary>
        /// <remarks>This encryption instance is used to encrypt commands sent to the client and decrypt
        /// responses received from the client, ensuring secure communication across different security contexts.</remarks>
        private ServerPipeEncryption? _ioEncryption;

        /// <summary>
        /// Provides ECDH-based encryption for the log pipe communication.
        /// </summary>
        /// <remarks>This separate encryption instance is used for the log channel to allow independent
        /// encrypted communication for logging purposes.</remarks>
        private ServerPipeEncryption? _logEncryption;

        /// <summary>
        /// Represents an asynchronous operation that retrieves the result of a client process.
        /// </summary>
        /// <remarks>The task encapsulates the execution of a client process and provides access to its
        /// result, which may be null if the process does not produce a result or fails.</remarks>
        private ProcessHandle? _clientProcess;

        /// <summary>
        /// Represents the <see cref="CancellationTokenSource"/> used to manage cancellation tokens for asynchronous
        /// operations.
        /// </summary>
        /// <remarks>This field is initialized as a new instance of <see cref="CancellationTokenSource"/>
        /// and is intended for internal use to signal cancellation of tasks or operations. It is not exposed
        /// publicly.</remarks>
        private CancellationTokenSource? _clientProcessCts;

        /// <summary>
        /// Represents the task responsible for writing log entries asynchronously.
        /// </summary>
        /// <remarks>This field holds a reference to the current logging task, if one is active. It may
        /// be null if no logging operation is in progress.</remarks>
        private Task? _logWriterTask;

        /// <summary>
        /// Provides a mechanism to cancel the ongoing log writer task.
        /// </summary>
        /// <remarks>This field is used internally to signal cancellation for the log writer task. It is
        /// initialized as a new instance of <see cref="CancellationTokenSource"/>.</remarks>
        private CancellationTokenSource? _logWriterTaskCts;

        /// <summary>
        /// Indicates whether the object has been disposed.
        /// </summary>
        /// <remarks>This field is used internally to track the disposal state of the object. It should
        /// not be accessed directly outside of the class.</remarks>
        private bool _disposed;

        /// <summary>
        /// Represents the file path of the assembly named "PSADT.ClientServer.Client.exe" currently loaded in the
        /// application domain.
        /// </summary>
        /// <remarks>This field retrieves the location of the first loaded assembly in the current
        /// application domain whose file name ends with "PSADT.ClientServer.Client.exe". It is intended for internal use
        /// only.</remarks>
        private static readonly string _assemblyLocation = typeof(ServerInstance).Assembly.Location.Replace("Server.dll", "Client.exe");
    }
}
