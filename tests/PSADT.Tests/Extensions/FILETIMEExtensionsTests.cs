using System;
using System.Runtime.InteropServices.ComTypes;
using Xunit;

namespace PSADT.Tests.Extensions
{
    /// <summary>
    /// Tests conversions exposed by the FILETIMEExtensions class.
    /// </summary>
    public sealed class FILETIMEExtensionsTests
    {
        /// <summary>
        /// Verifies that UTC conversion preserves UTC semantics.
        /// </summary>
        [Fact]
        public void ToDateTimeUtc_ReturnsUtcDateTime()
        {
            // Arrange
            DateTime expected = new(2025, 1, 15, 13, 45, 30, DateTimeKind.Utc);
            FILETIME filetime = CreateFileTime(expected.ToFileTimeUtc());

            // Act
            DateTime result = filetime.ToDateTimeUtc();

            // Assert
            Assert.Equal(expected, result);
            Assert.Equal(DateTimeKind.Utc, result.Kind);
        }

        /// <summary>
        /// Verifies that a time made of two zero halves is recognised as unset, which is how Windows
        /// reports a time it has no value for.
        /// </summary>
        /// <remarks>
        /// Worth its own member because zero is not an invalid time - it converts perfectly happily to
        /// the start of 1601. A caller that read it without asking this first would show that date to
        /// somebody as though a session had signed in then.
        /// </remarks>
        [Fact]
        public void IsZero_RecognisesAnUnsetTime()
        {
            // Assert: both halves zero
            Assert.True(new FILETIME { dwHighDateTime = 0, dwLowDateTime = 0 }.IsZero());

            // Assert: and either half alone is enough to make it set
            Assert.False(new FILETIME { dwHighDateTime = 1, dwLowDateTime = 0 }.IsZero());
            Assert.False(new FILETIME { dwHighDateTime = 0, dwLowDateTime = 1 }.IsZero());
        }

        /// <summary>
        /// Verifies that the two halves are combined into one number in the right order, with the high
        /// half above the low one.
        /// </summary>
        /// <remarks>
        /// The halves are signed thirty-two bit values holding what is really one unsigned sixty-four
        /// bit count, so the low half has to be widened without sign extension. A low half with its top
        /// bit set is the case that catches getting that wrong: extended as signed it would poison every
        /// upper bit and the result would come out negative.
        /// </remarks>
        [Fact]
        public void ToLong_CombinesTheHalvesInOrder()
        {
            // Assert: the halves land where they belong
            Assert.Equal(0L, new FILETIME { dwHighDateTime = 0, dwLowDateTime = 0 }.ToLong());
            Assert.Equal(1L, new FILETIME { dwHighDateTime = 0, dwLowDateTime = 1 }.ToLong());
            Assert.Equal(1L << 32, new FILETIME { dwHighDateTime = 1, dwLowDateTime = 0 }.ToLong());
            Assert.Equal((2L << 32) | 3L, new FILETIME { dwHighDateTime = 2, dwLowDateTime = 3 }.ToLong());

            // Assert: a low half with its top bit set is widened without sign extension
            Assert.Equal(0x80000000L, new FILETIME { dwHighDateTime = 0, dwLowDateTime = unchecked((int)0x80000000) }.ToLong());
            Assert.Equal(0xFFFFFFFFL, new FILETIME { dwHighDateTime = 0, dwLowDateTime = -1 }.ToLong());
        }

        /// <summary>
        /// Verifies that the combined number is the number the date conversion is built on, so the two
        /// cannot disagree about the same time.
        /// </summary>
        [Fact]
        public void ToLong_AgreesWithTheDateConversion()
        {
            // Arrange
            DateTime expected = new(2026, 8, 27, 9, 30, 0, DateTimeKind.Utc);
            long ticks = expected.ToFileTimeUtc();
            FILETIME filetime = new() { dwHighDateTime = (int)(ticks >> 32), dwLowDateTime = unchecked((int)(ticks & 0xFFFFFFFF)) };

            // Assert
            Assert.Equal(ticks, filetime.ToLong());
            Assert.Equal(expected, filetime.ToDateTimeUtc());
        }

        /// <summary>
        /// Verifies that the existing conversion continues to use local time semantics.
        /// </summary>
        [Fact]
        public void ToDateTime_ReturnsLocalDateTime()
        {
            // Arrange
            DateTime utcDateTime = new(2025, 1, 15, 13, 45, 30, DateTimeKind.Utc);
            long fileTime = utcDateTime.ToFileTimeUtc();
            FILETIME filetime = CreateFileTime(fileTime);

            // Act
            DateTime result = filetime.ToDateTime();

            // Assert
            Assert.Equal(DateTime.FromFileTime(fileTime), result);
            Assert.Equal(DateTimeKind.Local, result.Kind);
        }

        /// <summary>
        /// Creates a FILETIME structure from a 64-bit file time value.
        /// </summary>
        /// <param name="fileTime">The 64-bit file time value.</param>
        /// <returns>A FILETIME structure representing the specified value.</returns>
        private static FILETIME CreateFileTime(long fileTime)
        {
            return new()
            {
                dwLowDateTime = unchecked((int)(fileTime & 0xFFFFFFFFL)),
                dwHighDateTime = unchecked((int)(fileTime >> 32)),
            };
        }
    }
}
