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
    /// Decoded view of System Enclosure Type/Lock byte (Type 3, offset 0x05).
    /// </summary>
    public readonly record struct SystemEnclosureTypeAndLock
    {
        /// <summary>
        /// Initializes a new instance of the SystemEnclosureTypeAndLock structure using the specified raw byte value.
        /// </summary>
        /// <param name="raw">The raw byte value representing the enclosure type and lock state.</param>
        public SystemEnclosureTypeAndLock(byte raw)
        {
            Raw = raw;
        }

        /// <summary>
        /// Chassis type (bits 6:0).
        /// </summary>
        public ChassisType Type => (ChassisType)(Raw & 0x7F);

        /// <summary>
        /// True when chassis lock is present (bit 7).
        /// </summary>
        public bool ChassisLockPresent => (Raw & 0x80) != 0;

        /// <summary>
        /// Gets the raw byte value represented by this instance.
        /// </summary>
        public byte Raw { get; }
    }
}
