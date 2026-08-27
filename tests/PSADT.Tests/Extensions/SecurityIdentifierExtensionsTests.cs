using System.Security.Principal;
using Xunit;

namespace PSADT.Tests.Extensions
{
    /// <summary>
    /// Tests the binary form conversion used when a security identifier has to be handed to a native API.
    /// </summary>
    /// <remarks>
    /// <c>FileSystemUtilities.GetEffectiveAccess</c> pins the result of this and passes the pointer to
    /// <c>AuthzInitializeContextFromSid</c>, so a buffer that is the wrong length or filled from the
    /// wrong offset produces an access check against the wrong account rather than an error. The
    /// round-trip through the framework's own parser is the oracle.
    /// </remarks>
    public sealed class SecurityIdentifierExtensionsTests
    {
        /// <summary>
        /// Verifies that the buffer is exactly the length the identifier reports, since a native caller
        /// sizes its read from that same property.
        /// </summary>
        /// <param name="sidString">The identifier to convert.</param>
        [Theory]
        [InlineData("S-1-1-0")]
        [InlineData("S-1-5-18")]
        [InlineData("S-1-5-32-544")]
        [InlineData("S-1-5-21-1004336348-1177238915-682003330-512")]
        public void GetBinaryForm_IsExactlyTheReportedLength(string sidString)
        {
            // Arrange
            SecurityIdentifier sid = new(sidString);

            // Act
            byte[] binaryForm = sid.GetBinaryForm();

            // Assert
            Assert.Equal(sid.BinaryLength, binaryForm.Length);
        }

        /// <summary>
        /// Verifies that the buffer parses back to the identifier it came from, which covers the revision
        /// byte, the subauthority count and the byte order of each subauthority in one assertion.
        /// </summary>
        /// <param name="sidString">The identifier to convert and read back.</param>
        [Theory]
        [InlineData("S-1-1-0")]
        [InlineData("S-1-5-18")]
        [InlineData("S-1-5-19")]
        [InlineData("S-1-5-20")]
        [InlineData("S-1-5-32-544")]
        [InlineData("S-1-5-21-1004336348-1177238915-682003330-512")]
        [InlineData("S-1-16-12288")]
        public void GetBinaryForm_RoundTripsThroughTheFrameworkParser(string sidString)
        {
            // Arrange
            SecurityIdentifier sid = new(sidString);

            // Act
            SecurityIdentifier parsed = new(sid.GetBinaryForm(), 0);

            // Assert
            Assert.Equal(sid, parsed);
            Assert.Equal(sidString, parsed.Value);
        }

        /// <summary>
        /// Verifies that the conversion agrees with the framework's own writer, so nothing is lost by
        /// going through the extension instead.
        /// </summary>
        [Fact]
        public void GetBinaryForm_AgreesWithTheFrameworkWriter()
        {
            // Arrange
            SecurityIdentifier sid = new(WellKnownSidType.LocalSystemSid, domainSid: null);
            byte[] expected = new byte[sid.BinaryLength];
            sid.GetBinaryForm(expected, 0);

            // Act & Assert
            Assert.Equal(expected, sid.GetBinaryForm());
        }

        /// <summary>
        /// Verifies that the current user's identifier converts, which is the identifier the effective
        /// access checks are actually run against.
        /// </summary>
        [Fact]
        public void GetBinaryForm_HandlesTheCurrentUser()
        {
            // Arrange
            using WindowsIdentity identity = WindowsIdentity.GetCurrent();
            SecurityIdentifier? sid = identity.User;
            Assert.NotNull(sid);

            // Act
            byte[] binaryForm = sid.GetBinaryForm();

            // Assert
            Assert.Equal(sid.BinaryLength, binaryForm.Length);
            Assert.Equal(sid, new SecurityIdentifier(binaryForm, 0));
        }
    }
}
