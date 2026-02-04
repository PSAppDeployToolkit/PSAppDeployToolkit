using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using PSADT.ProcessManagement;
using PSAppDeployToolkit.Logging;

namespace PSADT.UserInterface.DialogState
{
    /// <summary>
    /// Represents the state and associated services for managing the closure of specified applications.
    /// </summary>
    /// <remarks>This type is used internally to manage the lifecycle of processes that need to be closed. It
    /// provides functionality for tracking running processes and managing countdown operations related to process
    /// closure.</remarks>
    internal sealed record CloseAppsDialogState : BaseDialogState, IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CloseAppsDialogState"/> class with the specified processes to close.
        /// </summary>
        /// <param name="closeProcesses">An array of <see cref="ProcessDefinition"/> objects representing the processes to be managed for closure. If
        /// the array is null or empty, no processes will be managed.</param>
        /// <param name="logAction">An optional delegate for logging messages with severity. If null, logging is disabled.</param>
        internal CloseAppsDialogState(ReadOnlyCollection<ProcessDefinition>? closeProcesses, Action<string, LogSeverity, string> logAction)
        {
            // Only initialise these variables if they're not null.
            if (closeProcesses?.Count > 0)
            {
                RunningProcessService = new(closeProcesses);
            }
            LogAction = (message, severity) => logAction.Invoke(message, severity, "Show-ADTInstallationWelcome");
        }

        /// <summary>
        /// Represents a service that manages and provides information about running processes.
        /// </summary>
        /// <remarks>This service is used to interact with and retrieve details about processes currently
        /// running on the system. It may provide functionality such as querying process information or managing process
        /// lifecycles. This field is intended for internal use only.</remarks>
        internal readonly RunningProcessService? RunningProcessService;

        /// <summary>
        /// A stopwatch used to track the remaining time for a countdown operation.
        /// </summary>
        /// <remarks>This stopwatch is intended for internal use and is initialized when the containing
        /// type is created. It can be used to measure elapsed time for countdown-related functionality.</remarks>
        internal readonly Stopwatch CountdownStopwatch = new();

        /// <summary>
        /// Represents a delegate used for logging operations with severity.
        /// </summary>
        /// <remarks>This delegate is intended for internal use only.When invoked, it writes the 
        /// provided message with the specified severity to the configured logging destination.</remarks>
        internal readonly Action<string, LogSeverity> LogAction;

        /// <summary>
        /// Disposes of the resources used by the <see cref="CloseAppsDialogState"/> record.
        /// </summary>
        public void Dispose()
        {
            RunningProcessService?.Dispose();
        }
    }
}
