namespace PSADT.DeviceManagement
{
    /// <summary>
    /// Represents the power line status of a device, indicating whether it is running on battery power, AC power, or if the status is unknown.
    /// </summary>
    /// <remarks>This enumeration is typically used to determine the current power source of a device. Use
    /// this information to adapt behavior based on power conditions, such as conserving battery life when the device is
    /// offline.</remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1028:Enum Storage should be Int32", Justification = "The type is correct for the data.")]
    public enum PowerLineStatus : byte
    {
        /// <summary>
        /// The device is running on battery power.
        /// </summary>
        Offline = 0,

        /// <summary>
        /// The device is running on AC power.
        /// </summary>
        Online = 1,

        /// <summary>
        /// The power line status is unknown.
        /// </summary>
        Unknown = 255,
    }
}
