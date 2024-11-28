using System.Diagnostics;
using System.Drawing;
using static PSADT.UserInterface.Utilities.NativeMethods;

namespace PSADT.UserInterface.Utilities
{
    public static class IconExtractor
    {
        public static Icon? GetIconFromFile(string filePath, bool largeIcon = true)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentNullException(nameof(filePath));

            IntPtr[] largeIcons = new IntPtr[1];
            IntPtr[] smallIcons = new IntPtr[1];

            try
            {
                uint iconsExtracted = ExtractIconEx(filePath, 0, largeIcons, smallIcons, 1);
                if (iconsExtracted > 0)
                {
                    IntPtr iconHandle = largeIcon ? largeIcons[0] : smallIcons[0];
                    if (iconHandle != IntPtr.Zero)
                    {
                        Icon icon = (Icon)Icon.FromHandle(iconHandle).Clone();
                        DestroyIcon(iconHandle);
                        return icon;
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error extracting icon from file: {ex.Message}");
            }
            finally
            {
                foreach (var ptr in largeIcons)
                {
                    if (ptr != IntPtr.Zero)
                        DestroyIcon(ptr);
                }

                foreach (var ptr in smallIcons)
                {
                    if (ptr != IntPtr.Zero)
                        DestroyIcon(ptr);
                }
            }

            return null;
        }
    }
}