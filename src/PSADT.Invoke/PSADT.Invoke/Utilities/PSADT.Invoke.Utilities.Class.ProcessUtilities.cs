using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using PSADT.Invoke.LibraryInterfaces;

namespace PSADT.Invoke.Utilities
{
    internal static class ProcessUtilities
    {
        /// <summary>
        /// Gets the parent process of the current process.
        /// </summary>
        /// <returns>An instance of the Process class.</returns>
        internal static Process GetParentProcess()
        {
            return GetParentProcess(Process.GetCurrentProcess().Handle);
        }

        /// <summary>
        /// Gets the parent process of specified process.
        /// </summary>
        /// <param name="id">The process id.</param>
        /// <returns>An instance of the Process class.</returns>
        internal static Process GetParentProcess(int id)
        {
            return GetParentProcess(Process.GetProcessById(id).Handle);
        }

        /// <summary>
        /// Gets the parent process of a specified process.
        /// </summary>
        /// <param name="handle">The process handle.</param>
        /// <returns>An instance of the Process class.</returns>
        internal static Process GetParentProcess(IntPtr handle)
        {
            NtDll.NtQueryInformationProcess(handle, PROCESSINFOCLASS.ProcessBasicInformation, out var pbi);
            return Process.GetProcessById(pbi.InheritedFromUniqueProcessId.ToInt32());
        }

        /// <summary>
        /// Gets a list of all parent processes of this one.
        /// </summary>
        /// <returns>An list of instances of the Process class.</returns>
        internal static List<Process> GetParentProcesses()
        {
            List<Process> procs = [];
            var proc = Process.GetCurrentProcess();
            while (true)
            {
                try
                {
                    if (procs.Contains((proc = GetParentProcess(proc.Handle))))
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
