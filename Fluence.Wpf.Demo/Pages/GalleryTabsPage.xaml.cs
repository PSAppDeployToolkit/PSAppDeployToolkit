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
        private const string TabControlBasicsXamlSource = @"<UserControl
    x:Class=""Fluence.Wpf.Demo.Pages.Tabs.TabControlBasics""
    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <TabControl Height=""210"">
        <TabItem Header=""Overview"">
            <StackPanel Margin=""20"">
                <TextBlock
                    FontSize=""18""
                    FontWeight=""SemiBold""
                    Foreground=""{DynamicResource TextFillColorPrimaryBrush}""
                    Text=""Overview"" />
                <TextBlock
                    Margin=""0,6,0,0""
                    Foreground=""{DynamicResource TextFillColorSecondaryBrush}""
                    Text=""A concise summary of the current workspace.""
                    TextWrapping=""Wrap"" />
            </StackPanel>
        </TabItem>
        <TabItem Header=""Activity"">
            <StackPanel Margin=""20"">
                <TextBlock
                    FontSize=""18""
                    FontWeight=""SemiBold""
                    Foreground=""{DynamicResource TextFillColorPrimaryBrush}""
                    Text=""Activity"" />
                <TextBlock
                    Margin=""0,6,0,0""
                    Foreground=""{DynamicResource TextFillColorSecondaryBrush}""
                    Text=""Recent changes and follow-up work stay grouped in one panel.""
                    TextWrapping=""Wrap"" />
            </StackPanel>
        </TabItem>
        <TabItem Header=""Settings"">
            <StackPanel Margin=""20"">
                <TextBlock
                    FontSize=""18""
                    FontWeight=""SemiBold""
                    Foreground=""{DynamicResource TextFillColorPrimaryBrush}""
                    Text=""Settings"" />
                <TextBlock
                    Margin=""0,6,0,0""
                    Foreground=""{DynamicResource TextFillColorSecondaryBrush}""
                    Text=""Preferences and configuration can live beside the main content.""
                    TextWrapping=""Wrap"" />
            </StackPanel>
        </TabItem>
    </TabControl>
</UserControl>
";

        private const string TabControlBasicsCSharpSource = @"using System.Windows.Controls;

namespace Fluence.Wpf.Demo.Pages.Tabs
{
    public partial class TabControlBasics : UserControl
    {
        public TabControlBasics()
        {
            InitializeComponent();
        }
    }
}
";
        private const string TabControlPlacementXamlSource = @"<UserControl
    x:Class=""Fluence.Wpf.Demo.Pages.Tabs.TabControlPlacement""
    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <TabControl
        x:Name=""LeftPlacementTabs""
        Height=""220""
        TabStripPlacement=""Left"">
        <TabItem Header=""Inbox"" Width=""{DynamicResource DemoPlacementTabHeaderWidth}"">
            <TextBlock
                Margin=""20""
                Foreground=""{DynamicResource TextFillColorPrimaryBrush}""
                Text=""Left tabs keep vertical categories visible.""
                TextWrapping=""Wrap"" />
        </TabItem>
        <TabItem Header=""Archive"" Width=""{DynamicResource DemoPlacementTabHeaderWidth}"">
            <TextBlock
                Margin=""20""
                Foreground=""{DynamicResource TextFillColorPrimaryBrush}""
                Text=""Archived conversations and completed items.""
                TextWrapping=""Wrap"" />
        </TabItem>
    </TabControl>
</UserControl>
";

        private const string TabControlPlacementCSharpSource = @"using System.Windows.Controls;

namespace Fluence.Wpf.Demo.Pages.Tabs
{
    public partial class TabControlPlacement : UserControl
    {
        public TabControlPlacement()
        {
            InitializeComponent();
        }
    }
}
";
        private const string TabViewDocumentsXamlSource = @"<UserControl
    x:Class=""Fluence.Wpf.Demo.Pages.Tabs.TabViewDocuments""
    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
    xmlns:ui=""clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf"">
    <StackPanel>
        <ui:TabView
            x:Name=""DemoTabView""
            Height=""260""
            AddTabButtonClick=""DemoTabView_AddTabButtonClick""
            CloseButtonOverlayMode=""Auto""
            TabCloseRequested=""DemoTabView_TabCloseRequested"">
            <ui:TabViewItem
                Header=""Document 1""
                IsSelected=""True"">
                <ui:TabViewItem.Icon>
                    <ui:FontIcon Glyph=""&#xE8A5;"" IconFontSize=""16"" />
                </ui:TabViewItem.Icon>
                <Border Background=""{DynamicResource LayerFillColorDefaultBrush}"">
                    <StackPanel Margin=""20"">
                        <TextBlock
                            FontSize=""18""
                            FontWeight=""SemiBold""
                            Foreground=""{DynamicResource TextFillColorPrimaryBrush}""
                            Text=""Document 1"" />
                        <TextBlock
                            Margin=""0,6,0,0""
                            Foreground=""{DynamicResource TextFillColorSecondaryBrush}""
                            Text=""Close document tabs or add another document from the tab row.""
                            TextWrapping=""Wrap"" />
                    </StackPanel>
                </Border>
            </ui:TabViewItem>
            <ui:TabViewItem Header=""Document 2"">
                <ui:TabViewItem.Icon>
                    <ui:FontIcon Glyph=""&#xE8A5;"" IconFontSize=""16"" />
                </ui:TabViewItem.Icon>
                <Border Background=""{DynamicResource LayerFillColorDefaultBrush}"">
                    <StackPanel Margin=""20"">
                        <TextBlock
                            FontSize=""18""
                            FontWeight=""SemiBold""
                            Foreground=""{DynamicResource TextFillColorPrimaryBrush}""
                            Text=""Document 2"" />
                        <TextBlock
                            Margin=""0,6,0,0""
                            Foreground=""{DynamicResource TextFillColorSecondaryBrush}""
                            Text=""Each tab hosts independent content.""
                            TextWrapping=""Wrap"" />
                    </StackPanel>
                </Border>
            </ui:TabViewItem>
            <ui:TabViewItem
                Header=""Pinned""
                IsClosable=""False"">
                <ui:TabViewItem.Icon>
                    <ui:FontIcon Glyph=""&#xE718;"" IconFontSize=""16"" />
                </ui:TabViewItem.Icon>
                <Border Background=""{DynamicResource LayerFillColorDefaultBrush}"">
                    <StackPanel Margin=""20"">
                        <TextBlock
                            FontSize=""18""
                            FontWeight=""SemiBold""
                            Foreground=""{DynamicResource TextFillColorPrimaryBrush}""
                            Text=""Pinned"" />
                        <TextBlock
                            Margin=""0,6,0,0""
                            Foreground=""{DynamicResource TextFillColorSecondaryBrush}""
                            Text=""Set IsClosable to false when a tab should stay available.""
                            TextWrapping=""Wrap"" />
                    </StackPanel>
                </Border>
            </ui:TabViewItem>
        </ui:TabView>
        <TextBlock
            x:Name=""DemoTabViewStatus""
            Margin=""0,12,0,0""
            Foreground=""{DynamicResource TextFillColorSecondaryBrush}""
            Text=""Tabs: 3"" />
    </StackPanel>
</UserControl>
";

        private const string TabViewDocumentsCSharpSource = @"using System.Windows;
using System.Windows.Controls;
using Fluence.Wpf.Controls;

namespace Fluence.Wpf.Demo.Pages.Tabs
{
    public partial class TabViewDocuments : UserControl
    {
        private int _nextDocumentNumber = 3;

        public TabViewDocuments()
        {
            InitializeComponent();
        }

        private void DemoTabView_AddTabButtonClick(object sender, RoutedEventArgs e)
        {
            int number = ++_nextDocumentNumber;
            System.Windows.Controls.TextBlock body = new()
            {
                Margin = new Thickness(20),
                Text = string.Format(""Fresh document {0} content."", number),
                TextWrapping = TextWrapping.Wrap
            };
            body.SetResourceReference(System.Windows.Controls.TextBlock.ForegroundProperty, ""TextFillColorSecondaryBrush"");

            System.Windows.Controls.Border bodySurface = new()
            {
                Child = body
            };
            bodySurface.SetResourceReference(System.Windows.Controls.Border.BackgroundProperty, ""LayerFillColorDefaultBrush"");

            TabViewItem tab = new()
            {
                Header = string.Format(""Document {0}"", number),
                Icon = new FontIcon { Glyph = ""\uE8A5"", IconFontSize = 16 },
                Content = bodySurface
            };

            DemoTabView.Items.Add(tab);
            DemoTabView.SelectedItem = tab;
            UpdateStatus();
        }

        private void DemoTabView_TabCloseRequested(object sender, RoutedEventArgs e)
        {
            if (e is not TabViewTabCloseRequestedEventArgs args || args.Tab is null)
            {
                return;
            }

            DemoTabView.Items.Remove(args.Tab);
            UpdateStatus();
        }

        private void UpdateStatus()
        {
            DemoTabViewStatus.Text = string.Format(""Tabs: {0}"", DemoTabView.Items.Count);
        }
    }
}
";

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
                TextWrapping = TextWrapping.Wrap
            };
            body.SetResourceReference(System.Windows.Controls.TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");

            System.Windows.Controls.Border bodySurface = new()
            {
                Child = body
            };
            bodySurface.SetResourceReference(System.Windows.Controls.Border.BackgroundProperty, "LayerFillColorDefaultBrush");

            TabViewItem tab = new()
            {
                Header = string.Format(CultureInfo.CurrentCulture, "Document {0}", number),
                Icon = icon,
                Content = bodySurface
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
