using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading.Tasks;
using PSADT.AccountManagement;
using PSADT.FileSystem;
using PSADT.Security;

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
        /// <param name="extraPaths">An optional list of additional file paths to include in the remediation process. All paths must be absolute
        /// and point to existing files.</param>
        /// <param name="elevatedTokenType">An optional parameter specifying the type of elevated token to use when checking permissions.
        /// The default value is <see cref="ElevatedTokenType.None"/>.</param>
        /// <exception cref="DriveNotFoundException">Thrown if any path in <paramref name="extraPaths"/> is not an absolute path.</exception>
        /// <exception cref="FileNotFoundException">Thrown if any path in <paramref name="extraPaths"/> or the default assemblies does not exist.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the permissions cannot be modified for any path due to insufficient privileges or if the file is located on a network share.</exception>"
        internal static async ValueTask Remediate(RunAsActiveUser runAsActiveUser, IReadOnlyList<FileInfo>? extraPaths = null, ElevatedTokenType elevatedTokenType = ElevatedTokenType.None)
        {
            // Get the primary token for the user if they have a valid session ID, otherwise we'll just use their SID.
            using WindowsIdentity currentUser = WindowsIdentity.GetCurrent();
            using SafeHandle? hPrimaryToken = runAsActiveUser.SessionId != uint.MaxValue
                ? runAsActiveUser != AccountUtilities.CallerRunAsActiveUser || TokenManager.CanGetUserPrimaryToken
                ? await TokenManager.GetUserPrimaryTokenAsync(runAsActiveUser.SessionId, elevatedTokenType).ConfigureAwait(false)
                : currentUser.AccessToken
                : null;
            Func<FileInfo, bool> testEffectiveAccess = hPrimaryToken is null
                ? (path) => FileSystemUtilities.TestEffectiveAccess(path, runAsActiveUser.SID, _requiredPermissions)
                : (path) => FileSystemUtilities.TestEffectiveAccess(path, hPrimaryToken, _requiredPermissions);

            // Test each individual file and remediate ACLs as required, using the miminum read/execute we require.
            FileSystemAccessRule fileSystemAccessRule = new(runAsActiveUser.SID, _requiredPermissions, InheritanceFlags.None, PropagationFlags.None, AccessControlType.Allow);
            foreach (FileInfo path in extraPaths?.Count > 0 ? _assemblies.Concat(extraPaths) : _assemblies)
            {
                if (!Path.IsPathFullyQualified(path.FullName))
                {
                    throw new DriveNotFoundException($"The path [{path.FullName}] is not rooted. All paths must be absolute.");
                }
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
                catch (Exception ex) when (ex.Message is not null)
                {
                    throw new InvalidOperationException($"Failed to grant [{runAsActiveUser.NTAccount}] the permissions [{_requiredPermissions}] to file [{path.FullName}]. This can occur when the caller can't modify permissions, such as when the file is located on a network share.", ex);
                }
            }
        }

        /// <summary>
        /// Determines whether the Local System account has the required file system permissions for all client/server assembly files.
        /// </summary>
        /// <returns>True if the Local System account has the required permissions; otherwise, false.</returns>
        internal static bool SystemAccountHasPermissions()
        {
            return _assemblies.All(path => FileSystemUtilities.TestEffectiveAccess(path, AccountUtilities.LocalSystemSid, _requiredPermissions));
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
