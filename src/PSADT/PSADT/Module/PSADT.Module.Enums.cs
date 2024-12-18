using System;

namespace PSADT.Module
{
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

    public enum DeploymentType
    {
        Install,
        Uninstall,
        Repair
    }

    public enum DeployMode
    {
        Interactive,
        NonInteractive,
        Silent
    }

    public enum DeploymentStatus
    {
        Complete,
        RestartRequired,
        FastRetry,
        Error
    }
}
