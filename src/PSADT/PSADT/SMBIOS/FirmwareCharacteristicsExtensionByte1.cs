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

namespace PSADT.SMBIOS
{
    /// <summary>
    /// Represents BIOS characteristics extension byte 1.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1028:Enum Storage should be Int32", Justification = "This enum is correctly typed as per the SMBIOS specification")]
    [Flags]
    public enum FirmwareCharacteristicsExtensionByte1 : byte
    {
        /// <summary>
        /// ACPI is supported
        /// </summary>
        AcpiSupported = 1 << 0,

        /// <summary>
        /// USB Legacy is supported
        /// </summary>
        UsbLegacySupported = 1 << 1,

        /// <summary>
        /// AGP is supported
        /// </summary>
        AgpSupported = 1 << 2,

        /// <summary>
        /// I2O boot is supported
        /// </summary>
        I2OBootSupported = 1 << 3,

        /// <summary>
        /// LS-120 SuperDisk boot is supported
        /// </summary>
        Ls120BootSupported = 1 << 4,

        /// <summary>
        /// ATAPI ZIP drive boot is supported
        /// </summary>
        AtapiZipBootSupported = 1 << 5,

        /// <summary>
        /// 1394 boot is supported
        /// </summary>
        Ieee1394BootSupported = 1 << 6,

        /// <summary>
        /// Smart battery is supported
        /// </summary>
        SmartBatterySupported = 1 << 7,
    }
}
