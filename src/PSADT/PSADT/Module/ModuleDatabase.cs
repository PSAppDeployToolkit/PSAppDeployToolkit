using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

namespace PSADT.Module
{
    /// <summary>
    /// The internal database for the PSAppDeployToolkit module, as initialised via PSAppDeployToolkit.psm1.
    /// </summary>
    public static class ModuleDatabase
    {
        private const string errorMessage = "Please ensure that [Initialize-ADTModule] is called before using any PSAppDeployToolkit functions or methods.";
        private static Runspace _defaultRunspace = Runspace.DefaultRunspace ?? throw new InvalidOperationException("The default runspace is not available. This assembly only supports loading via the PSAppDeployToolkit PowerShell module.");
        private static PSObject? _database = null;
        private static SessionState? _sessionState = null;

        /// <summary>
        /// Initialises the internal database with the database from PSAppDeployToolkit.psm1.
        /// </summary>
        /// <param name="database"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public static void Init(PSObject database)
        {
            if (!ScriptBlock.Create("Get-PSCallStack | & { process { if ($_.Command.Equals('PSAppDeployToolkit.psm1') -and $_.InvocationInfo.MyCommand.ScriptBlock.Module.Name.Equals('PSAppDeployToolkit')) { return $_ } } }").Invoke().Count.Equals(1))
            {
                throw new InvalidOperationException("The InternalDatabase class can only be initialized from within the PSAppDeployToolkit module.");
            }
            _database = database;
            _sessionState = (SessionState)_database!.Properties["SessionState"].Value;
        }

        /// <summary>
        /// Gets the internal database.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        internal static PSObject Get() => _database ?? throw new InvalidOperationException("This assembly only supports loading via the PSAppDeployToolkit PowerShell module.");

        /// <summary>
        /// Determines whether the database has been initialized.
        /// </summary>
        /// <remarks>This method checks the "Initialized" property of the database to determine its state.
        /// Ensure the database object is properly configured before calling this method.</remarks>
        /// <returns><see langword="true"/> if the database is initialized; otherwise, <see langword="false"/>.</returns>
        public static bool IsInitialized() => (bool)_database?.Properties["Initialized"].Value!;

        /// <summary>
        /// Gets the environment table from the internal database.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static IReadOnlyDictionary<string, object> GetEnvironment() => (IReadOnlyDictionary<string, object>)_database?.Properties["Environment"].Value! ?? throw new InvalidOperationException(errorMessage);

        /// <summary>
        /// Gets the config table from the internal database.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static Hashtable GetConfig() => (Hashtable)((PSObject?)_database?.Properties["Config"].Value)?.BaseObject! ?? throw new InvalidOperationException(errorMessage);

        /// <summary>
        /// Gets the active string table from the internal database.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static Hashtable GetStrings() => (Hashtable)((PSObject?)_database?.Properties["Strings"].Value)?.BaseObject! ?? throw new InvalidOperationException(errorMessage);

        /// <summary>
        /// Determines whether there is at least one active deployment session.
        /// </summary>
        /// <remarks>This method checks the current state of deployment sessions stored in the database.
        /// If the database or session data is unavailable, the method will return <see langword="false"/>.</remarks>
        /// <returns><see langword="true"/> if there is at least one active deployment session; otherwise, <see
        /// langword="false"/>.</returns>
        public static bool IsDeploymentSessionActive() => ((List<DeploymentSession>)_database?.Properties["Sessions"].Value!).Count > 0;

        /// <summary>
        /// Gets the active deployment session from the internal database.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static DeploymentSession GetDeploymentSession()
        {
            var sessionList = (List<DeploymentSession>)_database?.Properties["Sessions"].Value! ?? throw new InvalidOperationException(errorMessage);
            if (sessionList.Count == 0)
            {
                throw new InvalidOperationException("Please ensure that [Open-ADTSession] is called before using any PSAppDeployToolkit functions.");
            }
            return sessionList.Last();
        }

        /// <summary>
        /// Gets the module's internal SessionState from the database.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        internal static SessionState GetSessionState() => _sessionState ?? throw new InvalidOperationException("This assembly only supports loading via the PSAppDeployToolkit PowerShell module.");

        /// <summary>
        /// Retrieves the default PowerShell runspace associated with the current context.
        /// </summary>
        /// <returns>The default <see cref="Runspace"/> instance.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the default runspace is not initialized. This typically occurs if the assembly is not loaded via the PSAppDeployToolkit PowerShell module.</exception>
        internal static Runspace GetRunspace() => _defaultRunspace ?? throw new InvalidOperationException("This assembly only supports loading via the PSAppDeployToolkit PowerShell module.");

        /// <summary>
        /// Utility method to invoke a scriptblock using the module's internal SessionState.
        /// </summary>
        /// <param name="scriptBlock"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        internal static Collection<PSObject> InvokeScript(ScriptBlock scriptBlock, params object[]? args) => _sessionState!.InvokeCommand.InvokeScript(_sessionState!, scriptBlock, args);
    }
}
