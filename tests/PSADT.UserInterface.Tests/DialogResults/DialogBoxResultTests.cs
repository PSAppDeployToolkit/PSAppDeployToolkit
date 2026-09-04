using System;
using System.Linq;
using PSADT.UserInterface.DialogResults;
using PSADT.UserInterface.Tests.TestHelpers;
using Windows.Win32.UI.WindowsAndMessaging;
using Xunit;

namespace PSADT.UserInterface.Tests.DialogResults
{
    /// <summary>
    /// Tests the outcomes of a Win32 message box.
    /// </summary>
    /// <remarks>
    /// These carry the numeric values Win32 returns from <c language="csharp">MessageBox</c>, so the values are checked
    /// against the <c language="text">ID*</c> constants written out here rather than against the generated ones the source
    /// refers to. The lookup that turns a raw return value into one of these is built by reflecting over
    /// the type's own static fields, which is concise but means nothing fails until it is used.
    /// </remarks>
    public sealed class DialogBoxResultTests
    {
        /// <summary>
        /// Verifies the eleven outcomes and the Win32 values behind them.
        /// </summary>
        [Fact]
        public void Constants_MatchTheWin32MessageBoxResults()
        {
            // Arrange - IDOK through IDCONTINUE, plus IDTIMEOUT, from winuser.h.
            (string Name, long Value)[] expected =
            [
                ("OK", 1),
                ("Cancel", 2),
                ("Abort", 3),
                ("Retry", 4),
                ("Ignore", 5),
                ("Yes", 6),
                ("No", 7),
                ("Close", 8),
                ("TryAgain", 10),
                ("Continue", 11),
                ("Timeout", 32000),
            ];

            // Act
            (string Name, long Value)[] declared =
            [
                .. StaticConstants.Of<DialogBoxResult>().Select(static constant => (constant.Name, constant.Value.ToInt64())),
            ];

            // Assert
            Assert.Equal(expected, declared);
        }

        /// <summary>
        /// Verifies that every declared outcome can be resolved from the value Win32 returns.
        /// </summary>
        /// <remarks>
        /// The real regression here. The lookup is built by reflecting over the public static fields, so
        /// it covers whatever exists - but a constant whose value collided with another's would make the
        /// dictionary construction throw in a static initializer, surfacing as a
        /// <see cref="TypeInitializationException"/> from whichever member happened to be touched first.
        /// Resolving each one in turn proves the map is both complete and unambiguous.
        /// </remarks>
        [Fact]
        public void FromMessageBoxResult_ResolvesEveryDeclaredOutcome()
        {
            foreach ((string _, DialogBoxResult expected) in StaticConstants.Of<DialogBoxResult>())
            {
                // Act
                DialogBoxResult resolved = DialogBoxResult.FromMessageBoxResult((MESSAGEBOX_RESULT)(int)expected.ToInt64());

                // Assert - the same instance, not merely an equal one, since these are shared constants.
                Assert.Same(expected, resolved);
            }
        }

        /// <summary>
        /// Verifies that a value Win32 could return but the toolkit does not name is refused.
        /// </summary>
        /// <remarks>
        /// <c language="csharp">IDHELP</c> is the interesting case rather than an invented number: it is a real
        /// <c language="csharp">MESSAGEBOX_RESULT</c> that sits between two the toolkit does name, and it is absent here
        /// because none of the button sets on offer include a Help button. Should one ever be added, this
        /// is what says the result type has to gain a member too.
        /// </remarks>
        [Fact]
        public void FromMessageBoxResult_RefusesAResultTheToolkitDoesNotName()
        {
            // Act & Assert
            ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(static () => DialogBoxResult.FromMessageBoxResult(MESSAGEBOX_RESULT.IDHELP));
            Assert.Equal("value", exception.ParamName);
        }

        /// <summary>
        /// Verifies that the outcomes are distinct from one another.
        /// </summary>
        [Fact]
        public void Constants_AreDistinctFromOneAnother()
        {
            // Act
            DialogBoxResult[] all = [.. StaticConstants.Of<DialogBoxResult>().Select(static constant => constant.Value)];

            // Assert
            Assert.Equal(all.Length, all.Distinct().Count());
        }

        /// <summary>
        /// Verifies that an outcome compares equal to its name from PowerShell.
        /// </summary>
        [Fact]
        public void Constants_CompareEqualToTheirNameAsAString()
        {
            Assert.True(DialogBoxResult.Yes.Equals("yes"));
            Assert.False(DialogBoxResult.Yes.Equals("no"));
        }

        /// <summary>
        /// Verifies that the timeout outcome sits outside the range Win32 uses for buttons.
        /// </summary>
        /// <remarks>
        /// A message box shown with a timeout returns 32000, which is deliberately far from the button
        /// identifiers so it cannot be confused with a user's answer. Worth stating because it is the one
        /// value here that is not a button.
        /// </remarks>
        [Fact]
        public void Timeout_IsNotOneOfTheButtonValues()
        {
            Assert.True(DialogBoxResult.Timeout.ToInt64() > DialogBoxResult.Continue.ToInt64());
        }
    }
}
