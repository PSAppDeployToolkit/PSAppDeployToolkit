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
    /// Contains unit tests that verify the values of the ChassisType enumeration conform to the expected specification.
    /// </summary>
    /// <remarks>Use this test class to ensure that any changes to the ChassisType enum do not inadvertently
    /// alter the assigned byte values. This helps maintain compatibility with external systems or standards that rely
    /// on specific enumeration values.</remarks>
    public sealed class ChassisTypeTests
    {
        /// <summary>
        /// Verifies that each value of the ChassisType enumeration matches its expected byte value as specified by the
        /// standard.
        /// </summary>
        /// <remarks>This test ensures that the underlying byte values of all ChassisType enum members
        /// conform to the documented specification. If the enum definitions are modified or regenerated, this test will
        /// detect any unintended changes to their assigned values.</remarks>
        [Fact]
        public void EnumValues_MatchSpecification()
        {
            Assert.Equal<byte>(0x01, (byte)ChassisType.Other);
            Assert.Equal<byte>(0x02, (byte)ChassisType.Unknown);
            Assert.Equal<byte>(0x03, (byte)ChassisType.Desktop);
            Assert.Equal<byte>(0x04, (byte)ChassisType.LowProfileDesktop);
            Assert.Equal<byte>(0x05, (byte)ChassisType.PizzaBox);
            Assert.Equal<byte>(0x06, (byte)ChassisType.MiniTower);
            Assert.Equal<byte>(0x07, (byte)ChassisType.Tower);
            Assert.Equal<byte>(0x08, (byte)ChassisType.Portable);
            Assert.Equal<byte>(0x09, (byte)ChassisType.Laptop);
            Assert.Equal<byte>(0x0A, (byte)ChassisType.Notebook);
            Assert.Equal<byte>(0x0B, (byte)ChassisType.HandHeld);
            Assert.Equal<byte>(0x0C, (byte)ChassisType.DockingStation);
            Assert.Equal<byte>(0x0D, (byte)ChassisType.AllInOne);
            Assert.Equal<byte>(0x0E, (byte)ChassisType.SubNotebook);
            Assert.Equal<byte>(0x0F, (byte)ChassisType.SpaceSaving);
            Assert.Equal<byte>(0x10, (byte)ChassisType.LunchBox);
            Assert.Equal<byte>(0x11, (byte)ChassisType.MainServerChassis);
            Assert.Equal<byte>(0x12, (byte)ChassisType.ExpansionChassis);
            Assert.Equal<byte>(0x13, (byte)ChassisType.SubChassis);
            Assert.Equal<byte>(0x14, (byte)ChassisType.BusExpansionChassis);
            Assert.Equal<byte>(0x15, (byte)ChassisType.PeripheralChassis);
            Assert.Equal<byte>(0x16, (byte)ChassisType.RaidChassis);
            Assert.Equal<byte>(0x17, (byte)ChassisType.RackMountChassis);
            Assert.Equal<byte>(0x18, (byte)ChassisType.SealedCasePc);
            Assert.Equal<byte>(0x19, (byte)ChassisType.MultiSystemChassis);
            Assert.Equal<byte>(0x1A, (byte)ChassisType.CompactPci);
            Assert.Equal<byte>(0x1B, (byte)ChassisType.AdvancedTca);
            Assert.Equal<byte>(0x1C, (byte)ChassisType.Blade);
            Assert.Equal<byte>(0x1D, (byte)ChassisType.BladeEnclosure);
            Assert.Equal<byte>(0x1E, (byte)ChassisType.Tablet);
            Assert.Equal<byte>(0x1F, (byte)ChassisType.Convertible);
            Assert.Equal<byte>(0x20, (byte)ChassisType.Detachable);
            Assert.Equal<byte>(0x21, (byte)ChassisType.IoTGateway);
            Assert.Equal<byte>(0x22, (byte)ChassisType.EmbeddedPc);
            Assert.Equal<byte>(0x23, (byte)ChassisType.MiniPc);
            Assert.Equal<byte>(0x24, (byte)ChassisType.StickPc);
        }
    }
}
