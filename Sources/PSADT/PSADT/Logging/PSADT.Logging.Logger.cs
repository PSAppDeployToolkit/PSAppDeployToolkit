using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Management.Automation;
using System.Collections.Concurrent;
using PSADT.Logging.Models;
using PSADT.Logging.Utilities;
using PSADT.Logging.Interfaces;
using PSADT.Logging.Destinations;
using PSADT.Diagnostics.StackTraces;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace PSADT.Logging
{
    /// <summary>
    /// Logger provides logging capabilities with instance methods.
    /// Implements IDisposable for resource cleanup.
    /// </summary>
    public sealed class Logger : ILogger, IDisposable
    {
        // Configuration and state fields
        private LogOptions _logOptions;
        private readonly BlockingCollection<LogEntry> _logEntryQueue;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly StackParserConfig _errorParser;
        private readonly ConcurrentDictionary<ILogDestination, ILogDestination> _destinations = new ConcurrentDictionary<ILogDestination, ILogDestination>();
        private Task? _backgroundTask;
        private int _droppedMessages = 0;
        private bool _manualLoggingStarted = false;

        // Fields for managing repeated messages
        private string? _lastMessage = null;
        private int _lastMessageRepeatedTimes = 0;
        private int _isHandlingLoggingException;
        private bool _isLoggingQueueWarning = false;

        // Lock for configuration updates
        private readonly ReaderWriterLockSlim _configLock = new ReaderWriterLockSlim();

        private bool _eventsSubscribed = false;
        private int _isDisposed = 0;

        internal LogOptions LogOptions => _logOptions;
        internal CancellationTokenSource CancellationTokenSource => _cancellationTokenSource;

        /// <summary>
        /// Public constructor for creating an instance with custom LogOptions.
        /// Optionally subscribes to global exception events.
        /// </summary>
        /// <param name="logOptions">Custom LogOptions.</param>
        /// <param name="errorParser">Custom error parser. If null, a default ExceptionUtility is used.</param>
        public Logger(LogOptions logOptions, StackParserConfig? errorParser = null)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _logOptions = logOptions ?? throw new ArgumentNullException(nameof(logOptions));
            _logEntryQueue = new BlockingCollection<LogEntry>((int)_logOptions.MaxQueueSize);

            if (errorParser == null)
            {
                _errorParser = StackParserConfig.Create()
                                //.SetMethodNamesToSkip(new List<string> { "SomeMethodName" })
                                .SetDeclaringTypesToSkip(new List<Type> { typeof(UnifiedLogger), typeof(Logger) })
                                .Build();
            }
            else
            {
                _errorParser = errorParser;
            }

            EnsureDefaultLogDestination();

            SubscribeToEventHandlers();
        }

        /// <summary>
        /// Adds a log destination to the logger.
        /// Thread-safe, using ConcurrentDictionary.
        /// </summary>
        /// <param name="destination">The log destination to add.</param>
        public void AddLogDestination(ILogDestination destination)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            _destinations.TryAdd(destination, destination);
        }

        /// <summary>
        /// Removes a log destination from the logger.
        /// Thread-safe, using ConcurrentDictionary.
        /// </summary>
        /// <param name="destination">The log destination to remove.</param>
        public void RemoveLogDestination(ILogDestination destination)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            _destinations.TryRemove(destination, out _);
        }

        #region Async Instance Logging Methods

        [MethodImpl(MethodImplOptions.NoInlining)]
        public async Task LogInformationAsync(
            string message,
            LogType logCategory = LogType.General,
            ErrorType errorCategory = ErrorType.NotSpecified,
            CallerContext? callerContext = null)
        {
            callerContext = callerContext ?? new CallerContext();
            await LogAsync(message, LogLevel.Information, logCategory, errorCategory, callerContext);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public async Task LogWarningAsync(
            string message,
            LogType logCategory = LogType.General,
            ErrorType errorCategory = ErrorType.NotSpecified,
            CallerContext? callerContext = null)
        {
            callerContext = callerContext ?? new CallerContext();
            await LogAsync(message, LogLevel.Warning, logCategory, errorCategory, callerContext);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public async Task LogErrorAsync(
            string message,
            ErrorType errorCategory = ErrorType.NotSpecified,
            CallerContext? callerContext = null)
        {
            callerContext = callerContext ?? new CallerContext();
            await LogAsync(message, LogLevel.Error, LogType.Exception, errorCategory, callerContext);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public async Task LogErrorAsync(
            string message,
            Exception exception,
            ErrorType errorCategory = ErrorType.NotSpecified,
            CallerContext? callerContext = null)
        {
            callerContext = callerContext ?? new CallerContext();
            await LogErrorWithDetailsAsync(message, exception, null, LogType.Exception, errorCategory, callerContext);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public async Task LogErrorAsync(
            string message,
            ErrorRecord errorRecord,
            ErrorType errorCategory = ErrorType.NotSpecified,
            CallerContext? callerContext = null)
        {
            callerContext = callerContext ?? new CallerContext();
            await LogErrorWithDetailsAsync(message, null, errorRecord, LogType.Exception, errorCategory, callerContext);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public async Task LogDebugAsync(
            string message,
            LogType logCategory = LogType.Performance,
            ErrorType errorCategory = ErrorType.NotSpecified,
            CallerContext? callerContext = null)
        {
            callerContext = callerContext ?? new CallerContext();
            await LogAsync(message, LogLevel.Debug, logCategory, errorCategory, callerContext);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public async Task LogVerboseAsync(
            string message,
            LogType logCategory = LogType.General,
            ErrorType errorCategory = ErrorType.NotSpecified,
            CallerContext? callerContext = null)
        {
            callerContext = callerContext ?? new CallerContext();
            await LogAsync(message, LogLevel.Verbose, logCategory, errorCategory, callerContext);
        }

        #endregion

        /// <summary>
        /// Logs a message with the specified MessageType and LogCategory.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="messageType">The type of message.</param>
        /// <param name="logCategory">The category of the log.</param>
        private async Task LogAsync(string message, LogLevel messageType, LogType logCategory, ErrorType errorCategory = ErrorType.NotSpecified, CallerContext? callerContext = null)
        {
            _configLock.EnterReadLock();
            try
            {
                if (messageType < _logOptions.MinimumLogLevel)
                    return;

                // Check for repeated message
                if (IsRepeatedMessage(message))
                {
                    // Skip if max repeated messages have been logged
                    if (_logOptions.MaxRepeatedMessages != -1 &&
                        Interlocked.CompareExchange(ref _lastMessageRepeatedTimes, 0, _logOptions.MaxRepeatedMessages) == _logOptions.MaxRepeatedMessages)
                    {
                        return;
                    }
                }
                else
                {
                    // Reset counter for new messages
                    Interlocked.Exchange(ref _lastMessageRepeatedTimes, 0);
                    Interlocked.Exchange(ref _lastMessage, message);
                }

                var sanitizedMessage = SharedLoggerUtilities.SanitizeMessage(message);
                var logEntry = new LogEntry(sanitizedMessage, messageType, callerContext, Environment.CurrentManagedThreadId, logCategory);

                await Enqueue(logEntry);
            }
            catch (Exception ex)
            {
                await LogToEventLogAsync($"Failed to log message: {message}", ex);
            }
            finally
            {
                _configLock.ExitReadLock();
            }
        }

        private bool IsRepeatedMessage(string message)
        {
            // Compare _lastMessage and update atomically
            string? lastMessage = Interlocked.CompareExchange(ref _lastMessage, message, message);
            if (lastMessage == message)
            {
                // Increment repeated count atomically
                Interlocked.Increment(ref _lastMessageRepeatedTimes);
                return true;
            }

            // Return false if the message is new
            return false;
        }

        /// <summary>
        /// Logs a message with additional details from an exception or an error record.
        /// </summary>
        private async Task LogErrorWithDetailsAsync(string message, Exception? exception, ErrorRecord? errorRecord, LogType logCategory = LogType.Exception, ErrorType errorCategory = ErrorType.NotSpecified, CallerContext? callerContext = null)
        {
            _configLock.EnterReadLock();
            try
            {
                string details;
                if (exception != null)
                {
                    details = ErrorParser.Parse(exception, _logOptions.ErrorParserConfig);
                }
                else if (errorRecord != null)
                {
                    details = ErrorParser.Parse(errorRecord, _logOptions.ErrorParserConfig);
                }
                else
                {
                    details = message;
                }

                var fullMessage = string.IsNullOrEmpty(message)
                    ? details
                    : $"{message}{Environment.NewLine}{details}";

                var sanitizedMessage = SharedLoggerUtilities.SanitizeMessage(fullMessage);
                var logEntry = new LogEntry(sanitizedMessage, LogLevel.Error, callerContext, Environment.CurrentManagedThreadId, LogType.Exception);

                await Enqueue(logEntry);
            }
            catch (Exception ex)
            {
                await LogToEventLogAsync($"Failed to log message: {message}", ex);
            }
            finally
            {
                _configLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Handles exceptions that occur during logging by logging them to the event log.
        /// </summary>
        /// <param name="ex">The exception to handle.</param>
        public async Task LogToEventLogAsync(string? message = null, Exception? ex = null)
        {
            if (Interlocked.Exchange(ref _isHandlingLoggingException, 1) == 1)
                return;

            try
            {
                await LogToWindowsEventLogAsync(message, ex);
            }
            catch { }
            finally
            {
                Interlocked.Exchange(ref _isHandlingLoggingException, 0);
            }
        }

        /// <summary>
        /// Logs an exception or a custom message to the Windows Event Log.
        /// </summary>
        /// <param name="exception">The exception to log. If not provided, the message parameter will be logged.</param>
        /// <param name="message">Optional custom message to log, either as context for the exception or as a standalone log entry if no exception is provided.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task LogToWindowsEventLogAsync(string? message = null, Exception? exception = null)
        {
            // Asynchronous logging to the event log
            await Task.Run(() =>
            {
                try
                {
                    string source = "PSADTLogging";
                    string logName = "Application";

                    // Build the message to log
                    string logMessage = exception != null
                        ? $"{message}\nException: {exception.Message}\nStack Trace: {exception.StackTrace}"
                        : message ?? "An unspecified event occurred.";

                    if (!EventLog.SourceExists(source))
                    {
                        EventLog.CreateEventSource(source, logName);
                    }

                    EventLog.WriteEntry(source, logMessage, EventLogEntryType.Error);
                }
                catch
                {
                    // Suppress any exceptions thrown while logging to the event log
                }
            });
        }

        private async Task LogSelfAsync(string message, LogLevel messageType, LogType logCategory = LogType.LoggingSystem)
        {
            var callerContext = new CallerContext();
            var logEntry = new LogEntry(message, messageType, callerContext, Environment.CurrentManagedThreadId, logCategory);
            await Enqueue(logEntry);
        }

        /// <summary>
        /// Enqueues a log entry for processing.
        /// </summary>
        /// <param name="logEntry">The log entry to enqueue.</param>
        private async Task Enqueue(LogEntry logEntry)
        {
            try
            {
                // Attempt to add the log entry to the queue
                if (!_logEntryQueue.TryAdd(logEntry))
                {
                    // Log a warning only once when the queue is overloaded
                    if (!_isLoggingQueueWarning)
                    {
                        _isLoggingQueueWarning = true;
                        await LogSelfAsync("Queue exceeded maximum size, potential performance issue detected.", LogLevel.Warning);
                    }

                    // Retry logic only for high-priority messages (e.g., errors)
                    if (logEntry.MessageType >= LogLevel.Error && _logOptions.RetryAttempts > 0)
                    {
                        bool success = await RetryEnqueueAsync(logEntry, _logOptions.RetryAttempts, TimeSpan.FromMilliseconds(_logOptions.RetryIntervalInMilliseconds), _cancellationTokenSource.Token);
                        if (!success)
                        {
                            // If retries failed, then increment the dropped messages counter
                            Interlocked.Increment(ref _droppedMessages);
                        }
                    }
                    else
                    {
                        // For low-priority messages (below "Error"), consider them dropped right away in overload situations
                        Interlocked.Increment(ref _droppedMessages);
                    }
                }
                else
                {
                    // Reset the dropped messages counter if successfully added to the queue
                    if (_droppedMessages > 0)
                    {
                        Interlocked.Exchange(ref _droppedMessages, 0);
                    }

                    // Reset the warning flag only when the queue size issue has been resolved
                    if (_isLoggingQueueWarning && _logEntryQueue.Count < _logOptions.MaxQueueSize)
                    {
                        _isLoggingQueueWarning = false;
                    }

                    // Start background logging if it wasn't started manually
                    if (_logOptions.StartManually && !_manualLoggingStarted)
                    {
                        return;
                    }

                    StartLogging();
                }
            }
            catch (Exception ex)
            {
                await LogToEventLogAsync($"Failed to enqueue a log entry: {logEntry.Message}", ex);
            }
        }

        private async Task<bool> RetryEnqueueAsync(LogEntry logEntry, uint maxAttempts, TimeSpan retryInterval, CancellationToken cancellationToken)
        {
            uint attempts = 0;
            DateTime startTime = DateTime.UtcNow;

            while (attempts < maxAttempts && !cancellationToken.IsCancellationRequested)
            {
                attempts++;

                // Try to add the log entry again
                if (_logEntryQueue.TryAdd(logEntry))
                {
                    // Successfully added to the queue, reset dropped message count
                    Interlocked.Exchange(ref _droppedMessages, 0);
                    return true;  // Success, no need to retry further
                }

                // Check if retry attempts have exceeded the timeout
                TimeSpan elapsed = DateTime.UtcNow - startTime;
                if ((uint)elapsed.TotalMilliseconds > _logOptions.RetryTimeoutInMilliseconds)
                {
                    break; // Exit if retries are taking too long
                }

                // Exponential backoff with jitter
                await Task.Delay(ExponentialBackoff(retryInterval, attempts), cancellationToken);
            }

            // Log a message if all retry attempts failed
            await LogSelfAsync($"Failed to enqueue log entry after [{attempts}] attempts.", LogLevel.Warning);

            return false;  // Retry failed
        }

        private TimeSpan ExponentialBackoff(TimeSpan baseDelay, uint attempt)
        {
            // jitter up to 50% of the base delay
            var jitter = new Random().Next(50, (int)(baseDelay.TotalMilliseconds * 0.5));
            var maxBackoffDelay = _logOptions.MaxRetryDelayInMilliseconds;
            return TimeSpan.FromMilliseconds(Math.Min(baseDelay.TotalMilliseconds * Math.Pow(2, attempt), maxBackoffDelay) + jitter);
        }

        private void StartLogging()
        {
            var task = Task.Run(() => ProcessLogEntriesAsync(), _cancellationTokenSource.Token);
            Interlocked.CompareExchange(ref _backgroundTask, task, null);
        }

        public void StartLoggingManually()
        {
            if (_manualLoggingStarted)
                return;

            _manualLoggingStarted = true;

            StartLogging();
        }

        /// <summary>
        /// Asynchronously processes log entries from the queue.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task ProcessLogEntriesAsync()
        {
            LogEntry? logEntry = null;
            try
            {
                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    if (_logEntryQueue.TryTake(out logEntry, Timeout.Infinite, _cancellationTokenSource.Token))
                    {
                        await WriteLogEntryToAllDestinationsAsync(logEntry);
                    }
                }
            }
            catch (Exception ex) when (ex is not TaskCanceledException)
            {
                await LogToEventLogAsync($"Failed to process log entry from the queue: {logEntry?.Message}", ex);
            }
        }

        /// <summary>
        /// Writes a log entry to all configured destinations.
        /// </summary>
        /// <param name="logEntry">The log entry to write.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task WriteLogEntryToAllDestinationsAsync(LogEntry logEntry)
        {
            await Task.WhenAll(_destinations.Keys.Select(async destination =>
            {
                try
                {
                    await destination.WriteLogEntryAsync(logEntry);
                }
                catch (Exception ex)
                {
                    await LogToEventLogAsync($"Failed to enqueue a log entry: {logEntry.Message}", ex);
                }
            }));
        }

        /// <summary>
        /// Ensures that at least one default log destination is present.
        /// </summary>
        private void EnsureDefaultLogDestination()
        {
            if (_destinations.IsEmpty)
            {
                var defaultFileDestination = new FileLogDestination(_logOptions);
                _destinations.TryAdd(defaultFileDestination, defaultFileDestination);
            }
        }

        /// <summary>
        /// Retrieves queue statistics for the logger.
        /// </summary>
        /// <returns>QueueStats object containing queue depth and dropped messages count.</returns>
        public QueueStats GetQueueStats()
        {
            return new QueueStats(_logEntryQueue.Count, (uint)_droppedMessages);
        }

        /// <summary>
        /// Updates the logger's configuration dynamically.
        /// Thread-safe.
        /// </summary>
        /// <param name="newOptions">The new LogOptions to apply.</param>
        public async Task UpdateConfiguration(LogOptions newOptions)
        {
            if (newOptions == null) throw new ArgumentNullException(nameof(newOptions));

            _configLock.EnterWriteLock();
            try
            {
                await StopLoggingAsync();

                _logOptions = newOptions;

                _destinations.Clear();
                EnsureDefaultLogDestination();

                SubscribeToEventHandlers();

                if (!_logOptions.StartManually || _manualLoggingStarted)
                {
                    StartLogging();
                }
            }
            finally
            {
                _configLock.ExitWriteLock();
            }
        }


        /// <summary>
        /// Stops the logging process asynchronously.
        /// </summary>
        public async Task StopLoggingAsync()
        {
            if (_cancellationTokenSource.IsCancellationRequested)
                return;

            await LogSelfAsync("Logging stopped.", LogLevel.Information);

            // This cancels the logging process, allowing the background task to stop processing log entries.
            _cancellationTokenSource.Cancel();

            // Unsubscribe from global exception events to avoid further logging during shutdown
            UnsubscribeEventHandlers();

            // Create a delay task for the specified timeout
            Task delayTask = Task.Delay(_logOptions.StopLoggingTimeout, _cancellationTokenSource.Token);

            try
            {
                if (_backgroundTask != null)
                {
                    await Task.WhenAny(_backgroundTask, Task.Delay(_logOptions.StopLoggingTimeout));
                }
            }
            catch (Exception ex) when (ex is not TaskCanceledException)
            {
                await LogToEventLogAsync($"There was an error with the background task for stopping the logging process.", ex);
            }
            finally
            {
                // Drain the remaining log entries from the queue
                await DrainLogQueueAsync();

                _backgroundTask = null;
                _manualLoggingStarted = false;
            }
        }

        /// <summary>
        /// Drains the remaining log entries from the queue after the logger is stopped.
        /// Ensures that all log entries are processed before the logger shuts down.
        /// </summary>
        private async Task DrainLogQueueAsync()
        {
            try
            {
                // Continue processing remaining log entries in the queue
                while (_logEntryQueue.TryTake(out var logEntry))
                {
                    await WriteLogEntryToAllDestinationsAsync(logEntry);
                }
            }
            catch (Exception ex)
            {
                await LogToEventLogAsync($"There was an error while draining the log queue.", ex);
            }
        }



        /// <summary>
        /// Unsubscribes from global exception events to prevent further logging during shutdown.
        /// </summary>
        private void UnsubscribeEventHandlers()
        {
            if (!_eventsSubscribed)
            {
                return;
            }

            if (_logOptions.SubscribeToUnhandledException)
            {
                AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
            }

            if (_logOptions.SubscribeToUnobservedTaskException)
            {
                TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;
            }

            if (_logOptions.SubscribeToOnProcessExitAndCallDispose)
            {
                AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
            }

            /// <summary>
            /// Subscribes DisposeAsync to the ProcessExit event.
            /// </summary>
            if (_logOptions.SubscribeToOnProcessExitAndCallDispose)
            {
                AppDomain.CurrentDomain.ProcessExit += async (sender, e) => await DisposeAsync();
            }

            /// <summary>
            /// Unsubscribes DisposeAsync from the ProcessExit event.
            /// </summary>
            if (_logOptions.SubscribeToOnProcessExitAndCallDispose)
            {
                AppDomain.CurrentDomain.ProcessExit -= async (sender, e) => await DisposeAsync();
            }

            _eventsSubscribed = false;
        }

        private void SubscribeToEventHandlers()
        {
            if (_eventsSubscribed)
            {
                return;
            }

            if (_logOptions.SubscribeToUnhandledException)
            {
                AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            }

            if (_logOptions.SubscribeToUnobservedTaskException)
            {
                TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
            }

            if (_logOptions.SubscribeToOnProcessExitAndCallDispose)
            {
                AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
            }

            _eventsSubscribed = true;
        }


        #region Dispose Logic

        public void Dispose()
        {
            DisposeAsyncCore(false).GetAwaiter().GetResult();
        }

        public async Task DisposeAsync()
        {
            await DisposeAsyncCore(true);
        }

        private async Task DisposeAsyncCore(bool asyncDispose)
        {
            if (Interlocked.CompareExchange(ref _isDisposed, 1, 0) == 1)
            {
                return; // Already disposed
            }

            await StopLoggingAsync(); // This will unsubscribe from events
            CleanupResources();
        }

        private void CleanupResources()
        {
            _cancellationTokenSource?.Dispose();
            _logEntryQueue?.Dispose();
            _configLock?.Dispose();

            foreach (var destination in _destinations.Keys)
            {
                if (destination is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            _destinations.Clear();
        }

        /// <summary>
        /// Event handler for ProcessExit event to ensure Dispose is called.
        /// </summary>
        internal void OnProcessExit(object? sender, EventArgs e)
        {
            // Dispose in a fire-and-forget manner without blocking
            Task.Run(() => DisposeAsync()).ConfigureAwait(false);
        }

        /// <summary>
        /// Event handler for UnhandledException event to log the exception.
        /// </summary>
        internal void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                Task.Run(() => LogErrorAsync("An unhandled exception occurred.", ex, ErrorType.NotSpecified)).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Event handler for UnobservedTaskException to log unobserved task exceptions.
        /// </summary>
        internal void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            if (e.Exception != null)
            {
                Task.Run(() => LogErrorAsync("An unobserved task exception occurred.", e.Exception, ErrorType.NotSpecified)).ConfigureAwait(false);
                e.SetObserved(); // Prevent the process from terminating
            }
        }

        #endregion
    }
}
