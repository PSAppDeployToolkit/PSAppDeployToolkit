using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using PSADT.UserInterface.LibraryInterfaces;
using Windows.Wdk.System.Threading;
using Windows.Win32.Foundation;
using Windows.Win32.System.Threading;

namespace PSADT.UserInterface.Utilities
{
    internal static class ProcessUtilities
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
                NtDll.NtQueryInformationProcess(hProc, PROCESSINFOCLASS.ProcessCommandLineInformation, null, 0, out var requiredLength);
                IntPtr buffer = Marshal.AllocHGlobal((int)requiredLength);
                try
                {
                    NtDll.NtQueryInformationProcess(hProc, PROCESSINFOCLASS.ProcessCommandLineInformation, buffer.ToPointer(), requiredLength, out _);
                    return Marshal.PtrToStructure<UNICODE_STRING>(buffer).Buffer.ToString().Replace("\0", string.Empty).Trim();
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
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
            // Set up initial buffer that we need to query the process information and required length.
            var processIdInfo = new NtDll.SYSTEM_PROCESS_ID_INFORMATION { ProcessId = (IntPtr)processId };
            var processIdInfoSize = Marshal.SizeOf<NtDll.SYSTEM_PROCESS_ID_INFORMATION>();
            var processIdInfoPtr = Marshal.AllocHGlobal(processIdInfoSize);
            Marshal.StructureToPtr(processIdInfo, processIdInfoPtr, false);
            ushort stringLength;
            try
            {
                NtDll.NtQuerySystemInformation(SYSTEM_INFORMATION_CLASS.SystemProcessIdInformation, processIdInfoPtr, (uint)processIdInfoSize, out _);
                stringLength = Marshal.PtrToStructure<NtDll.SYSTEM_PROCESS_ID_INFORMATION>(processIdInfoPtr).ImageName.MaximumLength;
            }
            finally
            {
                Marshal.FreeHGlobal(processIdInfoPtr);
            }

            // Redo the call now that we have the correct information.
            processIdInfoPtr = Marshal.AllocHGlobal(processIdInfoSize);
            var imageNamePtr = Marshal.AllocHGlobal(stringLength);
            processIdInfo = new NtDll.SYSTEM_PROCESS_ID_INFORMATION
            {
                ProcessId = (IntPtr)processId,
                ImageName = new UNICODE_STRING
                {
                    MaximumLength = stringLength,
                }
            };
            unsafe { processIdInfo.ImageName.Buffer = (PWSTR)imageNamePtr.ToPointer(); }
            Marshal.StructureToPtr(processIdInfo, processIdInfoPtr, false);
            try
            {
                // Fill our buffer and extract out the image name.
                NtDll.NtQuerySystemInformation(SYSTEM_INFORMATION_CLASS.SystemProcessIdInformation, processIdInfoPtr, (uint)processIdInfoSize, out _);
                var imagePath = Marshal.PtrToStructure<NtDll.SYSTEM_PROCESS_ID_INFORMATION>(processIdInfoPtr).ImageName.Buffer.ToString().Replace("\0", string.Empty).Trim();

                // If we have a lookup table, replace the NT path with the drive letter.
                if (ntPathLookupTable != null)
                {
                    var ntDeviceName = $"\\{string.Join("\\", imagePath.Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries).Take(2))}";
                    if (!ntPathLookupTable.TryGetValue(ntDeviceName, out string? driveLetter))
                    {
                        throw new InvalidOperationException($"Unable to find drive letter for NT path: {ntDeviceName}.");
                    }
                    return imagePath.Replace(ntDeviceName, driveLetter);
                }
                return imagePath;
            }
            finally
            {
                Marshal.FreeHGlobal(processIdInfoPtr);
                Marshal.FreeHGlobal(imageNamePtr);
            }
        }
    }
}
