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
using System.Collections.Generic;
using PSADT.SMBIOS;

namespace PSADT.Tests.SMBIOS
{
    /// <summary>
    /// Contains unit tests for verifying the behavior of SMBIOS parsing methods, including structure offset detection,
    /// structure parsing, string extraction, and version information retrieval.
    /// </summary>
    /// <remarks>This class provides a suite of tests to ensure that the SMBIOS parsing API correctly handles
    /// various scenarios, such as invalid input buffers, missing or multiple structures, and string parsing edge cases.
    /// The tests validate both successful parsing and appropriate exception handling, helping to maintain the
    /// reliability and correctness of the SMBIOS parsing implementation.</remarks>
    public sealed class SmbiosParsingTests
    {
        /// <summary>
        /// Verifies that the GetStructureOffsets method throws an ArgumentException when the provided buffer is too
        /// short to contain a valid SMBIOS structure header.
        /// </summary>
        /// <remarks>This test ensures that GetStructureOffsets enforces input validation by rejecting
        /// buffers that do not meet the minimum required length for parsing SMBIOS structures.</remarks>
        [Fact]
        public void GetStructureOffsets_ThrowsWhenBufferTooShort()
        {
            _ = Assert.Throws<ArgumentException>(() => SmbiosParsing.GetStructureOffsets(new byte[7], SmbiosType.EndOfTable));
        }

        /// <summary>
        /// Verifies that the GetStructureOffsets method returns offsets that match the expected positions for SMBIOS
        /// structures of the specified type.
        /// </summary>
        /// <remarks>This test ensures that the method correctly identifies the offset and length of
        /// SMBIOS structures of type EndOfTable within a raw SMBIOS data buffer. It validates that only the expected
        /// structure is found and that its position information is accurate.</remarks>
        [Fact]
        public void GetStructureOffsets_ReturnsMatchingOffsets()
        {
            byte[] buffer = SmbiosTestDataBuilder.BuildRawSmbios(
                new SmbiosTestDataBuilder.SmbiosStructure(SmbiosType.Inactive, 0x1000, [0xAA], "A"),
                new SmbiosTestDataBuilder.SmbiosStructure(SmbiosType.EndOfTable, 0x2000, [], "Term", "")
            );
            IReadOnlyList<SmbiosTablePosition> positions = SmbiosParsing.GetStructureOffsets(buffer, SmbiosType.EndOfTable);
            _ = Assert.Single(positions);
            Assert.True(positions[0].Offset >= 8);
            Assert.Equal(4, positions[0].Length);
        }

        /// <summary>
        /// Verifies that the GetStructureOffsets method throws a SmbiosTypeNotFoundException when the specified SMBIOS
        /// structure type is not present in the buffer.
        /// </summary>
        /// <remarks>This test ensures that attempting to retrieve offsets for a missing SMBIOS structure
        /// type results in the expected exception, indicating correct error handling by the parser.</remarks>
        [Fact]
        public void GetStructureOffsets_ThrowsWhenTypeMissing()
        {
            byte[] buffer = SmbiosTestDataBuilder.BuildRawSmbios(
                new SmbiosTestDataBuilder.SmbiosStructure(SmbiosType.Inactive, 0x1000, [0xAA])
            );
            _ = Assert.Throws<SmbiosTypeNotFoundException>(() => SmbiosParsing.GetStructureOffsets(buffer, SmbiosType.EndOfTable));
        }

        /// <summary>
        /// Verifies that ParseStructure throws an InvalidOperationException when multiple structures of the specified
        /// type are found in the SMBIOS data.
        /// </summary>
        /// <remarks>This test ensures that the ParseStructure method enforces the expectation that only
        /// one structure of a given type should exist in the provided SMBIOS buffer. If more than one matching
        /// structure is present, the method is expected to throw an InvalidOperationException.</remarks>
        [Fact]
        public void ParseStructure_ThrowsWhenMultipleStructuresFound()
        {
            byte[] buffer = SmbiosTestDataBuilder.BuildRawSmbios(
                new SmbiosTestDataBuilder.SmbiosStructure(SmbiosType.EndOfTable, 0x1000, []),
                new SmbiosTestDataBuilder.SmbiosStructure(SmbiosType.EndOfTable, 0x1001, [])
            );
            _ = Assert.Throws<InvalidOperationException>(() => SmbiosParsing.ParseStructure(buffer, SmbiosType.EndOfTable, FakeStructureParser));
        }

        /// <summary>
        /// Verifies that the ParseAllStructures method parses each matching SMBIOS structure in the provided buffer.
        /// </summary>
        /// <remarks>This test ensures that all structures of the specified type are correctly identified
        /// and parsed from the input data. It checks that each resulting structure has the expected handle value,
        /// confirming correct parsing behavior.</remarks>
        [Fact]
        public void ParseAllStructures_ParsesEachMatch()
        {
            byte[] buffer = SmbiosTestDataBuilder.BuildRawSmbios(
                new SmbiosTestDataBuilder.SmbiosStructure(SmbiosType.EndOfTable, 0x1000, []),
                new SmbiosTestDataBuilder.SmbiosStructure(SmbiosType.EndOfTable, 0x1001, [])
            );
            IReadOnlyList<FakeStructure> result = SmbiosParsing.ParseAllStructures(buffer, SmbiosType.EndOfTable, FakeStructureParser);
            Assert.Collection(result, item => Assert.Equal(0x1000, item.Handle), item => Assert.Equal(0x1001, item.Handle));
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
                0x00
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
                0x00
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
        /// Verifies that GetSmbiosVersion throws an ArgumentException when provided with a buffer that is too short.
        /// </summary>
        /// <remarks>This test ensures that the SmbiosParsing.GetSmbiosVersion method enforces its input
        /// buffer length requirements by throwing an ArgumentException if the buffer does not meet the minimum expected
        /// size.</remarks>
        [Fact]
        public void GetSmbiosVersion_ThrowsWhenBufferTooShort()
        {
            _ = Assert.Throws<ArgumentException>(() => SmbiosParsing.GetSmbiosVersion(new byte[4]));
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
            return new FakeStructure(buffer[offset], length, handle);
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
