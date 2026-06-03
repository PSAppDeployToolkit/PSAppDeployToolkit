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

        private const string CheckBoxStatesXamlSource = @"<UserControl
    x:Class=""Fluence.Wpf.Demo.Pages.Selection.CheckBoxStates""
    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
    xmlns:ui=""clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf"">
    <StackPanel>
        <WrapPanel Margin=""0,0,0,16"">
            <ui:CheckBox
                x:Name=""TwoStateCheckBox""
                Margin=""0,0,32,10""
                Content=""Two-state checkbox""
                IsChecked=""True"" />
            <ui:CheckBox
                x:Name=""ThreeStateCheckBox""
                Margin=""0,0,32,10""
                Content=""Three-state checkbox""
                IsChecked=""{x:Null}""
                IsThreeState=""True"" />
            <ui:CheckBox
                Margin=""0,0,32,10""
                Content=""Disabled""
                IsChecked=""True""
                IsEnabled=""False"" />
        </WrapPanel>
        <StackPanel>
            <ui:CheckBox
                x:Name=""SelectAllCheckBox""
                Margin=""0,0,0,8""
                Checked=""SelectAllCheckBox_Changed""
                Content=""Select all""
                Indeterminate=""SelectAllCheckBox_Changed""
                IsThreeState=""True""
                Unchecked=""SelectAllCheckBox_Changed"" />
            <ui:CheckBox
                x:Name=""OptionOneCheckBox""
                Margin=""24,0,0,8""
                Checked=""OptionCheckBox_Changed""
                Content=""Option 1""
                Unchecked=""OptionCheckBox_Changed"" />
            <ui:CheckBox
                x:Name=""OptionTwoCheckBox""
                Margin=""24,0,0,8""
                Checked=""OptionCheckBox_Changed""
                Content=""Option 2""
                Unchecked=""OptionCheckBox_Changed"" />
            <ui:CheckBox
                x:Name=""OptionThreeCheckBox""
                Margin=""24,0,0,0""
                Checked=""OptionCheckBox_Changed""
                Content=""Option 3""
                Unchecked=""OptionCheckBox_Changed"" />
        </StackPanel>
    </StackPanel>
</UserControl>
";

        private const string CheckBoxStatesCSharpSource = @"using System.Windows;
using System.Windows.Controls;

namespace Fluence.Wpf.Demo.Pages.Selection
{
    public partial class CheckBoxStates : UserControl
    {
        private bool updatingSelectAll;

        public CheckBoxStates()
        {
            InitializeComponent();
        }

        private void SelectAllCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (updatingSelectAll || SelectAllCheckBox.IsChecked is null)
            {
                return;
            }

            bool isChecked = SelectAllCheckBox.IsChecked == true;
            updatingSelectAll = true;
            OptionOneCheckBox.IsChecked = isChecked;
            OptionTwoCheckBox.IsChecked = isChecked;
            OptionThreeCheckBox.IsChecked = isChecked;
            updatingSelectAll = false;
        }

        private void OptionCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            int selectedCount = 0;
            selectedCount += OptionOneCheckBox.IsChecked == true ? 1 : 0;
            selectedCount += OptionTwoCheckBox.IsChecked == true ? 1 : 0;
            selectedCount += OptionThreeCheckBox.IsChecked == true ? 1 : 0;

            updatingSelectAll = true;
            SelectAllCheckBox.IsChecked = selectedCount == 3
                ? true
                : selectedCount == 0 ? false : null;
            updatingSelectAll = false;
        }
    }
}
";
        private const string RadioButtonGroupsXamlSource = @"<UserControl
    x:Class=""Fluence.Wpf.Demo.Pages.Selection.RadioButtonGroups""
    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
    xmlns:ui=""clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf"">
    <StackPanel>
        <TextBlock
            Margin=""0,0,0,8""
            FontWeight=""SemiBold""
            Text=""Basic group"" />
        <StackPanel Margin=""0,0,0,16"" Orientation=""Horizontal"">
            <ui:RadioButton
                Margin=""0,0,16,0""
                Content=""Option A""
                GroupName=""BasicGroup""
                IsChecked=""True"" />
            <ui:RadioButton
                Margin=""0,0,16,0""
                Content=""Option B""
                GroupName=""BasicGroup"" />
            <ui:RadioButton Content=""Option C"" GroupName=""BasicGroup"" />
        </StackPanel>
        <TextBlock
            Margin=""0,0,0,8""
            FontWeight=""SemiBold""
            Text=""With descriptions"" />
        <ui:RadioButton
            Margin=""0,0,0,8""
            Content=""Standard""
            Description=""Uses default application settings""
            GroupName=""DescGroup""
            IsChecked=""True"" />
        <ui:RadioButton
            Margin=""0,0,0,8""
            Content=""Custom""
            Description=""Allows manual configuration""
            GroupName=""DescGroup"" />
        <ui:RadioButton
            Content=""Advanced""
            Description=""Expert-level options""
            GroupName=""DescGroup"" />
    </StackPanel>
</UserControl>
";

        private const string RadioButtonGroupsCSharpSource = @"using System.Windows.Controls;

namespace Fluence.Wpf.Demo.Pages.Selection
{
    public partial class RadioButtonGroups : UserControl
    {
        public RadioButtonGroups()
        {
            InitializeComponent();
        }
    }
}
";
        private const string ToggleSwitchStatesXamlSource = @"<UserControl
    x:Class=""Fluence.Wpf.Demo.Pages.Selection.ToggleSwitchStates""
    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
    xmlns:ui=""clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf"">
    <ui:StackPanel Spacing=""8"">
        <TextBlock
            x:Name=""WorkToggleHeaderText""
            Text=""Toggle work"" />
        <ui:StackPanel Orientation=""Horizontal"">
            <ui:ToggleSwitch
                x:Name=""WorkToggleSwitch""
                VerticalAlignment=""Center""
                IsChecked=""True"" />
            <TextBlock
                x:Name=""WorkToggleStateText""
                Margin=""12,0,0,0""
                VerticalAlignment=""Center""
                Text=""On"" />
            <ui:ProgressRing
                x:Name=""WorkToggleProgressRing""
                Width=""36""
                Height=""36""
                Margin=""24,0,0,0""
                VerticalAlignment=""Center""
                IsActive=""{Binding IsChecked, ElementName=WorkToggleSwitch}""
                IsIndeterminate=""True"" />
        </ui:StackPanel>
    </ui:StackPanel>
</UserControl>
";

        private const string ToggleSwitchStatesCSharpSource = @"using System.Windows.Controls;

namespace Fluence.Wpf.Demo.Pages.Selection
{
    public partial class ToggleSwitchStates : UserControl
    {
        public ToggleSwitchStates()
        {
            InitializeComponent();
        }
    }
}
";
        private const string RatingControlXamlSource = @"<UserControl
    x:Class=""Fluence.Wpf.Demo.Pages.Selection.RatingControlSample""
    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
    xmlns:ui=""clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf"">
    <ui:StackPanel Spacing=""14"">
        <ui:RatingControl
            Caption=""Rate the experience""
            MaxRating=""5""
            Value=""3"" />
        <ui:RatingControl
            Caption=""Read-only rating""
            IsReadOnly=""True""
            MaxRating=""5""
            Value=""4"" />
    </ui:StackPanel>
</UserControl>
";

        private const string RatingControlCSharpSource = @"using System.Windows.Controls;

namespace Fluence.Wpf.Demo.Pages.Selection
{
    public partial class RatingControlSample : UserControl
    {
        public RatingControlSample()
        {
            InitializeComponent();
        }
    }
}
";
        private const string ComboBoxSelectionXamlSource = @"<UserControl
    x:Class=""Fluence.Wpf.Demo.Pages.Selection.ComboBoxSelection""
    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
    xmlns:ui=""clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf"">
    <ui:StackPanel Spacing=""20"">
        <ui:ComboBox
            Width=""480""
            HorizontalAlignment=""Left""
            PlaceholderText=""Choose an option...""
            SelectedIndex=""-1"">
            <ComboBoxItem Content=""First item"" />
            <ComboBoxItem Content=""Second item"" />
            <ComboBoxItem Content=""Third item"" />
        </ui:ComboBox>
        <ui:ComboBox
            Width=""480""
            HorizontalAlignment=""Left""
            PlaceholderText=""With icon""
            SelectedIndex=""-1"">
            <ui:ComboBox.Icon>
                <ui:FontIcon Glyph=""&#xE721;"" IconFontSize=""14"" />
            </ui:ComboBox.Icon>
            <ComboBoxItem Content=""Alpha"" />
            <ComboBoxItem Content=""Beta"" />
            <ComboBoxItem Content=""Gamma"" />
        </ui:ComboBox>
        <ui:ComboBox
            Width=""480""
            HorizontalAlignment=""Left""
            IsEnabled=""False""
            PlaceholderText=""Disabled"" />
    </ui:StackPanel>
</UserControl>
";

        private const string ComboBoxSelectionCSharpSource = @"using System.Windows.Controls;

namespace Fluence.Wpf.Demo.Pages.Selection
{
    public partial class ComboBoxSelection : UserControl
    {
        public ComboBoxSelection()
        {
            InitializeComponent();
        }
    }
}
";

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
                _ => null
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
