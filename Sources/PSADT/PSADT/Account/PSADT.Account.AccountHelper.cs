using System;
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

    }
}
