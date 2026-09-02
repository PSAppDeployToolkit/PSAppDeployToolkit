using System;
using System.Collections.Generic;
using PSADT.DeviceManagement;
using Xunit;

namespace PSADT.Tests.DeviceManagement
{
    /// <summary>
    /// Tests the pending reboot summary.
    /// </summary>
    /// <remarks>
    /// The type is a data holder apart from one method, and that method decides whether a deployment
    /// tells the user a restart is needed. It draws on six of the eight collected indicators, and which
    /// six is not obvious from reading the constructor, so the truth table below states it explicitly.
    /// </remarks>
    public sealed class RebootInfoTests
    {
        /// <summary>
        /// Verifies that each contributing indicator on its own is enough to report a pending reboot, and
        /// that none of them together with nothing set reports one.
        /// </summary>
        /// <param name="system">Whether a system reboot is pending.</param>
        /// <param name="cbServicing">Whether component-based servicing has one pending.</param>
        /// <param name="windowsUpdate">Whether Windows Update has one pending.</param>
        /// <param name="sccm">Whether the Configuration Manager client has one pending.</param>
        /// <param name="appV">Whether App-V has one pending.</param>
        /// <param name="fileRename">Whether a pending file rename requires one.</param>
        /// <param name="expected">Whether a reboot should be reported as pending.</param>
        [Theory]
        // Nothing set.
        [InlineData(false, false, false, null, false, null, false)]
        // Each contributing indicator alone.
        [InlineData(true, false, false, null, false, null, true)]
        [InlineData(false, true, false, null, false, null, true)]
        [InlineData(false, false, true, null, false, null, true)]
        [InlineData(false, false, false, true, false, null, true)]
        [InlineData(false, false, false, null, true, null, true)]
        [InlineData(false, false, false, null, false, true, true)]
        // An explicit negative from a nullable indicator is not a positive.
        [InlineData(false, false, false, false, false, false, false)]
        // Everything set.
        [InlineData(true, true, true, true, true, true, true)]
        public void HasPendingReboot_ReportsAnyContributingIndicator(bool system, bool cbServicing, bool windowsUpdate, bool? sccm, bool appV, bool? fileRename, bool expected)
        {
            Assert.Equal(expected, Create(system, cbServicing, windowsUpdate, sccm, appV, fileRename).HasPendingReboot());
        }

        /// <summary>
        /// Verifies that the Intune client's indicator contributes to the summary, as the Configuration
        /// Manager equivalent beside it does.
        /// </summary>
        /// <remarks>
        /// Worth its own test because the two management clients are gathered the same way and shown the
        /// same way, and for a while only one of them counted. A machine managed through Intune alone
        /// would have been told no reboot was pending while its own management client was saying one was.
        /// </remarks>
        [Fact]
        public void HasPendingReboot_CountsBothManagementClients()
        {
            Assert.True(Create(intune: true).IsIntuneClientRebootPending);
            Assert.True(Create(intune: true).HasPendingReboot());
            Assert.True(Create(sccm: true).HasPendingReboot());

            // Assert: and an explicit negative from either is not a positive
            Assert.False(Create(sccm: false, intune: false).HasPendingReboot());
        }

        /// <summary>
        /// Verifies that a blank computer name is rejected, since the summary is meaningless without
        /// knowing which machine it describes.
        /// </summary>
        /// <param name="computerName">The blank name to reject.</param>
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_RejectsABlankComputerName(string computerName)
        {
            _ = Assert.Throws<ArgumentException>(() => Create(computerName: computerName));
        }

        /// <summary>
        /// Verifies that a null computer name is rejected as absent.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0191:Do not use the null-forgiving operator", Justification = "This is deliberate as part of unit testing.")]
        [Fact]
        public void Constructor_RejectsANullComputerName()
        {
            _ = Assert.Throws<ArgumentNullException>(static () => Create(computerName: null!));
        }

        /// <summary>
        /// Verifies that the pending file rename operations are exposed as an empty collection when there
        /// are none, so a caller can enumerate without a null check.
        /// </summary>
        [Fact]
        public void Constructor_ExposesEmptyCollectionsRatherThanNulls()
        {
            // Act
            RebootInfo info = Create();

            // Assert
            Assert.Empty(info.PendingFileRenameOperations);
            Assert.Empty(info.ErrorMsg);
        }

        /// <summary>
        /// Verifies that supplied file rename operations are snapshotted, so a caller mutating its own
        /// list afterwards cannot change the summary.
        /// </summary>
        [Fact]
        public void Constructor_SnapshotsThePendingFileRenameOperations()
        {
            // Arrange
            List<string> operations = ["first", "second"];

            // Act
            RebootInfo info = Create(pendingFileRenameOperations: operations);
            operations.Add("third");

            // Assert
            Assert.Equal(["first", "second"], info.PendingFileRenameOperations);
        }

        /// <summary>
        /// Verifies that the error messages are snapshotted the same way.
        /// </summary>
        [Fact]
        public void Constructor_SnapshotsTheErrorMessages()
        {
            // Arrange
            List<string> errors = ["could not read the servicing key"];

            // Act
            RebootInfo info = Create(errorMsg: errors);
            errors.Add("another");

            // Assert
            Assert.Equal(["could not read the servicing key"], info.ErrorMsg);
        }

        /// <summary>
        /// Verifies that the boot time is carried through unchanged, including its kind, since a caller
        /// comparing it against a local time needs to know which it got.
        /// </summary>
        [Fact]
        public void Constructor_PreservesTheBootTimeExactly()
        {
            // Arrange
            DateTime bootTime = new(2026, 8, 27, 6, 30, 0, DateTimeKind.Local);

            // Act
            RebootInfo info = Create(lastBootUpTime: bootTime);

            // Assert
            Assert.Equal(bootTime, info.LastBootUpTime);
            Assert.Equal(DateTimeKind.Local, info.LastBootUpTime.Kind);
        }

        /// <summary>
        /// Verifies that every indicator handed in is the indicator read back, so a summary cannot
        /// report one source's answer under another's name.
        /// </summary>
        /// <remarks>
        /// There are seven of these and they are all passed positionally through one constructor. Two
        /// transposed there would be invisible - the summary would still be a valid summary, just of a
        /// different machine - so each is set apart from the rest and read back.
        /// </remarks>
        [Fact]
        public void RebootInfo_KeepsEveryIndicatorSeparately()
        {
            // Assert: each indicator, set on its own, is reported
            Assert.True(Create(system: true).IsSystemRebootPending);
            Assert.True(Create(cbServicing: true).IsCBServicingRebootPending);
            Assert.True(Create(windowsUpdate: true).IsWindowsUpdateRebootPending);
            Assert.True(Create(sccm: true).IsSCCMClientRebootPending);
            Assert.True(Create(intune: true).IsIntuneClientRebootPending);
            Assert.True(Create(appV: true).IsAppVRebootPending);
            Assert.True(Create(fileRename: true).IsFileRenameRebootPending);

            // Assert: and setting one leaves the rest where they were
            RebootInfo systemOnly = Create(system: true);
            Assert.False(systemOnly.IsCBServicingRebootPending);
            Assert.False(systemOnly.IsWindowsUpdateRebootPending);
            Assert.Null(systemOnly.IsSCCMClientRebootPending);
            Assert.Null(systemOnly.IsIntuneClientRebootPending);
            Assert.False(systemOnly.IsAppVRebootPending);
            Assert.Null(systemOnly.IsFileRenameRebootPending);
        }

        /// <summary>
        /// Verifies that the machine the summary describes, and when it last started, are carried
        /// through - since a summary naming the wrong machine would be acted on all the same.
        /// </summary>
        [Fact]
        public void RebootInfo_NamesTheMachineAndWhenItStarted()
        {
            // Arrange
            DateTime bootTime = new(2026, 8, 27, 9, 30, 0, DateTimeKind.Local);

            // Act
            RebootInfo info = Create(computerName: "TESTHOST", lastBootUpTime: bootTime);

            // Assert
            Assert.Equal("TESTHOST", info.ComputerName, StringComparer.Ordinal);
            Assert.Equal(bootTime, info.LastBootUpTime);
        }

        /// <summary>
        /// Verifies that the lists carried on a summary are the ones handed in, and that a summary built
        /// without them reports empty lists rather than nothing at all.
        /// </summary>
        [Fact]
        public void RebootInfo_CarriesTheListsItWasGiven()
        {
            // Act
            RebootInfo populated = Create(pendingFileRenameOperations: [@"C:\old.dll", @"C:\new.dll"], errorMsg: ["something went wrong"]);
            RebootInfo empty = Create();

            // Assert
            Assert.Equal([@"C:\old.dll", @"C:\new.dll"], populated.PendingFileRenameOperations);
            Assert.Equal(["something went wrong"], populated.ErrorMsg);
            Assert.Empty(empty.PendingFileRenameOperations);
            Assert.Empty(empty.ErrorMsg);
        }

        /// <summary>
        /// Builds a summary, naming every constructor argument once so the tests above can vary only what
        /// they care about.
        /// </summary>
        /// <param name="system">Whether a system reboot is pending.</param>
        /// <param name="cbServicing">Whether component-based servicing has one pending.</param>
        /// <param name="windowsUpdate">Whether Windows Update has one pending.</param>
        /// <param name="sccm">Whether the Configuration Manager client has one pending.</param>
        /// <param name="appV">Whether App-V has one pending.</param>
        /// <param name="fileRename">Whether a pending file rename requires one.</param>
        /// <param name="intune">Whether the Intune client has one pending.</param>
        /// <param name="computerName">The machine the summary describes.</param>
        /// <param name="lastBootUpTime">When the machine last started.</param>
        /// <param name="pendingFileRenameOperations">The pending file rename operations, if any.</param>
        /// <param name="errorMsg">The errors encountered while gathering the summary, if any.</param>
        /// <returns>The constructed summary.</returns>
        private static RebootInfo Create(
            bool system = false,
            bool cbServicing = false,
            bool windowsUpdate = false,
            bool? sccm = null,
            bool appV = false,
            bool? fileRename = null,
            bool? intune = null,
            string computerName = "TESTHOST",
            DateTime lastBootUpTime = default,
            IReadOnlyList<string>? pendingFileRenameOperations = null,
            IReadOnlyList<string>? errorMsg = null)
        {
            return new(
                computerName: computerName,
                lastBootUpTime: lastBootUpTime,
                isSystemRebootPending: system,
                isCBServicingRebootPending: cbServicing,
                isWindowsUpdateRebootPending: windowsUpdate,
                isSCCMClientRebootPending: sccm,
                isIntuneClientRebootPending: intune,
                isAppVRebootPending: appV,
                isFileRenameRebootPending: fileRename,
                pendingFileRenameOperations: pendingFileRenameOperations,
                errorMsg: errorMsg ?? []);
        }

        /// <summary>
        /// Verifies that two summaries of the same machine are equal, and that a difference in either of
        /// the lists makes them unequal.
        /// </summary>
        /// <remarks>
        /// The lists are the part worth asserting. A collection compares by reference, so a record
        /// holding one directly never equals another built the same way - and this one is compared
        /// against an earlier reading to decide whether anything has changed since. They are held in a
        /// list that compares by its contents instead.
        /// </remarks>
        [Fact]
        public void Equality_IsByValueIncludingTheLists()
        {
            // Arrange
            RebootInfo left = Create(system: true, pendingFileRenameOperations: [@"C:\old.dll", @"C:\new.dll"], errorMsg: ["something went wrong"]);
            RebootInfo right = Create(system: true, pendingFileRenameOperations: [@"C:\old.dll", @"C:\new.dll"], errorMsg: ["something went wrong"]);

            // Assert
            Assert.Equal(left, right);
            Assert.Equal(left.GetHashCode(), right.GetHashCode());

            // Assert: and a difference in either list is a difference
            Assert.NotEqual(left, Create(system: true, pendingFileRenameOperations: [@"C:\old.dll"], errorMsg: ["something went wrong"]));
            Assert.NotEqual(left, Create(system: true, pendingFileRenameOperations: [@"C:\old.dll", @"C:\new.dll"], errorMsg: []));
        }
    }
}
