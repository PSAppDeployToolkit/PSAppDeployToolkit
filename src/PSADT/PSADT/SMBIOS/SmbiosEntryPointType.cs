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
    /// Represents the type of SMBIOS entry point structure found in the system.
    /// </summary>
    /// <remarks>
    /// SMBIOS data can be accessed through different entry point formats depending
    /// on the SMBIOS version and system implementation.
    /// </remarks>
    internal enum SmbiosEntryPointType : byte
    {
        /// <summary>
        /// Entry point type could not be determined.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// SMBIOS 2.x entry point structure.
        /// </summary>
        /// <remarks>
        /// Uses "_SM_" signature and 32-bit addressing.
        /// Supports SMBIOS versions 2.0 through 2.8.
        /// Entry point structure is 31 bytes.
        /// </remarks>
        Smbios2x = 1,

        /// <summary>
        /// SMBIOS 3.x entry point structure.
        /// </summary>
        /// <remarks>
        /// Uses "_SM3_" signature and 64-bit addressing.
        /// Supports SMBIOS versions 3.0 and later.
        /// Entry point structure is 24 bytes.
        /// </remarks>
        Smbios3x = 2
    }
}
