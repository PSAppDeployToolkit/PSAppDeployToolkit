using System;
using System.Linq;
using PSADT.PInvoke;
using PSADT.ConsoleEx;
using PSADT.ProcessEx;
using PSADT.CommandLine;
using System.Threading.Tasks;
using PSADT.Logging;

using System.Net;
using System.Collections;
using System.ComponentModel;
using System.DirectoryServices;
using System.Security.Principal;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;

namespace PSADT
{
    /// <summary>
    /// The main entry point class for the PSADT application.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        /// <param name="args">Command line arguments passed to the application.</param>
        /// <returns>A task representing the asynchronous operation, with the exit code as the result.</returns>
        public static async Task<int> Main(string[] args)
        {
            try
            {
#if !CoreCLR
                // Make sure the .NET Framework version supports .NET Standard 2.0
                NETStandardSupport.CheckNetFxVersion();
#endif

                ConsoleHelper.IsHelpMode = args.Any(arg => new[] { "-Help", "--Help", "-?", "--?" }.Contains(arg, StringComparer.OrdinalIgnoreCase));
                if (ConsoleHelper.IsHelpMode)
                {
                    NativeMethods.AllocConsole();
                    DisplayHelp();
                    return 0;
                }

                ConsoleHelper.IsDebugMode = args.Any(arg => new[] { "-Debug", "--Debug", "-b", "--b" }.Contains(arg, StringComparer.OrdinalIgnoreCase));
                if (ConsoleHelper.IsDebugMode)
                {
                    // In debug mode, we AttachConsole instead of AllocConsole so we can more easily see and capture log output.
                    NativeMethods.AttachConsole(NativeMethods.ATTACH_PARENT_PROCESS);
                    UnifiedLogger.Create().Message("Debug mode enabled. Attached to parent process console.");
                }

                var options = Arguments.Parse<LaunchOptions>(args);
                UnifiedLogger.Create().Message("Command-line options were successfully parsed.");

                UnifiedLogger.Create().Message($"Initial command-line FilePath property [{options.FilePath}].").Severity(LogLevel.Debug);
                UnifiedLogger.Create().Message($"Initial command-line ArgumentList property [{String.Join(", ", options.ArgumentList)}].").Severity(LogLevel.Debug);

                var launcher = new StartProcess();
                return await launcher.ExecuteAndMonitorAsync(options);
            }
            catch (Exception ex)
            {
                UnifiedLogger.Create().Message("An error occurred during execution.").Error(ex);
                return 1;
            }
            finally
            {
                if (ConsoleHelper.IsDebugMode || ConsoleHelper.IsHelpMode)
                {
                    await ConsoleHelper.ReadLineWithTimeout(TimeSpan.FromSeconds(30));
                    NativeMethods.FreeConsole();
                }
            }
        }

        /// <summary>
        /// Displays the help information for the application.
        /// </summary>
        private static void DisplayHelp()
        {
            Console.WriteLine("PsadtExec - Launch processes in specific Windows sessions");
            Console.WriteLine("");
            Console.WriteLine("Usage: PsadtExec.exe [options]");
            Console.WriteLine("");
            Console.WriteLine("Options:");
            Console.WriteLine("  -f  , --FilePath <path>                 Path to the executable or script to run");
            Console.WriteLine("  -l  , --ArgumentList <args>             Arguments to pass to the executable or script");
            Console.WriteLine("  -dir, --WorkingDirectory <path>         Set the working directory for the process");
            Console.WriteLine("  -h  , --HideWindow                      Hide the process window");
            Console.WriteLine("  -pus, --PrimaryActiveUserSession        Run the process in all active user sessions");
            Console.WriteLine("  -sid, --SessionId <id>                  Specific session ID to run the process in");
            Console.WriteLine("  -aus, --AllActiveUserSessions           Run the process in all active user sessions");
            Console.WriteLine("  -adm, --UseLinkedAdminToken             Use the linked admin token when available");
            Console.WriteLine("  -pxp, --PsExecutionPolicy <policy>      Set PowerShell execution policy (default: RemoteSigned)");
            Console.WriteLine("  -bxp, --BypassPsExecutionPolicy         Bypass PowerShell execution policy");
            Console.WriteLine("  -ext, --SuccessExitCodes <codes>        List of exit codes considered successful (default: 0,3010)");
            Console.WriteLine("  -con, --ConsoleTimeoutInSeconds <secs>  Timeout for console operations (default: 30 seconds)");
            Console.WriteLine("  -red, --RedirectOutput                  Redirect process output (default: true)");
            Console.WriteLine("  -out, --OutputDirectory <path>          Directory to save redirected output");
            Console.WriteLine("  -mrg, --MergeStdErrAndStdOut            Merge stderr into stdout");
            Console.WriteLine("  -trm, --TerminateOnTimeout              Terminate process on timeout (default: true)");
            Console.WriteLine("  -iev, --InheritEnvironmentVariables     Inherit environment variables from the parent process");
            Console.WriteLine("  -e  , --Env <KEY=VALUE>                 Add an environment variable to the process");
            Console.WriteLine("  -w  , --Wait                            Wait for the process to exit before continuing");
            Console.WriteLine("  -wt , --WaitType <option>               Options for waiting on processes (WaitForAny/WaitForAll, default: WaitForAny)");
            Console.WriteLine("  -v  , --Verbose                         Enable verbose output");
            Console.WriteLine("  -d  , --Debug                           Run in debug mode");
            Console.WriteLine("  -?  , --Help                            Display this help message");
            Console.WriteLine("");
            Console.WriteLine("Process Creation Flags:");
            foreach (var mapping in Arguments._creationFlagMappings)
            {
                Console.WriteLine($"  --{mapping.LongName} (or -{mapping.ShortName})");
            }
            Console.WriteLine("");
            Console.WriteLine("Examples:");
            Console.WriteLine("  PsadtExec.exe -f notepad.exe -sid 1");
            Console.WriteLine("  PsadtExec.exe -f powershell.exe -l \"-File C:\\Scripts\\MyScript.ps1\" -aus");
            Console.WriteLine("  PsadtExec.exe -f cmd.exe -l \"/c echo Hello World > C:\\output.txt\" -w");
            Console.WriteLine("  PsadtExec.exe -f myapp.exe --Env MY_VAR=MY_VALUE --Env ANOTHER_VAR=ANOTHER_VALUE");
            Console.WriteLine("  PsadtExec.exe -f myapp.exe -adm --WaitOptions WaitForAll");
            Console.WriteLine("  PsadtExec.exe -f myapp.exe --newconsole");
        }
    }

#if !CoreCLR
    internal static class NETStandardSupport
    {
        private const int Net462Version = 394802;
        /// <summary>
        /// Checks to see if the .NET Framework version supports .NET Standard 2.0
        /// </summary>
        public static void CheckNetFxVersion()
        {
            UnifiedLogger.Create().Message("Checking that .NET Framework version is at least 4.6.2.").Severity(LogLevel.Debug);
            using Microsoft.Win32.RegistryKey? key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Net Framework Setup\NDP\v4\Full");
            object? netFxValue = key?.GetValue("Release");
            if (netFxValue == null || netFxValue is not int netFxVersion)
            {
                return;
            }

            UnifiedLogger.Create().Message($".NET Framework version is {netFxVersion}.").Severity(LogLevel.Debug);

            if (netFxVersion < Net462Version)
            {
                UnifiedLogger.Create().Message($".NET Framework version {netFxVersion} lower than .NET 4.6.2. This runtime is not supported and you may experience errors. Please update your .NET runtime version.").Severity(LogLevel.Warning);
            }
        }
    }
#endif

    public static class QueryUser
    {
        [DllImport("wtsapi32.dll", CharSet = CharSet.Auto, SetLastError = false)]
        public static extern IntPtr WTSOpenServer(string pServerName);

        [DllImport("wtsapi32.dll", CharSet = CharSet.Auto, SetLastError = false)]
        public static extern void WTSCloseServer(IntPtr hServer);

        [DllImport("wtsapi32.dll", CharSet = CharSet.Ansi, SetLastError = false)]
        public static extern bool WTSQuerySessionInformation(IntPtr hServer, int sessionId, WTS_INFO_CLASS wtsInfoClass, out IntPtr pBuffer, out int pBytesReturned);

        [DllImport("wtsapi32.dll", CharSet = CharSet.Ansi, SetLastError = false)]
        public static extern int WTSEnumerateSessions(IntPtr hServer, int Reserved, int Version, out IntPtr pSessionInfo, out int pCount);

        [DllImport("wtsapi32.dll", CharSet = CharSet.Auto, SetLastError = false)]
        public static extern void WTSFreeMemory(IntPtr pMemory);

        [DllImport("winsta.dll", CharSet = CharSet.Auto, SetLastError = false)]
        public static extern int WinStationQueryInformation(IntPtr hServer, int sessionId, int information, ref WINSTATIONINFORMATIONW pBuffer, int bufferLength, ref int returnedLength);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = false)]
        public static extern int GetCurrentProcessId();

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = false)]
        public static extern bool ProcessIdToSessionId(int processId, ref int pSessionId);

        public class TerminalSessionData
        {
            public int SessionId;
            public string ConnectionState;
            public string SessionName;
            public bool IsUserSession;
            public TerminalSessionData(int sessionId, string connState, string sessionName, bool isUserSession)
            {
                SessionId = sessionId;
                ConnectionState = connState;
                SessionName = sessionName;
                IsUserSession = isUserSession;
            }
        }

        public class TerminalSessionInfo
        {
            public string? NTAccount;
            public string? SID;
            public string? UserName;
            public string? DomainName;
            public int SessionId;
            public string? SessionName;
            public string? ConnectState;
            public bool IsCurrentSession;
            public bool IsConsoleSession;
            public bool IsActiveUserSession;
            public bool IsUserSession;
            public bool IsRdpSession;
            public bool IsLocalAdmin;
            public DateTime? LogonTime;
            public TimeSpan? IdleTime;
            public DateTime? DisconnectTime;
            public string? ClientName;
            public string? ClientProtocolType;
            public string? ClientDirectory;
            public int ClientBuildNumber;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WTS_SESSION_INFO
        {
            public Int32 SessionId;
            [MarshalAs(UnmanagedType.LPStr)]
            public string SessionName;
            public WTS_CONNECTSTATE_CLASS State;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct WINSTATIONINFORMATIONW
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 70)]
            private byte[] Reserved1;
            public int SessionId;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            private byte[] Reserved2;
            public FILETIME ConnectTime;
            public FILETIME DisconnectTime;
            public FILETIME LastInputTime;
            public FILETIME LoginTime;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1096)]
            private byte[] Reserved3;
            public FILETIME CurrentTime;
        }

        public enum WINSTATIONINFOCLASS
        {
            WinStationInformation = 8
        }

        public enum WTS_CONNECTSTATE_CLASS
        {
            Active,
            Connected,
            ConnectQuery,
            Shadow,
            Disconnected,
            Idle,
            Listen,
            Reset,
            Down,
            Init
        }

        public enum WTS_INFO_CLASS
        {
            SessionId = 4,
            UserName,
            SessionName,
            DomainName,
            ConnectState,
            ClientBuildNumber,
            ClientName,
            ClientDirectory,
            ClientProtocolType = 16
        }

        private static IntPtr OpenServer(string Name)
        {
            IntPtr server = WTSOpenServer(Name);
            return server;
        }

        private static void CloseServer(IntPtr ServerHandle)
        {
            WTSCloseServer(ServerHandle);
        }

        private static IList<T> PtrToStructureList<T>(IntPtr ppList, int count) where T : struct
        {
            List<T> result = new List<T>();
            long pointer = ppList.ToInt64();
            int sizeOf = Marshal.SizeOf(typeof(T));

            for (int index = 0; index < count; index++)
            {
                T item = (T)Marshal.PtrToStructure(new IntPtr(pointer), typeof(T))!;
                result.Add(item);
                pointer += sizeOf;
            }
            return result;
        }

        public static DateTime? FileTimeToDateTime(FILETIME ft)
        {
            if (ft.dwHighDateTime == 0 && ft.dwLowDateTime == 0)
            {
                return null;
            }
            long hFT = (((long)ft.dwHighDateTime) << 32) + ft.dwLowDateTime;
            return DateTime.FromFileTime(hFT);
        }

        public static WINSTATIONINFORMATIONW GetWinStationInformation(IntPtr server, int sessionId)
        {
            int retLen = 0;
            WINSTATIONINFORMATIONW wsInfo = new WINSTATIONINFORMATIONW();
            WinStationQueryInformation(server, sessionId, (int)WINSTATIONINFOCLASS.WinStationInformation, ref wsInfo, Marshal.SizeOf(typeof(WINSTATIONINFORMATIONW)), ref retLen);
            return wsInfo;
        }

        public static TerminalSessionData[] ListSessions(string ServerName)
        {
            IntPtr server = IntPtr.Zero;
            if (ServerName == "localhost" || ServerName == String.Empty)
            {
                ServerName = Environment.MachineName;
            }

            List<TerminalSessionData> results = new List<TerminalSessionData>();

            try
            {
                server = OpenServer(ServerName);
                IntPtr ppSessionInfo = IntPtr.Zero;
                int count;
                bool _isUserSession = false;
                IList<WTS_SESSION_INFO> sessionsInfo;

                if (WTSEnumerateSessions(server, 0, 1, out ppSessionInfo, out count) == 0)
                {
                    throw new Win32Exception();
                }

                try
                {
                    sessionsInfo = PtrToStructureList<WTS_SESSION_INFO>(ppSessionInfo, count);
                }
                finally
                {
                    WTSFreeMemory(ppSessionInfo);
                }

                foreach (WTS_SESSION_INFO sessionInfo in sessionsInfo)
                {
                    if (sessionInfo.SessionName != "Services" && sessionInfo.SessionName != "RDP-Tcp")
                    {
                        _isUserSession = true;
                    }
                    results.Add(new TerminalSessionData(sessionInfo.SessionId, sessionInfo.State.ToString(), sessionInfo.SessionName, _isUserSession));
                    _isUserSession = false;
                }
            }
            finally
            {
                CloseServer(server);
            }

            TerminalSessionData[] returnData = results.ToArray();
            return returnData;
        }

        public static TerminalSessionInfo GetSessionInfo(string ServerName, int SessionId)
        {
            IntPtr server = IntPtr.Zero;
            IntPtr buffer = IntPtr.Zero;
            int bytesReturned;
            TerminalSessionInfo data = new TerminalSessionInfo();
            bool _IsCurrentSessionId = false;
            bool _IsConsoleSession = false;
            bool _IsUserSession = false;
            int currentSessionID = 0;
            string _NTAccount = String.Empty;
            if (ServerName.Length.Equals(0) || ServerName == "localhost")
            {
                ServerName = Dns.GetHostName();
            }
            if (ProcessIdToSessionId(GetCurrentProcessId(), ref currentSessionID) == false)
            {
                currentSessionID = -1;
            }

            // Get all members of the local administrators group
            bool _IsLocalAdminCheckSuccess = false;
            List<string> localAdminGroupSidsList = new List<string>();
            try
            {
                DirectoryEntry localMachine = new DirectoryEntry("WinNT://" + ServerName + ",Computer");
                string localAdminGroupName = new SecurityIdentifier("S-1-5-32-544").Translate(typeof(NTAccount)).Value.Split('\\')[1];
                DirectoryEntry admGroup = localMachine.Children.Find(localAdminGroupName, "group");
                object members = admGroup.Invoke("members", null)!;
                string validSidPattern = @"^S-\d-\d+-(\d+-){1,14}\d+$";
                foreach (object groupMember in (IEnumerable)members)
                {
                    DirectoryEntry member = new DirectoryEntry(groupMember);
                    if (member.Name != String.Empty)
                    {
                        if (Regex.IsMatch(member.Name, validSidPattern))
                        {
                            localAdminGroupSidsList.Add(member.Name);
                        }
                        else
                        {
                            localAdminGroupSidsList.Add((new NTAccount(member.Name)).Translate(typeof(SecurityIdentifier)).Value);
                        }
                    }
                }
                _IsLocalAdminCheckSuccess = true;
            }
            catch { }

            try
            {
                server = OpenServer(ServerName);

                if (WTSQuerySessionInformation(server, SessionId, WTS_INFO_CLASS.ClientBuildNumber, out buffer, out bytesReturned) == false)
                {
                    return data;
                }
                int lData = Marshal.ReadInt32(buffer);
                data.ClientBuildNumber = lData;

                if (WTSQuerySessionInformation(server, SessionId, WTS_INFO_CLASS.ClientDirectory, out buffer, out bytesReturned) == false)
                {
                    return data;
                }
                string strData = Marshal.PtrToStringAnsi(buffer)!;
                data.ClientDirectory = strData;

                if (WTSQuerySessionInformation(server, SessionId, WTS_INFO_CLASS.ClientName, out buffer, out bytesReturned) == false)
                {
                    return data;
                }
                strData = Marshal.PtrToStringAnsi(buffer)!;
                data.ClientName = strData;

                if (WTSQuerySessionInformation(server, SessionId, WTS_INFO_CLASS.ClientProtocolType, out buffer, out bytesReturned) == false)
                {
                    return data;
                }
                Int16 intData = Marshal.ReadInt16(buffer);
                if (intData == 2)
                {
                    strData = "RDP";
                    data.IsRdpSession = true;
                }
                else
                {
                    strData = "";
                    data.IsRdpSession = false;
                }
                data.ClientProtocolType = strData;

                if (WTSQuerySessionInformation(server, SessionId, WTS_INFO_CLASS.ConnectState, out buffer, out bytesReturned) == false)
                {
                    return data;
                }
                lData = Marshal.ReadInt32(buffer);
                data.ConnectState = ((WTS_CONNECTSTATE_CLASS)lData).ToString();

                if (WTSQuerySessionInformation(server, SessionId, WTS_INFO_CLASS.SessionId, out buffer, out bytesReturned) == false)
                {
                    return data;
                }
                lData = Marshal.ReadInt32(buffer);
                data.SessionId = lData;

                if (WTSQuerySessionInformation(server, SessionId, WTS_INFO_CLASS.DomainName, out buffer, out bytesReturned) == false)
                {
                    return data;
                }
                strData = Marshal.PtrToStringAnsi(buffer)!.ToUpper();
                data.DomainName = strData;
                if (strData != String.Empty)
                {
                    _NTAccount = strData;
                }

                if (WTSQuerySessionInformation(server, SessionId, WTS_INFO_CLASS.UserName, out buffer, out bytesReturned) == false)
                {
                    return data;
                }
                strData = Marshal.PtrToStringAnsi(buffer)!;
                data.UserName = strData;
                if (strData != String.Empty)
                {
                    data.NTAccount = _NTAccount + "\\" + strData;
                    string _Sid = (new NTAccount(_NTAccount + "\\" + strData)).Translate(typeof(SecurityIdentifier)).Value;
                    data.SID = _Sid;
                    if (_IsLocalAdminCheckSuccess == true)
                    {
                        foreach (string localAdminGroupSid in localAdminGroupSidsList)
                        {
                            if (localAdminGroupSid == _Sid)
                            {
                                data.IsLocalAdmin = true;
                                break;
                            }
                            else
                            {
                                data.IsLocalAdmin = false;
                            }
                        }
                    }
                }

                if (WTSQuerySessionInformation(server, SessionId, WTS_INFO_CLASS.SessionName, out buffer, out bytesReturned) == false)
                {
                    return data;
                }
                strData = Marshal.PtrToStringAnsi(buffer)!;
                data.SessionName = strData;
                if (strData != "Services" && strData != "RDP-Tcp" && data.UserName != String.Empty)
                {
                    _IsUserSession = true;
                }
                data.IsUserSession = _IsUserSession;
                if (strData == "Console")
                {
                    _IsConsoleSession = true;
                }
                data.IsConsoleSession = _IsConsoleSession;

                WINSTATIONINFORMATIONW wsInfo = GetWinStationInformation(server, SessionId);
                DateTime? _loginTime = FileTimeToDateTime(wsInfo.LoginTime);
                DateTime? _lastInputTime = FileTimeToDateTime(wsInfo.LastInputTime);
                DateTime? _disconnectTime = FileTimeToDateTime(wsInfo.DisconnectTime);
                DateTime? _currentTime = FileTimeToDateTime(wsInfo.CurrentTime);
                TimeSpan? _idleTime = (_currentTime != null && _lastInputTime != null) ? _currentTime.Value - _lastInputTime.Value : TimeSpan.Zero;
                data.LogonTime = _loginTime;
                data.IdleTime = _idleTime;
                data.DisconnectTime = _disconnectTime;

                if (currentSessionID == SessionId)
                {
                    _IsCurrentSessionId = true;
                }
                data.IsCurrentSession = _IsCurrentSessionId;
            }
            finally
            {
                WTSFreeMemory(buffer);
                buffer = IntPtr.Zero;
                CloseServer(server);
            }
            return data;
        }

        public static TerminalSessionInfo GetSessionInfo(int SessionId)
        {
            return GetSessionInfo(String.Empty, SessionId);
        }

        public static TerminalSessionInfo[] GetUserSessionInfo(string ServerName)
        {
            if (ServerName.Length.Equals(0) || ServerName == "localhost")
            {
                ServerName = Dns.GetHostName();
            }

            // Find and get detailed information for all user sessions
            // Also determine the active user session. If a console user exists, then that will be the active user session.
            // If no console user exists but users are logged in, such as on terminal servers, then select the first logged-in non-console user that is either 'Active' or 'Connected' as the active user.
            TerminalSessionData[] sessions = ListSessions(ServerName);
            TerminalSessionInfo sessionInfo = new TerminalSessionInfo();
            List<TerminalSessionInfo> userSessionsInfo = new List<TerminalSessionInfo>();
            string firstActiveUserNTAccount = String.Empty;
            bool IsActiveUserSessionSet = false;
            foreach (TerminalSessionData session in sessions)
            {
                if (session.IsUserSession == true)
                {
                    sessionInfo = GetSessionInfo(ServerName, session.SessionId);
                    if (sessionInfo.IsUserSession == true)
                    {
                        if ((firstActiveUserNTAccount == String.Empty) && (sessionInfo.ConnectState == "Active" || sessionInfo.ConnectState == "Connected"))
                        {
                            firstActiveUserNTAccount = sessionInfo.NTAccount!;
                        }

                        if (sessionInfo.IsConsoleSession == true)
                        {
                            sessionInfo.IsActiveUserSession = true;
                            IsActiveUserSessionSet = true;
                        }
                        else
                        {
                            sessionInfo.IsActiveUserSession = false;
                        }

                        userSessionsInfo.Add(sessionInfo);
                    }
                }
            }

            TerminalSessionInfo[] userSessions = userSessionsInfo.ToArray();
            if (IsActiveUserSessionSet == false)
            {
                foreach (TerminalSessionInfo userSession in userSessions)
                {
                    if (userSession.NTAccount == firstActiveUserNTAccount)
                    {
                        userSession.IsActiveUserSession = true;
                        break;
                    }
                }
            }

            return userSessions;
        }

        public static TerminalSessionInfo[] GetUserSessionInfo()
        {
            return GetUserSessionInfo(String.Empty);
        }
    }
}
