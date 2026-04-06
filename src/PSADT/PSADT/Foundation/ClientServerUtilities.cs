using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.Win32;
using PSADT.AccountManagement;
using PSADT.Interop.Extensions;
using PSADT.ProcessManagement;
using PSADT.Security;

namespace PSADT.Foundation
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
        /// Launches the client process with the specified arguments and security context, returning a handle to the
        /// created process.
        /// </summary>
        /// <param name="argumentList">The list of command-line arguments to pass to the client process.</param>
        /// <param name="runAsActiveUser">If specified, determines whether the client process should be launched as the active user.</param>
        /// <param name="elevatedTokenType">Specifies the elevation level to use when launching the client process.</param>
        /// <exception cref="InvalidOperationException">Thrown if the client process fails to launch.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ProcessHandle StartClientOperation(IReadOnlyList<string> argumentList, RunAsActiveUser? runAsActiveUser, ElevatedTokenType? elevatedTokenType)
        {
            return InvokeClientOperationImpl(ClientDefaultPath, argumentList, runAsActiveUser, elevatedTokenType);
        }

        /// <summary>
        /// Launches the client process with the specified arguments and security context, returning a handle to the
        /// created process.
        /// </summary>
        /// <param name="argumentList">The list of command-line arguments to pass to the client process.</param>
        /// <param name="runAsActiveUser">If specified, determines whether the client process should be launched as the active user.</param>
        /// <param name="elevatedTokenType">Specifies the elevation level to use when launching the client process.</param>
        /// <exception cref="InvalidOperationException">Thrown if the client process fails to launch.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ProcessHandle StartClientLauncherOperation(IReadOnlyList<string> argumentList, RunAsActiveUser? runAsActiveUser, ElevatedTokenType? elevatedTokenType)
        {
            return InvokeClientOperationImpl(ClientLauncherDefaultPath, argumentList, runAsActiveUser, elevatedTokenType);
        }

        /// <summary>
        /// Launches the client process with the specified arguments and security context, returning a handle to the
        /// created process.
        /// </summary>
        /// <param name="argumentList">The list of command-line arguments to pass to the client process.</param>
        /// <param name="runAsActiveUser">If specified, determines whether the client process should be launched as the active user.</param>
        /// <param name="handlesToInherit">A collection of native handles to inherit by the client process. May be null if no handles are to be
        /// inherited.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the process launch operation. May be null.</param>
        /// <returns>A handle to the launched client process.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the client process fails to launch.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ProcessHandle StartClientOperation(IReadOnlyList<string> argumentList, RunAsActiveUser? runAsActiveUser = null, IReadOnlyList<nint>? handlesToInherit = null, CancellationToken? cancellationToken = null)
        {
            return InvokeClientOperationImpl(ClientDefaultPath, argumentList, runAsActiveUser, handlesToInherit: handlesToInherit, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Launches the client process with the specified arguments and security context, returning a handle to the
        /// created process.
        /// </summary>
        /// <param name="argumentList">The list of command-line arguments to pass to the client process.</param>
        /// <param name="runAsActiveUser">If specified, determines whether the client process should be launched as the active user.</param>
        /// <param name="handlesToInherit">A collection of native handles to inherit by the client process. May be null if no handles are to be
        /// inherited.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the process launch operation. May be null.</param>
        /// <returns>A handle to the launched client process.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the client process fails to launch.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ProcessHandle StartClientLauncherOperation(IReadOnlyList<string> argumentList, RunAsActiveUser? runAsActiveUser = null, IReadOnlyList<nint>? handlesToInherit = null, CancellationToken? cancellationToken = null)
        {
            return InvokeClientOperationImpl(ClientLauncherDefaultPath, argumentList, runAsActiveUser, handlesToInherit: handlesToInherit, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Launches the client process with the specified arguments and security context, returning a handle to the
        /// created process.
        /// </summary>
        /// <param name="filePath">The file system path to the client executable to launch.</param>
        /// <param name="argumentList">The list of command-line arguments to pass to the client process.</param>
        /// <param name="runAsActiveUser">If specified, determines whether the client process should be launched as the active user.</param>
        /// <param name="elevatedTokenType">Specifies the elevation level to use when launching the client process.</param>
        /// <param name="handlesToInherit">A collection of native handles to inherit by the client process. May be null if no handles are to be
        /// inherited.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the process launch operation. May be null.</param>
        /// <returns>A handle to the launched client process.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the client process fails to launch.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ProcessHandle InvokeClientOperationImpl(FileInfo filePath, IReadOnlyList<string> argumentList, RunAsActiveUser? runAsActiveUser = null, ElevatedTokenType? elevatedTokenType = null, IReadOnlyList<nint>? handlesToInherit = null, CancellationToken? cancellationToken = null)
        {
            return ProcessManager.LaunchAsync(new(
                filePath.FullName,
                argumentList,
                Environment.SystemDirectory,
                runAsActiveUser,
                elevatedTokenType: elevatedTokenType ?? DefaultElevationType,
                denyUserTermination: true,
                runAsInvoker: runAsActiveUser?.Equals(AccountUtilities.CallerRunAsActiveUser) != false && handlesToInherit?.Count > 0,
                uiAccess: true,
                handlesToInherit: handlesToInherit,
                createNoWindow: !filePath.Name.Contains("Launcher"),
                waitForChildProcesses: true,
                windowStyle: ProcessWindowStyle.Hidden,
                cancellationToken: cancellationToken
            )) ?? throw new InvalidOperationException("Failed to launch client operation.");
        }

        /// <summary>
        /// Marks the operation as successful by setting the corresponding registry value to indicate a no-wait state.
        /// </summary>
        /// <remarks>This method updates a specific registry key to signal that a no-wait operation has
        /// completed successfully. It is intended for internal use and should not be called directly by external
        /// code.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void SetOperationSuccessFlag()
        {
            Registry.SetValue(UserRegistryPath, OperationSuccessRegistryProperty, 1, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Gets the default file system path to the ClientServer client executable within the application directory
        /// structure.
        /// </summary>
        /// <remarks>This path is constructed by combining the assembly's directory path with the
        /// executable name "PSADT.ClientServer.Client.exe". It is intended for use when launching or referencing the
        /// ClientServer client from within the application.</remarks>
        internal static readonly FileInfo ClientDefaultPath = new(Path.Combine(AssemblyManager.AssemblyDirectory.FullName, "PSADT.ClientServer.Client.exe"));

        /// <summary>
        /// Gets the file path for the compatible version of the PSADT Client Server executable.
        /// </summary>
        /// <remarks>This path is constructed by combining the base assembly path with the specific
        /// executable name. Ensure that the executable exists at the specified location before attempting to use
        /// it.</remarks>
        internal static readonly FileInfo ClientCompatiblePath = new(Path.Combine(AssemblyManager.AssemblyDirectory.FullName, "PSADT.ClientServer.Client.Compatible.exe"));

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
        internal static readonly FileInfo ClientLauncherDefaultPath = new(Path.Combine(AssemblyManager.AssemblyDirectory.FullName, "PSADT.ClientServer.Client.Launcher.exe"));

        /// <summary>
        /// Gets the file path for the compatible version of the Client Server Client Launcher executable.
        /// </summary>
        /// <remarks>This path is constructed by combining the assembly path with the executable name.
        /// Ensure that the executable is present at the specified location for proper functionality.</remarks>
        internal static readonly FileInfo ClientLauncherCompatiblePath = new(Path.Combine(AssemblyManager.AssemblyDirectory.FullName, "PSADT.ClientServer.Client.Launcher.Compatible.exe"));

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
        public const string OperationSuccessRegistryProperty = "ClientServerOperationSuccess";

        /// <summary>
        /// Specifies the exit code used to indicate a successful shell execute process operation in the client-server communication protocol.
        /// </summary>
        /// <remarks>The value of this constant is derived from `'ShellExecuteProcess'.GetHashCode()` under Windows PowerShell 5.1.</remarks>
        public const int ShellExecuteProcessSuccessCode = -1556154312;

        /// <summary>
        /// Specifies the default elevation type to use when requesting an elevated token.
        /// </summary>
        /// <remarks>This constant is typically used as a default value when an explicit elevation type is
        /// not provided. The value is set to HighestAvailable, which requests the highest available privileges for the
        /// current user context.</remarks>
        internal const ElevatedTokenType DefaultElevationType = ElevatedTokenType.HighestAvailable;
    }
}
