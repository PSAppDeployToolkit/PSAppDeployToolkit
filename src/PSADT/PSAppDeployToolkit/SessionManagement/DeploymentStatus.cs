namespace PSAppDeployToolkit.SessionManagement
{
    /// <summary>
    /// Status values to determine the overall state/success of a deployment.
    /// </summary>
    public enum DeploymentStatus
    {
        /// <summary>
        /// The deployment completed successfully.
        /// </summary>
        Complete,

        /// <summary>
        /// The deployment completed successfully, but a restart is required.
        /// </summary>
        RestartRequired,

        /// <summary>
        /// The deployment was deferred.
        /// </summary>
        FastRetry,

        /// <summary>
        /// The deployment encountered an error.
        /// </summary>
        Error
    }
}
