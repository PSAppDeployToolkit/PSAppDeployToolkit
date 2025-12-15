using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Security.Principal;
using Microsoft.Win32;

namespace PSADT.AccountManagement
{
    /// <summary>
    /// Represents account information used in group policy contexts, including the username and security identifier
    /// (SID).
    /// </summary>
    /// <remarks>This class encapsulates the account's username and security identifier, providing a way to
    /// associate these identity details with group policy operations. The <see cref="Username"/> and <see cref="SID"/>
    /// fields are immutable and must be provided during object construction.</remarks>
    public sealed record GroupPolicyAccountInfo
    {
        /// <summary>
        /// Retrieves a list of account information associated with Group Policy.
        /// </summary>
        /// <remarks>This method accesses the Group Policy Data Store in the Windows registry to gather
        /// account information. It returns a collection of <see cref="GroupPolicyAccountInfo"/> objects, each
        /// containing details about a user account and its associated security identifier (SID). If the data store is
        /// unavailable or no valid entries are found, an empty list is returned.</remarks>
        /// <returns>A read-only list of <see cref="GroupPolicyAccountInfo"/> objects representing the account information stored
        /// in Group Policy. The list will be empty if no valid entries are found.</returns>
        public static IReadOnlyList<GroupPolicyAccountInfo> Get()
        {
            // Confirm we have a Group Policy Data Store to work with.
            using RegistryKey? datastore = Registry.LocalMachine.OpenSubKey(GroupPolicyDataStorePath);
            if (datastore is null)
            {
                return new ReadOnlyCollection<GroupPolicyAccountInfo>([]);
            }

            // Create list to hold the account information and process each found SID, returning the accumulated results.
            List<GroupPolicyAccountInfo> accountInfoList = [];
            foreach (string sid in datastore.GetSubKeyNames())
            {
                // Skip over anything that's not a proper SID.
                if (!sid.StartsWith("S-1-", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Skip over the entry if there's no indices.
                using RegistryKey? indices = Registry.LocalMachine.OpenSubKey($@"{GroupPolicyDataStorePath}\{sid}");
                if (indices is null)
                {
                    continue;
                }

                // Process each found index.
                foreach (string index in indices.GetSubKeyNames())
                {
                    // If the username is available, add it to the list and skip to the next SID.
                    using RegistryKey? info = Registry.LocalMachine.OpenSubKey($@"{GroupPolicyDataStorePath}\{sid}\{index}");
                    if (info?.GetValue("szName", null) is string username && !string.IsNullOrWhiteSpace(username))
                    {
                        accountInfoList.Add(new(new(username.Trim()), new(sid))); break;
                    }
                }
            }
            return accountInfoList.AsReadOnly();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GroupPolicyAccountInfo"/> class with the specified username and
        /// security identifier.
        /// </summary>
        /// <param name="username">The account's username represented as an <see cref="NTAccount"/>. Cannot be <see langword="null"/>.</param>
        /// <param name="sid">The account's security identifier represented as a <see cref="SecurityIdentifier"/>. Cannot be <see
        /// langword="null"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="username"/> is <see langword="null"/> or if <paramref name="sid"/> is <see
        /// langword="null"/>.</exception>
        private GroupPolicyAccountInfo(NTAccount username, SecurityIdentifier sid)
        {
            Username = username ?? throw new ArgumentNullException(nameof(username), "Username cannot be null.");
            SID = sid ?? throw new ArgumentNullException(nameof(sid), "SID cannot be null.");
        }

        /// <summary>
        /// Represents the NTAccount object for the username associated with the current context.
        /// </summary>
        /// <remarks>This field provides access to the NTAccount representation of the username, which can
        /// be used for security-related operations or identity management within the system.</remarks>
        public NTAccount Username { get; }

        /// <summary>
        /// Represents a security identifier (SID) that uniquely identifies a user, group, or computer account.
        /// </summary>
        /// <remarks>A security identifier (SID) is a unique value used to identify a security principal
        /// in Windows-based systems. This field provides access to a predefined SID. The specific SID represented by
        /// this field depends on its context.</remarks>
        public SecurityIdentifier SID { get; }

        /// <summary>
        /// Represents the registry path to the Group Policy Data Store.
        /// </summary>
        /// <remarks>This path is used to access the Group Policy Data Store in the Windows registry. It
        /// is a static, read-only field and cannot be modified.</remarks>
        private const string GroupPolicyDataStorePath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Group Policy\DataStore";
    }
}
