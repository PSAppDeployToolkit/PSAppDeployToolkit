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

namespace iNKORE.UI.WPF.Modern.Gallery.Pages.Controls.Windows
{
    /// <summary>
    /// Interaction logic for RatingControlPage.xaml
    /// </summary>
    public partial class RatingControlPage
    {
        public RatingControlPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateExampleCode();
        }

        private void clearEnabledCheck_Click(object sender, RoutedEventArgs e)
        {
            UpdateExampleCode();
        }

        private void readOnlyCheck_Click(object sender, RoutedEventArgs e)
        {
            UpdateExampleCode();
        }

        private void slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateExampleCode();
        }

        private void RatingControl1_ValueChanged(object sender, object e)
        {
            RatingControl1.Caption = "Your rating";

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
<ui:RatingControl x:Name=""RatingControl1""
    AutomationProperties.Name=""Simple RatingControl"" IsClearEnabled=""{clearEnabledCheck.IsChecked}""
    IsReadOnly=""{readOnlyCheck.IsChecked}"" Caption=""{RatingControl1.Caption}""/>
";

        public string Example2Xaml => $@"
<ui:RatingControl x:Name=""RatingControl2"" AutomationProperties.Name=""RatingControl with placeholder"" PlaceholderValue=""{slider.Value}"" />
";


        #endregion
    }
}
