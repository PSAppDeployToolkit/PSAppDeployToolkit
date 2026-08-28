using PSADT.Interop.Tests.TestHelpers;
using Xunit;

namespace PSADT.Interop.Tests
{
    /// <summary>
    /// Tests the object information classes, which mix aliased and hand-typed values in the same run.
    /// </summary>
    public sealed class OBJECT_INFORMATION_CLASSTests
    {
        /// <summary>
        /// Verifies that the classes form an unbroken run from zero, and that the trailing Max member
        /// equals the count of the members before it.
        /// </summary>
        [Fact]
        public void Values_AreAContiguousSequenceFromZero()
        {
            // Assert
            EnumMembers.AssertContiguousFromZero(typeof(OBJECT_INFORMATION_CLASS), 9, "MaxObjectInfoClass");
        }
    }
}
