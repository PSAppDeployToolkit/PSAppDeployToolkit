using System;
using System.Collections.Generic;
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
    /// Remediation tests cover only the cases that find the access already in place, never the repair. Repairing
    /// rewrites the access control on the module's own directory, which is a change to the machine that
    /// would outlive the test run and could not be put back accurately - the access it replaces is
    /// whatever the site's own policy left there. Every remediation test therefore asserts up front that the
    /// caller already reaches the client, and skips rather than reaching the repair.
    /// <para>
    /// Those tests are unelevated, which is the only way to ask for a user's access without a token being
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
        /// Verifies that the local system account needs read and execute access to every client/server
        /// file, including files in subdirectories, and that testing it leaves access control untouched.
        /// </summary>
        /// <remarks>
        /// Uses the effective rights actually granted rather than assuming the checkout grants access.
        /// Unlike remediation, this check uses a well-known identifier and never brokers a token or repairs permissions.
        /// </remarks>
        [Fact(Skip = "Requires the client/server executables alongside the test assembly.", SkipUnless = nameof(TestEnvironment.ClientServerExecutablesPresent), SkipType = typeof(TestEnvironment))]
        public void SystemAccountHasAccess_RequiresReadAndExecuteOnEveryFileWithoutChangingAccessControl()
        {
            // Arrange
            FileInfo[] files = ClientServerUtilities.ClientServerDirectory.GetFiles("*", SearchOption.AllDirectories);
            Assert.NotEmpty(files);
            FileSystemRights granted = FileSystemRights.ReadAndExecute;
            Dictionary<FileInfo, string> accessControl = [];
            foreach (FileInfo file in files)
            {
                granted &= FileSystemUtilities.GetEffectiveAccess(file, AccountUtilities.LocalSystemSid, FileSystemRights.ReadAndExecute);
                accessControl.Add(file, FileSystemUtilities.GetAccessControl(file, AccessControlSections.Access).GetSecurityDescriptorSddlForm(AccessControlSections.Access));
            }

            // Act
            bool actual = ClientServerPermissions.SystemAccountHasAccess();

            // Assert
            Assert.Equal(granted.HasFlag(FileSystemRights.ReadAndExecute), actual);
            foreach (KeyValuePair<FileInfo, string> entry in accessControl)
            {
                Assert.Equal(entry.Value, FileSystemUtilities.GetAccessControl(entry.Key, AccessControlSections.Access).GetSecurityDescriptorSddlForm(AccessControlSections.Access));
            }
        }

        /// <summary>
        /// Verifies that a token is not brokered when neither acquisition route is eligible, and that
        /// the caller is told so rather than being left to wait on a broker that cannot start.
        /// </summary>
        /// <remarks>
        /// The regression this exists for: the target being someone other than the caller used to send the
        /// code brokering regardless of whether brokering was available, which on a module installed to a
        /// network path meant waiting out a scheduled task the local system account could never start.
        /// <para>
        /// Session zero is ineligible for process-token acquisition, and the unelevated caller cannot
        /// broker a token. The caller's real account is retained so only session eligibility changes.
        /// </para>
        /// </remarks>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task RemediateAsync_RefusesSessionWhenNoAcquisitionRouteIsEligibleAsync()
        {
            // Arrange
            Assert.SkipUnless(CallerReachesTheClient, ClientAccessRequired);
            Assert.SkipUnless(!TestEnvironment.IsElevated, UnelevatedRequired);
            Assert.SkipUnless(AccountUtilities.CallerSessionId is not 0, "Requires an interactive caller session.");
            RunAsActiveUser otherSession = new(AccountUtilities.CallerUsername, AccountUtilities.CallerSid, 0, AccountUtilities.CallerIsAdmin);

            // Act & Assert
            _ = await Assert.ThrowsAsync<NotSupportedException>(() => ClientServerPermissions.RemediateAsync(otherSession).AsTask()).ConfigureAwait(true);
        }

        /// <summary>
        /// Refuses another user on a perfectly good session when the caller has no route to a token.
        /// </summary>
        /// <remarks>
        /// The companion to the session case above, holding the session valid and varying the caller
        /// instead: eligibility is the conjunction of the two, so a run that only ever refused an
        /// ineligible session would leave half of it unasserted.
        /// <para>
        /// This used to expect the refusal to come out of acquisition itself, because a valid session
        /// alone was enough to send the code brokering and the administrator check inside it was what
        /// turned the caller away. Eligibility is now settled before anything is attempted, which is the
        /// point of the change and is why this can no longer assert on the identity check: that needs a
        /// token to have been acquired, so it is reachable only with a second session logged on and a
        /// broker allowed to run, and a test may arrange neither.
        /// </para>
        /// </remarks>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task RemediateAsync_RefusesAnotherUserWhenTheCallerHasNoTokenRouteAsync()
        {
            Assert.SkipUnless(CallerReachesTheClient, ClientAccessRequired);
            Assert.SkipUnless(!TestEnvironment.IsElevated, UnelevatedRequired);
            Assert.SkipUnless(AccountUtilities.CallerSessionId is not (0 or uint.MaxValue), "Requires an interactive caller session.");
            RunAsActiveUser mismatchedUser = new(AccountUtilities.CallerUsername,
                new SecurityIdentifier(WellKnownSidType.NullSid, domainSid: null), AccountUtilities.CallerSessionId, AccountUtilities.CallerIsAdmin);

            _ = await Assert.ThrowsAsync<NotSupportedException>(() => ClientServerPermissions.RemediateAsync(mismatchedUser).AsTask()).ConfigureAwait(true);
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
