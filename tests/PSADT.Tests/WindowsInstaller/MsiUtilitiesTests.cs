using System;
using System.ComponentModel;
using PSADT.WindowsInstaller;
using Xunit;

namespace PSADT.Tests.WindowsInstaller
{
    /// <summary>
    /// Tests the parts of the Windows Installer helpers that need no database to exercise.
    /// </summary>
    /// <remarks>
    /// The packed GUID form is the interesting one. Windows Installer stores product, upgrade and
    /// component codes in a 32-character form that is not simply the GUID with its braces removed: the
    /// first three fields are byte-reversed and written nibble-swapped, and the last eight bytes are
    /// written as swapped character pairs. Getting any of those three transformations wrong still
    /// produces a plausible-looking 32-character string, so the fixed vectors below are computed by hand
    /// from the documented layout rather than from the implementation.
    /// </remarks>
    public sealed class MsiUtilitiesTests
    {
        /// <summary>
        /// A GUID whose every byte is distinct, so a transposition anywhere in the packing is visible.
        /// </summary>
        /// <remarks>
        /// Built from its fields rather than parsed from text, because the field boundaries are exactly
        /// what the packed form rearranges: this is 01020304-0506-0708-090A-0B0C0D0E0F10.
        /// </remarks>
        private static readonly Guid SequentialGuid = new(0x01020304, 0x0506, 0x0708, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10);

        /// <summary>
        /// The packed form of <see cref="SequentialGuid"/>.
        /// </summary>
        /// <remarks>
        /// Derived by hand. The in-memory byte order of the GUID is 04 03 02 01, 06 05, 08 07, then
        /// 09 0A 0B 0C 0D 0E 0F 10, and each byte is written low nibble first: 04 becomes "40", 03
        /// becomes "30", and so on, with the final 10 becoming "01".
        /// </remarks>
        private const string SequentialGuidPacked = "403020106050807090A0B0C0D0E0F001";

        /// <summary>
        /// Verifies that a GUID packs to the documented 32-character form.
        /// </summary>
        [Fact]
        public void CompressGuid_ProducesTheDocumentedPackedForm()
        {
            Assert.Equal(SequentialGuidPacked, MsiUtilities.CompressGuid(SequentialGuid));
        }

        /// <summary>
        /// Verifies that the empty GUID packs to all zeroes, which is the degenerate case a caller is
        /// most likely to hit by accident.
        /// </summary>
        [Fact]
        public void CompressGuid_PacksTheEmptyGuidToZeroes()
        {
            Assert.Equal(new string('0', 32), MsiUtilities.CompressGuid(Guid.Empty));
        }

        /// <summary>
        /// Verifies that the packed form is always 32 upper-case hexadecimal characters, whatever the
        /// input, since Windows Installer will not accept anything else.
        /// </summary>
        /// <param name="guidString">The GUID to pack.</param>
        [Theory]
        [InlineData("00000000-0000-0000-0000-000000000000")]
        [InlineData("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF")]
        [InlineData("01020304-0506-0708-090A-0B0C0D0E0F10")]
        [InlineData("DEADBEEF-1234-5678-9ABC-DEF012345678")]
        [InlineData("2E5E1E4F-0A9C-4E1F-8B7A-6C5D4E3F2A1B")]
        public void CompressGuid_AlwaysProducesUpperCaseHexadecimal(string guidString)
        {
            // Act
            string packed = MsiUtilities.CompressGuid(new Guid(guidString));

            // Assert
            Assert.Equal(32, packed.Length);
            Assert.All(packed, static c => Assert.True(c is (>= '0' and <= '9') or (>= 'A' and <= 'F'), $"'{c}' is not upper-case hexadecimal."));
        }

        /// <summary>
        /// Verifies that the packed form unpacks to the GUID it was built from, which is the property
        /// every caller reading a product code out of a database depends on.
        /// </summary>
        [Fact]
        public void DecompressPackedGuid_ReversesTheDocumentedPackedForm()
        {
            Assert.Equal(SequentialGuid, MsiUtilities.DecompressPackedGuid(SequentialGuidPacked));
        }

        /// <summary>
        /// Verifies that packing and unpacking are inverse, across GUIDs chosen to exercise each field
        /// of the layout independently.
        /// </summary>
        /// <param name="guidString">The GUID to send through both directions.</param>
        [Theory]
        [InlineData("00000000-0000-0000-0000-000000000000")]
        [InlineData("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF")]
        [InlineData("01020304-0506-0708-090A-0B0C0D0E0F10")]
        [InlineData("DEADBEEF-1234-5678-9ABC-DEF012345678")]
        [InlineData("2E5E1E4F-0A9C-4E1F-8B7A-6C5D4E3F2A1B")]
        // One field non-zero at a time, so a field written into the wrong slot cannot cancel out.
        [InlineData("FFFFFFFF-0000-0000-0000-000000000000")]
        [InlineData("00000000-FFFF-0000-0000-000000000000")]
        [InlineData("00000000-0000-FFFF-0000-000000000000")]
        [InlineData("00000000-0000-0000-FFFF-000000000000")]
        [InlineData("00000000-0000-0000-0000-FFFFFFFFFFFF")]
        public void CompressGuid_RoundTripsThroughDecompressPackedGuid(string guidString)
        {
            // Arrange
            Guid original = new(guidString);

            // Act & Assert
            Assert.Equal(original, MsiUtilities.DecompressPackedGuid(MsiUtilities.CompressGuid(original)));
        }

        /// <summary>
        /// Verifies that lower-case hexadecimal is accepted when unpacking, since a database or a
        /// registry key written by another tool need not match this implementation's casing.
        /// </summary>
        [Fact]
        public void DecompressPackedGuid_AcceptsLowerCaseHexadecimal()
        {
            Assert.Equal(
                MsiUtilities.DecompressPackedGuid(SequentialGuidPacked),
                MsiUtilities.DecompressPackedGuid(SequentialGuidPacked.ToLowerInvariant()));
        }

        /// <summary>
        /// Verifies that anything other than exactly 32 characters is rejected, rather than read past
        /// the end of or short of the buffer.
        /// </summary>
        /// <param name="packed">The malformed input to reject.</param>
        [Theory]
        [InlineData("")]
        [InlineData("0")]
        [InlineData("4030201060508070")]
        [InlineData("403020106050807090A0B0C0D0E0F00")]
        [InlineData("403020106050807090A0B0C0D0E0F0011")]
        public void DecompressPackedGuid_RejectsAnythingButThirtyTwoCharacters(string packed)
        {
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => MsiUtilities.DecompressPackedGuid(packed));
        }

        /// <summary>
        /// Verifies that a null input is rejected as an out-of-range length rather than dereferenced,
        /// because the span overload treats a null string as empty.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0191:Do not use the null-forgiving operator", Justification = "This is deliberate as part of unit testing.")]
        [Fact]
        public void DecompressPackedGuid_RejectsNullAsAnEmptyInput()
        {
            _ = Assert.Throws<ArgumentOutOfRangeException>(static () => MsiUtilities.DecompressPackedGuid(null!));
        }

        /// <summary>
        /// Verifies that a non-hexadecimal character anywhere in the input is rejected, including at the
        /// first and last positions where an off-by-one in the validation loop would miss it.
        /// </summary>
        /// <param name="packed">The malformed input to reject.</param>
        [Theory]
        [InlineData("G03020106050807090A0B0C0D0E0F001")]
        [InlineData("403020106050807090A0B0C0D0E0F00G")]
        [InlineData("40302010605080709 A0B0C0D0E0F001")]
        [InlineData("40302010605080709-A0B0C0D0E0F001")]
        public void DecompressPackedGuid_RejectsNonHexadecimalCharacters(string packed)
        {
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => MsiUtilities.DecompressPackedGuid(packed));
        }

        /// <summary>
        /// Verifies the version layout Windows Installer packs into a single word: major in the high
        /// byte, minor in the next, and build in the low half.
        /// </summary>
        /// <param name="packed">The packed version value.</param>
        /// <param name="major">The expected major version.</param>
        /// <param name="minor">The expected minor version.</param>
        /// <param name="build">The expected build number.</param>
        [Theory]
        [InlineData(0, 0, 0, 0)]
        [InlineData(0x0102_0304, 1, 2, 772)]
        [InlineData(0x0A14_0BB8, 10, 20, 3000)]
        [InlineData(0x0100_0000, 1, 0, 0)]
        [InlineData(0x0001_0000, 0, 1, 0)]
        [InlineData(0x0000_FFFF, 0, 0, 65_535)]
        [InlineData(0x7F7F_FFFF, 127, 127, 65_535)]
        public void ParseVersionDWord_SplitsTheMajorMinorAndBuild(int packed, int major, int minor, int build)
        {
            // Act
            Version version = MsiUtilities.ParseVersionDWord(packed);

            // Assert
            Assert.Equal(major, version.Major);
            Assert.Equal(minor, version.Minor);
            Assert.Equal(build, version.Build);
        }

        /// <summary>
        /// Verifies that a value with the top bit set is read as an unsigned byte rather than sign
        /// extended, since the field is a version number and cannot be negative.
        /// </summary>
        [Fact]
        public void ParseVersionDWord_TreatsTheHighByteAsUnsigned()
        {
            // Act
            Version version = MsiUtilities.ParseVersionDWord(unchecked((int)0xFFFF_FFFF));

            // Assert
            Assert.Equal(new Version(255, 255, 65_535), version);
        }

        /// <summary>
        /// Verifies that an installer exit code becomes an exception carrying that code, which is what
        /// lets a caller rethrow it without losing the original result.
        /// </summary>
        /// <param name="exitCode">The exit code to translate.</param>
        [Theory]
        [InlineData(0)]
        [InlineData(1603)]
        [InlineData(1618)]
        [InlineData(1641)]
        [InlineData(3010)]
        public void GetExceptionForMsiExitCode_CarriesTheExitCode(int exitCode)
        {
            // Act
            Win32Exception exception = MsiUtilities.GetExceptionForMsiExitCode(exitCode);

            // Assert
            Assert.Equal(exitCode, exception.NativeErrorCode);
        }

        /// <summary>
        /// Verifies the shape of the message, which appends the symbolic name in brackets to the
        /// system's description and must not double up the sentence's full stop.
        /// </summary>
        /// <param name="exitCode">The exit code to translate.</param>
        [Theory]
        [InlineData(0)]
        [InlineData(1603)]
        [InlineData(1618)]
        [InlineData(3010)]
        public void GetExceptionForMsiExitCode_AppendsTheSymbolicNameOnce(int exitCode)
        {
            // Act
            string message = MsiUtilities.GetExceptionForMsiExitCode(exitCode).Message;

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(message));
            Assert.EndsWith(").", message, StringComparison.Ordinal);
            Assert.DoesNotContain("..", message, StringComparison.Ordinal);
            Assert.Equal(1, message.Split('(').Length - 1);
        }
    }
}
