using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;
using PSADT.ProcessManagement;
using Xunit;

namespace PSADT.Tests.ProcessManagement
{
    /// <summary>
    /// Tests matching the machine's running processes against a set of definitions.
    /// </summary>
    /// <remarks>
    /// The test host is the subject rather than a process started for the purpose, so nothing here starts
    /// or stops anything: the host is certain to be running, its name and image path are known, and it is
    /// owned by the caller - which is what the matching needs in order to report an owner at all.
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
    }
}
