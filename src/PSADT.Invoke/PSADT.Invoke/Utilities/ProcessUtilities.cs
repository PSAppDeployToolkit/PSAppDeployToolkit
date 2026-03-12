using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Windows.Win32.System.Threading;

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
        /// <returns>A read-only collection of <see cref="Process"/> objects representing the parent processes
        /// of the current process. The collection is empty if no parent processes can be determined.</returns>
        internal static ReadOnlyCollection<Process> GetParentProcesses()
        {
            // Internal method to get the parent process of a given process.
            static Process? GetParentProcess(Process proc)
            {
                _ = NativeMethods.NtQueryInformationProcess(proc.SafeHandle, out PROCESS_BASIC_INFORMATION pbi);
                try
                {
                    return Process.GetProcessById((int)pbi.InheritedFromUniqueProcessId);
                }
                catch
                {
                    return null;
                    throw;
                }
            }

            // Build a list of parent processes and return it to the caller.
            Process process = Process.GetCurrentProcess();
            List<Process> processes = [];
            while (true)
            {
                try
                {
                    if (GetParentProcess(process) is not Process current)
                    {
                        break;
                    }
                    if (processes.Contains(process = current))
                    {
                        break;
                    }
                    processes.Add(process);
                }
                catch
                {
                    break;
                    throw;
                }
            }
            return processes.AsReadOnly();
        }
    }
}
