using System;

namespace PSADT.Interop
{
    /// <summary>
    /// Specifies options for resolving shell links using the Shell Link Resolver. This enumeration provides flags that
    /// control the behavior of link resolution, such as whether to display UI, update link information, use distributed
    /// link tracking, or invoke Windows Installer.
    /// </summary>
    /// <remarks>Use the SLR_FLAGS enumeration to customize how shell links are resolved in Windows. Flags can
    /// be combined to achieve specific resolution behaviors, such as suppressing UI dialogs, disabling link tracking,
    /// or updating link metadata. Some flags are only supported on certain Windows versions; refer to individual flag
    /// documentation for details.</remarks>
    [Flags]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1008:Enums should have zero value", Justification = "This is typed just as it is in the Win32 API.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "This is typed just as it is in the Win32 API.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2217:Do not mark enums with FlagsAttribute", Justification = "Clearly this is a bitfield... C'mon Intellisense...")]
    public enum SLR_FLAGS
    {
        /// <summary>
        /// Specifies that no special link resolution options are applied.
        /// </summary>
        SLR_NONE = Windows.Win32.UI.Shell.SLR_FLAGS.SLR_NONE,

        /// <summary>
        /// Do not display a dialog box if the link cannot be resolved. When SLR_NO_UI is set, the high-order word of fFlags can be set to a time-out value that specifies the maximum amount of time to be spent resolving the link. The function returns if the link cannot be resolved within the time-out duration. If the high-order word is set to zero, the time-out duration will be set to the default value of 3,000 milliseconds (3 seconds). To specify a value, set the high word of fFlags to the desired time-out duration, in milliseconds.
        /// </summary>
        SLR_NO_UI = Windows.Win32.UI.Shell.SLR_FLAGS.SLR_NO_UI,

        /// <summary>
        /// Not used.
        /// </summary>
        SLR_ANY_MATCH = Windows.Win32.UI.Shell.SLR_FLAGS.SLR_ANY_MATCH,

        /// <summary>
        /// If the link object has changed, update its path and list of identifiers. If SLR_UPDATE is set, you do not need to call IPersistFile::IsDirty to determine whether the link object has changed.
        /// </summary>
        SLR_UPDATE = Windows.Win32.UI.Shell.SLR_FLAGS.SLR_UPDATE,

        /// <summary>
        /// Do not update the link information.
        /// </summary>
        SLR_NOUPDATE = Windows.Win32.UI.Shell.SLR_FLAGS.SLR_NOUPDATE,

        /// <summary>
        /// Do not execute the search heuristics.
        /// </summary>
        SLR_NOSEARCH = Windows.Win32.UI.Shell.SLR_FLAGS.SLR_NOSEARCH,

        /// <summary>
        /// Do not use distributed link tracking.
        /// </summary>
        SLR_NOTRACK = Windows.Win32.UI.Shell.SLR_FLAGS.SLR_NOTRACK,

        /// <summary>
        /// Disable distributed link tracking. By default, distributed link tracking tracks removable media across multiple devices based on the volume name. It also uses the UNC path to track remote file systems whose drive letter has changed. Setting SLR_NOLINKINFO disables both types of tracking.
        /// </summary>
        SLR_NOLINKINFO = Windows.Win32.UI.Shell.SLR_FLAGS.SLR_NOLINKINFO,

        /// <summary>
        /// Call the Windows Installer.
        /// </summary>
        SLR_INVOKE_MSI = Windows.Win32.UI.Shell.SLR_FLAGS.SLR_INVOKE_MSI,

        /// <summary>
        /// Windows XP and later.
        /// </summary>
        SLR_NO_UI_WITH_MSG_PUMP = Windows.Win32.UI.Shell.SLR_FLAGS.SLR_NO_UI_WITH_MSG_PUMP,

        /// <summary>
        /// Windows 7 and later. Offer the option to delete the shortcut when this method is unable to resolve it, even if the shortcut is not a shortcut to a file.
        /// </summary>
        SLR_OFFER_DELETE_WITHOUT_FILE = Windows.Win32.UI.Shell.SLR_FLAGS.SLR_OFFER_DELETE_WITHOUT_FILE,

        /// <summary>
        /// Windows 7 and later. Report as dirty if the target is a known folder and the known folder was redirected. This only works if the original target path was a file system path or ID list and not an aliased known folder ID list.
        /// </summary>
        SLR_KNOWNFOLDER = Windows.Win32.UI.Shell.SLR_FLAGS.SLR_KNOWNFOLDER,

        /// <summary>
        /// Windows 7 and later. Resolve the computer name in UNC targets that point to a local computer. This value is used with SLDF_KEEP_LOCAL_IDLIST_FOR_UNC_TARGET.
        /// </summary>
        SLR_MACHINE_IN_LOCAL_TARGET = Windows.Win32.UI.Shell.SLR_FLAGS.SLR_MACHINE_IN_LOCAL_TARGET,

        /// <summary>
        /// Windows 7 and later. Update the computer GUID and user SID if necessary.
        /// </summary>
        SLR_UPDATE_MACHINE_AND_SID = Windows.Win32.UI.Shell.SLR_FLAGS.SLR_UPDATE_MACHINE_AND_SID,

        /// <summary>
        /// Specifies that no object identifier is set when resolving a shell link.
        /// </summary>
        /// <remarks>Use this flag to indicate that the shell link should be resolved without requiring an
        /// object ID. This can be useful when object IDs are not available or not needed for the resolution
        /// process.</remarks>
        SLR_NO_OBJECT_ID = Windows.Win32.UI.Shell.SLR_FLAGS.SLR_NO_OBJECT_ID,
    }
}
