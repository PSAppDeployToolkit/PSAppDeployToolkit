<#
Copyright (c) 2026, Dan Cunningham. All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice,
   this list of conditions and the following disclaimer.

2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

3. Neither the name of the copyright holder nor the names of its
   contributors may be used to endorse or promote products derived from
   this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
#>


# 06-ControlsTour.ps1 - Common Fluence controls (buttons, toggle, checkbox, radios, text box, number
# box, split buttons, a slider, rating, an expander, a list, and icons) inside scrolling cards;
# the toggle drives an InfoBar message from PowerShell. The module handles STA, assembly loading,
# the Application, theming, and the message loop.
# Run: powershell.exe -File 06-ControlsTour.ps1   OR   pwsh -File 06-ControlsTour.ps1

# The -Initialize block keeps the canonical ($Window, $Data) signature shared by every window example;
# this tour holds no cross-click state, so $Data is intentionally unread here.
[Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'Data',
    Justification = 'Initialize blocks keep the canonical ($Window, $Data) signature; this example needs no $Data state.')]
param()

Import-Module "$PSScriptRoot/../src/Fluence.Wpf.PowerShell/Fluence.Wpf.PowerShell.psd1" -Force

$xaml = @'
<fluence:FluenceWindow
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:fluence="http://schemas.fluencewpf.com"
    Title="Fluence.Wpf - Controls tour"
    Width="620"
    Height="640"
    MinWidth="480"
    MinHeight="480"
    SystemBackdropType="Mica"
    ExtendsContentIntoTitleBar="False">
    <Grid Margin="24">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="*" />
            <RowDefinition Height="Auto" />
        </Grid.RowDefinitions>
        <StackPanel x:Name="ControlsTourHeader" Grid.Row="0" Margin="0,0,0,16">
            <TextBlock x:Name="ControlsTourTitle" Text="Controls tour" fluence:TextBlockExtensions.Typography="Title" Foreground="{DynamicResource TextFillColorPrimaryBrush}" TextWrapping="Wrap" />
            <TextBlock Text="Explore common Fluent controls and try their interactions. Toggle the switch to update the status message." Margin="0,8,0,0" fluence:TextBlockExtensions.Typography="Body" Foreground="{DynamicResource TextFillColorSecondaryBrush}" TextWrapping="Wrap" />
        </StackPanel>
        <fluence:SmoothScrollViewer x:Name="ControlsScrollViewer" Grid.Row="1" HorizontalScrollBarVisibility="Disabled" VerticalScrollBarVisibility="Auto">
            <StackPanel>
                <fluence:Card Margin="0,0,0,16">
                    <StackPanel>
                        <TextBlock Text="Buttons" fluence:TextBlockExtensions.Typography="BodyStrong" Foreground="{DynamicResource TextFillColorPrimaryBrush}" Margin="0,0,0,12" />
                        <StackPanel Orientation="Horizontal">
                            <fluence:Button Content="Standard" Margin="0,0,8,0" />
                            <fluence:Button Content="Accent" Appearance="Accent" Margin="0,0,8,0" />
                            <fluence:Button Content="Disabled" IsEnabled="False" />
                        </StackPanel>
                    </StackPanel>
                </fluence:Card>
                <fluence:Card Margin="0,0,0,16">
                    <StackPanel>
                        <TextBlock Text="Selection" fluence:TextBlockExtensions.Typography="BodyStrong" Foreground="{DynamicResource TextFillColorPrimaryBrush}" Margin="0,0,0,12" />
                        <fluence:ToggleSwitch x:Name="DemoToggle" OnContent="On" OffContent="Off" Margin="0,0,0,8" />
                        <fluence:CheckBox Content="I am a checkbox" Margin="0,0,0,8" />
                        <fluence:RadioButton Content="Option A" GroupName="Demo" Margin="0,0,0,8" />
                        <fluence:RadioButton Content="Option B" GroupName="Demo" />
                    </StackPanel>
                </fluence:Card>
                <fluence:Card Margin="0,0,0,16">
                    <StackPanel>
                        <TextBlock Text="Text input" fluence:TextBlockExtensions.Typography="BodyStrong" Foreground="{DynamicResource TextFillColorPrimaryBrush}" Margin="0,0,0,12" />
                        <fluence:TextBox PlaceholderText="Type here" AutomationProperties.Name="Text input" Margin="0,0,0,8" />
                        <fluence:NumberBox Header="A number" Minimum="0" Maximum="100" SpinButtonPlacementMode="Compact" />
                    </StackPanel>
                </fluence:Card>
                <fluence:Card Margin="0,0,0,16">
                    <StackPanel>
                        <TextBlock Text="Buttons with flyouts" fluence:TextBlockExtensions.Typography="BodyStrong" Foreground="{DynamicResource TextFillColorPrimaryBrush}" Margin="0,0,0,12" />
                        <StackPanel Orientation="Horizontal">
                            <fluence:DropDownButton Content="New" Margin="0,0,8,0">
                                <fluence:DropDownButton.Flyout>
                                    <StackPanel MinWidth="160" Margin="8">
                                        <fluence:Button Content="Document" Appearance="Subtle" HorizontalAlignment="Stretch" HorizontalContentAlignment="Left" />
                                        <fluence:Button Content="Folder" Appearance="Subtle" HorizontalAlignment="Stretch" HorizontalContentAlignment="Left" />
                                    </StackPanel>
                                </fluence:DropDownButton.Flyout>
                            </fluence:DropDownButton>
                            <fluence:SplitButton Content="Save" Appearance="Accent" Margin="0,0,8,0">
                                <fluence:SplitButton.Flyout>
                                    <StackPanel MinWidth="160" Margin="8">
                                        <fluence:Button Content="Save as..." Appearance="Subtle" HorizontalAlignment="Stretch" HorizontalContentAlignment="Left" />
                                        <fluence:Button Content="Export" Appearance="Subtle" HorizontalAlignment="Stretch" HorizontalContentAlignment="Left" />
                                    </StackPanel>
                                </fluence:SplitButton.Flyout>
                            </fluence:SplitButton>
                            <fluence:ToggleSplitButton Content="Bulleted list">
                                <fluence:ToggleSplitButton.Flyout>
                                    <StackPanel MinWidth="160" Margin="8">
                                        <fluence:Button Content="Bulleted" Appearance="Subtle" HorizontalAlignment="Stretch" HorizontalContentAlignment="Left" />
                                        <fluence:Button Content="Numbered" Appearance="Subtle" HorizontalAlignment="Stretch" HorizontalContentAlignment="Left" />
                                    </StackPanel>
                                </fluence:ToggleSplitButton.Flyout>
                            </fluence:ToggleSplitButton>
                        </StackPanel>
                    </StackPanel>
                </fluence:Card>
                <fluence:Card Margin="0,0,0,16">
                    <StackPanel>
                        <TextBlock Text="Rating and slider" fluence:TextBlockExtensions.Typography="BodyStrong" Foreground="{DynamicResource TextFillColorPrimaryBrush}" Margin="0,0,0,12" />
                        <fluence:RatingControl Caption="Rate the experience" MaxRating="5" Value="3" Margin="0,0,0,8" />
                        <fluence:Slider AutomationProperties.Name="Value" Minimum="0" Maximum="100" Value="40" />
                    </StackPanel>
                </fluence:Card>
                <fluence:Card Margin="0,0,0,16">
                    <StackPanel>
                        <TextBlock Text="Expander" fluence:TextBlockExtensions.Typography="BodyStrong" Foreground="{DynamicResource TextFillColorPrimaryBrush}" Margin="0,0,0,12" />
                        <fluence:Expander Header="Advanced options">
                            <TextBlock Text="Secondary settings that start collapsed until needed." TextWrapping="Wrap" fluence:TextBlockExtensions.Typography="Body" Foreground="{DynamicResource TextFillColorSecondaryBrush}" />
                        </fluence:Expander>
                    </StackPanel>
                </fluence:Card>
                <fluence:Card Margin="0,0,0,16">
                    <StackPanel>
                        <TextBlock Text="List" fluence:TextBlockExtensions.Typography="BodyStrong" Foreground="{DynamicResource TextFillColorPrimaryBrush}" Margin="0,0,0,12" />
                        <fluence:ListView Height="120">
                            <ListViewItem Content="First item" />
                            <ListViewItem Content="Second item" />
                            <ListViewItem Content="Third item" />
                        </fluence:ListView>
                    </StackPanel>
                </fluence:Card>
                <fluence:Card Margin="0,0,0,16">
                    <StackPanel>
                        <TextBlock Text="Icons" fluence:TextBlockExtensions.Typography="BodyStrong" Foreground="{DynamicResource TextFillColorPrimaryBrush}" Margin="0,0,0,12" />
                        <StackPanel Orientation="Horizontal">
                            <fluence:FontIcon Glyph="&#xE734;" Margin="0,0,16,0" />
                            <fluence:FontIcon Glyph="&#xE713;" Margin="0,0,16,0" />
                            <fluence:FontIcon Glyph="&#xE946;" />
                        </StackPanel>
                    </StackPanel>
                </fluence:Card>
                <fluence:InfoBar x:Name="StatusBar" IsOpen="True" IsClosable="False" Severity="Informational" Title="Toggle state" Message="Flip the switch above to update this message from PowerShell." />
            </StackPanel>
        </fluence:SmoothScrollViewer>
        <Grid x:Name="ControlsTourFooter" Grid.Row="2" Margin="0,16,0,0">
            <fluence:Button x:Name="CloseButton" Content="Close" MinWidth="120" HorizontalAlignment="Right" IsCancel="True" />
        </Grid>
    </Grid>
</fluence:FluenceWindow>
'@

Show-FluenceWindow -Xaml $xaml -WatchSystemTheme -Initialize {
    param($Window, $Data)

    $bar = $Window.FindName('StatusBar')
    $toggle = $Window.FindName('DemoToggle')
    $Window.FindName('CloseButton').add_Click({ Close-FluenceWindow -Window $Window }.GetNewClosure())

    $toggle.add_Checked({ $bar.Message = 'The switch is ON (handled in PowerShell).' }.GetNewClosure())
    $toggle.add_Unchecked({ $bar.Message = 'The switch is OFF (handled in PowerShell).' }.GetNewClosure())
}
