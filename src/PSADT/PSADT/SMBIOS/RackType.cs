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
    /// Represents the rack type unit for System Enclosure (Type 3) as per SMBIOS spec.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1028:Enum Storage should be Int32", Justification = "This enum is correctly typed as per the SMBIOS specification")]
    public enum RackType : byte
    {
        /// <summary>
        /// Unspecified
        /// </summary>
        Unspecified = 0x00,

        /// <summary>
        /// Rack Unit (U/OU)
        /// </summary>
        OU = 0x01
    }
}
