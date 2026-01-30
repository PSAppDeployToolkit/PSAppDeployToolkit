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
        CompatibilityMode = 1 << 1,
        SuppressRebootPassThru = 1 << 2,
        TerminalServerMode = 1 << 3,
        DisableLogging = 1 << 4,
        NoExitOnClose = 1 << 5,
        UseDefaultMsi = 1 << 6,
        NonInteractive = 1 << 7,
        Silent = 1 << 8,
        RequireAdmin = 1 << 9,
        DisableDefaultMsiProcessList = 1 << 10,
        ForceMsiDetection = 1 << 11,
        ForceWimDetection = 1 << 12,
        NoSessionDetection = 1 << 13,
        NoOobeDetection = 1 << 14,
        NoProcessDetection = 1 << 15,
        AllowWowProcess = 1 << 16,
    }
}
