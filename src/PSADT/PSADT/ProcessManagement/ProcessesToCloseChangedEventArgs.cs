using System;
using System.Collections.Generic;

namespace PSADT.ProcessManagement
{
    /// <summary>
    /// Represents the event arguments for the <see cref="RunningProcessService.ProcessesToCloseChanged"/> event.
    /// </summary>
    /// <param name="processesToClose">The list of processes that are currently marked for closure. This list is read-only and reflects the state at the time the event was raised.</param>
    internal sealed class ProcessesToCloseChangedEventArgs(IReadOnlyList<ProcessToClose> processesToClose) : EventArgs
    {
        /// <summary>
        /// Gets the list of running processes.
        /// </summary>
        internal readonly IReadOnlyList<ProcessToClose> ProcessesToClose = processesToClose;
    }
}
