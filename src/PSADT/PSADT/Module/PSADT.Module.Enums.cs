using System;

namespace PSADT.Module
{
    /// <summary>
    /// A bitfield representing the possible settings used within an active DeploymentSession object.
    /// </summary>
    [Flags]
    public enum DeploymentSettings
    {
        None = 0,
        Disposed = 1,
        SuppressRebootPassThru = 2,
        TerminalServerMode = 4,
        DisableLogging = 8,
        NoExitOnClose = 16,
        ZeroConfigInitiated = 32,
        UseDefaultMsi = 64,
        NonInteractive = 128,
        Silent = 256
    }

    /// <summary>
    /// The deployment types that a DeploymentSession can be.
    /// </summary>
    public enum DeploymentType
    {
        Install,
        Uninstall,
        Repair
    }

    /// <summary>
    /// The deployment modes that a DeploymentSession can be.
    /// </summary>
    public enum DeployMode
    {
        Interactive,
        NonInteractive,
        Silent
    }

    /// <summary>
    /// Status values to determine the overall state/success of a deployment.
    /// </summary>
    public enum DeploymentStatus
    {
        Complete,
        RestartRequired,
        FastRetry,
        Error
    }

    /// <summary>
    /// Flag to indicate how to write log entries to the console.
    /// </summary>
    public enum HostLogStream
    {
        None,
        Host,
        Console,
        Verbose
    }

    /// <summary>
    /// The severity of the log entry.
    /// </summary>
    public enum LogSeverity
    {
        Success = 0,
        Info = 1,
        Warning = 2,
        Error = 3,
    }

    /// <summary>
    /// Specifies the logging style to be used by the application.
    /// </summary>
    /// <remarks>This enumeration defines the available logging styles, which determine the format and behavior of log output.</remarks>
    public enum LogStyle
    {
        Legacy,
        CMTrace,
    }

    /// <summary>
    /// The type of callback to be executed.
    /// </summary>
    public enum CallbackType
    {
        /// <summary>
        /// The callback is executed before the module is initialized.
        /// </summary>
        OnInit,

        /// <summary>
        /// The callback is executed before the first deployment session is opened.
        /// </summary>
        OnStart,

        /// <summary>
        /// The callback is executed before a deployment session is opened.
        /// </summary>
        PreOpen,

        /// <summary>
        /// The callback is executed after a deployment session is opened.
        /// </summary>
        PostOpen,

        /// <summary>
        /// The callback is executed before the deployment session is closed.
        /// </summary>
        PreClose,

        /// <summary>
        /// The callback is executed after the deployment session is closed.
        /// </summary>
        PostClose,

        /// <summary>
        /// The callback is executed before the last deployment session is closed.
        /// </summary>
        OnFinish,

        /// <summary>
        /// The callback is executed after the last deployment session is closed.
        /// </summary>
        OnExit,
    }
}
