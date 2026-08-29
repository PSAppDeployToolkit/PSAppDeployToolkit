using System.Globalization;
using Microsoft.Win32;

namespace PSADT.WindowsRuntime.Tests.TestHelpers
{
    /// <summary>
    /// Facts about the machine a test run landed on, resolved once and shared by both test classes.
    /// </summary>
    /// <remarks>
    /// The one fact that matters here is which build of Windows is running, because both APIs this
    /// assembly wraps shipped in the same one. It is read from the registry rather than from
    /// <c>ApiInformation</c>: asking <c>ApiInformation</c> would restate the guard the code under test
    /// uses, and a test gated on the implementation's own answer agrees with it whether or not either
    /// is right. It is read from the registry rather than from <c>Environment.OSVersion</c> as well,
    /// because on .NET Framework that property reports what the process manifest permits it to report
    /// rather than what is running.
    /// <para>
    /// Nothing in this assembly is privilege-gated, so there is no elevation probe here. Both APIs are
    /// per-user reads that any caller may make.
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
