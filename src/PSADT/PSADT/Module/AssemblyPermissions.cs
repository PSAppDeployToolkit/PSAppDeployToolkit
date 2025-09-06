﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using Microsoft.Win32.SafeHandles;
using PSADT.FileSystem;
using PSADT.ProcessManagement;

namespace PSADT.Module
{
    /// <summary>
    /// Provides methods for managing and ensuring file system permissions for specific users and file paths.
    /// </summary>
    /// <remarks>The <see cref="AssemblyPermissions"/> class includes functionality to verify and remediate file
    /// system permissions for a user on specified file paths. It ensures that the user has the required permissions to
    /// access and execute files, without modifying them unnecessarily. This class is designed for scenarios where file
    /// system access control needs to be programmatically enforced.</remarks>
    internal static class AssemblyPermissions
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
        /// <param name="useLinkedAdminToken">Specifies whether to use the RunAsActiveUser's linked admin token only.</param>
        /// <param name="useHighestAvailableToken">Specifies whether to use the RunAsActiveUser's linked admin token if available.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="runAsActiveUser"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown if any path in <paramref name="extraPaths"/> is not an absolute path.</exception>
        /// <exception cref="FileNotFoundException">Thrown if any path in <paramref name="extraPaths"/> or the default assemblies does not exist.</exception>
        internal static void Remediate(RunAsActiveUser runAsActiveUser, IReadOnlyList<FileInfo>? extraPaths = null, bool useLinkedAdminToken = false, bool useHighestAvailableToken = false)
        {
            // Validate the runAsActiveUser parameter.
            if (null == runAsActiveUser)
            {
                throw new ArgumentNullException(nameof(runAsActiveUser), "RunAsActiveUser cannot be null.");
            }

            // Get the primary token for the user if they have a valid session ID, then proceed to check and remediate file system permissions.
            using SafeFileHandle? hPrimaryToken = runAsActiveUser.SessionId != uint.MaxValue ? ProcessToken.GetUserPrimaryToken(runAsActiveUser, useLinkedAdminToken, useHighestAvailableToken) : null;
            FileSystemAccessRule fileSystemAccessRule = new(runAsActiveUser.SID, _requiredPermissions, InheritanceFlags.None, PropagationFlags.None, AccessControlType.Allow);
            foreach (var path in _assemblies.Concat(extraPaths ?? []))
            {
                if (!Path.IsPathRooted(path.FullName))
                {
                    throw new ArgumentException($"The path [{path.FullName}] is not rooted. All paths must be absolute.", nameof(extraPaths));
                }
                if (!path.Exists)
                {
                    throw new FileNotFoundException($"The file [{path.FullName}] does not exist.", path.FullName);
                }
                if (null != hPrimaryToken ? FileSystemUtilities.TestEffectiveAccess(path.FullName, hPrimaryToken, _requiredPermissions) : FileSystemUtilities.TestEffectiveAccess(path.FullName, runAsActiveUser.SID, _requiredPermissions))
                {
                    continue;
                }
                FileSecurity fileSecurity = FileSystemAclExtensions.GetAccessControl(path, AccessControlSections.Access);
                fileSecurity.AddAccessRule(fileSystemAccessRule);
                FileSystemAclExtensions.SetAccessControl(path, fileSecurity);
            }
        }

        /// <summary>
        /// Represents the required file system permissions for the operation.
        /// </summary>
        /// <remarks>This field specifies the minimum permissions needed to access and execute files. It
        /// is set to <see cref="FileSystemRights.ReadAndExecute"/>, which allows reading and executing files but not
        /// modifying them.</remarks>
        private static readonly FileSystemRights _requiredPermissions = FileSystemRights.ReadAndExecute;

        /// <summary>
        /// Gets the path that contains this assembly (and all required client/server assembly files).
        /// </summary>
        private static readonly ReadOnlyCollection<FileInfo> _assemblies = Directory.GetFiles(Path.GetDirectoryName(typeof(AssemblyPermissions).Assembly.Location)!, "*", SearchOption.AllDirectories).Select(static f => new FileInfo(f)).ToList().AsReadOnly();
    }
}
