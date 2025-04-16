using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Management;
using System.Windows.Media;
using PSADT.UserInterface.Utilities;

namespace PSADT.UserInterface.Services
{
    /// <summary>
    /// Process evaluation service to track and manage running processes
    /// </summary>
    public class ProcessEvaluationService : IProcessEvaluationService, IDisposable
    {
        /// <summary>
        /// Creates a new instance of ProcessEvaluationService
        /// </summary>
        public ProcessEvaluationService()
        {
            _processCache = new ConcurrentDictionary<string, AppProcessInfo>(StringComparer.OrdinalIgnoreCase);
            _trackedProcessNames = new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase);
            _serviceCancellationTokenSource = new CancellationTokenSource();

            // Set up WMI event watchers for process start/stop events
            _processStartWatcher = new ManagementEventWatcher(new WqlEventQuery("SELECT * FROM Win32_ProcessStartTrace"));
            _processStartWatcher.EventArrived += ProcessStartWatcher_EventArrived;
            _processStartWatcher.Start();

            _processStopWatcher = new ManagementEventWatcher(new WqlEventQuery("SELECT * FROM Win32_ProcessStopTrace"));
            _processStopWatcher.EventArrived += ProcessStopWatcher_EventArrived;
            _processStopWatcher.Start();
        }

        /// <summary>
        /// Asynchronously evaluates which processes from the provided list are currently running
        /// </summary>
        /// <param name="appsToClose">List of applications to check</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of running applications</returns>
        public async Task<List<AppProcessInfo>> EvaluateRunningProcessesAsync(List<AppProcessInfo> appsToClose, CancellationToken cancellationToken)
        {
            // Check if the service is disposed or the input is null
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(ProcessEvaluationService));
            }
            if (appsToClose == null || appsToClose.Count == 0)
            {
                return [];
            }

            // Register all processes for tracking
            foreach (var app in appsToClose.Where(a => !string.IsNullOrWhiteSpace(a.ProcessName)))
            {
                _trackedProcessNames.TryAdd(app.ProcessName, 0);
            }

            // Check if any processes are running
            var result = new List<AppProcessInfo>();
            var cacheUpdateTime = DateTime.Now.Subtract(TimeSpan.FromSeconds(5));
            var tasks = appsToClose.Where(a => !string.IsNullOrWhiteSpace(a.ProcessName)).Select(async app =>
                {
                    // Check cache first with a quick process existence check
                    AppProcessInfo? processInfo = null;
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

            // Wait for all tasks to complete and filter out null results before returning
            return [.. (await Task.WhenAll(tasks).ConfigureAwait(false)).Where(i => i != null).ToList().OrderBy(x => x!.ProcessDescription ?? x.ProcessName)];
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
            {
                throw new ObjectDisposedException(nameof(ProcessEvaluationService));
            }

            if (string.IsNullOrWhiteSpace(processName))
            {
                return false;
            }

            // Return early if there's no running processes
            var processes = Process.GetProcessesByName(processName);
            if (processes.Length == 0)
            {
                _processCache.TryRemove(processName, out _);
                return false;
            }

            // Attempt to close each process
            bool allClosed = true;
            foreach (var process in processes)
            {
                using (process)
                {
                    try
                    {
                        // Try to close gracefully first, waiting up to 5 seconds for graceful exit
                        process.CloseMainWindow();
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

            // If all processes were closed, remove from cache before returning the state
            if (allClosed)
            {
                _processCache.TryRemove(processName, out _);
            }
            return allClosed;
        }

        /// <summary>
        /// Checks if a process with the specified name is running
        /// </summary>
        /// <param name="processName">Name of the process</param>
        /// <returns>True if running, false otherwise</returns>
        public bool IsProcessRunning(string processName)
        {
            // Check if the service is disposed or the input is null
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(ProcessEvaluationService));
            }
            if (string.IsNullOrWhiteSpace(processName))
            {
                return false;
            }

            // Quick check using cached info, otherwise return direct check
            if (_processCache.TryGetValue(processName, out var cachedInfo) && (DateTime.Now - cachedInfo.LastUpdated) <= TimeSpan.FromSeconds(1))
            {
                return true;
            }
            return Process.GetProcessesByName(processName).Length > 0;
        }

        /// <summary>
        /// Gets information about a specific process
        /// </summary>
        private async Task<AppProcessInfo?> GetProcessInfoAsync(string processName, CancellationToken cancellationToken)
        {
            // Return early if there's no running processes
            var processes = Process.GetProcessesByName(processName);
            if (processes.Length == 0)
            {
                return null;
            }

            // Build an AppProcessInfo object for each process
            foreach (var process in processes)
            {
                using (process)
                {
                    try
                    {
                        // Extract process file path
                        cancellationToken.ThrowIfCancellationRequested();
                        var processFullFileName = await Task.Run(() => process.GetMainModuleFileName(), cancellationToken).ConfigureAwait(false);
                        if (string.IsNullOrWhiteSpace(processFullFileName))
                        {
                            continue;
                        }

                        // Get version info
                        cancellationToken.ThrowIfCancellationRequested();
                        var processFileVersionInfo = await Task.Run(() => FileVersionInfo.GetVersionInfo(processFullFileName), cancellationToken).ConfigureAwait(false);

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
        /// Handler for process start events from WMI
        /// </summary>
        private async void ProcessStartWatcher_EventArrived(object sender, EventArrivedEventArgs e)
        {
            // Return early if the service is disposed or the process name is not tracked
            if (_isDisposed || !(e.NewEvent.Properties["ProcessName"]?.Value is string processName) || !_trackedProcessNames.ContainsKey(processName))
            {
                return;
            }

            // Update cache and raise event
            try
            {
                var processInfo = await GetProcessInfoAsync(processName, _serviceCancellationTokenSource.Token).ConfigureAwait(false);
                if (processInfo != null)
                {
                    _processCache[processName] = processInfo;
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

        /// <summary>
        /// Handler for process stop events from WMI
        /// </summary>
        private void ProcessStopWatcher_EventArrived(object sender, EventArrivedEventArgs e)
        {
            // Return early if the service is disposed or the process name is not tracked
            if (_isDisposed || !(e.NewEvent.Properties["ProcessName"]?.Value is string processName) || !_trackedProcessNames.ContainsKey(processName))
            {
                return;
            }

            // Remove from cache and raise event
            if (_processCache.TryRemove(processName, out var processInfo))
            {
                ProcessExited?.Invoke(this, processInfo);
            }
        }

        /// <summary>
        /// Event raised when a tracked process starts
        /// </summary>
        public event EventHandler<AppProcessInfo>? ProcessStarted;

        /// <summary>
        /// Event raised when a tracked process exits
        /// </summary>
        public event EventHandler<AppProcessInfo>? ProcessExited;

        /// <summary>
        /// WMI watcher for process start events
        /// </summary>
        private readonly ManagementEventWatcher? _processStartWatcher;

        /// <summary>
        /// WMI watcher for process stop events
        /// </summary>
        private readonly ManagementEventWatcher? _processStopWatcher;

        /// <summary>
        /// Cache for process information
        /// </summary>
        private readonly ConcurrentDictionary<string, AppProcessInfo> _processCache;

        /// <summary>
        /// Dictionary to track process names
        /// </summary>
        private readonly ConcurrentDictionary<string, byte> _trackedProcessNames;

        /// <summary>
        /// Cancellation token source for service operations
        /// </summary>
        private readonly CancellationTokenSource _serviceCancellationTokenSource;

        /// <summary>
        /// Lock object for thread safety
        /// </summary>
        private readonly object _syncLock = new();

        /// <summary>
        /// Flag to indicate if the service has been disposed
        /// </summary>
        private bool _isDisposed;


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
