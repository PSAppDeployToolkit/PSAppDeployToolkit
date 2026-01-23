using System;
using System.Runtime.InteropServices;
using System.Security.Principal;
using PSADT.Extensions;
using PSADT.LibraryInterfaces;
using PSADT.LibraryInterfaces.Extensions;
using Windows.Win32.Security;

namespace PSADT.Security
{
    /// <summary>
    /// Provides utility methods for querying and manipulating Windows security tokens.
    /// </summary>
    /// <remarks>This class contains static methods for determining token elevation, administrative status,
    /// retrieving security identifiers (SIDs), and obtaining session IDs from Windows access tokens. All methods
    /// require valid token handles with appropriate access rights. The class is intended for internal use in scenarios
    /// where direct interaction with Windows security tokens is necessary.</remarks>
    internal static class TokenUtilities
    {
        /// <summary>
        /// Determines whether the specified security token represents a user with administrative privileges.
        /// </summary>
        /// <remarks>The caller is responsible for ensuring that the provided token handle remains valid
        /// for the duration of the call. This method does not take ownership of the handle.</remarks>
        /// <param name="tokenHandle">A handle to the access token to evaluate. The handle must be valid and have appropriate access rights.</param>
        /// <returns>true if the token represents a user who is a member of the Administrators group; otherwise, false.</returns>
        internal static bool IsTokenAdministrative(SafeHandle tokenHandle)
        {
            bool tokenHandleAddRef = false;
            try
            {
                tokenHandle.DangerousAddRef(ref tokenHandleAddRef);
                using WindowsIdentity identity = new(tokenHandle.DangerousGetHandle());
                return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
            }
            finally
            {
                if (tokenHandleAddRef)
                {
                    tokenHandle.DangerousRelease();
                }
            }
        }

        /// <summary>
        /// Determines whether the specified access token is elevated.
        /// </summary>
        /// <remarks>An elevated token is associated with a process that has been granted administrative
        /// privileges. This method queries the token's elevation status using the Windows API.</remarks>
        /// <param name="tokenHandle">A handle to the access token to be checked. This handle must have the appropriate access rights for querying
        /// token information.</param>
        /// <returns><see langword="true"/> if the access token is elevated; otherwise, <see langword="false"/>. Elevated tokens
        /// typically indicate that the process is running with administrative privileges.</returns>
        internal static bool IsTokenElevated(SafeHandle tokenHandle)
        {
            Span<byte> buffer = stackalloc byte[Marshal.SizeOf<TOKEN_ELEVATION>()];
            _ = AdvApi32.GetTokenInformation(tokenHandle, TOKEN_INFORMATION_CLASS.TokenElevation, buffer, out _);
            ref TOKEN_ELEVATION tokenElevation = ref buffer.AsStructure<TOKEN_ELEVATION>();
            return tokenElevation.TokenIsElevated != 0;
        }

        /// <summary>
        /// Retrieves the security identifier (SID) associated with the specified token handle.
        /// </summary>
        /// <remarks>This method extracts the user SID from the token handle by querying token
        /// information. The caller must ensure that the token handle is valid and properly initialized before calling
        /// this method.</remarks>
        /// <param name="tokenHandle">A handle to the access token from which the SID is to be retrieved. The handle must be valid and have
        /// appropriate access rights.</param>
        /// <returns>A <see cref="SecurityIdentifier"/> object representing the SID associated with the specified token handle.</returns>
        internal static SecurityIdentifier GetTokenSid(SafeHandle tokenHandle)
        {
            // Get the required buffer size and allocate it.
            _ = AdvApi32.GetTokenInformation(tokenHandle, TOKEN_INFORMATION_CLASS.TokenUser, null, out uint returnLength);
            Span<byte> buffer = stackalloc byte[(int)returnLength];

            // Now grab the token's SID as requested.
            _ = AdvApi32.GetTokenInformation(tokenHandle, TOKEN_INFORMATION_CLASS.TokenUser, buffer, out _);
            ref TOKEN_USER tokenUser = ref buffer.AsStructure<TOKEN_USER>();
            return tokenUser.User.Sid.ToSecurityIdentifier();
        }

        /// <summary>
        /// Retrieves the session ID associated with a specified token handle.
        /// </summary>
        /// <remarks>This method uses the Windows API to obtain the session ID for the provided token
        /// handle. The caller must ensure that the token handle is valid and has the necessary permissions.</remarks>
        /// <param name="tokenHandle">The handle to the token from which to retrieve the session ID. This handle must have appropriate access
        /// rights.</param>
        /// <returns>The session ID as an unsigned integer.</returns>
        internal static uint GetTokenSessionId(SafeHandle tokenHandle)
        {
            Span<byte> buffer = stackalloc byte[sizeof(uint)];
            _ = AdvApi32.GetTokenInformation(tokenHandle, TOKEN_INFORMATION_CLASS.TokenSessionId, buffer, out _);
            return MemoryMarshal.Read<uint>(buffer);
        }
    }
}
