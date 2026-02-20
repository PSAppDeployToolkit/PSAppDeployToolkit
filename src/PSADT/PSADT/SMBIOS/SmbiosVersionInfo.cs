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
    /// Represents SMBIOS version and entry point information.
    /// </summary>
    /// <remarks>
    /// This structure provides version details about the SMBIOS implementation,
    /// including major and minor version numbers, DMI revision, and entry point type.
    /// This information is useful for determining which SMBIOS structures are supported.
    /// </remarks>
    internal sealed record SmbiosVersionInfo
    {
        /// <summary>
        /// Gets the SMBIOS major version number.
        /// </summary>
        /// <remarks>
        /// Common values include:
        /// - 2 for SMBIOS 2.x implementations
        /// - 3 for SMBIOS 3.x implementations
        /// </remarks>
        internal readonly byte MajorVersion;

        /// <summary>
        /// Gets the SMBIOS minor version number.
        /// </summary>
        /// <remarks>
        /// For SMBIOS 2.x: typically ranges from 0-8 (e.g., 2.0, 2.1, 2.8)
        /// For SMBIOS 3.x: typically ranges from 0-6 (e.g., 3.0, 3.1, 3.6)
        /// </remarks>
        internal readonly byte MinorVersion;

        /// <summary>
        /// Gets the DMI (Desktop Management Interface) revision number.
        /// </summary>
        /// <remarks>
        /// This field is primarily used in SMBIOS 2.x implementations.
        /// Common values include 0, 1, or 2 depending on the DMI revision supported.
        /// </remarks>
        internal readonly byte DmiRevision;

        /// <summary>
        /// Gets the SMBIOS entry point type.
        /// </summary>
        /// <remarks>
        /// Indicates whether this is a SMBIOS 2.x, 3.x, or unknown entry point format.
        /// </remarks>
        internal readonly SmbiosEntryPointType EntryPointType;

        /// <summary>
        /// Initializes a new instance of the <see cref="SmbiosVersionInfo"/> class.
        /// </summary>
        /// <param name="majorVersion">The SMBIOS major version number.</param>
        /// <param name="minorVersion">The SMBIOS minor version number.</param>
        /// <param name="dmiRevision">The DMI revision number.</param>
        /// <param name="entryPointType">The entry point type.</param>
        internal SmbiosVersionInfo(byte majorVersion, byte minorVersion, byte dmiRevision, SmbiosEntryPointType entryPointType)
        {
            MajorVersion = majorVersion;
            MinorVersion = minorVersion;
            DmiRevision = dmiRevision;
            EntryPointType = entryPointType;
        }

        /// <summary>
        /// Gets the version as a formatted string.
        /// </summary>
        /// <returns>A string in the format "Major.Minor" (e.g., "2.8", "3.1").</returns>
        internal string GetVersionString()
        {
            return $"{MajorVersion}.{MinorVersion}";
        }

        /// <summary>
        /// Gets the full version information as a formatted string.
        /// </summary>
        /// <returns>A string containing version and entry point information.</returns>
        internal string GetFullVersionString()
        {
            string version = GetVersionString();
            string entryPoint = EntryPointType == SmbiosEntryPointType.Unknown ? "" : $" ({EntryPointType})";
            string dmi = DmiRevision > 0 ? $", DMI {DmiRevision}" : "";
            return $"SMBIOS {version}{entryPoint}{dmi}";
        }

        /// <summary>
        /// Determines whether this SMBIOS version supports a specific structure type.
        /// </summary>
        /// <param name="structureType">The SMBIOS structure type to check.</param>
        /// <returns>True if the structure type is supported in this version; otherwise false.</returns>
        internal bool SupportsType(SmbiosType structureType)
        {
            // Define structure introduction versions based on SMBIOS specification
            return structureType switch
            {
                // SMBIOS 2.0 structures
                SmbiosType.PlatformFirmwareInformation => MajorVersion >= 2,
                SmbiosType.SystemInformation => MajorVersion >= 2,
                SmbiosType.BaseboardInformation => MajorVersion >= 2,
                SmbiosType.SystemEnclosure => MajorVersion >= 2,
                SmbiosType.ProcessorInformation => MajorVersion >= 2,
                SmbiosType.MemoryControllerInformation => MajorVersion >= 2, // Obsolete in 2.1+
                SmbiosType.MemoryModuleInformation => MajorVersion >= 2, // Obsolete in 2.1+
                SmbiosType.CacheInformation => MajorVersion >= 2,
                SmbiosType.PortConnectorInformation => MajorVersion >= 2,
                SmbiosType.SystemSlots => MajorVersion >= 2,

                // SMBIOS 2.1 structures  
                SmbiosType.OnBoardDevicesInformation => MajorVersion >= 2 && (MajorVersion > 2 || MinorVersion >= 1),
                SmbiosType.OemStrings => MajorVersion >= 2 && (MajorVersion > 2 || MinorVersion >= 1),
                SmbiosType.SystemConfigurationOptions => MajorVersion >= 2 && (MajorVersion > 2 || MinorVersion >= 1),
                SmbiosType.FirmwareLanguageInformation => MajorVersion >= 2 && (MajorVersion > 2 || MinorVersion >= 1),
                SmbiosType.GroupAssociations => MajorVersion >= 2 && (MajorVersion > 2 || MinorVersion >= 1),
                SmbiosType.SystemEventLog => MajorVersion >= 2 && (MajorVersion > 2 || MinorVersion >= 1),
                SmbiosType.PhysicalMemoryArray => MajorVersion >= 2 && (MajorVersion > 2 || MinorVersion >= 1),
                SmbiosType.MemoryDevice => MajorVersion >= 2 && (MajorVersion > 2 || MinorVersion >= 1),

                // SMBIOS 2.3+ structures
                SmbiosType.PortableBattery => MajorVersion >= 2 && (MajorVersion > 2 || MinorVersion >= 3),
                SmbiosType.SystemReset => MajorVersion >= 2 && (MajorVersion > 2 || MinorVersion >= 3),
                SmbiosType.HardwareSecurity => MajorVersion >= 2 && (MajorVersion > 2 || MinorVersion >= 3),

                // SMBIOS 2.7+ structures
                SmbiosType.SystemPowerSupply => MajorVersion >= 2 && (MajorVersion > 2 || MinorVersion >= 7),

                // SMBIOS 3.0+ structures
                SmbiosType.TpmDevice => MajorVersion >= 3,
                SmbiosType.FirmwareInventoryInformation => MajorVersion >= 3 && (MajorVersion > 3 || MinorVersion >= 1),
                SmbiosType.StringProperty => MajorVersion >= 3 && (MajorVersion > 3 || MinorVersion >= 1),

                // Always supported
                SmbiosType.Inactive => true,
                SmbiosType.EndOfTable => true,

                // Default: assume supported if SMBIOS 2.0+
                SmbiosType.MemoryErrorInformation32Bit => MajorVersion >= 2,
                SmbiosType.MemoryArrayMappedAddress => MajorVersion >= 2,
                SmbiosType.MemoryDeviceMappedAddress => MajorVersion >= 2,
                SmbiosType.BuiltInPointingDevice => MajorVersion >= 2,
                SmbiosType.SystemPowerControls => MajorVersion >= 2,
                SmbiosType.VoltageProbe => MajorVersion >= 2,
                SmbiosType.CoolingDevice => MajorVersion >= 2,
                SmbiosType.TemperatureProbe => MajorVersion >= 2,
                SmbiosType.ElectricalCurrentProbe => MajorVersion >= 2,
                SmbiosType.OutOfBandRemoteAccess => MajorVersion >= 2,
                SmbiosType.BootIntegrityServicesEntryPoint => MajorVersion >= 2,
                SmbiosType.SystemBootInformation => MajorVersion >= 2,
                SmbiosType.MemoryErrorInformation64Bit => MajorVersion >= 2,
                SmbiosType.ManagementDevice => MajorVersion >= 2,
                SmbiosType.ManagementDeviceComponent => MajorVersion >= 2,
                SmbiosType.ManagementDeviceThresholdData => MajorVersion >= 2,
                SmbiosType.MemoryChannel => MajorVersion >= 2,
                SmbiosType.IpmiDeviceInformation => MajorVersion >= 2,
                SmbiosType.AdditionalInformation => MajorVersion >= 2,
                SmbiosType.OnboardDevicesExtendedInformation => MajorVersion >= 2,
                SmbiosType.ManagementControllerHostInterface => MajorVersion >= 2,
                SmbiosType.ProcessorAdditionalInformation => MajorVersion >= 2,
                _ => MajorVersion >= 2
            };
        }

        /// <summary>
        /// Determines whether this is a modern SMBIOS version (3.0+).
        /// </summary>
        /// <returns>True if SMBIOS 3.0 or later; otherwise false.</returns>
        internal bool IsModernSmbios()
        {
            return MajorVersion >= 3;
        }

        /// <summary>
        /// Determines whether this is a legacy SMBIOS version (2.x).
        /// </summary>
        /// <returns>True if SMBIOS 2.x; otherwise false.</returns>
        internal bool IsLegacySmbios()
        {
            return MajorVersion == 2;
        }

        /// <summary>
        /// Determines whether a specific structure type is obsolete in this SMBIOS version.
        /// </summary>
        /// <param name="structureType">The SMBIOS structure type to check.</param>
        /// <returns>True if the structure type is obsolete in this version; otherwise false.</returns>
        /// <remarks>
        /// Structure types are considered obsolete when they have been deprecated or replaced
        /// by newer structures in later SMBIOS versions. Obsolete structures may still be
        /// present in older systems but should not be expected in newer implementations.
        /// </remarks>
        internal bool IsObsoleteType(SmbiosType structureType)
        {
            return structureType switch
            {
                // Memory Controller Information (Type 5) - Obsolete in SMBIOS 2.1+
                // Replaced by Physical Memory Array (Type 16) and Memory Device (Type 17)
                SmbiosType.MemoryControllerInformation => MajorVersion >= 2 && (MajorVersion > 2 || MinorVersion >= 1),

                // Memory Module Information (Type 6) - Obsolete in SMBIOS 2.1+  
                // Replaced by Memory Device (Type 17)
                SmbiosType.MemoryModuleInformation => MajorVersion >= 2 && (MajorVersion > 2 || MinorVersion >= 1),

                // On Board Devices Information (Type 10) - Obsolete in SMBIOS 2.6+
                // Replaced by Onboard Devices Extended Information (Type 41)
                SmbiosType.OnBoardDevicesInformation => MajorVersion >= 2 && (MajorVersion > 2 || MinorVersion >= 6),

                // Boot Integrity Services Entry Point (Type 31) - Obsolete/Deprecated
                // BIS never gained widespread adoption and is considered obsolete
                SmbiosType.BootIntegrityServicesEntryPoint => true, // Always obsolete

                // No other structure types are officially obsolete
                SmbiosType.PlatformFirmwareInformation => false,
                SmbiosType.SystemInformation => false,
                SmbiosType.BaseboardInformation => false,
                SmbiosType.SystemEnclosure => false,
                SmbiosType.ProcessorInformation => false,
                SmbiosType.CacheInformation => false,
                SmbiosType.PortConnectorInformation => false,
                SmbiosType.SystemSlots => false,
                SmbiosType.OemStrings => false,
                SmbiosType.SystemConfigurationOptions => false,
                SmbiosType.FirmwareLanguageInformation => false,
                SmbiosType.GroupAssociations => false,
                SmbiosType.SystemEventLog => false,
                SmbiosType.PhysicalMemoryArray => false,
                SmbiosType.MemoryDevice => false,
                SmbiosType.MemoryErrorInformation32Bit => false,
                SmbiosType.MemoryArrayMappedAddress => false,
                SmbiosType.MemoryDeviceMappedAddress => false,
                SmbiosType.BuiltInPointingDevice => false,
                SmbiosType.PortableBattery => false,
                SmbiosType.SystemReset => false,
                SmbiosType.HardwareSecurity => false,
                SmbiosType.SystemPowerControls => false,
                SmbiosType.VoltageProbe => false,
                SmbiosType.CoolingDevice => false,
                SmbiosType.TemperatureProbe => false,
                SmbiosType.ElectricalCurrentProbe => false,
                SmbiosType.OutOfBandRemoteAccess => false,
                SmbiosType.SystemBootInformation => false,
                SmbiosType.MemoryErrorInformation64Bit => false,
                SmbiosType.ManagementDevice => false,
                SmbiosType.ManagementDeviceComponent => false,
                SmbiosType.ManagementDeviceThresholdData => false,
                SmbiosType.MemoryChannel => false,
                SmbiosType.IpmiDeviceInformation => false,
                SmbiosType.SystemPowerSupply => false,
                SmbiosType.AdditionalInformation => false,
                SmbiosType.OnboardDevicesExtendedInformation => false,
                SmbiosType.ManagementControllerHostInterface => false,
                SmbiosType.TpmDevice => false,
                SmbiosType.ProcessorAdditionalInformation => false,
                SmbiosType.FirmwareInventoryInformation => false,
                SmbiosType.StringProperty => false,
                SmbiosType.Inactive => false,
                SmbiosType.EndOfTable => false,
                _ => false
            };
        }

        /// <summary>
        /// Returns a string representation of the SMBIOS version information.
        /// </summary>
        public override string ToString()
        {
            return GetFullVersionString();
        }
    }
}
