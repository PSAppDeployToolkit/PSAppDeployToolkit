using System;

namespace PSAppDeployToolkit.SessionManagement
{
    /// <summary>
    /// A bitfield representing the possible settings used within an active DeploymentSession object.
    /// </summary>
    [Flags]
    internal enum DeploymentSettings : uint
    {
        Disposed = 1,
        SuppressRebootPassThru = 2,
        TerminalServerMode = 4,
        DisableLogging = 8,
        NoExitOnClose = 16,
        UseDefaultMsi = 32,
        NonInteractive = 64,
        Silent = 128,
        RequireAdmin = 256,
        DisableDefaultMsiProcessList = 512,
        ForceMsiDetection = 1024,
        ForceWimDetection = 2048,
        NoSessionDetection = 4096,
        NoOobeDetection = 8192,
        NoProcessDetection = 16384,
    }
}
