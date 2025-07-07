using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using PSADT.Extensions;
using PSADT.LibraryInterfaces;
using PSADT.SafeHandles;
using Windows.Win32.Security;
using Windows.Win32.Foundation;
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
            using (var cProcess = Process.GetCurrentProcess())
            using (var hProcess = cProcess.SafeHandle)
            {
                AdvApi32.OpenProcessToken(hProcess, TOKEN_ACCESS_MASK.TOKEN_ADJUST_PRIVILEGES | TOKEN_ACCESS_MASK.TOKEN_QUERY, out var hProcessToken);
                using (hProcessToken)
                {
                    if (IsPrivilegeEnabled(hProcessToken, privilege))
                    {
                        return;
                    }
                    EnablePrivilege(hProcessToken, privilege);
                }
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
            AdvApi32.GetTokenInformation(token, TOKEN_INFORMATION_CLASS.TokenPrivileges, SafeMemoryHandle.Null, out var requiredLength);
            using (var buffer = SafeHGlobalHandle.Alloc((int)requiredLength))
            {
                AdvApi32.GetTokenInformation(token, TOKEN_INFORMATION_CLASS.TokenPrivileges, buffer, out _);
                var privilegeCount = buffer.ReadInt32();
                var bufferOffset = sizeof(int);
                var increment = Marshal.SizeOf<LUID_AND_ATTRIBUTES>();
                Span<char> charSpan = stackalloc char[256];
                for (int i = 0; i < privilegeCount; i++)
                {
                    var attr = buffer.ToStructure<LUID_AND_ATTRIBUTES>(bufferOffset + (increment * i));
                    AdvApi32.LookupPrivilegeName(null, attr.Luid, charSpan, out var retLength);
                    if (charSpan.Slice(0, (int)retLength).ToString().TrimRemoveNull().Equals(privilege.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                    charSpan.Clear();
                }
                return false;
            }
        }

        /// <summary>
        /// Determines whether a privilege is enabled in the current process token.
        /// </summary>
        /// <param name="privilege"></param>
        /// <returns></returns>
        internal static bool HasPrivilege(SE_PRIVILEGE privilege)
        {
            using (var cProcess = Process.GetCurrentProcess())
            using (var hProcess = cProcess.SafeHandle)
            {
                AdvApi32.OpenProcessToken(hProcess, TOKEN_ACCESS_MASK.TOKEN_QUERY, out var hProcessToken);
                using (hProcessToken)
                {
                    return HasPrivilege(hProcessToken, privilege);
                }
            }
        }

        /// <summary>
        /// Determines whether a privilege is enabled in the specified token.
        /// </summary>
        /// <param name="token"></param>
        /// <param name="privilege"></param>
        /// <returns></returns>
        private static bool IsPrivilegeEnabled(SafeFileHandle token, SE_PRIVILEGE privilege)
        {
            AdvApi32.GetTokenInformation(token, TOKEN_INFORMATION_CLASS.TokenPrivileges, SafeMemoryHandle.Null, out var returnLength);
            AdvApi32.LookupPrivilegeValue(null, privilege.ToString(), out var luid);
            using (var buffer = SafeHGlobalHandle.Alloc((int)returnLength))
            {
                AdvApi32.GetTokenInformation(token, TOKEN_INFORMATION_CLASS.TokenPrivileges, buffer, out _);
                var privilegeCount = buffer.ReadInt32();
                var bufferOffset = sizeof(int);
                var increment = Marshal.SizeOf<LUID_AND_ATTRIBUTES>();
                for (int i = 0; i < privilegeCount; i++)
                {
                    var attr = buffer.ToStructure<LUID_AND_ATTRIBUTES>(bufferOffset + (increment * i));
                    if (attr.Luid.LowPart == luid.LowPart && attr.Luid.HighPart == luid.HighPart)
                    {
                        return (attr.Attributes & TOKEN_PRIVILEGES_ATTRIBUTES.SE_PRIVILEGE_ENABLED) != 0;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// Determines whether a privilege is enabled in the current process token.
        /// </summary>
        /// <param name="privilege"></param>
        /// <returns></returns>
        internal static bool IsPrivilegeEnabled(SE_PRIVILEGE privilege)
        {
            using (var cProcess = Process.GetCurrentProcess())
            using (var hProcess = cProcess.SafeHandle)
            {
                AdvApi32.OpenProcessToken(hProcess, TOKEN_ACCESS_MASK.TOKEN_QUERY, out var hProcessToken);
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
            AdvApi32.AdjustTokenPrivileges(token, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero);
        }

        /// <summary>
        /// Enables a privilege in the current process token.
        /// </summary>
        /// <param name="privilege"></param>
        internal static void EnablePrivilege(SE_PRIVILEGE privilege)
        {
            using (var cProcess = Process.GetCurrentProcess())
            using (var hProcess = cProcess.SafeHandle)
            {
                AdvApi32.OpenProcessToken(hProcess, TOKEN_ACCESS_MASK.TOKEN_ADJUST_PRIVILEGES | TOKEN_ACCESS_MASK.TOKEN_QUERY, out var hProcessToken);
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
            using (var cProcess = Process.GetCurrentProcess())
            using (var hProcess = cProcess.SafeHandle)
            {
                try
                {
                    var res = Kernel32.DuplicateHandle(hProcess, token, hProcess, out var newHandle, accessRights, false, 0);
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
