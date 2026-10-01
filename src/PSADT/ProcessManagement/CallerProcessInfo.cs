using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.Win32.SafeHandles;
using PSADT.AccountManagement;
using PSADT.Interop;

namespace PSADT.ProcessManagement
{
    /// <summary>
    /// Facts about the current process that cannot change while it runs, each worked out once when first asked for.
    /// </summary>
    /// <remarks>Worked out lazily rather than in a static constructor, so a failure stays with the caller that asked.</remarks>
    internal static class CallerProcessInfo
    {
        /// <summary>
        /// Gets a value indicating whether the current process is running with ServiceUI anywhere as a parent process.
        /// </summary>
        internal static bool UsingServiceUI => UsingServiceUIValue.Value;

        /// <summary>
        /// Gets the architecture the current process runs as under WOW64, or <c language="csharp">IMAGE_FILE_MACHINE_UNKNOWN</c>
        /// if it is not running under WOW64.
        /// </summary>
        internal static Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE Wow64Machine => Wow64MachineValue.Value;

        /// <summary>
        /// Gets a value indicating whether the current process is running under WOW64.
        /// </summary>
        internal static bool IsWow64 => Wow64Machine is not Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_UNKNOWN;

        /// <summary>
        /// The value behind <see cref="UsingServiceUI"/>.
        /// </summary>
        private static readonly Lazy<bool> UsingServiceUIValue = new(static () =>
        {
            return AccountUtilities.CallerIsLocalSystem && ProcessUtilities.GetParentProcesses().Any(static p =>
            {
                if (ProcessUtilities.HasProcessExited(p))
                {
                    return false;
                }
                try
                {
                    return ProcessVersionInfo.GetVersionInfo(p).InternalName?.Equals("ServiceUI", StringComparison.OrdinalIgnoreCase) is true;
                }
                catch (NotSupportedException)
                {
                    try
                    {
                        return FileVersionInfo.GetVersionInfo(p.GetFilePath().FullName).InternalName?.Equals("ServiceUI", StringComparison.OrdinalIgnoreCase) is true;
                    }
                    catch
                    {
                        return false;
                        throw;
                    }
                }
                catch
                {
                    return false;
                    throw;
                }
            });
        });

        /// <summary>
        /// The value behind <see cref="Wow64Machine"/>.
        /// </summary>
        private static readonly Lazy<Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE> Wow64MachineValue = new(static () =>
        {
            using SafeProcessHandle currentProcess = NativeMethods.GetCurrentProcess();
            _ = NativeMethods.IsWow64Process2(currentProcess, out Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE processMachine, out _);
            return processMachine;
        });
    }
}
