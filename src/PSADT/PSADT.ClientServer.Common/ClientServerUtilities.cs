using System;
using System.IO;
using Microsoft.Win32;
using PSADT.Interop.Extensions;

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
        /// Gets the directory path of the assembly that contains the ClientServerUtilities type.
        /// </summary>
        /// <remarks>This field can be used to locate files or resources relative to the assembly's
        /// location at runtime. The returned path is determined by the assembly's current location, which may vary
        /// depending on how the application is deployed or executed.</remarks>
        public static readonly DirectoryInfo AssemblyPath = new DirectoryInfo(typeof(ClientServerUtilities).Assembly.Location).Parent ?? throw new InvalidOperationException("Failed to retrieve directory for this assembly.");

        /// <summary>
        /// Gets the default file system path to the ClientServer client executable within the application directory
        /// structure.
        /// </summary>
        /// <remarks>This path is constructed by combining the assembly's directory path with the
        /// executable name "PSADT.ClientServer.Client.exe". It is intended for use when launching or referencing the
        /// ClientServer client from within the application.</remarks>
        internal static readonly FileInfo ClientDefaultPath = new(Path.Combine(AssemblyPath.FullName, "PSADT.ClientServer.Client.exe"));

        /// <summary>
        /// Gets the file path for the compatible version of the PSADT Client Server executable.
        /// </summary>
        /// <remarks>This path is constructed by combining the base assembly path with the specific
        /// executable name. Ensure that the executable exists at the specified location before attempting to use
        /// it.</remarks>
        internal static readonly FileInfo ClientCompatiblePath = new(Path.Combine(AssemblyPath.FullName, "PSADT.ClientServer.Client.Compatible.exe"));

        /// <summary>
        /// Gets the path to the client server executable, selecting a compatible version if the primary executable is
        /// not Authenticode trusted.
        /// </summary>
        /// <remarks>The path is determined based on the trust status of the primary executable. If the
        /// primary executable is not trusted, the compatible version is used instead.</remarks>
        public static readonly FileInfo ClientPath = !ClientDefaultPath.IsAuthenticodeTrusted()
            ? ClientCompatiblePath
            : ClientDefaultPath;

        /// <summary>
        /// Gets the default file system path for the Client Server Client Launcher executable.
        /// </summary>
        /// <remarks>The path is constructed by combining the assembly directory with the executable name.
        /// Use this value to locate the launcher for the Client Server Client application when performing operations
        /// that require its presence.</remarks>
        internal static readonly FileInfo ClientLauncherDefaultPath = new(Path.Combine(AssemblyPath.FullName, "PSADT.ClientServer.Client.Launcher.exe"));

        /// <summary>
        /// Gets the file path for the compatible version of the Client Server Client Launcher executable.
        /// </summary>
        /// <remarks>This path is constructed by combining the assembly path with the executable name.
        /// Ensure that the executable is present at the specified location for proper functionality.</remarks>
        internal static readonly FileInfo ClientLauncherCompatiblePath = new(Path.Combine(AssemblyPath.FullName, "PSADT.ClientServer.Client.Launcher.Compatible.exe"));

        /// <summary>
        /// Gets the path to the client server launcher executable, selecting a compatible version if the primary
        /// executable is not Authenticode trusted.
        /// </summary>
        /// <remarks>This path is determined based on the trust status of the primary launcher executable.
        /// If the primary executable is not trusted, the compatible version will be used instead.</remarks>
        public static readonly FileInfo ClientLauncherPath = !ClientLauncherDefaultPath.IsAuthenticodeTrusted()
            ? ClientLauncherCompatiblePath
            : ClientLauncherDefaultPath;

        /// <summary>
        /// Specifies the registry path used for storing PSAppDeployToolkit configuration settings for the current user.
        /// </summary>
        public const string UserRegistryPath = "HKEY_CURRENT_USER\\SOFTWARE\\PSAppDeployToolkit";

        /// <summary>
        /// Specifies the registry value name used to indicate that the operation should not wait for success.
        /// </summary>
        public const string OperationSuccessRegistryValueName = "ClientServerOperationSuccess";

        /// <summary>
        /// Specifies the exit code used to indicate a successful shell execute process operation in the client-server communication protocol.
        /// </summary>
        /// <remarks>The value of this constant is derived from `'ShellExecuteProcess'.GetHashCode()` under Windows PowerShell 5.1.</remarks>
        public const int ShellExecuteProcessSuccessCode = -1556154312;
    }
}
