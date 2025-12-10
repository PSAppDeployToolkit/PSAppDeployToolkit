using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using PSADT.Invoke.LibraryInterfaces;

namespace PSADT.Invoke.Utilities
{
    /// <summary>
    /// Provides utility methods for working with operating system processes.
    /// </summary>
    internal static class ProcessUtilities
    {
        /// <summary>
        /// Retrieves a read-only collection containing the parent processes of the current process, ordered from
        /// immediate parent up the process hierarchy.
        /// </summary>
        /// <remarks>The returned collection does not include the current process itself. The order of the
        /// collection starts with the immediate parent and proceeds up the process tree. If a parent process cannot be
        /// accessed or does not exist, the collection may be truncated.</remarks>
        /// <returns>A read-only collection of <see cref="System.Diagnostics.Process"/> objects representing the parent processes
        /// of the current process. The collection is empty if no parent processes can be determined.</returns>
        internal static ReadOnlyCollection<Process> GetParentProcesses()
        {
            // Internal method to get the parent process of a given process.
            static Process GetParentProcess(Process proc)
            {
                NtDll.NtQueryInformationProcess(proc.SafeHandle, out var pbi);
                return Process.GetProcessById((int)pbi.InheritedFromUniqueProcessId);
            }

            // Build a list of parent processes and return it to the caller.
            var proc = Process.GetCurrentProcess();
            List<Process> procs = [];
            while (true)
            {
                try
                {
                    if (procs.Contains(proc = GetParentProcess(proc)))
                    {
                        break;
                    }
                    procs.Add(proc);
                }
                catch
                {
                    break;
                }
            }
            return procs.AsReadOnly();
        }
    }
}
