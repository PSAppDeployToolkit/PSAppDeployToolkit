using System;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using PSADT.PInvoke;
using PSADT.Diagnostics.Exceptions;

namespace PSADT.Shared
{
    public static class Utility
    {
        /// <summary>
        /// Determines if the Out of Box Experience (OOBE) process is complete on a Windows system.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the OOBE process is complete; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// The OOBE (Out of Box Experience) process is the initial setup that users go through when starting a new Windows
        /// device for the first time or after a system reset. It includes tasks such as setting up language preferences,
        /// creating a user account, agreeing to license terms, and configuring other initial settings.
        ///
        /// Some system operations or configurations may depend on the completion of the OOBE process. Knowing whether OOBE
        /// is complete can help ensure the system is fully initialized and ready for additional configurations or software
        /// deployments.
        ///
        /// If the underlying native method call fails, a system error will be thrown. Use this method in contexts where
        /// system initialization is critical to the next steps of execution.
        ///
        /// <param>If an error occurs, the method will throw a system error after a call to <see cref="GetLastWin32Error"/>.</param>
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the native method call to check the OOBE status fails./>
        /// for additional error information.
        /// </exception>
        public static bool IsOOBEComplete()
        {
            if (!NativeMethods.OOBEComplete(out int isOobeComplete))
            {
                ErrorHandler.ThrowSystemError("Failed to check OOBE status.", SystemErrorType.Win32);
            }

            return isOobeComplete != 0;
        }

        /// <summary>
        /// Resize the image to the specified width and height.
        /// </summary>
        /// <param name="img">The image to resize.</param>
        /// <param name="width">The width to resize to.</param>
        /// <param name="height">The height to resize to.</param>
        /// <returns>The resized image.</returns>
        public static Bitmap ResizeImage(Image img, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(img.HorizontalResolution, img.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(img, destRect, 0, 0, img.Width, img.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }

        /// <summary>
        /// Converts an image to an icon, automatically resizing to the maximum icon size if greater than 128px.
        /// </summary>
        /// <param name="image">The image to resize.</param>
        /// <returns>The resized image.</returns>
        public static Icon ConvertImageToIcon(Image img)
        {
            // Ensure the incoming image is < 128px in width/height.
            if ((img.Width > 128) || (img.Height > 128))
            {
                img = ResizeImage(img, 128, 128);
            }

            Icon icon;
            using (var msImg = new MemoryStream())
            using (var msIco = new MemoryStream())
            {
                img.Save(msImg, ImageFormat.Png);
                using (var bw = new BinaryWriter(msIco))
                {
                    bw.Write((short)0);           //0-1 reserved
                    bw.Write((short)1);           //2-3 image type, 1 = icon, 2 = cursor
                    bw.Write((short)1);           //4-5 number of images
                    bw.Write((byte)img.Width);    //6 image width
                    bw.Write((byte)img.Height);   //7 image height
                    bw.Write((byte)0);            //8 number of colors
                    bw.Write((byte)0);            //9 reserved
                    bw.Write((short)0);           //10-11 color planes
                    bw.Write((short)32);          //12-13 bits per pixel
                    bw.Write((int)msImg.Length);  //14-17 size of image data
                    bw.Write(22);                 //18-21 offset of image data
                    bw.Write(msImg.ToArray());    // write image data
                    bw.Flush();
                    bw.Seek(0, SeekOrigin.Begin);
                    icon = new Icon(msIco);
                }
            }
            return icon;
        }
    }
}
