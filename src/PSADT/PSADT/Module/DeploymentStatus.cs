namespace PSADT.Module
{
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
}
