using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using PSADT.AccountManagement;
using PSADT.Extensions;
using PSADT.LibraryInterfaces;
using PSADT.SafeHandles;
using Windows.Win32.Foundation;
using Windows.Win32.Security;
using Windows.Win32.System.Threading;

namespace PSADT.Security
{
    /// <summary>
    /// Utility methods for working with security tokens.
    /// </summary>
	internal static class PrivilegeManager
    {
        /// <summary>
        /// Ensures that a security token is enabled.
        /// </summary>
        /// <param name="privilege"></param>
        internal static void EnablePrivilegeIfDisabled(SE_PRIVILEGE privilege)
        {
            using (var cProcessSafeHandle = Kernel32.GetCurrentProcess())
            {
                AdvApi32.OpenProcessToken(cProcessSafeHandle, TOKEN_ACCESS_MASK.TOKEN_QUERY | TOKEN_ACCESS_MASK.TOKEN_ADJUST_PRIVILEGES, out var hProcessToken);
                using (hProcessToken)
                {
                    if (!IsPrivilegeEnabled(hProcessToken, privilege))
                    {
                        EnablePrivilege(hProcessToken, privilege);
                    }
                }
            }
        }

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
                AdvApi32.LookupPrivilegeName(null, attr.Luid, buffer, out var retLength);
                string privilegeName = buffer.Slice(0, (int)retLength).ToString().TrimRemoveNull();
                if (!Enum.TryParse<SE_PRIVILEGE>(privilegeName, true, out var privilege))
                {
                    throw new ArgumentException($"Unknown privilege: {privilegeName}");
                }
                return privilege;
            }

            // Get the size of the buffer required to hold the token privileges.
            AdvApi32.GetTokenInformation(token, TOKEN_INFORMATION_CLASS.TokenPrivileges, SafeMemoryHandle.Null, out var returnLength);
            using (var buffer = SafeHGlobalHandle.Alloc((int)returnLength))
            {
                // Retrieve the token privileges and filter them based on the specified attributes before returning them.
                AdvApi32.GetTokenInformation(token, TOKEN_INFORMATION_CLASS.TokenPrivileges, buffer, out _);
                var privilegeCount = buffer.ReadInt32();
                var bufferOffset = sizeof(int);
                var increment = Marshal.SizeOf<LUID_AND_ATTRIBUTES>();
                Span<char> charSpan = stackalloc char[256];
                List<SE_PRIVILEGE> privileges = [];
                if (null != attributes)
                {
                    for (int i = 0; i < privilegeCount; i++)
                    {
                        var attr = buffer.ToStructure<LUID_AND_ATTRIBUTES>(bufferOffset + (increment * i));
                        if ((attr.Attributes & attributes) != attributes)
                        {
                            privileges.Add(GetPrivilege(attr, charSpan));
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < privilegeCount; i++)
                    {
                        privileges.Add(GetPrivilege(buffer.ToStructure<LUID_AND_ATTRIBUTES>(bufferOffset + (increment * i)), charSpan));
                    }
                }
                return privileges.OrderBy(static p => p).ToList().AsReadOnly();
            }
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
            using (var cProcessSafeHandle = Kernel32.GetCurrentProcess())
            {
                AdvApi32.OpenProcessToken(cProcessSafeHandle, TOKEN_ACCESS_MASK.TOKEN_QUERY, out var hProcessToken);
                using (hProcessToken)
                {
                    return GetPrivileges(hProcessToken);
                }
            }
        }

        /// <summary>
        /// Determines whether a privilege is enabled in the specified token.
        /// </summary>
        /// <param name="token"></param>
        /// <param name="privilege"></param>
        /// <returns></returns>
        private static bool HasPrivilege(SafeFileHandle token, SE_PRIVILEGE privilege) => GetPrivileges(token).Contains(privilege);

        /// <summary>
        /// Determines whether a privilege is enabled in the current process token.
        /// </summary>
        /// <param name="privilege"></param>
        /// <returns></returns>
        internal static bool HasPrivilege(SE_PRIVILEGE privilege) => AccountUtilities.CallerPrivileges.Contains(privilege);

        /// <summary>
        /// Determines whether a privilege is enabled in the specified token.
        /// </summary>
        /// <param name="token"></param>
        /// <param name="privilege"></param>
        /// <returns></returns>
        private static bool IsPrivilegeEnabled(SafeFileHandle token, SE_PRIVILEGE privilege) => GetPrivileges(token, TOKEN_PRIVILEGES_ATTRIBUTES.SE_PRIVILEGE_ENABLED).Contains(privilege);

        /// <summary>
        /// Determines whether a privilege is enabled in the current process token.
        /// </summary>
        /// <param name="privilege"></param>
        /// <returns></returns>
        internal static bool IsPrivilegeEnabled(SE_PRIVILEGE privilege)
        {
            using (var cProcessSafeHandle = Kernel32.GetCurrentProcess())
            {
                AdvApi32.OpenProcessToken(cProcessSafeHandle, TOKEN_ACCESS_MASK.TOKEN_QUERY, out var hProcessToken);
                using (hProcessToken)
                {
                    return IsPrivilegeEnabled(hProcessToken, privilege);
                }
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
            AdvApi32.LookupPrivilegeValue(null, privilege.ToString(), out var luid);
            var tp = new TOKEN_PRIVILEGES
            {
                PrivilegeCount = 1,
            };
            tp.Privileges[0] = new LUID_AND_ATTRIBUTES
            {
                Luid = luid,
                Attributes = TOKEN_PRIVILEGES_ATTRIBUTES.SE_PRIVILEGE_ENABLED
            };
            AdvApi32.AdjustTokenPrivileges(token, tp);
        }

        /// <summary>
        /// Enables a privilege in the current process token.
        /// </summary>
        /// <param name="privilege"></param>
        internal static void EnablePrivilege(SE_PRIVILEGE privilege)
        {
            using (var cProcessSafeHandle = Kernel32.GetCurrentProcess())
            {
                AdvApi32.OpenProcessToken(cProcessSafeHandle, TOKEN_ACCESS_MASK.TOKEN_QUERY | TOKEN_ACCESS_MASK.TOKEN_ADJUST_PRIVILEGES, out var hProcessToken);
                using (hProcessToken)
                {
                    EnablePrivilege(hProcessToken, privilege);
                }
            }
        }

        /// <summary>
        /// Tests whether the current process has the specified access rights to a process handle.
        /// </summary>
        /// <param name="token"></param>
        /// <param name="accessRights"></param>
        /// <returns></returns>
        internal static bool TestProcessAccessRights(SafeProcessHandle token, PROCESS_ACCESS_RIGHTS accessRights)
        {
            using (var cProcessSafeHandle = Kernel32.GetCurrentProcess())
            {
                try
                {
                    var res = Kernel32.DuplicateHandle(cProcessSafeHandle, token, cProcessSafeHandle, out var newHandle, accessRights, false, 0);
                    using (newHandle)
                    {
                        return res;
                    }
                }
                catch (UnauthorizedAccessException ex) when (ex.HResult == HRESULT.E_ACCESSDENIED)
                {
                    return false;
                }
            }
        }
    }
}
