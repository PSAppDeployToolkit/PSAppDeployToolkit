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
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => SmbiosParsing.ReadStructure(new byte[7], SmbiosType.EndOfTable, FakeStructureParser));
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
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => SmbiosParsing.GetSmbiosVersion(new byte[4]));
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
