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
    /// Provides unit tests for verifying the correct bit assignments of firmware characteristic flags.
    /// </summary>
    /// <remarks>Use this class to ensure that each firmware characteristic flag is assigned to the expected
    /// bit position, typically when validating enumerations or constants representing firmware features. The tests help
    /// maintain consistency and correctness in bitwise flag definitions.</remarks>
    public sealed class FirmwareCharacteristicsTests
    {
        /// <summary>
        /// Verifies that the specified bit position corresponds to the expected flag value.
        /// </summary>
        /// <remarks>This test ensures that each flag is assigned to the correct bit by comparing the
        /// expected value to a left-shifted bit at the specified position. Use this test to validate bitwise flag
        /// assignments in enumerations or constants.</remarks>
        /// <param name="expectedValue">The expected flag value, represented as an unsigned 64-bit integer, for the given bit position.</param>
        /// <param name="bitPosition">The zero-based position of the bit to check within the flag value.</param>
        [Theory]
        [MemberData(nameof(BitAssignments))]
        public void Flags_AreAssignedToExpectedBit(ulong expectedValue, int bitPosition)
        {
            Assert.Equal(expectedValue, 1UL << bitPosition);
        }

        /// <summary>
        /// Gets a collection of test data representing firmware characteristic bit assignments and their corresponding
        /// bit positions.
        /// </summary>
        /// <remarks>Each entry in the collection maps a specific firmware characteristic, represented as
        /// a <see cref="ulong"/>, to its associated bit position as an <see cref="int"/>. This data is typically used
        /// for parameterized unit tests to verify correct handling of firmware characteristic flags.</remarks>
        public static TheoryData<ulong, int> BitAssignments => new()
        {
            { (ulong)FirmwareCharacteristics.IsaSupported, 4 },
            { (ulong)FirmwareCharacteristics.McaSupported, 5 },
            { (ulong)FirmwareCharacteristics.EisaSupported, 6 },
            { (ulong)FirmwareCharacteristics.PciSupported, 7 },
            { (ulong)FirmwareCharacteristics.PcCardSupported, 8 },
            { (ulong)FirmwareCharacteristics.PlugAndPlaySupported, 9 },
            { (ulong)FirmwareCharacteristics.ApmSupported, 10 },
            { (ulong)FirmwareCharacteristics.BiosUpgradeable, 11 },
            { (ulong)FirmwareCharacteristics.BiosShadowingAllowed, 12 },
            { (ulong)FirmwareCharacteristics.VlVesaSupported, 13 },
            { (ulong)FirmwareCharacteristics.EscdSupported, 14 },
            { (ulong)FirmwareCharacteristics.BootFromCdSupported, 15 },
            { (ulong)FirmwareCharacteristics.SelectableBootSupported, 16 },
            { (ulong)FirmwareCharacteristics.BiosRomSocketed, 17 },
            { (ulong)FirmwareCharacteristics.BootFromPcCardSupported, 18 },
            { (ulong)FirmwareCharacteristics.EddSupported, 19 },
            { (ulong)FirmwareCharacteristics.Nec98FloppySupported, 20 },
            { (ulong)FirmwareCharacteristics.ToshibaFloppySupported, 21 },
            { (ulong)FirmwareCharacteristics.Floppy525_360Supported, 22 },
            { (ulong)FirmwareCharacteristics.Floppy525_1200Supported, 23 },
            { (ulong)FirmwareCharacteristics.Floppy35_720Supported, 24 },
            { (ulong)FirmwareCharacteristics.Floppy35_2880Supported, 25 },
            { (ulong)FirmwareCharacteristics.PrintScreenSupported, 26 },
            { (ulong)FirmwareCharacteristics.Keyboard8042Supported, 27 },
            { (ulong)FirmwareCharacteristics.SerialSupported, 28 },
            { (ulong)FirmwareCharacteristics.PrinterSupported, 29 },
            { (ulong)FirmwareCharacteristics.CgaMonoVideoSupported, 30 },
            { (ulong)FirmwareCharacteristics.NecPc98, 31 }
        };
    }
}
