using System;
using System.IO;
using PSADT.Foundation;
using PSADT.Tests.TestHelpers;
using Xunit;

namespace PSADT.Tests.Foundation
{
    /// <summary>
    /// Tests how the client executables are located and which of them is chosen.
    /// </summary>
    /// <remarks>
    /// Launching a client is not exercised: it starts a process, and where a user is involved it registers
    /// a scheduled task to broker a token first. What is covered is everything decided before that - which
    /// paths are derived, which of the signed and compatible pairs is selected, and whether the caller is
    /// recognised as one of the clients itself.
    /// <para>
    /// Every test here is gated on the client executables being present. The type's static constructor
    /// asks whether each one is Authenticode trusted, and that question throws rather than answering false
    /// for a file that is not there, so without them the type cannot be touched at all.
    /// </para>
    /// </remarks>
    public sealed class ClientServerUtilitiesTests
    {
        /// <summary>
        /// Verifies that every client executable is found beside the library rather than merely named, so
        /// a launch is not going to fail on a missing file.
        /// </summary>
        [Fact(Skip = SkipReason, SkipUnless = nameof(TestEnvironment.ClientServerExecutablesPresent), SkipType = typeof(TestEnvironment))]
        public void Paths_PointAtExecutablesThatExist()
        {
            Assert.All(
                AllClientPaths(),
                static path => Assert.True(path.Exists, $"{path.FullName} does not exist."));
        }

        /// <summary>
        /// Verifies that every client executable is expected in the one directory, since they are shipped
        /// and located as a set.
        /// </summary>
        [Fact(Skip = SkipReason, SkipUnless = nameof(TestEnvironment.ClientServerExecutablesPresent), SkipType = typeof(TestEnvironment))]
        public void Paths_ShareTheClientServerDirectory()
        {
            // Arrange
            DirectoryInfo directory = ClientServerUtilities.ClientServerDirectory;

            // Assert
            Assert.True(directory.Exists, $"{directory.FullName} does not exist.");
            Assert.All(
                AllClientPaths(),
                path => Assert.Equal(directory.FullName, path.DirectoryName, StringComparer.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Verifies that the automatically chosen client is one of the two it chooses between, rather than
        /// a third path derived some other way.
        /// </summary>
        /// <remarks>
        /// The default build is chosen only when it is Authenticode trusted and interface access is in
        /// use, because that build declares interface access in its manifest and Windows refuses to grant
        /// it to an executable that is not signed. Anything else falls back to the compatible build, which
        /// does not ask for it. A repository build is unsigned, so this normally settles on the compatible
        /// one - which is why the assertion is that it is one of the pair rather than a particular one.
        /// </remarks>
        [Fact(Skip = SkipReason, SkipUnless = nameof(TestEnvironment.ClientServerExecutablesPresent), SkipType = typeof(TestEnvironment))]
        public void AutoPaths_AreOneOfTheTwoTheyChooseBetween()
        {
            // Compared by identity rather than by path, because the launching code compares them that way
            // too: it asks whether the file it was handed is the automatically chosen launcher, and two
            // separate objects naming the same file would not answer yes.
            Assert.True(
                ReferenceEquals(ClientServerUtilities.ClientAutoPath, ClientServerUtilities.ClientDefaultPath) || ReferenceEquals(ClientServerUtilities.ClientAutoPath, ClientServerUtilities.ClientCompatiblePath),
                "The automatically chosen client is neither of the two it chooses between.");
            Assert.True(
                ReferenceEquals(ClientServerUtilities.ClientLauncherAutoPath, ClientServerUtilities.ClientLauncherDefaultPath) || ReferenceEquals(ClientServerUtilities.ClientLauncherAutoPath, ClientServerUtilities.ClientLauncherCompatiblePath),
                "The automatically chosen launcher is neither of the two it chooses between.");
        }

        /// <summary>
        /// Verifies that an unsigned default build is never chosen, since Windows would refuse it the
        /// interface access that is the only reason to prefer it.
        /// </summary>
        [Fact(Skip = SkipReason, SkipUnless = nameof(TestEnvironment.ClientServerExecutablesPresent), SkipType = typeof(TestEnvironment))]
        public void AutoPaths_FallBackToTheCompatibleBuildWhenTheDefaultIsUnsigned()
        {
            if (!ClientServerUtilities.ClientDefaultPath.IsAuthenticodeTrusted())
            {
                Assert.Same(ClientServerUtilities.ClientCompatiblePath, ClientServerUtilities.ClientAutoPath);
            }
            if (!ClientServerUtilities.ClientLauncherDefaultPath.IsAuthenticodeTrusted())
            {
                Assert.Same(ClientServerUtilities.ClientLauncherCompatiblePath, ClientServerUtilities.ClientLauncherAutoPath);
            }
        }

        /// <summary>
        /// Verifies that the test host is not mistaken for one of the clients, since that flag decides
        /// whether the success marker is written to the user's registry on exit.
        /// </summary>
        [Fact(Skip = SkipReason, SkipUnless = nameof(TestEnvironment.ClientServerExecutablesPresent), SkipType = typeof(TestEnvironment))]
        public void CallerIsClientServerExecutable_IsFalseForTheTestHost()
        {
            Assert.False(ClientServerUtilities.CallerIsClientServerExecutable);
            Assert.False(ClientServerUtilities.CallerIsClientServerClient);
            Assert.False(ClientServerUtilities.CallerIsClientServerClientLauncher);
        }

        /// <summary>
        /// Verifies that the combined flag is exactly the two it is derived from, so neither can be set
        /// without it.
        /// </summary>
        [Fact(Skip = SkipReason, SkipUnless = nameof(TestEnvironment.ClientServerExecutablesPresent), SkipType = typeof(TestEnvironment))]
        public void CallerIsClientServerExecutable_IsTheUnionOfTheTwoKinds()
        {
            Assert.Equal(
                ClientServerUtilities.CallerIsClientServerClient || ClientServerUtilities.CallerIsClientServerClientLauncher,
                ClientServerUtilities.CallerIsClientServerExecutable);
        }

        /// <summary>
        /// Verifies that a build on a fixed local drive is not mistaken for a network one, since that
        /// decides whether a user's token can be brokered at all.
        /// </summary>
        /// <remarks>
        /// The drive's own type is the oracle here rather than the shape of the path, because the shape
        /// cannot tell the two apart: a mapped drive is spelled exactly like a local one.
        /// </remarks>
        [Fact]
        public void ClientServerOnNetworkPath_IsFalseForABuildOnAFixedDrive()
        {
            Assert.SkipUnless(TestEnvironment.ClientServerExecutablesPresent, SkipReason);
            Assert.SkipUnless(DriveBehindTheClient()?.DriveType is DriveType.Fixed, "Requires a build on a fixed local drive.");
            Assert.False(ClientServerUtilities.ClientServerOnNetworkPath, "A fixed local drive was mistaken for a network path.");
        }

        /// <summary>
        /// Verifies that a build on a share is recognised as a network one, which is the case the whole
        /// distinction exists for.
        /// </summary>
        /// <remarks>
        /// Only the share form is asserted, since it is the one form a path can be read for without
        /// asking the drive. A build on a mapped drive is the other half of the same case and is covered
        /// by the same code, but has no oracle independent of the implementation to assert against.
        /// </remarks>
        [Fact]
        public void ClientServerOnNetworkPath_IsTrueForABuildOnAShare()
        {
            Assert.SkipUnless(TestEnvironment.ClientServerExecutablesPresent, SkipReason);
            Assert.SkipUnless(new Uri(ClientServerUtilities.ClientServerDirectory.FullName).IsUnc, "Requires a build on a share.");
            Assert.True(ClientServerUtilities.ClientServerOnNetworkPath, "A share was not recognised as a network path.");
        }

        /// <summary>
        /// Resolves the drive the client/server executables were laid down on.
        /// </summary>
        /// <returns>The drive, or <see langword="null"/> if the path names no drive, as a share does.</returns>
        private static DriveInfo? DriveBehindTheClient()
        {
            try
            {
                return Path.GetPathRoot(ClientServerUtilities.ClientServerDirectory.FullName) is string pathRoot && !string.IsNullOrWhiteSpace(pathRoot) ? new DriveInfo(pathRoot) : null;
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        /// <summary>
        /// Verifies that the timeout a client operation is given is a usable one, since it bounds every
        /// call made into a user's session.
        /// </summary>
        [Fact(Skip = SkipReason, SkipUnless = nameof(TestEnvironment.ClientServerExecutablesPresent), SkipType = typeof(TestEnvironment))]
        public void ClientOperationTimeout_IsAUsableDuration()
        {
            Assert.True(ClientServerUtilities.ClientOperationTimeout > TimeSpan.Zero, "The client operation timeout is not a positive duration.");
        }

        /// <summary>
        /// Every client executable, as a set.
        /// </summary>
        /// <remarks>
        /// A method rather than a field, so that reading these does not happen while the class is being
        /// loaded - which would touch the type under test even on a machine where every test here skips.
        /// </remarks>
        /// <returns>The four client executables.</returns>
        private static FileInfo[] AllClientPaths()
        {
            return
            [
                ClientServerUtilities.ClientDefaultPath,
                ClientServerUtilities.ClientCompatiblePath,
                ClientServerUtilities.ClientLauncherDefaultPath,
                ClientServerUtilities.ClientLauncherCompatiblePath,
            ];
        }

        /// <summary>
        /// The reason every test in this file is gated, spelled once.
        /// </summary>
        private const string SkipReason = "Requires the client/server executables alongside the test assembly.";
    }
}
