using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;

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
                process.StartInfo.FileName = AssemblyPath.Replace(".Launcher", null);
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
                    File.WriteAllText(ErrorFilePath, $"Error launching [{process.StartInfo.FileName}] with Win32 error code [{ex.NativeErrorCode}]: {ex}");
                    return ex.NativeErrorCode;
                }
                catch (Exception ex)
                {
                    File.WriteAllText(ErrorFilePath, $"An unexpected exception occurred with HRESULT [{ex.HResult}]: {ex}");
                    return ex.HResult;
                }
            }
        }

        /// <summary>
        /// Gets the file system path of the assembly containing the <see cref="ClientLauncher"/> type.
        /// </summary>
        /// <remarks>This property provides the location of the assembly as a string, which can be used
        /// for tasks such as loading resources or determining the application's installation directory.</remarks>
        private static readonly string AssemblyPath = typeof(ClientLauncher).Assembly.Location;

        /// <summary>
        /// Represents the file path used to log error information.
        /// </summary>
        /// <remarks>The file path is generated dynamically using the system's temporary directory, the
        /// assembly name,  and the current timestamp in ISO 8601 format (excluding milliseconds and colons). This
        /// ensures uniqueness for each log file.</remarks>
        private static readonly string ErrorFilePath = Path.Combine(Path.GetTempPath(), $"{Path.GetFileName(AssemblyPath)}_{DateTime.Now.ToString("O").Split('.')[0].Replace(":", null)}.log");
    }
}
