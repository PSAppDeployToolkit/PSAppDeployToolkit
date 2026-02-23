using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace PSADT.ProcessManagement
{
    /// <summary>
    /// Service for managing running processes.
    /// </summary>
    internal sealed record RunningProcessService : IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the RunningProcessService class with the specified process definitions.
        /// </summary>
        /// <param name="processDefinitions">A read-only collection of process definitions to be managed by the service. Must contain at least one
        /// element.</param>
        /// <exception cref="ArgumentNullException">Thrown if processDefinitions is null or contains no elements.</exception>
        internal RunningProcessService(ReadOnlyCollection<ProcessDefinition> processDefinitions)
        {
            _processDefinitions = processDefinitions?.Count > 0 ? processDefinitions : throw new ArgumentNullException(nameof(processDefinitions), "Process definitions cannot be null.");
        }

        /// <summary>
        /// Starts monitoring running processes by initiating the polling task.
        /// </summary>
        /// <remarks>This method creates a new polling task and renews the cancellation token. Ensure that
        /// the polling task is not already active before calling this method to avoid an exception.</remarks>
        /// <exception cref="InvalidOperationException">Thrown if the polling task is already running.</exception>
        internal void Start()
        {
            // We can't restart the polling task if it's already running.
            if (IsRunning)
            {
                throw new InvalidOperationException("The polling task is already running.");
            }

            // Renew the cancellation token as once they're cancelled, they're not usable.
            _pollingTask = Task.Run(PollRunningProcesses, (_cancellationTokenSource = new()).Token);
        }

        /// <summary>
        /// Stops the polling operation and releases associated resources.
        /// </summary>
        /// <remarks>Call this method to cancel the ongoing polling operation and clean up resources when
        /// polling is no longer required. Failing to stop the polling task may result in resource leaks. This method is
        /// not thread-safe and should not be called concurrently with other operations that start or stop
        /// polling.</remarks>
        /// <exception cref="InvalidOperationException">Thrown if the polling task is not currently running.</exception>
        internal void Stop()
        {
            // We can't stop the polling task if it's not running.
            if (_pollingTask is null)
            {
                throw new InvalidOperationException("The polling task is not running.");
            }

            // Cancel the task and wait for it to complete.
            _cancellationTokenSource!.Cancel();
            _pollingTask.GetAwaiter().GetResult();
            _pollingTask.Dispose();
            _pollingTask = null;

            // Dispose of the cancellation token as once they're cancelled, they're not usable.
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;
        }

        /// <summary>
        /// Continuously polls the system for running processes at regular intervals and raises an event when the list
        /// of processes to close changes.
        /// </summary>
        /// <remarks>This method executes asynchronously and can be canceled using the associated
        /// cancellation token. It updates the cached list of running processes and notifies subscribers if the set of
        /// processes to close has changed. Polling continues until cancellation is requested.</remarks>
        /// <returns>A task that represents the asynchronous polling operation.</returns>
        private async Task PollRunningProcesses()
        {
            CancellationToken token = _cancellationTokenSource!.Token;
            while (!token.IsCancellationRequested)
            {
                // Update the list of running processes.
                await _mutex.WaitAsync(token).ConfigureAwait(false);
                try
                {
                    RefreshCachedProcessLists();
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    break;
                }
                finally
                {
                    _ = _mutex.Release();
                }

                // Raise the event if the list of processes to close has changed.
                ReadOnlyCollection<string> processDescs = new([.. _processesToClose.Select(static runningProcess => runningProcess.Description)]);
                if (!_lastProcessDescriptions.SequenceEqual(processDescs))
                {
                    _lastProcessDescriptions = processDescs;
                    ProcessesToCloseChanged?.Invoke(this, new(_processesToClose));
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

        /// <summary>
        /// Refreshes the cached lists of running processes and processes to close.
        /// </summary>
        /// <remarks>This method updates the internal cache of running processes and groups them by their description to determine which processes should be closed. The updated lists are used internally to manage process-related operations.</remarks>
        private void RefreshCachedProcessLists()
        {
            // Update the list of running processes.
            _runningProcesses = RunningProcessInfo.Get(_processDefinitions);
            _processesToClose = new ReadOnlyCollection<ProcessToClose>([.. _runningProcesses.GroupBy(static p => p.FileName, StringComparer.OrdinalIgnoreCase).Select(static p => new ProcessToClose(p.First()))]);
        }

        /// <summary>
        /// Event that is raised when the list of processes to show on a CloseAppsDialog changes.
        /// </summary>
        internal event EventHandler<ProcessesToCloseChangedEventArgs>? ProcessesToCloseChanged;

        /// <summary>
        /// Event that is raised when the list of running processes changes.
        /// </summary>
        internal IReadOnlyList<RunningProcessInfo> RunningProcesses
        {
            get
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(RunningProcessService));
                }
                _mutex.Wait();
                try
                {
                    RefreshCachedProcessLists();
                    return _runningProcesses;
                }
                finally
                {
                    _ = _mutex.Release();
                }
            }
        }

        /// <summary>
        /// Gets the list of processes to display on a CloseAppsDialog.
        /// </summary>
        internal IReadOnlyList<ProcessToClose> ProcessesToClose
        {
            get
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(RunningProcessService));
                }
                _mutex.Wait();
                try
                {
                    RefreshCachedProcessLists();
                    return _processesToClose;
                }
                finally
                {
                    _ = _mutex.Release();
                }
            }
        }

        /// <summary>
        /// Indicates whether the service is running or not.
        /// </summary>
        internal bool IsRunning => _pollingTask is not null;

        /// <summary>
        /// Releases the resources used by the object, optionally stopping any running processes and disposing managed
        /// resources.
        /// </summary>
        /// <remarks>This method implements the standard dispose pattern. When disposing is set to true,
        /// managed resources are released in addition to unmanaged resources. This method can be called multiple times
        /// without throwing an exception.</remarks>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }
            if (disposing)
            {
                if (IsRunning)
                {
                    Stop();
                }
                _mutex.Dispose();
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
        /// Gets the list of running processes.
        /// </summary>
        private IReadOnlyList<RunningProcessInfo> _runningProcesses = [];

        /// <summary>
        /// Gets the list of processes to display on a CloseAppsDialog.
        /// </summary>
        private IReadOnlyList<ProcessToClose> _processesToClose = [];

        /// <summary>
        /// Gets the list of process descriptions.
        /// </summary>
        private IReadOnlyList<string> _lastProcessDescriptions = [];

        /// <summary>
        /// Disposal flag for the <see cref="RunningProcessService"/> class.
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// The task that polls for running processes.
        /// </summary>
        private Task? _pollingTask;

        /// <summary>
        /// The cancellation token source for the polling task.
        /// </summary>
        private CancellationTokenSource? _cancellationTokenSource;

        /// <summary>
        /// The mutex used to synchronize access to the running processes list.
        /// </summary>
        private readonly SemaphoreSlim _mutex = new(1, 1);

        /// <summary>
        /// The caller's specified process definitions.
        /// </summary>
        private readonly ReadOnlyCollection<ProcessDefinition> _processDefinitions;

        /// <summary>
        /// The interval at which to poll for running processes.
        /// </summary>
        private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(1);
    }
}
