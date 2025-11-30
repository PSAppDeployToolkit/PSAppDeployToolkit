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
    /// Contains unit tests for verifying that the Type and ChassisLockPresent properties of the
    /// SystemEnclosureTypeAndLock class return the correct values for given raw input data.
    /// </summary>
    public sealed class SystemEnclosureTypeAndLockTests
    {
        /// <summary>
        /// Verifies that the Type and ChassisLockPresent properties of SystemEnclosureTypeAndLock return the expected
        /// values for the specified raw input.
        /// </summary>
        /// <param name="raw">The raw byte value representing the encoded chassis type and lock state to test.</param>
        /// <param name="expectedTypeValue">The expected byte value corresponding to the chassis type extracted from the raw input.</param>
        /// <param name="expectedLock">The expected value indicating whether the chassis lock is present. Set to <see langword="true"/> if the lock
        /// is expected; otherwise, <see langword="false"/>.</param>
        [Theory]
        [InlineData(0x80 | (byte)ChassisType.Desktop, (byte)ChassisType.Desktop, true)]
        [InlineData((byte)ChassisType.MiniTower, (byte)ChassisType.MiniTower, false)]
        [InlineData(0x80 | (byte)ChassisType.BladeEnclosure, (byte)ChassisType.BladeEnclosure, true)]
        public void Properties_ReturnExpectedValues(byte raw, byte expectedTypeValue, bool expectedLock)
        {
            SystemEnclosureTypeAndLock typeAndLock = new(raw);

            Assert.Equal((ChassisType)expectedTypeValue, typeAndLock.Type);
            Assert.Equal(expectedLock, typeAndLock.ChassisLockPresent);
        }
    }
}
