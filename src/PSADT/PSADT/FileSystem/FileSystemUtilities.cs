using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using PSADT.Interop;
using PSADT.Interop.Extensions;
using PSADT.Interop.SafeHandles;
using PSADT.SafeHandles;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Security;
using Windows.Win32.Security.Authorization;
using Windows.Win32.Security.WinTrust;
using Windows.Win32.Storage.FileSystem;

namespace PSADT.FileSystem
{
    /// <summary>
    /// A class containing utility methods for file system operations.
    /// </summary>
    public static class FileSystemUtilities
    {
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
                return p >= input.Length || input[p] != '"';
            }

            // Check for DOS drive path (starts with letter:\ or letter:/).
            if (position + 2 < input.Length && char.IsLetter(input[position]) && input[position + 1] == ':' && (input[position + 2] == '\\' || input[position + 2] == '/'))
            {
                return true;
            }

            // Check for POSIX path (starts with /letter/ where letter is a drive letter).
            return position + 2 < input.Length && input[position] == '/' && char.IsLetter(input[position + 1]) && input[position + 2] == '/';
        }

        /// <summary>
        /// Calculates the total logical size, in bytes, of all files within the specified directory and its
        /// subdirectories.
        /// </summary>
        /// <remarks>This method performs a parallel, breadth-first traversal of the directory tree to
        /// optimize performance and minimize memory usage, especially for large directory structures. Reparse points
        /// (such as symbolic links, junctions, and mount points) are skipped to avoid cycles. The method does not
        /// support cancellation or progress reporting. Only the logical file sizes are included; directory sizes and
        /// metadata are excluded.</remarks>
        /// <param name="rootPath">The full path to the root directory whose contents will be scanned. Cannot be null, empty, or whitespace.
        /// The directory must exist.</param>
        /// <returns>The total logical size, in bytes, of all files within the specified directory and its subdirectories.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="rootPath"/> is null, empty, or consists only of whitespace.</exception>
        /// <exception cref="DirectoryNotFoundException">Thrown if the directory specified by <paramref name="rootPath"/> does not exist.</exception>
        public static long GetLogicalSizeBytes(string rootPath)
        {
            // Internal helper method to trim trailing slashes off paths.
            static string TrimTrailingSeparators(string path)
            {
                while (path.Length > 3 && (path.EndsWith("\\") || path.EndsWith("/")))
                {
                    path = path.Substring(0, path.Length - 1);
                }
                return path;
            }

            // Note: this is a breadth-first parallel enumeration. It is "fire and forget" in that it does not support cancellation, progress,
            // or partial results. It is optimized for speed and low memory usage on large directory trees with many files and subdirectories.
            using BlockingCollection<string> queue = [(rootPath = TrimTrailingSeparators(Path.GetFullPath(rootPath.ThrowIfNullOrWhiteSpace()))).ThrowIfDirectoryDoesNotExist()];
            Task[] tasks = new Task[Math.Max(4, Math.Min(Environment.ProcessorCount * 2, 32))];
            long totalBytes = 0; int pendingDirs = 1; int completed = 0;
            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i] = Task.Run(() =>
                {
                    foreach (string dir in queue.GetConsumingEnumerable())
                    {
                        try
                        {
                            // Try to enumerate the directory. If it fails, skip it and move on (e.g. due to access denied, deleted/moved, etc.).\
                            // Note that we use FindFirstFileEx with FindExInfoBasic and FindExSearchNameMatch to minimize overhead.
                            FindCloseSafeHandle hFind;
                            WIN32_FIND_DATAW data;
                            unsafe
                            {
                                hFind = PInvoke.FindFirstFileEx(dir + "\\*", FINDEX_INFO_LEVELS.FindExInfoBasic, &data, FINDEX_SEARCH_OPS.FindExSearchNameMatch, FIND_FIRST_EX_FLAGS.FIND_FIRST_EX_LARGE_FETCH);
                            }
                            if (hFind.IsInvalid)
                            {
                                hFind.Dispose();
                                continue;
                            }

                            // Process the first result and then continue with FindNextFile in a loop until there are no more results. For each subdirectory, add it to the queue for processing.
                            using (hFind)
                            {
                                do
                                {
                                    // Validate the file name and skip "." and ".." entries.
                                    string name = data.cFileName.ToString();
                                    if (name is "." or "..")
                                    {
                                        continue;
                                    }

                                    // Check if this is a directory or a file. For directories, we add them to the queue for processing. For files, we add their size to the total.
                                    if (((FileAttributes)data.dwFileAttributes & FileAttributes.Directory) != 0)
                                    {
                                        // Skip reparse points (e.g. symbolic links, junctions, mount points) to avoid potential cycles.
                                        if (((FileAttributes)data.dwFileAttributes & FileAttributes.ReparsePoint) != 0)
                                        {
                                            continue;
                                        }

                                        // Increment the pending directory count before adding to the queue.
                                        _ = Interlocked.Increment(ref pendingDirs);
                                        try
                                        {
                                            queue.Add(dir + "\\" + name);
                                        }
                                        catch
                                        {
                                            _ = Interlocked.Decrement(ref pendingDirs);
                                            throw;
                                        }
                                    }
                                    else
                                    {
                                        ulong size = ((ulong)data.nFileSizeHigh << 32) | data.nFileSizeLow;
                                        if (size != 0)
                                        {
                                            _ = Interlocked.Add(ref totalBytes, unchecked((long)size));
                                        }
                                    }
                                }
                                while (PInvoke.FindNextFile(hFind, out data));
                            }
                        }
                        finally
                        {
                            if (Interlocked.Decrement(ref pendingDirs) == 0 && Interlocked.Exchange(ref completed, 1) == 0)
                            {
                                queue.CompleteAdding();
                            }
                        }
                    }
                });
            }
            Task.WaitAll(tasks);
            return totalBytes;
        }

        /// <summary>
        /// Determines whether the current process has the specified access rights to a given file.
        /// </summary>
        /// <remarks>This method attempts to open the specified file with the requested access rights
        /// using the Windows API. It does not modify the file or its permissions. The result indicates whether the
        /// current process can access the file as requested.</remarks>
        /// <param name="path">A <see cref="FileInfo"/> object representing the file to test. This parameter cannot be null and must refer
        /// to an existing file.</param>
        /// <param name="desiredAccess">The access rights to test for the file. Defaults to <see cref="FileSystemRights.ReadAndExecute"/> if not
        /// specified.</param>
        /// <returns>true if the specified access rights are granted for the file; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="path"/> is null.</exception>
        /// <exception cref="FileNotFoundException">Thrown if the file specified by <paramref name="path"/> does not exist.</exception>
        public static bool TestFileAccess(FileInfo path, FileSystemRights desiredAccess = FileSystemRights.ReadAndExecute)
        {
            // Validate the input path.
            if (path is null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            // Validate that the path exists.
            if (!path.Exists)
            {
                throw new FileNotFoundException($"The specified file does not exist: {path.FullName}", path.FullName);
            }

            // Set up the required flags for CreateFile, then see if we can open the file.
            FILE_SHARE_MODE dwShareMode = FILE_SHARE_MODE.FILE_SHARE_NONE;
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
                using SafeFileHandle hFile = NativeMethods.CreateFile(path.FullName, desiredAccess, dwShareMode, null, FILE_CREATION_DISPOSITION.OPEN_EXISTING, FileAttributes.Normal);
                return !hFile.IsInvalid;
            }
            catch
            {
                return false;
                throw;
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
        /// cref="FileSystemRights"/> flags.</param>
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
            using SafePinnedGCHandle pSID = SafePinnedGCHandle.Alloc(sidBytes);
            return GetEffectiveAccess(path, pSID, desiredAccessMask, NativeMethods.AuthzInitializeContextFromSid);
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Enforcing this rule just makes a mess.")]
        public static FileSystemRights GetEffectiveAccess(FileSystemInfo path, SafeHandle token, FileSystemRights desiredAccessMask)
        {
            return GetEffectiveAccess(path, token, desiredAccessMask, NativeMethods.AuthzInitializeContextFromToken);
        }

        /// <summary>
        /// Resets the permissions for the specified directory path, enabling inheritance and propagating inheritable
        /// permissions to all child objects.
        /// </summary>
        /// <remarks>This method performs the following actions: <list type="bullet"> <item>Enables
        /// inheritance for the specified directory by setting an empty Discretionary Access Control List (DACL).</item>
        /// <item>Propagates inheritable permissions from the directory to all child objects, effectively replacing
        /// existing child object permissions with the inheritable entries.</item> </list> Use this method to restore
        /// default permission inheritance behavior for a directory and its contents.</remarks>
        /// <param name="path">The absolute path of the directory for which permissions will be reset. The path must exist and cannot be
        /// null, empty, or whitespace.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="path"/> is null, empty, or consists only of whitespace.</exception>
        /// <exception cref="DirectoryNotFoundException">Thrown if the directory specified by <paramref name="path"/> does not exist.</exception>
        public static void ResetPermissionsForPath(string path)
        {
            // Define the flags for setting and getting security information.
            OBJECT_SECURITY_INFORMATION getSiFlags = OBJECT_SECURITY_INFORMATION.DACL_SECURITY_INFORMATION;
            OBJECT_SECURITY_INFORMATION setSiFlags = getSiFlags | OBJECT_SECURITY_INFORMATION.UNPROTECTED_DACL_SECURITY_INFORMATION;

            // Create an empty ACL for the purpose of enabling inheritance, then set it on the path.
            Span<byte> pEmptyAcl = stackalloc byte[Marshal.SizeOf<ACL>()]; _ = NativeMethods.InitializeAcl(pEmptyAcl, ACE_REVISION.ACL_REVISION);
            _ = NativeMethods.SetNamedSecurityInfo(path.ThrowIfDirectoryDoesNotExist(), SE_OBJECT_TYPE.SE_FILE_OBJECT, setSiFlags, default, default, pEmptyAcl, default);

            // Retrieve the set security descriptor for the path and reapply it to all child objects. This is the same as the
            // "Replace all child object permission entries with inheritable permission entries from this object" checkbox.
            _ = NativeMethods.GetNamedSecurityInfo(path, SE_OBJECT_TYPE.SE_FILE_OBJECT, getSiFlags, out SafeNoReleaseHandle? ppsidOwner, out SafeNoReleaseHandle? ppsidGroup, out LocalFreeSafeHandle? ppDacl, out LocalFreeSafeHandle? ppSacl, out LocalFreeSafeHandle ppSecurityDescriptor);
            using (ppSecurityDescriptor)
            using (ppsidOwner)
            using (ppsidGroup)
            using (ppDacl)
            using (ppSacl)
            {
                _ = NativeMethods.TreeResetNamedSecurityInfo(path, SE_OBJECT_TYPE.SE_FILE_OBJECT, setSiFlags, ppsidOwner, ppsidGroup, ppDacl, ppSacl, false, null, PROG_INVOKE_SETTING.ProgressInvokeNever);
            }
        }

        /// <summary>
        /// Creates a read-only dictionary that maps NT device paths to their corresponding drive letters.
        /// </summary>
        /// <remarks>This method queries the system for logical drives and their corresponding NT device
        /// paths. It handles exceptions that may occur during the query process, ensuring that only valid paths are
        /// included in the returned dictionary.</remarks>
        /// <returns>A read-only dictionary where each key is an NT device path and each value is the associated drive letter.</returns>
        internal static ReadOnlyDictionary<string, string> MakeNtPathLookupTable()
        {
            Dictionary<string, string> lookupTable = new() { { @"\Device\Mup", @"\" } };
            Span<char> targetPath = stackalloc char[1024]; targetPath.Clear();
            foreach (string driveLetter in Environment.GetLogicalDrives().Select(static l => l.TrimEnd('\\')))
            {
                try
                {
                    _ = NativeMethods.QueryDosDevice(driveLetter, targetPath);
                }
                catch
                {
                    continue;
                    throw;
                }
                foreach (string path in targetPath.ToString().Split(['\0'], StringSplitOptions.RemoveEmptyEntries))
                {
                    if (path.Length > 0 && !lookupTable.ContainsKey(path))
                    {
                        lookupTable.Add(path, driveLetter);
                    }
                }
                targetPath.Clear();
            }
            return new(lookupTable);
        }

        /// <summary>
        /// Determines whether the specified file is trusted based on its Authenticode signature.
        /// </summary>
        /// <remarks>This method performs a verification of the file's Authenticode signature using the
        /// WinVerifyTrust API. It does not perform any network communications during the verification
        /// process.</remarks>
        /// <param name="filePath">The path to the file to be verified. This parameter cannot be null or empty, and the file must exist.</param>
        /// <returns>true if the file is trusted; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="filePath"/> is null or empty.</exception>
        /// <exception cref="FileNotFoundException">Thrown if the specified file does not exist.</exception>
        public static bool IsAuthenticodeTrusted(string filePath)
        {
            // Load up everything we need for WinVerifyTrust. The CsWin32 projects this
            // all using pointers, so we must follow suite as well or roll our own setup.
            unsafe
            {
                fixed (char* pFilePath = filePath.ThrowIfFileDoesNotExist())
                {
                    // Set up WINTRUST_DATA to not perform any network comms.
                    WINTRUST_FILE_INFO wtFileInfo = new()
                    {
                        cbStruct = (uint)Marshal.SizeOf<WINTRUST_FILE_INFO>(),
                        pcwszFilePath = pFilePath,
                    };
                    WINTRUST_DATA wtData = new()
                    {
                        cbStruct = (uint)Marshal.SizeOf<WINTRUST_DATA>(),
                        dwUIChoice = WINTRUST_DATA_UICHOICE.WTD_UI_NONE,
                        fdwRevocationChecks = WINTRUST_DATA_REVOCATION_CHECKS.WTD_REVOKE_NONE,
                        dwUnionChoice = WINTRUST_DATA_UNION_CHOICE.WTD_CHOICE_FILE,
                        Anonymous = new() { pFile = &wtFileInfo },
                        dwStateAction = WINTRUST_DATA_STATE_ACTION.WTD_STATEACTION_IGNORE,
                        dwProvFlags = WINTRUST_DATA_PROVIDER_FLAGS.WTD_CACHE_ONLY_URL_RETRIEVAL,
                        dwUIContext = WINTRUST_DATA_UICONTEXT.WTD_UICONTEXT_EXECUTE,
                    };
                    try
                    {
                        Guid guid = PInvoke.WINTRUST_ACTION_GENERIC_VERIFY_V2;
                        HWND handle = (HWND)(nint)HANDLE.INVALID_HANDLE_VALUE;
                        return NativeMethods.WinVerifyTrust(handle, ref guid, &wtData) == HRESULT.S_OK;
                    }
                    catch
                    {
                        return false;
                        throw;
                    }
                }
            }
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
        /// <param name="AuthzInitializeContext">A callback function used to initialize the AuthZ client context for access evaluation. This function is
        /// invoked with the provided token and resource manager.</param>
        /// <returns>The effective access rights, represented as a <see cref="FileSystemRights"/> value, that the specified SID
        /// has on the file or directory.</returns>
        /// <exception cref="DirectoryNotFoundException">Thrown if the specified path refers to a directory that does not exist.</exception>
        /// <exception cref="FileNotFoundException">Thrown if the specified path refers to a file that does not exist.</exception>
        private static FileSystemRights GetEffectiveAccess(FileSystemInfo path, SafeHandle token, FileSystemRights desiredAccessMask, AuthzInitializeContext AuthzInitializeContext)
        {
            // Validate that the path exists.
            if (path is null)
            {
                throw new ArgumentNullException(nameof(path), "Path cannot be null.");
            }
            if (!path.Exists)
            {
                if (path is DirectoryInfo)
                {
                    throw new DirectoryNotFoundException($"The specified directory does not exist: {path.FullName}");
                }
                else
                {
                    throw new FileNotFoundException($"The specified file does not exist: {path.FullName}", path.FullName);
                }
            }

            // Retrieve the security descriptor for the file.
            _ = NativeMethods.GetNamedSecurityInfo(path.FullName, SE_OBJECT_TYPE.SE_FILE_OBJECT, OBJECT_SECURITY_INFORMATION.DACL_SECURITY_INFORMATION | OBJECT_SECURITY_INFORMATION.OWNER_SECURITY_INFORMATION | OBJECT_SECURITY_INFORMATION.GROUP_SECURITY_INFORMATION, out SafeNoReleaseHandle? ppsidOwner, out SafeNoReleaseHandle? ppsidGroup, out LocalFreeSafeHandle? ppDacl, out LocalFreeSafeHandle? ppSacl, out LocalFreeSafeHandle ppSecurityDescriptor);
            using (ppSecurityDescriptor)
            using (ppsidOwner)
            using (ppsidGroup)
            using (ppDacl)
            using (ppSacl)
            {
                // Initialize the AuthZ resource manager and client context.
                _ = NativeMethods.AuthzInitializeResourceManager(AUTHZ_RESOURCE_MANAGER_FLAGS.AUTHZ_RM_FLAG_NO_AUDIT, null, null, null, "PS-Authz", out AuthzFreeResourceManagerSafeHandle hAuthzResourceManager);
                using (hAuthzResourceManager)
                {
                    // Initialize the AuthZ client context.
                    _ = AuthzInitializeContext(0, token.ThrowIfNullOrInvalid(), hAuthzResourceManager, null, default, default, out AuthzFreeContextSafeHandle phAuthzClientContext);
                    using (phAuthzClientContext)
                    {
                        // Prepare the access request and reply structures.
                        AUTHZ_ACCESS_REQUEST req = new() { DesiredAccess = (uint)desiredAccessMask };
                        AUTHZ_ACCESS_REPLY reply = new() { ResultListLength = 1 };
                        uint grantedAccessMask, error;
                        unsafe
                        {
                            reply.GrantedAccessMask = &grantedAccessMask;
                            reply.Error = &error;
                        }

                        // Perform the access check.
                        _ = NativeMethods.AuthzAccessCheck(0, phAuthzClientContext, in req, null, ppSecurityDescriptor, null, ref reply, out AuthzFreeHandleSafeHandle phAccessCheckResults);
                        using (phAccessCheckResults)
                        {
                            return (FileSystemRights)grantedAccessMask;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Represents a callback method that initializes an authorization context for use with the Authz API.
        /// </summary>
        /// <remarks>This delegate is typically used to customize the initialization of authorization
        /// contexts in advanced scenarios, such as when integrating with the Windows Authz API. The caller is
        /// responsible for ensuring that all handles provided remain valid for the duration of the callback.</remarks>
        /// <param name="Flags">A set of flags that specify options for context initialization. The value must be a valid combination of
        /// AUTHZ_CONTEXT_FLAGS.</param>
        /// <param name="Handle">A handle to a security token or object used as the basis for the new authorization context. This handle must
        /// be valid and remain open for the duration of the callback.</param>
        /// <param name="hAuthzResourceManager">A handle to the resource manager with which the authorization context is associated. This handle must be
        /// valid.</param>
        /// <param name="pExpirationTime">The expiration time, in 100-nanosecond intervals since January 1, 1601 (UTC), for the authorization context,
        /// or null if no expiration is set.</param>
        /// <param name="Identifier">A reference to a locally unique identifier (LUID) that uniquely identifies the authorization context.</param>
        /// <param name="DynamicGroupArgs">A pointer to application-defined data used to compute dynamic groups for the context. This value may be null
        /// if not required.</param>
        /// <param name="phAuthzClientContext">When this method returns, contains a handle to the newly created authorization client context.</param>
        /// <returns>A BOOL value that is nonzero if the context was successfully initialized; otherwise, zero.</returns>
        private delegate BOOL AuthzInitializeContext(AUTHZ_CONTEXT_FLAGS Flags, SafeHandle Handle, SafeHandle hAuthzResourceManager, long? pExpirationTime, in LUID Identifier, nint DynamicGroupArgs, out AuthzFreeContextSafeHandle phAuthzClientContext);
    }
}
