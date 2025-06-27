using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO.Pipes;
using System.Reflection;
using Microsoft.Win32.SafeHandles;
using PSADT.LibraryInterfaces;
using PSADT.Security;
using PSADT.UserInterface;
using PSADT.UserInterface.Dialogs;
using Windows.Win32.Foundation;
using Windows.Win32.Security;

namespace PSADT.TokenBroker
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        private static int Main(string[] args)
        {
            // Display the help dialog if no arguments are provided.
            if (null == args || args.Length == 0)
            {
                ShowHelpDialog();
                throw new InvalidOperationException("No arguments were provided to the display server.");
            }

            // Read our arguments and make sure they're all valid.
            ReadOnlyDictionary<string, string> arguments = ConvertArgsToDictionary(args);
            if (!arguments.TryGetValue("PipeName", out string? pipeName) || string.IsNullOrWhiteSpace(pipeName))
            {
                throw new ArgumentException("The 'PipeName' argument is required and cannot be null or whitespace.");
            }
            if (!arguments.TryGetValue("ProcessId", out string? processIdStr) || string.IsNullOrWhiteSpace(processIdStr) || !int.TryParse(processIdStr, out int processId))
            {
                throw new ArgumentException("The 'ProcessId' argument is required and cannot be null or whitespace.");
            }
            if (!arguments.TryGetValue("SessionId", out string? sessionIdStr) || string.IsNullOrWhiteSpace(sessionIdStr) || !uint.TryParse(sessionIdStr, out uint sessionId))
            {
                throw new ArgumentException("The 'SessionId' argument is required and cannot be null or whitespace.");
            }
            if (!arguments.TryGetValue("UseLinkedAdminToken", out string? useLinkedAdminTokenStr) || string.IsNullOrWhiteSpace(useLinkedAdminTokenStr) || !bool.TryParse(useLinkedAdminTokenStr, out bool useLinkedAdminToken))
            {
                throw new ArgumentException("The 'UseLinkedAdminTokenStr' argument is required and cannot be null or whitespace.");
            }

            // Create a named pipe client stream to communicate with the server.
            using (var pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.None))
            {
                // Connect to the named pipe server.
                pipe.Connect();

                // If the session Id is greater than 0, get the user's token from the WTS subsystem.
                SafeFileHandle hBaseToken;
                if (sessionId > 0)
                {
                    if (useLinkedAdminToken)
                    {
                        WtsApi32.WTSQueryUserToken(sessionId, out var phToken);
                        using (phToken)
                        {
                            hBaseToken = TokenManager.GetLinkedToken(phToken);
                        }
                    }
                    else
                    {
                        WtsApi32.WTSQueryUserToken(sessionId, out hBaseToken);
                    }
                }
                else
                {
                    AdvApi32.OpenProcessToken(Process.GetCurrentProcess().SafeHandle, TOKEN_ACCESS_MASK.TOKEN_DUPLICATE | TOKEN_ACCESS_MASK.TOKEN_QUERY, out hBaseToken);
                }

                // Convert the base token to a primary token.
                SafeFileHandle hPrimaryToken;
                using (hBaseToken)
                {
                    hPrimaryToken = TokenManager.GetPrimaryToken(hBaseToken);
                }

                // Duplicate the token to the specified process ID.
                SafeFileHandle hDupToken;
                using (var currentProcess = Kernel32.GetCurrentProcess())
                using (var sourceProcess = Process.GetProcessById(processId).SafeHandle)
                using (hPrimaryToken)
                {
                    Kernel32.DuplicateHandle(currentProcess, hPrimaryToken, sourceProcess, out hDupToken, 0, true, DUPLICATE_HANDLE_OPTIONS.DUPLICATE_SAME_ACCESS);
                }

                // Write the duplicated token to the pipe.
                using (hDupToken)
                {
                    pipe.Write(BitConverter.GetBytes(hDupToken.DangerousGetHandle().ToInt64()), 0, sizeof(long));
                }
                pipe.Flush(); pipe.WaitForPipeDrain();
            }
            return 0;
        }

        /// <summary>
        /// Displays a help dialog with information about the application and its usage.
        /// </summary>
        /// <remarks>This method retrieves version and copyright information from the application's
        /// assembly and displays a dialog box with a message intended for end-users. The dialog informs users that the
        /// application is designed to be used with the PSAppDeployToolkit PowerShell module and should not be invoked
        /// directly.  After displaying the dialog, the method throws an <see cref="InvalidOperationException"/> to
        /// indicate that no arguments were provided to the display server.</remarks>
        /// <exception cref="InvalidOperationException">Thrown after the help dialog is displayed to indicate that no arguments were provided to the display server.</exception>
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
        }

        /// <summary>
        /// Converts an array of command-line arguments into a read-only dictionary of key-value pairs.
        /// </summary>
        /// <remarks>This method expects arguments in the format of key-value pairs, where keys start with
        /// a '-' character  and values follow immediately. For example: <c>["-key1", "value1", "-key2", "value2"]</c>.
        /// Keys and  values are trimmed of whitespace.  The returned dictionary is read-only to ensure the parsed data
        /// remains immutable.</remarks>
        /// <param name="args">An array of strings representing command-line arguments. Each key must start with a '-' character, and its
        /// corresponding value must follow immediately in the array.</param>
        /// <returns>A <see cref="ReadOnlyDictionary{TKey, TValue}"/> containing the parsed key-value pairs from the  input
        /// arguments. Keys are derived from arguments starting with '-' and values are the subsequent  arguments in the
        /// array.</returns>
        /// <exception cref="ArgumentException">Thrown if a key is followed by an invalid value, such as null, whitespace, or another key-like argument.</exception>
        private static ReadOnlyDictionary<string, string> ConvertArgsToDictionary(string[] args)
        {
            // Loop through arguments and match argument names to their values.
            Dictionary<string, string> arguments = [];
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
                    throw new ArgumentException($"The argument [{args[i]}] has an invalid value.");
                }
                arguments.Add(key, value);
            }

            // This data should never change once read, so return read-only.
            return new(arguments);
        }
    }
}
