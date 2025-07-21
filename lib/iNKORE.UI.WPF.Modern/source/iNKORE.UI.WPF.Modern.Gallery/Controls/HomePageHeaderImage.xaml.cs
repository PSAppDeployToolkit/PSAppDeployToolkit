using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Controls;
using Microsoft.UI; 
using iNKORE.UI.WPF.Modern;

namespace iNKORE.UI.WPF.Modern.Gallery.Controls
{
    public partial class HomePageHeaderImage : UserControl
    {
        const double GradientHeight = 180;   
        const double AnimationDuration = 300; 

        public HomePageHeaderImage()
        {
            InitializeComponent();
            ApplyTheme(ElementTheme.Light); 
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
        }

        private void ApplyTheme(ElementTheme theme)
        {
            string bgKey, opacityKey;
            switch (theme)
            {
                case ElementTheme.Dark:
                    bgKey = "BackgroundGradientDark";
                    opacityKey = "ImageOpacityDark";
                    break;
                default:
                    bgKey = "BackgroundGradientLight";
                    opacityKey = "ImageOpacityLight";
                    break;
            }
            Resources["BackgroundGradient"] = Resources[bgKey];
            Resources["ImageOpacity"]        = Resources[opacityKey];
        }

        private void ImageGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var h = ImageGrid.ActualHeight;
            GradientMask.StartPoint = new Point(0, h - GradientHeight);
            GradientMask.EndPoint   = new Point(0, h);

            GradientMask.GradientStops.Clear();

            for (double t = 0; t <= 1.0; t += 0.05)
            {
                double eased = new SineEase { EasingMode = EasingMode.EaseInOut }.Ease(1 - t);
                byte alpha   = (byte)(255 * eased);
                GradientMask.GradientStops.Add(new GradientStop(Color.FromArgb(alpha, 0, 0, 0), t));
            }
            GradientMask.GradientStops.Add(new GradientStop(Colors.Transparent, 1.0));
        }
    }
}
