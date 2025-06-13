using iNKORE.UI.WPF.Modern.Gallery.DataModel;
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
    /// FlipViewPage.xaml 的交互逻辑
    /// </summary>
    public partial class FlipViewPage : Page
    {
        public FlipViewPage()
        {
            InitializeComponent();
            UpdateExampleCode();
        }

        public IEnumerable<ControlInfoDataItem> Items { get; private set; }


        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            Items = ControlInfoDataSource.Instance.AllGroups.Take(3).SelectMany(g => g.Items).ToList();
            UpdateExampleCode();
        }

        #region Example Code

        public void UpdateExampleCode()
        {
            Example1.Xaml = Example1Xaml;
            Example2.Xaml = Example2Xaml;
            Example3.Xaml = Example3Xaml;
        }

        public string Example1Xaml => $@"
<ui:FlipView>
    <Image AutomationProperties.Name=""Cliff"" Source=""/Assets/SampleMedia/cliff.jpg"" />
    <Image AutomationProperties.Name=""Grapes"" Source=""/Assets/SampleMedia/grapes.jpg"" />
    <Image AutomationProperties.Name=""Rainier"" Source=""/Assets/SampleMedia/rainier.jpg"" />
    <Image AutomationProperties.Name=""Sunset"" Source=""/Assets/SampleMedia/sunset.jpg"" />
    <Image AutomationProperties.Name=""Valley"" Source=""/Assets/SampleMedia/valley.jpg"" />
</ui:FlipView>
";

        public string Example2Xaml => $@"
<ui:FlipView BorderBrush=""Black"" BorderThickness=""1""
    ItemsSource=""{{Binding Items, Mode=OneWay}}"">
    <ui:FlipView.ItemTemplate>
        <DataTemplate>
            <Grid>
                <Grid.RowDefinitions>
                    <RowDefinition Height=""*"" />
                    <RowDefinition Height=""Auto"" />
                </Grid.RowDefinitions>
                <Image
                    Width=""36""
                    VerticalAlignment=""Center""
                    Source=""{{Binding ImagePath}}""
                    Stretch=""Uniform"" />
                <Border
                    Grid.Row=""1""
                    Height=""60""
                    Background=""#A5FFFFFF"">
                    <TextBlock
                        x:Name=""Control2Text""
                        Padding=""12,12""
                        HorizontalAlignment=""Center""
                        Foreground=""Black""
                        Style=""{{StaticResource TitleTextBlockStyle}}""
                        Text=""{{Binding Title}}"" />
                </Border>
            </Grid>
        </DataTemplate>
    </ui:FlipView.ItemTemplate>
</ui:FlipView>
";

        public string Example3Xaml => $@"
<ui:FlipView Orientation=""Vertical"">
    <Image AutomationProperties.Name=""Cliff"" Source=""/Assets/SampleMedia/cliff.jpg"" />
    <Image AutomationProperties.Name=""Grapes"" Source=""/Assets/SampleMedia/grapes.jpg"" />
    <Image AutomationProperties.Name=""Rainier"" Source=""/Assets/SampleMedia/rainier.jpg"" />
    <Image AutomationProperties.Name=""Sunset"" Source=""/Assets/SampleMedia/sunset.jpg"" />
    <Image AutomationProperties.Name=""Valley"" Source=""/Assets/SampleMedia/valley.jpg"" />
    <ui:FlipView.ItemsPanel>
        <ItemsPanelTemplate>
            <VirtualizingStackPanel Orientation=""Vertical"" />
        </ItemsPanelTemplate>
    </ui:FlipView.ItemsPanel>
</ui:FlipView>
";

        #endregion

    }
}
