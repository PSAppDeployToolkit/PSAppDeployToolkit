using System;
using System.Globalization;
using PSAppDeployToolkit.Logging;
using Xunit;

namespace PSAppDeployToolkit.Tests.Logging
{
    /// <summary>
    /// Tests the record that carries one log entry from the point it is written to the point it is rendered.
    /// </summary>
    /// <remarks>
    /// Two things are being tested here and they pull in different directions. The rendered lines are a format
    /// two external readers depend on - OneTrace parses <see cref="LogEntry.CMTraceLogLine"/>, and the legacy
    /// line is what a text log has looked like since v3 - so those are asserted by their parts rather than by a
    /// whole string, which would only re-implement the format string and break on any machine in another
    /// timezone.
    /// <para>
    /// The equality tests are the other half, and they are the reason this file exists at all. This is a record,
    /// so it advertises that two entries describing the same message are the same entry. It did not honour that:
    /// <see cref="LogEntry.CallerFileName"/> was held as a <see cref="System.IO.FileInfo"/>, which compares by
    /// reference, so two entries built from identical arguments came out unequal.
    /// </para>
    /// </remarks>
    public sealed class LogEntryTests
    {
        /// <summary>
        /// Verifies that two entries built from identical arguments are equal and hash alike.
        /// </summary>
        /// <remarks>
        /// The case that was broken. Every argument here is a value, so a record holding them has no excuse for
        /// answering anything but equal - but a caller file name reached the record as a
        /// <see cref="System.IO.FileInfo"/>, and two of those built from one path are two objects.
        /// </remarks>
        [Fact]
        public void Equals_IsByTheValuesGiven()
        {
            // Arrange
            LogEntry first = NewLogEntry();
            LogEntry second = NewLogEntry();

            // Assert
            Assert.Equal(first, second);
            Assert.Equal(first.GetHashCode(), second.GetHashCode());
        }

        /// <summary>
        /// Verifies that the caller file name is part of the comparison, and compared by its path.
        /// </summary>
        /// <remarks>
        /// The counterpart to the test above: having stopped comparing by reference, it must not have stopped
        /// comparing at all. A backing field that was ignored rather than compared would pass that test and fail
        /// this one.
        /// </remarks>
        [Fact]
        public void Equals_TakesTheCallerFileNameIntoAccount()
        {
            Assert.NotEqual(NewLogEntry(callerFileName: @"C:\scripts\one.ps1"), NewLogEntry(callerFileName: @"C:\scripts\two.ps1"));
        }

        /// <summary>
        /// Verifies that entries differing in any one member are not equal.
        /// </summary>
        /// <remarks>
        /// One test rather than one per member, because the point is the set of members that counts rather than
        /// each member individually, and a member quietly dropped from the comparison is what this catches.
        /// </remarks>
        [Fact]
        public void Equals_TakesEveryMemberIntoAccount()
        {
            // Arrange
            LogEntry baseline = NewLogEntry();

            // Assert
            Assert.NotEqual(baseline, NewLogEntry(timeStamp: Timestamp.AddSeconds(1)));
            Assert.NotEqual(baseline, NewLogEntry(message: "a different message"));
            Assert.NotEqual(baseline, NewLogEntry(severity: LogSeverity.Error));
            Assert.NotEqual(baseline, NewLogEntry(source: "Another-Command"));
            Assert.NotEqual(baseline, NewLogEntry(scriptSection: "Uninstallation"));
            Assert.NotEqual(baseline, NewLogEntry(debugMessage: true));
            Assert.NotEqual(baseline, NewLogEntry(callerSource: "Another.Type.Method()"));
        }

        /// <summary>
        /// Verifies that an entry with no script section is equal to another with none, and unequal to one with.
        /// </summary>
        [Fact]
        public void Equals_TreatsAnAbsentScriptSectionAsAValue()
        {
            // Arrange
            LogEntry withoutSection = NewLogEntry(scriptSection: null);

            // Assert
            Assert.Equal(withoutSection, NewLogEntry(scriptSection: null));
            Assert.NotEqual(withoutSection, NewLogEntry(scriptSection: "Installation"));
        }

        /// <summary>
        /// Verifies that an entry whose caller file name was unavailable compares equal to another such entry.
        /// </summary>
        /// <remarks>
        /// A caller name in angle brackets is how a caller with no file - a script block, or a frame the runtime
        /// could not resolve - arrives, and it makes the property null. That case was the only one that ever
        /// compared correctly, since two nulls are equal, so it is worth keeping a test on it.
        /// </remarks>
        [Fact]
        public void Equals_HandlesAnUnavailableCallerFileName()
        {
            // Arrange
            LogEntry first = NewLogEntry(callerFileName: "<Unavailable>");

            // Assert
            Assert.Null(first.CallerFileName);
            Assert.Equal(first, NewLogEntry(callerFileName: "<Unavailable>"));
            Assert.NotEqual(first, NewLogEntry());
        }

        /// <summary>
        /// Verifies that the caller file name is exposed as the path it was given.
        /// </summary>
        /// <remarks>
        /// Guards the shape of the fix as well as its effect: whatever the record holds internally, callers still
        /// get a file back, and it still points where they said.
        /// </remarks>
        [Fact]
        public void CallerFileName_IsThePathItWasGiven()
        {
            Assert.Equal(@"C:\scripts\deploy.ps1", NewLogEntry(callerFileName: @"C:\scripts\deploy.ps1").CallerFileName?.FullName);
        }

        /// <summary>
        /// Verifies that each read of the caller file name is a usable file, and that mutating one does not
        /// disturb the entry.
        /// </summary>
        /// <remarks>
        /// A <see cref="System.IO.FileInfo"/> is mutable - it can be refreshed, moved or deleted through - so
        /// handing out one shared instance would let a caller alter what the entry reports. Rebuilding it per
        /// read is what keeps the record immutable in practice as well as on paper.
        /// </remarks>
        [Fact]
        public void CallerFileName_IsNotSharedBetweenReads()
        {
            // Arrange
            LogEntry entry = NewLogEntry();

            // Assert
            Assert.NotSame(entry.CallerFileName, entry.CallerFileName);
            Assert.Equal(entry.CallerFileName!.FullName, entry.CallerFileName!.FullName);
        }

        /// <summary>
        /// Verifies that a missing or blank message is refused.
        /// </summary>
        /// <param name="message">The message the constructor should refuse.</param>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void LogEntry_RefusesABlankMessage(string? message)
        {
            _ = Assert.ThrowsAny<ArgumentException>(() => NewLogEntry(message: message!));
        }

        /// <summary>
        /// Verifies that a missing or blank source is refused.
        /// </summary>
        /// <param name="source">The source the constructor should refuse.</param>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void LogEntry_RefusesABlankSource(string? source)
        {
            _ = Assert.ThrowsAny<ArgumentException>(() => NewLogEntry(source: source!));
        }

        /// <summary>
        /// Verifies that a missing or blank caller file name is refused.
        /// </summary>
        /// <param name="callerFileName">The caller file name the constructor should refuse.</param>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void LogEntry_RefusesABlankCallerFileName(string? callerFileName)
        {
            _ = Assert.ThrowsAny<ArgumentException>(() => NewLogEntry(callerFileName: callerFileName!));
        }

        /// <summary>
        /// Verifies that a missing or blank caller source is refused.
        /// </summary>
        /// <param name="callerSource">The caller source the constructor should refuse.</param>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void LogEntry_RefusesABlankCallerSource(string? callerSource)
        {
            _ = Assert.ThrowsAny<ArgumentException>(() => NewLogEntry(callerSource: callerSource!));
        }

        /// <summary>
        /// Verifies that a script section is optional but must say something if given.
        /// </summary>
        /// <remarks>
        /// The one argument where null and blank are treated differently: null means the caller supplied no
        /// section, whereas a blank one is a caller who meant to supply one and got it wrong.
        /// </remarks>
        [Fact]
        public void LogEntry_AllowsNoScriptSectionButRefusesABlankOne()
        {
            Assert.Null(NewLogEntry(scriptSection: null).ScriptSection);
            _ = Assert.ThrowsAny<ArgumentException>(static () => NewLogEntry(scriptSection: string.Empty));
            _ = Assert.ThrowsAny<ArgumentException>(static () => NewLogEntry(scriptSection: "   "));
        }

        /// <summary>
        /// Verifies that null characters are removed from the message and trailing whitespace trimmed.
        /// </summary>
        /// <remarks>
        /// Both matter to the rendered line rather than to the caller: a null character terminates the string
        /// early for some readers, and trailing whitespace shows up as a ragged log.
        /// </remarks>
        [Fact]
        public void Message_HasNullCharactersRemovedAndIsTrimmedAtTheEnd()
        {
            Assert.Equal("before after", NewLogEntry(message: "before\0 after\0").Message);
            Assert.Equal("  leading kept", NewLogEntry(message: "  leading kept   \t ").Message);
        }

        /// <summary>
        /// Verifies that the legacy line carries the timestamp, section, source, severity and message.
        /// </summary>
        [Fact]
        public void LegacyLogLine_CarriesEveryPartOfTheEntry()
        {
            // Arrange
            LogEntry entry = NewLogEntry();

            // Assert
            Assert.Equal(
                "[2026-03-04T05:06:07.0890000] [Installation] [Test-Command] [Info] :: a message",
                entry.LegacyLogLine);
        }

        /// <summary>
        /// Verifies that the legacy line omits the section when there is none, rather than leaving a gap.
        /// </summary>
        [Fact]
        public void LegacyLogLine_OmitsAnAbsentScriptSection()
        {
            Assert.Equal(
                "[2026-03-04T05:06:07.0890000] [Test-Command] [Info] :: a message",
                NewLogEntry(scriptSection: null).LegacyLogLine);
        }

        /// <summary>
        /// Verifies that <see cref="LogEntry.ToString"/> is the legacy line.
        /// </summary>
        [Fact]
        public void ToString_IsTheLegacyLine()
        {
            // Arrange
            LogEntry entry = NewLogEntry();

            // Assert
            Assert.Equal(entry.LegacyLogLine, entry.ToString());
        }

        /// <summary>
        /// Verifies that the CMTrace line carries the attributes OneTrace reads, with the severity as its number.
        /// </summary>
        /// <remarks>
        /// The severity is asserted as a number on purpose. OneTrace colours a line by the <c language="text">type</c> attribute,
        /// so the numeric values of <see cref="LogSeverity"/> are an external contract rather than an internal
        /// detail, and this is where that contract is actually exercised.
        /// </remarks>
        [Fact]
        public void CMTraceLogLine_CarriesTheAttributesOneTraceReads()
        {
            // Arrange
            LogEntry entry = NewLogEntry(severity: LogSeverity.Warning);

            // Assert
            Assert.StartsWith("<![LOG[", entry.CMTraceLogLine, StringComparison.Ordinal);
            Assert.Contains("]LOG]!>", entry.CMTraceLogLine, StringComparison.Ordinal);
            Assert.Contains("component=\"Test-Command\"", entry.CMTraceLogLine, StringComparison.Ordinal);
            Assert.Contains($"type=\"{((uint)LogSeverity.Warning).ToString(CultureInfo.InvariantCulture)}\"", entry.CMTraceLogLine, StringComparison.Ordinal);
            Assert.Contains($"file=\"{CallerFile}\"", entry.CMTraceLogLine, StringComparison.Ordinal);
            Assert.Contains("date=\"3-04-2026\"", entry.CMTraceLogLine, StringComparison.Ordinal);
            Assert.Contains("time=\"05:06:07.089", entry.CMTraceLogLine, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that the CMTrace line prefixes the message with the script section.
        /// </summary>
        [Fact]
        public void CMTraceLogLine_PrefixesTheMessageWithTheScriptSection()
        {
            Assert.Contains("<![LOG[[Installation] :: a message]LOG]!>", NewLogEntry().CMTraceLogLine, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that the section prefix is left off a divider.
        /// </summary>
        /// <remarks>
        /// A divider is a run of dashes used to separate one deployment's log from the next, and prefixing it
        /// with a section would push the dashes out of alignment. This is the one message treated specially.
        /// </remarks>
        [Fact]
        public void CMTraceLogLine_DoesNotPrefixADivider()
        {
            Assert.Contains($"<![LOG[{LogUtilities.LogDivider}]LOG]!>", NewLogEntry(message: LogUtilities.LogDivider).CMTraceLogLine, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that a multi-line message keeps its indentation, using a character OneTrace does not trim,
        /// and ends with a newline.
        /// </summary>
        /// <remarks>
        /// OneTrace trims ordinary leading whitespace, which would flatten an indented block into one column. The
        /// punctuation space at U+2008 is whitespace to .NET but not to OneTrace, so leading runs are replaced
        /// with it and the indentation survives. The trailing newline is what makes OneTrace render the block as
        /// multiple lines at all.
        /// </remarks>
        [Fact]
        public void CMTraceLogLine_PadsIndentedLinesWithAPunctuationSpace()
        {
            // Arrange
            LogEntry entry = NewLogEntry(message: "first\n    indented\n\nlast");

            // Act: the payload between the LOG markers.
            string payload = entry.CMTraceLogLine.Split(["<![LOG[", "]LOG]!>"], StringSplitOptions.None)[1];
            string[] lines = payload.Split([Environment.NewLine], StringSplitOptions.None);

            // Assert: the indent is punctuation spaces rather than ordinary ones, the blank line became a single
            // punctuation space, and the block is newline-terminated.
            Assert.Equal("[Installation] :: first", lines[0]);
            Assert.Equal($"{new string(PunctuationSpace, 4)}indented", lines[1]);
            Assert.Equal(PunctuationSpace.ToString(), lines[2]);
            Assert.Equal("last", lines[3]);
            Assert.Equal(string.Empty, lines[4]);
        }

        /// <summary>
        /// Verifies that a message split over Windows line endings is handled the same as one split over bare
        /// newlines.
        /// </summary>
        [Fact]
        public void CMTraceLogLine_TreatsBothLineEndingsAlike()
        {
            Assert.Equal(
                NewLogEntry(message: "first\n  second").CMTraceLogLine,
                NewLogEntry(message: "first\r\n  second").CMTraceLogLine);
        }

        /// <summary>
        /// Verifies that a single-line message is not given a trailing newline it did not ask for.
        /// </summary>
        [Fact]
        public void CMTraceLogLine_LeavesASingleLineMessageAlone()
        {
            Assert.DoesNotContain(Environment.NewLine, NewLogEntry().CMTraceLogLine, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that every property reports what the entry was built from.
        /// </summary>
        /// <remarks>
        /// One test over the plain properties rather than one each, since there is nothing to say about them
        /// individually beyond that the constructor puts each argument where it belongs.
        /// </remarks>
        [Fact]
        public void LogEntry_ReportsWhatItWasBuiltFrom()
        {
            // Arrange
            LogEntry entry = NewLogEntry(severity: LogSeverity.Success, debugMessage: true);

            // Assert
            Assert.Equal(Timestamp, entry.Timestamp);
            Assert.Equal("a message", entry.Message);
            Assert.Equal(LogSeverity.Success, entry.Severity);
            Assert.Equal("Test-Command", entry.Source);
            Assert.Equal("Installation", entry.ScriptSection);
            Assert.True(entry.DebugMessage);
            Assert.Equal(CallerFile, entry.CallerFileName?.FullName);
            Assert.Equal("Some.Type.Method()", entry.CallerSource);
        }

        /// <summary>
        /// Builds an entry, with every argument defaulted so a test can name only what it is about.
        /// </summary>
        /// <param name="timeStamp">The entry's timestamp.</param>
        /// <param name="message">The message.</param>
        /// <param name="severity">The severity.</param>
        /// <param name="source">The source.</param>
        /// <param name="scriptSection">The script section, or <see langword="null"/> for none.</param>
        /// <param name="debugMessage">Whether it is a debug message.</param>
        /// <param name="callerFileName">The caller's file name.</param>
        /// <param name="callerSource">The caller's source.</param>
        /// <returns>The entry.</returns>
        private static LogEntry NewLogEntry(
            DateTime? timeStamp = null,
            string message = "a message",
            LogSeverity severity = LogSeverity.Info,
            string source = "Test-Command",
            string? scriptSection = "Installation",
            bool debugMessage = false,
            string callerFileName = CallerFile,
            string callerSource = "Some.Type.Method()")
        {
            return new LogEntry(timeStamp ?? Timestamp, message, severity, source, scriptSection, debugMessage, callerFileName, callerSource);
        }

        /// <summary>
        /// A fixed timestamp, so that a rendered line is the same on every run.
        /// </summary>
        /// <remarks>
        /// Deliberately of unspecified kind. The round-trip format the legacy line uses appends a UTC offset
        /// for a local time, which would make the expected line depend on the machine's timezone.
        /// </remarks>
        private static readonly DateTime Timestamp = new(2026, 3, 4, 5, 6, 7, 89, DateTimeKind.Unspecified);

        /// <summary>
        /// The caller file name the entries are built with.
        /// </summary>
        private const string CallerFile = @"C:\scripts\deploy.ps1";

        /// <summary>
        /// The punctuation space the CMTrace line pads with, which .NET calls whitespace and OneTrace does not.
        /// </summary>
        private const char PunctuationSpace = '\x2008';
    }
}
