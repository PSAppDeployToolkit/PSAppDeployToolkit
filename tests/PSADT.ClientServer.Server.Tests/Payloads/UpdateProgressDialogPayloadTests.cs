using System;
using PSADT.ClientServer.Payloads;
using PSADT.UserInterface;
using Xunit;

namespace PSADT.ClientServer.Server.Tests.Payloads
{
    /// <summary>
    /// Tests the payload asking a client to change what a progress dialog is showing.
    /// </summary>
    /// <remarks>
    /// Everything it carries is optional, and nothing means "leave this as it is" rather than "clear this".
    /// So a payload carrying nothing at all is legal - it asks for no change - while a message of
    /// whitespace is not, because a caller passing one meant to say something and did not.
    /// </remarks>
    public sealed class UpdateProgressDialogPayloadTests
    {
        /// <summary>
        /// Verifies that everything it was built with is carried.
        /// </summary>
        [Fact]
        public void UpdateProgressDialogPayload_CarriesEverythingItWasGiven()
        {
            // Arrange
            UpdateProgressDialogPayload payload = new("a message", "a detail message", 42.5, DialogMessageAlignment.Center);

            // Assert
            Assert.Equal("a message", payload.Message);
            Assert.Equal("a detail message", payload.DetailMessage);
            Assert.Equal(42.5, payload.Percentage);
            Assert.Equal(DialogMessageAlignment.Center, payload.Alignment);
        }

        /// <summary>
        /// Verifies that a payload asking for nothing is accepted and carries nothing.
        /// </summary>
        /// <remarks>
        /// Legal because each part means "leave this alone" when absent. A caller updating only the
        /// percentage sends the rest as nothing, and the extreme of that is a payload that changes nothing
        /// at all.
        /// </remarks>
        [Fact]
        public void UpdateProgressDialogPayload_AcceptsAskingForNothing()
        {
            // Arrange
            UpdateProgressDialogPayload payload = new();

            // Assert
            Assert.Null(payload.Message);
            Assert.Null(payload.DetailMessage);
            Assert.Null(payload.Percentage);
            Assert.Null(payload.Alignment);
        }

        /// <summary>
        /// Verifies that a message of nothing but space is refused, on either of the two that take text.
        /// </summary>
        /// <param name="text">The text to refuse.</param>
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void UpdateProgressDialogPayload_RefusesAMessageOfNothing(string text)
        {
            _ = Assert.Throws<ArgumentException>(() => new UpdateProgressDialogPayload(text));
            _ = Assert.Throws<ArgumentException>(() => new UpdateProgressDialogPayload(detailMessage: text));
        }

        /// <summary>
        /// Verifies that a percentage outside the range a progress bar can show is refused.
        /// </summary>
        /// <remarks>
        /// This payload used to carry whatever it was given and leave the client to decide - which the
        /// client does by assigning it to a progress bar, whose value setter refuses anything outside its
        /// range on the thread drawing the dialog. The same guard now sits on
        /// <c language="csharp">ProgressDialogOptions</c>, which is the other door to the same bar; this is the update door.
        /// <para>
        /// NaN and the infinities are included because a guard written only as "less than nought or
        /// greater than a hundred" lets NaN through: every comparison against it is false.
        /// </para>
        /// </remarks>
        /// <param name="percentage">The percentage to refuse.</param>
        [Theory]
        [InlineData(-1.0)]
        [InlineData(101.0)]
        [InlineData(double.NaN)]
        [InlineData(double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity)]
        public void UpdateProgressDialogPayload_RefusesAPercentageItCannotShow(double percentage)
        {
            // Act & Assert
            ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new UpdateProgressDialogPayload(percentage: percentage));
            Assert.Equal("percentage", exception.ParamName);
        }

        /// <summary>
        /// Verifies that the ends of the range are carried.
        /// </summary>
        /// <remarks>
        /// A hundred per cent is the value every determinate progress bar finishes on, so a guard written
        /// with the wrong comparison would break the last update of every deployment that reports one.
        /// </remarks>
        /// <param name="percentage">The percentage to carry.</param>
        [Theory]
        [InlineData(0.0)]
        [InlineData(100.0)]
        public void UpdateProgressDialogPayload_CarriesThePercentagesAtEachEndOfTheRange(double percentage)
        {
            Assert.Equal(percentage, new UpdateProgressDialogPayload(percentage: percentage).Percentage);
        }

        /// <summary>
        /// Verifies that every part of it counts towards the comparison, including the difference between
        /// nothing and a value.
        /// </summary>
        [Fact]
        public void UpdateProgressDialogPayload_ComparesByEverythingItCarries()
        {
            // Arrange
            UpdateProgressDialogPayload payload = new("a message", "a detail message", 42.5, DialogMessageAlignment.Center);

            // Assert
            Assert.Equal(payload, new UpdateProgressDialogPayload("a message", "a detail message", 42.5, DialogMessageAlignment.Center));
            Assert.NotEqual(payload, new UpdateProgressDialogPayload("another message", "a detail message", 42.5, DialogMessageAlignment.Center));
            Assert.NotEqual(payload, new UpdateProgressDialogPayload("a message", "another detail message", 42.5, DialogMessageAlignment.Center));
            Assert.NotEqual(payload, new UpdateProgressDialogPayload("a message", "a detail message", 42.6, DialogMessageAlignment.Center));
            Assert.NotEqual(payload, new UpdateProgressDialogPayload("a message", "a detail message", 42.5, DialogMessageAlignment.Left));
            Assert.NotEqual(payload, new UpdateProgressDialogPayload("a message", "a detail message", 42.5, alignment: null));
        }

        /// <summary>
        /// Verifies that it survives the trip to the client with everything intact, including the parts it
        /// is not carrying.
        /// </summary>
        [Fact]
        public void UpdateProgressDialogPayload_SurvivesTheTripToTheClient()
        {
            // Arrange
            UpdateProgressDialogPayload original = new("a message", percentage: 42.5);

            // Act
            UpdateProgressDialogPayload restored = DataSerialization.DeserializeFromBytes<UpdateProgressDialogPayload>(DataSerialization.SerializeToBytes(original));

            // Assert
            Assert.Equal(original, restored);
            Assert.Equal("a message", restored.Message);
            Assert.Null(restored.DetailMessage);
            Assert.Equal(42.5, restored.Percentage);
            Assert.Null(restored.Alignment);
        }
    }
}
