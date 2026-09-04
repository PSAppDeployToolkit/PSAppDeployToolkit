namespace PSADT.Interop
{
    /// <summary>
    /// Specifies the state of the machine for the current user in relation to the propriety of sending a notification. Used by SHQueryUserNotificationState.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1008:Enums should have zero value", Justification = "There is no zero value for this in the Win32 API.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "These values are precisely as they're defined in the Win32 API.")]
    public enum QUERY_USER_NOTIFICATION_STATE
    {
        /// <summary>
        /// A screen saver is displayed, the machine is locked, or a nonactive Fast User Switching session is in progress.
        /// </summary>
        QUNS_NOT_PRESENT = Windows.Win32.UI.Shell.QUERY_USER_NOTIFICATION_STATE.QUNS_NOT_PRESENT,

        /// <summary>
        /// A full-screen application is running or Presentation Settings are applied. Presentation Settings allow a user to put their machine into a state fit for an uninterrupted presentation, such as a set of PowerPoint slides, with a single click.
        /// </summary>
        QUNS_BUSY = Windows.Win32.UI.Shell.QUERY_USER_NOTIFICATION_STATE.QUNS_BUSY,

        /// <summary>
        /// A full-screen (exclusive mode) Direct3D application is running.
        /// </summary>
        QUNS_RUNNING_D3D_FULL_SCREEN = Windows.Win32.UI.Shell.QUERY_USER_NOTIFICATION_STATE.QUNS_RUNNING_D3D_FULL_SCREEN,

        /// <summary>
        /// The user has activated Windows presentation settings to block notifications and pop-up messages.
        /// </summary>
        QUNS_PRESENTATION_MODE = Windows.Win32.UI.Shell.QUERY_USER_NOTIFICATION_STATE.QUNS_PRESENTATION_MODE,

        /// <summary>
        /// None of the other states are found, notifications can be freely sent.
        /// </summary>
        QUNS_ACCEPTS_NOTIFICATIONS = Windows.Win32.UI.Shell.QUERY_USER_NOTIFICATION_STATE.QUNS_ACCEPTS_NOTIFICATIONS,

        /// <summary>
        /// <para><b>Introduced in Windows 7</b>. The current user is in "quiet time", which is the first hour after a new user logs into his or her account for the first time. During this time, most notifications should not be sent or shown. This lets a user become accustomed to a new computer system without those distractions. Quiet time also occurs for each user after an operating system upgrade or clean installation.</para>
        /// <para>Applications should set the <a href="https://docs.microsoft.com/windows/desktop/api/shellapi/ns-shellapi-notifyicondataa">NIIF_RESPECT_QUIET_TIME</a> flag in their notifications or balloon tooltip, which prevents those items from being displayed while the current user is in the quiet-time state. Note that during quiet time, if the user is in one of the other blocked modes (QUNS_NOT_PRESENT, QUNS_BUSY, QUNS_PRESENTATION_MODE, or QUNS_RUNNING_D3D_FULL_SCREEN) <a href="https://docs.microsoft.com/windows/desktop/api/shellapi/nf-shellapi-shqueryusernotificationstate">SHQueryUserNotificationState</a> returns only that value, and does not report QUNS_QUIET_TIME.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-query_user_notification_state#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        QUNS_QUIET_TIME = Windows.Win32.UI.Shell.QUERY_USER_NOTIFICATION_STATE.QUNS_QUIET_TIME,

        /// <summary>
        /// <b>Introduced in Windows 8</b>. A Windows Store app is running.
        /// </summary>
        QUNS_APP = Windows.Win32.UI.Shell.QUERY_USER_NOTIFICATION_STATE.QUNS_APP,
    }
}
