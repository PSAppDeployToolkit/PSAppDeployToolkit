namespace PSADT.AccessToken
{
    /// <summary>
    /// Enumeration of all available security tokens.
    /// </summary>
    public enum SE_TOKEN
    {
        /// <summary>
        /// The right to attach a primary token to a process.
        /// </summary>
        SeAssignPrimaryTokenPrivilege,

        /// <summary>
        /// The right to generate security audits.
        /// </summary>
        SeAuditPrivilege,

        /// <summary>
        /// The right to back up files and directories.
        /// </summary>
        SeBackupPrivilege,

        /// <summary>
        /// The right to receive notifications of changes to files or directories.
        /// </summary>
        SeChangeNotifyPrivilege,

        /// <summary>
        /// The right to create global objects.
        /// </summary>
        SeCreateGlobalPrivilege,

        /// <summary>
        /// The right to create a pagefile.
        /// </summary>
        SeCreatePagefilePrivilege,

        /// <summary>
        /// The right to create a permanent object.
        /// </summary>
        SeCreatePermanentPrivilege,

        /// <summary>
        /// The right to create symbolic links.
        /// </summary>
        SeCreateSymbolicLinkPrivilege,

        /// <summary>
        /// The right to create a token object.
        /// </summary>
        SeCreateTokenPrivilege,

        /// <summary>
        /// The right to debug programs.
        /// </summary>
        SeDebugPrivilege,

        /// <summary>
        /// The right to impersonate a client after authentication.
        /// </summary>
        SeDelegateSessionUserImpersonatePrivilege,

        /// <summary>
        /// The right to enable computer and user accounts to be trusted for delegation.
        /// </summary>
        SeEnableDelegationPrivilege,

        /// <summary>
        /// The right to impersonate.
        /// </summary>
        SeImpersonatePrivilege,

        /// <summary>
        /// The right to increase the base priority of a process.
        /// </summary>
        SeIncreaseBasePriorityPrivilege,

        /// <summary>
        /// The right to increase quotas.
        /// </summary>
        SeIncreaseQuotaPrivilege,

        /// <summary>
        /// The right to increase non-paged pool quotas.
        /// </summary>
        SeIncreaseWorkingSetPrivilege,

        /// <summary>
        /// The right to load and unload device drivers.
        /// </summary>
        SeLoadDriverPrivilege,

        /// <summary>
        /// The right to lock pages in memory.
        /// </summary>
        SeLockMemoryPrivilege,

        /// <summary>
        /// The right to create a computer account.
        /// </summary>
        SeMachineAccountPrivilege,

        /// <summary>
        /// The right to manage the files on a volume.
        /// </summary>
        SeManageVolumePrivilege,

        /// <summary>
        /// The right to profile single process.
        /// </summary>
        SeProfileSingleProcessPrivilege,

        /// <summary>
        /// The right to relabel.
        /// </summary>
        SeRelabelPrivilege,

        /// <summary>
        /// The right to shut down the system.
        /// </summary>
        SeRemoteShutdownPrivilege,

        /// <summary>
        /// The right to restore files and directories.
        /// </summary>
        SeRestorePrivilege,

        /// <summary>
        /// The right to manage auditing and security log.
        /// </summary>
        SeSecurityPrivilege,

        /// <summary>
        /// The right to shut down the system.
        /// </summary>
        SeShutdownPrivilege,

        /// <summary>
        /// The right to synchronize directory service data.
        /// </summary>
        SeSyncAgentPrivilege,

        /// <summary>
        /// The right to modify the system environment.
        /// </summary>
        SeSystemEnvironmentPrivilege,

        /// <summary>
        /// The right to profile system performance.
        /// </summary>
        SeSystemProfilePrivilege,

        /// <summary>
        /// The right to change the system time.
        /// </summary>
        SeSystemtimePrivilege,

        /// <summary>
        /// The right to take ownership of files or other objects.
        /// </summary>
        SeTakeOwnershipPrivilege,

        /// <summary>
        /// The right to act as part of the operating system.
        /// </summary>
        SeTcbPrivilege,

        /// <summary>
        /// The right to change the time zone.
        /// </summary>
        SeTimeZonePrivilege,

        /// <summary>
        /// The right to access Credential Manager as a trusted caller.
        /// </summary>
        SeTrustedCredManAccessPrivilege,

        /// <summary>
        /// The right to undock a laptop.
        /// </summary>
        SeUndockPrivilege,

        /// <summary>
        /// The right to generate input for the system.
        /// </summary>
        SeUnsolicitedInputPrivilege
    }
}
