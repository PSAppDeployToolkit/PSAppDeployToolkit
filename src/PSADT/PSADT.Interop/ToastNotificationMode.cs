namespace PSADT.Interop
{
    /// <summary>
    /// Specifies the notification mode, which determines the set of notifications that are shown to the user.
    /// </summary>
    public enum ToastNotificationMode
    {
        /// <summary>
        /// All notifications are allowed, including push notifications and other types of system notifications.
        /// </summary>
        Unrestricted = Windows.UI.Notifications.ToastNotificationMode.Unrestricted,

        /// <summary>
        /// Allows only notifications marked as “priority” to be displayed, which is similar to the Do Not Disturb mode in Windows 11, where only certain priority notifications are allowed to be displayed (e.g. notifications from important contacts or apps).
        /// </summary>
        PriorityOnly = Windows.UI.Notifications.ToastNotificationMode.PriorityOnly,

        /// <summary>
        /// Allows only alert-like notifications to be shown, meaning that any non-alarm notifications are suppressed.
        /// </summary>
        AlarmsOnly = Windows.UI.Notifications.ToastNotificationMode.AlarmsOnly
    }
}
