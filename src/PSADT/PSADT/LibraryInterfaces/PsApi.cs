using System.Runtime.InteropServices;
using PSADT.SafeHandles;
using PSADT.Utilities;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.ProcessStatus;

namespace PSADT.LibraryInterfaces
{
    /// <summary>
    /// Provides methods for interacting with process modules in a Windows environment.
    /// </summary>
    /// <remarks>The <see cref="PsApi"/> class contains static methods that allow for the enumeration and
    /// retrieval of module information within a specified process. These methods require appropriate access rights to
    /// the process being queried.</remarks>
    internal static class PsApi
    {
        /// <summary>
        /// Enumerates the modules in the specified process.
        /// </summary>
        /// <param name="hProcess">A handle to the process whose modules are to be enumerated. This handle must have the
        /// PROCESS_QUERY_INFORMATION and PROCESS_VM_READ access rights.</param>
        /// <param name="lphModule">When this method returns, contains a handle to the module. This parameter is passed uninitialized.</param>
        /// <param name="cb">The size, in bytes, of the lphModule buffer.</param>
        /// <param name="lpcbNeeded">When this method returns, contains the number of bytes required to store all module handles in the lphModule
        /// buffer.</param>
        /// <returns><see langword="true"/> if the function succeeds; otherwise, <see langword="false"/>.</returns>
        internal unsafe static BOOL EnumProcessModules(SafeHandle hProcess, SafeMemoryHandle lphModule, out uint lpcbNeeded)
        {
            fixed (uint* pcbNeeded = &lpcbNeeded)
            {
                bool hProcessAddRef = false;
                bool lphModuleAddRef = false;
                try
                {
                    hProcess.DangerousAddRef(ref hProcessAddRef);
                    lphModule.DangerousAddRef(ref lphModuleAddRef);
                    var res = PInvoke.EnumProcessModules((HANDLE)hProcess.DangerousGetHandle(), (HMODULE*)lphModule.DangerousGetHandle().ToPointer(), (uint)lphModule.Length, pcbNeeded);
                    if (!res)
                    {
                        throw ExceptionUtilities.GetExceptionForLastWin32Error();
                    }
                    return res;
                }
                finally
                {
                    if (lphModuleAddRef)
                    {
                        lphModule.DangerousRelease();
                    }
                    if (hProcessAddRef)
                    {
                        hProcess.DangerousRelease();
                    }
                }
            }
        }

        /// <summary>
        /// Retrieves information about a specified module in the context of a given process.
        /// </summary>
        /// <param name="hProcess">A handle to the process that contains the module. This handle must have the PROCESS_QUERY_INFORMATION and
        /// PROCESS_VM_READ access rights.</param>
        /// <param name="hModule">A handle to the module whose information is to be retrieved.</param>
        /// <param name="lpmodinfo">When this method returns, contains a <see cref="MODULEINFO"/> structure that receives the module
        /// information.</param>
        /// <param name="cb">The size of the <see cref="MODULEINFO"/> structure, in bytes.</param>
        /// <returns><see langword="true"/> if the function succeeds; otherwise, <see langword="false"/>.</returns>
        internal unsafe static BOOL GetModuleInformation(SafeHandle hProcess, HMODULE hModule, out MODULEINFO lpmodinfo)
        {
            fixed (MODULEINFO* pModuleInfo = &lpmodinfo)
            {
                bool hProcessAddRef = false;
                try
                {
                    hProcess.DangerousAddRef(ref hProcessAddRef);
                    var res = PInvoke.GetModuleInformation((HANDLE)hProcess.DangerousGetHandle(), hModule, pModuleInfo, (uint)Marshal.SizeOf<MODULEINFO>());
                    if (!res)
                    {
                        throw ExceptionUtilities.GetExceptionForLastWin32Error();
                    }
                    return res;
                }
                finally
                {
                    if (hProcessAddRef)
                    {
                        hProcess.DangerousRelease();
                    }
                }
            }
        }
    }
}
