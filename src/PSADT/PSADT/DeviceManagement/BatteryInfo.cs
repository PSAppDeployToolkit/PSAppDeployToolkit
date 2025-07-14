using System;
using PSADT.LibraryInterfaces;
using Windows.Win32.System.Power;

namespace PSADT.DeviceManagement
{
    /// <summary>
    /// Provides information about the device's battery and power state.
    /// </summary>
    /// <remarks>The <see cref="BatteryInfo"/> class encapsulates details about the battery's charge status, 
    /// remaining life, power line status, and other related properties. It is designed to retrieve  and expose
    /// system-level information about the device's power and battery state. <para> Use the <see cref="Get"/> method to
    /// obtain an instance of <see cref="BatteryInfo"/> populated  with the current battery and power-related data.
    /// </para></remarks>
    public sealed class BatteryInfo
    {
        /// <summary>
        /// Retrieves the current battery information.
        /// </summary>
        /// <returns>A <see cref="BatteryInfo"/> object containing details about the battery's state.</returns>
        public static BatteryInfo Get() => new();

        /// <summary>
        /// Initializes a new instance of the <see cref="BatteryInfo"/> class.
        /// </summary>
        /// <remarks>This constructor retrieves initial battery and power-related information from the
        /// system. It uses system utilities to populate properties such as battery life, charge status, and power line
        /// status. This class is designed to provide information about the device's power and battery state.</remarks>
        private BatteryInfo()
        {
            var chassisType = DeviceUtilities.GetSystemChassisType();
            IsLaptop = chassisType == SystemChassisType.Laptop || chassisType == SystemChassisType.Notebook || chassisType == SystemChassisType.SubNotebook;
        }

        /// <summary>
        /// Gets the current status of the AC power line.
        /// </summary>
        public PowerLineStatus ACPowerLineStatus
        {
            get
            {
                // Ensure the system power status is up to date.
                UpdateSystemPowerStatus(); return (PowerLineStatus)systemPowerStatus.ACLineStatus;
            }
        }

        /// <summary>
        /// Represents the current charge status of the device's battery.
        /// </summary>
        public BatteryChargeStatus BatteryChargeStatus
        {
            get
            {
                // Ensure the system power status is up to date.
                UpdateSystemPowerStatus(); return (BatteryChargeStatus)systemPowerStatus.BatteryFlag;
            }
        }

        /// <summary>
        /// Represents the current battery life percentage of a device.
        /// </summary>
        public byte? BatteryLifePercent => !IsBatteryInvalid() && systemPowerStatus.BatteryLifePercent != byte.MaxValue ? systemPowerStatus.BatteryLifePercent : null;

        /// <summary>
        /// Represents the current battery life percentage of a device.
        /// </summary>
        public bool BatterySaverEnabled
        {
            get
            {
                // Ensure the system power status is up to date.
                UpdateSystemPowerStatus(); return systemPowerStatus.SystemStatusFlag == 1;
            }
        }

        /// <summary>
        /// Gets the remaining battery life as a <see cref="TimeSpan"/> value, or <see langword="null"/> if the battery
        /// life cannot be determined.
        /// </summary>
        public TimeSpan? BatteryLifeRemaining
        {
            get
            {
                // Ensure the system power status is up to date.
                UpdateSystemPowerStatus(); return systemPowerStatus.BatteryLifeTime != uint.MaxValue ? TimeSpan.FromSeconds(systemPowerStatus.BatteryLifeTime) : null;
            }
        }

        /// <summary>
        /// Gets the estimated full lifetime of the battery.
        /// </summary>
        public TimeSpan? BatteryFullLifetime
        {
            get
            {
                // Ensure the system power status is up to date.
                UpdateSystemPowerStatus(); return systemPowerStatus.BatteryFullLifeTime != uint.MaxValue ? TimeSpan.FromSeconds(systemPowerStatus.BatteryFullLifeTime) : null;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the system is currently using AC power.
        /// </summary>
        public bool IsUsingACPower => (IsBatteryInvalid() && systemPowerStatus.ACLineStatus == (byte)PowerLineStatus.Unknown) || systemPowerStatus.ACLineStatus == (byte)PowerLineStatus.Online;

        /// <summary>
        /// Indicates whether the device is a laptop.
        /// </summary>
        public readonly bool IsLaptop;

        /// <summary>
        /// Gets a value indicating whether the battery is invalid.
        /// </summary>
        private bool IsBatteryInvalid()
        {
            // Store off BatteryChargeStatus to prevent repeated UpdateSystemPowerStatus() calls.
            var batteryChargeStatus = BatteryChargeStatus; return batteryChargeStatus == BatteryChargeStatus.NoSystemBattery || batteryChargeStatus == BatteryChargeStatus.Unknown;
        }

        /// <summary>
        /// Updates the current system power status by retrieving information about the system's power state.
        /// </summary>
        /// <remarks>This method uses the <see cref="Kernel32.GetSystemPowerStatus"/> function to update
        /// the power status. The retrieved information includes details such as battery charge level, AC power status,
        /// and battery life.</remarks>
        private static void UpdateSystemPowerStatus() => Kernel32.GetSystemPowerStatus(out systemPowerStatus);

        /// <summary>
        /// Represents the current power status of the system.
        /// </summary>
        /// <remarks>This field holds the system's power status information, including battery charge
        /// level, AC power status, and other related details. It is intended for internal use and should be updated
        /// using appropriate system APIs.</remarks>
        private static SYSTEM_POWER_STATUS systemPowerStatus;
    }
}
