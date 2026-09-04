using System;
using System.Linq;
using PSADT.UserInterface.Tests.TestHelpers;
using Xunit;

namespace PSADT.UserInterface.Tests
{
    /// <summary>
    /// Tests the button sets a message box can be asked to display.
    /// </summary>
    /// <remarks>
    /// The members carry the numeric values of the <c language="text">MB_*</c> constants and are passed to
    /// <c language="csharp">MessageBox</c> after being combined with an icon and a default button. A wrong value shows a
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
        /// The style parameter of <c language="csharp">MessageBox</c> is a <c language="csharp">uint</c>. A narrower underlying type would
        /// still compile everywhere this is used and would truncate once combined with an icon value,
        /// which is why it is asserted rather than left to the declaration.
        /// </remarks>
        [Fact]
        public void UnderlyingType_IsUnsignedInt()
        {
            Assert.Equal(typeof(uint), Enum.GetUnderlyingType(typeof(DialogBoxButtons)));
        }

        /// <summary>
        /// Verifies that the type is not marked as flags, because these values do not compose.
        /// </summary>
        /// <remarks>
        /// Win32 packs the button set into the low nibble of the style word as a small integer, so the
        /// members run 0 to 6 rather than occupying separate bits. <c language="csharp">OkCancel | AbortRetryIgnore</c> is
        /// therefore <c language="csharp">YesNoCancel</c> - a different valid button set rather than a combination - and
        /// while the type carried <see cref="FlagsAttribute"/> that was something a caller could write and
        /// nothing would object to. Without it the analysers refuse the expression outright, so this test
        /// guards the declaration rather than the arithmetic: the arithmetic can no longer be written.
        /// <para>
        /// The style word is still assembled by combining a button set with an icon and a default button,
        /// but that is done on <c language="csharp">MESSAGEBOX_STYLE</c> after casting, which is where those bits actually
        /// live. Nothing needs this type to be flags for that to work.
        /// </para>
        /// </remarks>
        [Fact]
        public void Members_AreASmallIntegerRatherThanIndependentBits()
        {
            // Assert - not flags, and numbered as consecutive choices rather than as bits.
            Assert.False(Attribute.IsDefined(typeof(DialogBoxButtons), typeof(FlagsAttribute)));
            Assert.Equal<ulong[]>([0, 1, 2, 3, 4, 5, 6], [.. EnumValues.DeclaredPairs<DialogBoxButtons>().Select(static member => member.Value)]);
        }
    }
}
