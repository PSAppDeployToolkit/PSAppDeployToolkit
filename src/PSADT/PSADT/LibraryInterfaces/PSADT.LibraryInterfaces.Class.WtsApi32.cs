using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.RemoteDesktop;

namespace PSADT.LibraryInterfaces
{
    /// <summary>
    /// Public P/Invokes from the wtsapi32.dll library.
    /// </summary>
    public static class WtsApi32
    {
        /// <summary>
        /// Wrapper around WTSEnumerateSessions to manage error handling.
        /// </summary>
        /// <param name="hServer"></param>
        /// <param name="Reserved"></param>
        /// <param name="Version"></param>
        /// <param name="ppSessionInfo"></param>
        /// <param name="pCount"></param>
        /// <returns></returns>
        /// <exception cref="Win32Exception"></exception>
        internal static unsafe ReadOnlyCollection<WTS_SESSION_INFOW> WTSEnumerateSessions(HANDLE hServer)
        {
            var res = PInvoke.WTSEnumerateSessions(hServer, 0, 1, out var ppSessionInfo, out var pCount);
            if (!res)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
            try
            {
                List<WTS_SESSION_INFOW> sessions = [];
                for (int i = 0; i < pCount; i++)
                {
                    sessions.Add(ppSessionInfo[i]);
                }
                return sessions.AsReadOnly();
            }
            finally
            {
                PInvoke.WTSFreeMemory(ppSessionInfo);
            }
        }

        /// <summary>
        /// Wrapper around WTSQuerySessionInformation to manage error handling.
        /// </summary>
        /// <param name="hServer"></param>
        /// <param name="SessionId"></param>
        /// <param name="WTSInfoClass"></param>
        /// <param name="ppBuffer"></param>
        /// <param name="pBytesReturned"></param>
        /// <returns></returns>
        /// <exception cref="Win32Exception"></exception>
        internal static unsafe BOOL WTSQuerySessionInformation(HANDLE hServer, uint SessionId, WTS_INFO_CLASS WTSInfoClass, out PWSTR ppBuffer, out uint pBytesReturned)
        {
            var res = PInvoke.WTSQuerySessionInformation(hServer, SessionId, WTSInfoClass, out ppBuffer, out pBytesReturned);
            if (!res)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
            return res;
        }
    }
}
