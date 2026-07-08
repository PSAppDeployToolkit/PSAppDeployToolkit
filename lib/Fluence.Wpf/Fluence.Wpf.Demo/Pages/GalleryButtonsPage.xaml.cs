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
using System.Windows;
using System.Windows.Controls;

namespace Fluence.Wpf.Demo.Pages
{
    public partial class GalleryButtonsPage : UserControl
    {
        private const string ButtonAppearancesXamlSource = "<UserControl\n" +
                                                           "    x:Class=\"Fluence.Wpf.Demo.Pages.Buttons.ButtonAppearances\"\n" +
                                                           "    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"\n" +
                                                           "    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"\n" +
                                                           "    xmlns:ui=\"clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf\">\n" +
                                                           "    <StackPanel>\n" +
                                                           "        <WrapPanel VerticalAlignment=\"Center\">\n" +
                                                           "            <ui:Button\n" +
                                                           "                Margin=\"0,0,8,8\"\n" +
                                                           "                Content=\"Standard\"\n" +
                                                           "                IsEnabled=\"{Binding IsChecked, Source={x:Reference ButtonEnableCheckBox}}\" />\n" +
                                                           "            <ui:Button\n" +
                                                           "                Margin=\"0,0,8,8\"\n" +
                                                           "                Appearance=\"Accent\"\n" +
                                                           "                Content=\"Accent\"\n" +
                                                           "                IsEnabled=\"{Binding IsChecked, Source={x:Reference ButtonEnableCheckBox}}\" />\n" +
                                                           "            <ui:Button\n" +
                                                           "                Margin=\"0,0,8,8\"\n" +
                                                           "                Appearance=\"Subtle\"\n" +
                                                           "                Content=\"Subtle\"\n" +
                                                           "                IsEnabled=\"{Binding IsChecked, Source={x:Reference ButtonEnableCheckBox}}\" />\n" +
                                                           "        </WrapPanel>\n" +
                                                           "        <ui:CheckBox\n" +
                                                           "            x:Name=\"ButtonEnableCheckBox\"\n" +
                                                           "            Content=\"Enable buttons\"\n" +
                                                           "            IsChecked=\"True\" />\n" +
                                                           "    </StackPanel>\n" +
                                                           "</UserControl>\n";

        private const string ButtonAppearancesCSharpSource = "using System.Windows.Controls;\n" +
                                                             "\n" +
                                                             "namespace Fluence.Wpf.Demo.Pages.Buttons\n" +
                                                             "{\n" +
                                                             "    public partial class ButtonAppearances : UserControl\n" +
                                                             "    {\n" +
                                                             "        public ButtonAppearances()\n" +
                                                             "        {\n" +
                                                             "            InitializeComponent();\n" +
                                                             "        }\n" +
                                                             "    }\n" +
                                                             "}\n";
        private const string ButtonIconsXamlSource = "<UserControl\n" +
                                                     "    x:Class=\"Fluence.Wpf.Demo.Pages.Buttons.ButtonIcons\"\n" +
                                                     "    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"\n" +
                                                     "    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"\n" +
                                                     "    xmlns:ui=\"clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf\">\n" +
                                                     "    <WrapPanel VerticalAlignment=\"Center\">\n" +
                                                     "        <ui:Button Margin=\"0,0,8,8\" Content=\"Icon Left\">\n" +
                                                     "            <ui:Button.Icon>\n" +
                                                     "                <ui:FontIcon Glyph=\"&#xE774;\" IconFontSize=\"14\" />\n" +
                                                     "            </ui:Button.Icon>\n" +
                                                     "        </ui:Button>\n" +
                                                     "        <ui:Button\n" +
                                                     "            Margin=\"0,0,8,8\"\n" +
                                                     "            Content=\"Icon Right\"\n" +
                                                     "            IconPlacement=\"Right\">\n" +
                                                     "            <ui:Button.Icon>\n" +
                                                     "                <ui:FontIcon Glyph=\"&#xE8D6;\" IconFontSize=\"14\" />\n" +
                                                     "            </ui:Button.Icon>\n" +
                                                     "        </ui:Button>\n" +
                                                     "        <ui:Button\n" +
                                                     "            Margin=\"0,0,8,8\"\n" +
                                                     "            Appearance=\"Subtle\"\n" +
                                                     "            Content=\"Refresh\">\n" +
                                                     "            <ui:Button.Icon>\n" +
                                                     "                <ui:FontIcon Glyph=\"&#xE72C;\" IconFontSize=\"14\" />\n" +
                                                     "            </ui:Button.Icon>\n" +
                                                     "        </ui:Button>\n" +
                                                     "    </WrapPanel>\n" +
                                                     "</UserControl>\n";

        private const string ButtonIconsCSharpSource = "using System.Windows.Controls;\n" +
                                                       "\n" +
                                                       "namespace Fluence.Wpf.Demo.Pages.Buttons\n" +
                                                       "{\n" +
                                                       "    public partial class ButtonIcons : UserControl\n" +
                                                       "    {\n" +
                                                       "        public ButtonIcons()\n" +
                                                       "        {\n" +
                                                       "            InitializeComponent();\n" +
                                                       "        }\n" +
                                                       "    }\n" +
                                                       "}\n";
        private const string HyperlinkButtonsXamlSource = "<UserControl\n" +
                                                          "    x:Class=\"Fluence.Wpf.Demo.Pages.Buttons.HyperlinkButtons\"\n" +
                                                          "    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"\n" +
                                                          "    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"\n" +
                                                          "    xmlns:ui=\"clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf\">\n" +
                                                          "    <WrapPanel VerticalAlignment=\"Center\">\n" +
                                                          "        <ui:HyperlinkButton\n" +
                                                          "            Margin=\"0,0,16,8\"\n" +
                                                          "            Content=\"Documentation\"\n" +
                                                          "            NavigateUri=\"https://github.com/sintaxasn/Fluence.Wpf\" />\n" +
                                                          "        <ui:HyperlinkButton\n" +
                                                          "            Margin=\"0,0,16,8\"\n" +
                                                          "            Content=\"Release notes\"\n" +
                                                          "            NavigateUri=\"https://github.com/sintaxasn/Fluence.Wpf/releases\" />\n" +
                                                          "        <ui:HyperlinkButton\n" +
                                                          "            Margin=\"0,0,16,8\"\n" +
                                                          "            Content=\"With icon\"\n" +
                                                          "            NavigateUri=\"https://github.com/sintaxasn/Fluence.Wpf\">\n" +
                                                          "            <ui:HyperlinkButton.Icon>\n" +
                                                          "                <ui:FontIcon Glyph=\"&#xE71B;\" IconFontSize=\"14\" />\n" +
                                                          "            </ui:HyperlinkButton.Icon>\n" +
                                                          "        </ui:HyperlinkButton>\n" +
                                                          "        <ui:HyperlinkButton\n" +
                                                          "            Margin=\"0,0,16,8\"\n" +
                                                          "            Content=\"Disabled\"\n" +
                                                          "            IsEnabled=\"False\" />\n" +
                                                          "    </WrapPanel>\n" +
                                                          "</UserControl>\n";

        private const string HyperlinkButtonsCSharpSource = "using System.Windows.Controls;\n" +
                                                            "\n" +
                                                            "namespace Fluence.Wpf.Demo.Pages.Buttons\n" +
                                                            "{\n" +
                                                            "    public partial class HyperlinkButtons : UserControl\n" +
                                                            "    {\n" +
                                                            "        public HyperlinkButtons()\n" +
                                                            "        {\n" +
                                                            "            InitializeComponent();\n" +
                                                            "        }\n" +
                                                            "    }\n" +
                                                            "}\n";
        private const string DropDownButtonsXamlSource = "<UserControl\n" +
                                                         "    x:Class=\"Fluence.Wpf.Demo.Pages.Buttons.DropDownButtons\"\n" +
                                                         "    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"\n" +
                                                         "    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"\n" +
                                                         "    xmlns:ui=\"clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf\">\n" +
                                                         "    <WrapPanel VerticalAlignment=\"Center\">\n" +
                                                         "        <ui:DropDownButton Margin=\"0,0,8,8\" Content=\"New\">\n" +
                                                         "            <ui:DropDownButton.Flyout>\n" +
                                                         "                <StackPanel MinWidth=\"180\" Margin=\"4\">\n" +
                                                         "                    <ui:Button\n" +
                                                         "                        HorizontalAlignment=\"Stretch\"\n" +
                                                         "                        HorizontalContentAlignment=\"Left\"\n" +
                                                         "                        Appearance=\"Subtle\"\n" +
                                                         "                        Content=\"Document\" />\n" +
                                                         "                    <ui:Button\n" +
                                                         "                        HorizontalAlignment=\"Stretch\"\n" +
                                                         "                        HorizontalContentAlignment=\"Left\"\n" +
                                                         "                        Appearance=\"Subtle\"\n" +
                                                         "                        Content=\"Spreadsheet\" />\n" +
                                                         "                    <ui:Button\n" +
                                                         "                        HorizontalAlignment=\"Stretch\"\n" +
                                                         "                        HorizontalContentAlignment=\"Left\"\n" +
                                                         "                        Appearance=\"Subtle\"\n" +
                                                         "                        Content=\"Folder\" />\n" +
                                                         "                </StackPanel>\n" +
                                                         "            </ui:DropDownButton.Flyout>\n" +
                                                         "        </ui:DropDownButton>\n" +
                                                         "        <ui:DropDownButton Margin=\"0,0,8,8\" Content=\"Details\">\n" +
                                                         "            <ui:DropDownButton.Flyout>\n" +
                                                         "                <StackPanel MaxWidth=\"260\" Margin=\"12\">\n" +
                                                         "                    <TextBlock\n" +
                                                         "                        Margin=\"0,0,0,6\"\n" +
                                                         "                        Foreground=\"{DynamicResource TextFillColorPrimaryBrush}\"\n" +
                                                         "                        Text=\"Project status\" />\n" +
                                                         "                    <TextBlock\n" +
                                                         "                        Foreground=\"{DynamicResource TextFillColorSecondaryBrush}\"\n" +
                                                         "                        Text=\"Flyout content can be any WPF content.\"\n" +
                                                         "                        TextWrapping=\"Wrap\" />\n" +
                                                         "                </StackPanel>\n" +
                                                         "            </ui:DropDownButton.Flyout>\n" +
                                                         "        </ui:DropDownButton>\n" +
                                                         "        <ui:DropDownButton\n" +
                                                         "            Margin=\"0,0,8,8\"\n" +
                                                         "            Content=\"Disabled\"\n" +
                                                         "            IsEnabled=\"False\">\n" +
                                                         "            <ui:DropDownButton.Flyout>\n" +
                                                         "                <TextBlock Margin=\"12\" Text=\"Unavailable\" />\n" +
                                                         "            </ui:DropDownButton.Flyout>\n" +
                                                         "        </ui:DropDownButton>\n" +
                                                         "    </WrapPanel>\n" +
                                                         "</UserControl>\n";

        private const string DropDownButtonsCSharpSource = "using System.Windows.Controls;\n" +
                                                           "\n" +
                                                           "namespace Fluence.Wpf.Demo.Pages.Buttons\n" +
                                                           "{\n" +
                                                           "    public partial class DropDownButtons : UserControl\n" +
                                                           "    {\n" +
                                                           "        public DropDownButtons()\n" +
                                                           "        {\n" +
                                                           "            InitializeComponent();\n" +
                                                           "        }\n" +
                                                           "    }\n" +
                                                           "}\n";
        private const string SplitButtonsXamlSource = "<UserControl\n" +
                                                      "    x:Class=\"Fluence.Wpf.Demo.Pages.Buttons.SplitButtons\"\n" +
                                                      "    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"\n" +
                                                      "    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"\n" +
                                                      "    xmlns:ui=\"clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf\">\n" +
                                                      "    <WrapPanel VerticalAlignment=\"Center\">\n" +
                                                      "        <ui:SplitButton Margin=\"0,0,8,8\" Content=\"Save\">\n" +
                                                      "            <ui:SplitButton.Flyout>\n" +
                                                      "                <StackPanel MinWidth=\"180\" Margin=\"4\">\n" +
                                                      "                    <ui:Button\n" +
                                                      "                        HorizontalAlignment=\"Stretch\"\n" +
                                                      "                        HorizontalContentAlignment=\"Left\"\n" +
                                                      "                        Appearance=\"Subtle\"\n" +
                                                      "                        Content=\"Save as\" />\n" +
                                                      "                    <ui:Button\n" +
                                                      "                        HorizontalAlignment=\"Stretch\"\n" +
                                                      "                        HorizontalContentAlignment=\"Left\"\n" +
                                                      "                        Appearance=\"Subtle\"\n" +
                                                      "                        Content=\"Save a copy\" />\n" +
                                                      "                    <ui:Button\n" +
                                                      "                        HorizontalAlignment=\"Stretch\"\n" +
                                                      "                        HorizontalContentAlignment=\"Left\"\n" +
                                                      "                        Appearance=\"Subtle\"\n" +
                                                      "                        Content=\"Export\" />\n" +
                                                      "                </StackPanel>\n" +
                                                      "            </ui:SplitButton.Flyout>\n" +
                                                      "        </ui:SplitButton>\n" +
                                                      "        <ui:SplitButton\n" +
                                                      "            Margin=\"0,0,8,8\"\n" +
                                                      "            Appearance=\"Accent\"\n" +
                                                      "            Content=\"Publish\">\n" +
                                                      "            <ui:SplitButton.Flyout>\n" +
                                                      "                <StackPanel MinWidth=\"180\" Margin=\"4\">\n" +
                                                      "                    <ui:Button\n" +
                                                      "                        HorizontalAlignment=\"Stretch\"\n" +
                                                      "                        HorizontalContentAlignment=\"Left\"\n" +
                                                      "                        Appearance=\"Subtle\"\n" +
                                                      "                        Content=\"Publish draft\" />\n" +
                                                      "                    <ui:Button\n" +
                                                      "                        HorizontalAlignment=\"Stretch\"\n" +
                                                      "                        HorizontalContentAlignment=\"Left\"\n" +
                                                      "                        Appearance=\"Subtle\"\n" +
                                                      "                        Content=\"Schedule publish\" />\n" +
                                                      "                </StackPanel>\n" +
                                                      "            </ui:SplitButton.Flyout>\n" +
                                                      "        </ui:SplitButton>\n" +
                                                      "        <ui:SplitButton\n" +
                                                      "            Margin=\"0,0,8,8\"\n" +
                                                      "            Content=\"Disabled\"\n" +
                                                      "            IsEnabled=\"False\">\n" +
                                                      "            <ui:SplitButton.Flyout>\n" +
                                                      "                <TextBlock Margin=\"12\" Text=\"Unavailable\" />\n" +
                                                      "            </ui:SplitButton.Flyout>\n" +
                                                      "        </ui:SplitButton>\n" +
                                                      "    </WrapPanel>\n" +
                                                      "</UserControl>\n";

        private const string SplitButtonsCSharpSource = "using System.Windows.Controls;\n" +
                                                        "\n" +
                                                        "namespace Fluence.Wpf.Demo.Pages.Buttons\n" +
                                                        "{\n" +
                                                        "    public partial class SplitButtons : UserControl\n" +
                                                        "    {\n" +
                                                        "        public SplitButtons()\n" +
                                                        "        {\n" +
                                                        "            InitializeComponent();\n" +
                                                        "        }\n" +
                                                        "    }\n" +
                                                        "}\n";
        private const string RepeatButtonsXamlSource = "<UserControl\n" +
                                                       "    x:Class=\"Fluence.Wpf.Demo.Pages.Buttons.RepeatButtons\"\n" +
                                                       "    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"\n" +
                                                       "    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"\n" +
                                                       "    xmlns:ui=\"clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf\">\n" +
                                                       "    <WrapPanel VerticalAlignment=\"Center\">\n" +
                                                       "        <ui:RepeatButton\n" +
                                                       "            x:Name=\"RepeatCounterButton\"\n" +
                                                       "            Margin=\"0,0,8,8\"\n" +
                                                       "            Click=\"RepeatCounterButton_Click\"\n" +
                                                       "            Content=\"Hold to repeat\" />\n" +
                                                       "        <TextBlock\n" +
                                                       "            x:Name=\"RepeatButtonCountText\"\n" +
                                                       "            Margin=\"0,0,16,8\"\n" +
                                                       "            VerticalAlignment=\"Center\"\n" +
                                                       "            Foreground=\"{DynamicResource TextFillColorSecondaryBrush}\"\n" +
                                                       "            Text=\"Clicks: 0\" />\n" +
                                                       "    </WrapPanel>\n" +
                                                       "</UserControl>\n";

        private const string RepeatButtonsCSharpSource = "using System.Globalization;\n" +
                                                         "using System.Windows;\n" +
                                                         "using System.Windows.Controls;\n" +
                                                         "\n" +
                                                         "namespace Fluence.Wpf.Demo.Pages.Buttons\n" +
                                                         "{\n" +
                                                         "    public partial class RepeatButtons : UserControl\n" +
                                                         "    {\n" +
                                                         "        private int repeatButtonClickCount;\n" +
                                                         "\n" +
                                                         "        public RepeatButtons()\n" +
                                                         "        {\n" +
                                                         "            InitializeComponent();\n" +
                                                         "        }\n" +
                                                         "\n" +
                                                         "        private void RepeatCounterButton_Click(object sender, RoutedEventArgs e)\n" +
                                                         "        {\n" +
                                                         "            repeatButtonClickCount++;\n" +
                                                         "            RepeatButtonCountText.Text = string.Format(\n" +
                                                         "                CultureInfo.CurrentCulture,\n" +
                                                         "                \"Clicks: {0}\",\n" +
                                                         "                repeatButtonClickCount);\n" +
                                                         "        }\n" +
                                                         "    }\n" +
                                                         "}\n";
        private const string ToggleButtonsXamlSource = "<UserControl\n" +
                                                       "    x:Class=\"Fluence.Wpf.Demo.Pages.Buttons.ToggleButtons\"\n" +
                                                       "    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"\n" +
                                                       "    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"\n" +
                                                       "    xmlns:ui=\"clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf\">\n" +
                                                       "    <StackPanel>\n" +
                                                       "        <WrapPanel VerticalAlignment=\"Center\">\n" +
                                                       "            <ui:ToggleButton\n" +
                                                       "                x:Name=\"WrapToggleButton\"\n" +
                                                       "                Margin=\"0,0,8,8\"\n" +
                                                       "                Checked=\"WrapToggleButton_CheckedChanged\"\n" +
                                                       "                Content=\"Wrap text\"\n" +
                                                       "                Unchecked=\"WrapToggleButton_CheckedChanged\" />\n" +
                                                       "            <ui:ToggleButton\n" +
                                                       "                Margin=\"0,0,8,8\"\n" +
                                                       "                Content=\"Three-state\"\n" +
                                                       "                IsThreeState=\"True\" />\n" +
                                                       "            <ui:ToggleButton\n" +
                                                       "                Margin=\"0,0,8,8\"\n" +
                                                       "                Content=\"Disabled checked\"\n" +
                                                       "                IsChecked=\"True\"\n" +
                                                       "                IsEnabled=\"False\" />\n" +
                                                       "        </WrapPanel>\n" +
                                                       "        <TextBlock\n" +
                                                       "            x:Name=\"ToggleButtonStateText\"\n" +
                                                       "            Foreground=\"{DynamicResource TextFillColorSecondaryBrush}\"\n" +
                                                       "            Text=\"Wrap text: Off\" />\n" +
                                                       "    </StackPanel>\n" +
                                                       "</UserControl>\n";

        private const string ToggleButtonsCSharpSource = "using System.Windows;\n" +
                                                         "using System.Windows.Controls;\n" +
                                                         "\n" +
                                                         "namespace Fluence.Wpf.Demo.Pages.Buttons\n" +
                                                         "{\n" +
                                                         "    public partial class ToggleButtons : UserControl\n" +
                                                         "    {\n" +
                                                         "        public ToggleButtons()\n" +
                                                         "        {\n" +
                                                         "            InitializeComponent();\n" +
                                                         "        }\n" +
                                                         "\n" +
                                                         "        private void WrapToggleButton_CheckedChanged(object sender, RoutedEventArgs e)\n" +
                                                         "        {\n" +
                                                         "            ToggleButtonStateText.Text = WrapToggleButton.IsChecked == true\n" +
                                                         "                ? \"Wrap text: On\"\n" +
                                                         "                : \"Wrap text: Off\";\n" +
                                                         "        }\n" +
                                                         "    }\n" +
                                                         "}\n";
        private const string ToggleSplitButtonsXamlSource = "<UserControl\n" +
                                                            "    x:Class=\"Fluence.Wpf.Demo.Pages.Buttons.ToggleSplitButtons\"\n" +
                                                            "    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"\n" +
                                                            "    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"\n" +
                                                            "    xmlns:ui=\"clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf\">\n" +
                                                            "    <StackPanel>\n" +
                                                            "        <WrapPanel VerticalAlignment=\"Center\">\n" +
                                                            "            <ui:ToggleSplitButton\n" +
                                                            "                x:Name=\"ListToggleSplitButton\"\n" +
                                                            "                Margin=\"0,0,8,8\"\n" +
                                                            "                Content=\"Bulleted list\"\n" +
                                                            "                IsCheckedChanged=\"ListToggleSplitButton_IsCheckedChanged\">\n" +
                                                            "                <ui:ToggleSplitButton.Flyout>\n" +
                                                            "                    <StackPanel MinWidth=\"180\" Margin=\"4\">\n" +
                                                            "                        <ui:Button\n" +
                                                            "                            HorizontalAlignment=\"Stretch\"\n" +
                                                            "                            HorizontalContentAlignment=\"Left\"\n" +
                                                            "                            Appearance=\"Subtle\"\n" +
                                                            "                            Click=\"ListStyleButton_Click\"\n" +
                                                            "                            Content=\"Bulleted list\" />\n" +
                                                            "                        <ui:Button\n" +
                                                            "                            HorizontalAlignment=\"Stretch\"\n" +
                                                            "                            HorizontalContentAlignment=\"Left\"\n" +
                                                            "                            Appearance=\"Subtle\"\n" +
                                                            "                            Click=\"ListStyleButton_Click\"\n" +
                                                            "                            Content=\"Numbered list\" />\n" +
                                                            "                        <ui:Button\n" +
                                                            "                            HorizontalAlignment=\"Stretch\"\n" +
                                                            "                            HorizontalContentAlignment=\"Left\"\n" +
                                                            "                            Appearance=\"Subtle\"\n" +
                                                            "                            Click=\"ListStyleButton_Click\"\n" +
                                                            "                            Content=\"Checklist\" />\n" +
                                                            "                    </StackPanel>\n" +
                                                            "                </ui:ToggleSplitButton.Flyout>\n" +
                                                            "            </ui:ToggleSplitButton>\n" +
                                                            "            <ui:ToggleSplitButton\n" +
                                                            "                Margin=\"0,0,8,8\"\n" +
                                                            "                Content=\"Disabled\"\n" +
                                                            "                IsChecked=\"True\"\n" +
                                                            "                IsEnabled=\"False\">\n" +
                                                            "                <ui:ToggleSplitButton.Flyout>\n" +
                                                            "                    <TextBlock Margin=\"12\" Text=\"Unavailable\" />\n" +
                                                            "                </ui:ToggleSplitButton.Flyout>\n" +
                                                            "            </ui:ToggleSplitButton>\n" +
                                                            "        </WrapPanel>\n" +
                                                            "        <TextBlock\n" +
                                                            "            x:Name=\"ToggleSplitButtonStateText\"\n" +
                                                            "            Foreground=\"{DynamicResource TextFillColorSecondaryBrush}\"\n" +
                                                            "            Text=\"List formatting: Off\" />\n" +
                                                            "    </StackPanel>\n" +
                                                            "</UserControl>\n";

        private const string ToggleSplitButtonsCSharpSource = "using System.Globalization;\n" +
                                                              "using System.Windows;\n" +
                                                              "using System.Windows.Controls;\n" +
                                                              "\n" +
                                                              "namespace Fluence.Wpf.Demo.Pages.Buttons\n" +
                                                              "{\n" +
                                                              "    public partial class ToggleSplitButtons : UserControl\n" +
                                                              "    {\n" +
                                                              "        public ToggleSplitButtons()\n" +
                                                              "        {\n" +
                                                              "            InitializeComponent();\n" +
                                                              "        }\n" +
                                                              "\n" +
                                                              "        private void ListToggleSplitButton_IsCheckedChanged(object? sender, Fluence.Wpf.ToggleSplitButtonIsCheckedChangedEventArgs e)\n" +
                                                              "        {\n" +
                                                              "            UpdateListFormattingText(e.IsChecked);\n" +
                                                              "        }\n" +
                                                              "\n" +
                                                              "        private void ListStyleButton_Click(object sender, RoutedEventArgs e)\n" +
                                                              "        {\n" +
                                                              "            if (sender is Fluence.Wpf.Controls.Button button && button.Content is string listStyle)\n" +
                                                              "            {\n" +
                                                              "                ListToggleSplitButton.Content = listStyle;\n" +
                                                              "                ListToggleSplitButton.IsChecked = true;\n" +
                                                              "                UpdateListFormattingText(isChecked: true);\n" +
                                                              "            }\n" +
                                                              "        }\n" +
                                                              "\n" +
                                                              "        private void UpdateListFormattingText(bool isChecked)\n" +
                                                              "        {\n" +
                                                              "            ToggleSplitButtonStateText.Text = isChecked\n" +
                                                              "                ? string.Format(CultureInfo.CurrentCulture, \"List formatting: {0}\", ListToggleSplitButton.Content)\n" +
                                                              "                : \"List formatting: Off\";\n" +
                                                              "        }\n" +
                                                              "    }\n" +
                                                              "}\n";

        // Click counter for the RepeatButton interactive demo; incremented by
        // RepeatCounterButton_Click and displayed in RepeatButtonCountText.
        private int _repeatButtonClickCount;

        public GalleryButtonsPage()
        {
            InitializeComponent();

            // Move each hidden slot's control into its DemoSampleControl card and attach the
            // XAML/C# source shown in the expander. The Nth source maps to DemoSampleSlot{N}. See
            // DemoSamplePageWiring for the slot-naming contract.
            DemoSamplePageWiring.Apply(
                (DependencyObject)Content,
                new DemoSampleSource(1, ButtonAppearancesXamlSource, ButtonAppearancesCSharpSource),
                new DemoSampleSource(2, ButtonIconsXamlSource, ButtonIconsCSharpSource),
                new DemoSampleSource(3, HyperlinkButtonsXamlSource, HyperlinkButtonsCSharpSource),
                new DemoSampleSource(4, DropDownButtonsXamlSource, DropDownButtonsCSharpSource),
                new DemoSampleSource(5, SplitButtonsXamlSource, SplitButtonsCSharpSource),
                new DemoSampleSource(6, RepeatButtonsXamlSource, RepeatButtonsCSharpSource),
                new DemoSampleSource(7, ToggleButtonsXamlSource, ToggleButtonsCSharpSource),
                new DemoSampleSource(8, ToggleSplitButtonsXamlSource, ToggleSplitButtonsCSharpSource));
        }

        private void RepeatCounterButton_Click(object sender, RoutedEventArgs e)
        {
            _repeatButtonClickCount++;
            RepeatButtonCountText.Text = string.Format(
                CultureInfo.CurrentCulture,
                "Clicks: {0}",
                _repeatButtonClickCount);
        }

        private void WrapToggleButton_CheckedChanged(object sender, RoutedEventArgs e)
        {
            ToggleButtonStateText.Text = WrapToggleButton.IsChecked is true
                ? "Wrap text: On"
                : "Wrap text: Off";
        }

        private void ListToggleSplitButton_IsCheckedChanged(object? sender, ToggleSplitButtonIsCheckedChangedEventArgs e)
        {
            UpdateListFormattingText(e.IsChecked);
        }

        private void ListStyleButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Fluence.Wpf.Controls.Button button && button.Content is string listStyle)
            {
                ListToggleSplitButton.Content = listStyle;
                ListToggleSplitButton.IsChecked = true;
                UpdateListFormattingText(isChecked: true);
            }
        }

        private void UpdateListFormattingText(bool isChecked)
        {
            ToggleSplitButtonStateText.Text = isChecked
                ? string.Format(CultureInfo.CurrentCulture, "List formatting: {0}", ListToggleSplitButton.Content)
                : "List formatting: Off";
        }
    }
}
