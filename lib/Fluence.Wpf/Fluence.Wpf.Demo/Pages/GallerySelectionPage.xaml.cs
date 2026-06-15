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
    public partial class GallerySelectionPage : UserControl
    {
        private bool _updatingSelectAll;

        private const string CheckBoxStatesXamlSource = "<UserControl\n" +
                                                        "    x:Class=\"Fluence.Wpf.Demo.Pages.Selection.CheckBoxStates\"\n" +
                                                        "    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"\n" +
                                                        "    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"\n" +
                                                        "    xmlns:ui=\"clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf\">\n" +
                                                        "    <StackPanel>\n" +
                                                        "        <WrapPanel Margin=\"0,0,0,16\">\n" +
                                                        "            <ui:CheckBox\n" +
                                                        "                x:Name=\"TwoStateCheckBox\"\n" +
                                                        "                Margin=\"0,0,32,10\"\n" +
                                                        "                Content=\"Two-state checkbox\"\n" +
                                                        "                IsChecked=\"True\" />\n" +
                                                        "            <ui:CheckBox\n" +
                                                        "                x:Name=\"ThreeStateCheckBox\"\n" +
                                                        "                Margin=\"0,0,32,10\"\n" +
                                                        "                Content=\"Three-state checkbox\"\n" +
                                                        "                IsChecked=\"{x:Null}\"\n" +
                                                        "                IsThreeState=\"True\" />\n" +
                                                        "            <ui:CheckBox\n" +
                                                        "                Margin=\"0,0,32,10\"\n" +
                                                        "                Content=\"Disabled\"\n" +
                                                        "                IsChecked=\"True\"\n" +
                                                        "                IsEnabled=\"False\" />\n" +
                                                        "        </WrapPanel>\n" +
                                                        "        <StackPanel>\n" +
                                                        "            <ui:CheckBox\n" +
                                                        "                x:Name=\"SelectAllCheckBox\"\n" +
                                                        "                Margin=\"0,0,0,8\"\n" +
                                                        "                Checked=\"SelectAllCheckBox_Changed\"\n" +
                                                        "                Content=\"Select all\"\n" +
                                                        "                Indeterminate=\"SelectAllCheckBox_Changed\"\n" +
                                                        "                IsThreeState=\"True\"\n" +
                                                        "                Unchecked=\"SelectAllCheckBox_Changed\" />\n" +
                                                        "            <ui:CheckBox\n" +
                                                        "                x:Name=\"OptionOneCheckBox\"\n" +
                                                        "                Margin=\"24,0,0,8\"\n" +
                                                        "                Checked=\"OptionCheckBox_Changed\"\n" +
                                                        "                Content=\"Option 1\"\n" +
                                                        "                Unchecked=\"OptionCheckBox_Changed\" />\n" +
                                                        "            <ui:CheckBox\n" +
                                                        "                x:Name=\"OptionTwoCheckBox\"\n" +
                                                        "                Margin=\"24,0,0,8\"\n" +
                                                        "                Checked=\"OptionCheckBox_Changed\"\n" +
                                                        "                Content=\"Option 2\"\n" +
                                                        "                Unchecked=\"OptionCheckBox_Changed\" />\n" +
                                                        "            <ui:CheckBox\n" +
                                                        "                x:Name=\"OptionThreeCheckBox\"\n" +
                                                        "                Margin=\"24,0,0,0\"\n" +
                                                        "                Checked=\"OptionCheckBox_Changed\"\n" +
                                                        "                Content=\"Option 3\"\n" +
                                                        "                Unchecked=\"OptionCheckBox_Changed\" />\n" +
                                                        "        </StackPanel>\n" +
                                                        "    </StackPanel>\n" +
                                                        "</UserControl>\n";

        private const string CheckBoxStatesCSharpSource = "using System.Windows;\n" +
                                                          "using System.Windows.Controls;\n" +
                                                          "\n" +
                                                          "namespace Fluence.Wpf.Demo.Pages.Selection\n" +
                                                          "{\n" +
                                                          "    public partial class CheckBoxStates : UserControl\n" +
                                                          "    {\n" +
                                                          "        private bool updatingSelectAll;\n" +
                                                          "\n" +
                                                          "        public CheckBoxStates()\n" +
                                                          "        {\n" +
                                                          "            InitializeComponent();\n" +
                                                          "        }\n" +
                                                          "\n" +
                                                          "        private void SelectAllCheckBox_Changed(object sender, RoutedEventArgs e)\n" +
                                                          "        {\n" +
                                                          "            if (updatingSelectAll || SelectAllCheckBox.IsChecked is null)\n" +
                                                          "            {\n" +
                                                          "                return;\n" +
                                                          "            }\n" +
                                                          "\n" +
                                                          "            bool isChecked = SelectAllCheckBox.IsChecked == true;\n" +
                                                          "            updatingSelectAll = true;\n" +
                                                          "            OptionOneCheckBox.IsChecked = isChecked;\n" +
                                                          "            OptionTwoCheckBox.IsChecked = isChecked;\n" +
                                                          "            OptionThreeCheckBox.IsChecked = isChecked;\n" +
                                                          "            updatingSelectAll = false;\n" +
                                                          "        }\n" +
                                                          "\n" +
                                                          "        private void OptionCheckBox_Changed(object sender, RoutedEventArgs e)\n" +
                                                          "        {\n" +
                                                          "            int selectedCount = 0;\n" +
                                                          "            selectedCount += OptionOneCheckBox.IsChecked == true ? 1 : 0;\n" +
                                                          "            selectedCount += OptionTwoCheckBox.IsChecked == true ? 1 : 0;\n" +
                                                          "            selectedCount += OptionThreeCheckBox.IsChecked == true ? 1 : 0;\n" +
                                                          "\n" +
                                                          "            updatingSelectAll = true;\n" +
                                                          "            SelectAllCheckBox.IsChecked = selectedCount == 3\n" +
                                                          "                ? true\n" +
                                                          "                : selectedCount == 0 ? false : null;\n" +
                                                          "            updatingSelectAll = false;\n" +
                                                          "        }\n" +
                                                          "    }\n" +
                                                          "}\n";
        private const string RadioButtonGroupsXamlSource = "<UserControl\n" +
                                                           "    x:Class=\"Fluence.Wpf.Demo.Pages.Selection.RadioButtonGroups\"\n" +
                                                           "    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"\n" +
                                                           "    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"\n" +
                                                           "    xmlns:ui=\"clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf\">\n" +
                                                           "    <StackPanel>\n" +
                                                           "        <TextBlock\n" +
                                                           "            Margin=\"0,0,0,8\"\n" +
                                                           "            FontWeight=\"SemiBold\"\n" +
                                                           "            Text=\"Basic group\" />\n" +
                                                           "        <StackPanel Margin=\"0,0,0,16\" Orientation=\"Horizontal\">\n" +
                                                           "            <ui:RadioButton\n" +
                                                           "                Margin=\"0,0,16,0\"\n" +
                                                           "                Content=\"Option A\"\n" +
                                                           "                GroupName=\"BasicGroup\"\n" +
                                                           "                IsChecked=\"True\" />\n" +
                                                           "            <ui:RadioButton\n" +
                                                           "                Margin=\"0,0,16,0\"\n" +
                                                           "                Content=\"Option B\"\n" +
                                                           "                GroupName=\"BasicGroup\" />\n" +
                                                           "            <ui:RadioButton Content=\"Option C\" GroupName=\"BasicGroup\" />\n" +
                                                           "        </StackPanel>\n" +
                                                           "        <TextBlock\n" +
                                                           "            Margin=\"0,0,0,8\"\n" +
                                                           "            FontWeight=\"SemiBold\"\n" +
                                                           "            Text=\"With descriptions\" />\n" +
                                                           "        <ui:RadioButton\n" +
                                                           "            Margin=\"0,0,0,8\"\n" +
                                                           "            Content=\"Standard\"\n" +
                                                           "            Description=\"Uses default application settings\"\n" +
                                                           "            GroupName=\"DescGroup\"\n" +
                                                           "            IsChecked=\"True\" />\n" +
                                                           "        <ui:RadioButton\n" +
                                                           "            Margin=\"0,0,0,8\"\n" +
                                                           "            Content=\"Custom\"\n" +
                                                           "            Description=\"Allows manual configuration\"\n" +
                                                           "            GroupName=\"DescGroup\" />\n" +
                                                           "        <ui:RadioButton\n" +
                                                           "            Content=\"Advanced\"\n" +
                                                           "            Description=\"Expert-level options\"\n" +
                                                           "            GroupName=\"DescGroup\" />\n" +
                                                           "    </StackPanel>\n" +
                                                           "</UserControl>\n";

        private const string RadioButtonGroupsCSharpSource = "using System.Windows.Controls;\n" +
                                                             "\n" +
                                                             "namespace Fluence.Wpf.Demo.Pages.Selection\n" +
                                                             "{\n" +
                                                             "    public partial class RadioButtonGroups : UserControl\n" +
                                                             "    {\n" +
                                                             "        public RadioButtonGroups()\n" +
                                                             "        {\n" +
                                                             "            InitializeComponent();\n" +
                                                             "        }\n" +
                                                             "    }\n" +
                                                             "}\n";
        private const string ToggleSwitchStatesXamlSource = "<UserControl\n" +
                                                            "    x:Class=\"Fluence.Wpf.Demo.Pages.Selection.ToggleSwitchStates\"\n" +
                                                            "    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"\n" +
                                                            "    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"\n" +
                                                            "    xmlns:ui=\"clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf\">\n" +
                                                            "    <ui:StackPanel Spacing=\"8\">\n" +
                                                            "        <TextBlock\n" +
                                                            "            x:Name=\"WorkToggleHeaderText\"\n" +
                                                            "            Text=\"Toggle work\" />\n" +
                                                            "        <ui:StackPanel Orientation=\"Horizontal\">\n" +
                                                            "            <ui:ToggleSwitch\n" +
                                                            "                x:Name=\"WorkToggleSwitch\"\n" +
                                                            "                VerticalAlignment=\"Center\"\n" +
                                                            "                IsChecked=\"True\" />\n" +
                                                            "            <TextBlock\n" +
                                                            "                x:Name=\"WorkToggleStateText\"\n" +
                                                            "                Margin=\"12,0,0,0\"\n" +
                                                            "                VerticalAlignment=\"Center\"\n" +
                                                            "                Text=\"On\" />\n" +
                                                            "            <ui:ProgressRing\n" +
                                                            "                x:Name=\"WorkToggleProgressRing\"\n" +
                                                            "                Width=\"36\"\n" +
                                                            "                Height=\"36\"\n" +
                                                            "                Margin=\"24,0,0,0\"\n" +
                                                            "                VerticalAlignment=\"Center\"\n" +
                                                            "                IsActive=\"{Binding IsChecked, ElementName=WorkToggleSwitch}\"\n" +
                                                            "                IsIndeterminate=\"True\" />\n" +
                                                            "        </ui:StackPanel>\n" +
                                                            "    </ui:StackPanel>\n" +
                                                            "</UserControl>\n";

        private const string ToggleSwitchStatesCSharpSource = "using System.Windows.Controls;\n" +
                                                              "\n" +
                                                              "namespace Fluence.Wpf.Demo.Pages.Selection\n" +
                                                              "{\n" +
                                                              "    public partial class ToggleSwitchStates : UserControl\n" +
                                                              "    {\n" +
                                                              "        public ToggleSwitchStates()\n" +
                                                              "        {\n" +
                                                              "            InitializeComponent();\n" +
                                                              "        }\n" +
                                                              "    }\n" +
                                                              "}\n";
        private const string RatingControlXamlSource = "<UserControl\n" +
                                                       "    x:Class=\"Fluence.Wpf.Demo.Pages.Selection.RatingControlSample\"\n" +
                                                       "    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"\n" +
                                                       "    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"\n" +
                                                       "    xmlns:ui=\"clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf\">\n" +
                                                       "    <ui:StackPanel Spacing=\"14\">\n" +
                                                       "        <ui:RatingControl\n" +
                                                       "            Caption=\"Rate the experience\"\n" +
                                                       "            MaxRating=\"5\"\n" +
                                                       "            Value=\"3\" />\n" +
                                                       "        <ui:RatingControl\n" +
                                                       "            Caption=\"Read-only rating\"\n" +
                                                       "            IsReadOnly=\"True\"\n" +
                                                       "            MaxRating=\"5\"\n" +
                                                       "            Value=\"4\" />\n" +
                                                       "    </ui:StackPanel>\n" +
                                                       "</UserControl>\n";

        private const string RatingControlCSharpSource = "using System.Windows.Controls;\n" +
                                                         "\n" +
                                                         "namespace Fluence.Wpf.Demo.Pages.Selection\n" +
                                                         "{\n" +
                                                         "    public partial class RatingControlSample : UserControl\n" +
                                                         "    {\n" +
                                                         "        public RatingControlSample()\n" +
                                                         "        {\n" +
                                                         "            InitializeComponent();\n" +
                                                         "        }\n" +
                                                         "    }\n" +
                                                         "}\n";
        private const string ComboBoxSelectionXamlSource = "<UserControl\n" +
                                                           "    x:Class=\"Fluence.Wpf.Demo.Pages.Selection.ComboBoxSelection\"\n" +
                                                           "    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"\n" +
                                                           "    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"\n" +
                                                           "    xmlns:ui=\"clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf\">\n" +
                                                           "    <ui:StackPanel Spacing=\"20\">\n" +
                                                           "        <ui:ComboBox\n" +
                                                           "            Width=\"480\"\n" +
                                                           "            HorizontalAlignment=\"Left\"\n" +
                                                           "            PlaceholderText=\"Choose an option...\"\n" +
                                                           "            SelectedIndex=\"-1\">\n" +
                                                           "            <ComboBoxItem Content=\"First item\" />\n" +
                                                           "            <ComboBoxItem Content=\"Second item\" />\n" +
                                                           "            <ComboBoxItem Content=\"Third item\" />\n" +
                                                           "        </ui:ComboBox>\n" +
                                                           "        <ui:ComboBox\n" +
                                                           "            Width=\"480\"\n" +
                                                           "            HorizontalAlignment=\"Left\"\n" +
                                                           "            PlaceholderText=\"With icon\"\n" +
                                                           "            SelectedIndex=\"-1\">\n" +
                                                           "            <ui:ComboBox.Icon>\n" +
                                                           "                <ui:FontIcon Glyph=\"&#xE721;\" IconFontSize=\"14\" />\n" +
                                                           "            </ui:ComboBox.Icon>\n" +
                                                           "            <ComboBoxItem Content=\"Alpha\" />\n" +
                                                           "            <ComboBoxItem Content=\"Beta\" />\n" +
                                                           "            <ComboBoxItem Content=\"Gamma\" />\n" +
                                                           "        </ui:ComboBox>\n" +
                                                           "        <ui:ComboBox\n" +
                                                           "            Width=\"480\"\n" +
                                                           "            HorizontalAlignment=\"Left\"\n" +
                                                           "            IsEnabled=\"False\"\n" +
                                                           "            PlaceholderText=\"Disabled\" />\n" +
                                                           "    </ui:StackPanel>\n" +
                                                           "</UserControl>\n";

        private const string ComboBoxSelectionCSharpSource = "using System.Windows.Controls;\n" +
                                                             "\n" +
                                                             "namespace Fluence.Wpf.Demo.Pages.Selection\n" +
                                                             "{\n" +
                                                             "    public partial class ComboBoxSelection : UserControl\n" +
                                                             "    {\n" +
                                                             "        public ComboBoxSelection()\n" +
                                                             "        {\n" +
                                                             "            InitializeComponent();\n" +
                                                             "        }\n" +
                                                             "    }\n" +
                                                             "}\n";

        public GallerySelectionPage()
        {
            InitializeComponent();

            DemoSamplePageWiring.Apply(
                (DependencyObject)Content,
                new DemoSampleSource(1, CheckBoxStatesXamlSource, CheckBoxStatesCSharpSource),
                new DemoSampleSource(2, RadioButtonGroupsXamlSource, RadioButtonGroupsCSharpSource),
                new DemoSampleSource(3, ToggleSwitchStatesXamlSource, ToggleSwitchStatesCSharpSource),
                new DemoSampleSource(4, RatingControlXamlSource, RatingControlCSharpSource),
                new DemoSampleSource(5, ComboBoxSelectionXamlSource, ComboBoxSelectionCSharpSource));
        }

        private void SelectAllCheckBox_Changed(object? sender, RoutedEventArgs? e)
        {
            if (_updatingSelectAll || SelectAllCheckBox is null || SelectAllCheckBox.IsChecked is null)
            {
                return;
            }

            bool isChecked = SelectAllCheckBox.IsChecked == true;
            _updatingSelectAll = true;
            OptionOneCheckBox.IsChecked = isChecked;
            OptionTwoCheckBox.IsChecked = isChecked;
            OptionThreeCheckBox.IsChecked = isChecked;
            _updatingSelectAll = false;
        }

        private void OptionCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (SelectAllCheckBox is null || OptionOneCheckBox is null || OptionTwoCheckBox is null || OptionThreeCheckBox is null)
            {
                return;
            }

            int selectedCount = 0;
            selectedCount += OptionOneCheckBox.IsChecked == true ? 1 : 0;
            selectedCount += OptionTwoCheckBox.IsChecked == true ? 1 : 0;
            selectedCount += OptionThreeCheckBox.IsChecked == true ? 1 : 0;

            _updatingSelectAll = true;
            SelectAllCheckBox.IsChecked = selectedCount switch
            {
                0 => false,
                3 => true,
                _ => null,
            };
            _updatingSelectAll = false;
        }

        private void SelectionDemoCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboStateLabel is null || SelectionDemoCombo is null)
            {
                return;
            }

            ComboStateLabel.Text = string.Format(
                CultureInfo.CurrentCulture,
                "Selected: {0}",
                SelectionDemoCombo.SelectedItem is ComboBoxItem selectedItem ? selectedItem.Content : "none");
        }
    }
}
