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
using iNKORE.UI.WPF.Modern.Common; 
using iNKORE.UI.WPF.Modern.Controls;
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

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox pb)
            {
                if (string.IsNullOrEmpty(pb.Password) || pb.Password == "Password")
                {
                    Control1Output.Visibility = Visibility.Visible;
                    Control1Output.Text = "'Password' is not allowed.";
                    pb.Password = string.Empty;
                }
                else
                {
                    Control1Output.Text = string.Empty;
                    Control1Output.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void RevealModeCheckbox_Changed(object sender, RoutedEventArgs e)
        {
            if (revealModeCheckBox.IsChecked == true)
            {
                PasswordBoxHelper.SetPasswordRevealMode(passwordBoxWithReveal, PasswordRevealMode.Visible);
            }
            else
            {
                PasswordBoxHelper.SetPasswordRevealMode(passwordBoxWithReveal, PasswordRevealMode.Hidden);
            }
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
            Example3.Xaml = Example3Xaml;
        }

        public string Example1Xaml => $@"
<PasswordBox Width=""300"" AutomationProperties.Name=""Simple PasswordBox""/>
";

        public string Example2Xaml => $@"
<PasswordBox x:Name=""passwordBox"" Width=""300"" ui:ControlHelper.Header=""Password"" ui:ControlHelper.PlaceholderText=""Enter your password"" PasswordChar=""#"" />
";

public string Example3Xaml => $@"
<PasswordBox x:Name=""passwordBoxWithReveal"" Width=""250"" Margin=""0,0,8,0""
    ui:PasswordBoxHelper.PasswordRevealMode=""Hidden"" AutomationProperties.Name=""Sample password box""/>
<CheckBox x:Name=""revealModeCheckBox"" Content=""Show password"" IsChecked=""False""
    Checked=""RevealModeCheckbox_Changed"" Unchecked=""RevealModeCheckbox_Changed""/>
";

        #endregion
    }
}
