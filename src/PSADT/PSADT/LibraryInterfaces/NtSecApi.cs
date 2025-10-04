using System;

namespace PSADT.LibraryInterfaces
{
    /// <summary>
    /// Defines the access rights for interacting with the Local Security Authority (LSA) policy.
    /// </summary>
    /// <remarks>https://learn.microsoft.com/en-us/windows/win32/secmgmt/policy-object-access-rights</remarks>
    [Flags]
    internal enum LSA_POLICY_ACCESS : uint
    {
        /// <summary>
        /// This access type is needed to read the target system's miscellaneous security policy information. This includes the default quota, auditing, server state and role information, and trust information. This access type is also needed to enumerate trusted domains, accounts, and privileges.
        /// </summary>
        POLICY_VIEW_LOCAL_INFORMATION = Windows.Win32.PInvoke.POLICY_VIEW_LOCAL_INFORMATION,

        /// <summary>
        /// This access type is needed to view sensitive information, such as the names of accounts established for trusted domain relationships.
        /// </summary>
        POLICY_GET_PRIVATE_INFORMATION = Windows.Win32.PInvoke.POLICY_GET_PRIVATE_INFORMATION,

        /// <summary>
        /// This access type is needed to change the account domain or primary domain information.
        /// </summary>
        POLICY_TRUST_ADMIN = Windows.Win32.PInvoke.POLICY_TRUST_ADMIN,

        /// <summary>
        /// Set the default system quotas that are applied to user accounts.
        /// </summary>
        POLICY_SET_DEFAULT_QUOTA_LIMITS = Windows.Win32.PInvoke.POLICY_SET_DEFAULT_QUOTA_LIMITS,

        /// <summary>
        /// This access type is needed to create a new Private Data object.
        /// </summary>
        POLICY_CREATE_SECRET = Windows.Win32.PInvoke.POLICY_CREATE_SECRET,

        /// <summary>
        /// This access type is needed to create a new Account object.
        /// </summary>
        POLICY_CREATE_ACCOUNT = Windows.Win32.PInvoke.POLICY_CREATE_ACCOUNT,

        /// <summary>
        /// This access type is needed to update the auditing requirements of the system.
        /// </summary>
        POLICY_SET_AUDIT_REQUIREMENTS = Windows.Win32.PInvoke.POLICY_SET_AUDIT_REQUIREMENTS,

        /// <summary>
        /// This access type is needed to change the characteristics of the audit trail such as its maximum size or the retention period for audit records, or to clear the log.
        /// </summary>
        POLICY_AUDIT_LOG_ADMIN = Windows.Win32.PInvoke.POLICY_AUDIT_LOG_ADMIN,

        /// <summary>
        /// This access type is needed to view audit trail or audit requirements information.
        /// </summary>
        POLICY_VIEW_AUDIT_INFORMATION = Windows.Win32.PInvoke.POLICY_VIEW_AUDIT_INFORMATION,

        /// <summary>
        /// This access type is needed to modify the server state or role (master/replica) information.It is also needed to change the replica source and account name information.
        /// </summary>
        POLICY_SERVER_ADMIN = Windows.Win32.PInvoke.POLICY_SERVER_ADMIN,

        /// <summary>
        /// This access type is needed to translate between names and SIDs.
        /// </summary>
        POLICY_LOOKUP_NAMES = Windows.Win32.PInvoke.POLICY_LOOKUP_NAMES,

        /// <summary>
        /// Not yet supported.
        /// </summary>
        POLICY_CREATE_PRIVILEGE = Windows.Win32.PInvoke.POLICY_CREATE_PRIVILEGE,

        /// <summary>
        /// Represents a generic read access right that combines standard read permissions with the ability to view audit information and retrieve private policy information.
        /// </summary>
        GENERIC_READ = Windows.Win32.Storage.FileSystem.FILE_ACCESS_RIGHTS.STANDARD_RIGHTS_READ | POLICY_VIEW_AUDIT_INFORMATION | POLICY_GET_PRIVATE_INFORMATION,

        /// <summary>
        /// Represents a combination of access rights used to specify generic write permissions.
        /// </summary>
        GENERIC_WRITE = Windows.Win32.Storage.FileSystem.FILE_ACCESS_RIGHTS.STANDARD_RIGHTS_WRITE | POLICY_TRUST_ADMIN | POLICY_CREATE_ACCOUNT | POLICY_CREATE_SECRET | POLICY_CREATE_PRIVILEGE | POLICY_SET_DEFAULT_QUOTA_LIMITS | POLICY_SET_AUDIT_REQUIREMENTS | POLICY_AUDIT_LOG_ADMIN | POLICY_SERVER_ADMIN,

        /// <summary>
        /// Represents a combination of access rights that allow standard execute permissions, viewing local policy information, and looking up policy names.
        /// </summary>
        GENERIC_EXECUTE = Windows.Win32.Storage.FileSystem.FILE_ACCESS_RIGHTS.STANDARD_RIGHTS_EXECUTE | POLICY_VIEW_LOCAL_INFORMATION | POLICY_LOOKUP_NAMES,

        /// <summary>
        /// Represents the access rights required to perform all policy-related operations.
        /// </summary>
        POLICY_ALL_ACCESS = Windows.Win32.Storage.FileSystem.FILE_ACCESS_RIGHTS.STANDARD_RIGHTS_REQUIRED | POLICY_VIEW_LOCAL_INFORMATION | POLICY_VIEW_AUDIT_INFORMATION | POLICY_GET_PRIVATE_INFORMATION | POLICY_TRUST_ADMIN | POLICY_CREATE_ACCOUNT | POLICY_CREATE_SECRET | POLICY_CREATE_PRIVILEGE | POLICY_SET_DEFAULT_QUOTA_LIMITS | POLICY_SET_AUDIT_REQUIREMENTS | POLICY_AUDIT_LOG_ADMIN | POLICY_SERVER_ADMIN | POLICY_LOOKUP_NAMES,
    }
}
