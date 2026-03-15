namespace PSADT.UserInterface
{
    /// <summary>
    /// Specifies the available actions to take when a countdown dialog is shown for closing applications during
    /// deployment.
    /// </summary>
    /// <remarks>This enumeration is used to control the behavior of the deployment process when user
    /// interaction is required to close blocking applications. The selected action determines whether the deployment is
    /// deferred, continues if processes are closed, or forcibly closes blocking processes based on the availability of
    /// deferrals and the state of running applications.</remarks>
    public enum CloseAppsCountdownAction
    {
        /// <summary>
        /// Specifies that the deployment should be deferred if deferrals are enabled and available. If not, the dialog will be shown again.
        /// </summary>
        DeferIfAvailable,

        /// <summary>
        /// Specifies that the deployment should proceed if there are no blocking processes running that would prevent the deployment from succeeding.
        /// </summary>
        ContinueIfProcessesClosed,

        /// <summary>
        /// Specifies that the deployment should proceed, closing any blocking processes if they are running.
        /// </summary>
        CloseProcessesIfRunning,

        /// <summary>
        /// Specifies that the deployment should proceed, closing any blocking processes if deferrals are not available.
        /// </summary>
        CloseProcessesIfNoDeferrals,
    }
}
