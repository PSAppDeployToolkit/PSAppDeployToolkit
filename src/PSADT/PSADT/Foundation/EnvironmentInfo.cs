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
        public static readonly string AssemblyPath = Path.GetDirectoryName(typeof(EnvironmentInfo).Assembly.Location)!;

        /// <summary>
        /// Gets the path to the client server executable, selecting a compatible version if the primary executable is
        /// not Authenticode trusted.
        /// </summary>
        /// <remarks>The path is determined based on the trust status of the primary executable. If the
        /// primary executable is not trusted, the compatible version is used instead.</remarks>
        public static readonly string ClientServerClientPath = !FileSystemUtilities.IsAuthenticodeTrusted(Path.Combine(AssemblyPath, "PSADT.ClientServer.Client.exe"))
            ? Path.Combine(AssemblyPath, "PSADT.ClientServer.Client.Compatible.exe")
            : Path.Combine(AssemblyPath, "PSADT.ClientServer.Client.exe");

        /// <summary>
        /// Gets the path to the client server launcher executable, selecting a compatible version if the primary
        /// executable is not Authenticode trusted.
        /// </summary>
        /// <remarks>This path is determined based on the trust status of the primary launcher executable.
        /// If the primary executable is not trusted, the compatible version will be used instead.</remarks>
        public static readonly string ClientServerClientLauncherPath = Path.Combine(AssemblyPath, "PSADT.ClientServer.Client.Launcher.exe");
    }
}
