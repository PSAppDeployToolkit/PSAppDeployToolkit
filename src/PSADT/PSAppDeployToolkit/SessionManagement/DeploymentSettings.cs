using System;

namespace PSAppDeployToolkit.SessionManagement
{
    /// <summary>
    /// A bitfield representing the possible settings used within an active DeploymentSession object.
    /// </summary>
    [Flags]
    internal enum DeploymentSettings : uint
    {
        Disposed = 1 << 0,
        SuppressRebootPassThru = 1 << 1,
        TerminalServerMode = 1 << 2,
        DisableLogging = 1 << 3,
        NoExitOnClose = 1 << 4,
        UseDefaultMsi = 1 << 5,
        NonInteractive = 1 << 6,
        Silent = 1 << 7,
        RequireAdmin = 1 << 8,
        DisableDefaultMsiProcessList = 1 << 9,
        ForceMsiDetection = 1 << 10,
        ForceWimDetection = 1 << 11,
        NoSessionDetection = 1 << 12,
        NoOobeDetection = 1 << 13,
        NoProcessDetection = 1 << 14,
    }
}
