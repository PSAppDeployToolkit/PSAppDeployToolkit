using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using PSADT.Utilities;
using Xunit;

namespace PSADT.Tests.Utilities
{
    /// <summary>
    /// Tests the environment variable helpers.
    /// </summary>
    /// <remarks>
    /// Only the process scope is written to. A process-scoped variable lives and dies with the test host,
    /// so setting one changes nothing that outlasts the run; the user and machine scopes are registry
    /// writes and are never performed here.
    /// <para>
    /// The validation the user and machine scopes perform is still covered, because every one of those
    /// checks runs before anything is written. Each of those tests confirms afterwards that nothing was
    /// left behind, so a reordering that let a write slip through would be caught rather than silently
    /// altering the machine.
    /// </para>
    /// <para>
    /// The oracle throughout is <see cref="Environment.GetEnvironmentVariables()"/> rather than the
    /// single-variable accessor, which this repository bans in favour of the wrapper under test.
    /// </para>
    /// </remarks>
    public sealed class EnvironmentUtilitiesTests
    {
        /// <summary>
        /// Verifies that a variable set in this process is readable again, and agrees with what the
        /// framework reports for the same process.
        /// </summary>
        [Fact]
        public void SetEnvironmentVariable_RoundTripsThroughTheProcessScope()
        {
            // Arrange
            string name = NewVariableName();
            try
            {
                // Act
                EnvironmentUtilities.SetEnvironmentVariable(name, "a value");

                // Assert
                Assert.Equal("a value", EnvironmentUtilities.GetEnvironmentVariable(name));
                Assert.Equal("a value", Environment.GetEnvironmentVariables()[name]);
            }
            finally
            {
                Environment.SetEnvironmentVariable(name, value: null);
            }
        }

        /// <summary>
        /// Verifies that a variable that was never set reads back as absent rather than as empty.
        /// </summary>
        [Fact]
        public void GetEnvironmentVariable_ReturnsNullForAVariableThatIsNotSet()
        {
            Assert.Null(EnvironmentUtilities.GetEnvironmentVariable(NewVariableName()));
        }

        /// <summary>
        /// Verifies that a variable holding nothing but whitespace reads back as absent, so a caller
        /// cannot mistake a blank value for a real one.
        /// </summary>
        /// <remarks>
        /// The wrapper exists for this. The framework reports a whitespace-valued variable as a string of
        /// spaces, which reads as set; the wrapper reports it as absent, which is what a caller deciding
        /// whether a variable is configured actually wants to know.
        /// </remarks>
        [Fact]
        public void GetEnvironmentVariable_TreatsABlankValueAsAbsent()
        {
            // Arrange
            string name = NewVariableName();
            try
            {
                // Act: set through the framework, since the wrapper refuses to write a blank value
                Environment.SetEnvironmentVariable(name, "   ");

                // Assert: the framework sees it, the wrapper reports it as unset
                Assert.Equal("   ", Environment.GetEnvironmentVariables()[name]);
                Assert.Null(EnvironmentUtilities.GetEnvironmentVariable(name));
            }
            finally
            {
                Environment.SetEnvironmentVariable(name, value: null);
            }
        }

        /// <summary>
        /// Verifies that removing a variable leaves it unset.
        /// </summary>
        [Fact]
        public void RemoveEnvironmentVariable_UnsetsTheVariable()
        {
            // Arrange
            string name = NewVariableName();
            try
            {
                EnvironmentUtilities.SetEnvironmentVariable(name, "a value");
                Assert.NotNull(EnvironmentUtilities.GetEnvironmentVariable(name));

                // Act
                EnvironmentUtilities.RemoveEnvironmentVariable(name);

                // Assert
                Assert.Null(EnvironmentUtilities.GetEnvironmentVariable(name));
                Assert.False(Environment.GetEnvironmentVariables().Contains(name));
            }
            finally
            {
                Environment.SetEnvironmentVariable(name, value: null);
            }
        }

        /// <summary>
        /// Verifies that setting a variable to nothing removes it, which is how the framework's own
        /// accessor behaves and what a caller clearing a value expects.
        /// </summary>
        [Fact]
        public void SetEnvironmentVariable_RemovesTheVariableWhenGivenNoValue()
        {
            // Arrange
            string name = NewVariableName();
            try
            {
                EnvironmentUtilities.SetEnvironmentVariable(name, "a value");

                // Act
                EnvironmentUtilities.SetEnvironmentVariable(name, value: null);

                // Assert
                Assert.Null(EnvironmentUtilities.GetEnvironmentVariable(name));
            }
            finally
            {
                Environment.SetEnvironmentVariable(name, value: null);
            }
        }

        /// <summary>
        /// Verifies that a value with no content is refused, since writing one produces a variable that
        /// reads back as unset and is therefore never what the caller meant.
        /// </summary>
        /// <param name="value">The blank value to refuse.</param>
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\t")]
        public void SetEnvironmentVariable_RefusesABlankValue(string value)
        {
            _ = Assert.Throws<ArgumentException>(() => EnvironmentUtilities.SetEnvironmentVariable(NewVariableName(), value));
        }

        /// <summary>
        /// Verifies that a variable with no name is refused.
        /// </summary>
        /// <param name="name">The blank name to refuse.</param>
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void SetEnvironmentVariable_RefusesABlankName(string name)
        {
            _ = Assert.Throws<ArgumentException>(() => EnvironmentUtilities.SetEnvironmentVariable(name, "a value"));
            _ = Assert.Throws<ArgumentException>(() => EnvironmentUtilities.RemoveEnvironmentVariable(name));
        }

        /// <summary>
        /// Verifies that the process scope is handled by the simple path even when the elaborate overload
        /// is used, so the appending and removing options do not apply to it.
        /// </summary>
        [Fact]
        public void SetEnvironmentVariable_HandlesTheProcessScopeDirectly()
        {
            // Arrange
            string name = NewVariableName();
            try
            {
                // Act: options that the other scopes would refuse together are ignored for this one
                EnvironmentUtilities.SetEnvironmentVariable(name, "a value", EnvironmentVariableTarget.Process, expandable: true, append: true, remove: true);

                // Assert
                Assert.Equal("a value", EnvironmentUtilities.GetEnvironmentVariable(name));
            }
            finally
            {
                Environment.SetEnvironmentVariable(name, value: null);
            }
        }

        /// <summary>
        /// Verifies that appending and removing at once is refused for a persisted scope, since the two
        /// ask for opposite things.
        /// </summary>
        [Fact]
        public void SetEnvironmentVariable_RefusesToAppendAndRemoveAtOnce()
        {
            // Arrange
            string name = NewVariableName();

            // Act & Assert
            _ = Assert.Throws<NotSupportedException>(() => EnvironmentUtilities.SetEnvironmentVariable(name, "a value", EnvironmentVariableTarget.User, expandable: false, append: true, remove: true));
            AssertNothingWasPersisted(name);
        }

        /// <summary>
        /// Verifies that a name longer than the environment block allows is refused before anything is
        /// written.
        /// </summary>
        [Fact]
        public void SetEnvironmentVariable_RefusesAnOverlongName()
        {
            // Arrange
            string name = new('X', 1_025);

            // Act & Assert
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => EnvironmentUtilities.SetEnvironmentVariable(name, "a value", EnvironmentVariableTarget.User, expandable: false, append: false, remove: false));
            AssertNothingWasPersisted(name);
        }

        /// <summary>
        /// Verifies that a name containing the separator between name and value is refused, since it
        /// could not be read back.
        /// </summary>
        [Fact]
        public void SetEnvironmentVariable_RefusesANameContainingAnEqualsSign()
        {
            // Arrange
            string name = $"{NewVariableName()}=EMBEDDED";

            // Act & Assert
            _ = Assert.Throws<FormatException>(() => EnvironmentUtilities.SetEnvironmentVariable(name, "a value", EnvironmentVariableTarget.User, expandable: false, append: false, remove: false));
            AssertNothingWasPersisted(name);
        }

        /// <summary>
        /// Verifies that a blank value is refused for a persisted scope too, before anything is written.
        /// </summary>
        [Fact]
        public void SetEnvironmentVariable_RefusesABlankValueForAPersistedScope()
        {
            // Arrange
            string name = NewVariableName();

            // Act & Assert
            _ = Assert.Throws<ArgumentException>(() => EnvironmentUtilities.SetEnvironmentVariable(name, "   ", EnvironmentVariableTarget.User, expandable: false, append: false, remove: false));
            AssertNothingWasPersisted(name);
        }

        /// <summary>
        /// Verifies that a scope outside the defined set is refused rather than quietly treated as one of
        /// them.
        /// </summary>
        [Fact]
        public void SetEnvironmentVariable_RefusesAnUndefinedScope()
        {
            // Arrange
            string name = NewVariableName();

            // Act & Assert
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => EnvironmentUtilities.SetEnvironmentVariable(name, "a value", (EnvironmentVariableTarget)99, expandable: false, append: false, remove: false));
            AssertNothingWasPersisted(name);
        }

        /// <summary>
        /// Verifies that a variable reference is expanded against the current process.
        /// </summary>
        [Fact]
        public void ExpandEnvironmentVariables_ExpandsAReference()
        {
            // Arrange
            string name = NewVariableName();
            try
            {
                EnvironmentUtilities.SetEnvironmentVariable(name, "expanded");

                // Act & Assert
                Assert.Equal("before expanded after", EnvironmentUtilities.ExpandEnvironmentVariables($"before %{name}% after"));
            }
            finally
            {
                Environment.SetEnvironmentVariable(name, value: null);
            }
        }

        /// <summary>
        /// Verifies that a reference to a variable that is not set is left alone rather than removed,
        /// which is the expander's own behaviour and matters to a caller passing a path through.
        /// </summary>
        [Fact]
        public void ExpandEnvironmentVariables_LeavesAnUnsetReferenceAlone()
        {
            // Arrange
            string name = NewVariableName();

            // Act & Assert
            Assert.Equal($"%{name}%", EnvironmentUtilities.ExpandEnvironmentVariables($"%{name}%"));
        }

        /// <summary>
        /// Verifies that text with nothing to expand comes back unchanged.
        /// </summary>
        [Fact]
        public void ExpandEnvironmentVariables_LeavesPlainTextAlone()
        {
            Assert.Equal(@"C:\Program Files\App", EnvironmentUtilities.ExpandEnvironmentVariables(@"C:\Program Files\App"));
        }

        /// <summary>
        /// Verifies that blank input is refused rather than expanded to nothing.
        /// </summary>
        /// <param name="name">The blank input to refuse.</param>
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void ExpandEnvironmentVariables_RefusesBlankInput(string name)
        {
            _ = Assert.Throws<ArgumentException>(() => EnvironmentUtilities.ExpandEnvironmentVariables(name));
        }

        /// <summary>
        /// Verifies that the whole environment is readable, and holds the variables every process has.
        /// </summary>
        /// <remarks>
        /// The name is matched without regard to case rather than looked up directly. Windows treats
        /// environment variable names case-insensitively, but the dictionary handed back does not agree
        /// across the two target frameworks: under .NET Framework it compares keys without regard to case,
        /// and under .NET it compares them exactly. A process inherits whatever casing its parent used -
        /// a shell that spells it <c>SYSTEMROOT</c> passes that on - so a direct lookup would be asserting
        /// the casing of whatever launched the run.
        /// </remarks>
        [Fact]
        public void GetEnvironmentVariables_ReadsTheProcessEnvironment()
        {
            // Act
            IDictionary variables = EnvironmentUtilities.GetEnvironmentVariables();

            // Assert
            Assert.NotEmpty(variables);
            Assert.Contains(variables.Keys.Cast<string>(), static name => name.Equals("SystemRoot", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Confirms that a refused write left nothing behind in the persisted scope.
        /// </summary>
        /// <param name="name">The variable that was refused.</param>
        private static void AssertNothingWasPersisted(string name)
        {
            Assert.False(
                Environment.GetEnvironmentVariables(EnvironmentVariableTarget.User).Contains(name),
                $"A refused write left '{name}' behind in the user environment.");
        }

        /// <summary>
        /// Produces a variable name nothing else on the machine will be using.
        /// </summary>
        /// <returns>A unique variable name.</returns>
        private static string NewVariableName()
        {
            return $"PSADT_TESTS_{Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture)}";
        }
    }
}
