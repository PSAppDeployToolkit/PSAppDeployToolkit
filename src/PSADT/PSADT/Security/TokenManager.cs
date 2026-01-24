using System;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Threading;
using Microsoft.Win32.SafeHandles;
using PSADT.AccountManagement;
using PSADT.Extensions;
using PSADT.Foundation;
using PSADT.LibraryInterfaces;
using PSADT.LibraryInterfaces.Extensions;
using PSADT.Utilities;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Security;
using Windows.Win32.System.Com;
using Windows.Win32.System.TaskScheduler;
using Windows.Win32.System.Threading;

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
        /// Retrieves the primary security token for a specified user, optionally obtaining a linked administrative
        /// token or the highest available token.
        /// </summary>
        /// <remarks>This method retrieves the primary token for a user by either querying the user's
        /// session or obtaining the token from an active process associated with the user. If the caller is not running
        /// as the local system account, the method attempts to locate the user's Explorer process and retrieve its
        /// token. If the caller is running as the local system account, the method directly queries the user's session
        /// token.</remarks>
        /// <param name="runAsActiveUser">The user for whom the primary token is being retrieved. This must represent an active session.</param>
        /// <param name="useLinkedAdminToken">A value indicating whether to retrieve the linked administrative token for the user, if available. If <see
        /// langword="true"/>, the method attempts to retrieve the linked token.</param>
        /// <param name="useHighestAvailableToken">A value indicating whether to retrieve the highest available token for the user if the linked administrative
        /// token cannot be obtained. If <see langword="true"/>, the method falls back to the highest available token
        /// when the linked token is unavailable.</param>
        /// <returns>A <see cref="SafeFileHandle"/> representing the primary token for the specified user.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the primary token for the specified user cannot be retrieved. This may occur if the user is not
        /// logged on or does not have an active session.</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown if the linked administrative token cannot be retrieved and <paramref
        /// name="useHighestAvailableToken"/> is <see langword="false"/>.</exception>
        internal static SafeFileHandle GetUserPrimaryToken(RunAsActiveUser runAsActiveUser, bool useLinkedAdminToken, bool useHighestAvailableToken)
        {
            // Validate parameters.
            if (runAsActiveUser is null)
            {
                throw new ArgumentNullException(nameof(runAsActiveUser));
            }

            // Confirm that the caller is an administrator.
            if (!AccountUtilities.CallerIsAdmin)
            {
                throw new UnauthorizedAccessException("The caller must be an administrator to retrieve another user's primary token.");
            }

            // Get the user's token. If we're not local system, we need to use the token broker.
            if (!AccountUtilities.CallerIsLocalSystem)
            {
                // Set up the pipe server and start the client/server token broker process.
                PrivilegeManager.EnablePrivilegeIfDisabled(SE_PRIVILEGE.SeDebugPrivilege);
                string pipeName = $"PSADT.ClientServer.Client_TokenBroker_{CryptographicUtilities.SecureNewGuid()}";
                PipeSecurity pipeSecurity = new(); pipeSecurity.AddAccessRule(new(AccountUtilities.LocalSystemSid, PipeAccessRights.CreateNewInstance | PipeAccessRights.ReadWrite, AccessControlType.Allow));
                using NamedPipeServerStream pipe = CreateNamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.None, 0, 0, pipeSecurity);
                _ = Ole32.CoInitializeEx(Thread.CurrentThread.GetApartmentState().Equals(ApartmentState.STA) ? COINIT.COINIT_APARTMENTTHREADED : COINIT.COINIT_MULTITHREADED);
                Guid CLSID_TaskScheduler = new("0F87369F-A4E5-4CFC-BD3E-73E6154572DD");
                try
                {
                    // Create an instance of the TaskService to manage scheduled tasks and connect on localhost.
                    _ = Ole32.CoCreateInstance(in CLSID_TaskScheduler, null, CLSCTX.CLSCTX_INPROC_SERVER, out ITaskService servicePtr);
                    servicePtr.Connect(null, null, null, null);

                    // Set up the task as required.
                    BSTR folderName = (BSTR)Marshal.StringToBSTR(@"\");
                    BSTR taskName = (BSTR)Marshal.StringToBSTR(pipeName);
                    BSTR userId = (BSTR)Marshal.StringToBSTR(AccountUtilities.LocalSystemSid.Value);
                    BSTR path = (BSTR)Marshal.StringToBSTR(typeof(TokenManager).Assembly.Location.Replace(".dll", ".ClientServer.Client.exe"));
                    BSTR args = (BSTR)Marshal.StringToBSTR($"/TokenBroker -PipeName {pipeName} -ProcessId {AccountUtilities.CallerProcessId} -SessionId {runAsActiveUser.SessionId} -UseLinkedAdminToken {useLinkedAdminToken} -UseHighestAvailableToken {useHighestAvailableToken}");
                    try
                    {
                        // Create a new task definition.
                        servicePtr.GetFolder(folderName, out ITaskFolder rootFolder);
                        servicePtr.NewTask(0, out ITaskDefinition taskDefinition);
                        taskDefinition.Actions.Create(TASK_ACTION_TYPE.TASK_ACTION_EXEC, out IAction action);
                        IExecAction execAction = (IExecAction)action;
                        taskDefinition.Principal.UserId = userId;
                        taskDefinition.Principal.LogonType = TASK_LOGON_TYPE.TASK_LOGON_SERVICE_ACCOUNT;
                        execAction.Path = path;
                        execAction.Arguments = args;

                        // Register and start the task, then delete it. It'll keep running until it exits.
                        rootFolder.RegisterTaskDefinition(taskName, taskDefinition, (int)TASK_CREATION.TASK_CREATE_OR_UPDATE, null, null, TASK_LOGON_TYPE.TASK_LOGON_SERVICE_ACCOUNT, null, out IRegisteredTask task);
                        try
                        {
                            task.Run(null, out _);
                        }
                        finally
                        {
                            rootFolder.DeleteTask(taskName, 0);
                        }
                    }
                    finally
                    {
                        // Free all binary strings.
                        Marshal.FreeBSTR(args);
                        Marshal.FreeBSTR(path);
                        Marshal.FreeBSTR(userId);
                        Marshal.FreeBSTR(taskName);
                        Marshal.FreeBSTR(folderName);
                    }
                }
                finally
                {
                    // Uninitialize the COM library for the current thread.
                    PInvoke.CoUninitialize();
                }

                // Wait for the token broker to connect.
                using CancellationTokenSource cts = new(TimeSpan.FromSeconds(15));
                pipe.WaitForConnectionAsync(cts.Token).GetAwaiter().GetResult();

                // Read the token length from the pipe.
                if (pipe.ReadByte() is int tokenLength && tokenLength == -1)
                {
                    throw new InvalidOperationException("No token length received from the token broker.");
                }
                if (tokenLength is not 4 and not 8)
                {
                    throw new InvalidOperationException("Invalid token length received from the token broker.");
                }

                // Read the token from the pipe.
                byte[] tokenBuf = new byte[tokenLength];
                if (pipe.Read(tokenBuf, 0, tokenLength) != tokenLength)
                {
                    throw new InvalidOperationException("Invalid token received from the token broker.");
                }

                // Return the token handle.
                return new((nint)(tokenLength == 8 ? BitConverter.ToInt64(tokenBuf, 0) : BitConverter.ToInt32(tokenBuf, 0)), true);
            }
            else
            {
                // When we're local system, we can just get the primary token for the user.
                PrivilegeManager.EnablePrivilegeIfDisabled(SE_PRIVILEGE.SeTcbPrivilege);
                _ = WtsApi32.WTSQueryUserToken(runAsActiveUser.SessionId, out SafeFileHandle hUserToken);
                using (hUserToken)
                {
                    if (useLinkedAdminToken || useHighestAvailableToken)
                    {
                        try
                        {
                            return GetLinkedPrimaryToken(hUserToken);
                        }
                        catch (Exception ex)
                        {
                            if (!useHighestAvailableToken)
                            {
                                throw new UnauthorizedAccessException($"Failed to get the linked admin token for user [{runAsActiveUser.NTAccount}].", ex);
                            }
                        }
                    }
                    return GetPrimaryToken(hUserToken);
                }
            }
        }

        /// <summary>
        /// Retrieves a primary token for the Explorer process with limited access rights.
        /// </summary>
        /// <remarks>This method obtains a token associated with the Explorer process and duplicates it to
        /// create a primary token. The returned token can be used for operations requiring an unelevated
        /// context.</remarks>
        /// <returns>A <see cref="SafeFileHandle"/> representing the primary token for the Explorer process, or <see
        /// langword="null"/> if the operation fails.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Enforcing this rule just makes a mess.")]
        internal static SafeFileHandle GetUnelevatedCallerToken()
        {
            if (AccountUtilities.CallerIsLocalSystem)
            {
                throw new InvalidOperationException("Cannot retrieve an unelevated token when running as the local system account.");
            }
            if (!AccountUtilities.CallerIsAdmin)
            {
                throw new InvalidOperationException("The current process is already running with an unelevated token.");
            }
            using SafeFileHandle hProcess = Kernel32.OpenProcess(PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_LIMITED_INFORMATION, false, ShellUtilities.GetExplorerProcessId());
            _ = AdvApi32.OpenProcessToken(hProcess, TOKEN_ACCESS_MASK.TOKEN_QUERY | TOKEN_ACCESS_MASK.TOKEN_DUPLICATE, out SafeFileHandle hProcessToken);
            using (hProcessToken)
            {
                if (TokenUtilities.GetTokenSid(hProcessToken) != AccountUtilities.CallerSid)
                {
                    throw new InvalidOperationException("Failed to retrieve an unelevated token for the calling account.");
                }
                if (TokenUtilities.IsTokenElevated(hProcessToken))
                {
                    throw new InvalidOperationException("The calling account's shell is running elevated, therefore unable to get unelevated token.");
                }
                return GetPrimaryToken(hProcessToken);
            }
        }

        /// <summary>
        /// Retrieves the primary token associated with the specified security token handle.
        /// </summary>
        /// <remarks>This method duplicates the specified security token to create a primary token, which
        /// can be used for impersonation or other security-related operations. Ensure that the caller has appropriate
        /// permissions to access and duplicate the token.</remarks>
        /// <param name="tokenHandle">A handle to the security token. This handle must have the necessary access rights to allow duplication.</param>
        /// <returns>A <see cref="SafeFileHandle"/> representing the duplicated primary token.</returns>
        internal static SafeFileHandle GetPrimaryToken(SafeHandle tokenHandle)
        {
            _ = AdvApi32.DuplicateTokenEx(tokenHandle, TOKEN_ACCESS_MASK.TOKEN_ALL_ACCESS, null, SECURITY_IMPERSONATION_LEVEL.SecurityIdentification, TOKEN_TYPE.TokenPrimary, out SafeFileHandle hPrimaryToken);
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
            Span<byte> buffer = stackalloc byte[Marshal.SizeOf<TOKEN_LINKED_TOKEN>()];
            _ = AdvApi32.GetTokenInformation(tokenHandle, TOKEN_INFORMATION_CLASS.TokenLinkedToken, buffer, out _);
            ref readonly TOKEN_LINKED_TOKEN tokenLinkedToken = ref buffer.AsReadOnlyStructure<TOKEN_LINKED_TOKEN>();
            return new(tokenLinkedToken.LinkedToken, true);
        }

        /// <summary>
        /// Retrieves the primary token linked to the specified token handle.
        /// </summary>
        /// <remarks>This method uses the provided token handle to obtain a linked token and then
        /// retrieves its primary token. The caller is responsible for ensuring the validity of the input token
        /// handle.</remarks>
        /// <param name="tokenHandle">A <see cref="SafeHandle"/> representing the token handle for which the linked primary token is to be
        /// retrieved.</param>
        /// <returns>A <see cref="SafeFileHandle"/> representing the linked primary token.</returns>
        internal static SafeFileHandle GetLinkedPrimaryToken(SafeHandle tokenHandle)
        {
            using SafeFileHandle linkedToken = GetLinkedToken(tokenHandle);
            return GetPrimaryToken(linkedToken);
        }

        /// <summary>
        /// Retrieves the highest available primary token associated with the specified token handle.
        /// </summary>
        /// <remarks>This method attempts to retrieve the linked primary token associated with the
        /// specified token handle. If the linked token is unavailable, it falls back to retrieving the primary token of
        /// the original token handle.</remarks>
        /// <param name="tokenHandle">A <see cref="SafeHandle"/> representing the token handle for which the primary token is to be retrieved.</param>
        /// <returns>A <see cref="SafeFileHandle"/> representing the highest available primary token.</returns>
        internal static SafeFileHandle GetHighestPrimaryToken(SafeHandle tokenHandle)
        {
            // If the linked token is not available, fall back to the primary token of the original token handle.
            try
            {
                return GetLinkedPrimaryToken(tokenHandle);
            }
            catch
            {
                return GetPrimaryToken(tokenHandle);
                throw;
            }
        }

        /// <summary>
        /// Creates a new instance of a named pipe server stream with the specified configuration parameters.
        /// </summary>
        /// <remarks>This method allows fine-grained control over the creation of a named pipe server,
        /// including security, buffer sizes, and transmission mode. The caller is responsible for managing the lifetime
        /// and disposal of the returned stream. The pipe name must not conflict with existing named pipes on the
        /// system.</remarks>
        /// <param name="pipeName">The name of the pipe to create. This value must be unique on the system and cannot be null or empty.</param>
        /// <param name="direction">The direction of the pipe, indicating whether the pipe supports reading, writing, or both.</param>
        /// <param name="maxNumberOfServerInstances">The maximum number of server instances that can simultaneously share the same pipe name. Must be greater
        /// than zero.</param>
        /// <param name="transmissionMode">The transmission mode for the pipe, specifying how data is transmitted through the pipe (byte or message
        /// mode).</param>
        /// <param name="options">Pipe options that modify the behavior of the pipe, such as asynchronous operation or write-through.</param>
        /// <param name="inBufferSize">The size, in bytes, of the input buffer for the pipe. Must be a positive integer.</param>
        /// <param name="outBufferSize">The size, in bytes, of the output buffer for the pipe. Must be a positive integer.</param>
        /// <param name="pipeSecurity">An optional PipeSecurity object that specifies access control for the pipe. If null, default security is
        /// applied.</param>
        /// <returns>A NamedPipeServerStream instance configured with the specified parameters and ready to accept client
        /// connections.</returns>
        private static NamedPipeServerStream CreateNamedPipeServerStream(string pipeName, PipeDirection direction, int maxNumberOfServerInstances, PipeTransmissionMode transmissionMode, PipeOptions options, int inBufferSize, int outBufferSize, PipeSecurity? pipeSecurity)
        {
#if !NETFRAMEWORK
            return NamedPipeServerStreamAcl.Create(pipeName, direction, maxNumberOfServerInstances, transmissionMode, options, inBufferSize, outBufferSize, pipeSecurity);
#else
            return new NamedPipeServerStream(pipeName, direction, maxNumberOfServerInstances, transmissionMode, options, inBufferSize, outBufferSize, pipeSecurity);
#endif
        }
    }
}
