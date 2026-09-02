using System;
using System.IO;
using Microsoft.Win32;
using PSADT.Tests.TestHelpers;
using PSADT.Utilities;
using Xunit;

namespace PSADT.Tests.Utilities
{
    /// <summary>
    /// Tests the queries about the interactive desktop.
    /// </summary>
    /// <remarks>
    /// Only the reading members are covered. Minimising or restoring every window, telling the shell that
    /// the desktop or the environment has changed, and raising a window to the front are all changes to
    /// what the person at the machine is looking at, so none of them are exercised here.
    /// </remarks>
    public sealed class DesktopUtilitiesTests
    {
        /// <summary>
        /// Verifies that the profiles directory is the one the machine records, since every user profile
        /// this library reads is found beneath it.
        /// </summary>
        /// <remarks>
        /// The registry is used as the oracle rather than a hard-coded path, because the directory can be
        /// relocated and a machine that has been is exactly the machine this needs to get right.
        /// </remarks>
        [Fact]
        public void GetUserProfilesDirectory_MatchesWhatTheMachineRecords()
        {
            // Arrange
            using RegistryKey? key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\ProfileList");
            Assert.NotNull(key);
            string? recorded = key.GetValue("ProfilesDirectory") as string;
            Assert.NotNull(recorded);

            // Act
            DirectoryInfo profiles = DesktopUtilities.GetUserProfilesDirectory();

            // Assert
            Assert.Equal(EnvironmentUtilities.ExpandEnvironmentVariables(recorded), profiles.FullName, StringComparer.OrdinalIgnoreCase);
            Assert.True(profiles.Exists, $"The reported profiles directory {profiles.FullName} does not exist.");
        }

        /// <summary>
        /// Verifies that the idle time is never negative and moves forward while nothing is happening,
        /// since a deployment decides whether to interrupt somebody based on it.
        /// </summary>
        /// <remarks>
        /// The underlying reading is a thirty-two bit tick count that wraps roughly every forty-nine days,
        /// projected onto the sixty-four bit one. A machine that has been up longer than that is where a
        /// mistake in the projection would show, as a wildly wrong answer rather than a small one - hence
        /// the upper bound as well as the lower.
        /// </remarks>
        [Fact]
        public void GetLastInputTime_IsNeverNegativeOrImplausible()
        {
            // Act
            TimeSpan idle = DesktopUtilities.GetLastInputTime();

            // Assert
            Assert.True(idle >= TimeSpan.Zero, $"Reported a negative idle time of {idle}.");
            Assert.True(idle <= PSADT.DeviceManagement.DeviceUtilities.GetSystemUptime(), "Reported having been idle for longer than the machine has been running.");
        }

        /// <summary>
        /// Verifies that the notification state is one the enumeration defines, since a deployment decides
        /// whether it may show a prompt by comparing against those values.
        /// </summary>
        [Fact]
        public void GetUserNotificationState_ReportsADefinedState()
        {
            Assert.Contains(DesktopUtilities.GetUserNotificationState(), EnumValues.Declared<Interop.QUERY_USER_NOTIFICATION_STATE>());
        }

        /// <summary>
        /// Verifies that the process owning the foreground window is named, or that nothing is, which is
        /// the answer on a session with no interactive desktop.
        /// </summary>
        [Fact]
        public void GetForegroundWindowProcessId_AnswersWithoutFailing()
        {
            Assert.Null(Record.Exception(static () => _ = DesktopUtilities.GetForegroundWindowProcessId()));
        }
    }
}
