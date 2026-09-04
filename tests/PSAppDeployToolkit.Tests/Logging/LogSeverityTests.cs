using System.Globalization;
using PSAppDeployToolkit.Logging;
using PSAppDeployToolkit.Tests.TestHelpers;
using Xunit;

namespace PSAppDeployToolkit.Tests.Logging
{
    /// <summary>
    /// Tests the severity values, which are a contract with something outside this codebase.
    /// </summary>
    /// <remarks>
    /// The numeric value of each is written into every CMTrace log line as its <c language="text">type</c> attribute, and OneTrace
    /// colours a line by reading it. So these are not internal identifiers that may be renumbered: inserting a member
    /// in the middle would silently recolour every log the toolkit has ever written, with nothing in this codebase
    /// noticing.
    /// <para>
    /// The rendering is asserted where it happens, in the log entry's own tests. What is asserted here is the numbering
    /// itself, at the declaration, because that is what a future edit would change.
    /// </para>
    /// </remarks>
    public sealed class LogSeverityTests
    {
        /// <summary>
        /// Verifies that each severity keeps the number OneTrace expects.
        /// </summary>
        /// <remarks>
        /// One to three are OneTrace's own informational, warning and error levels. Zero is the toolkit's addition for
        /// success, which OneTrace renders as informational because it knows nothing of it.
        /// </remarks>
        [Fact]
        public void LogSeverity_KeepsTheNumbersOneTraceExpects()
        {
            Assert.Equal(0, (int)LogSeverity.Success);
            Assert.Equal(1, (int)LogSeverity.Info);
            Assert.Equal(2, (int)LogSeverity.Warning);
            Assert.Equal(3, (int)LogSeverity.Error);
        }

        /// <summary>
        /// Verifies that nothing has been added, so the set stays what the log format can express.
        /// </summary>
        [Fact]
        public void LogSeverity_DeclaresNothingElse()
        {
            Assert.Equal(["Success", "Info", "Warning", "Error"], EnumValues.DeclaredNames<LogSeverity>());
        }

        /// <summary>
        /// Verifies that every severity renders as a number the log format can carry.
        /// </summary>
        /// <remarks>
        /// The log line casts to an unsigned type, so a negative value would render as something enormous rather than
        /// failing. This is what stops that being possible.
        /// </remarks>
        [Fact]
        public void LogSeverity_RendersAsASmallPositiveNumber()
        {
            foreach (LogSeverity severity in EnumValues.Declared<LogSeverity>())
            {
                Assert.Equal(((int)severity).ToString(CultureInfo.InvariantCulture), ((uint)severity).ToString(CultureInfo.InvariantCulture));
            }
        }
    }
}
