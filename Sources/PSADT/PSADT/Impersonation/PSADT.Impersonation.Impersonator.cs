using Microsoft.Win32.SafeHandles;
using PSADT.AccessToken;
using PSADT.PInvoke;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Principal;
using PSADT.SecureIPC;
using System.Threading.Tasks;

namespace PSADT.Impersonation
{
    /// <summary>
    /// Provides methods for impersonation and privilege management.
    /// </summary>
    public class Impersonator : IDisposable
    {
        private WindowsIdentity? _impersonatedIdentity;
        private readonly ImpersonateOptions _options;

        /// <summary>
        /// Initializes a new instance of the Impersonator class.
        /// </summary>
        /// <param name="options">The options for impersonation.</param>
        public Impersonator(ImpersonateOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Impersonates the client connected to the specified named pipe.
        /// </summary>
        /// <param name="pipeHandle">The handle to the named pipe.</param>
        /// <exception cref="SecureNamedPipeException">Thrown when impersonation fails.</exception>
        public void ImpersonateNamedPipeClient(SafePipeHandle pipeHandle)
        {
            if (!NativeMethods.ImpersonateNamedPipeClient(pipeHandle))
            {
                int error = Marshal.GetLastWin32Error();
                throw new SecureNamedPipeException($"Failed to impersonate named pipe client. Error code [{error}].", new Win32Exception(error));
            }

            _impersonatedIdentity = WindowsIdentity.GetCurrent();
            ValidateImpersonatedUser();
            AdjustImpersonatedUserPrivileges();
        }

        /// <summary>
        /// Validates the impersonated user against the impersonation options.
        /// </summary>
        /// <exception cref="SecureNamedPipeException">Thrown when the impersonated user does not meet the specified criteria.</exception>
        private void ValidateImpersonatedUser()
        {
            if (_impersonatedIdentity == null)
            {
                throw new SecureNamedPipeException("Cannot validate impersonated user: No impersonated identity.");
            }

            var isSystem = _impersonatedIdentity.IsSystem;
            var isAdmin = new WindowsPrincipal(_impersonatedIdentity).IsInRole(WindowsBuiltInRole.Administrator);
            var currentIsAdmin = new WindowsPrincipal(WindowsIdentity.GetCurrent(TokenAccessLevels.Query)).IsInRole(WindowsBuiltInRole.Administrator);

            if (isSystem && !_options.AllowSystemImpersonation)
            {
                throw new SecureNamedPipeException("Impersonation of SYSTEM account is not allowed.");
            }

            if (isAdmin && !currentIsAdmin && !_options.AllowNonAdminToAdminImpersonation)
            {
                throw new SecureNamedPipeException("Non-admin to admin impersonation is not allowed.");
            }
        }

        /// <summary>
        /// Adjusts the privileges of the impersonated user based on the impersonation options.
        /// </summary>
        /// <exception cref="SecureNamedPipeException">Thrown when privilege adjustment fails.</exception>
        private void AdjustImpersonatedUserPrivileges()
        {
            if (_impersonatedIdentity == null)
            {
                throw new SecureNamedPipeException("Cannot adjust privileges: No impersonated identity.");
            }

            if (!SafeAccessToken.TryCreate(_impersonatedIdentity.Token, out SafeAccessToken tokenHandle))
            {
                throw new SecureNamedPipeException("Failed to create a safe token handle for impersonated user.");
            }

            using (tokenHandle)
            {
                SafeAccessToken adjustedTokenHandle = tokenHandle;

                if (_options.DoNotCheckAppLockerRulesOrApplySRP)
                {
                    CreateSandboxInertToken(tokenHandle, out adjustedTokenHandle);
                }

                try
                {
                    if (_options.ReduceAdminPrivileges && new WindowsPrincipal(_impersonatedIdentity).IsInRole(WindowsBuiltInRole.Administrator))
                    {
                        ReduceAdminPrivileges(adjustedTokenHandle);
                    }

                    foreach (var privilege in _options.PrivilegesToEnable)
                    {
                        TokenManager.AdjustTokenPrivilegeInternal(adjustedTokenHandle.DangerousGetHandle(), privilege, true);
                    }

                    foreach (var privilege in _options.PrivilegesToDisable)
                    {
                        TokenManager.AdjustTokenPrivilegeInternal(adjustedTokenHandle.DangerousGetHandle(), privilege, false);
                    }

                    WindowsIdentity newIdentity = null!;
                    WindowsIdentity.RunImpersonated(adjustedTokenHandle, () =>
                    {
                        newIdentity = WindowsIdentity.GetCurrent();
                    });

                    _impersonatedIdentity = newIdentity ?? throw new SecureNamedPipeException("Failed to get the new, adjusted impersonated identity.");
                }
                finally
                {
                    if (adjustedTokenHandle != tokenHandle)
                    {
                        adjustedTokenHandle.Dispose();
                    }
                }
            }
        }

        /// <summary>
        /// Creates a restricted token with the SANDBOX_INERT flag. If set, the system does not check
        /// AppLocker rules or apply Software Restriction Policies.
        /// </summary>
        /// <param name="existingTokenHandle">The handle to the existing token.</param>
        /// <returns>A SafeAccessTokenHandle representing the new restricted token.</returns>
        /// <exception cref="SecureNamedPipeException">Thrown when the creation of the restricted token fails.</exception>
        private static void CreateSandboxInertToken(SafeAccessToken existingTokenHandle, out SafeAccessToken newTokenHandle)
        {
            if (!NativeMethods.CreateRestrictedToken(
                existingTokenHandle.DangerousGetHandle(),
                NativeMethods.SANDBOX_INERT,
                0, IntPtr.Zero, 0, IntPtr.Zero, 0, IntPtr.Zero,
                out newTokenHandle))
            {
                throw new SecureNamedPipeException("Failed to create restricted token.", new Win32Exception(Marshal.GetLastWin32Error()));
            }
        }

        /// <summary>
        /// Reduces the privileges of an administrator token.
        /// </summary>
        /// <param name="tokenHandle">The handle to the token to be adjusted.</param>
        /// <exception cref="SecureNamedPipeException">Thrown when privilege reduction fails.</exception>
        private static void ReduceAdminPrivileges(SafeAccessToken tokenHandle)
        {
            if (!NativeMethods.DuplicateTokenEx(
                    tokenHandle,
                    TokenAccess.TOKEN_ADJUST_PRIVILEGES | TokenAccess.TOKEN_DUPLICATE | TokenAccess.TOKEN_QUERY,
                    SECURITY_ATTRIBUTES.Create(),
                    SECURITY_IMPERSONATION_LEVEL.SecurityImpersonation,
                    TOKEN_TYPE.TokenPrimary,
                    out SafeAccessToken newTokenHandle))
            {
                throw new SecureNamedPipeException("Failed to duplicate token for privilege reduction.", new Win32Exception(Marshal.GetLastWin32Error()));
            }

            using (newTokenHandle)
            {
                // Remove all privileges by setting PrivilegeCount to 0
                var tokenPrivileges = new TOKEN_PRIVILEGES { PrivilegeCount = 0 };
                if (!NativeMethods.AdjustTokenPrivileges(newTokenHandle.DangerousGetHandle(), true, ref tokenPrivileges, 0, IntPtr.Zero, IntPtr.Zero))
                {
                    throw new SecureNamedPipeException("Failed to adjust token privileges for admin reduction.", new Win32Exception(Marshal.GetLastWin32Error()));
                }

                // Enable a standerd set of permissions for a standard user
                TokenManager.SetStandardUserPrivileges(tokenHandle, true);

                // Impersonate with the reduced privileges
                WindowsIdentity.RunImpersonated(newTokenHandle, () => { });
            }
        }

        /// <summary>
        /// Executes an action while impersonating the client.
        /// </summary>
        /// <param name="action">The action to execute while impersonated.</param>
        /// <exception cref="SecureNamedPipeException">Thrown when impersonation fails.</exception>
        public void Impersonate(Action action)
        {
            if (_impersonatedIdentity == null)
            {
                throw new SecureNamedPipeException("Impersonation has not been performed.");
            }

            WindowsIdentity.RunImpersonated(_impersonatedIdentity.AccessToken, action);
        }

        public async Task ImpersonateAsync(Func<Task> action)
        {
            if (_impersonatedIdentity == null)
            {
                throw new SecureNamedPipeException("Impersonation has not been performed.");
            }

            await WindowsIdentity.RunImpersonated(_impersonatedIdentity.AccessToken, action).ConfigureAwait(false);
        }


        /// <summary>
        /// Reverts the impersonation.
        /// </summary>
        /// <exception cref="SecureNamedPipeException">Thrown when reverting impersonation fails.</exception>
        public void RevertToSelf()
        {
            if (_impersonatedIdentity != null)
            {
                _impersonatedIdentity.Dispose();
                _impersonatedIdentity = null;
            }

            if (!NativeMethods.RevertToSelf())
            {
                throw new SecureNamedPipeException("Failed to revert impersonation.", new Win32Exception(Marshal.GetLastWin32Error()));
            }
        }

        /// <summary>
        /// Disposes the Impersonator instance.
        /// </summary>
        public void Dispose()
        {
            RevertToSelf();
            GC.SuppressFinalize(this);
        }
    }
}
