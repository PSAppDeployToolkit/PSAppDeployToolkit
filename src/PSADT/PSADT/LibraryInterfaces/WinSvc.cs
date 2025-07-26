using System;

namespace PSADT.LibraryInterfaces
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

    /// <summary>
    /// Specifies the access rights for a service, used to control the level of access a caller has to a service.
    /// </summary>
    /// <remarks>These access rights are used with service management functions to perform various operations
    /// on services, such as querying status, changing configuration, or starting and stopping services. Each right
    /// corresponds to specific functions that require that level of access. For example, <see
    /// cref="SERVICE_QUERY_STATUS"/> is needed to query the status of a service, while <see cref="SERVICE_START"/> is
    /// required to start a service.</remarks>
    [Flags]
    internal enum SERVICE_ACCESS_RIGHTS : uint
    {
        /// <summary>
        /// Required to call the QueryServiceConfig and QueryServiceConfig2 functions to query the service configuration.
        /// </summary>
        SERVICE_QUERY_CONFIG = Windows.Win32.PInvoke.SERVICE_QUERY_CONFIG,

        /// <summary>
        /// Required to call the ChangeServiceConfig or ChangeServiceConfig2 function to change the service configuration. Because this grants the caller the right to change the executable file that the system runs, it should be granted only to administrators.
        /// </summary>
        SERVICE_CHANGE_CONFIG = Windows.Win32.PInvoke.SERVICE_CHANGE_CONFIG,

        /// <summary>
        /// Required to call the QueryServiceStatus or QueryServiceStatusEx function to ask the service control manager about the status of the service.
        /// Required to call the NotifyServiceStatusChange function to receive notification when a service changes status.
        /// </summary>
        SERVICE_QUERY_STATUS = Windows.Win32.PInvoke.SERVICE_QUERY_STATUS,

        /// <summary>
        /// Required to call the EnumDependentServices function to enumerate all the services dependent on the service.
        /// </summary>
        SERVICE_ENUMERATE_DEPENDENTS = Windows.Win32.PInvoke.SERVICE_ENUMERATE_DEPENDENTS,

        /// <summary>
        /// Required to call the StartService function to start the service.
        /// </summary>
        SERVICE_START = Windows.Win32.PInvoke.SERVICE_START,

        /// <summary>
        /// Required to call the ControlService function to stop the service.
        /// </summary>
        SERVICE_STOP = Windows.Win32.PInvoke.SERVICE_STOP,

        /// <summary>
        /// Required to call the ControlService function to pause or continue the service.
        /// </summary>
        SERVICE_PAUSE_CONTINUE = Windows.Win32.PInvoke.SERVICE_PAUSE_CONTINUE,

        /// <summary>
        /// Required to call the ControlService function to ask the service to report its status immediately.
        /// </summary>
        SERVICE_INTERROGATE = Windows.Win32.PInvoke.SERVICE_INTERROGATE,

        /// <summary>
        /// Required to call the ControlService function to specify a user-defined control code.
        /// </summary>
        SERVICE_USER_DEFINED_CONTROL = Windows.Win32.PInvoke.SERVICE_USER_DEFINED_CONTROL,

        /// <summary>
        /// Includes STANDARD_RIGHTS_REQUIRED in addition to all access rights in this table.
        /// </summary>
        SERVICE_ALL_ACCESS = Windows.Win32.PInvoke.SERVICE_ALL_ACCESS,
    }
}
