using System;
using System.IO;
using PSADT.ConsoleEx;
using System.Threading;
using System.Threading.Tasks;

namespace PSADT.ProcessEx
{
    /// <summary>
    /// Represents information about a process running in a specific session.
    /// </summary>
    public class ManagedProcess : IDisposable
    {
        private bool _disposed;
        private readonly object _disposeLock = new object();
        private CancellationTokenSource _redirectionCancellationSource = new CancellationTokenSource();

        /// <summary>
        /// Initializes a new instance of the <see cref="ManagedProcess"/> class.
        /// </summary>
        /// <param name="sessionInfo">The session information for this process.</param>
        public ManagedProcess(SessionDetails sessionInfo)
        {
            SessionInfo = sessionInfo ?? throw new ArgumentNullException(nameof(sessionInfo), "SessionInfo cannot be null.");
        }

        /// <summary>
        /// The session information for this process.
        /// </summary>
        public SessionDetails SessionInfo { get; set; }

        /// <summary>
        /// The Process object representing the running process.
        /// </summary>
        public System.Diagnostics.Process? Process { get; set; }

        /// <summary>
        /// Value indicating whether the process has exited.
        /// </summary>
        public bool HasExited => Process?.HasExited ?? true;

        /// <summary>
        /// Gets the exit code of the process, or null if the process hasn't exited.
        /// </summary>
        public int? ExitCode => Process?.HasExited == true ? Process.ExitCode : (int?)null;

        /// <summary>
        /// Gets the process id.
        /// </summary>
        public int ProcessId => Process?.Id ?? -1;

        /// <summary>
        /// Indicates whether the process is running with elevated privileges.
        /// </summary>
        public bool IsElevated { get; set; }

        /// <summary>
        /// Indicates whether the process is a GUI application.
        /// </summary>
        public bool IsGuiApplication { get; set; }

        /// <summary>
        /// Redirect standard output.
        /// </summary>
        public bool RedirectStandardOutput { get; set; }

        /// <summary>
        /// Redirect standard error.
        /// </summary>
        public bool RedirectStandardError { get; set; }

        /// <summary>
        /// Merge standard error and standard output.
        /// </summary>
        public bool MergeStdErrAndStdOut { get; set; }

        /// <summary>
        /// The task responsible for redirecting standard output.
        /// </summary>
        public Task StandardOutputRedirectionTask { get; set; } = Task.CompletedTask;

        /// <summary>
        /// The task responsible for redirecting standard error.
        /// </summary>
        public Task StandardErrorRedirectionTask { get; set; } = Task.CompletedTask;

        /// <summary>
        /// Sets up output redirection for the process.
        /// </summary>
        /// <param name="processInfo">The ManagedProcess containing the process to set up redirection for.</param>
        /// <param name="options">The launch options containing redirection settings.</param>
        public void ConfigureOutputRedirection(LaunchOptions options)
        {
            if (!options.RedirectOutput || this.Process == null)
                return;

            try
            {
                if (!this.RedirectStandardOutput && !this.RedirectStandardError)
                {
                    ConsoleHelper.DebugWrite("Process does not support output redirection. This is likely a GUI application.", MessageType.Info);
                    return;
                }

                string timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
                string baseFileName = $"S_{this.SessionInfo.SessionId}_P_{this.Process.Id}_{timestamp}";

                if (!string.IsNullOrEmpty(options.OutputDirectory))
                {
                    string stdoutFile = Path.Combine(options.OutputDirectory, $"{baseFileName}_stdout.txt");
                    string stderrFile = Path.Combine(options.OutputDirectory, $"{baseFileName}_stderr.txt");

                    if (this.RedirectStandardOutput)
                    {
                        this.StandardOutputRedirectionTask = RedirectToFileAsync(this.Process.StandardOutput, stdoutFile, _redirectionCancellationSource.Token);
                    }
                    if (this.RedirectStandardError && !this.MergeStdErrAndStdOut)
                    {
                        this.StandardErrorRedirectionTask = RedirectToFileAsync(this.Process.StandardError, stderrFile, _redirectionCancellationSource.Token);
                    }

                    ConsoleHelper.DebugWrite($"Configured output redirection to files: stdout={stdoutFile}, stderr={stderrFile}", MessageType.Debug);
                }
                else
                {
                    if (this.RedirectStandardOutput)
                    {
                        this.StandardOutputRedirectionTask = RedirectToConsoleAsync(this.Process.StandardOutput, Console.Out, _redirectionCancellationSource.Token);
                    }
                    if (this.RedirectStandardError && !this.MergeStdErrAndStdOut)
                    {
                        this.StandardErrorRedirectionTask = RedirectToConsoleAsync(this.Process.StandardError, Console.Error, _redirectionCancellationSource.Token);
                    }

                    ConsoleHelper.DebugWrite("Configured output redirection to console", MessageType.Debug);
                }
            }
            catch (Exception ex)
            {
                ConsoleHelper.DebugWrite($"Error configuring output redirection: {ex.Message}", MessageType.Error, ex);
            }
        }

        private static async Task RedirectToFileAsync(StreamReader reader, string filePath, CancellationToken cancellationToken)
        {
            try
            {
                using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read);
                using var writer = new StreamWriter(fileStream);
                await RedirectStreamAsync(reader, writer, cancellationToken);
                ConsoleHelper.DebugWrite($"Completed redirection to file [{filePath}].", MessageType.Debug);
            }
            catch (Exception ex)
            {
                ConsoleHelper.DebugWrite($"Error redirecting to file [{filePath}]: {ex.Message}.", MessageType.Error, ex);
                throw;
            }
        }

        private static Task RedirectToConsoleAsync(StreamReader reader, TextWriter writer, CancellationToken cancellationToken)
        {
            try
            {
                return RedirectStreamAsync(reader, writer, cancellationToken);
            }
            catch (Exception ex)
            {
                ConsoleHelper.DebugWrite($"Error redirecting to console: {ex.Message}.", MessageType.Error, ex);
                throw;
            }
        }

        private static async Task RedirectStreamAsync(StreamReader reader, TextWriter writer, CancellationToken cancellationToken)
        {
            char[] buffer = new char[4096];
            int bytesRead;

            try
            {
                while ((bytesRead = await reader.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await writer.WriteAsync(buffer, 0, bytesRead);
                    await writer.FlushAsync();
                }
            }
            catch (OperationCanceledException)
            {
                // Redirection was cancelled, which is expected when stopping monitors
            }
            catch (Exception ex)
            {

                ConsoleHelper.DebugWrite($"Failed to redirect stream: {ex.Message}.", MessageType.Error, ex);
                throw;
            }
        }

        /// <summary>
        /// Stops the redirection monitors.
        /// </summary>
        public void StopRedirectionMonitors()
        {
            _redirectionCancellationSource.Cancel();
        }

        /// <summary>
        /// Disposes of the resources used by the <see cref="ManagedProcess"/> class.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes of the resources used by the <see cref="ManagedProcess"/> class.
        /// </summary>
        /// <param name="disposing">Indicates whether the method is called from Dispose() or from a finalizer.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                lock (_disposeLock)
                {
                    if (!_disposed)
                    {
                        if (disposing)
                        {
                            StopRedirectionMonitors();
                            Process?.Dispose();
                            _redirectionCancellationSource.Dispose();
                        }
                        _disposed = true;
                    }
                }
            }
        }

        /// <summary>
        /// Destructor for <see cref="ManagedProcess"/>.
        /// </summary>
        ~ManagedProcess()
        {
            Dispose(false);
        }
    }
}
