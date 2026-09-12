using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Threading.Tasks;
using PSADT.AccountManagement;
using PSADT.FileSystem;
using PSADT.Security;
using Windows.Win32.Security;

namespace PSADT.Foundation
{
    /// <summary>
    /// Provides functionality to ensure that the specified user has the necessary file system permissions to access and execute the client/server assemblies.
    /// </summary>
    internal static class ClientServerPermissions
    {
        /// <summary>
        /// Ensures that the specified user has the required file system permissions for a set of file paths.
        /// </summary>
        /// <remarks>This method verifies and, if necessary, updates the file system permissions for the
        /// specified user on the provided file paths. If the user already has the required permissions, no changes are
        /// made.</remarks>
        /// <param name="runAsActiveUser">The user for whom the file system permissions will be remediated. This parameter cannot be <see
        /// langword="null"/>.</param>
        /// <param name="extraPaths">An optional list of additional file paths to include in the remediation process. All paths must point to
        /// existing files.</param>
        /// <param name="elevatedTokenType">An optional parameter specifying the type of elevated token to use when checking permissions.
        /// The default value is <see cref="ElevatedTokenType.None"/>.</param>
        /// <exception cref="FileNotFoundException">Thrown if any path in <paramref name="extraPaths"/> or the default assemblies does not exist.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the permissions cannot be modified for any path due to insufficient privileges or if the file is located on a network share.</exception>
        /// <exception cref="NotSupportedException">Thrown if <paramref name="runAsActiveUser"/> is another user whose session token cannot be brokered, as a client cannot be launched for them either.</exception>
        internal static async ValueTask RemediateAsync(RunAsActiveUser runAsActiveUser, IReadOnlyList<FileInfo>? extraPaths = null, ElevatedTokenType elevatedTokenType = ElevatedTokenType.None)
        {
            // Test the user's access with their token where one can be had, and with their identifier where one can't.
            // Only a user with no session of their own legitimately has no token; for anyone else its absence means the
            // brokering was refused, and nothing could use the answer anyway as launching a client for them needs it too.
            using SafeHandle? hPrimaryToken = await GetEffectiveAccessTokenAsync(runAsActiveUser, elevatedTokenType).ConfigureAwait(false);
            if (hPrimaryToken is null && runAsActiveUser.SessionId != uint.MaxValue)
            {
                throw new NotSupportedException("Cannot remediate another user's session as the SYSTEM account does not have access to the PSAppDeployToolkit module.");
            }
            Func<FileInfo, bool> testEffectiveAccess = hPrimaryToken is null
                ? (path) => FileSystemUtilities.TestEffectiveAccess(path, runAsActiveUser.SID, _requiredPermissions)
                : (path) => FileSystemUtilities.TestEffectiveAccess(path, hPrimaryToken, _requiredPermissions);

            // Test each individual file and remediate ACLs as required, using the minimum read/execute we require.
            FileSystemAccessRule fileSystemAccessRule = new(runAsActiveUser.SID, _requiredPermissions, InheritanceFlags.None, PropagationFlags.None, AccessControlType.Allow);
            foreach (FileInfo path in extraPaths?.Count > 0 ? _assemblies.Concat(extraPaths) : _assemblies)
            {
                if (!path.Exists)
                {
                    throw new FileNotFoundException($"The system could not find file [{path.FullName}] as it does not exist.", path.FullName);
                }
                try
                {
                    if (testEffectiveAccess(path))
                    {
                        continue;
                    }
                }
                catch (FileNotFoundException ex)
                {
                    throw new FileNotFoundException($"The Win32 API call could not find file [{path.FullName}] as it does not exist.", path.FullName, ex);
                }
                FileSecurity fileSecurity = FileSystemUtilities.GetAccessControl(path, AccessControlSections.Access);
                fileSecurity.AddAccessRule(fileSystemAccessRule);
                try
                {
                    FileSystemUtilities.SetAccessControl(path, fileSecurity);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to grant [{runAsActiveUser.NTAccount}] the permissions [{_requiredPermissions}] to file [{path.FullName}]. This can occur when the caller can't modify permissions, such as when the file is located on a network share.", ex);
                }
            }
        }

        /// <summary>
        /// Gets the token that the specified user's effective access should be tested with.
        /// </summary>
        /// <remarks>Ordered by cost. Our own token is exact for our own session and opens with a single call, so it is
        /// preferred over brokering another user's, which registers and runs a scheduled task and is worth avoiding
        /// wherever the answer would be the same.</remarks>
        /// <param name="runAsActiveUser">The user whose access is to be tested.</param>
        /// <param name="elevatedTokenType">The type of elevated token to request when one has to be brokered.</param>
        /// <returns>The user's token, or <see langword="null"/> if they have no session of their own or their token cannot be brokered.</returns>
        private static async ValueTask<SafeHandle?> GetEffectiveAccessTokenAsync(RunAsActiveUser runAsActiveUser, ElevatedTokenType elevatedTokenType)
        {
            // Our own token needs nothing brokered, and describes our own session exactly.
            if (runAsActiveUser == AccountUtilities.CallerRunAsActiveUser)
            {
                return TokenManager.GetCurrentProcessToken(TOKEN_ACCESS_MASK.TOKEN_QUERY);
            }

            // A user with no session of their own has no token to get, and any other user's has to be
            // brokered, which isn't possible from every context. Both leave the identifier to test with.
            return runAsActiveUser.SessionId != uint.MaxValue && TokenManager.CanGetUserPrimaryToken
                ? await TokenManager.GetUserPrimaryTokenAsync(runAsActiveUser.SessionId, elevatedTokenType).ConfigureAwait(false)
                : null;
        }

        /// <summary>
        /// Gets the path that contains this assembly (and all required client/server assembly files).
        /// </summary>
        private static readonly FrozenSet<FileInfo> _assemblies = ClientServerUtilities.ClientServerDirectory.GetFiles("*", SearchOption.AllDirectories).ToFrozenSet();

        /// <summary>
        /// Represents the required file system permissions for the operation.
        /// </summary>
        /// <remarks>This field specifies the minimum permissions needed to access and execute files. It
        /// is set to <see cref="FileSystemRights.ReadAndExecute"/>, which allows reading and executing files but not
        /// modifying them.</remarks>
        private const FileSystemRights _requiredPermissions = FileSystemRights.ReadAndExecute;
    }
}
