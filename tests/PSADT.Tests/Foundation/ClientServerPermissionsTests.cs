using PSADT.Foundation;
using PSADT.Tests.TestHelpers;
using Xunit;

namespace PSADT.Tests.Foundation
{
    /// <summary>
    /// Tests the check that the local system account can reach the client executables.
    /// </summary>
    /// <remarks>
    /// Only the check is covered, not the repair. Repairing rewrites the access control on the module's
    /// own directory, which is a change to the machine that would outlive the test run and could not be
    /// put back accurately - the access it replaces is whatever the site's own policy left there.
    /// <para>
    /// The check matters because it decides whether a token can be brokered at all: the broker runs as
    /// the local system account, and on a module installed to a network path that account is the machine
    /// rather than the user, so it may have no access to the executables it has been asked to launch.
    /// </para>
    /// </remarks>
    public sealed class ClientServerPermissionsTests
    {
        /// <summary>
        /// Verifies that the check answers rather than failing, and that it agrees with itself between
        /// two readings since nothing here changes what it is reading.
        /// </summary>
        /// <remarks>
        /// Nothing asserts which answer it gives. On a locally installed module the local system account
        /// reaches the executables and the answer is yes; on one installed to a network path it may be
        /// no, and both are correct answers about the machine the run landed on.
        /// </remarks>
        [Fact(Skip = "Requires the client/server executables alongside the test assembly.", SkipUnless = nameof(TestEnvironment.ClientServerExecutablesPresent), SkipType = typeof(TestEnvironment))]
        public void SystemAccountHasPermissions_AnswersConsistently()
        {
            Assert.Equal(ClientServerPermissions.SystemAccountHasPermissions(), ClientServerPermissions.SystemAccountHasPermissions());
        }

        /// <summary>
        /// Verifies that a build laid down locally is reachable by the local system account, since that
        /// account has access to everything on the machine it is running on.
        /// </summary>
        /// <remarks>
        /// Only asserted for a local build. A module on a network path is exactly the case the check
        /// exists for, and there the answer legitimately may be no.
        /// </remarks>
        [Fact(Skip = "Requires the client/server executables alongside the test assembly.", SkipUnless = nameof(TestEnvironment.ClientServerExecutablesPresent), SkipType = typeof(TestEnvironment))]
        public void SystemAccountHasPermissions_IsTrueForALocalBuild()
        {
            if (!ClientServerUtilities.ClientServerOnUncPath)
            {
                Assert.True(ClientServerPermissions.SystemAccountHasPermissions(), "The local system account cannot reach a locally built client.");
            }
        }
    }
}
