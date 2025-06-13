using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace iNKORE.UI.WPF.Modern.Controls
{
    internal partial class InputBoxContent : UserControl
    {
        public InputBoxContent()
        {
            InitializeComponent();

            Loaded += InputBoxContent_Loaded;
        }

        private void InputBoxContent_Loaded(object sender, RoutedEventArgs e)
        {
            responseTextControl.Focus();
            Keyboard.Focus(responseTextControl);
        }
    }
}
