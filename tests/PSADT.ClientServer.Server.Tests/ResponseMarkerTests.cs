using System;
using System.Linq;
using PSADT.ClientServer.Server.Tests.TestHelpers;
using Xunit;

namespace PSADT.ClientServer.Server.Tests
{
    /// <summary>
    /// Tests the byte that tells a server whether the response it just read is a result or an exception.
    /// </summary>
    /// <remarks>
    /// It is the first byte of every response, and the server decides how to deserialise the rest on the
    /// strength of it. So its storage size is part of the protocol rather than an implementation detail:
    /// an enumeration stored as an <see cref="int"/> would be written as four bytes by anything that wrote
    /// it whole, and the reader is only ever going to look at one.
    /// </remarks>
    public sealed class ResponseMarkerTests
    {
        /// <summary>
        /// Verifies that the marker is stored as a single byte, which is the width the protocol gives it.
        /// </summary>
        [Fact]
        public void ResponseMarker_IsStoredAsAByte()
        {
            Assert.Equal(typeof(byte), Enum.GetUnderlyingType(typeof(ResponseMarker)));
        }

        /// <summary>
        /// Verifies that success is non-zero and failure is zero, which is the convention the documented
        /// protocol borrows from Win32 and which a reader testing the byte for truth would rely on.
        /// </summary>
        [Fact]
        public void ResponseMarker_FollowsWin32BooleanSemantics()
        {
            Assert.Equal(0, (byte)ResponseMarker.Error);
            Assert.NotEqual(0, (byte)ResponseMarker.Success);
        }

        /// <summary>
        /// Verifies that there are exactly two markers and that they differ, since the reader treats
        /// anything that is not success as an error and a third value would be silently read as one.
        /// </summary>
        [Fact]
        public void ResponseMarker_HasNoOtherValues()
        {
            ResponseMarker[] declared = EnumValues.Declared<ResponseMarker>();
            Assert.Equal(2, declared.Length);
            Assert.Equal(2, declared.Distinct().Count());
        }
    }
}
