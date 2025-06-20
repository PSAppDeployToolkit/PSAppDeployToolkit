using iNKORE.UI.WPF.ColorPicker.Models;
using System.Windows;

namespace iNKORE.UI.WPF.ColorPicker
{
    public partial class StandardColorPickerOptimized : DualPickerControlBase
    {
        public static readonly DependencyProperty SmallChangeProperty =
            StandardColorPicker.SmallChangeProperty.AddOwner(typeof(StandardColorPickerOptimized));

        public static readonly DependencyProperty ShowAlphaProperty =
            StandardColorPicker.ShowAlphaProperty.AddOwner(typeof(StandardColorPickerOptimized));

        public static readonly DependencyProperty PickerTypeProperty =
            StandardColorPicker.PickerTypeProperty.AddOwner(typeof(StandardColorPickerOptimized));

        public double SmallChange
        {
            get => (double)GetValue(SmallChangeProperty);
            set => SetValue(SmallChangeProperty, value);
        }

        public bool ShowAlpha
        {
            get => (bool)GetValue(ShowAlphaProperty);
            set => SetValue(ShowAlphaProperty, value);
        }

        public PickerType PickerType
        {
            get => (PickerType)GetValue(PickerTypeProperty);
            set => SetValue(PickerTypeProperty, value);
        }

        public StandardColorPickerOptimized() : base()
        {
            InitializeComponent();
        }
    }
}
