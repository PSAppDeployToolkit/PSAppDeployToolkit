using PSADT.Interop.Tests.TestHelpers;
using Xunit;

namespace PSADT.Interop.Tests
{
    /// <summary>
    /// Tests the thread creation flags, a hand-typed list of bit values with a reserved gap in the middle.
    /// </summary>
    public sealed class THREAD_CREATE_FLAGSTests
    {
        /// <summary>
        /// Verifies that the flags are the documented single bits, including the gap at 0x08 which Windows
        /// reserves. A hand-typed list of bit values is exactly where a doubled or skipped shift hides, and
        /// the gap makes a naive "each value is twice the last" check useless.
        /// </summary>
        [Fact]
        public void Values_AreTheDocumentedSingleBits()
        {
            // Assert
            EnumMembers.AssertValuesAre(EnumMembers.Get(typeof(THREAD_CREATE_FLAGS)), [0x01L, 0x02L, 0x04L, 0x10L, 0x20L, 0x40L]);
        }
    }
}
