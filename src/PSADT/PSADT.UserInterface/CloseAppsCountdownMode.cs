namespace PSADT.UserInterface
{
    /// <summary>
    /// Specifies the available modes for displaying the countdown when prompting users to close applications.
    /// </summary>
    public enum CloseAppsCountdownMode
    {
        /// <summary>
        /// Specifies that the countdown should only be shown when deferrals are not enabled or have otherwise expired.
        /// </summary>
        WhenDeferralNotPossible,

        /// <summary>
        /// Specifies that the countdown should be shown every time, irrespective of whether deferrals are enabled or not.
        /// </summary>
        IrrespectiveOfDeferrals,
    }
}
