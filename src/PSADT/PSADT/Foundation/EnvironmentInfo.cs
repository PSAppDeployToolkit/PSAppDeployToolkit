using System;
using System.IO;
using PSADT.FileSystem;

namespace PSADT.Foundation
{
    /// <summary>
    /// Provides information about the current application environment, including paths to key executables and
    /// resources.
    /// </summary>
    /// <remarks>The EnvironmentInfo class exposes static properties that return paths based on the assembly's
    /// location and the trust status of executables. These properties assist in locating resources and selecting
    /// appropriate executables at runtime, which can vary depending on deployment or security requirements.</remarks>
    public static class EnvironmentInfo
    {
        /// <summary>
        /// Gets the directory path of the assembly that contains the EnvironmentInfo type.
        /// </summary>
        /// <remarks>This field can be used to locate files or resources relative to the assembly's
        /// location at runtime. The returned path is determined by the assembly's current location, which may vary
        /// depending on how the application is deployed or executed.</remarks>
        public static readonly string AssemblyPath = Path.GetDirectoryName(typeof(EnvironmentInfo).Assembly.Location) ?? throw new InvalidOperationException("Failed to retrieve directory for this assembly.");

        /// <summary>
        /// Gets the default file system path to the ClientServer client executable within the application directory
        /// structure.
        /// </summary>
        /// <remarks>This path is constructed by combining the assembly's directory path with the
        /// executable name "PSADT.ClientServer.Client.exe". It is intended for use when launching or referencing the
        /// ClientServer client from within the application.</remarks>
        internal static readonly string ClientServerClientDefaultPath = Path.Combine(AssemblyPath, "PSADT.ClientServer.Client.exe");

        /// <summary>
        /// Gets the file path for the compatible version of the PSADT Client Server executable.
        /// </summary>
        /// <remarks>This path is constructed by combining the base assembly path with the specific
        /// executable name. Ensure that the executable exists at the specified location before attempting to use
        /// it.</remarks>
        internal static readonly string ClientServerClientCompatiblePath = Path.Combine(AssemblyPath, "PSADT.ClientServer.Client.Compatible.exe");

        /// <summary>
        /// Gets the path to the client server executable, selecting a compatible version if the primary executable is
        /// not Authenticode trusted.
        /// </summary>
        /// <remarks>The path is determined based on the trust status of the primary executable. If the
        /// primary executable is not trusted, the compatible version is used instead.</remarks>
        public static readonly string ClientServerClientPath = !FileSystemUtilities.IsAuthenticodeTrusted(Path.Combine(AssemblyPath, "PSADT.ClientServer.Client.exe"))
            ? ClientServerClientCompatiblePath
            : ClientServerClientDefaultPath;

        /// <summary>
        /// Gets the default file system path for the Client Server Client Launcher executable.
        /// </summary>
        /// <remarks>The path is constructed by combining the assembly directory with the executable name.
        /// Use this value to locate the launcher for the Client Server Client application when performing operations
        /// that require its presence.</remarks>
        internal static readonly string ClientServerClientLauncherDefaultPath = Path.Combine(AssemblyPath, "PSADT.ClientServer.Client.Launcher.exe");

        /// <summary>
        /// Gets the file path for the compatible version of the Client Server Client Launcher executable.
        /// </summary>
        /// <remarks>This path is constructed by combining the assembly path with the executable name.
        /// Ensure that the executable is present at the specified location for proper functionality.</remarks>
        internal static readonly string ClientServerClientLauncherCompatiblePath = Path.Combine(AssemblyPath, "PSADT.ClientServer.Client.Launcher.Compatible.exe");

        /// <summary>
        /// Gets the path to the client server launcher executable, selecting a compatible version if the primary
        /// executable is not Authenticode trusted.
        /// </summary>
        /// <remarks>This path is determined based on the trust status of the primary launcher executable.
        /// If the primary executable is not trusted, the compatible version will be used instead.</remarks>
        public static readonly string ClientServerClientLauncherPath = !FileSystemUtilities.IsAuthenticodeTrusted(Path.Combine(AssemblyPath, "PSADT.ClientServer.Client.Launcher.exe"))
            ? ClientServerClientLauncherCompatiblePath
            : ClientServerClientLauncherDefaultPath;
    }
}
