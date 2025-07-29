using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using PSADT.UserInterface.Utilities;

namespace PSADT.UserInterface.Dialogs.Classic
{
    internal static class ClassicAssets
    {
        /// <summary>
        /// Get the icon for the dialog.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        internal static Icon GetIcon(string path)
        {
            // Use a cached icon if available, otherwise load and cache it before returning it.
            if (!iconCache.TryGetValue(path, out Icon? icon))
            {
                using var source = !Path.GetExtension(path).Equals(".ico", StringComparison.OrdinalIgnoreCase) ? DrawingUtilities.ConvertBitmapToIcon(path) : new Icon(path);
                icon = (Icon)source.Clone();
                iconCache.Add(path, icon);
            }
            return icon;
        }

        /// <summary>
        /// Get the banner image for the dialog.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        internal static Bitmap GetBanner(string path)
        {
            // Use a cached image if available, otherwise load and cache it before returning it.
            if (!imageCache.TryGetValue(path, out Bitmap? image))
            {
                using var source = Bitmap.FromFile(path);
                image = (Bitmap)source.Clone();
                imageCache.Add(path, image);
            }
            return image;
        }

        /// <summary>
        /// Cache for icons to avoid loading them multiple times.
        /// </summary>
        private static readonly Dictionary<string, Icon> iconCache = [];

        /// <summary>
        /// Cache for banners to avoid loading them multiple times.
        /// </summary>
        private static readonly Dictionary<string, Bitmap> imageCache = [];
    }
}
