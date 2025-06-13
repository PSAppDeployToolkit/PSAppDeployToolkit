using iNKORE.UI.WPF.Modern.Controls;
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
using Page = iNKORE.UI.WPF.Modern.Controls.Page;

namespace iNKORE.UI.WPF.Modern.Gallery.Pages.Controls.Windows
{
    /// <summary>
    /// RadioButtonPage.xaml 的交互逻辑
    /// </summary>
    public partial class RadioButtonPage : Page
    {
        public RadioButtonPage()
        {
            InitializeComponent();
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            Control1Output.Text = string.Format("You selected {0}", (sender as RadioButton).Content.ToString());
        }

        #region Example Code

        public void UpdateExampleCode()
        {
            Example1.Xaml = Example1Xaml;
        }

        public string Example1Xaml => $@"
<ui:RadioButtons Header=""Options:"">
    <RadioButton Checked=""RadioButton_Checked"" Content=""Option 1"" />
    <RadioButton Checked=""RadioButton_Checked"" Content=""Option 2"" />
    <RadioButton Checked=""RadioButton_Checked"" Content=""Option 3"" />
</ui:RadioButtons>
";

        #endregion

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateExampleCode();
        }
    }
}
