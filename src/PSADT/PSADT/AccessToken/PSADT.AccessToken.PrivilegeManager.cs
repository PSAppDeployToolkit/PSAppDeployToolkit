using System;
using System.ComponentModel;
using System.Security.Principal;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using PSADT.Logging;
using PSADT.PInvoke;

namespace PSADT.AccessToken
{
    /// <summary>
    /// Provides methods for adjusting token privileges.
    /// </summary>
    internal static class PrivilegeManager
    {
        /// <summary>
        /// A lookup dictionary mapping <see cref="TokenPrivilege"/> to their corresponding privilege names.
        /// </summary>
        internal static readonly Dictionary<TokenPrivilege, string> _privilegeLookup = new(35)
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
        /// Enables or disables a specific privilege on the current process token.
        /// </summary>
        /// <param name="privilege">The privilege to adjust.</param>
        /// <param name="enable">True to enable the privilege, false to disable it.</param>
        /// <exception cref="InvalidOperationException">Thrown when the privilege adjustment fails.</exception>
        public static void AdjustTokenPrivilege(TokenPrivilege privilege, bool enable)
        {
            if (!SafeAccessToken.TryCreate(WindowsIdentity.GetCurrent().Token, out var safeTokenHandle))
            {
                throw new InvalidOperationException("Failed to get current process token.");
            }

            using (safeTokenHandle)
            {
                AdjustTokenPrivilegeInternal(safeTokenHandle, privilege, enable);
            }
        }

        /// <summary>
        /// Enables or disables a specific privilege on the given token.
        /// </summary>
        /// <param name="tokenHandle">The safe handle to the token.</param>
        /// <param name="privilege">The privilege to adjust.</param>
        /// <param name="enable">True to enable the privilege, false to disable it.</param>
        /// <exception cref="Win32Exception">Thrown when the privilege adjustment fails.</exception>
        /// <exception cref="KeyNotFoundException">Thrown when the privilege is not found in the lookup.</exception>
        public static void AdjustTokenPrivilegeInternal(SafeAccessToken tokenHandle, TokenPrivilege privilege, bool enable)
        {
            // Attempt to lookup the privilege value using the system name
            if (!_privilegeLookup.TryGetValue(privilege, out string? privilegeName))
            {
                throw new KeyNotFoundException($"Privilege [{privilege}] not found in the lookup dictionary.");
            }

            string systemPrivilegeName = "Se" + privilegeName + "Privilege";

            if (!NativeMethods.LookupPrivilegeValue(null!, systemPrivilegeName, out var luid))
            {
                int error = Marshal.GetLastWin32Error();
                throw new Win32Exception(error, $"Failed to lookup privilege value for [{systemPrivilegeName}]. Error code: {error}");
            }

            // Set up the TOKEN_PRIVILEGES structure
            var tokenPrivileges = new TOKEN_PRIVILEGES
            {
                PrivilegeCount = 1,
                Privileges = new LUID_AND_ATTRIBUTES[1]
            };
            tokenPrivileges.Privileges[0].Luid = luid;
            tokenPrivileges.Privileges[0].Attributes = enable ? NativeMethods.SE_PRIVILEGE_ENABLED : 0;

            // Attempt to adjust the token privileges
            if (!NativeMethods.AdjustTokenPrivileges(tokenHandle.DangerousGetHandle(), false, ref tokenPrivileges, 0, IntPtr.Zero, IntPtr.Zero))
            {
                int error = Marshal.GetLastWin32Error();
                throw new Win32Exception(error, $"Failed to adjust token privileges for [{systemPrivilegeName}]. Error code: {error}");
            }

            // Check if all privileges were assigned successfully
            int lastError = Marshal.GetLastWin32Error();
            if (lastError == NativeMethods.ERROR_NOT_ALL_ASSIGNED)
            {
                throw new Win32Exception(lastError, $"Failed to assign all requested privileges for [{systemPrivilegeName}].");
            }
        }

        /// <summary>
        /// Retrieves the <see cref="TokenPrivilege"/> enumeration value corresponding to the specified privilege name.
        /// </summary>
        /// <param name="privilegeName">The name of the privilege.</param>
        /// <returns>The corresponding <see cref="TokenPrivilege"/> value.</returns>
        /// <exception cref="KeyNotFoundException">Thrown when the privilege name is not found.</exception>
        public static TokenPrivilege GetTokenPrivilegeByName(string privilegeName)
        {
            foreach (var kvp in _privilegeLookup)
            {
                if (kvp.Value.Equals(privilegeName, StringComparison.OrdinalIgnoreCase))
                {
                    return kvp.Key;
                }
            }

            throw new KeyNotFoundException($"Privilege with name '{privilegeName}' was not found in the lookup.");
        }

        /// <summary>
        /// Removes all privileges from the specified token.
        /// </summary>
        /// <param name="tokenHandle">The safe handle to the token to be adjusted.</param>
        /// <exception cref="InvalidOperationException">Thrown when privilege removal fails.</exception>
        public static void RemoveAllPrivileges(SafeAccessToken tokenHandle)
        {
            // Remove all privileges by setting PrivilegeCount to 0
            var tokenPrivileges = new TOKEN_PRIVILEGES { PrivilegeCount = 0 };

            if (!NativeMethods.AdjustTokenPrivileges(tokenHandle.DangerousGetHandle(), true, ref tokenPrivileges, 0, IntPtr.Zero, IntPtr.Zero))
            {
                int error = Marshal.GetLastWin32Error();
                throw new InvalidOperationException($"Failed to remove all privileges. Error code: {error}", new Win32Exception(error));
            }
        }

        /// <summary>
        /// Enables or disables standard user privileges on the specified token.
        /// </summary>
        /// <param name="tokenHandle">The safe handle to the token.</param>
        /// <param name="enable">True to enable the privileges, false to disable them.</param>
        public static void SetStandardUserPrivileges(SafeAccessToken tokenHandle, bool enable = true)
        {
            var standardUserPrivileges = new List<string>
            {
                "ChangeNotify",
                "Shutdown",
                "Undock",
                "IncreaseWorkingSet",
                "TimeZone"
            };

            foreach (var privilegeName in standardUserPrivileges)
            {
                try
                {
                    var privilege = GetTokenPrivilegeByName(privilegeName);

                    AdjustTokenPrivilegeInternal(tokenHandle, privilege, enable);
                }
                catch (KeyNotFoundException ex)
                {
                    UnifiedLogger.Create().Message($"Privilege [{privilegeName}] not found: {ex.Message}").Severity(LogLevel.Error);
                }
                catch (Exception ex)
                {
                    UnifiedLogger.Create().Message($"Failed to adjust privilege [{privilegeName}]: {ex.Message}").Severity(LogLevel.Error);
                }
            }
        }
    }
}
