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
    /// Contains unit tests for the SystemEnclosure class, verifying correct parsing, normalization, and validation of
    /// SMBIOS System Enclosure structures.
    /// </summary>
    /// <remarks>These tests cover scenarios including parsing of complete enclosure data, normalization of
    /// optional fields, handling of record length and buffer size constraints, and enforcement of input validation. The
    /// class ensures that the SystemEnclosure API behaves as expected when processing various SMBIOS data
    /// inputs.</remarks>
    public sealed class SystemEnclosureTests
    {
        /// <summary>
        /// Verifies that the SystemEnclosure.Get method correctly parses a complete enclosure structure with all fields
        /// populated.
        /// </summary>
        /// <remarks>This test ensures that all properties of the SystemEnclosure object, including
        /// manufacturer, version, serial number, asset tag, chassis type, states, security status, OEM-defined data,
        /// contained elements, SKU, rack type, and rack height, are accurately parsed from the provided buffer. It also
        /// validates the correct interpretation of contained element records and the behavior of related methods such
        /// as GetRackUnits, IsServerChassis, IsRackMount, and IsPortable.</remarks>
        [Fact]
        public void Get_ParsesCompleteStructure()
        {
            byte[] formatted =
            [
                1, // Manufacturer index
                0x80 | (byte)ChassisType.RackMountChassis, // Lock bit + rack-mount chassis
                2, // Version index
                3, // Serial index
                4, // Asset tag index
                (byte)ChassisState.Safe, // Boot-up state
                (byte)ChassisState.Warning, // Power supply state
                (byte)ChassisState.Critical, // Thermal state
                (byte)ChassisSecurityStatus.ExternalInterfaceLockedOut, // Security status
                0x44, // OEM defined (little-endian 0x11223344)
                0x33,
                0x22,
                0x11,
                6, // Height in rack units
                2, // Number of power cords
                2, // Contained element count
                3, // Contained element record length
                (byte)SmbiosType.BaseboardInformation, // Contained element 0 raw type
                1, // Minimum
                4, // Maximum
                0x80 | (byte)BaseboardType.Motherboard, // Contained element 1 raw type (baseboard)
                0xFF, // Minimum sentinel -> null
                0x00, // Maximum sentinel -> null
                5, // SKU string index
                (byte)RackType.OU, // Rack type
                0x16, // Rack height fallback
            ];
            byte[] buffer = BuildEnclosure(formatted, "Contoso", "RevA", "SN123", "Asset-01", "SKU-42");

            SystemEnclosure enclosure = SystemEnclosure.Get(buffer);
            Assert.Equal(SmbiosType.SystemEnclosure, enclosure.Type);
            Assert.Equal<ushort>(0x3040, enclosure.Handle);
            Assert.Equal<byte>(30, enclosure.Length);
            Assert.Equal("Contoso", enclosure.Manufacturer);
            Assert.Equal("RevA", enclosure.Version);
            Assert.Equal("SN123", enclosure.SerialNumber);
            Assert.Equal("Asset-01", enclosure.AssetTag);
            Assert.Equal(ChassisType.RackMountChassis, enclosure.TypeAndLock.Type);
            Assert.True(enclosure.TypeAndLock.ChassisLockPresent);
            Assert.Equal(ChassisState.Safe, enclosure.BootUpState);
            Assert.Equal(ChassisState.Warning, enclosure.PowerSupplyState);
            Assert.Equal(ChassisState.Critical, enclosure.ThermalState);
            Assert.Equal(ChassisSecurityStatus.ExternalInterfaceLockedOut, enclosure.SecurityStatus);
            Assert.Equal<uint?>(0x11223344, enclosure.OemDefined);
            Assert.Equal<byte?>(6, enclosure.Height);
            Assert.Equal<byte?>(2, enclosure.NumberOfPowerCords);
            Assert.Equal<byte>(2, enclosure.ContainedElementCount);
            Assert.Equal<byte>(3, enclosure.ContainedElementRecordLength);

            Assert.Collection(
                enclosure.ContainedElementRecords,
                record => Assert.Equal(new byte[] { (byte)SmbiosType.BaseboardInformation, 0x01, 0x04 }, record),
                record => Assert.Equal(new byte[] { 0x80 | (byte)BaseboardType.Motherboard, 0xFF, 0x00 }, record));

            Assert.Collection(
                enclosure.ContainedElements,
                element =>
                {
                    Assert.True(element.IsType);
                    Assert.Equal(SmbiosType.BaseboardInformation, element.Type);
                    Assert.Null(element.BaseboardType);
                    Assert.Equal<byte?>(1, element.Minimum);
                    Assert.Equal<byte?>(4, element.Maximum);
                    Assert.True(element.IsRangeValid);
                },
                element =>
                {
                    Assert.False(element.IsType);
                    Assert.Null(element.Type);
                    Assert.Equal(BaseboardType.Motherboard, element.BaseboardType);
                    Assert.Null(element.Minimum);
                    Assert.Null(element.Maximum);
                    Assert.False(element.IsRangeValid);
                });

            Assert.Equal("SKU-42", enclosure.SkuNumber);
            Assert.Equal(RackType.OU, enclosure.RackType);
            Assert.Equal<byte?>(0x16, enclosure.RackHeight);
            Assert.Equal(6, enclosure.GetRackUnits());
            Assert.True(enclosure.IsServerChassis());
            Assert.True(enclosure.IsRackMount());
            Assert.False(enclosure.IsPortable());
            Assert.Equal("Contoso RackMountChassis (SN123)", enclosure.ToString());
        }

        /// <summary>
        /// Verifies that the SystemEnclosure.Get method correctly normalizes optional string fields containing only
        /// whitespace or empty values, and that it handles unspecified or default values for enclosure properties as
        /// expected.
        /// </summary>
        /// <remarks>This test ensures that optional string fields such as Manufacturer, Version,
        /// SerialNumber, AssetTag, and SkuNumber are set to null when the corresponding input values are empty or
        /// contain only whitespace. It also checks that unspecified numeric and enum fields are handled according to
        /// SMBIOS conventions, and that related properties and methods reflect the normalized state.</remarks>
        [Fact]
        public void Get_NormalizesOptionalFieldsAndWhitespace()
        {
            byte[] formatted = new byte[20];
            formatted[0] = 1; // Manufacturer index -> whitespace
            formatted[1] = (byte)ChassisType.Notebook; // Portable chassis
            formatted[2] = 2; // Version index -> empty
            formatted[3] = 3; // Serial index -> whitespace
            formatted[4] = 4; // Asset tag index -> whitespace
            formatted[5] = (byte)ChassisState.Unknown;
            formatted[6] = (byte)ChassisState.Unknown;
            formatted[7] = (byte)ChassisState.Unknown;
            formatted[8] = (byte)ChassisSecurityStatus.Unknown;
            // OEM defined bytes left at zero
            formatted[13] = 0; // Height -> unspecified
            formatted[14] = 0; // Number of power cords -> unspecified
            formatted[15] = 0; // Contained element count
            formatted[16] = 0; // Record length
            formatted[17] = 5; // SKU index -> whitespace
            formatted[18] = (byte)RackType.Unspecified; // Rack type
            formatted[19] = 7; // Rack height fallback
            byte[] buffer = BuildEnclosure(formatted, "  ", string.Empty, " \t", "\r\n", "\t ");

            SystemEnclosure enclosure = SystemEnclosure.Get(buffer);
            Assert.Null(enclosure.Manufacturer);
            Assert.Null(enclosure.Version);
            Assert.Null(enclosure.SerialNumber);
            Assert.Null(enclosure.AssetTag);
            Assert.Null(enclosure.SkuNumber);
            Assert.Equal(ChassisType.Notebook, enclosure.TypeAndLock.Type);
            Assert.False(enclosure.TypeAndLock.ChassisLockPresent);
            Assert.True(enclosure.IsPortable());
            Assert.False(enclosure.IsServerChassis());
            Assert.False(enclosure.IsRackMount());
            Assert.Null(enclosure.Height);
            Assert.Null(enclosure.NumberOfPowerCords);
            Assert.Equal(RackType.Unspecified, enclosure.RackType);
            Assert.Equal<byte?>(7, enclosure.RackHeight);
            Assert.Equal(7, enclosure.GetRackUnits());
            Assert.Empty(enclosure.ContainedElementRecords);
            Assert.Empty(enclosure.ContainedElements);
        }

        /// <summary>
        /// Verifies that the SystemEnclosure.Get method skips the SKU number when the contained element record length
        /// is zero.
        /// </summary>
        /// <remarks>This test ensures that when the contained element record length is set to zero in the
        /// enclosure data, the resulting SystemEnclosure instance does not populate the SkuNumber property or related
        /// rack properties. It also checks that contained element records and elements are empty as expected.</remarks>
        [Fact]
        public void Get_SkipsSkuWhenContainedElementLengthZero()
        {
            byte[] formatted =
            [
                1, // Manufacturer index
                0x80 | (byte)ChassisType.MainServerChassis,
                2, // Version index
                3, // Serial index
                4, // Asset tag index
                (byte)ChassisState.Critical,
                (byte)ChassisState.Warning,
                (byte)ChassisState.Safe,
                (byte)ChassisSecurityStatus.ExternalInterfaceEnabled,
                0xAA,
                0xBB,
                0xCC,
                0xDD,
                0xFF, // Height sentinel -> null
                3, // Number of power cords
                1, // Contained element count without records
                0, // Record length zero -> skip SKU
            ];
            byte[] buffer = BuildEnclosure(formatted, "Vendor", "Model", "Serial", "Asset");

            SystemEnclosure enclosure = SystemEnclosure.Get(buffer);
            Assert.Equal(ChassisType.MainServerChassis, enclosure.TypeAndLock.Type);
            Assert.True(enclosure.TypeAndLock.ChassisLockPresent);
            Assert.True(enclosure.IsServerChassis());
            Assert.False(enclosure.IsRackMount());
            Assert.False(enclosure.IsPortable());
            Assert.Null(enclosure.Height);
            Assert.Equal<byte?>(3, enclosure.NumberOfPowerCords);
            Assert.Null(enclosure.SkuNumber);
            Assert.Null(enclosure.RackType);
            Assert.Null(enclosure.RackHeight);
            Assert.Null(enclosure.GetRackUnits());
            Assert.Equal<byte>(1, enclosure.ContainedElementCount);
            Assert.Equal<byte>(0, enclosure.ContainedElementRecordLength);
            Assert.Empty(enclosure.ContainedElementRecords);
            Assert.Empty(enclosure.ContainedElements);
        }

        /// <summary>
        /// Verifies that the SystemEnclosure class correctly limits the number of contained elements based on the
        /// available bytes in the SMBIOS enclosure data structure.
        /// </summary>
        /// <remarks>This test ensures that when the SMBIOS enclosure data specifies more contained
        /// element records than can fit in the available buffer, only the records that fit are parsed and exposed by
        /// the SystemEnclosure API. It checks that the ContainedElementCount and ContainedElementRecordLength
        /// properties reflect the data, and that the ContainedElements and ContainedElementRecords collections contain
        /// only the records that are fully present in the buffer.</remarks>
        [Fact]
        public void Get_LimitsContainedElementsToAvailableBytes()
        {
            byte[] formatted = new byte[20];
            formatted[0] = 1;
            formatted[1] = (byte)ChassisType.Tower;
            formatted[2] = 2;
            formatted[3] = 3;
            formatted[4] = 4;
            formatted[5] = (byte)ChassisState.Safe;
            formatted[6] = (byte)ChassisState.Safe;
            formatted[7] = (byte)ChassisState.Safe;
            formatted[8] = (byte)ChassisSecurityStatus.None;
            formatted[13] = 5; // Height
            formatted[14] = 1; // Power cords
            formatted[15] = 3; // Claims three records
            formatted[16] = 3; // Each record 3 bytes
            formatted[17] = (byte)SmbiosType.SystemInformation;
            formatted[18] = 0x02;
            formatted[19] = 0x03;
            byte[] buffer = BuildEnclosure(formatted, "MFR", "VER", "SN", "TAG");

            SystemEnclosure enclosure = SystemEnclosure.Get(buffer);
            Assert.Equal<byte>(3, enclosure.ContainedElementCount);
            Assert.Equal<byte>(3, enclosure.ContainedElementRecordLength);
            _ = Assert.Single(enclosure.ContainedElementRecords);
            _ = Assert.Single(enclosure.ContainedElements);
            Assert.Equal(SmbiosType.SystemInformation, enclosure.ContainedElements[0].Type);
            Assert.Equal(5, enclosure.GetRackUnits());
        }

        /// <summary>
        /// Verifies that the Get method throws an ArgumentException when the provided structure is too short.
        /// </summary>
        /// <remarks>This test ensures that SystemEnclosure.Get enforces input length requirements and
        /// provides an appropriate error message when the input buffer does not meet the minimum expected
        /// size.</remarks>
        [Fact]
        public void Get_ThrowsWhenStructureTooShort()
        {
            byte[] formatted = [1, 0, 0, 0];
            byte[] buffer = BuildEnclosure(formatted, "M");
            ArgumentException ex = Assert.Throws<ArgumentException>(() => SystemEnclosure.Get(buffer));
            Assert.Contains("too short", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Builds a raw SMBIOS System Enclosure structure using the specified formatted data and optional string
        /// values.
        /// </summary>
        /// <param name="formatted">The formatted portion of the SMBIOS structure as a byte array. Cannot be null.</param>
        /// <param name="strings">An optional array of strings to append to the SMBIOS structure. Each string represents a string field in the
        /// structure.</param>
        /// <returns>A byte array containing the raw SMBIOS System Enclosure structure with the provided formatted data and
        /// strings.</returns>
        private static byte[] BuildEnclosure(byte[] formatted, params string[] strings)
        {
            return SmbiosTestDataBuilder.BuildRawSmbios(
                new SmbiosTestDataBuilder.SmbiosStructure(
                    SmbiosType.SystemEnclosure,
                    0x3040,
                    formatted,
                    strings));
        }
    }
}
