using System;
using System.ComponentModel;
using System.Globalization;
using System.IO.Pipes;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.AccessControl;
using System.Security.Principal;
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
        /// Determines whether the specified session ID is valid for token acquisition requests.
        /// </summary>
        /// <param name="sessionId">The session identifier to validate.</param>
        /// <returns>Whether the session identifier is valid for token requests.</returns>
        internal static bool SessionIdIsValidForVending(uint sessionId)
        {
            return sessionId is not (0 or uint.MaxValue);
        }

        /// <summary>
        /// Checks whether any acquisition route may serve the request.
        /// </summary>
        /// <remarks>Process discovery is a search of the target session and can come up empty, so a caller that holds a no-token
        /// fallback wants <see cref="TryGetUserPrimaryTokenAsync(uint, ElevatedTokenType, bool)"/>, which reaches that fallback
        /// rather than answering a question no check can settle in advance.</remarks>
        /// <param name="sessionId">The requested desktop session.</param>
        /// <returns>Whether acquisition may be attempted.</returns>
        internal static bool CanGetUserPrimaryToken(uint sessionId)
        {
            return CallerCanRetrieveTokens && SessionIdIsValidForVending(sessionId);
        }

        /// <summary>
        /// Retrieves a session token, reporting the absence of one rather than raising it.
        /// </summary>
        /// <remarks>For a caller holding a fallback to use when no token can be had. Absorbs every failure meaning no token, per
        /// <see cref="IsAcquisitionFailure"/>; an undefined elevation is the caller's own error and still raises.</remarks>
        /// <param name="sessionId">The requested desktop session.</param>
        /// <param name="elevatedTokenType">The requested elevation.</param>
        /// <param name="uiAccess">Whether UIAccess is requested.</param>
        /// <returns>The owned primary token, or <see langword="null"/> if none could be had.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The elevation value is not defined.</exception>
        internal static async Task<SafeFileHandle?> TryGetUserPrimaryTokenAsync(uint sessionId, ElevatedTokenType elevatedTokenType = ElevatedTokenType.None, bool uiAccess = false)
        {
            if (!SessionIdIsValidForVending(sessionId))
            {
                return null;
            }
            try
            {
                return await GetUserPrimaryTokenAsync(sessionId, elevatedTokenType, uiAccess).ConfigureAwait(false);
            }
            catch (Exception ex) when (IsAcquisitionFailure(ex))
            {
                return null;
                throw;
            }
        }

        /// <summary>
        /// Retrieves a primary token for the expected user, reporting the absence of one rather than raising it.
        /// </summary>
        /// <remarks>Absorbs the identity check as well, as a session that has changed hands has no token for the user who was
        /// asked about.</remarks>
        /// <param name="user">The expected desktop user.</param>
        /// <param name="elevatedTokenType">The requested elevation.</param>
        /// <param name="uiAccess">Whether UIAccess is requested.</param>
        /// <returns>The owned token for the expected user, or <see langword="null"/> if none could be had.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The elevation value is not defined.</exception>
        internal static async ValueTask<SafeFileHandle?> TryGetUserPrimaryTokenAsync(RunAsActiveUser user, ElevatedTokenType elevatedTokenType = ElevatedTokenType.None, bool uiAccess = false)
        {
            if (!SessionIdIsValidForVending(user.SessionId))
            {
                return null;
            }
            try
            {
                return await GetUserPrimaryTokenAsync(user, elevatedTokenType, uiAccess).ConfigureAwait(false);
            }
            catch (Exception ex) when (IsAcquisitionFailure(ex))
            {
                return null;
                throw;
            }
        }

        /// <summary>
        /// Determines whether a failure to acquire a token means there is no token rather than that the caller erred.
        /// </summary>
        /// <remarks>The same set <c language="csharp">ProcessTokenProvider</c> treats as a candidate failure, less the argument failures, which
        /// carry an undefined elevation and belong to the caller. Named as a set rather than listed at each catch so that the
        /// native layer surfacing a refusal as something new is handled in one place.</remarks>
        /// <param name="exception">The failure to classify.</param>
        /// <returns>Whether the failure means no token was available.</returns>
        private static bool IsAcquisitionFailure(Exception exception)
        {
            return exception is Win32Exception or InvalidOperationException or UnauthorizedAccessException or SecurityException or IdentityNotMappedException;
        }

        /// <summary>
        /// Retrieves a session token through WTS for SYSTEM, or through process discovery with broker fallback for other callers.
        /// </summary>
        /// <param name="sessionId">The requested desktop session.</param>
        /// <param name="elevatedTokenType">The requested elevation.</param>
        /// <param name="uiAccess">Whether UIAccess is requested.</param>
        /// <returns>The owned primary token.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The elevation value is not defined.</exception>
        internal static Task<SafeFileHandle> GetUserPrimaryTokenAsync(uint sessionId, ElevatedTokenType elevatedTokenType = ElevatedTokenType.None, bool uiAccess = false)
        {
            return GetUserPrimaryTokenAsync(sessionId, sessionSid: null, elevatedTokenType, uiAccess);
        }

        /// <summary>
        /// Retrieves a primary token while validating the expected user's identity on either route.
        /// </summary>
        /// <param name="user">The expected desktop user.</param>
        /// <param name="elevatedTokenType">The requested elevation.</param>
        /// <param name="uiAccess">Whether UIAccess is requested.</param>
        /// <returns>The owned token for the expected user.</returns>
        /// <exception cref="UnauthorizedAccessException">The session now belongs to a different account.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The elevation value is not defined.</exception>
        internal static async ValueTask<SafeFileHandle> GetUserPrimaryTokenAsync(RunAsActiveUser user, ElevatedTokenType elevatedTokenType = ElevatedTokenType.None, bool uiAccess = false)
        {
            SafeFileHandle token = await GetUserPrimaryTokenAsync(user.SessionId, user.SID, elevatedTokenType, uiAccess).ConfigureAwait(false);
            try
            {
                return TokenUtilities.GetTokenSid(token) != user.SID || TokenUtilities.GetTokenSessionId(token) != user.SessionId
                    ? throw new UnauthorizedAccessException("The acquired token does not belong to the expected desktop user.")
                    : token;
            }
            catch (Exception ex)
            {
                token.Dispose();
                ExceptionDispatchInfo.Capture(ex).Throw();
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
            try
            {
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
            catch (Exception ex)
            {
                hPrimaryToken.Dispose();
                ExceptionDispatchInfo.Capture(ex).Throw();
                throw;
            }
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
        /// Shares caller-based acquisition, using WTS directly for SYSTEM and process-first acquisition otherwise.
        /// </summary>
        /// <param name="sessionId">The requested session.</param>
        /// <param name="sessionSid">The optional expected owner.</param>
        /// <param name="elevatedTokenType">The requested elevation.</param>
        /// <param name="uiAccess">Whether UIAccess is required.</param>
        /// <returns>The acquired primary token.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The session cannot be vended or the elevation value is not defined.</exception>
        /// <exception cref="UnauthorizedAccessException">The caller must be an administrator to retrieve another user's primary token.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Using a conditional expression here gives a CA2000 warning, the more severe of the two.")]
        private static Task<SafeFileHandle> GetUserPrimaryTokenAsync(uint sessionId, SecurityIdentifier? sessionSid, ElevatedTokenType elevatedTokenType, bool uiAccess)
        {
            if (!SessionIdIsValidForVending(sessionId))
            {
                throw new ArgumentOutOfRangeException(nameof(sessionId), sessionId, "The session cannot be vended.");
            }
            if (!Enum.IsDefined(elevatedTokenType))
            {
                throw new ArgumentOutOfRangeException(nameof(elevatedTokenType), elevatedTokenType, "The elevation value is not defined.");
            }
            if (!AccountUtilities.CallerIsAdmin)
            {
                throw new UnauthorizedAccessException("The caller must be an administrator to retrieve another user's primary token.");
            }
            if (AccountUtilities.CallerIsLocalSystem)
            {
                return Task.FromResult(GetUserPrimaryTokenViaWts(sessionId, elevatedTokenType, uiAccess));
            }
            if (GetUserPrimaryTokenFromProcess(sessionId, sessionSid, elevatedTokenType, uiAccess) is not SafeFileHandle token)
            {
                return GetUserPrimaryTokenViaBrokerAsync(sessionId, elevatedTokenType, uiAccess);
            }
            return Task.FromResult(token);
        }

        /// <summary>
        /// Attempts to retrieve the primary token for a user in the specified session through process discovery.
        /// </summary>
        /// <param name="sessionId">The identifier of the session for the user whose primary token is to be retrieved. Must correspond to a valid user session.</param>
        /// <param name="sessionSid">The optional expected owner.</param>
        /// <param name="elevatedTokenType">The requested elevation.</param>
        /// <param name="uiAccess">Whether UIAccess is required.</param>
        /// <returns>A SafeFileHandle representing the user's primary access token, or null if the token could not be retrieved.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the session ID is not valid for vending.</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown if the caller is not an administrator.</exception>
        private static SafeFileHandle? GetUserPrimaryTokenFromProcess(uint sessionId, SecurityIdentifier? sessionSid, ElevatedTokenType elevatedTokenType, bool uiAccess)
        {
            return !SessionIdIsValidForVending(sessionId) ? throw new ArgumentOutOfRangeException(nameof(sessionId), sessionId, "The session cannot be vended.")
                : !CanGetTokenFromProcess ? throw new UnauthorizedAccessException("The caller must be an administrator to retrieve another user's primary token.")
                : ProcessTokenProvider.TryGetToken(sessionId, sessionSid, elevatedTokenType, uiAccess, out SafeFileHandle? token) ? token
                : null;
        }

        /// <summary>
        /// Retrieves the primary access token for a user in the specified session through the token broker.
        /// </summary>
        /// <remarks>This method requires a non-SYSTEM administrator. The returned handle can be used to perform
        /// operations that require user authentication.</remarks>
        /// <param name="sessionId">The identifier of the session for the user whose primary token is to be retrieved. Must correspond to a
        /// valid user session.</param>
        /// <param name="elevatedTokenType">The type of elevated token to retrieve. Specify a value from the ElevatedTokenType enumeration. The default
        /// is None.</param>
        /// <param name="uiAccess">A boolean value indicating whether the retrieved primary token should have UI access enabled.</param>
        /// <returns>A SafeFileHandle representing the user's primary access token. The caller is responsible for disposing of
        /// the handle when it is no longer needed.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the session Id is invalid or cannot be vended.</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown if the caller is not an administrator or if an elevated token of type HighestMandatory cannot be
        /// obtained.</exception>
        /// <exception cref="InvalidProgramException">Thrown if the token broker fails to provide a valid token or if an invalid token length is received.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the token broker fails to provide a valid token or if an invalid token length is received.</exception>
        private static Task<SafeFileHandle> GetUserPrimaryTokenViaBrokerAsync(uint sessionId, ElevatedTokenType elevatedTokenType, bool uiAccess)
        {
            // Internal worker to abstract parameter validation away from the task.
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0004:Cast is redundant.", Justification = "This cast is needed for our net472 target.")]
            static async Task<SafeFileHandle> GetUserPrimaryTokenViaBrokerImplAsync(uint sessionId, ElevatedTokenType elevatedTokenType, bool uiAccess)
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
                                                Windows.Win32.System.TaskScheduler.IPrincipal principal = taskDefinition.Principal;
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

                        // Read the token payload based on the validated size indicator. A byte mode pipe may hand back
                        // fewer bytes than were asked for without having ended, so the payload is accumulated until it
                        // is whole rather than read once and measured.
                        Span<byte> tokenBuf = stackalloc byte[tokenSizeIndicator];
                        int tokenBufReadLength = 0;
                        while (tokenBufReadLength < tokenSizeIndicator)
                        {
                            int read = pipe.Read(tokenBuf[tokenBufReadLength..]);
                            if (read is 0)
                            {
                                throw new InvalidProgramException("The token broker pipe closed before reading the token payload.");
                            }
                            tokenBufReadLength += read;
                        }

                        // Return the token handle.
                        return tokenSizeIndicator is 8
                            ? new((nint)tokenBuf.AsReadOnlyStructure<long>(), ownsHandle: true)
                            : new((nint)tokenBuf.AsReadOnlyStructure<int>(), ownsHandle: true);
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException($"Failed to get the user token for Session Id [{sessionId}].", ex);
                    }
                }
            }

            // Validate state before proceeding.
            return !SessionIdIsValidForVending(sessionId) ? throw new ArgumentOutOfRangeException(nameof(sessionId), sessionId, "The session cannot be vended.")
                : AccountUtilities.CallerIsLocalSystem ? throw new UnauthorizedAccessException("Local System must retrieve user tokens through WTS directly.")
                : !CanGetTokenViaBroker ? throw new UnauthorizedAccessException("The token broker cannot access the client/server assemblies.")
                : GetUserPrimaryTokenViaBrokerImplAsync(sessionId, elevatedTokenType, uiAccess);
        }

        /// <summary>
        /// Retrieves a session token directly through WTS for the Local System caller.
        /// </summary>
        /// <param name="sessionId">The requested desktop session.</param>
        /// <param name="elevatedTokenType">The requested elevation.</param>
        /// <param name="uiAccess">Whether UIAccess is required.</param>
        /// <returns>The owned primary token.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The session Id is invalid or cannot be vended.</exception>
        /// <exception cref="UnauthorizedAccessException">The caller is not SYSTEM or the session cannot be vended.</exception>
        /// <exception cref="InvalidOperationException">Mandatory linked-token acquisition failed.</exception>
        private static SafeFileHandle GetUserPrimaryTokenViaWts(uint sessionId, ElevatedTokenType elevatedTokenType, bool uiAccess)
        {
            if (!SessionIdIsValidForVending(sessionId))
            {
                throw new ArgumentOutOfRangeException(nameof(sessionId), sessionId, "The session cannot be vended.");
            }
            if (!CanGetTokenViaWts)
            {
                throw new UnauthorizedAccessException("Direct WTS token acquisition requires Local System.");
            }
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
        /// Indicates whether the current execution context can retrieve user tokens from other sessions through process discovery.
        /// </summary>
        private static readonly bool CanGetTokenFromProcess = AccountUtilities.CallerIsAdmin;

        /// <summary>
        /// Indicates whether the current execution context can utilize token brokering to retrieve user tokens from other sessions.
        /// </summary>
        /// <remarks>An administrator brokers via a scheduled task running as the Local System account, which reaches a network
        /// path as the computer account rather than as itself. On a network path, brokering is allowed only when the Local System
        /// account has the required file system access to the client/server assemblies. This does not verify the share's permissions.</remarks>
        private static readonly bool CanGetTokenViaBroker = AccountUtilities.CallerIsAdmin && (!ClientServerUtilities.ClientServerOnNetworkPath || ClientServerPermissions.SystemAccountHasAccess());

        /// <summary>
        /// Indicates whether the current execution context can utilize WTS to retrieve user tokens from other sessions.
        /// </summary>
        private static readonly bool CanGetTokenViaWts = AccountUtilities.CallerIsLocalSystem;

        /// <summary>
        /// Indicates whether the current execution context has any route at all to a user token from another session.
        /// </summary>
        /// <remarks>Reduces to the administrator test, as every route requires one and the narrower flags only decide which
        /// route is taken rather than whether any is open. Named as the union regardless, so that it keeps describing every
        /// route should one of them come to require something else.</remarks>
        private static readonly bool CallerCanRetrieveTokens = CanGetTokenFromProcess || CanGetTokenViaBroker || CanGetTokenViaWts;
    }
}
