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

using PSADT.SMBIOS;

namespace PSADT.Tests.SMBIOS
{
    /// <summary>
    /// Contains unit tests for the SmbiosVersionInfo class, verifying version string formatting, SMBIOS structure type
    /// support, and version-related behaviors.
    /// </summary>
    /// <remarks>These tests cover various SMBIOS specification versions and structure types to ensure that
    /// SmbiosVersionInfo correctly reports supported and obsolete types, distinguishes between legacy and modern SMBIOS
    /// versions, and formats version strings as expected. The test methods use parameterized data to validate behavior
    /// across multiple SMBIOS types and version scenarios.</remarks>
    public sealed class SmbiosVersionInfoTests
    {
        /// <summary>
        /// Verifies that the GetVersionString method returns a string containing only the major and minor version
        /// numbers.
        /// </summary>
        [Fact]
        public void GetVersionString_ReturnsMajorMinor()
        {
            SmbiosVersionInfo info = new(3, 2, 0, SmbiosEntryPointType.Smbios3x);
            Assert.Equal("3.2", info.GetVersionString());
        }

        /// <summary>
        /// Verifies that the full version string returned by GetFullVersionString includes both the SMBIOS entry point
        /// type and the DMI version.
        /// </summary>
        [Fact]
        public void GetFullVersionString_IncludesEntryPointAndDmi()
        {
            SmbiosVersionInfo info = new(3, 1, 2, SmbiosEntryPointType.Smbios3x);
            Assert.Equal("SMBIOS 3.1 (Smbios3x), DMI 2", info.GetFullVersionString());
        }

        /// <summary>
        /// Verifies that the GetFullVersionString method omits optional version information when it is not present.
        /// </summary>
        /// <remarks>This test ensures that the returned version string does not include minor or revision
        /// numbers when they are zero or unspecified, and that only the major version is displayed.</remarks>
        [Fact]
        public void GetFullVersionString_OmitsOptionalInformation()
        {
            SmbiosVersionInfo info = new(2, 0, 0, SmbiosEntryPointType.Unknown);
            Assert.Equal("SMBIOS 2.0", info.GetFullVersionString());
        }

        /// <summary>
        /// Verifies that the ToString method returns the full SMBIOS version string for the SmbiosVersionInfo instance.
        /// </summary>
        [Fact]
        public void ToString_ReturnsFullVersion()
        {
            SmbiosVersionInfo info = new(2, 6, 1, SmbiosEntryPointType.Smbios2x);
            Assert.Equal(info.GetFullVersionString(), info.ToString());
        }

        /// <summary>
        /// Verifies that the specified SMBIOS type value is supported by SMBIOS version 2.0 structures.
        /// </summary>
        /// <remarks>This test ensures that common SMBIOS structure types are recognized as supported by
        /// SMBIOS version 2.0. It is intended for use with parameterized test cases covering multiple SMBIOS
        /// types.</remarks>
        /// <param name="typeValue">The byte value representing the SMBIOS structure type to test for support.</param>
        [Theory]
        [InlineData((byte)SmbiosType.PlatformFirmwareInformation)]
        [InlineData((byte)SmbiosType.SystemInformation)]
        [InlineData((byte)SmbiosType.BaseboardInformation)]
        [InlineData((byte)SmbiosType.SystemEnclosure)]
        [InlineData((byte)SmbiosType.ProcessorInformation)]
        [InlineData((byte)SmbiosType.MemoryControllerInformation)]
        [InlineData((byte)SmbiosType.MemoryModuleInformation)]
        [InlineData((byte)SmbiosType.CacheInformation)]
        [InlineData((byte)SmbiosType.PortConnectorInformation)]
        [InlineData((byte)SmbiosType.SystemSlots)]
        public void SupportsType_Smbios20Structures(byte typeValue)
        {
            SmbiosType type = (SmbiosType)typeValue;
            SmbiosVersionInfo info = new(2, 0, 0, SmbiosEntryPointType.Smbios2x);
            Assert.True(info.SupportsType(type));
        }

        /// <summary>
        /// Verifies that the specified SMBIOS structure type is supported in version 2.1 and later, but not in version
        /// 2.0.
        /// </summary>
        /// <remarks>This test ensures that only SMBIOS structure types introduced in version 2.1 or later
        /// are reported as supported by SmbiosVersionInfo instances for version 2.1 and above, and not by earlier
        /// versions.</remarks>
        /// <param name="typeValue">The byte value representing the SMBIOS structure type to test for support.</param>
        [Theory]
        [InlineData((byte)SmbiosType.OnBoardDevicesInformation)]
        [InlineData((byte)SmbiosType.OemStrings)]
        [InlineData((byte)SmbiosType.SystemConfigurationOptions)]
        [InlineData((byte)SmbiosType.FirmwareLanguageInformation)]
        [InlineData((byte)SmbiosType.GroupAssociations)]
        [InlineData((byte)SmbiosType.SystemEventLog)]
        [InlineData((byte)SmbiosType.PhysicalMemoryArray)]
        [InlineData((byte)SmbiosType.MemoryDevice)]
        public void SupportsType_Smbios21Structures(byte typeValue)
        {
            SmbiosType type = (SmbiosType)typeValue;
            SmbiosVersionInfo info = new(2, 1, 0, SmbiosEntryPointType.Smbios2x);
            Assert.True(info.SupportsType(type));
            SmbiosVersionInfo older = new(2, 0, 0, SmbiosEntryPointType.Smbios2x);
            Assert.False(older.SupportsType(type));
        }

        /// <summary>
        /// Verifies that SMBIOS 2.3 structures are supported for the specified type value in SMBIOS version 2.3 and not
        /// in version 2.2.
        /// </summary>
        /// <param name="typeValue">The numeric value representing the SMBIOS structure type to test for support.</param>
        [Theory]
        [InlineData((byte)SmbiosType.PortableBattery)]
        [InlineData((byte)SmbiosType.SystemReset)]
        [InlineData((byte)SmbiosType.HardwareSecurity)]
        public void SupportsType_Smbios23Structures(byte typeValue)
        {
            SmbiosType type = (SmbiosType)typeValue;
            SmbiosVersionInfo info = new(2, 3, 0, SmbiosEntryPointType.Smbios2x);
            Assert.True(info.SupportsType(type));
            SmbiosVersionInfo older = new(2, 2, 0, SmbiosEntryPointType.Smbios2x);
            Assert.False(older.SupportsType(type));
        }

        /// <summary>
        /// Verifies that the SupportsType method returns the correct result for SmbiosType.SystemPowerSupply based on
        /// the SMBIOS version.
        /// </summary>
        /// <remarks>This test ensures that SmbiosType.SystemPowerSupply is supported starting with SMBIOS
        /// version 2.7. It asserts that versions prior to 2.7 do not support this type, while version 2.7 and later
        /// do.</remarks>
        [Fact]
        public void SupportsType_SystemPowerSupplyRequires27()
        {
            SmbiosVersionInfo info = new(2, 7, 0, SmbiosEntryPointType.Smbios2x);
            SmbiosVersionInfo older = new(2, 6, 0, SmbiosEntryPointType.Smbios2x);
            Assert.True(info.SupportsType(SmbiosType.SystemPowerSupply));
            Assert.False(older.SupportsType(SmbiosType.SystemPowerSupply));
        }

        /// <summary>
        /// Verifies that the TPM Device (SmbiosType.TpmDevice) is only supported when using SMBIOS version 3.0 or
        /// later.
        /// </summary>
        /// <remarks>This test ensures that SmbiosVersionInfo.SupportsType returns true for
        /// SmbiosType.TpmDevice when the SMBIOS version is 3.0 or higher, and false for earlier versions. This behavior
        /// is required for correct feature detection based on SMBIOS version.</remarks>
        [Fact]
        public void SupportsType_TpmDeviceRequiresSmbios3()
        {
            SmbiosVersionInfo modern = new(3, 0, 0, SmbiosEntryPointType.Smbios3x);
            SmbiosVersionInfo legacy = new(2, 8, 0, SmbiosEntryPointType.Smbios2x);
            Assert.True(modern.SupportsType(SmbiosType.TpmDevice));
            Assert.False(legacy.SupportsType(SmbiosType.TpmDevice));
        }

        /// <summary>
        /// Verifies that the Firmware Inventory Information type is supported only for SMBIOS version 3.1.0 and later
        /// when using the SMBIOS 3.x entry point type.
        /// </summary>
        /// <remarks>This test ensures that the SupportsType method correctly identifies support for
        /// SmbiosType.FirmwareInventoryInformation based on the SMBIOS version. SMBIOS versions prior to 3.1.0 should
        /// not report support for this type.</remarks>
        [Fact]
        public void SupportsType_FirmwareInventoryRequires31()
        {
            SmbiosVersionInfo supported = new(3, 1, 0, SmbiosEntryPointType.Smbios3x);
            SmbiosVersionInfo unsupported = new(3, 0, 0, SmbiosEntryPointType.Smbios3x);
            Assert.True(supported.SupportsType(SmbiosType.FirmwareInventoryInformation));
            Assert.False(unsupported.SupportsType(SmbiosType.FirmwareInventoryInformation));
        }

        /// <summary>
        /// Verifies that the SupportsType method always returns true for the specified SMBIOS type value.
        /// </summary>
        /// <remarks>This test ensures that the SupportsType method of SmbiosVersionInfo returns <see
        /// langword="true"/> for types that are always supported, such as Inactive and EndOfTable.</remarks>
        /// <param name="typeValue">The byte value representing the SMBIOS type to test for support.</param>
        [Theory]
        [InlineData((byte)SmbiosType.Inactive)]
        [InlineData((byte)SmbiosType.EndOfTable)]
        public void SupportsType_AlwaysSupported(byte typeValue)
        {
            SmbiosType type = (SmbiosType)typeValue;
            SmbiosVersionInfo info = new(3, 0, 0, SmbiosEntryPointType.Smbios3x);
            Assert.True(info.SupportsType(type));
        }

        /// <summary>
        /// Verifies that the SupportsType method defaults to checking the major version when determining support for a
        /// specified SMBIOS type.
        /// </summary>
        /// <remarks>This test ensures that SupportsType returns true for types supported by the major
        /// version and false for types not supported, even when the minor version differs. It validates the default
        /// behavior for version compatibility checks.</remarks>
        [Fact]
        public void SupportsType_DefaultsToMajorVersionCheck()
        {
            SmbiosVersionInfo supported = new(2, 0, 0, SmbiosEntryPointType.Smbios2x);
            SmbiosVersionInfo unsupported = new(1, 9, 0, SmbiosEntryPointType.Unknown);
            Assert.True(supported.SupportsType(SmbiosType.ManagementControllerHostInterface));
            Assert.False(unsupported.SupportsType(SmbiosType.ManagementControllerHostInterface));
        }

        /// <summary>
        /// Verifies that the IsModernSmbios method returns <see langword="true"/> when the SMBIOS major version is 3.
        /// </summary>
        /// <remarks>This test ensures that SMBIOS version 3.0.0 is correctly identified as a modern
        /// SMBIOS implementation by the IsModernSmbios method.</remarks>
        [Fact]
        public void IsModernSmbios_ReturnsTrueForMajorThree()
        {
            SmbiosVersionInfo info = new(3, 0, 0, SmbiosEntryPointType.Smbios3x);
            Assert.True(info.IsModernSmbios());
        }

        /// <summary>
        /// Verifies that the IsLegacySmbios method returns <see langword="true"/> when the SMBIOS major version is 2.
        /// </summary>
        /// <remarks>This test ensures that SMBIOS versions with a major version of 2 are correctly
        /// identified as legacy by the IsLegacySmbios method.</remarks>
        [Fact]
        public void IsLegacySmbios_ReturnsTrueForMajorTwo()
        {
            SmbiosVersionInfo info = new(2, 5, 0, SmbiosEntryPointType.Smbios2x);
            Assert.True(info.IsLegacySmbios());
        }

        /// <summary>
        /// Verifies that the IsObsoleteType method of SmbiosVersionInfo returns the expected result for the specified
        /// SMBIOS type and minor version.
        /// </summary>
        /// <remarks>This test uses multiple inline data sets to validate the behavior of IsObsoleteType
        /// across different SMBIOS types and version combinations.</remarks>
        /// <param name="typeValue">The byte value representing the SMBIOS type to test.</param>
        /// <param name="minor">The minor version of the SMBIOS specification to use when constructing the SmbiosVersionInfo instance.</param>
        /// <param name="expected">The expected result indicating whether the specified SMBIOS type is considered obsolete for the given
        /// version.</param>
        [Theory]
        [InlineData((byte)SmbiosType.MemoryControllerInformation, 1, true)]
        [InlineData((byte)SmbiosType.MemoryModuleInformation, 1, true)]
        [InlineData((byte)SmbiosType.OnBoardDevicesInformation, 6, true)]
        [InlineData((byte)SmbiosType.BootIntegrityServicesEntryPoint, 0, true)]
        [InlineData((byte)SmbiosType.SystemInformation, 0, false)]
        public void IsObsoleteType_ReturnsExpectedValues(byte typeValue, byte minor, bool expected)
        {
            SmbiosType type = (SmbiosType)typeValue;
            SmbiosVersionInfo info = new(2, minor, 0, SmbiosEntryPointType.Smbios2x);
            Assert.Equal(expected, info.IsObsoleteType(type));
        }
    }
}
