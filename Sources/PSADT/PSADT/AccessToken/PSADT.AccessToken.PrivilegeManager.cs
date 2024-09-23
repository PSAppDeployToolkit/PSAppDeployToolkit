using System;
using PSADT.PInvoke;
using System.ComponentModel;
using System.Security.Principal;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using PSADT.Logging;

namespace PSADT.AccessToken
{
    internal static class PrivilegeManager
    {
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

        // AdjustCurrentProcessTokenPrivilege
        /// <summary>
        /// Enables or disables a specific privilege on the current process token.
        /// </summary>
        /// <param name="privilege">The privilege to adjust.</param>
        /// <param name="enable">True to enable the privilege, false to disable it.</param>
        /// <exception cref="SecureNamedPipesException">Thrown when the privilege adjustment fails.</exception>
        public static void AdjustTokenPrivilege(TokenPrivilege privilege, bool enable)
        {
            if (!SafeAccessToken.TryCreate(WindowsIdentity.GetCurrent().Token, out var safeTokenHandle))
            {
                throw new InvalidOperationException("Failed to get current process token.");
            }

            using (safeTokenHandle)
            {
                AdjustTokenPrivilegeInternal(safeTokenHandle.DangerousGetHandle(), privilege, enable);
            }
        }

        /// <summary>
        /// Enables or disables a specific privilege on the given token.
        /// </summary>
        /// <param name="tokenHandle">The handle to the token.</param>
        /// <param name="privilege">The privilege to adjust.</param>
        /// <param name="enable">True to enable the privilege, false to disable it.</param>
        /// <exception cref="SecureNamedPipesException">Thrown when the privilege adjustment fails.</exception>
        public static void AdjustTokenPrivilegeInternal(IntPtr tokenHandle, TokenPrivilege privilege, bool enable)
        {
            // Attempt to lookup the privilege value
            if (!NativeMethods.LookupPrivilegeValue(".", privilege.ToString(), out var luid))
            {
                int error = Marshal.GetLastWin32Error();
                throw new Win32Exception(error, $"Failed to lookup privilege value for [{privilege}]. Error code: {error}");
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
            if (!NativeMethods.AdjustTokenPrivileges(tokenHandle, false, ref tokenPrivileges, 0, IntPtr.Zero, IntPtr.Zero))
            {
                int error = Marshal.GetLastWin32Error();
                throw new Win32Exception(error, $"Failed to adjust token privileges for [{privilege}]. Error code: {error}");
            }
            
            // Check if all privileges were assigned successfully
            int lastError = Marshal.GetLastWin32Error();
            if (lastError == NativeMethods.ERROR_NOT_ALL_ASSIGNED)
            {
                throw new Win32Exception(lastError, $"Failed to assign all requested privileges for [{privilege}].");
            }
        }

        public static TokenPrivilege GetTokenPrivilegeByName(string privilegeName)
        {
            // Iterate through the dictionary to find the first matching value
            foreach (var kvp in _privilegeLookup)
            {
                if (kvp.Value.Equals(privilegeName, StringComparison.OrdinalIgnoreCase))
                {
                    return kvp.Key;
                }
            }

            // If no match is found, throw an exception
            throw new KeyNotFoundException($"Privilege with name '{privilegeName}' was not found in the lookup.");
        }

        /// <summary>
        /// Reduces the privileges of an administrator's token.
        /// </summary>
        /// <param name="tokenHandle">The handle to the token to be adjusted.</param>
        /// <exception cref="SecureNamedPipeException">Thrown when privilege reduction fails.</exception>
        public static void RemoveAllPrivileges(SafeAccessToken tokenHandle)
        {
            // Remove all privileges by setting PrivilegeCount to 0
            var tokenPrivileges = new TOKEN_PRIVILEGES { PrivilegeCount = 0 };

            if (!NativeMethods.AdjustTokenPrivileges(tokenHandle.DangerousGetHandle(), true, ref tokenPrivileges, 0, IntPtr.Zero, IntPtr.Zero))
            {
                throw new InvalidOperationException("Failed to adjust token privileges for admin reduction.", new Win32Exception(Marshal.GetLastWin32Error()));
            }
        }

        public static void SetStandardUserPrivileges(SafeAccessToken tokenHandle, bool enable = true)
        {
            // Define the standard user privileges by their string names
            var standardUserPrivileges = new List<string>
            {
                "ChangeNotify",
                "Shutdown",
                "Undock",
                "IncreaseWorkingSet",
                "TimeZone"
            };

            // Iterate over each standard user privilege
            foreach (var privilegeName in standardUserPrivileges)
            {
                try
                {
                    // Get the corresponding TokenPrivilege from the PrivLookup dictionary
                    var privilege = GetTokenPrivilegeByName(privilegeName);

                    // Adjust the token privileges based on the enable flag
                    AdjustTokenPrivilegeInternal(tokenHandle.DangerousGetHandle(), privilege, enable);
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
