using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
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
    /// <remarks>This method asks <see cref="ProcessUtilities.GetProcessImageName(int, ReadOnlyDictionary{string, string})"/>
    /// first, which translates the native image path through the provided NT path lookup table, and falls back to the
    /// process's main module only if every method that tries fails.</remarks>
    /// <param name="process">The process for which to obtain the executable file path. Must not be null.</param>
    /// <param name="ntPathLookupTable">An optional lookup table used to resolve NT device paths to file system paths. If null, a default lookup
    /// table is used.</param>
    /// <returns>The executable file the process is running.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="process"/> is null.</exception>
    /// <exception cref="AggregateException">Thrown if the path cannot be determined. It carries the failure of each method tried.</exception>
    internal static FileInfo GetFilePath(this Process process, ReadOnlyDictionary<string, string>? ntPathLookupTable = null)
    {
        ArgumentNullException.ThrowIfNull(process);
        try
        {
            return ProcessUtilities.GetProcessImageName(process.Id, ntPathLookupTable);
        }
        catch (AggregateException ex1)
        {
            try
            {
                if (process.MainModule is ProcessModule mainModule)
                {
                    return new(mainModule.FileName);
                }
            }
            catch (Exception ex2)
            {
                throw new AggregateException(ex1.Message, ex1.InnerExceptions.Append(ex2));
            }
            ExceptionDispatchInfo.Capture(ex1).Throw();
            throw;
        }
    }
}
