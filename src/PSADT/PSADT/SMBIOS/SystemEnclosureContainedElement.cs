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
    /// Decoded view of a System Enclosure (Type 3) Contained Element record (length >= 3).
    /// </summary>
    public readonly record struct SystemEnclosureContainedElement
    {
        /// <summary>
        /// Initializes a new instance of the SystemEnclosureContainedElement class with the specified raw type and
        /// value range.
        /// </summary>
        /// <param name="rawType">The raw type identifier for the contained element.</param>
        /// <param name="rawMinimum">The minimum raw value supported by the contained element.</param>
        /// <param name="rawMaximum">The maximum raw value supported by the contained element.</param>
        public SystemEnclosureContainedElement(byte rawType, byte rawMinimum, byte rawMaximum)
        {
            RawType = rawType;
            RawMinimum = rawMinimum;
            RawMaximum = rawMaximum;

        }
        /// <summary>
        /// True when RawType encodes an SMBIOS structure type (bit 7 == 0).
        /// False when it encodes an SMBIOS Baseboard Type (bit 7 == 1).
        /// </summary>
        public bool IsType => (RawType & 0x80) == 0;

        /// <summary>
        /// Lower7-bit type code (interpretation depends on IsType).
        /// </summary>
        public byte TypeCode => (byte)(RawType & 0x7F);

        /// <summary>
        /// SMBIOS structure type value when IsType is true; otherwise null.
        /// </summary>
        public SmbiosType? Type => IsType ? (SmbiosType)TypeCode : null;

        /// <summary>
        /// SMBIOS baseboard type value when IsType is false; otherwise null.
        /// </summary>
        public BaseboardType? BaseboardType => !IsType ? (BaseboardType)TypeCode : null;

        /// <summary>
        /// Normalized minimum (null when reserved value 0xFF is used).
        /// </summary>
        public byte? Minimum => RawMinimum != 0xFF ? RawMinimum : null;

        /// <summary>
        /// Normalized maximum (null when reserved value 0 is used).
        /// </summary>
        public byte? Maximum => RawMaximum != 0 ? RawMaximum : null;

        /// <summary>
        /// Returns true when Minimum/Maximum are within spec-defined ranges.
        /// </summary>
        public bool IsRangeValid => (RawMinimum <= 0xFE) && (RawMaximum >= 0x01);

        /// <summary>
        /// Gets the raw byte value representing the underlying type.
        /// </summary>
        public byte RawType { get; }

        /// <summary>
        /// Gets the raw minimum value as provided by the underlying data source.
        /// </summary>
        public byte RawMinimum { get; }

        /// <summary>
        /// Gets the raw maximum value as reported by the underlying device or data source.
        /// </summary>
        public byte RawMaximum { get; }
    }
}
