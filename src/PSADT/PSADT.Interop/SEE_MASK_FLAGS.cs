using System;

namespace PSADT.Interop
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
}
