using System;
using System.Threading;
using System.Globalization;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Management.Automation.Runspaces;
using Microsoft.PowerShell;


namespace PSADT.PowerShellHost
{
    /// <summary>
    /// Represents the options for executing a PowerShell script.
    /// </summary>
    public class PSExecutionOptions
    {
        /// <summary>
        /// Gets or sets the path to the PowerShell script file.
        /// </summary>
        public string? ScriptPath { get; set; }

        /// <summary>
        /// Gets or sets the PowerShell script text to execute.
        /// </summary>
        public string? ScriptText { get; set; }

        /// <summary>
        /// Gets or sets the parameters to pass to the script.
        /// </summary>
        public IDictionary<string, object>? Parameters { get; set; }

        /// <summary>
        /// Gets or sets the PowerShell execution policy to use.
        /// </summary>
        public ExecutionPolicy ExecutionPolicy { get; set; } = ExecutionPolicy.RemoteSigned;

        /// <summary>
        /// Gets or sets the scope for the execution policy.
        /// </summary>
        public ExecutionPolicyScope ExecutionPolicyScope { get; set; } = ExecutionPolicyScope.Process;

        /// <summary>
        /// Gets or sets whether to force the execution policy change.
        /// </summary>
        public bool ForceExecutionPolicy { get; set; } = true;

        /// <summary>
        /// Gets or sets the error action for the execution policy change.
        /// </summary>
        public ActionPreference ExecutionPolicyErrorAction { get; set; } = ActionPreference.SilentlyContinue;

        /// <summary>
        /// Gets or sets the PowerShell version to use.
        /// </summary>
        public PSEdition PowerShellVersion { get; set; } = PSEdition.Default;

        /// <summary>
        /// Gets or sets the architecture of the PowerShell host for out-of-process runspaces.
        /// </summary>
        public PSArchitecture PSArchitecture { get; set; } = PSArchitecture.CurrentProcess;

        /// <summary>
        /// Gets or sets whether to create an out-of-process runspace.
        /// </summary>
        public bool? IsOutOfProcessRunspace { get; set; }

        /// <summary>
        /// Gets or sets the initial session state for the runspace.
        /// </summary>
        public InitialSessionState? InitialSessionState { get; set; }

        /// <summary>
        /// Gets or sets the apartment state for the PowerShell thread.
        /// </summary>
        public ApartmentState ApartmentState { get; set; } = ApartmentState.Unknown;

        /// <summary>
        /// Gets or sets the thread options for the PowerShell runspace.
        /// </summary>
        public PSThreadOptions ThreadOptions { get; set; } = PSThreadOptions.Default;

        /// <summary>
        /// Gets or sets the working directory for the script execution.
        /// </summary>
        public string? WorkingDirectory { get; set; }

        /// <summary>
        /// Gets or sets additional modules to load into the session.
        /// </summary>
        public IEnumerable<string>? ModulesToImport { get; set; }

        /// <summary>
        /// Gets or sets additional type assemblies to load into the session.
        /// </summary>
        public IEnumerable<string>? AssembliesToLoad { get; set; }

        /// <summary>
        /// Gets or sets variables to be defined in the PowerShell session.
        /// </summary>
        public IDictionary<string, object>? Variables { get; set; }

        /// <summary>
        /// Gets or sets functions to be defined in the PowerShell session.
        /// </summary>
        public IDictionary<string, string>? Functions { get; set; }

        /// <summary>
        /// Gets or sets the maximum execution time for the script.
        /// </summary>
        public TimeSpan? Timeout { get; set; }

        /// <summary>
        /// Gets or sets the culture info for the PowerShell session.
        /// </summary>
        public CultureInfo? Culture { get; set; }

        /// <summary>
        /// Gets or sets the UI culture info for the PowerShell session.
        /// </summary>
        public CultureInfo? UICulture { get; set; }

        /// <summary>
        /// Gets or sets whether to throw on any error.
        /// </summary>
        public ActionPreference ErrorActionPreference { get; set; } = ActionPreference.Continue;

        /// <summary>
        /// Gets or sets whether to collect and return streams (verbose, debug, etc.).
        /// </summary>
        public bool CollectStreams { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to run the script with elevated privileges (requires UAC prompt).
        /// </summary>
        public bool RunAsAdministrator { get; set; } = false;

        /// <summary>
        /// Gets or sets custom PSHost implementation for advanced output control.
        /// </summary>
        public PSHost? PSADTHost { get; set; }
    }
}
