using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using PSADT.Interop;
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
            int result = NativeMethods.AddFontResource(fontFilePath);
            _ = NativeMethods.SendNotifyMessage(HWND.HWND_BROADCAST, WINDOW_MESSAGE.WM_FONTCHANGE);
            return result;
        }

        /// <summary>
        /// Adds font resources from the specified file paths and updates the system to recognize the new fonts.
        /// </summary>
        /// <remarks>After adding the fonts, the method broadcasts a system-wide notification to prompt
        /// all top-level windows to refresh their font settings.</remarks>
        /// <param name="fontFilePaths">A read-only list of strings that specify the file paths of the font files to add. Each path must refer to an
        /// existing font file.</param>
        /// <returns>A read-only dictionary that maps each font file path to its corresponding resource identifier.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="fontFilePaths"/> is <see langword="null"/>.</exception>
        /// <exception cref="FileNotFoundException">Thrown if any file specified in <paramref name="fontFilePaths"/> does not exist.</exception>
        public static IReadOnlyDictionary<string, int> AddFonts(IReadOnlyList<string> fontFilePaths)
        {
            if (fontFilePaths is null)
            {
                throw new ArgumentNullException(nameof(fontFilePaths));
            }
            Dictionary<string, int> fontResults = [];
            foreach (string fontFilePath in fontFilePaths)
            {
                if (!File.Exists(fontFilePath))
                {
                    throw new FileNotFoundException("Font file not found.", fontFilePath);
                }
                fontResults.Add(fontFilePath, NativeMethods.AddFontResource(fontFilePath));
            }
            _ = NativeMethods.SendNotifyMessage(HWND.HWND_BROADCAST, WINDOW_MESSAGE.WM_FONTCHANGE);
            return new ReadOnlyDictionary<string, int>(fontResults);
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
            bool result = NativeMethods.RemoveFontResource(fontFilePath);

            if (result)
            {
                // Notify all top-level windows that the font table has changed
                _ = NativeMethods.SendNotifyMessage(HWND.HWND_BROADCAST, WINDOW_MESSAGE.WM_FONTCHANGE, (WPARAM)0, (LPARAM)0);
            }

            return result;
        }
    }
}
