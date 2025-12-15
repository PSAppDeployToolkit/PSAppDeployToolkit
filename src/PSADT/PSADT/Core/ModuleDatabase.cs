using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

namespace PSADT.Core
{
    /// <summary>
    /// The internal database for the PSAppDeployToolkit module, as initialised via PSAppDeployToolkit.psm1.
    /// </summary>
    public static class ModuleDatabase
    {
        /// <summary>
        /// Initialises the internal database with the database from PSAppDeployToolkit.psm1.
        /// </summary>
        /// <param name="database"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public static void Init(PSObject database)
        {
            if (database is null)
            {
                throw new ArgumentNullException(nameof(database), "Database cannot be null.");
            }
            if (!ScriptBlock.Create("Get-PSCallStack | & { process { if ($_.Command.Equals('PSAppDeployToolkit.psm1') -and $_.InvocationInfo.MyCommand.ScriptBlock.Module.Name.Equals('PSAppDeployToolkit')) { return $_ } } }").Invoke().Count.Equals(1))
            {
                throw new InvalidOperationException("The InternalDatabase class can only be initialized from within the PSAppDeployToolkit module.");
            }
            _database = database; _sessionState = (SessionState)_database.Properties["SessionState"].Value;
        }

        /// <summary>
        /// Gets the internal database.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
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
            return (bool)_database?.Properties["Initialized"].Value!;
        }

        /// <summary>
        /// Gets the environment table from the internal database.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1024:Use properties where appropriate", Justification = "I like methods.")]
        public static IReadOnlyDictionary<string, object> GetEnvironment()
        {
            return (IReadOnlyDictionary<string, object>)_database?.Properties["Environment"].Value! ?? throw new InvalidOperationException(initErrorMessage);
        }

        /// <summary>
        /// Gets the config table from the internal database.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1024:Use properties where appropriate", Justification = "I like methods.")]
        public static Hashtable GetConfig()
        {
            return (Hashtable)((PSObject?)_database?.Properties["Config"].Value)?.BaseObject! ?? throw new InvalidOperationException(initErrorMessage);
        }

        /// <summary>
        /// Gets the active string table from the internal database.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1024:Use properties where appropriate", Justification = "I like methods.")]
        public static Hashtable GetStrings()
        {
            return (Hashtable)((PSObject?)_database?.Properties["Strings"].Value)?.BaseObject! ?? throw new InvalidOperationException(initErrorMessage);
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
            return ((List<DeploymentSession>)_database?.Properties["Sessions"].Value!).Count > 0;
        }

        /// <summary>
        /// Gets the active deployment session from the internal database.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static DeploymentSession GetDeploymentSession()
        {
            List<DeploymentSession> sessionList = (List<DeploymentSession>)_database?.Properties["Sessions"].Value!;
            return sessionList.Count == 0
                ? throw new InvalidOperationException("Please ensure that [Open-ADTSession] is called before using any PSAppDeployToolkit functions.")
                : sessionList.Last();
        }

        /// <summary>
        /// Gets the module's internal SessionState from the database.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        internal static SessionState GetSessionState()
        {
            return _sessionState ?? throw new InvalidOperationException(pwshErrorMessage);
        }

        /// <summary>
        /// Retrieves the default PowerShell runspace associated with the current context.
        /// </summary>
        /// <returns>The default <see cref="Runspace"/> instance.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the default runspace is not initialized. This typically occurs if the assembly is not loaded via the PSAppDeployToolkit PowerShell module.</exception>
        internal static Runspace GetRunspace()
        {
            return _defaultRunspace ?? throw new InvalidOperationException(pwshErrorMessage);
        }

        /// <summary>
        /// Utility method to invoke a scriptblock using the module's internal SessionState.
        /// </summary>
        /// <param name="scriptBlock"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        internal static ReadOnlyCollection<PSObject> InvokeScript(ScriptBlock scriptBlock, params object[]? args)
        {
            return new(_sessionState!.InvokeCommand.InvokeScript(_sessionState!, scriptBlock, args));
        }

        /// <summary>
        /// Represents the PSAppDeployToolkit module's internal database.
        /// </summary>
        private static PSObject? _database;

        /// <summary>
        /// Represents the PSAppDeployToolkit module's SessionState object.
        /// </summary>
        private static SessionState? _sessionState;

        /// <summary>
        /// Represents the default runspace for executing PowerShell commands.
        /// </summary>
        /// <remarks>This field is initialized with the default runspace provided by the PowerShell
        /// environment. If the default runspace is not available, an <see cref="InvalidOperationException"/> is thrown.
        /// This field is intended for use within the context of the PSAppDeployToolkit PowerShell module.</remarks>
        private static readonly Runspace _defaultRunspace = Runspace.DefaultRunspace ?? throw new InvalidOperationException(pwshErrorMessage);

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
