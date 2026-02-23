using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using PSADT.AccountManagement;
using PSADT.Interop;
using PSADT.Interop.Extensions;
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
                _ = NativeMethods.LookupPrivilegeName(null, attr.Luid, buffer, out uint retLength);
                string privilegeName = buffer.Slice(0, (int)retLength).ToString();
                return !Enum.TryParse(privilegeName, true, out SE_PRIVILEGE privilege)
                    ? throw new ArgumentException($"Unknown privilege: {privilegeName}")
                    : privilege;
            }

            // Get the size of the buffer required to hold the token privileges.
            _ = NativeMethods.GetTokenInformation(token, TOKEN_INFORMATION_CLASS.TokenPrivileges, null, out uint returnLength);
            Span<byte> buffer = stackalloc byte[(int)returnLength];

            // Retrieve the token privileges and filter them based on the specified attributes before returning them.
            _ = NativeMethods.GetTokenInformation(token, TOKEN_INFORMATION_CLASS.TokenPrivileges, buffer, out _);
            ref readonly TOKEN_PRIVILEGES tokenPrivileges = ref buffer.AsReadOnlyStructure<TOKEN_PRIVILEGES>();
            uint privilegeCount = tokenPrivileges.PrivilegeCount;
            int bufferOffset = sizeof(uint);
            int increment = Marshal.SizeOf<LUID_AND_ATTRIBUTES>();
            Span<char> charSpan = stackalloc char[1024]; charSpan.Clear();
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
            using SafeProcessHandle cProcessSafeHandle = NativeMethods.GetCurrentProcess();
            _ = NativeMethods.OpenProcessToken(cProcessSafeHandle, TOKEN_ACCESS_MASK.TOKEN_QUERY, out SafeFileHandle hProcessToken);
            using (hProcessToken)
            {
                return GetPrivileges(hProcessToken);
            }
        }

        /// <summary>
        /// Determines whether the specified access token includes the given privilege.
        /// </summary>
        /// <remarks>Use this method to verify that an access token possesses a particular privilege
        /// before performing operations that require it.</remarks>
        /// <param name="token">A safe handle to the access token to evaluate. The token must be valid and opened with appropriate access
        /// rights.</param>
        /// <param name="privilege">The privilege to check for in the access token. This should be a valid value of the SE_PRIVILEGE
        /// enumeration.</param>
        /// <returns>true if the access token contains the specified privilege; otherwise, false.</returns>
        private static bool HasPrivilege(SafeFileHandle token, SE_PRIVILEGE privilege)
        {
            return GetPrivileges(token).Contains(privilege);
        }

        /// <summary>
        /// Determines whether the current caller possesses the specified privilege.
        /// </summary>
        /// <remarks>Use this method to verify that the executing account has a particular privilege
        /// before performing operations that require elevated permissions.</remarks>
        /// <param name="privilege">The privilege to check for in the current caller's set of privileges.</param>
        /// <returns>true if the current caller has the specified privilege; otherwise, false.</returns>
        internal static bool HasPrivilege(SE_PRIVILEGE privilege)
        {
            return AccountUtilities.CallerPrivileges.Contains(privilege);
        }

        /// <summary>
        /// Determines whether the specified privilege is enabled for the given access token.
        /// </summary>
        /// <remarks>This method examines the privileges associated with the provided access token and
        /// checks if the specified privilege is currently enabled. Use this method to verify privilege status before
        /// performing operations that require specific privileges.</remarks>
        /// <param name="token">A safe handle to the access token to check. The token must be valid and opened with appropriate access
        /// rights.</param>
        /// <param name="privilege">The privilege to check for its enabled status within the specified access token.</param>
        /// <returns>true if the specified privilege is enabled for the access token; otherwise, false.</returns>
        private static bool IsPrivilegeEnabled(SafeFileHandle token, SE_PRIVILEGE privilege)
        {
            return GetPrivileges(token, TOKEN_PRIVILEGES_ATTRIBUTES.SE_PRIVILEGE_ENABLED).Contains(privilege);
        }

        /// <summary>
        /// Determines whether the specified privilege is enabled for the current process.
        /// </summary>
        /// <remarks>This method requires the calling process to have the necessary permissions to query
        /// the process token. It uses the current process's token to check the privilege status.</remarks>
        /// <param name="privilege">The privilege to check for its enabled status in the current process.</param>
        /// <returns>true if the specified privilege is enabled; otherwise, false.</returns>
        internal static bool IsPrivilegeEnabled(SE_PRIVILEGE privilege)
        {
            using SafeProcessHandle cProcessSafeHandle = NativeMethods.GetCurrentProcess();
            _ = NativeMethods.OpenProcessToken(cProcessSafeHandle, TOKEN_ACCESS_MASK.TOKEN_QUERY, out SafeFileHandle hProcessToken);
            using (hProcessToken)
            {
                return IsPrivilegeEnabled(hProcessToken, privilege);
            }
        }

        /// <summary>
        /// Enables the specified privilege for the provided access token, allowing the associated process to perform
        /// actions that require that privilege.
        /// </summary>
        /// <remarks>Enabling a privilege may be necessary for operations that require elevated
        /// permissions, such as modifying system settings or accessing protected resources. This method does not add
        /// privileges to the token; it only enables privileges that are already present.</remarks>
        /// <param name="token">A handle to the access token for which the privilege will be enabled. The token must have the specified
        /// privilege available.</param>
        /// <param name="privilege">The privilege to enable, specified as a value of the SE_PRIVILEGE enumeration.</param>
        /// <exception cref="UnauthorizedAccessException">Thrown if the specified privilege is not available in the provided access token.</exception>
        private static void EnablePrivilege(SafeFileHandle token, SE_PRIVILEGE privilege)
        {
            if (!HasPrivilege(token, privilege))
            {
                throw new UnauthorizedAccessException($"The current process does not have the [{privilege}] privilege available.");
            }
            _ = NativeMethods.LookupPrivilegeValue(privilege, out LUID luid);
            TOKEN_PRIVILEGES tp = new()
            {
                PrivilegeCount = 1,
            };
            tp.Privileges[0] = new()
            {
                Luid = luid,
                Attributes = TOKEN_PRIVILEGES_ATTRIBUTES.SE_PRIVILEGE_ENABLED
            };
            _ = NativeMethods.AdjustTokenPrivileges(token, tp);
        }

        /// <summary>
        /// Enables the specified system privilege for the current process.
        /// </summary>
        /// <remarks>This method requires the calling process to have permission to adjust its own
        /// privileges. Enabling certain privileges may be necessary to perform operations that require elevated rights,
        /// such as accessing system resources or modifying security settings.</remarks>
        /// <param name="privilege">The privilege to enable, specified as a value of the SE_PRIVILEGE enumeration.</param>
        internal static void EnablePrivilege(SE_PRIVILEGE privilege)
        {
            using SafeProcessHandle cProcessSafeHandle = NativeMethods.GetCurrentProcess();
            _ = NativeMethods.OpenProcessToken(cProcessSafeHandle, TOKEN_ACCESS_MASK.TOKEN_QUERY | TOKEN_ACCESS_MASK.TOKEN_ADJUST_PRIVILEGES, out SafeFileHandle hProcessToken);
            using (hProcessToken)
            {
                EnablePrivilege(hProcessToken, privilege);
            }
        }

        /// <summary>
        /// Enables the specified privilege for the current process if it is not already enabled.
        /// </summary>
        /// <remarks>This method checks whether the given privilege is enabled for the current process and
        /// enables it if it is not. The caller must have appropriate access rights to adjust process privileges. This
        /// operation may require administrative permissions depending on the privilege being enabled.</remarks>
        /// <param name="privilege">The privilege to enable for the current process. This value specifies which system privilege should be
        /// checked and enabled if necessary.</param>
        internal static void EnablePrivilegeIfDisabled(SE_PRIVILEGE privilege)
        {
            using SafeProcessHandle cProcessSafeHandle = NativeMethods.GetCurrentProcess();
            _ = NativeMethods.OpenProcessToken(cProcessSafeHandle, TOKEN_ACCESS_MASK.TOKEN_QUERY | TOKEN_ACCESS_MASK.TOKEN_ADJUST_PRIVILEGES, out SafeFileHandle hProcessToken);
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
