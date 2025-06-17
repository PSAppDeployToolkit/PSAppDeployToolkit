using System;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Microsoft.Win32.SafeHandles;
using PSADT.LibraryInterfaces;
using PSADT.SafeHandles;
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
            using (var buffer = SafeHGlobalHandle.Alloc(Marshal.SizeOf<TOKEN_ELEVATION>()))
            {
                AdvApi32.GetTokenInformation(tokenHandle, TOKEN_INFORMATION_CLASS.TokenElevation, buffer, out _);
                return buffer.ToStructure<TOKEN_ELEVATION>().TokenIsElevated != 0;
            }
        }

        /// <summary>
        /// Retrieves the linked token associated with the specified token handle.
        /// </summary>
        /// <remarks>This method retrieves the linked token, which is typically used in scenarios
        /// involving user impersonation or  elevated privileges. The caller must ensure that the provided token handle
        /// is valid and has the necessary  permissions to query linked token information.</remarks>
        /// <param name="tokenHandle">A <see cref="SafeHandle"/> representing the token handle for which the linked token is to be retrieved.</param>
        /// <returns>A <see cref="SafeAccessTokenHandle"/> representing the linked token associated with the specified token
        /// handle.</returns>
        internal static SafeAccessTokenHandle GetLinkedToken(SafeHandle tokenHandle)
        {
            using (var buffer = SafeHGlobalHandle.Alloc(Marshal.SizeOf<TOKEN_LINKED_TOKEN>()))
            {
                AdvApi32.GetTokenInformation(tokenHandle, TOKEN_INFORMATION_CLASS.TokenLinkedToken, buffer, out _);
                return new(buffer.ToStructure<TOKEN_LINKED_TOKEN>().LinkedToken);
            }
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
            AdvApi32.DuplicateTokenEx(tokenHandle, TOKEN_ACCESS_MASK.TOKEN_ALL_ACCESS, null, SECURITY_IMPERSONATION_LEVEL.SecurityImpersonation, TOKEN_TYPE.TokenPrimary, out var hPrimaryToken);
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
            using (var linkedToken = GetLinkedToken(tokenHandle))
            {
                return GetPrimaryToken(linkedToken);
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
            using (var buffer = SafeHGlobalHandle.Alloc(1024))
            {
                AdvApi32.GetTokenInformation(tokenHandle, TOKEN_INFORMATION_CLASS.TokenUser, buffer, out _);
                return new((IntPtr)buffer.ToStructure<TOKEN_USER>().User.Sid);
            }
        }
    }
}
