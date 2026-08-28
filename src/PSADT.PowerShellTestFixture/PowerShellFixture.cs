using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using PSAppDeployToolkit.Foundation;

namespace PSADT.PowerShellTestFixture
{
    /// <summary>
    /// A PowerShell engine hosted in the test process, so that the types in PSAppDeployToolkit can be
    /// exercised through their real constructors rather than through hand-built stand-ins.
    /// </summary>
    /// <remarks>
    /// Nothing in that assembly is reachable without an engine. <see cref="EnvironmentTable"/> takes a live
    /// <see cref="PSCmdlet"/>; <c>ModuleDatabase</c> holds a <see cref="PSObject"/> only the module seeds; and
    /// <c>LogUtilities</c>, once a runspace exists, stops resolving its caller from the stack and starts
    /// evaluating <c>&amp; $Script:CommandTable.'Get-PSCallStack'</c> in the session state the database carries.
    /// That last one is why this fixture imports a script module as well as this assembly: the variable has to
    /// exist in a real module's session state or the first log entry written fails.
    /// <para>
    /// One instance serves a whole test collection. A runspace runs one pipeline at a time and
    /// <see cref="Runspace.DefaultRunspace"/> is per-thread, so tests using it have to be serialised and each
    /// has to adopt the runspace on whichever thread it landed on - which is what <see cref="Enter"/> is for.
    /// The collection wrapper that enforces the serialisation lives in each test project rather than here,
    /// which keeps this assembly free of any dependency on a test framework.
    /// </para>
    /// </remarks>
    public sealed class PowerShellFixture : IDisposable
    {
        /// <summary>
        /// Opens a runspace and imports both halves of the fixture into it.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the engine refuses either import, since every
        /// later failure would otherwise be misdiagnosed as a fault in the code under test.</exception>
        public PowerShellFixture()
        {
            // CreateDefault2 loads Microsoft.PowerShell.Core alone and leaves the rest to be auto-loaded on
            // demand, which is what a real host does and is markedly faster to open than CreateDefault.
            InitialSessionState initialSessionState = InitialSessionState.CreateDefault2();
            initialSessionState.ExecutionPolicy = Microsoft.PowerShell.ExecutionPolicy.Bypass;
            Runspace = RunspaceFactory.CreateRunspace(initialSessionState);
            Runspace.Open();

            // The script module supplies $Script:CommandTable and the session state that holds it. Imported by
            // path rather than by assembly so that the module takes the file's name: an assembly imported with
            // -Assembly is named "dynamic_code_module_<full assembly name>", and that name reaches
            // EnvironmentTable.AppDeployToolkitName, from where it is built into install names, log file names
            // and registry paths.
            string fixtureDirectory = Path.GetDirectoryName(typeof(PowerShellFixture).Assembly.Location)
                ?? throw new InvalidOperationException("The fixture assembly has no directory to load its script module from.");
            ScriptModulePath = Path.Join(fixtureDirectory, $"{ModuleName}.psm1");
            if (!File.Exists(ScriptModulePath))
            {
                throw new InvalidOperationException($"The fixture's script module is missing from [{ScriptModulePath}]. The test project has to copy it beside the assembly.");
            }
            _ = InvokeInRunspace($"Import-Module -Name '{ScriptModulePath}' -Force -ErrorAction Stop");
            _ = InvokeInRunspace($"Import-Module -Name '{typeof(PowerShellFixture).Assembly.Location}' -Force -ErrorAction Stop");

            // Fetched once. A session state is a handle onto a scope rather than anything thread-affine, so the
            // one read here serves every test regardless of which thread asks for it.
            ModuleSessionState = Unwrap<SessionState>(InvokeInRunspace("Get-FixtureSessionState"));
        }

        /// <summary>
        /// The runspace this fixture owns.
        /// </summary>
        public Runspace Runspace { get; }

        /// <summary>
        /// The session state of the fixture's script module, which is the one to place in the module database.
        /// </summary>
        /// <remarks>
        /// It has to be a module's own session state rather than the global one. <c>LogUtilities</c> reaches
        /// PowerShell through <c>$Script:CommandTable</c>, and only the module that declares that variable can
        /// resolve it.
        /// </remarks>
        public SessionState ModuleSessionState { get; }

        /// <summary>
        /// The full path of the script module half of the fixture.
        /// </summary>
        public string ScriptModulePath { get; }

        /// <summary>
        /// The name the fixture's module is imported under, which becomes an environment table's
        /// <c>AppDeployToolkitName</c>.
        /// </summary>
        public const string ModuleName = "PSADT.PowerShellTestFixture";

        /// <summary>
        /// Adopts this fixture's runspace as the calling thread's default for the lifetime of the returned scope.
        /// </summary>
        /// <remarks>
        /// Needed because <see cref="Runspace.DefaultRunspace"/> is per-thread and a test does not run on the
        /// thread that built the fixture. It is also what decides which path the code under test takes: with no
        /// default runspace <c>LogUtilities</c> resolves its caller from the CLR stack, and with one it asks
        /// PowerShell instead, so both paths are reachable by entering or not entering this scope.
        /// </remarks>
        /// <returns>A scope that restores whatever the thread's default was on disposal.</returns>
        public IDisposable Enter()
        {
            return new RunspaceScope(Runspace);
        }

        /// <summary>
        /// Builds an <see cref="EnvironmentTable"/> through its real constructor.
        /// </summary>
        /// <remarks>
        /// Each call produces a distinct table, which is deliberate: it is what lets a test assert that two
        /// tables built moments apart are not equal, and therefore that the type is an identity rather than a
        /// value.
        /// </remarks>
        /// <returns>A new environment table describing this machine.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the engine returns no table.</exception>
        public EnvironmentTable NewEnvironmentTable()
        {
            return Unwrap<EnvironmentTable>(InvokeInRunspace("New-TestEnvironmentTable"));
        }

        /// <summary>
        /// Builds an <see cref="EnvironmentTable"/> from chosen version information rather than the runspace's own.
        /// </summary>
        /// <remarks>
        /// The two versions a caller can supply to the real constructor, so a test can exercise the table's
        /// treatment of absent, zero and missing version parts instead of only whatever this machine reports.
        /// </remarks>
        /// <param name="psVersion">The engine version to hand the table.</param>
        /// <param name="clrVersion">The CLR version to record, or <see langword="null"/> to record none.</param>
        /// <returns>A new environment table carrying those versions.</returns>
        public EnvironmentTable NewEnvironmentTable(Version psVersion, Version? clrVersion)
        {
            ArgumentNullException.ThrowIfNull(psVersion);
            return Unwrap<EnvironmentTable>(InvokeInRunspace($"New-TestEnvironmentTable -PSVersion '{psVersion}' -CLRVersion {(clrVersion is null ? "$null" : $"'{clrVersion}'")}"));
        }

        /// <summary>
        /// Seats a module database for the lifetime of the returned scope.
        /// </summary>
        /// <remarks>
        /// The session state is this fixture's module, so script blocks the code under test builds resolve
        /// <c>$Script:CommandTable</c> against it.
        /// </remarks>
        /// <param name="configuration">The configuration the types under test should read.</param>
        /// <param name="environment">The environment table, where the test needs one.</param>
        /// <returns>A scope that puts back whatever database was seated before.</returns>
        public ModuleDatabaseScope SeatModuleDatabase(ModuleConfiguration configuration, EnvironmentTable? environment = null)
        {
            return new ModuleDatabaseScope(configuration, ModuleSessionState, environment);
        }

        /// <summary>
        /// Runs a script in this fixture's runspace and returns what it wrote to the pipeline.
        /// </summary>
        /// <param name="script">The script to run.</param>
        /// <returns>The objects the script wrote out.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the script wrote to the error stream.</exception>
        public ReadOnlyCollection<PSObject> InvokeInRunspace(string script)
        {
            using PowerShell powerShell = PowerShell.Create();
            powerShell.Runspace = Runspace;
            _ = powerShell.AddScript(script);
            Collection<PSObject> output = powerShell.Invoke();
            ThrowIfWroteErrors(powerShell, script);
            return new ReadOnlyCollection<PSObject>(output);
        }

        /// <summary>
        /// Fails if the engine wrote anything to its error stream.
        /// </summary>
        /// <remarks>
        /// Reported rather than ignored. A failed import or an unresolvable command shows up here and nowhere
        /// else - an unsuccessful pipeline still returns an empty collection rather than throwing - so a fixture
        /// that carried on regardless would fail later, somewhere unrelated, and much less legibly.
        /// </remarks>
        /// <param name="powerShell">The engine that has just run.</param>
        /// <param name="ran">What it was asked to run, for the message.</param>
        /// <exception cref="InvalidOperationException">Thrown when the error stream is not empty.</exception>
        private static void ThrowIfWroteErrors(PowerShell powerShell, string ran)
        {
            if (powerShell.Streams.Error.Count is 0)
            {
                return;
            }
            throw new InvalidOperationException($"The fixture asked the engine to run [{ran}], which wrote to the error stream: {string.Join("; ", powerShell.Streams.Error.Select(static e => e.ToString()))}");
        }

        /// <summary>
        /// Takes the single object a fixture script wrote out and unwraps it to the type expected.
        /// </summary>
        /// <typeparam name="T">The type the script was expected to write.</typeparam>
        /// <param name="written">What the engine wrote to the pipeline.</param>
        /// <returns>The unwrapped object.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the script wrote nothing, or wrote something else.</exception>
        private static T Unwrap<T>(IReadOnlyList<PSObject> written) where T : class
        {
            if (written.Count is not 1)
            {
                throw new InvalidOperationException($"The fixture expected one {typeof(T).Name} from the engine but got {written.Count.ToString(CultureInfo.InvariantCulture)} objects.");
            }
            object? value = written[0];
            while (value is PSObject psObject)
            {
                value = psObject.BaseObject;
            }
            return value as T
                ?? throw new InvalidOperationException($"The fixture expected a {typeof(T).Name} from the engine but got a {value?.GetType().Name ?? "null"}.");
        }

        /// <summary>
        /// Closes the runspace.
        /// </summary>
        public void Dispose()
        {
            // Whichever thread disposes the fixture may not be the one that entered it, and a stale default
            // runspace pointing at a closed one produces failures far from here.
            if (ReferenceEquals(Runspace.DefaultRunspace, Runspace))
            {
                Runspace.DefaultRunspace = null;
            }
            Runspace.Dispose();
        }

        /// <summary>
        /// Holds a thread's default runspace for the lifetime of a test and puts back what was there before.
        /// </summary>
        private sealed class RunspaceScope : IDisposable
        {
            /// <summary>
            /// Adopts the given runspace on the calling thread, remembering what was there.
            /// </summary>
            /// <param name="runspace">The runspace to adopt.</param>
            internal RunspaceScope(Runspace runspace)
            {
                _previous = Runspace.DefaultRunspace;
                Runspace.DefaultRunspace = runspace;
            }

            /// <summary>
            /// Restores the thread's previous default runspace.
            /// </summary>
            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }
                Runspace.DefaultRunspace = _previous;
                _disposed = true;
            }

            /// <summary>
            /// Whatever the thread had before, which is almost always nothing.
            /// </summary>
            private readonly Runspace? _previous;

            /// <summary>
            /// Whether the adoption has already been undone.
            /// </summary>
            private bool _disposed;
        }
    }
}
