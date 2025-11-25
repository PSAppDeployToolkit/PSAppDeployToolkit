using System;
using Windows.Win32;

namespace PSADT.LibraryInterfaces
{
    /// <summary>
    /// Specifies flags that control the behavior of authorization context initialization in Authz APIs.
    /// </summary>
    /// <remarks>These flags are used with functions such as AuthzInitializeContextFromSid to modify how the
    /// authorization context is created. Multiple flags can be combined using a bitwise OR operation.</remarks>
    [Flags]
    internal enum AUTHZ_CONTEXT_FLAGS : uint
    {
        /// <summary>
        /// Causes AuthzInitializeContextFromSid to skip all group evaluations. When this flag is used, the context returned contains only the SID specified by the UserSid parameter. The specified SID can be an arbitrary or application-specific SID. Other SIDs can be added to this context by implementing the AuthzComputeGroupsCallback function or by calling the AuthzAddSidsToContext function.
        /// </summary>
        AUTHZ_SKIP_TOKEN_GROUPS = PInvoke.AUTHZ_SKIP_TOKEN_GROUPS,

        /// <summary>
        /// Causes AuthzInitializeContextFromSid to fail if Windows Services For User is not available to retrieve token group information.
        /// </summary>
        AUTHZ_REQUIRE_S4U_LOGON = PInvoke.AUTHZ_REQUIRE_S4U_LOGON,

        /// <summary>
        /// Causes AuthzInitializeContextFromSid to retrieve privileges for the new context. If this function performs an S4U logon, it retrieves privileges from the token. Otherwise, the function retrieves privileges from all SIDs in the context.
        /// </summary>
        AUTHZ_COMPUTE_PRIVILEGES = PInvoke.AUTHZ_COMPUTE_PRIVILEGES,
    }
}
