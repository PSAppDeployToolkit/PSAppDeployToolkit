using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Principal;

namespace PSADT.Tests.TestHelpers
{
    /// <summary>
    /// Facts about the machine a test run landed on, resolved once and shared by every test class.
    /// </summary>
    /// <remarks>
    /// Each member here answers a question a test needs before it can decide whether it is able to run
    /// at all. They are exposed as static properties so a test can name one in <c>SkipUnless</c> with
    /// <c>SkipType</c> pointing at this class, which keeps a single copy of the probe rather than one
    /// per test class.
    /// <para>
    /// Declaration order matters, and not only for the members that read each other directly. Static
    /// initialisers run in textual order, so anything a probe reads - including a table it reaches through
    /// a helper method - has to be declared above the member whose initialiser calls it, or the probe runs
    /// against a null and the whole class fails to initialise.
    /// </para>
    /// </remarks>
    public static class TestEnvironment
    {
        /// <summary>
        /// The directory holding the machine's installed fonts.
        /// </summary>
        private static string FontsDirectory { get; } = Path.Join(
            Environment.GetFolderPath(Environment.SpecialFolder.Windows),
            "Fonts");

        /// <summary>
        /// The 64-bit program files directory, which is where the signed-executable candidates live.
        /// </summary>
        private static string ProgramFilesDirectory { get; } = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

        /// <summary>
        /// The Windows Installer package cache.
        /// </summary>
        private static string InstallerCacheDirectory { get; } = Path.Join(
            Environment.GetFolderPath(Environment.SpecialFolder.Windows),
            "Installer");

        /// <summary>
        /// The executables <c>ClientServerUtilities</c> expects to find beside the assembly.
        /// </summary>
        private static readonly string[] ClientServerExecutableNames =
        [
            "PSADT.ClientServer.Client.exe",
            "PSADT.ClientServer.Client.Compatible.exe",
            "PSADT.ClientServer.Client.Launcher.exe",
            "PSADT.ClientServer.Client.Launcher.Compatible.exe",
        ];

        /// <summary>
        /// Whether the caller is running elevated, which gates the tests that cannot succeed otherwise.
        /// </summary>
        public static bool IsElevated { get; } = GetIsElevated();

        /// <summary>
        /// Whether the client/server executables are present where <c>ClientServerUtilities</c> looks
        /// for them, which decides whether that type can be touched at all.
        /// </summary>
        /// <remarks>
        /// Its static constructor asks whether each executable is Authenticode trusted, and that check
        /// throws rather than returning false for a file that does not exist. So every type reading
        /// <c>ClientServerUtilities</c> - <c>ProcessManager</c> through <c>ProcessHandle</c>,
        /// <c>TokenManager</c>, <c>ClientServerPermissions</c> - fails with a
        /// <see cref="TypeInitializationException"/> unless they were copied alongside. The project
        /// builds and copies them, but a build that addresses an inner target framework directly skips
        /// that step, so the tests confirm it rather than assume it.
        /// </remarks>
        public static bool ClientServerExecutablesPresent { get; } = GetClientServerExecutablesPresent();

        /// <summary>
        /// A font file to read a title out of, or <see langword="null"/> when the machine has none of
        /// the ones we recognise.
        /// </summary>
        /// <remarks>
        /// The candidate is restricted to a font that ships with every Windows installation, so the
        /// expected title is known ahead of time rather than being whatever the font happens to say.
        /// </remarks>
        public static FileInfo? ArialFont { get; } = FindFirstExistingFile(Path.Join(FontsDirectory, "arial.ttf"));

        /// <summary>
        /// A font collection file, used to cover the multiple-face branch of the name table walk, or
        /// <see langword="null"/> when the machine has none.
        /// </summary>
        public static FileInfo? FontCollection { get; } = FindFirstExistingFile(
            Path.Join(FontsDirectory, "cambria.ttc"),
            Path.Join(FontsDirectory, "batang.ttc"),
            Path.Join(FontsDirectory, "mingliu.ttc"));

        /// <summary>
        /// An executable carrying an embedded Authenticode signature, or <see langword="null"/> when
        /// none of the candidates are present.
        /// </summary>
        /// <remarks>
        /// Catalog-signed binaries are deliberately excluded. The trust check under test asks
        /// <c>WinVerifyTrust</c> for a file-based verification with URL retrieval limited to the cache,
        /// which does not consult the system catalogues, so an operating system binary such as
        /// <c>notepad.exe</c> reports as untrusted despite being signed. Only files carrying the
        /// signature in the image itself are a valid positive fixture.
        /// </remarks>
        public static FileInfo? EmbeddedSignedExecutable { get; } = FindFirstExistingFile(
            Path.Join(ProgramFilesDirectory, "PowerShell", "7", "pwsh.exe"),
            Path.Join(ProgramFilesDirectory, "dotnet", "dotnet.exe"),
            Path.Join(ProgramFilesDirectory, "Git", "cmd", "git.exe"));

        /// <summary>
        /// Whether a binary with an embedded signature was found, which gates the tests needing one.
        /// </summary>
        public static bool HasEmbeddedSignedExecutable => EmbeddedSignedExecutable is not null;

        /// <summary>
        /// Whether a font to read a title from was found, which gates the tests needing one.
        /// </summary>
        public static bool HasArialFont => ArialFont is not null;

        /// <summary>
        /// Whether a font collection was found, which gates the tests needing one.
        /// </summary>
        public static bool HasFontCollection => FontCollection is not null;

        /// <summary>
        /// Whether a cached installer was found, which gates the tests needing a real database.
        /// </summary>
        public static bool HasCachedMsiPackage => CachedMsiPackage is not null;

        /// <summary>
        /// Whether a cached patch was found, which gates the tests needing one.
        /// </summary>
        public static bool HasCachedMspPackage => CachedMspPackage is not null;

        /// <summary>
        /// An installer cached by Windows Installer, or <see langword="null"/> when the store is empty
        /// or unreadable.
        /// </summary>
        /// <remarks>
        /// The cache under <c>%SystemRoot%\Installer</c> holds a copy of every package installed
        /// through Windows Installer, which makes it a source of real databases to read without
        /// shipping one. Enumeration is best-effort: an unreadable store simply produces no fixture,
        /// and the tests that need one skip.
        /// </remarks>
        public static FileInfo? CachedMsiPackage { get; } = FindFirstReadableCachedPackage("*.msi");

        /// <summary>
        /// A patch cached by Windows Installer, or <see langword="null"/> when there is none.
        /// </summary>
        public static FileInfo? CachedMspPackage { get; } = FindFirstReadableCachedPackage("*.msp");

        /// <summary>
        /// Determines whether the caller is running with administrative rights.
        /// </summary>
        /// <returns><see langword="true"/> if the caller is elevated; otherwise, <see langword="false"/>.</returns>
        private static bool GetIsElevated()
        {
            using WindowsIdentity identity = WindowsIdentity.GetCurrent();
            return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
        }

        /// <summary>
        /// Determines whether every client/server executable is present beside the loaded assembly.
        /// </summary>
        /// <remarks>
        /// The paths are derived the same way <c>ClientServerUtilities</c> derives them, rather than by
        /// reading that type, because reading it is the thing this method exists to make safe.
        /// </remarks>
        /// <returns><see langword="true"/> if all four are present; otherwise, <see langword="false"/>.</returns>
        private static bool GetClientServerExecutablesPresent()
        {
            if (Path.GetDirectoryName(typeof(TestEnvironment).Assembly.Location) is not string assemblyDirectory)
            {
                return false;
            }
            string clientServerDirectory = assemblyDirectory.EndsWith("net472", StringComparison.Ordinal)
                ? assemblyDirectory
                : Path.Join(Directory.GetParent(assemblyDirectory)?.FullName, "net472");
            foreach (string name in ClientServerExecutableNames)
            {
                if (!File.Exists(Path.Join(clientServerDirectory, name)))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Returns the first of the given paths that exists.
        /// </summary>
        /// <param name="paths">The candidate paths, in preference order.</param>
        /// <returns>The first path that exists, or <see langword="null"/> if none do.</returns>
        private static FileInfo? FindFirstExistingFile(params string[] paths)
        {
            foreach (string path in paths)
            {
                FileInfo file = new(path);
                if (file.Exists)
                {
                    return file;
                }
            }
            return null;
        }

        /// <summary>
        /// Returns the first package matching the given pattern that the caller can open for reading.
        /// </summary>
        /// <param name="pattern">The search pattern to match, such as <c>*.msi</c>.</param>
        /// <returns>The first readable package, or <see langword="null"/> if there is none.</returns>
        private static FileInfo? FindFirstReadableCachedPackage(string pattern)
        {
            DirectoryInfo installerCache = new(InstallerCacheDirectory);
            if (!installerCache.Exists)
            {
                return null;
            }

            // Enumeration itself can be refused, and individual entries in the cache can carry
            // permissions that exclude an unelevated caller, so both are treated as "no fixture".
            IEnumerable<FileInfo> packages;
            try
            {
                packages = installerCache.EnumerateFiles(pattern, SearchOption.TopDirectoryOnly);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return null;
            }
            foreach (FileInfo package in packages)
            {
                if (CanOpenForReading(package))
                {
                    return package;
                }
            }
            return null;
        }

        /// <summary>
        /// Determines whether the given file can be opened for reading by this caller.
        /// </summary>
        /// <param name="file">The file to try.</param>
        /// <returns><see langword="true"/> if it opened; otherwise, <see langword="false"/>.</returns>
        private static bool CanOpenForReading(FileInfo file)
        {
            try
            {
                using FileStream stream = file.OpenRead();
                return true;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return false;
            }
        }
    }
}
