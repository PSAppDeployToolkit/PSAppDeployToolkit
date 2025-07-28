using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using PSADT.Invoke.LibraryInterfaces;

namespace PSADT.Invoke.Utilities
{
    /// <summary>
    /// Utility class for working with Process objects.
    /// </summary>
    internal static class ProcessUtilities
    {
        /// <summary>
        /// Gets a list of all parent processes of this one.
        /// </summary>
        /// <returns>An list of instances of the Process class.</returns>
        internal static ReadOnlyCollection<Process> GetParentProcesses()
        {
            // Internal method to get the parent process of a given process.
            static Process GetParentProcess(Process proc)
            {
                NtDll.NtQueryInformationProcess(proc.Handle, out NtDll.PROCESS_BASIC_INFORMATION pbi);
                return Process.GetProcessById(pbi.InheritedFromUniqueProcessId.ToInt32());
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
