using System;

namespace PSADT.DeviceManagement
{
    /// <summary>
    /// Represents the charge status of a battery, including its capacity level and charging state.
    /// </summary>
    /// <remarks>This enumeration is decorated with the <see cref="FlagsAttribute"/>, allowing for a bitwise
    /// combination of its values. Use this enumeration to determine the current state of the battery, such as whether
    /// it is charging, critically low, or absent.</remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1028:Enum Storage should be Int32", Justification = "The type is correct for the data.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2217:Do not mark enums with FlagsAttribute", Justification = "This is a bitfield...")]
    [Flags]
    public enum BatteryChargeStatus : byte
    {
        /// <summary>
        /// The battery capacity is at more than 66 percent.
        /// </summary>
        High = 1 << 0,

        /// <summary>
        /// The battery capacity is at less than 33 percent
        /// </summary>
        Low = 1 << 1,

        /// <summary>
        /// The battery capacity is at less than five percent.
        /// </summary>
        Critical = 1 << 2,

        /// <summary>
        /// The battery is charging.
        /// </summary>
        Charging = 1 << 3,

        /// <summary>
        /// The system has no battery installed.
        /// </summary>
        NoSystemBattery = 1 << 7,

        /// <summary>
        /// Unable to read the battery flag information.
        /// </summary>
        Unknown = byte.MaxValue
    }
}
