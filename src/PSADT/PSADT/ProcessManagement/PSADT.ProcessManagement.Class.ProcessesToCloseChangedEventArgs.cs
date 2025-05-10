using System;
using System.Collections.Generic;

namespace PSADT.ProcessManagement
{
    /// <summary>
    /// Represents the event arguments for the <see cref="RunningProcessService.ProcessesToCloseListChanged"/> event.
    /// </summary>
    internal sealed class ProcessesToCloseChangedEventArgs(IReadOnlyList<ProcessToClose> processesToClose) : EventArgs
    {
        /// <summary>
        /// Gets the list of running processes.
        /// </summary>
        internal readonly IReadOnlyList<ProcessToClose> ProcessesToClose = processesToClose;
    }
}
