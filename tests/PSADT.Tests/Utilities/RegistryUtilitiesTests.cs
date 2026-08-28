using System;
using System.Globalization;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using PSADT.Utilities;
using Xunit;

namespace PSADT.Tests.Utilities
{
    /// <summary>
    /// Tests the registry helpers, against keys every Windows installation has.
    /// </summary>
    /// <remarks>
    /// Nothing here writes. The last-write time is read through a native query because the framework does
    /// not expose it at all, and the path parser exists so a caller can hand over a key path in any of the
    /// spellings PowerShell produces - including the provider-qualified form, which is what
    /// <c>Get-Item</c> hands back.
    /// </remarks>
    public sealed class RegistryUtilitiesTests
    {
        /// <summary>
        /// Verifies that the last-write time of a key that exists on every machine is a plausible past
        /// moment, rather than the zero value a failed query would leave behind.
        /// </summary>
        [Fact]
        public void GetRegistryKeyLastWriteTime_ReadsAPlausibleTime()
        {
            // Act
            DateTime lastWrite = RegistryUtilities.GetRegistryKeyLastWriteTime(CurrentVersionKeyPath);

            // Assert: after Windows existed, and not in the future
            Assert.True(lastWrite > new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Local), $"Unexpectedly early: {lastWrite.ToString(CultureInfo.InvariantCulture)}.");
            Assert.True(lastWrite <= DateTime.Now.AddMinutes(1), $"Unexpectedly late: {lastWrite.ToString(CultureInfo.InvariantCulture)}.");
        }

        /// <summary>
        /// Verifies that the handle overload agrees with the path overload, since the path form opens a
        /// handle and forwards to it and a divergence would be silent.
        /// </summary>
        [Fact]
        public void GetRegistryKeyLastWriteTime_AgreesBetweenItsOverloads()
        {
            // Arrange
            using RegistryKey? key = Registry.LocalMachine.OpenSubKey(CurrentVersionSubKey);
            Assert.NotNull(key);
            using SafeRegistryHandle handle = key.Handle;

            // Act & Assert
            Assert.Equal(
                RegistryUtilities.GetRegistryKeyLastWriteTime(CurrentVersionKeyPath),
                RegistryUtilities.GetRegistryKeyLastWriteTime(handle));
        }

        /// <summary>
        /// Verifies that a key path is resolved through every hive spelling the parser accepts.
        /// </summary>
        /// <param name="keyPath">The key path, written one of the ways a caller might.</param>
        [Theory]
        [InlineData(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion")]
        [InlineData(@"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion")]
        [InlineData(@"Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion")]
        public void GetRegistryKeyForPath_ResolvesEveryHiveSpelling(string keyPath)
        {
            // Act
            using RegistryKey key = RegistryUtilities.GetRegistryKeyForPath(keyPath);

            // Assert
            Assert.NotNull(key.GetValue("CurrentVersion"));
        }

        /// <summary>
        /// Verifies that each hive is reachable by both its long and short name, since the two forms come
        /// from different places and a caller may use either.
        /// </summary>
        /// <param name="longName">The hive's full name.</param>
        /// <param name="shortName">The hive's abbreviated name.</param>
        /// <param name="subKey">A subkey that exists in that hive.</param>
        [Theory]
        [InlineData("HKEY_LOCAL_MACHINE", "HKLM", @"SOFTWARE\Microsoft")]
        [InlineData("HKEY_CURRENT_USER", "HKCU", "Environment")]
        [InlineData("HKEY_CLASSES_ROOT", "HKCR", ".txt")]
        [InlineData("HKEY_USERS", "HKU", ".DEFAULT")]
        public void GetRegistryKeyForPath_AcceptsBothHiveNames(string longName, string shortName, string subKey)
        {
            // Act
            using RegistryKey fromLong = RegistryUtilities.GetRegistryKeyForPath($@"{longName}\{subKey}");
            using RegistryKey fromShort = RegistryUtilities.GetRegistryKeyForPath($@"{shortName}\{subKey}");

            // Assert
            Assert.Equal(fromLong.Name, fromShort.Name, ignoreCase: true);
        }

        /// <summary>
        /// Verifies that a path naming no hive is reported as malformed rather than being searched for.
        /// </summary>
        /// <param name="keyPath">The malformed path.</param>
        [Theory]
        [InlineData("NoBackslashAtAll")]
        [InlineData("HKEY_LOCAL_MACHINE")]
        [InlineData("")]
        public void GetRegistryKeyForPath_ReportsAPathWithNoHive(string keyPath)
        {
            _ = Assert.Throws<FormatException>(() => RegistryUtilities.GetRegistryKeyForPath(keyPath));
        }

        /// <summary>
        /// Verifies that a hive nobody has is reported as malformed, naming what was asked for.
        /// </summary>
        [Fact]
        public void GetRegistryKeyForPath_ReportsAnUnknownHive()
        {
            // Act
            FormatException exception = Assert.Throws<FormatException>(static () => RegistryUtilities.GetRegistryKeyForPath(@"HKEY_MADE_UP\SOFTWARE"));

            // Assert
            Assert.Contains("HKEY_MADE_UP", exception.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that a key that is not there is reported as absent rather than returned as null, so a
        /// caller need not check.
        /// </summary>
        [Fact]
        public void GetRegistryKeyForPath_ReportsAKeyThatIsNotThere()
        {
            _ = Assert.Throws<InvalidOperationException>(static () => RegistryUtilities.GetRegistryKeyForPath(@"HKLM\SOFTWARE\PSAppDeployToolkit\NoSuchKeyForTesting"));
        }

        /// <summary>
        /// Verifies that a blank path is rejected as an absent argument when reading a write time.
        /// </summary>
        /// <param name="keyPath">The blank path to reject.</param>
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void GetRegistryKeyLastWriteTime_RejectsABlankPath(string keyPath)
        {
            _ = Assert.Throws<ArgumentException>(() => RegistryUtilities.GetRegistryKeyLastWriteTime(keyPath));
        }

        /// <summary>
        /// Verifies that a path with a hive and nothing else is reported as malformed when reading a write
        /// time, since there is no subkey to open.
        /// </summary>
        [Fact]
        public void GetRegistryKeyLastWriteTime_ReportsAPathWithNoSubKey()
        {
            _ = Assert.Throws<FormatException>(static () => RegistryUtilities.GetRegistryKeyLastWriteTime("HKEY_LOCAL_MACHINE"));
        }

        /// <summary>
        /// The provider-qualified path of a key present on every Windows installation.
        /// </summary>
        private const string CurrentVersionKeyPath = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion";

        /// <summary>
        /// The same key, as a subkey path beneath the local machine hive.
        /// </summary>
        private const string CurrentVersionSubKey = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion";
    }
}
