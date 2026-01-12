using PSADT.LibraryInterfaces;

namespace PSADT.TerminalServices
{
    /// <summary>
    /// Provides utility methods for interacting with terminal server modes.
    /// </summary>
    /// <remarks>This class contains static methods that assist in determining and managing the operational
    /// state of a terminal server, such as checking whether the server is in execute or install mode. These utilities
    /// are useful for applications that need to adapt their behavior based on the current terminal server
    /// configuration.</remarks>
    public static class TerminalServerUtilities
    {
        /// <summary>
        /// Determines whether the current session is running in Terminal Services application install mode.
        /// </summary>
        /// <remarks>Application install mode is used in Terminal Services environments to prepare the
        /// system for installing applications for multiple users. This method is typically used to check if special
        /// installation procedures should be followed.</remarks>
        /// <returns>true if the session is in application install mode; otherwise, false.</returns>
        public static bool InAppInstallMode()
        {
            return Kernel32.TermsrvAppInstallMode();
        }
    }
}
