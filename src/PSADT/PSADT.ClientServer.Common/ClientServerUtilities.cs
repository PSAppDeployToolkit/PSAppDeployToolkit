using Microsoft.Win32;

namespace PSADT.ClientServer
{
    /// <summary>
    /// Provides utility constants and methods for managing client-server operation state and configuration settings in
    /// the PSAppDeployToolkit environment.
    /// </summary>
    /// <remarks>This class contains constants for registry paths and value names used to store and retrieve
    /// client-server operation status for the current user. It is intended for use within the PSAppDeployToolkit
    /// infrastructure and is not designed for direct use by external code.</remarks>
    public static class ClientServerUtilities
    {
        /// <summary>
        /// Marks the operation as successful by setting the corresponding registry value to indicate a no-wait state.
        /// </summary>
        /// <remarks>This method updates a specific registry key to signal that a no-wait operation has
        /// completed successfully. It is intended for internal use and should not be called directly by external
        /// code.</remarks>
        internal static void SetClientServerOperationSuccess()
        {
            Registry.SetValue(UserRegistryPath, OperationSuccessRegistryValueName, 1, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Specifies the registry path used for storing PSAppDeployToolkit configuration settings for the current user.
        /// </summary>
        public const string UserRegistryPath = "HKEY_CURRENT_USER\\SOFTWARE\\PSAppDeployToolkit";

        /// <summary>
        /// Specifies the registry value name used to indicate that the operation should not wait for success.
        /// </summary>
        public const string OperationSuccessRegistryValueName = "ClientServerOperationSuccess";
    }
}
