using System;
using System.Linq;
using System.ComponentModel;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using PSADT.Logging;
using PSADT.PInvokes;

namespace PSADT.AccessToken
{
    /// <summary>
    /// Provides methods for adjusting token privileges.
    /// </summary>
    internal static class PrivilegeManager
    {
        private static readonly object _privilegeLock = new object();

        /// <summary>
        /// A lookup dictionary mapping <see cref="TokenPrivilege"/> to their corresponding privilege names.
        /// </summary>
        private static readonly Dictionary<TokenPrivilege, string> _privilegeLookup = new(35)
        {
            { TokenPrivilege.AssignPrimaryToken, "AssignPrimaryToken" },
            { TokenPrivilege.Audit, "Audit" },
            { TokenPrivilege.Backup, "Backup" },
            { TokenPrivilege.ChangeNotify, "ChangeNotify" },
            { TokenPrivilege.CreateGlobal, "CreateGlobal" },
            { TokenPrivilege.CreatePageFile, "CreatePagefile" },
            { TokenPrivilege.CreatePermanent, "CreatePermanent" },
            { TokenPrivilege.CreateSymbolicLink, "CreateSymbolicLink" },
            { TokenPrivilege.CreateToken, "CreateToken" },
            { TokenPrivilege.Debug, "Debug" },
            { TokenPrivilege.DelegateSessionUserImpersonate, "DelegateSessionUserImpersonate" },
            { TokenPrivilege.EnableDelegation, "EnableDelegation" },
            { TokenPrivilege.Impersonate, "Impersonate" },
            { TokenPrivilege.IncreaseBasePriority, "IncreaseBasePriority" },
            { TokenPrivilege.IncreaseQuota, "IncreaseQuota" },
            { TokenPrivilege.IncreaseWorkingSet, "IncreaseWorkingSet" },
            { TokenPrivilege.LoadDriver, "LoadDriver" },
            { TokenPrivilege.LockMemory, "LockMemory" },
            { TokenPrivilege.MachineAccount, "MachineAccount" },
            { TokenPrivilege.ManageVolume, "ManageVolume" },
            { TokenPrivilege.ProfileSingleProcess, "ProfileSingleProcess" },
            { TokenPrivilege.Relabel, "Relabel" },
            { TokenPrivilege.RemoteShutdown, "RemoteShutdown" },
            { TokenPrivilege.Restore, "Restore" },
            { TokenPrivilege.Security, "Security" },
            { TokenPrivilege.Shutdown, "Shutdown" },
            { TokenPrivilege.SyncAgent, "SyncAgent" },
            { TokenPrivilege.SystemEnvironment, "SystemEnvironment" },
            { TokenPrivilege.SystemProfile, "SystemProfile" },
            { TokenPrivilege.SystemTime, "SystemTime" },
            { TokenPrivilege.TakeOwnership, "TakeOwnership" },
            { TokenPrivilege.TrustedComputerBase, "Tcb" },
            { TokenPrivilege.TimeZone, "TimeZone" },
            { TokenPrivilege.TrustedCredentialManagerAccess, "TrustedCredManAccess" },
            { TokenPrivilege.Undock, "UndockPrivilege" },
            { TokenPrivilege.UnsolicitedInput, "UnsolicitedInput" },
            { TokenPrivilege.InteractiveLogon, "InteractiveLogonRight" },
            { TokenPrivilege.NetworkLogon, "NetworkLogonRight" },
            { TokenPrivilege.BatchLogon, "BatchLogonRight" },
            { TokenPrivilege.ServiceLogon, "ServiceLogonRight" },
            { TokenPrivilege.DenyInteractiveLogon, "DenyInteractiveLogonRight" },
            { TokenPrivilege.DenyNetworkLogon, "DenyNetworkLogonRight" },
            { TokenPrivilege.DenyBatchLogon, "DenyBatchLogonRight" },
            { TokenPrivilege.DenyServiceLogon, "DenyServiceLogonRight" },
            { TokenPrivilege.RemoteInteractiveLogon, "RemoteInteractiveLogonRight" },
            { TokenPrivilege.DenyRemoteInteractiveLogon, "DenyRemoteInteractiveLogonRight" }
        };

        /// <summary>
        /// Enables or disables multiple privileges on the given token in a single operation.
        /// </summary>
        /// <param name="tokenHandle">The safe handle to the token.</param>
        /// <param name="privileges">The privileges to adjust.</param>
        /// <param name="enable">True to enable the privileges, false to disable them.</param>
        /// <exception cref="ArgumentNullException">Thrown when tokenHandle or privileges is null.</exception>
        /// <exception cref="Win32Exception">Thrown when the privilege adjustment fails.</exception>
        public static void AdjustTokenPrivileges(SafeAccessToken tokenHandle, IEnumerable<TokenPrivilege> privileges, bool enable)
        {
            if (tokenHandle == null) throw new ArgumentNullException(nameof(tokenHandle));
            if (privileges == null) throw new ArgumentNullException(nameof(privileges));

            var privilegesList = privileges.ToList();
            if (!privilegesList.Any()) return;

            lock (_privilegeLock)
            {
                var tokenPrivileges = new TOKEN_PRIVILEGES
                {
                    PrivilegeCount = (uint)privilegesList.Count,
                    Privileges = new LUID_AND_ATTRIBUTES[privilegesList.Count]
                };

                for (int i = 0; i < privilegesList.Count; i++)
                {
                    if (!TryGetPrivilegeLuid(privilegesList[i], out LUID luid))
                    {
                        throw new InvalidOperationException($"Failed to lookup privilege value for [{privilegesList[i]}].");
                    }

                    tokenPrivileges.Privileges[i] = new LUID_AND_ATTRIBUTES
                    {
                        Luid = luid,
                        Attributes = enable ? NativeMethods.SE_PRIVILEGE_ENABLED : 0
                    };
                }

                if (!NativeMethods.AdjustTokenPrivileges(tokenHandle.DangerousGetHandle(), false, ref tokenPrivileges, 0, IntPtr.Zero, IntPtr.Zero))
                {
                    int error = Marshal.GetLastWin32Error();
                    throw new Win32Exception(error);
                }

                // Check if all privileges were assigned
                int lastError = Marshal.GetLastWin32Error();
                if (lastError == NativeMethods.ERROR_NOT_ALL_ASSIGNED)
                {
                    // Check which privileges were actually enabled
                    Dictionary<TokenPrivilege, bool> privilegeStatus = GetPrivilegeStatus(tokenHandle, privileges);
                    IEnumerable<TokenPrivilege> enabledPrivileges = privilegeStatus.Where(kvp => kvp.Value).Select(kvp => kvp.Key);
                    IEnumerable<TokenPrivilege> failedPrivileges = privilegeStatus.Where(kvp => !kvp.Value).Select(kvp => kvp.Key);

                    UnifiedLogger.Create().Message($"Successfully enabled privileges: {string.Join(", ", enabledPrivileges)}").Severity(LogLevel.Debug);

                    string failedPrivilegesString = string.Empty;
                    if (failedPrivileges.Any())
                    {
                        failedPrivilegesString = string.Join(", ", failedPrivileges);
                        UnifiedLogger.Create().Message($"Failed to enable privileges: {failedPrivilegesString}").Severity(LogLevel.Warning);
                    }
                    throw new Win32Exception(lastError, $"Failed to enable privileges [{failedPrivilegesString}].");
                }
            }
        }

        /// <summary>
        /// Adjusts the specified privileges on the current process token.
        /// </summary>
        /// <param name="privileges">The privileges to adjust.</param>
        /// <param name="enable">True to enable the privileges, false to disable them.</param>
        public static void AdjustCurrentProcessTokenPrivileges(IEnumerable<TokenPrivilege> privileges, bool enable)
        {
            SafeAccessToken processToken = SafeAccessToken.Invalid;

            processToken = TokenManager.GetCurrentProcessToken();
            using (processToken)
            {
                AdjustTokenPrivileges(processToken, privileges, enable);
            }
        }

        /// <summary>
        /// Enables or disables a privilege on the current process token.
        /// </summary>
        /// <param name="privilege">The privilege to adjust.</param>
        /// <param name="enable">True to enable the privilege, false to disable it.</param>
        /// <exception cref="InvalidOperationException">Thrown when privilege adjustment fails.</exception>
        public static void AdjustCurrentProcessTokenPrivilege(TokenPrivilege privilege, bool enable)
        {
            var privileges = new[] { privilege };
            AdjustCurrentProcessTokenPrivileges(privileges, enable);
        }

        /// <summary>
        /// Removes all privileges from the specified token.
        /// </summary>
        /// <param name="tokenHandle">The safe handle to the token to be adjusted.</param>
        /// <exception cref="ArgumentNullException">Thrown when tokenHandle is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when privilege removal fails.</exception>
        public static void RemoveAllPrivileges(SafeAccessToken tokenHandle)
        {
            if (tokenHandle == null) throw new ArgumentNullException(nameof(tokenHandle));

            lock (_privilegeLock)
            {
                var tokenPrivileges = new TOKEN_PRIVILEGES { PrivilegeCount = 0 };

                if (!NativeMethods.AdjustTokenPrivileges(tokenHandle.DangerousGetHandle(), true, ref tokenPrivileges, 0, IntPtr.Zero, IntPtr.Zero))
                {
                    int error = Marshal.GetLastWin32Error();
                    throw new InvalidOperationException($"Failed to remove all privileges. Error code: {error}", new Win32Exception(error));
                }
            }
        }

        /// <summary>
        /// Enables or disables standard user privileges on the specified token.
        /// </summary>
        /// <param name="tokenHandle">The safe handle to the token.</param>
        /// <param name="enable">True to enable the privileges, false to disable them.</param>
        /// <exception cref="ArgumentNullException">Thrown when tokenHandle is null.</exception>
        public static void SetStandardUserPrivileges(SafeAccessToken tokenHandle, bool enable = true)
        {
            if (tokenHandle == null) throw new ArgumentNullException(nameof(tokenHandle));

            var standardUserPrivileges = new[]
            {
                TokenPrivilege.ChangeNotify,
                TokenPrivilege.Shutdown,
                TokenPrivilege.Undock,
                TokenPrivilege.IncreaseWorkingSet,
                TokenPrivilege.TimeZone
            };

            try
            {
                AdjustTokenPrivileges(tokenHandle, standardUserPrivileges, enable);
                UnifiedLogger.Create().Message("Successfully set standard user privileges.").Severity(LogLevel.Debug);
            }
            catch (Exception ex)
            {
                UnifiedLogger.Create().Message("Failed to set standard user privileges.").Error(ex);
                throw;
            }
        }

        /// <summary>
        /// Checks the status of privileges on the current process token.
        /// </summary>
        /// <param name="privileges">The privileges to check.</param>
        /// <returns>A dictionary with each privilege and its enabled status.</returns>
        public static Dictionary<TokenPrivilege, bool> GetCurrentProcessPrivilegeStatus(IEnumerable<TokenPrivilege> privileges)
        {
            using var processToken = TokenManager.GetCurrentProcessToken();
            return GetPrivilegeStatus(processToken, privileges);
        }

        /// <summary>
        /// Checks the enabled status of multiple privileges for the given token.
        /// </summary>
        /// <param name="tokenHandle">The token to check.</param>
        /// <param name="privileges">The privileges to check.</param>
        /// <returns>A dictionary with each privilege and its enabled status.</returns>
        public static Dictionary<TokenPrivilege, bool> GetPrivilegeStatus(SafeAccessToken tokenHandle, IEnumerable<TokenPrivilege> privileges)
        {
            if (tokenHandle == null) throw new ArgumentNullException(nameof(tokenHandle));
            if (privileges == null) throw new ArgumentNullException(nameof(privileges));

            var results = new Dictionary<TokenPrivilege, bool>();

            foreach (var privilege in privileges)
            {
                try
                {
                    results[privilege] = IsPrivilegeEnabled(tokenHandle, privilege);
                }
                catch (Exception ex)
                {
                    UnifiedLogger.Create().Message($"Failed to check privilege [{privilege}] status:").Error(ex).Severity(LogLevel.Warning);
                    results[privilege] = false;
                }
            }

            return results;
        }

        /// <summary>
        /// Checks if the specified privilege is enabled for the given token.
        /// </summary>
        /// <param name="tokenHandle">The token to check.</param>
        /// <param name="privilege">The privilege to check.</param>
        /// <returns>True if the privilege is enabled, false otherwise.</returns>
        public static bool IsPrivilegeEnabled(SafeAccessToken tokenHandle, TokenPrivilege privilege)
        {
            if (tokenHandle == null) throw new ArgumentNullException(nameof(tokenHandle));

            try
            {
                if (!_privilegeLookup.TryGetValue(privilege, out string? privilegeName))
                {
                    throw new ArgumentException($"Privilege [{privilege}] not found in lookup.", nameof(privilege));
                }

                string systemPrivilegeName = $"Se{privilegeName}Privilege";

                if (!NativeMethods.LookupPrivilegeValue(null!, systemPrivilegeName, out var luid))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error(), $"Failed to lookup privilege value for [{systemPrivilegeName}].");
                }

                var privileges = new PRIVILEGE_SET
                {
                    PrivilegeCount = 1,
                    Control = PRIVILEGE_SET_CONTROL.PRIVILEGE_SET_ALL_NECESSARY,
                    Privilege = new LUID_AND_ATTRIBUTES[1]
                };

                privileges.Privilege[0].Luid = luid;
                privileges.Privilege[0].Attributes = NativeMethods.SE_PRIVILEGE_ENABLED;

                if (!NativeMethods.PrivilegeCheck(tokenHandle, ref privileges, out bool result))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to check privilege status.");
                }

                return result;
            }
            catch (Exception ex)
            {
                UnifiedLogger.Create().Message($"Error checking privilege [{privilege}] status:").Error(ex);
                throw;
            }
        }

        /// <summary>
        /// Attempts to get the LUID for a given privilege.
        /// </summary>
        /// <param name="privilege">The privilege to look up.</param>
        /// <param name="luid">When this method returns, contains the LUID of the privilege if found.</param>
        /// <returns>True if the privilege LUID was found; otherwise, false.</returns>
        private static bool TryGetPrivilegeLuid(TokenPrivilege privilege, out LUID luid)
        {
            luid = default;

            if (!_privilegeLookup.TryGetValue(privilege, out string? privilegeName))
            {
                return false;
            }

            string systemPrivilegeName = $"Se{privilegeName}Privilege";
            return NativeMethods.LookupPrivilegeValue(null!, systemPrivilegeName, out luid);
        }
    }
}
