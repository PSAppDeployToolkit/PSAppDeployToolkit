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
}
