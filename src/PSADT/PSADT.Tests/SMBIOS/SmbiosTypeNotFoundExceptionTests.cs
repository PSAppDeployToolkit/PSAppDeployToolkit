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

using System;
using PSADT.SMBIOS;

namespace PSADT.Tests.SMBIOS
{
    /// <summary>
    /// Contains unit tests for the SmbiosTypeNotFoundException class.
    /// </summary>
    /// <remarks>This class verifies the behavior of SmbiosTypeNotFoundException, ensuring that its
    /// constructors and properties function as expected.</remarks>
    public sealed class SmbiosTypeNotFoundExceptionTests
    {
        /// <summary>
        /// Verifies that the SmbiosTypeNotFoundException constructor correctly sets the exception message and type
        /// properties.
        /// </summary>
        /// <remarks>This test ensures that when an instance of SmbiosTypeNotFoundException is created
        /// with a specific SmbiosType, the resulting exception contains the expected type in its Message property and
        /// the Type property is set accordingly.</remarks>
        [Fact]
        public void Constructor_SetsMessageAndType()
        {
            SmbiosTypeNotFoundException ex = new(SmbiosType.SystemInformation);
            Assert.Contains("SystemInformation", ex.Message, StringComparison.Ordinal);
            Assert.Equal(SmbiosType.SystemInformation, ex.Type);
        }
    }
}
