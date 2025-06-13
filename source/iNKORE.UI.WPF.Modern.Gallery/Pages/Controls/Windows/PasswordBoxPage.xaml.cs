using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using iNKORE.UI.WPF.Modern.Controls.Helpers;

namespace iNKORE.UI.WPF.Modern.Gallery.Pages.Controls.Windows
{
    /// <summary>
    /// Interaction logic for PasswordBoxPage.xaml
    /// </summary>
    public partial class PasswordBoxPage
    {
        public PasswordBoxPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateExampleCode();
        }

        private void RadioButtons_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateExampleCode();
        }


        #region Example Code

        public void UpdateExampleCode()
        {
            if (!this.IsLoaded) return;

            Example1.Xaml = Example1Xaml;
            Example2.Xaml = Example2Xaml;
        }

        public string Example1Xaml => $@"
<PasswordBox/>
";

        public string Example2Xaml => $@"
<PasswordBox x:Name=""passwordBox""
    ui:ControlHelper.Header=""Password"" PasswordChar=""#"" 
    ui:ControlHelper.PlaceholderText=""Enter your password""
    ui:PasswordBoxHelper.PasswordRevealMode=""{PasswordBoxHelper.GetPasswordRevealMode(passwordBox)}"" />
";

        #endregion
    }
}
