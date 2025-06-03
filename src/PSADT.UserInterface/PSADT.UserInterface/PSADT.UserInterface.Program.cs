using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using PSADT.UserInterface.DialogOptions;
using PSADT.UserInterface.Dialogs;
using PSADT.UserInterface.Utilities;

namespace PSADT.UserInterface
{
    /// <summary>
    /// Provides the main entry point for the PSAppDeployToolkit User Interface Display Server application.
    /// </summary>
    /// <remarks>This application is designed to be used in conjunction with the PSAppDeployToolkit PowerShell
    /// module and should not be directly invoked by end-users. It processes command-line arguments to display various
    /// types of dialogs based on the provided options. If no arguments are supplied, the application displays an error
    /// message and exits with an appropriate exit code. The application also validates the provided arguments and
    /// ensures they conform to the expected format before executing the requested operation.</remarks>
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static int Main(string[] args)
        {
            // Detect what mode the executable has been asked to run in.
            try
            {
                // Determine the mode of operation based on the provided arguments.
                if (null == args || args.Length == 0)
                {
                    ShowHelpDialog();
                }
                else if (args.Any(static arg => arg.Equals("/SingleDialog")))
                {
                    EnterSingleUseMode(ConvertArgsToDictionary(args));
                }
                else if (args.Any(static arg => arg.Equals("/ClientServer")))
                {
                    EnterClientServerMode(ConvertArgsToDictionary(args));
                }
                else
                {
                    throw new ProgramException("The specified arguments were unable to be resolved into a type of operation.", ExitCode.InvalidMode);
                }
            }
            catch (ProgramException ex)
            {
                // We've caught our own error. Write it out and exit with its code.
                Console.Error.WriteLine(ex.Message);
                Environment.Exit(ex.HResult);
            }
            catch (Exception ex)
            {
                // This block is here as a fail-safe and should never be reached.
                Console.Error.WriteLine($"An unknown error has occurred: {ex.Message}");
                Environment.Exit((int)ExitCode.Unknown);
            }

            // If we're here, everything went well.
            return (int)ExitCode.Success;
        }

        /// <summary>
        /// Displays a help dialog with information about the application and its usage.
        /// </summary>
        /// <remarks>This method shows a dialog box containing the application's version, copyright
        /// information,  and a message indicating that the application is intended to be used with the
        /// PSAppDeployToolkit  PowerShell module. It also advises end-users to contact their helpdesk for assistance. 
        /// After displaying the dialog, the method throws a <see cref="ProgramException"/> to indicate  that no
        /// arguments were provided to the application.</remarks>
        /// <exception cref="ProgramException">Thrown to indicate that no arguments were provided to the application.</exception>
        private static void ShowHelpDialog()
        {
            var fileInfo = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
            var helpVersion = fileInfo.ProductVersion!.Split('+')[0];
            var helpTitle = $"{fileInfo.FileDescription!} {helpVersion}";
            var helpMessage = string.Join(Environment.NewLine, new[]
            {
                helpTitle,
                "",
                fileInfo.LegalCopyright,
                "",
                "This application is designed to be used with the PSAppDeployToolkit PowerShell module and should not be directly invoked.",
                "",
                "If you're an end-user or employee of your organization, please report this message to your helpdesk for further assistance.",
            });
            DialogManager.ShowDialogBox(helpTitle, helpMessage, DialogBoxButtons.Ok, DialogBoxDefaultButton.First, DialogBoxIcon.Stop, true, default);
            throw new ProgramException("No arguments were provided to the display server.", ExitCode.NoArguments);
        }

        /// <summary>
        /// Converts an array of command-line arguments into a read-only dictionary of key-value pairs.
        /// </summary>
        /// <remarks>Each key in the input must start with a hyphen ('-'), and its value must immediately
        /// follow as a separate argument.  If a key is not followed by a valid value (e.g., null, empty, or another
        /// key-like argument), the method writes an  error message to the standard error stream and terminates the
        /// application with an exit code indicating invalid arguments.</remarks>
        /// <param name="args">An array of strings representing command-line arguments. Each key must be prefixed with a hyphen ('-')  and
        /// followed by its corresponding value as a separate argument.</param>
        /// <returns>A <see cref="ReadOnlyDictionary{TKey, TValue}"/> containing the parsed key-value pairs from the input
        /// arguments.</returns>
        private static ReadOnlyDictionary<string, string> ConvertArgsToDictionary(string[] args)
        {
            // Loop through arguments and match argument names to their values.
            var arguments = new Dictionary<string, string>();
            for (int i = 0; i < args!.Length; i++)
            {
                if (!args[i].StartsWith("-"))
                {
                    continue;
                }
                var key = args[i].Substring(1).Trim();
                var value = (i + 1 < args.Length) ? args[i + 1].Trim() : null;
                if (null == value || string.IsNullOrWhiteSpace(value) || value!.StartsWith("-") || value!.StartsWith("/"))
                {
                    throw new ProgramException($"The argument [{args[i]}] has an invalid value.", ExitCode.InvalidArguments);
                }
                arguments.Add(key, value);
            }

            // This data should never change once read, so return read-only.
            return new ReadOnlyDictionary<string, string>(arguments);
        }

        /// <summary>
        /// Displays a single dialog based on the specified arguments.
        /// </summary>
        /// <remarks>This method validates the provided arguments and displays the appropriate dialog
        /// based on the specified <c>"DialogType"</c>. If any required argument is missing or invalid, the method
        /// writes an error message to the console and terminates the application with an appropriate exit code.</remarks>
        /// <param name="arguments">A read-only dictionary containing the arguments required to configure and display the dialog.  The following
        /// keys are expected: <list type="bullet"> <item> <description> <c>"DialogType"</c>: Specifies the type of
        /// dialog to display. Must be a valid <see cref="DialogType"/> value. </description> </item> <item>
        /// <description> <c>"DialogStyle"</c>: Specifies the style of the dialog. Must be a valid <see
        /// cref="DialogStyle"/> value. </description> </item> <item> <description> <c>"DialogOptions"</c>: A
        /// JSON-encoded string containing the options specific to the dialog type. </description> </item> </list></param>
        private static void EnterSingleUseMode(ReadOnlyDictionary<string, string> arguments)
        {
            // Show the dialog and write the result to the console.
            Console.WriteLine(ShowModalDialog(arguments));
        }

        /// <summary>
        /// Enters client-server mode by establishing communication through input and output pipes.
        /// </summary>
        /// <remarks>This method initializes anonymous pipe clients for input and output communication 
        /// using the provided pipe handles. If the required pipe handles are missing, invalid, or cannot be opened,
        /// the method writes an error message to the standard error stream  and terminates the process with an
        /// appropriate exit code.</remarks>
        /// <param name="arguments">A read-only dictionary containing the pipe handles required for communication. The dictionary must include
        /// the keys <c>"InputPipe"</c> and <c>"OutputPipe"</c>, each mapped to a valid, non-empty pipe handle string.</param>
        private static void EnterClientServerMode(ReadOnlyDictionary<string, string> arguments)
        {
            // Get the pipe handles from the arguments.
            if (!arguments.TryGetValue("OutputPipe", out string? outputPipeHandle) || null == outputPipeHandle || string.IsNullOrWhiteSpace(outputPipeHandle))
            {
                throw new ProgramException("The specified OutputPipe handle was null or invalid.", ExitCode.NoOutputPipe);
            }
            if (!arguments.TryGetValue("InputPipe", out string? inputPipeHandle) || null == inputPipeHandle || string.IsNullOrWhiteSpace(inputPipeHandle))
            {
                throw new ProgramException("The specified InputPipe handle was null or invalid.", ExitCode.NoInputPipe);
            }

            // Establish the pipe objects.
            AnonymousPipeClientStream outputPipeClient;
            AnonymousPipeClientStream inputPipeClient;
            try
            {
                outputPipeClient = new AnonymousPipeClientStream(PipeDirection.Out, outputPipeHandle);
            }
            catch (Exception ex)
            {
                throw new ProgramException($"Failed to open a pipe client for the specified OutputHandle: {ex.Message}", ex, ExitCode.InvalidOutputPipe);
            }
            try
            {
                inputPipeClient = new AnonymousPipeClientStream(PipeDirection.In, inputPipeHandle);
            }
            catch (Exception ex)
            {
                throw new ProgramException($"Failed to open a pipe client for the specified InputHandle: {ex.Message}", ex, ExitCode.InvalidInputPipe);
            }

            // Start reading data from the pipes. We only return
            // from here when the server's pipe closes on us.
            try
            {
                // These pipe streams require proper disposal.
                using (outputPipeClient)
                using (inputPipeClient)
                {
                    // Establish stream readers for incoming/outgoing data.
                    using (var outputWriter = new StreamWriter(outputPipeClient) { AutoFlush = true })
                    using (var inputReader = new StreamReader(inputPipeClient))
                    {
                        // Continuously loop until the end. When we receive null, the
                        // server has closed the pipe, so we should break and exit.
                        string? line; while ((line = inputReader.ReadLine()) != null)
                        {
                            // We never let an exception kill the pipe.
                            try
                            {
                                // Split the line on the pipe operator, it's our delimiter for args. We don't
                                // use a switch here so it's easier to break the while loop if we're exiting.
                                var parts = line.Split('|'); if (parts[0] == "ShowModalDialog")
                                {
                                    // Confirm the length of our parts showing the dialog and writing back the result.
                                    if (parts.Length != 4)
                                    {
                                        throw new ProgramException("The ShowModalDialog command requires exactly three arguments: DialogType, DialogStyle, and DialogOptions.", ExitCode.InvalidArguments);
                                    }
                                    outputWriter.WriteLine(ShowModalDialog(new Dictionary<string, string> { { "DialogType", parts[1] }, { "DialogStyle", parts[2] }, { "DialogOptions", parts[3] } }));
                                }
                                else if (parts[0] == "Open")
                                {
                                    // Write that we're good to go.
                                    outputWriter.WriteLine(true);
                                }
                                else if (parts[0] == "Close")
                                {
                                    // Indicate that we're going to terminate.
                                    outputWriter.WriteLine(true);
                                    break;
                                }
                                else
                                {
                                    // No idea what to do with whatever came through.
                                    throw new ProgramException($"The specified command [{parts[0]}] is not recognised.", ExitCode.InvalidArguments);
                                }
                            }
                            catch (Exception ex)
                            {
                                // Write the exception message and stack trace back to the caller over the pipe.
                                // We can't serialise the entire exception so this is the best we can do otherwise.
                                outputWriter.WriteLine($"Error|An unhandled exception occurred while processing line [{line}]: {ex.Message.TrimEnd('.')}. Stack trace received: {ex.StackTrace}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ProgramException($"Failed to read or write from the pipe: {ex.Message}", ex, ExitCode.PipeReadWriteError);
            }
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
        /// <returns>A JSON-serialized string representing the result of the dialog. The format and content of the result depend
        /// on the dialog type.</returns>
        /// <exception cref="ProgramException">Thrown if any of the following conditions occur: <list type="bullet"> <item><description>The
        /// <c>DialogType</c> key is missing, empty, or invalid.</description></item> <item><description>The
        /// <c>DialogStyle</c> key is missing, empty, or invalid.</description></item> <item><description>The
        /// <c>DialogOptions</c> key is missing, empty, or invalid.</description></item> <item><description>The
        /// specified <c>DialogType</c> is not supported.</description></item> </list></exception>
        private static string ShowModalDialog(IReadOnlyDictionary<string, string> arguments)
        {
            // Confirm we have a DialogType and that it's valid.
            if (!arguments.TryGetValue("DialogType", out string? dialogTypeArg) || string.IsNullOrWhiteSpace(dialogTypeArg))
            {
                throw new ProgramException("A required DialogType was not specified on the command line.", ExitCode.NoDialogType);
            }
            if (!Enum.TryParse(dialogTypeArg, true, out DialogType dialogType))
            {
                throw new ProgramException($"The specified DialogType of [{dialogTypeArg}] is invalid.", ExitCode.InvalidDialog);
            }

            // Confirm we've got a DialogStyle and that it's valid.
            if (!arguments.TryGetValue("DialogStyle", out string? dialogStyleArg) || string.IsNullOrWhiteSpace(dialogStyleArg))
            {
                throw new ProgramException("A required DialogStyle was not specified on the command line.", ExitCode.NoDialogStyle);
            }
            if (!Enum.TryParse(dialogStyleArg, true, out DialogStyle dialogStyle))
            {
                throw new ProgramException($"The specified DialogStyle of [{dialogStyleArg}] is invalid.", ExitCode.NoDialogStyle);
            }

            // Confirm we have dialog options and they're not null/invalid.
            if (!arguments.TryGetValue("DialogOptions", out string? dialogOptions))
            {
                throw new ProgramException("The required DialogOptions were not specified on the command line.", ExitCode.NoDialogOptions);
            }
            if (null == dialogOptions || string.IsNullOrWhiteSpace(dialogOptions))
            {
                throw new ProgramException($"The specified DialogOptions are null or invalid.", ExitCode.InvalidDialogOptions);
            }

            // Show the dialog and return the serialised result for the caller to handle.
            return dialogType switch
            {
                DialogType.DialogBox => SerializeDialogResult(DialogManager.ShowDialogBox(GetDialogOptions<DialogBoxOptions>(dialogOptions))),
                DialogType.InputDialog => SerializeDialogResult(DialogManager.ShowInputDialog(dialogStyle, GetDialogOptions<InputDialogOptions>(dialogOptions))),
                DialogType.CustomDialog => SerializeDialogResult(DialogManager.ShowCustomDialog(dialogStyle, GetDialogOptions<CustomDialogOptions>(dialogOptions))),
                DialogType.RestartDialog => SerializeDialogResult(DialogManager.ShowRestartDialog(dialogStyle, GetDialogOptions<RestartDialogOptions>(dialogOptions))),
                _ => throw new ProgramException($"The specified DialogType of [{dialogType}] is not supported.", ExitCode.UnsupportedDialog),
            };
        }

        /// <summary>
        /// Deserializes the provided dialog options string into an object of the specified type.
        /// </summary>
        /// <remarks>If an error occurs during deserialization, the application will terminate with an exit code indicating invalid dialog options.</remarks>
        /// <typeparam name="T">The type to which the dialog options string should be deserialized.</typeparam>
        /// <param name="dialogOptions">A JSON-formatted string containing the dialog options to deserialize.</param>
        /// <returns>The deserialized object of type <typeparamref name="T"/>.</returns>
        private static T GetDialogOptions<T>(string dialogOptions)
        {
            try
            {
                return SerializationUtilities.DeserializeFromString<T>(dialogOptions);
            }
            catch (Exception ex)
            {
                throw new ProgramException($"An error occurred while deserializing the dialog options: {ex.Message}", ex, ExitCode.InvalidDialogOptions);
            }
        }

        /// <summary>
        /// Serializes the specified dialog result object to a string.
        /// </summary>
        /// <remarks>This method serializes the provided dialog result object using a utility method.  If an error occurs during serialization, the application will log the error, terminate with an exit code, and rethrow the exception.</remarks>
        /// <typeparam name="T">The type of the dialog result object to serialize.</typeparam>
        /// <param name="dialogResult">The dialog result object to be serialized. Cannot be null.</param>
        /// <returns>A string representation of the serialized dialog result.</returns>
        private static string SerializeDialogResult<T>(T dialogResult)
        {
            try
            {
                return SerializationUtilities.SerializeToString(dialogResult);
            }
            catch (Exception ex)
            {
                throw new ProgramException($"An error occurred while serializing the dialog result: {ex.Message}", ex, ExitCode.InvalidDialogResult);
            }
        }

        /// <summary>
        /// Represents the exit codes that can be returned by the application to indicate the result of its execution.
        /// </summary>
        private enum ExitCode : int
        {
            Unknown = -1,
            Success = 0,
            NoArguments = 1,
            InvalidArguments = 2,
            InvalidMode = 3,

            NoDialogType = 10,
            InvalidDialog = 11,
            UnsupportedDialog = 12,
            NoDialogStyle = 13,
            InvalidDialogStyle = 14,
            NoDialogOptions = 15,
            InvalidDialogOptions = 16,
            InvalidDialogResult = 17,

            NoInputPipe = 20,
            NoOutputPipe = 21,
            InvalidInputPipe = 22,
            InvalidOutputPipe = 23,
            PipeReadWriteError = 24,
            InvalidCommand = 25,
        }

        /// <summary>
        /// Represents an exception that occurs during program execution, providing an error message and an associated
        /// exit code.
        /// </summary>
        /// <remarks>The <see cref="ProgramException"/> class is used to signal errors that occur during
        /// program execution, with an optional exit code that is set to the <see cref="Exception.HResult"/> property.
        /// This allows the exception to convey both the error details and a numeric code that can be used for
        /// programmatic handling  or process termination.</remarks>
        private class ProgramException : Exception
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="ProgramException"/> class with a specified error message
            /// and exit code.
            /// </summary>
            /// <param name="message">The error message that explains the reason for the exception.</param>
            /// <param name="exitCode">The exit code associated with the exception, which is used to set the <see cref="HResult"/> property.</param>
            internal ProgramException(string message, ExitCode exitCode) : base(message)
            {
                HResult = (int)exitCode;
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="ProgramException"/> class with a specified error message, 
            /// a reference to the inner exception that caused this exception, and an exit code.
            /// </summary>
            /// <param name="message">The error message that explains the reason for the exception.</param>
            /// <param name="innerException">The exception that is the cause of the current exception, or <see langword="null"/> if no inner exception is specified.</param>
            /// <param name="exitCode">The exit code associated with the exception, which is used to set the <see cref="HResult"/> property.</param>
            internal ProgramException(string message, Exception innerException, ExitCode exitCode) : base(message, innerException)
            {
                HResult = (int)exitCode;
            }
        }
    }
}
