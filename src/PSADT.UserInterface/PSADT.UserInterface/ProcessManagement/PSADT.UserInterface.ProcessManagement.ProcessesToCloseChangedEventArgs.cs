using System;
using System.Collections.Generic;
using System.Linq;

namespace PSADT.UserInterface.ProcessManagement
{
    /// <summary>
    /// Represents the event arguments for the <see cref="RunningProcessService.ProcessesToCloseListChanged"/> event.
    /// </summary>
    internal sealed class ProcessesToCloseChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessesToCloseChangedEventArgs"/> class with the specified list of running processes.
        /// </summary>
        /// <param name="processesToClose"></param>
        internal ProcessesToCloseChangedEventArgs(IEnumerable<ProcessToClose> processesToClose)
        {
            ProcessesToClose = processesToClose.ToList().AsReadOnly();
        }

        /// <summary>
        /// Gets the list of running processes.
        /// </summary>
        internal IReadOnlyList<ProcessToClose> ProcessesToClose { get; }
    }
}
