namespace PSADT.PowerShellHost
{
    /// <summary>
    /// Specifies the version of PowerShell to use.
    /// </summary>
    public enum PSEdition
    {
        /// <summary>
        /// Use the default PowerShell version.
        /// </summary>
        Default,

        /// <summary>
        /// Use Windows PowerShell (version 5.1 or earlier).
        /// </summary>
        WindowsPowerShell,

        /// <summary>
        /// Use PowerShell Core (version 6.0 or later).
        /// </summary>
        PowerShellCore
    }

    /// <summary>
    /// Represents the architecture of the PowerShell host.
    /// </summary>
    public enum PSArchitecture
    {
        /// <summary>
        /// Use the architecture of the current process.
        /// </summary>
        CurrentProcess,

        /// <summary>
        /// Use the x64 architecture.
        /// </summary>
        X64,

        /// <summary>
        /// Use the x86 architecture.
        /// </summary>
        X86
    }
}
