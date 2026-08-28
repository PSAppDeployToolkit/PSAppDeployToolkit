using PSADT.DeviceManagement;
using Xunit;

namespace PSADT.Tests.DeviceManagement
{
    /// <summary>
    /// Tests the firmware tables everything about the machine's identity is derived from.
    /// </summary>
    /// <remarks>
    /// The parsing of those tables is covered in depth against synthesised data elsewhere; what is left to
    /// establish here is that the real machine's tables are found and parsed at all, and that the answer
    /// is a snapshot taken once rather than a fresh read each time - reading firmware is not cheap, and
    /// callers ask repeatedly.
    /// </remarks>
    public sealed class HardwareInfoTests
    {
        /// <summary>
        /// Verifies that the hardware information read from firmware is present.
        /// </summary>
        [Fact]
        public void HardwareInfo_ReadsTheFirmwareTables()
        {
            Assert.NotNull(HardwareInfo.PlatformFirmwareInformation);
            Assert.NotNull(HardwareInfo.SystemInformation);
            Assert.NotNull(HardwareInfo.SystemEnclosure);
        }

        /// <summary>
        /// Verifies that the tables are read once and shared, rather than re-read for each caller.
        /// </summary>
        [Fact]
        public void HardwareInfo_IsReadOnce()
        {
            Assert.Same(HardwareInfo.PlatformFirmwareInformation, HardwareInfo.PlatformFirmwareInformation);
            Assert.Same(HardwareInfo.SystemInformation, HardwareInfo.SystemInformation);
            Assert.Same(HardwareInfo.SystemEnclosure, HardwareInfo.SystemEnclosure);
        }
    }
}
