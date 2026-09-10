using System;
using System.Security.Principal;

namespace PSADT.UserInterface.Interfaces.Tests.TestHelpers
{
    /// <summary>
    /// Facts about the account a test run landed on.
    /// </summary>
    /// <remarks>
    /// A dialog drawn by LocalSystem onto somebody's desktop is a different thing from one a user drew for
    /// themselves, and the Fluent dialogs treat it as such. Read off the process token rather than from the
    /// library's own answer for the same question: a test gated on the implementation's own flag agrees
    /// with it whether or not either is right.
    /// </remarks>
    public static class TestEnvironment
    {
        /// <summary>
        /// Whether LocalSystem is the one putting a window on a desktop.
        /// </summary>
        /// <remarks>
        /// The two halves are what makes it dangerous: LocalSystem alone is a service nobody can click, and
        /// an interactive session alone is an ordinary user. Together they are a window running as the
        /// machine that a person can reach.
        /// </remarks>
        public static bool CallerIsSystemInteractive { get; } = GetCallerIsLocalSystem() && Environment.UserInteractive;

        /// <summary>
        /// Determines whether the caller runs as LocalSystem.
        /// </summary>
        /// <returns><see langword="true"/> if it does; otherwise, <see langword="false"/>.</returns>
        private static bool GetCallerIsLocalSystem()
        {
            using WindowsIdentity identity = WindowsIdentity.GetCurrent();
            return identity.User?.IsWellKnown(WellKnownSidType.LocalSystemSid) is true;
        }
    }
}
