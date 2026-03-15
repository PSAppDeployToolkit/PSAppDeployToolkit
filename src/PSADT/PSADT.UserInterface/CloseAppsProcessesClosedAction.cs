namespace PSADT.UserInterface
{
    /// <summary>
    /// Specifies the action to take when all blocking processes have been closed during an application deployment.
    /// </summary>
    public enum CloseAppsProcessesClosedAction
    {
        /// <summary>
        /// Specifies that when all blocking processes are closed, the dialog should remain until the user explicitly presses Continue.
        /// </summary>
        RequireManualContinuation,

        /// <summary>
        /// Specifies that when all blocking processes are closed, the deployment should automatically continue without user intervention.
        /// </summary>
        AutomaticallyContinue,
    }
}
