using System.Collections.Generic;
using PSADT.PInvoke;

namespace PSADT.AccessToken
{
    /// <summary>
    /// Represents the options for impersonation.
    /// </summary>
    public class ImpersonationOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether to reduce privileges when impersonating an administrator.
        /// </summary>
        public bool ReduceAdminPrivileges { get; set; } = true;

        /// <summary>
        /// Gets or sets the list of privileges to enable when impersonating.
        /// </summary>
        public List<TokenPrivilege> PrivilegesToEnable { get; set; } = new List<TokenPrivilege>();

        /// <summary>
        /// Gets or sets the list of privileges to disable when impersonating.
        /// </summary>
        public List<TokenPrivilege> PrivilegesToDisable { get; set; } = new List<TokenPrivilege>();

        /// <summary>
        /// Gets or sets a value indicating whether to allow impersonation of the SYSTEM account.
        /// </summary>
        public bool AllowSystemImpersonation { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether to allow non-admin users to impersonate admin accounts.
        /// NOTE: This is not implemented. It is a placeholder for someone to implement this functionality, on their own, if they choose.
        /// WARNING: While this is possible, it is not reccomended. This allows non-admin users to impersonate admin accounts, which can
        ///          then impersonate the SYSTEM account, and this can be a security risk.
        /// CAUTION: If you do implement this option, on your own, use it only when absolutely necessary and understand the security implications.
        /// </summary>
        public bool AllowNonAdminToAdminImpersonation { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether to bypass AppLocker rules and Software Restriction Policies.
        /// If true, the system does not check AppLocker rules or apply Software Restriction Policies.
        /// This disables checks for all four AppLocker rule collections: Executable, Windows Installer, Script, and DLL.
        /// CAUTION: Use this option only when absolutely necessary and understand the security implications.
        /// </summary>
        public bool DoNotCheckAppLockerRulesOrApplySRP { get; set; } = false;
    }
}
