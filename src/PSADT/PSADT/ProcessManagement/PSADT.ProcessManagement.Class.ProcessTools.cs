using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using PSADT.Extensions;
using PSADT.LibraryInterfaces;
using PSADT.SafeHandles;
using Windows.Wdk.System.Threading;
using Windows.Win32.Foundation;
using Windows.Win32.System.Threading;

namespace PSADT.ProcessManagement
{
    internal static class ProcessTools
    {
        /// <summary>
        /// Retrieves the command line arguments of a process given its process ID.
        /// </summary>
        /// <param name="processId"></param>
        /// <returns></returns>
        internal static unsafe string GetProcessCommandLine(int processId)
        {
            // Open the process's handle with the relevant access rights.
            using (var hProc = Kernel32.OpenProcess(PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_LIMITED_INFORMATION, false, (uint)processId))
            {
                // Get the required length we need for the buffer, then retrieve the actual command line string.
                NtDll.NtQueryInformationProcess(hProc, PROCESSINFOCLASS.ProcessCommandLineInformation, SafeMemoryHandle.Null, out var requiredLength);
                using (var buffer = SafeHGlobalHandle.Alloc((int)requiredLength))
                {
                    NtDll.NtQueryInformationProcess(hProc, PROCESSINFOCLASS.ProcessCommandLineInformation, buffer, out _);
                    return buffer.ToStructure<UNICODE_STRING>().Buffer.ToString().TrimRemoveNull();
                }
            }
        }

        /// <summary>
        /// Retrieves the image name of a process given its process ID.
        /// </summary>
        /// <param name="processId"></param>
        /// <returns></returns>
        internal static string GetProcessImageName(int processId, ReadOnlyDictionary<string, string>? ntPathLookupTable = null)
        {
            // Set up initial buffer that we need to query the process information.
            var processIdInfo = new NtDll.SYSTEM_PROCESS_ID_INFORMATION { ProcessId = (IntPtr)processId };
            var processIdInfoSize = Marshal.SizeOf<NtDll.SYSTEM_PROCESS_ID_INFORMATION>();
            using (var processIdInfoPtr = SafeHGlobalHandle.Alloc(processIdInfoSize).FromStructure(processIdInfo, false))
            {
                // Perform initial query so we can reallocate with the required length.
                NtDll.NtQuerySystemInformation(SYSTEM_INFORMATION_CLASS.SystemProcessIdInformation, processIdInfoPtr, out _);
                processIdInfo = processIdInfoPtr.ToStructure<NtDll.SYSTEM_PROCESS_ID_INFORMATION>();
                using (var imageNamePtr = SafeHGlobalHandle.Alloc(processIdInfo.ImageName.MaximumLength))
                {
                    // Assign the ImageName buffer and perform the query again.
                    processIdInfo.ImageName.Buffer = imageNamePtr.ToPWSTR();
                    NtDll.NtQuerySystemInformation(SYSTEM_INFORMATION_CLASS.SystemProcessIdInformation, processIdInfoPtr.FromStructure(processIdInfo, false), out _);
                    var imagePath = processIdInfoPtr.ToStructure<NtDll.SYSTEM_PROCESS_ID_INFORMATION>().ImageName.Buffer.ToString().TrimRemoveNull();

                    // If we have a lookup table, replace the NT path with the drive letter before returning.
                    if (ntPathLookupTable != null)
                    {
                        var ntDeviceName = $@"\{string.Join(@"\", imagePath.Split(['\\'], StringSplitOptions.RemoveEmptyEntries).Take(2))}";
                        if (!ntPathLookupTable.TryGetValue(ntDeviceName, out string? driveLetter))
                        {
                            throw new InvalidOperationException($"Unable to find drive letter for NT path: {ntDeviceName}.");
                        }
                        return imagePath.Replace(ntDeviceName, driveLetter);
                    }
                    return imagePath;
                }
            }
        }

        /// <summary>
        /// Checks if a process is running by its process ID.
        /// </summary>
        /// <param name="processId"></param>
        /// <returns></returns>
        internal static bool IsProcessRunning(int processId)
        {
            // Opens a handle to a process and tests whether it's exit code is still active or not.
            // If we fail to open the process because of invalid input, we assume it is not running.
            try
            {
                using (var hProc = Kernel32.OpenProcess(PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_LIMITED_INFORMATION | PROCESS_ACCESS_RIGHTS.PROCESS_SYNCHRONIZE, false, (uint)processId))
                {
                    Kernel32.GetExitCodeProcess(hProc, out var exitCode);
                    return exitCode == NTSTATUS.STILL_ACTIVE;
                }
            }
            catch (ArgumentException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return true;
            }
        }
    }
}
