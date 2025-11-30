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
    /// Represents baseboard types as defined in SMBIOS specification.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1008:Enums should have zero value", Justification = "There is no zero value for this within the SMBIOS specification.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1028:Enum Storage should be Int32", Justification = "This enum is correctly typed as per the SMBIOS specification")]
    public enum BaseboardType : byte
    {
        /// <summary>
        /// Unknown
        /// </summary>
        Unknown = 0x01,

        /// <summary>
        /// Other
        /// </summary>
        Other = 0x02,

        /// <summary>
        /// Server Blade
        /// </summary>
        ServerBlade = 0x03,

        /// <summary>
        /// Connectivity Switch
        /// </summary>
        ConnectivitySwitch = 0x04,

        /// <summary>
        /// System Management Module
        /// </summary>
        SystemManagementModule = 0x05,

        /// <summary>
        /// Processor Module
        /// </summary>
        ProcessorModule = 0x06,

        /// <summary>
        /// I/O Module
        /// </summary>
        IoModule = 0x07,

        /// <summary>
        /// Memory Module
        /// </summary>
        MemoryModule = 0x08,

        /// <summary>
        /// Daughter Board
        /// </summary>
        DaughterBoard = 0x09,

        /// <summary>
        /// Motherboard (includes processor, memory, and I/O)
        /// </summary>
        Motherboard = 0x0A,

        /// <summary>
        /// Processor/Memory Module
        /// </summary>
        ProcessorMemoryModule = 0x0B,

        /// <summary>
        /// Processor/I/O Module
        /// </summary>
        ProcessorIoModule = 0x0C,

        /// <summary>
        /// Interconnect Board
        /// </summary>
        InterconnectBoard = 0x0D
    }
}
