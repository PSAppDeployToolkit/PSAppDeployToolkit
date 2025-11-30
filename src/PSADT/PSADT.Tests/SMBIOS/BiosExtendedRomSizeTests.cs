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
    /// Contains unit tests for the BiosExtendedRomSize struct, verifying the correctness of its properties and
    /// behavior.
    /// </summary>
    /// <remarks>This class includes both individual and parameterized tests to ensure that the
    /// BiosExtendedRomSize struct correctly interprets raw BIOS ROM size values and exposes accurate property values.
    /// The tests use representative sample data to validate expected outcomes for various BIOS ROM
    /// configurations.</remarks>
    public sealed class BiosExtendedRomSizeTests
    {
        /// <summary>
        /// Verifies that the Size property of the BiosExtendedRomSize struct returns the value of the lower fourteen
        /// bits of the underlying ushort.
        /// </summary>
        /// <remarks>This test ensures that only the least significant fourteen bits are considered when
        /// retrieving the Size value, regardless of the higher bits in the input.</remarks>
        [Fact]
        public void Size_ReturnsLowerFourteenBits()
        {
            BiosExtendedRomSize size = new(unchecked((ushort)0b01_0011001100110011));
            Assert.Equal(0b0011001100110011 & 0x3FFF, size.Size);
        }

        /// <summary>
        /// Verifies that the properties of a BiosExtendedRomSize instance return the expected values for the given
        /// input parameters.
        /// </summary>
        /// <remarks>This is a parameterized unit test that uses data from SampleData to validate the
        /// behavior of the BiosExtendedRomSize properties.</remarks>
        /// <param name="raw">The raw ushort value used to construct the BiosExtendedRomSize instance.</param>
        /// <param name="expectedUnitValue">The expected unit value, as a byte, to compare against the Unit property of the instance.</param>
        /// <param name="expectedRecognized">The expected value indicating whether the unit is recognized, to compare against the IsUnitRecognized
        /// property.</param>
        /// <param name="expectedBytes">The expected number of bytes, or null if not applicable, to compare against the Bytes property.</param>
        [Theory]
        [MemberData(nameof(SampleData))]
        public void Properties_ReturnExpectedValues(ushort raw, byte expectedUnitValue, bool expectedRecognized, ulong? expectedBytes)
        {
            BiosExtendedRomSize value = new(raw);
            BiosRomUnit expectedUnit = (BiosRomUnit)expectedUnitValue;
            Assert.Equal(raw, value.Raw);
            Assert.Equal(expectedUnit, value.Unit);
            Assert.Equal(expectedRecognized, value.IsUnitRecognized);
            Assert.Equal(expectedBytes, value.Bytes);
        }

        /// <summary>
        /// Gets a collection of sample BIOS ROM data sets for use in parameterized unit tests.
        /// </summary>
        /// <remarks>Each data set consists of a 16-bit identifier, a unit value as a byte, a flag
        /// indicating validity, and an optional ROM size in bytes. This property is intended to support test scenarios
        /// that require representative BIOS ROM configurations.</remarks>
        public static TheoryData<ushort, byte, bool, ulong?> SampleData { get; } = new()
        {
            { 0x0002, (byte)BiosRomUnit.MB, true, 2UL * 1024 * 1024 },
            { 0x4003, (byte)BiosRomUnit.GB, true, 3UL * 1024 * 1024 * 1024 },
        };
    }
}
