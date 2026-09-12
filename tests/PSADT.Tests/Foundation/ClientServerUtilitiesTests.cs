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
        /// Verifies that a share is recognised as a network location, which is the case the whole
        /// distinction exists for.
        /// </summary>
        /// <remarks>
        /// Asserted against a path rather than against this machine's build, so that it holds on every
        /// run instead of only on a run that happens to have been laid down on a share. Nothing needs to
        /// exist at the path: a share is recognised from its shape alone.
        /// </remarks>
        [Fact]
        public void GetPathIsNetworked_IsTrueForAShare()
        {
            Assert.True(ClientServerUtilities.GetPathIsNetworked(@"\\server\share\dir"), "A share was not recognised as a network path.");
        }

        /// <summary>
        /// Verifies that a mapped drive is recognised as a network location, which no reading of the path
        /// can reveal since a mapped drive is spelled exactly like a local one.
        /// </summary>
        /// <remarks>
        /// Runs only where the machine has a mapped drive, since the alternative is for the test to map
        /// one itself - a change to the machine that would outlive a run that died partway through.
        /// </remarks>
        [Fact]
        public void GetPathIsNetworked_IsTrueForAMappedDrive()
        {
            string mappedRoot = FindRootOfType(DriveType.Network);
            Assert.SkipUnless(mappedRoot.Length > 0, "Requires a mapped network drive on the machine.");
            Assert.True(ClientServerUtilities.GetPathIsNetworked(mappedRoot), "A mapped drive was not recognised as a network path.");
        }

        /// <summary>
        /// Verifies that a fixed local drive is not mistaken for a network one, since that decides
        /// whether a user's token can be brokered at all.
        /// </summary>
        [Fact]
        public void GetPathIsNetworked_IsFalseForAFixedDrive()
        {
            string fixedRoot = FindRootOfType(DriveType.Fixed);
            Assert.SkipUnless(fixedRoot.Length > 0, "Requires a fixed drive on the machine.");
            Assert.False(ClientServerUtilities.GetPathIsNetworked(fixedRoot), "A fixed local drive was mistaken for a network path.");
        }

        /// <summary>
        /// Verifies that a root naming a drive the machine does not have is reported as local, rather
        /// than faulting on a drive that cannot be asked about.
        /// </summary>
        [Fact]
        public void GetPathIsNetworked_IsFalseForARootThatNamesNoDrive()
        {
            string unusedRoot = FindUnusedDriveRoot();
            Assert.SkipUnless(unusedRoot.Length > 0, "Requires at least one unused drive letter.");
            Assert.False(ClientServerUtilities.GetPathIsNetworked(unusedRoot), "A root naming no drive was reported as a network path.");
        }

        /// <summary>
        /// Verifies that a path neither question can parse is reported as local rather than thrown over.
        /// </summary>
        /// <remarks>
        /// An extended-length path is not a <see cref="Uri"/> and its root names no drive, so both
        /// questions refuse it. The answer is reached from a static constructor, so a path that throws
        /// here does not fail one call: it fails the type, and everything that reads it.
        /// </remarks>
        [Fact]
        public void GetPathIsNetworked_IsFalseForAPathItCannotParse()
        {
            Assert.False(ClientServerUtilities.GetPathIsNetworked(@"\\?\C:\Windows"), "An unparseable path was reported as a network path.");
        }

        /// <summary>
        /// Verifies that the answer held for this build is the one its own directory gives, tying the
        /// value every caller reads to the classification covered above.
        /// </summary>
        [Fact]
        public void ClientServerOnNetworkPath_AgreesWithTheDirectoryItWasDerivedFrom()
        {
            Assert.SkipUnless(TestEnvironment.ClientServerExecutablesPresent, SkipReason);
            Assert.Equal(ClientServerUtilities.GetPathIsNetworked(ClientServerUtilities.ClientServerDirectory.FullName), ClientServerUtilities.ClientServerOnNetworkPath);
        }

        /// <summary>
        /// Finds the root of the first drive of the given type.
        /// </summary>
        /// <param name="driveType">The type of drive to look for.</param>
        /// <returns>The drive's root, or an empty string if the machine has no drive of that type.</returns>
        private static string FindRootOfType(DriveType driveType)
        {
            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                if (drive.DriveType == driveType)
                {
                    return drive.Name;
                }
            }
            return string.Empty;
        }

        /// <summary>
        /// Finds the root of a drive letter the machine is not using.
        /// </summary>
        /// <returns>The unused root, or an empty string if every letter tried is in use.</returns>
        private static string FindUnusedDriveRoot()
        {
            for (char letter = 'Z'; letter >= 'D'; letter--)
            {
                string root = $"{letter}:\\";
                if (new DriveInfo(root).DriveType is DriveType.NoRootDirectory)
                {
                    return root;
                }
            }
            return string.Empty;
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
