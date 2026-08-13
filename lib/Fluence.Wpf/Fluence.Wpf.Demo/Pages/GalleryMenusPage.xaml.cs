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
        private static readonly string MenuBarXamlSource = DemoSampleXaml.UserControl(
            "Fluence.Wpf.Demo.Pages.Menus.MenuBar",
                                                 "    <StackPanel>\n" +
                                                 "        <fluence:Menu\n" +
                                                 "            Margin=\"0,0,0,12\">\n" +
                                                 "            <fluence:MenuItem Header=\"_File\">\n" +
                                                 "                <fluence:MenuItem\n" +
                                                 "                    Click=\"MenuBar_Click\"\n" +
                                                 "                    Header=\"_New\"\n" +
                                                 "                    InputGestureText=\"Ctrl+N\"\n" +
                                                 "                    Tag=\"File - New\" />\n" +
                                                 "                <fluence:MenuItem\n" +
                                                 "                    Click=\"MenuBar_Click\"\n" +
                                                 "                    Header=\"_Open...\"\n" +
                                                 "                    InputGestureText=\"Ctrl+O\"\n" +
                                                 "                    Tag=\"File - Open\" />\n" +
                                                 "                <fluence:MenuItem Header=\"Open _Recent\">\n" +
                                                 "                    <fluence:MenuItem\n" +
                                                 "                        Click=\"MenuBar_Click\"\n" +
                                                 "                        Header=\"Roadmap.md\"\n" +
                                                 "                        Tag=\"File - Recent - Roadmap.md\" />\n" +
                                                 "                    <fluence:MenuItem\n" +
                                                 "                        Click=\"MenuBar_Click\"\n" +
                                                 "                        Header=\"LaunchPlan.xlsx\"\n" +
                                                 "                        Tag=\"File - Recent - LaunchPlan.xlsx\" />\n" +
                                                 "                </fluence:MenuItem>\n" +
                                                 "                <Separator />\n" +
                                                 "                <fluence:MenuItem\n" +
                                                 "                    Click=\"MenuBar_Click\"\n" +
                                                 "                    Header=\"_Save\"\n" +
                                                 "                    InputGestureText=\"Ctrl+S\"\n" +
                                                 "                    Tag=\"File - Save\" />\n" +
                                                 "                <fluence:MenuItem Header=\"Print\" IsEnabled=\"False\" />\n" +
                                                 "            </fluence:MenuItem>\n" +
                                                 "            <fluence:MenuItem Header=\"_View\">\n" +
                                                 "                <fluence:MenuItem\n" +
                                                 "                    Click=\"MenuBar_Click\"\n" +
                                                 "                    Header=\"Status bar\"\n" +
                                                 "                    IsCheckable=\"True\"\n" +
                                                 "                    IsChecked=\"True\"\n" +
                                                 "                    Tag=\"View - Status bar\" />\n" +
                                                 "                <fluence:MenuItem\n" +
                                                 "                    Click=\"MenuBar_Click\"\n" +
                                                 "                    Header=\"Word wrap\"\n" +
                                                 "                    IsCheckable=\"True\"\n" +
                                                 "                    Tag=\"View - Word wrap\" />\n" +
                                                 "                <Separator />\n" +
                                                 "                <fluence:MenuItem\n" +
                                                 "                    Click=\"MenuBar_Click\"\n" +
                                                 "                    Header=\"Zoom in\"\n" +
                                                 "                    InputGestureText=\"Ctrl++\"\n" +
                                                 "                    Tag=\"View - Zoom in\" />\n" +
                                                 "            </fluence:MenuItem>\n" +
                                                 "            <fluence:MenuItem Header=\"_Help\">\n" +
                                                 "                <fluence:MenuItem\n" +
                                                 "                    Click=\"MenuBar_Click\"\n" +
                                                 "                    Header=\"Documentation\"\n" +
                                                 "                    Tag=\"Help - Documentation\" />\n" +
                                                 "                <fluence:MenuItem Header=\"About\" IsEnabled=\"False\" />\n" +
                                                 "            </fluence:MenuItem>\n" +
                                                 "        </fluence:Menu>\n" +
                                                 "        <TextBlock\n" +
                                                 "            x:Name=\"MenuBarResultLabel\"\n" +
                                                 "            Foreground=\"{DynamicResource TextFillColorSecondaryBrush}\"\n" +
                                                 "            Text=\"Last menu action: None\" />\n" +
                                                 "    </StackPanel>\n");

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
        private static readonly string ContextMenuXamlSource = DemoSampleXaml.UserControl(
            "Fluence.Wpf.Demo.Pages.Menus.ContextMenuActions",
                                                     "    <fluence:Card Padding=\"16\" Variant=\"{x:Static fluence:CardVariant.Subtle}\">\n" +
                                                     "        <fluence:Card.ContextMenu>\n" +
                                                     "            <fluence:ContextMenu>\n" +
                                                     "                <fluence:MenuItem\n" +
                                                     "                    Click=\"ContextMenu_Click\"\n" +
                                                     "                    Header=\"Cut\"\n" +
                                                     "                    InputGestureText=\"Ctrl+X\"\n" +
                                                     "                    Tag=\"Cut\">\n" +
                                                     "                    <fluence:MenuItem.Icon>\n" +
                                                     "                        <fluence:FontIcon Glyph=\"&#xE8C6;\" IconFontSize=\"16\" />\n" +
                                                     "                    </fluence:MenuItem.Icon>\n" +
                                                     "                </fluence:MenuItem>\n" +
                                                     "                <fluence:MenuItem\n" +
                                                     "                    Click=\"ContextMenu_Click\"\n" +
                                                     "                    Header=\"Copy\"\n" +
                                                     "                    InputGestureText=\"Ctrl+C\"\n" +
                                                     "                    Tag=\"Copy\">\n" +
                                                     "                    <fluence:MenuItem.Icon>\n" +
                                                     "                        <fluence:FontIcon Glyph=\"&#xE8C8;\" IconFontSize=\"16\" />\n" +
                                                     "                    </fluence:MenuItem.Icon>\n" +
                                                     "                </fluence:MenuItem>\n" +
                                                     "                <fluence:MenuItem\n" +
                                                     "                    Click=\"ContextMenu_Click\"\n" +
                                                     "                    Header=\"Paste\"\n" +
                                                     "                    InputGestureText=\"Ctrl+V\"\n" +
                                                     "                    Tag=\"Paste\">\n" +
                                                     "                    <fluence:MenuItem.Icon>\n" +
                                                     "                        <fluence:FontIcon Glyph=\"&#xE77F;\" IconFontSize=\"16\" />\n" +
                                                     "                    </fluence:MenuItem.Icon>\n" +
                                                     "                </fluence:MenuItem>\n" +
                                                     "                <Separator />\n" +
                                                     "                <fluence:MenuItem\n" +
                                                     "                    Click=\"ContextMenu_Click\"\n" +
                                                     "                    Header=\"Add to favorites\"\n" +
                                                     "                    IsCheckable=\"True\"\n" +
                                                     "                    Tag=\"Add to favorites\" />\n" +
                                                     "                <fluence:MenuItem Header=\"Share\">\n" +
                                                     "                    <fluence:MenuItem Click=\"ContextMenu_Click\" Header=\"Copy link\" Tag=\"Share - Copy link\" />\n" +
                                                     "                    <fluence:MenuItem Click=\"ContextMenu_Click\" Header=\"Send email\" Tag=\"Share - Send email\" />\n" +
                                                     "                    <fluence:MenuItem Header=\"Export PDF\" IsEnabled=\"False\" />\n" +
                                                     "                </fluence:MenuItem>\n" +
                                                     "            </fluence:ContextMenu>\n" +
                                                     "        </fluence:Card.ContextMenu>\n" +
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
                                                     "    </fluence:Card>\n");

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
        private static readonly string ToolTipsXamlSource = DemoSampleXaml.UserControl(
            "Fluence.Wpf.Demo.Pages.Menus.ToolTips",
                                                  "    <WrapPanel>\n" +
                                                  "        <fluence:Button Margin=\"0,0,8,8\" Content=\"Save\">\n" +
                                                  "            <fluence:Button.ToolTip>\n" +
                                                  "                <fluence:ToolTip Content=\"Save changes (Ctrl+S)\" />\n" +
                                                  "            </fluence:Button.ToolTip>\n" +
                                                  "        </fluence:Button>\n" +
                                                  "        <fluence:Button Margin=\"0,0,8,8\" Content=\"Delete\">\n" +
                                                  "            <fluence:Button.ToolTip>\n" +
                                                  "                <fluence:ToolTip Content=\"Delete the selected item\" />\n" +
                                                  "            </fluence:Button.ToolTip>\n" +
                                                  "        </fluence:Button>\n" +
                                                  "        <fluence:Button Margin=\"0,0,8,8\" Content=\"Share\">\n" +
                                                  "            <fluence:Button.ToolTip>\n" +
                                                  "                <fluence:ToolTip>\n" +
                                                  "                    <StackPanel>\n" +
                                                  "                        <TextBlock\n" +
                                                  "                            FontWeight=\"SemiBold\"\n" +
                                                  "                            Foreground=\"{DynamicResource TextFillColorPrimaryBrush}\"\n" +
                                                  "                            Text=\"Share\" />\n" +
                                                  "                        <TextBlock\n" +
                                                  "                            Foreground=\"{DynamicResource TextFillColorSecondaryBrush}\"\n" +
                                                  "                            Text=\"Copy a link or send an email.\" />\n" +
                                                  "                    </StackPanel>\n" +
                                                  "                </fluence:ToolTip>\n" +
                                                  "            </fluence:Button.ToolTip>\n" +
                                                  "        </fluence:Button>\n" +
                                                  "        <fluence:Button\n" +
                                                  "            Margin=\"0,0,8,8\"\n" +
                                                  "            Content=\"Settings\"\n" +
                                                  "            IsEnabled=\"False\"\n" +
                                                  "            ToolTipService.ShowOnDisabled=\"True\">\n" +
                                                  "            <fluence:Button.ToolTip>\n" +
                                                  "                <fluence:ToolTip Content=\"Settings are disabled for this item\" />\n" +
                                                  "            </fluence:Button.ToolTip>\n" +
                                                  "        </fluence:Button>\n" +
                                                  "    </WrapPanel>\n");

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

        private static readonly string FlyoutXamlSource = DemoSampleXaml.UserControl(
            "Fluence.Wpf.Demo.Pages.Menus.FlyoutSample",
                                                "    <fluence:Button Click=\"FlyoutButton_Click\" Content=\"Show flyout\">\n" +
                                                "        <fluence:FlyoutBase.AttachedFlyout>\n" +
                                                "            <fluence:Flyout Placement=\"Bottom\">\n" +
                                                "                <fluence:Flyout.Content>\n" +
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
                                                "                </fluence:Flyout.Content>\n" +
                                                "            </fluence:Flyout>\n" +
                                                "        </fluence:FlyoutBase.AttachedFlyout>\n" +
                                                "    </fluence:Button>\n");

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

        private static readonly string ContentDialogXamlSource = DemoSampleXaml.UserControl(
            "Fluence.Wpf.Demo.Pages.Menus.ContentDialogSample",
                                                       "    <fluence:Button Click=\"ShowDialogButton_Click\" Content=\"Show dialog\" />\n");

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

        private static readonly string TeachingTipXamlSource = DemoSampleXaml.UserControl(
            "Fluence.Wpf.Demo.Pages.Menus.TeachingTipSample",
                                                     "    <Grid>\n" +
                                                     "        <fluence:Button x:Name=\"TipButton\" Click=\"ShowTipButton_Click\" Content=\"Show teaching tip\" />\n" +
                                                     "        <fluence:TeachingTip\n" +
                                                     "            x:Name=\"Tip\"\n" +
                                                     "            Title=\"Pro tip\"\n" +
                                                     "            Subtitle=\"A TeachingTip coaches the user from a target element without blocking.\"\n" +
                                                     "            CloseButtonContent=\"Got it\"\n" +
                                                     "            IsLightDismissEnabled=\"True\"\n" +
                                                     "            PreferredPlacement=\"Bottom\" />\n" +
                                                     "    </Grid>\n");

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

        private static readonly string CommandBarFlyoutXamlSource = DemoSampleXaml.UserControl(
            "Fluence.Wpf.Demo.Pages.Menus.CommandBarSample",
                                                          "    <fluence:Button Click=\"ShowCommandBarButton_Click\" Content=\"Show command bar\">\n" +
                                                          "        <fluence:FlyoutBase.AttachedFlyout>\n" +
                                                          "            <fluence:CommandBarFlyout>\n" +
                                                          "                <fluence:CommandBarFlyout.PrimaryCommands>\n" +
                                                          "                    <fluence:AppBarButton Click=\"Command_Click\" Label=\"Copy\" Tag=\"Copy\">\n" +
                                                          "                        <fluence:AppBarButton.Icon>\n" +
                                                          "                            <fluence:FontIcon Glyph=\"&#xE8C8;\" IconFontSize=\"16\" />\n" +
                                                          "                        </fluence:AppBarButton.Icon>\n" +
                                                          "                    </fluence:AppBarButton>\n" +
                                                          "                </fluence:CommandBarFlyout.PrimaryCommands>\n" +
                                                          "                <fluence:CommandBarFlyout.SecondaryCommands>\n" +
                                                          "                    <fluence:AppBarButton Click=\"Command_Click\" Label=\"Delete\" Tag=\"Delete\" />\n" +
                                                          "                </fluence:CommandBarFlyout.SecondaryCommands>\n" +
                                                          "            </fluence:CommandBarFlyout>\n" +
                                                          "        </fluence:FlyoutBase.AttachedFlyout>\n" +
                                                          "    </fluence:Button>\n");

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
