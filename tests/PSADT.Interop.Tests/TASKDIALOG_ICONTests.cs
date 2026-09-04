using PSADT.Interop.Tests.TestHelpers;
using Xunit;

namespace PSADT.Interop.Tests
{
    /// <summary>
    /// Tests the icons a task dialog can display, a pointer-valued constant family whose members are declared one per line from a
    /// Windows SDK symbol.
    /// </summary>
    public sealed class TASKDIALOG_ICONTests
    {
        /// <summary>
        /// Verifies that every constant names its own field and holds a value no sibling holds. A member
        /// built from the wrong SDK symbol still compiles and still looks right; it shows up here as two
        /// members sharing a value.
        /// </summary>
        [Fact]
        public void Members_AreNamedAfterTheirFieldAndHoldDistinctValues()
        {
            // Assert
            ConstantFamily.AssertMembersAreNamedAndDistinct<TASKDIALOG_ICON>(4);
        }
    }
}
