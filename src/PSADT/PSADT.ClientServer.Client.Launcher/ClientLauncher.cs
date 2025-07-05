using System;
using System.ComponentModel;
using System.Diagnostics;

namespace PSADT.ClientServer
{
    /// <summary>
    /// Provides the main entry point for the application.
    /// </summary>
    /// <remarks>This class is responsible for launching the main application process. It sets up a new
    /// process using the current assembly's location (with ".Launcher" removed from the file name) and passes the
    /// provided command-line arguments to it. The process is configured to run without creating a new window and does
    /// not use the shell to execute.</remarks>
    internal static class ClientLauncher
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        private static int Main(string[] args)
        {
            // Set up a new process to run the main application.
            using (Process process = new())
            {
                // Set the process start information.
                process.StartInfo.FileName = typeof(ClientLauncher).Assembly.Location.Replace(".Launcher.exe", ".exe");
                process.StartInfo.WorkingDirectory = Environment.SystemDirectory;
                process.StartInfo.Arguments = string.Join(" ", args);
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                try
                {
                    process.Start(); process.WaitForExit();
                    return process.ExitCode;
                }
                catch (Win32Exception ex)
                {
                    Environment.FailFast($"Error launching [{process.StartInfo.FileName}] with Win32 error code [{ex.NativeErrorCode}].\nException Info: {ex}", ex);
                    return ex.NativeErrorCode;
                }
                catch (Exception ex)
                {
                    Environment.FailFast($"An unexpected exception occurred with HRESULT [{ex.HResult}].\nException Info: {ex}", ex);
                    return ex.HResult;
                }
            }
        }
    }
}
