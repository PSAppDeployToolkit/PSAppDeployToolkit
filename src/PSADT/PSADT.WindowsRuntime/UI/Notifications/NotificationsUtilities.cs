using Windows.Foundation.Metadata;
using Windows.UI.Notifications;

namespace PSADT.WindowsRuntime.UI.Notifications
{
    /// <summary>
    /// Provides utility methods for querying Windows toast notification state.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0182: Avoid unused internal types.", Justification = "This is used across InternalsVisibleTo boundaries.")]
    internal static class NotificationsUtilities
    {
        /// <summary>
        /// Attempts to retrieve the current toast notification mode for the user.
        /// </summary>
        /// <remarks>This method checks for the presence of required Windows Runtime APIs before
        /// attempting to retrieve the notification mode. If the necessary APIs are not available, the method returns
        /// false and the value of the mode parameter is undefined.</remarks>
        /// <param name="mode">When this method returns, contains the current toast notification mode if the operation succeeds; otherwise,
        /// contains an undefined value.</param>
        /// <returns>true if the notification mode was successfully retrieved; otherwise, false.</returns>
        internal static bool TryGetNotificationMode(out ToastNotificationMode mode)
        {
            if (!ApiInformation.IsTypePresent("Windows.UI.Notifications.ToastNotificationManagerForUser") || !ApiInformation.IsTypePresent("Windows.UI.Notifications.ToastNotificationMode") || !ApiInformation.IsMethodPresent("Windows.UI.Notifications.ToastNotificationManager", "GetDefault") || !ApiInformation.IsPropertyPresent("Windows.UI.Notifications.ToastNotificationManagerForUser", "NotificationMode"))
            {
                mode = (ToastNotificationMode)(-1);
                return false;
            }
            mode = ToastNotificationManager.GetDefault().NotificationMode;
            return true;
        }
    }
}
