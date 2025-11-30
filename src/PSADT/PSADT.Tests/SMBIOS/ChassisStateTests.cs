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
    /// Contains unit tests for verifying the values and behavior of the ChassisState enumeration.
    /// </summary>
    /// <remarks>Use this test class to ensure that changes to the ChassisState enumeration do not break
    /// protocol compatibility or serialization requirements by altering the defined byte values. Update the tests if
    /// the specification for ChassisState values changes.</remarks>
    public sealed class ChassisStateTests
    {
        /// <summary>
        /// Verifies that the underlying byte values of the ChassisState enumeration members match the expected
        /// specification.
        /// </summary>
        /// <remarks>This test ensures that changes to the ChassisState enumeration do not alter the
        /// defined byte values, which may be required for protocol compatibility or serialization. Update this test if
        /// the specification for ChassisState values changes.</remarks>
        [Fact]
        public void EnumValues_MatchSpecification()
        {
            Assert.Equal<byte>(0x01, (byte)ChassisState.Other);
            Assert.Equal<byte>(0x02, (byte)ChassisState.Unknown);
            Assert.Equal<byte>(0x03, (byte)ChassisState.Safe);
            Assert.Equal<byte>(0x04, (byte)ChassisState.Warning);
            Assert.Equal<byte>(0x05, (byte)ChassisState.Critical);
            Assert.Equal<byte>(0x06, (byte)ChassisState.NonRecoverable);
        }
    }
}
