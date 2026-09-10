using System.Diagnostics.CodeAnalysis;
using Windows.UI.Notifications;

namespace PSADT.WindowsRuntime.UI.Notifications
{
    /// <summary>
    /// Provides utility methods for querying Windows toast notification state.
    /// </summary>
    [SuppressMessage("Design", "MA0182: Avoid unused internal types.", Justification = "This is used across InternalsVisibleTo boundaries.")]
    internal static class NotificationsUtilities
    {
        /// <summary>
        /// Attempts to retrieve the current toast notification mode for the user.
        /// </summary>
        /// <remarks>Asking is the check. An <c language="csharp">ApiInformation</c> probe would only cover the API
        /// being absent, which is one of two ways this fails and the rarer one: the manager is per-user, so a caller
        /// with no user of its own - a service running as LocalSystem, for instance - is refused outright on a system
        /// that has the API and passes every such probe. Both are the same answer here, since this reports whether a
        /// mode could be read and the sole caller already treats "could not" as its own answer.
        /// <para>Caught without naming the types because the two runtimes do not agree on them: .NET raises a
        /// <c language="csharp">COMException</c> where .NET Framework raises a plain
        /// <c language="csharp">Exception</c> carrying the same HRESULT (0x8000FFFF, observed from
        /// LocalSystem).</para></remarks>
        /// <param name="mode">When this method returns, contains the current toast notification mode if the operation succeeds; otherwise,
        /// contains an undefined value.</param>
        /// <returns>true if the notification mode was successfully retrieved; otherwise, false.</returns>
        internal static bool TryGetNotificationMode([NotNullWhen(true)] out ToastNotificationMode? mode)
        {
            try
            {
                mode = ToastNotificationManager.GetDefault().NotificationMode;
                return true;
            }
            catch
            {
                mode = null;
                return false;
                throw;
            }
        }
    }
}
