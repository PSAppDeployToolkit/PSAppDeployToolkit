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
    /// Contains unit tests for verifying the bit assignments of firmware characteristic extension flags in
    /// FirmwareCharacteristicsExtensionByte1.
    /// </summary>
    /// <remarks>This class provides parameterized tests to ensure that each firmware characteristic extension
    /// flag is assigned to the correct bit position within a byte. It is intended for use with test frameworks that
    /// support data-driven testing, such as xUnit.</remarks>
    public sealed class FirmwareCharacteristicsExtensionByte1Tests
    {
        /// <summary>
        /// Verifies that the specified bit position corresponds to the expected flag value.
        /// </summary>
        /// <param name="expectedValue">The expected byte value representing the flag assigned to the specified bit position.</param>
        /// <param name="bitPosition">The zero-based position of the bit to check within the flag value.</param>
        [Theory]
        [MemberData(nameof(BitAssignments))]
        public void Flags_AreAssignedToExpectedBit(byte expectedValue, int bitPosition)
        {
            Assert.Equal(expectedValue, (byte)(1 << bitPosition));
        }

        /// <summary>
        /// Returns a collection of bit assignments for firmware characteristic extension flags and their corresponding
        /// bit positions.
        /// </summary>
        /// <remarks>Each element in the returned collection represents a specific firmware characteristic
        /// extension flag and the bit position it occupies. This can be used for parameterized testing or for mapping
        /// flag values to their bit positions in a byte.</remarks>
        /// <returns>An enumerable collection of object arrays, each containing a byte value representing a firmware
        /// characteristic extension flag and an integer indicating its associated bit position.</returns>
        public static TheoryData<byte, int> BitAssignments =>
            new()
            {
                { (byte)FirmwareCharacteristicsExtensionByte1.AcpiSupported, 0 },
                { (byte)FirmwareCharacteristicsExtensionByte1.UsbLegacySupported, 1 },
                { (byte)FirmwareCharacteristicsExtensionByte1.AgpSupported, 2 },
                { (byte)FirmwareCharacteristicsExtensionByte1.I2OBootSupported, 3 },
                { (byte)FirmwareCharacteristicsExtensionByte1.Ls120BootSupported, 4 },
                { (byte)FirmwareCharacteristicsExtensionByte1.AtapiZipBootSupported, 5 },
                { (byte)FirmwareCharacteristicsExtensionByte1.Ieee1394BootSupported, 6 },
                { (byte)FirmwareCharacteristicsExtensionByte1.SmartBatterySupported, 7 }
            };
    }
}
