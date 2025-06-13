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

namespace iNKORE.UI.WPF.Modern.Gallery.Pages.Controls.Windows
{
    /// <summary>
    /// Interaction logic for ProgressRingPage.xaml
    /// </summary>
    public partial class ProgressRingPage
    {
        public ProgressRingPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateExampleCode();
        }

        private void ProgressToggle_Toggled(object sender, RoutedEventArgs e)
        {
            UpdateExampleCode();
        }

        private void ProgressValue_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
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
<ui:ProgressRing IsActive=""{ProgressToggle.IsOn}"" />
";

        public string Example2Xaml => $@"
<ui:ProgressRing Value=""{ProgressValue.Value}""/>
";

        #endregion

    }
}
