using System;
using System.Threading;
using System.Threading.Tasks;
using PSADT.Logging.Models;
using PSADT.Logging.Interfaces;
using PSADT.Diagnostics.StackTraces;

namespace PSADT.Logging
{
    /// <summary>
    /// UnifiedLogger provides static logging Builder method that delegate to a singleton Logger instance.
    /// </summary>
    public static class UnifiedLogger
    {
        private static readonly Lazy<Logger> _singletonLogger = CreateLogger();
        private static SemaphoreSlim? _configureSemaphore = new SemaphoreSlim(1, 1);
        private static SemaphoreSlim? _disposeSemaphore = new SemaphoreSlim(1, 1);
        private static int _isDisposed = 0;
        private static bool _processExitSubscribed = false;
        private static readonly object _subscriptionLock = new object();

        static UnifiedLogger()
        {
            try
            {
                // Automatically subscribe DisposeAsync to the process exit event.
                SubscribeToProcessExit();
            }
            catch (Exception ex)
            {
                // Handle any exceptions to prevent unhandled exceptions in static constructor
                SingletonLogger.LogToEventLogAsync("Error occured in the static constructor of the UnifiedLogger class.", ex);
            }
        }

        private static Lazy<Logger> CreateLogger()
        {
            return new Lazy<Logger>(() =>
            {
                var defaultLogOptions = LogOptions.CreateBuilder()
                    .SetLogFilePathComponents()
                    .SetMinimumLogLevel(LogLevel.Debug)
                    .SetTextSeparator(" | ")
                    .SetLogFormat(TextLogFormat.Standard)
                    .SubscribeToUnhandledException(true)
                    .SubscribeToUnobservedTaskException(true)
                    .SubscribeToOnProcessExitAndCallDispose(false)
                    .SetMaxLogFileSizeInMegabytes(10)
                    .SetErrorParserConfig(StackParserConfig.Create().Build())
                    .Build();

                var logger = new Logger(defaultLogOptions);

                // Add default destinations
                //logger.AddLogDestination(new ConsoleLogDestination());
                //logger.AddLogDestination(new FileLogDestination(defaultLogOptions));

                return logger;
            });
        }

        public static LogEntryBuilder Create(
            [System.Runtime.CompilerServices.CallerMemberName] string? callerMethodName = null,
            [System.Runtime.CompilerServices.CallerFilePath] string? callerFileName = null,
            [System.Runtime.CompilerServices.CallerLineNumber] int? callerLineNumber = null)
        {
            return new LogEntryBuilder(SingletonLogger, new CallerContext(callerMethodName, callerFileName, callerLineNumber));
        }

        private static Logger SingletonLogger => _singletonLogger.Value;

        /// <summary>
        /// Adds a log destination to the singleton Logger instance.
        /// </summary>
        /// <param name="destination">The log destination to add.</param>
        public static void AddLogDestination(ILogDestination destination)
        {
            SingletonLogger.AddLogDestination(destination);
        }

        /// <summary>
        /// Removes a log destination from the singleton Logger instance.
        /// </summary>
        /// <param name="destination">The log destination to remove.</param>
        public static void RemoveLogDestination(ILogDestination destination)
        {
            SingletonLogger.RemoveLogDestination(destination);
        }

        /// <summary>
        /// Updates the singleton Logger instance's configuration dynamically.
        /// Thread-safe.
        /// </summary>
        /// <param name="newOptions">The new LogOptions to apply.</param>
        /// <param name="cancellationToken">The cancellation token to observe.</param>
        public static async Task ConfigureAsync(LogOptions newOptions, CancellationToken cancellationToken = default)
        {
            if (newOptions == null) throw new ArgumentNullException(nameof(newOptions));

            if (_configureSemaphore != null)
            {
                await _configureSemaphore.WaitAsync(cancellationToken);
            }

            try
            {
                await SingletonLogger.UpdateConfiguration(newOptions);
            }
            finally
            {
                _configureSemaphore?.Release();
            }
        }

        /// <summary>
        /// Retrieves queue statistics for the singleton Logger instance.
        /// </summary>
        /// <returns>QueueStats object containing queue depth and dropped messages count.</returns>
        public static QueueStats GetQueueStats()
        {
            return SingletonLogger.GetQueueStats();
        }

        /// <summary>
        /// Subscribes DisposeAsync to the ProcessExit event.
        /// Idempotent subscription.
        /// </summary>
        public static void SubscribeToProcessExit()
        {
            lock (_subscriptionLock)
            {
                if (!_processExitSubscribed)
                {
                    AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
                    _processExitSubscribed = true;
                }
            }
        }

        /// <summary>
        /// Unsubscribes DisposeAsync from the ProcessExit event.
        /// </summary>
        private static void UnsubscribeFromProcessExit()
        {
            lock (_subscriptionLock)
            {
                if (_processExitSubscribed)
                {
                    AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
                    _processExitSubscribed = false;
                }
            }
        }

        /// <summary>
        /// ProcessExit event handler to dispose of the Logger.
        /// </summary>
        private static void OnProcessExit(object? sender, EventArgs e)
        {
            // Dispose in a fire-and-forget manner without blocking
            Task.Run(DisposeAsync).ConfigureAwait(false);
        }

        /// <summary>
        /// Disposes the singleton Logger instance asynchronously.
        /// </summary>
        public static async Task DisposeAsync()
        {
            if (_disposeSemaphore != null)
            {
                await _disposeSemaphore.WaitAsync();
            }

            try
            {
                if (Interlocked.CompareExchange(ref _isDisposed, 1, 0) == 1)
                    return;

                await SingletonLogger.DisposeAsync();
                UnsubscribeFromProcessExit();
            }
            finally
            {
                Interlocked.Exchange(ref _configureSemaphore, null)?.Dispose();
                Interlocked.Exchange(ref _disposeSemaphore, null)?.Dispose();
            }
        }
    }
}
