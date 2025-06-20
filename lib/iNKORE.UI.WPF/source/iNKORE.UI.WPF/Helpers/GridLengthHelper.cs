using System.Windows;

namespace iNKORE.UI.WPF.Helpers
{
    public static class GridLengthHelper
    {
        public static GridLength FromPixels(double pixels)
        {
            return new GridLength(pixels);
        }

        public static GridLength FromValueAndType(double value, GridUnitType type)
        {
            return new GridLength(value, type);
        }
    }
}
