using System;

namespace PSADT.Interop
{
    /// <summary>
    /// The STGM constants are flags that indicate conditions for creating and deleting the object and access modes for the object. The STGM constants are included in the IStorage, IStream, and IPropertySetStorage interfaces and in the StgCreateDocfile, StgCreateStorageEx, StgCreateDocfileOnILockBytes, StgOpenStorage, and StgOpenStorageEx functions.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1008:Enums should have zero value", Justification = "There's no zero value for this in the Win32 API.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1028:Enum Storage should be Int32", Justification = "The underlying type is uint to match the Win32 API.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1069:Enums values should not be duplicated", Justification = "These duplications are already within the Win32 API.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Matches the Win32 API naming.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1712:Do not prefix enum values with type name", Justification = "Matches the Win32 API naming.")]
    [Flags]
    public enum STGM : uint
    {
        /// <summary>
        /// Indicates that, in direct mode, each change to a storage or stream element is written as it occurs. This is the default if neither STGM_DIRECT nor STGM_TRANSACTED is specified.
        /// </summary>
        STGM_DIRECT = Windows.Win32.System.Com.STGM.STGM_DIRECT,

        /// <summary>
        /// Indicates that, in transacted mode, changes are buffered and written only if an explicit commit operation is called. To ignore the changes, call the Revert method in the IStream, IStorage, or IPropertyStorage interface. The COM compound file implementation of IStorage does not support transacted streams, which means that streams can be opened only in direct mode, and you cannot revert changes to them, however transacted storages are supported. The compound file, stand-alone, and NTFS file system implementations of IPropertySetStorage similarly do not support transacted, simple property sets because these property sets are stored in streams. However, transactioning of nonsimple property sets, which can be created by specifying the PROPSETFLAG_NONSIMPLE flag in the grfFlags parameter of IPropertySetStorage::Create, are supported.
        /// </summary>
        STGM_TRANSACTED = Windows.Win32.System.Com.STGM.STGM_TRANSACTED,

        /// <summary>
        /// Provides a faster implementation of a compound file in a limited, but frequently used, case. For more information, see the Remarks section.
        /// </summary>
        STGM_SIMPLE = Windows.Win32.System.Com.STGM.STGM_SIMPLE,

        /// <summary>
        /// Indicates that the object is read-only, meaning that modifications cannot be made. For example, if a stream object is opened with STGM_READ, the ISequentialStream::Read method may be called, but the ISequentialStream::Write method may not. Similarly, if a storage object opened with STGM_READ, the IStorage::OpenStream and IStorage::OpenStorage methods may be called, but the IStorage::CreateStream and IStorage::CreateStorage methods may not.
        /// </summary>
        STGM_READ = Windows.Win32.System.Com.STGM.STGM_READ,

        /// <summary>
        /// Enables you to save changes to the object, but does not permit access to its data. The provided implementations of the IPropertyStorage and IPropertySetStorage interfaces do not support this write-only mode.
        /// </summary>
        STGM_WRITE = Windows.Win32.System.Com.STGM.STGM_WRITE,

        /// <summary>
        /// Enables access and modification of object data. For example, if a stream object is created or opened in this mode, it is possible to call both IStream::Read and IStream::Write. Be aware that this constant is not a simple binary OR operation of the STGM_WRITE and STGM_READ elements.
        /// </summary>
        STGM_READWRITE = Windows.Win32.System.Com.STGM.STGM_READWRITE,

        /// <summary>
        /// Specifies that subsequent openings of the object are not denied read or write access. If no flag from the sharing group is specified, this flag is assumed.
        /// </summary>
        STGM_SHARE_DENY_NONE = Windows.Win32.System.Com.STGM.STGM_SHARE_DENY_NONE,

        /// <summary>
        /// Prevents others from subsequently opening the object in STGM_READ mode. It is typically used on a root storage object.
        /// </summary>
        STGM_SHARE_DENY_READ = Windows.Win32.System.Com.STGM.STGM_SHARE_DENY_READ,

        /// <summary>
        /// Prevents others from subsequently opening the object for STGM_WRITE or STGM_READWRITE access. In transacted mode, sharing of STGM_SHARE_DENY_WRITE or STGM_SHARE_EXCLUSIVE can significantly improve performance because they do not require snapshots. For more information about transactioning, see the Remarks section.
        /// </summary>
        STGM_SHARE_DENY_WRITE = Windows.Win32.System.Com.STGM.STGM_SHARE_DENY_WRITE,

        /// <summary>
        /// Prevents others from subsequently opening the object in any mode. Be aware that this value is not a simple bitwise OR operation of the STGM_SHARE_DENY_READ and STGM_SHARE_DENY_WRITE values. In transacted mode, sharing of STGM_SHARE_DENY_WRITE or STGM_SHARE_EXCLUSIVE can significantly improve performance because they do not require snapshots. For more information about transactioning, see the Remarks section.
        /// </summary>
        STGM_SHARE_EXCLUSIVE = Windows.Win32.System.Com.STGM.STGM_SHARE_EXCLUSIVE,

        /// <summary>
        /// Opens the storage object with exclusive access to the most recently committed version. Thus, other users cannot commit changes to the object while you have it open in priority mode. You gain performance benefits for copy operations, but you prevent others from committing changes. Limit the time that objects are open in priority mode. You must specify STGM_DIRECT and STGM_READ with priority mode, and you cannot specify STGM_DELETEONRELEASE. STGM_DELETEONRELEASE is only valid when creating a root object, such as with StgCreateStorageEx. It is not valid when opening an existing root object, such as with StgOpenStorageEx. It is also not valid when creating or opening a subelement, such as with IStorage::OpenStorage.
        /// </summary>
        STGM_PRIORITY = Windows.Win32.System.Com.STGM.STGM_PRIORITY,

        /// <summary>
        /// Indicates that the underlying file is to be automatically destroyed when the root storage object is released. This feature is most useful for creating temporary files. This flag can only be used when creating a root object, such as with StgCreateStorageEx. It is not valid when opening a root object, such as with StgOpenStorageEx, or when creating or opening a subelement, such as with IStorage::CreateStream. It is also not valid to use this flag and the STGM_CONVERT flag simultaneously.
        /// </summary>
        STGM_DELETEONRELEASE = Windows.Win32.System.Com.STGM.STGM_DELETEONRELEASE,

        /// <summary>
        /// Indicates that, in transacted mode, a temporary scratch file is usually used to save modifications until the Commit method is called. Specifying STGM_NOSCRATCH permits the unused portion of the original file to be used as work space instead of creating a new file for that purpose. This does not affect the data in the original file, and in certain cases can result in improved performance. It is not valid to specify this flag without also specifying STGM_TRANSACTED, and this flag may only be used in a root open. For more information about NoScratch mode, see the Remarks section.
        /// </summary>
        STGM_NOSCRATCH = Windows.Win32.System.Com.STGM.STGM_NOSCRATCH,

        /// <summary>
        /// Indicates that an existing storage object or stream should be removed before the new object replaces it. A new object is created when this flag is specified only if the existing object has been successfully removed.
        /// </summary>
        STGM_CREATE = Windows.Win32.System.Com.STGM.STGM_CREATE,

        /// <summary>
        /// Creates the new object while preserving existing data in a stream named "Contents". In the case of a storage object or a byte array, the old data is formatted into a stream regardless of whether the existing file or byte array currently contains a layered storage object. This flag can only be used when creating a root storage object. It cannot be used within a storage object; for example, in IStorage::CreateStream. It is also not valid to use this flag and the STGM_DELETEONRELEASE flag simultaneously.
        /// </summary>
        STGM_CONVERT = Windows.Win32.System.Com.STGM.STGM_CONVERT,

        /// <summary>
        /// Causes the create operation to fail if an existing object with the specified name exists. In this case, STG_E_FILEALREADYEXISTS is returned. This is the default creation mode; that is, if no other create flag is specified, STGM_FAILIFTHERE is implied.
        /// </summary>
        STGM_FAILIFTHERE = Windows.Win32.System.Com.STGM.STGM_FAILIFTHERE,

        /// <summary>
        /// This flag is used when opening a storage object with STGM_TRANSACTED and without STGM_SHARE_EXCLUSIVE or STGM_SHARE_DENY_WRITE. In this case, specifying STGM_NOSNAPSHOT prevents the system-provided implementation from creating a snapshot copy of the file. Instead, changes to the file are written to the end of the file. Unused space is not reclaimed unless consolidation is performed during the commit, and there is only one current writer on the file. When the file is opened in no snapshot mode, another open operation cannot be performed without specifying STGM_NOSNAPSHOT. This flag may only be used in a root open operation. For more information about NoSnapshot mode, see the Remarks section.
        /// </summary>
        STGM_NOSNAPSHOT = Windows.Win32.System.Com.STGM.STGM_NOSNAPSHOT,

        /// <summary>
        /// Supports direct mode for single-writer, multireader file operations. For more information, see the Remarks section.
        /// </summary>
        STGM_DIRECT_SWMR = Windows.Win32.System.Com.STGM.STGM_DIRECT_SWMR,
    }
}
