using System;
using System.Drawing;
using System.IO;
using PSADT.UserInterface.Tests.TestHelpers;
using Xunit;

namespace PSADT.UserInterface.Tests
{
    /// <summary>
    /// Tests the image handling behind the icons a dialog shows.
    /// </summary>
    /// <remarks>
    /// The only part of this project that writes anything, and it writes only where it is told to - the
    /// one test that exercises that gives it a path under a temporary directory it then removes.
    /// </remarks>
    public sealed class DrawingUtilitiesTests
    {
        /// <summary>
        /// Verifies that an icon is recognised as one.
        /// </summary>
        [Fact]
        public void IsStreamAnIcon_RecognisesAnIcon()
        {
            // Arrange
            using MemoryStream stream = new(TestImages.IcoBytes(32), writable: false);

            // Act & Assert
            Assert.True(DrawingUtilities.IsStreamAnIcon(stream));
        }

        /// <summary>
        /// Verifies that a bitmap is not mistaken for an icon.
        /// </summary>
        /// <remarks>
        /// This decides which of two loaders the options validator uses, so a bitmap read as an icon fails
        /// a perfectly good image and an icon read as a bitmap loses every frame but one.
        /// </remarks>
        [Fact]
        public void IsStreamAnIcon_DoesNotMistakeABitmapForOne()
        {
            // Arrange
            using MemoryStream stream = new(TestImages.PngBytes(32, 32), writable: false);

            // Act & Assert
            Assert.False(DrawingUtilities.IsStreamAnIcon(stream));
        }

        /// <summary>
        /// Verifies that a stream too short to hold a header is not an icon.
        /// </summary>
        /// <remarks>
        /// The length check comes before the read, so this is the case that stops a short stream being
        /// read past its end rather than merely reported as not an icon.
        /// </remarks>
        [Fact]
        public void IsStreamAnIcon_RejectsAStreamTooShortToHoldAHeader()
        {
            // Arrange
            using MemoryStream stream = new([0, 0, 1, 0], writable: false);

            // Act & Assert
            Assert.False(DrawingUtilities.IsStreamAnIcon(stream));
        }

        /// <summary>
        /// Verifies that something with the right length but the wrong header is not an icon.
        /// </summary>
        /// <param name="reserved">The reserved field, which must be zero.</param>
        /// <param name="type">The type field, which must be 1 for an icon or 2 for a cursor.</param>
        /// <param name="count">The frame count, which must be at least one.</param>
        [Theory]
        [InlineData(1, 1, 1)]
        [InlineData(0, 3, 1)]
        [InlineData(0, 1, 0)]
        public void IsStreamAnIcon_RejectsAHeaderThatIsNotValid(ushort reserved, ushort type, ushort count)
        {
            // Arrange - a full-length header carrying the given field values.
            byte[] bytes = new byte[64];
            BitConverter.GetBytes(reserved).CopyTo(bytes, 0);
            BitConverter.GetBytes(type).CopyTo(bytes, 2);
            BitConverter.GetBytes(count).CopyTo(bytes, 4);
            using MemoryStream stream = new(bytes, writable: false);

            // Act & Assert
            Assert.False(DrawingUtilities.IsStreamAnIcon(stream));
        }

        /// <summary>
        /// Verifies that the stream is left where it was found.
        /// </summary>
        /// <remarks>
        /// The caller reads the image from the same stream immediately afterwards, so a position left at
        /// the end of the header would hand the loader a truncated image. The check reads from the start
        /// regardless of where the stream was, which is why a non-zero starting position is worth using.
        /// </remarks>
        [Fact]
        public void IsStreamAnIcon_LeavesThePositionWhereItFoundIt()
        {
            // Arrange
            using MemoryStream stream = new(TestImages.IcoBytes(32), writable: false) { Position = 7 };

            // Act
            _ = DrawingUtilities.IsStreamAnIcon(stream);

            // Assert
            Assert.Equal(7, stream.Position);
        }

        /// <summary>
        /// Verifies that a stream which cannot be read or sought is refused rather than misread.
        /// </summary>
        /// <param name="canRead">Whether the stream reports itself readable.</param>
        /// <param name="canSeek">Whether the stream reports itself seekable.</param>
        [Theory]
        [InlineData(false, true)]
        [InlineData(true, false)]
        public void IsStreamAnIcon_RefusesAStreamItCannotUse(bool canRead, bool canSeek)
        {
            // Arrange
            using AwkwardStream stream = new(canRead, canSeek);

            // Act & Assert
            ArgumentException exception = Assert.Throws<ArgumentException>(() => DrawingUtilities.IsStreamAnIcon(stream));
            Assert.Equal("stream", exception.ParamName);
        }

        /// <summary>
        /// Verifies that a null stream is refused.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0191:Do not use the null-forgiving operator", Justification = "This is deliberate as part of unit testing.")]
        [Fact]
        public void IsStreamAnIcon_RefusesANullStream()
        {
            _ = Assert.Throws<ArgumentNullException>(static () => DrawingUtilities.IsStreamAnIcon(null!));
        }

        /// <summary>
        /// Verifies that resizing to the size a bitmap already is returns a copy rather than the original.
        /// </summary>
        /// <remarks>
        /// The caller disposes what it gets back. Returning the original would dispose a bitmap the caller
        /// still owns, which is the kind of fault that shows up as an unrelated drawing failure later.
        /// </remarks>
        [Fact]
        public void ResizeBitmap_ReturnsACopyWhenTheSizeAlreadyMatches()
        {
            // Arrange
            using Bitmap source = TestImages.CreateBitmap(32, 32);

            // Act
            using Bitmap resized = DrawingUtilities.ResizeBitmap(source, 32);

            // Assert
            Assert.NotSame(source, resized);
            Assert.Equal(new Size(32, 32), resized.Size);
        }

        /// <summary>
        /// Verifies that a square bitmap resizes to the requested size.
        /// </summary>
        /// <param name="size">The size to resize to.</param>
        [Theory]
        [InlineData(16)]
        [InlineData(48)]
        [InlineData(256)]
        public void ResizeBitmap_ProducesASquareOfTheRequestedSize(int size)
        {
            // Arrange
            using Bitmap source = TestImages.CreateBitmap(64, 64);

            // Act
            using Bitmap resized = DrawingUtilities.ResizeBitmap(source, size);

            // Assert
            Assert.Equal(new Size(size, size), resized.Size);
        }

        /// <summary>
        /// Verifies that a non-square bitmap is fitted into the square without being stretched.
        /// </summary>
        /// <remarks>
        /// A source twice as wide as it is tall should occupy the full width and half the height, centred,
        /// with the remainder left transparent. Checking the corners and the centre is what distinguishes
        /// a letterboxed image from one stretched to fill - a stretched image would have drawn into the
        /// corners.
        /// </remarks>
        [Fact]
        public void ResizeBitmap_FitsANonSquareSourceWithoutStretchingIt()
        {
            // Arrange
            using Bitmap source = TestImages.CreateBitmap(64, 32);

            // Act
            using Bitmap resized = DrawingUtilities.ResizeBitmap(source, 64);

            // Assert
            Assert.Equal(new Size(64, 64), resized.Size);
            Assert.Equal(0, resized.GetPixel(1, 1).A);
            Assert.Equal(0, resized.GetPixel(62, 62).A);
            Assert.Equal(255, resized.GetPixel(32, 32).A);
        }

        /// <summary>
        /// Verifies that the resized bitmap keeps the source's resolution.
        /// </summary>
        [Fact]
        public void ResizeBitmap_KeepsTheSourceResolution()
        {
            // Arrange
            using Bitmap source = TestImages.CreateBitmap(64, 64);
            source.SetResolution(120f, 120f);

            // Act
            using Bitmap resized = DrawingUtilities.ResizeBitmap(source, 32);

            // Assert
            Assert.Equal(120f, resized.HorizontalResolution);
            Assert.Equal(120f, resized.VerticalResolution);
        }

        /// <summary>
        /// Verifies that a size of nothing is refused.
        /// </summary>
        /// <param name="size">The size to refuse.</param>
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void ResizeBitmap_RefusesASizeBelowOne(int size)
        {
            // Arrange
            using Bitmap source = TestImages.CreateBitmap(32, 32);

            // Act & Assert
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => DrawingUtilities.ResizeBitmap(source, size));
        }

        /// <summary>
        /// Verifies that a null bitmap is refused.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0191:Do not use the null-forgiving operator", Justification = "This is deliberate as part of unit testing.")]
        [Fact]
        public void ResizeBitmap_RefusesANullBitmap()
        {
            _ = Assert.Throws<ArgumentNullException>(static () => DrawingUtilities.ResizeBitmap(null!, 32));
        }

        /// <summary>
        /// Verifies that a bitmap converts to an icon that loads back.
        /// </summary>
        [Fact]
        public void ConvertBitmapToIcon_ProducesAnIconThatLoads()
        {
            // Arrange
            using Bitmap source = TestImages.CreateBitmap(64, 64);

            // Act
            using Icon icon = DrawingUtilities.ConvertBitmapToIcon(source);

            // Assert
            Assert.NotEqual(Size.Empty, icon.Size);
        }

        /// <summary>
        /// Verifies that converting a file writes an icon holding every standard size the source can fill.
        /// </summary>
        /// <remarks>
        /// The container is read back by hand rather than by handing it to the framework, because what is
        /// being checked is the layout the writer produced: the frame count, and that each frame's
        /// recorded offset and length actually land on a PNG within the file. A wrong offset still loads
        /// under a forgiving reader and fails under a strict one.
        /// </remarks>
        /// <param name="width">The source width.</param>
        /// <param name="height">The source height.</param>
        /// <param name="expectedFrames">The number of frames the source can fill.</param>
        [Theory]
        [InlineData(16, 16, 1)]
        [InlineData(32, 32, 4)]
        [InlineData(64, 48, 5)]
        [InlineData(300, 300, 8)]
        public void ConvertBitmapFileToIcon_WritesEveryStandardSizeTheSourceCanFill(int width, int height, int expectedFrames)
        {
            // Arrange
            using TempDirectory directory = new();
            string input = directory.WriteFile("source.png", TestImages.PngBytes(width, height));
            string output = directory.GetPath("result.ico");

            // Act
            DrawingUtilities.ConvertBitmapFileToIcon(input, output);

            // Assert
            byte[] ico = File.ReadAllBytes(output);
            Assert.Equal(0, BitConverter.ToUInt16(ico, 0));
            Assert.Equal(1, BitConverter.ToUInt16(ico, 2));
            Assert.Equal(expectedFrames, BitConverter.ToUInt16(ico, 4));
            for (int frame = 0; frame < expectedFrames; frame++)
            {
                int entry = 6 + (frame * 16);
                int length = BitConverter.ToInt32(ico, entry + 8);
                int offset = BitConverter.ToInt32(ico, entry + 12);
                Assert.InRange(offset, 6 + (expectedFrames * 16), ico.Length - length);
                Assert.Equal(0x89, ico[offset]);
                Assert.Equal((byte)'P', ico[offset + 1]);
            }
        }

        /// <summary>
        /// Verifies that the written icon is one the framework will load.
        /// </summary>
        /// <remarks>
        /// The complement to reading the container by hand: that check proves the layout is what was
        /// intended, and this proves the intention was right.
        /// </remarks>
        [Fact]
        public void ConvertBitmapFileToIcon_WritesAnIconTheFrameworkAccepts()
        {
            // Arrange
            using TempDirectory directory = new();
            string input = directory.WriteFile("source.png", TestImages.PngBytes(64, 64));
            string output = directory.GetPath("result.ico");

            // Act
            DrawingUtilities.ConvertBitmapFileToIcon(input, output);

            // Assert
            using FileStream stream = File.OpenRead(output);
            Assert.True(DrawingUtilities.IsStreamAnIcon(stream));
            using Icon icon = new(stream);
            Assert.NotEqual(Size.Empty, icon.Size);
        }

        /// <summary>
        /// Verifies that a source too small for the smallest standard icon is refused.
        /// </summary>
        /// <remarks>
        /// The smallest frame an icon carries is sixteen pixels. A smaller source would produce a file
        /// with no frames in it at all, which is a valid-looking container holding nothing.
        /// </remarks>
        /// <param name="width">The source width.</param>
        /// <param name="height">The source height.</param>
        [Theory]
        [InlineData(15, 16)]
        [InlineData(16, 15)]
        [InlineData(8, 8)]
        public void ConvertBitmapFileToIcon_RefusesASourceSmallerThanTheSmallestFrame(int width, int height)
        {
            // Arrange
            using TempDirectory directory = new();
            string input = directory.WriteFile("source.png", TestImages.PngBytes(width, height));
            string output = directory.GetPath("result.ico");

            // Act & Assert
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => DrawingUtilities.ConvertBitmapFileToIcon(input, output));
            Assert.False(File.Exists(output));
        }

        /// <summary>
        /// A stream that reports itself unreadable or unseekable, for the guards that check.
        /// </summary>
        /// <param name="canRead">Whether to report the stream readable.</param>
        /// <param name="canSeek">Whether to report the stream seekable.</param>
        private sealed class AwkwardStream(bool canRead, bool canSeek) : MemoryStream
        {
            /// <inheritdoc/>
            public override bool CanRead { get; } = canRead;

            /// <inheritdoc/>
            public override bool CanSeek { get; } = canSeek;
        }
    }
}
