using System;
using System.Globalization;
using System.IO.Pipes;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using PSADT.AccountManagement;
using PSADT.Foundation;
using PSADT.Interop;
using PSADT.SafeHandles;
using PSADT.Utilities;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Security;
using Windows.Win32.System.TaskScheduler;

namespace PSADT.Security
{
    /// <summary>
    /// Provides static methods for retrieving and managing Windows security tokens for user sessions and processes.
    /// </summary>
    /// <remarks>The TokenManager class offers utility functions to obtain primary, linked, and unelevated
    /// tokens for users and processes, supporting scenarios such as impersonation, privilege elevation, and secure
    /// inter-process communication. All methods are intended for internal use and require appropriate permissions;
    /// callers must ensure they have administrative rights where necessary.</remarks>
    internal static class TokenManager
    {
        /// <summary>
        /// Indicates whether the current execution context can utilize token brokering to retrieve user tokens from other sessions.
        /// </summary>
        internal static readonly bool CanGetUserPrimaryToken = AccountUtilities.CallerIsLocalSystem || (AccountUtilities.CallerIsAdmin && (!ClientServerUtilities.ClientServerOnUncPath || ClientServerPermissions.SystemAccountHasPermissions()));

        /// <summary>
        /// Retrieves the primary access token for a user in the specified session, optionally requesting an elevated
        /// token type.
        /// </summary>
        /// <remarks>This method requires administrative privileges. When called from a non-local system
        /// context, a token broker process is used to obtain the token. The returned handle can be used to perform
        /// operations that require user authentication.</remarks>
        /// <param name="sessionId">The identifier of the session for the user whose primary token is to be retrieved. Must correspond to a
        /// valid user session.</param>
        /// <param name="elevatedTokenType">The type of elevated token to retrieve. Specify a value from the ElevatedTokenType enumeration. The default
        /// is None.</param>
        /// <param name="uiAccess">A boolean value indicating whether the retrieved primary token should have UI access enabled.</param>
        /// <returns>A SafeFileHandle representing the user's primary access token. The caller is responsible for disposing of
        /// the handle when it is no longer needed.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown if the caller is not an administrator or if an elevated token of type HighestMandatory cannot be
        /// obtained.</exception>
        /// <exception cref="InvalidProgramException">Thrown if the token broker fails to provide a valid token or if an invalid token length is received.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the token broker fails to provide a valid token or if an invalid token length is received.</exception>
        internal static async ValueTask<SafeFileHandle> GetUserPrimaryTokenAsync(uint sessionId, ElevatedTokenType elevatedTokenType = ElevatedTokenType.None, bool uiAccess = false)
        {
            // Confirm that the caller is an administrator.
            if (!AccountUtilities.CallerIsAdmin)
            {
                throw new UnauthorizedAccessException("The caller must be an administrator to retrieve another user's primary token.");
            }

            // Confirm the session Id isn't the SYSTEM's.
            if (sessionId == 0)
            {
                throw new UnauthorizedAccessException("Brokering of the Local System session token is not permitted.");
            }

            // Get the user's token. If we're not local system, we need to use the token broker.
            if (!AccountUtilities.CallerIsLocalSystem)
            {
                // Set up the pipe server and start the client/server token broker process.
                PrivilegeManager.EnablePrivilegeIfDisabled(SE_PRIVILEGE.SeDebugPrivilege);
                string pipeName = $"PSADT.ClientServer.Client_TokenBroker_{CryptographicUtilities.SecureNewGuid()}";
                PipeSecurity pipeSecurity = new(); pipeSecurity.AddAccessRule(new(AccountUtilities.LocalSystemSid, PipeAccessRights.CreateNewInstance | PipeAccessRights.ReadWrite, AccessControlType.Allow));
                #pragma warning disable format, IDE0063
                #if !NETFRAMEWORK
                NamedPipeServerStream pipe = NamedPipeServerStreamAcl.Create(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous, 0, 0, pipeSecurity);
                await using (pipe.ConfigureAwait(false))
                #else
                using (NamedPipeServerStream pipe = new(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous, 0, 0, pipeSecurity))
                #endif
                #pragma warning restore IDE0063, format
                {
                    // Create an instance of the TaskService to manage scheduled tasks and connect on localhost.
                    try
                    {
                        ITaskService servicePtr = (ITaskService)new Windows.Win32.System.TaskScheduler.TaskScheduler();
                        try
                        {
                            // Set up the task as required.
                            using SafeFreeBSTRHandle folderName = SafeFreeBSTRHandle.Alloc(@"\");
                            servicePtr.Connect(serverName: null, user: null, domain: null, password: null);
                            servicePtr.GetFolder(folderName, out ITaskFolder rootFolder);
                            try
                            {
                                servicePtr.NewTask(0, out ITaskDefinition taskDefinition);
                                try
                                {
                                    IActionCollection actions = taskDefinition.Actions;
                                    try
                                    {
                                        actions.Create(TASK_ACTION_TYPE.TASK_ACTION_EXEC, out IAction action);
                                        try
                                        {
                                            IExecAction execAction = (IExecAction)action;
                                            try
                                            {
                                                IPrincipal principal = taskDefinition.Principal;
                                                try
                                                {
                                                    ITaskSettings settings = taskDefinition.Settings;
                                                    try
                                                    {
                                                        using SafeFreeBSTRHandle userId = SafeFreeBSTRHandle.Alloc(AccountUtilities.LocalSystemSid.Value);
                                                        using SafeFreeBSTRHandle path = SafeFreeBSTRHandle.Alloc(ClientServerUtilities.ClientLauncherCompatiblePath.FullName);
                                                        using SafeFreeBSTRHandle args = SafeFreeBSTRHandle.Alloc($"/TokenBroker -PipeName {pipeName} -ProcessId {AccountUtilities.CallerProcessId} -SessionId {sessionId} -ElevatedTokenType {elevatedTokenType} -UIAccess {uiAccess}");
                                                        bool userIdAddRef = false; bool pathAddRef = false; bool argsAddRef = false;
                                                        try
                                                        {
                                                            // Register and start the task, then delete it. It'll keep running until it exits.
                                                            using SafeFreeBSTRHandle taskName = SafeFreeBSTRHandle.Alloc(pipeName);
                                                            userId.DangerousAddRef(ref userIdAddRef);
                                                            path.DangerousAddRef(ref pathAddRef);
                                                            args.DangerousAddRef(ref argsAddRef);
                                                            settings.StopIfGoingOnBatteries = false;
                                                            settings.DisallowStartIfOnBatteries = false;
                                                            principal.UserId = (BSTR)userId.DangerousGetHandle();
                                                            principal.LogonType = TASK_LOGON_TYPE.TASK_LOGON_SERVICE_ACCOUNT;
                                                            principal.RunLevel = TASK_RUNLEVEL_TYPE.TASK_RUNLEVEL_HIGHEST;
                                                            execAction.Path = (BSTR)path.DangerousGetHandle();
                                                            execAction.Arguments = (BSTR)args.DangerousGetHandle();
                                                            rootFolder.RegisterTaskDefinition(taskName, taskDefinition, (int)TASK_CREATION.TASK_CREATE_OR_UPDATE, userId: null, password: null, TASK_LOGON_TYPE.TASK_LOGON_SERVICE_ACCOUNT, sddl: null, out IRegisteredTask task);
                                                            try
                                                            {
                                                                // Wait for the token broker to connect while task is in scope for error reporting.
                                                                // Note: CancellationToken doesn't interrupt ConnectNamedPipe - so we dispose the pipe.
                                                                task.Run(@params: null, out IRunningTask runningTask);
                                                                _ = Marshal.FinalReleaseComObject(runningTask);
                                                                try
                                                                {
                                                                    using CancellationTokenSource cts = new(ClientServerUtilities.ClientOperationTimeout);
                                                                    await pipe.WaitForConnectionAsync(cts.Token).ConfigureAwait(false);
                                                                }
                                                                catch (OperationCanceledException)
                                                                {
                                                                    throw new InvalidProgramException($"Token broker task failed to connect within timeout. Task state: {task.State}, Last result: 0x{task.LastTaskResult:X8}.");
                                                                }
                                                            }
                                                            finally
                                                            {
                                                                rootFolder.DeleteTask(taskName, 0);
                                                                _ = Marshal.FinalReleaseComObject(task);
                                                            }
                                                        }
                                                        finally
                                                        {
                                                            if (userIdAddRef)
                                                            {
                                                                userId.DangerousRelease();
                                                            }
                                                            if (pathAddRef)
                                                            {
                                                                path.DangerousRelease();
                                                            }
                                                            if (argsAddRef)
                                                            {
                                                                args.DangerousRelease();
                                                            }
                                                        }
                                                    }
                                                    finally
                                                    {
                                                        _ = Marshal.FinalReleaseComObject(settings);
                                                    }
                                                }
                                                finally
                                                {
                                                    _ = Marshal.FinalReleaseComObject(principal);
                                                }
                                            }
                                            finally
                                            {
                                                _ = Marshal.FinalReleaseComObject(execAction);
                                            }
                                        }
                                        finally
                                        {
                                            _ = Marshal.FinalReleaseComObject(action);
                                        }
                                    }
                                    finally
                                    {
                                        _ = Marshal.FinalReleaseComObject(actions);
                                    }
                                }
                                finally
                                {
                                    _ = Marshal.FinalReleaseComObject(taskDefinition);
                                }
                            }
                            finally
                            {
                                _ = Marshal.FinalReleaseComObject(rootFolder);
                            }
                        }
                        finally
                        {
                            _ = Marshal.FinalReleaseComObject(servicePtr);
                        }

                        // Read a single-byte token size indicator from the pipe.
                        int tokenSizeIndicator = pipe.ReadByte();
                        if (tokenSizeIndicator == -1)
                        {
                            throw new InvalidProgramException("The token broker pipe closed before reading the token size indicator byte.");
                        }
                        if (tokenSizeIndicator is not 4 and not 8)
                        {
                            throw new InvalidProgramException($"Invalid token size indicator of {tokenSizeIndicator.ToString(CultureInfo.InvariantCulture)} received from the token broker. Expected 4 or 8.");
                        }

                        // Read the token payload based on the validated size indicator.
                        byte[] tokenBuf = new byte[tokenSizeIndicator]; int tokenBufReadLength = await pipe.ReadAsync(tokenBuf, 0, tokenSizeIndicator, default).ConfigureAwait(false);
                        if (tokenBufReadLength is 0)
                        {
                            throw new InvalidProgramException("The token broker pipe closed before reading the token payload.");
                        }
                        if (tokenBufReadLength != tokenSizeIndicator)
                        {
                            throw new InvalidProgramException(string.Create(CultureInfo.InvariantCulture, $"The token broker pipe read {tokenBufReadLength} bytes, but expected {tokenSizeIndicator} bytes."));
                        }

                        // Return the token handle.
                        return new(tokenBuf.AsReadOnlyStructure<nint>(), ownsHandle: true);
                    }
                    catch (Exception ex)
                    {
                        if (elevatedTokenType is ElevatedTokenType.HighestMandatory)
                        {
                            throw new InvalidOperationException($"Failed to get the linked admin token for Session Id [{sessionId}].", ex);
                        }
                    }
                }
            }

            // When we're local system, we can just get the primary token for the user.
            PrivilegeManager.EnablePrivilegeIfDisabled(SE_PRIVILEGE.SeTcbPrivilege);
            _ = NativeMethods.WTSQueryUserToken(sessionId, out SafeFileHandle hUserToken);
            using (hUserToken)
            {
                if (elevatedTokenType is not ElevatedTokenType.None)
                {
                    try
                    {
                        return GetLinkedPrimaryToken(hUserToken, uiAccess);
                    }
                    catch (Exception ex)
                    {
                        if (elevatedTokenType is ElevatedTokenType.HighestMandatory)
                        {
                            throw new InvalidOperationException($"Failed to get the linked admin token for Session Id [{sessionId}].", ex);
                        }
                    }
                }
                return GetPrimaryToken(hUserToken, uiAccess);
            }
        }

        /// <summary>
        /// Retrieves the primary token associated with the specified security token handle.
        /// </summary>
        /// <remarks>This method duplicates the specified security token to create a primary token, which
        /// can be used for impersonation or other security-related operations. Ensure that the caller has appropriate
        /// permissions to access and duplicate the token.</remarks>
        /// <param name="tokenHandle">A handle to the security token. This handle must have the necessary access rights to allow duplication.</param>
        /// <param name="uiAccess">A boolean value indicating whether the retrieved primary token should have UI access enabled.</param>
        /// <returns>A <see cref="SafeFileHandle"/> representing the duplicated primary token.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown if the caller does not have the required privileges to duplicate the token with UI access enabled.</exception>
        internal static SafeFileHandle GetPrimaryToken(SafeHandle tokenHandle, bool uiAccess = false)
        {
            _ = NativeMethods.DuplicateTokenEx(tokenHandle, TOKEN_ACCESS_MASK.TOKEN_QUERY | TOKEN_ACCESS_MASK.TOKEN_DUPLICATE | TOKEN_ACCESS_MASK.TOKEN_ASSIGN_PRIMARY | TOKEN_ACCESS_MASK.TOKEN_ADJUST_DEFAULT | TOKEN_ACCESS_MASK.TOKEN_ADJUST_SESSIONID, lpTokenAttributes: null, SECURITY_IMPERSONATION_LEVEL.SecurityAnonymous, TOKEN_TYPE.TokenPrimary, out SafeFileHandle hPrimaryToken);
            if (uiAccess)
            {
                if (!PrivilegeManager.HasPrivilege(SE_PRIVILEGE.SeTcbPrivilege))
                {
                    throw new UnauthorizedAccessException("The calling account must have SeTcbPrivilege to set UIAccess on the token.");
                }
                PrivilegeManager.EnablePrivilegeIfDisabled(SE_PRIVILEGE.SeTcbPrivilege);
                unsafe
                {
                    int tokenValue = 1; ReadOnlySpan<byte> tokenInformation = MemoryMarshal.AsBytes(new ReadOnlySpan<int>(&tokenValue, 1));
                    _ = NativeMethods.SetTokenInformation(hPrimaryToken, TOKEN_INFORMATION_CLASS.TokenUIAccess, tokenInformation);
                }
            }
            return hPrimaryToken;
        }

        /// <summary>
        /// Retrieves the linked token associated with the specified token handle.
        /// </summary>
        /// <remarks>This method retrieves the linked token, which is typically used in scenarios
        /// involving user impersonation or elevated privileges. The caller must ensure that the provided token handle
        /// is valid and has the necessary permissions to query linked token information.</remarks>
        /// <param name="tokenHandle">A <see cref="SafeHandle"/> representing the token handle for which the linked token is to be retrieved.</param>
        /// <returns>A <see cref="SafeFileHandle"/> representing the linked token associated with the specified token
        /// handle.</returns>
        internal static SafeFileHandle GetLinkedToken(SafeHandle tokenHandle)
        {
            Span<byte> buffer = stackalloc byte[Unsafe.SizeOf<TOKEN_LINKED_TOKEN>()];
            _ = NativeMethods.GetTokenInformation(tokenHandle, TOKEN_INFORMATION_CLASS.TokenLinkedToken, buffer, out _);
            ref readonly TOKEN_LINKED_TOKEN tokenLinkedToken = ref buffer.AsReadOnlyStructure<TOKEN_LINKED_TOKEN>();
            return new(tokenLinkedToken.LinkedToken, ownsHandle: true);
        }

        /// <summary>
        /// Retrieves the primary token linked to the specified token handle.
        /// </summary>
        /// <remarks>This method uses the provided token handle to obtain a linked token and then
        /// retrieves its primary token. The caller is responsible for ensuring the validity of the input token
        /// handle.</remarks>
        /// <param name="tokenHandle">A <see cref="SafeHandle"/> representing the token handle for which the linked primary token is to be
        /// retrieved.</param>
        /// <param name="uiAccess">A boolean value indicating whether the retrieved primary token should have UI access enabled.</param>
        /// <returns>A <see cref="SafeFileHandle"/> representing the linked primary token.</returns>
        internal static SafeFileHandle GetLinkedPrimaryToken(SafeHandle tokenHandle, bool uiAccess = false)
        {
            using SafeFileHandle linkedToken = GetLinkedToken(tokenHandle);
            return GetPrimaryToken(linkedToken, uiAccess);
        }

        /// <summary>
        /// Retrieves the highest available primary token associated with the specified token handle.
        /// </summary>
        /// <remarks>This method attempts to retrieve the linked primary token associated with the
        /// specified token handle. If the linked token is unavailable, it falls back to retrieving the primary token of
        /// the original token handle.</remarks>
        /// <param name="tokenHandle">A <see cref="SafeHandle"/> representing the token handle for which the primary token is to be retrieved.</param>
        /// <param name="uiAccess">A boolean value indicating whether the retrieved primary token should have UI access enabled.</param>
        /// <returns>A <see cref="SafeFileHandle"/> representing the highest available primary token.</returns>
        internal static SafeFileHandle GetHighestPrimaryToken(SafeHandle tokenHandle, bool uiAccess = false)
        {
            // If the linked token is not available, fall back to the primary token of the original token handle.
            try
            {
                return GetLinkedPrimaryToken(tokenHandle, uiAccess);
            }
            catch
            {
                return GetPrimaryToken(tokenHandle, uiAccess);
                throw;
            }
        }

        /// <summary>
        /// Retrieves the access token for the current process with the specified desired access rights.
        /// </summary>
        /// <param name="DesiredAccess">The desired access rights for the token.</param>
        /// <returns>A <see cref="SafeFileHandle"/> representing the access token for the current process.</returns>
        internal static SafeFileHandle GetCurrentProcessToken(TOKEN_ACCESS_MASK DesiredAccess)
        {
            using SafeProcessHandle hProcess = NativeMethods.GetCurrentProcess();
            _ = NativeMethods.OpenProcessToken(hProcess, DesiredAccess, out SafeFileHandle hToken);
            return hToken;
        }
    }
}
