namespace PSADT.LibraryInterfaces
{
    /// <summary>
    /// NtQueryObject information classes.
    /// </summary>
    internal enum OBJECT_INFORMATION_CLASS
    {
        /// <summary>
        /// Information about the name of the object.
        /// </summary>
        ObjectNameInformation = 1,

        /// <summary>
        /// Information about the type of the object.
        /// </summary>
        ObjectTypeInformation = 2,

        /// <summary>
        /// Information about the types of objects in the system.
        /// </summary>
        ObjectTypesInformation = 3,
    }
}
