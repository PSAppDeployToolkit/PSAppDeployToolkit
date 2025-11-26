using iNKORE.UI.WPF.Modern.Controls;
using iNKORE.UI.WPF.Modern.Gallery.Common;
using SamplesCommon.SamplePages;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Threading;
using SamplesCommon;
using Separator = iNKORE.UI.WPF.Modern.Gallery.Common.Separator;
using VirtualKey = System.Windows.Input.Key;
using iNKORE.UI.WPF.Modern.Common.IconKeys;

namespace iNKORE.UI.WPF.Modern.Gallery.Pages.Controls.Windows
{
    public sealed partial class NavigationViewPage
    {
        public static bool CameFromToggle = false;

        public static bool CameFromGridChange = false;

        public VirtualKey ArrowKey;

        public ObservableCollection<CategoryBase> Categories { get; set; }

        public NavigationViewPage()
        {
            DataContext = this;
            InitializeComponent();

            nvSample2.SelectedItem = nvSample2.MenuItems.OfType<NavigationViewItem>().First();
            nvSample5.SelectedItem = nvSample5.MenuItems.OfType<NavigationViewItem>().First();
            nvSample6.SelectedItem = nvSample6.MenuItems.OfType<NavigationViewItem>().First();
            nvSample7.SelectedItem = nvSample7.MenuItems.OfType<NavigationViewItem>().First();
            nvSample8.SelectedItem = nvSample8.MenuItems.OfType<NavigationViewItem>().First();
            nvSample9.SelectedItem = nvSample9.MenuItems.OfType<NavigationViewItem>().First();

            Categories = new ObservableCollection<CategoryBase>();
            Category firstCategory = new Category { Name = "Category 1", Icon = SegoeFluentIcons.Home, Tooltip = "This is category 1" };
            Categories.Add(firstCategory);
            Categories.Add(new Category { Name = "Category 2", Icon = SegoeFluentIcons.KeyboardFull, Tooltip = "This is category 2" });
            Categories.Add(new Category { Name = "Category 3", Icon = SegoeFluentIcons.Library, Tooltip = "This is category 3" });
            Categories.Add(new Category { Name = "Category 4", Icon = SegoeFluentIcons.Mail, Tooltip = "This is category 4" });
            Loaded += delegate
            {
                nvSample4.SelectedItem ??= firstCategory;
                UpdatePaneDisplayModeForSample2();
            };

            SizeChanged += (s, e) => UpdatePaneDisplayModeForSample2();

            

            //setASBSubstitutionString();

            // Fixes #218
            nvSample2.UpdateLayout();
        }

        public NavigationViewPaneDisplayMode ChoosePanePosition(bool toggleOn)
        {
            if (toggleOn)
            {
                return NavigationViewPaneDisplayMode.Left;
            }
            else
            {
                return NavigationViewPaneDisplayMode.Top;
            }
        }

        private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected)
            {
                contentFrame.Navigate(typeof(SampleSettingsPage));
            }
            else
            {
                var selectedItem = (NavigationViewItem)args.SelectedItem;
                if (selectedItem != null)
                {
                    string selectedItemTag = (string)selectedItem.Tag;
                    sender.Header = "Sample Page " + selectedItemTag.Substring(selectedItemTag.Length - 1);
                    string pageName = "SamplesCommon.SamplePages." + selectedItemTag;
                    Type pageType = typeof(SamplePage1).Assembly.GetType(pageName);
                    contentFrame.Navigate(pageType);
                }
            }
        }

        private void NavigationView_SelectionChanged2(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (!CameFromGridChange)
            {
                if (args.IsSettingsSelected)
                {
                    contentFrame2.Navigate(typeof(SampleSettingsPage));
                }
                else
                {
                    var selectedItem = (NavigationViewItem)args.SelectedItem;
                    string selectedItemTag = (string)selectedItem.Tag;
                    string pageName = "SamplesCommon.SamplePages." + selectedItemTag;
                    Type pageType = typeof(SamplePage1).Assembly.GetType(pageName);
                    contentFrame2.Navigate(pageType);
                }
            }

            CameFromGridChange = false;
        }

        private void NavigationView_SelectionChanged4(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected)
            {
                contentFrame4.Navigate(typeof(SampleSettingsPage));
            }
            else
            {
                Debug.WriteLine("Before hitting sample page 1");

                var selectedItem = (Category)args.SelectedItem;
                string selectedItemTag = selectedItem.Name;
                sender.Header = "Sample Page " + selectedItemTag.Substring(selectedItemTag.Length - 1);
                string pageName = "SamplesCommon.SamplePages." + "SamplePage1";
                Type pageType = typeof(SamplePage1).Assembly.GetType(pageName);
                contentFrame4.Navigate(pageType);
            }
        }


        private void NavigationView_SelectionChanged5(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected)
            {
                contentFrame5.Navigate(typeof(SampleSettingsPage));
            }
            else
            {
                var selectedItem = (NavigationViewItem)args.SelectedItem;
                string selectedItemTag = (string)selectedItem.Tag;
                sender.Header = "Sample Page " + selectedItemTag.Substring(selectedItemTag.Length - 1);
                string pageName = "SamplesCommon.SamplePages." + selectedItemTag;
                Type pageType = typeof(SamplePage1).Assembly.GetType(pageName);
                contentFrame5.Navigate(pageType);
            }
        }
        private void NavigationView_SelectionChanged6(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected)
            {
                contentFrame6.Navigate(typeof(SampleSettingsPage));
            }
            else
            {
                var selectedItem = (NavigationViewItem)args.SelectedItem;
                string pageName = "SamplesCommon.SamplePages." + (string)selectedItem.Tag;
                Type pageType = typeof(SamplePage1).Assembly.GetType(pageName);
                contentFrame6.Navigate(pageType);
            }
        }

        private void NavigationView_SelectionChanged7(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected)
            {
                contentFrame7.Navigate(typeof(SampleSettingsPage));
            }
            else
            {
                var selectedItem = (NavigationViewItem)args.SelectedItem;
                string pageName = "SamplesCommon.SamplePages." + (string)selectedItem.Tag;
                Type pageType = typeof(SamplePage1).Assembly.GetType(pageName);

                contentFrame7.Navigate(pageType, null, args.RecommendedNavigationTransitionInfo);
            }
        }

        private void NavigationView_SelectionChanged8(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            /* NOTE: for this function to work, every NavigationView must follow the same naming convention: nvSample# (i.e. nvSample3),
            and every corresponding content frame must follow the same naming convention: contentFrame# (i.e. contentFrame3) */

            // Get the sample number
            string sampleNum = (sender.Name).Substring(8);
            Debug.Print("num: " + sampleNum + "\n");

            if (args.IsSettingsSelected)
            {
                contentFrame8.Navigate(typeof(SampleSettingsPage));
            }
            else
            {
                var selectedItem = (NavigationViewItem)args.SelectedItem;
                string selectedItemTag = ((string)selectedItem.Tag);
                sender.Header = "Sample Page " + selectedItemTag.Substring(selectedItemTag.Length - 1);
                string pageName = "SamplesCommon.SamplePages." + selectedItemTag;
                Type pageType = typeof(SamplePage1).Assembly.GetType(pageName);
                contentFrame8.Navigate(pageType);
            }
        }

        private void NavigationView_SelectionChanged9(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected)
            {
                contentFrame9.Navigate(typeof(SampleSettingsPage));
            }
            else
            {
                var selectedItem = (NavigationViewItem)args.SelectedItem;
                if (selectedItem != null)
                {
                    string selectedItemTag = (string)selectedItem.Tag;
                    //sender.Header = "Sample Page " + selectedItemTag.Substring(selectedItemTag.Length - 1);
                    string pageName = "SamplesCommon.SamplePages." + selectedItemTag;
                    Type pageType = typeof(SamplePage1).Assembly.GetType(pageName);
                    contentFrame9.Navigate(pageType);
                }
            }
        }

        private void databindHeader_Checked(object sender, RoutedEventArgs e)
        {
            Categories = new ObservableCollection<CategoryBase>()
            {
                new Header { Name = "Header1 "},
                new Category { Name = "Category 1", Icon = SegoeFluentIcons.Home, Tooltip = "This is category 1" },
                new Category { Name = "Category 2", Icon = SegoeFluentIcons.KeyboardFull, Tooltip = "This is category 2" },
                new Separator(),
                new Header { Name = "Header2 "},
                new Category {Name = "Category 3", Icon = SegoeFluentIcons.Library, Tooltip = "This is category 3" },
                new Category {Name = "Category 4", Icon = SegoeFluentIcons.Mail, Tooltip = "This is category 3" }
            };
        }

        private void databindHeader_Checked_Unchecked(object sender, RoutedEventArgs e)
        {
            Categories = new ObservableCollection<CategoryBase>()
            {
                new Category { Name = "Category 1", Icon = SegoeFluentIcons.Home, Tooltip = "This is category 1" },
                new Category { Name = "Category 2", Icon = SegoeFluentIcons.KeyboardFull, Tooltip = "This is category 2" },
                new Category {Name = "Category 3", Icon = SegoeFluentIcons.Library, Tooltip = "This is category 3" },
                new Category {Name = "Category 4", Icon = SegoeFluentIcons.Mail, Tooltip = "This is category 3" }
            };
        }

        /*
        private void Grid_ManipulationDelta1(object sender, Windows.UI.Xaml.Input.ManipulationDeltaRoutedEventArgs e)
        {
            var grid = sender as Grid;
            grid.Width = grid.ActualWidth + e.Delta.Translation.X;
        }
        */

        private void headerCheck_Click(object sender, RoutedEventArgs e)
        {
            nvSample.AlwaysShowHeader = (sender as CheckBox).IsChecked == true ? true : false;

            UpdateExampleCode();
        }

        private void settingsCheck_Click(object sender, RoutedEventArgs e)
        {
            nvSample.IsSettingsVisible = (sender as CheckBox).IsChecked == true ? true : false;

            UpdateExampleCode();
        }

        private void visibleCheck_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as CheckBox).IsChecked == true)
            {
                nvSample.IsBackButtonVisible = NavigationViewBackButtonVisible.Visible;
            }
            else
            {
                nvSample.IsBackButtonVisible = NavigationViewBackButtonVisible.Collapsed;
            }

            UpdateExampleCode();
        }

        private void enableCheck_Click(object sender, RoutedEventArgs e)
        {
            nvSample.IsBackEnabled = (sender as CheckBox).IsChecked == true ? true : false;

            UpdateExampleCode();
        }

        private void autoSuggestCheck_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as CheckBox).IsChecked == true)
            {
                AutoSuggestBox asb = new AutoSuggestBox() { QueryIcon = new FontIcon(SegoeFluentIcons.Search) };
                asb.SetValue(AutomationProperties.NameProperty, "search");
                nvSample.AutoSuggestBox = asb;

                //setASBSubstitutionString();
            }
            else
            {
                nvSample.AutoSuggestBox = null;
                //navViewASB.Value = null;
            }

            UpdateExampleCode();
        }

        /*
        private void setASBSubstitutionString()
        {
            navViewASB.Value = "\r\n    <muxc:NavigationView.AutoSuggestBox> \r\n        <AutoSuggestBox QueryIcon=\"Find\" AutomationProperties.Name=\"Search\" /> \r\n    <" + "/" + "muxc:NavigationView.AutoSuggestBox> \r\n";
        }
        */

        private void panemc_Check_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as CheckBox).IsChecked == true)
            {
                PaneHyperlink.Visibility = Visibility.Visible;
            }
            else
            {
                PaneHyperlink.Visibility = Visibility.Collapsed;
            }

            UpdateExampleCode();
        }

        private void paneFooterCheck_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as CheckBox).IsChecked == true)
            {
                FooterStackPanel.Visibility = Visibility.Visible;
            }
            else
            {
                FooterStackPanel.Visibility = Visibility.Collapsed;
            }

            UpdateExampleCode();
        }

        private void panePositionLeft_Checked(object sender, RoutedEventArgs e)
        {
            if ((sender as RadioButton).IsChecked == true)
            {
                if ((sender as RadioButton).Name == "nvSampleLeft" && nvSample != null)
                {
                    nvSample.PaneDisplayMode = NavigationViewPaneDisplayMode.Left;
                    nvSample.IsPaneOpen = true;
                    FooterStackPanel.Orientation = Orientation.Vertical;
                }
                else if ((sender as RadioButton).Name == "nvSample8Left" && nvSample8 != null)
                {
                    nvSample8.PaneDisplayMode = NavigationViewPaneDisplayMode.Left;
                    //nvSample8.IsPaneOpen = true;
                }
                else if ((sender as RadioButton).Name == "nvSample9Left" && nvSample9 != null)
                {
                    nvSample9.PaneDisplayMode = NavigationViewPaneDisplayMode.Left;
                    //nvSample9.IsPaneOpen = true;
                }
            }

            UpdateExampleCode();
        }


        private void panePositionTop_Checked(object sender, RoutedEventArgs e)
        {
            if ((sender as RadioButton).IsChecked == true)
            {
                if ((sender as RadioButton).Name == "nvSampleTop" && nvSample != null)
                {
                    nvSample.PaneDisplayMode = NavigationViewPaneDisplayMode.Top;
                    nvSample.IsPaneOpen = false;
                    FooterStackPanel.Orientation = Orientation.Horizontal;
                }
                else if ((sender as RadioButton).Name == "nvSample8Top" && nvSample8 != null)
                {
                    nvSample8.PaneDisplayMode = NavigationViewPaneDisplayMode.Top;
                    nvSample8.IsPaneOpen = false;
                }
                else if ((sender as RadioButton).Name == "nvSample9Top" && nvSample9 != null)
                {
                    nvSample9.PaneDisplayMode = NavigationViewPaneDisplayMode.Top;
                    nvSample9.IsPaneOpen = false;
                }
            }

            UpdateExampleCode();
        }

        private void panePositionLeftCompact_Checked(object sender, RoutedEventArgs e)
        {
            if ((sender as RadioButton).IsChecked == true)
            {
                if ((sender as RadioButton).Name == "nvSample8LeftCompact" && nvSample8 != null)
                {
                    nvSample8.PaneDisplayMode = NavigationViewPaneDisplayMode.LeftCompact;
                    nvSample8.IsPaneOpen = false;
                }
            }

            UpdateExampleCode();
        }

        private void sffCheck_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as CheckBox).IsChecked == true)
            {
                nvSample.SelectionFollowsFocus = NavigationViewSelectionFollowsFocus.Enabled;
            }
            else
            {
                nvSample.SelectionFollowsFocus = NavigationViewSelectionFollowsFocus.Disabled;
            }

            UpdateExampleCode();
        }

        private void suppressselectionCheck_Checked_Click(object sender, RoutedEventArgs e)
        {
            SamplePage2Item.SelectsOnInvoked = (sender as CheckBox).IsChecked == true ? false : true;
            UpdateExampleCode();
        }

        private void headerText_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateExampleCode();
        }

        private void paneText_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateExampleCode();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateExampleCode();
        }

        private void UpdatePaneDisplayModeForSample2()
        {
            double threshold = nvSample2.CompactModeThresholdWidth > 0 ? nvSample2.CompactModeThresholdWidth : 641;
            if (ActualWidth >= threshold)
            {
                nvSample2.PaneDisplayMode = NavigationViewPaneDisplayMode.Top;
            }
            else
            {
                nvSample2.PaneDisplayMode = NavigationViewPaneDisplayMode.LeftMinimal;
            }
        }

        #region Example Code

        public void UpdateExampleCode()
        {
            if (!this.IsLoaded) return;

            Example1.Xaml = Example1Xaml;
            Example2.Xaml = Example2Xaml;
            Example3.CSharp = Exampl3CSharp;
            Example3.Xaml = Example3Xaml;
            Example4.Xaml = Example4Xaml;
            Example5.Xaml = Example5Xaml;
            Example6.Xaml = Example6Xaml;
            Example7.Xaml = Example7Xaml;
            Example9.Xaml = Example9Xaml;
        }

        public string Example1Xaml => $@"
<ui:NavigationView
    x:Name=""nvSample5""
    Header=""This is Header Text""
    IsTabStop=""False""
    PaneDisplayMode=""Auto"">
    <ui:NavigationView.MenuItems>
        <ui:NavigationViewItem
            Content=""Menu Item1""
            Tag=""SamplePage1"">
            <ui:NavigationViewItem.Icon>
                <ui:FontIcon Icon=""{{x:Static ui:SegoeFluentIcons.Play}}""/>
            </ui:NavigationViewItem.Icon>
        </ui:NavigationViewItem>
        <ui:NavigationViewItem
            Content=""Menu Item2""
            Tag=""SamplePage2"">
            <ui:NavigationViewItem.Icon>
                <ui:FontIcon Icon=""{{x:Static ui:SegoeFluentIcons.Save}}""/>
            </ui:NavigationViewItem.Icon>
        </ui:NavigationViewItem>
        <ui:NavigationViewItem
            Content=""Menu Item3""
            Tag=""SamplePage3"">
            <ui:NavigationViewItem.Icon>
                <ui:FontIcon Icon=""{{x:Static ui:SegoeFluentIcons.Refresh}}""/>
            </ui:NavigationViewItem.Icon>
        </ui:NavigationViewItem>
        <ui:NavigationViewItem
            Content=""Menu Item4""
            Tag=""SamplePage4"">
            <ui:NavigationViewItem.Icon>
                <ui:FontIcon Icon=""{{x:Static ui:SegoeFluentIcons.Download}}""/>
            </ui:NavigationViewItem.Icon>
        </ui:NavigationViewItem>
    </ui:NavigationView.MenuItems>
    <ui:Frame x:Name=""contentFrame5"" Margin=""0,0,0,0"" />
</ui:NavigationView>
";

        public string Example2Xaml => $@"
<ui:NavigationView x:Name=""nvSample6""
    Header=""This is Header Text""
    IsTabStop=""False"" PaneDisplayMode=""Top""
    SelectionChanged=""NavigationView_SelectionChanged6"">
    <ui:NavigationView.MenuItems>
        <ui:NavigationViewItem Content=""Menu Item1"" Tag=""SamplePage1"" />
        <ui:NavigationViewItem Content=""Menu Item2"" Tag=""SamplePage2"" />
        <ui:NavigationViewItem Content=""Menu Item3"" Tag=""SamplePage3"" />
        <ui:NavigationViewItem Content=""Menu Item4"" Tag=""SamplePage3"" />
    </ui:NavigationView.MenuItems>

    <ui:Frame x:Name=""contentFrame6"" />
</ui:NavigationView>
";

public string Exampl3CSharp => @"
// Responsive pane mode switching for nvSample2
private void UpdatePaneDisplayModeForSample2()
{
    double threshold = nvSample2.CompactModeThresholdWidth > 0 ? nvSample2.CompactModeThresholdWidth : 641;
    if (ActualWidth >= threshold)
    {
        nvSample2.PaneDisplayMode = NavigationViewPaneDisplayMode.Top;
    }
    else
    {
        nvSample2.PaneDisplayMode = NavigationViewPaneDisplayMode.LeftMinimal;
    }
}

// In your constructor or Loaded event:
SizeChanged += (s, e) => UpdatePaneDisplayModeForSample2();
";

        public string Example3Xaml => $@"
<ui:NavigationView x:Name=""nvSample2""
    IsTabStop=""False"" PaneDisplayMode=""Auto""
    SelectionChanged=""NavigationView_SelectionChanged2"">
    <ui:NavigationView.MenuItems>
        <ui:NavigationViewItem Content=""Menu Item1"" Tag=""SamplePage1"" />
        <ui:NavigationViewItem Content=""Menu Item2"" Tag=""SamplePage2"" />
        <ui:NavigationViewItem Content=""Menu Item3"" Tag=""SamplePage3"" />
        <ui:NavigationViewItem Content=""Menu Item4"" Tag=""SamplePage4"" />
    </ui:NavigationView.MenuItems>

    <ui:NavigationView.Content>
        <ui:Frame x:Name=""contentFrame2"" />
    </ui:NavigationView.Content>
</ui:NavigationView>
";

        public string Example4Xaml => $@"
<ui:NavigationView x:Name=""nvSample7""
    IsBackButtonVisible=""Collapsed""
    IsTabStop=""False""
    PaneDisplayMode=""Top""
    SelectionChanged=""NavigationView_SelectionChanged7""
    SelectionFollowsFocus=""Enabled"">
    <ui:NavigationView.MenuItems>
        <ui:NavigationViewItem Content=""Item1"" Tag=""SamplePage1"" />
        <ui:NavigationViewItem Content=""Item2"" Tag=""SamplePage2"" />
        <ui:NavigationViewItem Content=""Item3"" Tag=""SamplePage3"" />
        <ui:NavigationViewItem Content=""Item4"" Tag=""SamplePage4"" />
    </ui:NavigationView.MenuItems>
    <ui:Frame x:Name=""contentFrame7"" />
</ui:NavigationView>
";

        public string Example5Xaml => $@"
<ui:NavigationView x:Name=""nvSample4""
    IsTabStop=""False"" IsPaneOpen=""False""
    MenuItemTemplateSelector=""{{StaticResource selector}}""
    MenuItemsSource=""{{Binding Categories, Mode=OneWay}}""
    SelectionChanged=""NavigationView_SelectionChanged4"">
    <StackPanel>
        <ui:Frame x:Name=""contentFrame4"" Margin=""0,0,0,0"" />
    </StackPanel>
</ui:NavigationView>
";

        public string Example6Xaml => $@"
<ui:NavigationView x:Name=""nvSample8""
    IsTabStop=""False"" PaneDisplayMode=""{nvSample8.PaneDisplayMode}""
    SelectionChanged=""NavigationView_SelectionChanged8"">
    <ui:NavigationView.MenuItems>
        <ui:NavigationViewItem
            Content=""Home""
            Tag=""SamplePage1""
            ToolTipService.ToolTip=""Home"">
            <ui:NavigationViewItem.Icon>
                <ui:FontIcon Icon=""{{x:Static ui:SegoeFluentIcons.Home}}""/>
            </ui:NavigationViewItem.Icon>
        </ui:NavigationViewItem>
        <ui:NavigationViewItem
            Content=""Account""
            Tag=""SamplePage2""
            ToolTipService.ToolTip=""Account"">
            <ui:NavigationViewItem.Icon>
                <ui:FontIcon Icon=""{{x:Static ui:SegoeFluentIcons.Contact}}""/>
            </ui:NavigationViewItem.Icon>
            <ui:NavigationViewItem.MenuItems>
                <ui:NavigationViewItem
                    Content=""Mail""
                    Tag=""SamplePage3""
                    ToolTipService.ToolTip=""Mail"">
                    <ui:NavigationViewItem.Icon>
                        <ui:FontIcon Icon=""{{x:Static ui:SegoeFluentIcons.Mail}}""/>
                    </ui:NavigationViewItem.Icon>
                </ui:NavigationViewItem>
                <ui:NavigationViewItem
                    Content=""Calendar""
                    Tag=""SamplePage4""
                    ToolTipService.ToolTip=""Calendar"">
                    <ui:NavigationViewItem.Icon>
                        <ui:FontIcon Icon=""{{x:Static ui:SegoeFluentIcons.Calendar}}""/>
                    </ui:NavigationViewItem.Icon>
                </ui:NavigationViewItem>
            </ui:NavigationViewItem.MenuItems>
        </ui:NavigationViewItem>
        <ui:NavigationViewItem
            Content=""Document options""
            SelectsOnInvoked=""False""
            ToolTipService.ToolTip=""Document options"">
            <ui:NavigationViewItem.Icon>
                <ui:FontIcon Icon=""{{x:Static ui:SegoeFluentIcons.Page}}""/>
            </ui:NavigationViewItem.Icon>
            <ui:NavigationViewItem.MenuItems>
                <ui:NavigationViewItem
                    Content=""Create new""
                    Tag=""SamplePage5""
                    ToolTipService.ToolTip=""Create new"">
                    <ui:NavigationViewItem.Icon>
                        <ui:FontIcon Icon=""{{x:Static ui:SegoeFluentIcons.NewFolder}}""/>
                    </ui:NavigationViewItem.Icon>
                </ui:NavigationViewItem>
                <ui:NavigationViewItem
                    Content=""Upload file""
                    Tag=""SamplePage6""
                    ToolTipService.ToolTip=""Upload file"">
                    <ui:NavigationViewItem.Icon>
                        <ui:FontIcon Icon=""{{x:Static ui:SegoeFluentIcons.OpenLocal}}""/>
                    </ui:NavigationViewItem.Icon>
                </ui:NavigationViewItem>
            </ui:NavigationViewItem.MenuItems>
        </ui:NavigationViewItem>
    </ui:NavigationView.MenuItems>
    <ui:Frame x:Name=""contentFrame8"" />
</ui:NavigationView>
";

        public string Example7Xaml => $@"
<ui:NavigationView x:Name=""nvSample""
    ExpandedModeThresholdWidth=""500""
    Header=""{nvSample.Header}""
    IsTabStop=""False""
    PaneDisplayMode=""{nvSample.PaneDisplayMode}""
    PaneTitle=""{nvSample.PaneTitle}""
    IsSettingsVisible=""{nvSample.IsSettingsVisible}""
    AlwaysShowHeader=""{nvSample.AlwaysShowHeader}""
    IsBackButtonVisible=""{nvSample.IsBackButtonVisible}""
    IsBackEnabled=""{nvSample.IsBackEnabled}""
    SelectionFollowsFocus=""{nvSample.SelectionFollowsFocus}""
    SelectionChanged=""NavigationView_SelectionChanged"">

    <ui:NavigationView.MenuItems>
        <ui:NavigationViewItem
            x:Name=""SamplePage1Item""
            Content=""Menu Item1""
            Tag=""SamplePage1"">
            <ui:NavigationViewItem.Icon>
                <ui:FontIcon Icon=""{{x:Static ui:SegoeFluentIcons.Play}}"" />
            </ui:NavigationViewItem.Icon>
        </ui:NavigationViewItem>
        <ui:NavigationViewItemHeader Content=""Actions"" />
        <ui:NavigationViewItem
            x:Name=""SamplePage2Item""
            Content=""Menu Item2""
            SelectsOnInvoked=""{SamplePage2Item.SelectsOnInvoked}""
            Tag=""SamplePage2"">
            <ui:NavigationViewItem.Icon>
                <ui:FontIcon Icon=""{{x:Static ui:SegoeFluentIcons.Save}}"" />
            </ui:NavigationViewItem.Icon>
        </ui:NavigationViewItem>
        <ui:NavigationViewItem
            x:Name=""SamplePage3Item""
            Content=""Menu Item3""
            Tag=""SamplePage3"">
            <ui:NavigationViewItem.Icon>
                <ui:FontIcon Icon=""{{x:Static ui:SegoeFluentIcons.Refresh}}"" />
            </ui:NavigationViewItem.Icon>
        </ui:NavigationViewItem>
    </ui:NavigationView.MenuItems>

    <ui:NavigationView.PaneCustomContent>
        <ui:HyperlinkButton
            x:Name=""PaneHyperlink""
            Margin=""12,0""
            Content=""More info""
            Visibility=""{PaneHyperlink.Visibility}"" />
    </ui:NavigationView.PaneCustomContent>

{(nvSample.AutoSuggestBox == null ? "" : $@"
    <ui:NavigationView.AutoSuggestBox>
        <ui:AutoSuggestBox AutomationProperties.Name=""Search"">
            <ui:AutoSuggestBox.QueryIcon>
                <ui:FontIcon Icon=""{{x:Static ui:SegoeFluentIcons.Search}}""/>
            </ui:AutoSuggestBox.QueryIcon>
        </ui:AutoSuggestBox>
    </ui:NavigationView.AutoSuggestBox>

")}
    <ui:NavigationView.PaneFooter>
        <StackPanel
            x:Name=""FooterStackPanel""
            Orientation=""{FooterStackPanel.Orientation}""
            Visibility=""{FooterStackPanel.Visibility}"">
            <ui:NavigationViewItem AutomationProperties.Name=""download"">
                <ui:NavigationViewItem.Icon>
                    <ui:FontIcon Icon=""{{x:Static ui:SegoeFluentIcons.Download}}""/>
                </ui:NavigationViewItem.Icon>
            </ui:NavigationViewItem>
            <ui:NavigationViewItem AutomationProperties.Name=""favorite"">
                <ui:NavigationViewItem.Icon>
                    <ui:FontIcon Icon=""{{x:Static ui:SegoeFluentIcons.FavoriteStar}}""/>
                </ui:NavigationViewItem.Icon>
            </ui:NavigationViewItem>
        </StackPanel>
    </ui:NavigationView.PaneFooter>

    <ui:Frame x:Name=""contentFrame"" />
</ui:NavigationView>
";

public string Example9Xaml => $@"
<ui:NavigationView x:Name=""nvSample9""
    Header=""This is Header Text""
    PaneDisplayMode=""Top""
    SelectionChanged=""NavigationView_SelectionChanged9""
    IsSettingsVisible=""False"">
    <ui:NavigationView.MenuItems>
        <ui:NavigationViewItem Content=""Browse"" Tag=""SamplePage1"">
            <ui:NavigationViewItem.Icon>
                <ui:FontIcon Icon=""{{x:Static ui:SegoeFluentIcons.Library}}"" />
            </ui:NavigationViewItem.Icon>
        </ui:NavigationViewItem>
        <ui:NavigationViewItem Content=""Track an Order"" Tag=""SamplePage2"">
            <ui:NavigationViewItem.Icon>
                <ui:FontIcon Icon=""{{x:Static ui:SegoeFluentIcons.MapPin}}"" />
            </ui:NavigationViewItem.Icon>
        </ui:NavigationViewItem>
        <ui:NavigationViewItem Content=""Order History"" Tag=""SamplePage3"">
            <ui:NavigationViewItem.Icon>
                <ui:FontIcon Icon=""{{x:Static ui:SegoeFluentIcons.Tag}}"" />
            </ui:NavigationViewItem.Icon>
        </ui:NavigationViewItem>
    </ui:NavigationView.MenuItems>
    <ui:NavigationView.FooterMenuItems>
        <ui:NavigationViewItem Content=""Account"" Tag=""SamplePage4"">
            <ui:NavigationViewItem.Icon>
                <ui:FontIcon Icon=""{{x:Static ui:SegoeFluentIcons.Contact}}"" />
            </ui:NavigationViewItem.Icon>
        </ui:NavigationViewItem>
        <ui:NavigationViewItem Content=""Your Cart"" Tag=""SamplePage5"">
            <ui:NavigationViewItem.Icon>
                <ui:FontIcon Icon=""{{x:Static ui:SegoeFluentIcons.Shop}}"" />
            </ui:NavigationViewItem.Icon>
        </ui:NavigationViewItem>
        <ui:NavigationViewItem Content=""Help"" Tag=""SamplePage5"">
            <ui:NavigationViewItem.Icon>
                <ui:FontIcon Icon=""{{x:Static ui:SegoeFluentIcons.Help}}"" />
            </ui:NavigationViewItem.Icon>
        </ui:NavigationViewItem>
    </ui:NavigationView.FooterMenuItems>
    <ui:Frame x:Name=""contentFrame9"" />
</ui:NavigationView>
";

        #endregion
    }
}
