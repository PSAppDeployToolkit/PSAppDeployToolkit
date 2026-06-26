using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Principal;
using PSADT.Foundation;
using PSADT.Interop;
using PSADT.Interop.Extensions;
using PSADT.Interop.SafeHandles;
using PSADT.ProcessManagement;
using PSADT.Security;
using PSADT.TerminalServices;
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD002:Avoid problematic synchronous waits", Justification = "There's no async support during static construction.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0301:Simplify collection initialization", Justification = "The collection expression won't compile for net8.0...")]
        static AccountUtilities()
        {
            // Cache information about the current user.
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                CallerIsAdmin = new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
                CallerGroups = identity.Groups?.Cast<SecurityIdentifier>().ToFrozenSet() ?? FrozenSet<SecurityIdentifier>.Empty;
                CallerIsServiceAccount = CallerGroups.Contains(new SecurityIdentifier(WellKnownSidType.ServiceSid, domainSid: null));
                CallerSid = identity.User ?? throw new NotSupportedException("Current Windows identity does not have a user SID.");
                CallerUsername = new(identity.Name);
            }

            // Build out process/session id information.
            _ = NativeMethods.ProcessIdToSessionId(CallerProcessId = PInvoke.GetCurrentProcessId(), out CallerSessionId);

            // Retrieve the local account domain SID.
            LSA_OBJECT_ATTRIBUTES objectAttributes = new() { Length = (uint)Unsafe.SizeOf<LSA_OBJECT_ATTRIBUTES>() };
            _ = NativeMethods.LsaOpenPolicy(in objectAttributes, LSA_POLICY_ACCESS.POLICY_VIEW_LOCAL_INFORMATION, out LsaCloseSafeHandle hPolicy);
            using (hPolicy)
            {
                _ = NativeMethods.LsaQueryInformationPolicy(hPolicy, POLICY_INFORMATION_CLASS.PolicyAccountDomainInformation, out SafeLsaFreeMemoryHandle buf);
                using (buf)
                {
                    ref readonly POLICY_ACCOUNT_DOMAIN_INFO policyAccountDomainInfo = ref buf.AsReadOnlyStructure<POLICY_ACCOUNT_DOMAIN_INFO>();
                    LocalAccountDomainSid = policyAccountDomainInfo.DomainSid.ToSecurityIdentifier();
                }
            }

            // Determine if the caller is the local system account.
            CallerIsLocalSystem = CallerSid.IsWellKnown(WellKnownSidType.LocalSystemSid);
            CallerIsLocalService = CallerSid.IsWellKnown(WellKnownSidType.LocalServiceSid);
            CallerIsNetworkService = CallerSid.IsWellKnown(WellKnownSidType.NetworkServiceSid);
            CallerIsSystemInteractive = CallerIsLocalSystem && CallerIsInteractive;
            CallerUsingServiceUI = CallerIsLocalSystem && ProcessUtilities.GetParentProcesses().Any(static p =>
            {
                if (ProcessUtilities.HasProcessExited(p))
                {
                    return false;
                }
                try
                {
                    return ProcessVersionInfo.GetVersionInfo(p).InternalName?.Equals("ServiceUI", StringComparison.OrdinalIgnoreCase) == true;
                }
                catch (UnauthorizedAccessException)
                {
                    return false;
                }
            });
        }

        /// <summary>
        /// Gets a read-only list of privileges associated with the caller.
        /// </summary>
        public static IReadOnlyList<SE_PRIVILEGE> CallerPrivileges => PrivilegeManager.GetPrivileges();

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
        public static readonly FrozenSet<SecurityIdentifier> CallerGroups;

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
        /// Indicates whether the current caller is running in an interactive user context.
        /// </summary>
        /// <remarks>This value can be used to determine if the code is executing in an environment where
        /// user interaction is possible, such as a desktop session, as opposed to a background service or automated
        /// process.</remarks>
        public static readonly bool CallerIsInteractive = Environment.UserInteractive;

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
        /// Indicates whether the current caller is the user currently logged on to the system.
        /// </summary>
        public static bool CallerIsLoggedOnUser => CallerRunAsActiveUser == SessionRunAsActiveUser;

        /// <summary>
        /// Represents a predefined instance of <see cref="RunAsActiveUser"/> that executes operations as the currently
        /// active user.
        /// </summary>
        public static RunAsActiveUser CallerRunAsActiveUser => RunAsActiveUserConstants.Caller;

        /// <summary>
        /// Gets the value indicating whether the session should run as the active user, or null if the setting is
        /// unspecified.
        /// </summary>
        public static RunAsActiveUser? SessionRunAsActiveUser => RunAsActiveUserConstants.Session;

        /// <summary>
        /// Represents the security identifier (SID) for the local system account (NT AUTHORITY\SYSTEM).
        /// </summary>
        /// <remarks>This SID is commonly used to grant permissions to the local system account, which has
        /// extensive privileges on the local computer. Use this value when specifying access control or auditing rules
        /// that should apply to the system account.</remarks>
        public static readonly SecurityIdentifier LocalSystemSid = new(WellKnownSidType.LocalSystemSid, domainSid: null);

        /// <summary>
        /// Represents the security identifier (SID) for the local account domain.
        /// </summary>
        /// <remarks>This SID is used to identify the local account domain on the system. It is typically
        /// used in scenarios involving security-related operations, such as access control or user account
        /// management.</remarks>
        public static readonly SecurityIdentifier LocalAccountDomainSid;

        /// <summary>
        /// A private static class that encapsulates constant values related to running operations as the active user.
        /// </summary>
        private static class RunAsActiveUserConstants
        {
            /// <summary>
            /// Represents the active user context for the caller, encapsulated as a <see cref="RunAsActiveUser"/> instance.
            /// </summary>
            internal static readonly RunAsActiveUser Caller = new(CallerUsername, CallerSid, CallerSessionId, CallerIsAdmin);

            /// <summary>
            /// Represents the active user context for the current session, encapsulated as a nullable <see cref="RunAsActiveUser"/> instance.
            /// </summary>
            internal static readonly RunAsActiveUser? Session = SessionInfo.GetAsync(CallerSessionId).AsTask().ConfigureAwait(false).GetAwaiter().GetResult()?.ToRunAsActiveUser();
        }
    }
}
