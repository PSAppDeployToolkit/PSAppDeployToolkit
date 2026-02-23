using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;

namespace PSAppDeployToolkit.Foundation
{
    /// <summary>
    /// The internal database for the PSAppDeployToolkit module, as initialised via PSAppDeployToolkit.psm1.
    /// </summary>
    public static class ModuleDatabase
    {
        /// <summary>
        /// Initializes the internal database with the specified PowerShell object. This method must be called from
        /// within the PSAppDeployToolkit module context.
        /// </summary>
        /// <param name="database">The PowerShell object representing the database to initialize. This parameter cannot be null.</param>
        /// <exception cref="InvalidOperationException">Thrown if the method is called from outside the PSAppDeployToolkit module context.</exception>
        /// <exception cref="ArgumentNullException">Thrown if the <paramref name="database"/> parameter is null.</exception>
        public static void Init(PSObject database)
        {
            if (!ScriptBlock.Create("Get-PSCallStack | & { process { if ($_.Command.Equals('PSAppDeployToolkit.psm1') -and $_.InvocationInfo.MyCommand.ScriptBlock.Module.Name.Equals('PSAppDeployToolkit')) { return $_ } } }").Invoke().Count.Equals(1))
            {
                throw new InvalidOperationException("The InternalDatabase class can only be initialized from within the PSAppDeployToolkit module.");
            }
            _database = database ?? throw new ArgumentNullException(nameof(database), "Database cannot be null.");
        }

        /// <summary>
        /// Clears the current database instance, resetting the internal state to uninitialized.
        /// </summary>
        /// <remarks>Call this method to release the current database and prepare for reinitialization.
        /// After calling this method, any operations that depend on the database instance may fail until it is
        /// reinitialized.</remarks>
        public static void Clear()
        {
            if (!ScriptBlock.Create("Get-PSCallStack | & { process { if ($_.ScriptName -and ($_.ScriptName.EndsWith('PSAppDeployToolkit\\PSAppDeployToolkit.psm1') -or $_.ScriptName.EndsWith('PSAppDeployToolkit\\ImportsLast.ps1'))) { return $_ } } }").Invoke().Count.Equals(1))
            {
                throw new InvalidOperationException("The InternalDatabase class can only be cleared from within the PSAppDeployToolkit module.");
            }
            _database = null;
        }

        /// <summary>
        /// Retrieves the current PSObject instance representing the state of the database.
        /// </summary>
        /// <remarks>Callers should ensure that the database is properly initialized before invoking this
        /// method to avoid exceptions.</remarks>
        /// <returns>A PSObject that encapsulates the current state or configuration of the database.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the database has not been initialized.</exception>
        internal static PSObject Get()
        {
            return _database ?? throw new InvalidOperationException(pwshErrorMessage);
        }

        /// <summary>
        /// Determines whether the database has been initialized.
        /// </summary>
        /// <remarks>This method checks the "Initialized" property of the database to determine its state.
        /// Ensure the database object is properly configured before calling this method.</remarks>
        /// <returns><see langword="true"/> if the database is initialized; otherwise, <see langword="false"/>.</returns>
        public static bool IsInitialized()
        {
            return (bool?)_database?.Properties["Initialized"].Value == true;
        }

        /// <summary>
        /// Retrieves the environment properties stored in the database.
        /// </summary>
        /// <remarks>Ensure that the database is properly initialized before calling this method. The
        /// returned dictionary provides a read-only view of the environment settings as stored in the
        /// database.</remarks>
        /// <returns>An <see cref="IReadOnlyDictionary{TKey, TValue}"/> containing the environment properties, where each key is
        /// a property name and each value is the corresponding property value.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the environment properties cannot be retrieved because the database is not initialized.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1024:Use properties where appropriate", Justification = "I like methods.")]
        public static IReadOnlyDictionary<string, object> GetEnvironment()
        {
            return (IReadOnlyDictionary<string, object>?)_database?.Properties["Environment"].Value ?? throw new InvalidOperationException(initErrorMessage);
        }

        /// <summary>
        /// Retrieves the configuration settings from the database as a dictionary.
        /// </summary>
        /// <remarks>This method accesses the 'Config' property of the database object. Ensure that the
        /// database is properly initialized before calling this method.</remarks>
        /// <returns>An IDictionary containing the configuration settings. Returns null if the configuration is not available.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the configuration cannot be retrieved due to an initialization error.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1024:Use properties where appropriate", Justification = "I like methods.")]
        public static IDictionary GetConfig()
        {
            return (IDictionary?)((PSObject?)_database?.Properties["Config"].Value)?.BaseObject ?? throw new InvalidOperationException(initErrorMessage);
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1024:Use properties where appropriate", Justification = "I like methods.")]
        public static IDictionary GetStrings()
        {
            return (IDictionary?)((PSObject?)_database?.Properties["Strings"].Value)?.BaseObject ?? throw new InvalidOperationException(initErrorMessage);
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
            return ((List<DeploymentSession>?)_database?.Properties["Sessions"].Value)?.Count > 0;
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1024:Use properties where appropriate", Justification = "I like methods.")]
        public static DeploymentSession GetDeploymentSession()
        {
            return !(_database?.Properties["Sessions"].Value is List<DeploymentSession> sessionList && sessionList.Count > 0)
                ? throw new InvalidOperationException("Please ensure that [Open-ADTSession] is called before using any PSAppDeployToolkit functions.")
                : sessionList[sessionList.Count - 1];
        }

        /// <summary>
        /// Gets the module's internal SessionState from the database.
        /// </summary>
        /// <returns>The current session state.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the session state is not available.</exception>
        internal static SessionState GetSessionState()
        {
            return (SessionState?)_database?.Properties["SessionState"].Value ?? throw new InvalidOperationException(pwshErrorMessage);
        }

        /// <summary>
        /// Invokes the specified script block with the provided arguments in the current session state.
        /// </summary>
        /// <remarks>This method executes the script block in the context of the current session state,
        /// allowing access to session variables and commands.</remarks>
        /// <param name="scriptBlock">The script block to execute. This parameter must not be null.</param>
        /// <param name="args">An array of arguments to pass to the script block. This parameter can be null or empty if no arguments are
        /// required.</param>
        /// <returns>A read-only collection of PSObject instances that represent the results of the script execution.</returns>
        internal static ReadOnlyCollection<PSObject> InvokeScript(ScriptBlock scriptBlock, params object[]? args)
        {
            SessionState sessionState = GetSessionState(); return new(sessionState.InvokeCommand.InvokeScript(sessionState, scriptBlock, args));
        }

        /// <summary>
        /// Represents the PSAppDeployToolkit module's internal database.
        /// </summary>
        private static PSObject? _database;

        /// <summary>
        /// Represents the error message displayed when PSAppDeployToolkit functions or methods are used without prior initialization.
        /// </summary>
        private const string initErrorMessage = "Please ensure that [Initialize-ADTModule] is called before using any PSAppDeployToolkit functions or methods.";

        /// <summary>
        /// Represents the error message displayed when a PowerShell-dependent method is called outside of the PSAppDeployToolkit module context.
        /// </summary>
        private const string pwshErrorMessage = "This assembly only supports loading via the PSAppDeployToolkit PowerShell module.";
    }
}
