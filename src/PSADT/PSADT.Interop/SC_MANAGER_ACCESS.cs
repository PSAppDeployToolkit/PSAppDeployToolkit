using System;

namespace PSADT.Interop
{
    /// <summary>
    /// Specifies access rights for a service control manager (SCM) object.
    /// </summary>
    /// <remarks>This enumeration defines the access rights that can be granted to a handle for a service
    /// control manager. These rights determine the operations that can be performed on the SCM, such as connecting to
    /// it, creating services, enumerating services, and modifying boot configurations. The values are based on the
    /// Windows API constants.</remarks>
    [Flags]
    internal enum SC_MANAGER_ACCESS : uint
    {
        /// <summary>
        /// Required to connect to the service control manager.
        /// </summary>
        SC_MANAGER_CONNECT = Windows.Win32.PInvoke.SC_MANAGER_CONNECT,

        /// <summary>
        /// Required to call the CreateService function to create a service object and add it to the database.
        /// </summary>
        SC_MANAGER_CREATE_SERVICE = Windows.Win32.PInvoke.SC_MANAGER_CREATE_SERVICE,

        /// <summary>
        /// Required to call the EnumServicesStatus or EnumServicesStatusEx function to list the services that are in the database.
        /// Required to call the NotifyServiceStatusChange function to receive notification when any service is created or deleted.
        /// </summary>
        SC_MANAGER_ENUMERATE_SERVICE = Windows.Win32.PInvoke.SC_MANAGER_ENUMERATE_SERVICE,

        /// <summary>
        /// Required to call the LockServiceDatabase function to acquire a lock on the database.
        /// </summary>
        SC_MANAGER_LOCK = Windows.Win32.PInvoke.SC_MANAGER_LOCK,

        /// <summary>
        /// Required to call the QueryServiceLockStatus function to retrieve the lock status information for the database.
        /// </summary>
        SC_MANAGER_QUERY_LOCK_STATUS = Windows.Win32.PInvoke.SC_MANAGER_QUERY_LOCK_STATUS,

        /// <summary>
        /// Required to call the NotifyBootConfigStatus function.
        /// </summary>
        SC_MANAGER_MODIFY_BOOT_CONFIG = Windows.Win32.PInvoke.SC_MANAGER_MODIFY_BOOT_CONFIG,

        /// <summary>
        /// Includes STANDARD_RIGHTS_REQUIRED, in addition to all access rights in this table.
        /// </summary>
        SC_MANAGER_ALL_ACCESS = Windows.Win32.PInvoke.SC_MANAGER_ALL_ACCESS,
    }
}
