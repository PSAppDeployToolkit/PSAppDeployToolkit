using System;
using System.Collections.Specialized;

namespace PSADT.Types
{
    public struct InstalledApplication
    {
        public string UninstallSubkey;
        public string ProductCode;
        public string DisplayName;
        public string DisplayVersion;
        public string UninstallString;
        public string InstallSource;
        public string InstallLocation;
        public string InstallDate;
        public string Publisher;
        public bool Is64BitApplication;
    }

    public struct ProcessObject
    {
        public string Name;
        public string Description;
    }

    public struct ProcessInfo
    {
        public int Id;
        public IntPtr Handle;
        public string ProcessName;
    }

    public struct ProcessResult
    {
        public int ExitCode;
        public string StdOut;
        public string StdErr;
    }

    public struct UserProfile
    {
        public string NTAccount;
        public string SID;
        public string ProfilePath;
    }

    public struct WindowInfo
    {
        public string WindowTitle;
        public IntPtr WindowHandle;
        public string ParentProcess;
        public IntPtr ParentProcessMainWindowHandle;
        public int ParentProcessId;
    }

    public struct BatteryInfo
    {
        public string ACPowerLineStatus;
        public string BatteryChargeStatus;
        public float BatteryLifePercent;
        public int BatteryLifeRemaining;
        public int BatteryFullLifetime;
        public bool IsUsingACPower;
        public bool IsLaptop;
    }

    public struct RebootInfo
    {
        public string ComputerName;
        public DateTime LastBootUpTime;
        public bool IsSystemRebootPending;
        public bool IsCBServicingRebootPending;
        public bool IsWindowsUpdateRebootPending;
        public bool? IsSCCMClientRebootPending;
        public bool IsAppVRebootPending;
        public bool? IsFileRenameRebootPending;
        public string[] PendingFileRenameOperations;
        public StringCollection ErrorMsg;
    }

    public abstract class ShortcutBase
    {
        public string? Path;
        public string? TargetPath;
        public string? IconIndex;
        public string? IconLocation;
    }

    public class ShortcutUrl : ShortcutBase
    {
    }

    public class ShortcutLnk : ShortcutBase
    {
        public string? Arguments;
        public string? Description;
        public string? WorkingDirectory;
        public string? WindowStyle;
        public string? Hotkey;
        public bool RunAsAdmin;
    }

    public struct MsiSummaryInfo
    {
        public int CodePage;
        public string Title;
        public string Subject;
        public string Author;
        public string Keywords;
        public string Comments;
        public string Template;
        public string LastSavedBy;
        public Guid RevisionNumber;
        public Nullable<DateTime> LastPrinted;
        public DateTime CreateTimeDate;
        public DateTime LastSaveTimeDate;
        public int PageCount;
        public int WordCount;
        public Nullable<int> CharacterCount;
        public string CreatingApplication;
        public int Security;
    }
}