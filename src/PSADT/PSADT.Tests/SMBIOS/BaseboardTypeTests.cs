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
    /// Contains unit tests that verify the values of the BaseboardType enumeration against their expected byte
    /// representations.
    /// </summary>
    /// <remarks>Use this test class to ensure that changes to the BaseboardType enumeration do not introduce
    /// breaking changes to its underlying values. Failing tests may indicate that the enumeration no longer conforms to
    /// its intended specification.</remarks>
    public sealed class BaseboardTypeTests
    {
        /// <summary>
        /// Verifies that each value of the BaseboardType enumeration matches its expected byte representation.
        /// </summary>
        /// <remarks>This test ensures that the underlying byte values assigned to each BaseboardType
        /// enumeration member conform to the specification. Changes to the enumeration values may cause this test to
        /// fail, indicating a breaking change in the enum's definition.</remarks>
        [Fact]
        public void EnumValues_MatchSpecification()
        {
            Assert.Equal<byte>(0x01, (byte)BaseboardType.Unknown);
            Assert.Equal<byte>(0x02, (byte)BaseboardType.Other);
            Assert.Equal<byte>(0x03, (byte)BaseboardType.ServerBlade);
            Assert.Equal<byte>(0x04, (byte)BaseboardType.ConnectivitySwitch);
            Assert.Equal<byte>(0x05, (byte)BaseboardType.SystemManagementModule);
            Assert.Equal<byte>(0x06, (byte)BaseboardType.ProcessorModule);
            Assert.Equal<byte>(0x07, (byte)BaseboardType.IoModule);
            Assert.Equal<byte>(0x08, (byte)BaseboardType.MemoryModule);
            Assert.Equal<byte>(0x09, (byte)BaseboardType.DaughterBoard);
            Assert.Equal<byte>(0x0A, (byte)BaseboardType.Motherboard);
            Assert.Equal<byte>(0x0B, (byte)BaseboardType.ProcessorMemoryModule);
            Assert.Equal<byte>(0x0C, (byte)BaseboardType.ProcessorIoModule);
            Assert.Equal<byte>(0x0D, (byte)BaseboardType.InterconnectBoard);
        }
    }
}
