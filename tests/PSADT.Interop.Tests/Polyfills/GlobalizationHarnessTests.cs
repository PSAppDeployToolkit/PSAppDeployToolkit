using System;
using System.Globalization;
using Xunit;

namespace PSADT.Interop.Tests.Polyfills
{
    /// <summary>
    /// Guards the assumption the culture-sensitive polyfill tests rest on: that both target frameworks
    /// collate with NLS, so the net8.0 framework leg is a fair oracle for the net472 polyfill leg.
    /// </summary>
    /// <remarks>
    /// net472 has no ICU and always uses NLS. net8.0 defaults to ICU and is pinned to NLS by a
    /// RuntimeHostConfigurationOption in this project's csproj. If that pin ever stops working the
    /// culture-sensitive expectations elsewhere in this folder would start failing on net8.0 for
    /// reasons unrelated to the polyfills, so it is worth failing here instead with a clear cause.
    /// Non-ASCII characters are written as escapes throughout these tests because the exact code point
    /// is the subject under test, and several of them are invisible in source.
    /// </remarks>
    public sealed class GlobalizationHarnessTests
    {
        /// <summary>
        /// U+00DF LATIN SMALL LETTER SHARP S. NLS collates this equal to "ss" and ICU does not.
        /// </summary>
        private const string SharpS = "\u00DF";

        /// <summary>
        /// U+00AD SOFT HYPHEN, a character with no collation weight.
        /// </summary>
        private const string SoftHyphen = "\u00AD";

        /// <summary>
        /// Verifies the collation engine is NLS. NLS compares U+00DF equal to "ss" at the default
        /// strength and ICU does not, which distinguishes the two without depending on globalization
        /// internals. The Replace polyfill uses this same probe to decide whether to extend a match
        /// over ignorable characters, so this pins that decision too.
        /// </summary>
        [Fact]
        public void CollationEngine_IsNls()
        {
            // Act
            int result = CultureInfo.InvariantCulture.CompareInfo.Compare(SharpS, "ss", CompareOptions.None);

            // Assert
            Assert.Equal(0, result);
        }

        /// <summary>
        /// Verifies that the soft hyphen carries no collation weight, which is the property the
        /// variable-length match cases rely on to make a matched region longer than the value being
        /// searched for.
        /// </summary>
        [Fact]
        public void SoftHyphen_HasNoCollationWeight()
        {
            // Act
            int result = CultureInfo.InvariantCulture.CompareInfo.Compare(SoftHyphen, string.Empty, CompareOptions.None);

            // Assert
            Assert.Equal(0, result);
        }

        /// <summary>
        /// Verifies that ordinal comparison is unaffected by the collation engine, which is what lets
        /// the ordinal cases be asserted without any culture setup.
        /// </summary>
        [Fact]
        public void OrdinalComparison_DoesNotTreatSharpSAsSs()
        {
            // Assert
            Assert.NotEqual(SharpS, "ss", StringComparer.Ordinal);
        }
    }
}
