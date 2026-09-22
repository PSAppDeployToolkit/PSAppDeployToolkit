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
    public partial class GalleryNavigationPage : Page
    {
        private static readonly string LeftNavigationViewXamlSource = DemoSampleXaml.UserControl(
            "Fluence.Wpf.Demo.Pages.Navigation.LeftNavigationView",
                                                            "    <Border\n" +
                                                            "        Height=\"320\">\n" +
                                                            "        <fluence:NavigationView\n" +
                                                            "            PaneDisplayMode=\"Left\">\n" +
                                                            "            <fluence:NavigationView.PaneHeader>\n" +
                                                            "                <TextBlock\n" +
                                                            "                    Margin=\"12,8\"\n" +
                                                            "                    Foreground=\"{DynamicResource TextFillColorSecondaryBrush}\"\n" +
                                                            "                    Text=\"Navigation\" />\n" +
                                                            "            </fluence:NavigationView.PaneHeader>\n" +
                                                            "            <fluence:NavigationViewItem\n" +
                                                            "                Content=\"Home\"\n" +
                                                            "                IsSelected=\"True\">\n" +
                                                            "                <fluence:NavigationViewItem.Icon>\n" +
                                                            "                    <fluence:FontIcon Glyph=\"&#xE80F;\" IconFontSize=\"16\" />\n" +
                                                            "                </fluence:NavigationViewItem.Icon>\n" +
                                                            "            </fluence:NavigationViewItem>\n" +
                                                            "            <fluence:NavigationViewItem Content=\"Files\">\n" +
                                                            "                <fluence:NavigationViewItem.Icon>\n" +
                                                            "                    <fluence:FontIcon Glyph=\"&#xE8B7;\" IconFontSize=\"16\" />\n" +
                                                            "                </fluence:NavigationViewItem.Icon>\n" +
                                                            "            </fluence:NavigationViewItem>\n" +
                                                            "            <fluence:NavigationViewItem Content=\"Reports\">\n" +
                                                            "                <fluence:NavigationViewItem.Icon>\n" +
                                                            "                    <fluence:FontIcon Glyph=\"&#xE9D9;\" IconFontSize=\"16\" />\n" +
                                                            "                </fluence:NavigationViewItem.Icon>\n" +
                                                            "            </fluence:NavigationViewItem>\n" +
                                                            "        </fluence:NavigationView>\n" +
                                                            "    </Border>\n");

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
        private static readonly string TopNavigationViewXamlSource = DemoSampleXaml.UserControl(
            "Fluence.Wpf.Demo.Pages.Navigation.TopNavigationView",
                                                           "    <Border\n" +
                                                           "        Height=\"240\">\n" +
                                                           "        <fluence:NavigationView\n" +
                                                           "            Header=\"Insights\"\n" +
                                                           "            PaneDisplayMode=\"Top\">\n" +
                                                           "            <fluence:NavigationViewItem\n" +
                                                           "                Content=\"Overview\"\n" +
                                                           "                IsSelected=\"True\">\n" +
                                                           "                <fluence:NavigationViewItem.Icon>\n" +
                                                           "                    <fluence:FontIcon Glyph=\"&#xE9D2;\" IconFontSize=\"16\" />\n" +
                                                           "                </fluence:NavigationViewItem.Icon>\n" +
                                                           "            </fluence:NavigationViewItem>\n" +
                                                           "            <fluence:NavigationViewItem Content=\"Activity\">\n" +
                                                           "                <fluence:NavigationViewItem.Icon>\n" +
                                                           "                    <fluence:FontIcon Glyph=\"&#xE7F4;\" IconFontSize=\"16\" />\n" +
                                                           "                </fluence:NavigationViewItem.Icon>\n" +
                                                           "            </fluence:NavigationViewItem>\n" +
                                                           "            <fluence:NavigationViewItem Content=\"Settings\">\n" +
                                                           "                <fluence:NavigationViewItem.Icon>\n" +
                                                           "                    <fluence:FontIcon Glyph=\"&#xE713;\" IconFontSize=\"16\" />\n" +
                                                           "                </fluence:NavigationViewItem.Icon>\n" +
                                                           "            </fluence:NavigationViewItem>\n" +
                                                           "        </fluence:NavigationView>\n" +
                                                           "    </Border>\n");

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
        private static readonly string CompactNavigationViewXamlSource = DemoSampleXaml.UserControl(
            "Fluence.Wpf.Demo.Pages.Navigation.CompactNavigationView",
                                                               "    <StackPanel>\n" +
                                                               "        <Border\n" +
                                                               "            Height=\"300\"\n" +
                                                               "            Margin=\"0,0,0,12\"\n" +
                                                               "            BorderBrush=\"{DynamicResource CardStrokeColorDefaultBrush}\"\n" +
                                                               "            BorderThickness=\"1\">\n" +
                                                               "            <fluence:NavigationView\n" +
                                                               "                x:Name=\"CompactNavigationDemo\"\n" +
                                                               "                IsBackButtonVisible=\"True\"\n" +
                                                               "                IsBackEnabled=\"{Binding IsChecked, ElementName=BackEnabledToggle}\"\n" +
                                                               "                IsPaneToggleButtonVisible=\"True\"\n" +
                                                               "                IsPaneOpen=\"False\"\n" +
                                                               "                PaneDisplayMode=\"LeftCompact\">\n" +
                                                               "                <fluence:NavigationView.PaneFooter>\n" +
                                                               "                    <fluence:NavigationViewItem Content=\"Settings\">\n" +
                                                               "                        <fluence:NavigationViewItem.Icon>\n" +
                                                               "                            <fluence:FontIcon Glyph=\"&#xE713;\" IconFontSize=\"16\" />\n" +
                                                               "                        </fluence:NavigationViewItem.Icon>\n" +
                                                               "                    </fluence:NavigationViewItem>\n" +
                                                               "                </fluence:NavigationView.PaneFooter>\n" +
                                                               "                <fluence:NavigationViewItem\n" +
                                                               "                    Content=\"Dashboard\"\n" +
                                                               "                    IsSelected=\"True\">\n" +
                                                               "                    <fluence:NavigationViewItem.Icon>\n" +
                                                               "                        <fluence:FontIcon Glyph=\"&#xE80F;\" IconFontSize=\"16\" />\n" +
                                                               "                    </fluence:NavigationViewItem.Icon>\n" +
                                                               "                </fluence:NavigationViewItem>\n" +
                                                               "                <fluence:NavigationViewItem Content=\"Messages\">\n" +
                                                               "                    <fluence:NavigationViewItem.Icon>\n" +
                                                               "                        <fluence:FontIcon Glyph=\"&#xE8BD;\" IconFontSize=\"16\" />\n" +
                                                               "                    </fluence:NavigationViewItem.Icon>\n" +
                                                               "                </fluence:NavigationViewItem>\n" +
                                                               "            </fluence:NavigationView>\n" +
                                                               "        </Border>\n" +
                                                               "            <fluence:CheckBox\n" +
                                                               "                x:Name=\"BackEnabledToggle\"\n" +
                                                               "                Content=\"Back enabled\"\n" +
                                                               "                IsChecked=\"False\" />\n" +
                                                               "    </StackPanel>\n");

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
        private static readonly string InfoBadgeNavigationXamlSource = DemoSampleXaml.UserControl(
            "Fluence.Wpf.Demo.Pages.Navigation.InfoBadgeNavigation",
                                                             "    <Border Height=\"260\">\n" +
                                                             "        <fluence:NavigationView\n" +
                                                             "            Header=\"Inbox\"\n" +
                                                             "            IsPaneOpen=\"True\"\n" +
                                                             "            PaneDisplayMode=\"Left\">\n" +
                                                             "            <fluence:NavigationViewItem\n" +
                                                             "                Content=\"Inbox\"\n" +
                                                             "                IsSelected=\"True\">\n" +
                                                             "                <fluence:NavigationViewItem.Icon>\n" +
                                                             "                    <fluence:FontIcon Glyph=\"&#xE715;\" IconFontSize=\"16\" />\n" +
                                                             "                </fluence:NavigationViewItem.Icon>\n" +
                                                             "                <fluence:NavigationViewItem.InfoBadge>\n" +
                                                             "                    <fluence:InfoBadge Value=\"12\" />\n" +
                                                             "                </fluence:NavigationViewItem.InfoBadge>\n" +
                                                             "            </fluence:NavigationViewItem>\n" +
                                                             "            <fluence:NavigationViewItem Content=\"Approvals\">\n" +
                                                             "                <fluence:NavigationViewItem.Icon>\n" +
                                                             "                    <fluence:FontIcon Glyph=\"&#xE73E;\" IconFontSize=\"16\" />\n" +
                                                             "                </fluence:NavigationViewItem.Icon>\n" +
                                                             "                <fluence:NavigationViewItem.InfoBadge>\n" +
                                                             "                    <fluence:InfoBadge BadgeStyle=\"{x:Static fluence:InfoBadgeStyle.Caution}\" />\n" +
                                                             "                </fluence:NavigationViewItem.InfoBadge>\n" +
                                                             "            </fluence:NavigationViewItem>\n" +
                                                             "            <fluence:NavigationViewItem Content=\"Alerts\">\n" +
                                                             "                <fluence:NavigationViewItem.Icon>\n" +
                                                             "                    <fluence:FontIcon Glyph=\"&#xE7BA;\" IconFontSize=\"16\" />\n" +
                                                             "                </fluence:NavigationViewItem.Icon>\n" +
                                                             "                <fluence:NavigationViewItem.InfoBadge>\n" +
                                                             "                    <fluence:InfoBadge BadgeStyle=\"{x:Static fluence:InfoBadgeStyle.Critical}\" Value=\"2\" />\n" +
                                                             "                </fluence:NavigationViewItem.InfoBadge>\n" +
                                                             "            </fluence:NavigationViewItem>\n" +
                                                             "        </fluence:NavigationView>\n" +
                                                             "    </Border>\n");

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

        private static readonly string BreadcrumbBarXamlSource = DemoSampleXaml.UserControl(
            "Fluence.Wpf.Demo.Pages.Navigation.BreadcrumbTrail",
                                                       "    <fluence:BreadcrumbBar x:Name=\"Trail\" ItemClicked=\"Trail_ItemClicked\" />\n");

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

        /// <summary>
        /// The SelectorBar section the sample presenter is showing, so the next selection knows
        /// which way to slide.
        /// </summary>
        private int _selectedSectionIndex;

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
                new DemoSampleSource(6, PipsPagerXamlSource, PipsPagerCSharpSource),
                new DemoSampleSource(7, SelectorBarXamlSource, SelectorBarCSharpSource));

            DemoBreadcrumbBar.ItemsSource = _breadcrumbPath;

            Loaded += GalleryNavigationPage_Loaded;
        }

        private static readonly string SelectorBarXamlSource = DemoSampleXaml.UserControl(
            "Fluence.Wpf.Demo.Pages.Navigation.SectionSwitcher",
                                                     "    <fluence:StackPanel HorizontalAlignment=\"Left\" Spacing=\"4\">\n" +
                                                     "        <fluence:SelectorBar\n" +
                                                     "            x:Name=\"Sections\"\n" +
                                                     "            SelectionChanged=\"Sections_SelectionChanged\">\n" +
                                                     "            <fluence:SelectorBarItem Text=\"Recent\">\n" +
                                                     "                <fluence:SelectorBarItem.Icon>\n" +
                                                     "                    <fluence:FontIcon Glyph=\"&#xE823;\" IconFontSize=\"16\" />\n" +
                                                     "                </fluence:SelectorBarItem.Icon>\n" +
                                                     "            </fluence:SelectorBarItem>\n" +
                                                     "            <fluence:SelectorBarItem Text=\"Shared\">\n" +
                                                     "                <fluence:SelectorBarItem.Icon>\n" +
                                                     "                    <fluence:FontIcon Glyph=\"&#xE72D;\" IconFontSize=\"16\" />\n" +
                                                     "                </fluence:SelectorBarItem.Icon>\n" +
                                                     "            </fluence:SelectorBarItem>\n" +
                                                     "            <fluence:SelectorBarItem Text=\"Favorites\">\n" +
                                                     "                <fluence:SelectorBarItem.Icon>\n" +
                                                     "                    <fluence:FontIcon Glyph=\"&#xE734;\" IconFontSize=\"16\" />\n" +
                                                     "                </fluence:SelectorBarItem.Icon>\n" +
                                                     "            </fluence:SelectorBarItem>\n" +
                                                     "        </fluence:SelectorBar>\n" +
                                                     "        <fluence:SlideNavigationPresenter\n" +
                                                     "            x:Name=\"SectionHost\"\n" +
                                                     "            Width=\"360\"\n" +
                                                     "            Height=\"160\" />\n" +
                                                     "    </fluence:StackPanel>\n");

        private const string SelectorBarCSharpSource = "using System.Windows;\n" +
                                                       "using System.Windows.Controls;\n" +
                                                       "using Fluence.Wpf;\n" +
                                                       "\n" +
                                                       "namespace Fluence.Wpf.Demo.Pages.Navigation\n" +
                                                       "{\n" +
                                                       "    public partial class SectionSwitcher : UserControl\n" +
                                                       "    {\n" +
                                                       "        private int _selectedIndex;\n" +
                                                       "\n" +
                                                       "        public SectionSwitcher()\n" +
                                                       "        {\n" +
                                                       "            InitializeComponent();\n" +
                                                       "            Sections.SelectedIndex = 0;\n" +
                                                       "        }\n" +
                                                       "\n" +
                                                       "        private void Sections_SelectionChanged(object sender, SelectionChangedEventArgs e)\n" +
                                                       "        {\n" +
                                                       "            int index = Sections.SelectedIndex;\n" +
                                                       "\n" +
                                                       "            // Later section slides in from the right, earlier from the left.\n" +
                                                       "            SectionHost.TransitionEffect = index > _selectedIndex\n" +
                                                       "                ? SlideNavigationTransitionEffect.FromRight\n" +
                                                       "                : SlideNavigationTransitionEffect.FromLeft;\n" +
                                                       "            SectionHost.Content = CreateSection(index);\n" +
                                                       "            _selectedIndex = index;\n" +
                                                       "        }\n" +
                                                       "\n" +
                                                       "        // Each peer view carries the section content, not just its name.\n" +
                                                       "        private static UIElement CreateSection(int index)\n" +
                                                       "        {\n" +
                                                       "            string[] titles = index switch\n" +
                                                       "            {\n" +
                                                       "                0 => [\"Quarterly review.docx\", \"Launch metrics.xlsx\", \"Design notes.md\"],\n" +
                                                       "                1 => [\"Roadmap 2027\", \"Release checklist\", \"Budget forecast\"],\n" +
                                                       "                _ => [\"Team handbook\", \"Support playbook\", \"Brand assets\"],\n" +
                                                       "            };\n" +
                                                       "\n" +
                                                       "            StackPanel panel = new();\n" +
                                                       "            foreach (string title in titles)\n" +
                                                       "            {\n" +
                                                       "                panel.Children.Add(new TextBlock\n" +
                                                       "                {\n" +
                                                       "                    Margin = new Thickness(0, 0, 0, 10),\n" +
                                                       "                    Text = title,\n" +
                                                       "                });\n" +
                                                       "            }\n" +
                                                       "\n" +
                                                       "            return panel;\n" +
                                                       "        }\n" +
                                                       "    }\n" +
                                                       "}\n";

        private static readonly string PipsPagerXamlSource = DemoSampleXaml.UserControl(
            "Fluence.Wpf.Demo.Pages.Navigation.CarouselPager",
                                                   "    <fluence:PipsPager\n" +
                                                   "        x:Name=\"Pager\"\n" +
                                                   "        NextButtonVisibility=\"Visible\"\n" +
                                                   "        NumberOfPages=\"8\"\n" +
                                                   "        PreviousButtonVisibility=\"Visible\"\n" +
                                                   "        SelectedIndexChanged=\"Pager_SelectedIndexChanged\" />\n");

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

        private void DemoPipsPager_SelectedIndexChanged(object sender, PipsPagerSelectedIndexChangedEventArgs e)
        {
            PipsPagerResultLabel.Text = string.Format(
                System.Globalization.CultureInfo.CurrentCulture,
                "Page {0} of {1}",
                e.NewIndex + 1,
                DemoPipsPager.NumberOfPages);
        }

        private void DemoBreadcrumbBar_ItemClicked(object sender, BreadcrumbBarItemClickedEventArgs e)
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

        private void DemoSelectorBar_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int index = DemoSelectorBar.SelectedIndex;
            if (index < 0)
            {
                return;
            }

            // The WinUI Gallery's own Color page picks the effect the same way: a later peer
            // slides in from the right, an earlier one from the left.
            DemoSelectorBarPresenter.TransitionEffect = index > _selectedSectionIndex
                ? SlideNavigationTransitionEffect.FromRight
                : SlideNavigationTransitionEffect.FromLeft;

            DemoSelectorBarPresenter.Content = CreateSelectorBarSection(index);
            _selectedSectionIndex = index;
        }

        /// <summary>
        /// Builds the peer view for one SelectorBar section, so the slide carries the kind of
        /// content a real section holds instead of a single word.
        /// </summary>
        /// <param name="index">The zero-based index of the selected section.</param>
        private static FrameworkElement CreateSelectorBarSection(int index)
        {
            (string Glyph, string Title, string Detail)[] rows = index switch
            {
                0 =>
                [
                    ("\uE8A5", "Quarterly review.docx", "Edited 12 minutes ago"),
                    ("\uE8A5", "Launch metrics.xlsx", "Edited yesterday"),
                    ("\uE8A5", "Design notes.md", "Edited on Monday"),
                ],
                1 =>
                [
                    ("\uE77B", "Roadmap 2027", "Shared with the design team"),
                    ("\uE77B", "Release checklist", "Shared with quality assurance"),
                    ("\uE77B", "Budget forecast", "Shared with finance"),
                ],
                _ =>
                [
                    ("\uE735", "Team handbook", "Pinned by you"),
                    ("\uE735", "Support playbook", "Pinned by you"),
                    ("\uE735", "Brand assets", "Pinned by you"),
                ],
            };

            StackPanel panel = new() { Margin = new Thickness(0, 8, 0, 0) };
            foreach ((string Glyph, string Title, string Detail) row in rows)
            {
                _ = panel.Children.Add(CreateSelectorBarRow(row.Glyph, row.Title, row.Detail));
            }

            return panel;
        }

        /// <summary>
        /// Builds one row of a SelectorBar section: a leading glyph, the item title, and a
        /// secondary detail line.
        /// </summary>
        /// <param name="glyph">The Segoe Fluent Icons glyph for the row.</param>
        /// <param name="title">The primary text of the row.</param>
        /// <param name="detail">The secondary text of the row.</param>
        private static FrameworkElement CreateSelectorBarRow(string glyph, string title, string detail)
        {
            Grid row = new() { Margin = new Thickness(0, 0, 0, 10) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition());

            Controls.FontIcon icon = new()
            {
                Glyph = glyph,
                IconFontSize = 16,
                Margin = new Thickness(0, 0, 12, 0),
                VerticalAlignment = VerticalAlignment.Center,
            };
            icon.SetResourceReference(Control.ForegroundProperty, "TextFillColorSecondaryBrush");
            _ = row.Children.Add(icon);

            StackPanel text = new();
            Grid.SetColumn(text, 1);

            TextBlock titleBlock = new() { Text = title };
            titleBlock.SetResourceReference(StyleProperty, "BodyTextBlockStyle");
            titleBlock.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorPrimaryBrush");
            _ = text.Children.Add(titleBlock);

            TextBlock detailBlock = new() { Text = detail };
            detailBlock.SetResourceReference(StyleProperty, "CaptionTextBlockStyle");
            detailBlock.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");
            _ = text.Children.Add(detailBlock);

            _ = row.Children.Add(text);
            return row;
        }

        private void GalleryNavigationPage_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= GalleryNavigationPage_Loaded;
            DemoSelectorBar.SelectedIndex = 0;

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
