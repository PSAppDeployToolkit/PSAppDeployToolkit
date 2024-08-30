using System;
using System.Collections;
///using System.DirectoryServices;
using System.Security.Principal;
using System.Text.RegularExpressions;

namespace PSADT.Account
{
    public static class AccountHelper
    {
        /// <summary>
        /// Retrieves the Security Identifier (SID) of the built-in administrator account on the local machine.
        /// </summary>
        /// <returns>The <see cref="SecurityIdentifier"/> representing the built-in administrator account.</returns>
        /*public static SecurityIdentifier? GetBuiltInAdministratorAccountSid()
        {
            SecurityIdentifier? builtInAdministratorAccountSid = null;

            string machineName = Environment.MachineName;
            string localAdministratorsGroupName = new SecurityIdentifier("S-1-5-32-544")
                .Translate(typeof(NTAccount))
                .Value
                .Split('\\')[1];

            using var localMachine = new DirectoryEntry($"WinNT://{machineName},Computer");
            using var localAdministratorsGroup = localMachine.Children.Find(localAdministratorsGroupName, "group");

            string regexSidPattern = @"^S-\d-\d+-(\d+-){1,14}\d+$";
            DirectoryEntry? localAdministratorAccount = null;
            object? groupMembers = localAdministratorsGroup?.Invoke("members", null);

            if (groupMembers is IEnumerable groupMembersEnumerable)
            {
                foreach (object groupMember in groupMembersEnumerable)
                {
                    using var groupMemberDirectoryEntry = new DirectoryEntry(groupMember);
                    if (!string.IsNullOrEmpty(groupMemberDirectoryEntry.Name))
                    {
                        string groupMemberSid;
                        if (Regex.IsMatch(groupMemberDirectoryEntry.Name, regexSidPattern))
                        {
                            groupMemberSid = groupMemberDirectoryEntry.Name;
                        }
                        else
                        {
                            groupMemberSid = new NTAccount(groupMemberDirectoryEntry.Name)
                                .Translate(typeof(SecurityIdentifier))
                                .Value;
                        }

                        if (groupMemberSid.EndsWith("-500"))
                        {
                            localAdministratorAccount = groupMemberDirectoryEntry;
                        }
                    }
                }
            }

            if (localAdministratorAccount != null)
            {
                byte[] localAdministratorsGroupObjectSid = (byte[])localAdministratorAccount.InvokeGet("objectSID")!;
                builtInAdministratorAccountSid = new SecurityIdentifier(localAdministratorsGroupObjectSid, 0);
            }

            return builtInAdministratorAccountSid;
        }

        /// <summary>
        /// Retrieves the <see cref="NTAccount"/> object corresponding to the built-in administrator account.
        /// </summary>
        /// <returns>An <see cref="NTAccount"/> object representing the built-in administrator account.</returns>
        public static NTAccount? GetBuiltInAdministratorAccountNTAccount()
        {
            var sid = GetBuiltInAdministratorAccountSid();
            return sid != null ? GetNTAccountFromSid(sid) : null;
        }

        /// <summary>
        /// Retrieves the domain SID of the built-in administrator account.
        /// </summary>
        /// <returns>The <see cref="SecurityIdentifier"/> representing the domain SID.</returns>
        public static SecurityIdentifier? GetDomainSid()
        {
            var sid = GetBuiltInAdministratorAccountSid();
            return sid?.AccountDomainSid;
        }*/

        /// <summary>
        /// Parses the base SID from a given SID.
        /// </summary>
        /// <param name="sid">The <see cref="SecurityIdentifier"/> to parse.</param>
        /// <returns>The base <see cref="SecurityIdentifier"/>.</returns>
        public static SecurityIdentifier? ParseBaseSid(SecurityIdentifier sid)
        {
            string[] sidParts = sid.Value.Split('-');
            string baseSidString = string.Join("-", sidParts, 0, sidParts.Length - 1);
            return new SecurityIdentifier(baseSidString);
        }

        /// <summary>
        /// Retrieves the <see cref="NTAccount"/> corresponding to a given SID.
        /// </summary>
        /// <param name="sid">The <see cref="SecurityIdentifier"/> to translate.</param>
        /// <returns>An <see cref="NTAccount"/> object representing the SID.</returns>
        public static NTAccount GetNTAccountFromSid(SecurityIdentifier sid)
        {
            return (NTAccount)sid.Translate(typeof(NTAccount));
        }

        /// <summary>
        /// Retrieves a <see cref="SecurityIdentifier"/> from a well-known SID type and domain SID.
        /// </summary>
        /// <param name="wellKnownSidType">The well-known SID type.</param>
        /// <param name="domainSid">The domain SID, or null for a non-domain SID.</param>
        /// <returns>The <see cref="SecurityIdentifier"/> representing the well-known SID.</returns>
        public static SecurityIdentifier GetSidFromWellKnownSidType(WellKnownSidType wellKnownSidType, SecurityIdentifier? domainSid)
        {
            return new SecurityIdentifier(wellKnownSidType, domainSid);
        }

        /// <summary>
        /// Retrieves the <see cref="NTAccount"/> corresponding to a well-known SID type.
        /// </summary>
        /// <param name="wellKnownSidType">The well-known SID type.</param>
        /// <returns>An <see cref="NTAccount"/> object representing the well-known SID type.</returns>
        public static NTAccount GetNTAccountFromWellKnownSidType(WellKnownSidType wellKnownSidType)
        {
            var sidObject = new SecurityIdentifier(wellKnownSidType, null);
            return (NTAccount)sidObject.Translate(typeof(NTAccount));
        }

        /// <summary>
        /// Retrieves the <see cref="NTAccount"/> corresponding to a given SID string.
        /// </summary>
        /// <param name="sid">The SID string.</param>
        /// <returns>An <see cref="NTAccount"/> object representing the SID string.</returns>
        public static NTAccount GetNTAccountFromSidString(string sid)
        {
            var sidObject = new SecurityIdentifier(sid);
            return (NTAccount)sidObject.Translate(typeof(NTAccount));
        }

        /// <summary>
        /// Parses the NT domain from an NT account string.
        /// </summary>
        /// <param name="ntAccount">The NT account string.</param>
        /// <returns>The NT domain.</returns>
        public static string ParseNTDomainFromNTAccountString(string ntAccount)
        {
            return ntAccount.Split('\\')[0];
        }

        /// <summary>
        /// Parses the username from an NT account string.
        /// </summary>
        /// <param name="ntAccount">The NT account string.</param>
        /// <returns>The username.</returns>
        public static string ParseUserNameFromNTAccountString(string ntAccount)
        {
            return ntAccount.Split('\\')[1];
        }

        /// <summary>
        /// Retrieves the <see cref="SecurityIdentifier"/> corresponding to a given username.
        /// </summary>
        /// <param name="userName">The username.</param>
        /// <returns>The <see cref="SecurityIdentifier"/> representing the username.</returns>
        public static SecurityIdentifier GetSidFromUserName(string userName)
        {
            var ntAccount = new NTAccount(userName);
            return (SecurityIdentifier)ntAccount.Translate(typeof(SecurityIdentifier));
        }
    }
}
