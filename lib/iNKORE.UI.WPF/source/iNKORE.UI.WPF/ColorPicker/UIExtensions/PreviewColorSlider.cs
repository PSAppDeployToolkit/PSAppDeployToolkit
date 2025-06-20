using iNKORE.UI.WPF.ColorPicker.Models;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace iNKORE.UI.WPF.ColorPicker.UIExtensions
{
    internal abstract class PreviewColorSlider : Slider, INotifyPropertyChanged
    {

        public static readonly DependencyProperty CurrentColorStateProperty =
            DependencyProperty.Register(nameof(CurrentColorState), typeof(ColorState), typeof(PreviewColorSlider),
                new PropertyMetadata(ColorStateChangedCallback));

        public static readonly DependencyProperty SmallChangeBindableProperty =
            DependencyProperty.Register(nameof(SmallChangeBindable), typeof(double), typeof(PreviewColorSlider),
                new PropertyMetadata(1.0, SmallChangeBindableChangedCallback));

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual bool RefreshGradient => true;

        public PreviewColorSlider()
        {
            Minimum = 0;
            Maximum = 255;
            SmallChange = 1;
            LargeChange = 10;
            MinHeight = 12;
            PreviewMouseWheel += OnPreviewMouseWheel;
        }

        public double SmallChangeBindable
        {
            get => (double)GetValue(SmallChangeBindableProperty);
            set => SetValue(SmallChangeBindableProperty, value);
        }

        public ColorState CurrentColorState
        {
            get => (ColorState)GetValue(CurrentColorStateProperty);
            set => SetValue(CurrentColorStateProperty, value);
        }

        private readonly LinearGradientBrush backgroundBrush = new LinearGradientBrush();
        public GradientStopCollection BackgroundGradient
        {
            get => backgroundBrush.GradientStops;
            set => backgroundBrush.GradientStops = value;
        }

        private SolidColorBrush _leftCapColor = new SolidColorBrush();
        public SolidColorBrush LeftCapColor
        {
            get => _leftCapColor;
            set
            {
                _leftCapColor = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LeftCapColor)));
            }
        }

        private SolidColorBrush _rightCapColor = new SolidColorBrush();
        public SolidColorBrush RightCapColor
        {
            get => _rightCapColor;
            set
            {
                _rightCapColor = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RightCapColor)));
            }
        }

        public override void EndInit()
        {
            base.EndInit();
            Background = backgroundBrush;
            GenerateBackground();
        }

        protected abstract void GenerateBackground();
        protected static void ColorStateChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PreviewColorSlider slider = (PreviewColorSlider)d;
            if (slider.RefreshGradient)
                slider.GenerateBackground();
        }

        private static void SmallChangeBindableChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((PreviewColorSlider)d).SmallChange = (double)e.NewValue;
        }

        private void OnPreviewMouseWheel(object sender, MouseWheelEventArgs args)
        {
            Value = MathHelper.Clamp(Value + SmallChange * args.Delta / 120, Minimum, Maximum);
            args.Handled = true;
        }
    }
}
