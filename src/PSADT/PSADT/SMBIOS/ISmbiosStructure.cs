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
    /// Base interface for all SMBIOS structure types.
    /// </summary>
    public interface ISmbiosStructure
    {
        /// <summary>
        /// Gets the SMBIOS structure type.
        /// </summary>
        SmbiosType Type { get; }

        /// <summary>
        /// Gets the length of the structure in bytes.
        /// </summary>
        byte Length { get; }

        /// <summary>
        /// Gets the handle (unique identifier) for this structure.
        /// </summary>
        ushort Handle { get; }
    }
}
