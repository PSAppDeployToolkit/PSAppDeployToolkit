using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using PSADT.FileSystem;
using PSADT.Tests.TestHelpers;
using Xunit;

namespace PSADT.Tests.FileSystem
{
    /// <summary>
    /// Tests the file system helpers that read rather than modify.
    /// </summary>
    /// <remarks>
    /// The path recogniser is the part with the most reach: the command line parser calls it to decide
    /// whether what it is looking at is a path it should take whole or a run of arguments it should split,
    /// so its answers decide how an unquoted installer command line is understood. The rest of the file
    /// covers the size walk and the effective access checks, both of which are read-only.
    /// <para>
    /// The members that write access control are covered too. Everything they touch is created for the
    /// test inside a temporary directory that is removed afterwards, so nothing on the machine is
    /// altered; the ones that reassign ownership are gated on the privilege Windows requires for it,
    /// which an ordinary caller does not hold, and the refusal is gated the other way.
    /// </para>
    /// </remarks>
    public sealed class FileSystemUtilitiesTests
    {
        /// <summary>
        /// Verifies which strings are recognised as a path, across the three forms the recogniser accepts
        /// and the near misses that must be rejected.
        /// </summary>
        /// <param name="path">The candidate.</param>
        /// <param name="expected">Whether it should be recognised.</param>
        [Theory]
        // Drive-qualified paths, with either separator.
        [InlineData(@"C:\", true)]
        [InlineData(@"C:\Windows", true)]
        [InlineData(@"C:\Program Files\App\app.exe", true)]
        [InlineData("C:/Windows", true)]
        [InlineData(@"z:\lower", true)]
        // A drive letter with no separator after the colon is not a path.
        [InlineData("C:", false)]
        [InlineData("C:Windows", false)]
        // The character before the colon has to be a letter.
        [InlineData(@"1:\dir", false)]
        [InlineData(@"::\dir", false)]
        // Universal naming convention paths.
        [InlineData(@"\\server\share", true)]
        [InlineData(@"\\server\share\file.txt", true)]
        [InlineData(@"\\?\C:\Windows", true)]
        // A single leading separator is not one.
        [InlineData(@"\server\share", false)]
        // Forward-slash form, as a shell would write it.
        [InlineData("/c/Windows", true)]
        [InlineData("/C/", true)]
        [InlineData("/notadrive/", false)]
        [InlineData("/c", false)]
        // Plainly not paths.
        [InlineData("notapath", false)]
        [InlineData("--switch", false)]
        [InlineData("/quiet", false)]
        public void IsValidFilePath_RecognisesThePathForms(string path, bool expected)
        {
            Assert.Equal(expected, FileSystemUtilities.IsValidFilePath(path));
            Assert.Equal(expected, FileSystemUtilities.IsValidFilePath(path.AsSpan()));
        }

        /// <summary>
        /// Verifies that both public overloads reject blank input as an absent argument, while the
        /// positional overload answers for it.
        /// </summary>
        /// <remarks>
        /// The split is deliberate. Both public entry points validate what they are given, so a caller
        /// asking about nothing gets told so rather than receiving a plausible false. The positional
        /// overload is the one the command line parser walks a whole command line with, and it is reached
        /// with a position past the end on every pass, so for that one an answer is the useful response.
        /// </remarks>
        /// <param name="path">The blank input.</param>
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void IsValidFilePath_RejectsBlankInputThroughBothPublicOverloads(string path)
        {
            _ = Assert.Throws<ArgumentException>(() => FileSystemUtilities.IsValidFilePath(path));
            _ = Assert.Throws<ArgumentException>(() => FileSystemUtilities.IsValidFilePath(path.AsSpan()));
            Assert.False(FileSystemUtilities.IsValidFilePath(path.AsSpan(), 0));
        }

        /// <summary>
        /// Verifies that a run of separators followed by a quote is not taken for a network path.
        /// </summary>
        /// <remarks>
        /// This is the case the recogniser exists to get right. A command line argument ending in a
        /// backslash is escaped by doubling it before the closing quote, so a run of backslashes followed
        /// by a quote is the tail of an argument rather than the head of a path. Reading it as a path
        /// would make the parser swallow the rest of the command line.
        /// </remarks>
        /// <param name="input">The candidate.</param>
        /// <param name="expected">Whether it should be recognised as a path.</param>
        [Theory]
        [InlineData(@"\\""", false)]
        [InlineData(@"\\\""", false)]
        [InlineData(@"\\\\""", false)]
        [InlineData(@"\\server""", true)]
        [InlineData(@"\\\server", true)]
        public void IsValidFilePath_DoesNotMistakeAnEscapedArgumentForANetworkPath(string input, bool expected)
        {
            Assert.Equal(expected, FileSystemUtilities.IsValidFilePath(input));
        }

        /// <summary>
        /// Verifies that the recogniser can start part way through a string, which is how the command line
        /// parser uses it, and that a position past the end is answered rather than throwing.
        /// </summary>
        /// <param name="input">The whole string.</param>
        /// <param name="position">Where to start looking.</param>
        /// <param name="expected">Whether a path starts there.</param>
        [Theory]
        [InlineData(@"/log C:\temp\log.txt", 5, true)]
        [InlineData(@"/log C:\temp\log.txt", 0, false)]
        [InlineData(@"app.exe \\server\share", 8, true)]
        [InlineData("short", 99, false)]
        [InlineData("short", 5, false)]
        public void IsValidFilePath_StartsAtTheGivenPosition(string input, int position, bool expected)
        {
            Assert.Equal(expected, FileSystemUtilities.IsValidFilePath(input.AsSpan(), position));
        }

        /// <summary>
        /// Verifies that the fully qualified test agrees with the framework's own, since it forwards to it
        /// and exists only to be reachable from PowerShell.
        /// </summary>
        /// <param name="path">The path to classify.</param>
        [Theory]
        [InlineData(@"C:\Windows")]
        [InlineData(@"\\server\share")]
        [InlineData("relative")]
        [InlineData(@".\relative")]
        [InlineData(@"\rooted-but-not-qualified")]
        public void IsPathFullyQualified_AgreesWithTheFramework(string path)
        {
            Assert.Equal(Path.IsPathFullyQualified(path), FileSystemUtilities.IsPathFullyQualified(path));
            Assert.Equal(Path.IsPathFullyQualified(path.AsSpan()), FileSystemUtilities.IsPathFullyQualified(path.AsSpan()));
        }

        /// <summary>
        /// Verifies that the size walk totals every file beneath a directory, including those in nested
        /// subdirectories.
        /// </summary>
        [Fact]
        public void GetLogicalSizeBytes_TotalsEveryFileInTheTree()
        {
            // Arrange
            using TempDirectory temp = new();
            WriteFileOfSize(temp.GetPath("root-a.bin"), 100);
            WriteFileOfSize(temp.GetPath("root-b.bin"), 200);
            DirectoryInfo child = temp.CreateSubdirectory("child");
            WriteFileOfSize(Path.Join(child.FullName, "child.bin"), 300);
            DirectoryInfo grandchild = child.CreateSubdirectory("grandchild");
            WriteFileOfSize(Path.Join(grandchild.FullName, "grandchild.bin"), 400);

            // Act & Assert
            Assert.Equal(1_000, FileSystemUtilities.GetLogicalSizeBytes(temp.FullName));
        }

        /// <summary>
        /// Verifies that an empty directory totals nothing rather than failing.
        /// </summary>
        [Fact]
        public void GetLogicalSizeBytes_TotalsNothingForAnEmptyDirectory()
        {
            // Arrange
            using TempDirectory temp = new();

            // Act & Assert
            Assert.Equal(0, FileSystemUtilities.GetLogicalSizeBytes(temp.FullName));
        }

        /// <summary>
        /// Verifies that a directory holding only empty files still totals nothing, which exercises the
        /// branch that skips a zero-length file rather than adding it.
        /// </summary>
        [Fact]
        public void GetLogicalSizeBytes_TotalsNothingForEmptyFiles()
        {
            // Arrange
            using TempDirectory temp = new();
            _ = temp.WriteFile("empty-a.bin", string.Empty);
            _ = temp.WriteFile("empty-b.bin", string.Empty);

            // Act & Assert
            Assert.Equal(0, FileSystemUtilities.GetLogicalSizeBytes(temp.FullName));
        }

        /// <summary>
        /// Verifies that trailing separators are trimmed before the walk starts, so the same directory
        /// written three ways totals the same.
        /// </summary>
        [Fact]
        public void GetLogicalSizeBytes_IgnoresTrailingSeparators()
        {
            // Arrange
            using TempDirectory temp = new();
            WriteFileOfSize(temp.GetPath("file.bin"), 512);

            // Act & Assert
            Assert.Equal(512, FileSystemUtilities.GetLogicalSizeBytes(temp.FullName));
            Assert.Equal(512, FileSystemUtilities.GetLogicalSizeBytes(temp.FullName + @"\"));
            Assert.Equal(512, FileSystemUtilities.GetLogicalSizeBytes(temp.FullName + @"\\"));
            Assert.Equal(512, FileSystemUtilities.GetLogicalSizeBytes(temp.FullName + "/"));
        }

        /// <summary>
        /// Verifies that a directory that is not there is reported rather than totalling nothing, since a
        /// silent zero would read as an empty directory.
        /// </summary>
        [Fact]
        public void GetLogicalSizeBytes_ReportsAMissingDirectory()
        {
            // Arrange
            using TempDirectory temp = new();

            // Act & Assert
            _ = Assert.Throws<DirectoryNotFoundException>(() => FileSystemUtilities.GetLogicalSizeBytes(temp.GetPath("absent")));
        }

        /// <summary>
        /// Verifies that a blank root is rejected as an absent argument.
        /// </summary>
        /// <param name="rootPath">The blank root to reject.</param>
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void GetLogicalSizeBytes_RejectsABlankRoot(string rootPath)
        {
            _ = Assert.Throws<ArgumentException>(() => FileSystemUtilities.GetLogicalSizeBytes(rootPath));
        }

        /// <summary>
        /// Verifies that the device lookup table maps the system drive, which is what turns an NT path
        /// reported by the kernel back into something a caller can open.
        /// </summary>
        [Fact]
        public void MakeNtPathLookupTable_MapsTheSystemDrive()
        {
            // Arrange: through a local, because the two target frameworks disagree on whether GetPathRoot
            // can return null, so a null-forgiving operator is needed on one and redundant on the other
            string? systemRoot = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System));
            Assert.NotNull(systemRoot);
            string systemDrive = systemRoot.TrimEnd('\\');

            // Act
            ReadOnlyDictionary<string, string> table = FileSystemUtilities.MakeNtPathLookupTable();

            // Assert: every entry maps a device name to a drive, and the system drive is among them
            Assert.NotEmpty(table);
            Assert.Contains(systemDrive, table.Values, StringComparer.OrdinalIgnoreCase);
            Assert.All(table.Keys, static key => Assert.False(string.IsNullOrWhiteSpace(key)));
        }

        /// <summary>
        /// Verifies that the table carries the multiple universal naming convention provider, which is the
        /// one entry it adds itself rather than reading from the system.
        /// </summary>
        [Fact]
        public void MakeNtPathLookupTable_CarriesTheNetworkProvider()
        {
            // Act
            ReadOnlyDictionary<string, string> table = FileSystemUtilities.MakeNtPathLookupTable();

            // Assert
            Assert.True(table.TryGetValue(@"\Device\Mup", out string? mapped));
            Assert.Equal(@"\", mapped);
        }

        /// <summary>
        /// Verifies that device names are matched without regard to case, since the kernel and the drive
        /// query do not agree on it.
        /// </summary>
        [Fact]
        public void MakeNtPathLookupTable_MatchesDeviceNamesWithoutRegardToCase()
        {
            // Act
            ReadOnlyDictionary<string, string> table = FileSystemUtilities.MakeNtPathLookupTable();

            // Assert
            Assert.True(table.ContainsKey(@"\device\mup"));
            Assert.True(table.ContainsKey(@"\DEVICE\MUP"));
        }

        /// <summary>
        /// Verifies that the access control for a file is readable and names an owner, which is the
        /// minimum the remediation code needs before it can decide whether to change anything.
        /// </summary>
        [Fact]
        public void GetAccessControl_ReadsTheOwnerOfAFile()
        {
            // Arrange
            using TempDirectory temp = new();
            FileInfo file = new(temp.WriteFile("acl.txt", "content"));

            // Act
            FileSecurity security = FileSystemUtilities.GetAccessControl(file);

            // Assert
            Assert.NotNull(security.GetOwner(typeof(SecurityIdentifier)));
            Assert.NotEmpty(security.GetAccessRules(includeExplicit: true, includeInherited: true, typeof(SecurityIdentifier)));
        }

        /// <summary>
        /// Verifies that the same is true of a directory, since the two overloads take different types and
        /// call different APIs.
        /// </summary>
        [Fact]
        public void GetAccessControl_ReadsTheOwnerOfADirectory()
        {
            // Arrange
            using TempDirectory temp = new();

            // Act
            DirectorySecurity security = FileSystemUtilities.GetAccessControl(temp.Directory);

            // Assert
            Assert.NotNull(security.GetOwner(typeof(SecurityIdentifier)));
            Assert.NotEmpty(security.GetAccessRules(includeExplicit: true, includeInherited: true, typeof(SecurityIdentifier)));
        }

        /// <summary>
        /// Verifies that asking for only part of the security descriptor returns that part, so a caller
        /// reading access rules does not pay for the owner and group as well.
        /// </summary>
        [Fact]
        public void GetAccessControl_HonoursTheRequestedSections()
        {
            // Arrange
            using TempDirectory temp = new();
            FileInfo file = new(temp.WriteFile("sections.txt", "content"));

            // Act
            FileSecurity security = FileSystemUtilities.GetAccessControl(file, AccessControlSections.Access);

            // Assert
            Assert.NotEmpty(security.GetAccessRules(includeExplicit: true, includeInherited: true, typeof(SecurityIdentifier)));
        }

        /// <summary>
        /// Verifies that a file the caller just created can be opened for reading, and that the same check
        /// on a file that is not there is reported rather than answered.
        /// </summary>
        [Fact]
        public void TestFileAccess_AnswersForAFileTheCallerOwns()
        {
            // Arrange
            using TempDirectory temp = new();
            FileInfo file = new(temp.WriteFile("access.txt", "content"));

            // Act & Assert
            Assert.True(FileSystemUtilities.TestFileAccess(file));
            Assert.True(FileSystemUtilities.TestFileAccess(file, FileSystemRights.Read));
            Assert.True(FileSystemUtilities.TestFileAccess(file, FileSystemRights.Write));
        }

        /// <summary>
        /// Verifies that a file held open without sharing reports as inaccessible rather than throwing,
        /// which is the case the wrapper's own error handling exists for.
        /// </summary>
        [Fact]
        public void TestFileAccess_ReportsAFileThatCannotBeOpened()
        {
            // Arrange
            using TempDirectory temp = new();
            FileInfo file = new(temp.WriteFile("locked.txt", "content"));

            // Act & Assert: hold it exclusively, so the check cannot open it
            using (FileStream exclusive = new(file.FullName, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                Assert.False(FileSystemUtilities.TestFileAccess(file));
            }

            // Assert: and once released, it can be opened again
            Assert.True(FileSystemUtilities.TestFileAccess(file));
        }

        /// <summary>
        /// Verifies that a file that is not there is reported as missing rather than as inaccessible, so a
        /// caller can tell the two apart.
        /// </summary>
        [Fact]
        public void TestFileAccess_ReportsAMissingFile()
        {
            // Arrange
            using TempDirectory temp = new();

            // Act & Assert
            _ = Assert.Throws<FileNotFoundException>(() => FileSystemUtilities.TestFileAccess(new FileInfo(temp.GetPath("absent.txt"))));
        }

        /// <summary>
        /// Verifies that the caller's own token is granted the rights it needs on a file it just created.
        /// </summary>
        /// <remarks>
        /// Asked through the token rather than through the caller's identifier, because the identifier
        /// overload cannot answer for every account - see the next two tests - and the token is what the
        /// client and server permission check uses whenever it has one.
        /// </remarks>
        [Fact]
        public void GetEffectiveAccess_GrantsTheCallersOwnTokenAccessToItsOwnFile()
        {
            // Arrange
            using TempDirectory temp = new();
            FileInfo file = new(temp.WriteFile("effective.txt", "content"));
            using WindowsIdentity identity = WindowsIdentity.GetCurrent();

            // Act
            FileSystemRights granted = FileSystemUtilities.GetEffectiveAccess(file, identity.AccessToken, FileSystemRights.ReadAndExecute);

            // Assert
            Assert.True(granted.HasFlag(FileSystemRights.ReadAndExecute));
            Assert.True(FileSystemUtilities.TestEffectiveAccess(file, identity.AccessToken, FileSystemRights.ReadAndExecute));
        }

        /// <summary>
        /// Verifies that the identifier overload answers for a well-known account, which is the form the
        /// client and server permission check falls back to when it has no token.
        /// </summary>
        /// <remarks>
        /// The local system account is used rather than the caller's own on purpose. The underlying
        /// authorisation call resolves an identifier through its domain and cannot resolve one belonging
        /// to a cloud account: on an Entra joined device the caller's identifier has the form
        /// S-1-12-1-..., and asking about it fails with ERROR_NO_SUCH_DOMAIN. The classic identifiers
        /// resolve on any machine, so the test says the same thing everywhere.
        /// </remarks>
        [Fact]
        public void GetEffectiveAccess_AnswersForAWellKnownAccount()
        {
            // Arrange
            using TempDirectory temp = new();
            FileInfo file = new(temp.WriteFile("wellknown.txt", "content"));
            SecurityIdentifier localSystem = new(WellKnownSidType.LocalSystemSid, domainSid: null);

            // Act
            FileSystemRights granted = FileSystemUtilities.GetEffectiveAccess(file, localSystem, FileSystemRights.ReadAndExecute);

            // Assert
            Assert.True(granted.HasFlag(FileSystemRights.ReadAndExecute));
            Assert.True(FileSystemUtilities.TestEffectiveAccess(file, localSystem, FileSystemRights.ReadAndExecute));
        }

        /// <summary>
        /// Verifies that an account the file grants nothing to is reported as having nothing, rather than
        /// the check failing or falling back to a grant.
        /// </summary>
        [Fact]
        public void GetEffectiveAccess_ReportsNoAccessForAnAccountTheFileDoesNotGrant()
        {
            // Arrange
            using TempDirectory temp = new();
            FileInfo file = new(temp.WriteFile("denied.txt", "content"));
            SecurityIdentifier everyone = new(WellKnownSidType.WorldSid, domainSid: null);

            // Act & Assert: a file in the caller's own temporary directory grants nothing to everyone
            Assert.Equal(default, FileSystemUtilities.GetEffectiveAccess(file, everyone, FileSystemRights.ReadAndExecute));
            Assert.False(FileSystemUtilities.TestEffectiveAccess(file, everyone, FileSystemRights.ReadAndExecute));
        }

        /// <summary>
        /// Verifies that a directory is answered as readily as a file, since the check takes the base type
        /// and the two are distinguished only when reporting that one is missing.
        /// </summary>
        [Fact]
        public void GetEffectiveAccess_AnswersForADirectory()
        {
            // Arrange
            using TempDirectory temp = new();
            using WindowsIdentity identity = WindowsIdentity.GetCurrent();

            // Act & Assert
            Assert.True(FileSystemUtilities.TestEffectiveAccess(temp.Directory, identity.AccessToken, FileSystemRights.ReadAndExecute));
        }

        /// <summary>
        /// Verifies that a missing path is reported with the exception matching what was asked about, so a
        /// caller passing a directory does not receive a file error.
        /// </summary>
        [Fact]
        public void GetEffectiveAccess_ReportsAMissingPathByItsKind()
        {
            // Arrange
            using TempDirectory temp = new();
            SecurityIdentifier localSystem = new(WellKnownSidType.LocalSystemSid, domainSid: null);

            // Act & Assert
            _ = Assert.Throws<FileNotFoundException>(() => FileSystemUtilities.GetEffectiveAccess(new FileInfo(temp.GetPath("absent.txt")), localSystem, FileSystemRights.Read));
            _ = Assert.Throws<DirectoryNotFoundException>(() => FileSystemUtilities.GetEffectiveAccess(new DirectoryInfo(temp.GetPath("absent")), localSystem, FileSystemRights.Read));
        }

        /// <summary>
        /// Verifies that a file with no embedded signature is reported as untrusted.
        /// </summary>
        [Fact]
        public void IsAuthenticodeTrusted_ReportsAnUnsignedFileAsUntrusted()
        {
            // Arrange
            using TempDirectory temp = new();
            string path = temp.WriteFile("unsigned.txt", "not a signed binary");

            // Act & Assert
            Assert.False(FileSystemUtilities.IsAuthenticodeTrusted(path));
        }

        /// <summary>
        /// Verifies that a file that is not there is reported rather than answered, which is deliberate:
        /// the client and server paths depend on a missing executable failing loudly rather than being
        /// quietly treated as untrusted.
        /// </summary>
        [Fact]
        public void IsAuthenticodeTrusted_ReportsAMissingFile()
        {
            // Arrange
            using TempDirectory temp = new();

            // Act & Assert
            _ = Assert.Throws<FileNotFoundException>(() => FileSystemUtilities.IsAuthenticodeTrusted(temp.GetPath("absent.exe")));
        }

        /// <summary>
        /// Verifies that a binary carrying an embedded signature is reported as trusted.
        /// </summary>
        /// <remarks>
        /// Skipped when the machine has none of the candidates. The fixture deliberately excludes operating
        /// system binaries: they are signed through the system catalogues rather than in the image, and the
        /// check under test asks for a file-based verification that does not consult those, so
        /// <c language="text">notepad.exe</c> reports as untrusted despite being signed.
        /// </remarks>
        [Fact(Skip = "No binary with an embedded Authenticode signature was found on this machine.", SkipUnless = nameof(TestEnvironment.HasEmbeddedSignedExecutable), SkipType = typeof(TestEnvironment))]
        public void IsAuthenticodeTrusted_ReportsASignedBinaryAsTrusted()
        {
            // Arrange
            FileInfo? signed = TestEnvironment.EmbeddedSignedExecutable;
            Assert.NotNull(signed);

            // Act & Assert
            Assert.True(FileSystemUtilities.IsAuthenticodeTrusted(signed.FullName));
        }

        /// <summary>
        /// Verifies that a rule added to a file's access control is there when it is read back, which is
        /// how a deployment grants a user access to something it has just laid down.
        /// </summary>
        /// <remarks>
        /// Written to a file created for the test inside a temporary directory that is removed
        /// afterwards, so nothing on the machine is altered. The rule granted is to the caller's own
        /// account, which needs no privilege beyond ownership of the file.
        /// </remarks>
        [Fact]
        public void SetAccessControl_AppliesARuleToAFile()
        {
            // Arrange
            using TempDirectory temp = new();
            FileInfo file = new(temp.WriteFile("acl.txt", "contents"));
            using WindowsIdentity identity = WindowsIdentity.GetCurrent();
            Assert.NotNull(identity.User);

            // Act
            FileSecurity security = FileSystemUtilities.GetAccessControl(file, AccessControlSections.Access);
            security.AddAccessRule(new FileSystemAccessRule(identity.User, FileSystemRights.FullControl, AccessControlType.Allow));
            FileSystemUtilities.SetAccessControl(file, security);

            // Assert
            Assert.Contains(
                FileSystemUtilities.GetAccessControl(file, AccessControlSections.Access).GetAccessRules(includeExplicit: true, includeInherited: false, typeof(SecurityIdentifier)).Cast<FileSystemAccessRule>(),
                rule => rule.IdentityReference.Equals(identity.User) && rule.FileSystemRights.HasFlag(FileSystemRights.FullControl) && rule.AccessControlType is AccessControlType.Allow);
        }

        /// <summary>
        /// Verifies that a rule added to a directory's access control is there when it is read back.
        /// </summary>
        [Fact]
        public void SetAccessControl_AppliesARuleToADirectory()
        {
            // Arrange
            using TempDirectory temp = new();
            DirectoryInfo directory = temp.CreateSubdirectory("acl");
            using WindowsIdentity identity = WindowsIdentity.GetCurrent();
            Assert.NotNull(identity.User);

            // Act
            DirectorySecurity security = FileSystemUtilities.GetAccessControl(directory, AccessControlSections.Access);
            security.AddAccessRule(new FileSystemAccessRule(identity.User, FileSystemRights.FullControl, AccessControlType.Allow));
            FileSystemUtilities.SetAccessControl(directory, security);

            // Assert
            Assert.Contains(
                FileSystemUtilities.GetAccessControl(directory, AccessControlSections.Access).GetAccessRules(includeExplicit: true, includeInherited: false, typeof(SecurityIdentifier)).Cast<FileSystemAccessRule>(),
                rule => rule.IdentityReference.Equals(identity.User) && rule.FileSystemRights.HasFlag(FileSystemRights.FullControl));
        }

        /// <summary>
        /// Verifies that changing an owner is refused outright without the privilege Windows requires for
        /// it, rather than being attempted and failing part-way through.
        /// </summary>
        /// <remarks>
        /// The refusal is the library's rather than Windows'. It matters because a caller reassigning
        /// ownership is usually doing so to regain access to something another account created, and an
        /// attempt that half-succeeded would leave the item in a worse state than before.
        /// </remarks>
        [Fact(Skip = "Requires a caller without the privilege to take ownership.", SkipWhen = nameof(TestEnvironment.HasTakeOwnershipPrivilege), SkipType = typeof(TestEnvironment))]
        public void SetOwner_RefusesACallerWithoutTheTakeOwnershipPrivilege()
        {
            // Arrange
            using TempDirectory temp = new();
            FileInfo file = new(temp.WriteFile("owner.txt", "contents"));
            using WindowsIdentity identity = WindowsIdentity.GetCurrent();
            Assert.NotNull(identity.User);

            // Act & Assert
            _ = Assert.Throws<UnauthorizedAccessException>(() => FileSystemUtilities.SetOwner(file, identity.User));
            _ = Assert.Throws<UnauthorizedAccessException>(() => FileSystemUtilities.SetOwner(temp.Directory, identity.User));
        }

        /// <summary>
        /// Verifies that an owner set on a file is the owner read back from it.
        /// </summary>
        /// <remarks>
        /// The caller's own account is used as the new owner rather than a well-known one, so the file is
        /// left owned by whoever created it and the temporary directory can still be removed afterwards.
        /// </remarks>
        [Fact(Skip = "Requires the privilege to take ownership of a file.", SkipUnless = nameof(TestEnvironment.HasTakeOwnershipPrivilege), SkipType = typeof(TestEnvironment))]
        public void SetOwner_AppliesTheOwnerToAFile()
        {
            // Arrange
            using TempDirectory temp = new();
            FileInfo file = new(temp.WriteFile("owner.txt", "contents"));
            using WindowsIdentity identity = WindowsIdentity.GetCurrent();
            Assert.NotNull(identity.User);

            // Act
            FileSystemUtilities.SetOwner(file, identity.User);

            // Assert
            Assert.Equal(identity.User, FileSystemUtilities.GetAccessControl(file, AccessControlSections.Owner).GetOwner(typeof(SecurityIdentifier)));
        }

        /// <summary>
        /// Verifies that an owner set on a directory is the owner read back from it.
        /// </summary>
        [Fact(Skip = "Requires the privilege to take ownership of a directory.", SkipUnless = nameof(TestEnvironment.HasTakeOwnershipPrivilege), SkipType = typeof(TestEnvironment))]
        public void SetOwner_AppliesTheOwnerToADirectory()
        {
            // Arrange
            using TempDirectory temp = new();
            DirectoryInfo directory = temp.CreateSubdirectory("owner");
            using WindowsIdentity identity = WindowsIdentity.GetCurrent();
            Assert.NotNull(identity.User);

            // Act
            FileSystemUtilities.SetOwner(directory, identity.User);

            // Assert
            Assert.Equal(identity.User, FileSystemUtilities.GetAccessControl(directory, AccessControlSections.Owner).GetOwner(typeof(SecurityIdentifier)));
        }

        /// <summary>
        /// Verifies that a null argument is refused by both owner overloads.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0191:Do not use the null-forgiving operator", Justification = "This is deliberate as part of unit testing.")]
        [Fact]
        public void SetOwner_RefusesNullArguments()
        {
            // Arrange
            using TempDirectory temp = new();
            FileInfo file = new(temp.WriteFile("owner.txt", "contents"));
            using WindowsIdentity identity = WindowsIdentity.GetCurrent();
            Assert.NotNull(identity.User);

            // Act & Assert
            _ = Assert.Throws<ArgumentNullException>(() => FileSystemUtilities.SetOwner((FileInfo)null!, identity.User));
            _ = Assert.Throws<ArgumentNullException>(() => FileSystemUtilities.SetOwner((DirectoryInfo)null!, identity.User));
            _ = Assert.Throws<ArgumentNullException>(() => FileSystemUtilities.SetOwner(file, null!));
            _ = Assert.Throws<ArgumentNullException>(() => FileSystemUtilities.SetOwner(temp.Directory, null!));
        }

        /// <summary>
        /// Verifies that resetting a directory's permissions removes the explicit rules from it and from
        /// what it contains, leaving only what is inherited.
        /// </summary>
        /// <remarks>
        /// This is the equivalent of the checkbox that replaces every child's permissions with the ones
        /// inherited from the parent, so a rule placed on a file underneath is the thing to watch: it has
        /// to be gone afterwards, not merely gone from the directory it sat in.
        /// <para>
        /// Performed on a directory created for the test and removed afterwards.
        /// </para>
        /// </remarks>
        [Fact]
        public void ResetPermissionsForPath_RemovesExplicitRulesFromTheTree()
        {
            // Arrange
            using TempDirectory temp = new();
            DirectoryInfo directory = temp.CreateSubdirectory("reset");
            FileInfo file = new(Path.Join(directory.FullName, "child.txt"));
            File.WriteAllText(file.FullName, "contents");
            using WindowsIdentity identity = WindowsIdentity.GetCurrent();
            Assert.NotNull(identity.User);

            // Arrange: an explicit rule on the file underneath, which is what should be swept away
            FileSecurity security = FileSystemUtilities.GetAccessControl(file, AccessControlSections.Access);
            security.SetAccessRuleProtection(isProtected: true, preserveInheritance: true);
            security.AddAccessRule(new FileSystemAccessRule(identity.User, FileSystemRights.FullControl, AccessControlType.Allow));
            FileSystemUtilities.SetAccessControl(file, security);
            Assert.NotEmpty(ExplicitRules(file));

            // Act
            FileSystemUtilities.ResetPermissionsForPath(directory.FullName);

            // Assert
            Assert.Empty(ExplicitRules(file));
            Assert.False(FileSystemUtilities.GetAccessControl(file, AccessControlSections.Access).AreAccessRulesProtected);
        }

        /// <summary>
        /// Verifies that resetting the permissions of a directory that is not there is reported rather
        /// than quietly doing nothing.
        /// </summary>
        [Fact]
        public void ResetPermissionsForPath_ReportsAMissingDirectory()
        {
            using TempDirectory temp = new();
            _ = Assert.Throws<DirectoryNotFoundException>(() => FileSystemUtilities.ResetPermissionsForPath(temp.GetPath("absent")));
        }

        /// <summary>
        /// Verifies that resetting the permissions of nothing at all is refused.
        /// </summary>
        /// <param name="path">The blank path to refuse.</param>
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void ResetPermissionsForPath_RefusesABlankPath(string path)
        {
            _ = Assert.Throws<ArgumentException>(() => FileSystemUtilities.ResetPermissionsForPath(path));
        }

        /// <summary>
        /// The access rules set directly on a file, ignoring anything it inherits.
        /// </summary>
        /// <param name="file">The file to read.</param>
        /// <returns>Its explicit rules.</returns>
        private static FileSystemAccessRule[] ExplicitRules(FileInfo file)
        {
            return [.. FileSystemUtilities.GetAccessControl(file, AccessControlSections.Access).GetAccessRules(includeExplicit: true, includeInherited: false, typeof(SecurityIdentifier)).Cast<FileSystemAccessRule>()];
        }

        /// <summary>
        /// Writes a file of exactly the given length.
        /// </summary>
        /// <param name="path">Where to write it.</param>
        /// <param name="length">How many bytes it should hold.</param>
        private static void WriteFileOfSize(string path, int length)
        {
            File.WriteAllBytes(path, new byte[length]);
        }
    }
}
