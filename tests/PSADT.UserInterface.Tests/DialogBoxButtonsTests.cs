using System;
using PSADT.UserInterface.Tests.TestHelpers;
using Xunit;

namespace PSADT.UserInterface.Tests
{
    /// <summary>
    /// Tests the button sets a message box can be asked to display.
    /// </summary>
    /// <remarks>
    /// The members carry the numeric values of the <c>MB_*</c> constants and are passed to
    /// <c>MessageBox</c> after being combined with an icon and a default button. A wrong value shows a
    /// different set of buttons rather than failing, so the values are checked against the Win32 numbers
    /// written out here rather than against the generated constants the source refers to - restating
    /// those would only assert that the compiler copied them.
    /// </remarks>
    public sealed class DialogBoxButtonsTests
    {
        /// <summary>
        /// Verifies that every member has the value Win32 gives the constant it is named for.
        /// </summary>
        [Fact]
        public void Members_MatchTheWin32ButtonConstants()
        {
            // Arrange - MB_OK through MB_CANCELTRYCONTINUE, from winuser.h.
            (string Name, ulong Value)[] expected =
            [
                ("Ok", 0x00000000),
                ("OkCancel", 0x00000001),
                ("AbortRetryIgnore", 0x00000002),
                ("YesNoCancel", 0x00000003),
                ("YesNo", 0x00000004),
                ("RetryCancel", 0x00000005),
                ("CancelTryContinue", 0x00000006),
            ];

            // Assert
            Assert.Equal(expected, EnumValues.DeclaredPairs<DialogBoxButtons>());
        }

        /// <summary>
        /// Verifies that the enum is stored as the width the Win32 parameter is.
        /// </summary>
        /// <remarks>
        /// The style parameter of <c>MessageBox</c> is a <c>UINT</c>. A narrower underlying type would
        /// still compile everywhere this is used and would truncate once combined with an icon value,
        /// which is why it is asserted rather than left to the declaration.
        /// </remarks>
        [Fact]
        public void UnderlyingType_IsUnsignedInt()
        {
            Assert.Equal(typeof(uint), Enum.GetUnderlyingType(typeof(DialogBoxButtons)));
        }

        /// <summary>
        /// Verifies that these values do not behave as bit flags, despite the attribute saying they do.
        /// </summary>
        /// <remarks>
        /// The one surprising thing about the type, and worth stating rather than leaving to be
        /// rediscovered. Win32 packs the button set into the low nibble of the style word as a small
        /// integer, so the members run 0 to 6 rather than occupying separate bits, and combining two of
        /// them with <c>|</c> silently produces a third valid member instead of a combination.
        /// <see cref="FlagsAttribute"/> is present so a caller can OR a button set together with an icon
        /// and a default button, which do occupy their own bits; it does not mean the button values
        /// themselves compose.
        /// </remarks>
        [Fact]
        public void Members_AreASmallIntegerRatherThanIndependentBits()
        {
            Assert.Equal(DialogBoxButtons.YesNoCancel, DialogBoxButtons.OkCancel | DialogBoxButtons.AbortRetryIgnore);
        }
    }
}
