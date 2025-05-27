using System;
using System.Diagnostics;
using System.Reflection;

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
            return (int)ExitCode.Success;
        }

        /// <summary>
        /// Represents the exit codes that can be returned by the application to indicate the result of its execution.
        /// </summary>
        private enum ExitCode : int
        {
            Success = 0,
            NoArguments = 1,
        }
    }
}
