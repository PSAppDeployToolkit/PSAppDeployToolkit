using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using PSADT.Extensions;
using PSADT.LibraryInterfaces;
using PSADT.SafeHandles;
using Windows.Win32;
using Windows.Win32.Security;
using Windows.Win32.Security.Authorization;
using Windows.Win32.Storage.FileSystem;

namespace PSADT.FileSystem
{
    /// <summary>
    /// A class containing utility methods for file system operations.
    /// </summary>
    public static class FileSystemUtilities
    {
        /// <summary>
        /// Returns a lookup table for NT paths to drive letters.
        /// </summary>
        /// <returns></returns>
        internal static ReadOnlyDictionary<string, string> GetNtPathLookupTable()
        {
            var lookupTable = new Dictionary<string, string> { { @"\Device\Mup", @"\" } };
            Span<char> targetPath = stackalloc char[(int)PInvoke.MAX_PATH];
            foreach (string drive in Environment.GetLogicalDrives())
            {
                var driveLetter = drive.TrimEnd('\\');
                try
                {
                    Kernel32.QueryDosDevice(driveLetter, targetPath);
                }
                catch
                {
                    continue;
                }
                foreach (var path in targetPath.ToString().Split(['\0'], StringSplitOptions.RemoveEmptyEntries))
                {
                    var ntPath = path.TrimRemoveNull();
                    if (ntPath.Length > 0 && !lookupTable.ContainsKey(ntPath))
                    {
                        lookupTable.Add(ntPath, driveLetter);
                    }
                }
                targetPath.Clear();
            }
            return new(lookupTable);
        }

        /// <summary>
        /// Determines whether the specified file path is valid.
        /// </summary>
        /// <param name="path">The file path to validate. Cannot be <see langword="null"/> or empty.</param>
        /// <returns><see langword="true"/> if the specified file path is valid; otherwise, <see langword="false"/>.</returns>
        public static bool IsValidFilePath(string path)
        {
            return IsValidFilePath(path.AsSpan(), 0);
        }

        /// <summary>
        /// Determines whether the specified position in the input represents the start of a valid file path.
        /// </summary>
        /// <remarks>This method supports detecting various types of file paths, including UNC paths, DOS
        /// drive paths, and POSIX-style paths. It accounts for escaped arguments in UNC paths and ensures that such
        /// cases are not misinterpreted as valid paths.</remarks>
        /// <param name="input">The input span of characters to analyze.</param>
        /// <param name="position">The zero-based position within <paramref name="input"/> to check for the start of a file path.</param>
        /// <returns><see langword="true"/> if the specified position marks the beginning of a valid file path; otherwise, <see
        /// langword="false"/>.</returns>
        public static bool IsValidFilePath(ReadOnlySpan<char> input, int position)
        {
            // We're at the start of nothing if we're at the end of the command line.
            if (position >= input.Length)
            {
                return false;
            }

            // Check for UNC path (starts with \\).
            if (position + 1 < input.Length && input[position] == '\\' && input[position + 1] == '\\')
            {
                // If the characters following the initial \\ are more backslashes followed by a quote,
                // it's likely an escaped argument, not a UNC path. Let ParseSingleArgument handle it.
                int p = position + 2;
                while (p < input.Length && input[p] == '\\')
                {
                    p++;
                }
                if (p < input.Length && input[p] == '"')
                {
                    return false;
                }
                return true;
            }

            // Check for DOS drive path (starts with letter:\ or letter:/).
            if (position + 2 < input.Length && char.IsLetter(input[position]) && input[position + 1] == ':' && (input[position + 2] == '\\' || input[position + 2] == '/'))
            {
                return true;
            }

            // Check for POSIX path (starts with /letter/ where letter is a drive letter).
            if (position + 2 < input.Length && input[position] == '/' && char.IsLetter(input[position + 1]) && input[position + 2] == '/')
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Tests whether the specified file can be accessed with the desired access rights.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="desiredAccess"></param>
        /// <returns></returns>
        public static bool TestFileAccess(FileInfo path, FileSystemRights desiredAccess = FileSystemRights.ReadAndExecute)
        {
            // Validate the input path.
            if (null == path)
            {
                throw new ArgumentNullException(nameof(path));
            }

            // Validate that the path exists.
            if (!path.Exists)
            {
                throw new FileNotFoundException($"The specified path does not exist: {path}");
            }

            // Set up the required flags for CreateFile, then see if we can open the file.
            var dwShareMode = FILE_SHARE_MODE.FILE_SHARE_NONE;
            if ((desiredAccess & FileSystemRights.Read) == FileSystemRights.Read)
            {
                dwShareMode |= FILE_SHARE_MODE.FILE_SHARE_READ;
            }
            if ((desiredAccess & FileSystemRights.Write) == FileSystemRights.Write)
            {
                dwShareMode |= FILE_SHARE_MODE.FILE_SHARE_WRITE;
            }
            if ((desiredAccess & FileSystemRights.Delete) == FileSystemRights.Delete)
            {
                dwShareMode |= FILE_SHARE_MODE.FILE_SHARE_DELETE;
            }
            try
            {
                using var hFile = Kernel32.CreateFile(path.FullName, desiredAccess, dwShareMode, null, FILE_CREATION_DISPOSITION.OPEN_EXISTING, FILE_FLAGS_AND_ATTRIBUTES.FILE_ATTRIBUTE_NORMAL);
                if (hFile.IsInvalid)
                {
                    return false;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Determines whether the specified security identifier (SID) has the desired access rights to the specified
        /// file or directory.
        /// </summary>
        /// <remarks>This method performs an access check by retrieving the security descriptor of the
        /// specified file or directory and evaluating the access rights for the provided SID. It uses the Windows
        /// Authorization API to perform the access check.</remarks>
        /// <param name="path">The full path to the file or directory to check access for.</param>
        /// <param name="sid">The security identifier (SID) of the user or group whose access is being checked.</param>
        /// <param name="desiredAccessMask">The access rights to check, specified as a combination of <see cref="FileSystemRights"/> flags.</param>
        /// <returns><see langword="true"/> if the specified SID has the desired access rights to the file or directory;
        /// otherwise, <see langword="false"/>.</returns>
        public static bool TestEffectiveAccess(FileSystemInfo path, SecurityIdentifier sid, FileSystemRights desiredAccessMask)
        {
            return (GetEffectiveAccess(path, sid, desiredAccessMask) & desiredAccessMask) == desiredAccessMask;
        }

        /// <summary>
        /// Tests whether the specified security token has the desired access rights to the given file or directory
        /// path.
        /// </summary>
        /// <remarks>This method evaluates the effective access rights of the provided security token
        /// against the specified file or directory. It ensures that all requested access rights in <paramref
        /// name="desiredAccessMask"/> are granted for the token to return <see langword="true"/>.</remarks>
        /// <param name="path">The file or directory path to check access for. This cannot be null or empty.</param>
        /// <param name="token">The security token representing the user or process whose access is being tested. This cannot be null.</param>
        /// <param name="desiredAccessMask">The access rights to test, specified as a combination of <see
        /// cref="System.Security.AccessControl.FileSystemRights"/> flags.</param>
        /// <returns><see langword="true"/> if the specified token has all the requested access rights to the path; otherwise,
        /// <see langword="false"/>.</returns>
        public static bool TestEffectiveAccess(FileSystemInfo path, SafeHandle token, FileSystemRights desiredAccessMask)
        {
            return (GetEffectiveAccess(path, token, desiredAccessMask) & desiredAccessMask) == desiredAccessMask;
        }

        /// <summary>
        /// Determines the effective access rights for a specified security identifier (SID) on a given file or
        /// directory.
        /// </summary>
        /// <remarks>This method evaluates the effective access rights by considering the specified SID,
        /// the desired access mask, and the security settings of the file or directory at the given path. The result
        /// reflects the actual permissions granted to the SID, taking into account any deny or allow rules in the
        /// access control list (ACL).</remarks>
        /// <param name="path">The file or directory path for which to evaluate access rights.</param>
        /// <param name="sid">The security identifier (SID) of the user or group whose access rights are being evaluated.</param>
        /// <param name="desiredAccessMask">The desired access mask specifying the access rights to evaluate.</param>
        /// <returns>A <see cref="FileSystemRights"/> value representing the effective access rights for the specified SID on the
        /// given path.</returns>
        public static FileSystemRights GetEffectiveAccess(FileSystemInfo path, SecurityIdentifier sid, FileSystemRights desiredAccessMask)
        {
            if (sid is null)
            {
                throw new ArgumentNullException(nameof(sid), "SecurityIdentifier cannot be null.");
            }
            byte[] sidBytes = new byte[sid.BinaryLength]; sid.GetBinaryForm(sidBytes, 0);
            using var pSID = SafePinnedGCHandle.Alloc(sidBytes);
            return GetEffectiveAccess(path, pSID, desiredAccessMask, TokenType.SID);
        }

        /// <summary>
        /// Determines the effective access rights for a specified file or directory based on the provided security
        /// token and desired access mask.
        /// </summary>
        /// <remarks>This method evaluates the effective access rights by considering the security
        /// descriptor of the specified file or directory and the privileges associated with the provided security
        /// token. The result indicates the access rights that the token is allowed based on the specified desired
        /// access mask.</remarks>
        /// <param name="path">The path to the file or directory for which to determine access rights. This cannot be null or empty.</param>
        /// <param name="token">A valid security token representing the user or group for which to evaluate access rights. This cannot be
        /// null or invalid.</param>
        /// <param name="desiredAccessMask">The desired access rights to evaluate, specified as a combination of <see cref="FileSystemRights"/> flags.</param>
        /// <returns>The effective access rights, represented as a <see cref="FileSystemRights"/> value, that the specified token
        /// has for the given path.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="token"/> is null or invalid.</exception>
        public static FileSystemRights GetEffectiveAccess(FileSystemInfo path, SafeHandle token, FileSystemRights desiredAccessMask)
        {
            if (token is null || token.IsInvalid)
            {
                throw new ArgumentNullException(nameof(token), "Token cannot be null or invalid.");
            }
            return GetEffectiveAccess(path, token, desiredAccessMask, TokenType.UserToken);
        }

        /// <summary>
        /// Determines the effective access rights for a specified security identifier (SID) on a file or directory.
        /// </summary>
        /// <remarks>This method evaluates the effective access rights for the specified SID by performing
        /// an access check against the security descriptor of the file or directory. The effective access rights are
        /// determined based on the discretionary access control list (DACL) and the specified desired access
        /// mask.</remarks>
        /// <param name="path">The full path to the file or directory for which to evaluate access rights.</param>
        /// <param name="token">A valid security token representing the user or group for which to evaluate access rights. This cannot be
        /// null or invalid.</param>
        /// <param name="desiredAccessMask">The desired access mask specifying the access rights to evaluate.</param>
        /// <returns>The effective access rights, represented as a <see cref="FileSystemRights"/> value, that the specified SID
        /// has on the file or directory.</returns>
        private static FileSystemRights GetEffectiveAccess(FileSystemInfo path, SafeHandle token, FileSystemRights desiredAccessMask, TokenType tokenType)
        {
            // Validate that the path exists.
            if (!path.Exists)
            {
                if (path is DirectoryInfo)
                {
                    throw new DirectoryNotFoundException($"The specified directory does not exist: {path}");
                }
                else
                {
                    throw new FileNotFoundException($"The specified file does not exist: {path}");
                }
            }

            // Retrieve the security descriptor for the file.
            AdvApi32.GetNamedSecurityInfo(path.FullName, SE_OBJECT_TYPE.SE_FILE_OBJECT, OBJECT_SECURITY_INFORMATION.DACL_SECURITY_INFORMATION | OBJECT_SECURITY_INFORMATION.OWNER_SECURITY_INFORMATION | OBJECT_SECURITY_INFORMATION.GROUP_SECURITY_INFORMATION, out var ppsidOwner, out var ppsidGroup, out var ppDacl, out var ppSacl, out var ppSecurityDescriptor);
            using (ppSecurityDescriptor)
            using (ppsidOwner)
            using (ppsidGroup)
            using (ppDacl)
            using (ppSacl)
            {
                // Initialize the AuthZ resource manager and client context.
                AdvApi32.AuthzInitializeResourceManager(AUTHZ_RESOURCE_MANAGER_FLAGS.AUTHZ_RM_FLAG_NO_AUDIT, null, null, null, "PS-Authz", out var hAuthzResourceManager);
                using (hAuthzResourceManager)
                {
                    // Initialize the AuthZ client context.
                    AuthzFreeContextSafeHandle phAuthzClientContext;
                    switch (tokenType)
                    {
                        case TokenType.SID:
                            AdvApi32.AuthzInitializeContextFromSid(0, token, hAuthzResourceManager, null, default, IntPtr.Zero, out phAuthzClientContext);
                            break;
                        case TokenType.UserToken:
                            AdvApi32.AuthzInitializeContextFromToken(0, token, hAuthzResourceManager, null, default, IntPtr.Zero, out phAuthzClientContext);
                            break;
                        default:
                            throw new ArgumentException("Invalid token type specified.", nameof(tokenType));
                    }
                    using (var grantedAccessMask = SafeHGlobalHandle.Alloc(sizeof(uint)))
                    using (var error = SafeHGlobalHandle.Alloc(sizeof(uint)))
                    using (phAuthzClientContext)
                    {
                        bool grantedAccessMaskAddRef = false;
                        bool errorAddRef = false;
                        try
                        {
                            // Prepare the access request and reply structures.
                            var req = new AUTHZ_ACCESS_REQUEST { DesiredAccess = (uint)desiredAccessMask };
                            var reply = new AUTHZ_ACCESS_REPLY { ResultListLength = 1 };
                            unsafe
                            {
                                grantedAccessMask.DangerousAddRef(ref grantedAccessMaskAddRef);
                                reply.GrantedAccessMask = (uint*)grantedAccessMask.DangerousGetHandle();
                                error.DangerousAddRef(ref errorAddRef);
                                reply.Error = (uint*)error.DangerousGetHandle();
                            }

                            // Perform the access check.
                            AdvApi32.AuthzAccessCheck(0, phAuthzClientContext, in req, null, ppSecurityDescriptor, null, ref reply, out var phAccessCheckResults);
                            using (phAccessCheckResults)
                            {
                                return (FileSystemRights)grantedAccessMask.ReadInt32();
                            }
                        }
                        finally
                        {
                            if (grantedAccessMaskAddRef)
                            {
                                grantedAccessMask.DangerousRelease();
                            }
                            if (errorAddRef)
                            {
                                error.DangerousRelease();
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Represents the types of tokens that can be safely handled within the system.
        /// </summary>
        /// <remarks>This enumeration defines the specific categories of tokens, such as security
        /// identifiers (SID) and user tokens, that are used in the context of secure operations.</remarks>
        private enum TokenType
        {
            SID,
            UserToken,
        }
    }
}
