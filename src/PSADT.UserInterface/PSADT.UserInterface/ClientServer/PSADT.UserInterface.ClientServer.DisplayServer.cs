using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using PSADT.Execution;
using PSADT.TerminalServices;
using PSADT.UserInterface.DialogOptions;
using PSADT.UserInterface.DialogResults;
using PSADT.UserInterface.Dialogs;
using PSADT.UserInterface.Utilities;

namespace PSADT.UserInterface.ClientServer
{
    /// <summary>
    /// Provides functionality for inter-process communication between a server and a client using anonymous pipes.
    /// </summary>
    /// <remarks>The <see cref="DisplayServer"/> class facilitates communication between a server and a client
    /// process through anonymous pipes. It manages the lifecycle of the client process, handles input and output
    /// streams, and provides methods to send commands and retrieve responses. This class implements <see
    /// cref="IDisposable"/> to ensure proper cleanup of resources. <para> Typical usage involves creating an instance
    /// of <see cref="DisplayServer"/>, calling <see cref="Open"/> to initialize the client-server communication, and
    /// using <see cref="Invoke(ClientServerCommandType)"/> to send commands to the client. Once the communication is
    /// complete, the <see cref="Dispose"/> method should be called to release resources. </para></remarks>
    public class DisplayServer : IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DisplayServer"/> class, setting up inter-process communication
        /// using anonymous pipes.
        /// </summary>
        /// <remarks>This constructor creates anonymous pipe streams for input and output communication.
        /// The input stream is configured for reading, while the output stream is configured for writing. The output
        /// stream is set to automatically flush data to ensure timely communication.</remarks>
        public DisplayServer(SessionInfo user)
        {
            // Initialize the anonymous pipe streams for inter-process communication.
            _user = user ?? throw new ArgumentNullException(nameof(user), "User cannot be null.");
            _outputPipeServer = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.Inheritable);
            _inputPipeServer = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable);
            _outputStreamWriter = new StreamWriter(_outputPipeServer) { AutoFlush = true };
            _inputStreamReader = new StreamReader(_inputPipeServer);
        }

        /// <summary>
        /// Opens the client-server communication by starting the client process and initializing the connection.
        /// </summary>
        /// <remarks>This method launches the client process and establishes communication through
        /// inter-process pipes. It ensures that the client process is ready to receive commands before
        /// returning.</remarks>
        /// <exception cref="InvalidOperationException">Thrown if the client process fails to respond to the initial command, indicating that it is not properly
        /// initialized.</exception>
        public void Open()
        {
            // Start the server to listen for incoming connections and process data.
            _clientProcess = ProcessManager.LaunchAsync(new ProcessLaunchInfo(
                _assemblyLocation,
                ["/ClientServer", "-InputPipe", _outputPipeServer.GetClientHandleAsString(), "-OutputPipe", _inputPipeServer.GetClientHandleAsString()],
                null,
                _user.NTAccount,
                false,
                false,
                false,
                null,
                true,
                true,
                Encoding.UTF8,
                ProcessWindowStyle.Hidden,
                null,
                _cancellationTokenSource.Token,
                false
            ));
            _inputPipeServer.DisposeLocalCopyOfClientHandle();
            _outputPipeServer.DisposeLocalCopyOfClientHandle();

            // Confirm the client starts and is ready to receive commands.
            if (!(IsRunning = Invoke("Open")))
            {
                throw new InvalidOperationException("The opened client process is not properly responding to commands.");
            }
        }

        /// <summary>
        /// Closes the connection to the server.
        /// </summary>
        /// <remarks>This method attempts to close the connection by invoking the appropriate command. 
        /// Ensure that the connection is open before calling this method to avoid unexpected behavior.</remarks>
        /// <returns><see langword="true"/> if the connection was successfully closed; otherwise, <see langword="false"/>.</returns>
        private void Close()
        {
            if (IsRunning = !Invoke("Close"))
            {
                throw new InvalidOperationException("The opened client process did not properly respond to the close command.");
            }
        }

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
            _outputStreamWriter.WriteLine($"ShowModalDialog|{dialogType}|{dialogStyle}|{SerializationUtilities.SerializeToString(options)}");
            return SerializationUtilities.DeserializeFromString<TResult>(ReadInput());
        }

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
        protected virtual void Dispose(bool disposing)
        {
            // Check we're not already done.
            if (_disposed)
            {
                return;
            }

            // Tear down this object.
            if (disposing)
            {
                // The client is still running. Kill it and wait for it to die.
                if (null != _clientProcess && !_clientProcess.Task.IsCompleted)
                {
                    // Close it gracefully if we can.
                    try
                    {
                        Close();
                    }
                    catch
                    {
                        // We couldn't, so terminate the process.
                        _cancellationTokenSource.Cancel();
                    }
                    _clientProcess.Task.GetAwaiter().GetResult();
                    _clientProcess = null;
                    _cancellationTokenSource.Dispose();
                    _cancellationTokenSource = null!;
                }

                // Kill all input.
                _inputStreamReader.Dispose();
                _inputStreamReader = null!;
                _inputPipeServer.Dispose();
                _inputPipeServer = null!;

                // Kill all output.
                _outputStreamWriter.Dispose();
                _outputStreamWriter = null!;
                _outputPipeServer.Dispose();
                _outputPipeServer = null!;
            }
            _disposed = true;
        }

        /// <summary>
        /// Sends a command to the server and retrieves the server's response as a boolean value.
        /// </summary>
        /// <param name="command">The command to be sent to the server.</param>
        /// <returns><see langword="true"/> if the server's response indicates success; otherwise, <see langword="false"/>.</returns>
        private bool Invoke(string command)
        {
            _outputStreamWriter.WriteLine(command);
            return bool.Parse(ReadInput());
        }

        /// <summary>
        /// Reads a line of input from the underlying input stream.
        /// </summary>
        /// <returns>The next line of input from the stream as a <see cref="string"/>.</returns>
        /// <exception cref="InvalidDataException">Thrown if the input stream is unexpectedly closed or no data is available to read.</exception>
        private string ReadInput()
        {
            var response = _inputStreamReader.ReadLine();
            if (string.IsNullOrWhiteSpace(response))
            {
                throw new InvalidDataException("The display client shut down outside of our control.");
            }
            if (response.StartsWith("Error|", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(response.Substring(6));
            }
            return response;
        }

        /// <summary>
        /// Gets a value indicating whether the process is currently running.
        /// </summary>
        public bool IsRunning { get; private set; }

        /// <summary>
        /// Indicates whether the object has been disposed.
        /// </summary>
        /// <remarks>This field is used internally to track the disposal state of the object. It should
        /// not be accessed directly outside of the class.</remarks>
        private bool _disposed;

        /// <summary>
        /// Represents a server-side anonymous pipe stream for reading data.
        /// </summary>
        /// <remarks>This pipe stream is initialized with an input direction and inheritable handle
        /// settings, allowing it to be used for inter-process communication where the handle can be passed to a child
        /// process.</remarks>
        private AnonymousPipeServerStream _inputPipeServer;

        /// <summary>
        /// Represents the server side of an anonymous pipe used for interprocess communication.
        /// </summary>
        /// <remarks>This pipe server is initialized with an output direction and allows the handle to be
        /// inherited by child processes. It is typically used to send data from the current process to another
        /// process.</remarks>
        private AnonymousPipeServerStream _outputPipeServer;

        /// <summary>
        /// Represents the <see cref="StreamReader"/> used to read input data from a stream.
        /// </summary>
        /// <remarks>This field is read-only and is intended for internal use to process input streams.</remarks>
        private StreamReader _inputStreamReader;

        /// <summary>
        /// Represents the output stream writer used for writing data to a stream.
        /// </summary>
        /// <remarks>This field is read-only and is intended to be used internally for managing output operations.</remarks>
        private StreamWriter _outputStreamWriter;

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
        private CancellationTokenSource _cancellationTokenSource = new();

        /// <summary>
        /// Represents the session information for the current user.
        /// </summary>
        /// <remarks>This field stores details about the user's session, such as authentication or
        /// user-specific data. It is intended for internal use and should not be exposed directly to external
        /// consumers.</remarks>
        private readonly SessionInfo _user;

        /// <summary>
        /// Represents the file path of the assembly named "PSADT.UserInterface.exe" currently loaded in the
        /// application domain.
        /// </summary>
        /// <remarks>This field retrieves the location of the first loaded assembly in the current
        /// application domain  whose file name ends with "PSADT.UserInterface.exe". It is intended for internal use
        /// only.</remarks>
        private static readonly string _assemblyLocation = typeof(DisplayServer).Assembly.Location;
    }
}
