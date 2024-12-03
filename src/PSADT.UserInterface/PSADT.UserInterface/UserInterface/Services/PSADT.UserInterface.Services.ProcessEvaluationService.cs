using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Management;
using System.Windows.Media;
using PSADT.UserInterface.Utilities;

namespace PSADT.UserInterface.Services
{
    /// <summary>
    /// Process evaluation service to track running processes
    /// </summary>
    public class ProcessEvaluationService : IProcessEvaluationService, IDisposable
    {
        private readonly ManagementEventWatcher? _processStartWatcher;
        private readonly ManagementEventWatcher? _processStopWatcher;
        private readonly ConcurrentDictionary<string, AppProcessInfo> _trackedProcesses;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private volatile bool _disposed = false;
        private readonly ConcurrentDictionary<string, byte> _processesToTrack;
        private readonly object _lock = new();

        /// <summary>
        /// Event handler for when a tracked process starts
        /// </summary>
        public event EventHandler<AppProcessInfo>? ProcessStarted;

        /// <summary>
        /// Event handler for when a tracked process exits
        /// </summary>
        public event EventHandler<AppProcessInfo>? ProcessExited;

        /// <summary>
        /// Constructor for real-time process evaluation
        /// </summary>
        public ProcessEvaluationService()
        {
            _trackedProcesses = new ConcurrentDictionary<string, AppProcessInfo>(StringComparer.OrdinalIgnoreCase);
            _processesToTrack = new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase);
            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                _processStartWatcher = new ManagementEventWatcher(new WqlEventQuery("SELECT * FROM Win32_ProcessStartTrace"));
                _processStartWatcher.EventArrived += ProcessStartWatcher_EventArrived;
                _processStartWatcher.Start();

                _processStopWatcher = new ManagementEventWatcher(new WqlEventQuery("SELECT * FROM Win32_ProcessStopTrace"));
                _processStopWatcher.EventArrived += ProcessStopWatcher_EventArrived;
                _processStopWatcher.Start();
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error initializing process watchers: {ex.Message}");
                // The service will continue to function without real-time process monitoring
            }
        }

        /// <summary>
        /// Evaluates running processes asynchronously.
        /// Returns only processes that are in appsToClose and are currently running.
        /// </summary>
        public async Task<List<AppProcessInfo>> EvaluateRunningProcessesAsync(List<AppProcessInfo> appsToClose, CancellationToken cancellationToken)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ProcessEvaluationService));

            var result = new List<AppProcessInfo>();

            // Clear processes to track in a thread-safe manner
            lock (_lock)
            {
                _processesToTrack.Clear();
            }

            IEnumerable<Task<AppProcessInfo?>> tasks = appsToClose
                .Distinct()
                .Select(async app =>
                {
                    string processName = app.ProcessName!;
                    // Add to processes to track
                    _processesToTrack.TryAdd(processName, 0);

                    AppProcessInfo? updatedInfo;
                    if (_trackedProcesses.TryGetValue(processName, out var cachedInfo))
                    {
                        if (DateTime.Now - cachedInfo.LastUpdated <= TimeSpan.FromSeconds(5))
                        {
                            updatedInfo = cachedInfo;
                        }
                        else
                        {
                            updatedInfo = await GetProcessInfoAsync(processName, cancellationToken).ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        updatedInfo = await GetProcessInfoAsync(processName, cancellationToken).ConfigureAwait(false);
                    }

                    if (updatedInfo != null)
                    {
                        // Optionally, merge app-specific overrides here if needed
                        var finalInfo = new AppProcessInfo(
                            processName,
                            app.ProcessDescription ?? updatedInfo.ProcessDescription,
                            app.ProductName ?? updatedInfo.ProductName,
                            app.PublisherName ?? updatedInfo.PublisherName,
                            app.Icon ?? updatedInfo.Icon,
                            updatedInfo.LastUpdated
                        );

                        // Update the cache with the new info
                        if (_trackedProcesses.TryUpdate(processName, finalInfo, updatedInfo))
                        {
                            return finalInfo;
                        }
                    }

                    // Process is not running; do not include in the result
                    return null;
                });

            AppProcessInfo?[] processInfos = await Task.WhenAll(tasks).ConfigureAwait(false);
            foreach (AppProcessInfo? info in processInfos)
            {
                if (info != null)
                {
                    result.Add(info);
                }
            }

            return result;
        }

        /// <summary>
        /// Evaluates running processes synchronously.
        /// Returns only processes that are in appsToClose and are currently running.
        /// </summary>
        public List<AppProcessInfo> EvaluateRunningProcesses(List<AppProcessInfo> appsToClose)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ProcessEvaluationService));

            var result = new List<AppProcessInfo>();

            // Clear processes to track in a thread-safe manner
            lock (_lock)
            {
                _processesToTrack.Clear();
            }

            foreach (AppProcessInfo app in appsToClose.Distinct().ToList())
            {
                if (string.IsNullOrWhiteSpace(app.ProcessName))
                    continue;

                string processName = app.ProcessName!;
                // Add to processes to track
                _processesToTrack.TryAdd(processName, 0);

                AppProcessInfo? updatedInfo;
                if (_trackedProcesses.TryGetValue(processName, out var cachedInfo))
                {
                    if (DateTime.Now - cachedInfo.LastUpdated <= TimeSpan.FromSeconds(20))
                    {
                        updatedInfo = cachedInfo;
                    }
                    else
                    {
                        updatedInfo = GetProcessInfo(processName);
                    }
                }
                else
                {
                    updatedInfo = GetProcessInfo(processName);
                }

                if (updatedInfo != null)
                {
                    // Optionally, merge app-specific overrides here if needed
                    var finalInfo = new AppProcessInfo(
                        processName,
                        app.ProcessDescription ?? updatedInfo.ProcessDescription,
                        app.ProductName ?? updatedInfo.ProductName,
                        app.PublisherName ?? updatedInfo.PublisherName,
                        app.Icon ?? updatedInfo.Icon,
                        updatedInfo.LastUpdated
                    );

                    // Update the cache with the new info
                    if (_trackedProcesses.TryUpdate(processName, finalInfo, updatedInfo))
                    {
                        result.Add(finalInfo);
                    }
                }

                // If updatedInfo is null, the process is not running; do not add to the result
            }

            return [.. result.OrderBy(x => x.ProcessDescription)];
        }

        private async Task<AppProcessInfo?> GetProcessInfoAsync(string processName, CancellationToken cancellationToken)
        {
            var processes = Process.GetProcessesByName(processName);
            if (processes.Length == 0)
                return null;

            foreach (var process in processes)
            {
                using (process)
                {
                    try
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var processFullFileName = await Task.Run(() => process.GetMainModuleFileName(), cancellationToken).ConfigureAwait(false);
                        if (string.IsNullOrWhiteSpace(processFullFileName))
                            continue;

                        cancellationToken.ThrowIfCancellationRequested();

                        var processFileVersionInfo = await Task.Run(() => FileVersionInfo.GetVersionInfo(processFullFileName), cancellationToken).ConfigureAwait(false);

                        ImageSource? icon = null;
                        await Task.Run(() =>
                        {
                            using var extractedIcon = process.GetIcon(true); // Retrieve large icon
                            if (extractedIcon != null)
                            {
                                icon = extractedIcon.ToBitmap().ConvertToImageSource();
                                icon?.Freeze(); // Make the ImageSource thread-safe
                            }
                        }, cancellationToken).ConfigureAwait(false);

                        cancellationToken.ThrowIfCancellationRequested();

                        var newInfo = new AppProcessInfo(
                            processName,
                            processFileVersionInfo.FileDescription,
                            processFileVersionInfo.ProductName,
                            processFileVersionInfo.CompanyName,
                            icon
                        );

                        // Update the cache with the new info
                        _trackedProcesses[processName] = newInfo;

                        return newInfo;
                    }
                    catch (OperationCanceledException)
                    {
                        // Handle cancellation
                        throw;
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine($"Error getting process info for {processName}: {ex.Message}");
                        // Continue to next process if any
                    }
                }
            }

            return null;
        }

        private AppProcessInfo? GetProcessInfo(string processName)
        {
            var processes = Process.GetProcessesByName(processName);
            if (processes.Length == 0)
                return null;

            foreach (var process in processes)
            {
                using (process)
                {
                    try
                    {
                        var processFullFileName = process.GetMainModuleFileName();
                        if (string.IsNullOrWhiteSpace(processFullFileName))
                            continue;

                        var processFileVersionInfo = FileVersionInfo.GetVersionInfo(processFullFileName);

                        ImageSource? icon = null;
                        using var extractedIcon = process.GetIcon(true); // Retrieve large icon
                        if (extractedIcon != null)
                        {
                            using var bitmap = extractedIcon.ToBitmap();
                            icon = bitmap.ConvertToImageSource();
                            icon?.Freeze(); // Make the ImageSource thread-safe
                        }

                        var newInfo = new AppProcessInfo(
                            processName,
                            processFileVersionInfo.FileDescription,
                            processFileVersionInfo.ProductName,
                            processFileVersionInfo.CompanyName,
                            icon
                        );

                        // Update the cache with the new info
                        _trackedProcesses[processName] = newInfo;

                        return newInfo;
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine($"Error getting process info for {processName}: {ex.Message}");
                        // Continue to next process if any
                    }
                }
            }

            return null;
        }

        private async void ProcessStartWatcher_EventArrived(object sender, EventArrivedEventArgs e)
        {
            if (e.NewEvent.Properties["ProcessName"]?.Value is string processName)
            {
                bool shouldTrack;
                lock (_lock)
                {
                    shouldTrack = _processesToTrack.ContainsKey(processName);
                }

                if (shouldTrack)
                {
                    try
                    {
                        var processInfo = await GetProcessInfoAsync(processName, _cancellationTokenSource.Token).ConfigureAwait(false);
                        if (processInfo != null)
                        {
                            ProcessStarted?.Invoke(this, processInfo);
                        }
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine($"Error in ProcessStartWatcher_EventArrived: {ex.Message}");
                    }
                }
            }
        }

        private void ProcessStopWatcher_EventArrived(object sender, EventArrivedEventArgs e)
        {
            if (e.NewEvent.Properties["ProcessName"]?.Value is string processName)
            {
                bool shouldTrack;
                lock (_lock)
                {
                    shouldTrack = _processesToTrack.ContainsKey(processName);
                }

                if (shouldTrack)
                {
                    if (_trackedProcesses.TryRemove(processName, out var processInfo))
                    {
                        ProcessExited?.Invoke(this, processInfo);
                    }
                }
            }
        }

        /// <summary>
        /// Checks if a process is running
        /// </summary>
        /// <param name="processName"></param>
        /// <returns></returns>
        /// <exception cref="ObjectDisposedException"></exception>
        public bool IsProcessRunning(string processName)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ProcessEvaluationService));

            if (string.IsNullOrWhiteSpace(processName))
                return false;

            if (_trackedProcesses.TryGetValue(processName, out var cachedInfo))
            {
                if (DateTime.Now - cachedInfo.LastUpdated <= TimeSpan.FromSeconds(1))
                    return true;
            }

            return Process.GetProcessesByName(processName).Length > 0;
        }

        /// <summary>
        /// Closes a process
        /// </summary>
        /// <param name="processName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="ObjectDisposedException"></exception>
        public async Task<bool> CloseProcessAsync(string processName, CancellationToken cancellationToken)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ProcessEvaluationService));

            if (string.IsNullOrWhiteSpace(processName))
                return false;

            var processes = Process.GetProcessesByName(processName);
            if (processes.Length == 0)
            {
                _trackedProcesses.TryRemove(processName, out _);
                return false;
            }

            bool allClosed = true;

            foreach (var process in processes)
            {
                using (process)
                {
                    try
                    {
                        process.CloseMainWindow();

                        if (!await Task.Run(() => process.WaitForExit(5000), cancellationToken).ConfigureAwait(false))
                        {
                            process.Kill();
                        }
                    }
                    catch (InvalidOperationException ex)
                    {
                        Trace.WriteLine($"Process {processName} has already exited: {ex.Message}");
                    }
                    catch (Win32Exception ex)
                    {
                        Trace.WriteLine($"Error closing process {processName}: {ex.Message}");
                        allClosed = false;
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine($"Error closing process {processName}: {ex.Message}");
                        allClosed = false;
                    }
                }
            }

            _trackedProcesses.TryRemove(processName, out _);
            return allClosed;
        }

        /// <summary>
        /// Disposes the service
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                lock (_lock)
                {
                    if (!_disposed)
                    {
                        if (disposing)
                        {
                            if (_processStartWatcher != null)
                            {
                                _processStartWatcher.EventArrived -= ProcessStartWatcher_EventArrived;
                                _processStartWatcher.Stop();
                                _processStartWatcher.Dispose();
                            }

                            if (_processStopWatcher != null)
                            {
                                _processStopWatcher.EventArrived -= ProcessStopWatcher_EventArrived;
                                _processStopWatcher.Stop();
                                _processStopWatcher.Dispose();
                            }

                            _cancellationTokenSource?.Cancel();
                            _cancellationTokenSource?.Dispose();

                            _trackedProcesses?.Clear();
                            _processesToTrack?.Clear();
                        }

                        _disposed = true;
                    }
                }
            }
        }

        /// <summary>
        /// Disposes the service
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
