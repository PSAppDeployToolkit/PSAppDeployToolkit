using System;

namespace PSADT.Interop
{
    /// <summary>
    /// Represents the sub-authentication filter flags used in the MSV1_0 authentication package.
    /// </summary>
    [Flags]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Roslynator", "RCS1135:Declare enum member with zero value (when enum has FlagsAttribute)", Justification = "There's no zero value for this in the SDK.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0182: Avoid unused internal types.", Justification = "This is used across InternalsVisibleTo boundaries.")]
    internal enum MSV_SUB_AUTHENTICATION_FILTER : uint
    {
        /// <summary>
        /// Indicates that the logon is for a guest account.
        /// </summary>
        LOGON_GUEST = Windows.Win32.Security.Authentication.Identity.MSV_SUB_AUTHENTICATION_FILTER.LOGON_GUEST,

        /// <summary>
        /// Indicates that the logon does not require encryption.
        /// </summary>
        LOGON_NOENCRYPTION = Windows.Win32.Security.Authentication.Identity.MSV_SUB_AUTHENTICATION_FILTER.LOGON_NOENCRYPTION,

        /// <summary>
        /// Indicates that the logon is for a cached account.
        /// </summary>
        LOGON_CACHED_ACCOUNT = Windows.Win32.Security.Authentication.Identity.MSV_SUB_AUTHENTICATION_FILTER.LOGON_CACHED_ACCOUNT,

        /// <summary>
        /// Indicates that the logon used an LM password.
        /// </summary>
        LOGON_USED_LM_PASSWORD = Windows.Win32.Security.Authentication.Identity.MSV_SUB_AUTHENTICATION_FILTER.LOGON_USED_LM_PASSWORD,

        /// <summary>
        /// Indicates that the logon has extra SIDs associated with it.
        /// </summary>
        LOGON_EXTRA_SIDS = Windows.Win32.Security.Authentication.Identity.MSV_SUB_AUTHENTICATION_FILTER.LOGON_EXTRA_SIDS,

        /// <summary>
        /// Indicates that the logon has a session key associated with it.
        /// </summary>
        LOGON_SUBAUTH_SESSION_KEY = Windows.Win32.Security.Authentication.Identity.MSV_SUB_AUTHENTICATION_FILTER.LOGON_SUBAUTH_SESSION_KEY,

        /// <summary>
        /// Indicates that the logon is for a server trust account.
        /// </summary>
        LOGON_SERVER_TRUST_ACCOUNT = Windows.Win32.Security.Authentication.Identity.MSV_SUB_AUTHENTICATION_FILTER.LOGON_SERVER_TRUST_ACCOUNT,

        /// <summary>
        /// Indicates that the logon has resource groups associated with it.
        /// </summary>
        LOGON_RESOURCE_GROUPS = Windows.Win32.Security.Authentication.Identity.MSV_SUB_AUTHENTICATION_FILTER.LOGON_RESOURCE_GROUPS,

        /// <summary>
        /// Indicates that the logon has a profile path returned.
        /// </summary>
        LOGON_PROFILE_PATH_RETURNED = Windows.Win32.Security.Authentication.Identity.MSV_SUB_AUTHENTICATION_FILTER.LOGON_PROFILE_PATH_RETURNED,

        /// <summary>
        /// Indicates that the logon is using NTLMv2 authentication.
        /// </summary>
        LOGON_NT_V2 = 0x800,

        /// <summary>
        /// Indicates that the logon is using LMv2 authentication.
        /// </summary>
        LOGON_LM_V2 = 0x1000,

        /// <summary>
        /// Indicates that the logon is using NTLMv2 authentication with extended session security.
        /// </summary>
        LOGON_NTLM_V2 = 0x2000,

        /// <summary>
        /// Indicates that the logon is optimized for performance.
        /// </summary>
        LOGON_OPTIMIZED = 0x4000,

        /// <summary>
        /// Indicates that the logon is for a Winlogon process.
        /// </summary>
        LOGON_WINLOGON = 0x8000,

        /// <summary>
        /// Indicates that the logon is using PKINIT authentication.
        /// </summary>
        LOGON_PKINIT = 0x10000,

        /// <summary>
        /// Indicates that the logon is not optimized for performance.
        /// </summary>
        LOGON_NO_OPTIMIZED = 0x20000,

        /// <summary>
        /// Indicates that the logon is not elevated.
        /// </summary>
        LOGON_NO_ELEVATION = 0x40000,

        /// <summary>
        /// Indicates that the logon is for a managed service account.
        /// </summary>
        LOGON_MANAGED_SERVICE = 0x80000,
    }
}
