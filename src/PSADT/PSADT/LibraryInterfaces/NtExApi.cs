namespace PSADT.LibraryInterfaces
{
    /// <summary>
    /// System information classes.
    /// </summary>
    internal enum SYSTEM_INFORMATION_CLASS
    {
        /// <summary>
        /// Extended information about the system's handles.
        /// </summary>
        SystemExtendedHandleInformation = 64,

        /// <summary>
        /// Information about the system's processes.
        /// </summary>
        SystemProcessIdInformation = 88,
    }
}
