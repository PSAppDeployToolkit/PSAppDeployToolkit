using System;
using Xunit;

namespace PSADT.Interop.Tests.Polyfills
{
    /// <summary>
    /// Tests Nullable.GetValueRefOrDefaultRef, which PSADT.Interop uses to hand a nullable's storage
    /// straight to native code without copying.
    /// </summary>
    /// <remarks>
    /// This polyfill reinterprets Nullable&lt;T&gt; as its own struct through Unsafe.As, so it depends on
    /// the runtime laying that type out as a flag followed by the value. That assumption is invisible at
    /// the call site and would fail silently by returning whatever happened to sit at the value offset,
    /// so the types below deliberately span a range of sizes and alignments rather than testing int alone.
    /// </remarks>
    public sealed class NullablePolyfillTests
    {
        /// <summary>
        /// The GUID used by the fixed-size layout assertions.
        /// </summary>
        private static readonly Guid SampleGuid = new(0x2f8b8ea3, 0x30cd, 0x4a2b, 0x9a, 0x54, 0x9e, 0x0a, 0x9f, 0x5c, 0x1d, 0x77);

        /// <summary>
        /// Verifies that a nullable holding a value yields that value, across widths and alignments that
        /// would expose a wrong offset.
        /// </summary>
        [Fact]
        public void GetValueRefOrDefaultRef_ReturnsUnderlyingValue()
        {
            // Arrange
            byte? byteValue = 0xAB;
            char? charValue = 'Z';
            bool? boolValue = true;
            short? shortValue = -1234;
            int? intValue = 42;
            long? longValue = long.MinValue;
            double? doubleValue = -1.5;
            decimal? decimalValue = decimal.MaxValue;
            Guid? guidValue = SampleGuid;
            DateTime? dateTimeValue = new(2026, 8, 27, 16, 5, 0, DateTimeKind.Utc);

            // Assert
            Assert.Equal(0xAB, Nullable.GetValueRefOrDefaultRef(in byteValue));
            Assert.Equal('Z', Nullable.GetValueRefOrDefaultRef(in charValue));
            Assert.True(Nullable.GetValueRefOrDefaultRef(in boolValue));
            Assert.Equal(-1234, Nullable.GetValueRefOrDefaultRef(in shortValue));
            Assert.Equal(42, Nullable.GetValueRefOrDefaultRef(in intValue));
            Assert.Equal(long.MinValue, Nullable.GetValueRefOrDefaultRef(in longValue));
            Assert.Equal(-1.5, Nullable.GetValueRefOrDefaultRef(in doubleValue));
            Assert.Equal(decimal.MaxValue, Nullable.GetValueRefOrDefaultRef(in decimalValue));
            Assert.Equal(SampleGuid, Nullable.GetValueRefOrDefaultRef(in guidValue));
            Assert.Equal(new DateTime(2026, 8, 27, 16, 5, 0, DateTimeKind.Utc), Nullable.GetValueRefOrDefaultRef(in dateTimeValue));
        }

        /// <summary>
        /// Verifies that a custom multi-field struct round-trips every field, which a layout assumption
        /// off by any number of bytes would not.
        /// </summary>
        [Fact]
        public void GetValueRefOrDefaultRef_ReturnsUnderlyingValueForCompositeStruct()
        {
            // Arrange
            Composite? value = new Composite(0x5A, -9_000_000_000L, 'q');

            // Act
            ref readonly Composite result = ref Nullable.GetValueRefOrDefaultRef(in value);

            // Assert
            Assert.Equal(0x5A, result.First);
            Assert.Equal(-9_000_000_000L, result.Second);
            Assert.Equal('q', result.Third);
        }

        /// <summary>
        /// Verifies that a nullable without a value yields the default rather than throwing, which is the
        /// behaviour that distinguishes this from Value.
        /// </summary>
        [Fact]
        public void GetValueRefOrDefaultRef_ReturnsDefaultWhenNoValue()
        {
            // Arrange
            int? intValue = null;
            long? longValue = null;
            Guid? guidValue = null;
            Composite? compositeValue = null;

            // Assert
            Assert.Equal(0, Nullable.GetValueRefOrDefaultRef(in intValue));
            Assert.Equal(0L, Nullable.GetValueRefOrDefaultRef(in longValue));
            Assert.Equal(Guid.Empty, Nullable.GetValueRefOrDefaultRef(in guidValue));
            Assert.Equal(0, Nullable.GetValueRefOrDefaultRef(in compositeValue).First);
        }

        /// <summary>
        /// Verifies that the result aliases the nullable's own storage rather than being a copy, which is
        /// the entire reason this member exists.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0059:Unnecessary assignment of a value", Justification = "The reassignment is observed through the returned reference, which is what the test asserts.")]
        [Fact]
        public void GetValueRefOrDefaultRef_AliasesTheNullableStorage()
        {
            // Arrange
            int? value = 1;
            ref readonly int reference = ref Nullable.GetValueRefOrDefaultRef(in value);

            // Act
            value = 2;

            // Assert
            Assert.Equal(2, reference);
        }

        /// <summary>
        /// A struct larger than a machine word with mixed field sizes, to catch a layout assumption that
        /// happens to hold for a single primitive but not in general.
        /// </summary>
        /// <param name="first">The leading byte.</param>
        /// <param name="second">The 64-bit middle field.</param>
        /// <param name="third">The trailing character.</param>
        private readonly struct Composite(byte first, long second, char third)
        {
            /// <summary>
            /// Gets the leading byte.
            /// </summary>
            public byte First { get; } = first;

            /// <summary>
            /// Gets the 64-bit middle field.
            /// </summary>
            public long Second { get; } = second;

            /// <summary>
            /// Gets the trailing character.
            /// </summary>
            public char Third { get; } = third;
        }
    }
}
