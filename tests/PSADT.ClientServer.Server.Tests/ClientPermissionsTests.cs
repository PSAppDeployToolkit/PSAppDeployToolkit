using System;
using System.Threading.Tasks;
using Xunit;

namespace PSADT.ClientServer.Server.Tests
{
    /// <summary>
    /// Tests the entry point that grants a user access to the client executables.
    /// </summary>
    /// <remarks>
    /// Only the refusal is covered. What the method does when it is given a real user is rewrite the access
    /// control on the module's own directory, which is a change to the machine that would outlive the test
    /// run and could not be put back accurately - the access it replaces is whatever the site's own policy
    /// left there. The same reasoning already keeps the repair out of the tests for the check beneath it.
    /// <para>
    /// The guard is worth a test of its own because the method is a pass-through to an asynchronous one.
    /// Without a check of its own, nothing at all reaches that method and comes back as a null reference
    /// raised from somewhere else entirely, rather than as the argument it is - which is what it did before
    /// this test was written.
    /// </para>
    /// <para>
    /// That the refusal happens while the caller is still on the call, rather than through a faulted task,
    /// is not asserted. It is the better behaviour and is what the guard does, but every way of writing it
    /// down is a discarded task or an awaiter left dangling, and the analysers refuse each of them for
    /// reasons that are right everywhere else.
    /// </para>
    /// <para>
    /// The same gap remains one level down, in the internal method this forwards to, for its other callers
    /// inside the support library. That is outside this project and has been left alone rather than widened
    /// into silently.
    /// </para>
    /// </remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "RCS1046:Add suffix 'Async' to asynchronous method name", Justification = "Test names describe the scenario under test; the async suffix would obscure them.")]
    public sealed class ClientPermissionsTests
    {
        /// <summary>
        /// Verifies that no user at all is refused as an argument, rather than reaching the work and failing
        /// on a null reference.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0191:Do not use the null-forgiving operator", Justification = "This is deliberate as part of unit testing.")]
        [Fact]
        public async Task RemediateAsync_RefusesNoUserAtAll()
        {
            _ = await Assert.ThrowsAsync<ArgumentNullException>(static async () => await ClientPermissions.RemediateAsync(null!, extraPaths: null).ConfigureAwait(true)).ConfigureAwait(true);
        }
    }
}
