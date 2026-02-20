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
    /// Represents the type of SMBIOS structure.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1028:Enum Storage should be Int32", Justification = "This enum is correctly typed as per the SMBIOS specification")]
    public enum SmbiosType : byte
    {
        /// <summary>
        /// Platform Firmware Information (Type 0)
        /// </summary>
        PlatformFirmwareInformation = 0,

        /// <summary>
        /// System Information (Type 1)
        /// </summary>
        SystemInformation = 1,

        /// <summary>
        /// Baseboard (or Module) Information (Type 2)
        /// </summary>
        BaseboardInformation = 2,

        /// <summary>
        /// System Enclosure or Chassis (Type 3)
        /// </summary>
        SystemEnclosure = 3,

        /// <summary>
        /// Processor Information (Type 4)
        /// </summary>
        ProcessorInformation = 4,

        /// <summary>
        /// Memory Controller Information (Type 5) - Obsolete
        /// </summary>
        MemoryControllerInformation = 5,

        /// <summary>
        /// Memory Module Information (Type 6) - Obsolete
        /// </summary>
        MemoryModuleInformation = 6,

        /// <summary>
        /// Cache Information (Type 7)
        /// </summary>
        CacheInformation = 7,

        /// <summary>
        /// Port Connector Information (Type 8)
        /// </summary>
        PortConnectorInformation = 8,

        /// <summary>
        /// System Slots (Type 9)
        /// </summary>
        SystemSlots = 9,

        /// <summary>
        /// On Board Devices Information (Type 10) - Obsolete
        /// </summary>
        OnBoardDevicesInformation = 10,

        /// <summary>
        /// OEM Strings (Type 11)
        /// </summary>
        OemStrings = 11,

        /// <summary>
        /// System Configuration Options (Type 12)
        /// </summary>
        SystemConfigurationOptions = 12,

        /// <summary>
        /// Firmware Language Information (Type 13)
        /// </summary>
        FirmwareLanguageInformation = 13,

        /// <summary>
        /// Group Associations (Type 14)
        /// </summary>
        GroupAssociations = 14,

        /// <summary>
        /// System Event Log (Type 15)
        /// </summary>
        SystemEventLog = 15,

        /// <summary>
        /// Physical Memory Array (Type 16)
        /// </summary>
        PhysicalMemoryArray = 16,

        /// <summary>
        /// Memory Device (Type 17)
        /// </summary>
        MemoryDevice = 17,

        /// <summary>
        /// 32-Bit Memory Error Information (Type 18)
        /// </summary>
        MemoryErrorInformation32Bit = 18,

        /// <summary>
        /// Memory Array Mapped Address (Type 19)
        /// </summary>
        MemoryArrayMappedAddress = 19,

        /// <summary>
        /// Memory Device Mapped Address (Type 20)
        /// </summary>
        MemoryDeviceMappedAddress = 20,

        /// <summary>
        /// Built-in Pointing Device (Type 21)
        /// </summary>
        BuiltInPointingDevice = 21,

        /// <summary>
        /// Portable Battery (Type 22)
        /// </summary>
        PortableBattery = 22,

        /// <summary>
        /// System Reset (Type 23)
        /// </summary>
        SystemReset = 23,

        /// <summary>
        /// Hardware Security (Type 24)
        /// </summary>
        HardwareSecurity = 24,

        /// <summary>
        /// System Power Controls (Type 25)
        /// </summary>
        SystemPowerControls = 25,

        /// <summary>
        /// Voltage Probe (Type 26)
        /// </summary>
        VoltageProbe = 26,

        /// <summary>
        /// Cooling Device (Type 27)
        /// </summary>
        CoolingDevice = 27,

        /// <summary>
        /// Temperature Probe (Type 28)
        /// </summary>
        TemperatureProbe = 28,

        /// <summary>
        /// Electrical Current Probe (Type 29)
        /// </summary>
        ElectricalCurrentProbe = 29,

        /// <summary>
        /// Out-of-Band Remote Access (Type 30)
        /// </summary>
        OutOfBandRemoteAccess = 30,

        /// <summary>
        /// Boot Integrity Services (BIS) Entry Point (Type 31) - Obsolete/Deprecated
        /// </summary>
        /// <remarks>
        /// This structure type was defined in early SMBIOS specifications but is obsolete and rarely implemented.
        /// Boot Integrity Services (BIS) was part of early Intel specifications but never gained widespread adoption.
        /// Modern systems do not typically include this structure type.
        /// </remarks>
        BootIntegrityServicesEntryPoint = 31,

        /// <summary>
        /// System Boot Information (Type 32)
        /// </summary>
        SystemBootInformation = 32,

        /// <summary>
        /// 64-Bit Memory Error Information (Type 33)
        /// </summary>
        MemoryErrorInformation64Bit = 33,

        /// <summary>
        /// Management Device (Type 34)
        /// </summary>
        ManagementDevice = 34,

        /// <summary>
        /// Management Device Component (Type 35)
        /// </summary>
        ManagementDeviceComponent = 35,

        /// <summary>
        /// Management Device Threshold Data (Type 36)
        /// </summary>
        ManagementDeviceThresholdData = 36,

        /// <summary>
        /// Memory Channel (Type 37)
        /// </summary>
        MemoryChannel = 37,

        /// <summary>
        /// IPMI Device Information (Type 38)
        /// </summary>
        IpmiDeviceInformation = 38,

        /// <summary>
        /// System Power Supply (Type 39)
        /// </summary>
        SystemPowerSupply = 39,

        /// <summary>
        /// Additional Information (Type 40)
        /// </summary>
        AdditionalInformation = 40,

        /// <summary>
        /// Onboard Devices Extended Information (Type 41)
        /// </summary>
        OnboardDevicesExtendedInformation = 41,

        /// <summary>
        /// Management Controller Host Interface (Type 42)
        /// </summary>
        ManagementControllerHostInterface = 42,

        /// <summary>
        /// TPM Device (Type 43)
        /// </summary>
        TpmDevice = 43,

        /// <summary>
        /// Processor Additional Information (Type 44)
        /// </summary>
        ProcessorAdditionalInformation = 44,

        /// <summary>
        /// Firmware Inventory Information (Type 45)
        /// </summary>
        FirmwareInventoryInformation = 45,

        /// <summary>
        /// String Property (Type 46)
        /// </summary>
        StringProperty = 46,

        /// <summary>
        /// Inactive (Type 126)
        /// </summary>
        Inactive = 126,

        /// <summary>
        /// End-of-Table (Type 127)
        /// </summary>
        EndOfTable = 127,
    }
}
