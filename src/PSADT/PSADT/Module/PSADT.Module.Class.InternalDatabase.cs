using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Management.Automation;

namespace PSADT.Module
{
    /// <summary>
    /// The internal database for the PSAppDeployToolkit module, as initialised via PSAppDeployToolkit.psm1.
    /// </summary>
    public static class InternalDatabase
    {
        private const string errorMessage = "Please ensure that [Initialize-ADTModule] is called before using any PSAppDeployToolkit functions or methods.";
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
        internal static PSObject Get()
        {
            return _database ?? throw new InvalidOperationException("This assembly only supports loading via the PSAppDeployToolkit PowerShell module.");
        }

        /// <summary>
        /// Gets the environment table from the internal database.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        internal static IReadOnlyDictionary<string, object> GetEnvironment()
        {
            return (IReadOnlyDictionary<string, object>?)_database?.Properties["Environment"].Value ?? throw new InvalidOperationException(errorMessage);
        }

        /// <summary>
        /// Gets the config table from the internal database.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        internal static Hashtable GetConfig()
        {
            return (Hashtable?)((PSObject?)_database?.Properties["Config"].Value)?.BaseObject ?? throw new InvalidOperationException(errorMessage);
        }

        /// <summary>
        /// Gets the active string table from the internal database.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        internal static Hashtable GetStrings()
        {
            return (Hashtable?)((PSObject?)_database?.Properties["Strings"].Value)?.BaseObject ?? throw new InvalidOperationException(errorMessage);
        }

        /// <summary>
        /// Gets the active sessions from the internal database.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        internal static List<DeploymentSession> GetSessionList()
        {
            return (List<DeploymentSession>?)_database?.Properties["Sessions"].Value ?? throw new InvalidOperationException(errorMessage);
        }

        /// <summary>
        /// Gets the module's internal SessionState from the database.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        internal static SessionState GetSessionState()
        {
            return _sessionState ?? throw new InvalidOperationException("This assembly only supports loading via the PSAppDeployToolkit PowerShell module.");
        }

        /// <summary>
        /// Utility method to invoke a scriptblock using the module's internal SessionState.
        /// </summary>
        /// <param name="scriptBlock"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        internal static Collection<PSObject> InvokeScript(ScriptBlock scriptBlock, params object[]? args)
        {
            return _sessionState!.InvokeCommand.InvokeScript(_sessionState!, scriptBlock, args);
        }
    }
}
