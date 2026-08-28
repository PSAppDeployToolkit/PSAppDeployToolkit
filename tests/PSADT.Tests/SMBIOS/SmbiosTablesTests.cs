using System;
using System.Globalization;
using PSADT.SMBIOS;
using Xunit;

namespace PSADT.Tests.SMBIOS
{
    /// <summary>
    /// Tests reading the machine's raw firmware table.
    /// </summary>
    /// <remarks>
    /// This is where every fact the toolkit knows about the machine's hardware begins: the enclosure it
    /// is in, the manufacturer and model, whether it is portable. The parsing of what comes back is
    /// covered in depth against synthesised data elsewhere, so what is left here is the read itself -
    /// that the size asked for beforehand is the size delivered, since the two are separate calls into
    /// the firmware and a buffer sized from a stale answer would be filled short or overrun.
    /// </remarks>
    public sealed class SmbiosTablesTests
    {
        /// <summary>
        /// Verifies that the machine reports a firmware table of a plausible size.
        /// </summary>
        [Fact]
        public void GetRequiredLength_ReportsAPlausibleSize()
        {
            // Act
            int length = SmbiosTables.GetRequiredLength();

            // Assert: large enough for the header and the structures every machine carries, and not absurd
            Assert.True(length > 8, $"Reported a firmware table of only {length.ToString(CultureInfo.InvariantCulture)} bytes.");
            Assert.True(length < 16 * 1024 * 1024, $"Reported an implausible firmware table of {length.ToString(CultureInfo.InvariantCulture)} bytes.");
        }

        /// <summary>
        /// Verifies that asking twice gives the same size, since the table does not change while the
        /// machine is running and a buffer is sized from one call and filled by another.
        /// </summary>
        [Fact]
        public void GetRequiredLength_IsStableBetweenReadings()
        {
            Assert.Equal(SmbiosTables.GetRequiredLength(), SmbiosTables.GetRequiredLength());
        }

        /// <summary>
        /// Verifies that a buffer of the reported size is filled exactly, which is the agreement between
        /// the two calls.
        /// </summary>
        [Fact]
        public void FillBuffer_FillsABufferOfTheReportedSize()
        {
            // Arrange
            byte[] buffer = new byte[SmbiosTables.GetRequiredLength()];

            // Act
            SmbiosTables.FillBuffer(buffer);

            // Assert: something was actually written, rather than the buffer being left as it was
            Assert.Contains(buffer, static b => b is not 0);
        }

        /// <summary>
        /// Verifies that a buffer of the wrong size is refused rather than quietly filled short, since a
        /// partially filled table would be parsed as a truncated one.
        /// </summary>
        [Fact]
        public void FillBuffer_RefusesABufferOfTheWrongSize()
        {
            _ = Assert.Throws<InvalidOperationException>(static () => SmbiosTables.FillBuffer(new byte[SmbiosTables.GetRequiredLength() + 1]));
        }

        /// <summary>
        /// Verifies that the same table comes back each time, since nothing about the firmware changes
        /// while the machine is running.
        /// </summary>
        [Fact]
        public void FillBuffer_ReadsTheSameTableEachTime()
        {
            // Arrange
            byte[] first = new byte[SmbiosTables.GetRequiredLength()];
            byte[] second = new byte[first.Length];

            // Act
            SmbiosTables.FillBuffer(first);
            SmbiosTables.FillBuffer(second);

            // Assert
            Assert.Equal(first, second);
        }
    }
}
