using PSADT.PowerShellTestFixture;
using Xunit;

namespace PSADT.ClientServer.Server.Tests.TestHelpers
{
    /// <summary>
    /// The collection every test that needs a PowerShell engine belongs to.
    /// </summary>
    /// <remarks>
    /// A runspace runs one pipeline at a time, and the code under test reaches PowerShell through
    /// <c>Runspace.DefaultRunspace</c>, which is per-thread. Both facts mean these tests cannot run
    /// concurrently, and a collection is how that is expressed: xunit never runs two tests from the same
    /// collection at once, and shares one <see cref="PowerShellFixture"/> across them so the engine is opened
    /// once rather than per class.
    /// <para>
    /// A deliberate near-copy of the one beside PSAppDeployToolkit's own tests. The fixture is shared, but a
    /// collection definition is per-assembly, so each project that uses it declares its own.
    /// </para>
    /// </remarks>
    [CollectionDefinition(Name)]
    public sealed class PowerShellCollection : ICollectionFixture<PowerShellFixture>
    {
        /// <summary>
        /// The collection's name, for <see cref="CollectionAttribute"/>.
        /// </summary>
        public const string Name = "PowerShell";
    }
}
