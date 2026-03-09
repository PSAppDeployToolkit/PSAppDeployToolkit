using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using PSADT.Interop;
using PSADT.Interop.Extensions;

namespace PSADT.UserInterface.Utilities
{
    /// <summary>
    /// A collection of utility methods for drawing and bitmap manipulation.
    /// </summary>
    internal static class DrawingUtilities
    {
        /// <summary>
        /// Resize the bitmap to the specified width and height.
        /// </summary>
        /// <param name="img">The bitmap to resize.</param>
        /// <param name="size">The square size to resize to.</param>
        /// <returns>The resized bitmap.</returns>
        internal static Bitmap ResizeBitmap(Bitmap img, int size)
        {
            // Internal worker to letterbox/pillarbox a non-square source bitmap into a square canvas.
            static Rectangle GetAspectFitRectangle(int sourceWidth, int sourceHeight, int boxWidth, int boxHeight)
            {
                double scale = Math.Min((double)boxWidth / sourceWidth, (double)boxHeight / sourceHeight);
                int drawWidth = Math.Max(1, (int)Math.Round(sourceWidth * scale));
                int drawHeight = Math.Max(1, (int)Math.Round(sourceHeight * scale));
                return new(x: (boxWidth - drawWidth) / 2, y: (boxHeight - drawHeight) / 2, drawWidth, drawHeight);
            }

            // Validate input, and just clone if it's already the right size.
            ArgumentNullException.ThrowIfNull(img); ArgumentOutOfRangeException.ThrowIfLessThan(size, 1);
            if (img.Width == size && img.Height == size)
            {
                return new(img);
            }

            // Create a new bitmap and set the resolution.
            Bitmap destBitmap = new(size, size, PixelFormat.Format32bppArgb);
            destBitmap.SetResolution(img.HorizontalResolution, img.VerticalResolution);

            // Create a new graphic that we can resize.
            using Graphics graphics = Graphics.FromImage(destBitmap);
            graphics.Clear(Color.Transparent);
            graphics.CompositingMode = CompositingMode.SourceOver;
            graphics.CompositingQuality = CompositingQuality.HighQuality;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

            // Draw the resized graphic and return it.
            using ImageAttributes wrapMode = new();
            wrapMode.SetWrapMode(WrapMode.TileFlipXY);
            Rectangle destRect = GetAspectFitRectangle(img.Width, img.Height, size, size);
            graphics.DrawImage(img, destRect, 0, 0, img.Width, img.Height, GraphicsUnit.Pixel, wrapMode);
            return destBitmap;
        }

        /// <summary>
        /// Converts a bitmap to an icon, adding a square entry for each possible size without stretcing the source.
        /// </summary>
        /// <param name="img">The bitmap to resize.</param>
        /// <returns>The resized bitmap.</returns>
        internal static Icon ConvertBitmapToIcon(Bitmap img)
        {
            using MemoryStream ms = new(CreateIconByteArray(img), writable: false);
            return new(ms, 256, 256);
        }

        /// <summary>
        /// Converts a bitmap image from the specified file path to an icon.
        /// </summary>
        /// <remarks>The method loads the bitmap from the specified file path and converts it to an icon.
        /// Ensure that the file exists and is a valid bitmap format supported by the <see cref="Bitmap"/> class.</remarks>
        /// <param name="filename">The path to the bitmap file to convert. This parameter cannot be null or empty.</param>
        /// <returns>An Icon object that represents the converted bitmap.</returns>
        internal static Icon ConvertBitmapToIcon(string filename)
        {
            using Bitmap img = new(filename, true);
            return ConvertBitmapToIcon(img);
        }

        /// <summary>
        /// Saves the specified bitmap image as an icon file at the given destination path.
        /// </summary>
        /// <remarks>This method converts the bitmap image to an icon format and writes the resulting byte
        /// stream to the specified file. Ensure that the destination path has the appropriate file extension for an
        /// icon (e.g., .ico).</remarks>
        /// <param name="img">The bitmap image to be converted and saved as an icon file.</param>
        /// <param name="path">The file path where the icon file will be saved. The path must be valid and writable.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void SaveBitmapAsIconFile(Bitmap img, string path)
        {
            File.WriteAllBytes(path, CreateIconByteArray(img));
        }

        /// <summary>
        /// Determines whether the specified stream contains an icon file based on its header.
        /// </summary>
        /// <remarks>The method checks the first six bytes of the array for the ICONDIR header, which is
        /// required for recognizing the icon file format.</remarks>
        /// <param name="stream">The stream to examine. Must have a length of at least 6 bytes to be valid for icon detection.</param>
        /// <returns>true if the stream starts with the ICONDIR header indicating it is an icon file; otherwise, false.</returns>
        internal static bool IsStreamAnIcon(Stream stream)
        {
            // Confirm the stream is valid.
            ArgumentNullException.ThrowIfNull(stream);
            if (!stream.CanRead)
            {
                throw new ArgumentException("The stream must be readable.", nameof(stream));
            }
            if (!stream.CanSeek)
            {
                throw new ArgumentException("The stream must be seekable.", nameof(stream));
            }

            // Confirm the stream has enough data for an ICONDIR header.
            int iconDirSize = Marshal.SizeOf<ICONDIR>();
            if (stream.Length < iconDirSize)
            {
                return false;
            }

            // Validate that the byte data at the start of the stream matches the expected ICONDIR header values.
            long startingPosition = stream.Position; stream.Position = 0;
            try
            {
                // Read the ICONDIR header and confirm it has the expected values.
                byte[] buffer = new byte[iconDirSize]; int bytesRead = stream.Read(buffer, 0, iconDirSize);
                if (bytesRead != iconDirSize)
                {
                    return false;
                }

                // Interpret the buffer as an ICONDIR structure and validate its fields.
                ref readonly ICONDIR iconDir = ref buffer.AsReadOnlyStructure<ICONDIR>();
                return iconDir.IsValid;
            }
            finally
            {
                stream.Position = startingPosition;
            }
        }

        /// <summary>
        /// Creates a byte array representing an icon file from the specified bitmap image, supporting multiple sizes.
        /// </summary>
        /// <remarks>This method generates icon frames for standard sizes (16, 20, 24, 32, 48, 64, 128,
        /// 256) based on the source bitmap's dimensions. It ensures that the output is a valid ICO format, which can be
        /// used in applications requiring icon resources.</remarks>
        /// <param name="source">The bitmap image to convert into an icon format. The bitmap must have dimensions of at least 16x16 pixels.</param>
        /// <returns>A byte array containing the ICO file data generated from the provided bitmap.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the dimensions of the source bitmap are less than 16x16 pixels.</exception>
        private static byte[] CreateIconByteArray(Bitmap source)
        {
            // Internal worker functions to facilitate the main method logic.
            static List<int> GetSupportedSizes(Bitmap source)
            {
                // Determine which standard icon sizes are supported, up to a maximum of 256.
                int maxSourceDimension = Math.Min(256, Math.Min(source.Width, source.Height));
                int[] candidateSizes = [16, 20, 24, 32, 48, 64, 128, 256];
                List<int> sizes = new(candidateSizes.Length);
                for (int i = 0; i < candidateSizes.Length; i++)
                {
                    if (candidateSizes[i] <= maxSourceDimension)
                    {
                        sizes.Add(candidateSizes[i]);
                    }
                }
                return sizes;
            }

            // Confirm the source is valid.
            ArgumentNullException.ThrowIfNull(source);
            if (source.Width < 16 || source.Height < 16)
            {
                throw new ArgumentOutOfRangeException(nameof(source), "Source bitmap dimensions must be at least 16x16.");
            }

            // Render all frames up front so we know sizes and offsets before writing the ICO container.
            List<int> supportedSizes = GetSupportedSizes(source);
            (int Size, byte[] PngData)[] frames = new (int, byte[])[supportedSizes.Count];
            for (int i = 0; i < supportedSizes.Count; i++)
            {
                int newSize = supportedSizes[i];
                using Bitmap resized = ResizeBitmap(source, newSize);
                using MemoryStream pngStream = new();
                resized.Save(pngStream, ImageFormat.Png);
                frames[i] = (newSize, pngStream.ToArray());
            }
            int iconDirEntrySize = Marshal.SizeOf<ICONDIRENTRY>();
            int imageDataOffset = Marshal.SizeOf<ICONDIR>() - iconDirEntrySize + (frames.Length * iconDirEntrySize);
            int totalLength = imageDataOffset;
            for (int i = 0; i < frames.Length; i++)
            {
                totalLength += frames[i].PngData.Length;
            }

            // Set up the writers and write the ICO file structure.
            using MemoryStream icoStream = new(totalLength);
            using BinaryWriter writer = new(icoStream);

            // ICONDIR
            writer.Write((ushort)0);                 // idReserved
            writer.Write((ushort)1);                 // idType = 1 (icon)
            writer.Write((ushort)frames.Length);     // idCount

            // ICONDIRENTRY table
            int currentOffset = imageDataOffset;
            for (int i = 0; i < frames.Length; i++)
            {
                (int Size, byte[] PngData) frame = frames[i];
                byte dim = unchecked((byte)frame.Size);
                writer.Write(dim);                           // bWidth
                writer.Write(dim);                           // bHeight
                writer.Write((byte)0);                       // bColorCount
                writer.Write((byte)0);                       // bReserved
                writer.Write((ushort)1);                     // wPlanes
                writer.Write((ushort)32);                    // wBitCount
                writer.Write(frame.PngData.Length);          // dwBytesInRes
                writer.Write(currentOffset);                 // dwImageOffset
                currentOffset += frame.PngData.Length;
            }

            // Image payloads
            for (int i = 0; i < frames.Length; i++)
            {
                writer.Write(frames[i].PngData);
            }
            writer.Flush();
            return icoStream.ToArray();
        }
    }
}
