using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management.Automation;
using PSAppDeployToolkit.Extensions;

namespace PSAppDeployToolkit.Foundation
{
    /// <summary>
    /// Represents the internal database for the PSAppDeployToolkit module, providing access to session state, configuration, strings, and deployment sessions.
    /// </summary>
    public sealed class ModuleDatabase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ModuleDatabase"/> class with the specified default configurations, default strings, manifest, module information, signature, assemblies, and compilation status.
        /// </summary>
        /// <param name="defaults">A read-only dictionary containing the default configuration and string values for the module.</param>
        /// <param name="manifest">The module manifest.</param>
        /// <param name="moduleInfo">The module information.</param>
        /// <param name="assemblies">The module assemblies.</param>
        /// <param name="signature">The module signature.</param>
        /// <param name="compiled">A value indicating whether the module has been compiled.</param>
        /// <exception cref="ArgumentNullException">Thrown when any of the required parameters are null or empty.</exception>
        public ModuleDatabase(IReadOnlyDictionary<string, IReadOnlyDictionary<string, ScriptBlock>> defaults, IDictionary manifest, PSModuleInfo moduleInfo, ReadOnlyCollection<FileInfo> assemblies, Signature signature, bool compiled)
        {
            if (!(manifest?.Count > 0))
            {
                throw new ArgumentNullException(nameof(manifest), "Manifest cannot be null or empty.");
            }
            if (!(assemblies?.Count > 0))
            {
                throw new ArgumentNullException(nameof(assemblies), "Assemblies cannot be null or empty.");
            }
            Defaults = new(defaults);
            Manifest = manifest;
            ModuleInfo = moduleInfo ?? throw new ArgumentNullException(nameof(moduleInfo), "ModuleInfo cannot be null.");
            Assemblies = assemblies;
            Signature = signature ?? throw new ArgumentNullException(nameof(signature), "Signature cannot be null.");
            Compiled = compiled;
        }

        /// <summary>
        /// Initializes the internal database with the specified PowerShell object. This method must be called from
        /// within the PSAppDeployToolkit module context.
        /// </summary>
        /// <param name="sessionState">The SessionState object representing the current PowerShell session. This parameter cannot be null.</param>
        /// <param name="database">The ModuleDatabase object representing the database to initialize. This parameter cannot be null.</param>
        /// <param name="importStartTime">The DateTime representing the start time of the import operation. This parameter is used to calculate the import duration.</param>
        /// <exception cref="InvalidOperationException">Thrown if the method is called from outside the PSAppDeployToolkit module context.</exception>
        /// <exception cref="ArgumentNullException">Thrown if the <paramref name="database"/> parameter is null.</exception>
        [SuppressMessage("Major Code Smell", "S6561:Avoid using \"DateTime.Now\" for benchmarking or timing operations", Justification = "This is OK, we don't need nanosecond precision..")]
        public static void Init(SessionState sessionState, ModuleDatabase database, DateTime importStartTime)
        {
            if (!ScriptBlock.Create("Get-PSCallStack | & { process { if ($_.Command.Equals('PSAppDeployToolkit.psm1') -and $_.InvocationInfo.MyCommand.ScriptBlock.Module.Name.Equals('PSAppDeployToolkit')) { return $_ } } }").Invoke().Count.Equals(1))
            {
                throw new InvalidOperationException("The ModuleDatabase class can only be initialized from within the PSAppDeployToolkit module.");
            }
            if (Module is not null)
            {
                throw new InvalidOperationException("The ModuleDatabase class has already been initialized.");
            }
            Module = (sessionState ?? throw new ArgumentNullException(nameof(sessionState)), database ?? throw new ArgumentNullException(nameof(database)));
            database.ImportDuration = DateTime.Now - importStartTime;
        }

        /// <summary>
        /// Clears the current database instance, resetting the internal state to uninitialized.
        /// </summary>
        /// <remarks>Call this method to release the current database and prepare for reinitialization.
        /// After calling this method, any operations that depend on the database instance may fail until it is
        /// reinitialized.</remarks>
        /// <exception cref="InvalidOperationException">Thrown if the method is called from outside the PSAppDeployToolkit module context.</exception>
        public static void Clear()
        {
            if (!ScriptBlock.Create("Get-PSCallStack | & { process { if ($_.ScriptName -and ($_.ScriptName.EndsWith('PSAppDeployToolkit\\PSAppDeployToolkit.psm1') -or $_.ScriptName.EndsWith('PSAppDeployToolkit\\ImportsLast.ps1'))) { return $_ } } }").Invoke().Count.Equals(1))
            {
                throw new InvalidOperationException("The ModuleDatabase class can only be cleared from within the PSAppDeployToolkit module.");
            }
            if (Module is null)
            {
                throw new InvalidOperationException("The ModuleDatabase class has not been initialized.");
            }
            Module = null;
        }

        /// <summary>
        /// Determines whether the database has been initialized.
        /// </summary>
        /// <remarks>This method checks the "State" property of the database to determine its state.
        /// Ensure the database object is properly configured before calling this method.</remarks>
        /// <returns><see langword="true"/> if the database is initialized; otherwise, <see langword="false"/>.</returns>
        public static bool IsInitialized()
        {
            return GetModuleData().State is not null;
        }

        /// <summary>
        /// Retrieves the module directories from the database.
        /// </summary>
        /// <returns>A <see cref="ModuleDirectories"/> instance containing the module directories.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the module directories cannot be retrieved because the database is uninitialized or the required properties are missing.</exception>
        public static ModuleDirectories GetDirectories()
        {
            return GetModuleState().Directories;
        }

        /// <summary>
        /// Retrieves the current environment settings from the database.
        /// </summary>
        /// <remarks>Ensure that the database is properly initialized before calling this method.
        /// Accessing this method when the database is not ready will result in an exception.</remarks>
        /// <returns>An <see cref="EnvironmentTable"/> instance containing the environment settings.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the environment settings cannot be retrieved because the database is uninitialized or the required
        /// properties are missing.</exception>
        public static EnvironmentTable GetEnvironment()
        {
            return GetModuleState().Environment;
        }

        /// <summary>
        /// Retrieves the current language settings from the database.
        /// </summary>
        /// <returns>A <see cref="CultureInfo"/> instance representing the current language settings.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the language settings cannot be retrieved because the database is uninitialized or the required properties are missing.</exception>
        public static CultureInfo GetLanguage()
        {
            return GetModuleState().Language;
        }

        /// <summary>
        /// Retrieves the configuration settings from the database as a dictionary.
        /// </summary>
        /// <remarks>This method accesses the 'Config' property of the database object. Ensure that the
        /// database is properly initialized before calling this method.</remarks>
        /// <returns>An IDictionary containing the configuration settings. Returns null if the configuration is not available.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the configuration cannot be retrieved due to an initialization error.</exception>
        public static IDictionary GetConfig()
        {
            return GetModuleState().Config;
        }

        /// <summary>
        /// Retrieves a specific configuration value from the database based on the provided section and key.
        /// </summary>
        /// <remarks>Use <see cref="TryGetConfigValue{T}(string, string, out T)"/> for a setting a configuration is
        /// allowed to omit. The <see langword="notnull"/> constraint here is what keeps the two apart, and it has to
        /// be a constraint rather than a convention: a nullable annotation on a type argument is gone by the time
        /// this runs, so without it a caller could ask for a nullable type and be handed a null that the compiler
        /// believed could not happen.
        /// <para>
        /// This is also the reader to use for a setting that is optional but must be of its own type when present,
        /// since the other one passes over a value of the wrong type rather than naming it.
        /// </para></remarks>
        /// <typeparam name="T">The type of the configuration value to retrieve.</typeparam>
        /// <param name="section">The section of the configuration from which to retrieve the value.</param>
        /// <param name="key">The key of the configuration value to retrieve.</param>
        /// <returns>The configuration value cast to the specified type.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the configuration section or key is not available or cannot be cast to the specified type.</exception>
        [SuppressMessage("Critical Code Smell", "S2302:\"nameof\" should be used", Justification = "This makes no sense here; the rule can't see these are string messages")]
        public static T GetConfigValue<T>(string section, string key) where T : notnull
        {
            return GetConfigSection(section)[key] is not T value
                ? throw new InvalidOperationException($"The configuration key '{key}' in section '{section}' is not available or cannot be cast to type '{typeof(T).Name}'.")
                : value;
        }

        /// <summary>
        /// Retrieves a specific configuration value that the configuration is permitted to omit.
        /// </summary>
        /// <remarks>A key that is absent, a key holding null and a key holding something of another type are all the
        /// same answer here, since none of them supplied a value of the type asked for. A caller that needs a value
        /// of another type reported rather than passed over should use <see cref="GetConfigValue{T}(string, string)"/>,
        /// which names it. The section is not optional either way: one that is missing means the configuration is
        /// malformed rather than that every setting in it was declined, so it is refused rather than reported.</remarks>
        /// <typeparam name="T">The type of the configuration value to retrieve.</typeparam>
        /// <param name="section">The section of the configuration from which to retrieve the value.</param>
        /// <param name="key">The key of the configuration value to retrieve.</param>
        /// <param name="value">The configuration value cast to the specified type, or the default of that type if the configuration supplied no such value.</param>
        /// <returns><see langword="true"/> if the configuration supplied a value of the specified type; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the configuration section is not available.</exception>
        public static bool TryGetConfigValue<T>(string section, string key, [NotNullWhen(true)] out T? value) where T : notnull
        {
            if (GetConfigSection(section)[key] is T typedValue)
            {
                value = typedValue;
                return true;
            }
            value = default;
            return false;
        }

        /// <summary>
        /// Retrieves a dictionary containing string values from the database properties.
        /// </summary>
        /// <remarks>This method accesses the 'Strings' property of the database, which must be properly
        /// initialized before calling this method. Ensure that the database connection is established to avoid
        /// exceptions.</remarks>
        /// <returns>An IDictionary containing the string values. Returns null if the database is not initialized or the property
        /// is not found.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the database is not initialized or the property 'Strings' is not available.</exception>
        public static IDictionary GetStringTable()
        {
            return GetModuleState().StringTable;
        }

        /// <summary>
        /// Determines whether there is at least one active deployment session.
        /// </summary>
        /// <remarks>This method checks the current state of deployment sessions stored in the database.
        /// If the database or session data is unavailable, the method will return <see langword="false"/>.</remarks>
        /// <returns><see langword="true"/> if there is at least one active deployment session; otherwise, <see
        /// langword="false"/>.</returns>
        public static bool IsDeploymentSessionActive()
        {
            return GetModuleData().State?.Sessions.Count > 0;
        }

        /// <summary>
        /// Retrieves the most recent deployment session from the database of active sessions.
        /// </summary>
        /// <remarks>Callers should ensure that the deployment session database is properly initialized
        /// and contains at least one session before calling this method. This method is intended for scenarios where
        /// session management is handled externally and a valid session is guaranteed to exist.</remarks>
        /// <returns>The last active deployment session. This represents the most recently opened session in the current database
        /// context.</returns>
        /// <exception cref="InvalidOperationException">Thrown if no deployment session is available. This typically indicates that the session has not been
        /// initialized; ensure that [Open-ADTSession] is called before invoking this method.</exception>
        public static DeploymentSession GetDeploymentSession()
        {
            return GetModuleState().Sessions.LastOrDefault() ?? throw new InvalidOperationException("Please ensure that [Open-ADTSession] is called before using any PSAppDeployToolkit functions.");
        }

        /// <summary>
        /// Retrieves the import duration of the module from the database state.
        /// </summary>
        /// <returns>The import duration of the module.</returns>
        internal static TimeSpan GetImportDuration()
        {
            return GetModuleData().ImportDuration;
        }

        /// <summary>
        /// Retrieves the initialization duration of the module from the database state.
        /// </summary>
        /// <returns>The initialization duration of the module.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the module's state is not available.</exception>
        internal static TimeSpan GetInitDuration()
        {
            return GetModuleState().InitDuration;
        }

        /// <summary>
        /// Retrieves the default configuration values from the module's database. This method accesses the 'Defaults' property of the database and returns the default configuration as a dictionary.
        /// </summary>
        /// <returns>An IDictionary containing the default configuration values for the module.</returns>
        internal static IDictionary GetDefaultConfig()
        {
            return GetModuleData().Defaults.GetDefaultConfig();
        }

        /// <summary>
        /// Retrieves the default string values from the module's database. This method accesses the 'Defaults' property of the database and returns the default strings as a dictionary.
        /// </summary>
        /// <param name="locale">An optional CultureInfo parameter representing the locale for which to retrieve the default string values. If null, the default locale is used.</param>
        /// <returns>An IDictionary containing the default string values for the module.</returns>
        internal static IDictionary GetDefaultStringTable(CultureInfo? locale = null)
        {
            return GetModuleData().Defaults.GetDefaultStringTable(locale);
        }

        /// <summary>
        /// Retrieves the last exit code from the module's state. This method accesses the 'LastExitCode' property of the module's state and returns the most recent exit code recorded during the module's execution.
        /// </summary>
        /// <returns>The last exit code of the module, or null if no exit code is available.</returns>
        internal static int? GetLastExitCode()
        {
            return GetModuleState().LastExitCode;
        }

        /// <summary>
        /// Sets the last exit code in the module's state. This method updates the LastExitCode property of the module's state with the provided exit code.
        /// </summary>
        /// <param name="exitCode">The exit code to set.</param>
        /// <exception cref="InvalidOperationException">Thrown if the module's state is not available.</exception>
        internal static void SetLastExitCode(int exitCode)
        {
            GetModuleState().LastExitCode = exitCode;
        }

        /// <summary>
        /// Gets the module's internal SessionState from the database.
        /// </summary>
        /// <returns>The current session state.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the session state is not available.</exception>
        internal static SessionState GetSessionState()
        {
            return (Module ?? throw new InvalidOperationException(_pwshErrorMessage)).SessionState;
        }

        /// <summary>
        /// Invokes the specified script block with the provided arguments in the current session state.
        /// </summary>
        /// <remarks>This method executes the script block in the context of the current session state,
        /// allowing access to session variables and commands.</remarks>
        /// <param name="scriptBlock">The script block to execute. This parameter must not be null.</param>
        /// <param name="args">An array of arguments to pass to the script block. This parameter can be null or empty if no arguments are
        /// required.</param>
        internal static void InvokeScript(ScriptBlock scriptBlock, params object[] args)
        {
            GetSessionState().InvokeScript(scriptBlock, args);
        }

        /// <summary>
        /// Invokes the specified script block with the provided arguments in the current session state.
        /// </summary>
        /// <remarks>This method executes the script block in the context of the current session state,
        /// allowing access to session variables and commands.</remarks>
        /// <typeparam name="T">The type of the objects returned by the script block execution.</typeparam>
        /// <param name="scriptBlock">The script block to execute. This parameter must not be null.</param>
        /// <param name="args">An array of arguments to pass to the script block. This parameter can be null or empty if no arguments are
        /// required.</param>
        /// <returns>A read-only collection of objects of type <typeparamref name="T"/> that represent the results of the script execution.</returns>
        internal static IEnumerable<T> InvokeScript<T>(ScriptBlock scriptBlock, params object[] args)
        {
            return GetSessionState().InvokeScript<T>(scriptBlock, args);
        }

        /// <summary>
        /// Retrieves the module's database instance. This method returns the ModuleDatabase object representing the current state of the module, including configuration, strings, and session information.
        /// </summary>
        /// <returns>The current ModuleDatabase object.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the module's database is not available.</exception>
        private static ModuleDatabase GetModuleData()
        {
            return (Module ?? throw new InvalidOperationException(_pwshErrorMessage)).Database;
        }

        /// <summary>
        /// Retrieves the module's current state from the database. This method returns the ModuleState object representing the current state of the module, including configuration, strings, and session information.
        /// </summary>
        /// <returns>The current ModuleState object.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the module's state is not available.</exception>
        private static ModuleState GetModuleState()
        {
            return GetModuleData().State ?? throw new InvalidOperationException(_initErrorMessage);
        }

        /// <summary>
        /// Retrieves the table holding one section of the configuration.
        /// </summary>
        /// <param name="section">The section of the configuration to retrieve.</param>
        /// <returns>An IDictionary containing that section's settings.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the configuration section is not available, or is not a table.</exception>
        [SuppressMessage("Critical Code Smell", "S2302:\"nameof\" should be used", Justification = "This makes no sense here; the rule can't see these are string messages")]
        private static IDictionary GetConfigSection(string section)
        {
            return GetConfig()[section] as IDictionary ?? throw new InvalidOperationException($"The configuration section '{section}' is not available.");
        }

        /// <summary>
        /// Gets the module's manifest information from the database. The manifest contains metadata about the module, such as its name, version, author, and other relevant details.
        /// </summary>
        public IDictionary Manifest { get; }

        /// <summary>
        /// Gets the module's information from the database. This property provides access to the PSModuleInfo object, which contains details about the module, including its name, version, and other relevant information.
        /// </summary>
        public PSModuleInfo ModuleInfo { get; }

        /// <summary>
        /// Gets the list of assemblies loaded by the module from the database. This property provides access to the collection of assembly names that have been loaded into the module's context, allowing for inspection and management of dependencies.
        /// </summary>
        public IReadOnlyList<FileInfo> Assemblies { get; }

        /// <summary>
        /// Gets the module's signature information from the database. The signature provides details about the digital signature of the module, including the signer, timestamp, and other relevant information. This property can be used to verify the authenticity and integrity of the module.
        /// </summary>
        public Signature Signature { get; }

        /// <summary>
        /// Gets a value indicating whether the module is signed from the database. This property returns true if the module's signature status is valid, and false otherwise. It can be used to determine the signing state of the module for security and trust purposes.
        /// </summary>
        public bool Signed => Signature.Status is SignatureStatus.Valid;

        /// <summary>
        /// Gets a value indicating whether the module has been compiled from the database. This property returns true if the module has been compiled, and false otherwise. It can be used to determine the compilation state of the module for conditional logic or debugging purposes.
        /// </summary>
        public bool Compiled { get; }

        /// <summary>
        /// Gets the module's default configuration and string values from the database.
        /// </summary>
        public ModuleDefaults Defaults { get; }

        /// <summary>
        /// Gets the module's callbacks for various events and actions from the database.
        /// </summary>
        public ModuleCallbacks Callbacks { get; } = new();

        /// <summary>
        /// Gets or sets the module's current state from the database.
        /// </summary>
        public ModuleState? State { get; set; }

        /// <summary>
        /// Gets the import duration for the module. This property returns the duration of the import operation.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the import duration has not been set.</exception>
        internal TimeSpan ImportDuration { get => field == default ? throw new InvalidOperationException(_pwshErrorMessage) : field; private set; }

        /// <summary>
        /// Gets or sets the module's internal session state and database instance. This field is used to store the current session state and database for the PSAppDeployToolkit module.
        /// </summary>
        private static (SessionState SessionState, ModuleDatabase Database)? Module;

        /// <summary>
        /// Represents the error message displayed when PSAppDeployToolkit functions or methods are used without prior initialization.
        /// </summary>
        private const string _initErrorMessage = "Please ensure that [Initialize-ADTModule] is called before using any PSAppDeployToolkit functions or methods.";

        /// <summary>
        /// Represents the error message displayed when a PowerShell-dependent method is called outside of the PSAppDeployToolkit module context.
        /// </summary>
        private const string _pwshErrorMessage = "This assembly only supports loading via the PSAppDeployToolkit PowerShell module.";
    }
}
