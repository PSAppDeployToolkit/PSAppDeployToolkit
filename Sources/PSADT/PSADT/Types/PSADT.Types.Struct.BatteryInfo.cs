using System;

namespace PSADT.Types
{
    /// <summary>
    /// Represents information about the system's battery.
    /// </summary>
    public readonly struct BatteryInfo
    {
        /// <summary>
        /// Gets the current AC power line status.
        /// </summary>
        public PowerLineStatus ACPowerLineStatus { get; }

        /// <summary>
        /// Gets the current battery charge status.
        /// </summary>
        public BatteryChargeStatus BatteryChargeStatus { get; }

        /// <summary>
        /// Gets the current battery life percentage.
        /// </summary>
        public float BatteryLifePercent { get; }

        /// <summary>
        /// Gets the remaining battery life as a <see cref="TimeSpan"/>.
        /// </summary>
        public TimeSpan BatteryLifeRemaining { get; }

        /// <summary>
        /// Gets the full battery lifetime as a <see cref="TimeSpan"/>.
        /// </summary>
        public TimeSpan BatteryFullLifetime { get; }

        /// <summary>
        /// Gets a value indicating whether the system is using AC power.
        /// </summary>
        public bool IsUsingACPower { get; }

        /// <summary>
        /// Gets a value indicating whether the system is a laptop.
        /// </summary>
        public bool IsLaptop { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BatteryInfo"/> struct,
        /// converting original values from system sources to the appropriate types.
        /// </summary>
        /// <param name="acPowerLineStatus">The original AC power line status.</param>
        /// <param name="batteryChargeStatus">The original battery charge status.</param>
        /// <param name="batteryLifePercent">The battery life percentage.</param>
        /// <param name="batteryLifeRemainingSeconds">The battery life remaining in seconds.</param>
        /// <param name="batteryFullLifetimeSeconds">The full battery lifetime in seconds.</param>
        /// <param name="isUsingAcPower">Indicates whether the system is using AC power.</param>
        /// <param name="isLaptop">Indicates whether the system is a laptop.</param>
        public BatteryInfo(string acPowerLineStatus, string batteryChargeStatus, float batteryLifePercent, int batteryLifeRemainingSeconds, int batteryFullLifetimeSeconds, bool isUsingAcPower, bool isLaptop)
        {
            ACPowerLineStatus = MapACPowerLineStatus(acPowerLineStatus);
            BatteryChargeStatus = MapBatteryChargeStatus(batteryChargeStatus);
            BatteryLifePercent = batteryLifePercent;
            BatteryLifeRemaining = TimeSpan.FromSeconds(batteryLifeRemainingSeconds);
            BatteryFullLifetime = TimeSpan.FromSeconds(batteryFullLifetimeSeconds);
            IsUsingACPower = isUsingAcPower;
            IsLaptop = isLaptop;
        }

        /// <summary>
        /// Maps the original power line status from system values to the <see cref="PowerLineStatus"/> enum.
        /// </summary>
        /// <param name="status">The original AC power line status.</param>
        /// <returns>A <see cref="PowerLineStatus"/> representing the AC power line status.</returns>
        private static PowerLineStatus MapACPowerLineStatus(string status)
        {
            if (!Enum.TryParse<PowerLineStatus>(status, true, out PowerLineStatus powerLineStatus))
            {
                powerLineStatus = PowerLineStatus.Unknown;
            }

            return powerLineStatus;
        }

        /// <summary>
        /// Maps the original battery charge status from system values to the <see cref="BatteryChargeStatus"/> enum.
        /// </summary>
        /// <param name="status">The original battery charge status.</param>
        /// <returns>A <see cref="BatteryChargeStatus"/> representing the battery charge status.</returns>
        private static BatteryChargeStatus MapBatteryChargeStatus(string status)
        {
            if (!Enum.TryParse<BatteryChargeStatus>(status, true, out BatteryChargeStatus batteryStatus))
            {
                batteryStatus = BatteryChargeStatus.Unknown;
            }
            return batteryStatus;
        }

        /// <summary>
        /// Returns the battery life percentage as a formatted string (e.g., "85%").
        /// </summary>
        /// <returns>A string representing the formatted battery percentage.</returns>
        public readonly string GetFormattedBatteryPercentage()
        {
            return $"{BatteryLifePercent:F1}%";
        }

        /// <summary>
        /// Returns a summary of the battery information in a readable format.
        /// </summary>
        /// <returns>A string summarizing the battery status.</returns>
        public readonly string GetBatterySummary()
        {
            var status = IsUsingACPower ? "Plugged into AC" : "On Battery";
            var batteryLife = BatteryLifeRemaining.TotalSeconds > 0 ? $"{BatteryLifeRemaining.TotalMinutes:F0} minutes remaining" : "Unknown time remaining";

            return $"Status: {status}, Charge: {GetFormattedBatteryPercentage()}, {batteryLife}";
        }
    }

    /// <summary>
    /// Specifies the power line status of the system.
    /// </summary>
    public enum PowerLineStatus
    {
        /// <summary>
        /// The power status is unknown.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// The system is using an external power source (e.g., plugged into AC).
        /// </summary>
        Online = 1,

        /// <summary>
        /// The system is running on battery power.
        /// </summary>
        Offline = 2
    }

    /// <summary>
    /// Specifies the charge status of the battery.
    /// </summary>
    public enum BatteryChargeStatus
    {
        /// <summary>
        /// The battery status is unknown.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// The battery is discharging (i.e., the system is running on battery power).
        /// </summary>
        Discharging = 1,

        /// <summary>
        /// The battery is charging.
        /// </summary>
        Charging = 2,

        /// <summary>
        /// The battery is fully charged.
        /// </summary>
        FullyCharged = 3,

        /// <summary>
        /// No battery is present in the system.
        /// </summary>
        NoBattery = 4,

        /// <summary>
        /// The battery charge is low.
        /// </summary>
        Low = 5,

        /// <summary>
        /// The battery charge is critical.
        /// </summary>
        Critical = 6
    }
}
