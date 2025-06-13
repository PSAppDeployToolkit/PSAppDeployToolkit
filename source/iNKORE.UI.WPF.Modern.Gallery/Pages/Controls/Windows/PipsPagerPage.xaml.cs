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
using Page = iNKORE.UI.WPF.Modern.Controls.Page;

namespace iNKORE.UI.WPF.Modern.Gallery.Pages.Controls.Windows
{
    /// <summary>
    /// PipsPagerPage.xaml 的交互逻辑
    /// </summary>
    public partial class PipsPagerPage : Page
    {
        public List<string> Pictures = new List<string>()
        {
            "/Assets/SampleMedia/LandscapeImage1.jpg",
            "/Assets/SampleMedia/LandscapeImage2.jpg",
            "/Assets/SampleMedia/LandscapeImage3.jpg",
            "/Assets/SampleMedia/LandscapeImage4.jpg",
            "/Assets/SampleMedia/LandscapeImage5.jpg",
            "/Assets/SampleMedia/LandscapeImage6.jpg",
            "/Assets/SampleMedia/LandscapeImage7.jpg",
            "/Assets/SampleMedia/LandscapeImage8.jpg",
        };

        public PipsPagerPage()
        {
            this.InitializeComponent();
            DataContext = Pictures;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateExampleCode();
        }

        private void OrientationComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string orientation = e.AddedItems[0].ToString();

            switch (orientation)
            {
                case "Vertical":
                    TestPipsPager2.Orientation = Orientation.Vertical;
                    break;

                case "Horizontal":
                default:
                    TestPipsPager2.Orientation = Orientation.Horizontal;
                    break;
            }

            UpdateExampleCode();
        }

        private void PrevButtonComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string prevButtonVisibility = e.AddedItems[0].ToString();

            switch (prevButtonVisibility)
            {
                case "Visible":
                    TestPipsPager2.PreviousButtonVisibility = PipsPagerButtonVisibility.Visible;
                    break;

                case "VisibleOnPointerOver":
                    TestPipsPager2.PreviousButtonVisibility = PipsPagerButtonVisibility.VisibleOnPointerOver;
                    break;

                case "Collapsed":
                default:
                    TestPipsPager2.PreviousButtonVisibility = PipsPagerButtonVisibility.Collapsed;
                    break;
            }

            UpdateExampleCode();
        }

        private void NextButtonComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string nextButtonVisibility = e.AddedItems[0].ToString();

            switch (nextButtonVisibility)
            {
                case "Visible":
                    TestPipsPager2.NextButtonVisibility = PipsPagerButtonVisibility.Visible;
                    break;

                case "VisibleOnPointerOver":
                    TestPipsPager2.NextButtonVisibility = PipsPagerButtonVisibility.VisibleOnPointerOver;
                    break;

                case "Collapsed":
                default:
                    TestPipsPager2.NextButtonVisibility = PipsPagerButtonVisibility.Collapsed;
                    break;
            }

            UpdateExampleCode();
        }

        #region Example Code

        public void UpdateExampleCode()
        {
            if (!this.IsLoaded) return;

            Example1.Xaml = Example1Xaml;
            Example1.CSharp = Example1CS;
            Example2.Xaml = Example2Xaml;
        }

        public string Example1Xaml => $@"
<StackPanel>
    <ui:FlipView x:Name=""Gallery""
        ItemsSource=""{{Binding}}"">
        <ui:FlipView.ItemTemplate>
            <DataTemplate>
                <Image Source=""{{Binding Mode=OneWay}}"" />
            </DataTemplate>
        </ui:FlipView.ItemTemplate>
    </ui:FlipView>
    <ui:PipsPager x:Name=""FlipViewPipsPager""
        Margin=""0,12,0,0"" HorizontalAlignment=""Center""
        NumberOfPages=""{{Binding Count}}""
        SelectedPageIndex=""{{Binding SelectedIndex, ElementName=Gallery, Mode=TwoWay}}"" />
</StackPanel>
";

        public string Example1CS => $@"
public List<string> Pictures = new List<string>()
{{
    ""/Assets/SampleMedia/LandscapeImage1.jpg"",
    ""/Assets/SampleMedia/LandscapeImage2.jpg"",
    ""/Assets/SampleMedia/LandscapeImage3.jpg"",
    ""/Assets/SampleMedia/LandscapeImage4.jpg"",
    ""/Assets/SampleMedia/LandscapeImage5.jpg"",
    ""/Assets/SampleMedia/LandscapeImage6.jpg"",
    ""/Assets/SampleMedia/LandscapeImage7.jpg"",
    ""/Assets/SampleMedia/LandscapeImage8.jpg"",
}};
 
this.DataContext = Pictures;
";

        public string Example2Xaml => $@"
<ui:PipsPager x:Name=""TestPipsPager2""  Orientation=""{TestPipsPager2.Orientation}"" 
    PreviousButtonVisibility=""{TestPipsPager2.PreviousButtonVisibility}"" NextButtonVisibility=""{TestPipsPager2.NextButtonVisibility}""/>
";

        #endregion
    }
}
