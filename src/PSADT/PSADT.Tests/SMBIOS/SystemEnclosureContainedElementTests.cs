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
    /// Contains unit tests for the SystemEnclosureContainedElement class, verifying its behavior when representing
    /// structure-specific and baseboard-specific information.
    /// </summary>
    /// <remarks>These tests ensure that SystemEnclosureContainedElement correctly exposes its properties
    /// based on the type of element it represents, including type identification, value ranges, and baseboard type
    /// handling.</remarks>
    public sealed class SystemEnclosureContainedElementTests
    {
        /// <summary>
        /// Verifies that the TypeElement exposes structure-specific information as expected.
        /// </summary>
        /// <remarks>This test ensures that the SystemEnclosureContainedElement correctly reports its
        /// type, minimum and maximum values, and range validity for a MemoryDevice structure.</remarks>
        [Fact]
        public void TypeElement_ExposesStructureSpecificInformation()
        {
            SystemEnclosureContainedElement element = new((byte)SmbiosType.MemoryDevice, 0x01, 0x05);
            Assert.True(element.IsType);
            Assert.Equal(SmbiosType.MemoryDevice, element.Type);
            Assert.Null(element.BaseboardType);
            Assert.Equal<byte?>(1, element.Minimum);
            Assert.Equal<byte?>(5, element.Maximum);
            Assert.True(element.IsRangeValid);
        }

        /// <summary>
        /// Verifies that the SystemEnclosureContainedElement correctly exposes baseboard-specific information when
        /// initialized with a baseboard type value.
        /// </summary>
        /// <remarks>This test ensures that the BaseboardType property returns the expected value and that
        /// other properties reflect the absence of a general type or range when the element represents a
        /// baseboard.</remarks>
        [Fact]
        public void BaseboardTypeElement_ExposesBaseboardSpecificInformation()
        {
            SystemEnclosureContainedElement element = new(0x80 | (byte)BaseboardType.ServerBlade, 0xFF, 0x00);
            Assert.False(element.IsType);
            Assert.Null(element.Type);
            Assert.Equal(BaseboardType.ServerBlade, element.BaseboardType);
            Assert.Null(element.Minimum);
            Assert.Null(element.Maximum);
            Assert.False(element.IsRangeValid);
        }
    }
}
