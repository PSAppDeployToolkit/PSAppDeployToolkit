using System;
using System.IO;
using PSADT.PathEx;
using PSADT.Diagnostics.StackTraces;

namespace PSADT.Logging.Models
{
    /// <summary>
    /// Represents configuration options for the logger.
    /// Immutable after creation.
    /// </summary>
    public class LogOptions
    {
        // Backing fields
        private readonly string _logDirectory;
        private readonly string _logFileNamePrefix;
        private readonly DateTime _logFileNameTimestamp;
        private readonly string _logFileNameTimestampFormat;
        private readonly string _logFileNameSuffix;
        private readonly string _logFileExtension;
        private readonly string _logFileNameWithoutExtension;
        private readonly string _logFilePath;
        private readonly TextLogFormat _logFormat;
        private readonly LogLevel _minimumLogLevel;
        private readonly string _textSeparator;
        private readonly uint _maxQueueSize;
        private readonly int _maxRepeatedMessages;
        private readonly uint _retryAttempts;
        private readonly uint _retryTimeoutInMilliseconds;
        private readonly uint _retryIntervalInMilliseconds;
        private readonly uint _maxRetryDelayInMilliseconds;
        private readonly bool _startManually;
        private readonly TimeSpan _stopLoggingTimeout;
        private readonly StackParserConfig _errorParserConfig;
        private readonly bool _subscribeToUnhandledException;
        private readonly bool _subscribeToUnobservedTaskException;
        private readonly bool _subscribeToOnProcessExitAndCallDispose;
        private readonly ulong _maxLogFileSizeInBytes;

        // Log file naming and path configuration
        public string LogDirectory => _logDirectory;
        public string LogFileNamePrefix => _logFileNamePrefix;
        public DateTime LogFileNameTimestamp => _logFileNameTimestamp;
        public string LogFileNameTimestampFormat => _logFileNameTimestampFormat;
        public string LogFileNameSuffix => _logFileNameSuffix;
        public string LogFileExtension => _logFileExtension;
        public string LogFileNameWithoutExtension => _logFileNameWithoutExtension;
        public string LogFilePath => _logFilePath;

        // Logging behavior configuration
        public TextLogFormat LogFormat => _logFormat;
        public LogLevel MinimumLogLevel => _minimumLogLevel;
        public string TextSeparator => _textSeparator;

        /// <summary>
        /// The maximum size of the log entry queue. 
        /// This represents the upper limit on the number of log entries that can be enqueued for processing at any given time.
        /// A default value of 10,000 is typically used to balance memory usage and performance, 
        /// but this can be adjusted based on system requirements.
        /// When the queue exceeds this size, new log entries may be dropped or retried based on the retry logic.
        /// </summary>
        public uint MaxQueueSize => _maxQueueSize;

        public int MaxRepeatedMessages => _maxRepeatedMessages;

        /// <summary>
        /// Number of retry attempts in case of failure during logging. 
        /// It attempts up to the specified number of times to enqueue a log entry before considering it a failure.
        /// The default value of 10 retries is a reasonable starting point for transient failures. 
        /// However, in case of high queue overload, consider lowering this number or making it dependent on time.
        /// If log entries are critical (e.g., errors), limit retries to 3-5 attempts to avoid excessive retry loops.
        /// </summary>
        public uint RetryAttempts => _retryAttempts;

        /// <summary>
        /// Total timeout for retrying log entry enqueue operations in milliseconds. 
        /// This limits the total time spent on retrying the enqueue operation before giving up.
        /// It can prevent excessive retries and ensure that the system doesn't get overloaded during high traffic.
        /// A timeout of around 5,000-10,000 ms ensures that retries don't block resources indefinitely.
        /// </summary>
        public uint RetryTimeoutInMilliseconds => _retryTimeoutInMilliseconds;

        /// <summary>
        /// Base interval in milliseconds between retry attempts for log entry enqueue operations.
        /// The base interval determines the initial delay before the first retry, with each subsequent retry delay 
        /// being exponentially increased. 
        /// A reasonable default of 100ms is used, but this can be adjusted based on system performance and load.
        /// A base interval of 100-500 ms allows enough time between retries without being too aggressive.
        /// </summary>
        public uint RetryIntervalInMilliseconds => _retryIntervalInMilliseconds;

        /// <summary>
        /// Caps the maximum delay between retry attempts for exponential backoff in milliseconds. 
        /// This ensures that the delay doesn't grow indefinitely during multiple retry attempts. 
        /// The default cap of 5,000ms is typically sufficient, but it can be made configurable based on system load.
        /// </summary>
        public uint MaxRetryDelayInMilliseconds => _maxRetryDelayInMilliseconds;

        public bool StartManually => _startManually;
        public TimeSpan StopLoggingTimeout => _stopLoggingTimeout;
        public StackParserConfig ErrorParserConfig => _errorParserConfig;

        // Exception Configuration
        public bool SubscribeToUnhandledException => _subscribeToUnhandledException;
        public bool SubscribeToUnobservedTaskException => _subscribeToUnobservedTaskException;
        public bool SubscribeToOnProcessExitAndCallDispose => _subscribeToOnProcessExitAndCallDispose;

        // Log Rotation Configuration
        public ulong MaxLogFileSizeInBytes => _maxLogFileSizeInBytes;

        // Private constructor to be used by the Builder
        private LogOptions(
            string logDirectory,
            string logFileNamePrefix,
            DateTime logFileNameTimestamp,
            string logFileNameTimestampFormat,
            string logFileNameSuffix,
            string logFileExtension,
            string logFileNameWithoutExtension,
            string logFilePath,
            TextLogFormat logFormat,
            LogLevel minimumLogLevel,
            string textSeparator,
            uint maxQueueSize,
            int maxRepeatedMessages,
            uint retryAttempts,
            uint retryTimeoutInMilliseconds,
            uint retryIntervalInMilliseconds,
            uint maxRetryDelayInMilliseconds,
            bool startManually,
            TimeSpan stopLoggingTimeout,
            StackParserConfig errorParserConfig,
            bool subscribeToUnhandledException,
            bool subscribeToUnobservedTaskException,
            bool subscribeToOnProcessExitAndCallDispose,
            ulong maxLogFileSizeInBytes)
        {
            _logDirectory = logDirectory;
            _logFileNamePrefix = logFileNamePrefix;
            _logFileNameTimestamp = logFileNameTimestamp;
            _logFileNameTimestampFormat = logFileNameTimestampFormat;
            _logFileNameSuffix = logFileNameSuffix;
            _logFileExtension = logFileExtension;
            _logFileNameWithoutExtension = logFileNameWithoutExtension;
            _logFilePath = logFilePath;
            _logFormat = logFormat;
            _minimumLogLevel = minimumLogLevel;
            _textSeparator = textSeparator;
            _maxQueueSize = maxQueueSize;
            _maxRepeatedMessages = maxRepeatedMessages;
            _retryAttempts = retryAttempts;
            _retryTimeoutInMilliseconds = retryTimeoutInMilliseconds;
            _retryIntervalInMilliseconds = retryIntervalInMilliseconds;
            _maxRetryDelayInMilliseconds = maxRetryDelayInMilliseconds;
            _startManually = startManually;
            _stopLoggingTimeout = stopLoggingTimeout;
            _errorParserConfig = errorParserConfig;
            _subscribeToUnhandledException = subscribeToUnhandledException;
            _subscribeToUnobservedTaskException = subscribeToUnobservedTaskException;
            _subscribeToOnProcessExitAndCallDispose = subscribeToOnProcessExitAndCallDispose;
            _maxLogFileSizeInBytes = maxLogFileSizeInBytes;
        }

        /// <summary>
        /// Creates a new Builder for configuring LogOptions.
        /// </summary>
        /// <returns>A new Builder instance.</returns>
        public static Builder CreateBuilder() => new Builder();

        /// <summary>
        /// Builder class for LogOptions to facilitate fluent configuration.
        /// </summary>
        public class Builder
        {
            // Private builder fields to hold values before constructing LogOptions
            private string _logDirectory = GetDefaultLogDirectory();
            private string _logFileNamePrefix = string.Empty;
            private DateTime _logFileNameTimestamp = DateTime.UtcNow;
            private string _logFileNameTimestampFormat = "yyyy-MM-dd-HH-mm-ss";
            private string _logFileNameSuffix = string.Empty;
            private string _logFileExtension = "log";
            private string _logFileNameWithoutExtension = GetDefaultLogFileNameWithoutExtension();
            private TextLogFormat _logFormat = TextLogFormat.Standard;
            private LogLevel _minimumLogLevel = LogLevel.Information;
            private string _textSeparator = " | ";
            private uint _maxQueueSize = 10000;
            private int _maxRepeatedMessages = 3;
            private uint _retryAttempts = 5;
            private uint _retryTimeoutInMilliseconds = (uint)TimeSpan.FromSeconds(30).TotalMilliseconds;
            private uint _retryIntervalInMilliseconds = (uint)TimeSpan.FromMilliseconds(100).TotalMilliseconds;
            private uint _maxRetryDelayInMilliseconds = (uint)TimeSpan.FromSeconds(5).TotalMilliseconds;
            private bool _startManually = false;
            private TimeSpan _stopLoggingTimeout = TimeSpan.FromSeconds(30);
            private StackParserConfig _errorParserConfig = StackParserConfig.Create().Build();

            // New Configuration Options
            private bool _subscribeToUnhandledException = false;
            private bool _subscribeToUnobservedTaskException = false;
            private bool _subscribeToOnProcessExitAndCallDispose = false;

            // Log Rotation Configuration
            private ulong _maxLogFileSizeInBytes = 10 * 1024 * 1024; // 10 MB by default

            // Methods for setting builder fields, returning the Builder for chaining
            public Builder SetLogDirectory(string? logDirectory)
            {
                _logDirectory = logDirectory ?? GetDefaultLogDirectory();
                return this;
            }

            public Builder SetLogFileNamePrefix(string? logFileNamePrefix)
            {
                _logFileNamePrefix = logFileNamePrefix ?? string.Empty;
                return this;
            }

            public Builder SetLogFileNameTimestamp(DateTime? logFileNameTimestamp)
            {
                _logFileNameTimestamp = logFileNameTimestamp ?? DateTime.UtcNow;
                return this;
            }

            public Builder SetLogFileNameTimestampFormat(string? logFileNameTimestampFormat)
            {
                _logFileNameTimestampFormat = logFileNameTimestampFormat ?? "yyyy-MM-dd-HH-mm-ss";
                return this;
            }

            public Builder SetLogFileNameSuffix(string? logFileNameSuffix)
            {
                _logFileNameSuffix = logFileNameSuffix ?? string.Empty;
                return this;
            }

            public Builder SetLogFileNameWithoutExtension(string? logFileNameWithoutExtension)
            {
                _logFileNameWithoutExtension = logFileNameWithoutExtension ?? GetDefaultLogFileNameWithoutExtension();
                return this;
            }

            public Builder SetLogFileExtension(string? logFileExtension)
            {
                _logFileExtension = logFileExtension ?? "log";
                return this;
            }

            public Builder SetLogFormat(TextLogFormat? logFormat)
            {
                _logFormat = logFormat ?? TextLogFormat.Standard;
                return this;
            }

            public Builder SetMinimumLogLevel(LogLevel level)
            {
                _minimumLogLevel = level;
                return this;
            }

            public Builder SetTextSeparator(string? textSeparator)
            {
                _textSeparator = textSeparator ?? " | ";
                return this;
            }

            public Builder SetMaxQueueSize(uint? maxQueueSize)
            {
                _maxQueueSize = maxQueueSize ?? 10000;
                return this;
            }

            public Builder SetMaxRepeatedMessages(int? maxRepeatedMessages)
            {
                _maxRepeatedMessages = maxRepeatedMessages ?? 1;
                return this;
            }

            public Builder SetRetryAttempts(uint? retryAttempts)
            {
                _retryAttempts = retryAttempts ?? 5;
                return this;
            }

            public Builder SetRetryTimeoutInMilliseconds(uint? retryTimeoutInMilliseconds)
            {
                _retryTimeoutInMilliseconds = retryTimeoutInMilliseconds ?? (uint)TimeSpan.FromSeconds(30).TotalMilliseconds; ;
                return this;
            }

            public Builder SetRetryIntervalInMilliseconds(uint? retryIntervalInMilliseconds)
            {
                _retryIntervalInMilliseconds = retryIntervalInMilliseconds ?? (uint)TimeSpan.FromMilliseconds(100).TotalMilliseconds;
                return this;
            }
            
            public Builder SetMaxRetryDelayInMilliseconds(uint? maxRetryDelayInMilliseconds)
            {
                _maxRetryDelayInMilliseconds = maxRetryDelayInMilliseconds ?? (uint)TimeSpan.FromSeconds(5).TotalMilliseconds;
                return this;
            }

            public Builder SetStartManually(bool? startManually)
            {
                _startManually = startManually ?? false;
                return this;
            }

            public Builder SetStopLoggingTimeout(TimeSpan? stopLoggingTimeout)
            {
                _stopLoggingTimeout = stopLoggingTimeout ?? TimeSpan.FromSeconds(30);
                return this;
            }

            public Builder SetErrorParserConfig(StackParserConfig? errorParserConfig)
            {
                _errorParserConfig = errorParserConfig ?? StackParserConfig.Create().Build();
                return this;
            }

            public Builder SubscribeToUnhandledException(bool subscribe)
            {
                _subscribeToUnhandledException = subscribe;
                return this;
            }

            public Builder SubscribeToUnobservedTaskException(bool subscribe)
            {
                _subscribeToUnobservedTaskException = subscribe;
                return this;
            }

            public Builder SubscribeToOnProcessExitAndCallDispose(bool subscribe)
            {
                _subscribeToOnProcessExitAndCallDispose = subscribe;
                return this;
            }

            public Builder SetMaxLogFileSizeInBytes(ulong maxLogFileSizeInBytes)
            {
                _maxLogFileSizeInBytes = maxLogFileSizeInBytes;
                return this;
            }

            public Builder SetMaxLogFileSizeInMegabytes(ulong maxLogFileSizeInMegabytes)
            {
                _maxLogFileSizeInBytes = maxLogFileSizeInMegabytes * 1024 * 1024;
                return this;
            }

            private static string GetDefaultLogDirectory()
            {
                return PathHelper.GetExecutingAssemblyDirectory();
            }

            private static string GetDefaultLogFileNameWithoutExtension()
            {
                return PathHelper.GetExecutingAssemblyFileNameWithoutExtension() ?? "psadtdebug";
            }

            /// <summary>
            /// Sets the components for constructing the log file path.
            /// </summary>
            /// <param name="logDirectory">The directory where log files will be stored.</param>
            /// <param name="logFileNamePrefix">The prefix for the log file name.</param>
            /// <param name="logFileNameTimestamp">The timestamp used in the log file name.</param>
            /// <param name="logFileNameTimestampFormat">The format string for the log file timestamp.</param>
            /// <param name="logFileNameWithoutExtension">The base name of the log file without extension.</param>
            /// <param name="logFileNameSuffix">The suffix for the log file name.</param>
            /// <param name="logFileExtension">The file extension for the log file.</param>
            /// <returns>The Builder instance for chaining.</returns>
            public Builder SetLogFilePathComponents(
                string? logDirectory = null,
                string? logFileNamePrefix = null,
                DateTime? logFileNameTimestamp = null,
                string? logFileNameTimestampFormat = null,
                string? logFileNameWithoutExtension = null,
                string? logFileNameSuffix = null,
                string? logFileExtension = null)
            {
                SetLogDirectory(logDirectory);
                SetLogFileNamePrefix(logFileNamePrefix);
                SetLogFileNameTimestamp(logFileNameTimestamp);
                SetLogFileNameTimestampFormat(logFileNameTimestampFormat);
                SetLogFileNameWithoutExtension(logFileNameWithoutExtension);
                SetLogFileNameSuffix(logFileNameSuffix);
                SetLogFileExtension(logFileExtension);

                return this;
            }

            /// <summary>
            /// Builds the LogOptions instance.
            /// </summary>
            public LogOptions Build()
            {
                var logFilePath = Path.Combine(
                    _logDirectory,
                    $"{_logFileNamePrefix}{_logFileNameTimestamp.ToString(_logFileNameTimestampFormat)}_{_logFileNameWithoutExtension}{_logFileNameSuffix}.{_logFileExtension}"
                );

                // Ensure the directory exists or attempt to create it
                if (!Directory.Exists(_logDirectory))
                {
                    Directory.CreateDirectory(_logDirectory);
                }

                return new LogOptions(
                    _logDirectory,
                    _logFileNamePrefix,
                    _logFileNameTimestamp,
                    _logFileNameTimestampFormat,
                    _logFileNameSuffix,
                    _logFileExtension,
                    _logFileNameWithoutExtension,
                    logFilePath,
                    _logFormat,
                    _minimumLogLevel,
                    _textSeparator,
                    _maxQueueSize,
                    _maxRepeatedMessages,
                    _retryAttempts,
                    _retryTimeoutInMilliseconds,
                    _retryIntervalInMilliseconds,
                    _maxRetryDelayInMilliseconds,
                    _startManually,
                    _stopLoggingTimeout,
                    _errorParserConfig,
                    _subscribeToUnhandledException,
                    _subscribeToUnobservedTaskException,
                    _subscribeToOnProcessExitAndCallDispose,
                    _maxLogFileSizeInBytes
                );
            }
        }
    }
}
