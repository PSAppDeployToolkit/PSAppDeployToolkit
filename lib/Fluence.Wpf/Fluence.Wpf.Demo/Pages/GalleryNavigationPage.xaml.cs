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
using NavigationView = Fluence.Wpf.Controls.NavigationView;
using NavigationViewItem = Fluence.Wpf.Controls.NavigationViewItem;

namespace Fluence.Wpf.Demo.Pages
{
    public partial class GalleryNavigationPage : UserControl
    {
        private const string LeftNavigationViewXamlSource = @"<UserControl
    x:Class=""Fluence.Wpf.Demo.Pages.Navigation.LeftNavigationView""
    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
    xmlns:ui=""clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf"">
    <Border
        Height=""320"">
        <ui:NavigationView
            PaneDisplayMode=""Left"">
            <ui:NavigationView.PaneHeader>
                <TextBlock
                    Margin=""12,8""
                    Foreground=""{DynamicResource TextFillColorSecondaryBrush}""
                    Text=""Navigation"" />
            </ui:NavigationView.PaneHeader>
            <ui:NavigationViewItem
                Content=""Home""
                IsSelected=""True"">
                <ui:NavigationViewItem.Icon>
                    <ui:FontIcon Glyph=""&#xE80F;"" IconFontSize=""16"" />
                </ui:NavigationViewItem.Icon>
            </ui:NavigationViewItem>
            <ui:NavigationViewItem Content=""Files"">
                <ui:NavigationViewItem.Icon>
                    <ui:FontIcon Glyph=""&#xE8B7;"" IconFontSize=""16"" />
                </ui:NavigationViewItem.Icon>
            </ui:NavigationViewItem>
            <ui:NavigationViewItem Content=""Reports"">
                <ui:NavigationViewItem.Icon>
                    <ui:FontIcon Glyph=""&#xE9D9;"" IconFontSize=""16"" />
                </ui:NavigationViewItem.Icon>
            </ui:NavigationViewItem>
        </ui:NavigationView>
    </Border>
</UserControl>
";

        private const string LeftNavigationViewCSharpSource = @"using System.Windows.Controls;

namespace Fluence.Wpf.Demo.Pages.Navigation
{
    public partial class LeftNavigationView : UserControl
    {
        public LeftNavigationView()
        {
            InitializeComponent();
        }
    }
}
";
        private const string TopNavigationViewXamlSource = @"<UserControl
    x:Class=""Fluence.Wpf.Demo.Pages.Navigation.TopNavigationView""
    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
    xmlns:ui=""clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf"">
    <Border
        Height=""240"">
        <ui:NavigationView
            Header=""Insights""
            PaneDisplayMode=""Top"">
            <ui:NavigationViewItem
                Content=""Overview""
                IsSelected=""True"">
                <ui:NavigationViewItem.Icon>
                    <ui:FontIcon Glyph=""&#xE9D2;"" IconFontSize=""16"" />
                </ui:NavigationViewItem.Icon>
            </ui:NavigationViewItem>
            <ui:NavigationViewItem Content=""Activity"">
                <ui:NavigationViewItem.Icon>
                    <ui:FontIcon Glyph=""&#xE7F4;"" IconFontSize=""16"" />
                </ui:NavigationViewItem.Icon>
            </ui:NavigationViewItem>
            <ui:NavigationViewItem Content=""Settings"">
                <ui:NavigationViewItem.Icon>
                    <ui:FontIcon Glyph=""&#xE713;"" IconFontSize=""16"" />
                </ui:NavigationViewItem.Icon>
            </ui:NavigationViewItem>
        </ui:NavigationView>
    </Border>
</UserControl>
";

        private const string TopNavigationViewCSharpSource = @"using System.Windows.Controls;

namespace Fluence.Wpf.Demo.Pages.Navigation
{
    public partial class TopNavigationView : UserControl
    {
        public TopNavigationView()
        {
            InitializeComponent();
        }
    }
}
";
        private const string CompactNavigationViewXamlSource = @"<UserControl
    x:Class=""Fluence.Wpf.Demo.Pages.Navigation.CompactNavigationView""
    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
    xmlns:ui=""clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf"">
    <StackPanel>
        <Border
            Height=""300""
            Margin=""0,0,0,12""
            BorderBrush=""{DynamicResource CardStrokeColorDefaultBrush}""
            BorderThickness=""1"">
            <ui:NavigationView
                x:Name=""CompactNavigationDemo""
                IsBackButtonVisible=""True""
                IsBackEnabled=""{Binding IsChecked, ElementName=BackEnabledToggle}""
                IsPaneToggleButtonVisible=""True""
                IsPaneOpen=""False""
                PaneDisplayMode=""LeftCompact"">
                <ui:NavigationView.PaneFooter>
                    <ui:NavigationViewItem Content=""Settings"">
                        <ui:NavigationViewItem.Icon>
                            <ui:FontIcon Glyph=""&#xE713;"" IconFontSize=""16"" />
                        </ui:NavigationViewItem.Icon>
                    </ui:NavigationViewItem>
                </ui:NavigationView.PaneFooter>
                <ui:NavigationViewItem
                    Content=""Dashboard""
                    IsSelected=""True"">
                    <ui:NavigationViewItem.Icon>
                        <ui:FontIcon Glyph=""&#xE80F;"" IconFontSize=""16"" />
                    </ui:NavigationViewItem.Icon>
                </ui:NavigationViewItem>
                <ui:NavigationViewItem Content=""Messages"">
                    <ui:NavigationViewItem.Icon>
                        <ui:FontIcon Glyph=""&#xE8BD;"" IconFontSize=""16"" />
                    </ui:NavigationViewItem.Icon>
                </ui:NavigationViewItem>
            </ui:NavigationView>
        </Border>
            <ui:CheckBox
                x:Name=""BackEnabledToggle""
                Content=""Back enabled""
                IsChecked=""True"" />
    </StackPanel>
</UserControl>
";

        private const string CompactNavigationViewCSharpSource = @"using System.Windows.Controls;

namespace Fluence.Wpf.Demo.Pages.Navigation
{
    public partial class CompactNavigationView : UserControl
    {
        public CompactNavigationView()
        {
            InitializeComponent();
        }
    }
}
";
        private const string InfoBadgeNavigationXamlSource = @"<UserControl
    x:Class=""Fluence.Wpf.Demo.Pages.Navigation.InfoBadgeNavigation""
    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
    xmlns:ui=""clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf""
    xmlns:uicore=""clr-namespace:Fluence.Wpf;assembly=Fluence.Wpf"">
    <Border Height=""260"">
        <ui:NavigationView
            Header=""Inbox""
            IsPaneOpen=""True""
            PaneDisplayMode=""Left"">
            <ui:NavigationViewItem
                Content=""Inbox""
                IsSelected=""True"">
                <ui:NavigationViewItem.Icon>
                    <ui:FontIcon Glyph=""&#xE715;"" IconFontSize=""16"" />
                </ui:NavigationViewItem.Icon>
                <ui:NavigationViewItem.InfoBadge>
                    <ui:InfoBadge Value=""12"" />
                </ui:NavigationViewItem.InfoBadge>
            </ui:NavigationViewItem>
            <ui:NavigationViewItem Content=""Approvals"">
                <ui:NavigationViewItem.Icon>
                    <ui:FontIcon Glyph=""&#xE73E;"" IconFontSize=""16"" />
                </ui:NavigationViewItem.Icon>
                <ui:NavigationViewItem.InfoBadge>
                    <ui:InfoBadge BadgeStyle=""{x:Static uicore:InfoBadgeStyle.Caution}"" />
                </ui:NavigationViewItem.InfoBadge>
            </ui:NavigationViewItem>
            <ui:NavigationViewItem Content=""Alerts"">
                <ui:NavigationViewItem.Icon>
                    <ui:FontIcon Glyph=""&#xE7BA;"" IconFontSize=""16"" />
                </ui:NavigationViewItem.Icon>
                <ui:NavigationViewItem.InfoBadge>
                    <ui:InfoBadge BadgeStyle=""{x:Static uicore:InfoBadgeStyle.Critical}"" Value=""2"" />
                </ui:NavigationViewItem.InfoBadge>
            </ui:NavigationViewItem>
        </ui:NavigationView>
    </Border>
</UserControl>
";

        private const string InfoBadgeNavigationCSharpSource = @"using System.Windows.Controls;

namespace Fluence.Wpf.Demo.Pages.Navigation
{
    public partial class InfoBadgeNavigation : UserControl
    {
        public InfoBadgeNavigation()
        {
            InitializeComponent();
        }
    }
}
";

        public GalleryNavigationPage()
        {
            InitializeComponent();

            DemoSamplePageWiring.Apply(
                (DependencyObject)Content,
                new DemoSampleSource(1, LeftNavigationViewXamlSource, LeftNavigationViewCSharpSource),
                new DemoSampleSource(2, TopNavigationViewXamlSource, TopNavigationViewCSharpSource),
                new DemoSampleSource(3, CompactNavigationViewXamlSource, CompactNavigationViewCSharpSource),
                new DemoSampleSource(4, InfoBadgeNavigationXamlSource, InfoBadgeNavigationCSharpSource));

            Loaded += GalleryNavigationPage_Loaded;
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
            SetNavigationDemoContent(sender as NavigationView, e.InvokedItemContainer);
        }

        private static void SetNavigationDemoContent(NavigationView? nav, NavigationViewItem item)
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
                TextWrapping = TextWrapping.Wrap
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
                Text = text
            };
            textBlock.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorPrimaryBrush");
            return textBlock;
        }

    }
}
