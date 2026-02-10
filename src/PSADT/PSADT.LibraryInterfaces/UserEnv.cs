using System.ComponentModel;
using Microsoft.Win32.SafeHandles;
using PSADT.LibraryInterfaces.SafeHandles;
using PSADT.LibraryInterfaces.Utilities;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace PSADT.LibraryInterfaces
{
    /// <summary>
    /// CsWin32 P/Invoke wrappers for the userenv.dll library.
    /// </summary>
    internal static class UserEnv
    {
        /// <summary>
        /// Creates an environment block for a user.
        /// </summary>
        /// <param name="lpEnvironment"></param>
        /// <param name="hToken"></param>
        /// <param name="bInherit"></param>
        /// <returns></returns>
        /// <exception cref="Win32Exception"></exception>
        internal static BOOL CreateEnvironmentBlock(out SafeEnvironmentBlockHandle lpEnvironment, SafeFileHandle hToken, BOOL bInherit)
        {
            BOOL res;
            unsafe
            {
                res = PInvoke.CreateEnvironmentBlock(out void* lpEnvironmentPtr, hToken, bInherit);
                if (!res)
                {
                    throw ExceptionUtilities.GetExceptionForLastWin32Error();
                }
                lpEnvironment = new((nint)lpEnvironmentPtr, true);
            }
            return res;
        }
    }
}
