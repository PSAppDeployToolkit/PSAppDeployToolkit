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
        private const string FocusAndTabOrderXamlSource = @"<UserControl
    x:Class=""Fluence.Wpf.Demo.Pages.Accessibility.FocusAndTabOrder""
    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
    xmlns:ui=""clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf"">
    <StackPanel>
        <Grid x:Name=""KeyboardSupportPrimaryControls"" Margin=""0,0,0,8"">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width=""*"" />
                <ColumnDefinition Width=""*"" />
                <ColumnDefinition Width=""*"" />
                <ColumnDefinition Width=""*"" />
            </Grid.ColumnDefinitions>
            <Grid.RowDefinitions>
                <RowDefinition Height=""Auto"" />
                <RowDefinition Height=""Auto"" />
            </Grid.RowDefinitions>
            <ui:Button
                Grid.Row=""0""
                Grid.Column=""0""
                Margin=""0,0,12,12""
                HorizontalAlignment=""Stretch""
                VerticalAlignment=""Center""
                AutomationProperties.Name=""First focusable button""
                Content=""Button 1"" />
            <ui:Button
                Grid.Row=""0""
                Grid.Column=""1""
                Margin=""0,0,12,12""
                HorizontalAlignment=""Stretch""
                VerticalAlignment=""Center""
                AutomationProperties.Name=""Second focusable button""
                Content=""Button 2"" />
            <ui:TextBox
                Grid.Row=""0""
                Grid.Column=""2""
                Margin=""0,0,12,12""
                HorizontalAlignment=""Stretch""
                VerticalAlignment=""Center""
                AutomationProperties.Name=""Focusable text input""
                PlaceholderText=""TextBox"" />
            <ui:ComboBox
                Grid.Row=""0""
                Grid.Column=""3""
                Margin=""0,0,0,12""
                HorizontalAlignment=""Stretch""
                VerticalAlignment=""Center""
                AutomationProperties.Name=""Focusable combo box"">
                <ComboBoxItem Content=""Option A"" IsSelected=""True"" />
                <ComboBoxItem Content=""Option B"" />
            </ui:ComboBox>
            <ui:CheckBox
                Grid.Row=""1""
                Grid.Column=""0""
                Margin=""0,0,12,0""
                HorizontalAlignment=""Left""
                VerticalAlignment=""Center""
                AutomationProperties.Name=""Focusable checkbox""
                Content=""CheckBox"" />
            <ui:ToggleSwitch
                Grid.Row=""1""
                Grid.Column=""1""
                Margin=""0,0,12,0""
                HorizontalAlignment=""Left""
                VerticalAlignment=""Center""
                AutomationProperties.Name=""Focusable toggle"" />
            <ui:Slider
                Grid.Row=""1""
                Grid.Column=""2""
                Margin=""0,0,12,0""
                HorizontalAlignment=""Stretch""
                VerticalAlignment=""Center""
                AutomationProperties.Name=""Focusable slider""
                Maximum=""100""
                Minimum=""0""
                Value=""40"" />
            <ui:HyperlinkButton
                Grid.Row=""1""
                Grid.Column=""3""
                HorizontalAlignment=""Stretch""
                VerticalAlignment=""Center""
                AutomationProperties.Name=""Focusable hyperlink""
                Content=""HyperlinkButton"" />
        </Grid>

        <TextBlock
            Margin=""0,12,0,8""
            Foreground=""{DynamicResource TextFillColorSecondaryBrush}""
            Text=""The buttons below have an explicit reverse tab order: 3, 2, 1."" />
        <Grid
            x:Name=""KeyboardSupportExplicitOrderControls""
            Margin=""0,0,0,8""
            KeyboardNavigation.TabNavigation=""Local"">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width=""*"" />
                <ColumnDefinition Width=""*"" />
                <ColumnDefinition Width=""*"" />
            </Grid.ColumnDefinitions>
            <ui:Button
                x:Name=""ExplicitTabOrderThirdButton""
                Grid.Column=""0""
                Margin=""0,0,12,0""
                HorizontalAlignment=""Stretch""
                VerticalAlignment=""Center""
                Content=""Tab order: 3""
                TabIndex=""3"" />
            <ui:Button
                x:Name=""ExplicitTabOrderSecondButton""
                Grid.Column=""1""
                Margin=""0,0,12,0""
                HorizontalAlignment=""Stretch""
                VerticalAlignment=""Center""
                Content=""Tab order: 2""
                TabIndex=""2"" />
            <ui:Button
                x:Name=""ExplicitTabOrderFirstButton""
                Grid.Column=""2""
                HorizontalAlignment=""Stretch""
                VerticalAlignment=""Center""
                Appearance=""Accent""
                Content=""Tab order: 1 (first)""
                TabIndex=""1"" />
        </Grid>
    </StackPanel>
</UserControl>
";

        private const string FocusAndTabOrderCSharpSource = @"using System.Windows.Controls;

namespace Fluence.Wpf.Demo.Pages.Accessibility
{
    public partial class FocusAndTabOrder : UserControl
    {
        public FocusAndTabOrder()
        {
            InitializeComponent();
        }
    }
}
";
        private const string HighContrastMappingXamlSource = @"<UserControl
    x:Class=""Fluence.Wpf.Demo.Pages.Accessibility.HighContrastMapping""
    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
    xmlns:ui=""clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf"">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height=""Auto"" />
            <RowDefinition Height=""Auto"" />
        </Grid.RowDefinitions>
        <Border
            Padding=""12,8""
            Background=""{DynamicResource ControlFillColorDefaultBrush}"">
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width=""*"" />
                    <ColumnDefinition Width=""*"" />
                    <ColumnDefinition Width=""*"" />
                </Grid.ColumnDefinitions>
                <TextBlock
                    Style=""{StaticResource BodyStrongTextBlockStyle}""
                    Foreground=""{DynamicResource TextFillColorPrimaryBrush}""
                    Text=""Fluence key"" />
                <TextBlock
                    Grid.Column=""1""
                    Style=""{StaticResource BodyStrongTextBlockStyle}""
                    Foreground=""{DynamicResource TextFillColorPrimaryBrush}""
                    Text=""HC system color"" />
                <TextBlock
                    Grid.Column=""2""
                    Style=""{StaticResource BodyStrongTextBlockStyle}""
                    Foreground=""{DynamicResource TextFillColorPrimaryBrush}""
                    Text=""Live swatch"" />
            </Grid>
        </Border>
        <ItemsControl x:Name=""HcMappingTable"" Grid.Row=""1"">
            <ItemsControl.ItemTemplate>
                <DataTemplate>
                    <Border
                        Padding=""12,6""
                        BorderBrush=""{DynamicResource DividerStrokeColorDefaultBrush}""
                        BorderThickness=""0,0,0,1"">
                        <Grid>
                            <Grid.ColumnDefinitions>
                                <ColumnDefinition Width=""*"" />
                                <ColumnDefinition Width=""*"" />
                                <ColumnDefinition Width=""*"" />
                            </Grid.ColumnDefinitions>
                            <TextBlock
                                Style=""{StaticResource CaptionTextBlockStyle}""
                                Foreground=""{DynamicResource TextFillColorPrimaryBrush}""
                                Text=""{Binding Key}"" />
                            <TextBlock
                                Grid.Column=""1""
                                Style=""{StaticResource CaptionBlockStyle}""
                                Foreground=""{DynamicResource TextFillColorSecondaryBrush}""
                                Text=""{Binding HcMapping}"" />
                            <Border
                                Grid.Column=""2""
                                Width=""32""
                                Height=""16""
                                HorizontalAlignment=""Left""
                                Background=""{Binding Brush}""
                                BorderBrush=""{DynamicResource ControlStrokeColorDefaultBrush}""
                                BorderThickness=""1""
                                CornerRadius=""2"" />
                        </Grid>
                    </Border>
                </DataTemplate>
            </ItemsControl.ItemTemplate>
        </ItemsControl>
    </Grid>
</UserControl>
";

        private const string HighContrastMappingCSharpSource = @"using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;

namespace Fluence.Wpf.Demo.Pages.Accessibility
{
    public partial class HighContrastMapping : UserControl
    {
        private static readonly string[][] HcPairs = new string[][]
        {
            new string[] { ""TextFillColorPrimaryBrush"", ""WindowText"" },
            new string[] { ""TextFillColorSecondaryBrush"", ""WindowText"" },
            new string[] { ""TextFillColorTertiaryBrush"", ""GrayText"" },
            new string[] { ""TextFillColorDisabledBrush"", ""GrayText"" },
            new string[] { ""AccentFillColorDefaultBrush"", ""Highlight"" },
            new string[] { ""AccentTextFillColorPrimaryBrush"", ""HotTrack"" },
            new string[] { ""ControlFillColorDefaultBrush"", ""Control"" },
            new string[] { ""ControlStrokeColorDefaultBrush"", ""ControlDark"" },
            new string[] { ""FocusStrokeColorOuterBrush"", ""Highlight"" },
            new string[] { ""FocusStrokeColorInnerBrush"", ""HighlightText"" },
            new string[] { ""CardBackgroundFillColorDefaultBrush"", ""Control"" },
            new string[] { ""SolidBackgroundFillColorBaseBrush"", ""Window"" },
        };

        public HighContrastMapping()
        {
            InitializeComponent();
            PopulateHcTable();
        }

        private void PopulateHcTable()
        {
            List<HcBrushEntry> rows = new List<HcBrushEntry>();
            foreach (string[] pair in HcPairs)
            {
                Brush brush = TryFindResource(pair[0]) as Brush ?? Brushes.Transparent;
                rows.Add(new HcBrushEntry
                {
                    Key = pair[0],
                    HcMapping = pair[1],
                    Brush = brush
                });
            }

            HcMappingTable.ItemsSource = rows;
        }
    }

    public sealed class HcBrushEntry
    {
        public string Key { get; set; } = string.Empty;

        public string HcMapping { get; set; } = string.Empty;

        public Brush Brush { get; set; } = Brushes.Transparent;
    }
}
";
        private const string AutomationPropertiesXamlSource = @"<UserControl
    x:Class=""Fluence.Wpf.Demo.Pages.Accessibility.AutomationProperties""
    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
    xmlns:ui=""clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf"">
    <StackPanel>
        <TextBlock
            Margin=""0,0,0,8""
            Foreground=""{DynamicResource TextFillColorSecondaryBrush}""
            Text=""These icon-only buttons all have AutomationProperties.Name so Narrator announces their purpose:""
            TextWrapping=""Wrap"" />
        <StackPanel Orientation=""Horizontal"">
            <ui:Button
                x:Name=""AutomationNewDocumentButton""
                Width=""36""
                Height=""36""
                MinWidth=""36""
                Margin=""0,0,8,0""
                Padding=""0""
                AutomationProperties.Name=""New document"">
                <ui:FontIcon Glyph=""&#xE8A5;"" IconFontSize=""18"" />
            </ui:Button>
            <ui:Button
                x:Name=""AutomationOpenFileButton""
                Width=""36""
                Height=""36""
                MinWidth=""36""
                Margin=""0,0,8,0""
                Padding=""0""
                AutomationProperties.Name=""Open file"">
                <ui:FontIcon Glyph=""&#xE8E5;"" IconFontSize=""18"" />
            </ui:Button>
            <ui:Button
                x:Name=""AutomationSaveButton""
                Width=""36""
                Height=""36""
                MinWidth=""36""
                Margin=""0,0,8,0""
                Padding=""0""
                AutomationProperties.Name=""Save"">
                <ui:FontIcon Glyph=""&#xE74E;"" IconFontSize=""18"" />
            </ui:Button>
            <ui:Button
                x:Name=""AutomationDeleteButton""
                Width=""36""
                Height=""36""
                MinWidth=""36""
                Margin=""0,0,8,0""
                Padding=""0""
                AutomationProperties.Name=""Delete"">
                <ui:FontIcon Glyph=""&#xE74D;"" IconFontSize=""18"" />
            </ui:Button>
            <ui:Button
                x:Name=""AutomationShareButton""
                Width=""36""
                Height=""36""
                MinWidth=""36""
                Padding=""0""
                AutomationProperties.Name=""Share"">
                <ui:FontIcon Glyph=""&#xE72D;"" IconFontSize=""18"" />
            </ui:Button>
        </StackPanel>
    </StackPanel>
</UserControl>
";

        private const string AutomationPropertiesCSharpSource = @"using System.Windows.Controls;

namespace Fluence.Wpf.Demo.Pages.Accessibility
{
    public partial class AutomationProperties : UserControl
    {
        public AutomationProperties()
        {
            InitializeComponent();
        }
    }
}
";
        private const string RtlLayoutXamlSource = @"<UserControl
    x:Class=""Fluence.Wpf.Demo.Pages.Accessibility.RtlLayout""
    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
    xmlns:ui=""clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf""
    xmlns:uicore=""clr-namespace:Fluence.Wpf;assembly=Fluence.Wpf"">
    <StackPanel>
        <ui:ToggleSwitch
            x:Name=""RtlToggle""
            Margin=""0,0,0,12""
            Checked=""RtlToggle_Changed""
            Content=""Enable RTL on demo card""
            IsChecked=""True""
            Unchecked=""RtlToggle_Changed"" />
        <ui:Card
            x:Name=""RtlDemoCard""
            Padding=""16""
            FlowDirection=""RightToLeft""
            Variant=""{x:Static uicore:CardVariant.Outlined}"">
            <StackPanel>
                <TextBlock
                    Margin=""0,0,0,8""
                    Style=""{StaticResource BodyStrongTextBlockStyle}""
                    Foreground=""{DynamicResource TextFillColorPrimaryBrush}""
                    Text=""نموذج عنصر تحكم"" />
                <StackPanel Orientation=""Horizontal"">
                    <ui:Button
                        Margin=""0,0,8,0""
                        Appearance=""Accent""
                        Content=""زر رئيسي"" />
                    <ui:Button Content=""إلغاء"" />
                </StackPanel>
            </StackPanel>
        </ui:Card>
    </StackPanel>
</UserControl>
";

        private const string RtlLayoutCSharpSource = @"using System.Windows;
using System.Windows.Controls;

namespace Fluence.Wpf.Demo.Pages.Accessibility
{
    public partial class RtlLayout : UserControl
    {
        public RtlLayout()
        {
            InitializeComponent();
        }

        private void RtlToggle_Changed(object sender, RoutedEventArgs e)
        {
            RtlDemoCard.FlowDirection = RtlToggle.IsChecked == true
                ? FlowDirection.RightToLeft
                : FlowDirection.LeftToRight;
        }
    }
}
";

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

    /// <summary>
    /// Row model for the High Contrast brush mapping table.
    /// </summary>
    public sealed class HcBrushEntry
    {
        /// <summary>
        /// Gets or sets the Fluence resource key (e.g. <c>TextFillColorPrimaryBrush</c>).
        /// </summary>
        public string? Key { get; set; }

        /// <summary>
        /// Gets or sets the Windows HC system colour name (e.g. <c>WindowText</c>).
        /// </summary>
        public string? HcMapping { get; set; }

        /// <summary>
        /// Gets or sets the live brush resolved from the current theme dictionary.
        /// </summary>
        public Brush? Brush { get; set; }
    }
}
