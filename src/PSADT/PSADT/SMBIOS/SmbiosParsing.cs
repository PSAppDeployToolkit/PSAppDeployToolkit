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
using System.Text;

namespace PSADT.SMBIOS
{
    /// <summary>
    /// Provides utility methods for parsing SMBIOS (System Management BIOS) data structures.
    /// </summary>
    /// <remarks>This class includes methods for extracting specific SMBIOS structures, retrieving all
    /// instances of a particular structure type, and handling SMBIOS string tables. It is designed to facilitate
    /// working with raw SMBIOS data buffers.</remarks>
    internal static class SmbiosParsing
    {
        /// <summary>
        /// Delegate for parsing SMBIOS structures.
        /// </summary>
        /// <typeparam name="T">The type of SMBIOS structure to parse.</typeparam>
        /// <param name="buffer">The SMBIOS buffer containing the structure data.</param>
        /// <param name="structureOffset">The offset to the start of the structure in the buffer.</param>
        /// <param name="structureLength">The length of the structure in bytes.</param>
        /// <returns>The parsed SMBIOS structure.</returns>
        internal delegate T SmbiosParser<T>(ReadOnlySpan<byte> buffer, int structureOffset, byte structureLength) where T : ISmbiosStructure;

        /// <summary>
        /// Generic method to read and parse any SMBIOS structure type.
        /// </summary>
        /// <typeparam name="T">The type of SMBIOS structure to return.</typeparam>
        /// <param name="targetType">The SMBIOS structure type to search for.</param>
        /// <param name="parser">Function to parse the structure from the buffer.</param>
        /// <param name="buffer">An optional read-only span of bytes containing the SMBIOS data buffer.</param>
        /// <returns>The parsed structure or null if not found.</returns>
        internal static T GetStructure<T>(SmbiosType targetType, SmbiosParser<T> parser, ReadOnlySpan<byte> buffer = default) where T : ISmbiosStructure
        {
            if (buffer.IsEmpty)
            {
                Span<byte> localbuf = stackalloc byte[SmbiosTables.GetRequiredLength()]; SmbiosTables.FillBuffer(localbuf);
                return ParseStructure(localbuf, targetType, parser);
            }
            return ParseStructure(buffer, targetType, parser);
        }

        /// <summary>
        /// Generic method to read and parse all instances of a specific SMBIOS structure type.
        /// </summary>
        /// <typeparam name="T">The type of SMBIOS structure to return.</typeparam>
        /// <param name="targetType">The SMBIOS structure type to search for.</param>
        /// <param name="parser">Function to parse the structure from the buffer.</param>
        /// <param name="buffer">An optional read-only span of bytes containing the SMBIOS data buffer.</param>
        /// <returns>A collection of all parsed structures of the specified type.</returns>
        internal static IReadOnlyList<T> GetAllStructures<T>(SmbiosType targetType, SmbiosParser<T> parser, ReadOnlySpan<byte> buffer = default) where T : ISmbiosStructure
        {
            if (buffer.IsEmpty)
            {
                Span<byte> localbuf = stackalloc byte[SmbiosTables.GetRequiredLength()]; SmbiosTables.FillBuffer(localbuf);
                return ParseAllStructures(localbuf, targetType, parser);
            }
            return ParseAllStructures(buffer, targetType, parser);
        }

        /// <summary>
        /// Retrieves a list of positions for SMBIOS structures of a specified type from the provided buffer.
        /// </summary>
        /// <remarks>This method scans the provided buffer for all instances of the specified SMBIOS
        /// structure type and returns their positions. It throws an exception if no structures of the specified type
        /// are found.</remarks>
        /// <param name="buffer">A read-only span of bytes representing the SMBIOS data to search through.</param>
        /// <param name="targetType">The specific SMBIOS structure type to locate within the buffer.</param>
        /// <returns>A read-only list of <see cref="SmbiosTablePosition"/> objects, each representing the position and length of
        /// a found structure.</returns>
        /// <exception cref="InvalidOperationException">Thrown if no SMBIOS structures of the specified <paramref name="targetType"/> are found in the buffer.</exception>
        internal static IReadOnlyList<SmbiosTablePosition> GetStructureOffsets(ReadOnlySpan<byte> buffer, SmbiosType targetType)
        {
            // Determine the bounds of the SMBIOS table inside RAW_SMBIOS_DATA
            if (buffer.Length < 8)
            {
                throw new ArgumentException($"The specified buffer is too short: {buffer.Length} bytes");
            }

            // Loop through the data and find all instances of the target structure.
            List<SmbiosTablePosition> offsets = []; int offset = 8;
            while (offset < buffer.Length - 4)
            {
                // Have we found an instance?
                byte length = buffer[offset + 1];
                if (buffer[offset] == (byte)targetType)
                {
                    offsets.Add(new(offset, length));
                }

                // Move to the next structure, skipping unformatted string fields. A double terminator indicates the end.
                offset += length; while (offset < buffer.Length - 1 && !(buffer[offset] == 0 && buffer[offset + 1] == 0))
                {
                    offset++;
                }
                offset += 2;
            }
            return offsets.Count != 0 ? (IReadOnlyList<SmbiosTablePosition>)offsets.AsReadOnly() : throw new SmbiosTypeNotFoundException(targetType);
        }

        /// <summary>
        /// Parses SMBIOS data to extract a specific structure type.
        /// </summary>
        /// <typeparam name="T">The type of structure to parse.</typeparam>
        /// <param name="buffer">The SMBIOS buffer to parse.</param>
        /// <param name="targetType">The SMBIOS structure type to search for.</param>
        /// <param name="parser">Function to parse the structure from the buffer.</param>
        /// <returns>The parsed structure or null if not found.</returns>
        internal static T ParseStructure<T>(ReadOnlySpan<byte> buffer, SmbiosType targetType, SmbiosParser<T> parser) where T : ISmbiosStructure
        {
            // Get all structures that match the target type.
            IReadOnlyList<SmbiosTablePosition> offsets = GetStructureOffsets(buffer, targetType);
            return offsets.Count > 1
                ? throw new InvalidOperationException($"Multiple SMBIOS structures of type [{targetType}] found.")
                : parser(buffer, offsets[0].Offset, offsets[0].Length);
        }

        /// <summary>
        /// Parses SMBIOS data to extract all instances of a specific structure type.
        /// </summary>
        /// <typeparam name="T">The type of structure to parse.</typeparam>
        /// <param name="buffer">The SMBIOS buffer to parse.</param>
        /// <param name="targetType">The SMBIOS structure type to search for.</param>
        /// <param name="parser">Function to parse the structure from the buffer.</param>
        /// <returns>A collection of all parsed structures of the specified type.</returns>
        internal static IReadOnlyList<T> ParseAllStructures<T>(ReadOnlySpan<byte> buffer, SmbiosType targetType, SmbiosParser<T> parser) where T : ISmbiosStructure
        {
            // Loop through the data and find all instances of the target structure.
            List<T> structures = []; foreach (SmbiosTablePosition position in GetStructureOffsets(buffer, targetType))
            {
                structures.Add(parser(buffer, position.Offset, position.Length));
            }
            return structures.AsReadOnly();
        }

        /// <summary>
        /// Extracts a string from the SMBIOS string section.
        /// </summary>
        internal static string? GetSmbiosString(ReadOnlySpan<byte> buffer, int stringTableOffset, byte stringIndex)
        {
            // SMBIOS string indices are 1-based; 0 means no string.
            if (stringIndex == 0)
            {
                return null;
            }

            // Iterate through strings to find the requested index.
            int currentIndex = 1; int offset = stringTableOffset;
            while (offset < buffer.Length && currentIndex <= stringIndex)
            {
                // Read until null terminator.
                List<byte> stringBytes = [];
                while (offset < buffer.Length && buffer[offset] != 0)
                {
                    stringBytes.Add(buffer[offset]);
                    offset++;
                }
                if (currentIndex == stringIndex)
                {
                    string result = Encoding.ASCII.GetString([.. stringBytes]);
                    return !string.IsNullOrWhiteSpace(result) ? result : null;
                }

                // Move past the null terminator. A double null indicates end of the table, not an empty string entry.
                offset++; currentIndex++;
                if ((offset >= buffer.Length) || (buffer[offset] == 0 && (offset + 1 >= buffer.Length || buffer[offset + 1] == 0)))
                {
                    break;
                }
            }
            return null;
        }

        /// <summary>
        /// Parses SMBIOS version information from the table header.
        /// </summary>
        /// <param name="buffer">The SMBIOS buffer to parse version information from.</param>
        /// <returns>SMBIOS version information if available; otherwise null.</returns>
        internal static SmbiosVersionInfo GetSmbiosVersion(ReadOnlySpan<byte> buffer = default)
        {
            if (buffer.IsEmpty)
            {
                Span<byte> localbuf = stackalloc byte[SmbiosTables.GetRequiredLength()];
                SmbiosTables.FillBuffer(localbuf);
                return ParseSmbiosVersion(localbuf);
            }
            return ParseSmbiosVersion(buffer);
        }

        /// <summary>
        /// Parses SMBIOS version information from the buffer header.
        /// </summary>
        /// <param name="buffer">The SMBIOS buffer containing version data.</param>
        /// <returns>SMBIOS version information if valid; otherwise throws exception.</returns>
        private static SmbiosVersionInfo ParseSmbiosVersion(ReadOnlySpan<byte> buffer)
        {
            /*
            struct RawSMBIOSData
            {
                BYTE    Used20CallingMethod;
                BYTE    SMBIOSMajorVersion;
                BYTE    SMBIOSMinorVersion;
                BYTE    DmiRevision;
                DWORD   Length;
                BYTE    SMBIOSTableData[];
            };
            */
            if (buffer.Length < 8)
            {
                throw new ArgumentException($"The specified buffer is too short: {buffer.Length} bytes");
            }
            byte major = buffer[1]; byte minor = buffer[2]; byte dmiRevision = buffer[3];
            SmbiosEntryPointType entryPointType = major >= 3 ? SmbiosEntryPointType.Smbios3x : SmbiosEntryPointType.Smbios2x;
            return new SmbiosVersionInfo(major, minor, dmiRevision, entryPointType);
        }
    }
}
