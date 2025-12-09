using System;

namespace PSADT.LibraryInterfaces
{
    /// <summary>
    /// Bitmask of flags that specify object handle attributes.
    /// </summary>
    [Flags]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "These values are precisely as they're defined in the Win32 API.")]
    public enum OBJECT_ATTRIBUTES : uint
    {
        /// <summary>
        /// This handle is protected from closing.
        /// </summary>
        OBJ_PROTECT_CLOSE = 1,

        /// <summary>
        /// This handle can be inherited by child processes of the current process.
        /// </summary>
        OBJ_INHERIT = Windows.Win32.Foundation.OBJECT_ATTRIBUTE_FLAGS.OBJ_INHERIT,

        /// <summary>
        /// Undocumented.
        /// </summary>
        OBJ_AUDIT_OBJECT_CLOSE = 4,

        /// <summary>
        /// This flag prevents upgrading rights when duplicating the handle.
        /// </summary>
        OBJ_NO_RIGHTS_UPGRADE = 8,

        /// <summary>
        /// This flag only applies to objects that are named within the object manager. By default, such objects are deleted when all open handles to them are closed. If this flag is specified, the object is not deleted when all open handles are closed. Drivers can use the ZwMakeTemporaryObject routine to make a permanent object non-permanent.
        /// </summary>
        OBJ_PERMANENT = Windows.Win32.Foundation.OBJECT_ATTRIBUTE_FLAGS.OBJ_PERMANENT,

        /// <summary>
        /// If this flag is set and the OBJECT_ATTRIBUTES structure is passed to a routine that creates an object, the object can be accessed exclusively. That is, once a process opens such a handle to the object, no other processes can open handles to this object. If this flag is set and the OBJECT_ATTRIBUTES structure is passed to a routine that creates an object handle, the caller is requesting exclusive access to the object for the process context that the handle was created in. This request can be granted only if the OBJ_EXCLUSIVE flag was set when the object was created.
        /// </summary>
        OBJ_EXCLUSIVE = Windows.Win32.Foundation.OBJECT_ATTRIBUTE_FLAGS.OBJ_EXCLUSIVE,

        /// <summary>
        /// If this flag is specified, a case-insensitive comparison is used when matching the name pointed to by the ObjectName member against the names of existing objects. Otherwise, object names are compared using the default system settings.
        /// </summary>
        OBJ_CASE_INSENSITIVE = Windows.Win32.Foundation.OBJECT_ATTRIBUTE_FLAGS.OBJ_CASE_INSENSITIVE,

        /// <summary>
        /// If this flag is specified, by using the object handle, to a routine that creates objects and if that object already exists, the routine should open that object. Otherwise, the routine creating the object returns an NTSTATUS code of STATUS_OBJECT_NAME_COLLISION.
        /// </summary>
        OBJ_OPENIF = Windows.Win32.Foundation.OBJECT_ATTRIBUTE_FLAGS.OBJ_OPENIF,

        /// <summary>
        /// If an object handle, with this flag set, is passed to a routine that opens objects and if the object is a symbolic link object, the routine should open the symbolic link object itself, rather than the object that the symbolic link refers to (which is the default behavior).
        /// </summary>
        OBJ_OPENLINK = Windows.Win32.Foundation.OBJECT_ATTRIBUTE_FLAGS.OBJ_OPENLINK,

        /// <summary>
        /// The handle is created in system process context and can only be accessed from kernel mode.
        /// </summary>
        OBJ_KERNEL_HANDLE = Windows.Win32.Foundation.OBJECT_ATTRIBUTE_FLAGS.OBJ_KERNEL_HANDLE,

        /// <summary>
        /// The routine that opens the handle should enforce all access checks for the object, even if the handle is being opened in kernel mode.
        /// </summary>
        OBJ_FORCE_ACCESS_CHECK = Windows.Win32.Foundation.OBJECT_ATTRIBUTE_FLAGS.OBJ_FORCE_ACCESS_CHECK,

        /// <summary>
        /// A device map is a mapping between DOS device names and devices in the system, and is used when resolving DOS names. Separate device maps exists for each user in the system, and users can manage their own device maps. Typically during impersonation, the impersonated user's device map would be used. However, when this flag is set, the process user's device map is used instead.
        /// </summary>
        OBJ_IGNORE_IMPERSONATED_DEVICEMAP = Windows.Win32.Foundation.OBJECT_ATTRIBUTE_FLAGS.OBJ_IGNORE_IMPERSONATED_DEVICEMAP,

        /// <summary>
        /// If this flag is set, no reparse points will be followed when parsing the name of the associated object. If any reparses are encountered the attempt will fail and return an STATUS_REPARSE_POINT_ENCOUNTERED result. This can be used to determine if there are any reparse points in the object's path, in security scenarios.
        /// </summary>
        OBJ_DONT_REPARSE = Windows.Win32.Foundation.OBJECT_ATTRIBUTE_FLAGS.OBJ_DONT_REPARSE,

        /// <summary>
        /// Reserved.
        /// </summary>
        OBJ_VALID_ATTRIBUTES = Windows.Win32.Foundation.OBJECT_ATTRIBUTE_FLAGS.OBJ_VALID_ATTRIBUTES,
    }
}
