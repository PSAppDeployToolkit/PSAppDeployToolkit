using System.Windows;
using System.Windows.Controls;
using iNKORE.UI.WPF.Modern.Controls;

namespace iNKORE.UI.WPF.Modern.Gallery.Pages.Controls.Windows
{
    public partial class StackPanelPage
    {
        public StackPanelPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateExampleCode();
        }

        private void Slider_Spacing_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateExampleCode();
        }

        private void RadioButtons_Orientation_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateExampleCode();
        }

        #region Example Code

        public void UpdateExampleCode()
        {
            if (!this.IsLoaded) return;

            Example1.Xaml = Example1Xaml;
        }

        public string Example1Xaml => $@"
<ikw:SimpleStackPanel x:Name=""Control1""
    Orientation=""{RadioButtons_Orientation.SelectedItem}"" Spacing=""{Slider_Spacing.Value}"">
    <ikw:SimpleStackPanel.Resources>
        <Style TargetType=""Rectangle"">
            <Setter Property=""Height"" Value=""40"" />
            <Setter Property=""Width"" Value=""40"" />
        </Style>
    </ikw:SimpleStackPanel.Resources>
    <Rectangle Fill=""Red"" />
    <Rectangle Fill=""Blue"" />
    <Rectangle Fill=""Green"" />
    <Rectangle Fill=""Yellow"" />
</ikw:SimpleStackPanel>
";

        #endregion
    }
}
