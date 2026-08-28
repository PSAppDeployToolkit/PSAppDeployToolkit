using System;
using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using PSADT.DeviceManagement;
using Xunit;

namespace PSADT.Tests.DeviceManagement
{
    /// <summary>
    /// Tests what the machine reports about the operating system it is running.
    /// </summary>
    /// <remarks>
    /// The registry is the oracle here, not <see cref="Environment.OSVersion"/>. That property is backed
    /// by <c>GetVersionEx</c>, which the application compatibility shims rewrite for a process whose
    /// manifest does not declare support for the running release: under .NET Framework it reports major
    /// version 6 on a Windows 10 or 11 machine. Reading the version through the runtime library instead is
    /// exactly why this type exists, so testing it against the shimmed value would assert the opposite of
    /// what the type is for.
    /// </remarks>
    public sealed class OperatingSystemInfoTests
    {
        /// <summary>
        /// Verifies that the reported version matches the one the machine records for itself.
        /// </summary>
        [Fact]
        public void Version_MatchesWhatTheMachineRecords()
        {
            // Arrange
            using RegistryKey? key = Registry.LocalMachine.OpenSubKey(CurrentVersionKey);
            Assert.NotNull(key);

            // Act
            Version reported = OperatingSystemInfo.Current.Version;

            // Assert
            Assert.Equal(key.GetValue("CurrentMajorVersionNumber"), reported.Major);
            Assert.Equal(key.GetValue("CurrentMinorVersionNumber"), reported.Minor);
            Assert.Equal(key.GetValue("CurrentBuildNumber") as string, reported.Build.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Verifies that the revision is the update build revision recorded in the registry, which the
        /// framework's own version does not carry.
        /// </summary>
        [Fact]
        public void Version_RevisionIsTheUpdateBuildRevision()
        {
            // Arrange
            using RegistryKey? key = Registry.LocalMachine.OpenSubKey(CurrentVersionKey);
            Assert.NotNull(key);

            // Act & Assert
            Assert.Equal(key.GetValue("UBR") is int ubr ? ubr : 0, OperatingSystemInfo.Current.Version.Revision);
        }

        /// <summary>
        /// Verifies that the machine is reported as exactly one of the product types, since they are
        /// mutually exclusive and a caller branches on them.
        /// </summary>
        [Fact]
        public void Current_ReportsExactlyOneProductType()
        {
            // Act
            OperatingSystemInfo current = OperatingSystemInfo.Current;
            bool[] kinds = [current.IsWorkstation, current.IsServer, current.IsDomainController];

            // Assert
            _ = Assert.Single(kinds, static kind => kind);
        }

        /// <summary>
        /// Verifies that the architecture and bitness agree with the framework's own view.
        /// </summary>
        [Fact]
        public void Current_ArchitectureMatchesTheFramework()
        {
            Assert.Equal(RuntimeInformation.OSArchitecture, OperatingSystemInfo.Current.Architecture);
            Assert.Equal(Environment.Is64BitOperatingSystem, OperatingSystemInfo.Current.Is64BitOperatingSystem);
        }

        /// <summary>
        /// Verifies that the reported name follows the rename rule, which restates a Windows 11 build as
        /// eleven even though the registry still calls it ten.
        /// </summary>
        [Fact]
        public void Name_AppliesTheWindowsElevenRename()
        {
            // Arrange
            using RegistryKey? key = Registry.LocalMachine.OpenSubKey(CurrentVersionKey);
            Assert.NotNull(key);
            string registryName = $"Microsoft {key.GetValue("ProductName")}";

            // Act
            OperatingSystemInfo current = OperatingSystemInfo.Current;

            // Assert: a workstation on a build of 22000 or later is restated, anything else is left alone
            if (current.IsWorkstation && current.Version.Build >= 22_000 && registryName.Contains("10", StringComparison.Ordinal))
            {
                Assert.Contains("11", current.Name, StringComparison.Ordinal);
            }
            else
            {
                Assert.Equal(registryName, current.Name);
            }
        }

        /// <summary>
        /// Verifies that the edition and display version are reported, since both appear in deployment
        /// logs and an empty one reads as a failed query.
        /// </summary>
        [Fact]
        public void Current_ReportsAnEditionAndName()
        {
            // Act
            OperatingSystemInfo current = OperatingSystemInfo.Current;

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(current.Name));
            Assert.False(string.IsNullOrWhiteSpace(current.Edition));
        }

        /// <summary>
        /// Verifies that the same instance is handed out each time, since it is a snapshot taken once and
        /// callers compare against it.
        /// </summary>
        [Fact]
        public void Current_IsASingleSharedSnapshot()
        {
            Assert.Same(OperatingSystemInfo.Current, OperatingSystemInfo.Current);
        }

        /// <summary>
        /// Where Windows records what it is.
        /// </summary>
        private const string CurrentVersionKey = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion";
    }
}
