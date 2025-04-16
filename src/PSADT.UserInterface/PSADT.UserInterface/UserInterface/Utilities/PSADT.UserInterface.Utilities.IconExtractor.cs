using System.Drawing;
using PSADT.UserInterface.LibraryInterfaces;
using Windows.Win32;

namespace PSADT.UserInterface.Utilities
{
    /// <summary>
    /// Icon extractor utility
    /// </summary>
    public static class IconExtractor
    {
        /// <summary>
        /// Extracts an icon from a file
        /// </summary>
        public static Icon? GetIconFromFile(string filePath, bool largeIcon = true)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            uint iconsExtracted = Shell32.ExtractIconEx(filePath, 0, out var largeIcons, out var smallIcons, 1);
            using (largeIcons)
            using (smallIcons)
            {
                if (iconsExtracted > 0 && (largeIcon ? largeIcons : smallIcons) is DestroyIconSafeHandle iconHandle && !iconHandle.IsInvalid)
                {
                    return (Icon)Icon.FromHandle(iconHandle.DangerousGetHandle()).Clone();
                }
                return null;
            }
        }
    }
}
