using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using PSADT.ProcessManagement;
using PSADT.Tests.TestHelpers;
using Xunit;

namespace PSADT.Tests.ProcessManagement
{
    /// <summary>
    /// Tests reading version information out of a running process.
    /// </summary>
    /// <remarks>
    /// The point of this type is that it reads the version resource out of the process's memory rather
    /// than off the image on disk, which is what lets it describe a process whose file has since been
    /// replaced or deleted. That also makes the framework's own reader an excellent oracle: for a process
    /// whose image is untouched the two arrive at the same answer by entirely different routes, so any
    /// mistake in walking the resource in memory shows up as a disagreement.
    /// <para>
    /// Reading another process's memory needs a privilege that an ordinary account does not hold, so the
    /// reading tests are gated on it. The refusal is gated the other way, and between them one set or the
    /// other runs on any machine.
    /// </para>
    /// <para>
    /// The subject is the test host, which is certain to be running and whose image is known, except where a
    /// 32-bit process is needed: a copy of the 32-bit PING to read, or 32-bit Windows PowerShell to read from.
    /// </para>
    /// </remarks>
    public sealed class ProcessVersionInfoTests
    {
        /// <summary>
        /// Verifies that a caller without the privilege to read process memory is refused, rather than
        /// being handed an empty description it would take for a process with no version resource.
        /// </summary>
        [Fact(Skip = "Requires a caller without the privilege to read process memory.", SkipWhen = nameof(TestEnvironment.HasDebugPrivilege), SkipType = typeof(TestEnvironment))]
        public void GetVersionInfo_RefusesACallerThatCannotReadProcessMemory()
        {
            // Arrange
            using Process current = Process.GetCurrentProcess();

            // Act & Assert
            _ = Assert.Throws<UnauthorizedAccessException>(() => ProcessVersionInfo.GetVersionInfo(current));
            _ = Assert.Throws<UnauthorizedAccessException>(() => ProcessVersionInfo.GetVersionInfo(current.Id));
        }

        /// <summary>
        /// Verifies that the version information read out of the test host's memory is the same as the
        /// version information the framework reads off its image.
        /// </summary>
        [Fact(Skip = SkipReason, SkipUnless = nameof(TestEnvironment.HasDebugPrivilege), SkipType = typeof(TestEnvironment))]
        public void GetVersionInfo_MatchesWhatTheFrameworkReadsFromTheImage()
        {
            // Arrange
            using Process current = Process.GetCurrentProcess();
            string? imagePath = current.MainModule?.FileName;
            Assert.NotNull(imagePath);
            FileVersionInfo expected = FileVersionInfo.GetVersionInfo(imagePath);

            // Act
            ProcessVersionInfo actual = ProcessVersionInfo.GetVersionInfo(current);

            // Assert: the strings. Blank is normalised on both sides because the two readers disagree
            // about how to report a field the resource does not carry: this type reports null, while
            // FileVersionInfo reports an empty string for some builds and null for others.
            static string? Absent(string? value)
            {
                return !string.IsNullOrWhiteSpace(value) ? value : null;
            }
            Assert.Equal(Absent(expected.CompanyName), Absent(actual.CompanyName));
            Assert.Equal(Absent(expected.FileDescription), Absent(actual.FileDescription));
            Assert.Equal(Absent(expected.FileVersion), Absent(actual.FileVersion));
            Assert.Equal(Absent(expected.InternalName), Absent(actual.InternalName));
            Assert.Equal(Absent(expected.LegalCopyright), Absent(actual.LegalCopyright));
            Assert.Equal(Absent(expected.OriginalFilename), Absent(actual.OriginalFilename));
            Assert.Equal(Absent(expected.ProductName), Absent(actual.ProductName));
            Assert.Equal(Absent(expected.ProductVersion), Absent(actual.ProductVersion));

            // Assert: the numbers
            Assert.Equal(expected.FileMajorPart, actual.FileMajorPart);
            Assert.Equal(expected.FileMinorPart, actual.FileMinorPart);
            Assert.Equal(expected.FileBuildPart, actual.FileBuildPart);
            Assert.Equal(expected.FilePrivatePart, actual.FilePrivatePart);
            Assert.Equal(expected.ProductMajorPart, actual.ProductMajorPart);
            Assert.Equal(expected.ProductMinorPart, actual.ProductMinorPart);
            Assert.Equal(expected.ProductBuildPart, actual.ProductBuildPart);
            Assert.Equal(expected.ProductPrivatePart, actual.ProductPrivatePart);

            // Assert: the strings a build only sometimes carries
            Assert.Equal(Absent(expected.Comments), Absent(actual.Comments));
            Assert.Equal(Absent(expected.LegalTrademarks), Absent(actual.LegalTrademarks));
            Assert.Equal(Absent(expected.PrivateBuild), Absent(actual.PrivateBuild));
            Assert.Equal(Absent(expected.SpecialBuild), Absent(actual.SpecialBuild));
            Assert.Equal(Absent(expected.Language), Absent(actual.Language));

            // Assert: the flags
            Assert.Equal(expected.IsDebug, actual.IsDebug);
            Assert.Equal(expected.IsPatched, actual.IsPatched);
            Assert.Equal(expected.IsPrivateBuild, actual.IsPrivateBuild);
            Assert.Equal(expected.IsPreRelease, actual.IsPreRelease);
            Assert.Equal(expected.IsSpecialBuild, actual.IsSpecialBuild);
        }

        /// <summary>
        /// Verifies that the raw versions agree with the four parts they are assembled from, since a
        /// caller comparing versions uses the assembled one and a caller logging them uses the parts.
        /// </summary>
        /// <remarks>
        /// The raw version is the one worth having: the version string in a resource is free text and is
        /// routinely something like "10.0.19041.1 (WinBuild.160101.0800)", which does not parse, whereas
        /// the four numeric parts always do.
        /// </remarks>
        [Fact(Skip = SkipReason, SkipUnless = nameof(TestEnvironment.HasDebugPrivilege), SkipType = typeof(TestEnvironment))]
        public void GetVersionInfo_RawVersionsAgreeWithTheirParts()
        {
            // Arrange
            using Process current = Process.GetCurrentProcess();

            // Act
            ProcessVersionInfo info = ProcessVersionInfo.GetVersionInfo(current);

            // Assert
            Assert.Equal(new Version(info.FileMajorPart, info.FileMinorPart, info.FileBuildPart, info.FilePrivatePart), info.FileVersionRaw);
            Assert.Equal(new Version(info.ProductMajorPart, info.ProductMinorPart, info.ProductBuildPart, info.ProductPrivatePart), info.ProductVersionRaw);
        }

        /// <summary>
        /// Verifies that the image reported is the one the process is running, since everything else is
        /// read relative to it.
        /// </summary>
        [Fact(Skip = SkipReason, SkipUnless = nameof(TestEnvironment.HasDebugPrivilege), SkipType = typeof(TestEnvironment))]
        public void GetVersionInfo_ReportsTheProcessImage()
        {
            // Arrange
            using Process current = Process.GetCurrentProcess();
            string? imagePath = current.MainModule?.FileName;
            Assert.NotNull(imagePath);

            // Act
            ProcessVersionInfo info = ProcessVersionInfo.GetVersionInfo(current);

            // Assert
            Assert.Equal(imagePath, info.FileName.FullName, StringComparer.OrdinalIgnoreCase);
            Assert.True(info.FileName.Exists, $"The reported image {info.FileName.FullName} does not exist.");
        }

        /// <summary>
        /// Verifies that asking by identifier and asking by process describe the same process, since the
        /// two are interchangeable to a caller.
        /// </summary>
        [Fact(Skip = SkipReason, SkipUnless = nameof(TestEnvironment.HasDebugPrivilege), SkipType = typeof(TestEnvironment))]
        public void GetVersionInfo_ByIdentifierMatchesByProcess()
        {
            // Arrange
            using Process current = Process.GetCurrentProcess();

            // Act & Assert
            Assert.Equal(ProcessVersionInfo.GetVersionInfo(current), ProcessVersionInfo.GetVersionInfo(current.Id));
        }

        /// <summary>
        /// Verifies that an image path supplied by the caller is used as given, which is how the running
        /// process enumeration avoids resolving the same path twice.
        /// </summary>
        [Fact(Skip = SkipReason, SkipUnless = nameof(TestEnvironment.HasDebugPrivilege), SkipType = typeof(TestEnvironment))]
        public void GetVersionInfo_UsesAnImagePathItIsGiven()
        {
            // Arrange
            using Process current = Process.GetCurrentProcess();
            string? imagePath = current.MainModule?.FileName;
            Assert.NotNull(imagePath);

            // Act & Assert
            Assert.Equal(ProcessVersionInfo.GetVersionInfo(current), ProcessVersionInfo.GetVersionInfo(current, imagePath));
        }

        /// <summary>
        /// Verifies that a blank image path is refused rather than being taken for "work it out yourself",
        /// which is what a null means to the same parameter.
        /// </summary>
        /// <param name="filePath">The blank path to refuse.</param>
        [Theory(Skip = SkipReason, SkipUnless = nameof(TestEnvironment.HasDebugPrivilege), SkipType = typeof(TestEnvironment))]
        [InlineData("")]
        [InlineData("   ")]
        public void GetVersionInfo_RefusesABlankImagePath(string filePath)
        {
            // Arrange
            using Process current = Process.GetCurrentProcess();

            // Act & Assert
            _ = Assert.Throws<ArgumentException>(() => ProcessVersionInfo.GetVersionInfo(current, filePath));
        }

        /// <summary>
        /// Verifies that a 64-bit caller reads a 32-bit process, whose image is laid out differently from its own.
        /// </summary>
        [Fact(Skip = Wow64ProcessSkipReason, SkipUnless = nameof(TestEnvironment.CanRead32BitProcessMemory), SkipType = typeof(TestEnvironment))]
        public async Task GetVersionInfo_ReadsA32BitProcessFromA64BitCallerAsync()
        {
            // Arrange: a copy, so the framework reads the very image the process runs
            using TempDirectory directory = new();
            FileVersionInfo expected = CopyWow64Ping(directory);
            using RunningPing ping = await RunningPing.StartAsync(expected.FileName).ConfigureAwait(true);

            // Act
            ProcessVersionInfo actual = ProcessVersionInfo.GetVersionInfo(ping.Process);

            // Assert
            Assert.Equal(expected.FileDescription, actual.FileDescription);
            Assert.Equal(expected.FileVersion, actual.FileVersion);
            Assert.Equal(new Version(expected.FileMajorPart, expected.FileMinorPart, expected.FileBuildPart, expected.FilePrivatePart), actual.FileVersionRaw);
        }

        /// <summary>
        /// Verifies that a 32-bit caller is refused a native 64-bit process before it tries to enumerate the
        /// process's modules, which WOW64 cannot do.
        /// </summary>
        [Fact(Skip = Wow64SkipReason, SkipUnless = nameof(TestEnvironment.CanReadProcessMemoryUnderWow64), SkipType = typeof(TestEnvironment))]
        public async Task GetVersionInfo_RefusesANative64BitProcessFromA32BitCallerAsync()
        {
            // Arrange
            using Process current = Process.GetCurrentProcess();

            // Act
            Wow64PowerShellResult result = await Wow64PowerShell.InvokeAsync($"[PSADT.ProcessManagement.ProcessVersionInfo]::GetVersionInfo([int]{current.Id.ToString(CultureInfo.InvariantCulture)}).FileDescription").ConfigureAwait(true);

            // Assert
            Assert.True(result.ExceptionType is not null, result.Describe());
            Assert.Equal(typeof(NotSupportedException).FullName, result.ExceptionType);
        }

        /// <summary>
        /// Verifies that a 32-bit caller still reads a 32-bit process, so the refusal is confined to the
        /// architecture WOW64 cannot read.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "MA0136:Raw String contains an implicit end of line character", Justification = "The literal is PowerShell source, which parses either line ending, so the source file's choice cannot change what this does.")]
        [Fact(Skip = Wow64SkipReason, SkipUnless = nameof(TestEnvironment.CanReadProcessMemoryUnderWow64), SkipType = typeof(TestEnvironment))]
        public async Task GetVersionInfo_ReadsA32BitProcessFromA32BitCallerAsync()
        {
            // Arrange: a copy, so the framework reads the very image the process runs
            using TempDirectory directory = new();
            FileVersionInfo expected = CopyWow64Ping(directory);
            using RunningPing ping = await RunningPing.StartAsync(expected.FileName).ConfigureAwait(true);

            // Act
            Wow64PowerShellResult result = await Wow64PowerShell.InvokeAsync($$"""
                $info = [PSADT.ProcessManagement.ProcessVersionInfo]::GetVersionInfo([int]{{ping.Process.Id.ToString(CultureInfo.InvariantCulture)}})
                "$($info.FileDescription)|$($info.FileVersionRaw)"
                """).ConfigureAwait(true);

            // Assert
            Assert.True(result.Value is not null, result.Describe());
            Assert.Equal($"{expected.FileDescription}|{new Version(expected.FileMajorPart, expected.FileMinorPart, expected.FileBuildPart, expected.FilePrivatePart)}", result.Value);
        }

        /// <summary>
        /// Verifies that the description put in a log names the image and its version, since that is what
        /// it is read for.
        /// </summary>
        [Fact(Skip = SkipReason, SkipUnless = nameof(TestEnvironment.HasDebugPrivilege), SkipType = typeof(TestEnvironment))]
        public void ToString_NamesTheImageAndItsVersion()
        {
            // Arrange
            using Process current = Process.GetCurrentProcess();

            // Act
            string described = ProcessVersionInfo.GetVersionInfo(current).ToString();

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(described));
            Assert.Contains(current.ProcessName, described, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Verifies that a null process is refused.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0191:Do not use the null-forgiving operator", Justification = "This is deliberate as part of unit testing.")]
        [Fact]
        public void GetVersionInfo_RefusesANullProcess()
        {
            _ = Assert.Throws<ArgumentNullException>(static () => ProcessVersionInfo.GetVersionInfo(null!));
        }

        /// <summary>
        /// Verifies that an identifier no process is using is reported rather than described.
        /// </summary>
        [Fact]
        public void GetVersionInfo_RefusesAnIdentifierThatIsNotAProcess()
        {
            _ = Assert.Throws<ArgumentException>(static () => ProcessVersionInfo.GetVersionInfo(int.MaxValue));
        }

        /// <summary>
        /// Copies the 32-bit PING into the given directory, reading its version before the native MUI file it needs is put beside it.
        /// </summary>
        /// <param name="directory">The directory to copy it into.</param>
        /// <returns>The copy's own version information, which names its path.</returns>
        private static FileVersionInfo CopyWow64Ping(TempDirectory directory)
        {
            string image = directory.GetPath(TestEnvironment.Wow64PingExecutable.Name);
            File.Copy(TestEnvironment.Wow64PingExecutable.FullName, image);
            FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(image);
            RunningPing.CopyMessageResources(TestEnvironment.Wow64PingExecutable.FullName, image);
            return versionInfo;
        }

        /// <summary>
        /// The reason the reading tests are gated, spelled once.
        /// </summary>
        private const string SkipReason = "Requires the privilege to read another process's memory.";

        /// <summary>
        /// The reason the tests reading from 32-bit Windows PowerShell are gated, spelled once.
        /// </summary>
        private const string Wow64SkipReason = "Requires the net472 test host on 64-bit Windows, and the privilege to read another process's memory.";

        /// <summary>
        /// The reason the test reading a 32-bit process is gated, spelled once.
        /// </summary>
        private const string Wow64ProcessSkipReason = "Requires a 64-bit test host with WOW64, and the privilege to read another process's memory.";
    }
}
