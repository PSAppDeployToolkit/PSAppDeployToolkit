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
    /// Represents chassis types as defined in SMBIOS specification.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1008:Enums should have zero value", Justification = "There is no zero value for this within the SMBIOS specification.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1028:Enum Storage should be Int32", Justification = "This enum is correctly typed as per the SMBIOS specification")]
    public enum ChassisType : byte
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
        /// Desktop
        /// </summary>
        Desktop = 0x03,

        /// <summary>
        /// Low Profile Desktop
        /// </summary>
        LowProfileDesktop = 0x04,

        /// <summary>
        /// Pizza Box
        /// </summary>
        PizzaBox = 0x05,

        /// <summary>
        /// Mini Tower
        /// </summary>
        MiniTower = 0x06,

        /// <summary>
        /// Tower
        /// </summary>
        Tower = 0x07,

        /// <summary>
        /// Portable
        /// </summary>
        Portable = 0x08,

        /// <summary>
        /// Laptop
        /// </summary>
        Laptop = 0x09,

        /// <summary>
        /// Notebook
        /// </summary>
        Notebook = 0x0A,

        /// <summary>
        /// Hand Held
        /// </summary>
        HandHeld = 0x0B,

        /// <summary>
        /// Docking Station
        /// </summary>
        DockingStation = 0x0C,

        /// <summary>
        /// All in One
        /// </summary>
        AllInOne = 0x0D,

        /// <summary>
        /// Sub Notebook
        /// </summary>
        SubNotebook = 0x0E,

        /// <summary>
        /// Space-saving
        /// </summary>
        SpaceSaving = 0x0F,

        /// <summary>
        /// Lunch Box
        /// </summary>
        LunchBox = 0x10,

        /// <summary>
        /// Main Server Chassis
        /// </summary>
        MainServerChassis = 0x11,

        /// <summary>
        /// Expansion Chassis
        /// </summary>
        ExpansionChassis = 0x12,

        /// <summary>
        /// SubChassis
        /// </summary>
        SubChassis = 0x13,

        /// <summary>
        /// Bus Expansion Chassis
        /// </summary>
        BusExpansionChassis = 0x14,

        /// <summary>
        /// Peripheral Chassis
        /// </summary>
        PeripheralChassis = 0x15,

        /// <summary>
        /// RAID Chassis
        /// </summary>
        RaidChassis = 0x16,

        /// <summary>
        /// Rack Mount Chassis
        /// </summary>
        RackMountChassis = 0x17,

        /// <summary>
        /// Sealed-case PC
        /// </summary>
        SealedCasePc = 0x18,

        /// <summary>
        /// Multi-system chassis
        /// </summary>
        MultiSystemChassis = 0x19,

        /// <summary>
        /// Compact PCI
        /// </summary>
        CompactPci = 0x1A,

        /// <summary>
        /// Advanced TCA
        /// </summary>
        AdvancedTca = 0x1B,

        /// <summary>
        /// Blade
        /// </summary>
        Blade = 0x1C,

        /// <summary>
        /// Blade Enclosure
        /// </summary>
        BladeEnclosure = 0x1D,

        /// <summary>
        /// Tablet
        /// </summary>
        Tablet = 0x1E,

        /// <summary>
        /// Convertible
        /// </summary>
        Convertible = 0x1F,

        /// <summary>
        /// Detachable
        /// </summary>
        Detachable = 0x20,

        /// <summary>
        /// IoT Gateway
        /// </summary>
        IoTGateway = 0x21,

        /// <summary>
        /// Embedded PC
        /// </summary>
        EmbeddedPc = 0x22,

        /// <summary>
        /// Mini PC
        /// </summary>
        MiniPc = 0x23,

        /// <summary>
        /// Stick PC
        /// </summary>
        StickPc = 0x24
    }
}
