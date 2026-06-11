namespace PSADT.Interop
{
    /// <summary>
    /// Enumeration of all available security privileges.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "These values are precisely as they're defined in the Win32 API.")]
    public enum SE_PRIVILEGE
    {
        /// <summary>
        /// The right to attach a primary token to a process.
        /// </summary>
        SeAssignPrimaryTokenPrivilege = 0,

        /// <summary>
        /// The right to generate security audits.
        /// </summary>
        SeAuditPrivilege = 1,

        /// <summary>
        /// The right to back up files and directories.
        /// </summary>
        SeBackupPrivilege = 2,

        /// <summary>
        /// The right to receive notifications of changes to files or directories.
        /// </summary>
        SeChangeNotifyPrivilege = 3,

        /// <summary>
        /// The right to create global objects.
        /// </summary>
        SeCreateGlobalPrivilege = 4,

        /// <summary>
        /// The right to create a pagefile.
        /// </summary>
        SeCreatePagefilePrivilege = 5,

        /// <summary>
        /// The right to create a permanent object.
        /// </summary>
        SeCreatePermanentPrivilege = 6,

        /// <summary>
        /// The right to create symbolic links.
        /// </summary>
        SeCreateSymbolicLinkPrivilege = 7,

        /// <summary>
        /// The right to create a token object.
        /// </summary>
        SeCreateTokenPrivilege = 8,

        /// <summary>
        /// The right to debug programs.
        /// </summary>
        SeDebugPrivilege = 9,

        /// <summary>
        /// The right to impersonate a client after authentication.
        /// </summary>
        SeDelegateSessionUserImpersonatePrivilege = 10,

        /// <summary>
        /// The right to enable computer and user accounts to be trusted for delegation.
        /// </summary>
        SeEnableDelegationPrivilege = 11,

        /// <summary>
        /// The right to impersonate.
        /// </summary>
        SeImpersonatePrivilege = 12,

        /// <summary>
        /// The right to increase the base priority of a process.
        /// </summary>
        SeIncreaseBasePriorityPrivilege = 13,

        /// <summary>
        /// The right to increase quotas.
        /// </summary>
        SeIncreaseQuotaPrivilege = 14,

        /// <summary>
        /// The right to increase non-paged pool quotas.
        /// </summary>
        SeIncreaseWorkingSetPrivilege = 15,

        /// <summary>
        /// The right to load and unload device drivers.
        /// </summary>
        SeLoadDriverPrivilege = 16,

        /// <summary>
        /// The right to lock pages in memory.
        /// </summary>
        SeLockMemoryPrivilege = 17,

        /// <summary>
        /// The right to create a computer account.
        /// </summary>
        SeMachineAccountPrivilege = 18,

        /// <summary>
        /// The right to manage the files on a volume.
        /// </summary>
        SeManageVolumePrivilege = 19,

        /// <summary>
        /// The right to profile single process.
        /// </summary>
        SeProfileSingleProcessPrivilege = 20,

        /// <summary>
        /// The right to relabel.
        /// </summary>
        SeRelabelPrivilege = 21,

        /// <summary>
        /// The right to shut down the system.
        /// </summary>
        SeRemoteShutdownPrivilege = 22,

        /// <summary>
        /// The right to restore files and directories.
        /// </summary>
        SeRestorePrivilege = 23,

        /// <summary>
        /// The right to manage auditing and security log.
        /// </summary>
        SeSecurityPrivilege = 24,

        /// <summary>
        /// The right to shut down the system.
        /// </summary>
        SeShutdownPrivilege = 25,

        /// <summary>
        /// The right to synchronize directory service data.
        /// </summary>
        SeSyncAgentPrivilege = 26,

        /// <summary>
        /// The right to modify the system environment.
        /// </summary>
        SeSystemEnvironmentPrivilege = 27,

        /// <summary>
        /// The right to profile system performance.
        /// </summary>
        SeSystemProfilePrivilege = 28,

        /// <summary>
        /// The right to change the system time.
        /// </summary>
        SeSystemtimePrivilege = 29,

        /// <summary>
        /// The right to take ownership of files or other objects.
        /// </summary>
        SeTakeOwnershipPrivilege = 30,

        /// <summary>
        /// The right to act as part of the operating system.
        /// </summary>
        SeTcbPrivilege = 31,

        /// <summary>
        /// The right to change the time zone.
        /// </summary>
        SeTimeZonePrivilege = 32,

        /// <summary>
        /// The right to access Credential Manager as a trusted caller.
        /// </summary>
        SeTrustedCredManAccessPrivilege = 33,

        /// <summary>
        /// The right to undock a laptop.
        /// </summary>
        SeUndockPrivilege = 34,

        /// <summary>
        /// The right to generate input for the system.
        /// </summary>
        SeUnsolicitedInputPrivilege = 35
    }
}
