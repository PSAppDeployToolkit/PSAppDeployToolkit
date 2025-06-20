using System.Windows;

namespace iNKORE.UI.WPF.ColorPicker
{
    public partial class AlphaSlider : PickerControlBase
    {
        public static readonly DependencyProperty SmallChangeProperty =
            DependencyProperty.Register(nameof(SmallChange), typeof(double), typeof(AlphaSlider),
                new PropertyMetadata(1.0));

        public double SmallChange
        {
            get => (double)GetValue(SmallChangeProperty);
            set => SetValue(SmallChangeProperty, value);
        }
        public AlphaSlider()
        {
            InitializeComponent();
        }
    }
}
