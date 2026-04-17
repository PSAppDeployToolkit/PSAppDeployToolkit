using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using Microsoft.Win32.SafeHandles;
using PSADT.Extensions;
using PSADT.FileSystem;
using PSADT.Security;

namespace PSADT.Foundation
{
    /// <summary>
    /// Provides utility methods and fields for managing assembly-related file system operations, including permission
    /// remediation and assembly path discovery.
    /// </summary>
    /// <remarks>This class is intended for internal use to facilitate file system permission checks and
    /// access to assembly locations required by the application. It is not intended to be used directly by external
    /// consumers.</remarks>
    internal static class AssemblyManager
    {
        /// <summary>
        /// Initializes static members of the AssemblyManager class by determining the file path of the calling process.
        /// </summary>
        /// <remarks>This static constructor retrieves the executable path of the current process and
        /// assigns it to the CallingProcessPath property. This ensures that the path is available for use by other
        /// static members of the class.</remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1810:Initialize reference type static fields inline", Justification = "Needs to be in a static initialiser for process disposal management.")]
        static AssemblyManager()
        {
            using Process currentProcess = Process.GetCurrentProcess();
            CallingProcessPath = currentProcess.GetFilePath();
        }

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
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="runAsActiveUser"/> is <see langword="null"/>.</exception>
        /// <exception cref="DriveNotFoundException">Thrown if any path in <paramref name="extraPaths"/> is not an absolute path.</exception>
        /// <exception cref="FileNotFoundException">Thrown if any path in <paramref name="extraPaths"/> or the default assemblies does not exist.</exception>
        internal static void RemediatePermissions(RunAsActiveUser runAsActiveUser, IReadOnlyList<FileInfo>? extraPaths = null, ElevatedTokenType elevatedTokenType = ElevatedTokenType.None)
        {
            // Get the primary token for the user if they have a valid session ID, then proceed to check and remediate file system permissions.
            using SafeFileHandle? hPrimaryToken = runAsActiveUser.SessionId != uint.MaxValue ? TokenManager.GetUserPrimaryToken(runAsActiveUser.SessionId, elevatedTokenType) : null;
            FileSystemAccessRule fileSystemAccessRule = new(runAsActiveUser.SID, _requiredPermissions, InheritanceFlags.None, PropagationFlags.None, AccessControlType.Allow);
            foreach (FileInfo path in _assemblies.Concat(extraPaths ?? []))
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
                    if (hPrimaryToken is not null ? FileSystemUtilities.TestEffectiveAccess(path, hPrimaryToken, _requiredPermissions) : FileSystemUtilities.TestEffectiveAccess(path, runAsActiveUser.SID, _requiredPermissions))
                    {
                        continue;
                    }
                }
                catch (FileNotFoundException ex)
                {
                    throw new FileNotFoundException($"The Win32 API call could not find file [{path.FullName}] as it does not exist.", path.FullName, ex);
                }
                FileSecurity fileSecurity = path.GetAccessControl(AccessControlSections.Access);
                fileSecurity.AddAccessRule(fileSystemAccessRule);
                try
                {
                    path.SetAccessControl(fileSecurity);
                }
                catch (Exception ex) when (ex.Message is not null)
                {
                    throw new InvalidOperationException($"Failed to grant [{runAsActiveUser.NTAccount}] the permissions [{_requiredPermissions}] to file [{path.FullName}]. This can occur when the caller can't modify permissions, such as when the file is located on a network share.", ex);
                }
            }
        }

        /// <summary>
        /// Gets the file path of the process that initiated the current execution context.
        /// </summary>
        internal static readonly FileInfo CallingProcessPath;

        /// <summary>
        /// Gets the directory path of this assembly.
        /// </summary>
        /// <remarks>This field can be used to locate files or resources relative to the assembly's
        /// location at runtime. The returned path is determined by the assembly's current location, which may vary
        /// depending on how the application is deployed or executed.</remarks>
        internal static readonly DirectoryInfo AssemblyDirectory = new(Path.GetDirectoryName(typeof(AssemblyManager).Assembly.Location) ?? throw new InvalidProgramException("Failed to retrieve directory for this assembly."));

        /// <summary>
        /// Gets the path that contains this assembly (and all required client/server assembly files).
        /// </summary>
        private static readonly ReadOnlyCollection<FileInfo> _assemblies = new(AssemblyDirectory.GetFiles("*.dll", SearchOption.AllDirectories));

        /// <summary>
        /// Represents the required file system permissions for the operation.
        /// </summary>
        /// <remarks>This field specifies the minimum permissions needed to access and execute files. It
        /// is set to <see cref="FileSystemRights.ReadAndExecute"/>, which allows reading and executing files but not
        /// modifying them.</remarks>
        private const FileSystemRights _requiredPermissions = FileSystemRights.ReadAndExecute;
    }
}
