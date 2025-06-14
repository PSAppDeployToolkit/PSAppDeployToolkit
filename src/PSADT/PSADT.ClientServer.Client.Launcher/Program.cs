namespace PSADT.ClientServer.Launcher
{
    /// <summary>
    /// Provides the main entry point for the application.
    /// </summary>
    /// <remarks>This class is responsible for launching the main application process. It sets up a new
    /// process using the current assembly's location (with ".Launcher" removed from the file name) and passes the
    /// provided command-line arguments to it. The process is configured to run without creating a new window and does
    /// not use the shell to execute.</remarks>
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        private static int Main(string[] args)
        {
            // Set up a new process to run the main application.
            using (System.Diagnostics.Process process = new())
            {
                // Set the process start information.
                process.StartInfo.FileName = $"{System.IO.Path.GetFileNameWithoutExtension(System.Reflection.Assembly.GetExecutingAssembly().Location.Replace(".Launcher", null))}.exe";
                process.StartInfo.Arguments = string.Join(" ", args);
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.Start(); process.WaitForExit();
                return process.ExitCode;
            }
        }
    }
}
