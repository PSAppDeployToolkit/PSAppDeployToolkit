using System;
using System.IO;
using System.Security.Principal;

namespace PSADT.ClientServer.Server.Tests.TestHelpers
{
    /// <summary>
    /// Facts about the machine a test run landed on, resolved once and shared by every test class.
    /// </summary>
    /// <remarks>
    /// Each member here answers a question a test needs before it can decide whether it is able to run
    /// at all. They are exposed as static properties so a test can name one in <c>SkipUnless</c> with
    /// <c>SkipType</c> pointing at this class, which keeps a single copy of the probe rather than one
    /// per test class.
    /// <para>
    /// A deliberate near-copy of the one beside PSADT's own tests, carrying only the probes this project
    /// needs. It cannot be shared: that one lives in a test assembly, and a test project referencing
    /// another test project would drag its whole suite into this one's discovery.
    /// </para>
    /// <para>
    /// Declaration order matters here in a way it does not elsewhere. Static initialisers run in textual
    /// order, so the table of executable names is declared above the probe that reads it rather than down
    /// with the other fields; below it, the probe runs against a null and the whole class fails to
    /// initialise - taking every test that names one of these in a skip condition with it.
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
        /// Whether the caller is running elevated.
        /// </summary>
        public static bool IsElevated { get; } = GetIsElevated();

        /// <summary>
        /// Whether the caller is the local system account, which cannot launch a client into its own
        /// session because it has none.
        /// </summary>
        public static bool IsLocalSystem { get; } = GetIsLocalSystem();

        /// <summary>
        /// Whether the client/server executables are present where <c>ClientServerUtilities</c> looks
        /// for them, which decides whether that type can be touched at all.
        /// </summary>
        /// <remarks>
        /// Its static constructor asks whether each executable is Authenticode trusted, and that check
        /// throws rather than returning false for a file that does not exist. So <c>ServerInstance</c>,
        /// which reads it to launch a client, fails with a <see cref="TypeInitializationException"/>
        /// unless they were copied alongside. The project builds and copies them, but a build that
        /// addresses an inner target framework directly skips that step, so the tests confirm it rather
        /// than assume it.
        /// </remarks>
        public static bool ClientServerExecutablesPresent { get; } = GetClientServerExecutablesPresent();

        /// <summary>
        /// Whether a client process can actually be launched and talked to, which is what the end to end
        /// tests need.
        /// </summary>
        /// <remarks>
        /// Three things have to hold. The executables have to be there. The caller has to be an
        /// interactive logged-on user, because the server hands the client a session to run in and asks
        /// for its own; a caller that is not one - a build agent running as a service, or the local
        /// system account - has no session to hand over and the launch goes down the token brokering
        /// path instead, which is a different thing from what these tests are covering. And the caller
        /// must not be the local system account for the same reason.
        /// </remarks>
        public static bool CanLaunchClient { get; } = ClientServerExecutablesPresent && !IsLocalSystem && GetCallerIsLoggedOnUser();

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
        /// Determines whether the caller is the user logged on at the console.
        /// </summary>
        /// <remarks>
        /// Asked of the library rather than worked out here, because it is the same answer the launch
        /// path itself consults when deciding whether it can run a client as the caller. Guarded because
        /// reading it initialises <c>ClientServerUtilities</c>, which throws when the executables are
        /// missing.
        /// </remarks>
        /// <returns><see langword="true"/> if it is; otherwise, <see langword="false"/>.</returns>
        private static bool GetCallerIsLoggedOnUser()
        {
            try
            {
                return PSADT.AccountManagement.AccountUtilities.CallerIsLoggedOnUser;
            }
            catch (TypeInitializationException)
            {
                return false;
            }
        }

        /// <summary>
        /// Determines whether every client/server executable is present beside the loaded assembly.
        /// </summary>
        /// <remarks>
        /// The paths are derived the same way <c>ClientServerUtilities</c> derives them, rather than by
        /// reading that type, because reading it is the thing this method exists to make safe.
        /// </remarks>
        /// <returns><see langword="true"/> if all four are present; otherwise, <see langword="false"/>.</returns>
        private static bool GetClientServerExecutablesPresent()
        {
            if (Path.GetDirectoryName(typeof(TestEnvironment).Assembly.Location) is not string assemblyDirectory)
            {
                return false;
            }
            string clientServerDirectory = assemblyDirectory.EndsWith("net472", StringComparison.Ordinal)
                ? assemblyDirectory
                : Path.Join(Directory.GetParent(assemblyDirectory)?.FullName, "net472");
            foreach (string name in ClientServerExecutableNames)
            {
                if (!File.Exists(Path.Join(clientServerDirectory, name)))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
