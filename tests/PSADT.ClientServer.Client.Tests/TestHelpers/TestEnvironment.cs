using System.IO;
using System.Security.Principal;

namespace PSADT.ClientServer.Client.Tests.TestHelpers
{
    /// <summary>
    /// Facts about the machine a test run landed on, resolved once and shared by every test class.
    /// </summary>
    /// <remarks>
    /// Exposed as static properties so a test can name one in <c language="csharp">SkipUnless</c> with <c language="csharp">SkipType</c>
    /// pointing here, which keeps one copy of each probe rather than one per test class. A deliberate
    /// near-copy of the one beside the server's tests, carrying only what this project asks: a test
    /// project referencing another test project would drag its whole suite into this one's discovery.
    /// <para>
    /// Declaration order matters. Static initialisers run in textual order, so the two tables the
    /// probes read are declared above them; below, a probe runs against a null and the whole class
    /// fails to initialise, taking every test that names one of these in a skip condition with it.
    /// </para>
    /// </remarks>
    public static class TestEnvironment
    {
        /// <summary>
        /// The executables <c>ClientServerUtilities</c> expects to find beside the assembly.
        /// </summary>
        private static readonly string[] ClientServerExecutableNames =
        [
            "PSADT.ClientServer.Client.exe",
            "PSADT.ClientServer.Client.Compatible.exe",
            "PSADT.ClientServer.Client.Launcher.exe",
            "PSADT.ClientServer.Client.Launcher.Compatible.exe",
        ];

        /// <summary>
        /// The directory this assembly was loaded from.
        /// </summary>
        private static readonly string AssemblyDirectory = Path.GetDirectoryName(typeof(TestEnvironment).Assembly.Location) ?? string.Empty;

        /// <summary>
        /// Whether the caller is running elevated.
        /// </summary>
        public static bool IsElevated { get; } = GetIsElevated();

        /// <summary>
        /// Whether the caller is the local system account.
        /// </summary>
        public static bool IsLocalSystem { get; } = GetIsLocalSystem();

        /// <summary>
        /// Whether the caller is anything other than the local system account.
        /// </summary>
        /// <remarks>
        /// The inverse of <see cref="IsLocalSystem"/>, because the only account-gated path here refuses
        /// every caller that is not the local system account and there is no <c language="csharp">SkipIf</c> to express
        /// that with.
        /// </remarks>
        public static bool IsNotLocalSystem { get; } = !IsLocalSystem;

        /// <summary>
        /// Whether every client/server executable is present beside this assembly, which decides
        /// whether <c language="csharp">ClientServerUtilities</c> can be touched at all.
        /// </summary>
        /// <remarks>
        /// Its static constructor asks whether each executable is Authenticode trusted, and that check
        /// throws rather than returning false for a file that does not exist.
        /// </remarks>
        public static bool ClientServerExecutablesPresent { get; } = GetClientServerExecutablesPresent();

        /// <summary>
        /// The client executable the subprocess tests run.
        /// </summary>
        /// <remarks>
        /// Resolved the way the library resolves it, so an unsigned development build lands on the
        /// compatible variant. The default variant requests <c language="xml">uiAccess</c>, which Windows grants only
        /// to a signed executable in a secure path and otherwise refuses to launch at all. The launcher
        /// variants are excluded by that same resolution, which matters here beyond the signing: they
        /// are windowed, and <c language="csharp">InvokeMainErrorHandler</c> answers a failure in one with
        /// <c language="csharp"    >Environment.FailFast</c> rather than a serialized exception on standard error.
        /// </remarks>
        public static FileInfo? ClientExecutable { get; } = GetClientExecutable();

        /// <summary>
        /// Whether a client can be launched and its output read.
        /// </summary>
        public static bool CanRunClient { get; } = ClientExecutable?.Exists is true;

        /// <summary>
        /// Determines whether the caller is running elevated.
        /// </summary>
        /// <returns><see langword="true"/> if it is; otherwise, <see langword="false"/>.</returns>
        private static bool GetIsElevated()
        {
            using WindowsIdentity identity = WindowsIdentity.GetCurrent();
            return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
        }

        /// <summary>
        /// Determines whether the caller is the local system account.
        /// </summary>
        /// <returns><see langword="true"/> if it is; otherwise, <see langword="false"/>.</returns>
        private static bool GetIsLocalSystem()
        {
            using WindowsIdentity identity = WindowsIdentity.GetCurrent();
            return identity.User?.IsWellKnown(WellKnownSidType.LocalSystemSid) is true;
        }

        /// <summary>
        /// Determines whether every client/server executable is present beside the loaded assembly.
        /// </summary>
        /// <remarks>
        /// The paths are derived the same way <c language="csharp">ClientServerUtilities</c> derives them, rather than by
        /// reading that type, because reading it is the thing this method exists to make safe.
        /// </remarks>
        /// <returns><see langword="true"/> if all four are present; otherwise, <see langword="false"/>.</returns>
        private static bool GetClientServerExecutablesPresent()
        {
            foreach (string name in ClientServerExecutableNames)
            {
                if (!File.Exists(Path.Join(AssemblyDirectory, name)))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Determines which client executable the subprocess tests should run.
        /// </summary>
        /// <returns>The executable, or <see langword="null"/> if the set is incomplete.</returns>
        private static FileInfo? GetClientExecutable()
        {
            return !ClientServerExecutablesPresent ? null : Foundation.ClientServerUtilities.ClientAutoPath;
        }
    }
}
