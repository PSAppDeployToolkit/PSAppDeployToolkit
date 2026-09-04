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
                return ReadStructure(localbuf, targetType, parser);
            }
            return ReadStructure(buffer, targetType, parser);
        }

        /// <summary>
        /// Reads a specific SMBIOS structure from the provided buffer.
        /// </summary>
        /// <typeparam name="T">The type of SMBIOS structure to read.</typeparam>
        /// <param name="buffer">The SMBIOS buffer containing the structure data.</param>
        /// <param name="targetType">The SMBIOS structure type to search for.</param>
        /// <param name="parser">Function to parse the structure from the buffer.</param>
        /// <returns>The parsed SMBIOS structure.</returns>
        /// <exception cref="SmbiosTypeNotFoundException">Thrown if the specified SMBIOS structure type is not found in the buffer.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S3236:Caller information arguments should not be provided explicitly", Justification = "This is intentional as we're testing a parameter member.")]
        internal static T ReadStructure<T>(ReadOnlySpan<byte> buffer, SmbiosType targetType, SmbiosParser<T> parser) where T : ISmbiosStructure
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(buffer.Length, 8, nameof(buffer));
            int offset = 8; while (offset < buffer.Length - 4)
            {
                // Have we found an instance?
                byte length = buffer[offset + 1];
                if (buffer[offset] == (byte)targetType)
                {
                    return parser(buffer, offset, length);
                }

                // Move to the next structure, skipping unformatted string fields. A double terminator indicates the end.
                offset += length; while (offset < buffer.Length - 1 && !(buffer[offset] is 0 && buffer[offset + 1] is 0))
                {
                    offset++;
                }
                offset += 2;
            }
            throw new SmbiosTypeNotFoundException(targetType);
        }

        /// <summary>
        /// Extracts a string from the SMBIOS string section.
        /// </summary>
        /// <param name="buffer">The SMBIOS buffer containing the string data.</param>
        /// <param name="stringTableOffset">The offset to the string table within the buffer.</param>
        /// <param name="stringIndex">The 1-based index of the string to retrieve.</param>
        /// <returns>The requested string, or null if the index is 0 or the string is not found.</returns>
        internal static string? GetSmbiosString(ReadOnlySpan<byte> buffer, int stringTableOffset, byte stringIndex)
        {
            // SMBIOS string indices are 1-based; 0 means no string.
            if (stringIndex is 0)
            {
                return null;
            }

            // Iterate through strings to find the requested index.
            int currentIndex = 1; int offset = stringTableOffset;
            while (offset < buffer.Length && currentIndex <= stringIndex)
            {
                // Read until null terminator.
                List<byte> stringBytes = [];
                while (offset < buffer.Length && buffer[offset] is not 0)
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
                if ((offset >= buffer.Length) || (buffer[offset] is 0 && (offset + 1 >= buffer.Length || buffer[offset + 1] is 0)))
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S125:Sections of code should not be commented out", Justification = "This is an example struct that I'd like to leave here.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S3236:Caller information arguments should not be provided explicitly", Justification = "This is intentional as we're testing a parameter member.")]
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
            ArgumentOutOfRangeException.ThrowIfLessThan(buffer.Length, 8, nameof(buffer));
            byte major = buffer[1]; byte minor = buffer[2]; byte dmiRevision = buffer[3];
            SmbiosEntryPointType entryPointType = major >= 3 ? SmbiosEntryPointType.Smbios3x : SmbiosEntryPointType.Smbios2x;
            return new(major, minor, dmiRevision, entryPointType);
        }
    }
}
