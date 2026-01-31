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
    /// Contains unit tests for verifying the values and behavior of the ChassisSecurityStatus enumeration.
    /// </summary>
    /// <remarks>This class is intended to ensure that the ChassisSecurityStatus enum remains consistent with
    /// protocol specifications. It is typically used in automated test suites to detect unintended changes to the
    /// enum's assigned values.</remarks>
    public sealed class ChassisSecurityStatusTests
    {
        /// <summary>
        /// Verifies that the byte values of the ChassisSecurityStatus enumeration members match the expected
        /// specification.
        /// </summary>
        /// <remarks>This test ensures that changes to the ChassisSecurityStatus enum do not alter the
        /// assigned byte values, which may be required for protocol compatibility or serialization.</remarks>
        [Fact]
        public void EnumValues_MatchSpecification()
        {
            Assert.Equal<byte>(0x01, (byte)ChassisSecurityStatus.Other);
            Assert.Equal<byte>(0x02, (byte)ChassisSecurityStatus.Unknown);
            Assert.Equal<byte>(0x03, (byte)ChassisSecurityStatus.None);
            Assert.Equal<byte>(0x04, (byte)ChassisSecurityStatus.ExternalInterfaceLockedOut);
            Assert.Equal<byte>(0x05, (byte)ChassisSecurityStatus.ExternalInterfaceEnabled);
        }
    }
}
