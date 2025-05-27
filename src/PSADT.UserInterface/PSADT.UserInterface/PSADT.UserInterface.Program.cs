using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using PSADT.UserInterface.DialogOptions;
using PSADT.UserInterface.DialogResults;
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
            // Print a message if no arguments are passed.
            if (args?.Length == 0)
            {
                var fileInfo = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
                var helpTitle = fileInfo.FileDescription!;
                var helpVersion = fileInfo.ProductVersion!.Split('+')[0];
                var helpMessage = string.Join(Environment.NewLine, new[]
                {
                    helpTitle,
                    "",
                    "Copyright © 2025 PSAppDeployToolkit Team. All rights reserved.",
                    "",
                    "This application is designed to be used with the PSAppDeployToolkit PowerShell module and should not be directly invoked.",
                    "",
                    "If you're an end-user or employee of your organization, please report this message to your helpdesk for further assistance.",
                });
                DialogManager.ShowMessageBox($"{helpTitle} {helpVersion}", helpMessage, Microsoft.VisualBasic.MsgBoxStyle.Critical);
                return (int)ExitCode.NoArguments;
            }

            // Parse the arguments into a dictionary.
            var arguments = new Dictionary<string, string?>();
            for (int i = 0; i < args!.Length; i++)
            {
                if (args[i].StartsWith("-"))
                {
                    var key = args[i].Substring(1).Trim();
                    var value = (i + 1 < args.Length) ? args[i + 1].Trim() : null;
                    if (string.IsNullOrWhiteSpace(value) || value!.StartsWith("-"))
                    {
                        Console.Error.WriteLine($"The argument [{key}] has an invalid value.");
                        return (int)ExitCode.InvalidArguments;
                    }
                    arguments.Add(key, value);
                }
            }

            // Directly display a dialog if requested.
            if (arguments.TryGetValue("DialogType", out string? dialogTypeArg))
            {
                // Confirm the DialogStyle is valid.
                if (!Enum.TryParse(dialogTypeArg, true, out DialogType dialogType))
                {
                    return (int)ExitCode.InvalidDialog;
                }

                // Confirm we've got a DialogStyle.
                if (!arguments.TryGetValue("DialogStyle", out string? dialogStyleArg) || string.IsNullOrWhiteSpace(dialogStyleArg))
                {
                    return (int)ExitCode.NoDialogStyle;
                }

                // Confirm the DialogStyle is valid.
                if (!Enum.TryParse(dialogStyleArg, true, out DialogStyle dialogStyle))
                {
                    return (int)ExitCode.InvalidDialogStyle;
                }

                // Confirm we have dialog options.
                if (!arguments.TryGetValue("DialogOptions", out string? dialogOptionsArg) || string.IsNullOrWhiteSpace(dialogOptionsArg))
                {
                    return (int)ExitCode.NoDialogOptions;
                }

                // Switch on the DialogType to create the appropriate dialog.
                switch (dialogType)
                {
                    case DialogType.InputDialog:
                        DialogManager.ShowModalDialog<InputDialogResult>(dialogType, dialogStyle, GetDialogOptions<InputDialogOptions>(dialogOptionsArg!));
                        break;
                    case DialogType.CustomDialog:
                        DialogManager.ShowModalDialog<string>(dialogType, dialogStyle, GetDialogOptions<CustomDialogOptions>(dialogOptionsArg!));
                        break;
                    case DialogType.RestartDialog:
                        DialogManager.ShowModalDialog<string>(dialogType, dialogStyle, GetDialogOptions<RestartDialogOptions>(dialogOptionsArg!));
                        break;
                    default:
                        return (int)ExitCode.UnsupportedDialog;
                }
            }
            else
            {
                // If we're here, we didn't know what to do with the arguments.
                return (int)ExitCode.InvalidMode;
            }

            // If we're here, everything went well.
            return (int)ExitCode.Success;
        }

        /// <summary>
        /// Deserializes the provided dialog options string into an object of the specified type.
        /// </summary>
        /// <remarks>If an error occurs during deserialization, the application will terminate with an exit code indicating invalid dialog options.</remarks>
        /// <typeparam name="T">The type to which the dialog options string should be deserialized.</typeparam>
        /// <param name="dialogOptionsArg">A JSON-formatted string containing the dialog options to deserialize.</param>
        /// <returns>The deserialized object of type <typeparamref name="T"/>.</returns>
        private static T GetDialogOptions<T>(string dialogOptionsArg)
        {
            try
            {
                return SerializationUtilities.DeserializeFromString<T>(dialogOptionsArg);
            }
            catch
            {
                Environment.Exit((int)ExitCode.InvalidDialogOptions);
                throw;
            }
        }

        /// <summary>
        /// Represents the exit codes that can be returned by the application to indicate the result of its execution.
        /// </summary>
        private enum ExitCode : int
        {
            Success = 0,
            NoArguments = 1,
            InvalidArguments = 2,
            InvalidMode = 3,
            InvalidDialog = 4,
            UnsupportedDialog = 5,
            NoDialogStyle = 6,
            InvalidDialogStyle = 7,
            NoDialogOptions = 8,
            InvalidDialogOptions = 9,
        }
    }
}
