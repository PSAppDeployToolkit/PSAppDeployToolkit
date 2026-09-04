using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using PSADT.ProcessManagement;
using Xunit;

namespace PSADT.Tests.ProcessManagement
{
    /// <summary>
    /// Tests the options carried across to a shell execute invocation.
    /// </summary>
    /// <remarks>
    /// Everything here is derived in the constructor, which makes the type worth testing despite looking
    /// like a data holder: the file path is unquoted, the argument list is flattened into a single
    /// string, and a command line is composed from both. The flattening is deliberately asymmetric and
    /// is pinned below.
    /// </remarks>
    public sealed class UserShellExecuteOptionsTests
    {
        /// <summary>
        /// Verifies that a path the caller has already quoted is unquoted, since the command line adds
        /// its own quoting and would otherwise double it.
        /// </summary>
        /// <param name="filePath">The path as supplied.</param>
        /// <param name="expected">The path as stored.</param>
        [Theory]
        [InlineData(@"C:\Windows\notepad.exe", @"C:\Windows\notepad.exe")]
        [InlineData(@"""C:\Windows\notepad.exe""", @"C:\Windows\notepad.exe")]
        [InlineData(@"""C:\Program Files\App\app.exe""", @"C:\Program Files\App\app.exe")]
        [InlineData(@"""""C:\Windows\notepad.exe""""", @"C:\Windows\notepad.exe")]
        [InlineData(@"""C:\Windows\notepad.exe", @"C:\Windows\notepad.exe")]
        [InlineData(@"C:\Windows\notepad.exe""", @"C:\Windows\notepad.exe")]
        public void Constructor_UnquotesTheFilePath(string filePath, string expected)
        {
            Assert.Equal(expected, new UserShellExecuteOptions(filePath).FilePath);
        }

        /// <summary>
        /// Verifies that a blank file path is rejected.
        /// </summary>
        /// <param name="filePath">The blank path to reject.</param>
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_RejectsABlankFilePath(string filePath)
        {
            _ = Assert.Throws<ArgumentException>(() => new UserShellExecuteOptions(filePath));
        }

        /// <summary>
        /// Verifies that a null file path is rejected as absent.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0191:Do not use the null-forgiving operator", Justification = "This is deliberate as part of unit testing.")]
        [Fact]
        public void Constructor_RejectsANullFilePath()
        {
            _ = Assert.Throws<ArgumentNullException>(static () => new UserShellExecuteOptions(null!));
        }

        /// <summary>
        /// Verifies that no arguments produces no argument string, rather than an empty one that would
        /// add a trailing space to the command line.
        /// </summary>
        [Fact]
        public void Constructor_LeavesArgumentsUnsetWhenThereAreNone()
        {
            // Act
            UserShellExecuteOptions withNull = new(@"C:\app.exe", argumentList: null);
            UserShellExecuteOptions withEmpty = new(@"C:\app.exe", []);

            // Assert
            Assert.Null(withNull.Arguments);
            Assert.Empty(withNull.ArgumentList);
            Assert.Null(withEmpty.Arguments);
            Assert.Empty(withEmpty.ArgumentList);
        }

        /// <summary>
        /// Verifies that two or more arguments are joined with the escaping rules the command line
        /// parser expects, so a value containing a space or a quote survives.
        /// </summary>
        /// <param name="arguments">The arguments to supply.</param>
        [Theory]
        [MemberData(nameof(MultipleArgumentCases))]
        public void Constructor_EscapesTwoOrMoreArguments(string[] arguments)
        {
            // Act
            UserShellExecuteOptions options = new(@"C:\app.exe", arguments);

            // Assert: the composed string parses back into the arguments it was built from
            Assert.NotNull(options.Arguments);
            Assert.Equal(arguments, CommandLineUtilities.CommandLineToArgumentList(options.Arguments));
        }

        /// <summary>
        /// Verifies that a single argument is passed through without escaping.
        /// </summary>
        /// <remarks>
        /// This is deliberate rather than an oversight, and is pinned here so a future change to the
        /// composition has to be a decision. It is also safer than it first looks, because the command
        /// line parser is path aware: an unquoted drive path is recognised and taken whole, spaces
        /// included, so the argument a caller is most likely to pass unescaped still survives a round
        /// trip. What does not survive is a single spaced value that is not a path, which the last part
        /// of this test states outright.
        /// </remarks>
        [Fact]
        public void Constructor_PassesASingleArgumentThroughUnescaped()
        {
            // Assert: a simple argument is stored as-is
            Assert.Equal("/quiet", new UserShellExecuteOptions(@"C:\app.exe", ["/quiet"]).Arguments);

            // Assert: so is a path, verbatim and unquoted, despite the space in it
            UserShellExecuteOptions path = new(@"C:\app.exe", [@"C:\Program Files\log.txt"]);
            Assert.Equal(@"C:\Program Files\log.txt", path.Arguments);

            // Assert: and it still round-trips, because the parser recognises a drive path and takes it whole
            Assert.NotNull(path.Arguments);
            Assert.Equal([@"C:\Program Files\log.txt"], CommandLineUtilities.CommandLineToArgumentList(path.Arguments));

            // Assert: a spaced value that is not a path does not round-trip, which is the cost of not escaping
            UserShellExecuteOptions phrase = new(@"C:\app.exe", ["two words"]);
            Assert.Equal("two words", phrase.Arguments);
            Assert.NotNull(phrase.Arguments);
            Assert.Equal(["two", "words"], CommandLineUtilities.CommandLineToArgumentList(phrase.Arguments));
        }

        /// <summary>
        /// Verifies that the argument list is a snapshot, so a caller mutating its own list afterwards
        /// cannot change what will be launched.
        /// </summary>
        [Fact]
        public void Constructor_SnapshotsTheArgumentList()
        {
            // Arrange
            string[] arguments = ["/first", "/second"];

            // Act
            UserShellExecuteOptions options = new(@"C:\app.exe", arguments);
            arguments[0] = "/changed";

            // Assert
            Assert.Equal("/first", options.ArgumentList[0]);
        }

        /// <summary>
        /// Verifies that the command line quotes the file path and appends the arguments, which is the
        /// form shell execute is handed.
        /// </summary>
        /// <param name="filePath">The path to launch.</param>
        /// <param name="arguments">The arguments to pass.</param>
        /// <param name="expected">The expected command line.</param>
        [Theory]
        [MemberData(nameof(CommandLineCases))]
        public void MakeCommandLine_QuotesThePathAndAppendsTheArguments(string filePath, string[] arguments, string expected)
        {
            Assert.Equal(expected, new UserShellExecuteOptions(filePath, arguments).MakeCommandLine());
        }

        /// <summary>
        /// Verifies that a blank verb is rejected while an absent one is accepted, since shell execute
        /// treats an absent verb as "use the default".
        /// </summary>
        [Fact]
        public void Constructor_RejectsABlankVerbButAcceptsAnAbsentOne()
        {
            Assert.Null(new UserShellExecuteOptions(@"C:\app.exe", verb: null).Verb);
            Assert.Equal("runas", new UserShellExecuteOptions(@"C:\app.exe", verb: "runas").Verb);
            _ = Assert.Throws<ArgumentException>(static () => new UserShellExecuteOptions(@"C:\app.exe", verb: "   "));
        }

        /// <summary>
        /// Verifies that a blank working directory is rejected while an absent one is accepted.
        /// </summary>
        [Fact]
        public void Constructor_RejectsABlankWorkingDirectoryButAcceptsAnAbsentOne()
        {
            Assert.Null(new UserShellExecuteOptions(@"C:\app.exe", workingDirectory: null).WorkingDirectory);
            Assert.Equal(@"C:\Windows", new UserShellExecuteOptions(@"C:\app.exe", workingDirectory: @"C:\Windows").WorkingDirectory?.FullName);
            _ = Assert.Throws<ArgumentException>(static () => new UserShellExecuteOptions(@"C:\app.exe", workingDirectory: "  "));
        }

        /// <summary>
        /// Verifies that the optional window and priority settings stay unset when not supplied, so the
        /// launcher can tell "not specified" from a value that happens to be the default.
        /// </summary>
        [Fact]
        public void Constructor_LeavesOptionalSettingsUnset()
        {
            // Act
            UserShellExecuteOptions unset = new(@"C:\app.exe");
            UserShellExecuteOptions set = new(@"C:\app.exe", windowStyle: ProcessWindowStyle.Minimized, priorityClass: ProcessPriorityClass.BelowNormal);

            // Assert
            Assert.Null(unset.WindowStyle);
            Assert.Null(unset.PriorityClass);
            Assert.Equal(ProcessWindowStyle.Minimized, set.WindowStyle);
            Assert.Equal(ProcessPriorityClass.BelowNormal, set.PriorityClass);
        }

        /// <summary>
        /// Verifies that the remaining switches are carried through unchanged.
        /// </summary>
        [Fact]
        public void Constructor_CarriesTheSwitchesThrough()
        {
            // Act
            UserShellExecuteOptions options = new(
                @"C:\app.exe",
                expandEnvironmentVariables: true,
                createNoWindow: true,
                waitForChildProcesses: true,
                killChildProcessesWithParent: true);

            // Assert
            Assert.True(options.ExpandEnvironmentVariables);
            Assert.True(options.CreateNoWindow);
            Assert.True(options.WaitForChildProcesses);
            Assert.True(options.KillChildProcessesWithParent);
        }

        /// <summary>
        /// Verifies that the options survive serialisation, which is how they reach the client process
        /// that performs the invocation.
        /// </summary>
        [Fact]
        public void DataContract_RoundTripsEveryMember()
        {
            // Arrange
            UserShellExecuteOptions original = new(
                @"C:\Program Files\App\app.exe",
                ["/first", "/second value"],
                @"C:\Windows",
                expandEnvironmentVariables: true,
                verb: "runas",
                createNoWindow: true,
                waitForChildProcesses: true,
                killChildProcessesWithParent: true,
                windowStyle: ProcessWindowStyle.Hidden,
                priorityClass: ProcessPriorityClass.High);
            // The argument list is declared as an interface and holds a ReadOnlyCollection, which the
            // serializer cannot infer from the declaration. PSADT.ClientServer.Server names that same
            // concrete type in the known-type list it builds its serializer with, so supplying it here
            // mirrors the production contract rather than inventing a looser one.
            DataContractSerializer serializer = new(typeof(UserShellExecuteOptions), [typeof(ReadOnlyCollection<string>)]);

            // Act
            using MemoryStream stream = new();
            serializer.WriteObject(stream, original);
            stream.Position = 0;
            object? deserialized = serializer.ReadObject(stream);
            Assert.NotNull(deserialized);
            UserShellExecuteOptions restored = (UserShellExecuteOptions)deserialized;

            // Assert
            Assert.Equal(original.FilePath, restored.FilePath);
            Assert.Equal(original.Arguments, restored.Arguments);
            Assert.Equal(original.ArgumentList, restored.ArgumentList);
            Assert.Equal(original.WorkingDirectory?.FullName, restored.WorkingDirectory?.FullName);
            Assert.Equal(original.ExpandEnvironmentVariables, restored.ExpandEnvironmentVariables);
            Assert.Equal(original.Verb, restored.Verb);
            Assert.Equal(original.CreateNoWindow, restored.CreateNoWindow);
            Assert.Equal(original.WaitForChildProcesses, restored.WaitForChildProcesses);
            Assert.Equal(original.KillChildProcessesWithParent, restored.KillChildProcessesWithParent);
            Assert.Equal(original.WindowStyle, restored.WindowStyle);
            Assert.Equal(original.PriorityClass, restored.PriorityClass);
            Assert.Equal(original.MakeCommandLine(), restored.MakeCommandLine());
        }

        /// <summary>
        /// Verifies that converting to launch information preserves what shell execute needs and sets
        /// the flag that selects it.
        /// </summary>
        [Fact]
        public void ToLaunchInfo_PreservesTheOptionsAndSelectsShellExecute()
        {
            // Arrange
            UserShellExecuteOptions options = new(
                @"C:\Windows\notepad.exe",
                ["/first", "/second"],
                @"C:\Windows",
                verb: "open",
                createNoWindow: true,
                waitForChildProcesses: true,
                killChildProcessesWithParent: true,
                windowStyle: ProcessWindowStyle.Hidden,
                priorityClass: ProcessPriorityClass.High);

            // Act
            ProcessLaunchInfo launchInfo = options.ToLaunchInfo();

            // Assert
            Assert.True(launchInfo.UseShellExecute);
            Assert.Equal(options.FilePath, launchInfo.FilePath);
            Assert.Equal(options.ArgumentList, launchInfo.ArgumentList);
            Assert.Equal(options.Arguments, launchInfo.Arguments);
            Assert.Equal(options.WorkingDirectory?.FullName, launchInfo.WorkingDirectory?.FullName);
            Assert.Equal(options.Verb, launchInfo.Verb);
            Assert.Equal(options.CreateNoWindow, launchInfo.CreateNoWindow);
            Assert.Equal(options.WaitForChildProcesses, launchInfo.WaitForChildProcesses);
            Assert.Equal(options.KillChildProcessesWithParent, launchInfo.KillChildProcessesWithParent);
            Assert.Equal(options.PriorityClass, launchInfo.PriorityClass);
            Assert.Equal(options.MakeCommandLine(), launchInfo.MakeCommandLine());
        }

        /// <summary>
        /// Verifies that a relative path is accepted, because shell execute resolves it and the launch
        /// information only insists on a rooted path when it is not going through the shell.
        /// </summary>
        [Fact]
        public void ToLaunchInfo_AcceptsARelativePath()
        {
            // Act
            ProcessLaunchInfo launchInfo = new UserShellExecuteOptions("notepad.exe").ToLaunchInfo();

            // Assert
            Assert.Equal("notepad.exe", launchInfo.FilePath);
            Assert.True(launchInfo.UseShellExecute);
        }

        /// <summary>
        /// Argument lists of two or more, each containing something the escaping has to handle.
        /// </summary>
        public static TheoryData<string[]> MultipleArgumentCases
        {
            get
            {
                TheoryData<string[]> data = [];
                data.Add(["/first", "/second"]);
                data.Add(["/path", @"C:\Program Files\log.txt"]);
                data.Add(["/quote", @"a""b"]);
                data.Add(["/trailing", @"C:\dir\"]);
                data.Add(["/empty", string.Empty]);
                data.Add(["/one", "/two", "/three", "/four"]);
                return data;
            }
        }

        /// <summary>
        /// File paths and arguments paired with the command line they compose to.
        /// </summary>
        public static TheoryData<string, string[], string> CommandLineCases
        {
            get
            {
                TheoryData<string, string[], string> data = [];
                data.Add(@"C:\app.exe", [], @"""C:\app.exe""");
                data.Add(@"C:\app.exe", ["/quiet"], @"""C:\app.exe"" /quiet");
                data.Add(@"C:\Program Files\App\app.exe", ["/quiet"], @"""C:\Program Files\App\app.exe"" /quiet");
                data.Add(@"""C:\app.exe""", ["/quiet"], @"""C:\app.exe"" /quiet");
                data.Add(@"C:\app.exe", ["/a", "/b"], @"""C:\app.exe"" /a /b");
                return data;
            }
        }
    }
}
