using PSADT.PInvoke;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace PSADT.Accounts
{
    public static class AccountHelper
    {
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

        /// <summary>
        /// Determines whether the specified string represents a valid Windows Security Identifier (SID).
        /// </summary>
        /// <param name="sidString">The string to validate as a SID.</param>
        /// <returns>
        /// <c>true</c> if the specified string is a valid SID; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// This method attempts to create a <see cref="SecurityIdentifier"/> object from the provided string. 
        /// If the string is not in a valid SID format or another related exception occurs, the method will return <c>false</c>.
        /// It catches <see cref="ArgumentException"/> for invalid SIDs and <see cref="UnauthorizedAccessException"/> for access-related issues.
        /// </remarks>
        /// <example>
        /// <code>
        /// bool isValid = IsValidSid("S-1-5-21-3623811015-3361044348-30300820-1013");
        /// </code>
        /// </example>
        public static bool IsValidSid(string sidString)
        {
            if (string.IsNullOrWhiteSpace(sidString))
            {
                return false;
            }

            try
            {
                // Attempt to create a SecurityIdentifier object from the string
                SecurityIdentifier securityIdentifier = new SecurityIdentifier(sidString);
                return true; // If no exception, the SID is valid
            }
            catch (ArgumentException)
            {
                // Thrown if the SID string is not in a valid format
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                // Thrown when access to the SID is restricted due to permission issues
                return false;
            }
        }

        /// <summary>
        /// Determines whether the specified string represents a valid Windows NTAccount.
        /// </summary>
        /// <param name="accountName">The string to validate as an NTAccount.</param>
        /// <returns>
        /// <c>true</c> if the specified string is a valid NTAccount; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// This method attempts to create an <see cref="NTAccount"/> object from the provided account name 
        /// and translates it into a <see cref="SecurityIdentifier"/>. If the account cannot be translated 
        /// to a valid Security Identifier (SID), it is considered invalid and the method returns <c>false</c>.
        /// 
        /// The method can validate both local and domain accounts (e.g., "DOMAIN\\Username" or "Username").
        /// </remarks>
        /// <exception cref="IdentityNotMappedException">
        /// Thrown when the specified account name cannot be mapped to a valid <see cref="SecurityIdentifier"/>. 
        /// This typically occurs if the account name does not exist or is invalid.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown if the <paramref name="accountName"/> is in an invalid format, such as containing illegal characters.
        /// </exception>
        /// <exception cref="UnauthorizedAccessException">
        /// Thrown if access to the account information is denied due to insufficient permissions or privileges.
        /// </exception>
        /// <example>
        /// The following example demonstrates how to check if an NTAccount is valid:
        /// <code>
        /// bool isValid = IsValidNTAccount("DOMAIN\\Username");
        /// </code>
        /// </example>
        public static bool IsValidNTAccount(string accountName)
        {
            if (string.IsNullOrWhiteSpace(accountName))
            {
                return false;
            }

            try
            {
                // Create an NTAccount object from the provided account name
                NTAccount ntAccount = new NTAccount(accountName);

                // Try to translate the NTAccount to a SecurityIdentifier (SID)
                SecurityIdentifier sid = (SecurityIdentifier)ntAccount.Translate(typeof(SecurityIdentifier));

                // If the translation succeeds, the account is valid
                return true;
            }
            catch (IdentityNotMappedException)
            {
                // Thrown when the account cannot be mapped to a valid SID (invalid account)
                return false;
            }
            catch (ArgumentException)
            {
                // Thrown if the account name format is invalid
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                // Thrown when access to the account information is denied due to lack of privileges
                return false;
            }
        }

        public static string GetLocalAdministratorsGroupName()
        {
            try
            {
                // Get the SID for the built-in Administrators group
                SecurityIdentifier sid = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);

                // Translate the SID to an NTAccount to get the localized group name
                NTAccount ntAccount = (NTAccount)sid.Translate(typeof(NTAccount));

                // Extract the group name without the domain or machine name
                string[] parts = ntAccount.Value.Split('\\');
                string groupName = parts.Length > 1 ? parts[1] : ntAccount.Value;

                if (string.IsNullOrWhiteSpace(groupName))
                    throw new InvalidOperationException("Failed to determine the Administrators group name.");

                return groupName;
            }
            catch (Exception ex)
            {
                // Handle exception if needed
                throw new InvalidOperationException("Failed to get the Administrators group name.", ex);
            }
        }

        public static bool IsUserInLocalGroup(string username, string groupname)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentNullException(nameof(username), "Username cannot be null or empty.");

            if (string.IsNullOrWhiteSpace(groupname))
                throw new ArgumentNullException(nameof(groupname), "Group name cannot be null or empty.");

            int res = NativeMethods.NetUserGetLocalGroups(
                null!,
                username,
                0,
                0,
                out IntPtr bufptr,
                out int entriesread,
                out int totalentries
            );

            if (res == 0)
            {
                bool isMember = false;
                try
                {
                    if (entriesread > 0 && bufptr != IntPtr.Zero)
                    {
                        IntPtr iter = bufptr;
                        int increment = Marshal.SizeOf(typeof(LOCALGROUP_USERS_INFO_0));

                        for (int i = 0; i < entriesread; i++)
                        {
                            LOCALGROUP_USERS_INFO_0 groupInfo = Marshal.PtrToStructure<LOCALGROUP_USERS_INFO_0>(iter);

                            if (!string.IsNullOrWhiteSpace(groupInfo.lgrui0_name) &&
                                string.Equals(groupInfo.lgrui0_name, groupname, StringComparison.OrdinalIgnoreCase))
                            {
                                isMember = true;
                                break;
                            }
                            iter = IntPtr.Add(iter, increment);
                        }
                    }
                }
                finally
                {
                    // Free the buffer allocated by NetUserGetLocalGroups
                    NativeMethods.NetApiBufferFree(bufptr);
                }
                return isMember;
            }
            else
            {
                // Handle error if needed
                throw new Win32Exception(res);
            }
        }
    }
}
