# ControlsTour.ps1 - Common Fluence controls (buttons, toggle, checkbox, radios, text box, number
# box, split buttons, a slider, rating, an expander, a list, and icons) inside scrolling cards;
# the toggle drives an InfoBar message from PowerShell. The module handles STA, assembly loading,
# the Application, theming, and the message loop.
# Run: powershell.exe -File ControlsTour.ps1   OR   pwsh -File ControlsTour.ps1

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
    SystemBackdropType="Mica"
    ExtendsContentIntoTitleBar="False">
    <fluence:SmoothScrollViewer>
        <StackPanel Margin="24">
            <fluence:Card Margin="0,0,0,16">
                <StackPanel>
                    <TextBlock Text="Buttons" fluence:TextBlockExtensions.Typography="Subtitle" Foreground="{DynamicResource TextFillColorPrimaryBrush}" Margin="0,0,0,8" />
                    <StackPanel Orientation="Horizontal">
                        <fluence:Button Content="Standard" Margin="0,0,8,0" />
                        <fluence:Button Content="Accent" Appearance="Accent" Margin="0,0,8,0" />
                        <fluence:Button Content="Disabled" IsEnabled="False" />
                    </StackPanel>
                </StackPanel>
            </fluence:Card>
            <fluence:Card Margin="0,0,0,16">
                <StackPanel>
                    <TextBlock Text="Selection" fluence:TextBlockExtensions.Typography="Subtitle" Foreground="{DynamicResource TextFillColorPrimaryBrush}" Margin="0,0,0,8" />
                    <fluence:ToggleSwitch x:Name="DemoToggle" OnContent="On" OffContent="Off" Margin="0,0,0,8" />
                    <fluence:CheckBox Content="I am a checkbox" Margin="0,0,0,8" />
                    <fluence:RadioButton Content="Option A" GroupName="Demo" Margin="0,0,0,4" />
                    <fluence:RadioButton Content="Option B" GroupName="Demo" />
                </StackPanel>
            </fluence:Card>
            <fluence:Card Margin="0,0,0,16">
                <StackPanel>
                    <TextBlock Text="Text input" fluence:TextBlockExtensions.Typography="Subtitle" Foreground="{DynamicResource TextFillColorPrimaryBrush}" Margin="0,0,0,8" />
                    <fluence:TextBox PlaceholderText="Type here" Margin="0,0,0,8" />
                    <fluence:NumberBox Header="A number" Minimum="0" Maximum="100" SpinButtonPlacementMode="Compact" />
                </StackPanel>
            </fluence:Card>
            <fluence:Card Margin="0,0,0,16">
                <StackPanel>
                    <TextBlock Text="Buttons with flyouts" fluence:TextBlockExtensions.Typography="Subtitle" Foreground="{DynamicResource TextFillColorPrimaryBrush}" Margin="0,0,0,8" />
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
                    <TextBlock Text="Rating and slider" fluence:TextBlockExtensions.Typography="Subtitle" Foreground="{DynamicResource TextFillColorPrimaryBrush}" Margin="0,0,0,8" />
                    <fluence:RatingControl Caption="Rate the experience" MaxRating="5" Value="3" Margin="0,0,0,8" />
                    <fluence:Slider Minimum="0" Maximum="100" Value="40" />
                </StackPanel>
            </fluence:Card>
            <fluence:Card Margin="0,0,0,16">
                <StackPanel>
                    <TextBlock Text="Expander" fluence:TextBlockExtensions.Typography="Subtitle" Foreground="{DynamicResource TextFillColorPrimaryBrush}" Margin="0,0,0,8" />
                    <fluence:Expander Header="Advanced options">
                        <TextBlock Text="Secondary settings that start collapsed until needed." TextWrapping="Wrap" Foreground="{DynamicResource TextFillColorSecondaryBrush}" Margin="0,8,0,0" />
                    </fluence:Expander>
                </StackPanel>
            </fluence:Card>
            <fluence:Card Margin="0,0,0,16">
                <StackPanel>
                    <TextBlock Text="List" fluence:TextBlockExtensions.Typography="Subtitle" Foreground="{DynamicResource TextFillColorPrimaryBrush}" Margin="0,0,0,8" />
                    <fluence:ListView Height="120">
                        <ListViewItem Content="First item" />
                        <ListViewItem Content="Second item" />
                        <ListViewItem Content="Third item" />
                    </fluence:ListView>
                </StackPanel>
            </fluence:Card>
            <fluence:Card Margin="0,0,0,16">
                <StackPanel>
                    <TextBlock Text="Icons" fluence:TextBlockExtensions.Typography="Subtitle" Foreground="{DynamicResource TextFillColorPrimaryBrush}" Margin="0,0,0,8" />
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
</fluence:FluenceWindow>
'@

Show-FluenceWindow -Xaml $xaml -WatchSystemTheme -Initialize {
    param($Window, $Data)

    $bar = $Window.FindName('StatusBar')
    $toggle = $Window.FindName('DemoToggle')

    $toggle.add_Checked({ $bar.Message = 'The switch is ON (handled in PowerShell).' }.GetNewClosure())
    $toggle.add_Unchecked({ $bar.Message = 'The switch is OFF (handled in PowerShell).' }.GetNewClosure())
}
