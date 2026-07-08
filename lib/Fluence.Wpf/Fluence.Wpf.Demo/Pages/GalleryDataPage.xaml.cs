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
    public partial class GalleryDataPage : UserControl
    {
        private const string ListViewItemsXamlSource = "<UserControl\n" +
                                                       "    x:Class=\"Fluence.Wpf.Demo.Pages.Data.ListViewItems\"\n" +
                                                       "    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"\n" +
                                                       "    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"\n" +
                                                       "    xmlns:ui=\"clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf\">\n" +
                                                       "    <Grid>\n" +
                                                       "        <Grid.ColumnDefinitions>\n" +
                                                       "            <ColumnDefinition Width=\"*\" />\n" +
                                                       "            <ColumnDefinition Width=\"20\" />\n" +
                                                       "            <ColumnDefinition Width=\"*\" />\n" +
                                                       "        </Grid.ColumnDefinitions>\n" +
                                                       "        <Border\n" +
                                                       "            x:Name=\"SimpleListViewBackground\"\n" +
                                                       "            CornerRadius=\"{DynamicResource ControlCornerRadius}\">\n" +
                                                       "            <ui:ListView\n" +
                                                       "                x:Name=\"SimpleListView\"\n" +
                                                       "                Height=\"230\"\n" +
                                                       "                BorderBrush=\"{DynamicResource CardStrokeColorDefaultBrush}\"\n" +
                                                       "                BorderThickness=\"1\">\n" +
                                                       "                <ListViewItem Content=\"Ana Bowman\" />\n" +
                                                       "                <ListViewItem Content=\"Shawn Hughes\" />\n" +
                                                       "                <ListViewItem Content=\"Oscar Ward\" />\n" +
                                                       "                <ListViewItem Content=\"Madison Butler\" />\n" +
                                                       "                <ListViewItem Content=\"Graham Barnes\" />\n" +
                                                       "            </ui:ListView>\n" +
                                                       "        </Border>\n" +
                                                       "        <Border\n" +
                                                       "            x:Name=\"RichListViewBackground\"\n" +
                                                       "            Grid.Column=\"2\"\n" +
                                                       "            CornerRadius=\"{DynamicResource ControlCornerRadius}\">\n" +
                                                       "            <ui:ListView\n" +
                                                       "                x:Name=\"RichListView\"\n" +
                                                       "                Height=\"230\"\n" +
                                                       "                BorderBrush=\"{DynamicResource CardStrokeColorDefaultBrush}\"\n" +
                                                       "                BorderThickness=\"1\">\n" +
                                                       "                <ListViewItem>\n" +
                                                       "                    <Grid Margin=\"0,4\">\n" +
                                                       "                        <Grid.ColumnDefinitions>\n" +
                                                       "                            <ColumnDefinition Width=\"36\" />\n" +
                                                       "                            <ColumnDefinition Width=\"*\" />\n" +
                                                       "                        </Grid.ColumnDefinitions>\n" +
                                                       "                        <ui:FontIcon\n" +
                                                       "                            VerticalAlignment=\"Center\"\n" +
                                                       "                            Foreground=\"{DynamicResource TextFillColorSecondaryBrush}\"\n" +
                                                       "                            Glyph=\"&#xE77B;\"\n" +
                                                       "                            IconFontSize=\"20\" />\n" +
                                                       "                        <StackPanel Grid.Column=\"1\">\n" +
                                                       "                            <TextBlock FontWeight=\"SemiBold\" Text=\"Ana Bowman\" />\n" +
                                                       "                            <TextBlock\n" +
                                                       "                                FontSize=\"12\"\n" +
                                                       "                                Foreground=\"{DynamicResource TextFillColorSecondaryBrush}\"\n" +
                                                       "                                Text=\"Support Engineer\" />\n" +
                                                       "                        </StackPanel>\n" +
                                                       "                    </Grid>\n" +
                                                       "                </ListViewItem>\n" +
                                                       "                <ListViewItem>\n" +
                                                       "                    <Grid Margin=\"0,4\">\n" +
                                                       "                        <Grid.ColumnDefinitions>\n" +
                                                       "                            <ColumnDefinition Width=\"36\" />\n" +
                                                       "                            <ColumnDefinition Width=\"*\" />\n" +
                                                       "                        </Grid.ColumnDefinitions>\n" +
                                                       "                        <ui:FontIcon\n" +
                                                       "                            VerticalAlignment=\"Center\"\n" +
                                                       "                            Foreground=\"{DynamicResource TextFillColorSecondaryBrush}\"\n" +
                                                       "                            Glyph=\"&#xE77B;\"\n" +
                                                       "                            IconFontSize=\"20\" />\n" +
                                                       "                        <StackPanel Grid.Column=\"1\">\n" +
                                                       "                            <TextBlock FontWeight=\"SemiBold\" Text=\"Shawn Hughes\" />\n" +
                                                       "                            <TextBlock\n" +
                                                       "                                FontSize=\"12\"\n" +
                                                       "                                Foreground=\"{DynamicResource TextFillColorSecondaryBrush}\"\n" +
                                                       "                                Text=\"Platform Specialist\" />\n" +
                                                       "                        </StackPanel>\n" +
                                                       "                    </Grid>\n" +
                                                       "                </ListViewItem>\n" +
                                                       "                <ListViewItem>\n" +
                                                       "                    <Grid Margin=\"0,4\">\n" +
                                                       "                        <Grid.ColumnDefinitions>\n" +
                                                       "                            <ColumnDefinition Width=\"36\" />\n" +
                                                       "                            <ColumnDefinition Width=\"*\" />\n" +
                                                       "                        </Grid.ColumnDefinitions>\n" +
                                                       "                        <ui:FontIcon\n" +
                                                       "                            VerticalAlignment=\"Center\"\n" +
                                                       "                            Foreground=\"{DynamicResource TextFillColorSecondaryBrush}\"\n" +
                                                       "                            Glyph=\"&#xE77B;\"\n" +
                                                       "                            IconFontSize=\"20\" />\n" +
                                                       "                        <StackPanel Grid.Column=\"1\">\n" +
                                                       "                            <TextBlock FontWeight=\"SemiBold\" Text=\"Oscar Ward\" />\n" +
                                                       "                            <TextBlock\n" +
                                                       "                                FontSize=\"12\"\n" +
                                                       "                                Foreground=\"{DynamicResource TextFillColorSecondaryBrush}\"\n" +
                                                       "                                Text=\"DevOps Lead\" />\n" +
                                                       "                        </StackPanel>\n" +
                                                       "                    </Grid>\n" +
                                                       "                </ListViewItem>\n" +
                                                       "            </ui:ListView>\n" +
                                                       "        </Border>\n" +
                                                       "    </Grid>\n" +
                                                       "</UserControl>\n";

        private const string ListViewItemsCSharpSource = "using System.Windows.Controls;\n" +
                                                         "\n" +
                                                         "namespace Fluence.Wpf.Demo.Pages.Data\n" +
                                                         "{\n" +
                                                         "    public partial class ListViewItems : UserControl\n" +
                                                         "    {\n" +
                                                         "        public ListViewItems()\n" +
                                                         "        {\n" +
                                                         "            InitializeComponent();\n" +
                                                         "        }\n" +
                                                         "    }\n" +
                                                         "}\n";
        private const string ListViewEmptyStateXamlSource = "<UserControl\n" +
                                                            "    x:Class=\"Fluence.Wpf.Demo.Pages.Data.ListViewEmptyState\"\n" +
                                                            "    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"\n" +
                                                            "    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"\n" +
                                                            "    xmlns:ui=\"clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf\">\n" +
                                                            "    <StackPanel>\n" +
                                                            "        <ui:ListView\n" +
                                                            "            x:Name=\"EmptyStateListView\"\n" +
                                                            "            Height=\"180\"\n" +
                                                            "            Margin=\"0,0,0,12\"\n" +
                                                            "            Background=\"{DynamicResource CardBackgroundFillColorDefaultBrush}\"\n" +
                                                            "            BorderBrush=\"{DynamicResource CardStrokeColorDefaultBrush}\"\n" +
                                                            "            BorderThickness=\"1\">\n" +
                                                            "            <ui:ListView.EmptyContent>\n" +
                                                            "                <TextBlock\n" +
                                                            "                    HorizontalAlignment=\"Center\"\n" +
                                                            "                    VerticalAlignment=\"Center\"\n" +
                                                            "                    Foreground=\"{DynamicResource TextFillColorSecondaryBrush}\"\n" +
                                                            "                    Text=\"No items. Add one to begin.\" />\n" +
                                                            "            </ui:ListView.EmptyContent>\n" +
                                                            "        </ui:ListView>\n" +
                                                            "        <StackPanel\n" +
                                                            "            x:Name=\"EmptyStateActionsPanel\"\n" +
                                                            "            HorizontalAlignment=\"Center\"\n" +
                                                            "            VerticalAlignment=\"Center\"\n" +
                                                            "            Orientation=\"Horizontal\">\n" +
                                                            "            <ui:Button\n" +
                                                            "                Margin=\"0,0,8,0\"\n" +
                                                            "                Appearance=\"Accent\"\n" +
                                                            "                Click=\"AddListItem_Click\"\n" +
                                                            "                Content=\"Add item\"\n" +
                                                            "                MinWidth=\"140\" />\n" +
                                                            "            <ui:Button\n" +
                                                            "                Click=\"RemoveListItem_Click\"\n" +
                                                            "                Content=\"Remove item\"\n" +
                                                            "                MinWidth=\"140\" />\n" +
                                                            "        </StackPanel>\n" +
                                                            "    </StackPanel>\n" +
                                                            "</UserControl>\n";

        private const string ListViewEmptyStateCSharpSource = "using System.Windows;\n" +
                                                              "using System.Windows.Controls;\n" +
                                                              "\n" +
                                                              "namespace Fluence.Wpf.Demo.Pages.Data\n" +
                                                              "{\n" +
                                                              "    public partial class ListViewEmptyState : UserControl\n" +
                                                              "    {\n" +
                                                              "        private int _addCounter;\n" +
                                                              "\n" +
                                                              "        private static readonly string[] SampleNames =\n" +
                                                              "        {\n" +
                                                              "            \"Liam Torres\",\n" +
                                                              "            \"Nora Fischer\",\n" +
                                                              "            \"Eli Nakamura\",\n" +
                                                              "            \"Priya Kapoor\",\n" +
                                                              "            \"Dante Reeves\"\n" +
                                                              "        };\n" +
                                                              "\n" +
                                                              "        public ListViewEmptyState()\n" +
                                                              "        {\n" +
                                                              "            InitializeComponent();\n" +
                                                              "        }\n" +
                                                              "\n" +
                                                              "        private void AddListItem_Click(object sender, RoutedEventArgs e)\n" +
                                                              "        {\n" +
                                                              "            string name = SampleNames[_addCounter % SampleNames.Length];\n" +
                                                              "            _addCounter++;\n" +
                                                              "\n" +
                                                              "            EmptyStateListView.Items.Add(new ListViewItem { Content = name });\n" +
                                                              "        }\n" +
                                                              "\n" +
                                                              "        private void RemoveListItem_Click(object sender, RoutedEventArgs e)\n" +
                                                              "        {\n" +
                                                              "            if (EmptyStateListView.Items.Count == 0)\n" +
                                                              "            {\n" +
                                                              "                return;\n" +
                                                              "            }\n" +
                                                              "\n" +
                                                              "            object lastItem = EmptyStateListView.Items[EmptyStateListView.Items.Count - 1];\n" +
                                                              "            EmptyStateListView.AnimateRemove(lastItem, null);\n" +
                                                              "        }\n" +
                                                              "    }\n" +
                                                              "}\n";
        private const string ListBoxSelectionXamlSource = "<UserControl\n" +
                                                          "    x:Class=\"Fluence.Wpf.Demo.Pages.Data.ListBoxSelection\"\n" +
                                                          "    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"\n" +
                                                          "    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"\n" +
                                                          "    xmlns:ui=\"clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf\">\n" +
                                                          "    <Grid>\n" +
                                                          "        <Grid.ColumnDefinitions>\n" +
                                                          "            <ColumnDefinition Width=\"*\" />\n" +
                                                          "            <ColumnDefinition Width=\"20\" />\n" +
                                                          "            <ColumnDefinition Width=\"*\" />\n" +
                                                          "        </Grid.ColumnDefinitions>\n" +
                                                          "        <ui:ListBox\n" +
                                                          "            x:Name=\"SingleSelectListBox\"\n" +
                                                          "            Height=\"180\"\n" +
                                                          "            BorderBrush=\"{DynamicResource CardStrokeColorDefaultBrush}\"\n" +
                                                          "            BorderThickness=\"1\">\n" +
                                                          "            <ui:ListBoxItem Content=\"Documents\" IsSelected=\"True\" />\n" +
                                                          "            <ui:ListBoxItem Content=\"Pictures\" />\n" +
                                                          "            <ui:ListBoxItem Content=\"Music\" />\n" +
                                                          "            <ui:ListBoxItem Content=\"Videos\" />\n" +
                                                          "            <ui:ListBoxItem Content=\"Downloads\" />\n" +
                                                          "        </ui:ListBox>\n" +
                                                          "        <ui:ListBox\n" +
                                                          "            x:Name=\"MultiSelectListBox\"\n" +
                                                          "            Grid.Column=\"2\"\n" +
                                                          "            Height=\"180\"\n" +
                                                          "            BorderBrush=\"{DynamicResource CardStrokeColorDefaultBrush}\"\n" +
                                                          "            BorderThickness=\"1\"\n" +
                                                          "            SelectionMode=\"Extended\">\n" +
                                                          "            <ui:ListBoxItem Content=\"Critical\" IsSelected=\"True\" />\n" +
                                                          "            <ui:ListBoxItem Content=\"Error\" IsSelected=\"True\" />\n" +
                                                          "            <ui:ListBoxItem Content=\"Warning\" />\n" +
                                                          "            <ui:ListBoxItem Content=\"Information\" />\n" +
                                                          "            <ui:ListBoxItem Content=\"Verbose\" />\n" +
                                                          "        </ui:ListBox>\n" +
                                                          "    </Grid>\n" +
                                                          "</UserControl>\n";

        private const string ListBoxSelectionCSharpSource = "using System.Windows.Controls;\n" +
                                                            "\n" +
                                                            "namespace Fluence.Wpf.Demo.Pages.Data\n" +
                                                            "{\n" +
                                                            "    public partial class ListBoxSelection : UserControl\n" +
                                                            "    {\n" +
                                                            "        public ListBoxSelection()\n" +
                                                            "        {\n" +
                                                            "            InitializeComponent();\n" +
                                                            "        }\n" +
                                                            "    }\n" +
                                                            "}\n";
        private const string CardVariantsXamlSource = "<UserControl\n" +
                                                      "    x:Class=\"Fluence.Wpf.Demo.Pages.Data.CardVariants\"\n" +
                                                      "    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"\n" +
                                                      "    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"\n" +
                                                      "    xmlns:ui=\"clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf\"\n" +
                                                      "    xmlns:uicore=\"clr-namespace:Fluence.Wpf;assembly=Fluence.Wpf\">\n" +
                                                      "    <UniformGrid Columns=\"2\">\n" +
                                                      "        <ui:Card\n" +
                                                      "            MinHeight=\"110\"\n" +
                                                      "            Margin=\"0,0,16,16\"\n" +
                                                      "            Padding=\"18\"\n" +
                                                      "            Header=\"Default\"\n" +
                                                      "            Variant=\"{x:Static uicore:CardVariant.Default}\">\n" +
                                                      "            <TextBlock Text=\"Standard surface for grouped content.\" TextWrapping=\"Wrap\" />\n" +
                                                      "        </ui:Card>\n" +
                                                      "        <ui:Card\n" +
                                                      "            MinHeight=\"110\"\n" +
                                                      "            Margin=\"0,0,0,16\"\n" +
                                                      "            Padding=\"18\"\n" +
                                                      "            Header=\"Outlined\"\n" +
                                                      "            Variant=\"{x:Static uicore:CardVariant.Outlined}\">\n" +
                                                      "            <TextBlock Text=\"Emphasizes the boundary over fill.\" TextWrapping=\"Wrap\" />\n" +
                                                      "        </ui:Card>\n" +
                                                      "        <ui:Card\n" +
                                                      "            MinHeight=\"110\"\n" +
                                                      "            Margin=\"0,0,16,0\"\n" +
                                                      "            Padding=\"18\"\n" +
                                                      "            Header=\"Filled\"\n" +
                                                      "            Variant=\"{x:Static uicore:CardVariant.Filled}\">\n" +
                                                      "            <TextBlock Text=\"Adds stronger container presence.\" TextWrapping=\"Wrap\" />\n" +
                                                      "        </ui:Card>\n" +
                                                      "        <ui:Card\n" +
                                                      "            MinHeight=\"110\"\n" +
                                                      "            Padding=\"18\"\n" +
                                                      "            Header=\"Subtle\"\n" +
                                                      "            Variant=\"{x:Static uicore:CardVariant.Subtle}\">\n" +
                                                      "            <TextBlock Text=\"Keeps low-emphasis supporting content grouped.\" TextWrapping=\"Wrap\" />\n" +
                                                      "        </ui:Card>\n" +
                                                      "    </UniformGrid>\n" +
                                                      "</UserControl>\n";

        private const string CardVariantsCSharpSource = "using System.Windows.Controls;\n" +
                                                        "\n" +
                                                        "namespace Fluence.Wpf.Demo.Pages.Data\n" +
                                                        "{\n" +
                                                        "    public partial class CardVariants : UserControl\n" +
                                                        "    {\n" +
                                                        "        public CardVariants()\n" +
                                                        "        {\n" +
                                                        "            InitializeComponent();\n" +
                                                        "        }\n" +
                                                        "    }\n" +
                                                        "}\n";
        private const string PersonPictureXamlSource = "<UserControl\n" +
                                                       "    x:Class=\"Fluence.Wpf.Demo.Pages.Data.PersonPictureSample\"\n" +
                                                       "    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"\n" +
                                                       "    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"\n" +
                                                       "    xmlns:ui=\"clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf\">\n" +
                                                       "    <WrapPanel\n" +
                                                       "        HorizontalAlignment=\"Center\"\n" +
                                                       "        VerticalAlignment=\"Center\">\n" +
                                                       "        <ui:PersonPicture\n" +
                                                       "            Width=\"56\"\n" +
                                                       "            Height=\"56\"\n" +
                                                       "            Margin=\"0,0,12,12\"\n" +
                                                       "            DisplayName=\"Ana Bowman\"\n" +
                                                       "            ProfilePicture=\"pack://application:,,,/Fluence.Wpf.Demo;component/Resources/ControlImages/PersonPictureAnaBowman.png\" />\n" +
                                                       "        <ui:PersonPicture\n" +
                                                       "            Width=\"56\"\n" +
                                                       "            Height=\"56\"\n" +
                                                       "            Margin=\"0,0,12,12\"\n" +
                                                       "            DisplayName=\"Shawn Hughes\"\n" +
                                                       "            ProfilePicture=\"pack://application:,,,/Fluence.Wpf.Demo;component/Resources/ControlImages/PersonPictureShawnHughes.png\"\n" +
                                                       "            BadgeNumber=\"3\" />\n" +
                                                       "        <ui:PersonPicture\n" +
                                                       "            Width=\"56\"\n" +
                                                       "            Height=\"56\"\n" +
                                                       "            Margin=\"0,0,12,12\"\n" +
                                                       "            DisplayName=\"Priya Kapoor\"\n" +
                                                       "            ProfilePicture=\"pack://application:,,,/Fluence.Wpf.Demo;component/Resources/ControlImages/PersonPicturePriyaKapoor.png\" />\n" +
                                                       "        <ui:PersonPicture\n" +
                                                       "            Width=\"56\"\n" +
                                                       "            Height=\"56\"\n" +
                                                       "            Margin=\"0,0,12,12\"\n" +
                                                       "            DisplayName=\"Mateo Rivera\"\n" +
                                                       "            ProfilePicture=\"pack://application:,,,/Fluence.Wpf.Demo;component/Resources/ControlImages/PersonPictureMateoRivera.png\" />\n" +
                                                       "        <ui:PersonPicture\n" +
                                                       "            Width=\"56\"\n" +
                                                       "            Height=\"56\"\n" +
                                                       "            Margin=\"0,0,12,12\"\n" +
                                                       "            DisplayName=\"Madison Butler\"\n" +
                                                       "            ProfilePicture=\"pack://application:,,,/Fluence.Wpf.Demo;component/Resources/ControlImages/PersonPictureMadisonButler.png\" />\n" +
                                                       "    </WrapPanel>\n" +
                                                       "</UserControl>\n";

        private const string PersonPictureCSharpSource = "using System.Windows.Controls;\n" +
                                                         "\n" +
                                                         "namespace Fluence.Wpf.Demo.Pages.Data\n" +
                                                         "{\n" +
                                                         "    public partial class PersonPictureSample : UserControl\n" +
                                                         "    {\n" +
                                                         "        public PersonPictureSample()\n" +
                                                         "        {\n" +
                                                         "            InitializeComponent();\n" +
                                                         "        }\n" +
                                                         "    }\n" +
                                                         "}\n";

        private int _addCounter;

        private static readonly string[] SampleNames =
        [
            "Liam Torres",
            "Nora Fischer",
            "Eli Nakamura",
            "Priya Kapoor",
            "Dante Reeves",
        ];

        public GalleryDataPage()
        {
            InitializeComponent();

            DemoSamplePageWiring.Apply(
                (DependencyObject)Content,
                new DemoSampleSource(1, ListViewItemsXamlSource, ListViewItemsCSharpSource),
                new DemoSampleSource(2, ListViewEmptyStateXamlSource, ListViewEmptyStateCSharpSource),
                new DemoSampleSource(3, ListBoxSelectionXamlSource, ListBoxSelectionCSharpSource),
                new DemoSampleSource(4, PersonPictureXamlSource, PersonPictureCSharpSource),
                new DemoSampleSource(5, CardVariantsXamlSource, CardVariantsCSharpSource));
        }

        private void AddListItem_Click(object sender, RoutedEventArgs e)
        {
            if (EmptyStateListView is null)
            {
                return;
            }

            string name = SampleNames[_addCounter % SampleNames.Length];
            _addCounter++;

            _ = EmptyStateListView.Items.Add(new ListViewItem { Content = name });
        }

        private void RemoveListItem_Click(object sender, RoutedEventArgs e)
        {
            if (EmptyStateListView is null || EmptyStateListView.Items.Count is 0)
            {
                return;
            }

            object lastItem = EmptyStateListView.Items[^1];
            EmptyStateListView.AnimateRemove(lastItem, onCompleted: null);
        }
    }
}
