using System;
using System.Threading.Tasks;
using System.Management.Automation;
using PSADT.Diagnostics.StackTraces;

namespace PSADT.Logging.Interfaces
{
    /// <summary>
    /// Defines methods for logging messages at various levels.
    /// </summary>
    public interface ILogger
    {
        Task LogInformationAsync(string message,
                                 LogType logCategory = LogType.General,
                                 ErrorType errorCategory = ErrorType.NotSpecified,
                                 CallerContext? callerContext = null);
        Task LogWarningAsync(string message,
                             LogType logCategory = LogType.General,
                             ErrorType errorCategory = ErrorType.NotSpecified,
                             CallerContext? callerContext = null);
        Task LogErrorAsync(string message,
                           ErrorType errorCategory = ErrorType.NotSpecified,
                           CallerContext? callerContext = null);
        Task LogErrorAsync(string message,
                           Exception exception,
                           ErrorType errorCategory = ErrorType.NotSpecified,
                           CallerContext? callerContext = null);
        Task LogErrorAsync(string message,
                           ErrorRecord errorRecord,
                           ErrorType errorCategory = ErrorType.NotSpecified,
                           CallerContext? callerContext = null);
        Task LogDebugAsync(string message,
                           LogType logCategory = LogType.Performance,
                           ErrorType errorCategory = ErrorType.NotSpecified,
                           CallerContext? callerContext = null);
        Task LogVerboseAsync(string message,
                             LogType logCategory = LogType.General,
                             ErrorType errorCategory = ErrorType.NotSpecified,
                             CallerContext? callerContext = null);

        Task LogToEventLogAsync(string? message,
                                Exception? exception);
    }
}
