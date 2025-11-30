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
    /// Contains unit tests for the SmbiosTablePosition class.
    /// </summary>
    public sealed class SmbiosTablePositionTests
    {
        /// <summary>
        /// Verifies that the SmbiosTablePosition constructor correctly sets the Offset and Length properties based on
        /// the provided arguments.
        /// </summary>
        [Fact]
        public void Constructor_SetsOffsetAndLength()
        {
            SmbiosTablePosition position = new(42, 12);
            Assert.Equal(42, position.Offset);
            Assert.Equal((byte)12, position.Length);
        }
    }
}
