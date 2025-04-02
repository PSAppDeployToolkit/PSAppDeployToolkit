using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using PSADT.Diagnostics;
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
            void* lpEnvironmentLocal;
            var res = PInvoke.CreateEnvironmentBlock(&lpEnvironmentLocal, hToken, bInherit);
            if (!res)
            {
                throw ErrorHandler.GetExceptionForLastWin32Error();
            }
            lpEnvironment = (IntPtr)lpEnvironmentLocal;
            return res;
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
                throw ErrorHandler.GetExceptionForLastWin32Error();
            }
            return res;
        }
    }
}
