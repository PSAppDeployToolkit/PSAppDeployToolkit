using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PSADT.ProcessManagement
{
    /// <summary>
    /// Service for managing running processes.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0182: Avoid unused internal types.", Justification = "This is used across InternalsVisibleTo boundaries.")]
    internal sealed class RunningProcessService : IAsyncDisposable
    {
        /// <summary>
        /// Initializes a new instance of the RunningProcessService class with the specified process definitions.
        /// </summary>
        /// <param name="processDefinitions">A read-only collection of process definitions to be managed by the service. Must contain at least one
        /// element.</param>
        /// <exception cref="ArgumentNullException">Thrown if processDefinitions is null or contains no elements.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S3236:Caller information arguments should not be provided explicitly", Justification = "This is intentional as we're testing a parameter member.")]
        internal RunningProcessService(ReadOnlyCollection<ProcessDefinition> processDefinitions)
        {
            ArgumentOutOfRangeException.ThrowIfZero(processDefinitions.Count, nameof(processDefinitions));
            _processDefinitions = processDefinitions;
        }

        /// <summary>
        /// Starts monitoring running processes by initiating the polling task.
        /// </summary>
        /// <remarks>This method creates a new polling task and renews the cancellation token. Ensure that
        /// the polling task is not already active before calling this method to avoid an exception.</remarks>
        /// <exception cref="InvalidOperationException">Thrown if the polling task is already running.</exception>
        internal void Start()
        {
            // Internal state check to ensure the polling task is active before attempting to stop it.
            async Task PollRunningProcessesAsync()
            {
                if (_cancellationTokenSource is null)
                {
                    throw new InvalidOperationException("Cancellation token source is not initialized.");
                }
                CancellationToken token = _cancellationTokenSource.Token;
                IReadOnlyList<string> lastProcessDescriptions = [];
                while (!token.IsCancellationRequested)
                {
                    // Update the list of running processes and raise the event if the list of processes to close has changed.
                    ProcessSnapshot snapshot = GetLiveProcessSnapshot(); Volatile.Write(ref _processSnapshot, snapshot);
                    if (!lastProcessDescriptions.SequenceEqual(snapshot.ProcessDescriptions, StringComparer.OrdinalIgnoreCase))
                    {
                        lastProcessDescriptions = snapshot.ProcessDescriptions;
                        ProcessesToCloseChanged?.Invoke(this, new(snapshot.ProcessesToClose));
                    }

                    // Wait for the specified interval before polling again.
                    try
                    {
                        await Task.Delay(_pollInterval, token).ConfigureAwait(false);
                    }
                    catch (TaskCanceledException) when (token.IsCancellationRequested)
                    {
                        break;
                    }
                }
            }

            // We can't restart the polling task if it's already running.
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (IsRunning)
            {
                throw new InvalidOperationException("The polling task is already running.");
            }

            // Renew the cancellation token as once they're cancelled, they're not usable.
            _cancellationTokenSource = new(); _pollingTask = PollRunningProcessesAsync();
        }

        /// <summary>
        /// Stops the polling operation and releases associated resources.
        /// </summary>
        /// <remarks>Call this method to cancel the ongoing polling operation and clean up resources when
        /// polling is no longer required. Failing to stop the polling task may result in resource leaks. This method is
        /// not thread-safe and should not be called concurrently with other operations that start or stop
        /// polling.</remarks>
        /// <exception cref="InvalidOperationException">Thrown if the polling task is not currently running.</exception>
        internal async ValueTask StopAsync()
        {
            // We can't stop the polling task if it's not running.
            if (_pollingTask is null || _cancellationTokenSource is null)
            {
                throw new InvalidOperationException("The polling task is not running.");
            }

            // Cancel the task and wait for it to complete.
            try
            {
                using (_cancellationTokenSource)
                {
                    await _cancellationTokenSource.CancelAsync().ConfigureAwait(false);
                    await _pollingTask.ConfigureAwait(false);
                }
            }
            finally
            {
                Volatile.Write(ref _processSnapshot, null);
                _cancellationTokenSource = null;
                _pollingTask = null;
            }
        }

        /// <summary>
        /// Disposes of the resources used by the <see cref="RunningProcessService"/> class.
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }
            if (IsRunning)
            {
                await StopAsync().ConfigureAwait(false);
            }
            _disposed = true;
        }

        /// <summary>
        /// Gets a live snapshot of the running processes and processes to close.
        /// </summary>
        private ProcessSnapshot GetLiveProcessSnapshot()
        {
            IReadOnlyList<RunningProcessInfo> runningProcesses = RunningProcessInfo.Get(_processDefinitions);
            IReadOnlyList<ProcessToClose> processesToClose = GetProcessesToClose(runningProcesses);
            return new(runningProcesses, processesToClose, [.. processesToClose.Select(static p => p.Description)]);
        }

        /// <summary>
        /// Gets the list of processes to display on a CloseAppsDialog from the specified running processes.
        /// </summary>
        /// <param name="runningProcesses">The running processes to group into process close entries.</param>
        private static IReadOnlyList<ProcessToClose> GetProcessesToClose(IReadOnlyList<RunningProcessInfo> runningProcesses)
        {
            return [.. runningProcesses.GroupBy(static p => p.FileName.FullName, StringComparer.OrdinalIgnoreCase).Select(static p => new ProcessToClose(p.First()))];
        }

        /// <summary>
        /// Indicates whether the service is running or not.
        /// </summary>
        internal bool IsRunning => _pollingTask is not null;

        /// <summary>
        /// Event that is raised when the list of running processes changes.
        /// </summary>
        internal IReadOnlyList<RunningProcessInfo> RunningProcesses
        {
            get
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                return IsRunning && Volatile.Read(ref _processSnapshot) is ProcessSnapshot snapshot ? snapshot.RunningProcesses : RunningProcessInfo.Get(_processDefinitions);
            }
        }

        /// <summary>
        /// Gets the list of processes to display on a CloseAppsDialog.
        /// </summary>
        internal IReadOnlyList<ProcessToClose> ProcessesToClose
        {
            get
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                return IsRunning && Volatile.Read(ref _processSnapshot) is ProcessSnapshot snapshot ? snapshot.ProcessesToClose : GetProcessesToClose(RunningProcessInfo.Get(_processDefinitions));
            }
        }

        /// <summary>
        /// Event that is raised when the list of processes to show on a CloseAppsDialog changes.
        /// </summary>
        internal event EventHandler<ProcessesToCloseChangedEventArgs>? ProcessesToCloseChanged;

        /// <summary>
        /// The task that polls for running processes.
        /// </summary>
        private Task? _pollingTask;

        /// <summary>
        /// The cancellation token source for the polling task.
        /// </summary>
        private CancellationTokenSource? _cancellationTokenSource;

        /// <summary>
        /// The latest process snapshot published by the polling task.
        /// </summary>
        private ProcessSnapshot? _processSnapshot;

        /// <summary>
        /// Disposal flag for the <see cref="RunningProcessService"/> class.
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// The caller's specified process definitions.
        /// </summary>
        private readonly ReadOnlyCollection<ProcessDefinition> _processDefinitions;

        /// <summary>
        /// The interval at which to poll for running processes.
        /// </summary>
        private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Represents a coherent snapshot of the running processes and processes to close.
        /// </summary>
        /// <param name="runningProcesses">The running processes in the snapshot.</param>
        /// <param name="processesToClose">The processes to close in the snapshot.</param>
        /// <param name="processDescriptions">The process descriptions used to detect changes.</param>
        private sealed class ProcessSnapshot(IReadOnlyList<RunningProcessInfo> runningProcesses, IReadOnlyList<ProcessToClose> processesToClose, IReadOnlyList<string> processDescriptions)
        {
            /// <summary>
            /// Gets the running processes in the snapshot.
            /// </summary>
            internal IReadOnlyList<RunningProcessInfo> RunningProcesses { get; } = runningProcesses;

            /// <summary>
            /// Gets the processes to close in the snapshot.
            /// </summary>
            internal IReadOnlyList<ProcessToClose> ProcessesToClose { get; } = processesToClose;

            /// <summary>
            /// Gets the process descriptions used to detect changes.
            /// </summary>
            internal IReadOnlyList<string> ProcessDescriptions { get; } = processDescriptions;
        }
    }
}
