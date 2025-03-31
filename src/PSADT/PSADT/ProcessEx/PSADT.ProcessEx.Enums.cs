using System;
using Windows.Win32;

namespace PSADT.ProcessEx
{
    /// <summary>
    /// Execution flags for the ShellExecuteEx function.
    /// </summary>
    [Flags]
    internal enum SEE_MASK_FLAGS : uint
    {
        /// <summary>
        /// Default
        /// </summary>
        SEE_MASK_DEFAULT = PInvoke.SEE_MASK_DEFAULT,

        /// <summary>
        /// Class name
        /// </summary>
        SEE_MASK_CLASSNAME = PInvoke.SEE_MASK_CLASSNAME,

        /// <summary>
        /// Class key
        /// </summary>
        SEE_MASK_CLASSKEY = PInvoke.SEE_MASK_CLASSKEY,

        /// <summary>
        /// ID list
        /// </summary>
        SEE_MASK_IDLIST = PInvoke.SEE_MASK_IDLIST,

        /// <summary>
        /// INVOKE ID list
        /// </summary>
        SEE_MASK_INVOKEIDLIST = PInvoke.SEE_MASK_INVOKEIDLIST,

        /// <summary>
        /// ICON
        /// </summary>
        SEE_MASK_ICON = PInvoke.SEE_MASK_ICON,

        /// <summary>
        /// HOTKEY
        /// </summary>
        SEE_MASK_HOTKEY = PInvoke.SEE_MASK_HOTKEY,

        /// <summary>
        /// NOCLOSEPROCESS
        /// </summary>
        SEE_MASK_NOCLOSEPROCESS = PInvoke.SEE_MASK_NOCLOSEPROCESS,

        /// <summary>
        /// CONNECTNETDRV
        /// </summary>
        SEE_MASK_CONNECTNETDRV = PInvoke.SEE_MASK_CONNECTNETDRV,

        /// <summary>
        /// NOASYNC
        /// </summary>
        SEE_MASK_FLAG_DDEWAIT = PInvoke.SEE_MASK_FLAG_DDEWAIT,

        /// <summary>
        /// DOENVSUBST
        /// </summary>
        SEE_MASK_DOENVSUBST = PInvoke.SEE_MASK_DOENVSUBST,

        /// <summary>
        /// FLAG_NO_UI
        /// </summary>
        SEE_MASK_FLAG_NO_UI = PInvoke.SEE_MASK_FLAG_NO_UI,

        /// <summary>
        /// UNICODE
        /// </summary>
        SEE_MASK_UNICODE = PInvoke.SEE_MASK_UNICODE,

        /// <summary>
        /// NO_CONSOLE
        /// </summary>
        SEE_MASK_NO_CONSOLE = PInvoke.SEE_MASK_NO_CONSOLE,

        /// <summary>
        /// NOZONECHECKS
        /// </summary>
        SEE_MASK_NOZONECHECKS = PInvoke.SEE_MASK_NOZONECHECKS,

        /// <summary>
        /// NOASYNC
        /// </summary>
        SEE_MASK_NOASYNC = PInvoke.SEE_MASK_NOASYNC,

        /// <summary>
        /// HMONITOR
        /// </summary>
        SEE_MASK_HMONITOR = PInvoke.SEE_MASK_HMONITOR,

        /// <summary>
        /// NOQUERYCLASSSTORE
        /// </summary>
        SEE_MASK_NOQUERYCLASSSTORE = PInvoke.SEE_MASK_NOQUERYCLASSSTORE,

        /// <summary>
        /// WAITFORINPUTIDLE
        /// </summary>
        SEE_MASK_WAITFORINPUTIDLE = PInvoke.SEE_MASK_WAITFORINPUTIDLE,

        /// <summary>
        /// FLAG_LOG_USAGE
        /// </summary>
        SEE_MASK_FLAG_LOG_USAGE = PInvoke.SEE_MASK_FLAG_LOG_USAGE,

        /// <summary>
        /// FLAG_HINST_IS_SITE
        /// </summary>
        SEE_MASK_FLAG_HINST_IS_SITE = PInvoke.SEE_MASK_FLAG_HINST_IS_SITE,
    }
}
