using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.DirectoryServices;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using PSADT.Foundation;
using PSADT.LibraryInterfaces;
using PSADT.LibraryInterfaces.Extensions;
using PSADT.LibraryInterfaces.SafeHandles;
using PSADT.ProcessManagement;
using PSADT.Security;
using Windows.Win32;
using Windows.Win32.Security.Authentication.Identity;

namespace PSADT.AccountManagement
{
    /// <summary>
    /// Utility methods for working with Windows accounts and groups.
    /// </summary>
    public static class AccountUtilities
    {
        /// <summary>
        /// Static constructor for readonly constant values.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1810:Initialize reference type static fields inline", Justification = "The static constructor is very much needed here.")]
        static AccountUtilities()
        {
            // Cache information about the current user.
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                CallerIsAdmin = new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
                CallerUsername = new(identity.Name);
                CallerSid = identity.User!;
            }

            // Build out process/session id information.
            _ = Kernel32.ProcessIdToSessionId(CallerProcessId = PInvoke.GetCurrentProcessId(), out CallerSessionId);

            // Retrieve the local account domain SID.
            _ = AdvApi32.LsaOpenPolicy(null, new() { Length = (uint)Marshal.SizeOf<LSA_OBJECT_ATTRIBUTES>() }, LSA_POLICY_ACCESS.POLICY_VIEW_LOCAL_INFORMATION, out LsaCloseSafeHandle hPolicy);
            using (hPolicy)
            {
                _ = AdvApi32.LsaQueryInformationPolicy(hPolicy, POLICY_INFORMATION_CLASS.PolicyAccountDomainInformation, out SafeLsaFreeMemoryHandle buf);
                using (buf)
                {
                    ref readonly POLICY_ACCOUNT_DOMAIN_INFO policyAccountDomainInfo = ref buf.AsReadOnlyStructure<POLICY_ACCOUNT_DOMAIN_INFO>();
                    LocalAccountDomainSid = policyAccountDomainInfo.DomainSid.ToSecurityIdentifier();
                }
            }

            // Initialize the lookup table for well-known SIDs, skipping ones that don't construct.
            Array wellKnownSidTypes = typeof(WellKnownSidType).GetEnumValues();
            Dictionary<WellKnownSidType, SecurityIdentifier> wellKnownSids = new(wellKnownSidTypes.Length);
            foreach (WellKnownSidType wellKnownSidType in wellKnownSidTypes)
            {
                if (wellKnownSids.ContainsKey(wellKnownSidType) || wellKnownSidType == WellKnownSidType.LogonIdsSid || (int)wellKnownSidType == 80 || (int)wellKnownSidType == 83)  // WinLocalLogonSid/WinApplicationPackageAuthoritySid.
                {
                    continue;
                }
                wellKnownSids.Add(wellKnownSidType, new(wellKnownSidType, LocalAccountDomainSid));
            }
            WellKnownSidLookupTable = new(wellKnownSids);

            // Determine if the caller is the local system account.
            CallerIsLocalSystem = CallerSid.IsWellKnown(WellKnownSidType.LocalSystemSid);
            CallerIsSystemInteractive = CallerIsLocalSystem && Environment.UserInteractive;
            CallerUsingServiceUI = ProcessUtilities.GetParentProcesses().Any(static p => p.ProcessName.Equals("ServiceUI", StringComparison.OrdinalIgnoreCase));

            // Generate a RunAsActiveUser object for the current user.
            CallerRunAsActiveUser = new(CallerUsername, CallerSid, CallerSessionId, CallerIsAdmin);
        }

        /// <summary>
        /// Retrieves the <see cref="SecurityIdentifier"/> associated with the specified well-known SID type.
        /// </summary>
        /// <remarks>Well-known SIDs are predefined identifiers for common security principals, such as
        /// "Everyone" or "Local System." Use this method to obtain the <see cref="SecurityIdentifier"/> for a specific
        /// well-known SID type.</remarks>
        /// <param name="wellKnownSidType">The type of the well-known SID to retrieve. This must be a valid <see cref="WellKnownSidType"/> value.</param>
        /// <returns>A <see cref="SecurityIdentifier"/> representing the specified well-known SID type.</returns>
        /// <exception cref="ArgumentException">Thrown if the specified <paramref name="wellKnownSidType"/> is not recognized or is unavailable in the
        /// current context.</exception>
        public static SecurityIdentifier GetWellKnownSid(WellKnownSidType wellKnownSidType)
        {
            // Return the SecurityIdentifier for the specified well-known SID type.
            return !WellKnownSidLookupTable.TryGetValue(wellKnownSidType, out SecurityIdentifier? sid)
                ? throw new ArgumentException($"The specified well-known SID type '{wellKnownSidType}' is not recognized or not available in this context.")
                : sid;
        }

        /// <summary>
        /// Tests whether a given SID is a member of a given well known group.
        /// </summary>
        /// <param name="targetSid"></param>
        /// <param name="wellKnownGroupSid"></param>
        /// <returns></returns>
        internal static bool IsSidMemberOfWellKnownGroup(SecurityIdentifier targetSid, WellKnownSidType wellKnownGroupSid)
        {
            // Internal method to recursively check group membership.
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "No idea, but the compiler just doesn't understand that this is OK.")]
            static bool CheckMemberRecursive(DirectoryEntry groupEntry, SecurityIdentifier targetSid, HashSet<string> visited)
            {
                // Return early if we have no members to check.
                if (groupEntry.Invoke("Members") is not IEnumerable members)
                {
                    return false;
                }

                // Recursively test all member SIDs against our target SID, returning false if we have no match.
                foreach (object member in members)
                {
                    // Skip over already parsed groups (group membership loops).
                    using DirectoryEntry memberEntry = new(member);
                    if (!visited.Add(memberEntry.Path))
                    {
                        continue;
                    }

                    // Skip over the SID if it's malformed.
                    byte[]? sid = (byte[]?)memberEntry.Properties["ObjectSID"].Value;
                    if (sid is null)
                    {
                        continue;
                    }

                    // Return true if the current SID is the one we're testing for or if the member is a group that contains the target SID.
                    if (new SecurityIdentifier(sid, 0) == targetSid || (memberEntry.SchemaClassName == "Group" && CheckMemberRecursive(memberEntry, targetSid, visited)))
                    {
                        return true;
                    }
                }
                return false;
            }

            // Recursively check the members of the well-known group SID for the target SID.
            HashSet<string> visited = []; using DirectoryEntry groupEntry = new($"WinNT://./{GetWellKnownSid(wellKnownGroupSid).Translate(typeof(NTAccount)).ToString().Split('\\')[1]},group");
            return CheckMemberRecursive(groupEntry, targetSid, visited);
        }

        /// <summary>
        /// Determines whether the current process is elevated.
        /// </summary>
        /// <returns></returns>
        public static readonly bool CallerIsAdmin;

        /// <summary>
        /// Returns the current user's username.
        /// </summary>
        /// <returns></returns>
        public static readonly NTAccount CallerUsername;

        /// <summary>
        /// Represents the security identifier (SID) of the caller.
        /// </summary>
        /// <remarks>This field provides the SID associated with the caller, which can be used for 
        /// security-related operations such as access control or identity verification.</remarks>
        public static readonly SecurityIdentifier CallerSid;

        /// <summary>
        /// Gets the process ID of the caller's current process.
        /// </summary>
        public static readonly uint CallerProcessId;

        /// <summary>
        /// Session Id of the current user running this library.
        /// </summary>
        public static readonly uint CallerSessionId;

        /// <summary>
        /// Indicates whether the caller is the local system account.
        /// </summary>
        public static readonly bool CallerIsLocalSystem;

        /// <summary>
        /// Indicates whether the current caller is running in an interactive system environment.
        /// </summary>
        public static readonly bool CallerIsSystemInteractive;

        /// <summary>
        /// Gets a value indicating whether the current process is running with ServiceUI anywhere as a parent process.
        /// </summary>
        public static readonly bool CallerUsingServiceUI;

        /// <summary>
        /// Represents a predefined instance of <see cref="RunAsActiveUser"/> that executes operations as the currently
        /// active user.
        /// </summary>
        public static readonly RunAsActiveUser CallerRunAsActiveUser;

        /// <summary>
        /// Gets a read-only list of privileges associated with the caller.
        /// </summary>
        public static readonly IReadOnlyList<SE_PRIVILEGE> CallerPrivileges = PrivilegeManager.GetPrivileges();

        /// <summary>
        /// Represents the security identifier (SID) for the local account domain.
        /// </summary>
        /// <remarks>This SID is used to identify the local account domain on the system. It is typically
        /// used in scenarios involving security-related operations, such as access control or user account
        /// management.</remarks>
        public static readonly SecurityIdentifier LocalAccountDomainSid;

        /// <summary>
        /// A read-only dictionary that maps <see cref="WellKnownSidType"/> values to their corresponding <see
        /// cref="SecurityIdentifier"/> instances.
        /// </summary>
        /// <remarks>This dictionary provides a lookup table for well-known security identifiers (SIDs)
        /// based on their type. It is intended to facilitate quick access to predefined SIDs commonly used in
        /// security-related operations.</remarks>
        private static readonly ReadOnlyDictionary<WellKnownSidType, SecurityIdentifier> WellKnownSidLookupTable;
    }
}
