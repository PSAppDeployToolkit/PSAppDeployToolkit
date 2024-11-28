using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Security;
using System.Security.Principal;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using PSADT.PInvoke;

namespace PSADT.ProcessUtilities
{
    /// <summary>
    /// Provides functionality to detect processes that have locks on files or directories.
    /// </summary>
    public static class Handle
    {
        /// <summary>
        /// Gets a list of processes that have locks on the specified path.
        /// </summary>
        /// <param name="path">The file or directory path to check.</param>
        /// <param name="options">Options for controlling the search behavior.</param>
        /// <returns>A list of ProcessInfo objects containing details about processes with locks on the path.</returns>
        public static List<ProcessInfo> GetLockingProcessesInfo(string path, LockingProcessesOptions? options = null)
        {
            options ??= new LockingProcessesOptions();

            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));

            var processInfos = new ConcurrentDictionary<int, ProcessInfo>();
            path = Path.GetFullPath(path);

            // Commenting this out, let caller handle access check
            //if (!IsAdministrator())
            //{
            //    throw new UnauthorizedAccessException(
            //        "This operation requires administrative privileges. Please run as administrator.");
            //}

            if (Directory.Exists(path))
            {
                var pathsToCheck = new Queue<Dictionary<string, int>>();
                var initialPath = new Dictionary<string, int> { { path, 0 } };
                pathsToCheck.Enqueue(initialPath);

                while (pathsToCheck.Count > 0)
                {
                    var currentItem = pathsToCheck.Dequeue();
                    var currentPath = currentItem.Keys.First();
                    var depth = currentItem.Values.First();

                    // Check if we've exceeded max depth
                    if (options.MaxDepth != -1 && depth > options.MaxDepth)
                        continue;

                    try
                    {
                        // Check files in current directory
                        foreach (var file in Directory.GetFiles(currentPath))
                        {
                            CheckPathForLocks(file, processInfos);
                        }

                        // Add subdirectories to queue if recursive
                        if (options.Recursive)
                        {
                            foreach (var dir in Directory.GetDirectories(currentPath))
                            {
                                var nextPath = new Dictionary<string, int> { { dir, depth + 1 } };
                                pathsToCheck.Enqueue(nextPath);
                            }
                        }
                    }
                    catch (Exception ex) when (ex is UnauthorizedAccessException || ex is SecurityException)
                    {
                        if (!options.ContinueOnAccessDenied)
                            throw;
                    }
                }
            }
            else if (File.Exists(path))
            {
                CheckPathForLocks(path, processInfos);
            }
            else
            {
                throw new FileNotFoundException($"Path not found: {path}");
            }

            return processInfos.Values.ToList();
        }

        private static void CheckPathForLocks(string path, ConcurrentDictionary<int, ProcessInfo> processInfos)
        {
            uint sessionHandle;
            string sessionKey = Guid.NewGuid().ToString();

            int result = NativeMethods.RmStartSession(out sessionHandle, 0, sessionKey);
            if (result != 0)
                return;

            try
            {
                string[] pathArray = { path };
                result = NativeMethods.RmRegisterResources(sessionHandle, 1, pathArray, 0, null, 0, null);

                if (result != 0)
                    return;

                uint pnProcInfoNeeded;
                uint pnProcInfo = 0;
                uint lpdwRebootReasons = 0;

                result = NativeMethods.RmGetList(sessionHandle, out pnProcInfoNeeded, ref pnProcInfo, null, ref lpdwRebootReasons);

                if (result == NativeMethods.ERROR_MORE_DATA)
                {
                    var processInfo = new RM_PROCESS_INFO[pnProcInfoNeeded];
                    pnProcInfo = pnProcInfoNeeded;

                    result = NativeMethods.RmGetList(sessionHandle, out pnProcInfoNeeded, ref pnProcInfo, processInfo, ref lpdwRebootReasons);

                    if (result == 0)
                    {
                        for (int i = 0; i < pnProcInfo; i++)
                        {
                            try
                            {
                                var rmInfo = processInfo[i];
                                int processId = rmInfo.Process.dwProcessId;

                                processInfos.AddOrUpdate(
                                    processId,
                                    // Add new
                                    _ => CreateProcessInfo(processId, path, rmInfo),
                                    // Update existing
                                    (_, existing) =>
                                    {
                                        if (!existing.LockedPath.Contains(path))
                                        {
                                            existing.LockedPath = string.Join(
                                                Environment.NewLine,
                                                existing.LockedPath,
                                                path
                                            ).Trim();
                                        }
                                        return existing;
                                    }
                                );
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Error processing handle info: {ex.Message}");
                            }
                        }
                    }
                }
            }
            finally
            {
                NativeMethods.RmEndSession(sessionHandle);
            }
        }

        private static ProcessInfo CreateProcessInfo(int processId, string lockedPath, RM_PROCESS_INFO rmInfo)
        {
            string workingDir = string.Empty;
            string cmdLine = string.Empty;

            // TODO: Implement these properties properly
            //try
            //{
            //    workingDir = ProcessParameters.GetWorkingDirectory((uint)processId);
            //    Console.WriteLine($"Got working directory for PID {processId}: {workingDir}");
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine($"Failed to get working directory for PID {processId}: {ex.Message}");
            //}

            //try
            //{
            //    cmdLine = ProcessParameters.GetCommandLine((uint)processId);
            //    Console.WriteLine($"Got command line for PID {processId}: {cmdLine}");
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine($"Failed to get command line for PID {processId}: {ex.Message}");
            //}

            var info = new ProcessInfo
            {
                ProcessId = processId,
                ProcessName = rmInfo.strAppName,
                LockedPath = lockedPath,
                WorkingDirectory = workingDir,
                CommandLine = cmdLine
            };

            try
            {
                using var process = Process.GetProcessById(processId);
                info.MainWindowTitle = process.MainWindowTitle;

                try
                {
                    using var processHandle = NativeMethods.OpenProcess(
                        NativeMethods.PROCESS_QUERY_LIMITED_INFORMATION,
                        false,
                        (uint)processId);

                    if (!processHandle.IsInvalid)
                    {
                        uint bufferSize = 1024;
                        var pathBuilder = new StringBuilder((int)bufferSize);
                        if (NativeMethods.QueryFullProcessImageName(processHandle, 0, pathBuilder, ref bufferSize))
                        {
                            info.Path = pathBuilder.ToString(0, (int)bufferSize);
                        }
                    }
                }
                catch
                {
                    info.Path = "Access Denied";
                }

                try
                {
                    info.StartTime = process.StartTime;
                }
                catch
                {
                    // Keep default DateTime
                }

                // Get process owner
                info.UserName = GetProcessOwner(processId);
            }
            catch
            {
                // Process no longer exists, but we still return the basic info
            }

            return info;
        }

        private static string GetProcessOwner(int processId)
        {
            using var processHandle = NativeMethods.OpenProcess(
                NativeMethods.PROCESS_QUERY_LIMITED_INFORMATION,
                false,
                (uint)processId);

            if (processHandle.IsInvalid)
                return "Unknown";

            SafeAccessToken? tokenHandle = null;
            try
            {
                if (!NativeMethods.OpenProcessToken(processHandle, NativeMethods.TOKEN_QUERY, out tokenHandle))
                    return "Unknown";

                using (tokenHandle)
                {
                    NativeMethods.GetTokenInformation(
                        tokenHandle,
                        TOKEN_INFORMATION_CLASS.TokenUser,
                        IntPtr.Zero,
                        0,
                        out int tokenInfoLength);

                    if (tokenInfoLength == 0)
                        return "Unknown";

                    using var tokenInfo = new SafeHGlobalHandle((int)tokenInfoLength);
                    if (NativeMethods.GetTokenInformation(
                        tokenHandle,
                        TOKEN_INFORMATION_CLASS.TokenUser,
                        tokenInfo.DangerousGetHandle(),
                        tokenInfoLength,
                        out tokenInfoLength))
                    {
                        var tokenUser = Marshal.PtrToStructure<TOKEN_USER>(tokenInfo.DangerousGetHandle());

                        var sid = new SecurityIdentifier(tokenUser.User.Sid);
                        try
                        {
                            var account = sid.Translate(typeof(NTAccount)) as NTAccount;
                            return account?.Value ?? "Unknown";
                        }
                        catch
                        {
                            return sid.Value;
                        }
                    }
                }
            }
            catch
            {
                // Ignore any errors and return Unknown
            }

            return "Unknown";
        }

        //private static bool IsAdministrator()
        //{
        //    using var identity = WindowsIdentity.GetCurrent();
        //    var principal = new WindowsPrincipal(identity);
        //    return principal.IsInRole(WindowsBuiltInRole.Administrator);
        //}

        /// <summary>
        /// Gets a list of processes that have locks on the specified path.
        /// </summary>
        /// <param name="path">The file or directory path to check.</param>
        /// <param name="recursive">Whether to check subdirectories recursively.</param>
        /// <returns>A list of processes that have locks on the specified path.</returns>
        public static List<Process> GetLockingProcesses(string path, bool recursive = false)
        {
            var options = new LockingProcessesOptions { Recursive = recursive };
            return GetLockingProcessesInfo(path, options)
                .Select(pi => Process.GetProcessById(pi.ProcessId))
                .Where(p => p != null)
                .ToList();
        }
    }
}