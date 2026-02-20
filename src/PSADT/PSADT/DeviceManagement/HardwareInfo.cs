using System;
using PSADT.SMBIOS;

namespace PSADT.DeviceManagement
{
    /// <summary>
    /// Provides static access to hardware and firmware information for the current system, including platform firmware,
    /// system details, baseboard information, and system enclosure data.
    /// </summary>
    /// <remarks>The HardwareInfo class retrieves and exposes hardware-related information by reading the
    /// system's SMBIOS tables. All members are static and initialized automatically before first use. This class is
    /// intended for scenarios where detailed hardware metadata is required, such as diagnostics, inventory, or
    /// compatibility checks. The information provided is read-only and reflects the state of the system at the time of
    /// initialization.</remarks>
    public static class HardwareInfo
    {
        /// <summary>
        /// Initializes static data for the HardwareInfo class by retrieving hardware and firmware information from the
        /// system's SMBIOS tables.
        /// </summary>
        /// <remarks>This static constructor is called automatically before any static members of the
        /// HardwareInfo class are accessed. It ensures that platform firmware, system, baseboard, and enclosure
        /// information are available for use by the class's static properties.</remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1810:Initialize reference type static fields inline", Justification = "The static constructor is needed here.")]
        static HardwareInfo()
        {
            Span<byte> buffer = stackalloc byte[SmbiosTables.GetRequiredLength()]; SmbiosTables.FillBuffer(buffer);
            PlatformFirmwareInformation = PlatformFirmwareInformation.Get(buffer)!;
            SystemInformation = SystemInformation.Get(buffer)!;
            SystemEnclosure = SystemEnclosure.Get(buffer)!;
        }

        /// <summary>
        /// Represents information about the platform firmware, including details such as version and manufacturer.
        /// </summary>
        /// <remarks>This field provides read-only access to the platform firmware information. It can be
        /// used to retrieve metadata about the firmware, which may be useful for diagnostics, compatibility checks, or
        /// system reporting.</remarks>
        public static readonly PlatformFirmwareInformation PlatformFirmwareInformation;

        /// <summary>
        /// Represents system-related information, such as operating system details, hardware specifications, or other
        /// environment data.
        /// </summary>
        /// <remarks>This field provides access to a <see cref="SystemInformation"/> instance containing
        /// details about the system. The specific information available depends on the implementation of the <see
        /// cref="SystemInformation"/> class.</remarks>
        public static readonly SystemInformation SystemInformation;

        /// <summary>
        /// Represents the system enclosure of the current hardware configuration.
        /// </summary>
        /// <remarks>This field provides information about the physical enclosure of the system, such as
        /// its type or form factor. It is a read-only field and cannot be modified after initialization.</remarks>
        public static readonly SystemEnclosure SystemEnclosure;
    }
}
