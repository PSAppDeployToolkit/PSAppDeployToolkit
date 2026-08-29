using System;
using PSADT.UserInterface.Tests.TestHelpers;
using Xunit;

namespace PSADT.UserInterface.Tests
{
    /// <summary>
    /// Tests the icon a message box shows beside its text.
    /// </summary>
    /// <remarks>
    /// Win32 gives these four icons only two pairs of distinct values: <c>MB_ICONSTOP</c> is
    /// <c>MB_ICONHAND</c> and <c>MB_ICONINFORMATION</c> is <c>MB_ICONASTERISK</c>. The members here are
    /// named for the Visual Basic constants the toolkit's own callers use, which is why <c>Stop</c> and
    /// <c>Information</c> appear rather than the hand and asterisk they resolve to.
    /// </remarks>
    public sealed class DialogBoxIconTests
    {
        /// <summary>
        /// Verifies that every member has the value Win32 gives the constant it is named for.
        /// </summary>
        [Fact]
        public void Members_MatchTheWin32IconConstants()
        {
            // Arrange - MB_ICONHAND, MB_ICONQUESTION, MB_ICONEXCLAMATION and MB_ICONASTERISK, from
            // winuser.h. Stop is an alias of the first and Information of the last.
            (string Name, ulong Value)[] expected =
            [
                ("Stop", 0x00000010),
                ("Question", 0x00000020),
                ("Exclamation", 0x00000030),
                ("Information", 0x00000040),
            ];

            // Assert
            Assert.Equal(expected, EnumValues.DeclaredPairs<DialogBoxIcon>());
        }

        /// <summary>
        /// Verifies that the enum is stored as the width the Win32 parameter is.
        /// </summary>
        [Fact]
        public void UnderlyingType_IsUnsignedInt()
        {
            Assert.Equal(typeof(uint), Enum.GetUnderlyingType(typeof(DialogBoxIcon)));
        }

        /// <summary>
        /// Verifies that the type has no member for "no icon", so that the absence of one is expressed
        /// by not asking for an icon at all.
        /// </summary>
        /// <remarks>
        /// Win32 has no <c>MB_ICONNONE</c>; passing zero is what suppresses the icon. The options type
        /// therefore holds a nullable <see cref="DialogBoxIcon"/> and a zero member here would give two
        /// spellings of the same thing, one of which would survive a round trip as a value rather than
        /// as an absence. Both suppression rules on the declaration exist to allow this.
        /// </remarks>
        [Fact]
        public void Members_DoNotIncludeAZeroValue()
        {
            Assert.DoesNotContain(EnumValues.Declared<DialogBoxIcon>(), static icon => icon is 0);
        }
    }
}
