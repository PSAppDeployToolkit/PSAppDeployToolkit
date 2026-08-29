using System;
using System.Linq;
using System.Reflection;
using PSADT.UserInterface.DialogResults;
using Xunit;

namespace PSADT.UserInterface.Tests.DialogResults
{
    /// <summary>
    /// Tests the result of a custom dialog.
    /// </summary>
    /// <remarks>
    /// This type exists to be compared against a string from PowerShell. Its equality is hand-written
    /// rather than generated precisely so that <c>$result -eq 'Continue'</c> works, and its result value
    /// is deliberately kept off the public surface so that PowerShell prints it as the bare string it
    /// stands for rather than as a one-column table. Neither of those is visible from the declaration,
    /// which is why both are pinned here.
    /// </remarks>
    public sealed class CustomDialogResultTests
    {
        /// <summary>
        /// Verifies that the result renders as its value.
        /// </summary>
        [Fact]
        public void ToString_IsTheResultValue()
        {
            Assert.Equal("Timeout", CustomDialogResult.DefaultResult.ToString());
        }

        /// <summary>
        /// Verifies that nothing is publicly readable on this type.
        /// </summary>
        /// <remarks>
        /// The load-bearing assertion in this file. PowerShell renders an object with no public
        /// properties or fields through its <see cref="object.ToString"/>, and this type relies on that to
        /// print as a bare string. Making the result value public - which looks like a tidy-up, since a
        /// derived type already exposes it - would silently turn every dialog result in a transcript into
        /// a formatted table.
        /// </remarks>
        [Fact]
        public void PublicSurface_HoldsNoReadableMembersSoPowerShellPrintsTheValue()
        {
            // Act
            string[] readable =
            [
                .. typeof(CustomDialogResult).GetProperties(BindingFlags.Instance | BindingFlags.Public).Select(static p => p.Name),
                .. typeof(CustomDialogResult).GetFields(BindingFlags.Instance | BindingFlags.Public).Select(static f => f.Name),
            ];

            // Assert
            Assert.True(readable.Length is 0, $"CustomDialogResult now exposes {string.Join(", ", readable)}, which stops PowerShell rendering it as a string.");
        }

        /// <summary>
        /// Verifies that a result compares equal to its own value as a string, ignoring case.
        /// </summary>
        /// <remarks>
        /// The reason <c>Equals(object)</c> is overridden at all. PowerShell's <c>-eq</c> calls it rather
        /// than any operator, so this is what makes a comparison against a literal work in a deployment
        /// script.
        /// </remarks>
        /// <param name="value">The string to compare against.</param>
        [Theory]
        [InlineData("Timeout")]
        [InlineData("timeout")]
        [InlineData("TIMEOUT")]
        public void Equals_MatchesTheValueAsAStringIgnoringCase(string value)
        {
            Assert.True(CustomDialogResult.DefaultResult.Equals(value));
        }

        /// <summary>
        /// Verifies that a different string does not match.
        /// </summary>
        [Fact]
        public void Equals_DoesNotMatchADifferentString()
        {
            Assert.False(CustomDialogResult.DefaultResult.Equals("Continue"));
        }

        /// <summary>
        /// Verifies that string comparison is offered only by this type and not by its derivatives.
        /// </summary>
        /// <remarks>
        /// A derived result carries more than its result value, so answering "equal" to a bare string
        /// would be claiming an equivalence that loses the rest. The exact-type check in the base is what
        /// prevents it, and it is worth an assertion because the check is easy to read past.
        /// </remarks>
        [Fact]
        public void Equals_DoesNotMatchAStringForADerivedResult()
        {
            Assert.False(InputDialogResult.DefaultResult.Equals("Timeout"));
        }

        /// <summary>
        /// Verifies that two results with the same value are equal and that the hash agrees.
        /// </summary>
        [Fact]
        public void Equality_IsByTheValueForTheSameType()
        {
            // Arrange
            CustomDialogResult first = Create("Continue");
            CustomDialogResult second = Create("Continue");

            // Assert
            Assert.Equal(first, second);
            Assert.Equal(first.GetHashCode(), second.GetHashCode());
            Assert.NotEqual(first, Create("Close"));
        }

        /// <summary>
        /// Verifies that comparison against a derived type is refused in both directions.
        /// </summary>
        /// <remarks>
        /// Equality has to be symmetric to be usable in a collection, and comparing across the hierarchy
        /// is where a hand-written implementation usually loses that.
        /// </remarks>
        [Fact]
        public void Equality_IsRefusedAcrossTheHierarchyInBothDirections()
        {
            // Arrange
            CustomDialogResult baseResult = Create("Timeout");

            // Assert
            Assert.False(baseResult.Equals(InputDialogResult.DefaultResult));
            Assert.False(InputDialogResult.DefaultResult.Equals(baseResult));
        }

        /// <summary>
        /// Verifies that a result converts implicitly to its value.
        /// </summary>
        [Fact]
        public void ImplicitConversion_YieldsTheValue()
        {
            // Act
            string value = CustomDialogResult.DefaultResult;

            // Assert
            Assert.Equal("Timeout", value);
        }

        /// <summary>
        /// Verifies that converting a null result is refused rather than producing a null string.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0191:Do not use the null-forgiving operator", Justification = "This is deliberate as part of unit testing.")]
        [Fact]
        public void ImplicitConversion_RefusesANullResult()
        {
            _ = Assert.Throws<ArgumentNullException>(static () => (string)(CustomDialogResult)null!);
        }

        /// <summary>
        /// Verifies that a result with no value is refused.
        /// </summary>
        /// <param name="value">The blank value to refuse.</param>
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_RefusesABlankValue(string value)
        {
            _ = Assert.Throws<ArgumentException>(() => Create(value));
        }

        /// <summary>
        /// Verifies that the shared default names a timeout.
        /// </summary>
        /// <remarks>
        /// The module tests a returned result against this to tell a dialog that expired from one the user
        /// answered, so the value is a contract rather than an implementation detail.
        /// </remarks>
        [Fact]
        public void DefaultResult_IsATimeout()
        {
            Assert.Equal("Timeout", CustomDialogResult.DefaultResult.ToString());
        }

        /// <summary>
        /// Builds a result through its internal constructor.
        /// </summary>
        /// <param name="value">The result value.</param>
        /// <returns>The result.</returns>
        private static CustomDialogResult Create(string value)
        {
            return new(value);
        }
    }
}
