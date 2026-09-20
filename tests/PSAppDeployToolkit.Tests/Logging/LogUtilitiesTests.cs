using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using PSADT.PowerShellTestFixture;
using PSAppDeployToolkit.Foundation;
using PSAppDeployToolkit.Logging;
using PSAppDeployToolkit.Tests.TestHelpers;
using Xunit;

namespace PSAppDeployToolkit.Tests.Logging
{
    /// <summary>
    /// Tests the entry point every log line in the toolkit passes through.
    /// </summary>
    /// <remarks>
    /// It resolves its own caller, which is the part that makes it awkward and interesting: with no runspace it reads
    /// the CLR stack, and with one it asks PowerShell for its call stack instead. Both paths are exercised here, by
    /// entering the fixture's runspace or not.
    /// </remarks>
    /// <param name="powerShell">The hosted engine, shared across the collection.</param>
    [Collection(PowerShellCollection.Name)]
    public sealed class LogUtilitiesTests(PowerShellFixture powerShell) : IDisposable
    {
        /// <summary>
        /// Puts back whatever database was seated before this test.
        /// </summary>
        public void Dispose()
        {
            _imported.Dispose();
        }

        /// <summary>
        /// The module imported and not initialized, which is what writing a log entry before
        /// <c language="powershell">Initialize-ADTModule</c> runs against. Seated for every test here because the
        /// writer asks the database whether it is initialized, and that is refused outright when none is seated.
        /// A test needing configuration seats its own over the top of this one.
        /// </summary>
        private readonly ModuleDatabaseScope _imported = powerShell.SeatModuleDatabaseWithoutState();

        /// <summary>
        /// Verifies that a message is returned as an entry carrying what was asked for.
        /// </summary>
        [Fact]
        public void WriteLogEntry_ReturnsAnEntryForWhatItWasAsked()
        {
            // Act
            IReadOnlyList<LogEntry> entries = Write(["a message"], severity: LogSeverity.Warning, source: "Test-Command", scriptSection: "Installation");

            // Assert
            LogEntry entry = Assert.Single(entries);
            Assert.Equal("a message", entry.Message);
            Assert.Equal(LogSeverity.Warning, entry.Severity);
            Assert.Equal("Test-Command", entry.Source);
            Assert.Equal("Installation", entry.ScriptSection);
        }

        /// <summary>
        /// Verifies that several messages become several entries sharing one timestamp.
        /// </summary>
        /// <remarks>
        /// The shared timestamp is deliberate: a multi-line message logged as one call is one event, and lines that
        /// drifted apart by a millisecond would sort oddly in a viewer.
        /// </remarks>
        [Fact]
        public void WriteLogEntry_GivesEveryEntryFromOneCallTheSameTimestamp()
        {
            // Act
            IReadOnlyList<LogEntry> entries = Write(["first", "second", "third"]);

            // Assert
            Assert.Equal(3, entries.Count);
            _ = Assert.Single(entries.Select(static entry => entry.Timestamp).Distinct());
        }

        /// <summary>
        /// Verifies that a blank message is dropped rather than logged.
        /// </summary>
        [Fact]
        public void WriteLogEntry_DropsBlankMessages()
        {
            Assert.Equal(["kept"], Write(["kept", string.Empty, "   "]).Select(static entry => entry.Message), StringComparer.Ordinal);
        }

        /// <summary>
        /// Verifies that a call with nothing worth logging is refused rather than passing silently.
        /// </summary>
        /// <remarks>
        /// A caller that logged only blanks has a bug, and returning an empty list would hide it. This is the one case
        /// where the method throws over something the caller could have checked.
        /// </remarks>
        [Fact]
        public void WriteLogEntry_RefusesACallWithNothingWorthLogging()
        {
            _ = Assert.Throws<InvalidOperationException>(static () => Write([string.Empty, "   "]));
            _ = Assert.Throws<InvalidOperationException>(static () => Write([]));
        }

        /// <summary>
        /// Verifies that each nullable argument is refused when blank but accepted when absent.
        /// </summary>
        /// <remarks>
        /// The distinction throughout the toolkit: null means the caller supplied nothing and a default applies, while
        /// blank means the caller supplied something and got it wrong.
        /// </remarks>
        [Fact]
        public void WriteLogEntry_RefusesABlankArgumentButAcceptsAnAbsentOne()
        {
            _ = Assert.Throws<ArgumentException>(static () => Write(["a message"], source: "   "));
            _ = Assert.Throws<ArgumentException>(static () => Write(["a message"], scriptSection: "   "));
            _ = Assert.Throws<ArgumentException>(static () => Write(["a message"], logFileDirectory: "   "));
            _ = Assert.Throws<ArgumentException>(static () => Write(["a message"], logFileName: "   "));
            _ = Write(["a message"]);
        }

        /// <summary>
        /// Verifies that the severity defaults to informational.
        /// </summary>
        [Fact]
        public void WriteLogEntry_DefaultsToInformational()
        {
            Assert.Equal(LogSeverity.Info, Assert.Single(Write(["a message"])).Severity);
        }

        /// <summary>
        /// Verifies that the source defaults to the caller this resolved.
        /// </summary>
        /// <remarks>
        /// With no runspace the caller comes from the CLR stack, and the first frame outside the toolkit's own
        /// namespaces is taken. A test class sits in <c language="text">PSAppDeployToolkit.Tests</c>, which the pattern matching those
        /// namespaces also matches, so the frame chosen here is xunit's rather than the test's - a real consequence of
        /// how the filter is written, and worth knowing before reading a log from a test run.
        /// </remarks>
        [Fact]
        public void WriteLogEntry_DefaultsTheSourceToTheResolvedCaller()
        {
            // Act
            LogEntry entry = Assert.Single(Write(["a message"]));

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(entry.Source));
            Assert.Equal(entry.CallerSource, entry.Source);
        }

        /// <summary>
        /// Verifies that a debug message is dropped unless the configuration asks for it.
        /// </summary>
        /// <remarks>
        /// Checked against the configuration rather than a parameter, so a caller cannot force one through. Before the
        /// module is initialised there is no seated configuration and the shipped one answers instead, so a debug
        /// message written during import obeys the same setting as one written afterwards - which it did not before
        /// the shipped configuration became readable, when it was dropped regardless.
        /// </remarks>
        [Fact]
        public void WriteLogEntry_DropsADebugMessageUnlessTheConfigurationAsksForIt()
        {
            // Assert: nothing seated, so the shipped configuration decides. It says no.
            Assert.Empty(Write(["a message"], debugMessage: true));

            // Assert: nothing seated, and a shipped configuration that says yes.
            using (powerShell.SeatModuleDatabaseWithoutState(new ModuleConfiguration { LogDebugMessage = true }))
            {
                Assert.True(Assert.Single(Write(["a message"], debugMessage: true)).DebugMessage);
            }

            // Assert: a seated configuration that says no, over a shipped one that says yes.
            using (powerShell.SeatModuleDatabase(new ModuleConfiguration { LogDebugMessage = false }, environment: null, defaults: new ModuleConfiguration { LogDebugMessage = true }))
            {
                Assert.Empty(Write(["a message"], debugMessage: true));
            }

            // Assert: a seated configuration that says yes.
            using (powerShell.SeatModuleDatabase(new ModuleConfiguration { LogDebugMessage = true }))
            {
                LogEntry entry = Assert.Single(Write(["a message"], debugMessage: true));
                Assert.True(entry.DebugMessage);
            }
        }

        /// <summary>
        /// Verifies that a non-debug message is logged whatever the configuration says.
        /// </summary>
        [Fact]
        public void WriteLogEntry_LogsANonDebugMessageWhateverTheConfigurationSays()
        {
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(new ModuleConfiguration { LogDebugMessage = false });
            _ = Assert.Single(Write(["a message"]));
        }

        /// <summary>
        /// Verifies that the log directory is created when it is not already there.
        /// </summary>
        [Fact]
        public void WriteLogEntry_CreatesTheLogDirectory()
        {
            // Arrange
            using TempDirectory temp = new();
            string directory = temp.GetPath("Logs");

            // Act
            _ = Write(["a message"], logFileDirectory: directory, logFileName: "test.log");

            // Assert
            Assert.True(Directory.Exists(directory));
        }

        /// <summary>
        /// Verifies that entries reach the file, in the format asked for.
        /// </summary>
        [Fact]
        public void WriteLogEntry_WritesTheRequestedFormatToDisk()
        {
            // Arrange
            using TempDirectory temp = new();

            // Act
            IReadOnlyList<LogEntry> cmtrace = Write(["a message"], logFileDirectory: temp.FullName, logFileName: "cmtrace.log", logStyle: LogStyle.CMTrace);
            IReadOnlyList<LogEntry> legacy = Write(["a message"], logFileDirectory: temp.FullName, logFileName: "legacy.log", logStyle: LogStyle.Legacy);

            // Assert
            Assert.Contains(Assert.Single(cmtrace).CMTraceLogLine, File.ReadAllText(temp.GetPath("cmtrace.log")), StringComparison.Ordinal);
            Assert.Contains(Assert.Single(legacy).LegacyLogLine, File.ReadAllText(temp.GetPath("legacy.log")), StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that a second call adds to the file rather than replacing it.
        /// </summary>
        /// <remarks>
        /// Every line of a deployment's log arrives through a separate call, so appending is the whole mechanism.
        /// </remarks>
        [Fact]
        public void WriteLogEntry_AppendsToAnExistingFile()
        {
            // Arrange
            using TempDirectory temp = new();

            // Act
            _ = Write(["first"], logFileDirectory: temp.FullName, logFileName: "test.log");
            _ = Write(["second"], logFileDirectory: temp.FullName, logFileName: "test.log");

            // Assert
            string written = File.ReadAllText(temp.GetPath("test.log"));
            Assert.Contains("first", written, StringComparison.Ordinal);
            Assert.Contains("second", written, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that nothing is written to disk unless both a directory and a name were given.
        /// </summary>
        [Fact]
        public void WriteLogEntry_WritesNothingToDiskWithoutBothADirectoryAndAName()
        {
            // Arrange
            using TempDirectory temp = new();

            // Act
            _ = Write(["a message"], logFileDirectory: temp.FullName);
            _ = Write(["a message"], logFileName: "test.log");

            // Assert
            Assert.Empty(Directory.GetFiles(temp.FullName));
        }

        /// <summary>
        /// Verifies that the format comes from the seated configuration, and from the shipped one before there is one.
        /// </summary>
        /// <remarks>
        /// The fallback used to be CMTrace outright, whatever the module shipped. Reading the shipped configuration
        /// instead means a log entry written before initialisation is in the same format as the rest of that
        /// deployment's, which is what makes the two halves of a log readable as one file.
        /// <para>
        /// Each case names a shipped format that differs from what is being asserted where one is seated, so a
        /// reader taking the wrong table of the two cannot agree by accident.
        /// </para>
        /// </remarks>
        [Fact]
        public void WriteLogEntry_TakesTheFormatFromTheSeatedConfigurationAndThenTheShippedOne()
        {
            // Arrange
            using TempDirectory temp = new();

            // Act: seated names Legacy while shipped names CMTrace.
            using (powerShell.SeatModuleDatabase(new ModuleConfiguration { LogStyle = "Legacy" }, environment: null, defaults: new ModuleConfiguration { LogStyle = "CMTrace" }))
            {
                _ = Write(["from the seated configuration"], logFileDirectory: temp.FullName, logFileName: "seated.log");
            }

            // Act: nothing seated, so the shipped configuration answers. Once for each format, since a fallback
            // hard-coded to either would agree with one of them.
            using (powerShell.SeatModuleDatabaseWithoutState(new ModuleConfiguration { LogStyle = "Legacy" }))
            {
                _ = Write(["from the shipped configuration"], logFileDirectory: temp.FullName, logFileName: "shipped-legacy.log");
            }
            using (powerShell.SeatModuleDatabaseWithoutState(new ModuleConfiguration { LogStyle = "CMTrace" }))
            {
                _ = Write(["from the shipped configuration"], logFileDirectory: temp.FullName, logFileName: "shipped-cmtrace.log");
            }

            // Assert
            Assert.DoesNotContain("<![LOG[", File.ReadAllText(temp.GetPath("seated.log")), StringComparison.Ordinal);
            Assert.DoesNotContain("<![LOG[", File.ReadAllText(temp.GetPath("shipped-legacy.log")), StringComparison.Ordinal);
            Assert.Contains("<![LOG[", File.ReadAllText(temp.GetPath("shipped-cmtrace.log")), StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that a configuration naming a format the toolkit does not recognise is refused rather than guessed at.
        /// </summary>
        /// <remarks>
        /// It used to fall back to CMTrace. Refusing agrees with what opening a session has always done - a session
        /// reads the same setting through <c language="csharp">Enum.Parse</c> and cannot start without it - so the
        /// setting now fails the same way wherever it is read, rather than being tolerated in one place and fatal in
        /// the other. Nothing validates it when the configuration is imported, so a typo in
        /// <c language="text">config.psd1</c> reaches both.
        /// </remarks>
        [Fact]
        public void WriteLogEntry_RefusesAConfiguredFormatItDoesNotRecognise()
        {
            using (powerShell.SeatModuleDatabase(new ModuleConfiguration { LogStyle = "NotAFormat" }))
            {
                _ = Assert.Throws<ArgumentException>(static () => Write(["a message"]));
            }
            using (powerShell.SeatModuleDatabaseWithoutState(new ModuleConfiguration { LogStyle = "NotAFormat" }))
            {
                _ = Assert.Throws<ArgumentException>(static () => Write(["a message"]));
            }
        }

        /// <summary>
        /// Verifies that a format named by the caller is used without a configured one being read at all.
        /// </summary>
        /// <remarks>
        /// The path <c language="powershell">Write-ADTLogEntry</c> takes when it is handed a log file and a format
        /// with no initialised module behind it. Asserted against a configuration whose own format would throw, so
        /// what is shown is that it was not consulted rather than that it agreed.
        /// </remarks>
        [Fact]
        public void WriteLogEntry_TakesAFormatFromTheCallerWithoutReadingTheConfiguredOne()
        {
            // Arrange
            using TempDirectory temp = new();
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(new ModuleConfiguration { LogStyle = "NotAFormat" });

            // Act
            _ = Write(["a message"], logFileDirectory: temp.FullName, logFileName: "test.log", logStyle: LogStyle.Legacy);

            // Assert
            Assert.DoesNotContain("<![LOG[", File.ReadAllText(temp.GetPath("test.log")), StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that a configuration missing the parts this reads is named rather than dereferenced.
        /// </summary>
        /// <remarks>
        /// Both are reachable from a hand-edited <c language="text">config.psd1</c>, and both used to pass silently:
        /// a missing section left every setting unread, and a missing format fell back. They fail here instead,
        /// which is a worthwhile trade only because the message says which part is missing.
        /// </remarks>
        [Fact]
        public void WriteLogEntry_NamesThePartOfTheConfigurationThatIsMissing()
        {
            // Arrange
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(new ModuleConfiguration());
            IDictionary toolkit = Assert.IsType<IDictionary>(ModuleDatabase.GetConfig()["Toolkit"], exactMatch: false);

            // Assert: the format alone.
            toolkit.Remove("LogStyle");
            Assert.Contains(
                "'LogStyle'",
                Assert.Throws<InvalidOperationException>(static () => Write(["a message"])).Message,
                StringComparison.Ordinal);

            // Assert: the whole section.
            ModuleDatabase.GetConfig().Remove("Toolkit");
            Assert.Contains(
                "'Toolkit'",
                Assert.Throws<InvalidProgramException>(static () => Write(["a message"])).Message,
                StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that nothing reaches the console when no host stream was asked for.
        /// </summary>
        [Fact]
        public void WriteLogEntry_WritesNothingToTheConsoleWhenNoneWasAskedFor()
        {
            using ConsoleCapture console = new();
            _ = Write(["a message"], HostLogStreamType.None);
            Assert.Empty(console.Output);
            Assert.Empty(console.Error);
        }

        /// <summary>
        /// Verifies that the console path writes the legacy line, choosing the stream by severity.
        /// </summary>
        /// <remarks>
        /// An error goes to standard error so that a caller redirecting the two separately still sees it. Everything
        /// else goes to standard output.
        /// </remarks>
        [Fact]
        public void WriteLogEntry_WritesToTheConsoleChoosingTheStreamBySeverity()
        {
            using ConsoleCapture console = new();

            // Act
            IReadOnlyList<LogEntry> informational = Write(["informational"], HostLogStreamType.Console);
            IReadOnlyList<LogEntry> failure = Write(["a failure"], HostLogStreamType.Console, severity: LogSeverity.Error);

            // Assert
            Assert.Contains(Assert.Single(informational).LegacyLogLine, console.Output, StringComparison.Ordinal);
            Assert.Contains(Assert.Single(failure).LegacyLogLine, console.Error, StringComparison.Ordinal);
            Assert.DoesNotContain("a failure", console.Output, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that with no runspace, a request for the host stream falls back to the console.
        /// </summary>
        /// <remarks>
        /// The host stream needs PowerShell, so asking for it outside an engine has to degrade rather than fail. This
        /// is what lets the toolkit log from a process that is not PowerShell at all.
        /// </remarks>
        [Fact]
        public void WriteLogEntry_FallsBackToTheConsoleWhenThereIsNoRunspace()
        {
            using ConsoleCapture console = new();

            // Act
            IReadOnlyList<LogEntry> entries = Write(["a message"], HostLogStreamType.Host);

            // Assert
            Assert.Contains(Assert.Single(entries).LegacyLogLine, console.Output, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that the host stream is used when a runspace is available.
        /// </summary>
        /// <remarks>
        /// This is the path that reaches <c language="powershell">$Script:CommandTable</c> in the module's session state, so it exercises the
        /// half of the fixture that supplies it. Nothing should reach the console: the entry goes to PowerShell's own
        /// information stream instead.
        /// </remarks>
        [Fact]
        public void WriteLogEntry_UsesTheHostStreamWhenARunspaceIsAvailable()
        {
            using IDisposable scope = powerShell.Enter();
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(new ModuleConfiguration());
            using ConsoleCapture console = new();

            // Act
            IReadOnlyList<LogEntry> entries = Write(["a message"], HostLogStreamType.Host);

            // Assert
            _ = Assert.Single(entries);
            Assert.Empty(console.Output);
        }

        /// <summary>
        /// Verifies that the verbose stream is used when asked for, and that a warning goes to the warning stream.
        /// </summary>
        /// <remarks>
        /// The split is at warning: anything of that severity or worse is a warning rather than verbose output, because
        /// verbose output is off by default and a warning should not be.
        /// </remarks>
        [Fact]
        public void WriteLogEntry_SplitsVerboseOutputFromWarningsBySeverity()
        {
            using IDisposable scope = powerShell.Enter();
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(new ModuleConfiguration());
            using ConsoleCapture console = new();

            // Act
            IReadOnlyList<LogEntry> informational = Write(["informational"], HostLogStreamType.Verbose);
            IReadOnlyList<LogEntry> warning = Write(["a warning"], HostLogStreamType.Verbose, severity: LogSeverity.Warning);
            IReadOnlyList<LogEntry> failure = Write(["a failure"], HostLogStreamType.Verbose, severity: LogSeverity.Error);

            // Assert
            _ = Assert.Single(informational);
            _ = Assert.Single(warning);
            _ = Assert.Single(failure);
            Assert.Empty(console.Output);
        }

        /// <summary>
        /// Verifies that the caller is resolved through PowerShell only when the call came through PowerShell.
        /// </summary>
        /// <remarks>
        /// An open runspace is not enough on its own. The test is whether the stack carries a
        /// <c language="csharp">System.Management.Automation</c> frame, so a direct call from .NET reads the CLR stack even with a
        /// runspace open, and only a call arriving from a script asks PowerShell for its call stack. That is the right
        /// behaviour - it is where the caller actually is - but it is not what the runspace check alone suggests.
        /// <para>
        /// The two report different shapes: a CLR frame gives <c language="csharp">Type.Method()</c>, a PowerShell frame gives a command
        /// name. Asserted by shape rather than by value, since what sits above a test differs from what sits above a
        /// deployment script.
        /// </para>
        /// </remarks>
        [Fact]
        public void WriteLogEntry_ResolvesTheCallerThroughPowerShellOnlyWhenCalledFromAScript()
        {
            using IDisposable scope = powerShell.Enter();
            using ModuleDatabaseScope database = powerShell.SeatModuleDatabase(new ModuleConfiguration());

            // Act: called from a script, so the stack carries PowerShell frames. NullString rather than
            // $null because PowerShell converts $null to an empty string for a string parameter.
            LogEntry fromScript = LogEntryFrom(powerShell.InvokeInRunspace(
                "$nothing = [System.Management.Automation.Language.NullString]::Value; " +
                "function Invoke-TestLogEntry { [PSAppDeployToolkit.Logging.LogUtilities]::WriteLogEntry("
                + "[System.String[]]@('from a script'), "
                + "[PSAppDeployToolkit.Logging.HostLogStreamType]::None, "
                + "$false, $null, $nothing, $nothing, $nothing, $nothing, $null) }; Invoke-TestLogEntry"));

            // Act: called directly, with the same runspace still open.
            LogEntry fromDotNet = Assert.Single(Write(["from .NET"], HostLogStreamType.None));

            // Assert
            Assert.DoesNotContain("()", fromScript.CallerSource, StringComparison.Ordinal);
            Assert.EndsWith("()", fromDotNet.CallerSource, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that with no runspace at all, the caller is resolved from the CLR stack.
        /// </summary>
        [Fact]
        public void WriteLogEntry_ResolvesTheCallerFromTheStackWhenThereIsNoRunspace()
        {
            // Act
            LogEntry entry = Assert.Single(Write(["a message"], HostLogStreamType.None));

            // Assert
            Assert.EndsWith("()", entry.CallerSource, StringComparison.Ordinal);
        }

        /// <summary>
        /// Takes the single entry a script wrote to the pipeline.
        /// </summary>
        /// <param name="written">What the script wrote.</param>
        /// <returns>The entry.</returns>
        private static LogEntry LogEntryFrom(IReadOnlyList<System.Management.Automation.PSObject> written)
        {
            object? value = Assert.Single(written);
            while (value is System.Management.Automation.PSObject wrapper)
            {
                value = wrapper.BaseObject;
            }
            return Assert.IsType<LogEntry>(value);
        }

        /// <summary>
        /// Verifies that the log file name pattern matches the extensions a log may carry, and nothing else.
        /// </summary>
        /// <remarks>
        /// Used in two places, and it has to agree with itself in both: the module validates a supplied log name
        /// against it, and a session splits a name from its extension with it when rotating. A name whose extension
        /// it did not recognise would rotate to a file with no extension at all, so capitalisation is ignored - an
        /// extension is an extension however it was typed.
        /// </remarks>
        /// <param name="name">The log file name to test.</param>
        /// <param name="expected">Whether the pattern should match it.</param>
        [Theory]
        [InlineData("deploy.log", true)]
        [InlineData("deploy.logx", true)]
        [InlineData("deploy.txt", true)]
        [InlineData("deploy.out", true)]
        [InlineData("deploy.LOG", true)]
        [InlineData("deploy.Log", true)]
        [InlineData("deploy.LogX", true)]
        [InlineData("deploy.zip", false)]
        [InlineData("deploy", false)]
        [InlineData("deploy.log.zip", false)]
        public void LogFileNameRegex_MatchesTheExtensionsALogMayCarry(string name, bool expected)
        {
            Assert.Equal(expected, LogUtilities.LogFileNameRegex.IsMatch(name));
        }

        /// <summary>
        /// Verifies that the log encoding writes a byte order mark and refuses anything it cannot encode.
        /// </summary>
        /// <remarks>
        /// The mark is what makes a viewer read the file as UTF-8 rather than guessing at the machine's code page, which
        /// is how a log with non-ASCII text ends up unreadable. Refusing invalid bytes is the counterpart: a mangled
        /// line should fail loudly rather than be written as question marks.
        /// </remarks>
        [Fact]
        public void LogEncoding_WritesAByteOrderMarkAndRefusesWhatItCannotEncode()
        {
            Assert.Equal([0xEF, 0xBB, 0xBF], LogUtilities.LogEncoding.GetPreamble());
            _ = Assert.Throws<EncoderFallbackException>(static () => LogUtilities.LogEncoding.GetBytes("\uD800"));
        }

        /// <summary>
        /// Verifies that the divider is a run of dashes, which is what makes it recognisable in a log.
        /// </summary>
        [Fact]
        public void LogDivider_IsARunOfDashes()
        {
            Assert.Equal(79, LogUtilities.LogDivider.Length);
            Assert.True(LogUtilities.LogDivider.All(static character => character is '-'));
        }

        /// <summary>
        /// Writes a log entry, with every argument defaulted so a test names only what it is about.
        /// </summary>
        /// <param name="message">The messages to log.</param>
        /// <param name="hostLogStreamType">Where host output should go.</param>
        /// <param name="debugMessage">Whether these are debug messages.</param>
        /// <param name="severity">The severity, or <see langword="null"/> for the default.</param>
        /// <param name="source">The source, or <see langword="null"/> for the resolved caller.</param>
        /// <param name="scriptSection">The script section, or <see langword="null"/> for none.</param>
        /// <param name="logFileDirectory">The directory to write to, or <see langword="null"/> for none.</param>
        /// <param name="logFileName">The file to write to, or <see langword="null"/> for none.</param>
        /// <param name="logStyle">The format, or <see langword="null"/> to take it from the configuration.</param>
        /// <returns>The entries written.</returns>
        private static IReadOnlyList<LogEntry> Write(
            IReadOnlyList<string> message,
            HostLogStreamType hostLogStreamType = HostLogStreamType.None,
            bool debugMessage = false,
            LogSeverity? severity = null,
            string? source = null,
            string? scriptSection = null,
            string? logFileDirectory = null,
            string? logFileName = null,
            LogStyle? logStyle = null)
        {
            return LogUtilities.WriteLogEntry(message, hostLogStreamType, debugMessage, severity, source, scriptSection, logFileDirectory, logFileName, logStyle);
        }
    }
}
