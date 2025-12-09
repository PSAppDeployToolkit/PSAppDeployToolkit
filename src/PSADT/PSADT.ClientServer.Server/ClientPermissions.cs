using System;
using System.Collections.Generic;
using System.IO;
using PSADT.Core;

namespace PSADT.ClientServer
{
    /// <summary>
    /// Provides methods for managing and ensuring file system permissions for specific users and file paths.
    /// </summary>
    /// <remarks>The <see cref="ClientPermissions"/> class includes functionality to verify and remediate file
    /// system permissions for a user on specified file paths. It ensures that the user has the required permissions to
    /// access and execute files, without modifying them unnecessarily. This class is designed for scenarios where file
    /// system access control needs to be programmatically enforced.</remarks>
    public static class ClientPermissions
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
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="runAsActiveUser"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown if any path in <paramref name="extraPaths"/> is not an absolute path.</exception>
        /// <exception cref="FileNotFoundException">Thrown if any path in <paramref name="extraPaths"/> or the default assemblies does not exist.</exception>
        public static void Remediate(RunAsActiveUser runAsActiveUser, IReadOnlyList<FileInfo>? extraPaths)
        {
            AssemblyPermissions.Remediate(runAsActiveUser, extraPaths, ServerInstance.UseLinkedAdminToken, ServerInstance.UseHighestAvailableToken);
        }
    }
}
