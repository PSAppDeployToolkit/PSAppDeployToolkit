using System.Globalization;
using System.Security.Principal;
using Microsoft.Win32;

namespace PSADT.WindowsRuntime.Tests.TestHelpers
{
    /// <summary>
    /// Facts about the machine a test run landed on, resolved once and shared by both test classes.
    /// </summary>
    /// <remarks>
    /// The one fact that matters here is which build of Windows is running, because both APIs this
    /// assembly wraps shipped in the same one. It is read from the registry rather than from
    /// <c language="csharp">ApiInformation</c>: asking <c language="csharp">ApiInformation</c> would restate the guard the code under test
    /// uses, and a test gated on the implementation's own answer agrees with it whether or not either
    /// is right. It is read from the registry rather than from <c language="csharp">Environment.OSVersion</c> as well,
    /// because on .NET Framework that property reports what the process manifest permits it to report
    /// rather than what is running.
    /// <para>
    /// Nothing here is privilege-gated, but one of the two wrapped APIs needs a user to answer for. The
    /// toast notification manager is per-user and refuses a caller that has none, where the focus session
    /// manager answers for the machine and does not - measured from LocalSystem, where every focus session
    /// read succeeded and every notification mode read was refused.
    /// </para>
    /// </remarks>
    public static class TestEnvironment
    {
        /// <summary>
        /// The build both wrapped APIs shipped in: Windows 10, version 1903.
        /// </summary>
        /// <remarks>
        /// <c>Windows.UI.Shell.FocusSessionManager</c> and
        /// <c>ToastNotificationManagerForUser.NotificationMode</c> arrived together, in v8.0 of the
        /// universal API contract, so one threshold covers both.
        /// </remarks>
        private const int FirstBuildWithFocusSessionsAndNotificationMode = 18362;

        /// <summary>
        /// The running operating system's build number, or zero if it could not be read.
        /// </summary>
        public static int OperatingSystemBuild { get; } = GetOperatingSystemBuild();

        /// <summary>
        /// Whether the running system is new enough to carry both of the wrapped APIs.
        /// </summary>
        public static bool HasFocusSessionsAndNotificationMode { get; } = OperatingSystemBuild >= FirstBuildWithFocusSessionsAndNotificationMode;

        /// <summary>
        /// Whether the caller has a user of its own for a per-user API to answer for.
        /// </summary>
        /// <remarks>
        /// Read off the process token rather than by trying the API, for the same reason the build number is
        /// read from the registry: a test gated on the implementation's own answer agrees with it whether or
        /// not either is right.
        /// </remarks>
        public static bool CallerHasAUserContext { get; } = GetCallerHasAUserContext();

        /// <summary>
        /// Whether the notification mode can be read here: the API has to be present and there has to be a
        /// user for it to answer for.
        /// </summary>
        public static bool CanReadTheNotificationMode => HasFocusSessionsAndNotificationMode && CallerHasAUserContext;

        /// <summary>
        /// Determines whether the caller runs as an account with a user profile behind it.
        /// </summary>
        /// <returns><see langword="true"/> unless the caller is LocalSystem.</returns>
        private static bool GetCallerHasAUserContext()
        {
            using WindowsIdentity identity = WindowsIdentity.GetCurrent();
            return identity.User?.IsWellKnown(WellKnownSidType.LocalSystemSid) is not true;
        }

        /// <summary>
        /// Reads the operating system's build number from the registry.
        /// </summary>
        /// <returns>The build number, or zero if the value is absent or unreadable.</returns>
        private static int GetOperatingSystemBuild()
        {
            using RegistryKey? key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            return key?.GetValue("CurrentBuildNumber") is string build && int.TryParse(build, NumberStyles.None, CultureInfo.InvariantCulture, out int parsed) ? parsed : 0;
        }
    }
}
