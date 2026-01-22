using System;
using System.Collections.ObjectModel;
using System.IO;
using PSADT.LibraryInterfaces;
using Windows.Win32.Foundation;

namespace PSADT.Utilities
{
    /// <summary>
    /// Provides methods for managing font resources.
    /// </summary>
    public static class FontUtilities
    {
        /// <summary>
        /// Installs a font resource from the specified file.
        /// </summary>
        /// <param name="fontFilePath">The full path to the font file.</param>
        /// <returns>The number of fonts added if successful; otherwise, 0.</returns>
        /// <exception cref="FileNotFoundException">Thrown when the font file does not exist.</exception>
        public static int AddFont(string fontFilePath)
        {
            if (!File.Exists(fontFilePath))
            {
                throw new FileNotFoundException("Font file not found.", fontFilePath);
            }
            int result = Gdi32.AddFontResource(fontFilePath);
            _ = User32.SendNotifyMessage(HWND.HWND_BROADCAST, WINDOW_MESSAGE.WM_FONTCHANGE);
            return result;
        }

        /// <summary>
        /// Removes a font resource from the specified file.
        /// </summary>
        /// <param name="fontFilePaths">The full path to the font file.</param>
        /// <returns>True if the font was removed successfully; otherwise, false.</returns>
        /// ///
        public static int AddFonts(ReadOnlyCollection<string> fontFilePaths)
        {
            if (fontFilePaths is null)
            {
                throw new ArgumentNullException(nameof(fontFilePaths));
            }

            int result = 0;
            foreach (string fontFilePath in fontFilePaths)
            {
                if (!File.Exists(fontFilePath))
                {
                    throw new FileNotFoundException("Font file not found.", fontFilePath);
                }

                // Add the font resource
                result += Gdi32.AddFontResource(fontFilePath);
            }

            if (result > 0)
            {
                // Notify all top-level windows that the font table has changed
                _ = User32.SendNotifyMessage(HWND.HWND_BROADCAST, WINDOW_MESSAGE.WM_FONTCHANGE, (WPARAM)0, (LPARAM)0);
            }
            return result;
        }

        /// <summary>
        /// Removes a font resource from the specified file.
        /// </summary>
        /// <param name="fontFilePath">The full path to the font file.</param>
        /// <returns>True if the font was removed successfully; otherwise, false.</returns>
        /// ///
        public static bool RemoveFont(string fontFilePath)
        {
            // Remove the font resource
            // We don't check for file existence here because the file might be gone, but the resource still registered
            bool result = Gdi32.RemoveFontResource(fontFilePath);

            if (result)
            {
                // Notify all top-level windows that the font table has changed
                _ = User32.SendNotifyMessage(HWND.HWND_BROADCAST, WINDOW_MESSAGE.WM_FONTCHANGE, (WPARAM)0, (LPARAM)0);
            }

            return result;
        }
    }
}
