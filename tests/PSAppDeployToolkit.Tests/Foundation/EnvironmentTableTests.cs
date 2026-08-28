using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using PSAppDeployToolkit.Foundation;
using PSADT.PowerShellTestFixture;
using PSAppDeployToolkit.Tests.TestHelpers;
using Xunit;

namespace PSAppDeployToolkit.Tests.Foundation
{
    /// <summary>
    /// Tests the snapshot of the machine and session the module takes once during initialisation.
    /// </summary>
    /// <remarks>
    /// Built through its real constructor rather than fabricated, which takes a live <c>PSCmdlet</c> and
    /// therefore a hosted PowerShell engine - see <see cref="PowerShellFixture"/> for why that is the only way in.
    /// <para>
    /// This file covers the constructor's contract and the type's identity. The hundred-odd derived members are
    /// asserted separately; what is here is the part that had to be settled first, because it decided whether the
    /// type stays a record.
    /// </para>
    /// </remarks>
    /// <param name="powerShell">The hosted engine, shared across the collection.</param>
    [Collection(PowerShellCollection.Name)]
    public sealed class EnvironmentTableTests(PowerShellFixture powerShell)
    {
        /// <summary>
        /// Verifies that two tables built moments apart are not equal, even though the machine did not change
        /// between them.
        /// </summary>
        /// <remarks>
        /// This is the decision, written down. A table is a snapshot taken at a moment: a caller who initialises
        /// the module, keeps the table, and initialises again holds two snapshots, and telling them apart is the
        /// point. So the type is an identity rather than a value, and it is no longer declared as a record.
        /// <para>
        /// Worth noting that this test passed before the declaration changed, too - a record holding fifty
        /// <see cref="System.IO.DirectoryInfo"/> properties was already comparing by reference through them, so it
        /// never delivered the value equality it advertised. Removing the record removed a promise nothing kept
        /// rather than any behaviour a caller could have relied on.
        /// </para>
        /// </remarks>
        [Fact]
        public void Equals_IsByIdentityRatherThanByValue()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            EnvironmentTable first = powerShell.NewEnvironmentTable();
            EnvironmentTable second = powerShell.NewEnvironmentTable();

            // Assert
            Assert.NotEqual(first, second);
            Assert.NotSame(first, second);
            Assert.Equal(first, first);
        }

        /// <summary>
        /// Verifies that a table is equal to itself and that its hash code does not move.
        /// </summary>
        /// <remarks>
        /// The other half of an identity: reference equality is only useful if it is stable, and a hash code that
        /// varied between reads would make the table unusable as a dictionary key or set member. Several of its
        /// members delegate to live probes on each read, which is exactly how a record's generated hash code
        /// would have drifted.
        /// </remarks>
        [Fact]
        public void GetHashCode_DoesNotChangeBetweenReads()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            EnvironmentTable table = powerShell.NewEnvironmentTable();

            // Assert
            Assert.Equal(table.GetHashCode(), table.GetHashCode());
        }

        /// <summary>
        /// Verifies that the constructor refuses a missing cmdlet, version table or version.
        /// </summary>
        /// <remarks>
        /// The cmdlet argument is checked last in the constructor even though it is checked for null, so a caller
        /// passing nothing at all gets told about the version table first. Asserted individually rather than as a
        /// set so the parameter name in each exception is confirmed.
        /// </remarks>
        [Fact]
        public void EnvironmentTable_RefusesMissingArguments()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();

            // Assert
            Assert.Equal("psVersionTable", Assert.Throws<ArgumentNullException>(static () => new EnvironmentTable(null!, null!, null!)).ParamName);
            Assert.Equal("psVersion", Assert.Throws<ArgumentNullException>(static () => new EnvironmentTable(null!, [], null!)).ParamName);
            Assert.Equal("cmdlet", Assert.Throws<ArgumentNullException>(static () => new EnvironmentTable(null!, [], new Version(7, 4))).ParamName);
        }

        /// <summary>
        /// Verifies that the table names the module it was built from.
        /// </summary>
        /// <remarks>
        /// Not incidental. These three come from the module the constructing cmdlet belongs to, and
        /// <c>AppDeployToolkitName</c> goes on to be built into install names, log file names and the registry
        /// path deferral history is kept under, so a table that could not find its module would break all three.
        /// </remarks>
        [Fact]
        public void EnvironmentTable_NamesTheModuleItWasBuiltFrom()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            EnvironmentTable table = powerShell.NewEnvironmentTable();

            // Assert
            Assert.Equal(PowerShellFixture.ModuleName, table.AppDeployToolkitName);
            Assert.False(string.IsNullOrWhiteSpace(table.AppDeployToolkitPath));
            Assert.NotNull(table.AppDeployMainScriptVersion);
        }

        /// <summary>
        /// Verifies that the table carries the version table and engine version it was handed.
        /// </summary>
        [Fact]
        public void EnvironmentTable_CarriesTheEngineVersionItWasHanded()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            EnvironmentTable table = powerShell.NewEnvironmentTable();

            // Assert
            Assert.NotNull(table.EnvPSVersionTable);
            Assert.True(table.EnvPSVersionTable.Count > 0);
        }

        /// <summary>
        /// Verifies that each version's parts report the parts of the version they came from.
        /// </summary>
        /// <remarks>
        /// Sixteen near-identical properties across four families, which is exactly the shape a copy-paste error
        /// hides in - a minor returning a major, or a family reading another family's composite. Asserted as one
        /// labelled sequence so a single mis-wired part names itself.
        /// </remarks>
        [Fact]
        public void VersionParts_ProjectTheirComposite()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            EnvironmentTable table = powerShell.NewEnvironmentTable();

            // Assert
            string[] expected =
            [
                .. FromComposite(nameof(table.EnvHostVersion), table.EnvHostVersion),
                .. FromComposite(nameof(table.EnvOSVersion), table.EnvOSVersion),
                .. FromComposite(nameof(table.EnvPSVersion), table.EnvPSVersion),
                .. FromComposite(nameof(table.EnvCLRVersion), table.EnvCLRVersion),
            ];
            string[] actual =
            [
                .. FromTable(nameof(table.EnvHostVersion), table.EnvHostVersionMajor, table.EnvHostVersionMinor, table.EnvHostVersionBuild, table.EnvHostVersionRevision),
                .. FromTable(nameof(table.EnvOSVersion), table.EnvOSVersionMajor, table.EnvOSVersionMinor, table.EnvOSVersionBuild, table.EnvOSVersionRevision),
                .. FromTable(nameof(table.EnvPSVersion), table.EnvPSVersionMajor, table.EnvPSVersionMinor, table.EnvPSVersionBuild, table.EnvPSVersionRevision),
                .. FromTable(nameof(table.EnvCLRVersion), table.EnvCLRVersionMajor, table.EnvCLRVersionMinor, table.EnvCLRVersionBuild, table.EnvCLRVersionRevision),
            ];
            Assert.Equal(expected, actual);
        }

        /// <summary>
        /// Verifies that a build or revision of zero is reported, while one the version does not carry is not.
        /// </summary>
        /// <remarks>
        /// The distinction the table draws, and it cannot be reached with the engine's own version, which carries
        /// all four parts and none of them zero. A comparison against zero rather than against a negative would
        /// pass on this machine and turn <c>1.2.0.0</c> into a version with no build, so the case is fed in
        /// rather than waited for.
        /// </remarks>
        /// <param name="psVersion">The version to hand the table.</param>
        /// <param name="expectedBuild">The build it should report.</param>
        /// <param name="expectedRevision">The revision it should report.</param>
        [Theory]
        [InlineData("1.2", Absent, Absent)]
        [InlineData("1.2.0", "0", Absent)]
        [InlineData("1.2.0.0", "0", "0")]
        [InlineData("1.2.3.4", "3", "4")]
        public void VersionParts_ReportZeroPartsButNotAbsentOnes(string psVersion, string expectedBuild, string expectedRevision)
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            EnvironmentTable table = powerShell.NewEnvironmentTable(new Version(psVersion), clrVersion: null);

            // Assert
            Assert.Equal(1, table.EnvPSVersionMajor);
            Assert.Equal(2, table.EnvPSVersionMinor);
            Assert.Equal(expectedBuild, Part(table.EnvPSVersionBuild));
            Assert.Equal(expectedRevision, Part(table.EnvPSVersionRevision));
        }

        /// <summary>
        /// Verifies that the CLR members are all absent when the version table records no CLR version.
        /// </summary>
        /// <remarks>
        /// One of the two CLR cases, and neither is reachable on both frameworks unaided: Windows PowerShell
        /// always records a CLR version and PowerShell 7 never does, so whichever framework runs would otherwise
        /// leave one branch of four properties untouched.
        /// </remarks>
        [Fact]
        public void CLRVersionParts_AreAbsentWhenNoCLRVersionIsRecorded()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            EnvironmentTable table = powerShell.NewEnvironmentTable(new Version(7, 4), clrVersion: null);

            // Assert
            Assert.Null(table.EnvCLRVersion);
            Assert.Null(table.EnvCLRVersionMajor);
            Assert.Null(table.EnvCLRVersionMinor);
            Assert.Null(table.EnvCLRVersionBuild);
            Assert.Null(table.EnvCLRVersionRevision);
        }

        /// <summary>
        /// Verifies that the CLR members report the CLR version the version table records.
        /// </summary>
        [Fact]
        public void CLRVersionParts_ProjectTheRecordedCLRVersion()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            EnvironmentTable table = powerShell.NewEnvironmentTable(new Version(7, 4), new Version(4, 0, 30319, 42000));

            // Assert
            Assert.Equal(new Version(4, 0, 30319, 42000), table.EnvCLRVersion);
            Assert.Equal(4, table.EnvCLRVersionMajor);
            Assert.Equal(0, table.EnvCLRVersionMinor);
            Assert.Equal(30319, table.EnvCLRVersionBuild);
            Assert.Equal(42000, table.EnvCLRVersionRevision);
        }

        /// <summary>
        /// Verifies that the three kinds of Windows are exclusive and that the name agrees with the flags.
        /// </summary>
        /// <remarks>
        /// Two independent mappings off one number - a number to three booleans, and the same number to a name -
        /// so tying them to each other catches a wrong constant in either without restating either. The product
        /// type is one of three values on Windows, so exactly one kind holds and the name is never
        /// <c>Unknown</c>.
        /// </remarks>
        [Fact]
        public void EnvOSProductType_NamesExactlyOneKindOfOS()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            EnvironmentTable table = powerShell.NewEnvironmentTable();
            bool[] kinds = [table.IsWorkstationOS, table.IsDomainControllerOS, table.IsServerOS];

            // Assert
            Assert.Equal(1, kinds.Count(static kind => kind));
            Assert.Equal(table.IsWorkstationOS, string.Equals(table.EnvOSProductTypeName, "Workstation", StringComparison.Ordinal));
            Assert.Equal(table.IsDomainControllerOS, string.Equals(table.EnvOSProductTypeName, "Domain Controller", StringComparison.Ordinal));
            Assert.Equal(table.IsServerOS, string.Equals(table.EnvOSProductTypeName, "Server", StringComparison.Ordinal));
        }

        /// <summary>
        /// Verifies that the hardware type is one of the names the classification can produce, and that a virtual
        /// one says so first.
        /// </summary>
        /// <remarks>
        /// A seven-way classification off SMBIOS strings, none of which can be injected, so which branch runs is
        /// decided by the machine and the other six are unreachable from here. What is assertable is the contract
        /// callers actually use: the vocabulary is closed, and anything virtual is prefixed so a script can ask
        /// whether it is on a virtual machine without naming the hypervisor. A renamed or newly added value would
        /// break those callers silently.
        /// </remarks>
        [Fact]
        public void EnvHardwareType_IsOneOfTheNamesTheClassificationProduces()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            EnvironmentTable table = powerShell.NewEnvironmentTable();
            string[] names = ["Physical", "Virtual", "Virtual:Hyper-V", "Virtual:Virtual PC", "Virtual:Xen", "Virtual:VMware", "Virtual:Parallels"];

            // Assert
            Assert.Contains(table.EnvHardwareType, names, StringComparer.Ordinal);

            // Assert: physical or prefixed, with nothing in between.
            Assert.True(
                string.Equals(table.EnvHardwareType, "Physical", StringComparison.Ordinal) || table.EnvHardwareType.StartsWith("Virtual", StringComparison.Ordinal),
                table.EnvHardwareType);
        }

        /// <summary>
        /// Verifies that the caller's identity, rights and account kind agree with what Windows reports.
        /// </summary>
        /// <remarks>
        /// Read through <see cref="WindowsIdentity"/> rather than through the same helper the table uses, so this
        /// is a second opinion rather than a restatement. <c>IsAdmin</c> is the one that matters most: a session
        /// that requires elevation is refused on the strength of it.
        /// </remarks>
        [Fact]
        public void CallerIdentity_AgreesWithTheOperatingSystem()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            EnvironmentTable table = powerShell.NewEnvironmentTable();
            using WindowsIdentity identity = WindowsIdentity.GetCurrent();

            // Assert
            Assert.Equal(identity.User, table.CurrentProcessSID);
            Assert.Equal(identity.Name, table.ProcessNTAccount.Value, StringComparer.OrdinalIgnoreCase);
            Assert.Equal(new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator), table.IsAdmin);
            Assert.Equal(identity.User?.IsWellKnown(WellKnownSidType.LocalSystemSid) is true, table.IsLocalSystemAccount);
            Assert.Equal(identity.User?.IsWellKnown(WellKnownSidType.LocalServiceSid) is true, table.IsLocalServiceAccount);
            Assert.Equal(identity.User?.IsWellKnown(WellKnownSidType.NetworkServiceSid) is true, table.IsNetworkServiceAccount);
        }

        /// <summary>
        /// Verifies that session zero is the service account flags taken together.
        /// </summary>
        /// <remarks>
        /// Named for the session but derived from the account: the property never looks at a session identifier,
        /// so a service account running in an interactive session still reports session zero. Pinned because the
        /// name invites the other reading, and because the module chooses whether a dialog can be shown on the
        /// strength of it.
        /// </remarks>
        [Fact]
        public void SessionZero_IsTheServiceAccountFlagsCombined()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            EnvironmentTable table = powerShell.NewEnvironmentTable();

            // Assert
            Assert.Equal(table.IsLocalSystemAccount || table.IsLocalServiceAccount || table.IsNetworkServiceAccount || table.IsServiceAccount, table.SessionZero);

            // Assert: the three well-known accounts are distinct, so at most one of them can be the caller.
            bool[] wellKnownAccounts = [table.IsLocalSystemAccount, table.IsLocalServiceAccount, table.IsNetworkServiceAccount];
            Assert.InRange(wellKnownAccounts.Count(static flag => flag), 0, 1);
        }

        /// <summary>
        /// Verifies that the table's several accounts of the process bitness agree with each other.
        /// </summary>
        /// <remarks>
        /// The pointer size is the independent witness. The architecture check matters because the two are read
        /// from different places and the module picks registry views and program files directories off them.
        /// </remarks>
        [Fact]
        public void ProcessBitness_IsConsistentAcrossTheTable()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            EnvironmentTable table = powerShell.NewEnvironmentTable();

            // Assert
            Assert.Equal(IntPtr.Size is 8, table.Is64BitProcess);
            Assert.Equal(table.Is64BitProcess, table.PSArchitecture is Architecture.X64 or Architecture.Arm64);

            // Assert: a 64-bit process cannot be hosted by a 32-bit operating system.
            Assert.True(table.Is64Bit || !table.Is64BitProcess);
        }

        /// <summary>
        /// Verifies that the system directories are oriented for the process bitness and that the profile paths
        /// are built from them.
        /// </summary>
        /// <remarks>
        /// The two profile paths are assembled from the two system directories by name, and nothing but their
        /// parent tells them apart, so a swapped pair would produce two plausible paths pointing the wrong way
        /// round. Which orientation applies is decided by the machine, so both are described rather than one
        /// assumed.
        /// </remarks>
        [Fact]
        public void SystemDirectories_AreOrientedForTheProcessBitness()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            EnvironmentTable table = powerShell.NewEnvironmentTable();

            // Assert: the system drive is the root of the system directory.
            Assert.Equal(table.EnvSystem32Directory.Root.FullName, table.EnvSystemDrive.Name);

            // Assert: a native directory always exists, and the profile beneath it is named from it.
            Assert.NotNull(table.EnvSysNativeDirectory);
            Assert.Equal(Path.Join(table.EnvSysNativeDirectory.FullName, "Config", "systemprofile"), table.EnvSystemProfile?.FullName);

            if (!table.Is64Bit)
            {
                // Assert: a 32-bit machine has no WOW64 layer, so neither the directory nor its profile exists.
                Assert.Null(table.EnvSysWow64Directory);
                Assert.Null(table.EnvSystemProfileX86);
                return;
            }

            // Assert: on a 64-bit machine the pair straddles the process, and the x86 profile follows the WOW64
            // directory just as the native profile follows the native one.
            Assert.NotNull(table.EnvSysWow64Directory);
            Assert.Equal(Path.Join(table.EnvSysWow64Directory.FullName, "Config", "systemprofile"), table.EnvSystemProfileX86?.FullName);
            Assert.NotEqual(table.EnvSysNativeDirectory.FullName, table.EnvSysWow64Directory.FullName, StringComparer.OrdinalIgnoreCase);
            Assert.Equal(
                table.Is64BitProcess ? table.EnvSysNativeDirectory.FullName : table.EnvSysWow64Directory.FullName,
                table.EnvSystem32Directory.FullName,
                StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Verifies that a machine is described as domain-joined or as a workgroup member, never as both.
        /// </summary>
        /// <remarks>
        /// The constructor fills one side or the other and cases each differently - a domain lowercased, a
        /// workgroup uppercased - and the module builds both into log lines and registry lookups. Which side
        /// applies is decided by the machine, so both are described.
        /// </remarks>
        [Fact]
        public void DomainMembership_FillsOneSideAndCasesIt()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            EnvironmentTable table = powerShell.NewEnvironmentTable();

            // Assert
            if (table.IsMachinePartOfDomain)
            {
                Assert.Null(table.EnvMachineWorkgroup);
                Assert.StartsWith(table.EnvComputerName, table.EnvComputerNameFQDN, StringComparison.OrdinalIgnoreCase);
                if (table.EnvMachineADDomain is string domain)
                {
                    Assert.Equal(CultureInfo.InvariantCulture.TextInfo.ToLower(domain), domain, StringComparer.Ordinal);
                }
            }
            else
            {
                Assert.Null(table.EnvMachineADDomain);
                Assert.Equal(table.EnvComputerName, table.EnvComputerNameFQDN, StringComparer.OrdinalIgnoreCase);
                if (table.EnvMachineWorkgroup is string workgroup)
                {
                    Assert.Equal(workgroup.ToUpperInvariant(), workgroup, StringComparer.Ordinal);
                }
            }

            // Assert: the logon server is stripped of the leading slashes the environment reports it with.
            if (table.EnvLogonServer is string logonServer)
            {
                Assert.False(logonServer.StartsWith('\\'));
            }

            // Assert: the user's two domains are cased the opposite way from each other, deliberately.
            if (table.EnvUserDomain is string userDomain)
            {
                Assert.Equal(userDomain.ToUpperInvariant(), userDomain, StringComparer.Ordinal);
            }
            if (table.EnvUserDNSDomain is string userDnsDomain)
            {
                Assert.Equal(CultureInfo.InvariantCulture.TextInfo.ToLower(userDnsDomain), userDnsDomain, StringComparer.Ordinal);
            }
        }

        /// <summary>
        /// Verifies that the two language names are the uppercased two-letter codes of their cultures.
        /// </summary>
        /// <remarks>
        /// A three-letter code or a missing uppercasing would both show here. Whether the two are wired to the
        /// right culture cannot be settled while a machine's culture and UI culture agree, which is the usual
        /// case and is so on this one.
        /// </remarks>
        [Fact]
        public void LanguageNames_AreTheUppercasedTwoLetterCodes()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            EnvironmentTable table = powerShell.NewEnvironmentTable();

            // Assert
            Assert.Equal(table.Culture.TwoLetterISOLanguageName, table.CurrentLanguage, StringComparer.OrdinalIgnoreCase);
            Assert.Equal(table.CurrentLanguage.ToUpperInvariant(), table.CurrentLanguage, StringComparer.Ordinal);
            Assert.Equal(table.UICulture.TwoLetterISOLanguageName, table.CurrentUILanguage, StringComparer.OrdinalIgnoreCase);
            Assert.Equal(table.CurrentUILanguage.ToUpperInvariant(), table.CurrentUILanguage, StringComparer.Ordinal);
        }

        /// <summary>
        /// Verifies that the session members are drawn from the one list of sessions.
        /// </summary>
        /// <remarks>
        /// Four members are projections of the same list, two of them picked by a flag that differs only in its
        /// name - the current session and the console session - so a swapped pair would be invisible except
        /// here. The accounts are a projection in order, which is what lets the module report the two together.
        /// </remarks>
        [Fact]
        public void SessionMembers_AreProjectionsOfTheSessionList()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            EnvironmentTable table = powerShell.NewEnvironmentTable();

            // Assert
            Assert.NotNull(table.LoggedOnUserSessions);
            Assert.NotNull(table.UsersLoggedOn);
            Assert.Equal(
                [.. table.LoggedOnUserSessions.Select(static session => session.NTAccount.Value)],
                [.. table.UsersLoggedOn.Select(static account => account.Value)]);

            // Assert
            if (table.CurrentLoggedOnUserSession is not null)
            {
                Assert.True(table.CurrentLoggedOnUserSession.IsCurrentSession);
                Assert.Contains(table.LoggedOnUserSessions, session => ReferenceEquals(session, table.CurrentLoggedOnUserSession));
            }
            if (table.CurrentConsoleUserSession is not null)
            {
                Assert.True(table.CurrentConsoleUserSession.IsConsoleSession);
                Assert.Contains(table.LoggedOnUserSessions, session => ReferenceEquals(session, table.CurrentConsoleUserSession));
            }

            // Assert: everything about the active user hangs off having found one.
            if (table.RunAsActiveUser is null)
            {
                Assert.Null(table.RunAsUserProfile);
                Assert.Null(table.RunAsActiveUserLocale);
                Assert.Null(table.UserProfileName);
            }
            else
            {
                Assert.Equal(table.RunAsUserProfile?.Name, table.UserProfileName, StringComparer.Ordinal);
            }
        }

        /// <summary>
        /// Verifies that the Office members say nothing the Office variables do not.
        /// </summary>
        /// <remarks>
        /// Three members are read out of one registry key, so the thing worth asserting is that each is read
        /// from the value it claims. The channel names are a lookup of five identifiers and only the one this
        /// machine is on could ever be reached, so that mapping is left alone.
        /// </remarks>
        [Fact]
        public void OfficeMembers_AreReadFromTheOfficeVariables()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            EnvironmentTable table = powerShell.NewEnvironmentTable();

            // Assert: with no Click-to-Run key there is nothing for any of them to report.
            if (table.EnvOfficeVars is null)
            {
                Assert.Null(table.EnvOfficeVersion);
                Assert.Null(table.EnvOfficeBitness);
                Assert.Null(table.EnvOfficeChannel);
                return;
            }

            // Assert: the version is reported only when the value it comes from parses as one.
            Assert.Equal(
                table.EnvOfficeVars.TryGetValue("VersionToReport", out object? reported) && Version.TryParse(reported as string, out _),
                table.EnvOfficeVersion is not null);

            // Assert
            Assert.Equal(
                table.EnvOfficeVars.TryGetValue("Platform", out object? platform) ? platform as string : null,
                table.EnvOfficeBitness,
                StringComparer.Ordinal);
        }

        /// <summary>
        /// Verifies that the invalid file name characters and the pattern built from them agree with the
        /// framework's list.
        /// </summary>
        /// <remarks>
        /// Both are used to sanitise the log file name a session derives from its install name, so a pattern
        /// that missed a character would produce a name the file system refuses.
        /// </remarks>
        [Fact]
        public void InvalidFileNameChars_AndItsPatternAgreeWithTheFramework()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            EnvironmentTable table = powerShell.NewEnvironmentTable();

            // Assert
            Assert.Equal(Path.GetInvalidFileNameChars(), table.InvalidFileNameChars);
            Assert.All(table.InvalidFileNameChars, character => Assert.Matches(table.InvalidFileNameCharsRegexPattern, character.ToString()));
            Assert.DoesNotMatch(table.InvalidFileNameCharsRegexPattern, "Install-Application_1.0.log");
        }

        /// <summary>
        /// Verifies that the well-known accounts name the accounts their security identifiers belong to.
        /// </summary>
        /// <remarks>
        /// Each is built by translating a <see cref="WellKnownSidType"/> to a name, so the names are checked by
        /// translating back the other way. A wrong constant would still produce a real account, which is why it
        /// takes a round trip to notice: the module adds users to these groups and tests membership against them.
        /// </remarks>
        [Fact]
        public void WellKnownAccounts_NameTheAccountsTheirSidsBelongTo()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            EnvironmentTable table = powerShell.NewEnvironmentTable();

            // Assert
            Assert.True(SidOf(table.LocalSystemNTAccount).IsWellKnown(WellKnownSidType.LocalSystemSid), table.LocalSystemNTAccount.Value);
            Assert.True(SidOf(table.LocalUsersGroup).IsWellKnown(WellKnownSidType.BuiltinUsersSid), table.LocalUsersGroup.Value);
            Assert.True(SidOf(table.LocalAdministratorsGroup).IsWellKnown(WellKnownSidType.BuiltinAdministratorsSid), table.LocalAdministratorsGroup.Value);
        }

        /// <summary>
        /// Verifies that the collections the table hands out cannot be cast back to something mutable.
        /// </summary>
        /// <remarks>
        /// The table is a snapshot, and a caller who could reach the array or dictionary behind one of these
        /// members could change what a later reader sees. Swept rather than listed, so a member added later is
        /// held to the same rule without anyone remembering to add it here.
        /// </remarks>
        [Fact]
        public void ReadOnlyCollections_AreNotTheirBackingStores()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            EnvironmentTable table = powerShell.NewEnvironmentTable();
            PropertyInfo[] readOnlyCollections =
            [
                .. typeof(EnvironmentTable).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(static property => property.PropertyType.IsGenericType
                        && (property.PropertyType.GetGenericTypeDefinition() == typeof(IReadOnlyList<>)
                            || property.PropertyType.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>))),
            ];

            // Assert: the sweep found the members rather than none.
            Assert.True(readOnlyCollections.Length >= 5, $"Only {readOnlyCollections.Length.ToString(CultureInfo.InvariantCulture)} read-only collections were found.");

            // Assert
            foreach (PropertyInfo property in readOnlyCollections)
            {
                if (property.GetValue(table) is not object value)
                {
                    continue;
                }
                Type valueType = value.GetType();
                Assert.False(valueType.IsArray, $"{property.Name} handed out an array.");
                Assert.False(
                    valueType.IsGenericType && (valueType.GetGenericTypeDefinition() == typeof(List<>) || valueType.GetGenericTypeDefinition() == typeof(Dictionary<,>)),
                    $"{property.Name} handed out a {valueType.Name}.");
            }
        }

        /// <summary>
        /// Verifies that the reported memory is a plausible number of gibibytes.
        /// </summary>
        /// <remarks>
        /// The member carries no unit in its name and is derived by dividing a byte count, so the thing that can
        /// go wrong is the scale rather than the number. The bounds are wide enough never to fail on real
        /// hardware and narrow enough that a factor of a thousand either way would not fit between them.
        /// </remarks>
        [Fact]
        public void EnvSystemRAM_IsReportedInGibibytes()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            EnvironmentTable table = powerShell.NewEnvironmentTable();

            // Assert
            Assert.InRange(table.EnvSystemRAM, 0.5m, 4096m);
        }

        /// <summary>
        /// Verifies that the product code pattern accepts a braced GUID and nothing else.
        /// </summary>
        /// <remarks>
        /// The anchoring is the part worth asserting. An unanchored pattern would find a product code inside a
        /// longer string and report it as one, which is how a malformed code reaches Windows Installer.
        /// </remarks>
        [Fact]
        public void MsiProductCodeRegexPattern_AcceptsOnlyBracedGuids()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            EnvironmentTable table = powerShell.NewEnvironmentTable();
            string[] accepted =
            [
                "{2FB2E3A0-1A2B-4C3D-9E4F-5A6B7C8D9E0F}",
                "{2fb2e3a0-1a2b-4c3d-9e4f-5a6b7c8d9e0f}",
                "{00000000-0000-0000-0000-000000000000}",
            ];
            string[] rejected =
            [
                "2FB2E3A0-1A2B-4C3D-9E4F-5A6B7C8D9E0F",
                "{2FB2E3A0-1A2B-4C3D-9E4F-5A6B7C8D9E0}",
                "{2FB2E3A0-1A2B-4C3D-9E4F-5A6B7C8D9E0FF}",
                "{2FB2E3A0-1A2B-4C3D-9E4F-5A6B7C8D9E0G}",
                "{2FB2E3A01A2B4C3D9E4F5A6B7C8D9E0F}",
                " {2FB2E3A0-1A2B-4C3D-9E4F-5A6B7C8D9E0F}",
                "{2FB2E3A0-1A2B-4C3D-9E4F-5A6B7C8D9E0F} ",
                "MSI{2FB2E3A0-1A2B-4C3D-9E4F-5A6B7C8D9E0F}",
                "{2FB2E3A0-1A2B-4C3D-9E4F-5A6B7C8D9E0F}.msi",
            ];

            // Assert
            Assert.All(accepted, code => Assert.Matches(table.MsiProductCodeRegexPattern, code));
            Assert.All(rejected, code => Assert.DoesNotMatch(table.MsiProductCodeRegexPattern, code));
        }

        /// <summary>
        /// Verifies that the scheduled task name pattern matches each character Windows reserves and no other.
        /// </summary>
        [Fact]
        public void InvalidScheduledTaskNameCharsRegexPattern_MatchesTheReservedCharacters()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            EnvironmentTable table = powerShell.NewEnvironmentTable();

            // Assert
            Assert.All(@"\/:*?""<>|", character => Assert.Matches(table.InvalidScheduledTaskNameCharsRegexPattern, character.ToString()));
            Assert.DoesNotMatch(table.InvalidScheduledTaskNameCharsRegexPattern, "PSAppDeployToolkit Cleanup 1.0");
        }

        /// <summary>
        /// Verifies that every member of the table can be read.
        /// </summary>
        /// <remarks>
        /// The table is handed wholesale to script, which walks it to write the environment into a log, so a
        /// member that threw would take the whole deployment down rather than just itself. A third of them
        /// compute on each read - switches, chained null-conditionals, live probes - so being constructible is
        /// not the same as being readable.
        /// </remarks>
        [Fact]
        public void AllMembers_CanBeRead()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            EnvironmentTable table = powerShell.NewEnvironmentTable();
            PropertyInfo[] properties = typeof(EnvironmentTable).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            List<string> failures = [];

            // Act
            foreach (PropertyInfo property in properties)
            {
                try
                {
                    _ = property.GetValue(table);
                }
                catch (Exception ex) when (ex.Message is not null)
                {
                    failures.Add($"{property.Name}: {(ex as TargetInvocationException)?.InnerException?.Message ?? ex.Message}");
                }
            }

            // Assert
            Assert.Empty(failures);

            // Assert: the sweep found the table rather than an empty type.
            Assert.True(properties.Length > 100, $"Only {properties.Length.ToString(CultureInfo.InvariantCulture)} members were swept.");
        }

        /// <summary>
        /// Translates an account name back to the security identifier it belongs to.
        /// </summary>
        /// <param name="account">The account to translate.</param>
        /// <returns>The account's security identifier.</returns>
        private static SecurityIdentifier SidOf(NTAccount account)
        {
            return (SecurityIdentifier)account.Translate(typeof(SecurityIdentifier));
        }

        /// <summary>
        /// Renders the four parts of a version the way the table is expected to report them.
        /// </summary>
        /// <remarks>
        /// The expected side, read off <see cref="Version"/> itself. The rule for a part the version does not
        /// carry is written once here rather than trusted four times over in the type under test.
        /// </remarks>
        /// <param name="name">The name of the version, for the label.</param>
        /// <param name="version">The version to read.</param>
        /// <returns>One labelled line per part.</returns>
        private static IEnumerable<string> FromComposite(string name, Version? version)
        {
            yield return $"{name}.Major = {Part(version?.Major)}";
            yield return $"{name}.Minor = {Part(version?.Minor)}";
            yield return $"{name}.Build = {Part(version?.Build >= 0 ? version.Build : null)}";
            yield return $"{name}.Revision = {Part(version?.Revision >= 0 ? version.Revision : null)}";
        }

        /// <summary>
        /// Renders the four parts of a version as the table reports them.
        /// </summary>
        /// <param name="name">The name of the version, for the label.</param>
        /// <param name="major">The major part the table reported.</param>
        /// <param name="minor">The minor part the table reported.</param>
        /// <param name="build">The build part the table reported.</param>
        /// <param name="revision">The revision part the table reported.</param>
        /// <returns>One labelled line per part.</returns>
        private static IEnumerable<string> FromTable(string name, int? major, int? minor, int? build, int? revision)
        {
            yield return $"{name}.Major = {Part(major)}";
            yield return $"{name}.Minor = {Part(minor)}";
            yield return $"{name}.Build = {Part(build)}";
            yield return $"{name}.Revision = {Part(revision)}";
        }

        /// <summary>
        /// Renders one part of a version, naming the absence of one rather than leaving it blank.
        /// </summary>
        /// <param name="value">The part to render.</param>
        /// <returns>The part, or <see cref="Absent"/>.</returns>
        private static string Part(int? value)
        {
            return value?.ToString(CultureInfo.InvariantCulture) ?? Absent;
        }

        /// <summary>
        /// How a version part the version does not carry is written.
        /// </summary>
        private const string Absent = "absent";
    }
}
