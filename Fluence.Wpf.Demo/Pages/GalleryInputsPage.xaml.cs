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

using System.Windows.Controls;

namespace Fluence.Wpf.Demo.Pages
{
    public partial class GalleryInputsPage : UserControl
    {
        private const string TextBoxInputXamlSource = @"<UserControl
    x:Class=""Fluence.Wpf.Demo.Pages.Inputs.TextBoxInput""
    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
    xmlns:ui=""clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf"">
    <ui:StackPanel Spacing=""20"">
        <ui:TextBox
            Width=""480""
            HorizontalAlignment=""Left""
            PlaceholderText=""Basic text box..."" />
        <ui:TextBox
            Width=""480""
            HorizontalAlignment=""Left""
            PlaceholderText=""Search"">
            <ui:TextBox.Icon>
                <ui:FontIcon Glyph=""&#xE721;"" IconFontSize=""14"" />
            </ui:TextBox.Icon>
        </ui:TextBox>
        <ui:TextBox
            Width=""480""
            HorizontalAlignment=""Left""
            MaxLength=""40""
            PlaceholderText=""Limited to 40 characters..."" />
    </ui:StackPanel>
</UserControl>
";

        private const string TextBoxInputCSharpSource = @"using System.Windows.Controls;

namespace Fluence.Wpf.Demo.Pages.Inputs
{
    public partial class TextBoxInput : UserControl
    {
        public TextBoxInput()
        {
            InitializeComponent();
        }
    }
}
";
        private const string TextBoxValidationXamlSource = @"<UserControl
    x:Class=""Fluence.Wpf.Demo.Pages.Inputs.TextBoxValidation""
    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
    xmlns:ui=""clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf""
    xmlns:uicore=""clr-namespace:Fluence.Wpf;assembly=Fluence.Wpf"">
    <ui:StackPanel Spacing=""20"">
        <ui:TextBox
            Width=""480""
            HorizontalAlignment=""Left""
            HelperText=""Helper text can explain format before validation.""
            PlaceholderText=""With helper text"" />
        <ui:TextBox
            Width=""480""
            HorizontalAlignment=""Left""
            Text=""Valid input""
            ValidationMessage=""Looks good.""
            ValidationState=""{x:Static uicore:ValidationState.Success}"" />
        <ui:TextBox
            Width=""480""
            HorizontalAlignment=""Left""
            Text=""Check this value""
            ValidationMessage=""Review this before continuing.""
            ValidationState=""{x:Static uicore:ValidationState.Warning}"" />
        <ui:TextBox
            Width=""480""
            HorizontalAlignment=""Left""
            Text=""Bad value""
            ValidationMessage=""Please fix this field.""
            ValidationState=""{x:Static uicore:ValidationState.Error}"" />
    </ui:StackPanel>
</UserControl>
";

        private const string TextBoxValidationCSharpSource = @"using System.Windows.Controls;

namespace Fluence.Wpf.Demo.Pages.Inputs
{
    public partial class TextBoxValidation : UserControl
    {
        public TextBoxValidation()
        {
            InitializeComponent();
        }
    }
}
";
        private const string PasswordBoxInputXamlSource = @"<UserControl
    x:Class=""Fluence.Wpf.Demo.Pages.Inputs.PasswordBoxInput""
    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
    xmlns:ui=""clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf"">
    <ui:StackPanel Spacing=""20"">
        <ui:PasswordBox
            Width=""480""
            HorizontalAlignment=""Left""
            PlaceholderText=""Enter password...""
            RevealButtonEnabled=""True"" />
        <ui:PasswordBox
            Width=""480""
            HorizontalAlignment=""Left""
            Password=""CorrectHorse7!""
            RevealButtonEnabled=""True"" />
        <ui:PasswordBox
            Width=""480""
            HorizontalAlignment=""Left""
            IsEnabled=""False""
            PlaceholderText=""Disabled"" />
    </ui:StackPanel>
</UserControl>
";

        private const string PasswordBoxInputCSharpSource = @"using System.Windows.Controls;

namespace Fluence.Wpf.Demo.Pages.Inputs
{
    public partial class PasswordBoxInput : UserControl
    {
        public PasswordBoxInput()
        {
            InitializeComponent();
        }
    }
}
";
        private const string NumberBoxInputXamlSource = @"<UserControl
    x:Class=""Fluence.Wpf.Demo.Pages.Inputs.NumberBoxInput""
    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
    xmlns:ui=""clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf"">
    <ui:StackPanel Spacing=""20"">
        <ui:NumberBox
            Width=""260""
            Header=""Inline""
            Maximum=""100""
            Minimum=""0""
            SpinButtonPlacementMode=""Inline""
            Value=""5"" />
        <ui:NumberBox
            Width=""260""
            Header=""Compact""
            Maximum=""100""
            Minimum=""0""
            SpinButtonPlacementMode=""Compact""
            Value=""25"" />
        <ui:NumberBox
            Width=""260""
            Header=""Keyboard only""
            Maximum=""100""
            Minimum=""0""
            SpinButtonPlacementMode=""Hidden""
            Value=""50"" />
        <ui:NumberBox
            Width=""260""
            Header=""Disabled""
            IsEnabled=""False""
            Value=""42"" />
    </ui:StackPanel>
</UserControl>
";

        private const string NumberBoxInputCSharpSource = @"using System.Windows.Controls;

namespace Fluence.Wpf.Demo.Pages.Inputs
{
    public partial class NumberBoxInput : UserControl
    {
        public NumberBoxInput()
        {
            InitializeComponent();
        }
    }
}
";
        private const string SliderInputXamlSource = @"<UserControl
    x:Class=""Fluence.Wpf.Demo.Pages.Inputs.SliderInput""
    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
    xmlns:ui=""clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf"">
    <ui:StackPanel Spacing=""20"">
        <ui:StackPanel Spacing=""8"">
            <TextBlock Text=""Default"" />
            <ui:Slider
                Maximum=""100""
                Minimum=""0""
                Value=""35"" />
        </ui:StackPanel>
        <ui:StackPanel Spacing=""8"">
            <TextBlock Text=""Snapped to ticks"" />
            <ui:Slider
                IsSnapToTickEnabled=""True""
                Maximum=""10""
                Minimum=""0""
                TickFrequency=""1""
                TickPlacement=""BottomRight""
                Value=""4"" />
        </ui:StackPanel>
        <Grid
            MaxWidth=""292""
            HorizontalAlignment=""Center"">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width=""Auto"" />
                <ColumnDefinition Width=""32"" />
                <ColumnDefinition Width=""Auto"" />
            </Grid.ColumnDefinitions>
            <ui:StackPanel Grid.Column=""0"" Spacing=""8"" HorizontalAlignment=""Center"">
                <TextBlock HorizontalAlignment=""Center"" Text=""Vertical"" />
                <ui:Slider
                    Height=""210""
                    Maximum=""100""
                    Minimum=""0""
                    Orientation=""Vertical""
                    TickFrequency=""10""
                    TickPlacement=""BottomRight""
                    Value=""40"" />
            </ui:StackPanel>
            <ui:StackPanel Grid.Column=""2"" Spacing=""8"" HorizontalAlignment=""Center"">
                <TextBlock HorizontalAlignment=""Center"" Text=""Disabled"" />
                <ui:Slider
                    Height=""210""
                    IsEnabled=""False""
                    Maximum=""100""
                    Minimum=""0""
                    Orientation=""Vertical""
                    Value=""25"" />
            </ui:StackPanel>
        </Grid>
    </ui:StackPanel>
</UserControl>
";

        private const string SliderInputCSharpSource = @"using System.Windows.Controls;

namespace Fluence.Wpf.Demo.Pages.Inputs
{
    public partial class SliderInput : UserControl
    {
        public SliderInput()
        {
            InitializeComponent();
        }
    }
}
";

        public GalleryInputsPage()
        {
            InitializeComponent();

            DemoSamplePageWiring.Apply(
                (System.Windows.DependencyObject)Content,
                new DemoSampleSource(1, TextBoxInputXamlSource, TextBoxInputCSharpSource),
                new DemoSampleSource(2, TextBoxValidationXamlSource, TextBoxValidationCSharpSource),
                new DemoSampleSource(3, PasswordBoxInputXamlSource, PasswordBoxInputCSharpSource),
                new DemoSampleSource(4, NumberBoxInputXamlSource, NumberBoxInputCSharpSource),
                new DemoSampleSource(5, SliderInputXamlSource, SliderInputCSharpSource));
        }

    }
}
