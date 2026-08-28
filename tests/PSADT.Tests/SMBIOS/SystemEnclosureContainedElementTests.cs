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
using Xunit;

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

        /// <summary>
        /// Verifies that the raw bytes are kept exactly as the firmware wrote them, alongside the
        /// interpretations built from them.
        /// </summary>
        /// <remarks>
        /// The interpreted values throw information away deliberately - a minimum of 0xFF means "not
        /// specified" and is reported as nothing, and the top bit of the type is stripped to leave the
        /// code. Keeping the raw bytes is what lets a caller looking at an unfamiliar machine see what
        /// the firmware actually said rather than only what was made of it.
        /// </remarks>
        [Fact]
        public void RawValues_AreKeptExactlyAsTheFirmwareWroteThem()
        {
            // Act: the top bit set marks a baseboard type rather than an SMBIOS one
            SystemEnclosureContainedElement element = new(0x83, 0xFF, 0x00);

            // Assert: the raw bytes survive untouched
            Assert.Equal(0x83, element.RawType);
            Assert.Equal(0xFF, element.RawMinimum);
            Assert.Equal(0x00, element.RawMaximum);

            // Assert: while the interpretations drop what they are meant to
            Assert.Equal(0x03, element.TypeCode);
            Assert.False(element.IsType);
            Assert.Null(element.Minimum);
            Assert.Null(element.Maximum);
        }

        /// <summary>
        /// Verifies that the type code is the raw type with its top bit removed, since that bit says
        /// which of the two enumerations the code should be read against rather than being part of it.
        /// </summary>
        /// <param name="rawType">The byte the firmware wrote.</param>
        /// <param name="expectedTypeCode">The code it carries.</param>
        /// <param name="expectedIsType">Whether it names an SMBIOS type rather than a baseboard one.</param>
        [Theory]
        [InlineData(0x00, 0x00, true)]
        [InlineData(0x03, 0x03, true)]
        [InlineData(0x7F, 0x7F, true)]
        [InlineData(0x80, 0x00, false)]
        [InlineData(0x83, 0x03, false)]
        [InlineData(0xFF, 0x7F, false)]
        public void TypeCode_StripsTheFlagBitFromTheRawType(byte rawType, byte expectedTypeCode, bool expectedIsType)
        {
            // Act
            SystemEnclosureContainedElement element = new(rawType, 0x01, 0x02);

            // Assert
            Assert.Equal(rawType, element.RawType);
            Assert.Equal(expectedTypeCode, element.TypeCode);
            Assert.Equal(expectedIsType, element.IsType);
        }
    }
}
