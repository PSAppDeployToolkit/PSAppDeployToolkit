using System.Windows;
using System.Windows.Controls;

namespace iNKORE.UI.WPF.Modern.Gallery.Pages.Controls.Windows
{
    public partial class PopupPlacementPage
    {
        public PopupPlacementPage()
        {
            InitializeComponent();
        }

        private void RadioButtons_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateExampleCode();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
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
<ToggleButton x:Name=""Control1""
    Content=""Open/close popup"" />
<Popup x:Name=""Popup1""
    AllowsTransparency=""True""
    IsOpen=""{{Binding ElementName=Control1, Path=IsChecked}}""
    Placement=""{Popup1.Placement}""
    PlacementTarget=""{{Binding ElementName=Control1}}"">
    <Border>
        <ui:ThemeShadowChrome>
            <Border
                Width=""100""
                Height=""100""
                Background=""{{DynamicResource AcrylicBackgroundFillColorDefaultBrush}}""
                BorderBrush=""{{DynamicResource SystemControlHighlightAccentBrush}}""
                BorderThickness=""5"" />
        </ui:ThemeShadowChrome>
    </Border>
</Popup>
";

        #endregion
    }
}
