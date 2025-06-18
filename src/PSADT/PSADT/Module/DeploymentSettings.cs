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
}
