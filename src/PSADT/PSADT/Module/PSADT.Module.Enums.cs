using System;

namespace PSADT.Module
{
    public enum DeploymentStatus
    {
        Complete,
        RestartRequired,
        FastRetry,
        Error
    }

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
}
