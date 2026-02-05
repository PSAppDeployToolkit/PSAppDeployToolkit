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
    /// Represents BIOS characteristics extension byte 2.
    /// Bit 7 is reserved for future use.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1028:Enum Storage should be Int32", Justification = "This enum is correctly typed as per the SMBIOS specification")]
    [Flags]
    public enum FirmwareCharacteristicsExtensionByte2 : byte
    {
        /// <summary>
        /// BIOS boot specification is supported
        /// </summary>
        BiosBootSpecificationSupported = 1 << 0,

        /// <summary>
        /// Function key-initiated network service boot is supported
        /// </summary>
        FunctionKeyNetworkBootSupported = 1 << 1,

        /// <summary>
        /// Enable targeted content distribution
        /// </summary>
        TargetedContentDistribution = 1 << 2,

        /// <summary>
        /// UEFI specification is supported
        /// </summary>
        UefiSupported = 1 << 3,

        /// <summary>
        /// SMBIOS table describes a virtual machine
        /// </summary>
        VirtualMachine = 1 << 4,

        /// <summary>
        /// Manufacturing mode is supported. (Manufacturing mode is a special boot mode, 
        /// not normally available to end users, that modifies platform firmware features 
        /// and settings for use while the computer is being manufactured and tested.)
        /// </summary>
        ManufacturingModeSupported = 1 << 5,

        /// <summary>
        /// Manufacturing mode is enabled.
        /// </summary>
        ManufacturingModeEnabled = 1 << 6,
    }
}
