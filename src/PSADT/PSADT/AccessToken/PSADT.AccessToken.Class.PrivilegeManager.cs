using System.Runtime.InteropServices;
using PSADT.LibraryInterfaces;
using Windows.Win32;
using Windows.Win32.Security;
using Windows.Win32.Foundation;

namespace PSADT.AccessToken
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
        public static void EnsurePrivilegeEnabled(SE_TOKEN privilege)
        {
            AdvApi32.OpenProcessToken(PInvoke.GetCurrentProcess(), TOKEN_ACCESS_MASK.TOKEN_ADJUST_PRIVILEGES | TOKEN_ACCESS_MASK.TOKEN_QUERY, out var token);
            try
            {
                if (IsPrivilegeEnabled(token, privilege))
                {
                    return;
                }
                EnablePrivilege(token, privilege);
            }
            finally
            {
                Kernel32.CloseHandle(ref token);
            }
        }

        /// <summary>
        /// Determines whether a privilege is enabled in the specified token.
        /// </summary>
        /// <param name="token"></param>
        /// <param name="privilegeName"></param>
        /// <returns></returns>
        private static bool IsPrivilegeEnabled(HANDLE token, SE_TOKEN privilege)
        {
            AdvApi32.LookupPrivilegeValue(null, privilege.ToString(), out var luid);

            const int bufferSize = 1024;
            var buffer = Marshal.AllocHGlobal(bufferSize);
            try
            {
                AdvApi32.GetTokenInformation(token, TOKEN_INFORMATION_CLASS.TokenPrivileges, buffer, (uint)bufferSize, out _);

                var privilegeCount = Marshal.ReadInt32(buffer);
                var ptr = buffer + sizeof(uint);
                var inc = Marshal.SizeOf<LUID_AND_ATTRIBUTES>();
                for (int i = 0; i < privilegeCount; i++)
                {
                    var attr = Marshal.PtrToStructure<LUID_AND_ATTRIBUTES>(ptr);
                    if (attr.Luid.LowPart == luid.LowPart && attr.Luid.HighPart == luid.HighPart)
                    {
                        return (attr.Attributes & TOKEN_PRIVILEGES_ATTRIBUTES.SE_PRIVILEGE_ENABLED) != 0;
                    }
                    ptr += inc;
                }
                return false;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        /// <summary>
        /// Enables a privilege in the specified token.
        /// </summary>
        /// <param name="token"></param>
        /// <param name="privilegeName"></param>
        private static void EnablePrivilege(HANDLE token, SE_TOKEN privilege)
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
            AdvApi32.AdjustTokenPrivileges(token, false, ref tp, 0);
        }
    }
}
