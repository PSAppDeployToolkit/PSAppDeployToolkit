using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Reflection;
using Microsoft.Win32;
using PSADT.PowerShellTestFixture;
using PSADT.ProcessManagement;
using PSAppDeployToolkit.Foundation;
using PSAppDeployToolkit.Logging;
using PSAppDeployToolkit.Tests.TestHelpers;
using Xunit;

namespace PSAppDeployToolkit.Tests.Foundation
{
    /// <summary>
    /// Tests the object a deployment runs inside.
    /// </summary>
    /// <remarks>
    /// Built through its real constructor against a seated module database, which is the only way in: the
    /// constructor reads the configuration and environment out of the database and settles them for the lifetime of
    /// the session, so a mid-flight configuration change cannot disturb a deployment already under way.
    /// <para>
    /// The constructor is where nearly all of the behaviour is. It maps forty parameters, derives an install title
    /// and name, settles a log path, rotates logs, resolves the deployment mode and checks the session is viable -
    /// so most of what is tested here is asserted by constructing a session and looking at what came out.
    /// </para>
    /// </remarks>
    /// <param name="powerShell">The hosted engine, shared across the collection.</param>
    [Collection(PowerShellCollection.Name)]
    public sealed class DeploymentSessionTests(PowerShellFixture powerShell)
    {
        /// <summary>
        /// Verifies that every parameter the constructor reads lands on the property that belongs to it.
        /// </summary>
        /// <remarks>
        /// Twenty-seven near-identical blocks, each reading one key and casting it to one type, which is where a
        /// transposed key or a pasted-and-not-edited assignment hides. Every value here is distinct from every
        /// other of its type, so a pair read the wrong way round cannot agree by accident - the two versions and
        /// the two exit code lists are deliberately different from each other for that reason.
        /// </remarks>
        [Fact]
        public void DeploymentSession_MapsEveryParameterToItsOwnProperty()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            using TempDirectory temp = new();
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(Configuration(temp), powerShell.NewEnvironmentTable());
            DateTime scriptDate = new(2026, 3, 4, 0, 0, 0, DateTimeKind.Unspecified);
            Dictionary<string, object> parameters = new(StringComparer.OrdinalIgnoreCase)
            {
                { "DeploymentType", DeploymentType.Repair },
                { "DeployMode", DeployMode.Silent },
                { "AppVendor", "MappedVendor" },
                { "AppName", "MappedName" },
                { "AppVersion", "9.8.7" },
                { "AppArch", "MappedArch" },
                { "AppLang", "MappedLang" },
                { "AppRevision", "MappedRevision" },
                { "AppSuccessExitCodes", new[] { 8001, 8002 } },
                { "AppRebootExitCodes", new[] { 7001, 7002 } },
                { "AppProcessesToClose", new[] { new ProcessDefinition("mapped", "Mapped Process") } },
                { "AppScriptVersion", new Version(1, 2, 3, 4) },
                { "AppScriptDate", scriptDate },
                { "AppScriptAuthor", "MappedAuthor" },
                { "DeployAppScriptFriendlyName", "MappedFriendlyName" },
                { "DeployAppScriptVersion", new Version(5, 6, 7, 8) },
                { "DeployAppScriptParameters", new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase) { { "MappedKey", "MappedValue" } } },
                { "DeployAppScriptSessionState", powerShell.ModuleSessionState },
                { "ScriptDirectory", new[] { temp.GetPath("MappedScriptDirectory") } },
                { "DirFiles", temp.GetPath("MappedFiles") },
                { "DirSupportFiles", temp.GetPath("MappedSupportFiles") },
                { "DefaultMsiFile", temp.GetPath("Mapped.msi") },
                { "DefaultMstFile", temp.GetPath("Mapped.mst") },
                { "DefaultMspFiles", new[] { temp.GetPath("Mapped.msp") } },
                { "InstallTitle", "Mapped Install Title" },
                { "InstallName", "MappedInstallName" },
                { "LogName", "MappedLogName.log" },
            };

            // Act
            DeploymentSession session = new(parameters, noExitOnClose: true, compatibilityMode: false);

            // Assert: the two enumerations.
            Assert.Equal(DeploymentType.Repair, session.DeploymentType);
            Assert.Equal(DeployMode.Silent, session.DeployMode);

            // Assert: the application's own description.
            Assert.Equal("MappedVendor", session.AppVendor);
            Assert.Equal("MappedName", session.AppName);
            Assert.Equal("9.8.7", session.AppVersion);
            Assert.Equal("MappedArch", session.AppArch);
            Assert.Equal("MappedLang", session.AppLang);
            Assert.Equal("MappedRevision", session.AppRevision);
            Assert.Equal([8001, 8002], session.AppSuccessExitCodes);
            Assert.Equal([7001, 7002], session.AppRebootExitCodes);
            Assert.Equal(["mapped"], [.. session.AppProcessesToClose.Select(static process => process.Name)]);

            // Assert: the two scripts, whose versions are the pair most likely to be crossed.
            Assert.Equal(new Version(1, 2, 3, 4), session.AppScriptVersion);
            Assert.Equal(scriptDate, session.AppScriptDate);
            Assert.Equal("MappedAuthor", session.AppScriptAuthor);
            Assert.Equal("MappedFriendlyName", session.DeployAppScriptFriendlyName);
            Assert.Equal(new Version(5, 6, 7, 8), session.DeployAppScriptVersion);
            Assert.NotNull(session.DeployAppScriptParameters);
            Assert.Equal("MappedValue", Assert.Contains("MappedKey", session.DeployAppScriptParameters));
            Assert.Same(powerShell.ModuleSessionState, session.DeployAppScriptSessionState);

            // Assert: the directories and files, each of which arrives as a string and leaves as an object.
            Assert.Equal([temp.GetPath("MappedScriptDirectory")], [.. session.ScriptDirectory.Select(static directory => directory.FullName)]);
            Assert.Equal(temp.GetPath("MappedFiles"), session.DirFiles?.FullName);
            Assert.Equal(temp.GetPath("MappedSupportFiles"), session.DirSupportFiles?.FullName);
            Assert.Equal(temp.GetPath("Mapped.msi"), session.DefaultMsiFile?.FullName);
            Assert.Equal(temp.GetPath("Mapped.mst"), session.DefaultMstFile?.FullName);
            Assert.Equal([temp.GetPath("Mapped.msp")], [.. session.DefaultMspFiles.Select(static file => file.FullName)]);

            // Assert: the three the caller can name outright.
            Assert.Equal("Mapped Install Title", session.InstallTitle);
            Assert.Equal("MappedInstallName", session.InstallName);
            Assert.Equal("MappedLogName.log", session.LogName);
        }

        /// <summary>
        /// Verifies that each switch the constructor reads sets the flag that belongs to it.
        /// </summary>
        /// <remarks>
        /// Thirteen more near-identical blocks, and only four of the flags they set are visible on the session, so
        /// the rest are read off the field itself. The alternative would be to reach each one through the
        /// behaviour it changes much later in the constructor, which would test the behaviour rather than the
        /// mapping and would leave the ones with no separable behaviour untested.
        /// <para>
        /// <c>RequireAdmin</c> is left to its own test, since setting it decides whether the constructor throws.
        /// </para>
        /// </remarks>
        [Fact]
        public void DeploymentSession_MapsEverySwitchToItsOwnFlag()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            using TempDirectory temp = new();
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(Configuration(temp), powerShell.NewEnvironmentTable());
            Dictionary<string, object> parameters = MinimalParameters();
            foreach (string key in SwitchedSettings.Keys)
            {
                parameters.Add(key, new SwitchParameter(isPresent: true));
            }

            // Act
            DeploymentSession session = new(parameters, noExitOnClose: true, compatibilityMode: false);

            // Assert
            DeploymentSettings settings = SettingsOf(session);
            Assert.Equal(
                [.. SwitchedSettings.Select(static switched => $"{switched.Key} => set")],
                [.. SwitchedSettings.Select(switched => $"{switched.Key} => {(settings.HasFlag(switched.Value) ? "set" : "unset")}")]);

            // Assert: the four that are also visible from outside agree with the field.
            Assert.True(session.SuppressRebootPassThru);
            Assert.True(session.TerminalServerMode);
            Assert.True(session.DisableLogging);
        }

        /// <summary>
        /// Verifies that a session given no switches carries none of the flags they set.
        /// </summary>
        /// <remarks>
        /// The other half of the mapping. Without this a flag set unconditionally would pass the test above
        /// while being wrong for every deployment that did not ask for it.
        /// </remarks>
        [Fact]
        public void DeploymentSession_CarriesNoSwitchedFlagsWhenGivenNoSwitches()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            using TempDirectory temp = new();
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(Configuration(temp), powerShell.NewEnvironmentTable());

            // Act
            DeploymentSession session = new(MinimalParameters(), noExitOnClose: true, compatibilityMode: false);

            // Assert
            DeploymentSettings settings = SettingsOf(session);
            Assert.Equal(
                [.. SwitchedSettings.Select(static switched => $"{switched.Key} => unset")],
                [.. SwitchedSettings.Select(switched => $"{switched.Key} => {(settings.HasFlag(switched.Value) ? "set" : "unset")}")]);
            Assert.False(session.SuppressRebootPassThru);
            Assert.False(session.TerminalServerMode);
            Assert.False(session.DisableLogging);
            Assert.False(session.RequireAdmin);
        }

        /// <summary>
        /// Verifies that a switch passed but turned off is not the same as one never passed at all.
        /// </summary>
        /// <remarks>
        /// <c>NoProcessDetection</c> is the one parameter of the forty with three states rather than two. Absent
        /// leaves detection to the usual rules, on suppresses it, and explicitly off forces it - so the flag being
        /// clear is only half of what an explicit no means. The other half changes how the deployment mode is
        /// resolved and is asserted there.
        /// </remarks>
        [Fact]
        public void NoProcessDetection_PassedAsFalseDoesNotSetTheFlag()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            using TempDirectory temp = new();
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(Configuration(temp), powerShell.NewEnvironmentTable());
            Dictionary<string, object> parameters = MinimalParameters();
            parameters.Add("NoProcessDetection", new SwitchParameter(isPresent: false));

            // Act
            DeploymentSession session = new(parameters, noExitOnClose: true, compatibilityMode: false);

            // Assert
            Assert.False(SettingsOf(session).HasFlag(DeploymentSettings.NoProcessDetection));
        }

        /// <summary>
        /// Verifies that the choice of whether the session may exit the process is carried, not inferred.
        /// </summary>
        /// <param name="noExitOnClose">What the session was told at construction.</param>
        /// <param name="expected">Whether it should then agree to exit.</param>
        [Theory]
        [InlineData(true, false)]
        [InlineData(false, true)]
        public void CanExitOnClose_FollowsTheChoiceMadeAtConstruction(bool noExitOnClose, bool expected)
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            using TempDirectory temp = new();
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(Configuration(temp), powerShell.NewEnvironmentTable());

            // Act
            DeploymentSession session = new(MinimalParameters(), noExitOnClose, compatibilityMode: false);

            // Assert
            Assert.Equal(expected, session.CanExitOnClose());
        }

        /// <summary>
        /// Verifies that a session told nothing about the application describes itself from the module.
        /// </summary>
        /// <remarks>
        /// The path a caller takes by dot-sourcing the toolkit without a deployment script. Everything the session
        /// goes on to name itself - its title, its install name, its log file, its deferral registry key - is
        /// built from these four, so a session with no application would otherwise have nothing to be called.
        /// </remarks>
        [Fact]
        public void DeploymentSession_DescribesItselfFromTheModuleWhenGivenNoAppName()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            using TempDirectory temp = new();
            EnvironmentTable environment = powerShell.NewEnvironmentTable();
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(Configuration(temp), environment);

            // Act
            DeploymentSession session = new(
                new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase) { { "DeployMode", DeployMode.Silent } },
                noExitOnClose: true,
                compatibilityMode: false);

            // Assert
            Assert.Equal(environment.AppDeployToolkitName, session.AppName);
            Assert.Equal(environment.AppDeployMainScriptVersion.ToString(), session.AppVersion);
            Assert.Equal(environment.CurrentLanguage, session.AppLang);
            Assert.Equal("01", session.AppRevision);
        }

        /// <summary>
        /// Verifies that a vendor given without an application name is thrown away rather than kept.
        /// </summary>
        /// <remarks>
        /// Surprising enough to be worth writing down. A session that has to name itself from the module is not
        /// the caller's application at all, so keeping the caller's vendor would produce a title crediting them
        /// with the toolkit. The vendor is discarded even though it was supplied.
        /// </remarks>
        [Fact]
        public void DeploymentSession_DiscardsAVendorGivenWithoutAnAppName()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            using TempDirectory temp = new();
            EnvironmentTable environment = powerShell.NewEnvironmentTable();
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(Configuration(temp), environment);

            // Act
            DeploymentSession session = new(
                new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                {
                    { "AppVendor", "DiscardedVendor" },
                    { "DeployMode", DeployMode.Silent },
                },
                noExitOnClose: true,
                compatibilityMode: false);

            // Assert
            Assert.Null(session.AppVendor);
            Assert.DoesNotContain("DiscardedVendor", session.InstallTitle, StringComparison.Ordinal);
            Assert.DoesNotContain("DiscardedVendor", session.InstallName, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that an install title not supplied is built from the vendor, name and version.
        /// </summary>
        [Fact]
        public void InstallTitle_IsBuiltFromTheVendorNameAndVersion()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            using TempDirectory temp = new();
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(Configuration(temp), powerShell.NewEnvironmentTable());

            // Act
            DeploymentSession session = new(
                new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                {
                    { "AppVendor", "Contoso" },
                    { "AppName", "Test App" },
                    { "AppVersion", "1.0.0" },
                    { "DeployMode", DeployMode.Silent },
                },
                noExitOnClose: true,
                compatibilityMode: false);

            // Assert
            Assert.Equal("Contoso Test App 1.0.0", session.InstallTitle);
        }

        /// <summary>
        /// Verifies that a title with no vendor to put in front of it does not begin with a space.
        /// </summary>
        /// <remarks>
        /// The vendor is interpolated with a trailing space whether or not there is a vendor, so a session
        /// without one is built from a string that begins with the space that would have followed it.
        /// </remarks>
        [Fact]
        public void InstallTitle_HasNoLeadingSpaceWhenThereIsNoVendor()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            using TempDirectory temp = new();
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(Configuration(temp), powerShell.NewEnvironmentTable());

            // Act
            DeploymentSession session = new(
                new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                {
                    { "AppName", "Test App" },
                    { "AppVersion", "1.0.0" },
                    { "DeployMode", DeployMode.Silent },
                },
                noExitOnClose: true,
                compatibilityMode: false);

            // Assert
            Assert.Equal("Test App 1.0.0", session.InstallTitle);
        }

        /// <summary>
        /// Verifies that a run of whitespace in a supplied title is collapsed to a single space.
        /// </summary>
        /// <remarks>
        /// The title is shown to the person at the machine and written into every log, so a doubled space wants
        /// normalising. Collapsing is what the install name's own tidying does one line later, and what this is
        /// expected to do here.
        /// </remarks>
        [Fact]
        public void InstallTitle_CollapsesARunOfWhitespaceToASingleSpace()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            using TempDirectory temp = new();
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(Configuration(temp), powerShell.NewEnvironmentTable());
            Dictionary<string, object> parameters = MinimalParameters();
            parameters.Add("InstallTitle", "Contoso  Test  Suite");

            // Act
            DeploymentSession session = new(parameters, noExitOnClose: true, compatibilityMode: false);

            // Assert
            Assert.Equal("Contoso Test Suite", session.InstallTitle);
        }

        /// <summary>
        /// Verifies that an install name not supplied is built from all six parts of the application.
        /// </summary>
        [Fact]
        public void InstallName_IsBuiltFromAllSixPartsOfTheApplication()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            using TempDirectory temp = new();
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(Configuration(temp), powerShell.NewEnvironmentTable());

            // Act
            DeploymentSession session = new(
                new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                {
                    { "AppVendor", "Contoso" },
                    { "AppName", "Test App" },
                    { "AppVersion", "1.0.0" },
                    { "AppArch", "x64" },
                    { "AppLang", "EN" },
                    { "AppRevision", "01" },
                    { "DeployMode", DeployMode.Silent },
                },
                noExitOnClose: true,
                compatibilityMode: false);

            // Assert: the spaces are removed rather than replaced, so the name stays usable as a file name.
            Assert.Equal("Contoso_TestApp_1.0.0_x64_EN_01", session.InstallName);
        }

        /// <summary>
        /// Verifies that the separators left behind by parts the caller did not give are not left dangling.
        /// </summary>
        /// <remarks>
        /// Six parts joined by five underscores, so a session naming only three of them ends in a run of
        /// separators with nothing between them. They are trimmed from the end and collapsed in the middle,
        /// which is what keeps a log file name from ending in punctuation.
        /// </remarks>
        [Fact]
        public void InstallName_TrimsAndCollapsesTheSeparatorsOfPartsNotGiven()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            using TempDirectory temp = new();
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(Configuration(temp), powerShell.NewEnvironmentTable());

            // Act
            DeploymentSession session = new(
                new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                {
                    { "AppVendor", "Contoso" },
                    { "AppName", "TestApp" },
                    { "AppVersion", "1.0.0" },
                    { "DeployMode", DeployMode.Silent },
                },
                noExitOnClose: true,
                compatibilityMode: false);

            // Assert
            Assert.Equal("Contoso_TestApp_1.0.0", session.InstallName);
        }

        /// <summary>
        /// Verifies that characters a file name cannot carry are removed from the install name.
        /// </summary>
        /// <remarks>
        /// The install name becomes a log file name and a registry key name, so anything the file system refuses
        /// has to go before it gets there rather than when the log is first written.
        /// </remarks>
        [Fact]
        public void InstallName_StripsCharactersAFileNameCannotCarry()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            using TempDirectory temp = new();
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(Configuration(temp), powerShell.NewEnvironmentTable());

            // Act
            DeploymentSession session = new(
                new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                {
                    { "AppVendor", "Contoso" },
                    { "AppName", "Test:App*Name?" },
                    { "AppVersion", "1.0.0" },
                    { "DeployMode", DeployMode.Silent },
                },
                noExitOnClose: true,
                compatibilityMode: false);

            // Assert
            Assert.Equal("Contoso_TestAppName_1.0.0", session.InstallName);
        }

        /// <summary>
        /// Verifies that the two backwards-compatibility strings describe the moment the session began.
        /// </summary>
        /// <remarks>
        /// Both are formatted from the session's own start time for the benefit of scripts written against older
        /// versions. The date is day-first, which is the pair of parts most easily crossed and is only visible on
        /// a day that could not also be a month.
        /// </remarks>
        [Fact]
        public void CurrentDateAndTime_DescribeTheMomentTheSessionBegan()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            using TempDirectory temp = new();
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(Configuration(temp), powerShell.NewEnvironmentTable());

            // Act
            DeploymentSession session = new(MinimalParameters(), noExitOnClose: true, compatibilityMode: false);

            // Assert
            Assert.Equal(session.CurrentDateTime.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture), session.CurrentDate);
            Assert.Equal(session.CurrentDateTime.ToString("HH:mm:ss", CultureInfo.InvariantCulture), session.CurrentTime);

            // Assert: read back rather than only formatted, so a transposed day and month would show.
            DateTime parsed = DateTime.ParseExact(session.CurrentDate, "dd-MM-yyyy", CultureInfo.InvariantCulture);
            Assert.Equal(session.CurrentDateTime.Date, parsed);
        }

        /// <summary>
        /// Verifies that a session logs into the configured directory, creating it if it is not there.
        /// </summary>
        /// <remarks>
        /// The plain case, and the one that has to work before any of the nesting options mean anything. The
        /// directory is created rather than required, since a fresh machine has never logged anything.
        /// </remarks>
        [Fact]
        public void LogPath_IsTheConfiguredDirectoryAndIsCreated()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            using TempDirectory temp = new();
            ModuleConfiguration configuration = Configuration(temp);
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(configuration, powerShell.NewEnvironmentTable());
            Assert.False(Directory.Exists(configuration.LogPath));

            // Act
            DeploymentSession session = new(MinimalParameters(), noExitOnClose: true, compatibilityMode: false);

            // Assert
            Assert.Equal(configuration.LogPath, session.LogPath.FullName, StringComparer.OrdinalIgnoreCase);
            Assert.True(Directory.Exists(session.LogPath.FullName));
        }

        /// <summary>
        /// Verifies that a session told to log to a subfolder puts its logs under one named for the install.
        /// </summary>
        [Fact]
        public void LogPath_NestsUnderTheInstallNameWhenLoggingToASubfolder()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            using TempDirectory temp = new();
            ModuleConfiguration configuration = Configuration(temp);
            configuration.LogToSubfolder = true;
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(configuration, powerShell.NewEnvironmentTable());

            // Act
            DeploymentSession session = new(MinimalParameters(), noExitOnClose: true, compatibilityMode: false);

            // Assert
            Assert.Equal(
                Path.Join(configuration.LogPath, $"{session.InstallName}_{session.DeploymentType}"),
                session.LogPath.FullName,
                StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Verifies that a session told to log to a hierarchy nests by vendor, then name, then version.
        /// </summary>
        /// <remarks>
        /// The order is the point. Three levels of the same kind of value, so a pair transposed would still produce
        /// three plausible directories and only the shape would be wrong.
        /// </remarks>
        [Fact]
        public void LogPath_NestsByVendorThenNameThenVersionWhenLoggingToAHierarchy()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            using TempDirectory temp = new();
            ModuleConfiguration configuration = Configuration(temp);
            configuration.LogToHierarchy = true;
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(configuration, powerShell.NewEnvironmentTable());
            Dictionary<string, object> parameters = MinimalParameters();
            parameters.Add("AppVendor", "Contoso");

            // Act
            DeploymentSession session = new(parameters, noExitOnClose: true, compatibilityMode: false);

            // Assert
            Assert.Equal(
                Path.Join(configuration.LogPath, "Contoso", "TestApp", "1.0.0"),
                session.LogPath.FullName,
                StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Verifies that a hierarchy with no vendor to nest under does not gain an empty level.
        /// </summary>
        /// <remarks>
        /// The three levels are joined before being appended, so a session without a vendor builds a path that
        /// begins with the separator that would have followed it. An empty directory level would put every
        /// vendorless deployment in the same unnamed folder.
        /// </remarks>
        [Fact]
        public void LogPath_OmitsTheVendorLevelWhenThereIsNoVendor()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            using TempDirectory temp = new();
            ModuleConfiguration configuration = Configuration(temp);
            configuration.LogToHierarchy = true;
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(configuration, powerShell.NewEnvironmentTable());

            // Act
            DeploymentSession session = new(MinimalParameters(), noExitOnClose: true, compatibilityMode: false);

            // Assert
            Assert.Equal(
                Path.Join(configuration.LogPath, "TestApp", "1.0.0"),
                session.LogPath.FullName,
                StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Verifies that a hierarchy keeps only the configured number of previous versions.
        /// </summary>
        /// <remarks>
        /// The setting is named for hierarchy levels but counts sibling version folders of the application being
        /// deployed, and the folder for the version now being deployed is not one of them. So a maximum of two
        /// leaves two previous versions plus the current one, and the oldest are removed by creation time rather
        /// than by name - a version numbered higher is not necessarily newer.
        /// </remarks>
        [Fact]
        public void LogPath_KeepsOnlyTheConfiguredNumberOfPreviousVersions()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            using TempDirectory temp = new();
            ModuleConfiguration configuration = Configuration(temp);
            configuration.LogToHierarchy = true;
            configuration.LogMaxHierarchy = 2;
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(configuration, powerShell.NewEnvironmentTable());

            // Arrange: four previous versions, aged so their order is not in doubt.
            string appDirectory = Path.Join(configuration.LogPath, "Contoso", "TestApp");
            string[] previous = ["1.0.0", "2.0.0", "3.0.0", "4.0.0"];
            for (int index = 0; index < previous.Length; index++)
            {
                DirectoryInfo created = Directory.CreateDirectory(Path.Join(appDirectory, previous[index]));
                created.CreationTime = DateTime.Now.AddDays(index - previous.Length);
            }
            Dictionary<string, object> parameters = MinimalParameters();
            parameters["AppVersion"] = "5.0.0";
            parameters.Add("AppVendor", "Contoso");

            // Act
            DeploymentSession session = new(parameters, noExitOnClose: true, compatibilityMode: false);

            // Assert: the two oldest are gone, the two newest remain, and the current one was added.
            string[] remaining = [.. new DirectoryInfo(appDirectory).GetDirectories().Select(static directory => directory.Name)];
            Assert.Equal(3, remaining.Length);
            Assert.Contains("5.0.0", remaining, StringComparer.Ordinal);
            Assert.Contains("4.0.0", remaining, StringComparer.Ordinal);
            Assert.Contains("3.0.0", remaining, StringComparer.Ordinal);
            Assert.DoesNotContain("2.0.0", remaining, StringComparer.Ordinal);
            Assert.DoesNotContain("1.0.0", remaining, StringComparer.Ordinal);
            Assert.Equal(Path.Join(appDirectory, "5.0.0"), session.LogPath.FullName, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Verifies that a session set to compress its logs writes them somewhere temporary first.
        /// </summary>
        /// <remarks>
        /// Logs destined for a zip are written to a staging folder rather than to the configured directory, so that
        /// the archive can be assembled from a folder holding nothing else.
        /// </remarks>
        [Fact]
        public void LogPath_IsATemporaryStagingFolderWhenCompressingLogs()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            using TempDirectory temp = new();
            ModuleConfiguration configuration = Configuration(temp);
            configuration.CompressLogs = true;
            EnvironmentTable environment = powerShell.NewEnvironmentTable();
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(configuration, environment);
            Dictionary<string, object> parameters = MinimalParameters();
            parameters["AppName"] = UniqueAppName();

            // Act
            DeploymentSession session = new(parameters, noExitOnClose: true, compatibilityMode: false);

            // Assert
            try
            {
                Assert.Equal(
                    Path.Join(environment.EnvTemp.FullName, $"{session.InstallName}_{session.DeploymentType}"),
                    session.LogPath.FullName,
                    StringComparer.OrdinalIgnoreCase);
                Assert.True(Directory.Exists(session.LogPath.FullName));
            }
            finally
            {
                Delete(session.LogPath);
            }
        }

        /// <summary>
        /// Verifies that a staging folder left behind by an earlier run is emptied rather than added to.
        /// </summary>
        /// <remarks>
        /// The folder is named from the install and the deployment type, so a run that failed before its archive
        /// was assembled leaves one behind under exactly the name the next run will use. Adding to it would put
        /// another deployment's logs in this deployment's archive.
        /// </remarks>
        [Fact]
        public void LogPath_EmptiesAStagingFolderLeftByAnEarlierRun()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            using TempDirectory temp = new();
            ModuleConfiguration configuration = Configuration(temp);
            configuration.CompressLogs = true;
            EnvironmentTable environment = powerShell.NewEnvironmentTable();
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(configuration, environment);
            Dictionary<string, object> parameters = MinimalParameters();
            parameters["AppName"] = UniqueAppName();

            // Arrange: a session built only to find out what the staging folder will be called, then a leftover
            // file placed in it as an earlier run would have.
            DeploymentSession first = new(parameters, noExitOnClose: true, compatibilityMode: false);
            DirectoryInfo staging = first.LogPath;
            string leftover = Path.Join(staging.FullName, "LeftBehind.log");
            File.WriteAllText(leftover, "an earlier run");

            // Act
            DeploymentSession second = new(parameters, noExitOnClose: true, compatibilityMode: false);

            // Assert
            try
            {
                Assert.Equal(staging.FullName, second.LogPath.FullName, StringComparer.OrdinalIgnoreCase);
                Assert.False(File.Exists(leftover));
            }
            finally
            {
                Delete(second.LogPath);
            }
        }

        /// <summary>
        /// Verifies that a log file name is built from the install, a discriminator and the deployment type, and
        /// carries the user's name only when the deployment is not elevated.
        /// </summary>
        /// <remarks>
        /// The user name is appended when the process is not elevated because an unelevated user cannot write over
        /// a file in the configured log directory that belongs to somebody else. Asserted as an equivalence rather
        /// than for one case, so it holds whether or not the tests are being run elevated.
        /// </remarks>
        [Fact]
        public void NewLogFileName_NamesTheLogFromTheInstallAndDiscriminator()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            using TempDirectory temp = new();
            EnvironmentTable environment = powerShell.NewEnvironmentTable();
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(Configuration(temp), environment);

            // Act
            DeploymentSession session = new(MinimalParameters(), noExitOnClose: true, compatibilityMode: false);
            string name = session.NewLogFileName("Discriminator", fileNameOnly: true);

            // Assert
            Assert.Equal(
                $"{session.InstallName}_Discriminator_{session.DeploymentType}{(environment.IsAdmin ? null : $"_{environment.EnvUserName}")}.log",
                name,
                StringComparer.Ordinal);

            // Assert: the user's name is there exactly when the deployment could not write over another's file.
            Assert.Equal(!environment.IsAdmin, name.Contains($"_{environment.EnvUserName}.", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Verifies that a log file name is returned beneath the log path unless only the name was asked for.
        /// </summary>
        [Fact]
        public void NewLogFileName_ReturnsAFullPathUnlessOnlyTheNameWasAskedFor()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            using TempDirectory temp = new();
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(Configuration(temp), powerShell.NewEnvironmentTable());

            // Act
            DeploymentSession session = new(MinimalParameters(), noExitOnClose: true, compatibilityMode: false);

            // Assert
            Assert.Equal(
                Path.Join(session.LogPath.FullName, session.NewLogFileName("Discriminator", fileNameOnly: true)),
                session.NewLogFileName("Discriminator", fileNameOnly: false),
                StringComparer.Ordinal);
        }

        /// <summary>
        /// Verifies that a session given no log name takes one named after the module.
        /// </summary>
        [Fact]
        public void LogName_IsNamedAfterTheModuleWhenNoneWasSupplied()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            using TempDirectory temp = new();
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(Configuration(temp), powerShell.NewEnvironmentTable());

            // Act
            DeploymentSession session = new(MinimalParameters(), noExitOnClose: true, compatibilityMode: false);

            // Assert
            Assert.Equal(session.NewLogFileName(PowerShellFixture.ModuleName, fileNameOnly: true), session.LogName, StringComparer.Ordinal);
        }

        /// <summary>
        /// Verifies that an application whose name carries braces can still name its log file.
        /// </summary>
        /// <remarks>
        /// Braces are legal in a file name, so they survive into the install name, and the install name goes on to
        /// become the template the discriminator is substituted into. A product code is a realistic application
        /// name for a zero-config deployment, so this is a name a caller can plausibly supply.
        /// </remarks>
        [Fact]
        public void NewLogFileName_NamesTheLogWhenTheInstallNameCarriesBraces()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            using TempDirectory temp = new();
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(Configuration(temp), powerShell.NewEnvironmentTable());
            Dictionary<string, object> parameters = MinimalParameters();
            parameters["AppName"] = "{2FB2E3A0-1A2B-4C3D-9E4F-5A6B7C8D9E0F}";

            // Act
            DeploymentSession session = new(parameters, noExitOnClose: true, compatibilityMode: false);

            // Assert
            Assert.Contains("{2FB2E3A0-1A2B-4C3D-9E4F-5A6B7C8D9E0F}", session.InstallName, StringComparison.Ordinal);
            Assert.Contains("Discriminator", session.NewLogFileName("Discriminator", fileNameOnly: true), StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that an existing log is archived rather than added to when appending is turned off.
        /// </summary>
        /// <remarks>
        /// A named log file is supplied so the test knows what the file will be called before the session that
        /// rotates it exists. The archive keeps the extension, which is what stops a rotated log from becoming
        /// unrecognisable to whatever collects them.
        /// </remarks>
        [Fact]
        public void LogFile_IsArchivedWhenAppendingIsTurnedOff()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            using TempDirectory temp = new();
            ModuleConfiguration configuration = Configuration(temp);
            configuration.LogAppend = false;
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(configuration, powerShell.NewEnvironmentTable());
            string logDirectory = Directory.CreateDirectory(configuration.LogPath).FullName;
            File.WriteAllText(Path.Join(logDirectory, "rotate.log"), PreviousContent);
            Dictionary<string, object> parameters = MinimalParameters();
            parameters.Add("LogName", "rotate.log");

            // Act
            _ = new DeploymentSession(parameters, noExitOnClose: true, compatibilityMode: false);

            // Assert: the previous log was moved aside under a timestamped name, keeping its extension.
            string archive = Assert.Single(Directory.GetFiles(logDirectory, "rotate_*.log"));
            Assert.Equal(PreviousContent, File.ReadAllText(archive));

            // Assert: and a new log took its place.
            Assert.DoesNotContain(PreviousContent, File.ReadAllText(Path.Join(logDirectory, "rotate.log")), StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that a log past the configured size is archived even though appending is on.
        /// </summary>
        /// <remarks>
        /// The other reason to rotate, and the one a long-running deployment hits. The new log says why it was
        /// rotated, which is the only record of it once the old file has been renamed.
        /// </remarks>
        [Fact]
        public void LogFile_IsArchivedWhenItOutgrowsTheConfiguredSize()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            using TempDirectory temp = new();
            ModuleConfiguration configuration = Configuration(temp);
            configuration.LogAppend = true;
            configuration.LogMaxSize = 1;
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(configuration, powerShell.NewEnvironmentTable());
            string logDirectory = Directory.CreateDirectory(configuration.LogPath).FullName;
            File.WriteAllText(Path.Join(logDirectory, "rotate.log"), new string('x', 1_200_000));
            Dictionary<string, object> parameters = MinimalParameters();
            parameters.Add("LogName", "rotate.log");

            // Act
            _ = new DeploymentSession(parameters, noExitOnClose: true, compatibilityMode: false);

            // Assert
            _ = Assert.Single(Directory.GetFiles(logDirectory, "rotate_*.log"));
            Assert.Contains("Maximum log file size", File.ReadAllText(Path.Join(logDirectory, "rotate.log")), StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that archiving a log removes the oldest archives beyond the configured history.
        /// </summary>
        /// <remarks>
        /// Counted after the current log has been moved aside, so the freshly made archive is one of the files
        /// competing to be kept. Ordered by last write time rather than by name, since a timestamped name sorts
        /// differently from the order the files were written in.
        /// </remarks>
        [Fact]
        public void LogFile_ArchivingKeepsOnlyTheConfiguredHistory()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            using TempDirectory temp = new();
            ModuleConfiguration configuration = Configuration(temp);
            configuration.LogAppend = false;
            configuration.LogMaxHistory = 2;
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(configuration, powerShell.NewEnvironmentTable());
            string logDirectory = Directory.CreateDirectory(configuration.LogPath).FullName;
            File.WriteAllText(Path.Join(logDirectory, "rotate.log"), PreviousContent);

            // Arrange: three archives already there, aged so their order is not in doubt.
            string[] existing = ["rotate_oldest.log", "rotate_middle.log", "rotate_newest.log"];
            for (int index = 0; index < existing.Length; index++)
            {
                string path = Path.Join(logDirectory, existing[index]);
                File.WriteAllText(path, existing[index]);
                File.SetLastWriteTime(path, DateTime.Now.AddDays(index - existing.Length));
            }
            Dictionary<string, object> parameters = MinimalParameters();
            parameters.Add("LogName", "rotate.log");

            // Act
            _ = new DeploymentSession(parameters, noExitOnClose: true, compatibilityMode: false);

            // Assert: four archives competed and the two oldest lost.
            string[] archives = [.. Directory.GetFiles(logDirectory, "rotate_*.log").Select(static path => new FileInfo(path).Name)];
            Assert.Equal(2, archives.Length);
            Assert.Contains("rotate_newest.log", archives, StringComparer.Ordinal);
            Assert.DoesNotContain("rotate_oldest.log", archives, StringComparer.Ordinal);
            Assert.DoesNotContain("rotate_middle.log", archives, StringComparer.Ordinal);
        }

        /// <summary>
        /// Verifies that a session with no deferral history recorded reports none.
        /// </summary>
        /// <remarks>
        /// The first run of any deployment. The registry key does not exist, and the answer has to be nothing
        /// rather than an empty history, because the two mean different things to whatever asks.
        /// </remarks>
        [Fact]
        public void GetDeferHistory_ReportsNothingWhenNoneWasEverRecorded()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            using TempDirectory temp = new();
            using TempRegistryKey registry = new();
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(Configuration(temp, registry), powerShell.NewEnvironmentTable());

            // Act
            DeploymentSession session = new(MinimalParameters(), noExitOnClose: true, compatibilityMode: false);

            // Assert
            Assert.Null(session.GetDeferHistory());
        }

        /// <summary>
        /// Verifies that a deferral history written can be read back as it was written.
        /// </summary>
        /// <remarks>
        /// The values cross the registry as strings and a DWORD and come back parsed, so the round trip is where a
        /// format that cannot be read back would show. The deadline is written as universal time and read back
        /// without one, so it is the instant rather than the reading that has to survive.
        /// <para>
        /// The run interval is written and never read: the history a caller gets back has nowhere to put it. So it
        /// is checked in the registry directly, which is the only place it can be seen.
        /// </para>
        /// </remarks>
        [Fact]
        public void SetDeferHistory_WritesAHistoryThatCanBeReadBack()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            using TempDirectory temp = new();
            using TempRegistryKey registry = new();
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(Configuration(temp, registry), powerShell.NewEnvironmentTable());
            DateTime deadline = new(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc);
            DateTime lastTime = new(2026, 8, 30, 9, 30, 0, DateTimeKind.Utc);
            DeploymentSession session = new(MinimalParameters(), noExitOnClose: true, compatibilityMode: false);

            // Act
            session.SetDeferHistory(deferTimesRemaining: 3, deadline, TimeSpan.FromHours(1), lastTime);
            DeferHistory? history = session.GetDeferHistory();

            // Assert
            Assert.NotNull(history);
            Assert.Equal(3u, history.DeferTimesRemaining);
            Assert.Equal(deadline, Assert.NotNull(history.DeferDeadline).ToUniversalTime());
            Assert.Equal(lastTime, Assert.NotNull(history.DeferRunIntervalLastTime).ToUniversalTime());

            // Assert: the run interval reached the registry even though nothing reads it back.
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey($@"{registry.SubKeyName}\{PowerShellFixture.ModuleName}\DeferHistory\{session.InstallName}");
            Assert.NotNull(key);
            Assert.Equal("01:00:00", key.GetValue("DeferRunInterval"));
        }

        /// <summary>
        /// Verifies that only the parts of a deferral history that were given are written.
        /// </summary>
        /// <remarks>
        /// Each part is written separately, so a deployment counting deferrals without a deadline must not acquire
        /// one. A history is reported at all only because one of the three readable values is present.
        /// </remarks>
        [Fact]
        public void SetDeferHistory_WritesOnlyTheValuesItWasGiven()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            using TempDirectory temp = new();
            using TempRegistryKey registry = new();
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(Configuration(temp, registry), powerShell.NewEnvironmentTable());
            DeploymentSession session = new(MinimalParameters(), noExitOnClose: true, compatibilityMode: false);

            // Act
            session.SetDeferHistory(deferTimesRemaining: 2, deferDeadline: null, deferRunInterval: null, deferRunIntervalLastTime: null);
            DeferHistory? history = session.GetDeferHistory();

            // Assert
            Assert.NotNull(history);
            Assert.Equal(2u, history.DeferTimesRemaining);
            Assert.Null(history.DeferDeadline);
            Assert.Null(history.DeferRunIntervalLastTime);
        }

        /// <summary>
        /// Verifies that a session given nothing to record writes no deferral history at all.
        /// </summary>
        /// <remarks>
        /// The key is created by the first value written, so a call with nothing to write must not create it - an
        /// empty key would make a later read report a history where there is none.
        /// </remarks>
        [Fact]
        public void SetDeferHistory_WritesNothingWhenGivenNothing()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            using TempDirectory temp = new();
            using TempRegistryKey registry = new();
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(Configuration(temp, registry), powerShell.NewEnvironmentTable());
            DeploymentSession session = new(MinimalParameters(), noExitOnClose: true, compatibilityMode: false);

            // Act
            session.SetDeferHistory(deferTimesRemaining: null, deferDeadline: null, deferRunInterval: null, deferRunIntervalLastTime: null);

            // Assert
            Assert.Null(session.GetDeferHistory());
            Assert.Null(Registry.CurrentUser.OpenSubKey(registry.SubKeyName));
        }

        /// <summary>
        /// Verifies that resetting a deferral history takes away what was recorded.
        /// </summary>
        /// <remarks>
        /// Called when a deployment completes, so that the next one starts with its full complement of deferrals.
        /// Resetting one that was never recorded has to be harmless, since that is the usual case.
        /// </remarks>
        [Fact]
        public void ResetDeferHistory_TakesAwayWhatWasRecorded()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            using TempDirectory temp = new();
            using TempRegistryKey registry = new();
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(Configuration(temp, registry), powerShell.NewEnvironmentTable());
            DeploymentSession session = new(MinimalParameters(), noExitOnClose: true, compatibilityMode: false);
            session.SetDeferHistory(deferTimesRemaining: 1, deferDeadline: null, deferRunInterval: null, deferRunIntervalLastTime: null);
            Assert.NotNull(session.GetDeferHistory());

            // Act
            session.ResetDeferHistory();

            // Assert
            Assert.Null(session.GetDeferHistory());

            // Assert: and doing it again is not an error.
            session.ResetDeferHistory();
            Assert.Null(session.GetDeferHistory());
        }

        /// <summary>
        /// Verifies that a session can be closed with an exit message that carries braces.
        /// </summary>
        /// <remarks>
        /// The closing message is assembled once and the outcome put into it afterwards, so an exit message is a
        /// caller's text landing inside a template. Braces are ordinary in the text a deployment reports - an MSI
        /// product code, an installer's own error text - and they must reach the log as they were written.
        /// </remarks>
        [Fact]
        public void Close_ReportsAnExitMessageThatCarriesBraces()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            using TempDirectory temp = new();
            ModuleConfiguration configuration = Configuration(temp);
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(configuration, powerShell.NewEnvironmentTable());
            Dictionary<string, object> parameters = MinimalParameters();
            parameters.Add("LogName", "closing.log");
            DeploymentSession session = new(parameters, noExitOnClose: true, compatibilityMode: false);

            // Act
            _ = session.Close("MSI {2FB2E3A0-1A2B-4C3D-9E4F-5A6B7C8D9E0F} returned {1603}");

            // Assert: the message reached the log with its braces intact and the outcome substituted around it.
            string log = File.ReadAllText(Path.Join(configuration.LogPath, "closing.log"));
            Assert.Contains("MSI {2FB2E3A0-1A2B-4C3D-9E4F-5A6B7C8D9E0F} returned {1603}", log, StringComparison.Ordinal);
            Assert.Contains("completed", log, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that a session whose install name carries braces still archives its logs.
        /// </summary>
        /// <remarks>
        /// The archive is named from the install name and a timestamp is put into it afterwards. A failure here is
        /// quiet rather than loud - the archiving is wrapped in a catch that logs and carries on - so the
        /// deployment would appear to succeed while leaving its logs uncompressed in a staging folder that is
        /// never cleaned up.
        /// </remarks>
        [Fact]
        public void Close_ArchivesTheLogsWhenTheInstallNameCarriesBraces()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            using TempDirectory temp = new();
            ModuleConfiguration configuration = Configuration(temp);
            configuration.CompressLogs = true;
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(configuration, powerShell.NewEnvironmentTable());
            Dictionary<string, object> parameters = MinimalParameters();
            parameters["AppName"] = $"{{{UniqueAppName()}}}";
            DeploymentSession session = new(parameters, noExitOnClose: true, compatibilityMode: false);
            DirectoryInfo staging = session.LogPath;

            // Act
            try
            {
                _ = session.Close(exitMessage: null);

                // Assert: the logs were archived and the staging folder taken away with them.
                _ = Assert.Single(Directory.GetFiles(configuration.LogPath, "*.zip"));
                Assert.False(Directory.Exists(staging.FullName));
            }
            finally
            {
                Delete(staging);
            }
        }

        /// <summary>
        /// Verifies that a deployment with nothing to close resolves to silent.
        /// </summary>
        /// <remarks>
        /// The ordinary end of the resolution chain. A deployment that named no processes has nothing to ask a
        /// user about, so it stops being interactive rather than showing a dialog nobody needs to answer.
        /// </remarks>
        [Fact]
        public void DeployMode_ResolvesToSilentWhenNoProcessesWereSpecified()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            using TempDirectory temp = new();
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(Configuration(temp), powerShell.NewEnvironmentTable());

            // Act
            DeploymentSession session = new(AutoModeParameters(), noExitOnClose: true, compatibilityMode: false);

            // Assert
            Assert.Equal(DeployMode.Silent, session.DeployMode);
            Assert.True(session.IsSilent());
            Assert.True(session.IsNonInteractive());
        }

        /// <summary>
        /// Verifies that a deployment mode the caller chose is never resolved away from.
        /// </summary>
        /// <remarks>
        /// Every step of the resolution asks first whether the mode was set explicitly, and stops if it was. This
        /// is the case that would otherwise be silently overridden: a caller asking for an interactive deployment
        /// with no processes to close meets the rule that would have made it silent.
        /// </remarks>
        [Fact]
        public void DeployMode_IsLeftAloneWhenTheCallerChoseIt()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            using TempDirectory temp = new();
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(Configuration(temp), powerShell.NewEnvironmentTable());
            Dictionary<string, object> parameters = AutoModeParameters();
            parameters.Add("DeployMode", DeployMode.Interactive);

            // Act
            DeploymentSession session = new(parameters, noExitOnClose: true, compatibilityMode: false);

            // Assert
            Assert.Equal(DeployMode.Interactive, session.DeployMode);
            Assert.False(session.IsSilent());
            Assert.False(session.IsNonInteractive());
        }

        /// <summary>
        /// Verifies that a deployment stays interactive while a process it must close is running.
        /// </summary>
        /// <remarks>
        /// The reason the resolution consults the running process list at all: somebody has to be asked to close
        /// something. The test host is the process named, since it is the one process guaranteed to be running.
        /// </remarks>
        [Fact]
        public void DeployMode_StaysInteractiveWhileASpecifiedProcessIsRunning()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            using TempDirectory temp = new();
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(Configuration(temp), powerShell.NewEnvironmentTable());
            Dictionary<string, object> parameters = AutoModeParameters();
            using Process current = Process.GetCurrentProcess();
            parameters.Add("AppProcessesToClose", new[] { new ProcessDefinition(current.ProcessName) });

            // Act
            DeploymentSession session = new(parameters, noExitOnClose: true, compatibilityMode: false);

            // Assert
            Assert.Equal(DeployMode.Interactive, session.DeployMode);
            Assert.False(session.IsSilent());
        }

        /// <summary>
        /// Verifies that a deployment whose named processes are not running resolves to silent.
        /// </summary>
        [Fact]
        public void DeployMode_ResolvesToSilentWhenTheSpecifiedProcessesAreNotRunning()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            using TempDirectory temp = new();
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(Configuration(temp), powerShell.NewEnvironmentTable());
            Dictionary<string, object> parameters = AutoModeParameters();
            parameters.Add("AppProcessesToClose", new[] { new ProcessDefinition($"psadt-absent-{Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture)}") });

            // Act
            DeploymentSession session = new(parameters, noExitOnClose: true, compatibilityMode: false);

            // Assert
            Assert.Equal(DeployMode.Silent, session.DeployMode);
        }

        /// <summary>
        /// Verifies that a deployment told not to detect processes keeps the mode it started with.
        /// </summary>
        /// <remarks>
        /// Automatic resolution would have made this silent. Suppressing detection leaves the mode unresolved,
        /// and an unresolved mode becomes interactive at the end of the chain.
        /// </remarks>
        [Fact]
        public void DeployMode_IsLeftAloneWhenProcessDetectionIsSuppressed()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            using TempDirectory temp = new();
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(Configuration(temp), powerShell.NewEnvironmentTable());
            Dictionary<string, object> parameters = AutoModeParameters();
            parameters.Add("NoProcessDetection", new SwitchParameter(isPresent: true));

            // Act
            DeploymentSession session = new(parameters, noExitOnClose: true, compatibilityMode: false);

            // Assert
            Assert.Equal(DeployMode.Interactive, session.DeployMode);
        }

        /// <summary>
        /// Verifies that a switch passed but turned off forces the detection it would otherwise have suppressed.
        /// </summary>
        /// <remarks>
        /// The other half of the only three-state parameter of the forty, and the half that is not visible in the
        /// flags. A deployment with no processes named reaches the same silent mode either way, so what separates
        /// forcing from the ordinary path is which branch reported it - and the log is where that shows.
        /// </remarks>
        [Fact]
        public void DeployMode_TakesTheForcedPathWhenProcessDetectionIsExplicitlyNotSuppressed()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            using TempDirectory temp = new();
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(Configuration(temp), powerShell.NewEnvironmentTable());
            Dictionary<string, object> parameters = AutoModeParameters();
            parameters.Add("NoProcessDetection", new SwitchParameter(isPresent: false));

            // Act
            DeploymentSession session = new(parameters, noExitOnClose: true, compatibilityMode: false);

            // Assert
            Assert.Equal(DeployMode.Silent, session.DeployMode);
            Assert.Contains(
                "-NoProcessDetection was explicitly set to false",
                string.Join(Environment.NewLine, session.GetLogBuffer().Select(static entry => entry.Message)),
                StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that a non-interactive deployment is not also a silent one.
        /// </summary>
        /// <remarks>
        /// Two flags of three states between them: silent implies non-interactive, non-interactive does not imply
        /// silent. The module chooses whether to show a dialog on one and whether to suppress output on the other.
        /// </remarks>
        [Fact]
        public void DeployMode_NonInteractiveIsNonInteractiveWithoutBeingSilent()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            using TempDirectory temp = new();
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(Configuration(temp), powerShell.NewEnvironmentTable());
            Dictionary<string, object> parameters = AutoModeParameters();
            parameters.Add("DeployMode", DeployMode.NonInteractive);

            // Act
            DeploymentSession session = new(parameters, noExitOnClose: true, compatibilityMode: false);

            // Assert
            Assert.Equal(DeployMode.NonInteractive, session.DeployMode);
            Assert.True(session.IsNonInteractive());
            Assert.False(session.IsSilent());
        }

        /// <summary>
        /// Verifies that a deployment requiring administrative rights refuses to start without them.
        /// </summary>
        /// <remarks>
        /// Asserted as an equivalence rather than for one case, so it holds whether or not the tests are being run
        /// elevated: elevated, the session is built; otherwise it refuses. The refusal is wrapped, which is how
        /// every constructor failure reaches a caller, and the reason is kept as the inner exception.
        /// </remarks>
        [Fact]
        public void DeploymentSession_RefusesToStartWithoutTheAdministrativeRightsItRequires()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            using TempDirectory temp = new();
            EnvironmentTable environment = powerShell.NewEnvironmentTable();
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(Configuration(temp), environment);
            Dictionary<string, object> parameters = MinimalParameters();
            parameters.Add("RequireAdmin", new SwitchParameter(isPresent: true));

            // Act and assert
            if (environment.IsAdmin)
            {
                DeploymentSession session = new(parameters, noExitOnClose: true, compatibilityMode: false);
                Assert.True(session.RequireAdmin);
                return;
            }
            ApplicationException thrown = ThrowsLeavingTheProcessExitCodeAlone<ApplicationException>(
                () => new DeploymentSession(parameters, noExitOnClose: true, compatibilityMode: false));

            // Assert
            UnauthorizedAccessException reason = Assert.IsType<UnauthorizedAccessException>(thrown.InnerException);
            Assert.Contains("administrative permissions", reason.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that compatibility mode cannot be asked for without somewhere to put the variables.
        /// </summary>
        /// <remarks>
        /// Compatibility mode exists to publish the session's members as variables for deployment scripts written
        /// against version three, so being asked for it without a scope to publish into is a contradiction rather
        /// than something to do quietly.
        /// </remarks>
        [Fact]
        public void CompatibilityMode_IsRefusedWithoutASessionStateToPublishInto()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            using TempDirectory temp = new();
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(Configuration(temp), powerShell.NewEnvironmentTable());

            // Act
            ApplicationException thrown = ThrowsLeavingTheProcessExitCodeAlone<ApplicationException>(
                static () => new DeploymentSession(MinimalParameters(), noExitOnClose: true, compatibilityMode: true));

            // Assert
            InvalidOperationException reason = Assert.IsType<InvalidOperationException>(thrown.InnerException);
            Assert.Contains("SessionState is not available", reason.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that compatibility mode publishes the session's members into the caller's scope.
        /// </summary>
        /// <remarks>
        /// A version three deployment script reads <c>$installName</c> rather than asking a session for it, so
        /// every public member is set as a variable of the same name. The variables are taken away afterwards
        /// because the scope they are published into is the fixture's, and it outlives this test.
        /// </remarks>
        [Fact]
        public void CompatibilityMode_PublishesTheSessionsMembersIntoTheCallersScope()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            using TempDirectory temp = new();
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(Configuration(temp), powerShell.NewEnvironmentTable());
            Dictionary<string, object> parameters = MinimalParameters();
            parameters.Add("DeployAppScriptSessionState", powerShell.ModuleSessionState);

            // Act
            try
            {
                DeploymentSession session = new(parameters, noExitOnClose: true, compatibilityMode: true);

                // Assert
                Assert.Equal(session.InstallName, powerShell.ModuleSessionState.PSVariable.GetValue(nameof(session.InstallName)));
                Assert.Equal(session.DeployMode, powerShell.ModuleSessionState.PSVariable.GetValue(nameof(session.DeployMode)));
                Assert.Equal(session.LogPath.FullName, ((DirectoryInfo?)powerShell.ModuleSessionState.PSVariable.GetValue(nameof(session.LogPath)))?.FullName);
            }
            finally
            {
                foreach (string name in PublishedMemberNames())
                {
                    powerShell.ModuleSessionState.PSVariable.Remove(name);
                }
            }
        }

        /// <summary>
        /// Verifies that an exit code is judged against the deferral codes, then reboot, then success.
        /// </summary>
        /// <param name="exitCode">The exit code the deployment finished with.</param>
        /// <param name="expected">The status it should be judged as.</param>
        [Theory]
        [InlineData(0, DeploymentStatus.Complete)]
        [InlineData(1641, DeploymentStatus.RestartRequired)]
        [InlineData(3010, DeploymentStatus.RestartRequired)]
        [InlineData(60001, DeploymentStatus.FastRetry)]
        [InlineData(60012, DeploymentStatus.FastRetry)]
        [InlineData(1, DeploymentStatus.Error)]
        public void GetDeploymentStatus_JudgesAnExitCodeAgainstTheConfiguredSets(int exitCode, DeploymentStatus expected)
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            using TempDirectory temp = new();
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(Configuration(temp), powerShell.NewEnvironmentTable());
            DeploymentSession session = new(MinimalParameters(), noExitOnClose: true, compatibilityMode: false);

            // Act
            session.SetExitCode(exitCode);

            // Assert
            Assert.Equal(exitCode, session.GetExitCode());
            Assert.Equal(expected, session.GetDeploymentStatus());
        }

        /// <summary>
        /// Verifies the order the sets are consulted in where an exit code belongs to more than one.
        /// </summary>
        /// <remarks>
        /// The sets are not exclusive and a caller can put the same code in two of them, so the order decides the
        /// answer. Retrying beats everything, and needing a restart beats having succeeded - which matters because
        /// a deployment judged complete would never be restarted.
        /// </remarks>
        [Fact]
        public void GetDeploymentStatus_PrefersRetryThenRestartOverSuccess()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            using TempDirectory temp = new();
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(Configuration(temp), powerShell.NewEnvironmentTable());
            Dictionary<string, object> parameters = MinimalParameters();
            parameters.Add("AppSuccessExitCodes", new[] { 60001, 3010 });
            parameters.Add("AppRebootExitCodes", new[] { 3010 });
            DeploymentSession session = new(parameters, noExitOnClose: true, compatibilityMode: false);

            // Assert: a success code that is also the deferral code is a retry.
            session.SetExitCode(60001);
            Assert.Equal(DeploymentStatus.FastRetry, session.GetDeploymentStatus());

            // Assert: a success code that also asks for a restart asks for a restart.
            session.SetExitCode(3010);
            Assert.Equal(DeploymentStatus.RestartRequired, session.GetDeploymentStatus());
        }

        /// <summary>
        /// Verifies that closing a session hands back its exit code and marks it closed.
        /// </summary>
        [Fact]
        public void Close_HandsBackTheExitCodeAndMarksTheSessionClosed()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            using TempDirectory temp = new();
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(Configuration(temp), powerShell.NewEnvironmentTable());
            DeploymentSession session = new(MinimalParameters(), noExitOnClose: true, compatibilityMode: false);
            session.SetExitCode(1);
            Assert.False(session.IsClosed());

            // Act
            int exitCode = session.Close(exitMessage: null);

            // Assert
            Assert.Equal(1, exitCode);
            Assert.True(session.IsClosed());
        }

        /// <summary>
        /// Verifies that a session cannot be closed twice.
        /// </summary>
        /// <remarks>
        /// Closing writes the deployment's outcome and takes away anything it mounted, so doing it again would
        /// report a second outcome for a deployment that already finished.
        /// </remarks>
        [Fact]
        public void Close_RefusesASessionThatIsAlreadyClosed()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            using TempDirectory temp = new();
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(Configuration(temp), powerShell.NewEnvironmentTable());
            DeploymentSession session = new(MinimalParameters(), noExitOnClose: true, compatibilityMode: false);
            _ = session.Close(exitMessage: null);

            // Assert
            _ = Assert.Throws<ObjectDisposedException>(() => session.Close(exitMessage: null));
        }

        /// <summary>
        /// Verifies that the outcome written when closing matches how the exit code was judged.
        /// </summary>
        /// <param name="exitCode">The exit code the deployment finished with.</param>
        /// <param name="outcome">The word the log should use for it.</param>
        [Theory]
        [InlineData(0, "completed")]
        [InlineData(1, "failed")]
        [InlineData(60012, "was deferred")]
        public void Close_ReportsTheOutcomeThatMatchesTheExitCode(int exitCode, string outcome)
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            using TempDirectory temp = new();
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(Configuration(temp), powerShell.NewEnvironmentTable());
            DeploymentSession session = new(MinimalParameters(), noExitOnClose: true, compatibilityMode: false);
            session.SetExitCode(exitCode);

            // Act
            _ = session.Close(exitMessage: null);

            // Assert
            Assert.Contains(
                outcome,
                string.Join(Environment.NewLine, session.GetLogBuffer().Select(static entry => entry.Message)),
                StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that each convenience overload reaches the one that does the work with what it was given.
        /// </summary>
        /// <remarks>
        /// Six overloads standing in front of one, each supplying a different number of the arguments and leaving
        /// the rest to their defaults. A transposed argument would still compile and still write a log entry, so
        /// what is asserted is that each entry carries what its caller asked for.
        /// </remarks>
        [Fact]
        public void WriteLogEntry_OverloadsCarryTheirArgumentsThrough()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            using TempDirectory temp = new();
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(Configuration(temp), powerShell.NewEnvironmentTable());
            DeploymentSession session = new(MinimalParameters(), noExitOnClose: true, compatibilityMode: false);
            int written = session.GetLogBuffer().Count;

            // Act
            session.WriteLogEntry("plain");
            session.WriteLogEntry("severe", LogSeverity.Error);
            session.WriteLogEntry("sourced", LogSeverity.Warning, "TheSource");
            session.WriteLogEntry(["first", "second"]);
            session.WriteLogEntry(["third"], LogSeverity.Success);
            session.WriteLogEntry("hosted", writeHost: false);

            // Assert
            LogEntry[] entries = [.. session.GetLogBuffer().Skip(written)];
            Assert.Equal(
                ["plain", "severe", "sourced", "first", "second", "third", "hosted"],
                [.. entries.Select(static entry => entry.Message)]);
            Assert.Equal(LogSeverity.Info, entries[0].Severity);
            Assert.Equal(LogSeverity.Error, entries[1].Severity);
            Assert.Equal(LogSeverity.Warning, entries[2].Severity);
            Assert.Equal("TheSource", entries[2].Source);
            Assert.Equal(LogSeverity.Success, entries[5].Severity);

            // Assert: a list is written as one entry per line rather than as one joined entry.
            Assert.Equal(LogSeverity.Info, entries[3].Severity);
            Assert.Equal(LogSeverity.Info, entries[4].Severity);
        }

        /// <summary>
        /// Verifies that a source given but left blank is refused rather than written.
        /// </summary>
        /// <remarks>
        /// The source names the command a line came from and is the only attribution a reader gets, so a blank one
        /// is worse than none: it is checked only when supplied, since not supplying one lets the session work it
        /// out from the call stack.
        /// </remarks>
        [Fact]
        public void WriteLogEntry_RefusesASourceThatWasGivenButLeftBlank()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            using TempDirectory temp = new();
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(Configuration(temp), powerShell.NewEnvironmentTable());
            DeploymentSession session = new(MinimalParameters(), noExitOnClose: true, compatibilityMode: false);

            // Assert
            _ = Assert.Throws<ArgumentException>(() => session.WriteLogEntry("message", LogSeverity.Info, "   "));
        }

        /// <summary>
        /// Verifies that the log buffer is handed out as a view rather than as the list behind it.
        /// </summary>
        /// <remarks>
        /// A caller who could reach the list could rewrite what the session is about to flush to disk.
        /// </remarks>
        [Fact]
        public void GetLogBuffer_IsAViewRatherThanTheListBehindIt()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            using TempDirectory temp = new();
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(Configuration(temp), powerShell.NewEnvironmentTable());
            DeploymentSession session = new(MinimalParameters(), noExitOnClose: true, compatibilityMode: false);

            // Assert
            IReadOnlyList<LogEntry> buffer = session.GetLogBuffer();
            Assert.NotEmpty(buffer);
            Assert.IsNotType<List<LogEntry>>(buffer);
        }

        /// <summary>
        /// The parameters for a session that has not been told which mode to run in.
        /// </summary>
        /// <returns>A fresh dictionary a test can add to.</returns>
        private static Dictionary<string, object> AutoModeParameters()
        {
            return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                { "AppName", "TestApp" },
                { "AppVersion", "1.0.0" },
            };
        }

        /// <summary>
        /// The names compatibility mode publishes as variables.
        /// </summary>
        /// <returns>Every public property and field name of a session.</returns>
        private static IEnumerable<string> PublishedMemberNames()
        {
            return typeof(DeploymentSession).GetProperties().Select(static property => property.Name)
                .Concat(typeof(DeploymentSession).GetFields().Select(static field => field.Name));
        }

        /// <summary>
        /// Asserts that building a session throws, without letting the failure follow the test process out.
        /// </summary>
        /// <remarks>
        /// A constructor that fails closes the half-built session and puts its exit code on
        /// <see cref="Environment.ExitCode"/>. That is right for a deployment, whose process exists to carry the
        /// result, and wrong for a test host, which would then report a failure of its own after every test passed.
        /// </remarks>
        /// <typeparam name="T">The exception expected.</typeparam>
        /// <param name="construct">The construction to attempt.</param>
        /// <returns>The exception thrown.</returns>
        private static T ThrowsLeavingTheProcessExitCodeAlone<T>(Func<object> construct) where T : Exception
        {
            int processExitCode = Environment.ExitCode;
            try
            {
                return Assert.Throws<T>(construct);
            }
            finally
            {
                Environment.ExitCode = processExitCode;
            }
        }

        /// <summary>
        /// Removes a directory, tolerating one the code under test still holds open.
        /// </summary>
        /// <param name="directory">The directory to remove.</param>
        private static void Delete(DirectoryInfo directory)
        {
            try
            {
                directory.Delete(recursive: true);
            }
            catch (Exception ex) when (ex.Message is not null)
            {
                // Tidiness is not worth turning a passing test into a failing one.
            }
        }

        /// <summary>
        /// An application name no other test will produce, for the cases that write outside the scratch directory.
        /// </summary>
        /// <returns>The name.</returns>
        private static string UniqueAppName()
        {
            return $"TestApp{Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture)}";
        }

        /// <summary>
        /// What a log left behind by an earlier run is taken to contain.
        /// </summary>
        private const string PreviousContent = "a previous deployment wrote this";

        /// <summary>
        /// The parameters every session needs, which name the application and keep it out of the interactive path.
        /// </summary>
        /// <returns>A fresh dictionary a test can add to.</returns>
        private static Dictionary<string, object> MinimalParameters()
        {
            return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                { "AppName", "TestApp" },
                { "AppVersion", "1.0.0" },
                { "DeployMode", DeployMode.Silent },
            };
        }

        /// <summary>
        /// A configuration writing to a scratch directory and reading deferral history from a key that does not
        /// exist.
        /// </summary>
        /// <param name="temp">The scratch directory to log into.</param>
        /// <param name="registry">A scratch key for the tests that record a deferral, or nothing to point
        /// deferral history at a key that does not exist.</param>
        /// <returns>The configuration.</returns>
        private static ModuleConfiguration Configuration(TempDirectory temp, TempRegistryKey? registry = null)
        {
            return new ModuleConfiguration
            {
                LogPath = temp.GetPath("Logs"),
                RegPath = registry?.Path ?? @"HKCU:\SOFTWARE\PSAppDeployToolkit.Tests",
            };
        }

        /// <summary>
        /// Reads the settings a session is carrying.
        /// </summary>
        /// <remarks>
        /// Nine of the thirteen flags the constructor sets have no public surface at all, and the four that do
        /// only confirm themselves. Read directly so the mapping can be asserted as a mapping.
        /// </remarks>
        /// <param name="session">The session to read.</param>
        /// <returns>The session's settings.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the field is gone or holds nothing, since a
        /// silently skipped assertion would be worse than a failure.</exception>
        [SuppressMessage("Major Code Smell", "S3011:Reflection should not be used to increase accessibility of classes, methods, or fields", Justification = "The flags most of the constructor's switches set are private with no public surface, so reading the field is the only way to assert the mapping rather than its distant consequences.")]
        private static DeploymentSettings SettingsOf(DeploymentSession session)
        {
            FieldInfo field = typeof(DeploymentSession).GetField("Settings", BindingFlags.NonPublic | BindingFlags.Instance)
                ?? throw new InvalidOperationException("DeploymentSession no longer carries a Settings field for the tests to read.");
            return (DeploymentSettings)(field.GetValue(session)
                ?? throw new InvalidOperationException("DeploymentSession's Settings field held nothing."));
        }

        /// <summary>
        /// Every switch the constructor reads, against the flag it is meant to set.
        /// </summary>
        /// <remarks>
        /// <c>RequireAdmin</c> is absent because setting it decides whether the constructor throws, which is its
        /// own test rather than part of the mapping.
        /// </remarks>
        private static readonly IReadOnlyDictionary<string, DeploymentSettings> SwitchedSettings = new Dictionary<string, DeploymentSettings>(StringComparer.Ordinal)
        {
            { "SuppressRebootPassThru", DeploymentSettings.SuppressRebootPassThru },
            { "TerminalServerMode", DeploymentSettings.TerminalServerMode },
            { "DisableLogging", DeploymentSettings.DisableLogging },
            { "DisableDefaultMsiProcessList", DeploymentSettings.DisableDefaultMsiProcessList },
            { "ForceMsiDetection", DeploymentSettings.ForceMsiDetection },
            { "ForceWimDetection", DeploymentSettings.ForceWimDetection },
            { "NoSessionDetection", DeploymentSettings.NoSessionDetection },
            { "NoOobeDetection", DeploymentSettings.NoOobeDetection },
            { "NoProcessDetection", DeploymentSettings.NoProcessDetection },
            { "ProcessInteractivityDetection", DeploymentSettings.ProcessInteractivityDetection },
            { "ExitWithMsiCodes", DeploymentSettings.ExitWithMsiCodes },
            { "AllowWowProcess", DeploymentSettings.AllowWowProcess },
        };
    }
}
