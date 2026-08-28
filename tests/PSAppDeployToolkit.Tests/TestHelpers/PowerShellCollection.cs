using PSAppDeployToolkit.Tests.Fixture;
using Xunit;

namespace PSAppDeployToolkit.Tests.TestHelpers
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
    /// Declared here rather than in the fixture assembly on purpose. The fixture is also used by
    /// PSADT.ClientServer.Server.Tests, and keeping the xunit attributes out of it leaves that project free to
    /// declare its own collection without the two fighting over one definition.
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
