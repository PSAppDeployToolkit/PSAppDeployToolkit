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
using System.Buffers.Binary;

namespace PSADT.SMBIOS
{
    /// <summary>
    /// Immutable representation of SMBIOS System Information (Type 1) structure.
    /// </summary>
    public sealed record SystemInformation : ISmbiosStructure
    {
        /// <summary>
        /// Reads the SMBIOS System Information (Type 1) structure.
        /// </summary>
        /// <param name="buffer">The buffer containing the SMBIOS data.</param>
        /// <returns>The system information.</returns>
        internal static SystemInformation Get(ReadOnlySpan<byte> buffer = default)
        {
            return SmbiosParsing.GetStructure(SmbiosType.SystemInformation, Parse, buffer);
        }

        /// <summary>
        /// Parses a System Information (Type 1) structure.
        /// </summary>
        /// <param name="buffer">The buffer containing the SMBIOS data.</param>
        /// <param name="structureOffset">The offset of the structure within the buffer.</param>
        /// <param name="structureLength">The length of the structure in bytes.</param>
        private static SystemInformation Parse(ReadOnlySpan<byte> buffer, int structureOffset, byte structureLength)
        {
            // UUID (SMBIOS 2.1+). 16 bytes at offset 0x08. "Not present" if all 00h or all FFh.
            ArgumentOutOfRangeException.ThrowIfLessThan(structureLength, 8);
            Guid? uuid = null;
            if (structureLength >= 24)
            {
                ReadOnlySpan<byte> uuidSpan = buffer.Slice(structureOffset + 8, 16);
                uuid = ParseSmbiosUuid(uuidSpan);
            }

            // Return the information to the caller.
            int stringTableOffset = structureOffset + structureLength;
            return new(
                structureLength,
                handle: BinaryPrimitives.ReadUInt16LittleEndian(buffer.Slice(structureOffset + 2, 2)),
                manufacturer: SmbiosParsing.GetSmbiosString(buffer, stringTableOffset, buffer[structureOffset + 4]),
                productName: SmbiosParsing.GetSmbiosString(buffer, stringTableOffset, buffer[structureOffset + 5]),
                version: SmbiosParsing.GetSmbiosString(buffer, stringTableOffset, buffer[structureOffset + 6]),
                serialNumber: SmbiosParsing.GetSmbiosString(buffer, stringTableOffset, buffer[structureOffset + 7]),
                uuid: uuid,
                wakeUpType: structureLength >= 25 ? (SystemWakeUpType)buffer[structureOffset + 24] : SystemWakeUpType.Unknown,
                skuNumber: structureLength >= 26 ? SmbiosParsing.GetSmbiosString(buffer, stringTableOffset, buffer[structureOffset + 25]) : null,
                family: structureLength >= 27 ? SmbiosParsing.GetSmbiosString(buffer, stringTableOffset, buffer[structureOffset + 26]) : null);
        }

        /// <summary>
        /// SMBIOS stores the UUID in mixed-endian form that matches Guid(byte[]) expectations
        /// (Data1, Data2, Data3 little-endian; Data4 as-is). No byte swapping is required.
        /// </summary>
        /// <param name="raw16">The raw 16-byte UUID from the SMBIOS structure.</param>
        /// <returns>The parsed <see cref="Guid"/> if present; otherwise, <see langword="null"/>.</returns>
        private static Guid? ParseSmbiosUuid(ReadOnlySpan<byte> raw16)
        {
            // Treat all-zeros or all-0xFF as "not present"
            bool allZero = true, allFF = true;
            for (int i = 0; i < 16; i++)
            {
                byte b = raw16[i];
                if (b != 0x00)
                {
                    allZero = false;
                }
                if (b != 0xFF)
                {
                    allFF = false;
                }
                if (!allZero && !allFF)
                {
                    break;
                }
            }

            // Construct Guid directly from raw bytes (layout already matches Guid(byte[])).
            return !allZero && !allFF ? new(raw16.ToArray()) : null;
        }

        /// <summary>
        /// Gets the SMBIOS structure type.
        /// </summary>
        public SmbiosType Type => SmbiosType.SystemInformation;

        /// <summary>
        /// Gets the length of the structure in bytes.
        /// </summary>
        public byte Length { get; }

        /// <summary>
        /// Gets the handle (unique identifier) for this structure.
        /// </summary>
        public ushort Handle { get; }

        /// <summary>
        /// Gets the system manufacturer name.
        /// </summary>
        public string? Manufacturer { get; }

        /// <summary>
        /// Gets the system product name.
        /// </summary>
        public string? ProductName { get; }

        /// <summary>
        /// Gets the system version.
        /// </summary>
        public string? Version { get; }

        /// <summary>
        /// Gets the system serial number.
        /// </summary>
        public string? SerialNumber { get; }

        /// <summary>
        /// Gets the system UUID (null if not present).
        /// </summary>
        public Guid? Uuid { get; }

        /// <summary>
        /// Gets the system wake-up type.
        /// </summary>
        public SystemWakeUpType WakeUpType { get; }

        /// <summary>
        /// Gets the system SKU number.
        /// </summary>
        public string? SkuNumber { get; }

        /// <summary>
        /// Gets the system family.
        /// </summary>
        public string? Family { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemInformation"/> class.
        /// </summary>
        /// <param name="structureLength">The length of the structure in bytes.</param>
        /// <param name="handle">The handle (unique identifier) for this structure.</param>
        /// <param name="manufacturer">The system manufacturer name.</param>
        /// <param name="productName">The system product name.</param>
        /// <param name="version">The system version.</param>
        /// <param name="serialNumber">The system serial number.</param>
        /// <param name="uuid">The system UUID (null if not present).</param>
        /// <param name="wakeUpType">The system wake-up type.</param>
        /// <param name="skuNumber">The system SKU number.</param>
        /// <param name="family">The system family.</param>
        private SystemInformation(
            byte structureLength,
            ushort handle,
            string? manufacturer,
            string? productName,
            string? version,
            string? serialNumber,
            Guid? uuid,
            SystemWakeUpType wakeUpType,
            string? skuNumber,
            string? family)
        {
            Length = structureLength;
            Handle = handle;
            Manufacturer = !string.IsNullOrWhiteSpace(manufacturer) ? manufacturer : null;
            ProductName = !string.IsNullOrWhiteSpace(productName) ? productName : null;
            Version = !string.IsNullOrWhiteSpace(version) ? version : null;
            SerialNumber = !string.IsNullOrWhiteSpace(serialNumber) ? serialNumber : null;
            Uuid = uuid;
            WakeUpType = wakeUpType;
            SkuNumber = !string.IsNullOrWhiteSpace(skuNumber) ? skuNumber : null;
            Family = !string.IsNullOrWhiteSpace(family) ? family : null;
        }

        /// <summary>
        /// Returns a string representation of the system information.
        /// </summary>
        public override string ToString()
        {
            return $"{Manufacturer} {ProductName} ({SerialNumber})";
        }
    }
}
