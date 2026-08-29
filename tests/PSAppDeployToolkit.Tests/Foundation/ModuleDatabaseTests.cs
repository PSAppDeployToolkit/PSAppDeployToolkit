using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using PSADT.PowerShellTestFixture;
using PSAppDeployToolkit.Foundation;
using PSAppDeployToolkit.Tests.TestHelpers;
using Xunit;

namespace PSAppDeployToolkit.Tests.Foundation
{
    /// <summary>
    /// Tests the store every other type in the assembly reads its state from.
    /// </summary>
    /// <remarks>
    /// Two behaviours matter here. One is what each reader does when nothing has been seated, because the toolkit can be
    /// asked to log or read configuration before the module has initialised and the message a caller gets is the only
    /// guidance they have. The other is that a session is found by position rather than by identity.
    /// </remarks>
    /// <param name="powerShell">The hosted engine, shared across the collection.</param>
    [Collection(PowerShellCollection.Name)]
    public sealed class ModuleDatabaseTests(PowerShellFixture powerShell)
    {
        /// <summary>
        /// Verifies that the readers needing a whole database say so in terms of loading the module.
        /// </summary>
        /// <remarks>
        /// These two are asked for by code that only runs inside PowerShell, so the useful advice is that the assembly
        /// was loaded some other way - not that a command was missed.
        /// </remarks>
        [Fact]
        public void Get_SaysTheAssemblyWasNotLoadedByTheModule()
        {
            Assert.Contains(
                "only supports loading via the PSAppDeployToolkit PowerShell module",
                Assert.Throws<InvalidOperationException>(static () => ModuleDatabase.Get()).Message,
                StringComparison.Ordinal);
            Assert.Contains(
                "only supports loading via the PSAppDeployToolkit PowerShell module",
                Assert.Throws<InvalidOperationException>(static () => ModuleDatabase.GetSessionState()).Message,
                StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that the readers needing initialised state name the command that provides it.
        /// </summary>
        /// <remarks>
        /// A different message from the one above, and deliberately so: the assembly loaded correctly but
        /// <c language="powershell">Initialize-ADTModule</c> has not run, which is something the caller can act on.
        /// </remarks>
        [Fact]
        public void GetEnvironment_NamesTheCommandThatInitialisesTheModule()
        {
            foreach (Func<object> reader in new Func<object>[] { ModuleDatabase.GetEnvironment, ModuleDatabase.GetConfig, ModuleDatabase.GetStrings })
            {
                Assert.Contains(
                    "[Initialize-ADTModule] is called",
                    Assert.Throws<InvalidOperationException>(reader).Message,
                    StringComparison.Ordinal);
            }
        }

        /// <summary>
        /// Verifies that a database with nothing in it is not mistaken for an initialised one.
        /// </summary>
        [Fact]
        public void IsInitialized_IsFalseUntilSomethingIsSeated()
        {
            Assert.False(ModuleDatabase.IsInitialized());
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(new ModuleConfiguration());
            Assert.True(ModuleDatabase.IsInitialized());
        }

        /// <summary>
        /// Verifies that a seated database is handed back with what was put in it.
        /// </summary>
        [Fact]
        public void GetConfig_HandsBackWhatWasSeated()
        {
            // Arrange
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(new ModuleConfiguration { LogStyle = "Legacy" });

            // Act
            IDictionary toolkit = Assert.IsAssignableFrom<IDictionary>(ModuleDatabase.GetConfig()["Toolkit"]);

            // Assert
            Assert.Equal("Legacy", toolkit["LogStyle"]);
            Assert.NotNull(ModuleDatabase.GetStrings());
            Assert.Same(powerShell.ModuleSessionState, ModuleDatabase.GetSessionState());
        }

        /// <summary>
        /// Verifies that the environment table is handed back when one was seated.
        /// </summary>
        [Fact]
        public void GetEnvironment_HandsBackTheTableThatWasSeated()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            EnvironmentTable environment = powerShell.NewEnvironmentTable();
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(new ModuleConfiguration(), environment);

            // Assert
            Assert.Same(environment, ModuleDatabase.GetEnvironment());
        }

        /// <summary>
        /// Verifies that a seated database with no environment table still reports the initialisation message.
        /// </summary>
        /// <remarks>
        /// The state a caller reaches by loading the module and not initialising it, which is exactly what the message
        /// is for.
        /// </remarks>
        [Fact]
        public void GetEnvironment_StillRefusesWhenNoTableWasSeated()
        {
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(new ModuleConfiguration());
            Assert.Contains(
                "[Initialize-ADTModule] is called",
                Assert.Throws<InvalidOperationException>(static () => ModuleDatabase.GetEnvironment()).Message,
                StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that no session is reported as active until one is opened.
        /// </summary>
        /// <remarks>
        /// The check the client/server log reader makes on every frame it reads, so both the no-database and the
        /// empty-list cases are reached in normal running - before a session opens and after the last one closes.
        /// </remarks>
        [Fact]
        public void IsDeploymentSessionActive_IsFalseWithNoDatabaseAndWithNoSessions()
        {
            Assert.False(ModuleDatabase.IsDeploymentSessionActive());
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(new ModuleConfiguration());
            Assert.False(ModuleDatabase.IsDeploymentSessionActive());
        }

        /// <summary>
        /// Verifies that asking for a session when none is open names the command that opens one.
        /// </summary>
        [Fact]
        public void GetDeploymentSession_NamesTheCommandThatOpensASession()
        {
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(new ModuleConfiguration());
            Assert.Contains(
                "[Open-ADTSession] is called",
                Assert.Throws<InvalidOperationException>(static () => ModuleDatabase.GetDeploymentSession()).Message,
                StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that the most recently opened session is the one handed back.
        /// </summary>
        /// <remarks>
        /// Sessions nest: a deployment script may open one and call something that opens another, and the innermost is
        /// the one a log entry belongs to. Position in the list is what decides, so this is the behaviour every caller
        /// of <c language="powershell">Get-ADTSession</c> depends on.
        /// </remarks>
        [Fact]
        public void GetDeploymentSession_HandsBackTheMostRecentlyOpened()
        {
            // Arrange
            using TempDirectory temp = new();
            using IDisposable scope = powerShell.Enter();
            EnvironmentTable environment = powerShell.NewEnvironmentTable();
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(Configuration(temp), environment);

            DeploymentSession first = NewSession("First App");
            DeploymentSession second = NewSession("Second App");
            database.Sessions.Add(first);
            database.Sessions.Add(second);

            // Assert
            Assert.True(ModuleDatabase.IsDeploymentSessionActive());
            Assert.Same(second, ModuleDatabase.GetDeploymentSession());

            // Act: closing the innermost leaves the outer one current, as Close-ADTSession does.
            _ = database.Sessions.Remove(second);

            // Assert
            Assert.Same(first, ModuleDatabase.GetDeploymentSession());
        }

        /// <summary>
        /// Verifies that a script is invoked against the module's session state.
        /// </summary>
        /// <remarks>
        /// The session state matters rather than merely the runspace: the script blocks the toolkit builds refer to
        /// <c language="powershell">$Script:CommandTable</c>, which only the module's own scope can resolve.
        /// </remarks>
        [Fact]
        public void InvokeScript_RunsAgainstTheModulesSessionState()
        {
            using IDisposable scope = powerShell.Enter();
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(new ModuleConfiguration());

            // Act
            ModuleDatabase.InvokeScript(ScriptBlock.Create("$Script:InvokeScriptRan = $args[0]"), "yes");

            // Assert
            Assert.Equal("yes", powerShell.ModuleSessionState.PSVariable.GetValue("InvokeScriptRan"));
        }

        /// <summary>
        /// Verifies that the script's own command table is reachable from an invoked script.
        /// </summary>
        /// <remarks>
        /// Directly the thing that failed first when this fixture was built: without a module session state carrying
        /// that variable, every log entry written under a runspace fails.
        /// </remarks>
        [Fact]
        public void InvokeScript_ReachesTheModulesCommandTable()
        {
            using IDisposable scope = powerShell.Enter();
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(new ModuleConfiguration());

            // Act
            IEnumerable<string> names = ModuleDatabase.InvokeScript<string>(ScriptBlock.Create("$Script:CommandTable.Keys"));

            // Assert
            Assert.Contains("Get-PSCallStack", names, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Verifies that the generic form unwraps each result to the type asked for.
        /// </summary>
        [Fact]
        public void InvokeScript_UnwrapsEachResultToTheTypeAskedFor()
        {
            using IDisposable scope = powerShell.Enter();
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(new ModuleConfiguration());

            Assert.Equal([1, 2, 3], ModuleDatabase.InvokeScript<int>(ScriptBlock.Create("1; 2; 3")));
            Assert.Equal(["one"], ModuleDatabase.InvokeScript<string>(ScriptBlock.Create("'one'")), StringComparer.Ordinal);
        }

        /// <summary>
        /// Verifies that the generic form fails rather than skipping a result of the wrong type.
        /// </summary>
        /// <remarks>
        /// It casts rather than filters, so a script returning something unexpected is a fault at the call site rather
        /// than a quietly short result. Worth pinning: the alternative would hide a changed script.
        /// </remarks>
        [Fact]
        public void InvokeScript_FailsOnAResultOfTheWrongType()
        {
            using IDisposable scope = powerShell.Enter();
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(new ModuleConfiguration());

            _ = Assert.Throws<InvalidCastException>(static () => ModuleDatabase.InvokeScript<int>(ScriptBlock.Create("'not a number'")).ToList());
        }

        /// <summary>
        /// Verifies that seating a database is refused from outside the module.
        /// </summary>
        /// <remarks>
        /// The guard reads the call stack for a frame belonging to <c language="text">PSAppDeployToolkit.psm1</c> inside a module of
        /// that name, so nothing a test can do will satisfy it - which is the point, and is why these tests seat the
        /// field directly instead.
        /// <para>
        /// It runs before the argument checks, so a caller outside the module is told where it is calling from rather
        /// than what it passed. Asserted with a valid argument as well as with nothing, so the ordering is what is
        /// being shown rather than a coincidence.
        /// </para>
        /// </remarks>
        [Fact]
        public void Init_IsRefusedFromOutsideTheModule()
        {
            using IDisposable scope = powerShell.Enter();

            // Arrange
            PSObject database = new();
            database.Properties.Add(new PSNoteProperty("Initialized", value: true));

            // Assert
            Assert.Contains(
                "can only be initialized from within the PSAppDeployToolkit module",
                Assert.Throws<InvalidOperationException>(() => ModuleDatabase.Init(database)).Message,
                StringComparison.Ordinal);
            _ = Assert.Throws<InvalidOperationException>(static () => ModuleDatabase.Init(null!));
        }

        /// <summary>
        /// Verifies that clearing the database is refused from outside the module.
        /// </summary>
        [Fact]
        public void Clear_IsRefusedFromOutsideTheModule()
        {
            using IDisposable scope = powerShell.Enter();

            Assert.Contains(
                "can only be cleared from within the PSAppDeployToolkit module",
                Assert.Throws<InvalidOperationException>(ModuleDatabase.Clear).Message,
                StringComparison.Ordinal);
        }

        /// <summary>
        /// Builds a session against the seated database.
        /// </summary>
        /// <param name="appName">The application name, which is what tells two sessions apart in a log.</param>
        /// <returns>The session.</returns>
        private static DeploymentSession NewSession(string appName)
        {
            return new DeploymentSession(
                new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                {
                    { "AppName", appName },
                    { "AppVersion", "1.0.0" },
                    { "DeployMode", DeployMode.Silent },
                },
                noExitOnClose: true,
                compatibilityMode: false);
        }

        /// <summary>
        /// A configuration writing to a scratch directory and reading deferral history from a key that does not exist.
        /// </summary>
        /// <param name="temp">The scratch directory to log into.</param>
        /// <returns>The configuration.</returns>
        private static ModuleConfiguration Configuration(TempDirectory temp)
        {
            return new ModuleConfiguration
            {
                LogPath = temp.GetPath("Logs"),
                RegPath = @"HKCU:\SOFTWARE\PSAppDeployToolkit.Tests",
            };
        }
    }
}
