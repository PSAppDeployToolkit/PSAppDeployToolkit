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
    /// Decoded view with raw access and helpers for computing size.
    /// </summary>
    public readonly record struct BiosExtendedRomSize
    {
        /// <summary>
        /// Initializes a new instance of the BiosExtendedRomSize structure with the specified raw value.
        /// </summary>
        /// <param name="raw">The raw 16-bit value representing the BIOS extended ROM size.</param>
        public BiosExtendedRomSize(ushort raw)
        {
            Raw = raw;
        }

        /// <summary>
        /// The 14-bit size value (bits 13:0).
        /// </summary>
        public int Size => Raw & 0x3FFF;

        /// <summary>
        /// Unit selector (bits 15:14). 00 = MB, 01 = GB, others reserved.
        /// </summary>
        public BiosRomUnit Unit => (BiosRomUnit)((Raw >> 14) & 0x03);

        /// <summary>
        /// True when Unit is MB or GB.
        /// </summary>
        public bool IsUnitRecognized => Unit is BiosRomUnit.MB or BiosRomUnit.GB;

        /// <summary>
        /// Total byte size computed from Size and Unit. Null when unit is reserved.
        /// </summary>
        public ulong? Bytes => Unit switch
        {
            BiosRomUnit.GB => (ulong)Size * 1024UL * 1024UL * 1024UL,
            BiosRomUnit.MB => (ulong)Size * 1024UL * 1024UL,
            _ => null,
        };

        /// <summary>
        /// Gets the raw, unprocessed value as a 16-bit unsigned integer.
        /// </summary>
        public ushort Raw { get; }
    }
}
