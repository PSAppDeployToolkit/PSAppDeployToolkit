using System;
using System.Reflection;
using PSADT.DeviceManagement;
using PSADT.Tests.TestHelpers;
using Xunit;

namespace PSADT.Tests.DeviceManagement
{
    /// <summary>
    /// Tests the battery and power state reported for this machine.
    /// </summary>
    /// <remarks>
    /// Nothing here asserts a particular power state, since the machine may be a desktop with no battery,
    /// a laptop on mains power or a laptop on battery, and all three are valid. What is asserted is that
    /// the answers are internally consistent: a machine with no usable battery reports no charge level,
    /// and one reporting mains power is treated as being on mains power.
    /// </remarks>
    public sealed class BatteryInfoTests
    {
        /// <summary>
        /// Verifies that a battery reading reports a power line status the enumeration defines.
        /// </summary>
        [Fact]
        public void ACPowerLineStatus_IsADefinedValue()
        {
            Assert.Contains(BatteryInfo.Get().ACPowerLineStatus, EnumValues.Declared<PowerLineStatus>());
        }

        /// <summary>
        /// Verifies that a machine with no usable battery reports no charge level, and one with a battery
        /// reports a level within range.
        /// </summary>
        [Fact]
        public void BatteryLifePercent_IsReportedOnlyWhenThereIsABattery()
        {
            // Act
            BatteryInfo battery = BatteryInfo.Get();

            // Assert
            if (battery.IsBatteryInvalid())
            {
                Assert.Null(battery.BatteryLifePercent);
            }
            else if (battery.BatteryLifePercent is byte percent)
            {
                Assert.InRange(percent, (byte)0, (byte)100);
            }
        }

        /// <summary>
        /// Verifies that the battery information is a single shared live view rather than a new reading
        /// each time it is asked for.
        /// </summary>
        /// <remarks>
        /// The type mirrors the WinForms power status: each member refreshes the underlying reading before
        /// answering, so one instance can serve the whole process and there is nothing to be gained by
        /// handing out more. The reading is still held per-instance rather than in static storage, which
        /// is what keeps the refreshing members from writing over each other.
        /// </remarks>
        [Fact]
        public void Get_IsASingleSharedLiveView()
        {
            // Assert: the same instance every time
            Assert.Same(BatteryInfo.Get(), BatteryInfo.Get());

            // Assert: reading through an instance field, with no static one shadowing it
            Assert.NotNull(typeof(BatteryInfo).GetField("_systemPowerStatus", BindingFlags.Instance | BindingFlags.NonPublic));
            Assert.Null(typeof(BatteryInfo).GetField("_systemPowerStatus", BindingFlags.Static | BindingFlags.NonPublic));
        }

        /// <summary>
        /// Verifies that the type does not advertise itself as a value, which a live view is not.
        /// </summary>
        /// <remarks>
        /// It was a record for a while, which meant its comparison and its hash code were worked out from
        /// the power reading it holds - a reading that changes every time any member is asked for. The
        /// one shared instance would have hashed differently from one minute to the next, leaving it
        /// unfindable in any set or dictionary it had been put into. Pinned by asking whether the type
        /// implements the equality interface a record is given, which is what would come back if somebody
        /// made it a record again.
        /// </remarks>
        [Fact]
        public void BatteryInfo_DoesNotAdvertiseValueEquality()
        {
            Assert.False(typeof(IEquatable<BatteryInfo>).IsAssignableFrom(typeof(BatteryInfo)), "BatteryInfo is a live view of the machine, not a value, and should not compare by its contents.");
        }

        /// <summary>
        /// Verifies that whether the machine is a laptop agrees with what the firmware says about its
        /// enclosure, since that is where the answer comes from.
        /// </summary>
        [Fact]
        public void IsLaptop_MatchesTheEnclosure()
        {
            Assert.Equal(HardwareInfo.SystemEnclosure.IsPortable(), BatteryInfo.Get().IsLaptop);
        }

        /// <summary>
        /// Verifies that a machine reporting alternating current, or reporting nothing usable at all, is
        /// treated as being on mains power, which is what decides whether a deployment is allowed to run.
        /// </summary>
        [Fact]
        public void IsUsingACPower_TreatsAnUnusableBatteryAsMainsPower()
        {
            // Act
            BatteryInfo battery = BatteryInfo.Get();

            // Assert
            if (battery.ACPowerLineStatus is PowerLineStatus.Online)
            {
                Assert.True(battery.IsUsingACPower);
            }
            else if (battery.ACPowerLineStatus is PowerLineStatus.Unknown && battery.IsBatteryInvalid())
            {
                Assert.True(battery.IsUsingACPower);
            }
            else if (battery.ACPowerLineStatus is PowerLineStatus.Offline)
            {
                Assert.False(battery.IsUsingACPower);
            }
        }

        /// <summary>
        /// Verifies that a machine with no usable battery reports no remaining life, since a caller
        /// showing a countdown would otherwise show one derived from nothing.
        /// </summary>
        [Fact]
        public void BatteryLifeRemaining_IsAbsentWithoutABattery()
        {
            // Act
            BatteryInfo battery = BatteryInfo.Get();

            // Assert
            if (battery.IsBatteryInvalid())
            {
                Assert.Null(battery.BatteryLifeRemaining);
                Assert.Null(battery.BatteryFullLifetime);
            }
        }
    }
}
