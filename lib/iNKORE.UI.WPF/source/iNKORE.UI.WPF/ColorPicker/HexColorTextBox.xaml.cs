using System.Windows;
using System.Windows.Controls;

namespace iNKORE.UI.WPF.ColorPicker
{
    public partial class HexColorTextBox : PickerControlBase
    {
        public static readonly DependencyProperty ShowAlphaProperty =
            DependencyProperty.Register(nameof(ShowAlpha), typeof(bool), typeof(HexColorTextBox),
                new PropertyMetadata(true));

        public bool ShowAlpha
        {
            get => (bool)GetValue(ShowAlphaProperty);
            set => SetValue(ShowAlphaProperty, value);
        }

        public HexColorTextBox() : base()
        {
            InitializeComponent();
        }

        private void ColorToHexConverter_OnShowAlphaChange(object sender, System.EventArgs e)
        {
            textbox.GetBindingExpression(TextBox.TextProperty).UpdateTarget();
            //InvalidateProperty(SelectedColorProperty);
            //Color.RaisePropertyChanged(nameof(Color.RGB_R));
        }
    }
}
