using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PSADT.UserInterface.LibraryInterfaces;
using PSADT.UserInterface.ProcessManagement;
using PSADT.UserInterface.Utilities;

namespace PSADT.UserInterface.Processes
{
    /// <summary>
    /// Service for managing running processes.
    /// </summary>
    public sealed class RunningProcessService : IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RunningProcessService"/> class.
        /// </summary>
        public RunningProcessService(ProcessDefinition[] processDefinitions, TimeSpan pollInterval)
        {
            _processDefinitions = processDefinitions ?? throw new ArgumentNullException(nameof(processDefinitions), "Process definitions cannot be null.");
            _pollInterval = pollInterval.Ticks > 0 ? pollInterval : throw new ArgumentOutOfRangeException(nameof(pollInterval), "Poll interval needs to be greater than zero");
        }

        /// <summary>
        /// Starts the polling task to check for running processes.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public void Start()
        {
            if (_pollingTask != null)
            {
                throw new InvalidOperationException("The polling task is already running.");
            }
            _pollingTask = Task.Run(GetRunningProcesses, _cancellationTokenSource.Token);
        }

        /// <summary>
        /// Stops the polling task and waits for it to complete.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public void Stop()
        {
            if (_pollingTask == null)
            {
                throw new InvalidOperationException("The polling task is not running.");
            }
            _cancellationTokenSource.Cancel();
            _pollingTask.Wait();
            _pollingTask.Dispose();
            _pollingTask = null;
        }

        /// <summary>
        /// Returns a list of running processes that match the specified definitions.
        /// </summary>
        /// <returns></returns>
        private async Task GetRunningProcesses()
        {
            var token = _cancellationTokenSource.Token;
            while (!token.IsCancellationRequested)
            {
                // Set up some caches for performance.
                var ntPathLookupTable = FileSystemUtilities.GetNtPathLookupTable();
                Dictionary<Process, string[]> processCommandLines = [];

                // Inline lambda to get the command line from the given process.
                string[] GetCommandLine(Process process)
                {
                    // Get the command line from the cache if we have it.
                    if (processCommandLines.TryGetValue(process, out var commandLine))
                    {
                        return commandLine;
                    }

                    // Get the image path for this process. We use this instead of what we get
                    // from GetProcessCommandLine() because POSIX applications render incorrectly.
                    var imagePath = ProcessUtilities.GetProcessImageName(process.Id, ntPathLookupTable);

                    // Get the command line for the process. If this fails due to lack
                    // of privileges, we simply just return the image path and that's it.
                    try
                    {
                        commandLine = Shell32.CommandLineToArgv(ProcessUtilities.GetProcessCommandLine(process.Id));
                        commandLine[0] = imagePath;
                    }
                    catch
                    {
                        commandLine = [imagePath];
                    }
                    processCommandLines[process] = commandLine;
                    return commandLine;
                }

                // Pre-cache running processes and start looping through to find matches.
                var processNames = _processDefinitions.Select(p => (Path.IsPathRooted(p.Name) ? Path.GetFileNameWithoutExtension(p.Name) : p.Name).ToLower());
                var allProcesses = Process.GetProcesses().Where(p => processNames.Contains(p.ProcessName.ToLower()));
                List<RunningProcess> runningProcesses = [];
                foreach (var processDefinition in _processDefinitions)
                {
                    // Loop through each process and check if it matches the definition.
                    foreach (var process in allProcesses)
                    {
                        // Continue if the process has since terminated.
                        if (process.HasExited)
                        {
                            continue;
                        }

                        // Continue if this isn't our process or it's ended since we cached it.
                        var commandLine = GetCommandLine(process);
                        if (Path.IsPathRooted(processDefinition.Name))
                        {
                            if (!commandLine[0].Equals(processDefinition.Name, StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }
                        }
                        else
                        {
                            if (!process.ProcessName.Equals(processDefinition.Name, StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }
                        }

                        // Calculate a description for the running application.
                        var procInfo = FileVersionInfo.GetVersionInfo(commandLine[0]); string procDescription;
                        if (!string.IsNullOrWhiteSpace(processDefinition.Description))
                        {
                            procDescription = processDefinition.Description!;
                        }
                        else if (!string.IsNullOrWhiteSpace(procInfo.FileDescription))
                        {
                            procDescription = procInfo.FileDescription;
                        }
                        else
                        {
                            procDescription = process.ProcessName;
                        }

                        // Store the process information.
                        var runningProcess = new RunningProcess(process, procDescription, commandLine[0], commandLine.Length > 1 ? string.Join(" ", commandLine.Skip(1)) : null);
                        if ((null == processDefinition.Filter) || processDefinition.Filter(runningProcess))
                        {
                            runningProcesses.Add(runningProcess);
                        }
                    }
                }

                // Update the list of running processes.
                _runningProcesses = runningProcesses.OrderBy(runningProc => runningProc.Description).ToList().AsReadOnly();
                _processesToClose = _runningProcesses.GroupBy(runningProcess => runningProcess.Description).Select(runningProcess => runningProcess.First()).ToList().AsReadOnly();
                var processDescs = _processesToClose.Select(runningProcess => runningProcess.Description).ToList().AsReadOnly();

                // Raise the event if the list of processes to close has changed.
                if (!_lastProcessDescriptions.SequenceEqual(processDescs))
                {
                    _lastProcessDescriptions = processDescs;
                    ProcessListChanged?.Invoke(this, new ProcessesToCloseChangedEventArgs(_processesToClose));
                }

                // Wait for the specified interval before polling again.
                try
                {
                    await Task.Delay(_pollInterval, token).ConfigureAwait(false);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Event that is raised when the list of processes to show on a CloseAppsDialog changes.
        /// </summary>
        public event EventHandler<ProcessesToCloseChangedEventArgs>? ProcessListChanged;

        /// <summary>
        /// Event that is raised when the list of running processes changes.
        /// </summary>
        public IReadOnlyList<RunningProcess> RunningProcesses
        {
            get
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(RunningProcessService));
                }
                return _runningProcesses;
            }
        }

        /// <summary>
        /// Gets the list of processes to display on a CloseAppsDialog.
        /// </summary>
        public IReadOnlyList<RunningProcess> ProcessesToClose
        {
            get
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(RunningProcessService));
                }
                return _processesToClose;
            }
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
                Stop(); _cancellationTokenSource.Dispose();
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
        private volatile IReadOnlyList<RunningProcess> _runningProcesses = new List<RunningProcess>().AsReadOnly();

        /// <summary>
        /// Gets the list of processes to display on a CloseAppsDialog.
        /// </summary>
        private volatile IReadOnlyList<RunningProcess> _processesToClose = new List<RunningProcess>().AsReadOnly();

        /// <summary>
        /// Gets the list of process descriptions.
        /// </summary>
        private volatile IReadOnlyList<string> _lastProcessDescriptions = new List<string>().AsReadOnly();

        /// <summary>
        /// Disposal flag for the <see cref="RunningProcessService"/> class.
        /// </summary>
        private bool _disposed = false;

        /// <summary>
        /// The task that polls for running processes.
        /// </summary>
        private Task? _pollingTask;

        /// <summary>
        /// The interval at which to poll for running processes.
        /// </summary>
        private readonly TimeSpan _pollInterval;

        /// <summary>
        /// The cancellation token source for the polling task.
        /// </summary>
        private readonly CancellationTokenSource _cancellationTokenSource = new();

        /// <summary>
        /// The caller's specified process definitions.
        /// </summary>
        private readonly ProcessDefinition[] _processDefinitions;
    }
}
