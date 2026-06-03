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

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace Fluence.Wpf.Demo.Pages
{
    public partial class GalleryStatusPage : UserControl
    {
        private const string ProgressBarValueXamlSource = @"<UserControl
    x:Class=""Fluence.Wpf.Demo.Pages.Status.ProgressBarValue""
    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
    xmlns:ui=""clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf""
    xmlns:uicore=""clr-namespace:Fluence.Wpf;assembly=Fluence.Wpf"">
    <StackPanel>
        <ui:ProgressBar
            x:Name=""StandardProgressBar""
            Height=""8""
            Margin=""0,0,0,12""
            HorizontalAlignment=""Stretch""
            Maximum=""100""
            Minimum=""0""
            Value=""{Binding Value, Source={x:Reference ProgressValueNumberBox}}"" />
        <ui:NumberBox
            x:Name=""ProgressValueNumberBox""
            Header=""Value""
            HorizontalAlignment=""Center""
            VerticalAlignment=""Center""
            Maximum=""100""
            Minimum=""0""
            SmallChange=""5""
            SpinButtonPlacementMode=""{x:Static uicore:SpinButtonPlacementMode.Inline}""
            Value=""50"" />
        <Grid>
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width=""*"" />
                <ColumnDefinition Width=""16"" />
                <ColumnDefinition Width=""*"" />
            </Grid.ColumnDefinitions>
            <StackPanel Grid.Column=""0"">
                <TextBlock
                    Margin=""0,0,0,6""
                    Foreground=""{DynamicResource TextFillColorSecondaryBrush}""
                    Text=""Paused"" />
                <ui:ProgressBar
                    Height=""8""
                    HorizontalAlignment=""Stretch""
                    Maximum=""100""
                    Minimum=""0""
                    ProgressMode=""Paused""
                    Value=""62"" />
            </StackPanel>
            <StackPanel Grid.Column=""2"">
                <TextBlock
                    Margin=""0,0,0,6""
                    Foreground=""{DynamicResource TextFillColorSecondaryBrush}""
                    Text=""Failure"" />
                <ui:ProgressBar
                    Height=""8""
                    HorizontalAlignment=""Stretch""
                    Maximum=""100""
                    Minimum=""0""
                    ProgressMode=""Error""
                    Value=""78"" />
            </StackPanel>
        </Grid>
    </StackPanel>
</UserControl>
";

        private const string ProgressBarValueCSharpSource = @"using System.Windows.Controls;

namespace Fluence.Wpf.Demo.Pages.Status
{
    public partial class ProgressBarValue : UserControl
    {
        public ProgressBarValue()
        {
            InitializeComponent();
        }

    }
}
";
        private const string ProgressBarIndeterminateXamlSource = @"<UserControl
    x:Class=""Fluence.Wpf.Demo.Pages.Status.ProgressBarIndeterminate""
    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
    xmlns:ui=""clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf"">
    <StackPanel>
        <ui:ProgressBar
            x:Name=""IndeterminateProgressBar""
            Height=""8""
            Margin=""0,0,0,12""
            HorizontalAlignment=""Stretch""
            ProgressMode=""Indeterminate"" />
        <StackPanel Orientation=""Horizontal"">
            <ui:ToggleSwitch
                x:Name=""IndeterminateToggle""
                Margin=""0,0,12,0""
                Checked=""IndeterminateToggle_Toggled""
                HorizontalAlignment=""Center""
                VerticalAlignment=""Center""
                IsChecked=""True""
                OffContent=""On / Off""
                OnContent=""On / Off""
                Unchecked=""IndeterminateToggle_Toggled"" />
        </StackPanel>
    </StackPanel>
</UserControl>
";

        private const string ProgressBarIndeterminateCSharpSource = @"using System.Windows;
using System.Windows.Controls;
using Fluence.Wpf;

namespace Fluence.Wpf.Demo.Pages.Status
{
    public partial class ProgressBarIndeterminate : UserControl
    {
        public ProgressBarIndeterminate()
        {
            InitializeComponent();
        }

        private void IndeterminateToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (IndeterminateProgressBar is null || IndeterminateToggle is null)
            {
                return;
            }

            IndeterminateProgressBar.ProgressMode = IndeterminateToggle.IsChecked == true
                ? ProgressBarMode.Indeterminate
                : ProgressBarMode.Standard;
        }
    }
}
";
        private const string ProgressBarStepsXamlSource = @"<UserControl
    x:Class=""Fluence.Wpf.Demo.Pages.Status.ProgressBarSteps""
    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
    xmlns:ui=""clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf"">
    <StackPanel>
        <ui:ProgressBar
            x:Name=""StepProgressBar""
            Height=""8""
            Margin=""0,0,0,12""
            HorizontalAlignment=""Stretch""
            CurrentStep=""1""
            ProgressMode=""StepProgress""
            Steps=""10"" />
        <StackPanel Orientation=""Horizontal"">
            <ui:Button
                Margin=""0,0,12,0""
                Click=""ProgressStep_Click""
                Content=""Back""
                Tag=""Back"" />
            <ui:Button
                Margin=""0,0,16,0""
                Appearance=""Accent""
                Click=""ProgressStep_Click""
                Content=""Next""
                Tag=""Next"" />
            <TextBlock
                x:Name=""StepLabel""
                VerticalAlignment=""Center""
                Foreground=""{DynamicResource TextFillColorPrimaryBrush}""
                Text=""Step 1 of 10"" />
        </StackPanel>
    </StackPanel>
</UserControl>
";

        private const string ProgressBarStepsCSharpSource = @"using System;
using System.Windows;
using System.Windows.Controls;

namespace Fluence.Wpf.Demo.Pages.Status
{
    public partial class ProgressBarSteps : UserControl
    {
        public ProgressBarSteps()
        {
            InitializeComponent();
        }

        private void ProgressStep_Click(object sender, RoutedEventArgs e)
        {
            string tag = sender is FrameworkElement button && button.Tag is not null ? button.Tag.ToString() : string.Empty;

            if (string.Equals(tag, ""Next"", StringComparison.OrdinalIgnoreCase))
            {
                if (StepProgressBar.CurrentStep < StepProgressBar.Steps)
                {
                    StepProgressBar.CurrentStep++;
                }
            }
            else if (StepProgressBar.CurrentStep > 0)
            {
                StepProgressBar.CurrentStep--;
            }

            StepLabel.Text = string.Format(""Step {0} of {1}"", StepProgressBar.CurrentStep, StepProgressBar.Steps);
        }
    }
}
";
        private const string ProgressRingsXamlSource = @"<UserControl
    x:Class=""Fluence.Wpf.Demo.Pages.Status.ProgressRings""
    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
    xmlns:ui=""clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf""
    xmlns:uicore=""clr-namespace:Fluence.Wpf;assembly=Fluence.Wpf"">
    <Grid HorizontalAlignment=""Stretch"">
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width=""*"" />
            <ColumnDefinition Width=""*"" />
            <ColumnDefinition Width=""*"" />
            <ColumnDefinition Width=""*"" />
        </Grid.ColumnDefinitions>
        <Grid.RowDefinitions>
            <RowDefinition Height=""Auto"" />
            <RowDefinition Height=""Auto"" />
            <RowDefinition Height=""Auto"" />
        </Grid.RowDefinitions>

        <ui:ProgressRing
            x:Name=""IndeterminateProgressRing""
            Grid.Row=""0""
            Grid.Column=""0""
            Width=""48""
            Height=""48""
            HorizontalAlignment=""Center""
            IsActive=""True""
            IsIndeterminate=""True"" />
        <ui:ToggleSwitch
            Grid.Row=""1""
            Grid.Column=""0""
            Margin=""0,12,0,0""
            HorizontalAlignment=""Center""
            VerticalAlignment=""Center""
            IsChecked=""{Binding IsActive, ElementName=IndeterminateProgressRing, Mode=TwoWay}""
            OffContent=""On / Off""
            OnContent=""On / Off"" />
        <TextBlock
            x:Name=""IndeterminateProgressRingLabel""
            Grid.Row=""2""
            Grid.Column=""0""
            Margin=""0,8,0,0""
            HorizontalAlignment=""Center""
            Foreground=""{DynamicResource TextFillColorSecondaryBrush}""
            Text=""Indeterminate"" />

        <ui:ProgressRing
            x:Name=""DeterminateProgressRing""
            Grid.Row=""0""
            Grid.Column=""1""
            Width=""48""
            Height=""48""
            HorizontalAlignment=""Center""
            IsActive=""True""
            IsIndeterminate=""False""
            Maximum=""100""
            Minimum=""0""
            Value=""50"" />
        <ui:NumberBox
            x:Name=""ProgressRingValueBox""
            Grid.Row=""1""
            Grid.Column=""1""
            Width=""132""
            Margin=""0,24,0,0""
            HorizontalAlignment=""Center""
            VerticalAlignment=""Center""
            Maximum=""100""
            Minimum=""1""
            SmallChange=""1""
            SpinButtonPlacementMode=""{x:Static uicore:SpinButtonPlacementMode.Inline}""
            Value=""{Binding Value, ElementName=DeterminateProgressRing, Mode=TwoWay}"" />
        <TextBlock
            x:Name=""DeterminateProgressRingLabel""
            Grid.Row=""2""
            Grid.Column=""1""
            Margin=""0,8,0,0""
            HorizontalAlignment=""Center""
            Foreground=""{DynamicResource TextFillColorSecondaryBrush}""
            Text=""Determinate"" />

        <ui:ProgressRing
            x:Name=""PausedProgressRing""
            Grid.Row=""0""
            Grid.Column=""2""
            Width=""48""
            Height=""48""
            HorizontalAlignment=""Center""
            IsActive=""True""
            IsIndeterminate=""False""
            Maximum=""100""
            Minimum=""0""
            ProgressState=""{x:Static uicore:ProgressRingState.Paused}""
            Value=""80"" />
        <TextBlock
            Grid.Row=""2""
            Grid.Column=""2""
            Margin=""0,8,0,0""
            HorizontalAlignment=""Center""
            Foreground=""{DynamicResource TextFillColorSecondaryBrush}""
            Text=""Paused"" />

        <ui:ProgressRing
            x:Name=""ErrorProgressRing""
            Grid.Row=""0""
            Grid.Column=""3""
            Width=""48""
            Height=""48""
            HorizontalAlignment=""Center""
            IsActive=""True""
            IsIndeterminate=""False""
            Maximum=""100""
            Minimum=""0""
            ProgressState=""{x:Static uicore:ProgressRingState.Error}""
            Value=""80"" />
        <TextBlock
            Grid.Row=""2""
            Grid.Column=""3""
            Margin=""0,8,0,0""
            HorizontalAlignment=""Center""
            Foreground=""{DynamicResource TextFillColorSecondaryBrush}""
            Text=""Error"" />
    </Grid>
</UserControl>
";

        private const string ProgressRingsCSharpSource = @"using System.Windows.Controls;

namespace Fluence.Wpf.Demo.Pages.Status
{
    public partial class ProgressRings : UserControl
    {
        public ProgressRings()
        {
            InitializeComponent();
        }

    }
}
";
        private const string InfoBarsXamlSource = @"<UserControl
    x:Class=""Fluence.Wpf.Demo.Pages.Status.InfoBars""
    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
    xmlns:ui=""clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf"">
    <StackPanel>
        <ui:InfoBar
            x:Name=""InfoBarInformational""
            Title=""Informational""
            Margin=""0,0,0,8""
            HorizontalAlignment=""Stretch""
            IsOpen=""True""
            Message=""This is a general information message.""
            Severity=""Informational"" />
        <ui:InfoBar
            x:Name=""InfoBarSuccess""
            Title=""Success""
            Margin=""0,0,0,8""
            HorizontalAlignment=""Stretch""
            IsOpen=""True""
            Message=""The operation completed successfully.""
            Severity=""Success"" />
        <ui:InfoBar
            x:Name=""InfoBarWarning""
            Title=""Warning""
            Margin=""0,0,0,8""
            HorizontalAlignment=""Stretch""
            IsOpen=""True""
            Message=""Proceed with caution.""
            Severity=""Warning"" />
        <ui:InfoBar
            x:Name=""InfoBarError""
            Title=""Error""
            Margin=""0,0,0,8""
            HorizontalAlignment=""Stretch""
            IsOpen=""True""
            Message=""Something went wrong.""
            Severity=""Error"">
            <ui:InfoBar.ActionButton>
                <ui:Button Appearance=""Accent"" Content=""Retry"" />
            </ui:InfoBar.ActionButton>
        </ui:InfoBar>
        <ui:Button
            Margin=""0,4,0,0""
            HorizontalAlignment=""Left""
            Click=""ResetInfoBars_Click""
            Content=""Reset All InfoBars"" />
    </StackPanel>
</UserControl>
";

        private const string InfoBarsCSharpSource = @"using System.Windows;
using System.Windows.Controls;

namespace Fluence.Wpf.Demo.Pages.Status
{
    public partial class InfoBars : UserControl
    {
        public InfoBars()
        {
            InitializeComponent();
        }

        private void ResetInfoBars_Click(object sender, RoutedEventArgs e)
        {
            InfoBarInformational.IsOpen = true;
            InfoBarSuccess.IsOpen = true;
            InfoBarWarning.IsOpen = true;
            InfoBarError.IsOpen = true;
        }
    }
}
";

        public GalleryStatusPage()
        {
            InitializeComponent();

            DemoSamplePageWiring.Apply(
                (DependencyObject)Content,
                new DemoSampleSource(1, ProgressBarValueXamlSource, ProgressBarValueCSharpSource),
                new DemoSampleSource(2, ProgressBarIndeterminateXamlSource, ProgressBarIndeterminateCSharpSource),
                new DemoSampleSource(3, ProgressBarStepsXamlSource, ProgressBarStepsCSharpSource),
                new DemoSampleSource(4, ProgressRingsXamlSource, ProgressRingsCSharpSource),
                new DemoSampleSource(5, InfoBarsXamlSource, InfoBarsCSharpSource));

            Loaded += GalleryStatusPage_Loaded;
        }

        private void GalleryStatusPage_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= GalleryStatusPage_Loaded;
            IndeterminateToggle_Toggled(null, null);
        }

        private void IndeterminateToggle_Toggled(object? sender, RoutedEventArgs? e)
        {
            if (!IsLoaded || IndeterminateProgressBar is null || IndeterminateToggle is null)
            {
                return;
            }

            IndeterminateProgressBar.ProgressMode = IndeterminateToggle.IsChecked == true
                ? ProgressBarMode.Indeterminate
                : ProgressBarMode.Standard;
        }

        private void ProgressStep_Click(object sender, RoutedEventArgs e)
        {
            if (StepProgressBar is null)
            {
                return;
            }

            FrameworkElement? button = sender as FrameworkElement;

            string tag = button?.Tag?.ToString() ?? string.Empty;
            if (string.Equals(tag, "Next", StringComparison.OrdinalIgnoreCase))
            {
                if (StepProgressBar.CurrentStep < StepProgressBar.Steps)
                {
                    StepProgressBar.CurrentStep++;
                }
            }
            else if (StepProgressBar.CurrentStep > 0)
            {
                StepProgressBar.CurrentStep--;
            }

            StepLabel.Text = string.Format(CultureInfo.CurrentCulture, "Step {0} of {1}", StepProgressBar.CurrentStep, StepProgressBar.Steps);
        }

        private void ResetInfoBars_Click(object sender, RoutedEventArgs e)
        {
            InfoBarInformational.IsOpen = true;
            InfoBarSuccess.IsOpen = true;
            InfoBarWarning.IsOpen = true;
            InfoBarError.IsOpen = true;
        }
    }
}
