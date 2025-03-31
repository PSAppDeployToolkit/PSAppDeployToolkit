using System.Collections;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Security.Principal;

namespace PSADT.AccountManagement
{
    /// <summary>
    /// Utility methods for working with Windows accounts and groups.
    /// </summary>
    public static class AccountUtilities
    {
        /// <summary>
        /// Tests whether a given SID is a member of a given well known group.
        /// </summary>
        /// <param name="wellKnownSid"></param>
        /// <param name="targetSid"></param>
        /// <returns></returns>
        public static bool IsSidMemberOfGroup(WellKnownSidType wellKnownSid, SecurityIdentifier targetSid)
        {
            using (var groupEntry = new DirectoryEntry($"WinNT://./{new SecurityIdentifier(wellKnownSid, null).Translate(typeof(NTAccount)).ToString().Split('\\')[1]},group"))
            {
                var visited = new HashSet<string>();
                return CheckMemberRecursive(groupEntry, targetSid, visited);
            }
        }

        /// <summary>
        /// Internal helper method for IsSidMemberOfGroup() to scan all group members recurvsively via System.DirectoryServices.
        /// </summary>
        /// <param name="groupEntry"></param>
        /// <param name="targetSid"></param>
        /// <param name="visited"></param>
        /// <returns></returns>
        private static bool CheckMemberRecursive(DirectoryEntry groupEntry, SecurityIdentifier targetSid, HashSet<string> visited)
        {
            // Recursively test all member SIDs against our target SID, returning false if we have no match.
            if (groupEntry.Invoke("Members") is IEnumerable members)
            {
                foreach (object member in members)
                {
                    using (var memberEntry = new DirectoryEntry(member))
                    {
                        // Skip over already parsed groups (group membership loops).
                        if (!visited.Add(memberEntry.Path))
                        {
                            continue;
                        }

                        // Skip over the SID if it's malformed.
                        var sid = memberEntry.Properties["ObjectSID"].Value;
                        if (null == sid)
                        {
                            continue;
                        }

                        // Return true if the current SID is the one we're testing for.
                        if (new SecurityIdentifier((byte[])sid, 0) == targetSid)
                        {
                            return true;
                        }

                        // If this member is a group, scan through its members recursively.
                        if (memberEntry.SchemaClassName == "Group")
                        {
                            if (CheckMemberRecursive(memberEntry, targetSid, visited))
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }
    }
}
