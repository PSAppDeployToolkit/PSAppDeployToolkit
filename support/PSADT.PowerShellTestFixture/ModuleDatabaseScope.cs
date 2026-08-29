using System;
using System.Collections.Generic;
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
        /// <param name="configuration">The configuration the types under test should read.</param>
        /// <param name="sessionState">The module session state, which is what script blocks are invoked against.</param>
        /// <param name="environment">The environment table, where the test needs one.</param>
        internal ModuleDatabaseScope(ModuleConfiguration configuration, SessionState sessionState, EnvironmentTable? environment)
        {
            ArgumentNullException.ThrowIfNull(configuration);

            PSObject directories = new();
            directories.Properties.Add(new PSNoteProperty("Config", new[] { configuration.LogPath }));
            directories.Properties.Add(new PSNoteProperty("Strings", new[] { configuration.LogPath }));

            PSObject durations = new();
            durations.Properties.Add(new PSNoteProperty("ModuleImport", TimeSpan.FromSeconds(1)));
            durations.Properties.Add(new PSNoteProperty("ModuleInit", TimeSpan.FromSeconds(1)));

            Sessions = [];
            Database = new PSObject();
            Database.Properties.Add(new PSNoteProperty("Initialized", value: true));
            Database.Properties.Add(new PSNoteProperty("Environment", environment));
            Database.Properties.Add(new PSNoteProperty("Config", configuration.ToHashtable()));
            Database.Properties.Add(new PSNoteProperty("Strings", new System.Collections.Hashtable()));
            Database.Properties.Add(new PSNoteProperty("Sessions", Sessions));
            Database.Properties.Add(new PSNoteProperty("SessionState", sessionState));
            Database.Properties.Add(new PSNoteProperty("Directories", directories));
            Database.Properties.Add(new PSNoteProperty("Durations", durations));
            Database.Properties.Add(new PSNoteProperty("Language", "en"));
            Database.Properties.Add(new PSNoteProperty("LastExitCode", 0));

            _previous = DatabaseField.GetValue(null);
            DatabaseField.SetValue(null, Database);
        }

        /// <summary>
        /// The database this scope seated, for a test that needs to alter it further.
        /// </summary>
        public PSObject Database { get; }

        /// <summary>
        /// The open sessions the database holds, which a test can add to.
        /// </summary>
        /// <remarks>The instance behind this is a <see cref="List{T}"/>, because <c language="csharp">ModuleDatabase</c> casts the stored
        /// value to one rather than to an interface.</remarks>
        public IList<DeploymentSession> Sessions { get; }

        /// <summary>
        /// Puts back whatever database was seated before.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            DatabaseField.SetValue(null, _previous);
            _disposed = true;
        }

        /// <summary>
        /// Whatever was seated before, which is almost always nothing.
        /// </summary>
        private readonly object? _previous;

        /// <summary>
        /// Whether the database has already been put back.
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// The field holding the module database.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the field is not where it was expected, which means
        /// the type changed rather than that the code under test is wrong.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S3011:Reflection should not be used to increase accessibility of classes, methods, or fields", Justification = "ModuleDatabase.Init refuses any caller outside the module, so setting the field is the only way to seat a database for a test.")]
        private static readonly FieldInfo DatabaseField = typeof(ModuleDatabase).GetField("_database", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("ModuleDatabase no longer holds its database in a private static field named _database.");
    }
}
