using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace PSADT.UserInterface.Interfaces.Tests.TestHelpers
{
    /// <summary>
    /// Builds the images the tests feed to the code under test.
    /// </summary>
    /// <remarks>
    /// Generated rather than committed as binary fixtures, so that a test needing a particular size,
    /// aspect ratio or frame count asks for one instead of the suite carrying a file per case. A
    /// deliberate near-copy of the one beside PSADT.UserInterface's tests, carrying only what these
    /// tests need plus the multi-frame icon writer they do not have: a test project referencing another
    /// test project would drag its whole suite into this one's discovery.
    /// </remarks>
    internal static class TestImages
    {
        /// <summary>
        /// Creates an opaque bitmap of the requested size.
        /// </summary>
        /// <remarks>
        /// Two diagonal bands rather than a flat fill, so an image that silently comes back empty or
        /// uniformly transparent is distinguishable from one that actually drew the source.
        /// </remarks>
        /// <param name="width">The width in pixels.</param>
        /// <param name="height">The height in pixels.</param>
        /// <returns>The bitmap, which the caller owns.</returns>
        public static Bitmap CreateBitmap(int width, int height)
        {
            Bitmap bitmap = new(width, height, PixelFormat.Format32bppArgb);
            try
            {
                using Graphics graphics = Graphics.FromImage(bitmap);
                graphics.Clear(Color.CornflowerBlue);
                using SolidBrush brush = new(Color.Firebrick);
                graphics.FillPolygon(brush, [new Point(0, 0), new Point(width, 0), new Point(0, height)]);
                return bitmap;
            }
            catch
            {
                bitmap.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Encodes a bitmap of the requested size as PNG.
        /// </summary>
        /// <param name="width">The width in pixels.</param>
        /// <param name="height">The height in pixels.</param>
        /// <returns>The PNG bytes.</returns>
        public static byte[] PngBytes(int width, int height)
        {
            using Bitmap bitmap = CreateBitmap(width, height);
            using MemoryStream stream = new();
            bitmap.Save(stream, ImageFormat.Png);
            return stream.ToArray();
        }

        /// <summary>
        /// Encodes a bitmap of the requested size as a base64 PNG, which is one of the two forms the
        /// dialog options accept for an image.
        /// </summary>
        /// <param name="width">The width in pixels.</param>
        /// <param name="height">The height in pixels.</param>
        /// <returns>The base64 text.</returns>
        public static string PngBase64(int width, int height)
        {
            return Convert.ToBase64String(PngBytes(width, height));
        }

        /// <summary>
        /// A small valid base64 PNG, for the many cases that need a valid image and do not care which.
        /// </summary>
        /// <returns>The base64 text.</returns>
        public static string SampleImage()
        {
            return PngBase64(16, 16);
        }

        /// <summary>
        /// Writes an ICO wrapping a PNG frame per requested size.
        /// </summary>
        /// <remarks>
        /// A PNG-compressed frame is what a modern ICO holds at these sizes, so this is the shape the
        /// readers under test have to accept. The frames are written smallest-first regardless of the
        /// order asked for, so a reader that returns the first frame rather than the largest one is
        /// distinguishable from one that picks correctly.
        /// </remarks>
        /// <param name="sizes">The frame dimensions in pixels, each at most 256.</param>
        /// <returns>The ICO bytes.</returns>
        public static byte[] IcoBytes(params int[] sizes)
        {
            ArgumentNullException.ThrowIfNull(sizes);
            int[] ordered = sizes.Length > 0 ? [.. sizes] : [32];
            Array.Sort(ordered);
            foreach (int size in ordered)
            {
                ArgumentOutOfRangeException.ThrowIfLessThan(size, 1);
                ArgumentOutOfRangeException.ThrowIfGreaterThan(size, 256);
            }

            List<byte[]> frames = [];
            foreach (int size in ordered)
            {
                frames.Add(PngBytes(size, size));
            }

            using MemoryStream stream = new();
            using (BinaryWriter writer = new(stream))
            {
                // ICONDIR: reserved, type 1 (icon), frame count.
                writer.Write((ushort)0);
                writer.Write((ushort)1);
                writer.Write((ushort)ordered.Length);

                // Every ICONDIRENTRY precedes the first frame's bytes, so the first offset sits past all
                // of them: the six byte header plus sixteen bytes per entry.
                int offset = 6 + (16 * ordered.Length);
                for (int index = 0; index < ordered.Length; index++)
                {
                    // 256 is written as zero, which is how the format spells it.
                    byte dimension = unchecked((byte)ordered[index]);
                    writer.Write(dimension);
                    writer.Write(dimension);
                    writer.Write((byte)0);
                    writer.Write((byte)0);
                    writer.Write((ushort)1);
                    writer.Write((ushort)32);
                    writer.Write(frames[index].Length);
                    writer.Write(offset);
                    offset += frames[index].Length;
                }
                foreach (byte[] frame in frames)
                {
                    writer.Write(frame);
                }
                writer.Flush();
            }
            return stream.ToArray();
        }

        /// <summary>
        /// Bytes that are not an image in any format.
        /// </summary>
        /// <returns>The bytes.</returns>
        public static byte[] NotAnImage()
        {
            return System.Text.Encoding.UTF8.GetBytes("this is not an image, it is a sentence about one");
        }
    }
}
