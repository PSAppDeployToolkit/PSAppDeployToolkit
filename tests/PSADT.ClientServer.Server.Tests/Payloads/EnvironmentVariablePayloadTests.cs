using System;
using PSADT.ClientServer.Payloads;
using Xunit;

namespace PSADT.ClientServer.Server.Tests.Payloads
{
    /// <summary>
    /// Tests the payload carrying an environment variable operation.
    /// </summary>
    /// <remarks>
    /// One payload serves three commands - reading a variable, setting one and removing one - so most of
    /// what it carries is unused on any given trip, and the defaults are what the reading and removing
    /// commands rely on. Both name and value are refused when blank, which matters because a variable named
    /// by whitespace is not a variable and a value of whitespace cannot be told from one that went missing.
    /// </remarks>
    public sealed class EnvironmentVariablePayloadTests
    {
        /// <summary>
        /// Verifies that everything it was built with is carried.
        /// </summary>
        [Fact]
        public void EnvironmentVariablePayload_CarriesEverythingItWasGiven()
        {
            // Arrange
            EnvironmentVariablePayload payload = new("PATH", @"C:\a\folder", expandable: true, append: true, remove: true);

            // Assert
            Assert.Equal("PATH", payload.Name);
            Assert.Equal(@"C:\a\folder", payload.Value);
            Assert.True(payload.Expandable);
            Assert.True(payload.Append);
            Assert.True(payload.Remove);
        }

        /// <summary>
        /// Verifies that a payload built with a name alone carries nothing else, which is the shape the
        /// reading and removing commands send.
        /// </summary>
        [Fact]
        public void EnvironmentVariablePayload_DefaultsToCarryingNothingElse()
        {
            // Arrange
            EnvironmentVariablePayload payload = new("PATH");

            // Assert
            Assert.Equal("PATH", payload.Name);
            Assert.Null(payload.Value);
            Assert.False(payload.Expandable);
            Assert.False(payload.Append);
            Assert.False(payload.Remove);
        }

        /// <summary>
        /// Verifies that a name of nothing is refused.
        /// </summary>
        /// <param name="name">The name to refuse.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0191:Do not use the null-forgiving operator", Justification = "This is deliberate as part of unit testing.")]
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void EnvironmentVariablePayload_RefusesANameOfNothing(string? name)
        {
            _ = Assert.ThrowsAny<ArgumentException>(() => new EnvironmentVariablePayload(name!));
        }

        /// <summary>
        /// Verifies that a value of nothing but space is refused, while no value at all is accepted.
        /// </summary>
        [Fact]
        public void EnvironmentVariablePayload_RefusesAValueOfNothingButAcceptsNone()
        {
            _ = Assert.Throws<ArgumentException>(static () => new EnvironmentVariablePayload("PATH", ""));
            _ = Assert.Throws<ArgumentException>(static () => new EnvironmentVariablePayload("PATH", "   "));
            Assert.Null(new EnvironmentVariablePayload("PATH", value: null).Value);
        }

        /// <summary>
        /// Verifies that every part of it counts towards the comparison, since each changes what the client
        /// is being asked to do.
        /// </summary>
        [Fact]
        public void EnvironmentVariablePayload_ComparesByEverythingItCarries()
        {
            // Arrange
            EnvironmentVariablePayload payload = new("PATH", "a value", expandable: true, append: true, remove: true);

            // Assert
            Assert.Equal(payload, new EnvironmentVariablePayload("PATH", "a value", expandable: true, append: true, remove: true));
            Assert.NotEqual(payload, new EnvironmentVariablePayload("OTHER", "a value", expandable: true, append: true, remove: true));
            Assert.NotEqual(payload, new EnvironmentVariablePayload("PATH", "another value", expandable: true, append: true, remove: true));
            Assert.NotEqual(payload, new EnvironmentVariablePayload("PATH", "a value", expandable: false, append: true, remove: true));
            Assert.NotEqual(payload, new EnvironmentVariablePayload("PATH", "a value", expandable: true, append: false, remove: true));
            Assert.NotEqual(payload, new EnvironmentVariablePayload("PATH", "a value", expandable: true, append: true, remove: false));
        }

        /// <summary>
        /// Verifies that it survives the trip to the client with everything intact.
        /// </summary>
        [Fact]
        public void EnvironmentVariablePayload_SurvivesTheTripToTheClient()
        {
            // Arrange
            EnvironmentVariablePayload original = new("PATH", @"C:\a\folder", expandable: true, append: true, remove: false);

            // Act
            EnvironmentVariablePayload restored = DataSerialization.DeserializeFromBytes<EnvironmentVariablePayload>(DataSerialization.SerializeToBytes(original));

            // Assert
            Assert.Equal(original, restored);
            Assert.Equal(@"C:\a\folder", restored.Value);
            Assert.True(restored.Expandable);
            Assert.False(restored.Remove);
        }
    }
}
