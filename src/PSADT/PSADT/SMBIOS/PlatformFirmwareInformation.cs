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
using System.Globalization;

namespace PSADT.SMBIOS
{
    /// <summary>
    /// Immutable representation of SMBIOS BIOS Information (Type 0) structure.
    /// </summary>
    public sealed record PlatformFirmwareInformation : ISmbiosStructure
    {
        /// <summary>
        /// Reads the SMBIOS BIOS Information (Type 0) structure.
        /// </summary>
        /// <returns>The BIOS information or null if not found.</returns>
        internal static PlatformFirmwareInformation Get(ReadOnlySpan<byte> buffer = default)
        {
            return SmbiosParsing.GetStructure(SmbiosType.PlatformFirmwareInformation, Parse, buffer);
        }

        /// <summary>
        /// Parses a BIOS Information (Type 0) structure.
        /// </summary>
        private static PlatformFirmwareInformation Parse(ReadOnlySpan<byte> buffer, int structureOffset, byte structureLength)
        {
            // Qualify the structure length before proceeding.
            if (structureLength < 18)
            {
                throw new ArgumentException($"BIOS Information structure too short: {structureLength} bytes");
            }

            // Calculate ROM and extended ROM size (if available and ROM size byte is 0xFF).
            BiosExtendedRomSize? extendedRomSize = null;
            byte romSize = buffer[structureOffset + 9];
            // Per spec: 0xFF is a sentinel indicating extended size. Do not treat as legacy 16MB.
            uint? romSizeBytes = romSize != 0xFF ? (uint)(romSize + 1) * 64 * 1024 : null;
            if (structureLength >= 26 && romSize == 0xFF)
            {
                // Bits 15:14 = unit (00b=MB, 01b=GB; others reserved), Bits 13:0 = size.
                extendedRomSize = new BiosExtendedRomSize(BinaryPrimitives.ReadUInt16LittleEndian(buffer.Slice(structureOffset + 24, 2)));
            }

            // Starting address segment (0 means not applicable per spec)
            ushort startingSegRaw = BinaryPrimitives.ReadUInt16LittleEndian(buffer.Slice(structureOffset + 6, 2));
            ushort? startingSeg = startingSegRaw != 0 ? startingSegRaw : null;

            // Return the information to the caller.
            int stringTableOffset = structureOffset + structureLength;
            return new PlatformFirmwareInformation(
                structureLength,
                handle: BinaryPrimitives.ReadUInt16LittleEndian(buffer.Slice(structureOffset + 2, 2)),
                vendor: SmbiosParsing.GetSmbiosString(buffer, stringTableOffset, buffer[structureOffset + 4]),
                version: SmbiosParsing.GetSmbiosString(buffer, stringTableOffset, buffer[structureOffset + 5]),
                startingAddressSegment: startingSeg,
                releaseDate: SmbiosParsing.GetSmbiosString(buffer, stringTableOffset, buffer[structureOffset + 8]),
                romSizeBytes,
                characteristics: (FirmwareCharacteristics)BinaryPrimitives.ReadUInt64LittleEndian(buffer.Slice(structureOffset + 10, 8)),
                characteristicsExt1: structureLength >= 19 ? (FirmwareCharacteristicsExtensionByte1)buffer[structureOffset + 18] : 0,
                characteristicsExt2: structureLength >= 20 ? (FirmwareCharacteristicsExtensionByte2)buffer[structureOffset + 19] : 0,
                systemBiosMajorRelease: structureLength >= 21 ? NormalizeByte255(buffer[structureOffset + 20]) : null,
                systemBiosMinorRelease: structureLength >= 22 ? NormalizeByte255(buffer[structureOffset + 21]) : null,
                embeddedControllerMajorRelease: structureLength >= 23 ? NormalizeByte255(buffer[structureOffset + 22]) : null,
                embeddedControllerMinorRelease: structureLength >= 24 ? NormalizeByte255(buffer[structureOffset + 23]) : null,
                extendedRomSize: extendedRomSize);
        }

        /// <summary>
        /// Gets the SMBIOS structure type.
        /// </summary>
        public SmbiosType Type => SmbiosType.PlatformFirmwareInformation;

        /// <summary>
        /// Gets the length of the structure in bytes.
        /// </summary>
        public byte Length { get; }

        /// <summary>
        /// Gets the handle (unique identifier) for this structure.
        /// </summary>
        public ushort Handle { get; }

        /// <summary>
        /// Gets the BIOS vendor name.
        /// </summary>
        public string? Vendor { get; }

        /// <summary>
        /// Gets the BIOS version.
        /// </summary>
        public string? Version { get; }

        /// <summary>
        /// Gets the BIOS starting address segment (null when not applicable on UEFI-based systems).
        /// </summary>
        public ushort? StartingAddressSegment { get; }

        /// <summary>
        /// Gets the BIOS release date, if supplied by the SMBIOS string table and successfully parsed.
        /// </summary>
        /// <remarks>
        /// This represents the date when the BIOS was released by the vendor. If the Release Date string is not supplied
        /// (string index 0) or cannot be parsed, this will be null.
        /// </remarks>
        public DateTime ReleaseDate { get; }

        /// <summary>
        /// Gets the BIOS ROM size in bytes.
        /// </summary>
        public uint? RomSizeBytes { get; }

        /// <summary>
        /// Gets the BIOS characteristics.
        /// </summary>
        public FirmwareCharacteristics Characteristics { get; }

        /// <summary>
        /// Gets the BIOS characteristics extension byte 1.
        /// </summary>
        public FirmwareCharacteristicsExtensionByte1 CharacteristicsExt1 { get; }

        /// <summary>
        /// Gets the BIOS characteristics extension byte 2.
        /// </summary>
        public FirmwareCharacteristicsExtensionByte2 CharacteristicsExt2 { get; }

        /// <summary>
        /// Gets the system BIOS major release.
        /// </summary>
        public byte? SystemBiosMajorRelease { get; }

        /// <summary>
        /// Gets the system BIOS minor release.
        /// </summary>
        public byte? SystemBiosMinorRelease { get; }

        /// <summary>
        /// Gets the embedded controller firmware major release.
        /// </summary>
        public byte? EmbeddedControllerMajorRelease { get; }

        /// <summary>
        /// Gets the embedded controller firmware minor release.
        /// </summary>
        public byte? EmbeddedControllerMinorRelease { get; }

        /// <summary>
        /// Raw decoded Extended ROM Size field (present only when legacy size byte is 0xFF and length >= 26).
        /// </summary>
        public BiosExtendedRomSize? ExtendedRomSize { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PlatformFirmwareInformation"/> class.
        /// </summary>
        private PlatformFirmwareInformation(
            byte structureLength,
            ushort handle,
            string? vendor,
            string? version,
            ushort? startingAddressSegment,
            string? releaseDate,
            uint? romSizeBytes,
            FirmwareCharacteristics characteristics,
            FirmwareCharacteristicsExtensionByte1 characteristicsExt1,
            FirmwareCharacteristicsExtensionByte2 characteristicsExt2,
            byte? systemBiosMajorRelease,
            byte? systemBiosMinorRelease,
            byte? embeddedControllerMajorRelease,
            byte? embeddedControllerMinorRelease,
            BiosExtendedRomSize? extendedRomSize)
        {
            Length = structureLength;
            Handle = handle;
            Vendor = !string.IsNullOrWhiteSpace(vendor) ? vendor : null;
            Version = !string.IsNullOrWhiteSpace(version) ? version : null;
            StartingAddressSegment = startingAddressSegment;
            ReleaseDate = DateTime.TryParseExact(releaseDate?.TrimEnd('Z') + 'Z', ["MM/dd/yyyyZ", "M/d/yyyyZ", "MM/dd/yyZ", "M/d/yyZ"], CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate) ? parsedDate.ToUniversalTime() : throw new ArgumentOutOfRangeException($"The system's release date of [{releaseDate}] was unable to be parsed.", (Exception?)null);
            RomSizeBytes = romSizeBytes;
            Characteristics = characteristics;
            CharacteristicsExt1 = characteristicsExt1;
            CharacteristicsExt2 = characteristicsExt2;
            SystemBiosMajorRelease = systemBiosMajorRelease;
            SystemBiosMinorRelease = systemBiosMinorRelease;
            EmbeddedControllerMajorRelease = embeddedControllerMajorRelease;
            EmbeddedControllerMinorRelease = embeddedControllerMinorRelease;
            ExtendedRomSize = extendedRomSize;
        }

        /// <summary>
        /// Determines whether this system supports UEFI.
        /// </summary>
        /// <returns>True if UEFI is supported; otherwise, false.</returns>
        public bool IsUefiSupported()
        {
            return CharacteristicsExt2.HasFlag(FirmwareCharacteristicsExtensionByte2.UefiSupported);
        }

        /// <summary>
        /// Determines whether this BIOS is upgradeable.
        /// </summary>
        /// <returns>True if the BIOS is upgradeable; otherwise, false.</returns>
        public bool IsUpgradeable()
        {
            return Characteristics.HasFlag(FirmwareCharacteristics.BiosUpgradeable);
        }

        /// <summary>
        /// Determines whether this system is running in a virtual machine.
        /// </summary>
        /// <returns>True if running in a virtual machine; otherwise, false.</returns>
        public bool IsVirtualMachine()
        {
            return CharacteristicsExt2.HasFlag(FirmwareCharacteristicsExtensionByte2.VirtualMachine);
        }

        /// <summary>
        /// Determines whether this system supports manufacturing mode.
        /// </summary>
        /// <returns>True if manufacturing mode is supported; otherwise, false.</returns>
        public bool IsManufacturingModeSupported()
        {
            return CharacteristicsExt2.HasFlag(FirmwareCharacteristicsExtensionByte2.ManufacturingModeSupported);
        }

        /// <summary>
        /// Determines whether this system is currently in manufacturing mode.
        /// </summary>
        /// <returns>True if manufacturing mode is enabled; otherwise, false.</returns>
        public bool IsManufacturingModeEnabled()
        {
            return CharacteristicsExt2.HasFlag(FirmwareCharacteristicsExtensionByte2.ManufacturingModeEnabled);
        }

        /// <summary>
        /// Gets the BIOS age in days from the release date to now.
        /// </summary>
        /// <returns>The age in days; returns <see cref="double.NaN"/> if the release date is not available.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1024:Use properties where appropriate", Justification = "A property would insinuate that it's part of the SMBIOS specification.")]
        public double GetBiosAgeInDays()
        {
            return (DateTime.Now - ReleaseDate).TotalDays;
        }

        /// <summary>
        /// Determines whether this BIOS was released after a specified date.
        /// </summary>
        /// <param name="compareDate">The date to compare against.</param>
        /// <returns>True if BIOS was released after the specified date; otherwise, false.</returns>
        public bool IsReleasedAfter(DateTime compareDate)
        {
            return ReleaseDate > compareDate;
        }

        /// <summary>
        /// Normalizes a byte value by converting it to null if it equals 255.
        /// </summary>
        /// <param name="value">The byte value to normalize.</param>
        /// <returns>The original byte value if it is not 255; otherwise, <see langword="null"/>.</returns>
        private static byte? NormalizeByte255(byte value)
        {
            return value != 0xFF ? value : null;
        }

        /// <summary>
        /// Returns the System BIOS version as a System.Version if both major and minor values are present; otherwise null.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1024:Use properties where appropriate", Justification = "A property would insinuate that it's part of the SMBIOS specification.")]
        public Version? GetSystemBiosVersion()
        {
            return SystemBiosMajorRelease.HasValue && SystemBiosMinorRelease.HasValue ? new(SystemBiosMajorRelease.Value, SystemBiosMinorRelease.Value) : null;
        }

        /// <summary>
        /// Returns the Embedded Controller firmware version as a System.Version if both major and minor values are present; otherwise null.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1024:Use properties where appropriate", Justification = "A property would insinuate that it's part of the SMBIOS specification.")]
        public Version? GetEmbeddedControllerVersion()
        {
            return EmbeddedControllerMajorRelease.HasValue && EmbeddedControllerMinorRelease.HasValue ? new(EmbeddedControllerMajorRelease.Value, EmbeddedControllerMinorRelease.Value) : null;
        }

        /// <summary>
        /// Returns a string representation of the BIOS information.
        /// </summary>
        public override string ToString()
        {
            // Build without printing "null" when fields are missing
            string vendor = Vendor ?? string.Empty;
            string version = Version ?? string.Empty;
            string date = $" ({ReleaseDate:yyyy-MM-dd})";
            string spaceIfBoth = string.IsNullOrWhiteSpace(vendor) || string.IsNullOrWhiteSpace(version) ? string.Empty : " ";
            return $"{vendor}{spaceIfBoth}{version}{date}";
        }
    }
}
