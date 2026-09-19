/*
 * Copyright (C) 2025 Devicie Pty Ltd. All rights reserved.
 *
 * This file is part of PSAppDeployToolkit.
 *
 * PSAppDeployToolkit is free software: you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation, either version 3
 * of the License, or (at your option) any later version.
 *
 * PSAppDeployToolkit is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
 *
 * See the GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with PSAppDeployToolkit. If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.IO;
using PSADT.SMBIOS;
using Xunit;

namespace PSADT.Tests.SMBIOS
{
    /// <summary>
    /// Contains unit tests for verifying the behavior of SMBIOS parsing methods, including structure reading, string
    /// extraction, and version information retrieval.
    /// </summary>
    /// <remarks>This class provides a suite of tests to ensure that the SMBIOS parsing API correctly handles
    /// various scenarios, such as invalid input buffers, missing structures, and string parsing edge cases. The tests
    /// validate both successful parsing and appropriate exception handling, helping to maintain the reliability and
    /// correctness of the SMBIOS parsing implementation.</remarks>
    public sealed class SmbiosParsingTests
    {
        /// <summary>
        /// Verifies that the ReadStructure method throws an ArgumentOutOfRangeException when the provided buffer is too short
        /// to contain a valid SMBIOS structure header.
        /// </summary>
        /// <remarks>This test ensures that ReadStructure enforces input validation by rejecting
        /// buffers that do not meet the minimum required length for parsing SMBIOS structures.</remarks>
        [Fact]
        public void ReadStructure_ThrowsWhenBufferTooShort()
        {
            _ = Assert.Throws<ArgumentOutOfRangeException>(static () => SmbiosParsing.ReadStructure(new byte[7], SmbiosType.EndOfTable, FakeStructureParser));
        }

        /// <summary>
        /// Verifies that the ReadStructure method returns the first SMBIOS structure matching the requested type.
        /// </summary>
        /// <remarks>This test ensures that the method parses the first matching structure directly and does not reject
        /// subsequent duplicate structures.</remarks>
        [Fact]
        public void ReadStructure_ReturnsFirstMatchingStructure()
        {
            byte[] buffer = SmbiosTestDataBuilder.BuildRawSmbios(
                new SmbiosTestDataBuilder.SmbiosStructure(SmbiosType.Inactive, 0x1000, [0xAA], "A"),
                new SmbiosTestDataBuilder.SmbiosStructure(SmbiosType.EndOfTable, 0x2000, []),
                new SmbiosTestDataBuilder.SmbiosStructure(SmbiosType.EndOfTable, 0x2001, [])
            );
            FakeStructure structure = SmbiosParsing.ReadStructure(buffer, SmbiosType.EndOfTable, FakeStructureParser);
            Assert.Equal(SmbiosType.EndOfTable, structure.Type);
            Assert.Equal((ushort)0x2000, structure.Handle);
            Assert.Equal(4, structure.Length);
        }

        /// <summary>
        /// Verifies that the ReadStructure method throws a SmbiosTypeNotFoundException when the specified SMBIOS structure
        /// type is not present in the buffer.
        /// </summary>
        /// <remarks>This test ensures that attempting to read a missing SMBIOS structure type results in the expected
        /// exception, indicating correct error handling by the parser.</remarks>
        [Fact]
        public void ReadStructure_ThrowsWhenTypeMissing()
        {
            byte[] buffer = SmbiosTestDataBuilder.BuildRawSmbios(
                new SmbiosTestDataBuilder.SmbiosStructure(SmbiosType.Inactive, 0x1000, [0xAA])
            );
            _ = Assert.Throws<SmbiosTypeNotFoundException>(() => SmbiosParsing.ReadStructure(buffer, SmbiosType.EndOfTable, FakeStructureParser));
        }

        /// <summary>
        /// Verifies that the GetSmbiosString method returns the expected string values for various string indices and
        /// handles missing or zero indices correctly.
        /// </summary>
        /// <remarks>This unit test checks that GetSmbiosString correctly parses SMBIOS string data,
        /// returning the appropriate string for valid indices and null for missing or zero indices.</remarks>
        [Fact]
        public void GetSmbiosString_ReturnsExpectedString()
        {
            byte[] data =
            [
                0x00, 0x00, 0x00, 0x00,
                (byte)'A', (byte)'B', 0x00,
                (byte)'C', 0x00,
                0x00,
            ];
            string? first = SmbiosParsing.GetSmbiosString(data, 4, 1);
            string? second = SmbiosParsing.GetSmbiosString(data, 4, 2);
            string? missing = SmbiosParsing.GetSmbiosString(data, 4, 3);
            string? zeroIndex = SmbiosParsing.GetSmbiosString(data, 4, 0);
            Assert.Equal("AB", first);
            Assert.Equal("C", second);
            Assert.Null(missing);
            Assert.Null(zeroIndex);
        }

        /// <summary>
        /// Verifies that a structure declaring a length the table cannot hold is refused.
        /// </summary>
        /// <remarks>The length is declared by the structure itself, so it is the firmware's to get wrong. One
        /// that runs past the end of the table leaves the parser slicing outside it, and one under four does not
        /// cover the header it was read from, so the walk does not advance past the structure and every offset
        /// after it is read from the middle of something. Neither can be worked with, and saying which structure
        /// was wrong beats the slice failing somewhere further in.</remarks>
        /// <param name="declaredLength">The length for the structure to declare.</param>
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(64)]
        [InlineData(byte.MaxValue)]
        public void ReadStructure_ThrowsWhenAStructureDeclaresALengthThatDoesNotFit(byte declaredLength)
        {
            // Arrange: a well formed header whose declared length is then replaced with the one under test
            byte[] data =
            [
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                (byte)SmbiosType.PlatformFirmwareInformation, declaredLength, 0x00, 0x00,
                (byte)'A', 0x00,
                0x00,
            ];

            // Assert
            _ = Assert.Throws<InvalidDataException>(() => SmbiosParsing.ReadStructure(data, SmbiosType.PlatformFirmwareInformation, FakeStructureParser));
        }

        /// <summary>
        /// Verifies that padding written after the end-of-table structure is not read as a structure.
        /// </summary>
        /// <remarks>Firmware is free to pad the table out past the structures it published, and those bytes are
        /// zeroes rather than a header. Reading one as a structure declares a length of zero, which the length check
        /// refuses, so a table that merely lacks the structure asked for would be reported as malformed instead.
        /// That answer travels a long way: the only caller builds its structures in a static constructor, so the
        /// type stays faulted for the life of the process and every environment table built after it fails.</remarks>
        [Fact]
        public void ReadStructure_DoesNotReadPaddingAfterTheEndOfTable()
        {
            // Arrange: a table the firmware padded with zeroes past its end-of-table structure.
            byte[] table = SmbiosTestDataBuilder.BuildRawSmbios(
                new SmbiosTestDataBuilder.SmbiosStructure(SmbiosType.Inactive, 0x1000, [0xAA]),
                new SmbiosTestDataBuilder.SmbiosStructure(SmbiosType.EndOfTable, 0xFFFE, [])
            );
            byte[] padded = new byte[table.Length + 16];
            table.CopyTo(padded, 0);

            // Assert: reported missing, which it is, rather than refused as a structure the padding never was.
            _ = Assert.Throws<SmbiosTypeNotFoundException>(() => SmbiosParsing.ReadStructure(padded, SmbiosType.SystemInformation, FakeStructureParser));
        }

        /// <summary>
        /// Verifies that the walk stops at the end-of-table structure rather than reading on past it.
        /// </summary>
        /// <remarks>The end-of-table structure is what the firmware uses to say it published nothing further, so
        /// bytes after it are not part of the table whether or not they read like a structure. Taking them as one
        /// is what makes padding look malformed.</remarks>
        [Fact]
        public void ReadStructure_StopsAtTheEndOfTable()
        {
            // Arrange: a well formed structure written after the terminator, which is past what the table declares.
            byte[] table = SmbiosTestDataBuilder.BuildRawSmbios(
                new SmbiosTestDataBuilder.SmbiosStructure(SmbiosType.EndOfTable, 0xFFFE, []),
                new SmbiosTestDataBuilder.SmbiosStructure(SmbiosType.SystemInformation, 0x1000, [0xAA])
            );

            // Assert
            _ = Assert.Throws<SmbiosTypeNotFoundException>(() => SmbiosParsing.ReadStructure(table, SmbiosType.SystemInformation, FakeStructureParser));
        }

        /// <summary>
        /// Verifies that an index past the end of a structure's own strings does not reach the next structure's.
        /// </summary>
        /// <remarks>A string set ends with an empty string, and the set belonging to the structure that follows
        /// begins immediately after it. Nothing separates the two but that terminator, so a search that does not
        /// stop at it walks straight on and answers with a string belonging to something else. An index past the
        /// end is how firmware says a structure has no such string, so this is reached by a well formed table and
        /// not only by a malformed one.</remarks>
        /// <param name="stringIndex">An index beyond the strings the first structure declares.</param>
        [Theory]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(byte.MaxValue)]
        public void GetSmbiosString_DoesNotReadIntoTheFollowingStructure(byte stringIndex)
        {
            // Arrange: one structure's strings, its terminator, then a structure whose own strings follow
            byte[] data =
            [
                0x00, 0x00, 0x00, 0x00,
                (byte)'A', (byte)'B', 0x00,
                (byte)'C', 0x00,
                0x00,
                0x01, 0x04, 0x00, 0x00,
                (byte)'N', (byte)'E', (byte)'X', (byte)'T', 0x00,
                0x00,
            ];

            // Assert
            Assert.Null(SmbiosParsing.GetSmbiosString(data, 4, stringIndex));
        }

        /// <summary>
        /// Verifies that GetSmbiosString returns null when the extracted string consists only of whitespace characters.
        /// </summary>
        /// <remarks>This test ensures that the SmbiosParsing.GetSmbiosString method treats strings
        /// containing only whitespace as null, which may be important for consumers expecting meaningful SMBIOS string
        /// values.</remarks>
        [Fact]
        public void GetSmbiosString_ReturnsNullForWhitespace()
        {
            byte[] data =
            [
                0x00, 0x00, 0x00, 0x00,
                (byte)' ', 0x00,
                0x00,
            ];
            string? value = SmbiosParsing.GetSmbiosString(data, 4, 1);
            Assert.Null(value);
        }

        /// <summary>
        /// Verifies that the GetSmbiosVersion method correctly parses SMBIOS version information from a raw buffer.
        /// </summary>
        /// <remarks>This test ensures that the SMBIOS version and entry point type are accurately
        /// extracted from the provided buffer. It validates that the version string and entry point type match the
        /// expected values for the given input.</remarks>
        [Fact]
        public void GetSmbiosVersion_ParsesVersionInformationFromBuffer()
        {
            byte[] buffer = SmbiosTestDataBuilder.BuildRawSmbios(3, 2, 1, []);
            SmbiosVersionInfo version = SmbiosParsing.GetSmbiosVersion(buffer);
            Assert.Equal("3.2", version.GetVersionString());
            Assert.Equal(SmbiosEntryPointType.Smbios3x, version.EntryPointType);
        }

        /// <summary>
        /// Verifies that GetSmbiosVersion throws an ArgumentOutOfRangeException when provided with a buffer that is too short.
        /// </summary>
        /// <remarks>This test ensures that the SmbiosParsing.GetSmbiosVersion method enforces its input
        /// buffer length requirements by throwing an ArgumentOutOfRangeException if the buffer does not meet the minimum expected
        /// size.</remarks>
        [Fact]
        public void GetSmbiosVersion_ThrowsWhenBufferTooShort()
        {
            _ = Assert.Throws<ArgumentOutOfRangeException>(static () => SmbiosParsing.GetSmbiosVersion(new byte[4]));
        }

        /// <summary>
        /// Parses a buffer of bytes starting at the specified offset to create a new instance of the FakeStructure
        /// type.
        /// </summary>
        /// <param name="buffer">The buffer containing the raw data to parse.</param>
        /// <param name="offset">The zero-based index in the buffer at which to begin parsing.</param>
        /// <param name="length">The length, in bytes, of the structure to parse.</param>
        /// <returns>A FakeStructure instance parsed from the specified buffer segment.</returns>
        private static FakeStructure FakeStructureParser(ReadOnlySpan<byte> buffer, int offset, byte length)
        {
            ushort handle = (ushort)(buffer[offset + 2] | (buffer[offset + 3] << 8));
            return new(buffer[offset], length, handle);
        }

        /// <summary>
        /// Represents a System Management BIOS (SMBIOS) structure with type, length, and handle information.
        /// </summary>
        /// <remarks>This class provides a simple implementation of the ISmbiosStructure interface for
        /// representing SMBIOS structures. It is intended for internal use and is not thread-safe.</remarks>
        private sealed class FakeStructure : ISmbiosStructure
        {
            /// <summary>
            /// Initializes a new instance of the FakeStructure class with the specified type, structure length, and
            /// handle.
            /// </summary>
            /// <param name="type">The type identifier for the structure.</param>
            /// <param name="structureLength">The length of the structure, in bytes.</param>
            /// <param name="handle">The handle associated with the structure.</param>
            internal FakeStructure(byte type, byte structureLength, ushort handle)
            {
                RawType = type;
                Length = structureLength;
                Handle = handle;
            }

            /// <summary>
            /// Gets the raw underlying type code represented as a byte value.
            /// </summary>
            internal byte RawType { get; }

            /// <summary>
            /// Gets the SMBIOS structure type represented by this instance.
            /// </summary>
            public SmbiosType Type => (SmbiosType)RawType;

            /// <summary>
            /// Gets the length, in bytes, of the data represented by this instance.
            /// </summary>
            public byte Length { get; }

            /// <summary>
            /// Gets the underlying handle value associated with this instance.
            /// </summary>
            public ushort Handle { get; }
        }
    }
}
