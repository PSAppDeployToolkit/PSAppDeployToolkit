using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using PSADT.Timer;
using PSADT.Logging;
using PSADT.Diagnostics.Exceptions;

namespace PSADT.Registry
{
    /// <summary>
    /// Provides functionality for loading and unloading registry hives asynchronously and synchronously.
    /// </summary>
    public class RegistryHiveLoader : IDisposable
    {
        private readonly TimeSpan _disposeDelay;
        private readonly TimerController _disposeTimerController;
        private Guid _timerId;
        private bool _isDisposed;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly RegistryKeyInfo _keyPathInfo;

        /// <summary>
        /// Initializes a new instance of the <see cref="RegistryHiveLoader"/> class with a specified dispose delay.
        /// </summary>
        /// <param name="keyPathInfo">The registry key path information.</param>
        /// <param name="disposeDelay">The delay before disposing the loader.</param>
        public RegistryHiveLoader(RegistryKeyInfo keyPathInfo, TimeSpan disposeDelay)
        {
            _keyPathInfo = keyPathInfo ?? throw new ArgumentNullException(nameof(keyPathInfo));
            _disposeDelay = disposeDelay;
            _disposeTimerController = new TimerController(disposeDelay, 1);
        }

        /// <summary>
        /// Asynchronously loads a registry hive.
        /// </summary>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        public async Task LoadHiveAsync(CancellationToken cancellationToken = default)
        {
            UnifiedLogger.Create()
                .Message($@"Attempting to mount registry hive file [{_keyPathInfo.HiveFilePath}] to path [{_keyPathInfo.HiveMountPath}] on machine [{_keyPathInfo.MachineName ?? "localhost"}].")
                .Severity(LogLevel.Verbose)
                .Log();

            await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(RegistryHiveLoader));
                }

                if (_keyPathInfo.MachineName != null)
                {
                    // Connect to remote registry
                    if (!RegistryUtils.RegConnectRegistry(_keyPathInfo.MachineName, _keyPathInfo.BaseKeyHive, out SafeRegistryHandle remoteHiveHandle))
                    {
                        ErrorHandler.ThrowSystemError($"Failed to connect to remote registry on [{_keyPathInfo.MachineName}].", SystemErrorType.Win32);
                    }

                    // Use the handle from the remote connection
                    _keyPathInfo.UpdateBaseKeyHive(remoteHiveHandle);
                }

                RegistryUtils.RegLoadHiveFile(_keyPathInfo.HiveFilePath, _keyPathInfo.BaseKeyHive, _keyPathInfo.SubKey!);

                UnifiedLogger.Create()
                    .Message($@"Registry hive file [{_keyPathInfo.HiveFilePath}] successfully mounted at ")
                    .AppendMessage($@"[{_keyPathInfo.BaseKeyHive}\{_keyPathInfo.SubKey}] on machine [{_keyPathInfo.MachineName ?? "localhost"}].")
                    .Severity(LogLevel.Verbose).Log();

                StartDisposeTimerAsync();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Starts the disposal timer.
        /// </summary>
        public async Task StartDisposeTimerAsync()
        {
            _timerId = await _disposeTimerController.StartNewTimerAsync(() => Task.Run(() => Dispose()));
        }

        /// <summary>
        /// Pauses the disposal timer.
        /// </summary>
        public void PauseDisposeTimer() => _disposeTimerController.PauseTimer(_timerId);

        /// <summary>
        /// Resumes the disposal timer.
        /// </summary>
        public void ResumeDisposeTimer() => _disposeTimerController.ResumeTimer(_timerId);

        /// <summary>
        /// Restarts the disposal timer by resetting the timer to the original delay.
        /// </summary>
        public void RestartDisposeTimer() => _disposeTimerController.RestartTimer(_timerId);

        /// <summary>
        /// Stops the disposal timer and prevents automatic disposal.
        /// </summary>
        public void StopDisposeTimer()
        {
            _disposeTimerController.DisposeTimerAsync(_timerId).Wait();
        }

        /// <summary>
        /// Determines if the disposal timer is active.
        /// </summary>
        public bool IsDisposeTimerActive => _disposeTimerController.IsTimerActive(_timerId);

        /// <summary>
        /// Determines if the disposal timer is paused.
        /// </summary>
        public bool IsDisposeTimerPaused => _disposeTimerController.IsTimerPaused(_timerId);

        /// <summary>
        /// Disposes the resources used by the <see cref="RegistryHiveLoader"/>.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed) return;

            _semaphore.Wait();
            try
            {
                if (!_isDisposed)
                {
                    _isDisposed = true;
                    try
                    {
                        RegistryUtils.RegUnloadHiveFile(_keyPathInfo.BaseKeyHive, _keyPathInfo.SubKey!);
                    }
                    catch (Exception ex)
                    {
                        UnifiedLogger.Create()
                            .Message($@"Error dismounting registry hive file [{_keyPathInfo.HiveFilePath}] at subkey [{_keyPathInfo.BaseKeyHive}\{_keyPathInfo.SubKey!}] ")
                            .AppendMessage($@"on machine [{_keyPathInfo.MachineName ?? "localhost"}] during disposal.{Environment.NewLine}{ex.Message}")
                            .Error(ex).Severity(LogLevel.Error).ErrorCategory(ErrorType.InvalidOperation).Log();
                    }
                    finally
                    {
                        _semaphore?.Release();
                    }
                }
            }
            finally
            {
                _semaphore?.Dispose();
                _isDisposed = true;
            }
        }
    }
}
