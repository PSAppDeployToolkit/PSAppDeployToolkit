using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using PSADT.UserInterface.Utilities;

namespace PSADT.UserInterface.Services
{
    /// <summary>
    /// Process evaluation service to track and manage running processes
    /// </summary>
    public class ProcessEvaluationService : IProcessEvaluationService, IDisposable
    {
        private readonly ManagementEventWatcher? _processStartWatcher;
        private readonly ManagementEventWatcher? _processStopWatcher;
        private readonly ConcurrentDictionary<string, AppProcessInfo> _processCache;
        private readonly ConcurrentDictionary<string, byte> _trackedProcessNames;
        private readonly CancellationTokenSource _serviceCancellationTokenSource;
        private readonly object _syncLock = new();
        private bool _isDisposed;

        /// <summary>
        /// Event raised when a tracked process starts
        /// </summary>
        public event EventHandler<AppProcessInfo>? ProcessStarted;

        /// <summary>
        /// Event raised when a tracked process exits
        /// </summary>
        public event EventHandler<AppProcessInfo>? ProcessExited;

        /// <summary>
        /// Creates a new instance of ProcessEvaluationService
        /// </summary>
        public ProcessEvaluationService()
        {
            _processCache = new ConcurrentDictionary<string, AppProcessInfo>(StringComparer.OrdinalIgnoreCase);
            _trackedProcessNames = new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase);
            _serviceCancellationTokenSource = new CancellationTokenSource();

            try
            {
                // Set up WMI event watchers for process start/stop events
                _processStartWatcher = new ManagementEventWatcher(new WqlEventQuery("SELECT * FROM Win32_ProcessStartTrace"));
                _processStartWatcher.EventArrived += ProcessStartWatcher_EventArrived;
                _processStartWatcher.Start();

                _processStopWatcher = new ManagementEventWatcher(new WqlEventQuery("SELECT * FROM Win32_ProcessStopTrace"));
                _processStopWatcher.EventArrived += ProcessStopWatcher_EventArrived;
                _processStopWatcher.Start();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing process watchers: {ex.Message}");
                // Service will continue to function without real-time monitoring
            }
        }

        /// <summary>
        /// Evaluates which processes from the provided list are currently running
        /// </summary>
        /// <param name="appsToClose">List of applications to check</param>
        /// <returns>List of running applications</returns>
        public List<AppProcessInfo> EvaluateRunningProcesses(List<AppProcessInfo> appsToClose)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(ProcessEvaluationService));

            if (appsToClose == null || appsToClose.Count == 0)
                return [];

            var result = new List<AppProcessInfo>();
            var cacheUpdateTime = DateTime.Now.Subtract(TimeSpan.FromSeconds(5));

            // Register all processes for tracking
            foreach (var app in appsToClose.Where(a => !string.IsNullOrWhiteSpace(a.ProcessName)))
            {
                _trackedProcessNames.TryAdd(app.ProcessName, 0);
            }

            // Check each process in the list
            foreach (var app in appsToClose.Where(a => !string.IsNullOrWhiteSpace(a.ProcessName)))
            {
                AppProcessInfo? processInfo = null;

                // Check cache first
                if (_processCache.TryGetValue(app.ProcessName, out var cachedInfo))
                {
                    if (cachedInfo.LastUpdated >= cacheUpdateTime && IsProcessRunning(app.ProcessName))
                    {
                        processInfo = cachedInfo;
                    }
                }

                // If not in cache or cache is outdated, get fresh info
                if (processInfo == null)
                {
                    processInfo = GetProcessInfo(app.ProcessName);
                }

                // If process is running, merge app-specific overrides and add to result
                if (processInfo != null)
                {
                    var finalInfo = new AppProcessInfo(
                        app.ProcessName,
                        app.ProcessDescription ?? processInfo.ProcessDescription,
                        app.ProductName ?? processInfo.ProductName,
                        app.PublisherName ?? processInfo.PublisherName,
                        app.Icon ?? processInfo.Icon,
                        DateTime.Now
                    );

                    // Update the cache
                    _processCache[app.ProcessName] = finalInfo;
                    result.Add(finalInfo);
                }
            }

            return [.. result.OrderBy(x => x.ProcessDescription ?? x.ProcessName)];
        }

        /// <summary>
        /// Asynchronously evaluates which processes from the provided list are currently running
        /// </summary>
        /// <param name="appsToClose">List of applications to check</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of running applications</returns>
        public async Task<List<AppProcessInfo>> EvaluateRunningProcessesAsync(List<AppProcessInfo> appsToClose, CancellationToken cancellationToken)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(ProcessEvaluationService));

            if (appsToClose == null || appsToClose.Count == 0)
                return [];

            var result = new List<AppProcessInfo>();
            var cacheUpdateTime = DateTime.Now.Subtract(TimeSpan.FromSeconds(5));

            // Register all processes for tracking
            foreach (var app in appsToClose.Where(a => !string.IsNullOrWhiteSpace(a.ProcessName)))
            {
                _trackedProcessNames.TryAdd(app.ProcessName, 0);
            }

            var tasks = appsToClose
                .Where(a => !string.IsNullOrWhiteSpace(a.ProcessName))
                .Select(async app =>
                {
                    AppProcessInfo? processInfo = null;

                    // Check cache first with a quick process existence check
                    if (_processCache.TryGetValue(app.ProcessName, out var cachedInfo))
                    {
                        if (cachedInfo.LastUpdated >= cacheUpdateTime && IsProcessRunning(app.ProcessName))
                        {
                            processInfo = cachedInfo;
                        }
                    }

                    // If not in cache or cache is outdated, get fresh info
                    if (processInfo == null)
                    {
                        processInfo = await GetProcessInfoAsync(app.ProcessName, cancellationToken).ConfigureAwait(false);
                    }

                    // If process is running, merge app-specific overrides and add to result
                    if (processInfo != null)
                    {
                        var finalInfo = new AppProcessInfo(
                            app.ProcessName,
                            app.ProcessDescription ?? processInfo.ProcessDescription,
                            app.ProductName ?? processInfo.ProductName,
                            app.PublisherName ?? processInfo.PublisherName,
                            app.Icon ?? processInfo.Icon,
                            DateTime.Now
                        );

                        // Update the cache
                        _processCache[app.ProcessName] = finalInfo;
                        return finalInfo;
                    }

                    return null;
                });

            var processInfos = await Task.WhenAll(tasks).ConfigureAwait(false);

            foreach (var info in processInfos.Where(i => i != null))
            {
                result.Add(info!);
            }

            return [.. result.OrderBy(x => x.ProcessDescription ?? x.ProcessName)];
        }

        /// <summary>
        /// Checks if a process with the specified name is running
        /// </summary>
        /// <param name="processName">Name of the process</param>
        /// <returns>True if running, false otherwise</returns>
        public bool IsProcessRunning(string processName)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(ProcessEvaluationService));

            if (string.IsNullOrWhiteSpace(processName))
                return false;

            // Quick check using cached info
            if (_processCache.TryGetValue(processName, out var cachedInfo))
            {
                if ((DateTime.Now - cachedInfo.LastUpdated) <= TimeSpan.FromSeconds(1))
                    return true;
            }

            // Direct process check
            return Process.GetProcessesByName(processName).Length > 0;
        }

        /// <summary>
        /// Attempts to gracefully close a process, falling back to termination if necessary
        /// </summary>
        /// <param name="processName">Name of the process to close</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successfully closed, false otherwise</returns>
        public async Task<bool> CloseProcessAsync(string processName, CancellationToken cancellationToken)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(ProcessEvaluationService));

            if (string.IsNullOrWhiteSpace(processName))
                return false;

            var processes = Process.GetProcessesByName(processName);
            if (processes.Length == 0)
            {
                _processCache.TryRemove(processName, out _);
                return false;
            }

            bool allClosed = true;

            foreach (var process in processes)
            {
                using (process)
                {
                    try
                    {
                        // Try to close gracefully first
                        process.CloseMainWindow();

                        // Wait up to 5 seconds for graceful exit
                        if (!await Task.Run(() => process.WaitForExit(5000), cancellationToken).ConfigureAwait(false))
                        {
                            // Force termination if graceful exit fails
                            process.Kill();
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        // Process has already exited - consider this a success
                    }
                    catch (Win32Exception ex)
                    {
                        Debug.WriteLine($"Error closing process {processName}: {ex.Message}");
                        allClosed = false;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error closing process {processName}: {ex.Message}");
                        allClosed = false;
                    }
                }
            }

            // If all processes were closed, remove from cache
            if (allClosed)
            {
                _processCache.TryRemove(processName, out _);
            }

            return allClosed;
        }

        /// <summary>
        /// Gets information about a specific process
        /// </summary>
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

                        // Extract process file path
                        var processFullFileName = await Task.Run(() => process.GetMainModuleFileName(), cancellationToken)
                            .ConfigureAwait(false);

                        if (string.IsNullOrWhiteSpace(processFullFileName))
                            continue;

                        cancellationToken.ThrowIfCancellationRequested();

                        // Get version info
                        var processFileVersionInfo = await Task.Run(() => FileVersionInfo.GetVersionInfo(processFullFileName), cancellationToken)
                            .ConfigureAwait(false);

                        // Extract icon
                        ImageSource? icon = null;
                        await Task.Run(() =>
                        {
                            try
                            {
                                using var extractedIcon = process.GetIcon(true);
                                if (extractedIcon != null)
                                {
                                    using var bitmap = extractedIcon.ToBitmap();
                                    icon = bitmap.ConvertToImageSource();
                                    icon?.Freeze(); // Make thread-safe
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Error extracting icon: {ex.Message}");
                            }
                        }, cancellationToken).ConfigureAwait(false);

                        cancellationToken.ThrowIfCancellationRequested();

                        // Create and return the process info
                        return new AppProcessInfo(
                            processName,
                            processFileVersionInfo.FileDescription,
                            processFileVersionInfo.ProductName,
                            processFileVersionInfo.CompanyName,
                            icon
                        );
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error getting process info for {processName}: {ex.Message}");
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Gets information about a specific process (synchronous version)
        /// </summary>
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
                        // Extract process file path
                        var processFullFileName = process.GetMainModuleFileName();
                        if (string.IsNullOrWhiteSpace(processFullFileName))
                            continue;

                        // Get version info
                        var processFileVersionInfo = FileVersionInfo.GetVersionInfo(processFullFileName);

                        // Extract icon
                        ImageSource? icon = null;
                        try
                        {
                            using var extractedIcon = process.GetIcon(true);
                            if (extractedIcon != null)
                            {
                                using var bitmap = extractedIcon.ToBitmap();
                                icon = bitmap.ConvertToImageSource();
                                icon?.Freeze(); // Make thread-safe
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error extracting icon: {ex.Message}");
                        }

                        // Create and return the process info
                        return new AppProcessInfo(
                            processName,
                            processFileVersionInfo.FileDescription,
                            processFileVersionInfo.ProductName,
                            processFileVersionInfo.CompanyName,
                            icon
                        );
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error getting process info for {processName}: {ex.Message}");
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Handler for process start events from WMI
        /// </summary>
        private async void ProcessStartWatcher_EventArrived(object sender, EventArrivedEventArgs e)
        {
            if (_isDisposed)
                return;

            if (e.NewEvent.Properties["ProcessName"]?.Value is string processName)
            {
                bool shouldTrack = _trackedProcessNames.ContainsKey(processName);

                if (shouldTrack)
                {
                    try
                    {
                        var processInfo = await GetProcessInfoAsync(processName, _serviceCancellationTokenSource.Token)
                            .ConfigureAwait(false);

                        if (processInfo != null)
                        {
                            // Update cache
                            _processCache[processName] = processInfo;

                            // Raise event
                            ProcessStarted?.Invoke(this, processInfo);
                        }
                    }
                    catch (TaskCanceledException)
                    {
                        // Service is shutting down, ignore
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error in ProcessStartWatcher_EventArrived: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Handler for process stop events from WMI
        /// </summary>
        private void ProcessStopWatcher_EventArrived(object sender, EventArrivedEventArgs e)
        {
            if (_isDisposed)
                return;

            if (e.NewEvent.Properties["ProcessName"]?.Value is string processName)
            {
                bool shouldTrack = _trackedProcessNames.ContainsKey(processName);

                if (shouldTrack)
                {
                    if (_processCache.TryRemove(processName, out var processInfo))
                    {
                        ProcessExited?.Invoke(this, processInfo);
                    }
                }
            }
        }

        #region IDisposable Implementation

        /// <summary>
        /// Disposes managed and unmanaged resources
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                lock (_syncLock)
                {
                    if (!_isDisposed && disposing)
                    {
                        // Stop and dispose WMI watchers
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

                        // Cancel and dispose token source
                        _serviceCancellationTokenSource.Cancel();
                        _serviceCancellationTokenSource.Dispose();

                        // Clear collections
                        _processCache.Clear();
                        _trackedProcessNames.Clear();
                    }

                    _isDisposed = true;
                }
            }
        }

        /// <summary>
        /// Disposes all resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
