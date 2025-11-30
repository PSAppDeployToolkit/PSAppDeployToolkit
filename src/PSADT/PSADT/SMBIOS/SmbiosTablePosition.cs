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

namespace PSADT.SMBIOS
{
    /// <summary>
    /// Represents the position of a System Management BIOS (SMBIOS) table within a data structure.
    /// </summary>
    /// <remarks>This class is used to define the location and size of an SMBIOS table, which is essential for
    /// parsing and interpreting SMBIOS data. It is intended for internal use within the system's data processing
    /// components.</remarks>
    internal sealed record SmbiosTablePosition
    {
        /// <summary>
        /// Represents the position of an SMBIOS table within a data structure.
        /// </summary>
        /// <param name="offset">The offset position of the SMBIOS table within the data structure. Must be non-negative.</param>
        /// <param name="length">The length of the SMBIOS table. Must be a positive value.</param>
        internal SmbiosTablePosition(int offset, byte length)
        {
            Offset = offset;
            Length = length;
        }

        /// <summary>
        /// Represents the offset value used internally for calculations or data manipulation.
        /// </summary>
        /// <remarks>This field is intended for internal use only and should not be accessed directly by
        /// external components. It is used to maintain the state or position within a data structure or
        /// process.</remarks>
        internal readonly int Offset;

        /// <summary>
        /// Represents the length of a specific data structure or element.
        /// </summary>
        internal readonly byte Length;
    }
}
