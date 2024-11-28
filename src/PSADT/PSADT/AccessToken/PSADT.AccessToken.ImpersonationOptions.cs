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
    }
}
