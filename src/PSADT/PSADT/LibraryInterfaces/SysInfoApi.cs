namespace PSADT.LibraryInterfaces
{
    /// <summary>
    /// Firmware table IDs used with GetSystemFirmwareTable()
    /// Values are stored in little-endian format as expected by the Windows API
    /// </summary>
    public enum FIRMWARE_TABLE_ID : uint
    {
        /// <summary>
        /// SMBIOS firmware table (System Management BIOS)
        /// Used with provider signature 'RSMB' (0x52534D42)
        /// </summary>
        SMBIOS = 0x0000,

        /// <summary>
        /// Root System Description Pointer
        /// Used with provider signature 'ACPI' (0x41435049)
        /// </summary>
        ACPI_RSDP = 0x50445352, // 'RSDP' in little-endian

        /// <summary>
        /// Root System Description Table
        /// Used with provider signature 'ACPI' (0x41435049)
        /// </summary>
        ACPI_RSDT = 0x54445352, // 'RSDT' in little-endian

        /// <summary>
        /// Extended System Description Table
        /// Used with provider signature 'ACPI' (0x41435049)
        /// </summary>
        ACPI_XSDT = 0x54445358, // 'XSDT' in little-endian

        /// <summary>
        /// Fixed ACPI Description Table
        /// Used with provider signature 'ACPI' (0x41435049)
        /// </summary>
        ACPI_FACP = 0x50434146, // 'FACP' in little-endian

        /// <summary>
        /// Differentiated System Description Table
        /// Used with provider signature 'ACPI' (0x41435049)
        /// </summary>
        ACPI_DSDT = 0x54445344, // 'DSDT' in little-endian

        /// <summary>
        /// Secondary System Description Table
        /// Used with provider signature 'ACPI' (0x41435049)
        /// </summary>
        ACPI_SSDT = 0x54445353, // 'SSDT' in little-endian

        /// <summary>
        /// Multiple APIC Description Table
        /// Used with provider signature 'ACPI' (0x41435049)
        /// </summary>
        ACPI_APIC = 0x43495041, // 'APIC' in little-endian

        /// <summary>
        /// Boot Graphics Resource Table
        /// Used with provider signature 'ACPI' (0x41435049)
        /// </summary>
        ACPI_BGRT = 0x54524742, // 'BGRT' in little-endian

        /// <summary>
        /// Firmware Performance Data Table
        /// Used with provider signature 'ACPI' (0x41435049)
        /// </summary>
        ACPI_FPDT = 0x54445046, // 'FPDT' in little-endian

        /// <summary>
        /// Generic Timer Description Table
        /// Used with provider signature 'ACPI' (0x41435049)
        /// </summary>
        ACPI_GTDT = 0x54445447, // 'GTDT' in little-endian

        /// <summary>
        /// High Precision Event Timer Table
        /// Used with provider signature 'ACPI' (0x41435049)
        /// </summary>
        ACPI_HPET = 0x54455048, // 'HPET' in little-endian

        /// <summary>
        /// Memory Mapped Configuration Space Base Address Description Table
        /// Used with provider signature 'ACPI' (0x41435049)
        /// </summary>
        ACPI_MCFG = 0x4746434D, // 'MCFG' in little-endian

        /// <summary>
        /// Memory Power State Table
        /// Used with provider signature 'ACPI' (0x41435049)
        /// </summary>
        ACPI_MPST = 0x5453504D, // 'MPST' in little-endian

        /// <summary>
        /// Platform Communications Channel Table
        /// Used with provider signature 'ACPI' (0x41435049)
        /// </summary>
        ACPI_PCCT = 0x54434350, // 'PCCT' in little-endian

        /// <summary>
        /// Processor Properties Topology Table
        /// Used with provider signature 'ACPI' (0x41435049)
        /// </summary>
        ACPI_PPTT = 0x54545050, // 'PPTT' in little-endian

        /// <summary>
        /// System Locality Distance Information Table
        /// Used with provider signature 'ACPI' (0x41435049)
        /// </summary>
        ACPI_SLIT = 0x54494C53, // 'SLIT' in little-endian

        /// <summary>
        /// System Resource Affinity Table
        /// Used with provider signature 'ACPI' (0x41435049)
        /// </summary>
        ACPI_SRAT = 0x54415253, // 'SRAT' in little-endian

        /// <summary>
        /// Trusted Platform Module 2.0 Table
        /// Used with provider signature 'ACPI' (0x41435049)
        /// </summary>
        ACPI_TPM2 = 0x324D5054, // 'TPM2' in little-endian

        /// <summary>
        /// UEFI ACPI Data Table
        /// Used with provider signature 'ACPI' (0x41435049)
        /// </summary>
        ACPI_UEFI = 0x49464555, // 'UEFI' in little-endian

        /// <summary>
        /// Windows Security Mitigations Table
        /// Used with provider signature 'ACPI' (0x41435049)
        /// </summary>
        ACPI_WSMT = 0x544D5357, // 'WSMT' in little-endian

        /// <summary>
        /// Windows Platform Binary Table
        /// Used with provider signature 'ACPI' (0x41435049)
        /// </summary>
        ACPI_WPBT = 0x54425057, // 'WPBT' in little-endian

        /// <summary>
        /// Coherent Device Attribute Table
        /// Used with provider signature 'ACPI' (0x41435049)
        /// </summary>
        ACPI_CDAT = 0x54414443, // 'CDAT' in little-endian

        /// <summary>
        /// Platform Debug Trigger Table
        /// Used with provider signature 'ACPI' (0x41435049)
        /// </summary>
        ACPI_PDTT = 0x54544450, // 'PDTT' in little-endian

        /// <summary>
        /// FIRM firmware table - Default/Raw firmware data
        /// Used with provider signature 'FIRM' (0x4D524946)
        /// </summary>
        FIRM_DEFAULT = 0x0000
    }
}
