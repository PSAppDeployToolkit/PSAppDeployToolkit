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

namespace PSADT.Tests.SMBIOS
{
    /// <summary>
    /// Contains unit tests for the PlatformFirmwareInformation class, verifying correct parsing and behavior of SMBIOS
    /// Platform Firmware Information structures and related properties and methods.
    /// </summary>
    /// <remarks>These tests cover scenarios including complete and partial SMBIOS data parsing, handling of
    /// optional and legacy fields, extended ROM size interpretation, reserved unit handling, and validation of error
    /// conditions. The tests ensure that the PlatformFirmwareInformation API behaves as expected across a variety of
    /// SMBIOS data formats and edge cases.</remarks>
    public sealed class PlatformFirmwareInformationTests
    {
        /// <summary>
        /// Verifies that the PlatformFirmwareInformation.Get method correctly parses a complete SMBIOS Platform
        /// Firmware Information structure and all associated properties and methods return expected values.
        /// </summary>
        /// <remarks>This test covers parsing of all major fields, including vendor, version, release
        /// date, ROM size, firmware characteristics, extension bytes, BIOS and embedded controller versions, and
        /// derived methods such as IsUpgradeable and IsUefiSupported. It also validates date-based calculations and
        /// string representations to ensure comprehensive coverage of the structure's exposed API.</remarks>
        [Fact]
        public void Get_ParsesCompleteStructure()
        {
            const ushort handle = 0x1234;
            ulong characteristics = (ulong)(FirmwareCharacteristics.BiosUpgradeable | FirmwareCharacteristics.PciSupported);
            byte[] formatted = new byte[20];
            formatted[0] = 1; // Vendor index
            formatted[1] = 2; // Version index
            formatted[2] = 0xF0; // Starting address segment low
            formatted[3] = 0xFF; // Starting address segment high (0xFFF0)
            formatted[4] = 3; // Release date index
            formatted[5] = 0x20; // ROM size byte
            CopyUInt64LittleEndian(characteristics, formatted, 6);
            formatted[14] = (byte)(FirmwareCharacteristicsExtensionByte1.UsbLegacySupported | FirmwareCharacteristicsExtensionByte1.AcpiSupported);
            formatted[15] = (byte)(FirmwareCharacteristicsExtensionByte2.UefiSupported | FirmwareCharacteristicsExtensionByte2.VirtualMachine);
            formatted[16] = 1; // System BIOS major
            formatted[17] = 2; // System BIOS minor
            formatted[18] = 3; // EC major
            formatted[19] = 4; // EC minor

            byte[] buffer = SmbiosTestDataBuilder.BuildRawSmbios(
                new SmbiosTestDataBuilder.SmbiosStructure(
                    SmbiosType.PlatformFirmwareInformation,
                    handle,
                    formatted,
                    "Acme Corp.",
                    "FW 1.2",
                    "01/31/2020"));

            PlatformFirmwareInformation info = PlatformFirmwareInformation.Get(buffer);
            Assert.Equal(SmbiosType.PlatformFirmwareInformation, info.Type);
            Assert.Equal(handle, info.Handle);
            Assert.Equal((byte)24, info.Length);
            Assert.Equal("Acme Corp.", info.Vendor);
            Assert.Equal("FW 1.2", info.Version);
            Assert.True(info.StartingAddressSegment.HasValue);
            Assert.Equal<ushort>(0xFFF0, info.StartingAddressSegment!.Value);
            Assert.Equal<DateTime>(new(2020, 1, 31), info.ReleaseDate);
            Assert.True(info.RomSizeBytes.HasValue);
            Assert.Equal<uint>((0x20 + 1) * 64 * 1024, info.RomSizeBytes!.Value);
            Assert.Equal(characteristics, (ulong)info.Characteristics);
            Assert.True(info.IsUpgradeable());
            Assert.True(info.IsUefiSupported());
            Assert.True(info.IsVirtualMachine());
            Assert.False(info.IsManufacturingModeSupported());
            Assert.False(info.IsManufacturingModeEnabled());
            Assert.Equal((FirmwareCharacteristicsExtensionByte1)formatted[14], info.CharacteristicsExt1);
            Assert.Equal((FirmwareCharacteristicsExtensionByte2)formatted[15], info.CharacteristicsExt2);
            Assert.Equal(new Version(1, 2), info.GetSystemBiosVersion());
            Assert.Equal(new Version(3, 4), info.GetEmbeddedControllerVersion());

            double? age = info.GetBiosAgeInDays();
            _ = Assert.NotNull(age);
            Assert.True(age!.Value > 1800); // Expected to be well over 1800 days when running these tests in 2025
            bool? releasedAfter2019 = info.IsReleasedAfter(new DateTime(2019, 12, 31));
            bool? releasedAfter2021 = info.IsReleasedAfter(new DateTime(2021, 1, 1));
            Assert.True(releasedAfter2019.HasValue);
            Assert.True(releasedAfter2019.Value);
            Assert.True(releasedAfter2021.HasValue);
            Assert.False(releasedAfter2021.Value);
            Assert.Equal("Acme Corp. FW 1.2 (2020-01-31)", info.ToString());
        }

        /// <summary>
        /// Verifies that the Get method correctly normalizes optional fields in the PlatformFirmwareInformation
        /// structure when certain values are missing or set to null-equivalent representations.
        /// </summary>
        /// <remarks>This test ensures that fields such as Vendor, StartingAddressSegment, System BIOS
        /// version, and Embedded Controller version are set to null when their corresponding data is absent or marked
        /// as missing in the raw SMBIOS data. It also checks that other fields, such as Version, RomSizeBytes, and
        /// manufacturing mode flags, are populated as expected.</remarks>
        [Fact]
        public void Get_NormalizesOptionalFields()
        {
            const ushort handle = 0x0102;
            ulong characteristics = (ulong)FirmwareCharacteristics.BootFromCdSupported;
            byte[] formatted = new byte[20];
            formatted[0] = 0; // Vendor index missing
            formatted[1] = 2; // Version index
            formatted[2] = 0x00; // Starting address segment low (will be null)
            formatted[3] = 0x00; // Starting address segment high
            formatted[4] = 3; // Release date index
            formatted[5] = 0x00; // ROM size byte
            CopyUInt64LittleEndian(characteristics, formatted, 6);
            formatted[14] = 0;
            formatted[15] = (byte)(FirmwareCharacteristicsExtensionByte2.ManufacturingModeSupported | FirmwareCharacteristicsExtensionByte2.ManufacturingModeEnabled);
            formatted[16] = 0xFF; // System BIOS major -> null
            formatted[17] = 5;    // System BIOS minor
            formatted[18] = 6;    // EC major
            formatted[19] = 0xFF; // EC minor -> null

            byte[] buffer = SmbiosTestDataBuilder.BuildRawSmbios(
                new SmbiosTestDataBuilder.SmbiosStructure(
                    SmbiosType.PlatformFirmwareInformation,
                    handle,
                    formatted,
                    "  ",
                    "VersionOnly",
                    "01/01/2025"));

            PlatformFirmwareInformation info = PlatformFirmwareInformation.Get(buffer);
            Assert.Null(info.Vendor);
            Assert.Equal("VersionOnly", info.Version);
            Assert.Null(info.StartingAddressSegment);
            Assert.True(info.RomSizeBytes.HasValue);
            Assert.Equal<uint>(64 * 1024, info.RomSizeBytes!.Value); // (0 + 1) * 64 KB
            Assert.Null(info.GetSystemBiosVersion());
            Assert.Null(info.GetEmbeddedControllerVersion());
            Assert.True(info.IsManufacturingModeSupported());
            Assert.True(info.IsManufacturingModeEnabled());
            Assert.Equal("VersionOnly (2025-01-01)", info.ToString());
        }

        /// <summary>
        /// Verifies that the extended ROM size is used when the legacy ROM size byte is set to the sentinel value in
        /// the SMBIOS Platform Firmware Information structure.
        /// </summary>
        /// <remarks>This test ensures that when the legacy ROM size field contains the sentinel value
        /// (0xFF), the implementation correctly reads the extended ROM size and unit fields, and that the legacy ROM
        /// size property is null. It validates that the extended ROM size is interpreted as 2 GB with the appropriate
        /// unit and byte count.</remarks>
        [Fact]
        public void Get_UsesExtendedRomSizeWhenLegacyByteIsSentinel()
        {
            const ushort handle = 0x0B0B;
            ulong characteristics = 0;
            byte[] formatted = new byte[22];
            formatted[0] = 1;
            formatted[1] = 2;
            formatted[2] = 0;
            formatted[3] = 0;
            formatted[4] = 3;
            formatted[5] = 0xFF; // Sentinel indicating extended size
            CopyUInt64LittleEndian(characteristics, formatted, 6);
            formatted[14] = 0;
            formatted[15] = 0;
            formatted[16] = 0;
            formatted[17] = 0;
            formatted[18] = 0;
            formatted[19] = 0;
            ushort extendedRaw = ((int)BiosRomUnit.GB << 14) | 2; // 2 GB
            formatted[20] = (byte)(extendedRaw & 0xFF);
            formatted[21] = (byte)(extendedRaw >> 8);

            byte[] buffer = SmbiosTestDataBuilder.BuildRawSmbios(
                new SmbiosTestDataBuilder.SmbiosStructure(
                    SmbiosType.PlatformFirmwareInformation,
                    handle,
                    formatted,
                    "Vendor",
                    "Version",
                    "02/01/2024"));

            PlatformFirmwareInformation info = PlatformFirmwareInformation.Get(buffer);
            Assert.Null(info.RomSizeBytes);
            _ = Assert.NotNull(info.ExtendedRomSize);
            Assert.Equal(2, info.ExtendedRomSize!.Value.Size);
            Assert.Equal(BiosRomUnit.GB, info.ExtendedRomSize.Value.Unit);
            Assert.Equal(2UL * 1024UL * 1024UL * 1024UL, info.ExtendedRomSize.Value.Bytes);
        }

        /// <summary>
        /// Verifies that the ExtendedRomSize property correctly identifies and handles reserved unit values in the
        /// SMBIOS PlatformFirmwareInformation structure.
        /// </summary>
        /// <remarks>This test ensures that when the ExtendedRomSize field contains a reserved unit value,
        /// the IsUnitRecognized property is false and the Bytes property is null, indicating that the unit is not
        /// recognized according to the SMBIOS specification.</remarks>
        [Fact]
        public void Get_ExtendedRomSizeHandlesReservedUnits()
        {
            byte[] formatted = new byte[22];
            formatted[0] = 1;
            formatted[1] = 2;
            formatted[4] = 3;
            formatted[5] = 0xFF;
            CopyUInt64LittleEndian(0, formatted, 6);
            ushort extendedRaw = (2 << 14) | 100;
            formatted[20] = (byte)(extendedRaw & 0xFF);
            formatted[21] = (byte)(extendedRaw >> 8);

            byte[] buffer = SmbiosTestDataBuilder.BuildRawSmbios(
                new SmbiosTestDataBuilder.SmbiosStructure(
                    SmbiosType.PlatformFirmwareInformation,
                    0x2000,
                    formatted,
                    "Vendor",
                    "Version",
                    "01/01/2020"));

            PlatformFirmwareInformation info = PlatformFirmwareInformation.Get(buffer);
            _ = Assert.NotNull(info.ExtendedRomSize);
            Assert.False(info.ExtendedRomSize!.Value.IsUnitRecognized);
            Assert.Null(info.ExtendedRomSize!.Value.Bytes);
        }

        /// <summary>
        /// Verifies that when a legacy Platform Firmware Information structure does not include the extended ROM size
        /// field, the corresponding properties remain null after parsing.
        /// </summary>
        /// <remarks>This test ensures backward compatibility with legacy SMBIOS data that omits the
        /// extended ROM size field. It confirms that the parser does not assign a value to the extended size properties
        /// when the field is absent.</remarks>
        [Fact]
        public void Get_LegacySentinelWithoutExtendedFieldLeavesExtendedSizeNull()
        {
            byte[] formatted = new byte[20];
            formatted[0] = 1;
            formatted[1] = 2;
            formatted[4] = 3;
            formatted[5] = 0xFF; // Sentinel but no extended size present
            CopyUInt64LittleEndian(0, formatted, 6);

            byte[] buffer = SmbiosTestDataBuilder.BuildRawSmbios(
                new SmbiosTestDataBuilder.SmbiosStructure(
                    SmbiosType.PlatformFirmwareInformation,
                    0x2222,
                    formatted,
                    "Vendor",
                    "Version",
                    "04/01/2021"));

            PlatformFirmwareInformation info = PlatformFirmwareInformation.Get(buffer);
            Assert.Null(info.RomSizeBytes);
            Assert.Null(info.ExtendedRomSize);
        }

        /// <summary>
        /// Verifies that the Get method throws an ArgumentException when the SMBIOS structure buffer is too short to
        /// contain valid platform firmware information.
        /// </summary>
        /// <remarks>This test ensures that the Get method properly validates the length of the input
        /// buffer and provides an appropriate error message when the buffer does not meet the minimum required
        /// size.</remarks>
        [Fact]
        public void Get_ThrowsWhenStructureTooShort()
        {
            byte[] formatted = new byte[10];
            formatted[0] = 1;
            formatted[1] = 2;

            byte[] buffer = SmbiosTestDataBuilder.BuildRawSmbios(
                new SmbiosTestDataBuilder.SmbiosStructure(
                    SmbiosType.PlatformFirmwareInformation,
                    0xAAAA,
                    formatted,
                    "Vendor",
                    "Version"));

            ArgumentException ex = Assert.Throws<ArgumentException>(() => PlatformFirmwareInformation.Get(buffer));
            Assert.Contains("too short", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Copies the specified 64-bit unsigned integer value to the destination array in little-endian byte order
        /// starting at the given index.
        /// </summary>
        /// <remarks>This method writes eight bytes to the destination array, representing the value in
        /// little-endian order. Callers must ensure that the destination array has sufficient space to avoid an
        /// IndexOutOfRangeException.</remarks>
        /// <param name="value">The 64-bit unsigned integer value to copy to the destination array.</param>
        /// <param name="destination">The byte array that receives the little-endian representation of the value. Must have at least eight
        /// available bytes starting at the specified index.</param>
        /// <param name="index">The zero-based index in the destination array at which to begin writing the bytes.</param>
        private static void CopyUInt64LittleEndian(ulong value, byte[] destination, int index)
        {
            for (int i = 0; i < 8; i++)
            {
                destination[index + i] = (byte)((value >> (8 * i)) & 0xFF);
            }
        }
    }
}
