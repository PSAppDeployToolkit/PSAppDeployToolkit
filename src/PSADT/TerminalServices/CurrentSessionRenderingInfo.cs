using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using PSADT.Interop;
using PSADT.Interop.SafeHandles;
using PSADT.Interop.Utilities;
using Windows.Win32;
using Windows.Win32.System.RemoteDesktop;

namespace PSADT.TerminalServices
{
    /// <summary>
    /// Reads remote-rendering evidence for the UI client's own Windows session.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0182:Avoid unused internal types", Justification = "This type is consumed across the PSADT.UserInterface.Interfaces friend assembly boundary in Task 3.")]
    internal static class CurrentSessionRenderingInfo
    {
        // Citrix Wfapi.h: WFClientName is the eleventh (zero-based 10) WF_INFO_CLASS member.
        private const int WFClientName = 10;
        private const uint WFCurrentSession = uint.MaxValue;

        [UnmanagedFunctionPointer(CallingConvention.Winapi, SetLastError = true)]
        internal delegate int WFQuerySessionInformation(nint server, uint sessionId, int infoClass, out nint buffer, out uint bytesReturned);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        internal delegate void WFFreeMemory(nint buffer);

        /// <summary>
        /// Queries Windows and, unless Windows confirms remoting, the optional Citrix session API.
        /// </summary>
        /// <param name="clientSessionId">The UI client process's session ID.</param>
        /// <returns>Nullable Windows and Citrix remote evidence; null denotes an unavailable or failed probe.</returns>
        internal static (bool? WindowsRemote, bool? CitrixRemote) Query(uint clientSessionId)
        {
            return Query(clientSessionId, ProbeWindows, ProbeCitrix);
        }

        // An injectable edge keeps orchestration tests independent of installed remote-session software.
        internal static (bool? WindowsRemote, bool? CitrixRemote) Query(uint clientSessionId, Func<uint, bool?> windowsProbe, Func<bool?> citrixProbe)
        {
            ArgumentNullException.ThrowIfNull(windowsProbe);
            ArgumentNullException.ThrowIfNull(citrixProbe);

            bool? windows = SafelyProbe(() => windowsProbe(clientSessionId), "WTSIsRemoteSession");
            return windows is true ? (true, null) : (windows, SafelyProbe(citrixProbe, "WFClientName"));
        }

        private static bool? ProbeWindows(uint clientSessionId)
        {
            _ = NativeMethods.WTSQuerySessionInformation(clientSessionId, WTS_INFO_CLASS.WTSIsRemoteSession, out SafeWtsHandle buffer);
            using (buffer)
            {
                return ReadRemoteFlag(buffer.DangerousGetHandle(), (uint)buffer.Length);
            }
        }

        private static bool? ProbeCitrix()
        {
            string systemDirectory = Environment.GetFolderPath(Environment.SpecialFolder.System);
            if (string.IsNullOrWhiteSpace(systemDirectory))
            {
                Trace.TraceWarning("WFClientName probe unavailable: Windows system directory could not be resolved.");
                return null;
            }
            string dllPath = Path.Join(systemDirectory, Environment.Is64BitProcess ? "Wfapi64.dll" : "Wfapi.dll");
            return ProbeCitrix(dllPath);
        }

        internal static bool? ProbeCitrix(string dllPath)
        {
            using FreeLibrarySafeHandle library = NativeMethods.LoadLibraryEx(dllPath);
            WFQuerySessionInformation query = Marshal.GetDelegateForFunctionPointer<WFQuerySessionInformation>(NativeMethods.GetProcAddress(library, "WFQuerySessionInformationW").Value);
            WFFreeMemory free = Marshal.GetDelegateForFunctionPointer<WFFreeMemory>(NativeMethods.GetProcAddress(library, "WFFreeMemory").Value);

            return QueryCitrix(query, free);
        }

        internal static bool? QueryCitrix(WFQuerySessionInformation query, WFFreeMemory free)
        {
            nint buffer = default;
            try
            {
                int succeeded = query(default, WFCurrentSession, WFClientName, out buffer, out uint bytesReturned);
                if (succeeded is 0)
                {
                    Trace.TraceWarning("WFClientName probe failed with Win32 error {0}.", ExceptionUtilities.GetLastWin32Error());
                    return null;
                }
                return ReadCitrixClientName(buffer, bytesReturned);
            }
            finally
            {
                if (buffer is not 0)
                {
                    free(buffer);
                }
            }
        }

        internal static bool? ReadRemoteFlag(nint buffer, uint bytesReturned)
        {
            if (buffer is 0 || bytesReturned is not sizeof(int))
            {
                Trace.TraceWarning("Remote session probe returned an invalid BOOL/ULONG buffer of {0} bytes.", bytesReturned);
                return null;
            }
            return Marshal.ReadInt32(buffer) is not 0;
        }

        internal static bool? ReadCitrixClientName(nint buffer, uint bytesReturned)
        {
            if (buffer is 0 || bytesReturned < 2 || bytesReturned > int.MaxValue || (bytesReturned & 1) is not 0)
            {
                Trace.TraceWarning("WFClientName probe returned an invalid Unicode buffer of {0} bytes.", bytesReturned);
                return null;
            }

            bool expectingLowSurrogate = false;
            for (int offset = 0; offset < (int)bytesReturned; offset += sizeof(char))
            {
                char codeUnit = (char)Marshal.ReadInt16(buffer, offset);
                if (codeUnit is '\0')
                {
                    return offset > 0 && !expectingLowSurrogate ? true : null;
                }
                if (expectingLowSurrogate)
                {
                    if (!char.IsLowSurrogate(codeUnit))
                    {
                        return null;
                    }
                    expectingLowSurrogate = false;
                }
                else if (char.IsHighSurrogate(codeUnit))
                {
                    expectingLowSurrogate = true;
                }
                else if (char.IsLowSurrogate(codeUnit))
                {
                    return null;
                }
            }

            Trace.TraceWarning("WFClientName probe returned an unterminated Unicode buffer.");
            return null;
        }

        private static bool? SafelyProbe(Func<bool?> probe, string name)
        {
            try
            {
                return probe();
            }
            catch (Exception exception) when (exception is Win32Exception or FileNotFoundException or InvalidOperationException or ArgumentException or DllNotFoundException or EntryPointNotFoundException or BadImageFormatException)
            {
                Trace.TraceWarning("{0} probe unavailable: {1}", name, exception);
                return null;
            }
        }
    }
}
