using iNKORE.UI.WPF.ColorPicker.Models;
using System.Windows;

namespace iNKORE.UI.WPF.ColorPicker
{
    public partial class SquarePicker : PickerControlBase
    {
        public static DependencyProperty PickerTypeProperty
            = DependencyProperty.Register(nameof(PickerType), typeof(PickerType), typeof(SquarePicker),
                new PropertyMetadata(PickerType.HSV));

        public static readonly DependencyProperty SmallChangeProperty =
            DependencyProperty.Register(nameof(SmallChange), typeof(double), typeof(SquarePicker),
                new PropertyMetadata(1.0));

        public PickerType PickerType
        {
            get => (PickerType)GetValue(PickerTypeProperty);
            set => SetValue(PickerTypeProperty, value);
        }

        public double SmallChange
        {
            get => (double)GetValue(SmallChangeProperty);
            set => SetValue(SmallChangeProperty, value);
        }

        public SquarePicker() : base()
        {
            InitializeComponent();
        }
    }
}
