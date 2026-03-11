using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using PSADT.Foundation;
using PSADT.Interop;
using PSADT.Interop.Extensions;
using PSADT.Interop.SafeHandles;
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1065:Do not raise exceptions in unexpected locations", Justification = "This exception will never be thrown during operation.")]
        static AccountUtilities()
        {
            // Cache information about the current user.
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                CallerIsAdmin = new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
                CallerGroups = identity.Groups?.Select(static g => (SecurityIdentifier)g).ToList().AsReadOnly();
                CallerIsServiceAccount = CallerGroups?.Contains(new SecurityIdentifier(WellKnownSidType.ServiceSid, null)) == true;
                CallerSid = identity.User ?? throw new InvalidOperationException("Current Windows identity does not have a user SID.");
                CallerUsername = new(identity.Name);
            }

            // Build out process/session id information.
            _ = NativeMethods.ProcessIdToSessionId(CallerProcessId = PInvoke.GetCurrentProcessId(), out CallerSessionId);

            // Retrieve the local account domain SID.
            _ = NativeMethods.LsaOpenPolicy(null, new() { Length = (uint)Marshal.SizeOf<LSA_OBJECT_ATTRIBUTES>() }, LSA_POLICY_ACCESS.POLICY_VIEW_LOCAL_INFORMATION, out LsaCloseSafeHandle hPolicy);
            using (hPolicy)
            {
                _ = NativeMethods.LsaQueryInformationPolicy(hPolicy, POLICY_INFORMATION_CLASS.PolicyAccountDomainInformation, out SafeLsaFreeMemoryHandle buf);
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
            LocalSystemSid = wellKnownSids[WellKnownSidType.LocalSystemSid];
            WellKnownSidLookupTable = new(wellKnownSids);

            // Determine if the caller is the local system account.
            CallerIsLocalSystem = CallerSid.IsWellKnown(WellKnownSidType.LocalSystemSid);
            CallerIsLocalService = CallerSid.IsWellKnown(WellKnownSidType.LocalServiceSid);
            CallerIsNetworkService = CallerSid.IsWellKnown(WellKnownSidType.NetworkServiceSid);
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
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the specified <paramref name="wellKnownSidType"/> is not recognized or is unavailable in the
        /// current context.</exception>
        public static SecurityIdentifier GetWellKnownSid(WellKnownSidType wellKnownSidType)
        {
            // Return the SecurityIdentifier for the specified well-known SID type.
            return !WellKnownSidLookupTable.TryGetValue(wellKnownSidType, out SecurityIdentifier? sid)
                ? throw new ArgumentOutOfRangeException(nameof(wellKnownSidType), wellKnownSidType, $"The specified well-known SID type '{wellKnownSidType}' is not recognized or not available in this context.")
                : sid;
        }

        /// <summary>
        /// Determines whether the current process is elevated.
        /// </summary>
        public static readonly bool CallerIsAdmin;

        /// <summary>
        /// Gets the list of security identifiers that represent the groups to which the current caller belongs.
        /// </summary>
        /// <remarks>This property is read-only and can be used to determine the group memberships of the
        /// caller for authorization or auditing purposes. The value may be null if group information is unavailable for
        /// the caller.</remarks>
        public static readonly IReadOnlyList<SecurityIdentifier>? CallerGroups;

        /// <summary>
        /// Gets a value indicating whether the current caller is a service account.
        /// </summary>
        /// <remarks>This field is static and read-only. It is set to <see langword="true"/> if the
        /// current execution context represents a service account; otherwise, it is <see langword="false"/>. Use this
        /// property to determine if privileged or automated account logic should be applied.</remarks>
        public static readonly bool CallerIsServiceAccount;

        /// <summary>
        /// Returns the current user's username.
        /// </summary>
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
        /// Gets a value indicating whether the current caller is a local service.
        /// </summary>
        public static readonly bool CallerIsLocalService;

        /// <summary>
        /// Gets a value indicating whether the current process is running under the Network Service account.
        /// </summary>
        public static readonly bool CallerIsNetworkService;

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
        /// Represents the security identifier (SID) for the local system account (NT AUTHORITY\SYSTEM).
        /// </summary>
        /// <remarks>This SID is commonly used to grant permissions to the local system account, which has
        /// extensive privileges on the local computer. Use this value when specifying access control or auditing rules
        /// that should apply to the system account.</remarks>
        public static readonly SecurityIdentifier LocalSystemSid;

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
