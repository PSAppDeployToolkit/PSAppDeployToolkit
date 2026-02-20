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
    /// Contains unit tests for verifying the parsing and normalization behavior of the SystemInformation class when
    /// processing SMBIOS System Information structures.
    /// </summary>
    /// <remarks>These tests cover scenarios such as handling complete and incomplete SMBIOS data,
    /// normalization of empty or whitespace-only string fields, correct interpretation of sentinel values for UUIDs,
    /// and validation of required structure length. Use these tests to ensure that the SystemInformation class
    /// correctly maps SMBIOS fields to its properties and handles edge cases as expected.</remarks>
    public sealed class SystemInformationTests
    {
        /// <summary>
        /// Verifies that the SystemInformation.Get method correctly parses a complete SMBIOS System Information
        /// structure, including all string and binary fields.
        /// </summary>
        /// <remarks>This test ensures that all properties of the SystemInformation class are populated as
        /// expected when provided with a fully populated SMBIOS structure. It checks the correct mapping of
        /// manufacturer, product name, version, serial number, UUID, wake-up type, SKU number, and family
        /// fields.</remarks>
        [Fact]
        public void Get_ParsesCompleteStructure()
        {
            Guid expectedUuid = Guid.Parse("00112233-4455-6677-8899-AABBCCDDEEFF");
            byte[] formatted = new byte[23];
            formatted[0] = 1; // Manufacturer index
            formatted[1] = 2; // Product index
            formatted[2] = 3; // Version index
            formatted[3] = 4; // Serial index
            expectedUuid.ToByteArray().CopyTo(formatted, 4);
            formatted[20] = (byte)SystemWakeUpType.PowerSwitch;
            formatted[21] = 5; // SKU index
            formatted[22] = 6; // Family index

            byte[] buffer = SmbiosTestDataBuilder.BuildRawSmbios(
                new SmbiosTestDataBuilder.SmbiosStructure(
                    SmbiosType.SystemInformation,
                    0x2222,
                    formatted,
                    "Contoso",
                    "Workstation",
                    "Rev A",
                    "SN123",
                    "SKU-ABC",
                    "FamilyPro"));

            SystemInformation info = SystemInformation.Get(buffer);
            Assert.Equal(SmbiosType.SystemInformation, info.Type);
            Assert.Equal<ushort>(0x2222, info.Handle);
            Assert.Equal((byte)27, info.Length);
            Assert.Equal("Contoso", info.Manufacturer);
            Assert.Equal("Workstation", info.ProductName);
            Assert.Equal("Rev A", info.Version);
            Assert.Equal("SN123", info.SerialNumber);
            Assert.Equal(expectedUuid, info.Uuid);
            Assert.Equal(SystemWakeUpType.PowerSwitch, info.WakeUpType);
            Assert.Equal("SKU-ABC", info.SkuNumber);
            Assert.Equal("FamilyPro", info.Family);
            Assert.Equal("Contoso Workstation (SN123)", info.ToString());
        }

        /// <summary>
        /// Verifies that the Get method normalizes empty or whitespace-only string fields to null when parsing system
        /// information data.
        /// </summary>
        /// <remarks>This test ensures that fields containing only empty strings or whitespace are treated
        /// as null values by the SystemInformation.Get method, matching expected normalization behavior for SMBIOS
        /// string fields.</remarks>
        [Fact]
        public void Get_NormalizesEmptyStrings()
        {
            byte[] formatted = new byte[23];
            formatted[0] = 1; // Manufacturer index
            formatted[1] = 2; // Product index
            formatted[2] = 3; // Version index
            formatted[3] = 4; // Serial index
            // UUID left zeroed -> treated as null
            formatted[20] = (byte)SystemWakeUpType.Unknown;
            formatted[21] = 5; // SKU index
            formatted[22] = 6; // Family index

            byte[] buffer = SmbiosTestDataBuilder.BuildRawSmbios(
                new SmbiosTestDataBuilder.SmbiosStructure(
                    SmbiosType.SystemInformation,
                    0x1000,
                    formatted,
                    " ",
                    null!,
                    "",
                    " \t",
                    "",
                    "  "));

            SystemInformation info = SystemInformation.Get(buffer);
            Assert.Null(info.Manufacturer);
            Assert.Null(info.ProductName);
            Assert.Null(info.Version);
            Assert.Null(info.SerialNumber);
            Assert.Null(info.Uuid);
            Assert.Equal(SystemWakeUpType.Unknown, info.WakeUpType);
            Assert.Null(info.SkuNumber);
            Assert.Null(info.Family);
            Assert.Equal("  ()", info.ToString());
        }

        /// <summary>
        /// Verifies that the Uuid property is set to null when the underlying data contains a sentinel value.
        /// </summary>
        /// <remarks>This test ensures that when the UUID field in the SMBIOS data is filled with a
        /// sentinel value, the SystemInformation.Get method correctly interprets it as null. This behavior is important
        /// for distinguishing between valid and unset UUIDs in SMBIOS structures.</remarks>
        /// <param name="fillValue">The byte value used to fill the UUID field in the test data. Represents a potential sentinel value
        /// indicating an unset or invalid UUID.</param>
        [Theory]
        [MemberData(nameof(UuidSentinelData))]
        public void Get_SetsUuidToNullForSentinelValues(byte fillValue)
        {
            byte[] formatted = new byte[23];
            formatted[0] = 1;
            formatted[1] = 2;
            formatted[2] = 3;
            formatted[3] = 4;
            for (int i = 0; i < 16; i++)
            {
                formatted[4 + i] = fillValue;
            }
            formatted[20] = (byte)SystemWakeUpType.LanRemote;
            formatted[21] = 5;
            formatted[22] = 6;

            byte[] buffer = SmbiosTestDataBuilder.BuildRawSmbios(
                new SmbiosTestDataBuilder.SmbiosStructure(
                    SmbiosType.SystemInformation,
                    0x0F0F,
                    formatted,
                    "Manufacturer",
                    "Product",
                    "Version",
                    "Serial",
                    "SKU",
                    "Family"));

            SystemInformation info = SystemInformation.Get(buffer);
            Assert.Null(info.Uuid);
            Assert.Equal(SystemWakeUpType.LanRemote, info.WakeUpType);
        }

        /// <summary>
        /// Gets a collection of byte values used as sentinel data for UUID-related tests.
        /// </summary>
        /// <remarks>This data set is intended for use with parameterized unit tests that require
        /// representative sentinel byte values for UUID scenarios.</remarks>
        public static TheoryData<byte> UuidSentinelData { get; } =
        [
            0x00,
            0xFF,
        ];

        /// <summary>
        /// Verifies that the SystemInformation.Get method sets the Uuid property to null when the SMBIOS structure is
        /// shorter than the expected length for a UUID field.
        /// </summary>
        /// <remarks>This test ensures that incomplete or truncated SMBIOS System Information structures
        /// do not result in invalid UUID values. It also checks that other properties, such as WakeUpType, SkuNumber,
        /// and Family, are set to their default values when the structure is too short.</remarks>
        [Fact]
        public void Get_LeavesUuidNullWhenStructureTooShort()
        {
            byte[] formatted = new byte[19]; // Structure length 23
            formatted[0] = 1;
            formatted[1] = 2;
            formatted[2] = 3;
            formatted[3] = 4;
            for (int i = 0; i < 15; i++)
            {
                formatted[4 + i] = (byte)(0x10 + i);
            }

            byte[] buffer = SmbiosTestDataBuilder.BuildRawSmbios(
                new SmbiosTestDataBuilder.SmbiosStructure(
                    SmbiosType.SystemInformation,
                    0x3030,
                    formatted,
                    "Manufacturer",
                    "Product",
                    "Version",
                    "Serial"));

            SystemInformation info = SystemInformation.Get(buffer);
            Assert.Null(info.Uuid);
            Assert.Equal(SystemWakeUpType.Unknown, info.WakeUpType);
            Assert.Null(info.SkuNumber);
            Assert.Null(info.Family);
        }

        /// <summary>
        /// Verifies that the SystemInformation.Get method correctly parses the UUID when the WakeUpType field is
        /// missing from the SMBIOS data.
        /// </summary>
        /// <remarks>This test ensures that the UUID is extracted as expected and that the WakeUpType
        /// property is set to SystemWakeUpType.Unknown when the corresponding field is not present in the SMBIOS
        /// structure. It also verifies that optional string properties such as SkuNumber and Family are null when not
        /// provided.</remarks>
        [Fact]
        public void Get_ParsesUuidWhenWakeUpTypeMissing()
        {
            Guid expectedUuid = Guid.Parse("11223344-5566-7788-99AA-BBCCDDEEFF00");
            byte[] formatted = new byte[20]; // Structure length 24
            formatted[0] = 1;
            formatted[1] = 2;
            formatted[2] = 3;
            formatted[3] = 4;
            expectedUuid.ToByteArray().CopyTo(formatted, 4);

            byte[] buffer = SmbiosTestDataBuilder.BuildRawSmbios(
                new SmbiosTestDataBuilder.SmbiosStructure(
                    SmbiosType.SystemInformation,
                    0x5050,
                    formatted,
                    "Manufacturer",
                    "Product",
                    "Version",
                    "Serial"));

            SystemInformation info = SystemInformation.Get(buffer);
            Assert.Equal(expectedUuid, info.Uuid);
            Assert.Equal(SystemWakeUpType.Unknown, info.WakeUpType);
            Assert.Null(info.SkuNumber);
            Assert.Null(info.Family);
        }

        /// <summary>
        /// Verifies that the WakeUpType property of the SystemInformation class defaults to SystemWakeUpType.Unknown
        /// when the wake-up type field is not present in the SMBIOS data structure.
        /// </summary>
        /// <remarks>This test also confirms that the SkuNumber and Family properties are null when their
        /// corresponding fields are absent from the SMBIOS data. Use this test to ensure correct default behavior when
        /// optional SMBIOS fields are missing.</remarks>
        [Fact]
        public void Get_DefaultsWakeUpTypeWhenNotPresent()
        {
            byte[] formatted = new byte[19]; // Structure length 23 (no UUID/Wakeup/SKU/Family)
            formatted[0] = 1;
            formatted[1] = 2;
            formatted[2] = 3;
            formatted[3] = 4;

            byte[] buffer = SmbiosTestDataBuilder.BuildRawSmbios(
                new SmbiosTestDataBuilder.SmbiosStructure(
                    SmbiosType.SystemInformation,
                    0x4040,
                    formatted,
                    "Manufacturer",
                    "Product",
                    "Version",
                    "Serial"));

            SystemInformation info = SystemInformation.Get(buffer);
            Assert.Equal(SystemWakeUpType.Unknown, info.WakeUpType);
            Assert.Null(info.SkuNumber);
            Assert.Null(info.Family);
        }

        /// <summary>
        /// Verifies that the Get method throws an ArgumentException when the provided SMBIOS structure buffer is too
        /// short to be valid.
        /// </summary>
        /// <remarks>This test ensures that the SystemInformation.Get method correctly validates the
        /// length of the input buffer and provides an appropriate error message when the buffer does not meet the
        /// minimum required size.</remarks>
        [Fact]
        public void Get_ThrowsWhenStructureTooShort()
        {
            byte[] formatted = [1, 2];
            byte[] buffer = SmbiosTestDataBuilder.BuildRawSmbios(
                new SmbiosTestDataBuilder.SmbiosStructure(
                    SmbiosType.SystemInformation,
                    0x0,
                    formatted,
                    "Manufacturer"));

            ArgumentException ex = Assert.Throws<ArgumentException>(() => SystemInformation.Get(buffer));
            Assert.Contains("too short", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
    }
}
