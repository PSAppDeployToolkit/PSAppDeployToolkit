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
        private const string ButtonAppearancesXamlSource = @"<UserControl
    x:Class=""Fluence.Wpf.Demo.Pages.Buttons.ButtonAppearances""
    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
    xmlns:ui=""clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf"">
    <StackPanel>
        <WrapPanel VerticalAlignment=""Center"">
            <ui:Button
                Margin=""0,0,8,8""
                Content=""Standard""
                IsEnabled=""{Binding IsChecked, Source={x:Reference ButtonEnableCheckBox}}"" />
            <ui:Button
                Margin=""0,0,8,8""
                Appearance=""Accent""
                Content=""Accent""
                IsEnabled=""{Binding IsChecked, Source={x:Reference ButtonEnableCheckBox}}"" />
            <ui:Button
                Margin=""0,0,8,8""
                Appearance=""Subtle""
                Content=""Subtle""
                IsEnabled=""{Binding IsChecked, Source={x:Reference ButtonEnableCheckBox}}"" />
        </WrapPanel>
        <ui:CheckBox
            x:Name=""ButtonEnableCheckBox""
            Content=""Enable buttons""
            IsChecked=""True"" />
    </StackPanel>
</UserControl>
";

        private const string ButtonAppearancesCSharpSource = @"using System.Windows.Controls;

namespace Fluence.Wpf.Demo.Pages.Buttons
{
    public partial class ButtonAppearances : UserControl
    {
        public ButtonAppearances()
        {
            InitializeComponent();
        }
    }
}
";
        private const string ButtonIconsXamlSource = @"<UserControl
    x:Class=""Fluence.Wpf.Demo.Pages.Buttons.ButtonIcons""
    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
    xmlns:ui=""clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf"">
    <WrapPanel VerticalAlignment=""Center"">
        <ui:Button Margin=""0,0,8,8"" Content=""Icon Left"">
            <ui:Button.Icon>
                <ui:FontIcon Glyph=""&#xE774;"" IconFontSize=""14"" />
            </ui:Button.Icon>
        </ui:Button>
        <ui:Button
            Margin=""0,0,8,8""
            Content=""Icon Right""
            IconPlacement=""Right"">
            <ui:Button.Icon>
                <ui:FontIcon Glyph=""&#xE8D6;"" IconFontSize=""14"" />
            </ui:Button.Icon>
        </ui:Button>
        <ui:Button
            Margin=""0,0,8,8""
            Appearance=""Subtle""
            Content=""Refresh"">
            <ui:Button.Icon>
                <ui:FontIcon Glyph=""&#xE72C;"" IconFontSize=""14"" />
            </ui:Button.Icon>
        </ui:Button>
    </WrapPanel>
</UserControl>
";

        private const string ButtonIconsCSharpSource = @"using System.Windows.Controls;

namespace Fluence.Wpf.Demo.Pages.Buttons
{
    public partial class ButtonIcons : UserControl
    {
        public ButtonIcons()
        {
            InitializeComponent();
        }
    }
}
";
        private const string HyperlinkButtonsXamlSource = @"<UserControl
    x:Class=""Fluence.Wpf.Demo.Pages.Buttons.HyperlinkButtons""
    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
    xmlns:ui=""clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf"">
    <WrapPanel VerticalAlignment=""Center"">
        <ui:HyperlinkButton
            Margin=""0,0,16,8""
            Content=""Documentation""
            NavigateUri=""https://github.com/sintaxasn/Fluence.Wpf"" />
        <ui:HyperlinkButton
            Margin=""0,0,16,8""
            Content=""Release notes""
            NavigateUri=""https://github.com/sintaxasn/Fluence.Wpf/releases"" />
        <ui:HyperlinkButton
            Margin=""0,0,16,8""
            Content=""With icon""
            NavigateUri=""https://github.com/sintaxasn/Fluence.Wpf"">
            <ui:HyperlinkButton.Icon>
                <ui:FontIcon Glyph=""&#xE71B;"" IconFontSize=""14"" />
            </ui:HyperlinkButton.Icon>
        </ui:HyperlinkButton>
        <ui:HyperlinkButton
            Margin=""0,0,16,8""
            Content=""Disabled""
            IsEnabled=""False"" />
    </WrapPanel>
</UserControl>
";

        private const string HyperlinkButtonsCSharpSource = @"using System.Windows.Controls;

namespace Fluence.Wpf.Demo.Pages.Buttons
{
    public partial class HyperlinkButtons : UserControl
    {
        public HyperlinkButtons()
        {
            InitializeComponent();
        }
    }
}
";
        private const string DropDownButtonsXamlSource = @"<UserControl
    x:Class=""Fluence.Wpf.Demo.Pages.Buttons.DropDownButtons""
    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
    xmlns:ui=""clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf"">
    <WrapPanel VerticalAlignment=""Center"">
        <ui:DropDownButton Margin=""0,0,8,8"" Content=""New"">
            <ui:DropDownButton.Flyout>
                <StackPanel MinWidth=""180"" Margin=""4"">
                    <ui:Button
                        HorizontalAlignment=""Stretch""
                        HorizontalContentAlignment=""Left""
                        Appearance=""Subtle""
                        Content=""Document"" />
                    <ui:Button
                        HorizontalAlignment=""Stretch""
                        HorizontalContentAlignment=""Left""
                        Appearance=""Subtle""
                        Content=""Spreadsheet"" />
                    <ui:Button
                        HorizontalAlignment=""Stretch""
                        HorizontalContentAlignment=""Left""
                        Appearance=""Subtle""
                        Content=""Folder"" />
                </StackPanel>
            </ui:DropDownButton.Flyout>
        </ui:DropDownButton>
        <ui:DropDownButton Margin=""0,0,8,8"" Content=""Details"">
            <ui:DropDownButton.Flyout>
                <StackPanel MaxWidth=""260"" Margin=""12"">
                    <TextBlock
                        Margin=""0,0,0,6""
                        Foreground=""{DynamicResource TextFillColorPrimaryBrush}""
                        Text=""Project status"" />
                    <TextBlock
                        Foreground=""{DynamicResource TextFillColorSecondaryBrush}""
                        Text=""Flyout content can be any WPF content.""
                        TextWrapping=""Wrap"" />
                </StackPanel>
            </ui:DropDownButton.Flyout>
        </ui:DropDownButton>
        <ui:DropDownButton
            Margin=""0,0,8,8""
            Content=""Disabled""
            IsEnabled=""False"">
            <ui:DropDownButton.Flyout>
                <TextBlock Margin=""12"" Text=""Unavailable"" />
            </ui:DropDownButton.Flyout>
        </ui:DropDownButton>
    </WrapPanel>
</UserControl>
";

        private const string DropDownButtonsCSharpSource = @"using System.Windows.Controls;

namespace Fluence.Wpf.Demo.Pages.Buttons
{
    public partial class DropDownButtons : UserControl
    {
        public DropDownButtons()
        {
            InitializeComponent();
        }
    }
}
";
        private const string SplitButtonsXamlSource = @"<UserControl
    x:Class=""Fluence.Wpf.Demo.Pages.Buttons.SplitButtons""
    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
    xmlns:ui=""clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf"">
    <WrapPanel VerticalAlignment=""Center"">
        <ui:SplitButton Margin=""0,0,8,8"" Content=""Save"">
            <ui:SplitButton.Flyout>
                <StackPanel MinWidth=""180"" Margin=""4"">
                    <ui:Button
                        HorizontalAlignment=""Stretch""
                        HorizontalContentAlignment=""Left""
                        Appearance=""Subtle""
                        Content=""Save as"" />
                    <ui:Button
                        HorizontalAlignment=""Stretch""
                        HorizontalContentAlignment=""Left""
                        Appearance=""Subtle""
                        Content=""Save a copy"" />
                    <ui:Button
                        HorizontalAlignment=""Stretch""
                        HorizontalContentAlignment=""Left""
                        Appearance=""Subtle""
                        Content=""Export"" />
                </StackPanel>
            </ui:SplitButton.Flyout>
        </ui:SplitButton>
        <ui:SplitButton
            Margin=""0,0,8,8""
            Appearance=""Accent""
            Content=""Publish"">
            <ui:SplitButton.Flyout>
                <StackPanel MinWidth=""180"" Margin=""4"">
                    <ui:Button
                        HorizontalAlignment=""Stretch""
                        HorizontalContentAlignment=""Left""
                        Appearance=""Subtle""
                        Content=""Publish draft"" />
                    <ui:Button
                        HorizontalAlignment=""Stretch""
                        HorizontalContentAlignment=""Left""
                        Appearance=""Subtle""
                        Content=""Schedule publish"" />
                </StackPanel>
            </ui:SplitButton.Flyout>
        </ui:SplitButton>
        <ui:SplitButton
            Margin=""0,0,8,8""
            Content=""Disabled""
            IsEnabled=""False"">
            <ui:SplitButton.Flyout>
                <TextBlock Margin=""12"" Text=""Unavailable"" />
            </ui:SplitButton.Flyout>
        </ui:SplitButton>
    </WrapPanel>
</UserControl>
";

        private const string SplitButtonsCSharpSource = @"using System.Windows.Controls;

namespace Fluence.Wpf.Demo.Pages.Buttons
{
    public partial class SplitButtons : UserControl
    {
        public SplitButtons()
        {
            InitializeComponent();
        }
    }
}
";
        private const string ToggleAndRepeatButtonsXamlSource = @"<UserControl
    x:Class=""Fluence.Wpf.Demo.Pages.Buttons.ToggleAndRepeatButtons""
    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
    xmlns:ui=""clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf"">
    <WrapPanel VerticalAlignment=""Center"">
        <ui:RepeatButton
            x:Name=""RepeatCounterButton""
            Margin=""0,0,8,8""
            Click=""RepeatCounterButton_Click""
            Content=""Hold to repeat"" />
        <TextBlock
            x:Name=""RepeatButtonCountText""
            Margin=""0,0,16,8""
            VerticalAlignment=""Center""
            Foreground=""{DynamicResource TextFillColorSecondaryBrush}""
            Text=""Clicks: 0"" />
    </WrapPanel>
</UserControl>
";

        private const string ToggleAndRepeatButtonsCSharpSource = @"using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace Fluence.Wpf.Demo.Pages.Buttons
{
    public partial class ToggleAndRepeatButtons : UserControl
    {
        private int repeatButtonClickCount;

        public ToggleAndRepeatButtons()
        {
            InitializeComponent();
        }

        private void RepeatCounterButton_Click(object sender, RoutedEventArgs e)
        {
            repeatButtonClickCount++;
            RepeatButtonCountText.Text = string.Format(
                CultureInfo.CurrentCulture,
                ""Clicks: {0}"",
                repeatButtonClickCount);
        }
    }
}
";

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
                new DemoSampleSource(6, ToggleAndRepeatButtonsXamlSource, ToggleAndRepeatButtonsCSharpSource));
        }

        private void RepeatCounterButton_Click(object sender, RoutedEventArgs e)
        {
            _repeatButtonClickCount++;
            RepeatButtonCountText.Text = string.Format(
                CultureInfo.CurrentCulture,
                "Clicks: {0}",
                _repeatButtonClickCount);
        }
    }
}
