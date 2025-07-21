using iNKORE.UI.WPF.Modern.Controls;
using iNKORE.UI.WPF.Modern.Media;
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
using System.Diagnostics;                                  
using iNKORE.UI.WPF.Modern.Gallery.DataModel;
using Page = iNKORE.UI.WPF.Modern.Controls.Page;

namespace iNKORE.UI.WPF.Modern.Gallery.Pages.Controls.Windows
{
    /// <summary>
    /// AcrylicPage.xaml 的交互逻辑
    /// </summary>
    public partial class AcrylicPage : Page
    {
        public AcrylicPage()
        {
            InitializeComponent();
            Loaded += AcrylicPage_Loaded;
        }

        private void AcrylicPage_Loaded(object sender, RoutedEventArgs e)
        {
            ColorSelectorInApp.SelectedIndex = 0;
            UpdateExampleCode();
        }

        private void ColorSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AcrylicPanel shape = CustomAcrylicShapeInApp;
            shape.TintColor = ((SolidColorBrush)e.AddedItems[0]).Color;

            UpdateExampleCode();
        }

        private void OpacitySliderInApp_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateExampleCode();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }

        private async void SystemBackdropLink_Click(object sender, RoutedEventArgs e)
        {
            var realms = await ControlInfoDataSource.Instance.GetRealmsAsync();
            var item = realms
                .SelectMany(r => r.Groups)
                .SelectMany(g => g.Items)
                .FirstOrDefault(ci => ci.UniqueId == "SystemBackdrops");
            if (item == null) return;

            NavigationRootPage.RootFrame.Navigate(ItemPage.Create(item));
        }

        private void OpacitySliderLumin_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateExampleCode();
        }

        private void LuminositySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateExampleCode();
        }

        private void UpdateExampleCode()
        {
            Example1.Xaml = Example1Xaml;
            Example3.Xaml = Example3Xaml;
            Example4.Xaml = Example4Xaml;
        }

        string Example1Xaml => @"            
<Grid x:Name=""Acrylic1Grid"" Background=""{DynamicResource {x:Static ui:ThemeKeys.SolidBackgroundFillColorBaseBrushKey}}"">
    <Rectangle
        Width=""100""
        Height=""200""
        HorizontalAlignment=""Left""
        VerticalAlignment=""Top""
        Fill=""Aqua"" />
    <Ellipse
        Width=""152""
        Height=""152""
        HorizontalAlignment=""Center""
        VerticalAlignment=""Center""
        Fill=""Magenta"" />
    <Rectangle
        Width=""80""
        Height=""100""
        HorizontalAlignment=""Right""
        VerticalAlignment=""Bottom""
        Fill=""Yellow"" />
</Grid>
";

        string Example3Xaml => $@"
<Grid x:Name=""Example3Grid""
    Width=""320"" Height=""200""
    HorizontalAlignment=""Left"">
    <Grid x:Name=""Acrylic3Grid"" 
        Background=""{{DynamicResource {{x:Static ui:ThemeKeys.SolidBackgroundFillColorBaseBrushKey}}}}"">
        <Rectangle Width=""100"" Height=""200"" Fill=""Aqua""
            HorizontalAlignment=""Left"" VerticalAlignment=""Top"" />
        <Ellipse Width=""152"" Height=""152"" Fill=""Magenta""
            HorizontalAlignment=""Center"" VerticalAlignment=""Center"" />
        <Rectangle Width=""80"" Height=""100"" Fill=""Yellow""
            HorizontalAlignment=""Right"" VerticalAlignment=""Bottom"" />
    </Grid>
    <ui:AcrylicPanel x:Name=""CustomAcrylicShapeInApp""
        Margin=""12"" Target=""{{Binding ElementName=Acrylic3Grid}}""
        TintColor=""{CustomAcrylicShapeInApp.TintColor.ToHEX()}"" TintOpacity=""{CustomAcrylicShapeInApp.TintOpacity.ToString()}"" />
</Grid>
";

string Example4Xaml => $@"
<Grid x:Name=""Example4Grid""
    Width=""320"" Height=""200""
    HorizontalAlignment=""Left"">
    <Grid x:Name=""Acrylic4Grid"" 
        Background=""{{DynamicResource {{x:Static ui:ThemeKeys.SolidBackgroundFillColorBaseBrushKey}}}}"">
        <Rectangle
            Width=""100""
            Height=""200""
            HorizontalAlignment=""Left""
            VerticalAlignment=""Top""
            Fill=""Aqua"" />
        <Ellipse
            Width=""152""
            Height=""152""
            HorizontalAlignment=""Center""
            VerticalAlignment=""Center""
            Fill=""Magenta"" />
        <Rectangle
            Width=""80""
            Height=""100""
            HorizontalAlignment=""Right""
            VerticalAlignment=""Bottom""
            Fill=""Yellow"" />
    </Grid>
    <ui:AcrylicPanel x:Name=""CustomAcrylicShapeLumin""
        Margin=""12"" TintColor=""SkyBlue""
        Target=""{{Binding ElementName=Acrylic4Grid}}""
        TintOpacity=""{CustomAcrylicShapeLumin.TintOpacity.ToString()}"" Amount=""{CustomAcrylicShapeLumin.Amount.ToString()}"" />
</Grid>
";

    }
}
