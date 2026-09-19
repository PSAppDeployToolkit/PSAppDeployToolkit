using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Management.Automation;
using System.Reflection;
using PSAppDeployToolkit.Foundation;

namespace PSADT.PowerShellTestFixture
{
    /// <summary>
    /// Seats a module database for the lifetime of one test and puts back whatever was there.
    /// </summary>
    /// <remarks>
    /// The real database is only ever seated from inside <c language="text">PSAppDeployToolkit.psm1</c>, which
    /// <c language="csharp">ModuleDatabase.Init</c> enforces by inspecting the call stack. There is no way to satisfy that from a test,
    /// so the private field is set directly - which is also what lets a test choose the configuration a case turns on.
    /// <para>
    /// Restoring on disposal matters because the field is static: a test that left one seated would change what every
    /// later test sees.
    /// </para>
    /// </remarks>
    public sealed class ModuleDatabaseScope : IDisposable
    {
        /// <summary>
        /// Seats a database built from the given configuration.
        /// </summary>
        /// <param name="configuration">The configuration the types under test should read, or <see langword="null"/> to
        /// seat a database with no state, which is the module imported but not yet initialized.</param>
        /// <param name="sessionState">The module session state, which is what script blocks are invoked against.</param>
        /// <param name="moduleInfo">The module the session state belongs to, which the database records.</param>
        /// <param name="environment">The environment table the state is built around, where there is state.</param>
        internal ModuleDatabaseScope(ModuleConfiguration? configuration, SessionState sessionState, PSModuleInfo moduleInfo, EnvironmentTable? environment)
        {
            // A database carries what the module knows before it is initialized. The defaults are shaped rather than
            // real: one config entry and at least one string entry, each keyed on an empty string.
            Database = new ModuleDatabase(
                new Dictionary<string, IReadOnlyDictionary<string, ScriptBlock>>(StringComparer.OrdinalIgnoreCase)
                {
                    { "Config", new Dictionary<string, ScriptBlock>(StringComparer.OrdinalIgnoreCase) { { string.Empty, ScriptBlock.Create(string.Empty) } } },
                    { "Strings", new Dictionary<string, ScriptBlock>(StringComparer.OrdinalIgnoreCase) { { string.Empty, ScriptBlock.Create(string.Empty) } } },
                },
                new Hashtable { { "ModuleVersion", "4.0.0" } },
                moduleInfo,
                new ReadOnlyCollection<FileInfo>([new(typeof(ModuleDatabase).Assembly.Location)]),
                UnsignedSignature(),
                compiled: false);

            // The state is what Initialize-ADTModule seats, so a scope carrying one stands for an initialized module
            // and a scope without one stands for a module that has been imported and nothing more.
            if (configuration is not null)
            {
                ReadOnlyCollection<DirectoryInfo> directories = new([new(string.IsNullOrWhiteSpace(configuration.LogPath) ? Path.GetTempPath() : configuration.LogPath)]);
                Database.State = new ModuleState(
                    directories,
                    directories,
                    directories,
                    environment ?? throw new ArgumentNullException(nameof(environment), "A database seating state needs an environment table."),
                    configuration.ToHashtable(),
                    CultureInfo.GetCultureInfo("en-US"),
                    new Hashtable { { "Placeholder", string.Empty } },
                    DateTime.Now);
            }

            // Init seats this and a scope cannot go through Init, so it is set here. Left unset it reads as default,
            // which the property refuses rather than returns, and opening a session logs it.
            ImportDurationProperty.SetValue(Database, TimeSpan.FromSeconds(1));

            // The field is a nullable tuple, and SetValue takes a boxed value of the underlying type for one of those.
            _previous = ModuleField.GetValue(null);
            ModuleField.SetValue(null, (sessionState, Database));
        }

        /// <summary>
        /// The database this scope seated, for a test that needs to alter it further.
        /// </summary>
        public ModuleDatabase Database { get; }

        /// <summary>
        /// The open sessions the database holds, which a test can add to.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when this scope seated no state, which is what holds them.</exception>
        public IList<DeploymentSession> Sessions => Database.State?.Sessions ?? throw new InvalidOperationException("This scope seated no module state, so there are no sessions to reach.");

        /// <summary>
        /// Puts back whatever database was seated before.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            ModuleField.SetValue(null, _previous);
            _disposed = true;
        }

        /// <summary>
        /// Builds the signature a seated database carries, reporting the module as unsigned.
        /// </summary>
        /// <remarks><c language="powershell">Get-AuthenticodeSignature</c> is not reachable from this fixture's
        /// runspace, which loads Microsoft.PowerShell.Core alone, so the signature is built rather than read. Nothing
        /// on a path under test looks at it beyond <c language="csharp">ModuleDatabase.Signed</c>, which this answers
        /// truthfully.</remarks>
        /// <returns>A signature whose status is not valid.</returns>
        private static Signature UnsignedSignature()
        {
            return (Signature)SignatureConstructor.Invoke([typeof(ModuleDatabaseScope).Assembly.Location, TrustENoSignature]);
        }

        /// <summary>
        /// Whatever was seated before, which is almost always nothing.
        /// </summary>
        private readonly object? _previous;

        /// <summary>
        /// TRUST_E_NOSIGNATURE, which is what a signature reports for a file carrying none.
        /// </summary>
        private const uint TrustENoSignature = 0x800B0100;

        /// <summary>
        /// The signature constructor taking a path and a Win32 error, which is the only way to build one.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the constructor is not where it was expected, which
        /// means the type changed rather than that the code under test is wrong.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S3011:Reflection should not be used to increase accessibility of classes, methods, or fields", Justification = "Signature has no public constructor and the cmdlet that builds one is not loadable in this fixture's runspace.")]
        private static readonly ConstructorInfo SignatureConstructor = typeof(Signature).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, binder: null, [typeof(string), typeof(uint)], modifiers: null)
            ?? throw new InvalidOperationException("Signature no longer has an internal constructor taking a file path and a Win32 error.");

        /// <summary>
        /// Whether the database has already been put back.
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// The field holding the module's session state and database.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the field is not where it was expected, which means
        /// the type changed rather than that the code under test is wrong.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S3011:Reflection should not be used to increase accessibility of classes, methods, or fields", Justification = "ModuleDatabase.Init refuses any caller outside the module, so setting the field is the only way to seat a database for a test.")]
        private static readonly FieldInfo ModuleField = typeof(ModuleDatabase).GetField("Module", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("ModuleDatabase no longer holds its session state and database in a private static field named Module.");

        /// <summary>
        /// The import duration, which only <c language="csharp">ModuleDatabase.Init</c> can otherwise set.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the property is not where it was expected, which
        /// means the type changed rather than that the code under test is wrong.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S3011:Reflection should not be used to increase accessibility of classes, methods, or fields", Justification = "The setter is private and only reachable through Init, which refuses any caller outside the module.")]
        private static readonly PropertyInfo ImportDurationProperty = typeof(ModuleDatabase).GetProperty("ImportDuration", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("ModuleDatabase no longer carries an ImportDuration property.");
    }
}
