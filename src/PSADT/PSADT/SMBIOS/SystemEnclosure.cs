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
using System.Collections.Generic;

namespace PSADT.SMBIOS
{
    /// <summary>
    /// Immutable representation of SMBIOS System Enclosure (Type 3) structure.
    /// </summary>
    public sealed record SystemEnclosure : ISmbiosStructure
    {
        /// <summary>
        /// Reads the SMBIOS System Enclosure (Type 3) structure.
        /// </summary>
        /// <returns>The system enclosure information.</returns>
        internal static SystemEnclosure Get(ReadOnlySpan<byte> buffer = default)
        {
            return SmbiosParsing.GetStructure(SmbiosType.SystemEnclosure, Parse, buffer);
        }

        /// <summary>
        /// Parses a System Enclosure (Type 3) structure.
        /// </summary>
        private static SystemEnclosure Parse(ReadOnlySpan<byte> buffer, int structureOffset, byte structureLength)
        {
            // Qualify the structure length before proceeding.
            if (structureLength < 9)
            {
                throw new ArgumentException($"System Enclosure structure too short: {structureLength} bytes");
            }

            // Height (0 => unspecified; 0xFF => unknown per spec)
            byte? height = null;
            if (structureLength >= 18)
            {
                byte heightRaw = buffer[structureOffset + 17];
                if (heightRaw is not 0 and not 0xFF)
                {
                    height = heightRaw;
                }
            }

            // Number of power cords (0 => unspecified)
            byte? numberOfPowerCords = null;
            if (structureLength >= 19)
            {
                byte cordsRaw = buffer[structureOffset + 18];
                if (cordsRaw != 0)
                {
                    numberOfPowerCords = cordsRaw;
                }
            }

            // Contained Elements metadata
            byte containedElementCount = 0;
            byte containedElementRecordLength = 0;
            if (structureLength >= 21)
            {
                containedElementCount = buffer[structureOffset + 0x13];
                containedElementRecordLength = buffer[structureOffset + 0x14];
            }

            // Contained element records
            byte[][] containedElementRecords = [];
            List<SystemEnclosureContainedElement> containedElements = [];
            int recordsStart = structureOffset + 0x15;
            if (structureLength > 0x15 && containedElementCount > 0 && containedElementRecordLength > 0)
            {
                int bytesAvailable = structureLength - 0x15;
                int maxRecords = bytesAvailable / containedElementRecordLength;
                int recordsToRead = Math.Min(containedElementCount, (byte)maxRecords);
                byte[][] recs = new byte[recordsToRead][];
                for (int i = 0; i < recordsToRead; i++)
                {
                    ReadOnlySpan<byte> recSpan = buffer.Slice(recordsStart + (i * containedElementRecordLength), containedElementRecordLength);
                    recs[i] = recSpan.ToArray();
                    if (containedElementRecordLength >= 3)
                    {
                        containedElements.Add(new SystemEnclosureContainedElement(recSpan[0], recSpan[1], recSpan[2]));
                    }
                }
                containedElementRecords = recs;
            }

            // After contained records, SKU string index (if space allows).
            int afterRecordsOffset = 0x15;
            if (containedElementCount > 0 && containedElementRecordLength > 0)
            {
                int bytesForRecords = Math.Min(containedElementCount * containedElementRecordLength, Math.Max(0, structureLength - 0x15));
                afterRecordsOffset += bytesForRecords;
            }

            // SKU Number string index
            string? skuNumber = null;
            int stringTableOffset = structureOffset + structureLength;
            bool canReadSku = !(containedElementCount > 0 && containedElementRecordLength == 0);
            if (canReadSku && structureLength > afterRecordsOffset)
            {
                skuNumber = SmbiosParsing.GetSmbiosString(buffer, stringTableOffset, buffer[structureOffset + afterRecordsOffset]);
            }

            // Rack Type and Rack Height (optional, immediately after SKU index)
            RackType? rackType = null;
            byte? rackHeight = null;
            int rackTypeOffset = afterRecordsOffset + 1;
            int rackHeightOffset = afterRecordsOffset + 2;
            if (structureLength > rackTypeOffset)
            {
                rackType = (RackType)buffer[structureOffset + rackTypeOffset];
            }
            if (structureLength > rackHeightOffset)
            {
                rackHeight = buffer[structureOffset + rackHeightOffset];
            }

            // Return the information to the caller.
            return new SystemEnclosure(
                structureLength,
                handle: BinaryPrimitives.ReadUInt16LittleEndian(buffer.Slice(structureOffset + 2, 2)),
                manufacturer: SmbiosParsing.GetSmbiosString(buffer, stringTableOffset, buffer[structureOffset + 4]),
                typeAndLock: new(buffer[structureOffset + 5]),
                version: SmbiosParsing.GetSmbiosString(buffer, stringTableOffset, buffer[structureOffset + 6]),
                serialNumber: SmbiosParsing.GetSmbiosString(buffer, stringTableOffset, buffer[structureOffset + 7]),
                assetTag: SmbiosParsing.GetSmbiosString(buffer, stringTableOffset, buffer[structureOffset + 8]),
                bootUpState: structureLength >= 10 ? (ChassisState)buffer[structureOffset + 9] : null,
                powerSupplyState: structureLength >= 11 ? (ChassisState)buffer[structureOffset + 10] : null,
                thermalState: structureLength >= 12 ? (ChassisState)buffer[structureOffset + 11] : null,
                securityStatus: structureLength >= 13 ? (ChassisSecurityStatus)buffer[structureOffset + 12] : null,
                oemDefined: structureLength >= 17 ? BinaryPrimitives.ReadUInt32LittleEndian(buffer.Slice(structureOffset + 13, 4)) : null,
                height: height,
                numberOfPowerCords: numberOfPowerCords,
                containedElementCount: containedElementCount,
                containedElementRecordLength: containedElementRecordLength,
                containedElementRecords: containedElementRecords,
                containedElements: containedElements,
                skuNumber: skuNumber,
                rackType: rackType,
                rackHeight: rackHeight);
        }

        /// <summary>
        /// Gets the SMBIOS structure type.
        /// </summary>
        public SmbiosType Type => SmbiosType.SystemEnclosure;

        /// <summary>
        /// Gets the length of the structure in bytes.
        /// </summary>
        public byte Length { get; }

        /// <summary>
        /// Gets the handle (unique identifier) for this structure.
        /// </summary>
        public ushort Handle { get; }

        /// <summary>
        /// Gets the enclosure manufacturer name.
        /// </summary>
        public string? Manufacturer { get; }

        /// <summary>
        /// Type/Lock byte decoded view (offset0x05).
        /// </summary>
        public SystemEnclosureTypeAndLock TypeAndLock { get; }

        /// <summary>
        /// Gets the enclosure version.
        /// </summary>
        public string? Version { get; }

        /// <summary>
        /// Gets the enclosure serial number.
        /// </summary>
        public string? SerialNumber { get; }

        /// <summary>
        /// Gets the enclosure asset tag.
        /// </summary>
        public string? AssetTag { get; }

        /// <summary>
        /// Gets the boot-up state.
        /// </summary>
        public ChassisState? BootUpState { get; }

        /// <summary>
        /// Gets the power supply state.
        /// </summary>
        public ChassisState? PowerSupplyState { get; }

        /// <summary>
        /// Gets the thermal state.
        /// </summary>
        public ChassisState? ThermalState { get; }

        /// <summary>
        /// Gets the chassis security status.
        /// </summary>
        public ChassisSecurityStatus? SecurityStatus { get; }

        /// <summary>
        /// Gets the OEM-defined information.
        /// </summary>
        public uint? OemDefined { get; }

        /// <summary>
        /// Height in rack units. Null if unspecified (0) or specified in extended field (0xFF).
        /// </summary>
        public byte? Height { get; }

        /// <summary>
        /// Number of power cords. Null if unspecified (0).
        /// </summary>
        public byte? NumberOfPowerCords { get; }

        /// <summary>
        /// Gets the number of contained element records.
        /// </summary>
        public byte ContainedElementCount { get; }

        /// <summary>
        /// Gets the contained element record length in bytes.
        /// /// </summary>
        public byte ContainedElementRecordLength { get; }

        /// <summary>
        /// Gets the contained element records (raw bytes per record, length equals ContainedElementRecordLength).
        /// </summary>
        public IReadOnlyList<byte[]> ContainedElementRecords { get; }

        /// <summary>
        /// Gets the contained elements (typed records when record length >= 3).
        /// </summary>
        public IReadOnlyList<SystemEnclosureContainedElement> ContainedElements { get; }

        /// <summary>
        /// Gets the SKU number.
        /// </summary>
        public string? SkuNumber { get; }

        /// <summary>
        /// Gets the rack type.
        /// </summary>
        public RackType? RackType { get; }

        /// <summary>
        /// Gets the rack height.
        /// </summary>
        public byte? RackHeight { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemEnclosure"/> class.
        /// </summary>
        private SystemEnclosure(
            byte structureLength,
            ushort handle,
            string? manufacturer,
            SystemEnclosureTypeAndLock typeAndLock,
            string? version,
            string? serialNumber,
            string? assetTag,
            ChassisState? bootUpState,
            ChassisState? powerSupplyState,
            ChassisState? thermalState,
            ChassisSecurityStatus? securityStatus,
            uint? oemDefined,
            byte? height,
            byte? numberOfPowerCords,
            byte containedElementCount,
            byte containedElementRecordLength,
            IReadOnlyList<byte[]> containedElementRecords,
            IReadOnlyList<SystemEnclosureContainedElement> containedElements,
            string? skuNumber,
            RackType? rackType,
            byte? rackHeight)
        {
            Length = structureLength;
            Handle = handle;
            Manufacturer = !string.IsNullOrWhiteSpace(manufacturer) ? manufacturer : null;
            TypeAndLock = typeAndLock;
            Version = !string.IsNullOrWhiteSpace(version) ? version : null;
            SerialNumber = !string.IsNullOrWhiteSpace(serialNumber) ? serialNumber : null;
            AssetTag = !string.IsNullOrWhiteSpace(assetTag) ? assetTag : null;
            BootUpState = bootUpState;
            PowerSupplyState = powerSupplyState;
            ThermalState = thermalState;
            SecurityStatus = securityStatus;
            OemDefined = oemDefined;
            Height = height;
            NumberOfPowerCords = numberOfPowerCords;
            ContainedElementCount = containedElementCount;
            ContainedElementRecordLength = containedElementRecordLength;
            ContainedElementRecords = containedElementRecords ?? [];
            ContainedElements = containedElements ?? [];
            SkuNumber = !string.IsNullOrWhiteSpace(skuNumber) ? skuNumber : null;
            RackType = rackType;
            RackHeight = rackHeight;
        }

        /// <summary>
        /// Determines whether this is a laptop or portable system.
        /// </summary>
        /// <returns>True if this is a portable system; otherwise, false.</returns>
        public bool IsPortable()
        {
            return TypeAndLock.Type is ChassisType.Laptop or ChassisType.Notebook or ChassisType.Portable or ChassisType.HandHeld or ChassisType.SubNotebook or ChassisType.Tablet or ChassisType.Convertible or ChassisType.Detachable;
        }

        /// <summary>
        /// Determines whether this is a server chassis.
        /// </summary>
        /// <returns>True if this is a server chassis; otherwise, false.</returns>
        public bool IsServerChassis()
        {
            return TypeAndLock.Type is ChassisType.MainServerChassis or ChassisType.RackMountChassis or ChassisType.BladeEnclosure or ChassisType.Blade;
        }

        /// <summary>
        /// Determines whether this is a rack-mount form factor.
        /// </summary>
        /// <returns>True if rack-mount; otherwise, false.</returns>
        public bool IsRackMount()
        {
            return TypeAndLock.Type is ChassisType.RackMountChassis or ChassisType.BladeEnclosure;
        }

        /// <summary>
        /// Gets the effective rack height in rack units (U). This returns the Height field when specified (non-zero, not 0xFF); otherwise null.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1024:Use properties where appropriate", Justification = "A property would insinuate that it's part of the SMBIOS specification.")]
        public int? GetRackUnits()
        {
            return Height.HasValue ? Height.Value : RackHeight.HasValue ? RackHeight.Value : null;
        }

        /// <summary>
        /// Returns a string representation of the system enclosure information.
        /// </summary>
        public override string ToString()
        {
            return $"{Manufacturer} {TypeAndLock.Type} ({SerialNumber})";
        }
    }
}
