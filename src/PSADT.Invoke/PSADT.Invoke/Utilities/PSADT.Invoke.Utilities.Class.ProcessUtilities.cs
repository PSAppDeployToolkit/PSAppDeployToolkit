using System;
using System.Collections.Generic;
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
        /// Gets the parent process of a specified process.
        /// </summary>
        /// <param name="handle">The process handle.</param>
        /// <returns>An instance of the Process class.</returns>
        internal static Process GetParentProcess(Process proc)
        {
            NtDll.NtQueryInformationProcess(proc.Handle, PROCESSINFOCLASS.ProcessBasicInformation, out var pbi);
            return Process.GetProcessById(pbi.InheritedFromUniqueProcessId.ToInt32());
        }

        /// <summary>
        /// Gets a list of all parent processes of this one.
        /// </summary>
        /// <returns>An list of instances of the Process class.</returns>
        internal static List<Process> GetParentProcesses()
        {
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
            return procs;
        }
    }
}
