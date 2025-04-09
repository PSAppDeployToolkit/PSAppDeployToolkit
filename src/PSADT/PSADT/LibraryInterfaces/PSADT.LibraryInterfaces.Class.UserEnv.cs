using System;
using System.ComponentModel;
using PSADT.Utilities;
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
        internal static unsafe BOOL CreateEnvironmentBlock(out IntPtr lpEnvironment, HANDLE hToken, BOOL bInherit)
        {
            void* lpEnvironmentPtr;
            var res = PInvoke.CreateEnvironmentBlock(&lpEnvironmentPtr, hToken, bInherit);
            if (!res)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            lpEnvironment = new IntPtr(lpEnvironmentPtr);
            return res;
        }

        /// <summary>
        /// Destroys an environment block created by the CreateEnvironmentBlock function.
        /// </summary>
        /// <param name="lpEnvironment"></param>
        /// <returns></returns>
        /// <exception cref="Win32Exception"></exception>
        internal static unsafe BOOL DestroyEnvironmentBlock(ref IntPtr lpEnvironment)
        {
            if (lpEnvironment == default || IntPtr.Zero == lpEnvironment)
            {
                return true;
            }
            var res = PInvoke.DestroyEnvironmentBlock(lpEnvironment.ToPointer());
            if (!res)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            lpEnvironment = default;
            return res;
        }
    }
}
