using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace iNKORE.UI.WPF.Modern.Gallery.Pages.Controls.Windows
{
    /// <summary>
    /// Interaction logic for ToggleSwitchPage.xaml
    /// </summary>
    public partial class ToggleSwitchPage
    {
        public ToggleSwitchPage()
        {
            InitializeComponent();
            UpdateExampleCode();
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            UpdateExampleCode();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateExampleCode();
        }


        #region Example Code

        public void UpdateExampleCode()
        {
            if (!this.IsInitialized) return;

            Example1.Xaml = Example1Xaml;
            Example2.Xaml = Example2Xaml;
        }

        public string Example1Xaml => $@"
<ui:ToggleSwitch AutomationProperties.Name=""simple ToggleSwitch"" />
";

        public string Example2Xaml => $@"
<ui:ToggleSwitch x:Name=""toggleSwitch""
    Header=""{toggleSwitch.Header}"" IsOn=""{toggleSwitch.IsOn}""
    OffContent=""{toggleSwitch.OffContent}"" OnContent=""{toggleSwitch.OnContent}""/>
";

        #endregion

    }
}
