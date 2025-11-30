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
    /// Contains unit tests that verify the values of the SystemWakeUpType enumeration members against the expected
    /// specification.
    /// </summary>
    /// <remarks>Use this test class to ensure that changes to the SystemWakeUpType enumeration do not
    /// inadvertently modify the defined byte values, which may be required for interoperability or serialization
    /// scenarios.</remarks>
    public sealed class SystemWakeUpTypeTests
    {
        /// <summary>
        /// Verifies that the underlying byte values of the SystemWakeUpType enumeration members match the expected
        /// specification.
        /// </summary>
        /// <remarks>This test ensures that any changes to the SystemWakeUpType enumeration do not alter
        /// the defined byte values, which may be required for interoperability or serialization scenarios.</remarks>
        [Fact]
        public void EnumValues_MatchSpecification()
        {
            Assert.Equal<byte>(0x01, (byte)SystemWakeUpType.Other);
            Assert.Equal<byte>(0x02, (byte)SystemWakeUpType.Unknown);
            Assert.Equal<byte>(0x03, (byte)SystemWakeUpType.ApmTimer);
            Assert.Equal<byte>(0x04, (byte)SystemWakeUpType.ModemRing);
            Assert.Equal<byte>(0x05, (byte)SystemWakeUpType.LanRemote);
            Assert.Equal<byte>(0x06, (byte)SystemWakeUpType.PowerSwitch);
            Assert.Equal<byte>(0x07, (byte)SystemWakeUpType.PciPme);
            Assert.Equal<byte>(0x08, (byte)SystemWakeUpType.AcPowerRestored);
        }
    }
}
