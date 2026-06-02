using Windows.Foundation.Metadata;
using Windows.UI.Shell;

namespace PSADT.WindowsRuntime.UI.Shell
{
    /// <summary>
    /// Provides utility methods for interacting with Windows Shell features.
    /// </summary>
    internal static class ShellUtilities
    {
        /// <summary>
        /// Attempts to determine whether a Windows Focus Session is currently active.
        /// </summary>
        /// <remarks>This method checks for the presence and support of the Windows Focus Session API
        /// before attempting to retrieve the session state. If the API is unavailable or unsupported, <paramref
        /// name="isActive"/> is set to <see langword="false"/> and the method returns <see
        /// langword="false"/>.</remarks>
        /// <param name="isActive">When this method returns, contains <see langword="true"/> if a Focus Session is active; otherwise, <see
        /// langword="false"/>. This parameter is passed uninitialized.</param>
        /// <returns><see langword="true"/> if the Focus Session state was successfully retrieved; otherwise, <see
        /// langword="false"/>.</returns>
        internal static bool TryGetFocusSessionActive(out bool? isActive)
        {
            if (!ApiInformation.IsTypePresent("Windows.UI.Shell.FocusSessionManager") || !FocusSessionManager.IsSupported)
            {
                isActive = null;
                return false;
            }
            isActive = FocusSessionManager.GetDefault().IsFocusActive;
            return true;
        }
    }
}
