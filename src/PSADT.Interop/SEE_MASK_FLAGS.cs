using System;

namespace PSADT.Interop
{
    /// <summary>
    /// Flags that indicate the content and validity of the other members of a <see cref="SHELLEXECUTEINFOW"/> structure.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1069:Enums values should not be duplicated", Justification = "These values are precisely as they're defined in the Win32 API.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2217:Do not mark enums with FlagsAttribute", Justification = "This is a bitfield... The analyser is just getting confused due to non-consecutive numbers.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S2344:Enumeration type names should not have \"Flags\" or \"Enum\" suffixes", Justification = "This is OK here, it's a standard Win32 convention.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Roslynator", "RCS1157:Composite enum value contains undefined flag", Justification = "This is just how it's defined in the Win32 SDK...")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0062:Non-flags enums should not be marked with \"FlagsAttribute\"", Justification = "Again, this is how it's represented in the Win32 API.")]
    [Flags]
    internal enum SEE_MASK_FLAGS : uint
    {
        /// <summary>
        /// Use default values.
        /// </summary>
        SEE_MASK_DEFAULT = Windows.Win32.PInvoke.SEE_MASK_DEFAULT,

        /// <summary>
        /// Use the class name given by the lpClass member. If both SEE_MASK_CLASSKEY and SEE_MASK_CLASSNAME are set, the class key is used.
        /// </summary>
        SEE_MASK_CLASSNAME = Windows.Win32.PInvoke.SEE_MASK_CLASSNAME,

        /// <summary>
        /// Use the class key given by the hkeyClass member. If both SEE_MASK_CLASSKEY and SEE_MASK_CLASSNAME are set, the class key is used.
        /// </summary>
        SEE_MASK_CLASSKEY = Windows.Win32.PInvoke.SEE_MASK_CLASSKEY,

        /// <summary>
        /// Use the item identifier list given by the lpIDList member. The lpIDList member must point to an ITEMIDLIST structure.
        /// </summary>
        SEE_MASK_IDLIST = Windows.Win32.PInvoke.SEE_MASK_IDLIST,

        /// <summary>
        /// Use the IContextMenu interface of the selected item's shortcut menu handler. Use either lpFile to identify the item by its file system path or lpIDList to identify the item by its PIDL. This flag allows applications to use ShellExecuteEx to invoke verbs from shortcut menu extensions instead of the static verbs listed in the registry.
        /// </summary>
        SEE_MASK_INVOKEIDLIST = Windows.Win32.PInvoke.SEE_MASK_INVOKEIDLIST,

        /// <summary>
        /// Use the icon given by the hIcon member. This flag cannot be combined with SEE_MASK_HMONITOR.
        /// </summary>
        SEE_MASK_ICON = Windows.Win32.PInvoke.SEE_MASK_ICON,

        /// <summary>
        /// Use the keyboard shortcut given by the dwHotKey member.
        /// </summary>
        SEE_MASK_HOTKEY = Windows.Win32.PInvoke.SEE_MASK_HOTKEY,

        /// <summary>
        /// Use to indicate that the hProcess member receives the process handle. This handle is typically used to allow an application to find out when a process created with ShellExecuteEx terminates. In some cases, such as when execution is satisfied through a DDE conversation, no handle will be returned. The calling application is responsible for closing the handle when it is no longer needed.
        /// </summary>
        SEE_MASK_NOCLOSEPROCESS = Windows.Win32.PInvoke.SEE_MASK_NOCLOSEPROCESS,

        /// <summary>
        /// Validate the share and connect to a drive letter. This enables reconnection of disconnected network drives. The lpFile member is a UNC path of a file on a network.
        /// </summary>
        SEE_MASK_CONNECTNETDRV = Windows.Win32.PInvoke.SEE_MASK_CONNECTNETDRV,

        /// <summary>
        /// Only respected when launching files, does not apply to uris or shell namespace items (e.g. "This PC"). Wait for the async part of the execute operation, (e.g. DDE) to complete before returning. When this applies it ensures the launching operation finishes before returning. Applications that exit immediately after calling ShellExecuteEx should specify this flag. Note, ShellExecuteEx moves its work to a background thread if the caller's threading model is not Apartment. Forcing the call to be synchronous disables that behavior and uses the callers COM apartment. Specifing SEE_MASK_FLAG_HINST_IS_SITE forces synchronous behavior always. If the execute operation is performed on a background thread and the caller did not specify the SEE_MASK_ASYNCOK flag, then the calling thread waits until the new process has started before returning. This typically means that either CreateProcess has been called, the DDE communication has completed, or that the custom execution delegate has notified ShellExecuteEx that it is done. If the SEE_MASK_WAITFORINPUTIDLE flag is specified, then ShellExecuteEx calls WaitForInputIdle and waits for the new process to idle before returning, with a maximum timeout of 1 minute.
        /// </summary>
        SEE_MASK_NOASYNC = Windows.Win32.PInvoke.SEE_MASK_NOASYNC,

        /// <summary>
        /// The same as SEE_MASK_NOASYNC, use of that option is preferred.
        /// </summary>
        SEE_MASK_FLAG_DDEWAIT = Windows.Win32.PInvoke.SEE_MASK_FLAG_DDEWAIT,

        /// <summary>
        /// Expand any environment variables specified in the string given by the lpDirectory or lpFile member.
        /// </summary>
        SEE_MASK_DOENVSUBST = Windows.Win32.PInvoke.SEE_MASK_DOENVSUBST,

        /// <summary>
        /// Do not display user interface (UI) error dialogs that would normally be presented without this option. Security prompts are exempted and will still be shown.
        /// </summary>
        SEE_MASK_FLAG_NO_UI = Windows.Win32.PInvoke.SEE_MASK_FLAG_NO_UI,

        /// <summary>
        /// Use this flag to indicate a Unicode application.
        /// </summary>
        SEE_MASK_UNICODE = Windows.Win32.PInvoke.SEE_MASK_UNICODE,

        /// <summary>
        /// Use to inherit the parent's console for the new process instead of having it create a new console. It is the opposite of using a CREATE_NEW_CONSOLE flag with CreateProcess.
        /// </summary>
        SEE_MASK_NO_CONSOLE = Windows.Win32.PInvoke.SEE_MASK_NO_CONSOLE,

        /// <summary>
        /// The execution can be performed on a background thread and the call should return immediately without waiting for the background thread to finish. Note that in certain cases ShellExecuteEx ignores this flag and waits for the process to finish before returning.
        /// </summary>
        SEE_MASK_ASYNCOK = Windows.Win32.PInvoke.SEE_MASK_ASYNCOK,

        /// <summary>
        /// Use this flag when specifying a monitor on multi-monitor systems. The monitor is specified in the hMonitor member. This flag cannot be combined with SEE_MASK_ICON.
        /// </summary>
        SEE_MASK_HMONITOR = Windows.Win32.PInvoke.SEE_MASK_HMONITOR,

        /// <summary>
        /// Do not perform a zone check. This flag allows ShellExecuteEx to bypass zone checking put into place by IAttachmentExecute.
        /// </summary>
        SEE_MASK_NOZONECHECKS = Windows.Win32.PInvoke.SEE_MASK_NOZONECHECKS,

        /// <summary>
        /// Not used.
        /// </summary>
        SEE_MASK_NOQUERYCLASSSTORE = Windows.Win32.PInvoke.SEE_MASK_NOQUERYCLASSSTORE,

        /// <summary>
        /// After the new process is created, wait for the process to become idle before returning, with a one minute timeout. See WaitForInputIdle for more details.
        /// </summary>
        SEE_MASK_WAITFORINPUTIDLE = Windows.Win32.PInvoke.SEE_MASK_WAITFORINPUTIDLE,

        /// <summary>
        /// Indicates a user initiated launch that enables tracking of frequently used programs and other behaviors.
        /// </summary>
        SEE_MASK_FLAG_LOG_USAGE = Windows.Win32.PInvoke.SEE_MASK_FLAG_LOG_USAGE,

        /// <summary>
        /// The hInstApp member is used to specify the IUnknown of an object that implements IServiceProvider. This object will be used as a site pointer. The site pointer is used to provide services to the ShellExecuteEx function, the handler binding process, and invoked verb handlers. ICreatingProcess can be provided to allow the caller to alter some parameters of the process being created. This flag is supported in Windows 8 and later. When this option is specified the call runs synchronously on the calling thread.
        /// </summary>
        SEE_MASK_FLAG_HINST_IS_SITE = Windows.Win32.PInvoke.SEE_MASK_FLAG_HINST_IS_SITE,
    }
}
