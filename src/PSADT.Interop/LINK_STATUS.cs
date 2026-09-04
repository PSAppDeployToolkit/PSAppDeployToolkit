namespace PSADT.Interop
{
    /// <summary>
    /// Represents the status of a link (shortcut) file.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1712:Do not prefix enum values with type name", Justification = "This is how it's named in the Win32 API.")]
    public enum LINK_STATUS
    {
        /// <summary>
        /// The link is unresolved and the target file's existence is unknown.
        /// </summary>
        LINK_STATUS_UNRESOLVED = 0,

        /// <summary>
        /// The link is resolved and the target file exists.
        /// </summary>
        LINK_STATUS_RESOLVED = Windows.Win32.PInvoke.LINK_STATUS_RESOLVED,

        /// <summary>
        /// The link is resolved but the target file does not exist.
        /// </summary>
        LINK_STATUS_BROKEN = Windows.Win32.PInvoke.LINK_STATUS_BROKEN,
    }
}
