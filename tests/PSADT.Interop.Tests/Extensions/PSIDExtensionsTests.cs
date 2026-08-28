using System;
using System.Security.Principal;
using Windows.Win32.Security;
using Xunit;

namespace PSADT.Interop.Tests.Extensions
{
    /// <summary>
    /// Tests the conversion from a native security identifier pointer to the managed type. Every native
    /// call handing back an identifier goes through this, and the pointer it is given always refers to
    /// memory somebody else owns.
    /// </summary>
    public sealed class PSIDExtensionsTests
    {
        /// <summary>
        /// Verifies that an identifier round-trips through its binary form, which is the layout every
        /// native call hands back.
        /// </summary>
        [Fact]
        public void ToSecurityIdentifier_ReadsTheBinaryFormBackOut()
        {
            // Arrange
            using WindowsIdentity identity = WindowsIdentity.GetCurrent();
            Assert.NotNull(identity.User);
            byte[] binary = new byte[identity.User.BinaryLength];
            identity.User.GetBinaryForm(binary, 0);

            // Act & Assert
            unsafe
            {
                fixed (byte* block = binary)
                {
                    Assert.Equal(identity.User, ((PSID)block).ToSecurityIdentifier());
                }
            }
        }

        /// <summary>
        /// Verifies that a well-known identifier reads back as itself, so the conversion is checked against
        /// something whose value does not depend on the machine running the test.
        /// </summary>
        [Fact]
        public void ToSecurityIdentifier_ReadsAWellKnownIdentifier()
        {
            // Arrange
            SecurityIdentifier everyone = new(WellKnownSidType.WorldSid, domainSid: null);
            byte[] binary = new byte[everyone.BinaryLength];
            everyone.GetBinaryForm(binary, 0);

            // Act & Assert
            unsafe
            {
                fixed (byte* block = binary)
                {
                    Assert.Equal("S-1-1-0", ((PSID)block).ToSecurityIdentifier().Value, StringComparer.Ordinal);
                }
            }
        }
    }
}
