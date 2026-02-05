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
    /// Represents BIOS/Firmware characteristics as defined in the SMBIOS specification.
    /// Bit meanings:
    /// 0 Reserved
    /// 1 Reserved
    /// 2 Unknown
    /// 3 Firmware Characteristics are not supported (if set, other capability bits should be ignored)
    /// 4-31 Standard defined capability bits
    /// 32-47 Reserved for platform firmware vendor (named PlatformFirmwareVendorReservedXX)
    /// 48-63 Reserved for system vendor (named SystemVendorReservedXX)
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1028:Enum Storage should be Int32", Justification = "This enum is correctly typed as per the SMBIOS specification")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "<Pending>")]
    [Flags]
    public enum FirmwareCharacteristics : ulong
    {
        /// <summary>
        /// Unknown (Bit 2). Reported when firmware cannot determine characteristics.
        /// </summary>
        Unknown = 1UL << 2,

        /// <summary>
        /// Firmware characteristics not supported (Bit 3). If set, ignore other capability bits.
        /// </summary>
        NotSupported = 1UL << 3,

        /// <summary>
        /// ISA is supported (Bit 4)
        /// </summary>
        IsaSupported = 1UL << 4,

        /// <summary>
        /// MCA is supported (Bit 5)
        /// </summary>
        McaSupported = 1UL << 5,

        /// <summary>
        /// EISA is supported (Bit 6)
        /// </summary>
        EisaSupported = 1UL << 6,

        /// <summary>
        /// PCI is supported (Bit 7)
        /// </summary>
        PciSupported = 1UL << 7,

        /// <summary>
        /// PC Card (PCMCIA) is supported (Bit 8)
        /// </summary>
        PcCardSupported = 1UL << 8,

        /// <summary>
        /// Plug and Play is supported (Bit 9)
        /// </summary>
        PlugAndPlaySupported = 1UL << 9,

        /// <summary>
        /// APM is supported (Bit 10)
        /// </summary>
        ApmSupported = 1UL << 10,

        /// <summary>
        /// Firmware is upgradeable (Flash) (Bit 11)
        /// </summary>
        BiosUpgradeable = 1UL << 11,

        /// <summary>
        /// Firmware shadowing is allowed (Bit 12)
        /// </summary>
        BiosShadowingAllowed = 1UL << 12,

        /// <summary>
        /// VL-VESA is supported (Bit 13)
        /// </summary>
        VlVesaSupported = 1UL << 13,

        /// <summary>
        /// ESCD support is available (Bit 14)
        /// </summary>
        EscdSupported = 1UL << 14,

        /// <summary>
        /// Boot from CD is supported (Bit 15)
        /// </summary>
        BootFromCdSupported = 1UL << 15,

        /// <summary>
        /// Selectable boot is supported (Bit 16)
        /// </summary>
        SelectableBootSupported = 1UL << 16,

        /// <summary>
        /// Firmware ROM is socketed (Bit 17)
        /// </summary>
        BiosRomSocketed = 1UL << 17,

        /// <summary>
        /// Boot from PC Card (PCMCIA) is supported (Bit 18)
        /// </summary>
        BootFromPcCardSupported = 1UL << 18,

        /// <summary>
        /// EDD specification is supported (Bit 19)
        /// </summary>
        EddSupported = 1UL << 19,

        /// <summary>
        /// Int13h—Japanese floppy for NEC98001.2MB supported (Bit 20)
        /// </summary>
        Nec98FloppySupported = 1UL << 20,

        /// <summary>
        /// Int13h—Japanese floppy for Toshiba1.2MB supported (Bit 21)
        /// </summary>
        ToshibaFloppySupported = 1UL << 21,

        /// <summary>
        /// Int13h—5.25"/360KB floppy services supported (Bit 22)
        /// </summary>
        Floppy525_360Supported = 1UL << 22,

        /// <summary>
        /// Int13h—5.25"/1.2MB floppy services supported (Bit 23)
        /// </summary>
        Floppy525_1200Supported = 1UL << 23,

        /// <summary>
        /// Int13h—3.5"/720KB floppy services supported (Bit 24)
        /// </summary>
        Floppy35_720Supported = 1UL << 24,

        /// <summary>
        /// Int13h—3.5"/2.88MB floppy services supported (Bit 25)
        /// </summary>
        Floppy35_2880Supported = 1UL << 25,

        /// <summary>
        /// Int5h—Print Screen service supported (Bit 26)
        /// </summary>
        PrintScreenSupported = 1UL << 26,

        /// <summary>
        /// Int9h—8042 keyboard services supported (Bit 27)
        /// </summary>
        Keyboard8042Supported = 1UL << 27,

        /// <summary>
        /// Int14h—Serial services supported (Bit 28)
        /// </summary>
        SerialSupported = 1UL << 28,

        /// <summary>
        /// Int17h—Printer services supported (Bit 29)
        /// </summary>
        PrinterSupported = 1UL << 29,

        /// <summary>
        /// Int10h—CGA/Mono video services supported (Bit 30)
        /// </summary>
        CgaMonoVideoSupported = 1UL << 30,

        /// <summary>
        /// NEC PC-98 (Bit 31)
        /// </summary>
        NecPc98 = 1UL << 31,
    }
}
