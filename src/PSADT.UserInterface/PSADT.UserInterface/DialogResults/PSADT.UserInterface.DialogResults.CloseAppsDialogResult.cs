namespace PSADT.UserInterface.DialogResults
{
    /// <summary>
    /// Represents the possible outcomes of a dialog prompting the user to close applications.
    /// </summary>
    /// <remarks>This enumeration is used to indicate the user's response to a dialog that requests action regarding open applications, such as closing them, continuing without closing, or deferring the operation.</remarks>
    public enum CloseAppsDialogResult
    {
        /// <summary>
        /// Returned when the user has not responded to the dialog in time.
        /// </summary>
        Timeout,

        /// <summary>
        /// Specifies that the user has chosen to close the application.
        /// </summary>
        Close,

        /// <summary>
        /// Specifies that the user has chosen to continue without closing the application.
        /// </summary>
        Continue,

        /// <summary>
        /// Specifies that the user has chosen to defer the deployment.
        /// </summary>
        Defer,
    }
}
