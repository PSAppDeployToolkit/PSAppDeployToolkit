using System.Windows;
using System.Windows.Media;

namespace iNKORE.UI.WPF.ColorPicker.UIExtensions
{
    internal class RgbColorSlider : PreviewColorSlider
    {
        public static readonly DependencyProperty SliderArgbTypeProperty =
            DependencyProperty.Register(nameof(SliderArgbType), typeof(string), typeof(RgbColorSlider),
                new PropertyMetadata(""));

        public RgbColorSlider() : base() { }

        public string SliderArgbType
        {
            get => (string)GetValue(SliderArgbTypeProperty);
            set => SetValue(SliderArgbTypeProperty, value);
        }
        protected override void GenerateBackground()
        {
            var colorStart = GetColorForSelectedArgb(0);
            var colorEnd = GetColorForSelectedArgb(255);
            LeftCapColor.Color = colorStart;
            RightCapColor.Color = colorEnd;
            BackgroundGradient = new GradientStopCollection
            {
                new GradientStop(colorStart, 0.0),
                new GradientStop(colorEnd, 1)
            };
        }

        private Color GetColorForSelectedArgb(int value)
        {
            byte a = (byte)(CurrentColorState.A * 255);
            byte r = (byte)(CurrentColorState.RGB_R * 255);
            byte g = (byte)(CurrentColorState.RGB_G * 255);
            byte b = (byte)(CurrentColorState.RGB_B * 255);
            switch (SliderArgbType)
            {
                case "A": return Color.FromArgb((byte)value, r, g, b);
                case "R": return Color.FromArgb(255, (byte)value, g, b);
                case "G": return Color.FromArgb(255, r, (byte)value, b);
                case "B": return Color.FromArgb(255, r, g, (byte)value);
                default: return Color.FromArgb(a, r, g, b);
            };
        }
    }
}
