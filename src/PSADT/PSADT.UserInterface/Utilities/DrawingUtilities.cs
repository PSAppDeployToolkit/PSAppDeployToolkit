using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using PSADT.Interop;
using Windows.Win32.UI.Shell;

namespace PSADT.UserInterface.Utilities
{
    /// <summary>
    /// A collection of utility methods for drawing and image manipulation.
    /// </summary>
    internal static class DrawingUtilities
    {
        /// <summary>
        /// Resize the bitmap to the specified width and height.
        /// </summary>
        /// <param name="img">The image to resize.</param>
        /// <param name="size">The square size to resize to.</param>
        /// <returns>The resized image.</returns>
        internal static Bitmap ResizeBitmap(Bitmap img, int size)
        {
            // Internal worker to letterbox/pillarbox a non-square source image into a square canvas.
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
                return (Bitmap)img.Clone();
            }

            // Create a new bitmap and set the resolution.
            Bitmap destImage = new(size, size, PixelFormat.Format32bppArgb);
            destImage.SetResolution(img.HorizontalResolution, img.VerticalResolution);

            // Create a new graphic that we can resize.
            using Graphics graphics = Graphics.FromImage(destImage);
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
            return destImage;
        }

        /// <summary>
        /// Converts an image to an icon, automatically resizing to the maximum icon size if greater than 128px.
        /// </summary>
        /// <param name="img">The image to resize.</param>
        /// <returns>The resized image.</returns>
        internal static Icon ConvertBitmapToIcon(Bitmap img)
        {
            using MemoryStream ms = new(CreateIconByteStreamFromBitmap(img), writable: false);
            using Icon icon = new(ms);
            return (Icon)icon.Clone();
        }

        /// <summary>
        /// Converts a bitmap image from the specified file path to an icon.
        /// </summary>
        /// <remarks>The method loads the image from the specified file path and converts it to an icon.
        /// Ensure that the file exists and is a valid image format supported by the Image class.</remarks>
        /// <param name="filename">The path to the image file to convert. This parameter cannot be null or empty.</param>
        /// <returns>An Icon object that represents the converted bitmap image.</returns>
        internal static Icon ConvertBitmapToIcon(string filename)
        {
            using Bitmap img = new(filename);
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
        internal static void SaveBitmapAsIconFile(Bitmap img, string path)
        {
            File.WriteAllBytes(path, CreateIconByteStreamFromBitmap(img));
        }

        /// <summary>
        /// Extracts the icon associated with the specified executable file.
        /// </summary>
        /// <remarks>This method uses the SHGetFileInfo function to retrieve the icon associated with the
        /// executable file. Ensure that the specified path points to a valid .exe file to avoid exceptions.</remarks>
        /// <param name="path">The full path to the executable (.exe) file from which to extract the icon. The path must refer to a valid
        /// file with a .exe extension.</param>
        /// <returns>An Icon object representing the icon extracted from the specified executable file. Returns null if the icon
        /// cannot be extracted.</returns>
        /// <exception cref="ArgumentException">Thrown if the provided path does not have a .exe extension.</exception>
        internal static Icon ExtractIconFromExecutable(string path)
        {
            // Get the icon handle using SHGetFileInfo, clone it, then return it.
            _ = NativeMethods.SHGetFileInfo(path, out SHFILEINFO psfi, SHGFI_FLAGS.SHGFI_ICON | SHGFI_FLAGS.SHGFI_LARGEICON);
            using (psfi)
            {
                using Icon icon = Icon.FromHandle(psfi.hIcon);
                return (Icon)icon.Clone();
            }
        }

        /// <summary>
        /// Extracts a bitmap image from the icon contained within the specified executable file.
        /// </summary>
        /// <remarks>This method assumes that the specified executable contains an icon resource. If the
        /// file does not exist, is not a valid executable, or does not contain an icon, the method may return
        /// null.</remarks>
        /// <param name="path">The path to the executable file from which to extract the bitmap. This parameter cannot be null or empty.</param>
        /// <returns>A Bitmap object representing the icon extracted from the executable. Returns null if the extraction fails or
        /// if the executable does not contain an icon.</returns>
        internal static Bitmap ExtractBitmapFromExecutable(string path)
        {
            // Convert the icon to a bitmap and return it.
            using Icon icon = ExtractIconFromExecutable(path);
            return icon.ToBitmap();
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
        private static byte[] CreateIconByteStreamFromBitmap(Bitmap source)
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
            const int iconDirSize = 6; const int iconEntrySize = 16;
            int imageDataOffset = iconDirSize + (frames.Length * iconEntrySize);
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
