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

using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Fluence.Wpf.Demo.Pages
{
    public partial class GalleryMenusPage : UserControl
    {
        private const string MenuBarXamlSource = @"<UserControl
    x:Class=""Fluence.Wpf.Demo.Pages.Menus.MenuBar""
    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
    xmlns:ui=""clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf"">
    <StackPanel>
        <ui:Menu
            Margin=""0,0,0,12"">
            <ui:MenuItem Header=""_File"">
                <ui:MenuItem
                    Click=""MenuBar_Click""
                    Header=""_New""
                    InputGestureText=""Ctrl+N""
                    Tag=""File - New"" />
                <ui:MenuItem
                    Click=""MenuBar_Click""
                    Header=""_Open...""
                    InputGestureText=""Ctrl+O""
                    Tag=""File - Open"" />
                <ui:MenuItem Header=""Open _Recent"">
                    <ui:MenuItem
                        Click=""MenuBar_Click""
                        Header=""Roadmap.md""
                        Tag=""File - Recent - Roadmap.md"" />
                    <ui:MenuItem
                        Click=""MenuBar_Click""
                        Header=""LaunchPlan.xlsx""
                        Tag=""File - Recent - LaunchPlan.xlsx"" />
                </ui:MenuItem>
                <Separator />
                <ui:MenuItem
                    Click=""MenuBar_Click""
                    Header=""_Save""
                    InputGestureText=""Ctrl+S""
                    Tag=""File - Save"" />
                <ui:MenuItem Header=""Print"" IsEnabled=""False"" />
            </ui:MenuItem>
            <ui:MenuItem Header=""_View"">
                <ui:MenuItem
                    Click=""MenuBar_Click""
                    Header=""Status bar""
                    IsCheckable=""True""
                    IsChecked=""True""
                    Tag=""View - Status bar"" />
                <ui:MenuItem
                    Click=""MenuBar_Click""
                    Header=""Word wrap""
                    IsCheckable=""True""
                    Tag=""View - Word wrap"" />
                <Separator />
                <ui:MenuItem
                    Click=""MenuBar_Click""
                    Header=""Zoom in""
                    InputGestureText=""Ctrl++""
                    Tag=""View - Zoom in"" />
            </ui:MenuItem>
            <ui:MenuItem Header=""_Help"">
                <ui:MenuItem
                    Click=""MenuBar_Click""
                    Header=""Documentation""
                    Tag=""Help - Documentation"" />
                <ui:MenuItem Header=""About"" IsEnabled=""False"" />
            </ui:MenuItem>
        </ui:Menu>
        <TextBlock
            x:Name=""MenuBarResultLabel""
            Foreground=""{DynamicResource TextFillColorSecondaryBrush}""
            Text=""Last menu action: None"" />
    </StackPanel>
</UserControl>
";

        private const string MenuBarCSharpSource = @"using System.Windows;
using System.Windows.Controls;

namespace Fluence.Wpf.Demo.Pages.Menus
{
    public partial class MenuBar : UserControl
    {
        public MenuBar()
        {
            InitializeComponent();
        }

        private void MenuBar_Click(object sender, RoutedEventArgs e)
        {
            string action = sender is FrameworkElement element && element.Tag is string tag ? tag : string.Empty;
            MenuBarResultLabel.Text = string.Format(""Last menu action: {0}"", string.IsNullOrWhiteSpace(action) ? ""None"" : action);
        }
    }
}
";
        private const string ContextMenuXamlSource = @"<UserControl
    x:Class=""Fluence.Wpf.Demo.Pages.Menus.ContextMenuActions""
    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
    xmlns:ui=""clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf""
    xmlns:uicore=""clr-namespace:Fluence.Wpf;assembly=Fluence.Wpf"">
    <ui:Card Padding=""16"" Variant=""{x:Static uicore:CardVariant.Subtle}"">
        <ui:Card.ContextMenu>
            <ui:ContextMenu>
                <ui:MenuItem
                    Click=""ContextMenu_Click""
                    Header=""Cut""
                    InputGestureText=""Ctrl+X""
                    Tag=""Cut"">
                    <ui:MenuItem.Icon>
                        <ui:FontIcon Glyph=""&#xE8C6;"" IconFontSize=""16"" />
                    </ui:MenuItem.Icon>
                </ui:MenuItem>
                <ui:MenuItem
                    Click=""ContextMenu_Click""
                    Header=""Copy""
                    InputGestureText=""Ctrl+C""
                    Tag=""Copy"">
                    <ui:MenuItem.Icon>
                        <ui:FontIcon Glyph=""&#xE8C8;"" IconFontSize=""16"" />
                    </ui:MenuItem.Icon>
                </ui:MenuItem>
                <ui:MenuItem
                    Click=""ContextMenu_Click""
                    Header=""Paste""
                    InputGestureText=""Ctrl+V""
                    Tag=""Paste"">
                    <ui:MenuItem.Icon>
                        <ui:FontIcon Glyph=""&#xE77F;"" IconFontSize=""16"" />
                    </ui:MenuItem.Icon>
                </ui:MenuItem>
                <Separator />
                <ui:MenuItem
                    Click=""ContextMenu_Click""
                    Header=""Add to favorites""
                    IsCheckable=""True""
                    Tag=""Add to favorites"" />
                <ui:MenuItem Header=""Share"">
                    <ui:MenuItem Click=""ContextMenu_Click"" Header=""Copy link"" Tag=""Share - Copy link"" />
                    <ui:MenuItem Click=""ContextMenu_Click"" Header=""Send email"" Tag=""Share - Send email"" />
                    <ui:MenuItem Header=""Export PDF"" IsEnabled=""False"" />
                </ui:MenuItem>
            </ui:ContextMenu>
        </ui:Card.ContextMenu>
        <StackPanel>
            <TextBlock
                Foreground=""{DynamicResource TextFillColorPrimaryBrush}""
                Text=""Right-click this note"" />
            <TextBlock
                x:Name=""ContextMenuResultLabel""
                Margin=""0,8,0,0""
                Foreground=""{DynamicResource TextFillColorSecondaryBrush}""
                Text=""Last action: None"" />
        </StackPanel>
    </ui:Card>
</UserControl>
";

        private const string ContextMenuCSharpSource = @"using System.Windows;
using System.Windows.Controls;

namespace Fluence.Wpf.Demo.Pages.Menus
{
    public partial class ContextMenuActions : UserControl
    {
        public ContextMenuActions()
        {
            InitializeComponent();
        }

        private void ContextMenu_Click(object sender, RoutedEventArgs e)
        {
            string action = sender is FrameworkElement element && element.Tag is string tag ? tag : string.Empty;
            ContextMenuResultLabel.Text = string.Format(""Last action: {0}"", string.IsNullOrWhiteSpace(action) ? ""None"" : action);
        }
    }
}
";
        private const string ToolTipsXamlSource = @"<UserControl
    x:Class=""Fluence.Wpf.Demo.Pages.Menus.ToolTips""
    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
    xmlns:ui=""clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf"">
    <WrapPanel>
        <ui:Button Margin=""0,0,8,8"" Content=""Save"">
            <ui:Button.ToolTip>
                <ui:ToolTip Content=""Save changes (Ctrl+S)"" />
            </ui:Button.ToolTip>
        </ui:Button>
        <ui:Button Margin=""0,0,8,8"" Content=""Delete"">
            <ui:Button.ToolTip>
                <ui:ToolTip Content=""Delete the selected item"" />
            </ui:Button.ToolTip>
        </ui:Button>
        <ui:Button Margin=""0,0,8,8"" Content=""Share"">
            <ui:Button.ToolTip>
                <ui:ToolTip>
                    <StackPanel>
                        <TextBlock
                            FontWeight=""SemiBold""
                            Foreground=""{DynamicResource TextFillColorPrimaryBrush}""
                            Text=""Share"" />
                        <TextBlock
                            Foreground=""{DynamicResource TextFillColorSecondaryBrush}""
                            Text=""Copy a link or send an email."" />
                    </StackPanel>
                </ui:ToolTip>
            </ui:Button.ToolTip>
        </ui:Button>
        <ui:Button
            Margin=""0,0,8,8""
            Content=""Settings""
            IsEnabled=""False""
            ToolTipService.ShowOnDisabled=""True"">
            <ui:Button.ToolTip>
                <ui:ToolTip Content=""Settings are disabled for this item"" />
            </ui:Button.ToolTip>
        </ui:Button>
    </WrapPanel>
</UserControl>
";

        private const string ToolTipsCSharpSource = @"using System.Windows.Controls;

namespace Fluence.Wpf.Demo.Pages.Menus
{
    public partial class ToolTips : UserControl
    {
        public ToolTips()
        {
            InitializeComponent();
        }
    }
}
";

        private const string FlyoutXamlSource = @"<UserControl
    x:Class=""Fluence.Wpf.Demo.Pages.Menus.FlyoutSample""
    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
    xmlns:ui=""clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf"">
    <ui:Button Click=""FlyoutButton_Click"" Content=""Show flyout"">
        <ui:FlyoutBase.AttachedFlyout>
            <ui:Flyout Placement=""Bottom"">
                <ui:Flyout.Content>
                    <StackPanel MaxWidth=""260"">
                        <TextBlock
                            FontWeight=""SemiBold""
                            Foreground=""{DynamicResource TextFillColorPrimaryBrush}""
                            Text=""Quick note"" />
                        <TextBlock
                            Margin=""0,4,0,0""
                            Foreground=""{DynamicResource TextFillColorSecondaryBrush}""
                            Text=""A lightweight, light-dismiss popup anchored to its owner.""
                            TextWrapping=""Wrap"" />
                    </StackPanel>
                </ui:Flyout.Content>
            </ui:Flyout>
        </ui:FlyoutBase.AttachedFlyout>
    </ui:Button>
</UserControl>
";

        private const string FlyoutCSharpSource = @"using System.Windows;
using System.Windows.Controls;
using Fluence.Wpf.Controls;

namespace Fluence.Wpf.Demo.Pages.Menus
{
    public partial class FlyoutSample : UserControl
    {
        public FlyoutSample()
        {
            InitializeComponent();
        }

        private void FlyoutButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                FlyoutBase.ShowAttachedFlyout(element);
            }
        }
    }
}
";

        private const string ContentDialogXamlSource = @"<UserControl
    x:Class=""Fluence.Wpf.Demo.Pages.Menus.ContentDialogSample""
    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
    xmlns:ui=""clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf"">
    <ui:Button Click=""ShowDialogButton_Click"" Content=""Show dialog"" />
</UserControl>
";

        private const string ContentDialogCSharpSource = @"using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Fluence.Wpf;
using Fluence.Wpf.Controls;

namespace Fluence.Wpf.Demo.Pages.Menus
{
    public partial class ContentDialogSample : UserControl
    {
        public ContentDialogSample()
        {
            InitializeComponent();
        }

        private void ShowDialogButton_Click(object sender, RoutedEventArgs e)
        {
            _ = ShowDialogAsync();
        }

        private async Task ShowDialogAsync()
        {
            ContentDialog dialog = new()
            {
                Title = ""Delete file?"",
                Content = ""Roadmap.md will be permanently deleted. This cannot be undone."",
                PrimaryButtonText = ""Delete"",
                CloseButtonText = ""Cancel"",
                DefaultButton = ContentDialogButton.Close
            };

            ContentDialogResult result = await dialog.ShowAsync();
        }
    }
}
";

        private const string TeachingTipXamlSource = @"<UserControl
    x:Class=""Fluence.Wpf.Demo.Pages.Menus.TeachingTipSample""
    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
    xmlns:ui=""clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf"">
    <Grid>
        <ui:Button x:Name=""TipButton"" Click=""ShowTipButton_Click"" Content=""Show teaching tip"" />
        <ui:TeachingTip
            x:Name=""Tip""
            Title=""Pro tip""
            Subtitle=""A TeachingTip coaches the user from a target element without blocking.""
            CloseButtonContent=""Got it""
            IsLightDismissEnabled=""True""
            PreferredPlacement=""Bottom"" />
    </Grid>
</UserControl>
";

        private const string TeachingTipCSharpSource = @"using System.Windows;
using System.Windows.Controls;

namespace Fluence.Wpf.Demo.Pages.Menus
{
    public partial class TeachingTipSample : UserControl
    {
        public TeachingTipSample()
        {
            InitializeComponent();
        }

        private void ShowTipButton_Click(object sender, RoutedEventArgs e)
        {
            Tip.Target = TipButton;
            Tip.IsOpen = true;
        }
    }
}
";

        public GalleryMenusPage()
        {
            InitializeComponent();

            DemoSamplePageWiring.Apply(
                (DependencyObject)Content,
                new DemoSampleSource(1, MenuBarXamlSource, MenuBarCSharpSource),
                new DemoSampleSource(2, ContextMenuXamlSource, ContextMenuCSharpSource),
                new DemoSampleSource(3, ToolTipsXamlSource, ToolTipsCSharpSource),
                new DemoSampleSource(4, FlyoutXamlSource, FlyoutCSharpSource),
                new DemoSampleSource(5, ContentDialogXamlSource, ContentDialogCSharpSource),
                new DemoSampleSource(6, TeachingTipXamlSource, TeachingTipCSharpSource),
                new DemoSampleSource(7, CommandBarFlyoutXamlSource, CommandBarFlyoutCSharpSource));
        }

        private void MenuBar_Click(object sender, RoutedEventArgs e)
        {
            SetTextFromTag(MenuBarResultLabel, "Last menu action", sender);
        }

        private void ContextMenu_Click(object sender, RoutedEventArgs e)
        {
            SetTextFromTag(ContextMenuResultLabel, "Last action", sender);
        }

        private void FlyoutButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                Fluence.Wpf.Controls.FlyoutBase.ShowAttachedFlyout(element);
            }
        }

        private void ShowDialogButton_Click(object sender, RoutedEventArgs e)
        {
            _ = ShowDialogAsync();
        }

        private async Task ShowDialogAsync()
        {
            Fluence.Wpf.Controls.ContentDialog dialog = new()
            {
                Title = "Delete file?",
                Content = "Roadmap.md will be permanently deleted. This cannot be undone.",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
                DefaultButton = Fluence.Wpf.ContentDialogButton.Close
            };

            Fluence.Wpf.ContentDialogResult result = await dialog.ShowAsync();
            DialogResultLabel.Text = string.Format(CultureInfo.CurrentCulture, "Dialog result: {0}", result);
        }

        private const string CommandBarFlyoutXamlSource = @"<UserControl
    x:Class=""Fluence.Wpf.Demo.Pages.Menus.CommandBarSample""
    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
    xmlns:ui=""clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf"">
    <ui:Button Click=""ShowCommandBarButton_Click"" Content=""Show command bar"">
        <ui:FlyoutBase.AttachedFlyout>
            <ui:CommandBarFlyout>
                <ui:CommandBarFlyout.PrimaryCommands>
                    <ui:AppBarButton Click=""Command_Click"" Label=""Copy"" Tag=""Copy"">
                        <ui:AppBarButton.Icon>
                            <ui:FontIcon Glyph=""&#xE8C8;"" IconFontSize=""16"" />
                        </ui:AppBarButton.Icon>
                    </ui:AppBarButton>
                </ui:CommandBarFlyout.PrimaryCommands>
                <ui:CommandBarFlyout.SecondaryCommands>
                    <ui:AppBarButton Click=""Command_Click"" Label=""Delete"" Tag=""Delete"" />
                </ui:CommandBarFlyout.SecondaryCommands>
            </ui:CommandBarFlyout>
        </ui:FlyoutBase.AttachedFlyout>
    </ui:Button>
</UserControl>
";

        private const string CommandBarFlyoutCSharpSource = @"using System.Windows;
using System.Windows.Controls;
using Fluence.Wpf.Controls;

namespace Fluence.Wpf.Demo.Pages.Menus
{
    public partial class CommandBarSample : UserControl
    {
        public CommandBarSample()
        {
            InitializeComponent();
        }

        private void ShowCommandBarButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                FlyoutBase.ShowAttachedFlyout(element);
            }
        }

        private void Command_Click(object sender, RoutedEventArgs e)
        {
            // Invoked commands dismiss the flyout automatically.
        }
    }
}
";

        private void CommandBarAction_Click(object sender, RoutedEventArgs e)
        {
            SetTextFromTag(CommandBarResultLabel, "Last command", sender);
        }

        private void ShowTeachingTipButton_Click(object sender, RoutedEventArgs e)
        {
            DemoTeachingTip.Target = TeachingTipButton;
            DemoTeachingTip.IsOpen = true;
        }

        private static void SetTextFromTag(TextBlock label, string prefix, object sender)
        {
            string? action = sender is FrameworkElement element ? element.Tag as string : null;
            label.Text = string.Format(CultureInfo.CurrentCulture, "{0}: {1}", prefix, string.IsNullOrWhiteSpace(action) ? "None" : action);
        }
    }
}
