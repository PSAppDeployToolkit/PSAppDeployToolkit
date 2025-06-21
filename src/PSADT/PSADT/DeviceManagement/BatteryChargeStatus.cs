using System;

namespace PSADT.DeviceManagement
{
    [Flags]
    public enum BatteryChargeStatus : byte
    {
        /// <summary>
        /// The battery capacity is at more than 66 percent.
        /// </summary>
        High = 1,

        /// <summary>
        /// The battery capacity is at less than 33 percent
        /// </summary>
        Low = 2,

        /// <summary>
        /// The battery capacity is at less than five percent.
        /// </summary>
        Critical = 4,

        /// <summary>
        /// The battery is charging.
        /// </summary>
        Charging = 8,

        /// <summary>
        /// The system has no battery installed.
        /// </summary>
        NoSystemBattery = 128,

        /// <summary>
        /// Unable to read the battery flag information.
        /// </summary>
        Unknown = 255
    }
}
