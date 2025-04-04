using System;
using System.ComponentModel;
using PSADT.Utilities;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace PSADT.LibraryInterfaces
{
    /// <summary>
    /// Public P/Invokes from the userenv.dll library.
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
            fixed (void* lpEnvironmentPtr = &lpEnvironment)
            {
                var res = PInvoke.CreateEnvironmentBlock(&lpEnvironmentPtr, hToken, bInherit);
                if (!res)
                {
                    throw ExceptionUtilities.GetExceptionForLastWin32Error();
                }
                return res;
            }
        }

        /// <summary>
        /// Destroys an environment block created by the CreateEnvironmentBlock function.
        /// </summary>
        /// <param name="lpEnvironment"></param>
        /// <returns></returns>
        /// <exception cref="Win32Exception"></exception>
        internal static unsafe BOOL DestroyEnvironmentBlock(IntPtr lpEnvironment)
        {
            if (IntPtr.Zero == lpEnvironment)
            {
                return true;
            }
            var res = PInvoke.DestroyEnvironmentBlock(lpEnvironment.ToPointer());
            if (!res)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            return res;
        }
    }
}
