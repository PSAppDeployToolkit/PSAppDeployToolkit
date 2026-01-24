using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using PSADT.FileSystem;
using PSADT.ProcessManagement;

namespace PSADT.Extensions
{
    /// <summary>
    /// Provides extension methods for working with <see cref="Process"/> instances.
    /// </summary>
    internal static class ProcessExtensions
    {
        /// <summary>
        /// Retrieves the full file system path of the executable associated with the specified process.
        /// </summary>
        /// <remarks>This method attempts to retrieve the file path using the process's main module. If
        /// that fails, it falls back to an alternative mechanism that may use the provided NT path lookup table. The
        /// returned path may be empty if the process is inaccessible or the path cannot be resolved.</remarks>
        /// <param name="process">The process for which to obtain the executable file path. Must not be null.</param>
        /// <param name="ntPathLookupTable">An optional lookup table used to resolve NT device paths to file system paths. If null, a default lookup
        /// table is used.</param>
        /// <returns>A string containing the full file system path of the process's executable. Returns an empty string if the
        /// path cannot be determined.</returns>
        internal static string GetFilePath(this Process process, ReadOnlyDictionary<string, string>? ntPathLookupTable = null)
        {
            try
            {
                if (process.MainModule is not null)
                {
                    return process.MainModule.FileName;
                }
            }
            catch (Exception ex) when (ex.Message is not null)
            {
                return ProcessUtilities.GetProcessImageName(process, ntPathLookupTable ?? FileSystemUtilities.GetNtPathLookupTable());
            }
            return ProcessUtilities.GetProcessImageName(process, ntPathLookupTable ?? FileSystemUtilities.GetNtPathLookupTable());
        }
    }
}
