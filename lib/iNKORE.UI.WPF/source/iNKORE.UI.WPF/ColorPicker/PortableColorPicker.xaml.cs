using iNKORE.UI.WPF.ColorPicker.Models;
using System.Windows;

namespace iNKORE.UI.WPF.ColorPicker
{
    public partial class PortableColorPicker : DualPickerControlBase
    {
        public static readonly DependencyProperty SmallChangeProperty =
            DependencyProperty.Register(nameof(SmallChange), typeof(double), typeof(PortableColorPicker),
                new PropertyMetadata(1.0));

        public static readonly DependencyProperty ShowAlphaProperty =
            DependencyProperty.Register(nameof(ShowAlpha), typeof(bool), typeof(PortableColorPicker),
                new PropertyMetadata(true));

        public static readonly DependencyProperty PickerTypeProperty
            = DependencyProperty.Register(nameof(PickerType), typeof(PickerType), typeof(PortableColorPicker),
                new PropertyMetadata(PickerType.HSV));

        public static readonly RoutedEvent ColorPickedEvent=EventManager.RegisterRoutedEvent(nameof(ColorPicked), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(PortableColorPicker));
        public event RoutedEventHandler ColorPicked
        {
            add { AddHandler(ColorPickedEvent, value); }
            remove { RemoveHandler(ColorPickedEvent, value); }
        }

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

        public PortableColorPicker()
        {
            InitializeComponent();
        }

        private void popup_Closed(object sender, System.EventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(ColorPickedEvent));
        }
    }
}
