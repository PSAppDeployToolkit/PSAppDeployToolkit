using System;
using PSADT.DeviceManagement;
using PSADT.Tests.TestHelpers;
using Xunit;

namespace PSADT.Tests.DeviceManagement
{
    /// <summary>
    /// Tests the read-only queries about the machine the tests are running on.
    /// </summary>
    /// <remarks>
    /// Every assertion here is either a shape the answer must have whatever machine this is, or a
    /// comparison against the same fact obtained another way. Nothing asserts a value particular to one
    /// machine, so the file says the same thing on a laptop, a server and a build agent.
    /// <para>
    /// Restarting the computer is, for obvious reasons, not covered.
    /// </para>
    /// </remarks>
    public sealed class DeviceUtilitiesTests
    {
        /// <summary>
        /// Verifies that the reported uptime is positive and moves forward, which is the least a caller
        /// deciding whether a machine has been up long enough can rely on.
        /// </summary>
        [Fact]
        public void GetSystemUptime_IsPositiveAndMovesForward()
        {
            // Act
            TimeSpan first = DeviceUtilities.GetSystemUptime();
            TimeSpan second = DeviceUtilities.GetSystemUptime();

            // Assert
            Assert.True(first > TimeSpan.Zero, "Expected the machine to have been up for some time.");
            Assert.True(second >= first, "Uptime went backwards between two readings.");
        }

        /// <summary>
        /// Verifies that the reported boot time is consistent with the reported uptime, since one is
        /// derived from the other and a caller may use either.
        /// </summary>
        [Fact]
        public void GetSystemBootTime_AgreesWithTheUptime()
        {
            // Act
            DateTime bootTime = DeviceUtilities.GetSystemBootTime();
            TimeSpan uptime = DeviceUtilities.GetSystemUptime();

            // Assert: the two are read a moment apart, so they agree to within a small tolerance
            Assert.True(bootTime < DateTime.Now, "The machine reports having booted in the future.");
            Assert.True((DateTime.Now - uptime - bootTime).Duration() < TimeSpan.FromSeconds(5), "Boot time and uptime disagree by more than a few seconds.");
        }

        /// <summary>
        /// Verifies that the reported physical memory is non-zero and plausible for a machine capable of
        /// running this at all.
        /// </summary>
        [Fact]
        public void GetTotalSystemMemory_ReportsAPlausibleAmount()
        {
            // Act
            ulong total = DeviceUtilities.GetTotalSystemMemory();

            // Assert: at least half a gigabyte, and less than a petabyte
            Assert.True(total > 512UL * 1024 * 1024, "Reported less memory than any machine running this would have.");
            Assert.True(total < 1024UL * 1024 * 1024 * 1024 * 1024, "Reported an implausible amount of memory.");
        }

        /// <summary>
        /// Verifies that the domain membership reported is one of the states the enumeration defines, and
        /// that a joined machine names what it is joined to.
        /// </summary>
        [Fact]
        public void GetDomainStatus_ReportsADefinedState()
        {
            // Act
            DomainStatus status = DeviceUtilities.GetDomainStatus();

            // Assert
            Assert.Contains(status.JoinStatus, EnumValues.Declared<Interop.NETSETUP_JOIN_STATUS>());
            if (status.JoinStatus is Interop.NETSETUP_JOIN_STATUS.NetSetupDomainName or Interop.NETSETUP_JOIN_STATUS.NetSetupWorkgroupName)
            {
                Assert.False(string.IsNullOrWhiteSpace(status.DomainOrWorkgroupName), "A joined machine reported no name for what it is joined to.");
            }
        }

        /// <summary>
        /// Verifies that the out-of-box experience is reported as finished, which it must be on a machine
        /// somebody is running tests on.
        /// </summary>
        [Fact]
        public void IsOOBEComplete_ReportsCompleteOnALoggedOnMachine()
        {
            Assert.True(DeviceUtilities.IsOOBEComplete());
        }

        /// <summary>
        /// Verifies that asking whether the microphone is in use answers rather than failing, on a machine
        /// that may have no capture device at all.
        /// </summary>
        [Fact]
        public void IsMicrophoneInUse_AnswersWithoutFailing()
        {
            Assert.Null(Record.Exception(static () => _ = DeviceUtilities.IsMicrophoneInUse()));
        }
    }
}
