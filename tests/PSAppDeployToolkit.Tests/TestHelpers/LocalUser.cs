// See LocalPrincipal for why this namespace does not follow the folder.
#pragma warning disable IDE0130

using System.Security.Principal;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Stands in for a derived principal, as the module's own LocalUser and LocalGroup both are.
    /// </summary>
    /// <param name="sid">The account's identifier.</param>
    internal sealed class LocalUser(SecurityIdentifier sid) : LocalPrincipal(sid);
}
