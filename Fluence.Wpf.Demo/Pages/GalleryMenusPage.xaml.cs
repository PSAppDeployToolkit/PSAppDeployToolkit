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
        private const string MenuBarXamlSource = "<UserControl\n" +
                                                 "    x:Class=\"Fluence.Wpf.Demo.Pages.Menus.MenuBar\"\n" +
                                                 "    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"\n" +
                                                 "    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"\n" +
                                                 "    xmlns:ui=\"clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf\">\n" +
                                                 "    <StackPanel>\n" +
                                                 "        <ui:Menu\n" +
                                                 "            Margin=\"0,0,0,12\">\n" +
                                                 "            <ui:MenuItem Header=\"_File\">\n" +
                                                 "                <ui:MenuItem\n" +
                                                 "                    Click=\"MenuBar_Click\"\n" +
                                                 "                    Header=\"_New\"\n" +
                                                 "                    InputGestureText=\"Ctrl+N\"\n" +
                                                 "                    Tag=\"File - New\" />\n" +
                                                 "                <ui:MenuItem\n" +
                                                 "                    Click=\"MenuBar_Click\"\n" +
                                                 "                    Header=\"_Open...\"\n" +
                                                 "                    InputGestureText=\"Ctrl+O\"\n" +
                                                 "                    Tag=\"File - Open\" />\n" +
                                                 "                <ui:MenuItem Header=\"Open _Recent\">\n" +
                                                 "                    <ui:MenuItem\n" +
                                                 "                        Click=\"MenuBar_Click\"\n" +
                                                 "                        Header=\"Roadmap.md\"\n" +
                                                 "                        Tag=\"File - Recent - Roadmap.md\" />\n" +
                                                 "                    <ui:MenuItem\n" +
                                                 "                        Click=\"MenuBar_Click\"\n" +
                                                 "                        Header=\"LaunchPlan.xlsx\"\n" +
                                                 "                        Tag=\"File - Recent - LaunchPlan.xlsx\" />\n" +
                                                 "                </ui:MenuItem>\n" +
                                                 "                <Separator />\n" +
                                                 "                <ui:MenuItem\n" +
                                                 "                    Click=\"MenuBar_Click\"\n" +
                                                 "                    Header=\"_Save\"\n" +
                                                 "                    InputGestureText=\"Ctrl+S\"\n" +
                                                 "                    Tag=\"File - Save\" />\n" +
                                                 "                <ui:MenuItem Header=\"Print\" IsEnabled=\"False\" />\n" +
                                                 "            </ui:MenuItem>\n" +
                                                 "            <ui:MenuItem Header=\"_View\">\n" +
                                                 "                <ui:MenuItem\n" +
                                                 "                    Click=\"MenuBar_Click\"\n" +
                                                 "                    Header=\"Status bar\"\n" +
                                                 "                    IsCheckable=\"True\"\n" +
                                                 "                    IsChecked=\"True\"\n" +
                                                 "                    Tag=\"View - Status bar\" />\n" +
                                                 "                <ui:MenuItem\n" +
                                                 "                    Click=\"MenuBar_Click\"\n" +
                                                 "                    Header=\"Word wrap\"\n" +
                                                 "                    IsCheckable=\"True\"\n" +
                                                 "                    Tag=\"View - Word wrap\" />\n" +
                                                 "                <Separator />\n" +
                                                 "                <ui:MenuItem\n" +
                                                 "                    Click=\"MenuBar_Click\"\n" +
                                                 "                    Header=\"Zoom in\"\n" +
                                                 "                    InputGestureText=\"Ctrl++\"\n" +
                                                 "                    Tag=\"View - Zoom in\" />\n" +
                                                 "            </ui:MenuItem>\n" +
                                                 "            <ui:MenuItem Header=\"_Help\">\n" +
                                                 "                <ui:MenuItem\n" +
                                                 "                    Click=\"MenuBar_Click\"\n" +
                                                 "                    Header=\"Documentation\"\n" +
                                                 "                    Tag=\"Help - Documentation\" />\n" +
                                                 "                <ui:MenuItem Header=\"About\" IsEnabled=\"False\" />\n" +
                                                 "            </ui:MenuItem>\n" +
                                                 "        </ui:Menu>\n" +
                                                 "        <TextBlock\n" +
                                                 "            x:Name=\"MenuBarResultLabel\"\n" +
                                                 "            Foreground=\"{DynamicResource TextFillColorSecondaryBrush}\"\n" +
                                                 "            Text=\"Last menu action: None\" />\n" +
                                                 "    </StackPanel>\n" +
                                                 "</UserControl>\n";

        private const string MenuBarCSharpSource = "using System.Windows;\n" +
                                                   "using System.Windows.Controls;\n" +
                                                   "\n" +
                                                   "namespace Fluence.Wpf.Demo.Pages.Menus\n" +
                                                   "{\n" +
                                                   "    public partial class MenuBar : UserControl\n" +
                                                   "    {\n" +
                                                   "        public MenuBar()\n" +
                                                   "        {\n" +
                                                   "            InitializeComponent();\n" +
                                                   "        }\n" +
                                                   "\n" +
                                                   "        private void MenuBar_Click(object sender, RoutedEventArgs e)\n" +
                                                   "        {\n" +
                                                   "            string action = sender is FrameworkElement element && element.Tag is string tag ? tag : string.Empty;\n" +
                                                   "            MenuBarResultLabel.Text = string.Format(\"Last menu action: {0}\", string.IsNullOrWhiteSpace(action) ? \"None\" : action);\n" +
                                                   "        }\n" +
                                                   "    }\n" +
                                                   "}\n";
        private const string ContextMenuXamlSource = "<UserControl\n" +
                                                     "    x:Class=\"Fluence.Wpf.Demo.Pages.Menus.ContextMenuActions\"\n" +
                                                     "    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"\n" +
                                                     "    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"\n" +
                                                     "    xmlns:ui=\"clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf\"\n" +
                                                     "    xmlns:uicore=\"clr-namespace:Fluence.Wpf;assembly=Fluence.Wpf\">\n" +
                                                     "    <ui:Card Padding=\"16\" Variant=\"{x:Static uicore:CardVariant.Subtle}\">\n" +
                                                     "        <ui:Card.ContextMenu>\n" +
                                                     "            <ui:ContextMenu>\n" +
                                                     "                <ui:MenuItem\n" +
                                                     "                    Click=\"ContextMenu_Click\"\n" +
                                                     "                    Header=\"Cut\"\n" +
                                                     "                    InputGestureText=\"Ctrl+X\"\n" +
                                                     "                    Tag=\"Cut\">\n" +
                                                     "                    <ui:MenuItem.Icon>\n" +
                                                     "                        <ui:FontIcon Glyph=\"&#xE8C6;\" IconFontSize=\"16\" />\n" +
                                                     "                    </ui:MenuItem.Icon>\n" +
                                                     "                </ui:MenuItem>\n" +
                                                     "                <ui:MenuItem\n" +
                                                     "                    Click=\"ContextMenu_Click\"\n" +
                                                     "                    Header=\"Copy\"\n" +
                                                     "                    InputGestureText=\"Ctrl+C\"\n" +
                                                     "                    Tag=\"Copy\">\n" +
                                                     "                    <ui:MenuItem.Icon>\n" +
                                                     "                        <ui:FontIcon Glyph=\"&#xE8C8;\" IconFontSize=\"16\" />\n" +
                                                     "                    </ui:MenuItem.Icon>\n" +
                                                     "                </ui:MenuItem>\n" +
                                                     "                <ui:MenuItem\n" +
                                                     "                    Click=\"ContextMenu_Click\"\n" +
                                                     "                    Header=\"Paste\"\n" +
                                                     "                    InputGestureText=\"Ctrl+V\"\n" +
                                                     "                    Tag=\"Paste\">\n" +
                                                     "                    <ui:MenuItem.Icon>\n" +
                                                     "                        <ui:FontIcon Glyph=\"&#xE77F;\" IconFontSize=\"16\" />\n" +
                                                     "                    </ui:MenuItem.Icon>\n" +
                                                     "                </ui:MenuItem>\n" +
                                                     "                <Separator />\n" +
                                                     "                <ui:MenuItem\n" +
                                                     "                    Click=\"ContextMenu_Click\"\n" +
                                                     "                    Header=\"Add to favorites\"\n" +
                                                     "                    IsCheckable=\"True\"\n" +
                                                     "                    Tag=\"Add to favorites\" />\n" +
                                                     "                <ui:MenuItem Header=\"Share\">\n" +
                                                     "                    <ui:MenuItem Click=\"ContextMenu_Click\" Header=\"Copy link\" Tag=\"Share - Copy link\" />\n" +
                                                     "                    <ui:MenuItem Click=\"ContextMenu_Click\" Header=\"Send email\" Tag=\"Share - Send email\" />\n" +
                                                     "                    <ui:MenuItem Header=\"Export PDF\" IsEnabled=\"False\" />\n" +
                                                     "                </ui:MenuItem>\n" +
                                                     "            </ui:ContextMenu>\n" +
                                                     "        </ui:Card.ContextMenu>\n" +
                                                     "        <StackPanel>\n" +
                                                     "            <TextBlock\n" +
                                                     "                Foreground=\"{DynamicResource TextFillColorPrimaryBrush}\"\n" +
                                                     "                Text=\"Right-click this note\" />\n" +
                                                     "            <TextBlock\n" +
                                                     "                x:Name=\"ContextMenuResultLabel\"\n" +
                                                     "                Margin=\"0,8,0,0\"\n" +
                                                     "                Foreground=\"{DynamicResource TextFillColorSecondaryBrush}\"\n" +
                                                     "                Text=\"Last action: None\" />\n" +
                                                     "        </StackPanel>\n" +
                                                     "    </ui:Card>\n" +
                                                     "</UserControl>\n";

        private const string ContextMenuCSharpSource = "using System.Windows;\n" +
                                                       "using System.Windows.Controls;\n" +
                                                       "\n" +
                                                       "namespace Fluence.Wpf.Demo.Pages.Menus\n" +
                                                       "{\n" +
                                                       "    public partial class ContextMenuActions : UserControl\n" +
                                                       "    {\n" +
                                                       "        public ContextMenuActions()\n" +
                                                       "        {\n" +
                                                       "            InitializeComponent();\n" +
                                                       "        }\n" +
                                                       "\n" +
                                                       "        private void ContextMenu_Click(object sender, RoutedEventArgs e)\n" +
                                                       "        {\n" +
                                                       "            string action = sender is FrameworkElement element && element.Tag is string tag ? tag : string.Empty;\n" +
                                                       "            ContextMenuResultLabel.Text = string.Format(\"Last action: {0}\", string.IsNullOrWhiteSpace(action) ? \"None\" : action);\n" +
                                                       "        }\n" +
                                                       "    }\n" +
                                                       "}\n";
        private const string ToolTipsXamlSource = "<UserControl\n" +
                                                  "    x:Class=\"Fluence.Wpf.Demo.Pages.Menus.ToolTips\"\n" +
                                                  "    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"\n" +
                                                  "    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"\n" +
                                                  "    xmlns:ui=\"clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf\">\n" +
                                                  "    <WrapPanel>\n" +
                                                  "        <ui:Button Margin=\"0,0,8,8\" Content=\"Save\">\n" +
                                                  "            <ui:Button.ToolTip>\n" +
                                                  "                <ui:ToolTip Content=\"Save changes (Ctrl+S)\" />\n" +
                                                  "            </ui:Button.ToolTip>\n" +
                                                  "        </ui:Button>\n" +
                                                  "        <ui:Button Margin=\"0,0,8,8\" Content=\"Delete\">\n" +
                                                  "            <ui:Button.ToolTip>\n" +
                                                  "                <ui:ToolTip Content=\"Delete the selected item\" />\n" +
                                                  "            </ui:Button.ToolTip>\n" +
                                                  "        </ui:Button>\n" +
                                                  "        <ui:Button Margin=\"0,0,8,8\" Content=\"Share\">\n" +
                                                  "            <ui:Button.ToolTip>\n" +
                                                  "                <ui:ToolTip>\n" +
                                                  "                    <StackPanel>\n" +
                                                  "                        <TextBlock\n" +
                                                  "                            FontWeight=\"SemiBold\"\n" +
                                                  "                            Foreground=\"{DynamicResource TextFillColorPrimaryBrush}\"\n" +
                                                  "                            Text=\"Share\" />\n" +
                                                  "                        <TextBlock\n" +
                                                  "                            Foreground=\"{DynamicResource TextFillColorSecondaryBrush}\"\n" +
                                                  "                            Text=\"Copy a link or send an email.\" />\n" +
                                                  "                    </StackPanel>\n" +
                                                  "                </ui:ToolTip>\n" +
                                                  "            </ui:Button.ToolTip>\n" +
                                                  "        </ui:Button>\n" +
                                                  "        <ui:Button\n" +
                                                  "            Margin=\"0,0,8,8\"\n" +
                                                  "            Content=\"Settings\"\n" +
                                                  "            IsEnabled=\"False\"\n" +
                                                  "            ToolTipService.ShowOnDisabled=\"True\">\n" +
                                                  "            <ui:Button.ToolTip>\n" +
                                                  "                <ui:ToolTip Content=\"Settings are disabled for this item\" />\n" +
                                                  "            </ui:Button.ToolTip>\n" +
                                                  "        </ui:Button>\n" +
                                                  "    </WrapPanel>\n" +
                                                  "</UserControl>\n";

        private const string ToolTipsCSharpSource = "using System.Windows.Controls;\n" +
                                                    "\n" +
                                                    "namespace Fluence.Wpf.Demo.Pages.Menus\n" +
                                                    "{\n" +
                                                    "    public partial class ToolTips : UserControl\n" +
                                                    "    {\n" +
                                                    "        public ToolTips()\n" +
                                                    "        {\n" +
                                                    "            InitializeComponent();\n" +
                                                    "        }\n" +
                                                    "    }\n" +
                                                    "}\n";

        private const string FlyoutXamlSource = "<UserControl\n" +
                                                "    x:Class=\"Fluence.Wpf.Demo.Pages.Menus.FlyoutSample\"\n" +
                                                "    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"\n" +
                                                "    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"\n" +
                                                "    xmlns:ui=\"clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf\">\n" +
                                                "    <ui:Button Click=\"FlyoutButton_Click\" Content=\"Show flyout\">\n" +
                                                "        <ui:FlyoutBase.AttachedFlyout>\n" +
                                                "            <ui:Flyout Placement=\"Bottom\">\n" +
                                                "                <ui:Flyout.Content>\n" +
                                                "                    <StackPanel MaxWidth=\"260\">\n" +
                                                "                        <TextBlock\n" +
                                                "                            FontWeight=\"SemiBold\"\n" +
                                                "                            Foreground=\"{DynamicResource TextFillColorPrimaryBrush}\"\n" +
                                                "                            Text=\"Quick note\" />\n" +
                                                "                        <TextBlock\n" +
                                                "                            Margin=\"0,4,0,0\"\n" +
                                                "                            Foreground=\"{DynamicResource TextFillColorSecondaryBrush}\"\n" +
                                                "                            Text=\"A lightweight, light-dismiss popup anchored to its owner.\"\n" +
                                                "                            TextWrapping=\"Wrap\" />\n" +
                                                "                    </StackPanel>\n" +
                                                "                </ui:Flyout.Content>\n" +
                                                "            </ui:Flyout>\n" +
                                                "        </ui:FlyoutBase.AttachedFlyout>\n" +
                                                "    </ui:Button>\n" +
                                                "</UserControl>\n";

        private const string FlyoutCSharpSource = "using System.Windows;\n" +
                                                  "using System.Windows.Controls;\n" +
                                                  "using Fluence.Wpf.Controls;\n" +
                                                  "\n" +
                                                  "namespace Fluence.Wpf.Demo.Pages.Menus\n" +
                                                  "{\n" +
                                                  "    public partial class FlyoutSample : UserControl\n" +
                                                  "    {\n" +
                                                  "        public FlyoutSample()\n" +
                                                  "        {\n" +
                                                  "            InitializeComponent();\n" +
                                                  "        }\n" +
                                                  "\n" +
                                                  "        private void FlyoutButton_Click(object sender, RoutedEventArgs e)\n" +
                                                  "        {\n" +
                                                  "            if (sender is FrameworkElement element)\n" +
                                                  "            {\n" +
                                                  "                FlyoutBase.ShowAttachedFlyout(element);\n" +
                                                  "            }\n" +
                                                  "        }\n" +
                                                  "    }\n" +
                                                  "}\n";

        private const string ContentDialogXamlSource = "<UserControl\n" +
                                                       "    x:Class=\"Fluence.Wpf.Demo.Pages.Menus.ContentDialogSample\"\n" +
                                                       "    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"\n" +
                                                       "    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"\n" +
                                                       "    xmlns:ui=\"clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf\">\n" +
                                                       "    <ui:Button Click=\"ShowDialogButton_Click\" Content=\"Show dialog\" />\n" +
                                                       "</UserControl>\n";

        private const string ContentDialogCSharpSource = "using System.Threading.Tasks;\n" +
                                                         "using System.Windows;\n" +
                                                         "using System.Windows.Controls;\n" +
                                                         "using Fluence.Wpf;\n" +
                                                         "using Fluence.Wpf.Controls;\n" +
                                                         "\n" +
                                                         "namespace Fluence.Wpf.Demo.Pages.Menus\n" +
                                                         "{\n" +
                                                         "    public partial class ContentDialogSample : UserControl\n" +
                                                         "    {\n" +
                                                         "        public ContentDialogSample()\n" +
                                                         "        {\n" +
                                                         "            InitializeComponent();\n" +
                                                         "        }\n" +
                                                         "\n" +
                                                         "        private void ShowDialogButton_Click(object sender, RoutedEventArgs e)\n" +
                                                         "        {\n" +
                                                         "            _ = ShowDialogAsync();\n" +
                                                         "        }\n" +
                                                         "\n" +
                                                         "        private async Task ShowDialogAsync()\n" +
                                                         "        {\n" +
                                                         "            ContentDialog dialog = new()\n" +
                                                         "            {\n" +
                                                         "                Title = \"Delete file?\",\n" +
                                                         "                Content = \"Roadmap.md will be permanently deleted. This cannot be undone.\",\n" +
                                                         "                PrimaryButtonText = \"Delete\",\n" +
                                                         "                CloseButtonText = \"Cancel\",\n" +
                                                         "                DefaultButton = ContentDialogButton.Close\n" +
                                                         "            };\n" +
                                                         "\n" +
                                                         "            ContentDialogResult result = await dialog.ShowAsync();\n" +
                                                         "        }\n" +
                                                         "    }\n" +
                                                         "}\n";

        private const string TeachingTipXamlSource = "<UserControl\n" +
                                                     "    x:Class=\"Fluence.Wpf.Demo.Pages.Menus.TeachingTipSample\"\n" +
                                                     "    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"\n" +
                                                     "    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"\n" +
                                                     "    xmlns:ui=\"clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf\">\n" +
                                                     "    <Grid>\n" +
                                                     "        <ui:Button x:Name=\"TipButton\" Click=\"ShowTipButton_Click\" Content=\"Show teaching tip\" />\n" +
                                                     "        <ui:TeachingTip\n" +
                                                     "            x:Name=\"Tip\"\n" +
                                                     "            Title=\"Pro tip\"\n" +
                                                     "            Subtitle=\"A TeachingTip coaches the user from a target element without blocking.\"\n" +
                                                     "            CloseButtonContent=\"Got it\"\n" +
                                                     "            IsLightDismissEnabled=\"True\"\n" +
                                                     "            PreferredPlacement=\"Bottom\" />\n" +
                                                     "    </Grid>\n" +
                                                     "</UserControl>\n";

        private const string TeachingTipCSharpSource = "using System.Windows;\n" +
                                                       "using System.Windows.Controls;\n" +
                                                       "\n" +
                                                       "namespace Fluence.Wpf.Demo.Pages.Menus\n" +
                                                       "{\n" +
                                                       "    public partial class TeachingTipSample : UserControl\n" +
                                                       "    {\n" +
                                                       "        public TeachingTipSample()\n" +
                                                       "        {\n" +
                                                       "            InitializeComponent();\n" +
                                                       "        }\n" +
                                                       "\n" +
                                                       "        private void ShowTipButton_Click(object sender, RoutedEventArgs e)\n" +
                                                       "        {\n" +
                                                       "            Tip.Target = TipButton;\n" +
                                                       "            Tip.IsOpen = true;\n" +
                                                       "        }\n" +
                                                       "    }\n" +
                                                       "}\n";

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
                DefaultButton = Fluence.Wpf.ContentDialogButton.Close,
            };

            Fluence.Wpf.ContentDialogResult result = await dialog.ShowAsync();
            DialogResultLabel.Text = string.Format(CultureInfo.CurrentCulture, "Dialog result: {0}", result);
        }

        private const string CommandBarFlyoutXamlSource = "<UserControl\n" +
                                                          "    x:Class=\"Fluence.Wpf.Demo.Pages.Menus.CommandBarSample\"\n" +
                                                          "    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"\n" +
                                                          "    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"\n" +
                                                          "    xmlns:ui=\"clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf\">\n" +
                                                          "    <ui:Button Click=\"ShowCommandBarButton_Click\" Content=\"Show command bar\">\n" +
                                                          "        <ui:FlyoutBase.AttachedFlyout>\n" +
                                                          "            <ui:CommandBarFlyout>\n" +
                                                          "                <ui:CommandBarFlyout.PrimaryCommands>\n" +
                                                          "                    <ui:AppBarButton Click=\"Command_Click\" Label=\"Copy\" Tag=\"Copy\">\n" +
                                                          "                        <ui:AppBarButton.Icon>\n" +
                                                          "                            <ui:FontIcon Glyph=\"&#xE8C8;\" IconFontSize=\"16\" />\n" +
                                                          "                        </ui:AppBarButton.Icon>\n" +
                                                          "                    </ui:AppBarButton>\n" +
                                                          "                </ui:CommandBarFlyout.PrimaryCommands>\n" +
                                                          "                <ui:CommandBarFlyout.SecondaryCommands>\n" +
                                                          "                    <ui:AppBarButton Click=\"Command_Click\" Label=\"Delete\" Tag=\"Delete\" />\n" +
                                                          "                </ui:CommandBarFlyout.SecondaryCommands>\n" +
                                                          "            </ui:CommandBarFlyout>\n" +
                                                          "        </ui:FlyoutBase.AttachedFlyout>\n" +
                                                          "    </ui:Button>\n" +
                                                          "</UserControl>\n";

        private const string CommandBarFlyoutCSharpSource = "using System.Windows;\n" +
                                                            "using System.Windows.Controls;\n" +
                                                            "using Fluence.Wpf.Controls;\n" +
                                                            "\n" +
                                                            "namespace Fluence.Wpf.Demo.Pages.Menus\n" +
                                                            "{\n" +
                                                            "    public partial class CommandBarSample : UserControl\n" +
                                                            "    {\n" +
                                                            "        public CommandBarSample()\n" +
                                                            "        {\n" +
                                                            "            InitializeComponent();\n" +
                                                            "        }\n" +
                                                            "\n" +
                                                            "        private void ShowCommandBarButton_Click(object sender, RoutedEventArgs e)\n" +
                                                            "        {\n" +
                                                            "            if (sender is FrameworkElement element)\n" +
                                                            "            {\n" +
                                                            "                FlyoutBase.ShowAttachedFlyout(element);\n" +
                                                            "            }\n" +
                                                            "        }\n" +
                                                            "\n" +
                                                            "        private void Command_Click(object sender, RoutedEventArgs e)\n" +
                                                            "        {\n" +
                                                            "            // Invoked commands dismiss the flyout automatically.\n" +
                                                            "        }\n" +
                                                            "    }\n" +
                                                            "}\n";

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
