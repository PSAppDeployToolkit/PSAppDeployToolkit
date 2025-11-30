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
    /// Contains unit tests for verifying the bit assignments and flag values of the
    /// FirmwareCharacteristicsExtensionByte2 enumeration.
    /// </summary>
    /// <remarks>This class is intended for use with automated test frameworks to ensure that each flag in the
    /// FirmwareCharacteristicsExtensionByte2 enumeration is assigned to the correct bit position. The tests help
    /// maintain consistency and correctness when working with firmware characteristic flags represented as
    /// bytes.</remarks>
    public sealed class FirmwareCharacteristicsExtensionByte2Tests
    {
        /// <summary>
        /// Verifies that the specified flag value corresponds to the expected bit position in a byte.
        /// </summary>
        /// <param name="expectedValue">The expected byte value with a single bit set at the specified position.</param>
        /// <param name="bitPosition">The zero-based position of the bit that should be set in the expected value.</param>
        [Theory]
        [MemberData(nameof(BitAssignments))]
        public void Flags_AreAssignedToExpectedBit(byte expectedValue, int bitPosition)
        {
            Assert.Equal(expectedValue, (byte)(1 << bitPosition));
        }

        /// <summary>
        /// Provides an enumeration of bit assignments for each value in the FirmwareCharacteristicsExtensionByte2
        /// enumeration.
        /// </summary>
        /// <remarks>Each returned object array contains two elements: the first is the byte value of a
        /// specific FirmwareCharacteristicsExtensionByte2 flag, and the second is the zero-based bit position
        /// associated with that flag. This method is typically used for parameterized testing or scenarios where
        /// bitwise flag assignments need to be enumerated.</remarks>
        /// <returns>An enumerable collection of object arrays, where each array contains a byte value representing a
        /// FirmwareCharacteristicsExtensionByte2 flag and its corresponding bit position as an integer.</returns>
        public static TheoryData<byte, int> BitAssignments()
        {
            TheoryData<byte, int> data = new()
            {
                { (byte)FirmwareCharacteristicsExtensionByte2.BiosBootSpecificationSupported, 0 },
                { (byte)FirmwareCharacteristicsExtensionByte2.FunctionKeyNetworkBootSupported, 1 },
                { (byte)FirmwareCharacteristicsExtensionByte2.TargetedContentDistribution, 2 },
                { (byte)FirmwareCharacteristicsExtensionByte2.UefiSupported, 3 },
                { (byte)FirmwareCharacteristicsExtensionByte2.VirtualMachine, 4 },
                { (byte)FirmwareCharacteristicsExtensionByte2.ManufacturingModeSupported, 5 },
                { (byte)FirmwareCharacteristicsExtensionByte2.ManufacturingModeEnabled, 6 }
            };
            return data;
        }
    }
}
