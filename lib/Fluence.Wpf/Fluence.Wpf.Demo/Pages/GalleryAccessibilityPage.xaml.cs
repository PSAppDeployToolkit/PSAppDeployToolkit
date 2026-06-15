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

using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Fluence.Wpf.Demo.Pages
{
    /// <summary>
    /// Gallery page demonstrating accessibility features: focus rings, tab order, HC brush mapping, and RTL layout.
    /// </summary>
    public partial class GalleryAccessibilityPage : UserControl
    {
        private const string FocusAndTabOrderXamlSource = "<UserControl\n" +
                                                          "    x:Class=\"Fluence.Wpf.Demo.Pages.Accessibility.FocusAndTabOrder\"\n" +
                                                          "    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"\n" +
                                                          "    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"\n" +
                                                          "    xmlns:ui=\"clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf\">\n" +
                                                          "    <StackPanel>\n" +
                                                          "        <Grid x:Name=\"KeyboardSupportPrimaryControls\" Margin=\"0,0,0,8\">\n" +
                                                          "            <Grid.ColumnDefinitions>\n" +
                                                          "                <ColumnDefinition Width=\"*\" />\n" +
                                                          "                <ColumnDefinition Width=\"*\" />\n" +
                                                          "                <ColumnDefinition Width=\"*\" />\n" +
                                                          "                <ColumnDefinition Width=\"*\" />\n" +
                                                          "            </Grid.ColumnDefinitions>\n" +
                                                          "            <Grid.RowDefinitions>\n" +
                                                          "                <RowDefinition Height=\"Auto\" />\n" +
                                                          "                <RowDefinition Height=\"Auto\" />\n" +
                                                          "            </Grid.RowDefinitions>\n" +
                                                          "            <ui:Button\n" +
                                                          "                Grid.Row=\"0\"\n" +
                                                          "                Grid.Column=\"0\"\n" +
                                                          "                Margin=\"0,0,12,12\"\n" +
                                                          "                HorizontalAlignment=\"Stretch\"\n" +
                                                          "                VerticalAlignment=\"Center\"\n" +
                                                          "                AutomationProperties.Name=\"First focusable button\"\n" +
                                                          "                Content=\"Button 1\" />\n" +
                                                          "            <ui:Button\n" +
                                                          "                Grid.Row=\"0\"\n" +
                                                          "                Grid.Column=\"1\"\n" +
                                                          "                Margin=\"0,0,12,12\"\n" +
                                                          "                HorizontalAlignment=\"Stretch\"\n" +
                                                          "                VerticalAlignment=\"Center\"\n" +
                                                          "                AutomationProperties.Name=\"Second focusable button\"\n" +
                                                          "                Content=\"Button 2\" />\n" +
                                                          "            <ui:TextBox\n" +
                                                          "                Grid.Row=\"0\"\n" +
                                                          "                Grid.Column=\"2\"\n" +
                                                          "                Margin=\"0,0,12,12\"\n" +
                                                          "                HorizontalAlignment=\"Stretch\"\n" +
                                                          "                VerticalAlignment=\"Center\"\n" +
                                                          "                AutomationProperties.Name=\"Focusable text input\"\n" +
                                                          "                PlaceholderText=\"TextBox\" />\n" +
                                                          "            <ui:ComboBox\n" +
                                                          "                Grid.Row=\"0\"\n" +
                                                          "                Grid.Column=\"3\"\n" +
                                                          "                Margin=\"0,0,0,12\"\n" +
                                                          "                HorizontalAlignment=\"Stretch\"\n" +
                                                          "                VerticalAlignment=\"Center\"\n" +
                                                          "                AutomationProperties.Name=\"Focusable combo box\">\n" +
                                                          "                <ComboBoxItem Content=\"Option A\" IsSelected=\"True\" />\n" +
                                                          "                <ComboBoxItem Content=\"Option B\" />\n" +
                                                          "            </ui:ComboBox>\n" +
                                                          "            <ui:CheckBox\n" +
                                                          "                Grid.Row=\"1\"\n" +
                                                          "                Grid.Column=\"0\"\n" +
                                                          "                Margin=\"0,0,12,0\"\n" +
                                                          "                HorizontalAlignment=\"Left\"\n" +
                                                          "                VerticalAlignment=\"Center\"\n" +
                                                          "                AutomationProperties.Name=\"Focusable checkbox\"\n" +
                                                          "                Content=\"CheckBox\" />\n" +
                                                          "            <ui:ToggleSwitch\n" +
                                                          "                Grid.Row=\"1\"\n" +
                                                          "                Grid.Column=\"1\"\n" +
                                                          "                Margin=\"0,0,12,0\"\n" +
                                                          "                HorizontalAlignment=\"Left\"\n" +
                                                          "                VerticalAlignment=\"Center\"\n" +
                                                          "                AutomationProperties.Name=\"Focusable toggle\" />\n" +
                                                          "            <ui:Slider\n" +
                                                          "                Grid.Row=\"1\"\n" +
                                                          "                Grid.Column=\"2\"\n" +
                                                          "                Margin=\"0,0,12,0\"\n" +
                                                          "                HorizontalAlignment=\"Stretch\"\n" +
                                                          "                VerticalAlignment=\"Center\"\n" +
                                                          "                AutomationProperties.Name=\"Focusable slider\"\n" +
                                                          "                Maximum=\"100\"\n" +
                                                          "                Minimum=\"0\"\n" +
                                                          "                Value=\"40\" />\n" +
                                                          "            <ui:HyperlinkButton\n" +
                                                          "                Grid.Row=\"1\"\n" +
                                                          "                Grid.Column=\"3\"\n" +
                                                          "                HorizontalAlignment=\"Stretch\"\n" +
                                                          "                VerticalAlignment=\"Center\"\n" +
                                                          "                AutomationProperties.Name=\"Focusable hyperlink\"\n" +
                                                          "                Content=\"HyperlinkButton\" />\n" +
                                                          "        </Grid>\n" +
                                                          "\n" +
                                                          "        <TextBlock\n" +
                                                          "            Margin=\"0,12,0,8\"\n" +
                                                          "            Foreground=\"{DynamicResource TextFillColorSecondaryBrush}\"\n" +
                                                          "            Text=\"The buttons below have an explicit reverse tab order: 3, 2, 1.\" />\n" +
                                                          "        <Grid\n" +
                                                          "            x:Name=\"KeyboardSupportExplicitOrderControls\"\n" +
                                                          "            Margin=\"0,0,0,8\"\n" +
                                                          "            KeyboardNavigation.TabNavigation=\"Local\">\n" +
                                                          "            <Grid.ColumnDefinitions>\n" +
                                                          "                <ColumnDefinition Width=\"*\" />\n" +
                                                          "                <ColumnDefinition Width=\"*\" />\n" +
                                                          "                <ColumnDefinition Width=\"*\" />\n" +
                                                          "            </Grid.ColumnDefinitions>\n" +
                                                          "            <ui:Button\n" +
                                                          "                x:Name=\"ExplicitTabOrderThirdButton\"\n" +
                                                          "                Grid.Column=\"0\"\n" +
                                                          "                Margin=\"0,0,12,0\"\n" +
                                                          "                HorizontalAlignment=\"Stretch\"\n" +
                                                          "                VerticalAlignment=\"Center\"\n" +
                                                          "                Content=\"Tab order: 3\"\n" +
                                                          "                TabIndex=\"3\" />\n" +
                                                          "            <ui:Button\n" +
                                                          "                x:Name=\"ExplicitTabOrderSecondButton\"\n" +
                                                          "                Grid.Column=\"1\"\n" +
                                                          "                Margin=\"0,0,12,0\"\n" +
                                                          "                HorizontalAlignment=\"Stretch\"\n" +
                                                          "                VerticalAlignment=\"Center\"\n" +
                                                          "                Content=\"Tab order: 2\"\n" +
                                                          "                TabIndex=\"2\" />\n" +
                                                          "            <ui:Button\n" +
                                                          "                x:Name=\"ExplicitTabOrderFirstButton\"\n" +
                                                          "                Grid.Column=\"2\"\n" +
                                                          "                HorizontalAlignment=\"Stretch\"\n" +
                                                          "                VerticalAlignment=\"Center\"\n" +
                                                          "                Appearance=\"Accent\"\n" +
                                                          "                Content=\"Tab order: 1 (first)\"\n" +
                                                          "                TabIndex=\"1\" />\n" +
                                                          "        </Grid>\n" +
                                                          "    </StackPanel>\n" +
                                                          "</UserControl>\n";

        private const string FocusAndTabOrderCSharpSource = "using System.Windows.Controls;\n" +
                                                            "\n" +
                                                            "namespace Fluence.Wpf.Demo.Pages.Accessibility\n" +
                                                            "{\n" +
                                                            "    public partial class FocusAndTabOrder : UserControl\n" +
                                                            "    {\n" +
                                                            "        public FocusAndTabOrder()\n" +
                                                            "        {\n" +
                                                            "            InitializeComponent();\n" +
                                                            "        }\n" +
                                                            "    }\n" +
                                                            "}\n";
        private const string HighContrastMappingXamlSource = "<UserControl\n" +
                                                             "    x:Class=\"Fluence.Wpf.Demo.Pages.Accessibility.HighContrastMapping\"\n" +
                                                             "    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"\n" +
                                                             "    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"\n" +
                                                             "    xmlns:ui=\"clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf\">\n" +
                                                             "    <Grid>\n" +
                                                             "        <Grid.RowDefinitions>\n" +
                                                             "            <RowDefinition Height=\"Auto\" />\n" +
                                                             "            <RowDefinition Height=\"Auto\" />\n" +
                                                             "        </Grid.RowDefinitions>\n" +
                                                             "        <Border\n" +
                                                             "            Padding=\"12,8\"\n" +
                                                             "            Background=\"{DynamicResource ControlFillColorDefaultBrush}\">\n" +
                                                             "            <Grid>\n" +
                                                             "                <Grid.ColumnDefinitions>\n" +
                                                             "                    <ColumnDefinition Width=\"*\" />\n" +
                                                             "                    <ColumnDefinition Width=\"*\" />\n" +
                                                             "                    <ColumnDefinition Width=\"*\" />\n" +
                                                             "                </Grid.ColumnDefinitions>\n" +
                                                             "                <TextBlock\n" +
                                                             "                    Style=\"{StaticResource BodyStrongTextBlockStyle}\"\n" +
                                                             "                    Foreground=\"{DynamicResource TextFillColorPrimaryBrush}\"\n" +
                                                             "                    Text=\"Fluence key\" />\n" +
                                                             "                <TextBlock\n" +
                                                             "                    Grid.Column=\"1\"\n" +
                                                             "                    Style=\"{StaticResource BodyStrongTextBlockStyle}\"\n" +
                                                             "                    Foreground=\"{DynamicResource TextFillColorPrimaryBrush}\"\n" +
                                                             "                    Text=\"HC system color\" />\n" +
                                                             "                <TextBlock\n" +
                                                             "                    Grid.Column=\"2\"\n" +
                                                             "                    Style=\"{StaticResource BodyStrongTextBlockStyle}\"\n" +
                                                             "                    Foreground=\"{DynamicResource TextFillColorPrimaryBrush}\"\n" +
                                                             "                    Text=\"Live swatch\" />\n" +
                                                             "            </Grid>\n" +
                                                             "        </Border>\n" +
                                                             "        <ItemsControl x:Name=\"HcMappingTable\" Grid.Row=\"1\">\n" +
                                                             "            <ItemsControl.ItemTemplate>\n" +
                                                             "                <DataTemplate>\n" +
                                                             "                    <Border\n" +
                                                             "                        Padding=\"12,6\"\n" +
                                                             "                        BorderBrush=\"{DynamicResource DividerStrokeColorDefaultBrush}\"\n" +
                                                             "                        BorderThickness=\"0,0,0,1\">\n" +
                                                             "                        <Grid>\n" +
                                                             "                            <Grid.ColumnDefinitions>\n" +
                                                             "                                <ColumnDefinition Width=\"*\" />\n" +
                                                             "                                <ColumnDefinition Width=\"*\" />\n" +
                                                             "                                <ColumnDefinition Width=\"*\" />\n" +
                                                             "                            </Grid.ColumnDefinitions>\n" +
                                                             "                            <TextBlock\n" +
                                                             "                                Style=\"{StaticResource CaptionTextBlockStyle}\"\n" +
                                                             "                                Foreground=\"{DynamicResource TextFillColorPrimaryBrush}\"\n" +
                                                             "                                Text=\"{Binding Key}\" />\n" +
                                                             "                            <TextBlock\n" +
                                                             "                                Grid.Column=\"1\"\n" +
                                                             "                                Style=\"{StaticResource CaptionBlockStyle}\"\n" +
                                                             "                                Foreground=\"{DynamicResource TextFillColorSecondaryBrush}\"\n" +
                                                             "                                Text=\"{Binding HcMapping}\" />\n" +
                                                             "                            <Border\n" +
                                                             "                                Grid.Column=\"2\"\n" +
                                                             "                                Width=\"32\"\n" +
                                                             "                                Height=\"16\"\n" +
                                                             "                                HorizontalAlignment=\"Left\"\n" +
                                                             "                                Background=\"{Binding Brush}\"\n" +
                                                             "                                BorderBrush=\"{DynamicResource ControlStrokeColorDefaultBrush}\"\n" +
                                                             "                                BorderThickness=\"1\"\n" +
                                                             "                                CornerRadius=\"2\" />\n" +
                                                             "                        </Grid>\n" +
                                                             "                    </Border>\n" +
                                                             "                </DataTemplate>\n" +
                                                             "            </ItemsControl.ItemTemplate>\n" +
                                                             "        </ItemsControl>\n" +
                                                             "    </Grid>\n" +
                                                             "</UserControl>\n";

        private const string HighContrastMappingCSharpSource = "using System.Collections.Generic;\n" +
                                                               "using System.Windows.Controls;\n" +
                                                               "using System.Windows.Media;\n" +
                                                               "\n" +
                                                               "namespace Fluence.Wpf.Demo.Pages.Accessibility\n" +
                                                               "{\n" +
                                                               "    public partial class HighContrastMapping : UserControl\n" +
                                                               "    {\n" +
                                                               "        private static readonly string[][] HcPairs = new string[][]\n" +
                                                               "        {\n" +
                                                               "            new string[] { \"TextFillColorPrimaryBrush\", \"WindowText\" },\n" +
                                                               "            new string[] { \"TextFillColorSecondaryBrush\", \"WindowText\" },\n" +
                                                               "            new string[] { \"TextFillColorTertiaryBrush\", \"GrayText\" },\n" +
                                                               "            new string[] { \"TextFillColorDisabledBrush\", \"GrayText\" },\n" +
                                                               "            new string[] { \"AccentFillColorDefaultBrush\", \"Highlight\" },\n" +
                                                               "            new string[] { \"AccentTextFillColorPrimaryBrush\", \"HotTrack\" },\n" +
                                                               "            new string[] { \"ControlFillColorDefaultBrush\", \"Control\" },\n" +
                                                               "            new string[] { \"ControlStrokeColorDefaultBrush\", \"ControlDark\" },\n" +
                                                               "            new string[] { \"FocusStrokeColorOuterBrush\", \"Highlight\" },\n" +
                                                               "            new string[] { \"FocusStrokeColorInnerBrush\", \"HighlightText\" },\n" +
                                                               "            new string[] { \"CardBackgroundFillColorDefaultBrush\", \"Control\" },\n" +
                                                               "            new string[] { \"SolidBackgroundFillColorBaseBrush\", \"Window\" },\n" +
                                                               "        };\n" +
                                                               "\n" +
                                                               "        public HighContrastMapping()\n" +
                                                               "        {\n" +
                                                               "            InitializeComponent();\n" +
                                                               "            PopulateHcTable();\n" +
                                                               "        }\n" +
                                                               "\n" +
                                                               "        private void PopulateHcTable()\n" +
                                                               "        {\n" +
                                                               "            List<HcBrushEntry> rows = new List<HcBrushEntry>();\n" +
                                                               "            foreach (string[] pair in HcPairs)\n" +
                                                               "            {\n" +
                                                               "                Brush brush = TryFindResource(pair[0]) as Brush ?? Brushes.Transparent;\n" +
                                                               "                rows.Add(new HcBrushEntry\n" +
                                                               "                {\n" +
                                                               "                    Key = pair[0],\n" +
                                                               "                    HcMapping = pair[1],\n" +
                                                               "                    Brush = brush\n" +
                                                               "                });\n" +
                                                               "            }\n" +
                                                               "\n" +
                                                               "            HcMappingTable.ItemsSource = rows;\n" +
                                                               "        }\n" +
                                                               "    }\n" +
                                                               "\n" +
                                                               "    public sealed class HcBrushEntry\n" +
                                                               "    {\n" +
                                                               "        public string Key { get; set; } = string.Empty;\n" +
                                                               "\n" +
                                                               "        public string HcMapping { get; set; } = string.Empty;\n" +
                                                               "\n" +
                                                               "        public Brush Brush { get; set; } = Brushes.Transparent;\n" +
                                                               "    }\n" +
                                                               "}\n";
        private const string AutomationPropertiesXamlSource = "<UserControl\n" +
                                                              "    x:Class=\"Fluence.Wpf.Demo.Pages.Accessibility.AutomationProperties\"\n" +
                                                              "    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"\n" +
                                                              "    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"\n" +
                                                              "    xmlns:ui=\"clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf\">\n" +
                                                              "    <StackPanel>\n" +
                                                              "        <TextBlock\n" +
                                                              "            Margin=\"0,0,0,8\"\n" +
                                                              "            Foreground=\"{DynamicResource TextFillColorSecondaryBrush}\"\n" +
                                                              "            Text=\"These icon-only buttons all have AutomationProperties.Name so Narrator announces their purpose:\"\n" +
                                                              "            TextWrapping=\"Wrap\" />\n" +
                                                              "        <StackPanel Orientation=\"Horizontal\">\n" +
                                                              "            <ui:Button\n" +
                                                              "                x:Name=\"AutomationNewDocumentButton\"\n" +
                                                              "                Width=\"36\"\n" +
                                                              "                Height=\"36\"\n" +
                                                              "                MinWidth=\"36\"\n" +
                                                              "                Margin=\"0,0,8,0\"\n" +
                                                              "                Padding=\"0\"\n" +
                                                              "                AutomationProperties.Name=\"New document\">\n" +
                                                              "                <ui:FontIcon Glyph=\"&#xE8A5;\" IconFontSize=\"18\" />\n" +
                                                              "            </ui:Button>\n" +
                                                              "            <ui:Button\n" +
                                                              "                x:Name=\"AutomationOpenFileButton\"\n" +
                                                              "                Width=\"36\"\n" +
                                                              "                Height=\"36\"\n" +
                                                              "                MinWidth=\"36\"\n" +
                                                              "                Margin=\"0,0,8,0\"\n" +
                                                              "                Padding=\"0\"\n" +
                                                              "                AutomationProperties.Name=\"Open file\">\n" +
                                                              "                <ui:FontIcon Glyph=\"&#xE8E5;\" IconFontSize=\"18\" />\n" +
                                                              "            </ui:Button>\n" +
                                                              "            <ui:Button\n" +
                                                              "                x:Name=\"AutomationSaveButton\"\n" +
                                                              "                Width=\"36\"\n" +
                                                              "                Height=\"36\"\n" +
                                                              "                MinWidth=\"36\"\n" +
                                                              "                Margin=\"0,0,8,0\"\n" +
                                                              "                Padding=\"0\"\n" +
                                                              "                AutomationProperties.Name=\"Save\">\n" +
                                                              "                <ui:FontIcon Glyph=\"&#xE74E;\" IconFontSize=\"18\" />\n" +
                                                              "            </ui:Button>\n" +
                                                              "            <ui:Button\n" +
                                                              "                x:Name=\"AutomationDeleteButton\"\n" +
                                                              "                Width=\"36\"\n" +
                                                              "                Height=\"36\"\n" +
                                                              "                MinWidth=\"36\"\n" +
                                                              "                Margin=\"0,0,8,0\"\n" +
                                                              "                Padding=\"0\"\n" +
                                                              "                AutomationProperties.Name=\"Delete\">\n" +
                                                              "                <ui:FontIcon Glyph=\"&#xE74D;\" IconFontSize=\"18\" />\n" +
                                                              "            </ui:Button>\n" +
                                                              "            <ui:Button\n" +
                                                              "                x:Name=\"AutomationShareButton\"\n" +
                                                              "                Width=\"36\"\n" +
                                                              "                Height=\"36\"\n" +
                                                              "                MinWidth=\"36\"\n" +
                                                              "                Padding=\"0\"\n" +
                                                              "                AutomationProperties.Name=\"Share\">\n" +
                                                              "                <ui:FontIcon Glyph=\"&#xE72D;\" IconFontSize=\"18\" />\n" +
                                                              "            </ui:Button>\n" +
                                                              "        </StackPanel>\n" +
                                                              "    </StackPanel>\n" +
                                                              "</UserControl>\n";

        private const string AutomationPropertiesCSharpSource = "using System.Windows.Controls;\n" +
                                                                "\n" +
                                                                "namespace Fluence.Wpf.Demo.Pages.Accessibility\n" +
                                                                "{\n" +
                                                                "    public partial class AutomationProperties : UserControl\n" +
                                                                "    {\n" +
                                                                "        public AutomationProperties()\n" +
                                                                "        {\n" +
                                                                "            InitializeComponent();\n" +
                                                                "        }\n" +
                                                                "    }\n" +
                                                                "}\n";
        private const string RtlLayoutXamlSource = "<UserControl\n" +
                                                   "    x:Class=\"Fluence.Wpf.Demo.Pages.Accessibility.RtlLayout\"\n" +
                                                   "    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"\n" +
                                                   "    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"\n" +
                                                   "    xmlns:ui=\"clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf\"\n" +
                                                   "    xmlns:uicore=\"clr-namespace:Fluence.Wpf;assembly=Fluence.Wpf\">\n" +
                                                   "    <StackPanel>\n" +
                                                   "        <ui:ToggleSwitch\n" +
                                                   "            x:Name=\"RtlToggle\"\n" +
                                                   "            Margin=\"0,0,0,12\"\n" +
                                                   "            Checked=\"RtlToggle_Changed\"\n" +
                                                   "            Content=\"Enable RTL on demo card\"\n" +
                                                   "            IsChecked=\"True\"\n" +
                                                   "            Unchecked=\"RtlToggle_Changed\" />\n" +
                                                   "        <ui:Card\n" +
                                                   "            x:Name=\"RtlDemoCard\"\n" +
                                                   "            Padding=\"16\"\n" +
                                                   "            FlowDirection=\"RightToLeft\"\n" +
                                                   "            Variant=\"{x:Static uicore:CardVariant.Outlined}\">\n" +
                                                   "            <StackPanel>\n" +
                                                   "                <TextBlock\n" +
                                                   "                    Margin=\"0,0,0,8\"\n" +
                                                   "                    Style=\"{StaticResource BodyStrongTextBlockStyle}\"\n" +
                                                   "                    Foreground=\"{DynamicResource TextFillColorPrimaryBrush}\"\n" +
                                                   "                    Text=\"نموذج عنصر تحكم\" />\n" +
                                                   "                <StackPanel Orientation=\"Horizontal\">\n" +
                                                   "                    <ui:Button\n" +
                                                   "                        Margin=\"0,0,8,0\"\n" +
                                                   "                        Appearance=\"Accent\"\n" +
                                                   "                        Content=\"زر رئيسي\" />\n" +
                                                   "                    <ui:Button Content=\"إلغاء\" />\n" +
                                                   "                </StackPanel>\n" +
                                                   "            </StackPanel>\n" +
                                                   "        </ui:Card>\n" +
                                                   "    </StackPanel>\n" +
                                                   "</UserControl>\n";

        private const string RtlLayoutCSharpSource = "using System.Windows;\n" +
                                                     "using System.Windows.Controls;\n" +
                                                     "\n" +
                                                     "namespace Fluence.Wpf.Demo.Pages.Accessibility\n" +
                                                     "{\n" +
                                                     "    public partial class RtlLayout : UserControl\n" +
                                                     "    {\n" +
                                                     "        public RtlLayout()\n" +
                                                     "        {\n" +
                                                     "            InitializeComponent();\n" +
                                                     "        }\n" +
                                                     "\n" +
                                                     "        private void RtlToggle_Changed(object sender, RoutedEventArgs e)\n" +
                                                     "        {\n" +
                                                     "            RtlDemoCard.FlowDirection = RtlToggle.IsChecked == true\n" +
                                                     "                ? FlowDirection.RightToLeft\n" +
                                                     "                : FlowDirection.LeftToRight;\n" +
                                                     "        }\n" +
                                                     "    }\n" +
                                                     "}\n";

        // Representative Fluence key → Windows HC system colour pairs shown in the mapping table.
        private static readonly string[][] HcPairs =
        [
            ["TextFillColorPrimaryBrush",       "WindowText"],
            ["TextFillColorSecondaryBrush",     "WindowText"],
            ["TextFillColorTertiaryBrush",      "GrayText"],
            ["TextFillColorDisabledBrush",      "GrayText"],
            ["AccentFillColorDefaultBrush",     "Highlight"],
            ["AccentTextFillColorPrimaryBrush", "HotTrack"],
            ["ControlFillColorDefaultBrush",    "Control"],
            ["ControlStrokeColorDefaultBrush",  "ControlDark"],
            ["FocusStrokeColorOuterBrush",      "Highlight"],
            ["FocusStrokeColorInnerBrush",      "HighlightText"],
            ["CardBackgroundFillColorDefaultBrush", "Control"],
            ["SolidBackgroundFillColorBaseBrush",   "Window"],
        ];

        /// <summary>
        /// Initializes a new instance of <see cref="GalleryAccessibilityPage"/>.
        /// </summary>
        public GalleryAccessibilityPage()
        {
            InitializeComponent();

            DemoSamplePageWiring.Apply(
                (DependencyObject)Content,
                new DemoSampleSource(1, FocusAndTabOrderXamlSource, FocusAndTabOrderCSharpSource),
                new DemoSampleSource(2, HighContrastMappingXamlSource, HighContrastMappingCSharpSource),
                new DemoSampleSource(3, AutomationPropertiesXamlSource, AutomationPropertiesCSharpSource),
                new DemoSampleSource(4, RtlLayoutXamlSource, RtlLayoutCSharpSource));

            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;
            PopulateHcTable();
        }

        private void PopulateHcTable()
        {
            if (HcMappingTable is null)
            {
                return;
            }

            List<HcBrushEntry> rows = [];
            foreach (string[] pair in HcPairs)
            {
                Brush brush = TryFindResource(pair[0]) as Brush ?? Brushes.Transparent;
                rows.Add(new HcBrushEntry
                {
                    Key = pair[0],
                    HcMapping = pair[1],
                    Brush = brush,
                });
            }

            HcMappingTable.ItemsSource = rows;
        }

        private void RtlToggle_Changed(object sender, RoutedEventArgs e)
        {
            if (RtlDemoCard is null || RtlToggle is null)
            {
                return;
            }

            RtlDemoCard.FlowDirection = RtlToggle.IsChecked == true
                ? FlowDirection.RightToLeft
                : FlowDirection.LeftToRight;
        }
    }
}
