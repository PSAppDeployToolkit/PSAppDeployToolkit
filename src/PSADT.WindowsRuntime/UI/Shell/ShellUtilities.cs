using System.Diagnostics.CodeAnalysis;
using Windows.UI.Shell;

namespace PSADT.WindowsRuntime.UI.Shell
{
    /// <summary>
    /// Provides utility methods for interacting with Windows Shell features.
    /// </summary>
    [SuppressMessage("Design", "MA0182: Avoid unused internal types.", Justification = "This is used across InternalsVisibleTo boundaries.")]
    internal static class ShellUtilities
    {
        /// <summary>
        /// Attempts to determine whether a Windows Focus Session is currently active.
        /// </summary>
        /// <remarks>Asking is the check, for the reason given on the toast notification mode: probing for the API
        /// only covers it being absent, where being present, supported and still refused is the other way this fails,
        /// and both are the same answer to whether the state could be read. Unlike the notification mode this one is
        /// not known to be refused - the focus session manager answers for the machine rather than for a user, and it
        /// answered from LocalSystem - so this is the contract being honoured rather than a fault being handled.
        /// <para>Caught without naming the types because the two runtimes do not agree on them.</para></remarks>
        /// <param name="isActive">When this method returns, contains <see langword="true"/> if a Focus Session is active; otherwise, <see
        /// langword="false"/>. This parameter is passed uninitialized.</param>
        /// <returns><see langword="true"/> if the Focus Session state was successfully retrieved; otherwise, <see
        /// langword="false"/>.</returns>
        internal static bool TryGetFocusSessionActive([NotNullWhen(true)] out bool? isActive)
        {
            try
            {
                isActive = FocusSessionManager.GetDefault().IsFocusActive;
                return true;
            }
            catch
            {
                isActive = null;
                return false;
                throw;
            }
        }
    }
}
