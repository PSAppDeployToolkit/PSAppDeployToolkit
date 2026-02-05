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
    /// Contains unit tests for verifying the correctness and stability of the BiosRomUnit enumeration values.
    /// </summary>
    /// <remarks>Use this test class to ensure that changes to the BiosRomUnit enumeration do not
    /// inadvertently alter the assigned byte values, which may impact serialization, interoperability, or dependent
    /// components.</remarks>
    public sealed class BiosRomUnitTests
    {
        /// <summary>
        /// Verifies that the underlying byte values of the BiosRomUnit enumeration members match the expected
        /// specification.
        /// </summary>
        /// <remarks>This test ensures that the values assigned to BiosRomUnit.MB and BiosRomUnit.GB
        /// remain consistent with their intended specification. Changes to these values may affect serialization,
        /// interoperability, or other components that depend on the exact numeric values of the enumeration.</remarks>
        [Fact]
        public void EnumValues_MatchSpecification()
        {
            Assert.Equal(0, (byte)BiosRomUnit.MB);
            Assert.Equal(1, (byte)BiosRomUnit.GB);
        }
    }
}
