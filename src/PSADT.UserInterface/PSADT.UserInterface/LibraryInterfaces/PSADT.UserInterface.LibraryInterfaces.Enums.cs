namespace PSADT.UserInterface.LibraryInterfaces
{
    /// <summary>
    /// Flags for SHGetFileInfo function.
    /// </summary>
    [Flags]
    internal enum SHGFI_FLAGS : uint
    {
        /// <summary>
        /// Default value that represents the large icon.
        /// </summary>
        SHGFI_LARGEICON = 0x00000000,

        /// <summary>
        /// Retrieve the small icon.
        /// </summary>
        SHGFI_SMALLICON = 0x00000001,

        /// <summary>
        /// Retrieve the icon for an open object.
        /// </summary>
        SHGFI_OPENICON = 0x00000002,

        /// <summary>
        /// Retrieve the shell-sized icon.
        /// </summary>
        SHGFI_SHELLICONSIZE = 0x00000004,

        /// <summary>
        /// Indicates that the pszPath parameter is actually a PIDL rather than a path.
        /// </summary>
        SHGFI_PIDL = 0x00000008,

        /// <summary>
        /// Retrieve the handle to the icon that represents the file.
        /// </summary>
        SHGFI_ICON = 0x00000100,

        /// <summary>
        /// Retrieve the display name for the file.
        /// </summary>
        SHGFI_DISPLAYNAME = 0x00000200,

        /// <summary>
        /// Retrieve the type name for the file.
        /// </summary>
        SHGFI_TYPENAME = 0x00000400,

        /// <summary>
        /// Retrieve the file attributes.
        /// </summary>
        SHGFI_ATTRIBUTES = 0x00000800,

        /// <summary>
        /// Retrieve the name of the icon location; for example, the file path of the icon.
        /// </summary>
        SHGFI_ICONLOCATION = 0x00001000,

        /// <summary>
        /// Retrieve the executable type (used with executable files).
        /// </summary>
        SHGFI_EXETYPE = 0x00002000,

        /// <summary>
        /// Retrieve the index of the system image list.
        /// </summary>
        SHGFI_SYSICONINDEX = 0x00004000,

        /// <summary>
        /// Add the link overlay for shortcut files.
        /// </summary>
        SHGFI_LINKOVERLAY = 0x00008000,

        /// <summary>
        /// Retrieve the icon for a selected item.
        /// </summary>
        SHGFI_SELECTED = 0x00010000,

        /// <summary>
        /// Retrieve only the attributes specified in the dwFileAttributes parameter.
        /// </summary>
        SHGFI_ATTR_SPECIFIED = 0x00020000,
    }
}
