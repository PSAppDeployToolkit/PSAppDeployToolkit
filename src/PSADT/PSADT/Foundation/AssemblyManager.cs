using System;
using System.Diagnostics;
using System.IO;
using PSADT.Extensions;

namespace PSADT.Foundation
{
    /// <summary>
    /// Provides utility methods and fields for managing assembly-related file system operations, including permission
    /// remediation and assembly path discovery.
    /// </summary>
    /// <remarks>This class is intended for internal use to facilitate file system permission checks and
    /// access to assembly locations required by the application. It is not intended to be used directly by external
    /// consumers.</remarks>
    internal static class AssemblyManager
    {
        /// <summary>
        /// Initializes static members of the AssemblyManager class by determining the file path of the calling process.
        /// </summary>
        /// <remarks>This static constructor retrieves the executable path of the current process and
        /// assigns it to the CallingProcessPath property. This ensures that the path is available for use by other
        /// static members of the class.</remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1810:Initialize reference type static fields inline", Justification = "Needs to be in a static initialiser for process disposal management.")]
        static AssemblyManager()
        {
            using Process currentProcess = Process.GetCurrentProcess();
            CallingProcessPath = currentProcess.GetFilePath();
        }

        /// <summary>
        /// Gets the file path of the process that initiated the current execution context.
        /// </summary>
        internal static readonly FileInfo CallingProcessPath;

        /// <summary>
        /// Gets the directory path of this assembly.
        /// </summary>
        /// <remarks>This field can be used to locate files or resources relative to the assembly's
        /// location at runtime. The returned path is determined by the assembly's current location, which may vary
        /// depending on how the application is deployed or executed.</remarks>
        internal static readonly DirectoryInfo AssemblyDirectory = new(Path.GetDirectoryName(typeof(AssemblyManager).Assembly.Location) ?? throw new InvalidProgramException("Failed to retrieve directory for this assembly."));
    }
}
