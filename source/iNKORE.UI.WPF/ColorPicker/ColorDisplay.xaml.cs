using System.Windows;
using System.Windows.Input;

namespace iNKORE.UI.WPF.ColorPicker
{
    public partial class ColorDisplay : DualPickerControlBase
    {
        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(nameof(CornerRadius), typeof(double), typeof(ColorDisplay)
                , new PropertyMetadata(0d));

        public double CornerRadius
        {
            get { return (double)GetValue(CornerRadiusProperty); }
            set { SetValue(CornerRadiusProperty, value); }
        }


        public ColorDisplay() : base()
        {
            InitializeComponent();
        }

        private void SwapButton_Click(object sender, RoutedEventArgs e)
        {
            SwapColors();
        }

        private void HintColor_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            SetMainColorFromHintColor();
        }
    }
}
