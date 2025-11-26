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
    /// ParallaxViewPage.xaml 的交互逻辑
    /// </summary>
    public partial class ParallaxViewPage : Page
    {
        public ParallaxViewPage()
        {
            InitializeComponent();
        }

        public IEnumerable<ControlInfoDataItem> Items { get; private set; }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            Items = ControlInfoDataSource.Instance.AllGroups.SelectMany(g => g.Items).OrderBy(i => i.Title).ToList();
            UpdateExampleCode();
        }

        #region Example Code

        public void UpdateExampleCode()
        {
            Example1.Xaml = Example1Xaml;
            Example2.Xaml = Example2Xaml;
        }

        public string Example1Xaml => $@"
<Grid>
    <ui:ParallaxView x:Name=""parallaxView""
        HorizontalAlignment=""Stretch""
        VerticalAlignment=""Stretch""
        Source=""{{Binding ElementName=listView}}""
        VerticalShift=""500"">
        <Image Source=""/Assets/SampleMedia/cliff.jpg"" Stretch=""UniformToFill"" />
    </ui:ParallaxView>
    <ui:ListView x:Name=""listView""
        HorizontalAlignment=""Stretch""
        VerticalAlignment=""Top""
        Background=""#80000000""
        ItemsSource=""{{Binding Items}}"">
        <ui:ListView.ItemTemplate>
            <DataTemplate>
                <TextBlock Foreground=""{{DynamicResource SystemControlForegroundAltHighBrush}}"" Text=""{{Binding Title}}"" />
            </DataTemplate>
        </ui:ListView.ItemTemplate>
        <ui:ListView.Header>
            <TextBlock MaxWidth=""280""
                HorizontalAlignment=""Center""
                VerticalAlignment=""Center""
                FontSize=""28""
                Foreground=""White""
                Text=""Scroll the list to see parallaxing of images""
                TextWrapping=""Wrap"" />
        </ui:ListView.Header>
    </ui:ListView>
</Grid>
";

        public string Example2Xaml => $@"
<Grid>
    <ui:ParallaxView
        HorizontalAlignment=""Stretch""
        VerticalAlignment=""Stretch""
        Source=""{{Binding ElementName=scrollViewer}}""
        VerticalShift=""500"">
        <Image Source=""/Assets/SampleMedia/cliff.jpg"" Stretch=""UniformToFill"" />
    </ui:ParallaxView>
    <TextBlock
        MaxWidth=""280""
        HorizontalAlignment=""Center""
        VerticalAlignment=""Top""
        FontSize=""28""
        Foreground=""White""
        Text=""Scroll the rectangles to see parallaxing of image""
        TextWrapping=""Wrap"" />
    <ui:ScrollViewerEx x:Name=""scrollViewer"" Width=""150"" HorizontalAlignment=""Left"">
        <StackPanel>
            <Rectangle Height=""150"" Fill=""AliceBlue"" />
            <!-- ... -->
            <Rectangle Height=""150"" Fill=""Cyan"" />
        </StackPanel>
    </ui:ScrollViewerEx>
</Grid>
";

        #endregion

    }
}
