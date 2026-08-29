using System;
using System.Collections.Generic;
using System.Text;
using PSADT.Utilities;
using Xunit;

namespace PSADT.Tests.Utilities
{
    /// <summary>
    /// Tests the line trimming and base64 decoding helpers.
    /// </summary>
    /// <remarks>
    /// The line trimming is what <c language="csharp">ProcessResult</c> puts every captured output stream through, so its
    /// exact behaviour decides what a caller sees from a launched process. Three things are easy to get
    /// wrong and are covered deliberately: interior blank lines must survive, trailing whitespace is
    /// stripped per line while leading whitespace is not, and the string overload re-joins with the
    /// platform separator regardless of what it split on.
    /// </remarks>
    public sealed class MiscUtilitiesTests
    {
        /// <summary>
        /// Verifies that blank lines are removed from the start and the end of the sequence.
        /// </summary>
        [Fact]
        public void TrimLeadingTrailingLines_RemovesBlankLinesFromBothEnds()
        {
            Assert.Equal(["first", "last"], MiscUtilities.TrimLeadingTrailingLines(["", "  ", "first", "last", "\t", ""]));
        }

        /// <summary>
        /// Verifies that a blank line between two non-blank lines is kept, since it is content rather
        /// than padding.
        /// </summary>
        [Fact]
        public void TrimLeadingTrailingLines_KeepsInteriorBlankLines()
        {
            Assert.Equal(["first", "", "last"], MiscUtilities.TrimLeadingTrailingLines(["first", "   ", "last"]));
        }

        /// <summary>
        /// Verifies that each line has its trailing whitespace removed but keeps its leading whitespace,
        /// so indented output such as a stack trace stays readable.
        /// </summary>
        [Fact]
        public void TrimLeadingTrailingLines_TrimsTrailingWhitespaceButNotLeading()
        {
            Assert.Equal(["    indented", "trailing"], MiscUtilities.TrimLeadingTrailingLines(["    indented   ", "trailing\t "]));
        }

        /// <summary>
        /// Verifies that a sequence with nothing but blank lines collapses to nothing rather than to a
        /// single empty line.
        /// </summary>
        /// <param name="lines">The all-blank input.</param>
        [Theory]
        [MemberData(nameof(AllBlankSequences))]
        public void TrimLeadingTrailingLines_CollapsesAnAllBlankSequenceToEmpty(string[] lines)
        {
            Assert.Empty(MiscUtilities.TrimLeadingTrailingLines(lines));
        }

        /// <summary>
        /// Verifies that the result is a snapshot rather than a view over the caller's list, so a later
        /// change to the input cannot alter a result already handed out.
        /// </summary>
        [Fact]
        public void TrimLeadingTrailingLines_DoesNotObserveLaterChangesToTheInput()
        {
            // Arrange
            List<string> lines = ["first", "last"];

            // Act
            IReadOnlyList<string> trimmed = MiscUtilities.TrimLeadingTrailingLines(lines);
            lines.Add("added");

            // Assert
            Assert.Equal(["first", "last"], trimmed);
        }

        /// <summary>
        /// Verifies that a null sequence is rejected rather than enumerated.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0191:Do not use the null-forgiving operator", Justification = "This is deliberate as part of unit testing.")]
        [Fact]
        public void TrimLeadingTrailingLines_RejectsANullSequence()
        {
            _ = Assert.Throws<ArgumentNullException>(static () => MiscUtilities.TrimLeadingTrailingLines((IEnumerable<string>)null!));
        }

        /// <summary>
        /// Verifies that the string overload splits on either line ending and re-joins with the
        /// platform's, which is what makes output captured from a process consistent regardless of what
        /// the process emitted.
        /// </summary>
        /// <param name="input">The text to trim.</param>
        /// <param name="expectedLines">The lines the result should consist of.</param>
        [Theory]
        [InlineData("first\r\nlast", new[] { "first", "last" })]
        [InlineData("first\nlast", new[] { "first", "last" })]
        [InlineData("first\r\nmiddle\nlast", new[] { "first", "middle", "last" })]
        [InlineData("\r\n\r\nfirst\r\nlast\r\n\r\n", new[] { "first", "last" })]
        [InlineData("\n\nfirst\nlast\n\n", new[] { "first", "last" })]
        [InlineData("first\r\n\r\nlast", new[] { "first", "", "last" })]
        [InlineData("only", new[] { "only" })]
        public void TrimLeadingTrailingLines_JoinsWithThePlatformLineEnding(string input, string[] expectedLines)
        {
            Assert.Equal(string.Join(Environment.NewLine, expectedLines), MiscUtilities.TrimLeadingTrailingLines(input));
        }

        /// <summary>
        /// Verifies that a lone carriage return is not treated as a line ending, so text using it as one
        /// arrives as a single line. This is deliberate rather than incidental: the split list names
        /// only the two endings a Windows process emits.
        /// </summary>
        [Fact]
        public void TrimLeadingTrailingLines_DoesNotSplitOnALoneCarriageReturn()
        {
            Assert.Equal("first\rlast", MiscUtilities.TrimLeadingTrailingLines("first\rlast"));
        }

        /// <summary>
        /// Verifies that text that is entirely blank becomes empty, and that an already-empty string is
        /// accepted rather than rejected as a missing argument.
        /// </summary>
        /// <param name="input">The blank input.</param>
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\r\n")]
        [InlineData("\r\n\r\n")]
        [InlineData("  \r\n\t\r\n  ")]
        public void TrimLeadingTrailingLines_ReducesBlankTextToEmpty(string input)
        {
            Assert.Equal(string.Empty, MiscUtilities.TrimLeadingTrailingLines(input));
        }

        /// <summary>
        /// Verifies that a null string is rejected rather than treated as empty.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0191:Do not use the null-forgiving operator", Justification = "This is deliberate as part of unit testing.")]
        [Fact]
        public void TrimLeadingTrailingLines_RejectsANullString()
        {
            _ = Assert.Throws<ArgumentNullException>(static () => MiscUtilities.TrimLeadingTrailingLines((string)null!));
        }

        /// <summary>
        /// Verifies that well-formed base64 decodes to the bytes it encodes.
        /// </summary>
        [Fact]
        public void GetBase64StringBytes_DecodesWellFormedInput()
        {
            // Arrange
            byte[] expected = Encoding.UTF8.GetBytes("PSAppDeployToolkit");

            // Act & Assert
            Assert.Equal(expected, MiscUtilities.GetBase64StringBytes(Convert.ToBase64String(expected)));
        }

        /// <summary>
        /// Verifies that malformed base64 yields null rather than throwing, which is the whole reason
        /// this wrapper exists in front of the framework method.
        /// </summary>
        /// <param name="input">The malformed input.</param>
        [Theory]
        [InlineData("!!!!")]
        [InlineData("not base64 at all")]
        [InlineData("QQ")]
        [InlineData("QQ===")]
        [InlineData("====")]
        public void GetBase64StringBytes_ReturnsNullForMalformedInput(string input)
        {
            Assert.Null(MiscUtilities.GetBase64StringBytes(input));
        }

        /// <summary>
        /// Verifies that an absent argument is rejected rather than reported as malformed, so a caller
        /// can tell a programming error from bad data.
        /// </summary>
        /// <param name="input">The blank input to reject.</param>
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\t")]
        public void GetBase64StringBytes_RejectsBlankInput(string input)
        {
            _ = Assert.Throws<ArgumentException>(() => MiscUtilities.GetBase64StringBytes(input));
        }

        /// <summary>
        /// Verifies that a null argument is rejected as absent rather than as malformed.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0191:Do not use the null-forgiving operator", Justification = "This is deliberate as part of unit testing.")]
        [Fact]
        public void GetBase64StringBytes_RejectsNull()
        {
            _ = Assert.Throws<ArgumentNullException>(static () => MiscUtilities.GetBase64StringBytes(null!));
        }

        /// <summary>
        /// Sequences consisting only of blank lines, in the several shapes a process can emit them.
        /// </summary>
        public static TheoryData<string[]> AllBlankSequences
        {
            get
            {
                TheoryData<string[]> data = [];
                data.Add([]);
                data.Add([string.Empty]);
                data.Add([string.Empty, string.Empty]);
                data.Add(["   ", "\t", string.Empty]);
                return data;
            }
        }
    }
}
