using System;
using PSADT.Interop;
using Windows.Win32.System.Power;

namespace PSADT.DeviceManagement
{
    /// <summary>
    /// Provides information about the device's battery and power state.
    /// </summary>
    /// <remarks>The <see cref="BatteryInfo"/> class encapsulates details about the battery's charge status,
    /// remaining life, power line status, and other related properties. It is designed to retrieve and expose
    /// system-level information about the device's power and battery state. <para> Use the <see cref="Get"/> method to
    /// obtain an instance of <see cref="BatteryInfo"/> through which the current battery and power-related data can be
    /// read. </para><para> Deliberately not a record. A record advertises that a type is a value, compared by its
    /// contents - and this is the opposite of one: a single shared instance that re-reads the machine on every member,
    /// so what it holds changes underneath whoever is looking at it. As a record its hash code would have varied over
    /// the life of the one instance, which would have left it unfindable in any set or dictionary it had been put
    /// into. </para></remarks>
    public sealed class BatteryInfo
    {
        /// <summary>
        /// Retrieves the current battery information.
        /// </summary>
        /// <remarks>Always the same instance. Every member refreshes the underlying reading before
        /// answering, so one instance serves the whole process, and handing back a shared one avoids
        /// re-reading the firmware tables that <see cref="IsLaptop"/> is derived from each time.</remarks>
        /// <returns>A <see cref="BatteryInfo"/> object through which the battery's state can be read.</returns>
        public static BatteryInfo Get()
        {
            return Instance;
        }

        /// <summary>
        /// The one instance handed out by <see cref="Get"/>.
        /// </summary>
        private static readonly BatteryInfo Instance = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="BatteryInfo"/> class.
        /// </summary>
        /// <remarks>This constructor retrieves initial battery and power-related information from the
        /// system. It uses system utilities to populate properties such as battery life, charge status, and power line
        /// status. This class is designed to provide information about the device's power and battery state.</remarks>
        private BatteryInfo()
        {
            UpdateSystemPowerStatus();
        }

        /// <summary>
        /// Gets the current status of the AC power line.
        /// </summary>
        public PowerLineStatus ACPowerLineStatus
        {
            get
            {
                UpdateSystemPowerStatus(); return (PowerLineStatus)_systemPowerStatus.ACLineStatus;
            }
        }

        /// <summary>
        /// Represents the current charge status of the device's battery.
        /// </summary>
        public BatteryChargeStatus BatteryChargeStatus
        {
            get
            {
                UpdateSystemPowerStatus(); return (BatteryChargeStatus)_systemPowerStatus.BatteryFlag;
            }
        }

        /// <summary>
        /// Represents the current battery life percentage of a device.
        /// </summary>
        public byte? BatteryLifePercent
        {
            get
            {
                UpdateSystemPowerStatus(); return !IsBatteryInvalid() && _systemPowerStatus.BatteryLifePercent != byte.MaxValue ? _systemPowerStatus.BatteryLifePercent : null;
            }
        }

        /// <summary>
        /// Indicates whether battery saver is currently switched on.
        /// </summary>
        public bool BatterySaverEnabled
        {
            get
            {
                UpdateSystemPowerStatus(); return _systemPowerStatus.SystemStatusFlag is 1;
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
                UpdateSystemPowerStatus(); return _systemPowerStatus.BatteryLifeTime != uint.MaxValue ? TimeSpan.FromSeconds(_systemPowerStatus.BatteryLifeTime) : null;
            }
        }

        /// <summary>
        /// Gets the estimated full lifetime of the battery.
        /// </summary>
        public TimeSpan? BatteryFullLifetime
        {
            get
            {
                UpdateSystemPowerStatus(); return _systemPowerStatus.BatteryFullLifeTime != uint.MaxValue ? TimeSpan.FromSeconds(_systemPowerStatus.BatteryFullLifeTime) : null;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the system is currently using AC power.
        /// </summary>
        public bool IsUsingACPower
        {
            get
            {
                UpdateSystemPowerStatus(); return (IsBatteryInvalid() && _systemPowerStatus.ACLineStatus == (byte)PowerLineStatus.Unknown) || _systemPowerStatus.ACLineStatus == (byte)PowerLineStatus.Online;
            }
        }

        /// <summary>
        /// Indicates whether the device is a laptop.
        /// </summary>
        public bool IsLaptop { get; } = HardwareInfo.SystemEnclosure.IsPortable();

        /// <summary>
        /// Gets a value indicating whether the battery is invalid.
        /// </summary>
        /// <remarks>Public because the module reads it: Test-ADTBattery reports a damaged battery by asking
        /// this when the power line status comes back as unknown.</remarks>
        /// <returns><see langword="true"/> if there is no usable battery; otherwise, <see langword="false"/>.</returns>
        public bool IsBatteryInvalid()
        {
            return BatteryChargeStatus is BatteryChargeStatus.NoSystemBattery or BatteryChargeStatus.Unknown;
        }

        /// <summary>
        /// Updates the current system power status by retrieving information about the system's power state.
        /// </summary>
        /// <remarks>This method uses the <see cref="NativeMethods.GetSystemPowerStatus"/> function to update
        /// the power status. The retrieved information includes details such as battery charge level, AC power status,
        /// and battery life.</remarks>
        private void UpdateSystemPowerStatus()
        {
            _ = NativeMethods.GetSystemPowerStatus(out _systemPowerStatus);
        }

        /// <summary>
        /// Represents the current power status of the system.
        /// </summary>
        /// <remarks>
        /// Refreshed by every member that reads it, so an instance is a live view of the machine rather
        /// than a reading taken once - the same arrangement as the WinForms power status this mirrors.
        /// <para>
        /// It is per-instance, not shared. A static field would make every instance report through the
        /// same storage, so one instance being read would alter what another reports.
        /// </para>
        /// </remarks>
        private SYSTEM_POWER_STATUS _systemPowerStatus;
    }
}
