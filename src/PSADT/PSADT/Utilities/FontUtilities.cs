using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using PSADT.LibraryInterfaces;
using PSADT.LibraryInterfaces.Extensions;
using PSADT.LibraryInterfaces.SafeHandles;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.Shell.PropertiesSystem;

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
                fontResults.Add(fontFilePath, Gdi32.AddFontResource(fontFilePath));
            }
            _ = User32.SendNotifyMessage(HWND.HWND_BROADCAST, WINDOW_MESSAGE.WM_FONTCHANGE);
            return new ReadOnlyDictionary<string, int>(fontResults);
        }

        /// <summary>
        /// Removes a font resource from the specified file.
        /// </summary>
        /// <param name="fontFilePath">The full path to the font file.</param>
        /// <returns>True if the font was removed successfully; otherwise, false.</returns>
        /// ///
        public static void RemoveFont(string fontFilePath)
        {
            // Remove the font resource. We don't check for file existence because the input is just value that names a font resource file.
            // See https://learn.microsoft.com/en-us/windows/win32/api/wingdi/nf-wingdi-removefontresourcew#parameters for more details.
            _ = Gdi32.RemoveFontResource(fontFilePath);
            _ = User32.SendNotifyMessage(HWND.HWND_BROADCAST, WINDOW_MESSAGE.WM_FONTCHANGE);
        }

        /// <summary>
        /// Removes the specified font resources from the system font table.
        /// </summary>
        /// <remarks>After removing the fonts, a system-wide font change notification is broadcast. The
        /// specified file paths are passed directly to the underlying system API and are not validated for existence.
        /// Removing a font does not delete the font file from disk; it only unregisters the font from the
        /// system.</remarks>
        /// <param name="fontFilePaths">A read-only list of file paths that identify the font resource files to remove from the system.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="fontFilePaths"/> is <see langword="null"/>.</exception>
        public static void RemoveFonts(IReadOnlyList<string> fontFilePaths)
        {
            if (fontFilePaths is null)
            {
                throw new ArgumentNullException(nameof(fontFilePaths));
            }
            foreach (string fontFilePath in fontFilePaths)
            {
                // Remove the font resource. We don't check for file existence because the input is just value that names a font resource file.
                // See https://learn.microsoft.com/en-us/windows/win32/api/wingdi/nf-wingdi-removefontresourcew#parameters for more details.
                _ = Gdi32.RemoveFontResource(fontFilePath);
            }
            _ = User32.SendNotifyMessage(HWND.HWND_BROADCAST, WINDOW_MESSAGE.WM_FONTCHANGE);
        }

        /// <summary>
        /// Retrieves the title metadata from a font file, if available.
        /// </summary>
        /// <remarks>This method reads the 'Title' property from the font file's metadata using Windows
        /// Shell APIs. If the font file does not contain title information, the method returns null. The method does
        /// not validate the font file format; ensure the file path points to a valid font file.</remarks>
        /// <param name="fontFilePath">The full path to the font file from which to extract the title. Cannot be null or empty.</param>
        /// <returns>A string containing the title of the font if present; otherwise, null.</returns>
        public static string? GetFontTitle(string fontFilePath)
        {
            _ = Shell32.SHCreateItemFromParsingName(fontFilePath, null, out IShellItem2 shellItem);
            shellItem.GetPropertyStore(GETPROPERTYSTOREFLAGS.GPS_DEFAULT, out IPropertyStore store);
            store.GetValue(in PInvoke.PKEY_Title, out SafePropVariantHandle pv);
            using (pv)
            {
                _ = PropSys.PropVariantToStringAlloc(pv, out SafeCoTaskMemoryHandle ppszOut);
                using (ppszOut)
                {
                    return ppszOut.ToStringUni();
                }
            }
        }
    }
}
