using System;
using System.Threading.Tasks;
using System.Management.Automation;
using System.Runtime.CompilerServices;
using PSADT.Diagnostics.StackTraces;

namespace PSADT.Logging
{
    public class LogEntryBuilder
    {
        private readonly Logger _logger;
        private CallerContext _callerContext;
        private string _message = string.Empty;
        private LogLevel _logLevel = LogLevel.Information;
        private LogType _logCategory = LogType.General;
        private bool _isLogCategorySet = false;
        private ErrorType _errorCategory = ErrorType.NotSpecified;
        private Exception? _exception = null;
        private ErrorRecord? _errorRecord = null;

        public LogEntryBuilder(Logger logger, CallerContext callerContext)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _callerContext = callerContext ?? throw new ArgumentNullException(nameof(callerContext));
        }

        public LogEntryBuilder Message(string message)
        {
            _message = $@"{_message}{message}";
            return this;
        }

        public LogEntryBuilder AppendMessage(string appendMessage)
        {
            _message = $@"{_message}{appendMessage}";
            return this;
        }

        public LogEntryBuilder Category(LogType logCategory)
        {
            _logCategory = logCategory;
            _isLogCategorySet = true;
            return this;
        }

        public LogEntryBuilder ErrorCategory(ErrorType errorCategory)
        {
            _errorCategory = errorCategory;
            return this;
        }

        public LogEntryBuilder Parse(object input, StackParserConfig? config = null)
        {
            string parsedMessage = ErrorParser.Parse(input, config);
            _message = string.IsNullOrWhiteSpace(_message) ? parsedMessage : $@"{_message}{Environment.NewLine}{parsedMessage}";
            return this;
        }

        public LogEntryBuilder Error(object error)
        {
            if (error is Exception exception)
            {
                _exception = exception;
            }
            else if (error is ErrorRecord errorRecord)
            {
                _errorRecord = errorRecord;
                if (_errorCategory == ErrorType.NotSpecified)
                {
                    ErrorCategory((ErrorType)errorRecord.CategoryInfo.Category);
                }
            }
            else
            {
                throw new ArgumentException($"Unsupported error type [{error.GetType()}]. Must be [Exception] or [ErrorRecord].", nameof(error));
            }

            if (_logLevel == LogLevel.Information)
            {
                Severity(LogLevel.Error);
            }

            return this;
        }

        public LogEntryBuilder Severity(LogLevel logLevel)
        {
            _logLevel = logLevel;
            if (!_isLogCategorySet)
            {
                switch (_logLevel)
                {
                    case LogLevel.Error:
                        Category(LogType.Exception);
                        break;
                    case LogLevel.Debug:
                        Category(LogType.Performance);
                        break;
                    case LogLevel.Verbose:
                        Category(LogType.Diagnostics);
                        break;
                    default:
                        Category(LogType.General);
                        break;
                }
            }

            return this;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public LogEntryBuilder CallerContext(CallerContext callerContext)
        {
            _callerContext = callerContext ?? throw new ArgumentNullException(nameof(callerContext));
            return this;
        }

        public async Task LogAsync()
        {
            switch (_logLevel)
            {
                case LogLevel.Information:
                    await LogInformationAsync();
                    break;
                case LogLevel.Warning:
                    await LogWarningAsync();
                    break;
                case LogLevel.Error:
                    await LogErrorAsync();
                    break;
                case LogLevel.Debug:
                    await LogDebugAsync();
                    break;
                case LogLevel.Verbose:
                    await LogVerboseAsync();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void Log()
        {
            Task.Run(LogAsync).Wait();
        }

        private async Task LogInformationAsync()
        {
            await _logger.LogInformationAsync(_message, _logCategory, _errorCategory, _callerContext);
        }

        private async Task LogWarningAsync()
        {
            await _logger.LogWarningAsync(_message, _logCategory, _errorCategory, _callerContext);
        }

        private async Task LogErrorAsync()
        {
            if (_exception != null)
            {
                await _logger.LogErrorAsync(_message, _exception, _errorCategory, _callerContext);
            }
            else if (_errorRecord != null)
            {
                await _logger.LogErrorAsync(_message, _errorRecord, _errorCategory, _callerContext);
            }
            else
            {
                await _logger.LogErrorAsync(_message, _errorCategory, _callerContext);
            }
        }

        private async Task LogDebugAsync()
        {
            await _logger.LogDebugAsync(_message, _logCategory, _errorCategory, _callerContext);
        }

        private async Task LogVerboseAsync()
        {
            await _logger.LogVerboseAsync(_message, _logCategory, _errorCategory, _callerContext);
        }
    }
}
