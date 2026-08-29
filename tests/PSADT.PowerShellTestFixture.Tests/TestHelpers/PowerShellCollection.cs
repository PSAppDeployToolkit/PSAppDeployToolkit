using Xunit;

namespace PSADT.PowerShellTestFixture.Tests.TestHelpers
{
    /// <summary>
    /// The collection every test that needs a PowerShell engine belongs to.
    /// </summary>
    /// <remarks>
    /// A runspace runs one pipeline at a time, and <see cref="System.Management.Automation.Runspaces.Runspace.DefaultRunspace"/>
    /// is per-thread. Both facts mean these tests cannot run concurrently, and a collection is how that is
    /// expressed: xunit never runs two tests from the same collection at once, and shares one
    /// <see cref="PowerShellFixture"/> across them so the engine is opened once rather than per class.
    /// <para>
    /// A deliberate near-copy of the ones beside the two suites that use the fixture to test something else. The
    /// fixture is shared, but a collection definition is per-assembly, so each project declares its own.
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
