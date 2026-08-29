using System;
using System.IO;
using Xunit;

namespace PSADT.Interop.Tests.Polyfills
{
    /// <summary>
    /// Tests the Path polyfills: Join over an array of segments, and IsPathFullyQualified over both the
    /// string and span overloads. These are pure path arithmetic and touch no filesystem.
    /// </summary>
    public sealed class PathPolyfillTests
    {
        /// <summary>
        /// Verifies that segments are joined with exactly one separator, that an existing separator on
        /// either side of a boundary is not doubled, and that null or empty segments are skipped without
        /// leaving a stray separator behind.
        /// </summary>
        /// <param name="segments">The segments to join.</param>
        /// <param name="expected">The expected result.</param>
        [Theory]
        [InlineData(new[] { "a", "b" }, @"a\b")]
        [InlineData(new[] { @"a\", "b" }, @"a\b")]
        [InlineData(new[] { "a", @"\b" }, @"a\b")]
        [InlineData(new[] { @"a\", @"\b" }, @"a\\b")]
        [InlineData(new[] { "a/", "b" }, "a/b")]
        [InlineData(new[] { @"C:\dir", "file.txt" }, @"C:\dir\file.txt")]
        [InlineData(new[] { "a", "", "b" }, @"a\b")]
        [InlineData(new[] { "a", null, "b" }, @"a\b")]
        [InlineData(new[] { "", "", "" }, "")]
        [InlineData(new string[0], "")]
        [InlineData(new[] { "only" }, "only")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "MA0109:Consider adding an overload with a Span<T> or Memory<T>", Justification = "The parameter shape is dictated by the InlineData cases under test.")]
        public void Join_MatchesFrameworkBehaviour(string?[] segments, string expected)
        {
            // Act
            string result = Path.Join(segments);

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Verifies that a null array is rejected rather than dereferenced.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0191:Do not use the null-forgiving operator", Justification = "This is deliberate as part of unit testing.")]
        [Fact]
        public void Join_ThrowsOnNullArray()
        {
            _ = Assert.Throws<ArgumentNullException>(static () => Path.Join(null!));
        }

        /// <summary>
        /// Verifies which paths count as fully qualified on Windows: a drive letter followed by a
        /// separator, a UNC root, and a device path. Rooted-but-drive-relative and relative paths do not
        /// qualify, which is the distinction callers rely on.
        /// </summary>
        /// <param name="path">The path to classify.</param>
        /// <param name="expected">Whether the path is expected to be fully qualified.</param>
        [Theory]
        [InlineData(@"C:\dir", true)]
        [InlineData("C:/dir", true)]
        [InlineData(@"c:\", true)]
        [InlineData(@"\\server\share", true)]
        [InlineData("//server/share", true)]
        [InlineData(@"\\?\C:\dir", true)]
        [InlineData(@"\\.\device", true)]
        [InlineData("C:dir", false)]
        [InlineData("C:", false)]
        [InlineData(@"\dir", false)]
        [InlineData("/dir", false)]
        [InlineData(@"dir\file", false)]
        [InlineData("file.txt", false)]
        [InlineData(".", false)]
        [InlineData("", false)]
        public void IsPathFullyQualified_String_MatchesFrameworkBehaviour(string path, bool expected)
        {
            // Act
            bool result = Path.IsPathFullyQualified(path);

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Verifies that the span overload classifies paths the same way as the string overload, since
        /// callers use them interchangeably.
        /// </summary>
        /// <param name="path">The path to classify.</param>
        /// <param name="expected">Whether the path is expected to be fully qualified.</param>
        [Theory]
        [InlineData(@"C:\dir", true)]
        [InlineData(@"\\server\share", true)]
        [InlineData(@"\\?\C:\dir", true)]
        [InlineData("C:dir", false)]
        [InlineData(@"\dir", false)]
        [InlineData("file.txt", false)]
        [InlineData("", false)]
        public void IsPathFullyQualified_Span_MatchesStringOverload(string path, bool expected)
        {
            // Act
            bool result = Path.IsPathFullyQualified(path.AsSpan());

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Verifies that a null path is rejected rather than dereferenced.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0191:Do not use the null-forgiving operator", Justification = "This is deliberate as part of unit testing.")]
        [Fact]
        public void IsPathFullyQualified_ThrowsOnNullPath()
        {
            _ = Assert.Throws<ArgumentNullException>(static () => Path.IsPathFullyQualified(null!));
        }

        /// <summary>
        /// Verifies that a drive-shaped path whose drive letter is not a letter is judged not fully
        /// qualified rather than throwing.
        /// </summary>
        /// <remarks>
        /// The polyfill's drive-letter check is written as (uint)((value | 0x20) - 'a') &lt;= 'z' - 'a',
        /// which relies on that subtraction wrapping when the character sorts below 'a'. It is not wrapped
        /// in unchecked, so under this repository's CheckForOverflowUnderflow setting the conversion throws.
        /// Reachable from production code: CommandLineUtilities parses "-D=&lt;path&gt;" arguments and hands
        /// the remainder to the span overload, so "-D=1:\dir" throws where it should return false.
        /// </remarks>
        /// <param name="path">The drive-shaped path whose drive letter is not a letter.</param>
        [Theory]
        [InlineData(@"1:\dir")]
        [InlineData(@"!:\dir")]
        [InlineData(@" :\dir")]
        public void IsPathFullyQualified_NonLetterDriveCharacter(string path)
        {
            // Assert
            Assert.False(Path.IsPathFullyQualified(path));
            Assert.False(Path.IsPathFullyQualified(path.AsSpan()));
        }
    }
}
