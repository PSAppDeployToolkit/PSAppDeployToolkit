using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using PSADT.ProcessManagement;

/// <summary>
/// Provides extension methods for working with <see cref="Process"/> instances.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Roslynator", "RCS1110:Declare type inside namespace", Justification = "Polyfills aren't meant to be part of a namespace.")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0047:Declare types in namespaces", Justification = "Polyfills aren't meant to be part of a namespace.")]
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
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="process"/> is null.</exception>
    internal static FileInfo GetFilePath(this Process process, ReadOnlyDictionary<string, string>? ntPathLookupTable = null)
    {
        ArgumentNullException.ThrowIfNull(process);
        try
        {
            return ProcessUtilities.GetProcessImageName(process.Id, ntPathLookupTable);
        }
        catch
        {
            if (process.MainModule is not null)
            {
                return new(process.MainModule.FileName);
            }
            throw;
        }
    }
}
