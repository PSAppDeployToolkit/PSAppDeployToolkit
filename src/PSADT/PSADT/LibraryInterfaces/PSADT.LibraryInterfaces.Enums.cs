using System;

namespace PSADT.LibraryInterfaces
{
    /// <summary>
    /// The architecture of the executable.
    /// </summary>
    public enum IMAGE_FILE_MACHINE : ushort
    {
        /// <summary>
        /// Alpha
        /// </summary>
        IMAGE_FILE_MACHINE_AXP64 = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_AXP64,

        /// <summary>
        /// x86
        /// </summary>
        IMAGE_FILE_MACHINE_I386 = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_I386,

        /// <summary>
        /// Itanium
        /// </summary>
        IMAGE_FILE_MACHINE_IA64 = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_IA64,

        /// <summary>
        /// AMD64
        /// </summary>
        IMAGE_FILE_MACHINE_AMD64 = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_AMD64,

        /// <summary>
        /// Unknown
        /// </summary>
        IMAGE_FILE_MACHINE_UNKNOWN = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_UNKNOWN,

        /// <summary>
        /// Target host
        /// </summary>
        IMAGE_FILE_MACHINE_TARGET_HOST = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_TARGET_HOST,

        /// <summary>
        /// R3000
        /// </summary>
        IMAGE_FILE_MACHINE_R3000 = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_R3000,

        /// <summary>
        /// R4000
        /// </summary>
        IMAGE_FILE_MACHINE_R4000 = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_R4000,

        /// <summary>
        /// R10000
        /// </summary>
        IMAGE_FILE_MACHINE_R10000 = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_R10000,

        /// <summary>
        /// Windows CE MIPS
        /// </summary>
        IMAGE_FILE_MACHINE_WCEMIPSV2 = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_WCEMIPSV2,

        /// <summary>
        /// Alpha
        /// </summary>
        IMAGE_FILE_MACHINE_ALPHA = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_ALPHA,

        /// <summary>
        /// SH3
        /// </summary>
        IMAGE_FILE_MACHINE_SH3 = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_SH3,

        /// <summary>
        /// SH3DSP
        /// </summary>
        IMAGE_FILE_MACHINE_SH3DSP = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_SH3DSP,

        /// <summary>
        /// SH3E
        /// </summary>
        IMAGE_FILE_MACHINE_SH3E = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_SH3E,

        /// <summary>
        /// SH4
        /// </summary>
        IMAGE_FILE_MACHINE_SH4 = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_SH4,

        /// <summary>
        /// SH5
        /// </summary>
        IMAGE_FILE_MACHINE_SH5 = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_SH5,

        /// <summary>
        /// ARM
        /// </summary>
        IMAGE_FILE_MACHINE_ARM = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_ARM,

        /// <summary>
        /// Thumb
        /// </summary>
        IMAGE_FILE_MACHINE_THUMB = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_THUMB,

        /// <summary>
        /// ARMNT
        /// </summary>
        IMAGE_FILE_MACHINE_ARMNT = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_ARMNT,

        /// <summary>
        /// AM33
        /// </summary>
        IMAGE_FILE_MACHINE_AM33 = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_AM33,

        /// <summary>
        /// PowerPC
        /// </summary>
        IMAGE_FILE_MACHINE_POWERPC = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_POWERPC,

        /// <summary>
        /// PowerPCFP
        /// </summary>
        IMAGE_FILE_MACHINE_POWERPCFP = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_POWERPCFP,

        /// <summary>
        /// MIPS16
        /// </summary>
        IMAGE_FILE_MACHINE_MIPS16 = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_MIPS16,

        /// <summary>
        /// Alpha64
        /// </summary>
        IMAGE_FILE_MACHINE_ALPHA64 = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_ALPHA64,

        /// <summary>
        /// MIPSFPU
        /// </summary>
        IMAGE_FILE_MACHINE_MIPSFPU = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_MIPSFPU,

        /// <summary>
        /// MIPSFPU16
        /// </summary>
        IMAGE_FILE_MACHINE_MIPSFPU16 = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_MIPSFPU16,

        /// <summary>
        /// Tricore
        /// </summary>
        IMAGE_FILE_MACHINE_TRICORE = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_TRICORE,

        /// <summary>
        /// CEF
        /// </summary>
        IMAGE_FILE_MACHINE_CEF = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_CEF,

        /// <summary>
        /// EBC
        /// </summary>
        IMAGE_FILE_MACHINE_EBC = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_EBC,

        /// <summary>
        /// M32R
        /// </summary>
        IMAGE_FILE_MACHINE_M32R = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_M32R,

        /// <summary>
        /// ARM64
        /// </summary>
        IMAGE_FILE_MACHINE_ARM64 = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_ARM64,

        /// <summary>
        /// CEE
        /// </summary>
        IMAGE_FILE_MACHINE_CEE = Windows.Win32.System.SystemInformation.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_CEE,
    }

    /// <summary>
    /// The subsystem of the executable.
    /// </summary>
    public enum IMAGE_SUBSYSTEM : ushort
    {
        /// <summary>
        /// Unknown
        /// </summary>
        IMAGE_SUBSYSTEM_UNKNOWN = Windows.Win32.System.Diagnostics.Debug.IMAGE_SUBSYSTEM.IMAGE_SUBSYSTEM_UNKNOWN,

        /// <summary>
        /// Native
        /// </summary>
        IMAGE_SUBSYSTEM_NATIVE = Windows.Win32.System.Diagnostics.Debug.IMAGE_SUBSYSTEM.IMAGE_SUBSYSTEM_NATIVE,

        /// <summary>
        /// Windows GUI
        /// </summary>
        IMAGE_SUBSYSTEM_WINDOWS_GUI = Windows.Win32.System.Diagnostics.Debug.IMAGE_SUBSYSTEM.IMAGE_SUBSYSTEM_WINDOWS_GUI,

        /// <summary>
        /// Windows CUI
        /// </summary>
        IMAGE_SUBSYSTEM_WINDOWS_CUI = Windows.Win32.System.Diagnostics.Debug.IMAGE_SUBSYSTEM.IMAGE_SUBSYSTEM_WINDOWS_CUI,

        /// <summary>
        /// OS/2 CUI
        /// </summary>
        IMAGE_SUBSYSTEM_OS2_CUI = Windows.Win32.System.Diagnostics.Debug.IMAGE_SUBSYSTEM.IMAGE_SUBSYSTEM_OS2_CUI,

        /// <summary>
        /// POSIX CUI
        /// </summary>
        IMAGE_SUBSYSTEM_POSIX_CUI = Windows.Win32.System.Diagnostics.Debug.IMAGE_SUBSYSTEM.IMAGE_SUBSYSTEM_POSIX_CUI,

        /// <summary>
        /// Windows CE GUI
        /// </summary>
        IMAGE_SUBSYSTEM_WINDOWS_CE_GUI = Windows.Win32.System.Diagnostics.Debug.IMAGE_SUBSYSTEM.IMAGE_SUBSYSTEM_WINDOWS_CE_GUI,

        /// <summary>
        /// EFI Application
        /// </summary>
        IMAGE_SUBSYSTEM_EFI_APPLICATION = Windows.Win32.System.Diagnostics.Debug.IMAGE_SUBSYSTEM.IMAGE_SUBSYSTEM_EFI_APPLICATION,

        /// <summary>
        /// EFI Boot Service Driver
        /// </summary>
        IMAGE_SUBSYSTEM_EFI_BOOT_SERVICE_DRIVER = Windows.Win32.System.Diagnostics.Debug.IMAGE_SUBSYSTEM.IMAGE_SUBSYSTEM_EFI_BOOT_SERVICE_DRIVER,

        /// <summary>
        /// EFI Runtime Driver
        /// </summary>
        IMAGE_SUBSYSTEM_EFI_RUNTIME_DRIVER = Windows.Win32.System.Diagnostics.Debug.IMAGE_SUBSYSTEM.IMAGE_SUBSYSTEM_EFI_RUNTIME_DRIVER,

        /// <summary>
        /// EFI ROM
        /// </summary>
        IMAGE_SUBSYSTEM_EFI_ROM = Windows.Win32.System.Diagnostics.Debug.IMAGE_SUBSYSTEM.IMAGE_SUBSYSTEM_EFI_ROM,

        /// <summary>
        /// Xbox
        /// </summary>
        IMAGE_SUBSYSTEM_XBOX = Windows.Win32.System.Diagnostics.Debug.IMAGE_SUBSYSTEM.IMAGE_SUBSYSTEM_XBOX,

        /// <summary>
        /// Windows boot application
        /// </summary>
        IMAGE_SUBSYSTEM_WINDOWS_BOOT_APPLICATION = Windows.Win32.System.Diagnostics.Debug.IMAGE_SUBSYSTEM.IMAGE_SUBSYSTEM_WINDOWS_BOOT_APPLICATION,
    }

    /// <summary>
    /// Execution flags for the ShellExecuteEx function.
    /// </summary>
    [Flags]
    internal enum SEE_MASK_FLAGS : uint
    {
        /// <summary>
        /// Default
        /// </summary>
        SEE_MASK_DEFAULT = Windows.Win32.PInvoke.SEE_MASK_DEFAULT,

        /// <summary>
        /// Class name
        /// </summary>
        SEE_MASK_CLASSNAME = Windows.Win32.PInvoke.SEE_MASK_CLASSNAME,

        /// <summary>
        /// Class key
        /// </summary>
        SEE_MASK_CLASSKEY = Windows.Win32.PInvoke.SEE_MASK_CLASSKEY,

        /// <summary>
        /// ID list
        /// </summary>
        SEE_MASK_IDLIST = Windows.Win32.PInvoke.SEE_MASK_IDLIST,

        /// <summary>
        /// INVOKE ID list
        /// </summary>
        SEE_MASK_INVOKEIDLIST = Windows.Win32.PInvoke.SEE_MASK_INVOKEIDLIST,

        /// <summary>
        /// ICON
        /// </summary>
        SEE_MASK_ICON = Windows.Win32.PInvoke.SEE_MASK_ICON,

        /// <summary>
        /// HOTKEY
        /// </summary>
        SEE_MASK_HOTKEY = Windows.Win32.PInvoke.SEE_MASK_HOTKEY,

        /// <summary>
        /// NOCLOSEPROCESS
        /// </summary>
        SEE_MASK_NOCLOSEPROCESS = Windows.Win32.PInvoke.SEE_MASK_NOCLOSEPROCESS,

        /// <summary>
        /// CONNECTNETDRV
        /// </summary>
        SEE_MASK_CONNECTNETDRV = Windows.Win32.PInvoke.SEE_MASK_CONNECTNETDRV,

        /// <summary>
        /// NOASYNC
        /// </summary>
        SEE_MASK_FLAG_DDEWAIT = Windows.Win32.PInvoke.SEE_MASK_FLAG_DDEWAIT,

        /// <summary>
        /// DOENVSUBST
        /// </summary>
        SEE_MASK_DOENVSUBST = Windows.Win32.PInvoke.SEE_MASK_DOENVSUBST,

        /// <summary>
        /// FLAG_NO_UI
        /// </summary>
        SEE_MASK_FLAG_NO_UI = Windows.Win32.PInvoke.SEE_MASK_FLAG_NO_UI,

        /// <summary>
        /// UNICODE
        /// </summary>
        SEE_MASK_UNICODE = Windows.Win32.PInvoke.SEE_MASK_UNICODE,

        /// <summary>
        /// NO_CONSOLE
        /// </summary>
        SEE_MASK_NO_CONSOLE = Windows.Win32.PInvoke.SEE_MASK_NO_CONSOLE,

        /// <summary>
        /// NOZONECHECKS
        /// </summary>
        SEE_MASK_NOZONECHECKS = Windows.Win32.PInvoke.SEE_MASK_NOZONECHECKS,

        /// <summary>
        /// NOASYNC
        /// </summary>
        SEE_MASK_NOASYNC = Windows.Win32.PInvoke.SEE_MASK_NOASYNC,

        /// <summary>
        /// HMONITOR
        /// </summary>
        SEE_MASK_HMONITOR = Windows.Win32.PInvoke.SEE_MASK_HMONITOR,

        /// <summary>
        /// NOQUERYCLASSSTORE
        /// </summary>
        SEE_MASK_NOQUERYCLASSSTORE = Windows.Win32.PInvoke.SEE_MASK_NOQUERYCLASSSTORE,

        /// <summary>
        /// WAITFORINPUTIDLE
        /// </summary>
        SEE_MASK_WAITFORINPUTIDLE = Windows.Win32.PInvoke.SEE_MASK_WAITFORINPUTIDLE,

        /// <summary>
        /// FLAG_LOG_USAGE
        /// </summary>
        SEE_MASK_FLAG_LOG_USAGE = Windows.Win32.PInvoke.SEE_MASK_FLAG_LOG_USAGE,

        /// <summary>
        /// FLAG_HINST_IS_SITE
        /// </summary>
        SEE_MASK_FLAG_HINST_IS_SITE = Windows.Win32.PInvoke.SEE_MASK_FLAG_HINST_IS_SITE,
    }

    /// <summary>
    /// Specifies the connection state of a Remote Desktop Services session.
    /// </summary>
    /// <remarks>
    /// <para><see href="https://learn.microsoft.com/windows/win32/api/wtsapi32/ne-wtsapi32-wts_connectstate_class">Learn more about this API from docs.microsoft.com</see>.</para>
    /// </remarks>
    public enum WTS_CONNECTSTATE_CLASS
    {
        /// <summary>
        /// A user is logged on to the WinStation. This state occurs when a user is signed in and actively connected to the device.
        /// </summary>
        WTSActive = Windows.Win32.System.RemoteDesktop.WTS_CONNECTSTATE_CLASS.WTSActive,

        /// <summary>
        /// The WinStation is connected to the client.
        /// </summary>
        WTSConnected = Windows.Win32.System.RemoteDesktop.WTS_CONNECTSTATE_CLASS.WTSConnected,

        /// <summary>
        /// The WinStation is in the process of connecting to the client.
        /// </summary>
        WTSConnectQuery = Windows.Win32.System.RemoteDesktop.WTS_CONNECTSTATE_CLASS.WTSConnectQuery,

        /// <summary>
        /// The WinStation is shadowing another WinStation.
        /// </summary>
        WTSShadow = Windows.Win32.System.RemoteDesktop.WTS_CONNECTSTATE_CLASS.WTSShadow,

        /// <summary>
        /// The WinStation is active but the client is disconnected. This state occurs when a user is signed in but not actively connected to the device, such as when the user has chosen to exit to the lock screen.
        /// </summary>
        WTSDisconnected = Windows.Win32.System.RemoteDesktop.WTS_CONNECTSTATE_CLASS.WTSDisconnected,

        /// <summary>
        /// The WinStation is waiting for a client to connect.
        /// </summary>
        WTSIdle = Windows.Win32.System.RemoteDesktop.WTS_CONNECTSTATE_CLASS.WTSIdle,

        /// <summary>
        /// The WinStation is listening for a connection. A listener session waits for requests for new client connections. No user is logged on a listener session. A listener session cannot be reset, shadowed, or changed to a regular client session.
        /// </summary>
        WTSListen = Windows.Win32.System.RemoteDesktop.WTS_CONNECTSTATE_CLASS.WTSListen,

        /// <summary>
        /// The WinStation is being reset.
        /// </summary>
        WTSReset = Windows.Win32.System.RemoteDesktop.WTS_CONNECTSTATE_CLASS.WTSReset,

        /// <summary>
        /// The WinStation is down due to an error.
        /// </summary>
        WTSDown = Windows.Win32.System.RemoteDesktop.WTS_CONNECTSTATE_CLASS.WTSDown,

        /// <summary>
        /// The WinStation is initializing.
        /// </summary>
        WTSInit = Windows.Win32.System.RemoteDesktop.WTS_CONNECTSTATE_CLASS.WTSInit,
    }

    /// <summary>
    /// Specifies the state of the machine for the current user in relation to the propriety of sending a notification. Used by SHQueryUserNotificationState.
    /// </summary>
    public enum QUERY_USER_NOTIFICATION_STATE
    {
        /// <summary>
        /// A screen saver is displayed, the machine is locked, or a nonactive Fast User Switching session is in progress.
        /// </summary>
        QUNS_NOT_PRESENT = Windows.Win32.UI.Shell.QUERY_USER_NOTIFICATION_STATE.QUNS_NOT_PRESENT,

        /// <summary>
        /// A full-screen application is running or Presentation Settings are applied. Presentation Settings allow a user to put their machine into a state fit for an uninterrupted presentation, such as a set of PowerPoint slides, with a single click.
        /// </summary>
        QUNS_BUSY = Windows.Win32.UI.Shell.QUERY_USER_NOTIFICATION_STATE.QUNS_BUSY,

        /// <summary>
        /// A full-screen (exclusive mode) Direct3D application is running.
        /// </summary>
        QUNS_RUNNING_D3D_FULL_SCREEN = Windows.Win32.UI.Shell.QUERY_USER_NOTIFICATION_STATE.QUNS_RUNNING_D3D_FULL_SCREEN,

        /// <summary>
        /// The user has activated Windows presentation settings to block notifications and pop-up messages.
        /// </summary>
        QUNS_PRESENTATION_MODE = Windows.Win32.UI.Shell.QUERY_USER_NOTIFICATION_STATE.QUNS_PRESENTATION_MODE,

        /// <summary>
        /// None of the other states are found, notifications can be freely sent.
        /// </summary>
        QUNS_ACCEPTS_NOTIFICATIONS = Windows.Win32.UI.Shell.QUERY_USER_NOTIFICATION_STATE.QUNS_ACCEPTS_NOTIFICATIONS,

        /// <summary>
        /// <para><b>Introduced in Windows 7</b>. The current user is in "quiet time", which is the first hour after a new user logs into his or her account for the first time. During this time, most notifications should not be sent or shown. This lets a user become accustomed to a new computer system without those distractions. Quiet time also occurs for each user after an operating system upgrade or clean installation.</para>
        /// <para>Applications should set the <a href="https://docs.microsoft.com/windows/desktop/api/shellapi/ns-shellapi-notifyicondataa">NIIF_RESPECT_QUIET_TIME</a> flag in their notifications or balloon tooltip, which prevents those items from being displayed while the current user is in the quiet-time state. Note that during quiet time, if the user is in one of the other blocked modes (QUNS_NOT_PRESENT, QUNS_BUSY, QUNS_PRESENTATION_MODE, or QUNS_RUNNING_D3D_FULL_SCREEN) <a href="https://docs.microsoft.com/windows/desktop/api/shellapi/nf-shellapi-shqueryusernotificationstate">SHQueryUserNotificationState</a> returns only that value, and does not report QUNS_QUIET_TIME.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-query_user_notification_state#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        QUNS_QUIET_TIME = Windows.Win32.UI.Shell.QUERY_USER_NOTIFICATION_STATE.QUNS_QUIET_TIME,

        /// <summary>
        /// <b>Introduced in Windows 8</b>. A Windows Store app is running.
        /// </summary>
        QUNS_APP = Windows.Win32.UI.Shell.QUERY_USER_NOTIFICATION_STATE.QUNS_APP,
    }
}
