using System;
using System.IO;
using System.Runtime.Serialization;
using PSADT.WindowManagement;
using Xunit;

namespace PSADT.Tests.WindowManagement
{
    /// <summary>
    /// Tests the request to send a key sequence to a window.
    /// </summary>
    /// <remarks>
    /// The keys are never actually sent here: doing so would type into whatever window the machine has
    /// focused, which is a change to what the person at it is doing. What is covered is the request
    /// itself, which crosses a process boundary on its way to a client running in the user's session and
    /// so has to survive being serialised.
    /// </remarks>
    public sealed class SendKeysOptionsTests
    {
        /// <summary>
        /// Verifies that the window and the keys handed in are the ones read back.
        /// </summary>
        [Fact]
        public void SendKeysOptions_KeepsWhatItIsGiven()
        {
            // Act
            SendKeysOptions options = new(0x1234, "^{ESC}");

            // Assert
            Assert.Equal(0x1234, options.WindowHandle);
            Assert.Equal("^{ESC}", options.Keys);
        }

        /// <summary>
        /// Verifies that a request with no keys in it is refused, since it would ask a window to be sent
        /// nothing.
        /// </summary>
        /// <param name="keys">The blank sequence to refuse.</param>
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void SendKeysOptions_RefusesABlankSequence(string keys)
        {
            _ = Assert.Throws<ArgumentException>(() => new SendKeysOptions(0x1234, keys));
        }

        /// <summary>
        /// Verifies that a request with no keys at all is refused.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0191:Do not use the null-forgiving operator", Justification = "This is deliberate as part of unit testing.")]
        [Fact]
        public void SendKeysOptions_RefusesANullSequence()
        {
            _ = Assert.Throws<ArgumentNullException>(static () => new SendKeysOptions(0x1234, null!));
        }

        /// <summary>
        /// Verifies that the request survives a data contract round trip, which is what happens between
        /// the deployment process building it and the client that sends the keys reading it.
        /// </summary>
        [Fact]
        public void Serialization_RoundTripsTheRequest()
        {
            // Arrange
            SendKeysOptions original = new(0x1234, "^{ESC}");
            DataContractSerializer serializer = new(typeof(SendKeysOptions));

            // Act
            using MemoryStream stream = new();
            serializer.WriteObject(stream, original);
            stream.Position = 0;

            // Assigned through a local rather than cast inline: the two target frameworks disagree on
            // whether ReadObject's return is nullable, so a null-forgiving operator is necessary on one
            // and flagged as redundant on the other.
            object? deserialized = serializer.ReadObject(stream);
            Assert.NotNull(deserialized);
            SendKeysOptions restored = (SendKeysOptions)deserialized;

            // Assert
            Assert.Equal(original, restored);
            Assert.Equal(original.WindowHandle, restored.WindowHandle);
            Assert.Equal(original.Keys, restored.Keys);
        }

        /// <summary>
        /// Verifies that two requests naming the same window and the same keys are equal.
        /// </summary>
        [Fact]
        public void Equality_IsByTheWindowAndTheKeys()
        {
            Assert.Equal(new SendKeysOptions(0x1234, "^{ESC}"), new SendKeysOptions(0x1234, "^{ESC}"));
            Assert.NotEqual(new SendKeysOptions(0x1234, "^{ESC}"), new SendKeysOptions(0x9999, "^{ESC}"));
            Assert.NotEqual(new SendKeysOptions(0x1234, "^{ESC}"), new SendKeysOptions(0x1234, "{ENTER}"));
        }
    }
}
