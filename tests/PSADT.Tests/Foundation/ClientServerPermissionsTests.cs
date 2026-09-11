using System;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading.Tasks;
using PSADT.AccountManagement;
using PSADT.FileSystem;
using PSADT.Foundation;
using PSADT.Tests.TestHelpers;
using Xunit;

namespace PSADT.Tests.Foundation
{
    /// <summary>
    /// Tests granting a user the access they need to the client executables.
    /// </summary>
    /// <remarks>
    /// Only the cases that find the access already in place are covered, never the repair. Repairing
    /// rewrites the access control on the module's own directory, which is a change to the machine that
    /// would outlive the test run and could not be put back accurately - the access it replaces is
    /// whatever the site's own policy left there. Every test here therefore asserts up front that the
    /// caller already reaches the client, and skips rather than reaching the repair.
    /// <para>
    /// All of them are unelevated, which is the only way to ask for a user's access without a token being
    /// brokered for it: brokering registers a scheduled task running as the local system account, which is
    /// another change to the machine. That gate is also what makes the refusals below observable, since
    /// brokering refuses an unelevated caller before it registers anything.
    /// </para>
    /// <para>
    /// Remediating a user by identifier alone - the path taken for a user with no session of their own -
    /// is deliberately not covered. It builds an authorization context from the identifier, which cannot
    /// be done for an Entra-joined machine's accounts: there is no domain to expand their group
    /// membership against, and the attempt fails rather than answering.
    /// </para>
    /// </remarks>
    public sealed class ClientServerPermissionsTests
    {
        /// <summary>
        /// The reason a test is skipped when the caller cannot already reach the client.
        /// </summary>
        private const string ClientAccessRequired = "Requires the client/server executables alongside the test assembly, already reachable by the caller.";

        /// <summary>
        /// The reason a test is skipped when the caller is elevated.
        /// </summary>
        private const string UnelevatedRequired = "Requires an unelevated caller, for which no token is brokered.";

        /// <summary>
        /// Whether the caller already reaches the client executable, which every test here assumes.
        /// </summary>
        /// <remarks>
        /// Asked of the caller's own token rather than their identifier, for the same reason the identifier
        /// path is not covered at all. Short-circuits on the presence check because reading
        /// <see cref="ClientServerUtilities"/> at all throws when the executables are absent: its
        /// initialiser asks whether each one is Authenticode trusted, and that check throws rather than
        /// returning false for a file that does not exist.
        /// </remarks>
        private static readonly bool CallerReachesTheClient = TestEnvironment.ClientServerExecutablesPresent && CallerReachesTheClientImpl();

        /// <summary>
        /// Verifies that a token is not brokered for another user when brokering is unavailable, and that
        /// the caller is told so rather than being left to wait on a broker that cannot start.
        /// </summary>
        /// <remarks>
        /// The regression this exists for: the target being someone other than the caller used to send the
        /// code brokering regardless of whether brokering was available, which on a module installed to a
        /// network path meant waiting out a scheduled task the local system account could never start.
        /// <para>
        /// The user names the caller's own account against a session other than the caller's, which is
        /// another user as far as this is concerned while keeping a real account behind it. Brokering would
        /// have refused the lack of elevation with a different exception, so the one asserted here is what
        /// proves brokering was never reached.
        /// </para>
        /// </remarks>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task RemediateAsync_RefusesAnotherUserWhenBrokeringIsUnavailableAsync()
        {
            // Arrange
            Assert.SkipUnless(CallerReachesTheClient, ClientAccessRequired);
            Assert.SkipUnless(!TestEnvironment.IsElevated, UnelevatedRequired);
            RunAsActiveUser otherSession = new(AccountUtilities.CallerUsername, AccountUtilities.CallerSid, AccountUtilities.CallerSessionId + 1, AccountUtilities.CallerIsAdmin);

            // Act & Assert
            _ = await Assert.ThrowsAsync<NotSupportedException>(() => ClientServerPermissions.RemediateAsync(otherSession).AsTask()).ConfigureAwait(true);
        }

        /// <summary>
        /// Verifies that the caller's own access is tested with the caller's own token, which needs no
        /// brokering and is exact for the session actually running.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task RemediateAsync_AcceptsTheCallerWithoutBrokeringAsync()
        {
            // Arrange
            Assert.SkipUnless(CallerReachesTheClient, ClientAccessRequired);
            Assert.SkipUnless(!TestEnvironment.IsElevated, UnelevatedRequired);

            // Act & Assert
            await ClientServerPermissions.RemediateAsync(AccountUtilities.CallerRunAsActiveUser).ConfigureAwait(true);
        }

        /// <summary>
        /// Verifies that an extra path which does not exist is named as missing, rather than being silently
        /// skipped or reported as an access failure.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task RemediateAsync_RefusesAnExtraPathThatDoesNotExistAsync()
        {
            // Arrange
            Assert.SkipUnless(CallerReachesTheClient, ClientAccessRequired);
            Assert.SkipUnless(!TestEnvironment.IsElevated, UnelevatedRequired);
            FileInfo missing = new(Path.Join(Path.GetTempPath(), "PSADT.Tests.NoSuchAsset.png"));

            // Act
            FileNotFoundException ex = await Assert.ThrowsAsync<FileNotFoundException>(() => ClientServerPermissions.RemediateAsync(AccountUtilities.CallerRunAsActiveUser, [missing]).AsTask()).ConfigureAwait(true);

            // Assert
            Assert.Equal(missing.FullName, ex.FileName);
        }

        /// <summary>
        /// Verifies that access control is left exactly as it was found when the access asked for is
        /// already held, which is the case every run on a healthy machine takes.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task RemediateAsync_LeavesAccessControlUntouchedWhenAccessIsAlreadyHeldAsync()
        {
            // Arrange
            Assert.SkipUnless(CallerReachesTheClient, ClientAccessRequired);
            Assert.SkipUnless(!TestEnvironment.IsElevated, UnelevatedRequired);
            FileInfo probe = ClientServerUtilities.ClientCompatiblePath;
            string before = FileSystemUtilities.GetAccessControl(probe, AccessControlSections.Access).GetSecurityDescriptorSddlForm(AccessControlSections.Access);

            // Act
            await ClientServerPermissions.RemediateAsync(AccountUtilities.CallerRunAsActiveUser).ConfigureAwait(true);

            // Assert
            Assert.Equal(before, FileSystemUtilities.GetAccessControl(probe, AccessControlSections.Access).GetSecurityDescriptorSddlForm(AccessControlSections.Access));
        }

        /// <summary>
        /// Determines whether the caller's own token reaches the client executable.
        /// </summary>
        /// <returns><see langword="true"/> if it does; otherwise, <see langword="false"/>.</returns>
        private static bool CallerReachesTheClientImpl()
        {
            using WindowsIdentity currentUser = WindowsIdentity.GetCurrent();
            return FileSystemUtilities.TestEffectiveAccess(ClientServerUtilities.ClientCompatiblePath, currentUser.AccessToken, FileSystemRights.ReadAndExecute);
        }
    }
}
