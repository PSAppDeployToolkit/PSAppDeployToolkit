namespace PSADT.Logging
{
    /// <summary>
    /// Specifies the format for text logs.
    /// </summary>
    public enum TextLogFormat
    {
        /// <summary>
        /// The default format for logs.
        /// </summary>
        Standard,

        /// <summary>
        /// Format suitable for viewing in CMTrace (often used in system logs).
        /// </summary>
        CMTrace,

        /// <summary>
        /// Logs formatted as JSON objects for structured logging.
        /// </summary>
        Json,

        /// <summary>
        /// Custom-defined format for logs.
        /// </summary>
        CustomFormat
    }


    /// <summary>
    /// Specifies the type of log message, with equivalent mappings to PowerShell logging levels.
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// Debug level logs, equivalent to PowerShell's "Debug" streams.
        /// Used for detailed internal information, typically not needed during regular operations.
        /// </summary>
        Debug,

        /// <summary>
        /// Verbose logs, equivalent to PowerShell's "Verbose" streams.
        /// Provides additional details about application flow and state beyond standard information logs.
        /// </summary>
        Verbose,

        /// <summary>
        /// Informational logs, equivalent to PowerShell's standard "Information" streams.
        /// Used for logging general operation messages, such as normal progress updates.
        /// </summary>
        Information,

        /// <summary>
        /// Warning logs, equivalent to PowerShell's "Warning" streams.
        /// Indicates a potential issue or risk but does not stop program execution.
        /// </summary>
        Warning,

        /// <summary>
        /// Error logs, equivalent to PowerShell's "Error" streams.
        /// Represents significant issues that prevent normal execution.
        /// </summary>
        Error
    }


    /// <summary>
    /// Represents categories for log entries, allowing classification of log messages by their context.
    /// </summary>
    public enum LogType
    {
        /// <summary>
        /// General log messages, for logs that don't fit into other categories.
        /// </summary>
        General,

        /// <summary>
        /// Security-related logs, including authentication and authorization events.
        /// </summary>
        Security,

        /// <summary>
        /// Logs related to performance metrics and bottlenecks.
        /// </summary>
        Performance,

        /// <summary>
        /// Logs related to exceptions and error handling.
        /// </summary>
        Exception,

        /// <summary>
        /// Logs related to system or application configuration changes.
        /// </summary>
        Configuration,

        /// <summary>
        /// Logs specific to the logging system itself, such as logging failures.
        /// </summary>
        LoggingSystem,

        /// <summary>
        /// Logs related to network operations, such as requests, responses, or connection issues.
        /// </summary>
        Network,

        /// <summary>
        /// Logs related to database operations, such as queries and connections.
        /// </summary>
        Database,

        /// <summary>
        /// Logs related to file system interactions, such as file read/write operations or access errors.
        /// </summary>
        FileSystem,

        /// <summary>
        /// Logs related to user authentication, authorization, and related security checks.
        /// </summary>
        Authentication,

        /// <summary>
        /// Logs of user activities within the application, useful for auditing and tracking user behavior.
        /// </summary>
        UserActivity,

        /// <summary>
        /// Logs related to system-level events and operations, such as resource monitoring or system health checks.
        /// </summary>
        System,

        /// <summary>
        /// Logs for tracking business or data transactions, particularly in transactional systems.
        /// </summary>
        Transaction,

        /// <summary>
        /// Detailed logs for diagnostic purposes, typically used to troubleshoot issues or debug performance.
        /// </summary>
        Diagnostics,

        /// <summary>
        /// Logs related to user interface interactions or errors, particularly in client applications.
        /// </summary>
        UI,

        /// <summary>
        /// Logs for API requests, responses, and any related issues or metrics.
        /// </summary>
        API,

        /// <summary>
        /// Logs related to external services or third-party dependencies, such as external API calls or libraries.
        /// </summary>
        Dependency
    }


    /// <summary>
    /// Defines the categories for error conditions in PowerShell commands. 
    /// Each category represents a different type of error that can occur during execution.
    /// </summary>
    public enum ErrorType
    {
        /// <summary>
        /// Error that is not specified or does not fit into any other category.
        /// </summary>
        NotSpecified,

        /// <summary>
        /// Error occurred while trying to open a resource (e.g., file, device, or connection).
        /// </summary>
        OpenError,

        /// <summary>
        /// Error occurred while trying to close a resource (e.g., file, device, or connection).
        /// </summary>
        CloseError,

        /// <summary>
        /// Error occurred due to issues with a physical or virtual device (e.g., hardware failure).
        /// </summary>
        DeviceError,

        /// <summary>
        /// Error occurred due to a detected deadlock condition, often related to multithreading or resource contention.
        /// </summary>
        DeadlockDetected,

        /// <summary>
        /// Error occurred because an invalid argument was provided to a function or command.
        /// </summary>
        InvalidArgument,

        /// <summary>
        /// Error occurred because invalid data was encountered or processed.
        /// </summary>
        InvalidData,

        /// <summary>
        /// Error occurred because an operation was performed that was not valid in the current context.
        /// </summary>
        InvalidOperation,

        /// <summary>
        /// Error occurred due to an invalid or unexpected result from a command or function.
        /// </summary>
        InvalidResult,

        /// <summary>
        /// Error occurred due to an unexpected or invalid type being used (e.g., wrong data type).
        /// </summary>
        InvalidType,

        /// <summary>
        /// Error occurred because of issues related to metadata (e.g., incorrect or missing metadata).
        /// </summary>
        MetadataError,

        /// <summary>
        /// Error occurred because the requested operation is not implemented.
        /// </summary>
        NotImplemented,

        /// <summary>
        /// Error occurred because the required component or feature is not installed.
        /// </summary>
        NotInstalled,

        /// <summary>
        /// Error occurred because the object being operated on was not found.
        /// </summary>
        ObjectNotFound,

        /// <summary>
        /// Error occurred because the operation was explicitly stopped (e.g., user intervention).
        /// </summary>
        OperationStopped,

        /// <summary>
        /// Error occurred due to a timeout while performing an operation.
        /// </summary>
        OperationTimeout,

        /// <summary>
        /// Error occurred due to a syntax issue in a command or script.
        /// </summary>
        SyntaxError,

        /// <summary>
        /// Error occurred during the parsing of a command, script, or input data.
        /// </summary>
        ParserError,

        /// <summary>
        /// Error occurred because the required permissions were denied.
        /// </summary>
        PermissionDenied,

        /// <summary>
        /// Error occurred because the resource needed for the operation is currently busy.
        /// </summary>
        ResourceBusy,

        /// <summary>
        /// Error occurred because the resource being created already exists.
        /// </summary>
        ResourceExists,

        /// <summary>
        /// Error occurred because the required resource is unavailable.
        /// </summary>
        ResourceUnavailable,

        /// <summary>
        /// Error occurred while attempting to read from a resource.
        /// </summary>
        ReadError,

        /// <summary>
        /// Error occurred while attempting to write to a resource.
        /// </summary>
        WriteError,

        /// <summary>
        /// Error occurred as a result of information received from standard error (stderr) output.
        /// </summary>
        FromStdErr,

        /// <summary>
        /// Error occurred due to a security-related issue (e.g., authentication or access control).
        /// </summary>
        SecurityError,

        /// <summary>
        /// Error occurred because of a protocol-related issue (e.g., communication protocol failure).
        /// </summary>
        ProtocolError,

        /// <summary>
        /// Error occurred because a connection to a resource or service could not be established or was lost.
        /// </summary>
        ConnectionError,

        /// <summary>
        /// Error occurred because the authentication process failed.
        /// </summary>
        AuthenticationError,

        /// <summary>
        /// Error occurred because limits imposed by the system or environment were exceeded.
        /// </summary>
        LimitsExceeded,

        /// <summary>
        /// Error occurred because quota limits for a resource or service were exceeded.
        /// </summary>
        QuotaExceeded,

        /// <summary>
        /// Error occurred because the requested feature or service is not enabled.
        /// </summary>
        NotEnabled
    }

}
