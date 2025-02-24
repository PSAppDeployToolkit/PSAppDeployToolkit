using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Management.Automation;

namespace PSADT.Module
{
    public static class InternalDatabase
    {
        private const string errorMessage = "Please ensure that [Initialize-ADTModule] is called before using any PSAppDeployToolkit functions or methods.";
        private static PSObject? _database = null;
        private static SessionState? _sessionState = null;

        public static void Init(PSObject database)
        {
            if (!ScriptBlock.Create("Get-PSCallStack | & { process { if ($_.Command.Equals('PSAppDeployToolkit.psm1') -and $_.InvocationInfo.MyCommand.ScriptBlock.Module.Name.Equals('PSAppDeployToolkit')) { return $_ } } }").Invoke().Count.Equals(1))
            {
                throw new InvalidOperationException("The InternalDatabase class can only be initialized from within the PSAppDeployToolkit module.");
            }
            _database = database;
            _sessionState = (SessionState)_database!.Properties["SessionState"].Value;
        }

        internal static PSObject Get()
        {
            return _database ?? throw new InvalidOperationException("This assembly only supports loading via the PSAppDeployToolkit PowerShell module.");
        }

        internal static OrderedDictionary GetEnvironment()
        {
            return (OrderedDictionary?)_database?.Properties["Environment"].Value ?? throw new InvalidOperationException(errorMessage);
        }

        internal static Hashtable GetConfig()
        {
            return (Hashtable?)((PSObject?)_database?.Properties["Config"].Value)?.BaseObject ?? throw new InvalidOperationException(errorMessage);
        }

        internal static Hashtable GetStrings()
        {
            return (Hashtable?)((PSObject?)_database?.Properties["Strings"].Value)?.BaseObject ?? throw new InvalidOperationException(errorMessage);
        }

        internal static List<DeploymentSession> GetSessionList()
        {
            return (List<DeploymentSession>?)_database?.Properties["Sessions"].Value ?? throw new InvalidOperationException(errorMessage);
        }

        internal static SessionState GetSessionState()
        {
            return _sessionState ?? throw new InvalidOperationException("This assembly only supports loading via the PSAppDeployToolkit PowerShell module.");
        }

        internal static Collection<PSObject> InvokeScript(ScriptBlock scriptBlock, params object[]? args)
        {
            return _sessionState!.InvokeCommand.InvokeScript(_sessionState!, scriptBlock, args);
        }
    }
}
