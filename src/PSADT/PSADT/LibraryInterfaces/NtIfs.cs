namespace PSADT.LibraryInterfaces
{
    /// <summary>
    /// NtQueryObject information classes.
    /// </summary>
    internal enum OBJECT_INFORMATION_CLASS
    {
        /// <summary>
        /// Specifies that basic information about a Windows object should be retrieved.
        /// Data type: OBJECT_BASIC_INFORMATION
        /// </summary>
        ObjectBasicInformation = Windows.Wdk.Foundation.OBJECT_INFORMATION_CLASS.ObjectBasicInformation,

        /// <summary>
        /// Information about the name of the object.
        /// Data type: OBJECT_NAME_INFORMATION
        /// </summary>
        ObjectNameInformation = 1,

        /// <summary>
        /// Information about the type of the object.
        /// Data type: OBJECT_TYPE_INFORMATION
        /// </summary>
        ObjectTypeInformation = Windows.Wdk.Foundation.OBJECT_INFORMATION_CLASS.ObjectTypeInformation,

        /// <summary>
        /// Information about the types of objects in the system.
        /// Data type: OBJECT_TYPES_INFORMATION
        /// </summary>
        ObjectTypesInformation = 3,

        /// <summary>
        /// Indicates that the handle information includes flag details for the associated object.
        /// Data type: OBJECT_HANDLE_FLAG_INFORMATION
        /// </summary>
        ObjectHandleFlagInformation,

        /// <summary>
        /// Represents session-related information for an object, such as authentication details or session state (requires SeTcbPrivilege).
        /// </summary>
        ObjectSessionInformation,

        /// <summary>
        /// Represents information about an object session, including details relevant to the object's current state or
        /// context (requires SeTcbPrivilege).
        /// </summary>
        ObjectSessionObjectInformation,

        /// <summary>
        /// Provides trace information related to object set references for diagnostic or logging purposes.
        /// </summary>
        ObjectSetRefTraceInformation,

        /// <summary>
        /// Represents the maximum object information class supported by the system.
        /// </summary>
        MaxObjectInfoClass,
    }
}
