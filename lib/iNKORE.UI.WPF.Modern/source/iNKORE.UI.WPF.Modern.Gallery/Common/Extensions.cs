using System.Text.Json;
using System.Windows;
using System.Windows.Media;

namespace iNKORE.UI.WPF.Modern.Gallery
{
    public static class Extensions
    {
        public static void ToggleTheme(this FrameworkElement element)
        {
            ElementTheme newTheme;
            if (ThemeManager.GetActualTheme(element) == ElementTheme.Dark)
            {
                newTheme = ElementTheme.Light;
            }
            else
            {
                newTheme = ElementTheme.Dark;
            }
            ThemeManager.SetRequestedTheme(element, newTheme);
        }

        public static string ToHEX(this Color color)
        {
            return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }

        public static JsonElement? TryGetProperty(this JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var value))
            {
                return value;
            }

            return null;
        }
    }
}
