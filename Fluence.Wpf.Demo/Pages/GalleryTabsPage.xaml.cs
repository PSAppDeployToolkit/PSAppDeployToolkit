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

using Fluence.Wpf.Controls;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace Fluence.Wpf.Demo.Pages
{
    public partial class GalleryTabsPage : UserControl
    {
        private const string TabControlBasicsXamlSource = "<UserControl\n" +
                                                          "    x:Class=\"Fluence.Wpf.Demo.Pages.Tabs.TabControlBasics\"\n" +
                                                          "    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"\n" +
                                                          "    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">\n" +
                                                          "    <TabControl Height=\"210\">\n" +
                                                          "        <TabItem Header=\"Overview\">\n" +
                                                          "            <StackPanel Margin=\"20\">\n" +
                                                          "                <TextBlock\n" +
                                                          "                    FontSize=\"18\"\n" +
                                                          "                    FontWeight=\"SemiBold\"\n" +
                                                          "                    Foreground=\"{DynamicResource TextFillColorPrimaryBrush}\"\n" +
                                                          "                    Text=\"Overview\" />\n" +
                                                          "                <TextBlock\n" +
                                                          "                    Margin=\"0,6,0,0\"\n" +
                                                          "                    Foreground=\"{DynamicResource TextFillColorSecondaryBrush}\"\n" +
                                                          "                    Text=\"A concise summary of the current workspace.\"\n" +
                                                          "                    TextWrapping=\"Wrap\" />\n" +
                                                          "            </StackPanel>\n" +
                                                          "        </TabItem>\n" +
                                                          "        <TabItem Header=\"Activity\">\n" +
                                                          "            <StackPanel Margin=\"20\">\n" +
                                                          "                <TextBlock\n" +
                                                          "                    FontSize=\"18\"\n" +
                                                          "                    FontWeight=\"SemiBold\"\n" +
                                                          "                    Foreground=\"{DynamicResource TextFillColorPrimaryBrush}\"\n" +
                                                          "                    Text=\"Activity\" />\n" +
                                                          "                <TextBlock\n" +
                                                          "                    Margin=\"0,6,0,0\"\n" +
                                                          "                    Foreground=\"{DynamicResource TextFillColorSecondaryBrush}\"\n" +
                                                          "                    Text=\"Recent changes and follow-up work stay grouped in one panel.\"\n" +
                                                          "                    TextWrapping=\"Wrap\" />\n" +
                                                          "            </StackPanel>\n" +
                                                          "        </TabItem>\n" +
                                                          "        <TabItem Header=\"Settings\">\n" +
                                                          "            <StackPanel Margin=\"20\">\n" +
                                                          "                <TextBlock\n" +
                                                          "                    FontSize=\"18\"\n" +
                                                          "                    FontWeight=\"SemiBold\"\n" +
                                                          "                    Foreground=\"{DynamicResource TextFillColorPrimaryBrush}\"\n" +
                                                          "                    Text=\"Settings\" />\n" +
                                                          "                <TextBlock\n" +
                                                          "                    Margin=\"0,6,0,0\"\n" +
                                                          "                    Foreground=\"{DynamicResource TextFillColorSecondaryBrush}\"\n" +
                                                          "                    Text=\"Preferences and configuration can live beside the main content.\"\n" +
                                                          "                    TextWrapping=\"Wrap\" />\n" +
                                                          "            </StackPanel>\n" +
                                                          "        </TabItem>\n" +
                                                          "    </TabControl>\n" +
                                                          "</UserControl>\n";

        private const string TabControlBasicsCSharpSource = "using System.Windows.Controls;\n" +
                                                            "\n" +
                                                            "namespace Fluence.Wpf.Demo.Pages.Tabs\n" +
                                                            "{\n" +
                                                            "    public partial class TabControlBasics : UserControl\n" +
                                                            "    {\n" +
                                                            "        public TabControlBasics()\n" +
                                                            "        {\n" +
                                                            "            InitializeComponent();\n" +
                                                            "        }\n" +
                                                            "    }\n" +
                                                            "}\n";
        private const string TabControlPlacementXamlSource = "<UserControl\n" +
                                                             "    x:Class=\"Fluence.Wpf.Demo.Pages.Tabs.TabControlPlacement\"\n" +
                                                             "    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"\n" +
                                                             "    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">\n" +
                                                             "    <TabControl\n" +
                                                             "        x:Name=\"LeftPlacementTabs\"\n" +
                                                             "        Height=\"220\"\n" +
                                                             "        TabStripPlacement=\"Left\">\n" +
                                                             "        <TabItem Header=\"Inbox\" Width=\"{DynamicResource DemoPlacementTabHeaderWidth}\">\n" +
                                                             "            <TextBlock\n" +
                                                             "                Margin=\"20\"\n" +
                                                             "                Foreground=\"{DynamicResource TextFillColorPrimaryBrush}\"\n" +
                                                             "                Text=\"Left tabs keep vertical categories visible.\"\n" +
                                                             "                TextWrapping=\"Wrap\" />\n" +
                                                             "        </TabItem>\n" +
                                                             "        <TabItem Header=\"Archive\" Width=\"{DynamicResource DemoPlacementTabHeaderWidth}\">\n" +
                                                             "            <TextBlock\n" +
                                                             "                Margin=\"20\"\n" +
                                                             "                Foreground=\"{DynamicResource TextFillColorPrimaryBrush}\"\n" +
                                                             "                Text=\"Archived conversations and completed items.\"\n" +
                                                             "                TextWrapping=\"Wrap\" />\n" +
                                                             "        </TabItem>\n" +
                                                             "    </TabControl>\n" +
                                                             "</UserControl>\n";

        private const string TabControlPlacementCSharpSource = "using System.Windows.Controls;\n" +
                                                               "\n" +
                                                               "namespace Fluence.Wpf.Demo.Pages.Tabs\n" +
                                                               "{\n" +
                                                               "    public partial class TabControlPlacement : UserControl\n" +
                                                               "    {\n" +
                                                               "        public TabControlPlacement()\n" +
                                                               "        {\n" +
                                                               "            InitializeComponent();\n" +
                                                               "        }\n" +
                                                               "    }\n" +
                                                               "}\n";
        private const string TabViewDocumentsXamlSource = "<UserControl\n" +
                                                          "    x:Class=\"Fluence.Wpf.Demo.Pages.Tabs.TabViewDocuments\"\n" +
                                                          "    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"\n" +
                                                          "    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"\n" +
                                                          "    xmlns:ui=\"clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf\">\n" +
                                                          "    <StackPanel>\n" +
                                                          "        <ui:TabView\n" +
                                                          "            x:Name=\"DemoTabView\"\n" +
                                                          "            Height=\"260\"\n" +
                                                          "            AddTabButtonClick=\"DemoTabView_AddTabButtonClick\"\n" +
                                                          "            CloseButtonOverlayMode=\"Auto\"\n" +
                                                          "            TabCloseRequested=\"DemoTabView_TabCloseRequested\">\n" +
                                                          "            <ui:TabViewItem\n" +
                                                          "                Header=\"Document 1\"\n" +
                                                          "                IsSelected=\"True\">\n" +
                                                          "                <ui:TabViewItem.Icon>\n" +
                                                          "                    <ui:FontIcon Glyph=\"&#xE8A5;\" IconFontSize=\"16\" />\n" +
                                                          "                </ui:TabViewItem.Icon>\n" +
                                                          "                <Border Background=\"{DynamicResource LayerFillColorDefaultBrush}\">\n" +
                                                          "                    <StackPanel Margin=\"20\">\n" +
                                                          "                        <TextBlock\n" +
                                                          "                            FontSize=\"18\"\n" +
                                                          "                            FontWeight=\"SemiBold\"\n" +
                                                          "                            Foreground=\"{DynamicResource TextFillColorPrimaryBrush}\"\n" +
                                                          "                            Text=\"Document 1\" />\n" +
                                                          "                        <TextBlock\n" +
                                                          "                            Margin=\"0,6,0,0\"\n" +
                                                          "                            Foreground=\"{DynamicResource TextFillColorSecondaryBrush}\"\n" +
                                                          "                            Text=\"Close document tabs or add another document from the tab row.\"\n" +
                                                          "                            TextWrapping=\"Wrap\" />\n" +
                                                          "                    </StackPanel>\n" +
                                                          "                </Border>\n" +
                                                          "            </ui:TabViewItem>\n" +
                                                          "            <ui:TabViewItem Header=\"Document 2\">\n" +
                                                          "                <ui:TabViewItem.Icon>\n" +
                                                          "                    <ui:FontIcon Glyph=\"&#xE8A5;\" IconFontSize=\"16\" />\n" +
                                                          "                </ui:TabViewItem.Icon>\n" +
                                                          "                <Border Background=\"{DynamicResource LayerFillColorDefaultBrush}\">\n" +
                                                          "                    <StackPanel Margin=\"20\">\n" +
                                                          "                        <TextBlock\n" +
                                                          "                            FontSize=\"18\"\n" +
                                                          "                            FontWeight=\"SemiBold\"\n" +
                                                          "                            Foreground=\"{DynamicResource TextFillColorPrimaryBrush}\"\n" +
                                                          "                            Text=\"Document 2\" />\n" +
                                                          "                        <TextBlock\n" +
                                                          "                            Margin=\"0,6,0,0\"\n" +
                                                          "                            Foreground=\"{DynamicResource TextFillColorSecondaryBrush}\"\n" +
                                                          "                            Text=\"Each tab hosts independent content.\"\n" +
                                                          "                            TextWrapping=\"Wrap\" />\n" +
                                                          "                    </StackPanel>\n" +
                                                          "                </Border>\n" +
                                                          "            </ui:TabViewItem>\n" +
                                                          "            <ui:TabViewItem\n" +
                                                          "                Header=\"Pinned\"\n" +
                                                          "                IsClosable=\"False\">\n" +
                                                          "                <ui:TabViewItem.Icon>\n" +
                                                          "                    <ui:FontIcon Glyph=\"&#xE718;\" IconFontSize=\"16\" />\n" +
                                                          "                </ui:TabViewItem.Icon>\n" +
                                                          "                <Border Background=\"{DynamicResource LayerFillColorDefaultBrush}\">\n" +
                                                          "                    <StackPanel Margin=\"20\">\n" +
                                                          "                        <TextBlock\n" +
                                                          "                            FontSize=\"18\"\n" +
                                                          "                            FontWeight=\"SemiBold\"\n" +
                                                          "                            Foreground=\"{DynamicResource TextFillColorPrimaryBrush}\"\n" +
                                                          "                            Text=\"Pinned\" />\n" +
                                                          "                        <TextBlock\n" +
                                                          "                            Margin=\"0,6,0,0\"\n" +
                                                          "                            Foreground=\"{DynamicResource TextFillColorSecondaryBrush}\"\n" +
                                                          "                            Text=\"Set IsClosable to false when a tab should stay available.\"\n" +
                                                          "                            TextWrapping=\"Wrap\" />\n" +
                                                          "                    </StackPanel>\n" +
                                                          "                </Border>\n" +
                                                          "            </ui:TabViewItem>\n" +
                                                          "        </ui:TabView>\n" +
                                                          "        <TextBlock\n" +
                                                          "            x:Name=\"DemoTabViewStatus\"\n" +
                                                          "            Margin=\"0,12,0,0\"\n" +
                                                          "            Foreground=\"{DynamicResource TextFillColorSecondaryBrush}\"\n" +
                                                          "            Text=\"Tabs: 3\" />\n" +
                                                          "    </StackPanel>\n" +
                                                          "</UserControl>\n";

        private const string TabViewDocumentsCSharpSource = "using System.Windows;\n" +
                                                            "using System.Windows.Controls;\n" +
                                                            "using Fluence.Wpf.Controls;\n" +
                                                            "\n" +
                                                            "namespace Fluence.Wpf.Demo.Pages.Tabs\n" +
                                                            "{\n" +
                                                            "    public partial class TabViewDocuments : UserControl\n" +
                                                            "    {\n" +
                                                            "        private int _nextDocumentNumber = 3;\n" +
                                                            "\n" +
                                                            "        public TabViewDocuments()\n" +
                                                            "        {\n" +
                                                            "            InitializeComponent();\n" +
                                                            "        }\n" +
                                                            "\n" +
                                                            "        private void DemoTabView_AddTabButtonClick(object sender, RoutedEventArgs e)\n" +
                                                            "        {\n" +
                                                            "            int number = ++_nextDocumentNumber;\n" +
                                                            "            System.Windows.Controls.TextBlock body = new()\n" +
                                                            "            {\n" +
                                                            "                Margin = new Thickness(20),\n" +
                                                            "                Text = string.Format(\"Fresh document {0} content.\", number),\n" +
                                                            "                TextWrapping = TextWrapping.Wrap\n" +
                                                            "            };\n" +
                                                            "            body.SetResourceReference(System.Windows.Controls.TextBlock.ForegroundProperty, \"TextFillColorSecondaryBrush\");\n" +
                                                            "\n" +
                                                            "            System.Windows.Controls.Border bodySurface = new()\n" +
                                                            "            {\n" +
                                                            "                Child = body\n" +
                                                            "            };\n" +
                                                            "            bodySurface.SetResourceReference(System.Windows.Controls.Border.BackgroundProperty, \"LayerFillColorDefaultBrush\");\n" +
                                                            "\n" +
                                                            "            TabViewItem tab = new()\n" +
                                                            "            {\n" +
                                                            "                Header = string.Format(\"Document {0}\", number),\n" +
                                                            "                Icon = new FontIcon { Glyph = \"\\uE8A5\", IconFontSize = 16 },\n" +
                                                            "                Content = bodySurface\n" +
                                                            "            };\n" +
                                                            "\n" +
                                                            "            DemoTabView.Items.Add(tab);\n" +
                                                            "            DemoTabView.SelectedItem = tab;\n" +
                                                            "            UpdateStatus();\n" +
                                                            "        }\n" +
                                                            "\n" +
                                                            "        private void DemoTabView_TabCloseRequested(object sender, RoutedEventArgs e)\n" +
                                                            "        {\n" +
                                                            "            if (e is not TabViewTabCloseRequestedEventArgs args || args.Tab is null)\n" +
                                                            "            {\n" +
                                                            "                return;\n" +
                                                            "            }\n" +
                                                            "\n" +
                                                            "            DemoTabView.Items.Remove(args.Tab);\n" +
                                                            "            UpdateStatus();\n" +
                                                            "        }\n" +
                                                            "\n" +
                                                            "        private void UpdateStatus()\n" +
                                                            "        {\n" +
                                                            "            DemoTabViewStatus.Text = string.Format(\"Tabs: {0}\", DemoTabView.Items.Count);\n" +
                                                            "        }\n" +
                                                            "    }\n" +
                                                            "}\n";

        private int _nextDocumentNumber = 4;

        public GalleryTabsPage()
        {
            InitializeComponent();

            DemoSamplePageWiring.Apply(
                (DependencyObject)Content,
                new DemoSampleSource(1, TabControlBasicsXamlSource, TabControlBasicsCSharpSource),
                new DemoSampleSource(2, TabControlPlacementXamlSource, TabControlPlacementCSharpSource),
                new DemoSampleSource(3, TabViewDocumentsXamlSource, TabViewDocumentsCSharpSource));
        }

        private void DemoTabView_AddTabButtonClick(object sender, RoutedEventArgs e)
        {
            if (DemoTabView is null)
            {
                return;
            }

            int number = _nextDocumentNumber++;
            FontIcon icon = new() { Glyph = "\uE8A5", IconFontSize = 16 };
            System.Windows.Controls.TextBlock body = new()
            {
                Margin = new Thickness(16),
                Text = string.Format(CultureInfo.CurrentCulture, "Fresh document {0} content.", number),
                TextWrapping = TextWrapping.Wrap,
            };
            body.SetResourceReference(System.Windows.Controls.TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");

            System.Windows.Controls.Border bodySurface = new()
            {
                Child = body,
            };
            bodySurface.SetResourceReference(System.Windows.Controls.Border.BackgroundProperty, "LayerFillColorDefaultBrush");

            TabViewItem tab = new()
            {
                Header = string.Format(CultureInfo.CurrentCulture, "Document {0}", number),
                Icon = icon,
                Content = bodySurface,
            };

            _ = DemoTabView.Items.Add(tab);
            DemoTabView.SelectedItem = tab;
            UpdateStatus();
        }

        private void DemoTabView_TabCloseRequested(object sender, RoutedEventArgs e)
        {
            if (e is not TabViewTabCloseRequestedEventArgs args || DemoTabView is null || args.Tab is null)
            {
                return;
            }

            DemoTabView.Items.Remove(args.Tab);
            UpdateStatus();
        }

        private void UpdateStatus()
        {
            if (DemoTabViewStatus is null || DemoTabView is null)
            {
                return;
            }

            DemoTabViewStatus.Text = string.Format(CultureInfo.CurrentCulture, "Tabs: {0}", DemoTabView.Items.Count);
        }
    }
}
