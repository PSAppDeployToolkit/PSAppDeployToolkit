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
    /// Contains unit tests for verifying the values and behavior of the RackType enumeration.
    /// </summary>
    /// <remarks>Use this test class to ensure that changes to the RackType enumeration do not break
    /// compatibility with expected values or external systems that depend on specific enumeration
    /// assignments.</remarks>
    public sealed class RackTypeTests
    {
        /// <summary>
        /// Verifies that the underlying byte values of the RackType enumeration members match the expected
        /// specification.
        /// </summary>
        /// <remarks>This test ensures that changes to the RackType enumeration do not alter the assigned
        /// byte values for Unspecified and OU. Maintaining these values is important for compatibility with external
        /// systems or persisted data that rely on specific enumeration values.</remarks>
        [Fact]
        public void EnumValues_MatchSpecification()
        {
            Assert.Equal<byte>(0x00, (byte)RackType.Unspecified);
            Assert.Equal<byte>(0x01, (byte)RackType.OU);
        }
    }
}
