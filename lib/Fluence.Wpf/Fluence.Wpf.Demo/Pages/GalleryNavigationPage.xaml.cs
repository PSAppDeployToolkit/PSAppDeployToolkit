/*
 * Copyright 2026 Dan Cunningham
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *
 * 1. Redistributions of source code must retain the above copyright notice,
 *    this list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright notice,
 *    this list of conditions and the following disclaimer in the documentation
 *    and/or other materials provided with the distribution.
 * 3. Neither the name of the copyright holder nor the names of its contributors
 *    may be used to endorse or promote products derived from this software
 *    without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE
 * LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
 * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF
 * THE POSSIBILITY OF SUCH DAMAGE.
 */

using System.Windows;
using System.Windows.Controls;

namespace Fluence.Wpf.Demo.Pages
{
    public partial class GalleryNavigationPage : UserControl
    {
        private const string LeftNavigationViewXamlSource = "<UserControl\n" +
                                                            "    x:Class=\"Fluence.Wpf.Demo.Pages.Navigation.LeftNavigationView\"\n" +
                                                            "    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"\n" +
                                                            "    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"\n" +
                                                            "    xmlns:ui=\"clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf\">\n" +
                                                            "    <Border\n" +
                                                            "        Height=\"320\">\n" +
                                                            "        <ui:NavigationView\n" +
                                                            "            PaneDisplayMode=\"Left\">\n" +
                                                            "            <ui:NavigationView.PaneHeader>\n" +
                                                            "                <TextBlock\n" +
                                                            "                    Margin=\"12,8\"\n" +
                                                            "                    Foreground=\"{DynamicResource TextFillColorSecondaryBrush}\"\n" +
                                                            "                    Text=\"Navigation\" />\n" +
                                                            "            </ui:NavigationView.PaneHeader>\n" +
                                                            "            <ui:NavigationViewItem\n" +
                                                            "                Content=\"Home\"\n" +
                                                            "                IsSelected=\"True\">\n" +
                                                            "                <ui:NavigationViewItem.Icon>\n" +
                                                            "                    <ui:FontIcon Glyph=\"&#xE80F;\" IconFontSize=\"16\" />\n" +
                                                            "                </ui:NavigationViewItem.Icon>\n" +
                                                            "            </ui:NavigationViewItem>\n" +
                                                            "            <ui:NavigationViewItem Content=\"Files\">\n" +
                                                            "                <ui:NavigationViewItem.Icon>\n" +
                                                            "                    <ui:FontIcon Glyph=\"&#xE8B7;\" IconFontSize=\"16\" />\n" +
                                                            "                </ui:NavigationViewItem.Icon>\n" +
                                                            "            </ui:NavigationViewItem>\n" +
                                                            "            <ui:NavigationViewItem Content=\"Reports\">\n" +
                                                            "                <ui:NavigationViewItem.Icon>\n" +
                                                            "                    <ui:FontIcon Glyph=\"&#xE9D9;\" IconFontSize=\"16\" />\n" +
                                                            "                </ui:NavigationViewItem.Icon>\n" +
                                                            "            </ui:NavigationViewItem>\n" +
                                                            "        </ui:NavigationView>\n" +
                                                            "    </Border>\n" +
                                                            "</UserControl>\n";

        private const string LeftNavigationViewCSharpSource = "using System.Windows.Controls;\n" +
                                                              "\n" +
                                                              "namespace Fluence.Wpf.Demo.Pages.Navigation\n" +
                                                              "{\n" +
                                                              "    public partial class LeftNavigationView : UserControl\n" +
                                                              "    {\n" +
                                                              "        public LeftNavigationView()\n" +
                                                              "        {\n" +
                                                              "            InitializeComponent();\n" +
                                                              "        }\n" +
                                                              "    }\n" +
                                                              "}\n";
        private const string TopNavigationViewXamlSource = "<UserControl\n" +
                                                           "    x:Class=\"Fluence.Wpf.Demo.Pages.Navigation.TopNavigationView\"\n" +
                                                           "    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"\n" +
                                                           "    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"\n" +
                                                           "    xmlns:ui=\"clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf\">\n" +
                                                           "    <Border\n" +
                                                           "        Height=\"240\">\n" +
                                                           "        <ui:NavigationView\n" +
                                                           "            Header=\"Insights\"\n" +
                                                           "            PaneDisplayMode=\"Top\">\n" +
                                                           "            <ui:NavigationViewItem\n" +
                                                           "                Content=\"Overview\"\n" +
                                                           "                IsSelected=\"True\">\n" +
                                                           "                <ui:NavigationViewItem.Icon>\n" +
                                                           "                    <ui:FontIcon Glyph=\"&#xE9D2;\" IconFontSize=\"16\" />\n" +
                                                           "                </ui:NavigationViewItem.Icon>\n" +
                                                           "            </ui:NavigationViewItem>\n" +
                                                           "            <ui:NavigationViewItem Content=\"Activity\">\n" +
                                                           "                <ui:NavigationViewItem.Icon>\n" +
                                                           "                    <ui:FontIcon Glyph=\"&#xE7F4;\" IconFontSize=\"16\" />\n" +
                                                           "                </ui:NavigationViewItem.Icon>\n" +
                                                           "            </ui:NavigationViewItem>\n" +
                                                           "            <ui:NavigationViewItem Content=\"Settings\">\n" +
                                                           "                <ui:NavigationViewItem.Icon>\n" +
                                                           "                    <ui:FontIcon Glyph=\"&#xE713;\" IconFontSize=\"16\" />\n" +
                                                           "                </ui:NavigationViewItem.Icon>\n" +
                                                           "            </ui:NavigationViewItem>\n" +
                                                           "        </ui:NavigationView>\n" +
                                                           "    </Border>\n" +
                                                           "</UserControl>\n";

        private const string TopNavigationViewCSharpSource = "using System.Windows.Controls;\n" +
                                                             "\n" +
                                                             "namespace Fluence.Wpf.Demo.Pages.Navigation\n" +
                                                             "{\n" +
                                                             "    public partial class TopNavigationView : UserControl\n" +
                                                             "    {\n" +
                                                             "        public TopNavigationView()\n" +
                                                             "        {\n" +
                                                             "            InitializeComponent();\n" +
                                                             "        }\n" +
                                                             "    }\n" +
                                                             "}\n";
        private const string CompactNavigationViewXamlSource = "<UserControl\n" +
                                                               "    x:Class=\"Fluence.Wpf.Demo.Pages.Navigation.CompactNavigationView\"\n" +
                                                               "    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"\n" +
                                                               "    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"\n" +
                                                               "    xmlns:ui=\"clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf\">\n" +
                                                               "    <StackPanel>\n" +
                                                               "        <Border\n" +
                                                               "            Height=\"300\"\n" +
                                                               "            Margin=\"0,0,0,12\"\n" +
                                                               "            BorderBrush=\"{DynamicResource CardStrokeColorDefaultBrush}\"\n" +
                                                               "            BorderThickness=\"1\">\n" +
                                                               "            <ui:NavigationView\n" +
                                                               "                x:Name=\"CompactNavigationDemo\"\n" +
                                                               "                IsBackButtonVisible=\"True\"\n" +
                                                               "                IsBackEnabled=\"{Binding IsChecked, ElementName=BackEnabledToggle}\"\n" +
                                                               "                IsPaneToggleButtonVisible=\"True\"\n" +
                                                               "                IsPaneOpen=\"False\"\n" +
                                                               "                PaneDisplayMode=\"LeftCompact\">\n" +
                                                               "                <ui:NavigationView.PaneFooter>\n" +
                                                               "                    <ui:NavigationViewItem Content=\"Settings\">\n" +
                                                               "                        <ui:NavigationViewItem.Icon>\n" +
                                                               "                            <ui:FontIcon Glyph=\"&#xE713;\" IconFontSize=\"16\" />\n" +
                                                               "                        </ui:NavigationViewItem.Icon>\n" +
                                                               "                    </ui:NavigationViewItem>\n" +
                                                               "                </ui:NavigationView.PaneFooter>\n" +
                                                               "                <ui:NavigationViewItem\n" +
                                                               "                    Content=\"Dashboard\"\n" +
                                                               "                    IsSelected=\"True\">\n" +
                                                               "                    <ui:NavigationViewItem.Icon>\n" +
                                                               "                        <ui:FontIcon Glyph=\"&#xE80F;\" IconFontSize=\"16\" />\n" +
                                                               "                    </ui:NavigationViewItem.Icon>\n" +
                                                               "                </ui:NavigationViewItem>\n" +
                                                               "                <ui:NavigationViewItem Content=\"Messages\">\n" +
                                                               "                    <ui:NavigationViewItem.Icon>\n" +
                                                               "                        <ui:FontIcon Glyph=\"&#xE8BD;\" IconFontSize=\"16\" />\n" +
                                                               "                    </ui:NavigationViewItem.Icon>\n" +
                                                               "                </ui:NavigationViewItem>\n" +
                                                               "            </ui:NavigationView>\n" +
                                                               "        </Border>\n" +
                                                               "            <ui:CheckBox\n" +
                                                               "                x:Name=\"BackEnabledToggle\"\n" +
                                                               "                Content=\"Back enabled\"\n" +
                                                               "                IsChecked=\"True\" />\n" +
                                                               "    </StackPanel>\n" +
                                                               "</UserControl>\n";

        private const string CompactNavigationViewCSharpSource = "using System.Windows.Controls;\n" +
                                                                 "\n" +
                                                                 "namespace Fluence.Wpf.Demo.Pages.Navigation\n" +
                                                                 "{\n" +
                                                                 "    public partial class CompactNavigationView : UserControl\n" +
                                                                 "    {\n" +
                                                                 "        public CompactNavigationView()\n" +
                                                                 "        {\n" +
                                                                 "            InitializeComponent();\n" +
                                                                 "        }\n" +
                                                                 "    }\n" +
                                                                 "}\n";
        private const string InfoBadgeNavigationXamlSource = "<UserControl\n" +
                                                             "    x:Class=\"Fluence.Wpf.Demo.Pages.Navigation.InfoBadgeNavigation\"\n" +
                                                             "    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"\n" +
                                                             "    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"\n" +
                                                             "    xmlns:ui=\"clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf\"\n" +
                                                             "    xmlns:uicore=\"clr-namespace:Fluence.Wpf;assembly=Fluence.Wpf\">\n" +
                                                             "    <Border Height=\"260\">\n" +
                                                             "        <ui:NavigationView\n" +
                                                             "            Header=\"Inbox\"\n" +
                                                             "            IsPaneOpen=\"True\"\n" +
                                                             "            PaneDisplayMode=\"Left\">\n" +
                                                             "            <ui:NavigationViewItem\n" +
                                                             "                Content=\"Inbox\"\n" +
                                                             "                IsSelected=\"True\">\n" +
                                                             "                <ui:NavigationViewItem.Icon>\n" +
                                                             "                    <ui:FontIcon Glyph=\"&#xE715;\" IconFontSize=\"16\" />\n" +
                                                             "                </ui:NavigationViewItem.Icon>\n" +
                                                             "                <ui:NavigationViewItem.InfoBadge>\n" +
                                                             "                    <ui:InfoBadge Value=\"12\" />\n" +
                                                             "                </ui:NavigationViewItem.InfoBadge>\n" +
                                                             "            </ui:NavigationViewItem>\n" +
                                                             "            <ui:NavigationViewItem Content=\"Approvals\">\n" +
                                                             "                <ui:NavigationViewItem.Icon>\n" +
                                                             "                    <ui:FontIcon Glyph=\"&#xE73E;\" IconFontSize=\"16\" />\n" +
                                                             "                </ui:NavigationViewItem.Icon>\n" +
                                                             "                <ui:NavigationViewItem.InfoBadge>\n" +
                                                             "                    <ui:InfoBadge BadgeStyle=\"{x:Static uicore:InfoBadgeStyle.Caution}\" />\n" +
                                                             "                </ui:NavigationViewItem.InfoBadge>\n" +
                                                             "            </ui:NavigationViewItem>\n" +
                                                             "            <ui:NavigationViewItem Content=\"Alerts\">\n" +
                                                             "                <ui:NavigationViewItem.Icon>\n" +
                                                             "                    <ui:FontIcon Glyph=\"&#xE7BA;\" IconFontSize=\"16\" />\n" +
                                                             "                </ui:NavigationViewItem.Icon>\n" +
                                                             "                <ui:NavigationViewItem.InfoBadge>\n" +
                                                             "                    <ui:InfoBadge BadgeStyle=\"{x:Static uicore:InfoBadgeStyle.Critical}\" Value=\"2\" />\n" +
                                                             "                </ui:NavigationViewItem.InfoBadge>\n" +
                                                             "            </ui:NavigationViewItem>\n" +
                                                             "        </ui:NavigationView>\n" +
                                                             "    </Border>\n" +
                                                             "</UserControl>\n";

        private const string InfoBadgeNavigationCSharpSource = "using System.Windows.Controls;\n" +
                                                               "\n" +
                                                               "namespace Fluence.Wpf.Demo.Pages.Navigation\n" +
                                                               "{\n" +
                                                               "    public partial class InfoBadgeNavigation : UserControl\n" +
                                                               "    {\n" +
                                                               "        public InfoBadgeNavigation()\n" +
                                                               "        {\n" +
                                                               "            InitializeComponent();\n" +
                                                               "        }\n" +
                                                               "    }\n" +
                                                               "}\n";

        private const string BreadcrumbBarXamlSource = "<UserControl\n" +
                                                       "    x:Class=\"Fluence.Wpf.Demo.Pages.Navigation.BreadcrumbTrail\"\n" +
                                                       "    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"\n" +
                                                       "    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"\n" +
                                                       "    xmlns:ui=\"clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf\">\n" +
                                                       "    <ui:BreadcrumbBar x:Name=\"Trail\" ItemClicked=\"Trail_ItemClicked\" />\n" +
                                                       "</UserControl>\n";

        private const string BreadcrumbBarCSharpSource = "using System.Collections.ObjectModel;\n" +
                                                         "using System.Windows.Controls;\n" +
                                                         "using Fluence.Wpf;\n" +
                                                         "\n" +
                                                         "namespace Fluence.Wpf.Demo.Pages.Navigation\n" +
                                                         "{\n" +
                                                         "    public partial class BreadcrumbTrail : UserControl\n" +
                                                         "    {\n" +
                                                         "        private readonly ObservableCollection<string> _path =\n" +
                                                         "            [\"Home\", \"Documents\", \"Design\", \"Specs\"];\n" +
                                                         "\n" +
                                                         "        public BreadcrumbTrail()\n" +
                                                         "        {\n" +
                                                         "            InitializeComponent();\n" +
                                                         "            Trail.ItemsSource = _path;\n" +
                                                         "        }\n" +
                                                         "\n" +
                                                         "        private void Trail_ItemClicked(object sender, BreadcrumbBarItemClickedEventArgs e)\n" +
                                                         "        {\n" +
                                                         "            // Trim the path back to the clicked crumb.\n" +
                                                         "            for (int i = _path.Count - 1; i > e.Index; i--)\n" +
                                                         "            {\n" +
                                                         "                _path.RemoveAt(i);\n" +
                                                         "            }\n" +
                                                         "        }\n" +
                                                         "    }\n" +
                                                         "}\n";

        private readonly System.Collections.ObjectModel.ObservableCollection<string> _breadcrumbPath =
            ["Home", "Documents", "Design", "Specs"];

        public GalleryNavigationPage()
        {
            InitializeComponent();

            DemoSamplePageWiring.Apply(
                (DependencyObject)Content,
                new DemoSampleSource(1, LeftNavigationViewXamlSource, LeftNavigationViewCSharpSource),
                new DemoSampleSource(2, TopNavigationViewXamlSource, TopNavigationViewCSharpSource),
                new DemoSampleSource(3, CompactNavigationViewXamlSource, CompactNavigationViewCSharpSource),
                new DemoSampleSource(4, InfoBadgeNavigationXamlSource, InfoBadgeNavigationCSharpSource),
                new DemoSampleSource(5, BreadcrumbBarXamlSource, BreadcrumbBarCSharpSource),
                new DemoSampleSource(6, PipsPagerXamlSource, PipsPagerCSharpSource));

            DemoBreadcrumbBar.ItemsSource = _breadcrumbPath;

            Loaded += GalleryNavigationPage_Loaded;
        }

        private const string PipsPagerXamlSource = "<UserControl\n" +
                                                   "    x:Class=\"Fluence.Wpf.Demo.Pages.Navigation.CarouselPager\"\n" +
                                                   "    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"\n" +
                                                   "    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"\n" +
                                                   "    xmlns:ui=\"clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf\">\n" +
                                                   "    <ui:PipsPager\n" +
                                                   "        x:Name=\"Pager\"\n" +
                                                   "        NextButtonVisibility=\"Visible\"\n" +
                                                   "        NumberOfPages=\"8\"\n" +
                                                   "        PreviousButtonVisibility=\"Visible\"\n" +
                                                   "        SelectedIndexChanged=\"Pager_SelectedIndexChanged\" />\n" +
                                                   "</UserControl>\n";

        private const string PipsPagerCSharpSource = "using System.Windows.Controls;\n" +
                                                     "using Fluence.Wpf;\n" +
                                                     "\n" +
                                                     "namespace Fluence.Wpf.Demo.Pages.Navigation\n" +
                                                     "{\n" +
                                                     "    public partial class CarouselPager : UserControl\n" +
                                                     "    {\n" +
                                                     "        public CarouselPager()\n" +
                                                     "        {\n" +
                                                     "            InitializeComponent();\n" +
                                                     "        }\n" +
                                                     "\n" +
                                                     "        private void Pager_SelectedIndexChanged(object sender, PipsPagerSelectedIndexChangedEventArgs e)\n" +
                                                     "        {\n" +
                                                     "            // e.NewIndex is the zero-based page to show.\n" +
                                                     "        }\n" +
                                                     "    }\n" +
                                                     "}\n";

        private void DemoPipsPager_SelectedIndexChanged(object sender, Fluence.Wpf.PipsPagerSelectedIndexChangedEventArgs e)
        {
            PipsPagerResultLabel.Text = string.Format(
                System.Globalization.CultureInfo.CurrentCulture,
                "Page {0} of {1}",
                e.NewIndex + 1,
                DemoPipsPager.NumberOfPages);
        }

        private void DemoBreadcrumbBar_ItemClicked(object sender, Fluence.Wpf.BreadcrumbBarItemClickedEventArgs e)
        {
            for (int i = _breadcrumbPath.Count - 1; i > e.Index; i--)
            {
                _breadcrumbPath.RemoveAt(i);
            }

            BreadcrumbResultLabel.Text = string.Format(
                System.Globalization.CultureInfo.CurrentCulture,
                "Navigated to: {0}",
                e.Item);
        }

        private void GalleryNavigationPage_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= GalleryNavigationPage_Loaded;

            LeftNavigationDemo.SelectedItem = LeftNavigationHomeItem;
            TopNavigationDemo.SelectedItem = TopNavigationOverviewItem;
            CompactNavigationDemo.SelectedItem = CompactNavigationDashboardItem;
            SetNavigationDemoContent(LeftNavigationDemo, LeftNavigationHomeItem);
            SetNavigationDemoContent(TopNavigationDemo, TopNavigationOverviewItem);
            SetNavigationDemoContent(CompactNavigationDemo, CompactNavigationDashboardItem);
        }

        private void NavigationDemo_ItemInvoked(object sender, NavigationViewItemInvokedEventArgs e)
        {
            SetNavigationDemoContent(sender as Controls.NavigationView, e.InvokedItemContainer);
        }

        private static void SetNavigationDemoContent(Controls.NavigationView? nav, Controls.NavigationViewItem item)
        {
            if (nav is null || item is null)
            {
                return;
            }

            string? title = item.Content as string;
            nav.Content = CreateNavigationDemoContent(title ?? string.Empty);
        }

        private static FrameworkElement CreateNavigationDemoContent(string title)
        {
            return title switch
            {
                "Home" => CreateDescribedContent(
                                        "Home",
                                        "A persistent left pane keeps destinations available while content changes."),
                "Dashboard" => CreateDescribedContent(
                                        "Dashboard",
                                        "Toggle back availability below to update the back button state."),
                "Overview" => CreateSimpleContent("Overview dashboard"),
                "Activity" => CreateSimpleContent("Recent activity"),
                _ => CreateSimpleContent(title),
            };
        }

        private static StackPanel CreateDescribedContent(string title, string description)
        {
            StackPanel panel = new() { Margin = new Thickness(20) };
            TextBlock titleBlock = CreateSimpleContent(title);
            titleBlock.Margin = new Thickness(0);
            titleBlock.FontSize = 18;
            titleBlock.FontWeight = FontWeights.SemiBold;
            _ = panel.Children.Add(titleBlock);

            TextBlock descriptionBlock = new()
            {
                Margin = new Thickness(0, 6, 0, 0),
                Text = description,
                TextWrapping = TextWrapping.Wrap,
            };
            descriptionBlock.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");
            _ = panel.Children.Add(descriptionBlock);
            return panel;
        }

        private static TextBlock CreateSimpleContent(string text)
        {
            TextBlock textBlock = new()
            {
                Margin = new Thickness(20),
                Text = text,
            };
            textBlock.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorPrimaryBrush");
            return textBlock;
        }

    }
}
