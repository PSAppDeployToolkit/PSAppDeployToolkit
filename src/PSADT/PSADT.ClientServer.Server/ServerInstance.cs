using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PSADT.LibraryInterfaces;
using PSADT.Module;
using PSADT.ProcessManagement;
using PSADT.Types;
using PSADT.UserInterface.DialogOptions;
using PSADT.UserInterface.DialogResults;
using PSADT.UserInterface.Dialogs;
using PSADT.WindowManagement;

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
    public sealed class ServerInstance : IDisposable
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
            // Initialize the anonymous pipe streams for inter-process communication.
            RunAsActiveUser = runAsActiveUser ?? throw new ArgumentNullException("User cannot be null.", (Exception?)null);
            _outputServer = new(PipeDirection.Out, HandleInheritability.Inheritable);
            _inputServer = new(PipeDirection.In, HandleInheritability.Inheritable);
            _logServer = new(PipeDirection.In, HandleInheritability.Inheritable);
            _outputWriter = new(_outputServer, Encoding.UTF8);
            _inputReader = new(_inputServer, Encoding.UTF8);
            _logReader = new(_logServer, Encoding.UTF8);
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
            // Start the server to listen for incoming connections and process data.
            try
            {
                _clientProcess = ProcessManager.LaunchAsync(new(
                    _assemblyLocation,
                    ["/ClientServer", "-InputPipe", _outputServer.GetClientHandleAsString(), "-OutputPipe", _inputServer.GetClientHandleAsString(), "-LogPipe", _logServer.GetClientHandleAsString()],
                    Environment.SystemDirectory,
                    RunAsActiveUser,
                    UseLinkedAdminToken,
                    UseHighestAvailableToken,
                    denyUserTermination: true,
                    inheritHandles: true,
                    createNoWindow: true,
                    waitForChildProcesses: true,
                    killChildProcessesWithParent: true,
                    streamEncoding: Encoding.UTF8,
                    windowStyle: ProcessWindowStyle.Hidden,
                    cancellationToken: (_clientProcessCts = new()).Token
                ));
            }
            finally
            {
                _outputServer.DisposeLocalCopyOfClientHandle();
                _inputServer.DisposeLocalCopyOfClientHandle();
                _logServer.DisposeLocalCopyOfClientHandle();
            }

            // Confirm the client starts and is ready to receive commands.
            bool? opened = null;
            try
            {
                if (!(opened = Invoke<bool>("Open")).Value)
                {
                    throw new ApplicationException("The opened client process is not properly responding to commands.");
                }
            }
            catch (InvalidDataException ex) when (null == _clientProcess?.Task)
            {
                throw new ApplicationException("The opened client process is not properly responding to commands.", ex);
            }
            finally
            {
                if (opened is null || !opened.Value)
                {
                    Close(true);
                }
            }

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
            // Confirm that the server instance is open and has not been closed already.
            if (null == _clientProcessCts || null == _clientProcess)
            {
                throw new InvalidOperationException("The server instance is not open or has already been closed.");
            }

            // Ensure the client process closes no matter what.
            bool? closed = null;
            try
            {
                if (!force && IsRunning && !(closed = Invoke<bool>("Close")).Value)
                {
                    throw new ApplicationException("The opened client process did not properly respond to the close command.");
                }
            }
            finally
            {
                // Close the log writer and wait for it to finish.
                if (null != _logWriterTaskCts && null != _logWriterTask)
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

                // Close the client process and wait for it to exit.
                if (closed is null || !closed.Value)
                {
                    _clientProcessCts.Cancel();
                }
                try
                {
                    _clientProcess.Task.GetAwaiter().GetResult();
                }
                catch (TaskCanceledException)
                {
                    // The client process task was canceled, which is expected when closing the server instance.
                }
                finally
                {
                    _clientProcess.Task.Dispose();
                    _clientProcess.Process.Dispose();
                    _clientProcess = null;
                    _clientProcessCts.Dispose();
                    _clientProcessCts = null;
                }
            }
        }

        /// <summary>
        /// Closes the current connection and releases associated resources.
        /// </summary>
        /// <remarks>This method closes the connection and optionally performs additional cleanup
        /// operations depending on the internal implementation. Once closed, the connection cannot be reused and must
        /// be reopened if needed.</remarks>
        public void Close() => Close(false);

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
            _logSource = "Show-ADTInstallationWelcome";
            return Invoke<bool>($"InitCloseAppsDialog{(closeProcesses is not null ? $"{CommonUtilities.ArgumentSeparator}{DataSerialization.SerializeToString(closeProcesses)}" : null)}");
        }

        /// <summary>
        /// Prompts the user to close any running applications that may interfere with the installation process.
        /// </summary>
        /// <remarks>This method invokes a prompt to the user and returns their response. Ensure that the
        /// environment allows user interaction before calling this method.</remarks>
        /// <returns><see langword="true"/> if the user agrees to close the applications; otherwise, <see langword="false"/>.</returns>
        public bool PromptToCloseApps(TimeSpan promptToCloseTimeout)
        {
            _logSource = "Show-ADTInstallationWelcome";
            return Invoke<bool>($"PromptToCloseApps{CommonUtilities.ArgumentSeparator}{promptToCloseTimeout}");
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
        public CloseAppsDialogResult ShowCloseAppsDialog(DialogStyle dialogStyle, CloseAppsDialogOptions options) => ShowModalDialog<CloseAppsDialogResult, CloseAppsDialogOptions>(DialogType.CloseAppsDialog, dialogStyle, options);

        /// <summary>
        /// Displays a custom dialog with the specified style and options, and returns the user's input as a string.
        /// </summary>
        /// <remarks>Use this method to display a modal input dialog to the user. The dialog's behavior
        /// and appearance are determined by the provided <paramref name="dialogStyle"/> and <paramref
        /// name="options"/>.</remarks>
        /// <param name="dialogStyle">The style of the dialog, which determines its appearance and behavior.</param>
        /// <param name="options">The options to configure the dialog, such as title, message, and input settings.</param>
        /// <returns>The user's input as a string, or <see langword="null"/> if the dialog is canceled.</returns>
        public string ShowCustomDialog(DialogStyle dialogStyle, CustomDialogOptions options) => ShowModalDialog<string, CustomDialogOptions>(DialogType.CustomDialog, dialogStyle, options);

        /// <summary>
        /// Displays an input dialog to the user and returns the result of the interaction.
        /// </summary>
        /// <remarks>Use this method to present a modal input dialog to the user. The dialog's behavior
        /// and appearance can be customized using the <paramref name="dialogStyle"/> and <paramref name="options"/>
        /// parameters.</remarks>
        /// <param name="dialogStyle">The style of the dialog, which determines its appearance and behavior.</param>
        /// <param name="options">The options to configure the input dialog, such as the prompt text, default value, and validation rules.</param>
        /// <returns>An <see cref="InputDialogResult"/> object containing the user's input and the dialog's outcome.</returns>
        public InputDialogResult ShowInputDialog(DialogStyle dialogStyle, InputDialogOptions options) => ShowModalDialog<InputDialogResult, InputDialogOptions>(DialogType.InputDialog, dialogStyle, options);

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
        public string ShowRestartDialog(DialogStyle dialogStyle, RestartDialogOptions options) => ShowModalDialog<string, RestartDialogOptions>(DialogType.RestartDialog, dialogStyle, options);

        /// <summary>
        /// Displays a modal dialog box with the specified options, and returns the result of the dialog interaction.
        /// </summary>
        /// <remarks>Use this method to display a modal dialog box that requires user input or
        /// confirmation. The dialog box will block the calling thread until the user closes it, and the result will
        /// indicate the user's action (e.g., OK, Cancel).</remarks>
        /// <param name="options">The options to configure the dialog box, such as title, message, and input fields.</param>
        /// <returns>A <see cref="DialogBoxResult"/> that represents the result of the user's interaction with the dialog box.</returns>
        public DialogBoxResult ShowDialogBox(DialogBoxOptions options) => ShowModalDialog<DialogBoxResult, DialogBoxOptions>(DialogType.DialogBox, 0, options);

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
            _logSource = "Show-ADTInstallationProgress";
            return Invoke<bool>($"ShowProgressDialog{CommonUtilities.ArgumentSeparator}{dialogStyle}{CommonUtilities.ArgumentSeparator}{DataSerialization.SerializeToString(options)}");
        }

        /// <summary>
        /// Determines whether the progress dialog is currently open.
        /// </summary>
        /// <remarks>This method checks the state of the progress dialog and returns a boolean value
        /// indicating whether it is currently displayed to the user.</remarks>
        /// <returns><see langword="true"/> if the progress dialog is open; otherwise, <see langword="false"/>.</returns>
        public bool ProgressDialogOpen() => Invoke<bool>("ProgressDialogOpen");

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
            _logSource = "Show-ADTInstallationProgress";
            return Invoke<bool>($"UpdateProgressDialog{CommonUtilities.ArgumentSeparator}{(!string.IsNullOrWhiteSpace(progressMessage) ? progressMessage : ' ')}{CommonUtilities.ArgumentSeparator}{(!string.IsNullOrWhiteSpace(progressDetailMessage) ? progressDetailMessage : ' ')}{CommonUtilities.ArgumentSeparator}{((progressPercentage is not null) ? progressPercentage.ToString() : ' ')}{CommonUtilities.ArgumentSeparator}{((messageAlignment is not null) ? messageAlignment.ToString() : ' ')}");
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
            _logSource = "Close-ADTInstallationProgress";
            return Invoke<bool>("CloseProgressDialog");
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
            _logSource = "Show-ADTBalloonTip";
            return Invoke<bool>($"ShowBalloonTip{CommonUtilities.ArgumentSeparator}{DataSerialization.SerializeToString(options)}");
        }

        /// <summary>
        /// Minimizes all open windows on the desktop.
        /// </summary>
        /// <remarks>This method attempts to minimize all currently open windows. The return value
        /// indicates whether the operation was successful. Note that the success of this operation may depend on
        /// system permissions or the current state of the desktop environment.</remarks>
        /// <returns><see langword="true"/> if the operation succeeds; otherwise, <see langword="false"/>.</returns>
        public bool MinimizeAllWindows() => Invoke<bool>("MinimizeAllWindows");

        /// <summary>
        /// Restores all minimized or hidden windows to their original state.
        /// </summary>
        /// <remarks>This method attempts to restore all windows that were previously minimized or hidden.
        /// The return value indicates whether the operation was successful for all windows.</remarks>
        /// <returns><see langword="true"/> if all windows were successfully restored; otherwise, <see langword="false"/>.</returns>
        public bool RestoreAllWindows() => Invoke<bool>("RestoreAllWindows");

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
            _logSource = "Send-ADTKeys";
            return Invoke<bool>($"SendKeys{CommonUtilities.ArgumentSeparator}{DataSerialization.SerializeToString(options)}");
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
            _logSource = "Get-ADTWindowTitle";
            return Invoke<ReadOnlyCollection<WindowInfo>>($"GetProcessWindowInfo{CommonUtilities.ArgumentSeparator}{DataSerialization.SerializeToString(options)}");
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
            _logSource = "Refresh-ADTDesktopAndEnvironmentVariables";
            return Invoke<bool>("RefreshDesktopAndEnvironmentVariables");
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
            _logSource = "Get-ADTUserNotificationState";
            return Invoke<QUERY_USER_NOTIFICATION_STATE>("GetUserNotificationState");
        }

        /// <summary>
        /// Retrieves the process ID of the application that currently owns the foreground window.
        /// </summary>
        /// <remarks>This method sends a command to query the foreground window's process ID and parses
        /// the result. The returned process ID can be used to identify the application currently in focus.</remarks>
        /// <returns>The process ID of the application that owns the foreground window.</returns>
        public uint GetForegroundWindowProcessId()
        {
            _logSource = "Get-ADTForegroundWindowProcessId";
            return Invoke<uint>("GetForegroundWindowProcessId");
        }

        /// <summary>
        /// Retrieves the value of a specified environment variable.
        /// </summary>
        /// <param name="variable"></param>
        /// <returns></returns>
        public string? GetEnvironmentVariable(string variable)
        {
            _logSource = "Get-ADTEnvironmentVariable";
            return Invoke<string?>($"GetEnvironmentVariable{CommonUtilities.ArgumentSeparator}{variable}");
        }

        /// <summary>
        /// Sets the value of a specified environment variable.
        /// </summary>
        /// <param name="variable"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool SetEnvironmentVariable(string variable, string value)
        {
            _logSource = "Set-ADTEnvironmentVariable";
            return Invoke<bool>($"SetEnvironmentVariable{CommonUtilities.ArgumentSeparator}{variable}{CommonUtilities.ArgumentSeparator}{value}");
        }

        /// <summary>
        /// Removes a specified environment variable for the user.
        /// </summary>
        /// <param name="variable"></param>
        /// <returns></returns>
        public bool RemoveEnvironmentVariable(string variable)
        {
            _logSource = "Remove-ADTEnvironmentVariable";
            return Invoke<bool>($"RemoveEnvironmentVariable{CommonUtilities.ArgumentSeparator}{variable}");
        }

        /// <summary>
        /// Retrieves the exception, if any, that occurred during the execution of the log writer task.
        /// </summary>
        /// <returns>An <see cref="AggregateException"/> containing the exceptions thrown by the log writer task,  or <see
        /// langword="null"/> if no exception occurred or the task has not been initialized.</returns>
        public AggregateException? GetLogWriterException() => _logWriterTask?.Exception;

        /// <summary>
        /// Retrieves the result of the client process task.
        /// </summary>
        /// <remarks>This method blocks the current thread if <paramref name="iKnowWhatImDoing"/> is <see
        /// langword="true"/>. Use with caution, as blocking the thread can lead to deadlocks or performance issues in
        /// asynchronous environments.</remarks>
        /// <param name="iKnowWhatImDoing">A value indicating whether the caller understands the risks of blocking the current thread. If <see
        /// langword="true"/>, the method will synchronously wait for the client process task to complete.</param>
        /// <returns>The result of the client process task if <paramref name="iKnowWhatImDoing"/> is <see langword="true"/>; 
        /// otherwise, <see langword="null"/>.</returns>
        public ProcessResult GetClientProcessResult(bool iKnowWhatImDoing) => iKnowWhatImDoing ? _clientProcess!.Task.GetAwaiter().GetResult() : null!;

        /// <summary>
        /// Releases the resources used by the current instance of the class.
        /// </summary>
        /// <remarks>This method should be called when the instance is no longer needed to free up
        /// resources. It suppresses the finalization of the object to optimize garbage collection.</remarks>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
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

            // Tear down this object.
            if (disposing)
            {
                // Close the client process if it is running.
                if (null != _clientProcess)
                {
                    Close();
                }

                // Kill all input.
                _inputReader.Dispose();
                _inputReader = null!;
                _inputServer.Dispose();
                _inputServer = null!;

                // Kill all output.
                _outputWriter.Dispose();
                _outputWriter = null!;
                _outputServer.Dispose();
                _outputServer = null!;

                // Kill all logging.
                _logReader.Dispose();
                _logReader = null!;
                _logServer.Dispose();
                _logServer = null!;
            }
            _disposed = true;
        }

        /// <summary>
        /// Displays a modal dialog of the specified type and style, passing the provided options, and returns the
        /// result.
        /// </summary>
        /// <remarks>The method serializes the provided <paramref name="options"/> and sends them to the
        /// dialog system. The result is deserialized into the specified type <typeparamref name="TResult"/>.</remarks>
        /// <typeparam name="TResult">The type of the result returned by the dialog.</typeparam>
        /// <typeparam name="TOptions">The type of the options passed to the dialog.</typeparam>
        /// <param name="dialogType">The type of the dialog to display.</param>
        /// <param name="dialogStyle">The style of the dialog to display.</param>
        /// <param name="options">The options to configure the dialog. This parameter cannot be null.</param>
        /// <returns>The result of the dialog, deserialized to the specified type <typeparamref name="TResult"/>.</returns>
        private TResult ShowModalDialog<TResult, TOptions>(DialogType dialogType, DialogStyle dialogStyle, TOptions options)
        {
            _logSource = dialogType switch
            {
                DialogType.CloseAppsDialog => "Show-ADTInstallationWelcome",
                DialogType.CustomDialog => "Show-ADTInstallationPrompt",
                DialogType.DialogBox => "Show-ADTDialogBox",
                DialogType.InputDialog => "Show-ADTInstallationPrompt",
                DialogType.ProgressDialog => "Show-ADTInstallationProgress",
                DialogType.RestartDialog => "Show-ADTInstallationRestartPrompt",
                _ => throw new ArgumentOutOfRangeException(nameof(dialogType), $"Unsupported dialog type: {dialogType}"),
            };
            return Invoke<TResult>($"ShowModalDialog{CommonUtilities.ArgumentSeparator}{dialogType}{CommonUtilities.ArgumentSeparator}{dialogStyle}{CommonUtilities.ArgumentSeparator}{DataSerialization.SerializeToString(options)}");
        }

        /// <summary>
        /// Executes the specified command and deserializes the result into an object of type <typeparamref name="T"/>.
        /// </summary>
        /// <remarks>This method writes the provided command, processes the result, and deserializes it
        /// into the specified type. Ensure that the command produces a result that can be successfully deserialized
        /// into the expected type.</remarks>
        /// <typeparam name="T">The type of the object to deserialize the result into.</typeparam>
        /// <param name="command">The command to execute. Cannot be null or empty.</param>
        /// <returns>An object of type <typeparamref name="T"/> representing the deserialized result of the command execution.</returns>
        private T Invoke<T>(string command)
        {
            // Send the command off to the client.
            try
            {
                _outputWriter.Write(command);
                _outputWriter.Flush();
            }
            catch (IOException ex)
            {
                throw new InvalidDataException("An error occurred while writing to the output stream.", ex);
            }

            // Read the client's response.
            string response;
            try
            {
                response = _inputReader.ReadString();
            }
            catch (EndOfStreamException ex)
            {
                throw new InvalidDataException("The input stream was unexpectedly closed or no data is available to read.", ex);
            }
            catch (IOException ex)
            {
                throw new InvalidDataException("An error occurred while reading from the input stream.", ex);
            }

            // If the response is an error, rethrow it. Otherwise, deserialize the response.
            if (response.StartsWith($"Error{CommonUtilities.ArgumentSeparator}"))
            {
                throw new ServerException("The client process returned an exception.", DataSerialization.DeserializeFromString<Exception>(response.Substring(6)));
            }
            return DataSerialization.DeserializeFromString<T>(response);
        }

        /// <summary>
        /// Reads and processes log entries from the underlying log stream.
        /// </summary>
        /// <remarks>This method reads each line from the log stream until the end of the stream is
        /// reached. Non-empty and non-whitespace lines are processed as needed.</remarks>
        private void ReadLog()
        {
            // Read the log stream until cancellation is requested or the end of the stream is reached.
            while (!_logWriterTaskCts!.IsCancellationRequested)
            {
                try
                {
                    // Only log the message if a deployment session is active.
                    if (_logReader.ReadString() is string line && ModuleDatabase.IsDeploymentSessionActive())
                    {
                        // Test the line for a log severity.
                        if (line.Contains(CommonUtilities.ArgumentSeparator.ToString()))
                        {
                            var parts = line.Split(CommonUtilities.ArgumentSeparator);
                            ModuleDatabase.GetDeploymentSession().WriteLogEntry(parts[1].Trim(), (LogSeverity)int.Parse(parts[0]), _logSource);
                        }
                        else
                        {
                            ModuleDatabase.GetDeploymentSession().WriteLogEntry(line.Trim(), _logSource);
                        }
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
                catch (IOException ex)
                {
                    // Some kind of read issue occurred that was unexpected.
                    throw new InvalidDataException("An error occurred while reading from the log stream.", ex);
                }
            }
        }

        /// <summary>
        /// Represents the session information for the current user.
        /// </summary>
        /// <remarks>This field stores details about the user's session, such as authentication or
        /// user-specific data. It is intended for internal use and should not be exposed directly to external
        /// consumers.</remarks>
        public readonly RunAsActiveUser RunAsActiveUser;

        /// <summary>
        /// Gets a value indicating whether the process is currently running.
        /// </summary>
        public bool IsRunning => null != _clientProcess && !_clientProcess.Process.HasExited;

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
        /// Indicates whether the object has been disposed.
        /// </summary>
        /// <remarks>This field is used internally to track the disposal state of the object. It should
        /// not be accessed directly outside of the class.</remarks>
        private bool _disposed;

        /// <summary>
        /// Represents the server side of an anonymous pipe used for inter-process communication.
        /// </summary>
        /// <remarks>This field is used to manage the server stream for logging purposes. It provides a
        /// communication channel between processes, allowing data to be sent from the server to a connected
        /// client.</remarks>
        private AnonymousPipeServerStream _logServer;

        /// <summary>
        /// Represents a server-side anonymous pipe stream for reading data.
        /// </summary>
        /// <remarks>This pipe stream is initialized with an input direction and inheritable handle
        /// settings, allowing it to be used for inter-process communication where the handle can be passed to a child
        /// process.</remarks>
        private AnonymousPipeServerStream _inputServer;

        /// <summary>
        /// Represents the server side of an anonymous pipe used for interprocess communication.
        /// </summary>
        /// <remarks>This pipe server is initialized with an output direction and allows the handle to be
        /// inherited by child processes. It is typically used to send data from the current process to another
        /// process.</remarks>
        private AnonymousPipeServerStream _outputServer;

        /// <summary>
        /// Represents the stream reader used to read log data.
        /// </summary>
        /// <remarks>This field is intended for internal use and provides access to the underlying stream
        /// for reading log information. It is not exposed publicly.</remarks>
        private BinaryReader _logReader;

        /// <summary>
        /// Represents the <see cref="StreamReader"/> used to read input data from a stream.
        /// </summary>
        /// <remarks>This field is read-only and is intended for internal use to process input streams.</remarks>
        private BinaryReader _inputReader;

        /// <summary>
        /// Represents the output stream writer used for writing data to a stream.
        /// </summary>
        /// <remarks>This field is read-only and is intended to be used internally for managing output operations.</remarks>
        private BinaryWriter _outputWriter;

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
        /// Represents the source identifier for logging related to the "Show-ADTModalDialog" functionality.
        /// </summary>
        /// <remarks>This constant is used to tag log entries originating from the "Show-ADTModalDialog"
        /// feature. It is intended for internal use and helps in categorizing and filtering logs.</remarks>
        private static string _logSource = null!;

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
