using System;
using PSADT.UserInterface.Processes;

namespace PSADT.UserInterface.Dialogs
{
    /// <summary>
    /// Represents all data needed by a Show-ADTInstallationWelcome invocation.
    /// </summary>
    public sealed class CloseAppsDialogState : IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CloseAppsDialogState"/> class with the specified options.
        /// </summary>
        /// <param name="runningProcessService"></param>
        /// <param name="countdownEnd"></param>
        public CloseAppsDialogState(RunningProcessService? runningProcessService, DateTime? countdownEnd)
        {
            RunningProcessService = runningProcessService;
            CountdownEnd = countdownEnd;
        }

        /// <summary>
        /// Disposes of the resources used by the <see cref="RunningProcessService"/> class.
        /// </summary>
        /// <param name="disposing"></param>
        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }
            if (disposing)
            {
                RunningProcessService?.Dispose();
            }
            _disposed = true;
        }

        /// <summary>
        /// Disposes of the resources used by the <see cref="RunningProcessService"/> class.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Disposes of this object; principally the RunningProcessService object within it.
        /// </summary>
        private bool _disposed = false;

        /// <summary>
        /// Gets/sets the running applications.
        /// </summary>
        public readonly RunningProcessService? RunningProcessService;

        /// <summary>
        /// Gets/sets the InstallationWelcome's CloseProcesses countdown.
        /// </summary>
        public readonly DateTime? CountdownEnd;
    }
}
