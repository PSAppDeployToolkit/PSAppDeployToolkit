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
        AllowRebootPassThru = 2,
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
}
