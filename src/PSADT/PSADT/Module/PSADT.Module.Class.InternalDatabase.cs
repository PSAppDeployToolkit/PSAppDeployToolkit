using System;
using System.Collections;
using System.Collections.Specialized;
using System.Management.Automation;

namespace PSADT.Module
{
    public static class InternalDatabase
    {
        private static PSObject? _database = null;

        public static void Init(PSObject database)
        {
            if (!ScriptBlock.Create("$((Get-PSCallStack).Where({$_.Command.Equals('PSAppDeployToolkit.psm1') -and $_.InvocationInfo.MyCommand.ScriptBlock.Module.Name.Equals('PSAppDeployToolkit')}))").Invoke().Count.Equals(1))
            {
                throw new InvalidOperationException("The InternalDatabase class can only be initialized from within the PSAppDeployToolkit module.");
            }
            _database = database;
        }

        internal static PSObject Get()
        {
            return _database!;
        }

        internal static OrderedDictionary GetEnvironment()
        {
            return (OrderedDictionary)_database!.Properties["Environment"].Value;
        }

        internal static Hashtable GetConfig()
        {
            return (Hashtable)((PSObject)_database!.Properties["Config"].Value).BaseObject;
        }

        internal static Hashtable GetStrings()
        {
            return (Hashtable)((PSObject)_database!.Properties["Strings"].Value).BaseObject;
        }

        internal static SessionState GetSessionState()
        {
            return (SessionState)_database!.Properties["SessionState"].Value;
        }
    }
}
