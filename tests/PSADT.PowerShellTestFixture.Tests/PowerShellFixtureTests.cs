using System;
using System.Management.Automation.Runspaces;
using PSADT.PowerShellTestFixture.Tests.TestHelpers;
using Xunit;

namespace PSADT.PowerShellTestFixture.Tests
{
    /// <summary>
    /// Tests that adopting the fixture's runspace on a thread gives back whatever that thread had.
    /// </summary>
    /// <remarks>
    /// The same reasoning as the database scope beside this. Everything else the fixture does announces its own
    /// failure; putting the thread back as it was found does not, and a thread left pointing at a runspace it no
    /// longer owns produces failures nowhere near the test that caused them - the fixture's own disposal already
    /// guards against a stale pointer for exactly that reason.
    /// </remarks>
    /// <param name="powerShell">The hosted engine, shared across the collection.</param>
    [Collection(PowerShellCollection.Name)]
    public sealed class PowerShellFixtureTests(PowerShellFixture powerShell)
    {
        /// <summary>
        /// Verifies that leaving the scope gives the thread back the default runspace it had, having none.
        /// </summary>
        /// <remarks>
        /// The usual case: a test thread arrives with no default runspace, and has to leave with none. A scope
        /// that left the fixture's runspace behind would change which path the code under test takes in every
        /// test that ran afterwards on that thread, without failing anything itself.
        /// </remarks>
        [Fact]
        public void Enter_GivesTheThreadBackTheDefaultRunspaceItHad()
        {
            // Arrange
            Runspace? previous = Runspace.DefaultRunspace;

            // Act
            using (IDisposable scope = powerShell.Enter())
            {
                Assert.Same(powerShell.Runspace, Runspace.DefaultRunspace);
            }

            // Assert
            Assert.Same(previous, Runspace.DefaultRunspace);
        }

        /// <summary>
        /// Verifies that a thread which already had a default runspace gets that one back rather than nothing.
        /// </summary>
        /// <remarks>
        /// The other half, and the half an implementation that cleared rather than restored would get wrong.
        /// A second runspace is opened to stand in for one, because the fixture's own is what the scope replaces
        /// it with and the two have to be told apart.
        /// </remarks>
        [Fact]
        public void Enter_GivesBackARunspaceTheThreadAlreadyHad()
        {
            // Arrange
            using Runspace other = RunspaceFactory.CreateRunspace();
            other.Open();
            Runspace? previous = Runspace.DefaultRunspace;
            Runspace.DefaultRunspace = other;

            // Act and assert
            try
            {
                using (IDisposable scope = powerShell.Enter())
                {
                    Assert.Same(powerShell.Runspace, Runspace.DefaultRunspace);
                }

                // Assert
                Assert.Same(other, Runspace.DefaultRunspace);
            }
            finally
            {
                // Put the thread back as it was found, whatever the assertions made of it.
                Runspace.DefaultRunspace = previous;
            }
        }
    }
}
