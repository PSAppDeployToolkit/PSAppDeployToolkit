using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using PSADT.PowerShellTestFixture;
using PSAppDeployToolkit.Foundation;
using PSAppDeployToolkit.Tests.TestHelpers;
using Xunit;

namespace PSAppDeployToolkit.Tests.Foundation
{
    /// <summary>
    /// Tests everything a seated module knows about itself.
    /// </summary>
    /// <remarks>
    /// This type is the module's initialized state, and its existence is what "initialized" means: the database holds
    /// one of these or it holds nothing, and every reader keys off that. So the constructor's refusals matter more
    /// than they would on an ordinary container - a half-built one seated anyway would be a module reporting itself
    /// ready while missing whatever failed to arrive.
    /// </remarks>
    /// <param name="powerShell">The hosted engine, shared across the collection.</param>
    [Collection(PowerShellCollection.Name)]
    public sealed class ModuleStateTests(PowerShellFixture powerShell)
    {
        /// <summary>
        /// Verifies that a well-formed state hands back everything it was given.
        /// </summary>
        [Fact]
        public void Constructor_HandsBackWhatItWasGiven()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            EnvironmentTable environment = powerShell.NewEnvironmentTable();
            Hashtable config = Config();
            Hashtable strings = Strings();
            CultureInfo language = CultureInfo.GetCultureInfo("en-AU");

            // Act
            ModuleState state = new(Directories(@"C:\Script"), Directories(@"C:\Config"), Directories(@"C:\Strings"), environment, config, strings, language, DateTime.Now);

            // Assert
            Assert.Same(environment, state.Environment);
            Assert.Same(config, state.Config);
            Assert.Same(strings, state.StringTable);
            Assert.Same(language, state.Language);
            Assert.Equal([@"C:\Script"], [.. state.Directories.Script.Select(static d => d.FullName)]);
            Assert.Equal([@"C:\Config"], [.. state.Directories.Config.Select(static d => d.FullName)]);
            Assert.Equal([@"C:\Strings"], [.. state.Directories.Strings.Select(static d => d.FullName)]);
        }

        /// <summary>
        /// Verifies that a state starts with no sessions and no exit code.
        /// </summary>
        /// <remarks>
        /// The exit code is deliberately absent rather than zero. Nothing has exited yet, and a zero here would read
        /// as a deployment that succeeded - which is how a failure path came to report success before this was
        /// nullable.
        /// </remarks>
        [Fact]
        public void Constructor_StartsWithNoSessionsAndNoExitCode()
        {
            // Act
            ModuleState state = NewState();

            // Assert
            Assert.Empty(state.Sessions);
            Assert.Null(state.LastExitCode);
            Assert.Null(state.RestartOnExitOptions);
        }

        /// <summary>
        /// Verifies that the time spent initializing is measured from the moment the caller started.
        /// </summary>
        /// <remarks>
        /// The caller passes when it began rather than how long it took, so the state times itself. Asserted as a
        /// range rather than a value, since the only thing worth pinning is that it measures the caller's work and
        /// not its own.
        /// </remarks>
        [Fact]
        public void Constructor_MeasuresTheInitializationFromWhenItStarted()
        {
            // Arrange
            DateTime started = DateTime.Now.AddSeconds(-5);

            // Act
            ModuleState state = NewState(started);

            // Assert
            Assert.InRange(state.InitDuration, TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(30));
        }

        /// <summary>
        /// Verifies that a state missing something it cannot work without is refused.
        /// </summary>
        /// <remarks>
        /// Each of these is read by name somewhere on a deployment's path, so a null reaches its caller as a failure
        /// a long way from the initialization that let it through.
        /// </remarks>
        [Fact]
        public void Constructor_RefusesAnythingItCannotWorkWithout()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            EnvironmentTable environment = powerShell.NewEnvironmentTable();
            ReadOnlyCollection<DirectoryInfo> directories = Directories(@"C:\Script");

            // Assert
            _ = Assert.Throws<ArgumentNullException>(() => new ModuleState(directories, directories, directories, null!, Config(), Strings(), CultureInfo.InvariantCulture, DateTime.Now));
            _ = Assert.Throws<ArgumentNullException>(() => new ModuleState(directories, directories, directories, environment, null!, Strings(), CultureInfo.InvariantCulture, DateTime.Now));
            _ = Assert.Throws<ArgumentNullException>(() => new ModuleState(directories, directories, directories, environment, Config(), null!, CultureInfo.InvariantCulture, DateTime.Now));
            _ = Assert.Throws<ArgumentNullException>(() => new ModuleState(directories, directories, directories, environment, Config(), Strings(), null!, DateTime.Now));
        }

        /// <summary>
        /// Verifies that an empty config or string table is refused as firmly as an absent one.
        /// </summary>
        /// <remarks>
        /// An empty table is the shape a failed import leaves behind, and it would otherwise seat happily and fail
        /// later at whichever value was asked for first.
        /// </remarks>
        [Fact]
        public void Constructor_RefusesAnEmptyConfigOrStringTable()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            EnvironmentTable environment = powerShell.NewEnvironmentTable();
            ReadOnlyCollection<DirectoryInfo> directories = Directories(@"C:\Script");

            // Assert
            _ = Assert.Throws<ArgumentException>(() => new ModuleState(directories, directories, directories, environment, [], Strings(), CultureInfo.InvariantCulture, DateTime.Now));
            _ = Assert.Throws<ArgumentException>(() => new ModuleState(directories, directories, directories, environment, Config(), [], CultureInfo.InvariantCulture, DateTime.Now));
        }

        /// <summary>
        /// Verifies that a state with no config or strings directory of its own is accepted.
        /// </summary>
        /// <remarks>
        /// The ordinary case for a deployment carrying neither, which runs on the module's shipped defaults. Only the
        /// script directory is required.
        /// </remarks>
        [Fact]
        public void Constructor_AcceptsAStateWithOnlyAScriptDirectory()
        {
            // Arrange
            using IDisposable scope = powerShell.Enter();
            EnvironmentTable environment = powerShell.NewEnvironmentTable();

            // Act
            ModuleState state = new(Directories(@"C:\Script"), configDirectories: null, stringDirectories: null, environment, Config(), Strings(), CultureInfo.InvariantCulture, DateTime.Now);

            // Assert
            _ = Assert.Single(state.Directories.Script);
            Assert.Empty(state.Directories.Config);
            Assert.Empty(state.Directories.Strings);
        }

        /// <summary>
        /// Verifies that the sessions a state holds can be added to and removed from.
        /// </summary>
        /// <remarks>
        /// Opening and closing a deployment session is exactly this list growing and shrinking, so it has to be the
        /// live one rather than a copy handed out on each read.
        /// </remarks>
        [Fact]
        public void Sessions_IsTheLiveListRatherThanACopy()
        {
            // Arrange
            ModuleState state = NewState();

            // Act
            state.Sessions.Add(null!);

            // Assert
            _ = Assert.Single(state.Sessions);
            _ = state.Sessions.Remove(null!);
            Assert.Empty(state.Sessions);
        }

        /// <summary>
        /// Builds a state around the fixture's environment table, for a case that does not turn on its contents.
        /// </summary>
        /// <param name="initStartDateTime">When the initialization being measured began.</param>
        /// <returns>The state.</returns>
        private ModuleState NewState(DateTime? initStartDateTime = null)
        {
            using IDisposable scope = powerShell.Enter();
            ReadOnlyCollection<DirectoryInfo> directories = Directories(@"C:\Script");
            return new ModuleState(directories, directories, directories, powerShell.NewEnvironmentTable(), Config(), Strings(), CultureInfo.InvariantCulture, initStartDateTime ?? DateTime.Now);
        }

        /// <summary>
        /// Builds directories for the given paths without touching the file system.
        /// </summary>
        /// <param name="paths">The paths to build directories for.</param>
        /// <returns>The directories.</returns>
        private static ReadOnlyCollection<DirectoryInfo> Directories(params string[] paths)
        {
            return new ReadOnlyCollection<DirectoryInfo>([.. paths.Select(static p => new DirectoryInfo(p))]);
        }

        /// <summary>
        /// Builds a config carrying enough to be accepted.
        /// </summary>
        /// <returns>The config.</returns>
        private static Hashtable Config()
        {
            return new Hashtable { { "Toolkit", new Hashtable { { "CompanyName", "Somewhere" } } } };
        }

        /// <summary>
        /// Builds a string table carrying enough to be accepted.
        /// </summary>
        /// <returns>The string table.</returns>
        private static Hashtable Strings()
        {
            return new Hashtable { { "BalloonTip", new Hashtable { { "Start", "Started." } } } };
        }
    }
}
