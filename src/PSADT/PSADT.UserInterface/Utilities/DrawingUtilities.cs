using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using PSADT.LibraryInterfaces;
using Windows.Win32;
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
        /// <param name="width">The width to resize to.</param>
        /// <param name="height">The height to resize to.</param>
        /// <returns>The resized image.</returns>
        internal static Bitmap ResizeBitmap(Bitmap img, int width, int height)
        {
            // Create a new bitmap and set the resolution.
            Bitmap destImage = new(width, height);
            destImage.SetResolution(img.HorizontalResolution, img.VerticalResolution);

            // Create a new graphic that we can resize.
            using Graphics graphics = Graphics.FromImage(destImage);
            graphics.CompositingMode = CompositingMode.SourceCopy;
            graphics.CompositingQuality = CompositingQuality.HighQuality;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

            // Draw the resized graphic and return it.
            using ImageAttributes wrapMode = new();
            wrapMode.SetWrapMode(WrapMode.TileFlipXY);
            graphics.DrawImage(img, new(0, 0, width, height), 0, 0, img.Width, img.Height, GraphicsUnit.Pixel, wrapMode);
            return destImage;
        }

        /// <summary>
        /// Converts an image to an icon, automatically resizing to the maximum icon size if greater than 128px.
        /// </summary>
        /// <param name="img">The image to resize.</param>
        /// <returns>The resized image.</returns>
        internal static Icon ConvertBitmapToIcon(Bitmap img)
        {
            // Internal implementation of the ConvertBitmapToIcon method.
            static Icon ConvertBitmapToIconImpl(Bitmap img)
            {
                // Place the image into an icon object and return it.
                using MemoryStream msImg = new();
                img.Save(msImg, ImageFormat.Png);
                using MemoryStream msIco = new();
                using BinaryWriter bw = new(msIco);
                bw.Write((short)0);           // 0-1 reserved
                bw.Write((short)1);           // 2-3 image type, 1 = icon, 2 = cursor
                bw.Write((short)1);           // 4-5 number of images
                bw.Write((byte)img.Width);    // 6 image width
                bw.Write((byte)img.Height);   // 7 image height
                bw.Write((byte)0);            // 8 number of colors
                bw.Write((byte)0);            // 9 reserved
                bw.Write((short)0);           // 10-11 color planes
                bw.Write((short)32);          // 12-13 bits per pixel
                bw.Write((int)msImg.Length);  // 14-17 size of image data
                bw.Write(22);                 // 18-21 offset of image data
                bw.Write(msImg.ToArray());    // write image data
                bw.Flush();
                _ = bw.Seek(0, SeekOrigin.Begin);
                return new(msIco);
            }

            // Ensure the incoming image is <128px in width/height.
            if ((img.Width > 128) || (img.Height > 128))
            {
                using Bitmap resizedImg = ResizeBitmap(img, 128, 128);
                return ConvertBitmapToIconImpl(resizedImg);
            }
            return ConvertBitmapToIconImpl(img);
        }

        /// <summary>
        /// Converts an image file to an icon, automatically resizing to the maximum icon size if greater than 128px.
        /// </summary>
        /// <param name="imagePath"></param>
        /// <returns></returns>
        internal static Icon ConvertBitmapToIcon(string imagePath)
        {
            using Bitmap img = (Bitmap)Image.FromFile(imagePath);
            return ConvertBitmapToIcon(img);
        }

        /// <summary>
        /// Get the icon from a given executable path.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        internal static Icon ExtractIconFromExecutable(string path)
        {
            // Check if the process is null or if the main module's file name is not a string.
            if (!Path.GetExtension(path).Equals(".exe", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"The path [{path}] is invalid.", nameof(path));
            }

            // Get the icon handle using SHGetFileInfo, clone it, then return it.
            _ = Shell32.SHGetFileInfo(path, out Shell32.SHFILEINFO psfi, SHGFI_FLAGS.SHGFI_ICON | SHGFI_FLAGS.SHGFI_LARGEICON);
            using DestroyIconSafeHandle hIcon = new(psfi.hIcon, true);
            bool hIconAddRef = false;
            try
            {
                hIcon.DangerousAddRef(ref hIconAddRef);
                using Icon icon = Icon.FromHandle(hIcon.DangerousGetHandle());
                return (Icon)icon.Clone();
            }
            finally
            {
                if (hIconAddRef)
                {
                    hIcon.DangerousRelease();
                }
            }
        }

        /// <summary>
        /// Get the bitmap from a given executable path.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        internal static Bitmap ExtractBitmapFromExecutable(string path)
        {
            // Convert the icon to a bitmap and return it.
            using Icon icon = ExtractIconFromExecutable(path);
            return icon.ToBitmap();
        }
    }
}
