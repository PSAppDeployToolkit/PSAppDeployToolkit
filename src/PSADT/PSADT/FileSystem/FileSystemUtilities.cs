using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using PSADT.LibraryInterfaces;
using PSADT.SafeHandles;
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
            Span<char> targetPath = stackalloc char[260];
            for (char drive = 'A'; drive <= 'Z'; drive++)
            {
                var driveLetter = drive + ":";
                try
                {
                    Kernel32.QueryDosDevice(driveLetter, targetPath);
                }
                catch
                {
                    continue;
                }
                foreach (var path in targetPath.ToString().Trim('\0').Trim().Split('\0'))
                {
                    var ntPath = path.Trim();
                    if (ntPath.Length > 0)
                    {
                        lookupTable.Add(ntPath, driveLetter);
                    }
                }
                targetPath.Clear();
            }
            return new(lookupTable);
        }

        /// <summary>
        /// Tests whether the specified file can be accessed with the desired access rights.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="desiredAccess"></param>
        /// <returns></returns>
        public static bool TestFileAccess(FileInfo path, FileSystemRights desiredAccess = FileSystemRights.ReadAndExecute)
        {
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
        public static bool TestEffectiveAccess(string path, SecurityIdentifier sid, FileSystemRights desiredAccessMask)
        {
            // Retrieve the security descriptor for the file.
            AdvApi32.GetNamedSecurityInfo(path, SE_OBJECT_TYPE.SE_FILE_OBJECT, OBJECT_SECURITY_INFORMATION.DACL_SECURITY_INFORMATION | OBJECT_SECURITY_INFORMATION.OWNER_SECURITY_INFORMATION | OBJECT_SECURITY_INFORMATION.GROUP_SECURITY_INFORMATION, out var ppsidOwner, out var ppsidGroup, out var ppDacl, out var ppSacl, out var ppSecurityDescriptor);
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
                    // Create a binary representation of the SID and initialize the AuthZ client context.
                    byte[] sidBytes = new byte[sid.BinaryLength]; sid.GetBinaryForm(sidBytes, 0); using var pSID = SafePinnedGCHandle.Alloc(sidBytes);
                    AdvApi32.AuthzInitializeContextFromSid(0, pSID, hAuthzResourceManager, 0, default, IntPtr.Zero, out var phAuthzClientContext);
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
                                return ((FileSystemRights)grantedAccessMask.ReadInt32() & desiredAccessMask) == desiredAccessMask;
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
    }
}
