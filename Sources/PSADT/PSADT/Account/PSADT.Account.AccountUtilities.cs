using PSADT.Logging;
using PSADT.PInvoke;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace PSADT.Account
{
    public static class AccountUtilities
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

        public static string GetBuiltinAdministratorsGroupName()
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
                throw new InvalidOperationException("Failed to get the Administrators group name.", ex);
            }
        }

        public static SecurityIdentifier GetBuiltinAdministratorsGroupSid()
        {
            try
            {
                // Get the SID for the built-in Administrators group
                SecurityIdentifier sid = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);

                return sid;
            }
            catch (Exception ex)
            {
                UnifiedLogger.Create().Message("Failed to get Administrators group SID.").Error(ex).Severity(LogLevel.Error);

                throw;
            }
        }

        //private static int GetLevelFromStructure<T>()
        //{
        //    var m = System.Text.RegularExpressions.Regex.Match(typeof(T).Name, @"(\d+)$");
        //    var i = 0;
        //    if (m.Success)
        //        int.TryParse(m.Value, out i);
        //    return i;
        //}

        public static bool IsUserInBuiltInAdministratorsGroup(string username)
        {
            return IsUserInLocalGroup(username, GetBuiltinAdministratorsGroupName());
        }

        /// <summary>
        /// Checks if a specified user is a member of a local group.
        /// </summary>
        /// <param name="username">The username to check (e.g., "AzureAD\\username").</param>
        /// <param name="groupname">The name of the local group.</param>
        /// <returns>True if the user is a member of the group; otherwise, false.</returns>
        public static bool IsUserInLocalGroup(string username, string groupname)
        {
            uint level = 3;
            var groupMembers = GetLocalGroupMembers<LOCALGROUP_MEMBERS_INFO_3>(null, groupname, level);

            foreach (var member in groupMembers)
            {
                if (string.Equals(member, username, StringComparison.OrdinalIgnoreCase))
                {
                    UnifiedLogger.Create().Message($"User [{username}] is a member of group [{groupname}]").Severity(LogLevel.Debug);
                    return true;
                }
            }

            UnifiedLogger.Create().Message($"User [{username}] is not a member of group [{groupname}]").Severity(LogLevel.Debug);
            return false;
        }

        /// <summary>
        /// Retrieves the members of a specified local group using various different formats.
        /// </summary>
        /// <typeparam name="T">The structure type corresponding to the information level.</typeparam>
        /// <param name="servername">The name of the server (null for local machine).</param>
        /// <param name="groupname">The name of the local group.</param>
        /// <param name="level">The information level (0, 1, 2, or 3).</param>
        /// <returns>A list of group member identifiers (strings or SIDs) based on the information level.</returns>
        /// <exception cref="ArgumentNullException">Thrown when groupname is null or empty.</exception>
        public static List<string> GetLocalGroupMembers<T>(
            string? servername,
            string groupname,
            uint level = uint.MaxValue) where T : struct
        {
            if (string.IsNullOrWhiteSpace(groupname))
                throw new ArgumentNullException(nameof(groupname), "Group name cannot be null or empty.");

            UnifiedLogger.Create().Message($"Getting all members of local group [{groupname}]").Severity(LogLevel.Debug);

            IntPtr bufptr = IntPtr.Zero;
            const uint MAX_PREFERRED_LENGTH = unchecked((uint)-1);
            List<string> groupMembers = new List<string>();
            IntPtr resumehandle = IntPtr.Zero;

            if (level == uint.MaxValue) level = (uint)GetLevelFromStructure<T>();

            try
            {
                int status = NativeMethods.NetLocalGroupGetMembers(
                    servername!,
                    groupname,
                    level,
                    out bufptr,
                    MAX_PREFERRED_LENGTH,
                    out uint entriesRead,
                    out uint totalEntries,
                    ref resumehandle);

                if (status != 0)
                {
                    var errorMessage = new Win32Exception(status).Message;
                    UnifiedLogger.Create()
                        .Message($"Failed to get group membership for group [{groupname}]. Error code [{status}]: {errorMessage}")
                        .Severity(LogLevel.Error)
                        .Log();
                    throw new Win32Exception(status, $"Failed to get group membership for group [{groupname}]. Error code [{status}]: {errorMessage}");
                }

                if (entriesRead > 0 && bufptr != IntPtr.Zero)
                {
                    var sizeOfStruct = Marshal.SizeOf<T>();
                    string fieldName = GetMemberFieldName(level);

                    for (int i = 0; i < entriesRead; i++)
                    {
                        IntPtr current = IntPtr.Add(bufptr, i * sizeOfStruct);

                        try
                        {
                            T memberInfo = Marshal.PtrToStructure<T>(current);

                            // Use reflection to get the value of the appropriate field
                            var type = typeof(T);
                            var field = type.GetField(fieldName);
                            if (field == null)
                            {
                                throw new InvalidOperationException($"Field '{fieldName}' not found in type '{type.Name}'.");
                            }

                            var value = field.GetValue(memberInfo);
                            string? memberName = ExtractMemberName(value, level);

                            if (!string.IsNullOrEmpty(memberName))
                            {
                                groupMembers.Add(memberName!);
                                UnifiedLogger.Create().Message($"Group member: [{memberName}].").Severity(LogLevel.Debug);
                            }
                        }
                        catch (Exception ex)
                        {
                            UnifiedLogger.Create().Message($"Failed to process member at index [{i}]: {ex.Message}").Severity(LogLevel.Warning);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UnifiedLogger.Create().Message($"Failed to get members of group [{groupname}].").Error(ex);
                throw;
            }
            finally
            {
                if (bufptr != IntPtr.Zero)
                {
                    NativeMethods.NetApiBufferFree(bufptr);
                    bufptr = IntPtr.Zero;
                }
            }

            return groupMembers;
        }

        /// <summary>
        /// Determines the information level based on the structure type name.
        /// </summary>
        /// <typeparam name="T">The structure type.</typeparam>
        /// <returns>The information level as an integer.</returns>
        private static int GetLevelFromStructure<T>() where T : struct
        {
            var match = System.Text.RegularExpressions.Regex.Match(typeof(T).Name, @"(\d+)$");
            return match.Success && int.TryParse(match.Value, out int level) ? level : throw new InvalidOperationException($"Cannot determine level from type {typeof(T).Name}");
        }

        /// <summary>
        /// Gets the field name corresponding to the specified information level.
        /// </summary>
        /// <param name="level">The information level.</param>
        /// <returns>The field name as a string.</returns>
        /// <exception cref="ArgumentException">Thrown when an unsupported level is specified.</exception>
        private static string GetMemberFieldName(uint level)
        {
            return level switch
            {
                0 => "lgrmi0_sid",
                1 => "lgrmi1_name",
                2 => "lgrmi2_domainandname",
                3 => "lgrmi3_domainandname",
                _ => throw new ArgumentException($"Unsupported level: {level}", nameof(level)),
            };
        }

        /// <summary>
        /// Extracts the member name or SID from the field value based on the information level.
        /// </summary>
        /// <param name="value">The field value.</param>
        /// <param name="level">The information level.</param>
        /// <returns>The member name or SID as a string, or null if unavailable.</returns>
        private static string? ExtractMemberName(object? value, uint level)
        {
            if (value == null)
                return null;

            if (level == 0 && value is IntPtr ptrValue)
            {
                // Convert the SID pointer to a SecurityIdentifier
                try
                {
                    if (ptrValue != IntPtr.Zero)
                    {
                        var sid = new SecurityIdentifier(ptrValue);
                        return sid.Value;
                    }
                    else
                    {
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    UnifiedLogger.Create().Message($"Failed to convert SID pointer to SecurityIdentifier: {ex.Message}").Severity(LogLevel.Error);
                    return null;
                }
            }
            else if (value is string strValue)
            {
                return strValue;
            }
            else
            {
                return value.ToString();
            }
        }
    }
}
