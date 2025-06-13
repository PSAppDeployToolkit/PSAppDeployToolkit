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
    /// Interaction logic for ShadowPage.xaml
    /// </summary>
    public partial class ShadowPage
    {
        public ShadowPage()
        {
            InitializeComponent();
        }

        #region Example Code

        public void UpdateExampleCode()
        {
            if (!this.IsLoaded) return;

            Example1.Xaml = Example1Xaml;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateExampleCode();
        }

        private void DepthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateExampleCode();
        }


        public string Example1Xaml => $@"
<ui:ThemeShadowChrome
    Depth=""{DepthSlider.Value}""
    IsShadowEnabled=""True"">
    <Rectangle Width=""200"" Height=""200""
        Fill=""{{DynamicResource {{x:Static ui:ThemeKeys.SystemControlBackgroundAltHighBrushKey}}}}"" />
</ui:ThemeShadowChrome>
";

        #endregion
    }
}
