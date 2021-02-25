// Date Modified: 26/01/2021
// Version Number: 3.8.4

using System;
using System.Text;
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
	public class Msi
	{
		enum LoadLibraryFlags : int
		{
			DONT_RESOLVE_DLL_REFERENCES 		= 0x00000001,
			LOAD_IGNORE_CODE_AUTHZ_LEVEL		= 0x00000010,
			LOAD_LIBRARY_AS_DATAFILE    		= 0x00000002,
			LOAD_LIBRARY_AS_DATAFILE_EXCLUSIVE	= 0x00000040,
			LOAD_LIBRARY_AS_IMAGE_RESOURCE  	= 0x00000020,
			LOAD_WITH_ALTERED_SEARCH_PATH 		= 0x00000008
		}

		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = false)]
		static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hFile, LoadLibraryFlags dwFlags);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
		static extern int LoadString(IntPtr hInstance, uint uID, StringBuilder lpBuffer, int nBufferMax);

		// Get MSI exit code message from msimsg.dll resource dll
		public static string GetMessageFromMsiExitCode(int errCode)
		{
			IntPtr hModuleInstance = LoadLibraryEx("msimsg.dll", IntPtr.Zero, LoadLibraryFlags.LOAD_LIBRARY_AS_DATAFILE);

			StringBuilder sb = new StringBuilder(255);
			uint u = Convert.ToUInt32(errCode);
			LoadString(hModuleInstance, u, sb, sb.Capacity + 1);

			return sb.ToString();
		}
	}

	public class Explorer
	{
		private static readonly IntPtr HWND_BROADCAST = new IntPtr(0xffff);
		private const int WM_SETTINGCHANGE = 0x1a;
		private const int SMTO_ABORTIFHUNG = 0x0002;

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
		static extern bool SendNotifyMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
		private static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, IntPtr wParam, string lParam, uint fuFlags, uint uTimeout, IntPtr lpdwResult);

		[DllImport("shell32.dll", CharSet = CharSet.Auto, SetLastError = false)]
		private static extern void SHChangeNotify(int eventId, uint flags, IntPtr item1, IntPtr item2);

		public static void RefreshDesktopAndEnvironmentVariables()
		{
			// Update desktop icons
			SHChangeNotify(0x8000000, 0x1000, IntPtr.Zero, IntPtr.Zero);
			SendMessageTimeout(HWND_BROADCAST, WM_SETTINGCHANGE, IntPtr.Zero, null, SMTO_ABORTIFHUNG, 100, IntPtr.Zero);
			// Update environment variables
			SendMessageTimeout(HWND_BROADCAST, WM_SETTINGCHANGE, IntPtr.Zero, "Environment", SMTO_ABORTIFHUNG, 100, IntPtr.Zero);
		}
	}

	public sealed class FileVerb
	{
		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
		public static extern int LoadString(IntPtr h, uint id, StringBuilder sb, int maxBuffer);

		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = false)]
		public static extern IntPtr LoadLibrary(string s);

		public static string GetPinVerb(int VerbId)
		{
			IntPtr hShell32 = LoadLibrary("shell32.dll");
			const int nChars  = 255;
			StringBuilder Buff = new StringBuilder("", nChars);
			uint u = Convert.ToUInt32(VerbId);
			LoadString(hShell32, u, Buff, Buff.Capacity);
			return Buff.ToString();
		}
	}

	public sealed class IniFile
	{
		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = false)]
		public static extern int GetPrivateProfileString(string lpAppName, string lpKeyName, string lpDefault, StringBuilder lpReturnedString, int nSize, string lpFileName);

		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = false)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool WritePrivateProfileString(string lpAppName, string lpKeyName, StringBuilder lpString, string lpFileName);

		public static string GetIniValue(string section, string key, string filepath)
		{
			string sDefault	= "";
			const int  nChars  = 1024;
			StringBuilder Buff = new StringBuilder(nChars);

			GetPrivateProfileString(section, key, sDefault, Buff, Buff.Capacity, filepath);
			return Buff.ToString();
		}

		public static void SetIniValue(string section, string key, StringBuilder value, string filepath)
		{
			WritePrivateProfileString(section, key, value, filepath);
		}
	}

	public class UiAutomation
	{
		public enum GetWindow_Cmd : int
		{
			GW_HWNDFIRST    = 0,
			GW_HWNDLAST     = 1,
			GW_HWNDNEXT     = 2,
			GW_HWNDPREV     = 3,
			GW_OWNER        = 4,
			GW_CHILD        = 5,
			GW_ENABLEDPOPUP = 6
		}

		public enum ShowWindowEnum
		{
			Hide                    = 0,
			ShowNormal              = 1,
			ShowMinimized           = 2,
			ShowMaximized           = 3,
			Maximize                = 3,
			ShowNormalNoActivate    = 4,
			Show                    = 5,
			Minimize                = 6,
			ShowMinNoActivate       = 7,
			ShowNoActivate          = 8,
			Restore                 = 9,
			ShowDefault             = 10,
			ForceMinimized          = 11
		}

		public enum UserNotificationState
		{
			// http://msdn.microsoft.com/en-us/library/bb762533(v=vs.85).aspx
			ScreenSaverOrLockedOrFastUserSwitching		=1, // A screen saver is displayed, the machine is locked, or a nonactive Fast User Switching session is in progress.
			FullScreenOrPresentationModeOrLoginScreen	=2, // A full-screen application is running or Presentation Settings are applied. Presentation Settings allow a user to put their machine into a state fit for an uninterrupted presentation, such as a set of PowerPoint slides, with a single click. Also returns this state if machine is at the login screen.
			RunningDirect3DFullScreen					=3, // A full-screen, exclusive mode, Direct3D application is running.
			PresentationMode 							=4, // The user has activated Windows presentation settings to block notifications and pop-up messages.
			AcceptsNotifications						=5, // None of the other states are found, notifications can be freely sent.
			QuietTime									=6, // Introduced in Windows 7. The current user is in "quiet time", which is the first hour after a new user logs into his or her account for the first time.
			WindowsStoreAppRunning						=7  // Introduced in Windows 8. A Windows Store app is running.
		}

		// Only for Vista or above
		[DllImport("shell32.dll", CharSet = CharSet.Auto, SetLastError = false)]
		static extern int SHQueryUserNotificationState(out UserNotificationState pquns);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool EnumWindows(EnumWindowsProcD lpEnumFunc, ref IntPtr lParam);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
		public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
		public static extern int GetWindowTextLength(IntPtr hWnd);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
		private static extern IntPtr GetDesktopWindow();

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
		private static extern IntPtr GetShellWindow();

        public enum DeviceCap
        {
            HORZRES = 8,
            VERTRES = 10,
            DESKTOPVERTRES = 117,
            DESKTOPHORZRES = 118
        }

        [DllImport("gdi32.dll", CharSet = CharSet.Auto, SetLastError = false)]
        public static extern int GetDeviceCaps(IntPtr hDC, int nIndex);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool IsWindowEnabled(IntPtr hWnd);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
		public static extern bool IsWindowVisible(IntPtr hWnd);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool IsIconic(IntPtr hWnd);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool ShowWindow(IntPtr hWnd, ShowWindowEnum flags);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
		public static extern IntPtr SetActiveWindow(IntPtr hwnd);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool SetForegroundWindow(IntPtr hWnd);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
		public static extern IntPtr GetForegroundWindow();

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
		public static extern IntPtr SetFocus(IntPtr hWnd);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
		public static extern bool BringWindowToTop(IntPtr hWnd);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
		public static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = false)]
		public static extern int GetCurrentThreadId();

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
		public static extern bool AttachThreadInput(int idAttach, int idAttachTo, bool fAttach);

		[DllImport("user32.dll", EntryPoint = "GetWindowLong", CharSet = CharSet.Auto, SetLastError = false)]
		public static extern IntPtr GetWindowLong32(IntPtr hWnd, int nIndex);

		[DllImport("user32.dll", EntryPoint = "GetWindowLongPtr", CharSet = CharSet.Auto, SetLastError = false)]
		public static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
		public static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
		public static extern bool EnableMenuItem(IntPtr hMenu, uint uIDEnableItem, uint uEnable);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
		public static extern IntPtr DestroyMenu(IntPtr hWnd);

		public delegate bool EnumWindowsProcD(IntPtr hWnd, ref IntPtr lItems);

		public static bool EnumWindowsProc(IntPtr hWnd, ref IntPtr lItems)
		{
			if (hWnd != IntPtr.Zero)
			{
				GCHandle hItems = GCHandle.FromIntPtr(lItems);
				List<IntPtr> items = hItems.Target as List<IntPtr>;
				items.Add(hWnd);
				return true;
			}
			else
			{
				return false;
			}
		}

		public static List<IntPtr> EnumWindows()
		{
			try
			{
				List<IntPtr> items = new List<IntPtr>();
				EnumWindowsProcD CallBackPtr = new EnumWindowsProcD(EnumWindowsProc);
				GCHandle hItems = GCHandle.Alloc(items);
				IntPtr lItems = GCHandle.ToIntPtr(hItems);
				EnumWindows(CallBackPtr, ref lItems);
				return items;
			}
			catch (Exception ex)
			{
				throw new Exception("An error occured during window enumeration: " + ex.Message);
			}
		}

		public static string GetWindowText(IntPtr hWnd)
		{
			int iTextLength = GetWindowTextLength(hWnd);
			if (iTextLength > 0)
			{
				StringBuilder sb = new StringBuilder(iTextLength);
				GetWindowText(hWnd, sb, iTextLength + 1);
				return sb.ToString();
			}
			else
			{
				return String.Empty;
			}
		}

		public static bool BringWindowToFront(IntPtr windowHandle)
		{
			bool breturn = false;
			if (IsIconic(windowHandle))
			{
				// Show minimized window because SetForegroundWindow does not work for minimized windows
				ShowWindow(windowHandle, ShowWindowEnum.ShowMaximized);
			}

			int lpdwProcessId;
			int windowThreadProcessId = GetWindowThreadProcessId(GetForegroundWindow(), out lpdwProcessId);
			int currentThreadId = GetCurrentThreadId();
			AttachThreadInput(windowThreadProcessId, currentThreadId, true);

			BringWindowToTop(windowHandle);
			breturn = SetForegroundWindow(windowHandle);
			SetActiveWindow(windowHandle);
			SetFocus(windowHandle);

			AttachThreadInput(windowThreadProcessId, currentThreadId, false);
			return breturn;
		}

		public static int GetWindowThreadProcessId(IntPtr windowHandle)
		{
			int processID = 0;
			GetWindowThreadProcessId(windowHandle, out processID);
			return processID;
		}

		public static IntPtr GetWindowLong(IntPtr hWnd, int nIndex)
		{
			if (IntPtr.Size == 4)
			{
				return GetWindowLong32(hWnd, nIndex);
			}
			return GetWindowLongPtr64(hWnd, nIndex);
		}

		public static string GetUserNotificationState()
		{
			// Only works for Windows Vista or higher
			UserNotificationState state;
			int returnVal = SHQueryUserNotificationState(out state);
			return state.ToString();
		}
	}

	public class QueryUser
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
			public string NTAccount;
			public string SID;
			public string UserName;
			public string DomainName;
			public int SessionId;
			public string SessionName;
			public string ConnectState;
			public bool IsCurrentSession;
			public bool IsConsoleSession;
			public bool IsActiveUserSession;
			public bool IsUserSession;
			public bool IsRdpSession;
			public bool IsLocalAdmin;
			public DateTime? LogonTime;
			public TimeSpan? IdleTime;
			public DateTime? DisconnectTime;
			public string ClientName;
			public string ClientProtocolType;
			public string ClientDirectory;
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
			SessionId=4,
			UserName,
			SessionName,
			DomainName,
			ConnectState,
			ClientBuildNumber,
			ClientName,
			ClientDirectory,
			ClientProtocolType=16
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
				T item = (T) Marshal.PtrToStructure(new IntPtr(pointer), typeof(T));
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
			long hFT = (((long) ft.dwHighDateTime) << 32) + ft.dwLowDateTime;
			return DateTime.FromFileTime(hFT);
		}

		public static WINSTATIONINFORMATIONW GetWinStationInformation(IntPtr server, int sessionId)
		{
			int retLen = 0;
			WINSTATIONINFORMATIONW wsInfo = new WINSTATIONINFORMATIONW();
			WinStationQueryInformation(server, sessionId, (int) WINSTATIONINFOCLASS.WinStationInformation, ref wsInfo, Marshal.SizeOf(typeof(WINSTATIONINFORMATIONW)), ref retLen);
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
			if (ServerName == "localhost" || ServerName == String.Empty)
			{
				ServerName = Environment.MachineName;
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
				object members = admGroup.Invoke("members", null);
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
				string strData = Marshal.PtrToStringAnsi(buffer);
				data.ClientDirectory = strData;

				if (WTSQuerySessionInformation(server, SessionId, WTS_INFO_CLASS.ClientName, out buffer, out bytesReturned) == false)
				{
					return data;
				}
				strData = Marshal.PtrToStringAnsi(buffer);
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
				data.ConnectState = ((WTS_CONNECTSTATE_CLASS) lData).ToString();

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
				strData = Marshal.PtrToStringAnsi(buffer).ToUpper();
				data.DomainName = strData;
				if (strData != String.Empty)
				{
					_NTAccount = strData;
				}

				if (WTSQuerySessionInformation(server, SessionId, WTS_INFO_CLASS.UserName, out buffer, out bytesReturned) == false)
				{
					return data;
				}
				strData = Marshal.PtrToStringAnsi(buffer);
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
				strData = Marshal.PtrToStringAnsi(buffer);
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

		public static TerminalSessionInfo[] GetUserSessionInfo(string ServerName)
		{
			if (ServerName == "localhost" || ServerName == String.Empty)
			{
				ServerName = Environment.MachineName;
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
							firstActiveUserNTAccount = sessionInfo.NTAccount;
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
	}
}
