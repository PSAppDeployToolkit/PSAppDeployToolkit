using PSADT.Interop.Tests.TestHelpers;
using Xunit;

namespace PSADT.Interop.Tests
{
    /// <summary>
    /// Tests the extended process creation flags, which are hand-typed from the unofficial headers.
    /// </summary>
    public sealed class EXTENDED_PROCESS_CREATION_FLAGTests
    {
        /// <summary>
        /// Verifies that the flags are consecutive single bits from the lowest.
        /// </summary>
        [Fact]
        public void Values_AreConsecutiveSingleBits()
        {
            // Assert
            EnumMembers.AssertValuesAre(EnumMembers.Get(typeof(EXTENDED_PROCESS_CREATION_FLAG)), [0x01L, 0x02L, 0x04L]);
        }
    }
}
