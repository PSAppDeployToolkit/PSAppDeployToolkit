using System.Drawing;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PSADT.UserInterface.LibraryInterfaces;

namespace PSADT.UserInterface.Extensions
{
    /// <summary>
    /// Bitmap extensions
    /// </summary>
    public static class BitmapExtensions
    {
        /// <summary>
        /// Convert a bitmap to an image source
        /// </summary>
        public static ImageSource ConvertToImageSource(this Bitmap bitmap)
        {
            if (bitmap == null)
            {
                throw new ArgumentNullException(nameof(bitmap));
            }

            IntPtr hBitmap = bitmap.GetHbitmap();
            try
            {
                BitmapSource bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());

                bitmapSource.Freeze(); // Make the image source thread-safe
                return bitmapSource;
            }
            finally
            {
                Gdi32.DeleteObject(hBitmap);
            }
        }
    }
}
