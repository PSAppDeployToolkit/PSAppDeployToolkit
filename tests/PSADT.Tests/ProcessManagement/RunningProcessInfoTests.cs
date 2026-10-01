using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using PSADT.FileSystem;
using PSADT.Interop;
using PSADT.ProcessManagement;
using PSADT.Tests.TestHelpers;
using Windows.Win32.Storage.FileSystem;
using Xunit;

namespace PSADT.Tests.ProcessManagement
{
    /// <summary>
    /// Tests matching the machine's running processes against a set of definitions.
    /// </summary>
    /// <remarks>
    /// The test host is the subject wherever it will do: it is certain to be running, its name and image path
    /// are known, and it is owned by the caller - which is what the matching needs in order to report an owner
    /// at all. The exceptions start a copy of PING and rename its image, since only a process whose file has
    /// gone shows where its description was read from.
    /// </remarks>
    public sealed class RunningProcessInfoTests
    {
        /// <summary>
        /// Verifies that the test host is found by its bare process name.
        /// </summary>
        [Fact]
        public void Get_FindsTheTestHostByName()
        {
            // Arrange
            using Process current = Process.GetCurrentProcess();

            // Act
            IReadOnlyList<RunningProcessInfo> running = RunningProcessInfo.Get([new(current.ProcessName)]);

            // Assert
            Assert.Contains(running, info => info.Process.Id == current.Id);
        }

        /// <summary>
        /// Verifies that the test host is found by its fully qualified image path, which is the branch
        /// that compares the resolved path rather than only the name.
        /// </summary>
        [Fact]
        public void Get_FindsTheTestHostByFullPath()
        {
            // Arrange
            using Process current = Process.GetCurrentProcess();
            string? imagePath = current.MainModule?.FileName;
            Assert.NotNull(imagePath);

            // Act
            IReadOnlyList<RunningProcessInfo> running = RunningProcessInfo.Get([new(imagePath)]);

            // Assert
            Assert.Contains(running, info => info.Process.Id == current.Id);
        }

        /// <summary>
        /// Verifies that a path that is not the test host's does not match it, so the path comparison is
        /// doing work rather than the name alone deciding.
        /// </summary>
        [Fact]
        public void Get_DoesNotMatchADifferentPathWithTheSameName()
        {
            // Arrange
            using Process current = Process.GetCurrentProcess();

            // Act: the host's own file name, but somewhere it certainly is not
            IReadOnlyList<RunningProcessInfo> running = RunningProcessInfo.Get([new($@"C:\PSADTNoSuchDirectory\{current.ProcessName}.exe")]);

            // Assert
            Assert.DoesNotContain(running, info => info.Process.Id == current.Id);
        }

        /// <summary>
        /// Verifies that a wildcard name matches, which is the branch that compiles the definition into a
        /// pattern instead of comparing it directly.
        /// </summary>
        [Fact]
        public void Get_FindsTheTestHostByWildcard()
        {
            // Arrange
            using Process current = Process.GetCurrentProcess();

            // Act: the leading portion of the host's own name, wildcarded
            IReadOnlyList<RunningProcessInfo> running = RunningProcessInfo.Get([new($"{current.ProcessName[..3]}*")]);

            // Assert
            Assert.Contains(running, info => info.Process.Id == current.Id);
        }

        /// <summary>
        /// Verifies that a definition nothing can match reports nothing rather than failing.
        /// </summary>
        [Fact]
        public void Get_ReportsNothingForADefinitionThatCannotMatch()
        {
            Assert.Empty(RunningProcessInfo.Get([new("PSADTNoSuchProcessNameForTesting")]));
        }

        /// <summary>
        /// Verifies that a matched process is fully described: a description, an image path that exists,
        /// and the account it belongs to.
        /// </summary>
        [Fact]
        public void Get_DescribesEveryProcessItMatches()
        {
            // Arrange
            using Process current = Process.GetCurrentProcess();
            using WindowsIdentity identity = WindowsIdentity.GetCurrent();

            // Act
            RunningProcessInfo? host = RunningProcessInfo.Get([new(current.ProcessName)]).FirstOrDefault(info => info.Process.Id == current.Id);

            // Assert
            Assert.NotNull(host);
            Assert.False(string.IsNullOrWhiteSpace(host.Description));
            Assert.True(host.FileName.Exists, $"The reported image {host.FileName.FullName} does not exist.");
            Assert.Equal(identity.User, host.SID);
        }

        /// <summary>
        /// Verifies that the arguments reported are the process's own, with the image path stripped off
        /// the front, since a caller showing them to a user does not want the path repeated.
        /// </summary>
        [Fact]
        public void Get_ReportsTheArgumentsWithoutTheImagePath()
        {
            // Arrange
            using Process current = Process.GetCurrentProcess();

            // Act
            RunningProcessInfo? host = RunningProcessInfo.Get([new(current.ProcessName)]).FirstOrDefault(info => info.Process.Id == current.Id);

            // Assert
            Assert.NotNull(host);
            Assert.DoesNotContain(host.ArgumentList, argument => argument.Equals(host.FileName.FullName, StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(host.ArgumentList, static argument => string.IsNullOrWhiteSpace(argument));
        }

        /// <summary>
        /// Verifies that a description supplied on the definition is preferred over anything read from the
        /// image, since a caller naming a process for a user to see wants its own wording used.
        /// </summary>
        [Fact]
        public void Get_PrefersTheDescriptionOnTheDefinition()
        {
            // Arrange
            using Process current = Process.GetCurrentProcess();

            // Act
            RunningProcessInfo? host = RunningProcessInfo.Get([new(current.ProcessName, "A supplied description")]).FirstOrDefault(info => info.Process.Id == current.Id);

            // Assert
            Assert.NotNull(host);
            Assert.Equal("A supplied description", host.Description);
        }

        /// <summary>
        /// Verifies that a process whose image has been renamed since it started is described from its memory,
        /// the one place its version resource is left.
        /// </summary>
        [Fact(Skip = DebugPrivilegeSkipReason, SkipUnless = nameof(TestEnvironment.HasDebugPrivilege), SkipType = typeof(TestEnvironment))]
        public async Task Get_DescribesAProcessWhoseImageWasRenamedFromItsMemoryAsync()
        {
            // Arrange
            using TempDirectory directory = new();
            string name = NewProcessName();
            using RunningPing ping = await StartRenamedPingAsync(directory, name).ConfigureAwait(true);
            string? expected = FileVersionInfo.GetVersionInfo(directory.GetPath($"{name}.renamed")).FileDescription;
            Assert.False(string.IsNullOrWhiteSpace(expected), "PING carries no description to read back.");

            // Act
            RunningProcessInfo info = Assert.Single(RunningProcessInfo.Get([new(name)]));

            // Assert: nothing is left at the reported path, so the description came out of the process
            Assert.False(info.FileName.Exists, $"The reported image {info.FileName.FullName} still exists.");
            Assert.Equal(expected, info.Description);
        }

        /// <summary>
        /// Verifies that a 32-bit caller describes a native process whose image has been renamed by its name,
        /// since neither the file nor the process's memory can be read, rather than failing to describe it.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "MA0136:Raw String contains an implicit end of line character", Justification = "The literal is PowerShell source, which parses either line ending, so the source file's choice cannot change what this does.")]
        [Fact(Skip = Wow64SkipReason, SkipUnless = nameof(TestEnvironment.CanReadProcessMemoryUnderWow64), SkipType = typeof(TestEnvironment))]
        public async Task Get_DescribesARenamedNativeProcessByItsNameFromA32BitCallerAsync()
        {
            // Arrange
            using TempDirectory directory = new();
            string name = NewProcessName();
            using RunningPing ping = await StartRenamedPingAsync(directory, name).ConfigureAwait(true);

            // Act
            Wow64PowerShellResult result = await Wow64PowerShell.InvokeAsync($$"""
                $running = [PSADT.ProcessManagement.RunningProcessInfo]::Get([PSADT.ProcessManagement.ProcessDefinition[]]@([PSADT.ProcessManagement.ProcessDefinition]::new('{{name}}')))
                "$($running.Count)|$($running[0].Description)"
                """).ConfigureAwait(true);

            // Assert
            Assert.True(result.Value is not null, result.Describe());
            Assert.Equal($"1|{name}", result.Value);
        }

        /// <summary>
        /// Verifies that results are ordered by description, so a list shown to a user is stable rather
        /// than following whatever order the machine enumerated processes in.
        /// </summary>
        /// <remarks>
        /// The wildcard also exercises the path that swallows failures: a definition containing one will
        /// match processes this caller cannot open, and those have to be passed over rather than ending
        /// the enumeration.
        /// </remarks>
        [Fact]
        public void Get_OrdersResultsByDescription()
        {
            // Act: a pattern broad enough that most of the machine's processes match
            IReadOnlyList<RunningProcessInfo> running = RunningProcessInfo.Get([new("*s*")]);

            // Assert: each description is at or after the one before it
            Assert.All(
                running.Skip(1).Select((info, index) => (Previous: running[index].Description, Current: info.Description)),
                static pair => Assert.True(
                    StringComparer.OrdinalIgnoreCase.Compare(pair.Previous, pair.Current) <= 0,
                    $"'{pair.Current}' was reported after '{pair.Previous}'."));
        }

        /// <summary>
        /// Verifies that an empty definition list is refused, since a caller passing one has lost its
        /// contents rather than meaning "match everything".
        /// </summary>
        [Fact]
        public void Get_RefusesAnEmptyDefinitionList()
        {
            _ = Assert.Throws<ArgumentOutOfRangeException>(static () => RunningProcessInfo.Get([]));
        }

        /// <summary>
        /// Verifies that a null definition list is refused.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0191:Do not use the null-forgiving operator", Justification = "This is deliberate as part of unit testing.")]
        [Fact]
        public void Get_RefusesANullDefinitionList()
        {
            _ = Assert.Throws<ArgumentNullException>(static () => RunningProcessInfo.Get(null!));
        }

        /// <summary>
        /// Verifies that two descriptions of the same running process are equal, which is what the type
        /// being a record promises anything comparing one reading against the next.
        /// </summary>
        /// <remarks>
        /// The two are taken by separate calls, so nothing is shared between them: each holds its own
        /// <see cref="Process"/>, its own path and its own argument list. That is the case worth pinning.
        /// A <see cref="Process"/> is a live handle rather than a value - each one is opened separately
        /// and holds a different handle value - and a collection compares by reference, so a record
        /// holding either directly never equalled another describing the same process.
        /// </remarks>
        [Fact]
        public void Equality_IsByValue()
        {
            // Arrange
            using Process current = Process.GetCurrentProcess();

            // Act: two separate readings of the same process
            RunningProcessInfo? first = RunningProcessInfo.Get([new(current.ProcessName)]).FirstOrDefault(info => info.Process.Id == current.Id);
            RunningProcessInfo? second = RunningProcessInfo.Get([new(current.ProcessName)]).FirstOrDefault(info => info.Process.Id == current.Id);
            Assert.NotNull(first);
            Assert.NotNull(second);

            // Assert: nothing is shared between them, and they are equal all the same
            Assert.NotSame(first.Process, second.Process);
            Assert.Equal(first, second);
            Assert.Equal(first.GetHashCode(), second.GetHashCode());
        }

        /// <summary>
        /// Verifies that descriptions of two different processes are not equal, so the comparison is
        /// doing work rather than matching everything.
        /// </summary>
        [Fact]
        public void Equality_DistinguishesDifferentProcesses()
        {
            // Arrange: every process on the machine that this caller can describe
            using Process current = Process.GetCurrentProcess();
            IReadOnlyList<RunningProcessInfo> running = RunningProcessInfo.Get([new("*s*")]);
            RunningProcessInfo? other = running.FirstOrDefault(info => info.Process.Id != current.Id);
            if (other is null)
            {
                // Nothing else on the machine was describable, so there is nothing to distinguish from.
                return;
            }

            // Act
            RunningProcessInfo? host = RunningProcessInfo.Get([new(current.ProcessName)]).FirstOrDefault(info => info.Process.Id == current.Id);

            // Assert
            Assert.NotNull(host);
            Assert.NotEqual(host, other);
        }

        /// <summary>
        /// Starts a copy of the native PING under the given name, then renames its image so nothing is left at the
        /// path it was started from.
        /// </summary>
        /// <param name="directory">The directory to put the copy in.</param>
        /// <param name="name">The name to start it under, which becomes its process name.</param>
        /// <returns>The running copy.</returns>
        private static async Task<RunningPing> StartRenamedPingAsync(TempDirectory directory, string name)
        {
            string image = directory.GetPath($"{name}.exe");
            string source = Path.Join(Environment.SystemDirectory, "PING.EXE");
            File.Copy(source, image); RunningPing.CopyMessageResources(source, image);

            // Opened before the launch: a new image's first run can be held briefly without sharing deletion.
            using SafeFileHandle imageHandle = NativeMethods.CreateFile(image, FileSystemRights.Delete, FILE_SHARE_MODE.FILE_SHARE_READ | FILE_SHARE_MODE.FILE_SHARE_WRITE | FILE_SHARE_MODE.FILE_SHARE_DELETE, lpSecurityAttributes: null, FILE_CREATION_DISPOSITION.OPEN_EXISTING, FileAttributes.Normal);
            RunningPing ping = await RunningPing.StartAsync(image).ConfigureAwait(true);
            try
            {
                FileSystemUtilities.RenameFile(imageHandle, directory.GetPath($"{name}.renamed"));
                return ping;
            }
            catch
            {
                ping.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Makes a process name no other process on the machine has.
        /// </summary>
        /// <returns>The name.</returns>
        private static string NewProcessName()
        {
            return $"PSADT{Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture)}";
        }

        /// <summary>
        /// The reason the test reading process memory is gated, spelled once.
        /// </summary>
        private const string DebugPrivilegeSkipReason = "Requires the privilege to read another process's memory.";

        /// <summary>
        /// The reason the test describing from 32-bit Windows PowerShell is gated, spelled once.
        /// </summary>
        private const string Wow64SkipReason = "Requires the net472 test host on 64-bit Windows, and the privilege to read another process's memory.";
    }
}
