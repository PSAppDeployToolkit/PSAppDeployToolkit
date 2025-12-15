using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Microsoft.Win32.SafeHandles;
using PSADT.Extensions;
using PSADT.LibraryInterfaces;
using Windows.Win32.Security;

namespace PSADT.Security
{
    internal static class TokenManager
    {
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
            AdvApi32.GetTokenInformation(tokenHandle, TOKEN_INFORMATION_CLASS.TokenElevation, buffer, out _);
            ref TOKEN_ELEVATION tokenElevation = ref Unsafe.As<byte, TOKEN_ELEVATION>(ref MemoryMarshal.GetReference(buffer));
            return tokenElevation.TokenIsElevated != 0;
        }

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
        /// Retrieves the linked token associated with the specified token handle.
        /// </summary>
        /// <remarks>This method retrieves the linked token, which is typically used in scenarios
        /// involving user impersonation or elevated privileges. The caller must ensure that the provided token handle
        /// is valid and has the necessary permissions to query linked token information.</remarks>
        /// <param name="tokenHandle">A <see cref="SafeHandle"/> representing the token handle for which the linked token is to be retrieved.</param>
        /// <returns>A <see cref="SafeFileHandle"/> representing the linked token associated with the specified token
        /// handle.</returns>
        internal static SafeFileHandle GetLinkedToken(SafeHandle tokenHandle)
        {
            Span<byte> buffer = stackalloc byte[Marshal.SizeOf<TOKEN_LINKED_TOKEN>()];
            AdvApi32.GetTokenInformation(tokenHandle, TOKEN_INFORMATION_CLASS.TokenLinkedToken, buffer, out _);
            ref TOKEN_LINKED_TOKEN tokenLinkedToken = ref Unsafe.As<byte, TOKEN_LINKED_TOKEN>(ref MemoryMarshal.GetReference(buffer));
            return new(tokenLinkedToken.LinkedToken, true);
        }

        /// <summary>
        /// Retrieves the primary token associated with the specified security token handle.
        /// </summary>
        /// <remarks>This method duplicates the specified security token to create a primary token, which
        /// can be used for impersonation or other security-related operations. Ensure that the caller has appropriate
        /// permissions to access and duplicate the token.</remarks>
        /// <param name="tokenHandle">A handle to the security token. This handle must have the necessary access rights to allow duplication.</param>
        /// <returns>A <see cref="SafeFileHandle"/> representing the duplicated primary token.</returns>
        internal static SafeFileHandle GetPrimaryToken(SafeHandle tokenHandle)
        {
            AdvApi32.DuplicateTokenEx(tokenHandle, TOKEN_ACCESS_MASK.TOKEN_ALL_ACCESS, null, SECURITY_IMPERSONATION_LEVEL.SecurityIdentification, TOKEN_TYPE.TokenPrimary, out SafeFileHandle hPrimaryToken);
            return hPrimaryToken;
        }

        /// <summary>
        /// Retrieves the primary token linked to the specified token handle.
        /// </summary>
        /// <remarks>This method uses the provided token handle to obtain a linked token and then
        /// retrieves its primary token. The caller is responsible for ensuring the validity of the input token
        /// handle.</remarks>
        /// <param name="tokenHandle">A <see cref="SafeHandle"/> representing the token handle for which the linked primary token is to be
        /// retrieved.</param>
        /// <returns>A <see cref="SafeFileHandle"/> representing the linked primary token.</returns>
        internal static SafeFileHandle GetLinkedPrimaryToken(SafeHandle tokenHandle)
        {
            using SafeFileHandle linkedToken = GetLinkedToken(tokenHandle);
            return GetPrimaryToken(linkedToken);
        }

        /// <summary>
        /// Retrieves the highest available primary token associated with the specified token handle.
        /// </summary>
        /// <remarks>This method attempts to retrieve the linked primary token associated with the
        /// specified token handle. If the linked token is unavailable, it falls back to retrieving the primary token of
        /// the original token handle.</remarks>
        /// <param name="tokenHandle">A <see cref="SafeHandle"/> representing the token handle for which the primary token is to be retrieved.</param>
        /// <returns>A <see cref="SafeFileHandle"/> representing the highest available primary token.</returns>
        internal static SafeFileHandle GetHighestPrimaryToken(SafeHandle tokenHandle)
        {
            // If the linked token is not available, fall back to the primary token of the original token handle.
            try
            {
                return GetLinkedPrimaryToken(tokenHandle);
            }
            catch
            {
                return GetPrimaryToken(tokenHandle);
            }
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
            AdvApi32.GetTokenInformation(tokenHandle, TOKEN_INFORMATION_CLASS.TokenUser, null, out uint returnLength);
            Span<byte> buffer = stackalloc byte[(int)returnLength];

            // Now grab the token's SID as requested.
            AdvApi32.GetTokenInformation(tokenHandle, TOKEN_INFORMATION_CLASS.TokenUser, buffer, out _);
            ref TOKEN_USER tokenUser = ref Unsafe.As<byte, TOKEN_USER>(ref MemoryMarshal.GetReference(buffer));
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
            AdvApi32.GetTokenInformation(tokenHandle, TOKEN_INFORMATION_CLASS.TokenSessionId, buffer, out _);
            return Unsafe.As<byte, uint>(ref MemoryMarshal.GetReference(buffer));
        }
    }
}
