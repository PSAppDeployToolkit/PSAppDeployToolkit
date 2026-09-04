using System;
using PSADT.PowerShellTestFixture.Tests.TestHelpers;
using PSAppDeployToolkit.Foundation;
using Xunit;

namespace PSADT.PowerShellTestFixture.Tests
{
    /// <summary>
    /// Tests that the scope seating a module database puts back what it found.
    /// </summary>
    /// <remarks>
    /// Almost nothing in the fixture needs testing, because almost everything in it fails loudly: a runspace that
    /// would not open, a table the engine would not build or a database that was never seated all stop the tests
    /// that depend on them, immediately and by name.
    /// <para>
    /// Restoring is the exception. A scope that failed to put back the database it displaced would leave one
    /// seated for whatever ran next, and the failure would surface in a different test, intermittently, and be
    /// read as a fault in the code that test covers. That is worth a few lines to rule out.
    /// </para>
    /// <para>
    /// Which database is seated is told apart by its environment table rather than by anything written into it.
    /// A table is an identity rather than a value - two built moments apart are deliberately not equal - so
    /// comparing by reference answers exactly the question being asked.
    /// </para>
    /// </remarks>
    /// <param name="powerShell">The hosted engine, shared across the collection.</param>
    [Collection(PowerShellCollection.Name)]
    public sealed class ModuleDatabaseScopeTests(PowerShellFixture powerShell)
    {
        /// <summary>
        /// Verifies that a nested scope puts back the database the outer one had seated.
        /// </summary>
        [Fact]
        public void Dispose_PutsBackTheDatabaseThatWasSeatedBefore()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            EnvironmentTable outerEnvironment = powerShell.NewEnvironmentTable();
            EnvironmentTable innerEnvironment = powerShell.NewEnvironmentTable();
            Assert.NotSame(outerEnvironment, innerEnvironment);

            // Act and assert
            using ModuleDatabaseScope outer = powerShell.SeatModuleDatabase(new ModuleConfiguration(), outerEnvironment);
            Assert.Same(outerEnvironment, ModuleDatabase.GetEnvironment());
            using (ModuleDatabaseScope inner = powerShell.SeatModuleDatabase(new ModuleConfiguration(), innerEnvironment))
            {
                Assert.Same(innerEnvironment, ModuleDatabase.GetEnvironment());
            }

            // Assert: the inner scope gave the outer one back rather than clearing it.
            Assert.Same(outerEnvironment, ModuleDatabase.GetEnvironment());
        }

        /// <summary>
        /// Verifies that a scope over nothing puts nothing back, rather than leaving its own database seated.
        /// </summary>
        /// <remarks>
        /// The case an implementation that only ever restores what it recognises would get wrong, and the one
        /// that matters most: every test outside this file starts with no database seated, so a scope that left
        /// one behind would hand the next test a configuration and an environment belonging to this one.
        /// </remarks>
        [Fact]
        public void Dispose_LeavesNothingSeatedWhenNothingWasSeatedBefore()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            _ = Assert.Throws<InvalidOperationException>(static () => ModuleDatabase.GetEnvironment());

            // Act
            using (ModuleDatabaseScope database = powerShell.SeatModuleDatabase(new ModuleConfiguration(), powerShell.NewEnvironmentTable()))
            {
                Assert.NotNull(ModuleDatabase.GetEnvironment());
            }

            // Assert
            _ = Assert.Throws<InvalidOperationException>(static () => ModuleDatabase.GetEnvironment());
        }
    }
}
