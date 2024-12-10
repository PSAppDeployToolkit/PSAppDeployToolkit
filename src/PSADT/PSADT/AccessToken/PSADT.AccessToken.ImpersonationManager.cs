using System;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Security.Principal;
using System.Management.Automation;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using PSADT.Shared;
using PSADT.PInvoke;
using PSADT.Logging;

namespace PSADT.AccessToken
{
    /// <summary>
    /// Manages impersonation of Windows identities and their privileges. Thread-safe.
    /// </summary>
    public sealed partial class ImpersonationManager : IDisposable
    {
        private WindowsIdentity? _impersonatedIdentity;
        private readonly ImpersonationOptions _options;
        private bool _disposed;
        private readonly SemaphoreSlim _impersonationLock = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Gets the currently impersonated identity, if any.
        /// </summary>
        public WindowsIdentity? CurrentIdentity
        {
            get
            {
                ThrowIfDisposed();
                lock (this)
                {
                    return _impersonatedIdentity;
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether impersonation is active.
        /// </summary>
        public bool IsImpersonating
        {
            get
            {
                ThrowIfDisposed();
                lock (this)
                {
                    return _impersonatedIdentity != null && !_impersonatedIdentity.AccessToken.IsInvalid;
                }
            }
        }

        /// <summary>
        /// Initializes a new instance with default options.
        /// </summary>
        /// <param name="options"></param>
        private ImpersonationManager(ImpersonationOptions? options = null)
        {
            _options = options ?? new ImpersonationOptions();
        }

        /// <summary>
        /// Initializes a new instance using a session ID.
        /// </summary>
        public ImpersonationManager(uint sessionId, ImpersonationOptions? options = null)
        {
            _options = options ?? new ImpersonationOptions();
            SetIdentityForSessionId(sessionId);
        }

        /// <summary>
        /// Asynchronously initializes a new instance using a session ID.
        /// </summary>
        public static async Task<ImpersonationManager> CreateAsync(uint sessionId, ImpersonationOptions? options = null, CancellationToken cancellationToken = default)
        {
            var manager = new ImpersonationManager(options);
            await manager.SetIdentityForSessionIdAsync(sessionId, cancellationToken).ConfigureAwait(false);
            return manager;
        }

        /// <summary>
        /// Initializes a new instance using a named pipe.
        /// </summary>
        public ImpersonationManager(SafePipeHandle pipeHandle, ImpersonationOptions? options = null)
        {
            _options = options ?? new ImpersonationOptions();
            SetIdentityForConnectedNamedPipeClient(pipeHandle);
        }

        /// <summary>
        /// Asynchronously initializes a new instance using a named pipe.
        /// </summary>
        public static async Task<ImpersonationManager> CreateAsync(SafePipeHandle pipeHandle, ImpersonationOptions? options = null, CancellationToken cancellationToken = default)
        {
            var manager = new ImpersonationManager(options);
            await manager.SetIdentityForConnectedNamedPipeClientAsync(pipeHandle, cancellationToken).ConfigureAwait(false);
            return manager;
        }

        /// <summary>
        /// Sets the identity for the specified session ID synchronously.
        /// </summary>
        private void SetIdentityForSessionId(uint sessionId)
        {
            lock (this)
            {
                try
                {
                    if (!TokenManager.GetSecurityIdentificationTokenForSessionId(sessionId, out SafeAccessToken securityIdentificationToken))
                    {
                        throw new InvalidOperationException($"Failed to obtain security identification token for session ID [{sessionId}].");
                    }

                    using (securityIdentificationToken)
                    {
                        SetAndConfigureIdentity(securityIdentificationToken);
                    }
                }
                catch (Exception ex)
                {
                    CleanupIdentity();
                    throw new InvalidOperationException($"Failed to get identity for session ID [{sessionId}].", ex);
                }
            }
        }

        /// <summary>
        /// Sets the identity for the specified session ID asynchronously.
        /// </summary>
        private async Task SetIdentityForSessionIdAsync(uint sessionId, CancellationToken cancellationToken)
        {
            await _impersonationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                SetIdentityForSessionId(sessionId);
            }
            finally
            {
                _impersonationLock.Release();
            }
        }

        /// <summary>
        /// Sets and configures an identity from a token synchronously.
        /// </summary>
        private void SetAndConfigureIdentity(SafeAccessToken token)
        {
            if (!TokenManager.CreateImpersonationToken(token, out SafeAccessToken impersonationToken))
            {
                throw new InvalidOperationException("Failed to create impersonation token.");
            }

            using (impersonationToken)
            {
                if (!TokenManager.TryGetWindowsIdentity(impersonationToken, out _impersonatedIdentity))
                {
                    throw new InvalidOperationException("Failed to get Windows identity.");
                }

                CheckImpersonationRestrictions();
                ApplyTokenImpersonationOptions(impersonationToken);
            }
        }

        /// <summary>
        /// Sets the identity for a connected named pipe client synchronously.
        /// </summary>
        private void SetIdentityForConnectedNamedPipeClient(SafePipeHandle pipeHandle)
        {
            if (pipeHandle == null) throw new ArgumentNullException(nameof(pipeHandle));

            lock (this)
            {
                bool impersonated = false;
                try
                {
                    if (!NativeMethods.ImpersonateNamedPipeClient(pipeHandle))
                    {
                        int error = Marshal.GetLastWin32Error();
                        throw new InvalidOperationException(
                            $"Failed to impersonate named pipe client. Error code [{error}].",
                            new Win32Exception(error));
                    }

                    impersonated = true;
                    _impersonatedIdentity = WindowsIdentity.GetCurrent();

                    CheckImpersonationRestrictions();

                    if (!SafeAccessToken.TryCreate(_impersonatedIdentity.Token, out SafeAccessToken tokenHandle))
                    {
                        throw new InvalidOperationException("Failed to create a safe token handle for impersonated user.");
                    }

                    ApplyTokenImpersonationOptions(tokenHandle);
                }
                catch (Exception ex)
                {
                    CleanupIdentity();
                    throw new InvalidOperationException("Failed to get identity from named pipe client.", ex);
                }
                finally
                {
                    if (impersonated)
                    {
                        RevertImpersonation();
                    }
                }
            }
        }

        /// <summary>
        /// Sets the identity for a connected named pipe client asynchronously.
        /// </summary>
        private async Task SetIdentityForConnectedNamedPipeClientAsync(SafePipeHandle pipeHandle, CancellationToken cancellationToken)
        {
            await _impersonationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                SetIdentityForConnectedNamedPipeClient(pipeHandle);
            }
            finally
            {
                _impersonationLock.Release();
            }
        }

        /// <summary>
        /// Checks impersonation restrictions synchronously.
        /// </summary>
        private void CheckImpersonationRestrictions()
        {
            if (_impersonatedIdentity == null)
            {
                throw new InvalidOperationException("Cannot validate impersonated user: No impersonated identity.");
            }

            if (_impersonatedIdentity.IsSystem && !_options.AllowSystemImpersonation)
            {
                throw new InvalidOperationException("Impersonation of SYSTEM account is not allowed.");
            }
        }

        /// <summary>
        /// Applies token privileges synchronously.
        /// </summary>
        private void ApplyTokenImpersonationOptions(SafeAccessToken tokenHandle)
        {
            try
            {
                if (_options.ReduceAdminPrivileges &&
                    new WindowsPrincipal(_impersonatedIdentity!).IsInRole(WindowsBuiltInRole.Administrator))
                {
                    PrivilegeManager.RemoveAllPrivileges(tokenHandle);
                    PrivilegeManager.SetStandardUserPrivileges(tokenHandle, true);
                }

                if (_options.PrivilegesToEnable.Count > 0)
                {
                    PrivilegeManager.AdjustTokenPrivileges(tokenHandle, _options.PrivilegesToEnable, true);
                }

                if (_options.PrivilegesToDisable.Count > 0)
                {
                    PrivilegeManager.AdjustTokenPrivileges(tokenHandle, _options.PrivilegesToDisable, false);
                }

                WindowsIdentity? newIdentity = null;
                WindowsIdentity.RunImpersonated(tokenHandle, () =>
                {
                    newIdentity = WindowsIdentity.GetCurrent();
                });

                _impersonatedIdentity!.Dispose();
                _impersonatedIdentity = newIdentity ??
                    throw new InvalidOperationException("Failed to get new adjusted impersonated identity.");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to apply token impersonation options.", ex);
            }
        }

        /// <summary>
        /// Executes an action with MTA threading under impersonation.
        /// </summary>
        /// <typeparam name="T">The type of the result.</typeparam>
        /// <param name="action">The action to execute.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task<T> RunImpersonatedMethodWithMTAAsync<T>(
            [PowerShellScriptBlock] Func<Task<T>> action,
            CancellationToken cancellationToken = default)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            await _impersonationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                ThrowIfDisposed();
                ValidateImpersonationState();

                var taskCompletionSource = new TaskCompletionSource<T>();
                Exception? exception = null;

                // Handle PowerShell ScriptBlock if present
                if (action.Target is ScriptBlock scriptBlock)
                {
                    var scriptBlockWrapper = new ScriptBlockWrapper(action, scriptBlock);
                    action = scriptBlockWrapper.GetWrappedAsyncFunc<T>();
                }

                var thread = new Thread(() =>
                {
                    try
                    {
                        var result = RunImpersonatedMethodAsync(action, cancellationToken).GetAwaiter().GetResult();
                        taskCompletionSource.SetResult(result);
                    }
                    catch (Exception ex)
                    {
                        exception = ex;
                        taskCompletionSource.SetException(ex);
                    }
                });

                thread.SetApartmentState(ApartmentState.MTA);
                thread.Start();

                try
                {
                    using (cancellationToken.Register(() => taskCompletionSource.TrySetCanceled()))
                    {
                        return await taskCompletionSource.Task.ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch when (exception != null)
                {
                    throw new InvalidOperationException("Execution in MTA thread failed.", exception);
                }
            }
            finally
            {
                _impersonationLock.Release();
            }
        }

        /// <summary>
        /// Executes an action with MTA threading under impersonation without returning a value.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task RunImpersonatedMethodWithMTAAsync(
            [PowerShellScriptBlock] Func<Task> action,
            CancellationToken cancellationToken = default)
        {
            await RunImpersonatedMethodWithMTAAsync(async () =>
            {
                await action().ConfigureAwait(false);
                return true;
            }, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes an action asynchronously under the current impersonated identity.
        /// </summary>
        /// <typeparam name="T">The type of the result.</typeparam>
        /// <param name="action">The action to execute.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when action is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when impersonation fails or is invalid.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when the manager has been disposed.</exception>
        public async Task<T> RunImpersonatedMethodAsync<T>(
            [PowerShellScriptBlock] Func<Task<T>> action,
            CancellationToken cancellationToken = default)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            await _impersonationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                ThrowIfDisposed();
                ValidateImpersonationState();

                var taskCompletionSource = new TaskCompletionSource<T>();
                Exception? exception = null;

                // Handle PowerShell ScriptBlock if present
                if (action.Target is ScriptBlock scriptBlock)
                {
                    var scriptBlockWrapper = new ScriptBlockWrapper(action, scriptBlock);
                    action = scriptBlockWrapper.GetWrappedAsyncFunc<T>();
                }

                try
                {
                    using (cancellationToken.Register(() => taskCompletionSource.TrySetCanceled()))
                    {
                        return await WindowsIdentity.RunImpersonated(_impersonatedIdentity!.AccessToken, async () =>
                        {
                            try
                            {
                                cancellationToken.ThrowIfCancellationRequested();
                                return await action().ConfigureAwait(false);
                            }
                            catch (Exception ex)
                            {
                                exception = ex;
                                UnifiedLogger.Create().Message("Error executing impersonated method.").Error(ex).Severity(LogLevel.Error);
                                throw;
                            }
                        }).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch when (exception != null)
                {
                    throw new InvalidOperationException("Execution under impersonation failed.", exception);
                }
            }
            finally
            {
                _impersonationLock.Release();
            }
        }

        /// <summary>
        /// Executes an action asynchronously under impersonation without returning a value.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when action is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when impersonation fails or is invalid.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when the manager has been disposed.</exception>
        public async Task RunImpersonatedMethodAsync(
            [PowerShellScriptBlock] Func<Task> action,
            CancellationToken cancellationToken = default)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            await RunImpersonatedMethodAsync(async () =>
            {
                await action().ConfigureAwait(false);
                return true;
            }, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Cleans up the current impersonated identity.
        /// </summary>
        private void CleanupIdentity()
        {
            _impersonatedIdentity?.Dispose();
            _impersonatedIdentity = null;
        }

        /// <summary>
        /// Reverts any active impersonation.
        /// </summary>
        private static void RevertImpersonation()
        {
            if (!NativeMethods.RevertToSelf())
            {
                UnifiedLogger.Create().Message("Failed to revert impersonation.").Error(new Win32Exception(Marshal.GetLastWin32Error())).Severity(LogLevel.Warning);
            }
        }

        /// <summary>
        /// Ensures the object hasn't been disposed.
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(ImpersonationManager));
            }
        }

        /// <summary>
        /// Validates the current impersonation state.
        /// </summary>
        private void ValidateImpersonationState()
        {
            if (!IsImpersonating)
            {
                throw new InvalidOperationException("No active impersonation context.");
            }
        }

        /// <summary>
        /// Disposes of resources.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                lock (this)
                {
                    if (!_disposed)
                    {
                        _disposed = true;
                        CleanupIdentity();
                        _impersonationLock.Dispose();
                    }
                }
            }
        }
    }
}
