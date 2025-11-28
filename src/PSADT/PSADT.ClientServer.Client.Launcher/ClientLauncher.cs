using System;
using System.ComponentModel;
using PSADT.ProcessManagement;

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
        private static int Main(string[] argv)
        {
            // Set up a new process to run the main application.
            try
            {
                return ProcessManager.LaunchAsync(new(typeof(ClientLauncher).Assembly.Location.Replace(".Launcher.exe", ".exe"), argv.Length > 0 ? argv : null, Environment.SystemDirectory, denyUserTermination: true, createNoWindow: true))!.Task.GetAwaiter().GetResult().ExitCode;
            }
            catch (Win32Exception ex)
            {
                Environment.FailFast($"An unexpected Win32 error occurred with code [{ex.NativeErrorCode}].\nException Info: {ex}", ex);
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
