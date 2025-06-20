namespace iNKORE.UI.WPF.ColorPicker.Models
{
    internal static class MathHelper

    {
        public static double Clamp(double value, double min, double max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }

        public static double Mod(double value, double m)
        {
            return (value % m + m) % m;
        }
    }
}
