using System;
using System.Globalization;
using System.Management.Automation;
using PSAppDeployToolkit.Attributes;
using PSAppDeployToolkit.Tests.TestHelpers;
using Xunit;

namespace PSAppDeployToolkit.Tests.Attributes
{
    /// <summary>
    /// Tests the attribute that turns what a caller typed into a <see cref="TimeSpan"/>.
    /// </summary>
    /// <remarks>
    /// The attribute exists because PowerShell's own conversion reads a bare number as ticks, which is never what a
    /// caller means by a timeout. It is applied to around twenty public parameters - the countdowns in
    /// Show-ADTInstallationWelcome, Start-ADTProcess's timeouts, Send-ADTKeys, Set-ADTDeferHistory - so what it does
    /// with each shape of input is a public contract.
    /// <para>
    /// Numbers are read invariantly and mean seconds. That is asserted under several cultures set explicitly, because
    /// the answer must not depend on the machine: a deployment script has to mean the same thing on a German build
    /// agent as on an Australian workstation.
    /// </para>
    /// </remarks>
    public sealed class TimeSpanTransformationAttributeTests
    {
        /// <summary>
        /// Verifies that a numeric string is read as seconds.
        /// </summary>
        /// <remarks>
        /// This was the bug. <see cref="TimeSpan.TryParse(string, out TimeSpan)"/> accepts a bare integer and reads it
        /// as whole days, and it used to run before the numeric fallbacks - so <c language="powershell">-CloseProcessesCountdown '90'</c> meant
        /// ninety days while <c language="powershell">-CloseProcessesCountdown 90</c> meant ninety seconds, a factor of 86,400 apart with no
        /// error either way.
        /// </remarks>
        /// <param name="input">The string the caller supplied.</param>
        /// <param name="expectedSeconds">The number of seconds it should mean.</param>
        [Theory]
        [InlineData("10", 10)]
        [InlineData("90", 90)]
        [InlineData("3300", 3300)]
        [InlineData("10.5", 10.5)]
        [InlineData("0", 0)]
        [InlineData("-5", -5)]
        [InlineData("1e3", 1000)]
        [InlineData(" 42 ", 42)]
        public void Transform_ReadsANumericStringAsSeconds(string input, double expectedSeconds)
        {
            Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), Transform(input));
        }

        /// <summary>
        /// Verifies that a numeric string means the same thing whatever the machine's culture.
        /// </summary>
        /// <remarks>
        /// The numeric read is deliberately invariant rather than culture-sensitive. Under the current culture,
        /// <c language="powershell">"10.5"</c> is ten and a half on an English machine and one hundred and five on a German one, where the
        /// point is a thousands separator - so a script would mean different things on different agents.
        /// </remarks>
        /// <param name="culture">The culture to read under.</param>
        [Theory]
        [InlineData("en-AU")]
        [InlineData("en-US")]
        [InlineData("de-DE")]
        [InlineData("fr-FR")]
        [InlineData("ja-JP")]
        public void Transform_ReadsANumericStringTheSameWayInEveryCulture(string culture)
        {
            using CultureScope scope = new(culture);
            Assert.Equal(TimeSpan.FromSeconds(10.5), Transform("10.5"));
            Assert.Equal(TimeSpan.FromSeconds(90), Transform("90"));
        }

        /// <summary>
        /// Verifies that a string in duration format is still read as a duration.
        /// </summary>
        /// <remarks>
        /// The numeric check runs first, so it must not swallow these. None of them parse as a number, which is what
        /// keeps the two readings apart: a duration always carries a colon.
        /// </remarks>
        /// <param name="input">The duration the caller supplied.</param>
        /// <param name="expected">The duration it should mean, in round-trip form.</param>
        [Theory]
        [InlineData("00:00:10", "00:00:10")]
        [InlineData("00:02:00", "00:02:00")]
        [InlineData("01:30:00", "01:30:00")]
        [InlineData("1.02:03:04", "1.02:03:04")]
        [InlineData("00:00:10.5", "00:00:10.5000000")]
        public void Transform_ReadsADurationStringAsADuration(string input, string expected)
        {
            Assert.Equal(TimeSpan.Parse(expected, CultureInfo.InvariantCulture), Transform(input));
        }

        /// <summary>
        /// Verifies that a <see cref="TimeSpan"/> passes straight through.
        /// </summary>
        [Fact]
        public void Transform_PassesATimeSpanThrough()
        {
            // Arrange
            TimeSpan timeout = TimeSpan.FromMinutes(55);

            // Assert
            Assert.Equal(timeout, Transform(timeout));
        }

        /// <summary>
        /// Verifies that every numeric type PowerShell might hand over is read as seconds.
        /// </summary>
        /// <remarks>
        /// One test over the set rather than one per type. What matters is that the set is complete - a type missing
        /// from the switch falls through to the "cannot transform" refusal - not how each individual case behaves.
        /// </remarks>
        [Fact]
        public void Transform_ReadsEveryNumericTypeAsSeconds()
        {
            Assert.Equal(TimeSpan.FromSeconds(7), Transform((sbyte)7));
            Assert.Equal(TimeSpan.FromSeconds(7), Transform((byte)7));
            Assert.Equal(TimeSpan.FromSeconds(7), Transform((short)7));
            Assert.Equal(TimeSpan.FromSeconds(7), Transform((ushort)7));
            Assert.Equal(TimeSpan.FromSeconds(7), Transform(7));
            Assert.Equal(TimeSpan.FromSeconds(7), Transform(7u));
            Assert.Equal(TimeSpan.FromSeconds(7), Transform(7L));
            Assert.Equal(TimeSpan.FromSeconds(7), Transform(7uL));
            Assert.Equal(TimeSpan.FromSeconds(7.5), Transform(7.5f));
            Assert.Equal(TimeSpan.FromSeconds(7.5), Transform(7.5d));
            Assert.Equal(TimeSpan.FromSeconds(7.5), Transform(7.5m));
        }

        /// <summary>
        /// Verifies that a quoted number and the same number unquoted mean the same thing.
        /// </summary>
        /// <remarks>
        /// The property the fix exists to establish, stated directly. Whether a caller quotes a timeout is a matter of
        /// habit and should never change what it means.
        /// </remarks>
        /// <param name="seconds">The number of seconds, supplied both ways.</param>
        [Theory]
        [InlineData(10)]
        [InlineData(90)]
        [InlineData(3300)]
        public void Transform_AgreesWhetherOrNotTheNumberWasQuoted(int seconds)
        {
            Assert.Equal(Transform(seconds), Transform(seconds.ToString(CultureInfo.InvariantCulture)));
        }

        /// <summary>
        /// Verifies that a value wrapped by PowerShell is unwrapped before being read.
        /// </summary>
        /// <remarks>
        /// Everything crossing the engine boundary arrives wrapped, so an attribute that did not unwrap would refuse
        /// every real argument. Wrapped with <see cref="PSObject.AsPSObject"/> rather than the constructor:
        /// <c language="csharp">new PSObject(30)</c> binds to a different overload on PowerShell 7 and leaves the value unwrapped, so a
        /// test written that way passes on .NET Framework and fails on .NET for a reason that has nothing to do with
        /// the code under test.
        /// </remarks>
        [Fact]
        public void Transform_UnwrapsAPSObject()
        {
            Assert.Equal(TimeSpan.FromSeconds(30), Transform(PSObject.AsPSObject(30)));
            Assert.Equal(TimeSpan.FromSeconds(30), Transform(PSObject.AsPSObject("30")));
            Assert.Equal(TimeSpan.FromSeconds(30), Transform(PSObject.AsPSObject(TimeSpan.FromSeconds(30))));
        }

        /// <summary>
        /// Verifies that nothing at all is refused, and named as null rather than as the wrong type.
        /// </summary>
        [Fact]
        public void Transform_RefusesNothingAtAll()
        {
            Assert.Contains("Cannot transform null", Assert.Throws<ArgumentNullException>(static () => Transform(inputData: null)).Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that a value it cannot read is refused, and says what type it was given.
        /// </summary>
        /// <param name="input">Something that is not a duration.</param>
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("not a timeout")]
        [InlineData("10 minutes")]
        public void Transform_RefusesAStringItCannotRead(string input)
        {
            Assert.Contains("System.String", Assert.Throws<ArgumentException>(() => Transform(input)).Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that a type it has no reading for is refused.
        /// </summary>
        [Fact]
        public void Transform_RefusesATypeItCannotRead()
        {
            Assert.Contains("System.Guid", Assert.Throws<ArgumentException>(static () => Transform(Guid.Empty)).Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that the format provider given to the constructor governs the duration reading.
        /// </summary>
        /// <remarks>
        /// The one place a culture still has a say. Some cultures separate fractional seconds with a comma, so
        /// <c language="powershell">"00:00:10,5"</c> is ten and a half seconds under German rules and unreadable under invariant ones. The
        /// numeric reading ahead of it is unaffected either way, since a duration carries colons and never parses as a
        /// number.
        /// </remarks>
        [Fact]
        public void Transform_UsesTheFormatProviderItWasGivenForDurations()
        {
            // Arrange
            TimeSpanTransformationAttribute german = new(CultureInfo.GetCultureInfo("de-DE"));
            TimeSpanTransformationAttribute invariant = new(CultureInfo.InvariantCulture);

            // Assert
            Assert.Equal(TimeSpan.FromSeconds(10.5), (TimeSpan)german.Transform(engineIntrinsics: null!, "00:00:10,5"));
            _ = Assert.Throws<ArgumentException>(() => invariant.Transform(engineIntrinsics: null!, "00:00:10,5"));
        }

        /// <summary>
        /// Verifies that the parameterless constructor takes the current culture.
        /// </summary>
        /// <remarks>
        /// PowerShell can only use the parameterless form from a parameter declaration, so this is the constructor that
        /// runs in practice.
        /// </remarks>
        [Fact]
        public void FormatProvider_DefaultsToTheCurrentCulture()
        {
            using CultureScope scope = new("de-DE");
            Assert.Equal(CultureInfo.GetCultureInfo("de-DE"), new TimeSpanTransformationAttribute().FormatProvider);
        }

        /// <summary>
        /// Runs the attribute over a value.
        /// </summary>
        /// <param name="inputData">The value to transform.</param>
        /// <returns>The resulting duration.</returns>
        private static TimeSpan Transform(object? inputData)
        {
            return (TimeSpan)new TimeSpanTransformationAttribute().Transform(engineIntrinsics: null!, inputData);
        }
    }
}
