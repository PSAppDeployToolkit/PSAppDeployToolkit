using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
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
        internal static string GetProcessCommandLine(int processId)
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

        /// <summary>
        /// Converts a list of command-line arguments into a single command-line string.
        /// </summary>
        /// <remarks>This method handles quoting and escaping according to standard command-line parsing
        /// rules: - Arguments containing whitespace or quotes are enclosed in quotes. - Backslashes preceding a quote
        /// are doubled to ensure correct parsing. - A closing quote followed by another quote is treated as a literal
        /// quote.</remarks>
        /// <param name="argv">A read-only list of command-line arguments to be converted.</param>
        /// <returns>A command-line string that represents the concatenated arguments, with necessary quoting and escaping
        /// applied. Returns <see langword="null"/> if the resulting command-line string is empty or consists only of
        /// whitespace.</returns>
        internal static string? ArgvToCommandLine(IReadOnlyList<string> argv)
        {
            // Internal worker to test the argument for whitespace or quotes.
            const char Backslash = '\\'; const char Quote = '\"'; const char Space = ' ';
            static bool ContainsNoWhitespaceOrQuotes(string s)
            {
                for (int i = 0; i < s.Length; i++)
                {
                    char c = s[i];
                    if (char.IsWhiteSpace(c) || c == Quote)
                    {
                        return false;
                    }
                }
                return true;
            }

            // Build out the command line string.
            StringBuilder stringBuilder = new();
            foreach (string argument in argv.Select(static a => a.Trim()))
            {
                // Continue if the argument is null or empty.
                if (string.IsNullOrWhiteSpace(argument))
                {
                    continue;
                }

                // Quote the argument and escape and quotes/backslashes.
                if (!ContainsNoWhitespaceOrQuotes(argument))
                {
                    stringBuilder.Append(Quote); int idx = 0;
                    while (idx < argument.Length)
                    {
                        char c = argument[idx++];
                        if (c == Backslash)
                        {
                            int numBackSlash = 1;
                            while (idx < argument.Length && argument[idx] == Backslash)
                            {
                                idx++;
                                numBackSlash++;
                            }

                            if (idx == argument.Length)
                            {
                                // We'll emit an end quote after this so must double the number of backslashes.
                                stringBuilder.Append(Backslash, numBackSlash * 2);
                            }
                            else if (argument[idx] == Quote)
                            {
                                // Backslashes will be followed by a quote. Must double the number of backslashes.
                                stringBuilder.Append(Backslash, numBackSlash * 2 + 1);
                                stringBuilder.Append(Quote);
                                idx++;
                            }
                            else
                            {
                                // Backslash will not be followed by a quote, so emit as normal characters.
                                stringBuilder.Append(Backslash, numBackSlash);
                            }
                            continue;
                        }

                        if (c == Quote)
                        {
                            // Escape the quote so it appears as a literal. This also guarantees that we won't end up generating a closing quote followed
                            // by another quote (which parses differently pre-2008 vs. post-2008.)
                            stringBuilder.Append(Backslash);
                            stringBuilder.Append(Quote);
                            continue;
                        }
                        stringBuilder.Append(c);
                    }
                    stringBuilder.Append(Quote);
                }
                else
                {
                    // Argument can just be added.
                    stringBuilder.Append(argument);
                }
                stringBuilder.Append(Space);
            }

            // Return the built command line string.
            return stringBuilder.ToString().Trim() is string arguments && !string.IsNullOrWhiteSpace(arguments) ? arguments : null;
        }
    }
}
