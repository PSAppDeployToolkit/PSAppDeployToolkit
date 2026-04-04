using System;
using System.Runtime.InteropServices.ComTypes;
using PSADT.Extensions;
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
