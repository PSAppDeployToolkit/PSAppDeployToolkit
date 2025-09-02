﻿using System;
using System.ComponentModel;
using Microsoft.Win32.SafeHandles;
using PSADT.SafeHandles;
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
        internal unsafe static BOOL CreateEnvironmentBlock(out SafeEnvironmentBlockHandle lpEnvironment, SafeFileHandle hToken, BOOL bInherit)
        {
            var res = PInvoke.CreateEnvironmentBlock(out var lpEnvironmentPtr, hToken, bInherit);
            if (!res)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            lpEnvironment = new((IntPtr)lpEnvironmentPtr, true);
            return res;
        }
    }
}
