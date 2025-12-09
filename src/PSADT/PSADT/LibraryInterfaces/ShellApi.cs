using System;

namespace PSADT.LibraryInterfaces
{
    /// <summary>
    /// Execution flags for the ShellExecuteEx function.
    /// </summary>
    [Flags]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1069:Enums values should not be duplicated", Justification = "These values are precisely as they're defined in the Win32 API.")]
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
        /// <para><b>Introduced in Windows 7</b>. The current user is in "quiet time", which is the first hour after a new user logs into his or her account for the first time. During this time, most notifications should not be sent or shown. This lets a user become accustomed to a new computer system without those distractions. Quiet time also occurs for each user after an operating system upgrade or clean installation.</para>
        /// <para>Applications should set the <a href="https://docs.microsoft.com/windows/desktop/api/shellapi/ns-shellapi-notifyicondataa">NIIF_RESPECT_QUIET_TIME</a> flag in their notifications or balloon tooltip, which prevents those items from being displayed while the current user is in the quiet-time state. Note that during quiet time, if the user is in one of the other blocked modes (QUNS_NOT_PRESENT, QUNS_BUSY, QUNS_PRESENTATION_MODE, or QUNS_RUNNING_D3D_FULL_SCREEN) <a href="https://docs.microsoft.com/windows/desktop/api/shellapi/nf-shellapi-shqueryusernotificationstate">SHQueryUserNotificationState</a> returns only that value, and does not report QUNS_QUIET_TIME.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-query_user_notification_state#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        QUNS_QUIET_TIME = Windows.Win32.UI.Shell.QUERY_USER_NOTIFICATION_STATE.QUNS_QUIET_TIME,

        /// <summary>
        /// <b>Introduced in Windows 8</b>. A Windows Store app is running.
        /// </summary>
        QUNS_APP = Windows.Win32.UI.Shell.QUERY_USER_NOTIFICATION_STATE.QUNS_APP,
    }

    /// <summary>
    /// Flags for SHGetImageList function.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1069:Enums values should not be duplicated", Justification = "These values are precisely as they're defined in the Win32 API.")]
    internal enum SHIL_SIZE : uint
    {
        /// <summary>
        /// The image size is normally 32x32 pixels. However, if the Use large icons option is selected from the Effects section of the Appearance tab in Display Properties, the image is 48x48 pixels.
        /// </summary>
        SHIL_LARGE = Windows.Win32.PInvoke.SHIL_LARGE,

        /// <summary>
        /// These images are the Shell standard small icon size of 16x16, but the size can be customized by the user.
        /// </summary>
        SHIL_SMALL = Windows.Win32.PInvoke.SHIL_SMALL,

        /// <summary>
        /// These images are the Shell standard extra-large icon size. This is typically 48x48, but the size can be customized by the user.
        /// </summary>
        SHIL_EXTRALARGE = Windows.Win32.PInvoke.SHIL_EXTRALARGE,

        /// <summary>
        /// These images are the size specified by GetSystemMetrics called with SM_CXSMICON and GetSystemMetrics called with SM_CYSMICON.
        /// </summary>
        SHIL_SYSSMALL = Windows.Win32.PInvoke.SHIL_SYSSMALL,

        /// <summary>
        /// Windows Vista and later. The image is normally 256x256 pixels.
        /// </summary>
        SHIL_JUMBO = Windows.Win32.PInvoke.SHIL_JUMBO,

        /// <summary>
        /// The largest valid flag value, for validation purposes.
        /// </summary>
        SHIL_LAST = Windows.Win32.PInvoke.SHIL_LAST,
    }
}
