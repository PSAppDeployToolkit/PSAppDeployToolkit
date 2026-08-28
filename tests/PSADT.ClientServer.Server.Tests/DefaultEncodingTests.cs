using System.Text;
using Xunit;

namespace PSADT.ClientServer.Server.Tests
{
    /// <summary>
    /// Tests the encoding the client and server agree to speak.
    /// </summary>
    /// <remarks>
    /// Both properties it is configured with are load-bearing rather than stylistic. Emitting no byte
    /// order mark keeps three bytes off the front of everything encoded with it, which matters because
    /// the derived key is taken from a label encoded this way and a mark would change it. Throwing on
    /// invalid bytes means a frame that arrived corrupt fails rather than being decoded into replacement
    /// characters and acted on.
    /// </remarks>
    public sealed class DefaultEncodingTests
    {
        /// <summary>
        /// Verifies that nothing is put in front of what is encoded.
        /// </summary>
        [Fact]
        public void Value_EmitsNoByteOrderMark()
        {
            Assert.Empty(DefaultEncoding.Value.GetPreamble());
            Assert.Equal([0x61], DefaultEncoding.Value.GetBytes("a"));
        }

        /// <summary>
        /// Verifies that bytes which are not valid UTF-8 are refused rather than decoded into replacement
        /// characters.
        /// </summary>
        /// <remarks>
        /// <c>0xC3</c> begins a two-byte sequence and <c>0x28</c> cannot continue one. The framework's own
        /// UTF-8 encoding would hand back a replacement character for this and say nothing.
        /// </remarks>
        [Fact]
        public void Value_RefusesInvalidBytes()
        {
            _ = Assert.Throws<DecoderFallbackException>(static () => DefaultEncoding.Value.GetString([0xC3, 0x28]));
        }

        /// <summary>
        /// Verifies that text which cannot be encoded is refused rather than encoded into replacement
        /// characters.
        /// </summary>
        /// <remarks>
        /// A high surrogate with nothing following it is not a character, and is what a string cut in half
        /// part way through an emoji leaves behind.
        /// </remarks>
        [Fact]
        public void Value_RefusesUnpairedSurrogates()
        {
            _ = Assert.Throws<EncoderFallbackException>(static () => DefaultEncoding.Value.GetBytes("\uD83D"));
        }

        /// <summary>
        /// Verifies that text outside the ASCII range survives a round trip, since log messages and dialog
        /// text both go over the wire encoded this way.
        /// </summary>
        [Fact]
        public void Value_RoundTripsTextOutsideAscii()
        {
            // Arrange
            const string Original = "Ärger, 日本語, \U0001F600";

            // Act
            byte[] encoded = DefaultEncoding.Value.GetBytes(Original);

            // Assert
            Assert.Equal(Original, DefaultEncoding.Value.GetString(encoded));
        }
    }
}
