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
    /// Represents the system wake-up type as defined in SMBIOS specification.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1008:Enums should have zero value", Justification = "There is no zero value for this within the SMBIOS specification.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1028:Enum Storage should be Int32", Justification = "This enum is correctly typed as per the SMBIOS specification")]
    public enum SystemWakeUpType : byte
    {
        /// <summary>
        /// Other
        /// </summary>
        Other = 0x01,

        /// <summary>
        /// Unknown
        /// </summary>
        Unknown = 0x02,

        /// <summary>
        /// APM Timer
        /// </summary>
        ApmTimer = 0x03,

        /// <summary>
        /// Modem Ring
        /// </summary>
        ModemRing = 0x04,

        /// <summary>
        /// LAN Remote
        /// </summary>
        LanRemote = 0x05,

        /// <summary>
        /// Power Switch
        /// </summary>
        PowerSwitch = 0x06,

        /// <summary>
        /// PCI PME#
        /// </summary>
        PciPme = 0x07,

        /// <summary>
        /// AC Power Restored
        /// </summary>
        AcPowerRestored = 0x08
    }
}
