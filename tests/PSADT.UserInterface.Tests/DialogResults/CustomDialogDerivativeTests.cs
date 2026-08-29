using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using PSADT.UserInterface.DialogResults;
using Xunit;

namespace PSADT.UserInterface.Tests.DialogResults
{
    /// <summary>
    /// Tests the base shared by the dialog results that carry a value alongside their outcome.
    /// </summary>
    /// <remarks>
    /// Abstract and with a <c language="csharp">private protected</c> constructor, so it is exercised through
    /// <see cref="InputDialogResult"/>. What it contributes is one property, and the shape of that
    /// property is the whole point of the type: it re-exposes the base's non-public result value so a
    /// derived result prints as a table, without declaring a second field that would put the value on the
    /// wire twice.
    /// </remarks>
    public sealed class CustomDialogDerivativeTests
    {
        /// <summary>
        /// Verifies that a derived result does expose its result value.
        /// </summary>
        /// <remarks>
        /// The counterpart to
        /// <see cref="CustomDialogResultTests.PublicSurface_HoldsNoReadableMembersSoPowerShellPrintsTheValue"/>.
        /// The base hides the value so it renders as a string; the derivative re-exposes it so a result
        /// carrying more than an outcome renders as a table listing both. Removing this property would
        /// make an input dialog's result print only its text, with no indication of whether the user
        /// confirmed or cancelled.
        /// </remarks>
        [Fact]
        public void Result_IsPubliclyReadableOnADerivedResult()
        {
            // Act
            PropertyInfo? result = typeof(CustomDialogDerivative).GetProperty("Result", BindingFlags.Instance | BindingFlags.Public);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Timeout", result.GetValue(InputDialogResult.DefaultResult));
        }

        /// <summary>
        /// Verifies that it is a property rather than a field.
        /// </summary>
        /// <remarks>
        /// Not pedantry about member kinds. A field here would need a <see cref="DataMemberAttribute"/> to
        /// survive the pipe, and that would be a second data member of the same name as the base's - which
        /// is what the type used to do, writing the value into the XML twice for every input and
        /// list-selection result sent. A property carries no data member and reads through to the one
        /// field that does.
        /// </remarks>
        [Fact]
        public void Result_IsAPropertyRatherThanASecondField()
        {
            // Act
            string[] fields = [.. typeof(CustomDialogDerivative).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly).Select(static f => f.Name)];

            // Assert
            Assert.True(fields.Length is 0, $"CustomDialogDerivative declares {string.Join(", ", fields)}; a field here duplicates the base's data member.");
        }

        /// <summary>
        /// Verifies that the value read through the derivative is the one the base was given.
        /// </summary>
        [Fact]
        public void Result_ReadsThroughToTheValueTheBaseHolds()
        {
            // Act
            InputDialogResult result = new("Continue", "typed text");

            // Assert
            Assert.Equal("Continue", result.Result);
            Assert.Equal("Continue", result.ToString());
        }

        /// <summary>
        /// Verifies that a derived result with no outcome is refused.
        /// </summary>
        /// <remarks>
        /// The check lives in the base constructor now rather than being repeated here, so this confirms
        /// the derivative still inherits it.
        /// </remarks>
        /// <param name="value">The blank outcome to refuse.</param>
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_RefusesABlankResult(string value)
        {
            _ = Assert.Throws<ArgumentException>(() => new InputDialogResult(value, text: null));
        }
    }
}
