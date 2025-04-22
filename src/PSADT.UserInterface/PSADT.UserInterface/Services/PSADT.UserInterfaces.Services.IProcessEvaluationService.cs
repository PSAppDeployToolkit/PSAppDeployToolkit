using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PSADT.UserInterface.Services
{
    /// <summary>
    /// Defines the contract for a service that evaluates running processes.
    /// </summary>
    public interface IProcessEvaluationService : IDisposable
    {
        /// <summary>
        /// Evaluates the currently running processes against a list of applications to close.
        /// </summary>
        /// <param name="appsToClose">A list of applications that should be closed.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of AppProcessInfo objects representing the running processes that match the apps to close.</returns>
        Task<List<AppProcessInfo>> EvaluateRunningProcessesAsync(List<AppProcessInfo> appsToClose, CancellationToken cancellationToken);

        /// <summary>
        /// Attempts to close a specific process.
        /// </summary>
        /// <param name="processName">The name of the process to close.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains true if the process was successfully closed, false otherwise.</returns>
        Task<bool> CloseProcessAsync(string processName, CancellationToken cancellationToken);

        /// <summary>
        /// Checks if a specific process is currently running.
        /// </summary>
        /// <param name="processName">The name of the process to check.</param>
        /// <returns>True if the process is running, false otherwise.</returns>
        bool IsProcessRunning(string processName);

        /// <summary>
        /// Event that is raised when a tracked process starts.
        /// </summary>
        event EventHandler<AppProcessInfo> ProcessStarted;

        /// <summary>
        /// Event that is raised when a tracked process exits.
        /// </summary>
        event EventHandler<AppProcessInfo> ProcessExited;
    }
}
