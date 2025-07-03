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
        UseDefaultMsi = 32,
        NonInteractive = 64,
        Silent = 128,
        RequireAdmin = 256,
    }
}
