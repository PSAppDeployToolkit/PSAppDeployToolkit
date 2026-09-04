using System;
using PSADT.UserInterface.Tests.TestHelpers;
using Xunit;

namespace PSADT.UserInterface.Tests
{
    /// <summary>
    /// Tests which button a message box preselects.
    /// </summary>
    /// <remarks>
    /// Unlike the button sets, these really are bit values: Win32 puts the default button in bits 8 to
    /// 11 of the style word, so they have to be spaced 0x100 apart to survive being combined with a
    /// button set and an icon. A member numbered 1, 2, 3 by mistake would collide with the button set
    /// values and quietly change which buttons appear.
    /// </remarks>
    public sealed class DialogBoxDefaultButtonTests
    {
        /// <summary>
        /// Verifies that every member has the value Win32 gives the constant it is named for.
        /// </summary>
        [Fact]
        public void Members_MatchTheWin32DefaultButtonConstants()
        {
            // Arrange - MB_DEFBUTTON1 through MB_DEFBUTTON3, from winuser.h.
            (string Name, ulong Value)[] expected =
            [
                ("First", 0x00000000),
                ("Second", 0x00000100),
                ("Third", 0x00000200),
            ];

            // Assert
            Assert.Equal(expected, EnumValues.DeclaredPairs<DialogBoxDefaultButton>());
        }

        /// <summary>
        /// Verifies that the enum is stored as the width the Win32 parameter is.
        /// </summary>
        [Fact]
        public void UnderlyingType_IsUnsignedInt()
        {
            Assert.Equal(typeof(uint), Enum.GetUnderlyingType(typeof(DialogBoxDefaultButton)));
        }

        /// <summary>
        /// Verifies that a default button can be combined with a button set and an icon without any of
        /// the three disturbing the others.
        /// </summary>
        /// <remarks>
        /// This is what the three enums exist to do together, and the only place it is checked. Each
        /// occupies its own part of the style word, so the combination has to be separable back into the
        /// three values that went into it.
        /// </remarks>
        [Fact]
        public void Members_OccupyTheirOwnBitsInTheStyleWord()
        {
            // Act
            const uint style = (uint)DialogBoxButtons.YesNoCancel | (uint)DialogBoxIcon.Exclamation | (uint)DialogBoxDefaultButton.Third;

            // Assert
            Assert.Equal((uint)DialogBoxButtons.YesNoCancel, style & 0x0000000F);
            Assert.Equal((uint)DialogBoxIcon.Exclamation, style & 0x000000F0);
            Assert.Equal((uint)DialogBoxDefaultButton.Third, style & 0x00000F00);
        }
    }
}
