using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using PSADT.AccountManagement;
using PSADT.Extensions;
using PSADT.LibraryInterfaces;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Security;

namespace PSADT.Security
{
    /// <summary>
    /// Utility methods for working with security tokens.
    /// </summary>
	internal static class PrivilegeManager
    {
        /// <summary>
        /// Retrieves a read-only collection of privileges associated with the specified token.
        /// </summary>
        /// <remarks>This method queries the privileges of the provided token and optionally filters them
        /// based on the specified attributes. If no attributes are provided, all privileges associated with the token
        /// are returned.</remarks>
        /// <param name="token">A <see cref="SafeFileHandle"/> representing the token from which privileges are retrieved.</param>
        /// <param name="attributes">Optional attributes used to filter the privileges. If specified, only privileges matching the given <see
        /// cref="TOKEN_PRIVILEGES_ATTRIBUTES"/> will be included in the result.</param>
        /// <returns>A <see cref="ReadOnlyCollection{T}"/> containing the privileges associated with the token.</returns>
        /// <exception cref="ArgumentException">Thrown if a privilege name retrieved from the token cannot be mapped to a known <see cref="SE_PRIVILEGE"/>
        /// value.</exception>
        private static ReadOnlyCollection<SE_PRIVILEGE> GetPrivileges(SafeFileHandle token, TOKEN_PRIVILEGES_ATTRIBUTES? attributes = null)
        {
            // Internal worker function to retrieve the privilege name from the token attributes.
            static SE_PRIVILEGE GetPrivilege(in LUID_AND_ATTRIBUTES attr, Span<char> buffer)
            {
                _ = AdvApi32.LookupPrivilegeName(null, attr.Luid, buffer, out uint retLength);
                string privilegeName = buffer.Slice(0, (int)retLength).ToString().TrimRemoveNull();
                return !Enum.TryParse(privilegeName, true, out SE_PRIVILEGE privilege)
                    ? throw new ArgumentException($"Unknown privilege: {privilegeName}")
                    : privilege;
            }

            // Get the size of the buffer required to hold the token privileges.
            _ = AdvApi32.GetTokenInformation(token, TOKEN_INFORMATION_CLASS.TokenPrivileges, null, out uint returnLength);
            Span<byte> buffer = stackalloc byte[(int)returnLength];

            // Retrieve the token privileges and filter them based on the specified attributes before returning them.
            _ = AdvApi32.GetTokenInformation(token, TOKEN_INFORMATION_CLASS.TokenPrivileges, buffer, out _);
            ref readonly TOKEN_PRIVILEGES tokenPrivileges = ref buffer.AsReadOnlyStructure<TOKEN_PRIVILEGES>();
            uint privilegeCount = tokenPrivileges.PrivilegeCount;
            int bufferOffset = sizeof(uint);
            int increment = Marshal.SizeOf<LUID_AND_ATTRIBUTES>();
            Span<char> charSpan = stackalloc char[(int)PInvoke.MAX_PATH];
            List<SE_PRIVILEGE> privileges = [];
            if (attributes is not null)
            {
                for (int i = 0; i < privilegeCount; i++)
                {
                    ref readonly LUID_AND_ATTRIBUTES attr = ref buffer.Slice(bufferOffset + (increment * i)).AsReadOnlyStructure<LUID_AND_ATTRIBUTES>();
                    if ((attr.Attributes & attributes) == attributes)
                    {
                        privileges.Add(GetPrivilege(in attr, charSpan));
                    }
                }
            }
            else
            {
                for (int i = 0; i < privilegeCount; i++)
                {
                    ref readonly LUID_AND_ATTRIBUTES attr = ref buffer.Slice(bufferOffset + (increment * i)).AsReadOnlyStructure<LUID_AND_ATTRIBUTES>();
                    privileges.Add(GetPrivilege(in attr, charSpan));
                }
            }
            return new ReadOnlyCollection<SE_PRIVILEGE>([.. privileges.OrderBy(static p => p)]);
        }

        /// <summary>
        /// Retrieves a read-only collection of privileges associated with the current process.
        /// </summary>
        /// <remarks>This method queries the current process's token to determine its associated
        /// privileges. The returned collection is immutable and reflects the privileges at the time of the
        /// query.</remarks>
        /// <returns>A <see cref="ReadOnlyCollection{T}"/> containing the privileges of the current process. If no privileges are
        /// available, the collection will be empty.</returns>
        internal static ReadOnlyCollection<SE_PRIVILEGE> GetPrivileges()
        {
            using SafeProcessHandle cProcessSafeHandle = Kernel32.GetCurrentProcess();
            _ = AdvApi32.OpenProcessToken(cProcessSafeHandle, TOKEN_ACCESS_MASK.TOKEN_QUERY, out SafeFileHandle hProcessToken);
            using (hProcessToken)
            {
                return GetPrivileges(hProcessToken);
            }
        }

        /// <summary>
        /// Determines whether a privilege is enabled in the specified token.
        /// </summary>
        /// <param name="token"></param>
        /// <param name="privilege"></param>
        /// <returns></returns>
        private static bool HasPrivilege(SafeFileHandle token, SE_PRIVILEGE privilege)
        {
            return GetPrivileges(token).Contains(privilege);
        }

        /// <summary>
        /// Determines whether a privilege is enabled in the current process token.
        /// </summary>
        /// <param name="privilege"></param>
        /// <returns></returns>
        internal static bool HasPrivilege(SE_PRIVILEGE privilege)
        {
            return AccountUtilities.CallerPrivileges.Contains(privilege);
        }

        /// <summary>
        /// Determines whether a privilege is enabled in the specified token.
        /// </summary>
        /// <param name="token"></param>
        /// <param name="privilege"></param>
        /// <returns></returns>
        private static bool IsPrivilegeEnabled(SafeFileHandle token, SE_PRIVILEGE privilege)
        {
            return GetPrivileges(token, TOKEN_PRIVILEGES_ATTRIBUTES.SE_PRIVILEGE_ENABLED).Contains(privilege);
        }

        /// <summary>
        /// Determines whether a privilege is enabled in the current process token.
        /// </summary>
        /// <param name="privilege"></param>
        /// <returns></returns>
        internal static bool IsPrivilegeEnabled(SE_PRIVILEGE privilege)
        {
            using SafeProcessHandle cProcessSafeHandle = Kernel32.GetCurrentProcess();
            _ = AdvApi32.OpenProcessToken(cProcessSafeHandle, TOKEN_ACCESS_MASK.TOKEN_QUERY, out SafeFileHandle hProcessToken);
            using (hProcessToken)
            {
                return IsPrivilegeEnabled(hProcessToken, privilege);
            }
        }

        /// <summary>
        /// Enables a privilege in the specified token.
        /// </summary>
        /// <param name="token"></param>
        /// <param name="privilege"></param>
        private static void EnablePrivilege(SafeFileHandle token, SE_PRIVILEGE privilege)
        {
            if (!HasPrivilege(token, privilege))
            {
                throw new UnauthorizedAccessException($"The current process does not have the [{privilege}] privilege available.");
            }
            _ = AdvApi32.LookupPrivilegeValue(privilege, out LUID luid);
            TOKEN_PRIVILEGES tp = new()
            {
                PrivilegeCount = 1,
            };
            tp.Privileges[0] = new()
            {
                Luid = luid,
                Attributes = TOKEN_PRIVILEGES_ATTRIBUTES.SE_PRIVILEGE_ENABLED
            };
            _ = AdvApi32.AdjustTokenPrivileges(token, tp);
        }

        /// <summary>
        /// Enables a privilege in the current process token.
        /// </summary>
        /// <param name="privilege"></param>
        internal static void EnablePrivilege(SE_PRIVILEGE privilege)
        {
            using SafeProcessHandle cProcessSafeHandle = Kernel32.GetCurrentProcess();
            _ = AdvApi32.OpenProcessToken(cProcessSafeHandle, TOKEN_ACCESS_MASK.TOKEN_QUERY | TOKEN_ACCESS_MASK.TOKEN_ADJUST_PRIVILEGES, out SafeFileHandle hProcessToken);
            using (hProcessToken)
            {
                EnablePrivilege(hProcessToken, privilege);
            }
        }

        /// <summary>
        /// Ensures that a security token is enabled.
        /// </summary>
        /// <param name="privilege"></param>
        internal static void EnablePrivilegeIfDisabled(SE_PRIVILEGE privilege)
        {
            using SafeProcessHandle cProcessSafeHandle = Kernel32.GetCurrentProcess();
            _ = AdvApi32.OpenProcessToken(cProcessSafeHandle, TOKEN_ACCESS_MASK.TOKEN_QUERY | TOKEN_ACCESS_MASK.TOKEN_ADJUST_PRIVILEGES, out SafeFileHandle hProcessToken);
            using (hProcessToken)
            {
                if (!IsPrivilegeEnabled(hProcessToken, privilege))
                {
                    EnablePrivilege(hProcessToken, privilege);
                }
            }
        }
    }
}
