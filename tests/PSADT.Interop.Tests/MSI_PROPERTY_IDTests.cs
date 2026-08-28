using Xunit;

namespace PSADT.Interop.Tests
{
    /// <summary>
    /// Tests the summary-information property identifiers, one of which is hand-typed because CsWin32 does
    /// not surface it.
    /// </summary>
    public sealed class MSI_PROPERTY_IDTests
    {
        /// <summary>
        /// Verifies that the one hand-typed identifier sits immediately after its compiler-checked
        /// neighbour, which is where the property-set specification puts it. The neighbour is the only
        /// oracle available for it.
        /// </summary>
        [Fact]
        public void HandTypedIdentifier_FollowsItsNeighbour()
        {
            // Assert
            Assert.Equal((uint)MSI_PROPERTY_ID.PID_APPNAME + 1, (uint)MSI_PROPERTY_ID.PID_SECURITY);
        }
    }
}
