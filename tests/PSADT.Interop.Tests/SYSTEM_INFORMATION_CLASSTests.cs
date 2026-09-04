using PSADT.Interop.Tests.TestHelpers;
using Xunit;

namespace PSADT.Interop.Tests
{
    /// <summary>
    /// Tests the system information classes, of which 244 of the 255 values are typed in by hand.
    /// </summary>
    public sealed class SYSTEM_INFORMATION_CLASSTests
    {
        /// <summary>
        /// Verifies that the classes form an unbroken run from zero, and that the trailing Max member
        /// equals the count of the members before it. A skipped or repeated number is the likeliest defect
        /// in a list this long, and this catches it wherever it lands.
        /// </summary>
        [Fact]
        public void Values_AreAContiguousSequenceFromZero()
        {
            // Assert
            EnumMembers.AssertContiguousFromZero(typeof(SYSTEM_INFORMATION_CLASS), 255, "MaxSystemInfoClass");
        }
    }
}
