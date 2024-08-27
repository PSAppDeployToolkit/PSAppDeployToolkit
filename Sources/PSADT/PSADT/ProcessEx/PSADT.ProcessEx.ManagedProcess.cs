using System;
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
        private readonly CancellationTokenSource _redirectionCts = new CancellationTokenSource();

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
        /// Stops the redirection monitors.
        /// </summary>
        public void StopRedirectionMonitors()
        {
            _redirectionCts.Cancel();
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
                            _redirectionCts.Dispose();
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
