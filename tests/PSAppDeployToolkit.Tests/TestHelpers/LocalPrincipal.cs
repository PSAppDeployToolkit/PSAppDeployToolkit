// IDE0130 wants the namespace to follow the folder. It cannot here: the attribute under test finds this type by
// namespace and name, so the namespace is the fixture.
#pragma warning disable IDE0130

using System.Security.Principal;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Stands in for the LocalAccounts module's own type, which the attribute finds by namespace and name.
    /// </summary>
    /// <remarks>
    /// Declared rather than referenced: the real module is not a dependency of this solution, and the attribute reaches
    /// it by reflection, so a type of the same namespace, name and shape is indistinguishable from the real one.
    /// </remarks>
    /// <param name="sid">The account's identifier.</param>
    internal class LocalPrincipal(SecurityIdentifier sid)
    {
        /// <summary>
        /// The account's identifier, which is the only member the attribute reads.
        /// </summary>
        public SecurityIdentifier SID { get; } = sid;
    }
}
