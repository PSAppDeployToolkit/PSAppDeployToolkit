using System;
using PSAppDeployToolkit.Foundation;
using PSAppDeployToolkit.Tests.Fixture;
using PSAppDeployToolkit.Tests.TestHelpers;
using Xunit;

namespace PSAppDeployToolkit.Tests.Foundation
{
    /// <summary>
    /// Tests the snapshot of the machine and session the module takes once during initialisation.
    /// </summary>
    /// <remarks>
    /// Built through its real constructor rather than fabricated, which takes a live <c>PSCmdlet</c> and
    /// therefore a hosted PowerShell engine - see <see cref="PowerShellFixture"/> for why that is the only way in.
    /// <para>
    /// This file covers the constructor's contract and the type's identity. The hundred-odd derived members are
    /// asserted separately; what is here is the part that had to be settled first, because it decided whether the
    /// type stays a record.
    /// </para>
    /// </remarks>
    /// <param name="powerShell">The hosted engine, shared across the collection.</param>
    [Collection(PowerShellCollection.Name)]
    public sealed class EnvironmentTableTests(PowerShellFixture powerShell)
    {
        /// <summary>
        /// Verifies that two tables built moments apart are not equal, even though the machine did not change
        /// between them.
        /// </summary>
        /// <remarks>
        /// This is the decision, written down. A table is a snapshot taken at a moment: a caller who initialises
        /// the module, keeps the table, and initialises again holds two snapshots, and telling them apart is the
        /// point. So the type is an identity rather than a value, and it is no longer declared as a record.
        /// <para>
        /// Worth noting that this test passed before the declaration changed, too - a record holding fifty
        /// <see cref="System.IO.DirectoryInfo"/> properties was already comparing by reference through them, so it
        /// never delivered the value equality it advertised. Removing the record removed a promise nothing kept
        /// rather than any behaviour a caller could have relied on.
        /// </para>
        /// </remarks>
        [Fact]
        public void Equals_IsByIdentityRatherThanByValue()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            EnvironmentTable first = powerShell.NewEnvironmentTable();
            EnvironmentTable second = powerShell.NewEnvironmentTable();

            // Assert
            Assert.NotEqual(first, second);
            Assert.NotSame(first, second);
            Assert.Equal(first, first);
        }

        /// <summary>
        /// Verifies that a table is equal to itself and that its hash code does not move.
        /// </summary>
        /// <remarks>
        /// The other half of an identity: reference equality is only useful if it is stable, and a hash code that
        /// varied between reads would make the table unusable as a dictionary key or set member. Several of its
        /// members delegate to live probes on each read, which is exactly how a record's generated hash code
        /// would have drifted.
        /// </remarks>
        [Fact]
        public void GetHashCode_DoesNotChangeBetweenReads()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            EnvironmentTable table = powerShell.NewEnvironmentTable();

            // Assert
            Assert.Equal(table.GetHashCode(), table.GetHashCode());
        }

        /// <summary>
        /// Verifies that the constructor refuses a missing cmdlet, version table or version.
        /// </summary>
        /// <remarks>
        /// The cmdlet argument is checked last in the constructor even though it is checked for null, so a caller
        /// passing nothing at all gets told about the version table first. Asserted individually rather than as a
        /// set so the parameter name in each exception is confirmed.
        /// </remarks>
        [Fact]
        public void EnvironmentTable_RefusesMissingArguments()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();

            // Assert
            Assert.Equal("psVersionTable", Assert.Throws<ArgumentNullException>(static () => new EnvironmentTable(null!, null!, null!)).ParamName);
            Assert.Equal("psVersion", Assert.Throws<ArgumentNullException>(static () => new EnvironmentTable(null!, [], null!)).ParamName);
            Assert.Equal("cmdlet", Assert.Throws<ArgumentNullException>(static () => new EnvironmentTable(null!, [], new Version(7, 4))).ParamName);
        }

        /// <summary>
        /// Verifies that the table names the module it was built from.
        /// </summary>
        /// <remarks>
        /// Not incidental. These three come from the module the constructing cmdlet belongs to, and
        /// <c>AppDeployToolkitName</c> goes on to be built into install names, log file names and the registry
        /// path deferral history is kept under, so a table that could not find its module would break all three.
        /// </remarks>
        [Fact]
        public void EnvironmentTable_NamesTheModuleItWasBuiltFrom()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            EnvironmentTable table = powerShell.NewEnvironmentTable();

            // Assert
            Assert.Equal(PowerShellFixture.ModuleName, table.AppDeployToolkitName);
            Assert.False(string.IsNullOrWhiteSpace(table.AppDeployToolkitPath));
            Assert.NotNull(table.AppDeployMainScriptVersion);
        }

        /// <summary>
        /// Verifies that the table carries the version table and engine version it was handed.
        /// </summary>
        [Fact]
        public void EnvironmentTable_CarriesTheEngineVersionItWasHanded()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            EnvironmentTable table = powerShell.NewEnvironmentTable();

            // Assert
            Assert.NotNull(table.EnvPSVersionTable);
            Assert.True(table.EnvPSVersionTable.Count > 0);
            Assert.Equal(table.EnvPSVersion.Major, table.EnvPSVersionMajor);
            Assert.Equal(table.EnvPSVersion.Minor, table.EnvPSVersionMinor);
        }
    }
}
