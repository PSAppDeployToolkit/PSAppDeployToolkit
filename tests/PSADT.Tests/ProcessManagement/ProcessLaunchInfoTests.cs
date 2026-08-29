using System;
using System.IO;
using PSADT.ProcessManagement;
using PSADT.Security;
using Xunit;

namespace PSADT.Tests.ProcessManagement
{
    /// <summary>
    /// Tests the launch information's validation and the fields it derives.
    /// </summary>
    /// <remarks>
    /// Only the paths that need no user token are covered here. Anything reached by supplying a
    /// <c language="csharp">RunAsActiveUser</c> or asking for environment expansion in another user's context brokers a
    /// token, which belongs with the elevation-gated tests rather than with pure construction.
    /// <para>
    /// The shell execute combination checks are the substance. Shell execute cannot carry a token, so the
    /// constructor refuses four separate options alongside it, and each refusal has to name the option it
    /// is actually complaining about or it sends whoever hits it looking in the wrong place.
    /// </para>
    /// </remarks>
    public sealed class ProcessLaunchInfoTests
    {
        /// <summary>
        /// Verifies that a path the caller has already quoted is unquoted before use.
        /// </summary>
        /// <param name="filePath">The path as supplied.</param>
        /// <param name="expected">The path as stored.</param>
        [Theory]
        [InlineData(@"C:\Windows\notepad.exe", @"C:\Windows\notepad.exe")]
        [InlineData(@"""C:\Windows\notepad.exe""", @"C:\Windows\notepad.exe")]
        [InlineData(@"""C:\Program Files\App\app.exe""", @"C:\Program Files\App\app.exe")]
        public void Constructor_UnquotesTheFilePath(string filePath, string expected)
        {
            Assert.Equal(expected, new ProcessLaunchInfo(filePath).FilePath);
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
            _ = Assert.Throws<ArgumentException>(() => new ProcessLaunchInfo(filePath));
        }

        /// <summary>
        /// Verifies that a path which is not fully qualified is rejected when the shell is not being
        /// used, because there is then nothing to resolve it against.
        /// </summary>
        /// <param name="filePath">The unrooted path to reject.</param>
        [Theory]
        [InlineData("notepad.exe")]
        [InlineData(@"Windows\notepad.exe")]
        [InlineData(@".\notepad.exe")]
        [InlineData(@"\notepad.exe")]
        public void Constructor_RequiresAFullyQualifiedPathWithoutShellExecute(string filePath)
        {
            _ = Assert.Throws<DriveNotFoundException>(() => new ProcessLaunchInfo(filePath));
        }

        /// <summary>
        /// Verifies that the same path is accepted when the shell is being used, since the shell resolves
        /// it against the search path.
        /// </summary>
        /// <param name="filePath">The unrooted path to accept.</param>
        [Theory]
        [InlineData("notepad.exe")]
        [InlineData("notepad")]
        [InlineData(@".\notepad.exe")]
        public void Constructor_AllowsAnUnrootedPathWithShellExecute(string filePath)
        {
            Assert.Equal(filePath, new ProcessLaunchInfo(filePath, useShellExecute: true).FilePath);
        }

        /// <summary>
        /// Verifies that shell execute refuses each option that would require a token, and that the
        /// message names the option being refused.
        /// </summary>
        /// <remarks>
        /// The message text is asserted because it is the only thing distinguishing four otherwise
        /// identical exceptions, and one of them previously named the wrong option.
        /// </remarks>
        [Fact]
        public void Constructor_RefusesShellExecuteWithAnElevatedTokenType()
        {
            // Act
            NotSupportedException exception = Assert.Throws<NotSupportedException>(
                static () => new ProcessLaunchInfo(@"C:\app.exe", elevatedTokenType: ElevatedTokenType.HighestAvailable, useShellExecute: true));

            // Assert
            Assert.Contains("UseShellExecute", exception.Message, StringComparison.Ordinal);
            Assert.Contains("ElevatedTokenType", exception.Message, StringComparison.Ordinal);
            Assert.DoesNotContain("RunAsActiveUser", exception.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that shell execute refuses to run as the invoker, and names that option.
        /// </summary>
        [Fact]
        public void Constructor_RefusesShellExecuteWithRunAsInvoker()
        {
            // Act
            NotSupportedException exception = Assert.Throws<NotSupportedException>(
                static () => new ProcessLaunchInfo(@"C:\app.exe", runAsInvoker: true, useShellExecute: true));

            // Assert
            Assert.Contains("UseShellExecute", exception.Message, StringComparison.Ordinal);
            Assert.Contains("RunAsInvoker", exception.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that shell execute refuses to bypass image file execution options, and names that
        /// option.
        /// </summary>
        [Fact]
        public void Constructor_RefusesShellExecuteWithBypassIfeo()
        {
            // Act
            NotSupportedException exception = Assert.Throws<NotSupportedException>(
                static () => new ProcessLaunchInfo(@"C:\app.exe", bypassIfeo: true, useShellExecute: true));

            // Assert
            Assert.Contains("UseShellExecute", exception.Message, StringComparison.Ordinal);
            Assert.Contains("BypassIfeo", exception.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that an elevated token type of none is not treated as a request for elevation, so it
        /// remains combinable with shell execute.
        /// </summary>
        [Fact]
        public void Constructor_AllowsShellExecuteWithNoElevation()
        {
            // Act
            ProcessLaunchInfo launchInfo = new(@"C:\app.exe", elevatedTokenType: ElevatedTokenType.None, useShellExecute: true);

            // Assert
            Assert.True(launchInfo.UseShellExecute);
            Assert.Equal(ElevatedTokenType.None, launchInfo.ElevatedTokenType);
        }

        /// <summary>
        /// Verifies that the same options are accepted when the shell is not being used, so the refusals
        /// above are about the combination rather than about the options themselves.
        /// </summary>
        [Fact]
        public void Constructor_AllowsTheRefusedOptionsWithoutShellExecute()
        {
            // Act
            ProcessLaunchInfo launchInfo = new(
                @"C:\app.exe",
                elevatedTokenType: ElevatedTokenType.HighestAvailable,
                runAsInvoker: true,
                bypassIfeo: true);

            // Assert
            Assert.Equal(ElevatedTokenType.HighestAvailable, launchInfo.ElevatedTokenType);
            Assert.True(launchInfo.RunAsInvoker);
            Assert.True(launchInfo.BypassIfeo);
        }

        /// <summary>
        /// Verifies that no arguments produces no argument string, so the command line has no trailing
        /// space.
        /// </summary>
        [Fact]
        public void Constructor_LeavesArgumentsUnsetWhenThereAreNone()
        {
            Assert.Null(new ProcessLaunchInfo(@"C:\app.exe").Arguments);
            Assert.Null(new ProcessLaunchInfo(@"C:\app.exe", []).Arguments);
            Assert.Empty(new ProcessLaunchInfo(@"C:\app.exe").ArgumentList);
        }

        /// <summary>
        /// Verifies that a single argument is passed through unescaped while two or more are escaped.
        /// </summary>
        /// <remarks>
        /// Deliberate, and matching <c language="csharp">UserShellExecuteOptions</c>. Pinned here so the asymmetry stays a
        /// decision rather than drifting.
        /// </remarks>
        [Fact]
        public void Constructor_EscapesOnlyWhenThereIsMoreThanOneArgument()
        {
            // Assert: one argument, verbatim even though it contains a space
            Assert.Equal(@"C:\Program Files\log.txt", new ProcessLaunchInfo(@"C:\app.exe", [@"C:\Program Files\log.txt"]).Arguments);

            // Assert: two arguments, escaped so they parse back
            ProcessLaunchInfo two = new(@"C:\app.exe", ["/log", @"C:\Program Files\log.txt"]);
            Assert.NotNull(two.Arguments);
            Assert.Equal(["/log", @"C:\Program Files\log.txt"], CommandLineUtilities.CommandLineToArgumentList(two.Arguments));
        }

        /// <summary>
        /// Verifies that the command line quotes the path and appends the arguments.
        /// </summary>
        [Fact]
        public void MakeCommandLine_QuotesThePathAndAppendsTheArguments()
        {
            Assert.Equal(@"""C:\app.exe""", new ProcessLaunchInfo(@"C:\app.exe").MakeCommandLine());
            Assert.Equal(@"""C:\app.exe"" /quiet", new ProcessLaunchInfo(@"C:\app.exe", ["/quiet"]).MakeCommandLine());
            Assert.Equal(@"""C:\Program Files\app.exe"" /a /b", new ProcessLaunchInfo(@"C:\Program Files\app.exe", ["/a", "/b"]).MakeCommandLine());
        }

        /// <summary>
        /// Verifies that the null-terminated form appends exactly one terminator, which the process
        /// creation call requires and which must not be duplicated.
        /// </summary>
        [Fact]
        public void MakeCommandLine_AppendsASingleNullTerminatorWhenAsked()
        {
            // Arrange
            ProcessLaunchInfo launchInfo = new(@"C:\app.exe", ["/quiet"]);

            // Act
            string terminated = launchInfo.MakeCommandLine(nullTerminated: true);

            // Assert
            Assert.Equal(launchInfo.MakeCommandLine() + '\0', terminated);
            Assert.Equal('\0', terminated[^1]);
            Assert.Equal(1, terminated.Split('\0').Length - 1);
        }

        /// <summary>
        /// Verifies that a file which cannot be read as an image falls back to classifying by extension,
        /// which is how a batch script gets treated as a console application.
        /// </summary>
        /// <param name="filePath">The path whose extension decides the classification.</param>
        /// <param name="expectedCli">Whether it should be treated as a console application.</param>
        [Theory]
        [InlineData(@"C:\does\not\exist\script.cmd", true)]
        [InlineData(@"C:\does\not\exist\script.bat", true)]
        [InlineData(@"C:\does\not\exist\legacy.com", true)]
        [InlineData(@"C:\does\not\exist\app.exe", false)]
        [InlineData(@"C:\does\not\exist\noextension", false)]
        [InlineData(@"C:\does\not\exist\script.CMD", true)]
        [InlineData(@"C:\does\not\exist\script.Bat", true)]
        public void Constructor_ClassifiesByExtensionWhenTheImageCannotBeRead(string filePath, bool expectedCli)
        {
            Assert.Equal(expectedCli, new ProcessLaunchInfo(filePath).IsCliApplication());
        }

        /// <summary>
        /// Verifies that a real console executable is classified from its image header rather than its
        /// extension, which is the path the fallback above exists to cover for.
        /// </summary>
        [Fact]
        public void Constructor_ClassifiesARealConsoleImageFromItsHeader()
        {
            // Arrange: the test host is a console application, so its own image is a known-good fixture
            using System.Diagnostics.Process current = System.Diagnostics.Process.GetCurrentProcess();
            string? testHost = current.MainModule?.FileName;
            Assert.NotNull(testHost);
            Assert.True(File.Exists(testHost));

            // Act & Assert
            Assert.True(new ProcessLaunchInfo(testHost).IsCliApplication());
        }

        /// <summary>
        /// Verifies that a blank verb or working directory is rejected while an absent one is accepted.
        /// </summary>
        [Fact]
        public void Constructor_RejectsBlankOptionalStrings()
        {
            Assert.Null(new ProcessLaunchInfo(@"C:\app.exe").Verb);
            Assert.Null(new ProcessLaunchInfo(@"C:\app.exe").WorkingDirectory);
            _ = Assert.Throws<ArgumentException>(static () => new ProcessLaunchInfo(@"C:\app.exe", verb: "  "));
            _ = Assert.Throws<ArgumentException>(static () => new ProcessLaunchInfo(@"C:\app.exe", workingDirectory: "  "));
        }

        /// <summary>
        /// Verifies that asking for no window also forces the window style to hidden, since the two
        /// settings would otherwise contradict each other.
        /// </summary>
        [Fact]
        public void Constructor_HidesTheWindowWhenAskedToCreateNone()
        {
            // Act
            ProcessLaunchInfo launchInfo = new(@"C:\app.exe", createNoWindow: true);

            // Assert
            Assert.True(launchInfo.CreateNoWindow);
            Assert.Equal(System.Diagnostics.ProcessWindowStyle.Hidden, launchInfo.WindowStyle);
        }

        /// <summary>
        /// Verifies that an explicit window style takes precedence over the one implied by asking for no
        /// window, since it is applied afterwards.
        /// </summary>
        [Fact]
        public void Constructor_LetsAnExplicitWindowStyleOverrideCreateNoWindow()
        {
            // Act
            ProcessLaunchInfo launchInfo = new(@"C:\app.exe", createNoWindow: true, windowStyle: System.Diagnostics.ProcessWindowStyle.Minimized);

            // Assert
            Assert.True(launchInfo.CreateNoWindow);
            Assert.Equal(System.Diagnostics.ProcessWindowStyle.Minimized, launchInfo.WindowStyle);
        }

        /// <summary>
        /// Verifies that the stream encoding defaults to the process default and is otherwise the one
        /// asked for, since it is stored by name rather than as the object.
        /// </summary>
        [Fact]
        public void Constructor_StoresTheStreamEncodingByName()
        {
            Assert.Equal(System.Text.Encoding.Default.WebName, new ProcessLaunchInfo(@"C:\app.exe").StreamEncoding.WebName);
            Assert.Equal("utf-8", new ProcessLaunchInfo(@"C:\app.exe", streamEncoding: System.Text.Encoding.UTF8).StreamEncoding.WebName);
        }

        /// <summary>
        /// Verifies that inherited handles survive the conversion to and from the serialisable form,
        /// which stores them widened so the value is the same on both architectures.
        /// </summary>
        [Fact]
        public void HandlesToInherit_RoundTripsThroughTheStoredForm()
        {
            // Arrange
            nint[] handles = [1, 2, 0x7FFF_FFFF, -1];

            // Act
            ProcessLaunchInfo launchInfo = new(@"C:\app.exe", handlesToInherit: handles);

            // Assert
            Assert.Equal(handles, launchInfo.HandlesToInherit);
        }

        /// <summary>
        /// Verifies that no inherited handles yields an empty collection rather than null, so callers can
        /// enumerate unconditionally.
        /// </summary>
        [Fact]
        public void HandlesToInherit_IsEmptyWhenNoneWereSupplied()
        {
            Assert.Empty(new ProcessLaunchInfo(@"C:\app.exe").HandlesToInherit);
        }

        /// <summary>
        /// Verifies that standard input is snapshotted and defaults to empty.
        /// </summary>
        [Fact]
        public void Constructor_SnapshotsStandardInput()
        {
            // Arrange
            string[] input = ["first", "second"];

            // Act
            ProcessLaunchInfo launchInfo = new(@"C:\app.exe", standardInput: input);
            input[0] = "changed";

            // Assert
            Assert.Equal(["first", "second"], launchInfo.StandardInput);
            Assert.Empty(new ProcessLaunchInfo(@"C:\app.exe").StandardInput);
        }

        /// <summary>
        /// Verifies that every option a launch carries is the option read back, since they are passed
        /// positionally through a constructor with more than twenty parameters.
        /// </summary>
        /// <remarks>
        /// Two transposed there would produce a launch that is entirely valid and does the wrong thing -
        /// running without elevation where elevation was asked for, or showing a window meant to be
        /// hidden. Each is set apart from the rest, so a transposition shows up as the wrong one being
        /// reported rather than as a plausible whole.
        /// </remarks>
        [Fact]
        public void ProcessLaunchInfo_KeepsEveryOptionSeparately()
        {
            Assert.True(new ProcessLaunchInfo(@"C:\app.exe", inheritEnvironmentVariables: true).InheritEnvironmentVariables);
            Assert.True(new ProcessLaunchInfo(@"C:\app.exe", expandEnvironmentVariables: true).ExpandEnvironmentVariables);
            Assert.True(new ProcessLaunchInfo(@"C:\app.exe", denyUserTermination: true).DenyUserTermination);
            Assert.True(new ProcessLaunchInfo(@"C:\app.exe", runAsInvoker: true).RunAsInvoker);
            Assert.True(new ProcessLaunchInfo(@"C:\app.exe", bypassIfeo: true).BypassIfeo);
            Assert.True(new ProcessLaunchInfo(@"C:\app.exe", createNoWindow: true).CreateNoWindow);
            Assert.True(new ProcessLaunchInfo(@"C:\app.exe", waitForChildProcesses: true).WaitForChildProcesses);
            Assert.True(new ProcessLaunchInfo(@"C:\app.exe", killChildProcessesWithParent: true).KillChildProcessesWithParent);
            Assert.True(new ProcessLaunchInfo(@"C:\app.exe", noTerminateOnTimeout: true).NoTerminateOnTimeout);
        }

        /// <summary>
        /// Verifies that a launch asking for nothing in particular has none of those options set.
        /// </summary>
        [Fact]
        public void ProcessLaunchInfo_LeavesEveryOptionOffByDefault()
        {
            // Act
            ProcessLaunchInfo launchInfo = new(@"C:\app.exe");

            // Assert
            Assert.False(launchInfo.InheritEnvironmentVariables);
            Assert.False(launchInfo.ExpandEnvironmentVariables);
            Assert.False(launchInfo.DenyUserTermination);
            Assert.False(launchInfo.RunAsInvoker);
            Assert.False(launchInfo.UIAccess);
            Assert.False(launchInfo.BypassIfeo);
            Assert.False(launchInfo.CreateNoWindow);
            Assert.False(launchInfo.WaitForChildProcesses);
            Assert.False(launchInfo.KillChildProcessesWithParent);
            Assert.False(launchInfo.NoTerminateOnTimeout);
        }

        /// <summary>
        /// Verifies that the kind of executable being launched is read from the file itself, since it
        /// decides whether a console is created and whether the streams can be captured at all.
        /// </summary>
        [Fact]
        public void ImageSubsystem_IsReadFromTheExecutable()
        {
            // Act
            ProcessLaunchInfo launchInfo = new(Path.Join(Environment.SystemDirectory, "cmd.exe"));

            // Assert: the interpreter is a console application, and the launch agrees
            Assert.Equal(Interop.IMAGE_SUBSYSTEM.IMAGE_SUBSYSTEM_WINDOWS_CUI, launchInfo.ImageSubsystem);
            Assert.True(launchInfo.IsCliApplication());
        }

        /// <summary>
        /// Verifies that a file whose header cannot be read is still given a subsystem, since a launch
        /// has to decide about a window one way or the other.
        /// </summary>
        /// <remarks>
        /// A batch file has no header to read, so the extension is what settles it - and it settles it as
        /// a console application, which is what one is.
        /// </remarks>
        [Fact]
        public void ImageSubsystem_FallsBackToTheExtension()
        {
            Assert.Equal(Interop.IMAGE_SUBSYSTEM.IMAGE_SUBSYSTEM_WINDOWS_CUI, new ProcessLaunchInfo(@"C:\does-not-exist\script.bat").ImageSubsystem);
        }

        /// <summary>
        /// Verifies that two launches asking for the same thing are equal, and that a difference in any
        /// of the lists makes them unequal.
        /// </summary>
        /// <remarks>
        /// The lists are the part worth asserting. A collection compares by reference, so a record
        /// holding one directly never equals another built the same way - and this record is built twice
        /// from the same request routinely, once on either side of the boundary a client is launched
        /// across. They are held in a list that compares by its contents instead.
        /// </remarks>
        [Fact]
        public void Equality_IsByValueIncludingTheLists()
        {
            // Arrange
            ProcessLaunchInfo left = new(@"C:\app.exe", ["/quiet", "/norestart"], standardInput: ["first", "second"]);
            ProcessLaunchInfo right = new(@"C:\app.exe", ["/quiet", "/norestart"], standardInput: ["first", "second"]);

            // Assert
            Assert.Equal(left, right);
            Assert.Equal(left.GetHashCode(), right.GetHashCode());

            // Assert: and a difference in either list is a difference
            Assert.NotEqual(left, new ProcessLaunchInfo(@"C:\app.exe", ["/quiet"], standardInput: ["first", "second"]));
            Assert.NotEqual(left, new ProcessLaunchInfo(@"C:\app.exe", ["/quiet", "/norestart"], standardInput: ["first"]));
            Assert.NotEqual(left, new ProcessLaunchInfo(@"C:\app.exe", ["/norestart", "/quiet"], standardInput: ["first", "second"]));
        }
    }
}
